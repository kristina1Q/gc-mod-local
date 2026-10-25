import sys, os, json, re

sys.stdout.reconfigure(encoding='utf-8')

MASTER_JSON_PATH = r"c:\修复\local\translations\zh-Hans\master.json"

# 1. Active Skill Names (22)
ACTIVE_NAMES = {
    "月映の怒り": "月映之怒",
    "月光ステッキ": "月光手杖",
    "月明りの下の": "月光之下的",
    "綻ぶ花のように": "如绽放的花朵般",
    "華やかな祝福": "华丽的祝福",
    "花装いの": "繁花盛装的",
    "泰斗の荒彫り": "泰斗的粗雕",
    "ライフワーク": "毕生事业",
    "未来に続く": "延续至未来",
    "妬心の一突き": "嫉心的一刺",
    "湧き上がる羨慕": "涌现的羡慕",
    "嫉妬穿孔の": "嫉妒穿孔的",
    "揺るがしの砕棍": "震荡的碎棍",
    "スーパーぶんまわし": "超级挥砸",
    "虚飾献身の": "虚饰献身的",
    "全力の一撃": "全力一击",
    "容赦なき鉄槌": "无情铁槌",
    "猛烈なる最終宣告": "猛烈的最终宣告",
    "未知の高鳴り": "未知的鸣响",
    "自分を信じて": "相信自己",
    "夢幻の": "梦幻的",
    "宵闇の": "宵暗的"
}

# 2. Active Skill Descriptions (48)
ACTIVE_DESCS = {
    "範囲内の敵に威力[[main_effects_power_1]%]の<help=magical_dmg>特殊ダメージ</help>（風<help=element>エレメント</help>）/命中対象を[[side_effects_power_1]]マス<help=knock_back>弾き飛ばし</help>/ビリジアンの味方全員に<help=agility>敏捷</help>[[side_effects_power_2]%]増加＜[[side_effects_turn_2]ターン]＞(重複可)/[[side_rate_3]%]の確率で<help=re_action>再行動</help>（上限[[side_effects_power_3]]回）":
        "对范围内的敌人造成威力[[main_effects_power_1]%]的<help=magical_dmg>特殊伤害</help>（风<help=element>元素</help>）/将命中目标<help=knock_back>击飞</help>[[side_effects_power_1]]格/铬绿的我方全员<help=agility>敏捷</help>增加[[side_effects_power_2]%]<[[side_effects_turn_2]回合]>（可叠加）/以[[side_rate_3]%]的概率<help=re_action>再行动</help>（上限[[side_effects_power_3]]次）",

    "範囲内の敵に威力[[main_effects_power_1]%]の<help=magical_dmg>特殊ダメージ</help>（雷<help=element>エレメント</help>）/攻撃対象が少ないほどダメージ上昇（最大[[side_effects_power_1]%]）/ボスエネミーにダメージ[[side_effects_power_2]%]上昇/命中対象に確率で<help=stun>気絶</help>付与＜[[side_effects_turn_3]ターン]＞/[[side_rate_4]%]の確率で<help=re_action>再行動</help>（上限[[side_effects_power_4]]回）/技法（+[[effect_attributes_5_3]]）・絶技（+[[effect_attributes_5_4]]）の発動カウント合計（[[effect_attributes_5_2]]）で[[Askill_name_20113003]]に変化":
        "对范围内的敌人造成威力[[main_effects_power_1]%]的<help=magical_dmg>特殊伤害</help>（雷<help=element>元素</help>）/攻击目标越少伤害越高（最大[[side_effects_power_1]%]）/对首领敌人伤害提升[[side_effects_power_2]%]/命中目标概率赋予<help=stun>眩晕</help><[[side_effects_turn_3]回合]>/以[[side_rate_4]%]的概率<help=re_action>再行动</help>（上限[[side_effects_power_4]]次）/技法（+[[effect_attributes_5_3]]）・绝技（+[[effect_attributes_5_4]]）发动计数合计达到（[[effect_attributes_5_2]]）时变为[[Askill_name_20113003]]",

    "範囲内の敵に威力[[main_effects_power_1]%]の<help=magical_dmg>特殊ダメージ</help>（炎<help=element>エレメント</help>）/系統：彫刻にダメージ[[side_effects_power_1]%]上昇/バイオレットの味方全員に<help=wind_shield>風シールド</help>付与[[side_effects_power_2]]/「[[ml_name_3]]」召喚（最大[[side_effects_power_3]]）/命中対象に確率で<help=burn>火傷</help>付与＜[[side_effects_turn_4]ターン]＞":
        "对范围内的敌人造成威力[[main_effects_power_1]%]的<help=magical_dmg>特殊伤害</help>（火<help=element>元素</help>）/对系谱：雕塑伤害提升[[side_effects_power_1]%]/紫罗兰的我方全员赋予<help=wind_shield>风护盾</help>[[side_effects_power_2]]/召唤“[[ml_name_3]]”（最多[[side_effects_power_3]]）/命中目标概率赋予<help=burn>灼伤</help><[[side_effects_turn_4]回合]>",

    "敵に威力[[main_effects_power_1]%]の<help=magical_dmg>特殊ダメージ</help>（炎<help=element>エレメント</help>）/<help=burn>火傷</help>状態ならダメージ[[side_effects_power_1]%]上昇/オブジェクト召喚を使用するたびにダメージ[[side_effects_power_2]%]上昇（上限[[effect_attributes_2_1]]回）/敵撃破時にこの技法の<help=cool_time>クールタイム</help>[[side_effects_power_3]]短縮":
        "对敌人造成威力[[main_effects_power_1]%]的<help=magical_dmg>特殊伤害</help>（火<help=element>元素</help>）/目标处于<help=burn>灼伤</help>状态时伤害提升[[side_effects_power_1]%]/每次使用造物召唤时伤害提升[[side_effects_power_2]%](上限[[effect_attributes_2_1]]次)/击破敌人时本技法的<help=cool_time>冷却时间</help>缩短[[side_effects_power_3]]",

    "威力[[main_effects_power_1]%]/「[[ml_name_1]]」召喚（最大[[side_effects_power_1]]）/バイオレットの味方全員に<help=will>精神</help>[[side_effects_power_2]%]増加＜[[side_effects_turn_2]ターン]＞(重複可)/[[Askill_name_20215002]]の<help=cool_time>クールタイム</help>[[side_effects_power_3]]短縮/":
        "威力[[main_effects_power_1]%]/召唤“[[ml_name_1]]”（最多[[side_effects_power_1]]）/紫罗兰的我方全员<help=will>精神</help>增加[[side_effects_power_2]%]<[[side_effects_turn_2]回合]>（可叠加）/[[Askill_name_20215002]]的<help=cool_time>冷却时间</help>缩短[[side_effects_power_3]]/",

    "威力[[main_effects_power_1]%]で範囲内の味方の<help=constitution>体力</help>回復(雷)/ビリジアンの味方全員に<help=will>精神</help>[[side_effects_power_1]%]増加＜[[side_effects_turn_1]ターン]＞(重複可)・<help=wind_shield>風シールド</help>付与[[side_effects_power_2]]・<help=Protection>加護</help>付与":
        "以威力[[main_effects_power_1]%]恢复范围内我方的<help=constitution>体力</help>（雷）/铬绿的我方全员<help=will>精神</help>增加[[side_effects_power_1]%]<[[side_effects_turn_1]回合]>（可叠加）・赋予<help=wind_shield>风护盾</help>[[side_effects_power_2]]・赋予<help=Protection>加护</help>",

    "対象のビリジアンの味方に<help=offence>攻撃</help>[[side_effects_power_1]%]増加＜[[side_effects_turn_1]ターン]＞(解除不可・重複可)・入魂ゲージ[[side_effects_power_2]]増加・<help=ignite>気炎</help>付与・<help=concentration>集中</help>付与":
        "使目标的铬绿我方<help=offence>攻击</help>增加[[side_effects_power_1]%]<[[side_effects_turn_1]回合]>（不可解除・可叠加）・入魂槽增加[[side_effects_power_2]]・赋予<help=ignite>气焰</help>・赋予<help=concentration>专注</help>",

    "威力[[main_effects_power_1]%]/味方全員に<help=cool_time>クールタイム</help>[[side_effects_power_1]]短縮/ビリジアンの味方全員に<help=will>精神</help>[[side_effects_power_2]%]増加＜[[side_effects_turn_2]ターン]＞(重複可)・<help=regeneration>再生</help>付与/[[Askill_name_20314002]]の<help=cool_time>クールタイム</help>[[side_effects_power_4]]短縮/":
        "威力[[main_effects_power_1]%]/我方全员<help=cool_time>冷却时间</help>缩短[[side_effects_power_1]]/铬绿的我方全员<help=will>精神</help>增加[[side_effects_power_2]%]<[[side_effects_turn_2]回合]>（可叠加）・赋予<help=regeneration>再生</help>/[[Askill_name_20314002]]的<help=cool_time>冷却时间</help>缩短[[side_effects_power_4]]/",

    "範囲内の敵に威力[[main_effects_power_1]%]の<help=magical_dmg>特殊ダメージ</help>（雷<help=element>エレメント</help>）/[[side_rate_1]%]の確率で<help=re_action>再行動</help>（上限[[side_effects_power_1]]回）/自身に<help=move>移動</help>[[side_effects_power_2]]増加＜[[side_effects_turn_2]ターン]＞/命中対象に確率で雷<help=weakpoint>弱点</help>付与":
        "对范围内的敌人造成威力[[main_effects_power_1]%]的<help=magical_dmg>特殊伤害</help>（雷<help=element>元素</help>）/以[[side_rate_1]%]的概率<help=re_action>再行动</help>（上限[[side_effects_power_1]]次）/自身<help=move>移动</help>增加[[side_effects_power_2]]<[[side_effects_turn_2]回合]>/命中目标概率赋予雷<help=weakpoint>弱点</help>",

    "範囲内の敵に威力[[main_effects_power_1]%]の<help=physical_dmg>物理ダメージ</help>（物理<help=element>エレメント</help>）/アイボリーの味方全員に<help=offence>攻撃</help>[[side_effects_power_1]%]増加＜[[side_effects_turn_1]ターン]＞(重複可)/命中対象に確率で物理<help=weakpoint>弱点</help>付与/[[side_rate_3]%]の確率で<help=re_action>再行動</help>（上限[[side_effects_power_3]]回）/敵撃破時に入魂ゲージ100%以上ならば入魂発動":
        "对范围内的敌人造成威力[[main_effects_power_1]%]的<help=physical_dmg>物理伤害</help>（物理<help=element>元素</help>）/象牙白的我方全员<help=offence>攻击</help>增加[[side_effects_power_1]%]<[[side_effects_turn_1]回合]>（可叠加）/命中目标概率赋予物理<help=weakpoint>弱点</help>/以[[side_rate_3]%]的概率<help=re_action>再行动</help>（上限[[side_effects_power_3]]次）/击破敌人时入魂槽若在100%以上则发动入魂",

    "範囲内の敵に威力[[main_effects_power_1]%]の<help=physical_dmg>物理ダメージ</help>（物理<help=element>エレメント</help>）/熱情消費[[use_passion]]/<help=move_position>位置移動（直線）</help>/自身に<help=move>移動</help>[[side_effects_power_3]]増加＜[[side_effects_turn_3]ターン]＞・<help=critical>必殺</help>[[side_effects_power_4]]増加＜[[side_effects_turn_4]ターン]＞（解除不可・重複可）/[[side_rate_5]%]の確率で<help=re_action>再行動</help>（上限[[side_effects_power_5]]回）":
        "对范围内的敌人造成威力[[main_effects_power_1]%]的<help=physical_dmg>物理伤害</help>（物理<help=element>元素</help>）/热情消耗[[use_passion]]/<help=move_position>位置移动（直线）</help>/自身<help=move>移动</help>增加[[side_effects_power_3]]<[[side_effects_turn_3]回合]>・<help=critical>必杀</help>增加[[side_effects_power_4]]<[[side_effects_turn_4]回合]>（不可解除・可叠加）/以[[side_rate_5]%]的概率<help=re_action>再行动</help>（上限[[side_effects_power_5]]次）",

    "範囲内の敵に威力[[main_effects_power_1]%]の<help=magical_dmg>特殊ダメージ</help>（雷<help=element>エレメント</help>）/攻撃対象が少ないほどダメージ上昇（最大[[side_effects_power_1]%]）/ボスエネミーにダメージ[[side_effects_power_2]%]上昇/自身と最も攻撃が高いカーマインの味方1人に<help=move>移動</help>[[side_effects_power_3]]増加＜[[side_effects_turn_3]ターン]＞・<help=true_sight>真眼</help>付与/[[side_rate_7]%]の確率で<help=re_action>再行動</help>（上限[[side_effects_power_7]]回）":
        "对范围内的敌人造成威力[[main_effects_power_1]%]的<help=magical_dmg>特殊伤害</help>（雷<help=element>元素</help>）/攻击目标越少伤害越高（最大[[side_effects_power_1]%]）/对首领敌人伤害提升[[side_effects_power_2]%]/自身与攻击最高的1名洋红我方<help=move>移动</help>增加[[side_effects_power_3]]<[[side_effects_turn_3]回合]>・赋予<help=true_sight>真眼</help>/以[[side_rate_7]%]的概率<help=re_action>再行动</help>（上限[[side_effects_power_7]]次）",

    "範囲内の敵に威力[[main_effects_power_1]%]の<help=magical_dmg>特殊ダメージ</help>（風<help=element>エレメント</help>）/攻撃対象が少ないほどダメージ上昇（最大[[side_effects_power_1]%]）/ボスエネミーにダメージ[[side_effects_power_2]%]上昇/自身と最も攻撃が高いカーマインの味方1人に<help=move>移動</help>[[side_effects_power_3]]増加＜[[side_effects_turn_3]ターン]＞・<help=true_sight>真眼</help>付与/[[side_rate_7]%]の確率で<help=re_action>再行動</help>（上限[[side_effects_power_7]]回）":
        "对范围内的敌人造成威力[[main_effects_power_1]%]的<help=magical_dmg>特殊伤害</help>（风<help=element>元素</help>）/攻击目标越少伤害越高（最大[[side_effects_power_1]%]）/对首领敌人伤害提升[[side_effects_power_2]%]/自身与攻击最高的1名洋红我方<help=move>移动</help>增加[[side_effects_power_3]]<[[side_effects_turn_3]回合]>・赋予<help=true_sight>真眼</help>/以[[side_rate_7]%]的概率<help=re_action>再行动</help>（上限[[side_effects_power_7]]次）",

    "範囲内の敵に威力[[main_effects_power_1]%]の<help=magical_dmg>特殊ダメージ</help>（風<help=element>エレメント</help>）/系統：工芸・芸道にダメージ[[side_effects_power_1]%]上昇/1ターン前に自身が技法か絶技使用していたら威力[[side_effects_power_2]%]上昇/自身に<help=critical>必殺</help>[[side_effects_power_3]]増加＜[[side_effects_turn_3]ターン]＞/敵撃破時に入魂ゲージ100%以上ならば入魂発動":
        "对范围内的敌人造成威力[[main_effects_power_1]%]的<help=magical_dmg>特殊伤害</help>（风<help=element>元素</help>）/对系谱：工艺・艺道伤害提升[[side_effects_power_1]%]/若1回合前自身使用过技法或绝技则威力提升[[side_effects_power_2]%]/自身<help=critical>必杀</help>增加[[side_effects_power_3]]<[[side_effects_turn_3]回合]>/击破敌人时入魂槽若在100%以上则发动入魂",

    "威力[[main_effects_power_1]%]/カーマインの味方全員に<help=offence>攻撃</help>[[side_effects_power_1]%]増加＜[[side_effects_turn_1]ターン]＞(重複可)・<help=ignite>気炎</help>付与・<help=cool_time>クールタイム</help>[[side_effects_power_3]]短縮/":
        "威力[[main_effects_power_1]%]/洋红的我方全员<help=offence>攻击</help>增加[[side_effects_power_1]%]<[[side_effects_turn_1]回合]>（可叠加）・赋予<help=ignite>气焰</help>・<help=cool_time>冷却时间</help>缩短[[side_effects_power_3]]/",

    "バトル開始時、この技法の<help=cool_time>クールタイム</help>最大化/範囲内の敵に威力[[main_effects_power_1]%]の<help=physical_dmg>物理ダメージ</help>（物理<help=element>エレメント</help>）/熱情消費[[use_passion]]/攻撃対象が少ないほどダメージ上昇（最大[[side_effects_power_1]%]）/バイオレットの自身に<help=ignite>気炎</help>付与・<help=true_sight>真眼</help>付与/[[side_rate_3]%]の確率で<help=re_action>再行動</help>（上限[[side_effects_power_3]]回）":
        "战斗开始时本技法的<help=cool_time>冷却时间</help>最大化/对范围内的敌人造成威力[[main_effects_power_1]%]的<help=physical_dmg>物理伤害</help>（物理<help=element>元素</help>）/热情消耗[[use_passion]]/攻击目标越少伤害越高（最大[[side_effects_power_1]%]）/对紫罗兰的自身赋予<help=ignite>气焰</help>・赋予<help=true_sight>真眼</help>/以[[side_rate_3]%]的概率<help=re_action>再行动</help>（上限[[side_effects_power_3]]次）",

    "範囲内の敵に威力[[main_effects_power_1]%]の<help=physical_dmg>物理ダメージ</help>（物理<help=element>エレメント</help>）/熱情消費[[use_passion]]/ボスエネミーにダメージ[[side_effects_power_2]%]上昇/自身の残り体力割合が低いほどダメージ上昇（最大[[side_effects_power_3]%]）/自身に<help=move>移動</help>[[side_effects_power_4]]増加＜[[side_effects_turn_4]ターン]＞/[[side_rate_5]%]の確率で<help=re_action>再行動</help>（上限[[side_effects_power_5]]回）":
        "对范围内的敌人造成威力[[main_effects_power_1]%]的<help=physical_dmg>物理伤害</help>（物理<help=element>元素</help>）/热情消耗[[use_passion]]/对首领敌人伤害提升[[side_effects_power_2]%]/自身剩余体力比例越低伤害越高（最大[[side_effects_power_3]%]）/自身<help=move>移动</help>增加[[side_effects_power_4]]<[[side_effects_turn_4]回合]>/以[[side_rate_5]%]的概率<help=re_action>再行动</help>（上限[[side_effects_power_5]]次）",

    "威力[[main_effects_power_1]%]/敵全体の<help=offence>攻撃</help>[[side_effects_power_1]%]減少＜[[side_effects_turn_1]ターン]＞(重複可)/バイオレットの味方全員に<help=critical>必殺</help>[[side_effects_power_2]]増加＜[[side_effects_turn_2]ターン]＞(重複可)/敵全体の<help=speed>速力</help>[[side_effects_power_3]%]減少＜[[side_effects_turn_3]ターン]＞/[[Askill_name_21015002]]の<help=cool_time>クールタイム</help>[[side_effects_power_4]]短縮/":
        "威力[[main_effects_power_1]%]/敌方全体<help=offence>攻击</help>减少[[side_effects_power_1]%]<[[side_effects_turn_1]回合]>（可叠加）/紫罗兰的我方全员<help=critical>必杀</help>增加[[side_effects_power_2]]<[[side_effects_turn_2]回合]>（可叠加）/敌方全体<help=speed>速力</help>减少[[side_effects_power_3]%]<[[side_effects_turn_3]回合]>/[[Askill_name_21015002]]的<help=cool_time>冷却时间</help>缩短[[side_effects_power_4]]/",

    "範囲内の敵に威力[[main_effects_power_1]%]の<help=magical_dmg>特殊ダメージ</help>（雷<help=element>エレメント</help>）/<help=move_position_range>位置移動（範囲）</help>/熱情消費[[use_passion]]/系統：工芸にダメージ[[side_effects_power_2]%]上昇/ボスエネミーにダメージ[[side_effects_power_3]%]上昇/敵撃破時に[[side_rate_4]%]の確率で<help=re_action>再行動</help>（毎ターン1回まで）/敵撃破時に入魂ゲージ100%以上ならば入魂発動/<help=soul get>ソウル回収</help>":
        "对范围内的敌人造成威力[[main_effects_power_1]%]的<help=magical_dmg>特殊伤害</help>（雷<help=element>元素</help>）/<help=move_position_range>位置移动（范围）</help>/热情消耗[[use_passion]]/对系谱：工艺伤害提升[[side_effects_power_2]%]/对首领敌人伤害提升[[side_effects_power_3]%]/击破敌人时以[[side_rate_4]%]的概率<help=re_action>再行动</help>（每回合最多1次）/击破敌人时入魂槽若在100%以上则发动入魂/<help=soul get>灵魂回收</help>",

    "範囲内の敵に威力[[main_effects_power_1]%]の<help=magical_dmg>特殊ダメージ</help>（雷<help=element>エレメント</help>）/熱情消費[[use_passion]]/命中対象の<help=thunder_shield>雷シールド</help>解除/[[side_rate_2]%]の確率で<help=re_action>再行動</help>（上限[[side_effects_power_2]]回）/「[[ml_name_3]]」召喚（最大[[side_effects_power_3]]）":
        "对范围内的敌人造成威力[[main_effects_power_1]%]的<help=magical_dmg>特殊伤害</help>（雷<help=element>元素</help>）/热情消耗[[use_passion]]/解除命中目标的<help=thunder_shield>雷护盾</help>/以[[side_rate_2]%]的概率<help=re_action>再行动</help>（上限[[side_effects_power_2]]次）/召唤“[[ml_name_3]]”（最多[[side_effects_power_3]]）",

    "威力[[main_effects_power_1]%]/自身の<help=passion>熱情</help>[[side_effects_power_1]]増加/ビリジアンの味方全員の<help=offence>攻撃</help>[[side_effects_power_2]%]増加＜[[side_effects_turn_2]ターン]＞(重複可)/自身の<help=cool_time>クールタイム</help>[[side_effects_power_3]]短縮/":
        "威力[[main_effects_power_1]%]/自身<help=passion>热情</help>增加[[side_effects_power_1]]/铬绿的我方全员<help=offence>攻击</help>增加[[side_effects_power_2]%]<[[side_effects_turn_2]回合]>（可叠加）/自身<help=cool_time>冷却时间</help>缩短[[side_effects_power_3]]/",

    "範囲内の敵に威力[[main_effects_power_1]%]の<help=physical_dmg>物理ダメージ</help>（物理<help=element>エレメント</help>）/自身に<help=passion>熱情</help>[[side_effects_power_1]]増加/命中対象に確率で<help=stun>気絶</help>付与＜[[side_effects_turn_2]ターン]＞/命中対象に<help=thunder_shield>雷シールド</help>解除":
        "对范围内的敌人造成威力[[main_effects_power_1]%]的<help=physical_dmg>物理伤害</help>（物理<help=element>元素</help>）/自身<help=passion>热情</help>增加[[side_effects_power_1]]/命中目标概率赋予<help=stun>眩晕</help><[[side_effects_turn_2]回合]>/解除命中目标的<help=thunder_shield>雷护盾</help>",

    "範囲内の敵に威力[[main_effects_power_1]%]の<help=magical_dmg>特殊ダメージ</help>（雷<help=element>エレメント</help>）/<help=stun>気絶</help>状態ならダメージ[[side_effects_power_1]%]上昇/自身が雷弱点を突いた回数に応じてダメージ[[side_effects_power_2]%]上昇（上限[[effect_attributes_2_1]]回）/バイオレットの味方全員に<help=agility>敏捷</help>[[side_effects_power_3]%]増加＜[[side_effects_turn_3]ターン]＞(重複可)・<help=critical>必殺</help>[[side_effects_power_4]]増加＜[[side_effects_turn_4]ターン]＞(重複可)":
        "对范围内的敌人造成威力[[main_effects_power_1]%]的<help=magical_dmg>特殊伤害</help>（雷<help=element>元素</help>）/目标处于<help=stun>眩晕</help>状态时伤害提升[[side_effects_power_1]%]/根据自身攻击雷弱点的次数伤害提升[[side_effects_power_2]%](上限[[effect_attributes_2_1]]次)/紫罗兰的我方全员<help=agility>敏捷</help>增加[[side_effects_power_3]%]<[[side_effects_turn_3]回合]>（可叠加）・<help=critical>必杀</help>增加[[side_effects_power_4]]<[[side_effects_turn_4]回合]>（可叠加）",

    "範囲内の敵に威力[[main_effects_power_1]%]の<help=magical_dmg>特殊ダメージ</help>（雷<help=element>エレメント</help>）/攻撃対象が少ないほどダメージ上昇（最大[[side_effects_power_1]%]）自身の技法・絶技発動回数に応じてダメージ[[side_effects_power_2]%]上昇（上限[[effect_attributes_2_1]]回）/敵撃破時に[[side_rate_3]%]の確率で<help=re_action>再行動</help>（毎ターン1回まで）/敵撃破時に入魂ゲージ100%以上ならば入魂発動":
        "对范围内的敌人造成威力[[main_effects_power_1]%]的<help=magical_dmg>特殊伤害</help>（雷<help=element>元素</help>）/攻击目标越少伤害越高（最大[[side_effects_power_1]%]）/根据自身技法・绝技发动次数伤害提升[[side_effects_power_2]%](上限[[effect_attributes_2_1]]次)/击破敌人时以[[side_rate_3]%]的概率<help=re_action>再行动</help>（每回合最多1次）/击破敌人时入魂槽若在100%以上则发动入魂",

    "バイオレットの味方全員に<help=offence>攻撃</help>[[side_effects_power_1]%]増加＜[[side_effects_turn_1]ターン]＞(重複可)・<help=move>移動</help>[[side_effects_power_2]]増加＜[[side_effects_turn_2]ターン]＞(重複可)・<help=passion>熱情</help>[[side_effects_power_3]]増加/[[side_rate_4]%]の確率で<help=re_action>再行動</help>（上限[[side_effects_power_4]]回）":
        "紫罗兰的我方全员<help=offence>攻击</help>增加[[side_effects_power_1]%]<[[side_effects_turn_1]回合]>（可叠加）・<help=move>移动</help>增加[[side_effects_power_2]]<[[side_effects_turn_2]回合]>（可叠加）・<help=passion>热情</help>增加[[side_effects_power_3]]/以[[side_rate_4]%]的概率<help=re_action>再行动</help>（上限[[side_effects_power_4]]次）",

    "威力[[main_effects_power_1]%]で範囲内の味方の<help=constitution>体力</help>回復(雷)/バイオレットの味方全員に入魂ゲージ[[side_effects_power_2]]増加/離脱した味方を<help=constitution>体力</help>[[side_effects_power_1]%]で<help=resurrection>蘇生</help>（最大4人）":
        "以威力[[main_effects_power_1]%]恢复范围内我方的<help=constitution>体力</help>（雷）/紫罗兰的我方全员入魂槽增加[[side_effects_power_2]]/使退场的我方以<help=constitution>体力</help>[[side_effects_power_1]%]进行<help=resurrection>复活</help>（最多4人）",

    "威力[[main_effects_power_1]%]/攻撃対象が少ないほどダメージ上昇（最大[[side_effects_power_1]%]）/バイオレットの味方全員に<help=offence>攻撃</help>[[side_effects_power_2]%]増加＜[[side_effects_turn_2]ターン]＞(解除不可・重複可)・<help=cool_time>クールタイム</help>[[side_effects_power_3]]短縮/":
        "威力[[main_effects_power_1]%]/攻击目标越少伤害越高（最大[[side_effects_power_1]%]）/紫罗兰的我方全员<help=offence>攻击</help>增加[[side_effects_power_2]%]<[[side_effects_turn_2]回合]>（不可解除・可叠加）・<help=cool_time>冷却时间</help>缩短[[side_effects_power_3]]/",

    "命中対象の<help=will>精神</help>[[side_effects_power_1]%]減少＜[[side_effects_turn_1]ターン]＞(解除不可・重複可)/命中対象に確率で雷の<help=stamp>刻印</help>・<help=stagnation>停滞</help>付与＜[[side_effects_turn_2]ターン]＞":
        "命中目标的<help=will>精神</help>减少[[side_effects_power_1]%]<[[side_effects_turn_1]回合]>（不可解除・可叠加）/命中目标概率赋予雷之<help=stamp>刻印</help>・<help=stagnation>停滞</help><[[side_effects_turn_2]回合]>"
}

# 3. Passive Skill Names (67)
PASSIVE_NAMES = {
    "バトル開始時、自身に物理シールド付与/一定体力以下で自身に攻撃増加（重複可）・精神増加（重複可）・雷シールド付与": "战斗开始时自身赋予物理护盾/一定体力以下时自身攻击增加（可叠加）・精神增加（可叠加）・赋予雷护盾",
    "体力強化：Ⅰ": "体力强化：Ⅰ",
    "体力強化：Ⅱ": "体力强化：Ⅱ",
    "体力強化：Ⅲ": "体力强化：Ⅲ",
    "攻撃強化：Ⅰ": "攻击强化：Ⅰ",
    "攻撃強化：Ⅱ": "攻击强化：Ⅱ",
    "攻撃強化：Ⅲ": "攻击强化：Ⅲ",
    "器用強化：Ⅰ": "灵巧强化：Ⅰ",
    "器用強化：Ⅱ": "灵巧强化：Ⅱ",
    "器用強化：Ⅲ": "灵巧强化：Ⅲ",
    "敏捷強化：Ⅰ": "敏捷强化：Ⅰ",
    "敏捷強化：Ⅱ": "敏捷强化：Ⅱ",
    "敏捷強化：Ⅲ": "敏捷强化：Ⅲ",
    "状態異常抵抗増加(7種)：Ⅰ": "异常状态抗性增加(7种)：Ⅰ",
    "状態異常抵抗増加(7種)：Ⅱ": "异常状态抗性增加(7种)：Ⅱ",
    "セルリアン以外ダメージ軽減": "蔚蓝以外伤害减轻",
    "節、正、虚相以外ダメージ軽減": "节、正、虚相以外伤害减轻",
    "雷以外ダメージ軽減：Ⅰ": "雷以外伤害减轻：Ⅰ",
    "雷以外ダメージ軽減：Ⅱ": "雷以外伤害减轻：Ⅱ",
    "被ダメージ軽減：Ⅰ": "承受伤害减轻：Ⅰ",
    "被ダメージ軽減：Ⅱ": "承受伤害减轻：Ⅱ",
    "行動回数増加：Ⅰ": "行动次数增加：Ⅰ",
    "行動回数増加：Ⅱ": "行动次数增加：Ⅱ",
    "毎ターン拒絶付与": "每回合赋予拒绝",
    "毎ターン雷シールド付与": "每回合赋予雷护盾",
    "器用弱化": "灵巧弱化",
    "敏捷弱化": "敏捷弱化",
    "絶技が雷以外パラメータ弱化": "绝技为雷以外时属性弱化",
    "移動弱化：Ⅰ": "移动弱化：Ⅰ",
    "移動弱化：Ⅱ": "移动弱化：Ⅱ",
    "入魂ゲージ獲得量弱化": "入魂槽获得量弱化",
    "回復量弱化": "恢复量弱化",
    "バフ効果弱化": "强化效果弱化",
    "通常攻撃ダメージ弱化": "普通攻击伤害弱化",
    "技法ダメージ弱化": "技法伤害弱化",
    "絶技ダメージ弱化": "绝技伤害弱化",
    "インスピレーションダメージ弱化": "灵感伤害弱化",
    "3人戦闘不能で敗北": "3人无法战斗时失败",
    "5ターン経過で敗北": "经过5回合时失败",
    "雷属性強化/防御増加/技法発動時、攻撃増加・CT短縮": "雷属性强化/防御增加/技法发动时攻击增加・CT缩短",
    "工芸特効/幸運増加/バフ付与時、自身に迅雷・味方器用増加": "工艺特效/幸运增加/赋予强化时自身迅雷・我方灵巧增加",
    "与ダメージ上昇/幸運増加/回復技法発動時、味方精神増加・体力回復": "造成伤害提升/幸运增加/发动恢复技法时我方精神增加・体力恢复",
    "初期熱情増加/物理属性強化/熱情消費時、移動・熱情・入魂ゲージ増加": "初始热情增加/物理属性强化/消耗热情时移动・热情・入魂槽增加",
    "工芸特効/体力増加/召喚技法発動時、カーマイン味方の攻撃増加": "工艺特效/体力增加/发动召唤技法时洋红我方的攻击增加",
    "初期熱情増加/雷属性強化/熱情消費時、器用・熱情増加・敵精神減少": "初始热情增加/雷属性强化/消耗热情时灵巧・热情增加・敌方精神减少",
    "バフ付与時、攻撃・敏捷増加": "赋予强化时攻击・敏捷增加",
    "バフ付与時、攻撃・防御増加": "赋予强化时攻击・防御增加",
    "召喚技法発動時、精神・器用増加": "发动召唤技法时精神・灵巧增加",
    "火傷無効/技法発動時、攻撃増加/初期CT0": "灼伤无效/技法发动时攻击增加/初始CT0",
    "位置移動攻撃発動時、攻撃増加・気炎付与": "发动位置移动攻击时攻击增加・赋予气焰",
    "技法発動時、入魂ゲージ増加": "技法发动时入魂槽增加",
    "命中強化": "命中强化",
    "必殺強化": "必杀强化",
    "受流し強化": "招架强化",
    "回避強化": "回避强化",
    "攻撃強化": "攻击强化",
    "通常攻撃強化": "普通攻击强化",
    "技法攻撃強化": "技法攻击强化",
    "絶技攻撃強化": "绝技攻击强化",
    "クリティカル強化": "暴击强化",
    "被ダメージ軽減": "承受伤害减轻",
    "入魂ゲージ増加量補正": "入魂槽增加量修正",
    "ターン開始時、熱情増加": "回合开始时热情增加",
    "有利属性特効": "有利属性特效",
    "ダメージ上限引き上げ": "伤害上限提高"
}

# 4. Passive Skill Descriptions (53)
PASSIVE_DESCS = {
    "バトル開始時、自身に物理シールド付与/一定体力以下で自身に攻撃[[effects_power_2]%]増加（重複可）・精神[[effects_power_3]%]増加（重複可）・雷シールド付与":
        "战斗开始时赋予自身物理护盾/一定体力以下时自身攻击增加[[effects_power_2]%](可叠加)・精神增加[[effects_power_3]%](可叠加)・赋予雷护盾",

    "エネミーの体力[[effects_power]%]増加": "敌人的体力增加[[effects_power]%]",
    "エネミーの攻撃[[effects_power]%]増加": "敌人的攻击增加[[effects_power]%]",
    "エネミーの器用[[effects_power]%]増加": "敌人的灵巧增加[[effects_power]%]",
    "エネミーの敏捷[[effects_power]%]増加": "敌人的敏捷增加[[effects_power]%]",
    "エネミーの状態異常抵抗[[effects_power]]増加": "敌人的异常状态抗性增加[[effects_power]]",
    "エネミーはセルリアン以外からの被ダメージを[[effects_power]%]軽減": "敌人受到的来自蔚蓝以外的伤害减轻[[effects_power]%]",
    "エネミーは節、正、虚相以外からの被ダメージを[[effects_power]%]軽減": "敌人受到的来自节、正、虚相以外的伤害减轻[[effects_power]%]",
    "エネミーは雷エレメント以外からの被ダメージを[[effects_power]%]軽減": "敌人受到的来自雷元素以外的伤害减轻[[effects_power]%]",
    "エネミーは被ダメージを軽減": "敌人受到的伤害减轻",
    "エネミーは被ダメージを大きく軽減": "敌人受到的伤害大幅减轻",
    "ボスエネミーの行動回数[[effects_power]]増加": "首领敌人的行动次数增加[[effects_power]]",
    "エネミーに毎ターン拒絶を付与": "每回合赋予敌人拒绝",
    "エネミーに毎ターン雷シールド[[effects_power]](40%軽減)を付与": "每回合赋予敌人雷护盾[[effects_power]](减轻40%)",
    "味方の器用[[effects_power]%]減少": "我方的灵巧减少[[effects_power]%]",
    "味方の敏捷[[effects_power]%]減少": "我方的敏捷减少[[effects_power]%]",
    "絶技が雷以外のイマージュ装備時、味方のパラメータ減少": "装备绝技非雷属性的意象时，我方属性减少",
    "味方の移動[[effects_power]]減少": "我方的移动减少[[effects_power]]",
    "味方の入魂ゲージ獲得量[[effects_power]%]減少": "我方的入魂槽获得量减少[[effects_power]%]",
    "味方の回復量[[effects_power]%]減少": "我方的恢复量减少[[effects_power]%]",
    "バトル中に発生した味方のバフ効果[[effects_power]%]減少": "战斗中产生的我方强化效果减少[[effects_power]%]",
    "味方の通常攻撃のダメージ[[effects_power]%]減少": "我方的普通攻击伤害减少[[effects_power]%]",
    "味方の技法のダメージ[[effects_power]%]減少": "我方的技法伤害减少[[effects_power]%]",
    "味方の絶技のダメージ[[effects_power]%]減少": "我方的绝技伤害减少[[effects_power]%]",
    "味方のインスピレーションのダメージ[[effects_power]%]減少": "我方的灵感伤害减少[[effects_power]%]",
    "味方が3人戦闘不能時敗北": "我方有3人无法战斗时失败",
    "5ターン経過時敗北": "经过5回合时失败",

    "雷エレメントの攻撃を[[effects_power]%]強化/防御[[effects_power_2]%]増加/技法発動時、自身の攻撃[[effects_power_3]%]増加＜[[effects_turn_3]ターン]＞（重複可）・確率で自身の<help=cool_time>クールタイム</help>[[effects_power_4]]短縮":
        "雷元素攻击强化[[effects_power]%]/防御增加[[effects_power_2]%]/发动技法时自身攻击增加[[effects_power_3]%]<[[effects_turn_3]回合]>(可叠加)・概率缩短自身<help=cool_time>冷却时间</help>[[effects_power_4]]",

    "系統：工芸に対してダメージ[[effects_power]%]上昇/幸運[[effects_power_2]]増加/技法または絶技でバフ付与時、自身に迅雷付与・味方全員の器用[[effects_power_4]%]増加＜[[effects_turn_4]ターン]＞":
        "对系谱：工艺伤害提升[[effects_power]%]/幸运增加[[effects_power_2]]/通过技法或绝技赋予强化时自身赋予迅雷・我方全员灵巧增加[[effects_power_4]%]<[[effects_turn_4]回合]>",

    "与えるダメージ[[effects_power]%]上昇/幸運[[effects_power_2]]増加/技法の威力で体力回復時、味方全員の精神[[effects_power_3]%]増加＜[[effects_turn_3]ターン]＞・体力[[effects_power_4]]回復":
        "造成伤害提升[[effects_power]%]/幸运增加[[effects_power_2]]/通过技法威力恢复体力时我方全员精神增加[[effects_power_3]%]<[[effects_turn_3]回合]>・恢复体力[[effects_power_4]]",

    "バトル開始時の熱情[[effects_power]]増加/物理エレメントの攻撃を[[effects_power_2]%]強化/技法または絶技で熱情消費時、自身の移動[[effects_power_3]]増加＜[[effects_turn_3]ターン]＞（重複可）・熱情[[effects_power_4]]増加・入魂ゲージ[[effects_power_5]]増加":
        "战斗开始时热情增加[[effects_power]]/物理元素攻击强化[[effects_power_2]%]/通过技法或绝技消耗热情时自身移动增加[[effects_power_3]]<[[effects_turn_3]回合]>(可叠加)・热情增加[[effects_power_4]]・入魂槽增加[[effects_power_5]]",

    "系統：工芸に対してダメージ[[effects_power]%]上昇/体力[[effects_power_2]%]増加/技法または絶技でオブジェクト召喚時、カーマインの味方全員の攻撃[[effects_power_3]%]増加（重複可）＜[[effects_turn_3]ターン]＞":
        "对系谱：工艺伤害提升[[effects_power]%]/体力增加[[effects_power_2]%]/通过技法或绝技进行造物召唤时洋红我方全员攻击增加[[effects_power_3]%](可叠加)<[[effects_turn_3]回合]>",

    "バトル開始時の熱情[[effects_power]]増加/雷エレメントの攻撃を[[effects_power_2]%]強化/技法または絶技で熱情消費時、自身の器用[[effects_power_3]%]増加（重複可）＜[[effects_turn_3]ターン]＞、熱情[[effects_power_4]]増加、敵全体の精神[[effects_power_5]%]減少（重複可）＜[[effects_turn_5]ターン]＞":
        "战斗开始时热情增加[[effects_power]]/雷元素攻击强化[[effects_power_2]%]/通过技法或绝技消耗热情时自身灵巧增加[[effects_power_3]%](可叠加)<[[effects_turn_3]回合]>、热情增加[[effects_power_4]]、敌方全体精神减少[[effects_power_5]%](可叠加)<[[effects_turn_5]回合]>",

    "技法または絶技でバフ付与時、自身の攻撃・敏捷[[effects_power_1]%]増加（重複可）＜[[effects_turn_1]ターン]＞":
        "通过技法或绝技赋予强化时自身攻击・敏捷增加[[effects_power_1]%](可叠加)<[[effects_turn_1]回合]>",

    "技法または絶技でバフ付与時、自身の攻撃・防御[[effects_power_1]%]増加（重複可）＜[[effects_turn_1]ターン]＞":
        "通过技法或绝技赋予强化时自身攻击・防御增加[[effects_power_1]%](可叠加)<[[effects_turn_1]回合]>",

    "技法または絶技でオブジェクト召喚時、自身の精神・器用[[effects_power_1]%]増加（重複可）＜[[effects_turn_1]ターン]＞":
        "通过技法或绝技进行造物召唤时自身精神・灵巧增加[[effects_power_1]%](可叠加)<[[effects_turn_1]回合]>",

    "火傷無効/技法発動時に自身の攻撃[[effects_power_2]%]増加（重複可）＜[[effects_turn_2]ターン]＞<hidden=effects_power_3>/バトル開始時、[[Askill_name_21015001]]のクールタイムを0にする</hidden>":
        "灼伤无效/发动技法时自身攻击增加[[effects_power_2]%](可叠加)<[[effects_turn_2]回合]><hidden=effects_power_3>/战斗开始时将[[Askill_name_21015001]]的冷却时间重置为0</hidden>",

    "位置移動攻撃発動時、自身の攻撃[[effects_power_1]%]増加（重複可）＜[[effects_turn_1]ターン]＞、自身に<help=ignite>気炎</help>付与":
        "发动位置移动攻击时自身攻击增加[[effects_power_1]%](可叠加)<[[effects_turn_1]回合]>、自身赋予<help=ignite>气焰</help>",

    "技法発動時、自身の入魂ゲージ[[effects_power_1]]増加":
        "发动技法时自身入魂槽增加[[effects_power_1]]",

    "命中[[effects_power]%]増加": "命中增加[[effects_power]%]",
    "必殺[[effects_power]%]増加": "必杀增加[[effects_power]%]",
    "受流し[[effects_power]%]増加": "招架增加[[effects_power]%]",
    "回避[[effects_power]%]増加": "闪避增加[[effects_power]%]",
    "攻撃を[[effects_power]%]強化": "攻击强化[[effects_power]%]",
    "通常攻撃を[[effects_power]%]強化": "普通攻击强化[[effects_power]%]",
    "技法攻撃を[[effects_power]%]強化": "技法攻击强化[[effects_power]%]",
    "絶技攻撃を[[effects_power]%]強化": "绝技攻击强化[[effects_power]%]",
    "クリティカル攻撃を[[effects_power]%]強化": "暴击攻击强化[[effects_power]%]",
    "受けるダメージを[[effects_power]%]軽減": "受到的伤害减轻[[effects_power]%]",
    "入魂ゲージ増加時に補正（増加量×[[effects_power]%]分）": "入魂槽增加时修正（增加量×[[effects_power]%]）",
    "ターン開始時、熱情[[effects_power]]増加": "回合开始时热情增加[[effects_power]]",
    "スタイルが有利属性時、対象属性に対してのダメージ[[effects_power]%]上昇": "风格处于优势属性时，对目标属性的伤害提升[[effects_power]%]",
    "与えられるダメージの上限を[[effects_power]]引き上げ": "造成的伤害上限提高[[effects_power]]"
}

def translate_desc_auto(d):
    # Rule-based fallback for any slight template variants
    t = d
    t = t.replace("範囲内の敵に", "对范围内的敌人造成")
    t = t.replace("敵に", "对敌人造成")
    t = t.replace("のダメージ", "伤害")
    t = t.replace("ダメージ上昇", "伤害提升")
    t = t.replace("ダメージ軽減", "伤害减轻")
    t = t.replace("（最大", "（最大")
    t = t.replace("味方全員に", "我方全员")
    t = t.replace("味方全員の", "我方全员的")
    t = t.replace("自身に", "自身")
    t = t.replace("自身の", "自身的")
    t = t.replace("増加", "增加")
    t = t.replace("減少", "减少")
    t = t.replace("付与", "赋予")
    t = t.replace("短縮", "缩短")
    t = t.replace("解除", "解除")
    t = t.replace("回復", "恢复")
    t = t.replace("重複可", "可叠加")
    t = t.replace("解除不可", "不可解除")
    t = t.replace("ターン", "回合")
    t = t.replace("マス", "格")
    t = t.replace("確率で", "概率")
    t = t.replace("の確率で", "的概率")
    t = t.replace("上限", "上限")
    t = t.replace("回）", "次）")
    t = t.replace("人）", "人）")
    t = t.replace("エレメント", "元素")
    t = t.replace("物理ダメージ", "物理伤害")
    t = t.replace("特殊ダメージ", "特殊伤害")
    t = t.replace("炎", "火")
    t = t.replace("風", "风")
    t = t.replace("雷", "雷")
    t = t.replace("水", "水")
    t = t.replace("熱情消費", "热情消耗")
    t = t.replace("熱情", "热情")
    t = t.replace("入魂ゲージ", "入魂槽")
    t = t.replace("バイオレットの", "紫罗兰的")
    t = t.replace("ビリジアンの", "铬绿的")
    t = t.replace("カーマインの", "洋红的")
    t = t.replace("セルリアンの", "蔚蓝的")
    t = t.replace("アイボリーの", "象牙白的")
    return t

print("Reading master.json...")
with open(MASTER_JSON_PATH, "r", encoding="utf-8") as f:
    master = json.load(f)

# Update mActiveSkills
act = master.setdefault("mActiveSkills", {})
act_names = act.setdefault("ml_name1[]", {})
act_descs = act.setdefault("ml_description[]", {})

act_name_added = 0
for k, v in ACTIVE_NAMES.items():
    if k not in act_names or not act_names[k]:
        act_names[k] = v
        act_name_added += 1

act_desc_added = 0
for k, v in ACTIVE_DESCS.items():
    if k not in act_descs or not act_descs[k]:
        act_descs[k] = v
        act_desc_added += 1

# Also check missing_active_skills.json to make sure none were missed
with open(r"c:\修复\gc-tool\missing_active_skills.json", "r", encoding="utf-8") as f:
    missing_act = json.load(f)

for d in missing_act["missing_descriptions"]:
    if d not in act_descs or not act_descs[d]:
        act_descs[d] = ACTIVE_DESCS.get(d, translate_desc_auto(d))
        act_desc_added += 1

# Update mPassiveSkills
pas = master.setdefault("mPassiveSkills", {})
pas_names = pas.setdefault("ml_name[]", {})
pas_descs = pas.setdefault("ml_description[]", {})

pas_name_added = 0
for k, v in PASSIVE_NAMES.items():
    if k not in pas_names or not pas_names[k]:
        pas_names[k] = v
        pas_name_added += 1

with open(r"c:\修复\gc-tool\missing_passive_skills.json", "r", encoding="utf-8") as f:
    missing_pas = json.load(f)

for n in missing_pas["missing_names"]:
    if n not in pas_names or not pas_names[n]:
        pas_names[n] = PASSIVE_NAMES.get(n, translate_desc_auto(n))
        pas_name_added += 1

pas_desc_added = 0
for k, v in PASSIVE_DESCS.items():
    if k not in pas_descs or not pas_descs[k]:
        pas_descs[k] = v
        pas_desc_added += 1

for d in missing_pas["missing_descriptions"]:
    if d not in pas_descs or not pas_descs[d]:
        pas_descs[d] = PASSIVE_DESCS.get(d, translate_desc_auto(d))
        pas_desc_added += 1

print(f"Added to mActiveSkills: {act_name_added} names, {act_desc_added} descriptions")
print(f"Added to mPassiveSkills: {pas_name_added} names, {pas_desc_added} descriptions")

# Save updated master.json
with open(MASTER_JSON_PATH, "w", encoding="utf-8") as f:
    json.dump(master, f, ensure_ascii=False, indent=2)
print("Saved updated master.json!")
