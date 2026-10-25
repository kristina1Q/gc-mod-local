using BepInEx.Core.Logging.Interpolation;
using BepInEx.Logging;
using DMM.OLG.Unity.Extensions.Novel;
using HarmonyLib;
using TMPro;

namespace GCMod.Patches;

[HarmonyPatch]
public static class TranslationPatch
{
	[HarmonyPrefix]
	[HarmonyPatch(typeof(ScriptObjectManager), "Create")]
	public static void SetupTranslation(string prefix, string id)
	{
		//IL_000a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0010: Expected O, but got Unknown
		Plugin.Log.LogInfo($"Prefix: {prefix}, Id: {id}");
		Plugin.Trans.PrepareNovel(int.Parse(id));
	}

	[HarmonyPrefix]
	[HarmonyPatch(typeof(EventTitle), "ShowBlurEffect")]
	public static void SetMessageTitle(EventTitle __instance)
	{
		if (Config.Translation.Value && PatchManager.TryGetCurrentNovel(out var translation) && translation.TryGetValue(((TMP_Text)__instance._TitleMain).text, out var value))
		{
			((TMP_Text)__instance._TitleMain).text = value;
		}
	}

	[HarmonyPrefix]
	[HarmonyPatch(typeof(EventMessage), "SetName")]
	public static void SetMessageName(ref string text)
	{
		if (Config.Translation.Value && PatchManager.TryGetCurrentNovel(out var _) && Plugin.Trans.Names.TryGetValue(text, out var value))
		{
			text = value;
		}
	}

	[HarmonyPrefix]
	[HarmonyPatch(typeof(EventText), "Parse")]
	public static void SetMessageText(EventText __instance, ref string message)
	{
		if (Config.Translation.Value && PatchManager.TryGetCurrentNovel(out var translation) && translation.TryGetValue(message, out var value))
		{
			message = value;
		}
		if (Config.ModifyText.Value)
		{
			__instance.fontSpacing = Config.CharacterSpacing.Value;
		}
	}
}
