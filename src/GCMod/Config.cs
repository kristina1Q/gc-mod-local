using System;
using BepInEx.Configuration;
using BepInEx.Core.Logging.Interpolation;
using BepInEx.Logging;
using UnityEngine;
using Utility.Notifications;

namespace GCMod;

public static class Config
{
	public static ConfigEntry<int> FrameRate;

	public static ConfigEntry<bool> IsSkipCutin;

	public static ConfigEntry<bool> Translation;

	public static ConfigEntry<string> TranslationCDN;

	public static ConfigEntry<string> TranslationLanguage;

	public static ConfigEntry<bool> AsyncMode;

	public static ConfigEntry<string[]> MasterDataTables;

	public static ConfigEntry<string> FontBundlePath;

	public static ConfigEntry<float> NormalAlpha;

	public static ConfigEntry<float> CgModeAlpha;

	public static ConfigEntry<bool> ModifyText;

	public static ConfigEntry<string> NameTextColorHex;

	public static ConfigEntry<string> MessageTextColorHex;

	public static ConfigEntry<float> FaceDilate;

	public static ConfigEntry<string> OutlineColorHex;

	public static ConfigEntry<float> OutlineWidth;

	public static ConfigEntry<float> OutlineSoftness;

	public static ConfigEntry<float> CharacterSpacing;

	public static ConfigEntry<string> CustomTranslationPath;

	public static ConfigEntry<bool> CheckUpdate;

	public static Color NameTextColor { get; private set; } = Color.white;


	public static Color MessageTextColor { get; private set; } = Color.white;


	public static Color OutlineColor { get; private set; } = new Color(0.235f, 0.235f, 0.235f);


	public static void Initialize()
	{
		BindAllEntries();
		BindColorEntries();
		BindSettingChangedLog();
	}

	private static void BindAllEntries()
	{
		//IL_00de: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e3: Unknown result type (might be due to invalid IL or missing references)
		//IL_0108: Unknown result type (might be due to invalid IL or missing references)
		//IL_0132: Expected O, but got Unknown
		FrameRate = Plugin.ConfigFile.Bind<int>("General", "FrameRate", 60, "游戏帧率（正整数）");
		CheckUpdate = Plugin.ConfigFile.Bind<bool>("General", "CheckUpdate", false, "是否检查更新（官方仓库失效时建议保持关闭）");
		CustomTranslationPath = Plugin.ConfigFile.Bind<string>("Translation", "CustomPath", "", "自定义本地翻译文件夹路径（绝对路径）。留空则默认使用 BepInEx/plugins/GCMod/translations");
		IsSkipCutin = Plugin.ConfigFile.Bind<bool>("Battle", "IsSkipCutin", false, "是否跳过大招动画（包括变身和释放动画）");
		Translation = Plugin.ConfigFile.Bind<bool>("Translation", "Enabled", true, "是否开启游戏内翻译（剧情与主数据）");
		TranslationCDN = Plugin.ConfigFile.Bind<string>("Translation", "CDN", "https://raw.githubusercontent.com/anosu/girlscreation-translation/refs/heads/main", "翻译仓库或本地服务的根地址，自动拼接 /translations/<Language>/");
		TranslationLanguage = Plugin.ConfigFile.Bind<string>("Translation", "Language", "zh-Hans", "翻译语言，取值范围：[zh-Hans]");
		AsyncMode = Plugin.ConfigFile.Bind<bool>("Translation", "AsyncMode", false, "异步请求剧情翻译；关闭时剧情与主数据入口最多等待 10 秒，超时保留原文并继续后台加载。主数据始终使用此等待上限");
		if (!TomlTypeConverter.CanConvert(typeof(string[])))
		{
			TomlTypeConverter.AddConverter(typeof(string[]), new TypeConverter
			{
				ConvertToString = (object value, Type _) => string.Join(", ", (string[])value),
				ConvertToObject = (string value, Type _) => value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
			});
		}
		MasterDataTables = Plugin.ConfigFile.Bind<string[]>("Translation.MasterData", "EnabledTables", new string[1] { "*" }, "启用翻译的主数据表名，逗号分隔。默认 * 启用全部；也可指定 mItems, mUnits 等表名，留空全部关闭。区分大小写，更改后通常需重启游戏。");
		FontBundlePath = Plugin.ConfigFile.Bind<string>("Translation.Font", "AssetBundlePath", "GCMod/fonts/tsukuardgothic-std-medium", "TMP字体AssetBundle的路径，默认相对于插件目录，也可使用绝对路径");
		NormalAlpha = Plugin.ConfigFile.Bind<float>("Message.Window", "NormalAlpha", 0f, "普通剧情中的对话框不透明度，默认完全透明");
		CgModeAlpha = Plugin.ConfigFile.Bind<float>("Message.Window", "CgModeAlpha", 0f, "寝室剧情中的对话框不透明度，默认完全透明");
		ModifyText = Plugin.ConfigFile.Bind<bool>("Message.Text", "Modified", true, "是否更改对话框文本样式（用于对话框透明时提高对比度）");
		NameTextColorHex = Plugin.ConfigFile.Bind<string>("Message.Text", "NameColor", "FFFFFFFF", "对话框人物名文本颜色");
		MessageTextColorHex = Plugin.ConfigFile.Bind<string>("Message.Text", "MessageColor", "FFFFFFFF", "对话框消息文本颜色");
		FaceDilate = Plugin.ConfigFile.Bind<float>("Message.Text", "FaceDilate", 0.3f, "字体粗细，取值范围：[-1, 1]");
		OutlineColorHex = Plugin.ConfigFile.Bind<string>("Message.Text", "OutlineColor", "3A3A3AFF", "文本描边颜色，十六进制格式");
		OutlineWidth = Plugin.ConfigFile.Bind<float>("Message.Text", "OutlineWidth", 0.3f, "文本描边宽度，取值范围：[0, 1]");
		OutlineSoftness = Plugin.ConfigFile.Bind<float>("Message.Text", "OutlineSoftness", 0.01f, "文本描边羽化程度，取值范围：[0, 1]");
		CharacterSpacing = Plugin.ConfigFile.Bind<float>("Message.Text", "CharacterSpacing", 0.1f, "字间距，仅对消息文本设置，不应用于人物名");
	}

	private static void BindSettingChangedLog()
	{
		Plugin.ConfigFile.SettingChanged += delegate(object _, SettingChangedEventArgs e)
		{
			//IL_0033: Unknown result type (might be due to invalid IL or missing references)
			//IL_0039: Expected O, but got Unknown
			ConfigEntryBase changedSetting = e.ChangedSetting;
			object obj = ((changedSetting.BoxedValue is string[] value) ? string.Join(", ", value) : changedSetting.BoxedValue);
			Plugin.Log.LogInfo($"[{changedSetting.Definition.Section}] {changedSetting.Definition.Key} => {obj}");
			Toast.Info("[" + changedSetting.Definition.Section + "]", $"{changedSetting.Definition.Key} => {obj}");
		};
	}

	private static void BindColorEntries()
	{
		(ConfigEntry<string>, Action<Color>)[] array = new(ConfigEntry<string>, Action<Color>)[3]
		{
			(NameTextColorHex, delegate(Color c)
			{
				//IL_0000: Unknown result type (might be due to invalid IL or missing references)
				NameTextColor = c;
			}),
			(MessageTextColorHex, delegate(Color c)
			{
				//IL_0000: Unknown result type (might be due to invalid IL or missing references)
				MessageTextColor = c;
			}),
			(OutlineColorHex, delegate(Color c)
			{
				//IL_0000: Unknown result type (might be due to invalid IL or missing references)
				OutlineColor = c;
			})
		};
		for (int i = 0; i < array.Length; i++)
		{
			var (entry, setter) = array[i];
			ParseAndSetColor(entry, setter);
			entry.SettingChanged += delegate
			{
				ParseAndSetColor(entry, setter);
			};
		}
	}

	private static void ParseAndSetColor(ConfigEntry<string> entry, Action<Color> setter)
	{
		//IL_0015: Unknown result type (might be due to invalid IL or missing references)
		Color obj = default(Color);
		if (ColorUtility.TryParseHtmlString(NormalizeHexColor(entry.Value), out obj))
		{
			setter(obj);
		}
	}

	private static string NormalizeHexColor(string hex)
	{
		if (string.IsNullOrEmpty(hex))
		{
			return "#FFFFFF";
		}
		hex = hex.Trim().TrimStart('#');
		return "#" + hex;
	}
}
