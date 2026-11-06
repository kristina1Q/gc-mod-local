using System;
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
		try
		{
			Plugin.Log.LogInfo($"Prefix: {prefix}, Id: {id}");
			if (int.TryParse(id, out int novelId))
			{
				Plugin.Trans.PrepareNovel(novelId);
			}
			else
			{
				Plugin.Log.LogWarning($"[TranslationPatch] Non-integer novel Id: {id}");
			}
		}
		catch (Exception ex)
		{
			Plugin.Log.LogWarning($"[TranslationPatch] SetupTranslation failed: {ex.Message}");
		}
	}

	[HarmonyPrefix]
	[HarmonyPatch(typeof(EventTitle), "ShowBlurEffect")]
	public static void SetMessageTitle(EventTitle __instance)
	{
		try
		{
			if (!Config.Translation.Value || __instance == null || (UnityEngine.Object)(object)__instance == (UnityEngine.Object)null)
			{
				return;
			}
			var titleMain = __instance._TitleMain;
			if (titleMain == null || (UnityEngine.Object)(object)titleMain == (UnityEngine.Object)null)
			{
				return;
			}
			string curText = ((TMP_Text)titleMain).text;
			if (!string.IsNullOrEmpty(curText) && PatchManager.TryGetCurrentNovel(out var translation) && translation != null && translation.TryGetValue(curText, out var value))
			{
				((TMP_Text)titleMain).text = value;
			}
		}
		catch
		{
		}
	}

	[HarmonyPrefix]
	[HarmonyPatch(typeof(EventMessage), "SetName")]
	public static void SetMessageName(ref string text)
	{
		try
		{
			if (string.IsNullOrEmpty(text) || !Config.Translation.Value)
			{
				return;
			}
			if (PatchManager.TryGetCurrentNovel(out var _) && Plugin.Trans?.Names != null && Plugin.Trans.Names.TryGetValue(text, out var value))
			{
				text = value;
			}
		}
		catch
		{
		}
	}

	[HarmonyPrefix]
	[HarmonyPatch(typeof(EventText), "Parse")]
	public static void SetMessageText(EventText __instance, ref string message)
	{
		try
		{
			if (!string.IsNullOrEmpty(message) && Config.Translation.Value && PatchManager.TryGetCurrentNovel(out var translation) && translation != null && translation.TryGetValue(message, out var value))
			{
				message = value;
			}
			if (Config.ModifyText.Value && __instance != null && (UnityEngine.Object)(object)__instance != (UnityEngine.Object)null)
			{
				__instance.fontSpacing = Config.CharacterSpacing.Value;
			}
		}
		catch
		{
		}
	}
}
