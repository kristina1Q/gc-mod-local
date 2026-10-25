# 少女美术馆 (Girls' Creation) 全本地化汉化补丁 / GCMod Local

[![Release](https://img.shields.io/github/v/release/kristina1Q/gc-mod-local?color=blue&label=Release)](https://github.com/kristina1Q/gc-mod-local/releases)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)
[![BepInEx](https://img.shields.io/badge/BepInEx-6.0_IL2CPP-purple)](https://github.com/BepInEx/BepInEx)
[![Status](https://img.shields.io/badge/Offline-100%25_Local-green)](#)

本项目为 DMM GAMES / FANZA 平台二次元策略 RPG 游戏《**ガールズクリエイション - 少女美術館 -**》（Girls' Creation / 少女美术馆）提供高品质、完全离线、开箱即用的简体中文汉化支持。

---

## 📖 背景与项目致敬 (Attribution)

本项目基于开源社区知名原汉化项目 [**anosu/girlscreation-translation**](https://github.com/anosu/girlscreation-translation) 的优秀架构与初始翻译数据进行深度重构与衍生开发。

原版插件主要依赖在线 GitHub Raw CDN 动态请求翻译资源。在国内网络环境下，常受到 DNS 污染、CDN 连接超时及网络卡顿的影响，且随着游戏版本演进，许多新登场角色技法、主数据表及动态 UI 无法被完全覆盖。

为了提供极致稳定、无网络门槛且零风险的游戏体验，本项目进行了以下全方位重构与升级：
1. **全面纯本地化运行**：摒弃在线网络请求依赖，将 1,900+ 篇剧情、字典、技法数据与字体全部内嵌于本地，秒级加载，离线可用，对游戏账号无任何网络特征干扰。
2. **逆向破解 MasterLoader 加密算法**：攻克了 Unity IL2CPP 下 `master.dmm` 核心数据表的 TripleDES-ECB-MD5 动态解密，实现了全角色 1,772 个主动技法与 807 个被动技法的 100% 完整中文化覆盖。
3. **新增 TextMeshPro 动态 UI 拦截 (UiPatch)**：在游戏渲染底层挂钩 TextMeshPro 文本流，实现首页导航栏、技能详情弹窗、战斗界面等 1,000+ UI 词条的深度汉化。

---

## ✨ 核心特性

- 🌐 **100% 离线单机运行**：无需梯子、代理或配置 host，彻底告别 CDN 超时与卡死。
- ⚔️ **全技能与主数据 100% 汉化**：主动技能、被动技能、特性效果、消耗数值等全部中文化，新角色技能不再是日文或乱码。
- 🖼️ **深度 UI 汉化**：涵盖主界面导航（任务、编队、角色、工房、扭蛋、商店）、战斗结算、角色技能属性弹窗等。
- 📚 **完整收录 1,900+ 篇剧情文本**：包含主线章节、支线剧情、角色好感剧情及全年龄/R18剧情文本。
- 🔤 **高精中文矢量字体替换**：内置清晰易读的日系中文字体，字符排版紧凑不溢出，无缺字漏字。
- 🔒 **安全轻量**：纯客户端展示层拦截，不修改游戏核心逻辑与服务器通信包，无封号风险。

---

## 📥 安装使用教程

### 步骤 1：下载补丁
前往 [GitHub Releases](https://github.com/kristina1Q/gc-mod-local/releases) 页面，下载最新发布的压缩包：
- **`GCMod-Local-v7.1.0.zip`**

### 步骤 2：解压并复制到游戏目录
1. 解压下载的 `GCMod-Local-v7.1.0.zip`。
2. 将压缩包解压出的所有文件与文件夹，复制并粘贴到您的《Girls' Creation》游戏安装根目录下（即 **`GC.exe` 所在的文件夹**）。

解压后游戏根目录结构预览：
```text
<游戏安装根目录>/
├── GC.exe                         (游戏原有可执行文件)
├── GameAssembly.dll               (游戏原有核心库)
├── GC_Data/                       (游戏原有资源文件夹)
├── winhttp.dll                    [补丁加载入口]
├── doorstop_config.ini            [补丁引导配置]
├── .doorstop_version              [版本标记]
├── dotnet/                        [IL2CPP 运行时环境]
└── BepInEx/                       [核心插件与本地翻译库]
    ├── config/
    │   └── GCMod.cfg              (补丁配置文件)
    ├── core/                      (BepInEx 核心程序)
    └── plugins/
        └── GCMod/
            ├── GCMod.dll          (汉化核心补丁模块)
            ├── Utility.dll        (辅助工具库)
            ├── fonts/             (本地中文字体包)
            └── translations/      (全本地翻译数据目录)
```

### 步骤 3：启动游戏
直接双击运行游戏根目录下的 **`GC.exe`**，即可直接畅享全中文界面与剧情！

---

## 🗑️ 卸载说明

如需卸载汉化或还原游戏纯净状态，只需删除游戏根目录下的以下文件即可，完全不影响原版游戏：
- `BepInEx/` 文件夹
- `dotnet/` 文件夹
- `winhttp.dll`
- `doorstop_config.ini`
- `.doorstop_version`

---

## ⚙️ 个性化配置 (`GCMod.cfg`)

配置文件路径：`BepInEx/config/GCMod.cfg`（首次启动后自动生成或可直接编辑已带配置）：

| 配置项 | 默认值 | 说明 |
| :--- | :--- | :--- |
| `[Translation] Enabled` | `true` | 是否启用汉化补丁（剧情、技能与 UI） |
| `[Translation] CustomPath` | *(留空)* | 自定义外部翻译路径；留空时自动加载本地内置字典 |
| `[Translation] CDN` | `offline` | 离线模式标识，无需连接外网 |
| `[Battle] IsSkipCutin` | `false` | 是否跳过大招释放与变身动画（可设为 `true` 加快战斗） |
| `[General] FrameRate` | `60` | 游戏锁定帧率（支持 60 / 120 / 144） |
| `[General] CheckUpdate` | `false` | 离线版请保持 `false` |

---

## 🛠️ 仓库架构与工具链

本项目开源完整插件源代码与逆向维护工具链：

```text
gc-mod-local/
├── src/                           # C# 插件源代码 (.NET 6 / BepInEx 6 IL2CPP)
│   ├── GCMod/                     # 插件生命周期、配置管理、热键支持
│   ├── GCMod.Patches/             # UI 挂钩 (UiPatch.cs)、主数据拦截 (MasterDataPatch.cs)
│   ├── GCMod.Services/            # 本地翻译缓存与管理器 (TranslationManager.cs)
│   └── GCMod.csproj
├── tools/                         # 自动化提取与翻译工具链 (Python)
│   ├── gc_translator_tool.py      # MasterLoader TripleDES 解密与技能导出/应用综合工具
│   ├── build_ui_json.py           # TextMeshPro UI 字典生成与合并器
│   ├── extract_all_skills.py      # 游戏全技能提取与缺失率统计器
│   ├── apply_all_skill_translations.py # 技能文本本地化应用脚本
│   └── inspector/                 # IL2CPP 反射调试工具
├── translations/                  # 本地化翻译数据源
│   └── zh-Hans/
│       ├── master.json            # 100% 汉化的技能与主数据
│       ├── ui.json                # 界面 UI 汉化映射字典
│       ├── names.json             # 角色/NPC 中文译名表
│       ├── manifest.json          # 资源哈希与目录清单
│       └── novels/                # 1,900+ 篇剧情章节本地文本
└── README.md
```

---

## 🤝 致谢与贡献 (Acknowledgements)

- 感谢原开源项目 [anosu/girlscreation-translation](https://github.com/anosu/girlscreation-translation) 及其全部贡献者在剧情翻译、基础框架搭建方面付出的辛勤努力。
- 感谢 [BepInEx](https://github.com/BepInEx/BepInEx) 与 [Il2CppInterop](https://github.com/BepInEx/Il2CppInterop) 为 Unity IL2CPP 平台 Mod 注入提供的优秀基础设施。
- 感谢广大热爱《Girls' Creation》的美术馆馆长们的支持与反馈！

---

## ⚖️ 免责声明 (Disclaimer)

1. 本汉化补丁为玩家社区自发维护的非官方衍生项目，所有文字翻译及工具仅供学习、交流与研究使用。
2. 严禁将本补丁用于任何商业盈利目的（包括但不限于整合售卖、赞助打包等）。
3. 游戏本体的所有版权、商标及相关知识产权均归原作者、开发商以及 DMM GAMES / FANZA 所有。
