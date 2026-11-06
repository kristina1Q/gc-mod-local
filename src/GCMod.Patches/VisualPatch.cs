using System;
using DMM.OLG.Unity.Extensions.Novel;
using HarmonyLib;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace GCMod.Patches;

[HarmonyPatch]
public static class VisualPatch
{
	private static class Defaults
	{
		public static readonly Color NameColor = new Color(0.957f, 0.957f, 0.914f);

		public const float FaceDilate = 0f;

		public const float OutlineWidth = 0f;

		public const float OutlineSoftness = 0f;
	}

	public static Image NormalFrame;

	public static Image CgModeFrame;

	public static Image BaseNameFrame;

	private static void ApplyImageAlpha(Image image, float alpha)
	{
		if (image == null || (Object)(object)image == (Object)null)
		{
			return;
		}
		Color color = ((Graphic)image).color;
		color.a = alpha;
		((Graphic)image).color = color;
	}

	public static void ApplyTextStyle(TextMeshProUGUI text, Color color)
	{
		if (text == null || (Object)(object)text == (Object)null)
		{
			return;
		}
		((Graphic)text).color = color;
		var mat = ((TMP_Text)text).fontMaterial;
		if (mat == null)
		{
			return;
		}
		mat.EnableKeyword("OUTLINE_ON");
		mat.SetFloat("_FaceDilate", Config.FaceDilate.Value);
		mat.SetColor("_OutlineColor", Config.OutlineColor);
		mat.SetFloat("_OutlineWidth", Config.OutlineWidth.Value);
		mat.SetFloat("_OutlineSoftness", Config.OutlineSoftness.Value);
	}

	public static void ResetTextStyle(TextMeshProUGUI text, Color color)
	{
		if (text == null || (Object)(object)text == (Object)null)
		{
			return;
		}
		((Graphic)text).color = color;
		var mat = ((TMP_Text)text).fontMaterial;
		if (mat == null)
		{
			return;
		}
		mat.SetFloat("_FaceDilate", 0f);
		mat.SetColor("_OutlineColor", Color.black);
		mat.SetFloat("_OutlineWidth", 0f);
		mat.SetFloat("_OutlineSoftness", 0f);
		mat.DisableKeyword("OUTLINE_ON");
	}

	[HarmonyPostfix]
	[HarmonyPatch(typeof(EventMessage), "SetName")]
	public static void SetMessageNameFont(EventMessage __instance)
	{
		try
		{
			if (__instance == null || (Object)(object)__instance == (Object)null)
			{
				return;
			}
			var msgName = __instance.MessageName;
			if (msgName == null || (Object)(object)msgName == (Object)null)
			{
				return;
			}
			if (Config.ModifyText.Value)
			{
				ApplyTextStyle(msgName, Config.NameTextColor);
			}
			else
			{
				ResetTextStyle(msgName, Defaults.NameColor);
			}
		}
		catch (Exception ex)
		{
			Plugin.Log.LogWarning($"[VisualPatch] SetMessageNameFont failed: {ex.Message}");
		}
	}

	[HarmonyPostfix]
	[HarmonyPatch(typeof(EventText), "SetRuby")]
	public static void SetMessageTextFont(GameObject go, EventText.Letter letter, ref TextMeshProUGUI text)
	{
		try
		{
			if (text == null || (Object)(object)text == (Object)null)
			{
				return;
			}
			if (Config.ModifyText.Value)
			{
				ApplyTextStyle(text, Config.MessageTextColor);
			}
		}
		catch
		{
		}
	}

	[HarmonyPostfix]
	[HarmonyPatch(typeof(EventMessage), "Init")]
	public static void SaveTextBackground(EventMessage __instance)
	{
		try
		{
			if (__instance == null || (Object)(object)__instance == (Object)null)
			{
				return;
			}

			var comp = (Component)__instance;
			if (comp != null && (Object)(object)comp != (Object)null)
			{
				var childImages = comp.GetComponentsInChildren<Image>(true);
				if (childImages != null)
				{
					foreach (Image componentsInChild in childImages)
					{
						if (componentsInChild == null || (Object)(object)componentsInChild == (Object)null)
						{
							continue;
						}
						string name = ((Object)componentsInChild).name;
						if (name == "Normal")
						{
							ApplyImageAlpha(componentsInChild, Config.NormalAlpha.Value);
							NormalFrame = componentsInChild;
						}
						else if (name == "MessageWindow")
						{
							ApplyImageAlpha(componentsInChild, Config.CgModeAlpha.Value);
							CgModeFrame = componentsInChild;
						}
					}
				}
			}

			GameObject nameImg = null;
			try
			{
				nameImg = __instance.NameImage;
			}
			catch
			{
			}

			if (nameImg != null && (Object)(object)nameImg != (Object)null)
			{
				var nameChildImages = nameImg.GetComponentsInChildren<Image>(true);
				if (nameChildImages != null)
				{
					foreach (Image componentsInChild2 in nameChildImages)
					{
						if (componentsInChild2 == null || (Object)(object)componentsInChild2 == (Object)null)
						{
							continue;
						}
						if (((Object)componentsInChild2).name == "BaseName")
						{
							ApplyImageAlpha(componentsInChild2, Config.NormalAlpha.Value);
							BaseNameFrame = componentsInChild2;
						}
					}
				}
			}
		}
		catch (Exception ex)
		{
			Plugin.Log.LogWarning($"[VisualPatch] SaveTextBackground failed: {ex.Message}");
		}
	}
}
