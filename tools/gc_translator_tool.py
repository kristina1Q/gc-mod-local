# -*- coding: utf-8 -*-
"""
Girls' Creation 汉化本地化辅助与剧情解包工具
功能：
1. unpack: 解包游戏缓存中的剧情脚本到 c:\\修复\\gc解包（支持按 ID 过滤、自动跳过已解包文件）
2. extract: 提取解包剧情的日文文本，生成待翻译的 JSON 模板（保留已有翻译，增量追加新条目）
3. manifest: 增量重新计算并生成 manifest.json 的哈希索引（复用未变动文件的缓存哈希）
4. sync: 增量同步本地翻译到游戏插件目录（只拷贝变动和新增文件，避免重复复制）
"""

import os
import sys

try:
    if sys.stdout:
        sys.stdout.reconfigure(encoding='utf-8')
    if sys.stderr:
        sys.stderr.reconfigure(encoding='utf-8')
except Exception:
    pass

import csv
import io
import json
import hashlib
import shutil
from pathlib import Path
from functools import cmp_to_key

# 默认路径配置
GAME_CACHE_DIR = Path(r"c:\Users\34632\girlscreation_r\GC_Data\Caches\notinit")
GAME_PLUGIN_TRANS_DIR = Path(r"c:\Users\34632\girlscreation_r\BepInEx\plugins\GCMod\translations\zh-Hans")
LOCAL_TRANS_DIR = Path(r"c:\修复\local\translations\zh-Hans")
UNPACK_DIR = Path(r"c:\修复\gc解包")

def compare_keys(a, b):
    for ca, cb in zip(a, b):
        oa, ob = ord(ca), ord(cb)
        if oa != ob:
            return -1 if oa < ob else 1
    return -1 if len(a) < len(b) else (1 if len(a) > len(b) else 0)

def compute_hash(data):
    """按照 GCMod 算法计算 TranslationHash"""
    buf = []
    def append_table(table, prefix=''):
        keys = sorted(table.keys(), key=cmp_to_key(compare_keys))
        for k in keys:
            v = table[k]
            p = prefix + k
            if isinstance(v, dict):
                append_table(v, p + '\u0001')
            else:
                buf.append(p + '\0' + (str(v) if v is not None else '') + '\0')
    append_table(data)
    full_str = ''.join(buf)
    return hashlib.md5(full_str.encode('utf-8')).hexdigest().lower()

def cmd_unpack(target_id=None, force=False):
    """解包游戏缓存中的剧情 AssetBundle（支持按 ID 过滤，默认跳过已解包文件）"""
    import UnityPy

    UNPACK_DIR.mkdir(parents=True, exist_ok=True)
    cache_roots = [
        GAME_CACHE_DIR / "novel_script",
        GAME_CACHE_DIR / "novelchara_main",
        GAME_CACHE_DIR / "novelchara_main_r18",
        GAME_CACHE_DIR / "novelchara_sub",
        GAME_CACHE_DIR / "novelchara_sub_r18",
    ]

    total_unpacked = 0
    skipped_count = 0
    filter_hint = f" (目标ID/关键字: '{target_id}')" if target_id else ""
    print(f"[*] 正在扫描游戏缓存目录{filter_hint}...")

    for root in cache_roots:
        if not root.exists():
            continue
        for data_file in root.rglob("__data"):
            dmm_dir = data_file.parent.parent
            if not dmm_dir.name.endswith(".dmm"):
                continue
            base_name = dmm_dir.name[:-4]
            if base_name.endswith("_voice") or base_name.endswith("_image"):
                continue

            # 支持按 ID 或角色名前缀筛选
            if target_id and target_id not in base_name:
                continue

            # 快速检查目标输出文件是否已存在且较新
            expected_out = UNPACK_DIR / f"{base_name}.txt"
            if not force and expected_out.exists() and expected_out.stat().st_mtime >= data_file.stat().st_mtime:
                skipped_count += 1
                continue

            try:
                env = UnityPy.load(str(data_file))
                for obj in env.objects:
                    if obj.type.name == "TextAsset":
                        data = obj.read()
                        script_text = getattr(data, "m_Script", "")
                        name = getattr(data, "m_Name", base_name)
                        out_path = UNPACK_DIR / f"{name}.txt"

                        if not force and out_path.exists() and out_path.stat().st_mtime >= data_file.stat().st_mtime:
                            skipped_count += 1
                            continue

                        with open(out_path, "w", encoding="utf-8") as f:
                            f.write(script_text)
                        print(f"  [+] 解包成功: {name}.txt ({len(script_text)} 字符)")
                        total_unpacked += 1
            except Exception as e:
                print(f"  [-] 解包失败 {data_file}: {e}")

    print(f"\n[OK] 解包完成！新解包: {total_unpacked} 个，已跳过已存在的未变动文件: {skipped_count} 个。保存在: {UNPACK_DIR}")

def extract_from_script(script_text):
    """解析剧本文本，提取标题和台词对话"""
    lines = script_text.strip().splitlines()
    dialogue_lines = []
    speakers = set()

    for line in lines:
        line = line.strip()
        if not line:
            continue
        try:
            reader = csv.reader(io.StringIO(line))
            parts = next(reader)
        except Exception:
            continue

        if not parts:
            continue

        cmd = parts[0]
        args = parts[1:]

        # 标题命令: title,标题名
        if cmd == "title" and len(args) >= 1:
            title = args[0].strip()
            if title:
                dialogue_lines.append(title)

        # 对话命令: message,说话人,台词,头像
        elif cmd == "message" and len(args) >= 2:
            speaker = args[0].strip()
            msg = args[1]
            if speaker:
                speakers.add(speaker)
            if msg:
                dialogue_lines.append(msg)

        # 语音同步对话: msgvoicesync,目标,说话人,台词,头像,语音id
        elif cmd == "msgvoicesync" and len(args) >= 3:
            speaker = args[1].strip()
            msg = args[2]
            if speaker:
                speakers.add(speaker)
            if msg:
                dialogue_lines.append(msg)

    return dialogue_lines, speakers

def cmd_extract(target_id=None):
    """提取解包脚本并生成待翻译的 JSON 模板（安全保留已有翻译，增量合并）"""
    if not UNPACK_DIR.exists():
        print(f"[!] 解包目录不存在: {UNPACK_DIR}，请先运行 unpack 命令。")
        return

    novels_dir = LOCAL_TRANS_DIR / "novels"
    novels_dir.mkdir(parents=True, exist_ok=True)

    names_file = LOCAL_TRANS_DIR / "names.json"
    known_names = {}
    if names_file.exists():
        with open(names_file, "r", encoding="utf-8") as f:
            known_names = json.load(f)

    txt_files = list(UNPACK_DIR.glob("*.txt"))
    if target_id:
        txt_files = [f for f in txt_files if target_id in f.stem]

    if not txt_files:
        print(f"[!] 未找到匹配的解包剧情文件 (目标ID: {target_id})")
        return

    print(f"[*] 正在为 {len(txt_files)} 个剧本提取文本模板...")
    all_new_speakers = set()
    total_new_lines = 0

    for txt_file in txt_files:
        # 提取 ID: 例如 evs_20003601 -> 20003601, hmn_40109141 -> 40109141
        stem = txt_file.stem
        # 剥离前缀: evs_, mas_, hmn_, hmr_ 等
        parts = stem.split("_")
        novel_id = parts[-1] if len(parts) > 1 else stem

        with open(txt_file, "r", encoding="utf-8") as f:
            script_text = f.read()

        dialogue_lines, speakers = extract_from_script(script_text)

        # 检查是否有未翻译的人名
        for sp in speakers:
            if sp not in known_names:
                all_new_speakers.add(sp)

        out_json_path = novels_dir / f"{novel_id}.json"
        existing_translations = {}
        if out_json_path.exists():
            try:
                with open(out_json_path, "r", encoding="utf-8") as f:
                    existing_translations = json.load(f)
            except Exception:
                pass

        # 合并新旧文本：绝对保留已翻译文本，仅新增未翻译条目
        result_dict = {}
        new_count = 0
        translated_count = 0
        for line in dialogue_lines:
            if line in existing_translations:
                trans = existing_translations[line]
                result_dict[line] = trans
                if trans:
                    translated_count += 1
                else:
                    new_count += 1
            else:
                result_dict[line] = "" # 留空待翻译
                new_count += 1

        total_new_lines += new_count

        with open(out_json_path, "w", encoding="utf-8") as f:
            json.dump(result_dict, f, ensure_ascii=False, indent=2)

        status = f"共 {len(result_dict)} 句 (已汉化: {translated_count} 句, 待翻译: {new_count} 句)"
        print(f"  [+] 剧情模板: novels/{novel_id}.json -> {status}")

    if all_new_speakers:
        print("\n[!] 提示：检测到以下人物名在 names.json 中未找到，若需要可在 names.json 中添加中文译名:")
        for sp in sorted(list(all_new_speakers)):
            print(f"    \"{sp}\": \"\"")

    print(f"\n[OK] 提取完成！本次新增待翻译条目共 {total_new_lines} 句。模板存放在: {novels_dir}")

def cmd_manifest():
    """增量重新计算并更新 manifest.json 的 MD5 哈希"""
    print("[*] 正在计算本地翻译哈希值并生成 manifest.json...")

    names_file = LOCAL_TRANS_DIR / "names.json"
    master_file = LOCAL_TRANS_DIR / "master.json"
    novels_dir = LOCAL_TRANS_DIR / "novels"
    manifest_file = LOCAL_TRANS_DIR / "manifest.json"

    # 读取旧的 manifest 以支持增量缓存哈希
    old_novels_map = {}
    manifest_mtime = 0
    if manifest_file.exists():
        try:
            manifest_mtime = manifest_file.stat().st_mtime
            with open(manifest_file, "r", encoding="utf-8") as f:
                old_novels_map = json.load(f).get("novels", {})
        except Exception:
            pass

    names_hash = ""
    if names_file.exists():
        with open(names_file, "r", encoding="utf-8") as f:
            names_hash = compute_hash(json.load(f))

    master_hash = ""
    if master_file.exists():
        with open(master_file, "r", encoding="utf-8") as f:
            master_hash = compute_hash(json.load(f))

    novels_map = {}
    rehashed = 0
    cached = 0
    if novels_dir.exists():
        novel_files = sorted(list(novels_dir.glob("*.json")), key=lambda p: p.stem)
        for nf in novel_files:
            stem = nf.stem
            # 如果文件修改时间早于当前 manifest 且已有哈希，直接复用
            if stem in old_novels_map and nf.stat().st_mtime <= manifest_mtime:
                novels_map[stem] = old_novels_map[stem]
                cached += 1
            else:
                try:
                    with open(nf, "r", encoding="utf-8") as f:
                        data = json.load(f)
                        novels_map[stem] = compute_hash(data)
                        rehashed += 1
                except Exception as e:
                    print(f"  [-] 解析失败 {nf.name}: {e}")

    manifest_data = {
        "hash": hashlib.md5((names_hash + master_hash + str(len(novels_map))).encode("utf-8")).hexdigest(),
        "names": names_hash,
        "master": master_hash,
        "novels": novels_map
    }

    with open(manifest_file, "w", encoding="utf-8") as f:
        json.dump(manifest_data, f, ensure_ascii=False, indent=2)

    print(f"[OK] manifest.json 生成成功！共 {len(novels_map)} 篇剧情 (新计算: {rehashed}，复用缓存: {cached})。")

def cmd_sync(target_id=None):
    """将 local\\translations 增量同步至游戏目录（只拷贝变动或新增文件）"""
    print(f"[*] 正在增量同步本地翻译到游戏目录: {GAME_PLUGIN_TRANS_DIR} ...")
    GAME_PLUGIN_TRANS_DIR.mkdir(parents=True, exist_ok=True)
    (GAME_PLUGIN_TRANS_DIR / "novels").mkdir(parents=True, exist_ok=True)

    # 复制主文件
    synced_main = 0
    for fname in ["manifest.json", "master.json", "names.json", "ui.json"]:
        src = LOCAL_TRANS_DIR / fname
        if src.exists():
            dst = GAME_PLUGIN_TRANS_DIR / fname
            if not dst.exists() or src.stat().st_mtime > dst.stat().st_mtime or src.stat().st_size != dst.stat().st_size:
                shutil.copy2(src, dst)
                print(f"  [+] 已同步主配置: {fname} ({src.stat().st_size} 字节)")
                synced_main += 1

    # 复制剧情文件（增量比对）
    src_novels = list((LOCAL_TRANS_DIR / "novels").glob("*.json"))
    if target_id:
        src_novels = [f for f in src_novels if target_id in f.stem]

    dst_novels_dir = GAME_PLUGIN_TRANS_DIR / "novels"
    count = 0
    skipped = 0
    for nf in src_novels:
        dst = dst_novels_dir / nf.name
        if not dst.exists() or nf.stat().st_mtime > dst.stat().st_mtime or nf.stat().st_size != dst.stat().st_size:
            shutil.copy2(nf, dst)
            count += 1
        else:
            skipped += 1

    if count > 0 or synced_main > 0:
        print(f"[OK] 同步完成！共更新 {count} 篇剧情文件，跳过未变动的 {skipped} 篇剧情。")
    else:
        print(f"[OK] 所有文件均已是最新状态，无需重复复制 ({skipped} 篇剧情已一致)。")

def main():
    if len(sys.argv) < 2:
        print("""
使用方法:
    python gc_translator_tool.py unpack [ID] [--force]    增量解包游戏缓存 (可指定角色/剧情ID，--force强制全量覆盖)
    python gc_translator_tool.py extract [ID]            提取剧情台词生成待翻译模板 (保留已有翻译，增量追加)
    python gc_translator_tool.py manifest                增量快速计算并生成 manifest.json 校验索引
    python gc_translator_tool.py sync [ID]               增量同步翻译到游戏目录 (只拷贝新增或变动的文件)
    python gc_translator_tool.py all [ID]                按顺序执行: 解包 -> 提取 -> 生成索引 -> 同步
""")
        return

    action = sys.argv[1].lower()
    target_id = None
    force = False

    for arg in sys.argv[2:]:
        if arg.lower() in ["--force", "-f"]:
            force = True
        elif not target_id:
            target_id = arg

    if action == "unpack":
        cmd_unpack(target_id, force)
    elif action == "extract":
        cmd_extract(target_id)
    elif action == "manifest":
        cmd_manifest()
    elif action == "sync":
        cmd_sync(target_id)
    elif action == "all":
        cmd_unpack(target_id, force)
        cmd_extract(target_id)
        cmd_manifest()
        cmd_sync(target_id)
    else:
        print(f"[!] 未知操作: {action}")

if __name__ == "__main__":
    main()
