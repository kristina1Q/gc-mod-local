using DMM.OLG.Unity.Extensions.Novel;
using HarmonyLib;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

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
		//IL_000b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0010: Unknown result type (might be due to invalid IL or missing references)
		//IL_001a: Unknown result type (might be due to invalid IL or missing references)
		if (!((Object)(object)image == (Object)null))
		{
			Color color = ((Graphic)image).color;
			color.a = alpha;
			((Graphic)image).color = color;
		}
	}

	public static void ApplyTextStyle(TextMeshProUGUI text, Color color)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_003c: Unknown result type (might be due to invalid IL or missing references)
		((Graphic)text).color = color;
		((TMP_Text)text).fontMaterial.EnableKeyword("OUTLINE_ON");
		((TMP_Text)text).fontMaterial.SetFloat("_FaceDilate", Config.FaceDilate.Value);
		((TMP_Text)text).fontMaterial.SetColor("_OutlineColor", Config.OutlineColor);
		((TMP_Text)text).fontMaterial.SetFloat("_OutlineWidth", Config.OutlineWidth.Value);
		((TMP_Text)text).fontMaterial.SetFloat("_OutlineSoftness", Config.OutlineSoftness.Value);
	}

	public static void ResetTextStyle(TextMeshProUGUI text, Color color)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0027: Unknown result type (might be due to invalid IL or missing references)
		((Graphic)text).color = color;
		((TMP_Text)text).fontMaterial.SetFloat("_FaceDilate", 0f);
		((TMP_Text)text).fontMaterial.SetColor("_OutlineColor", Color.black);
		((TMP_Text)text).fontMaterial.SetFloat("_OutlineWidth", 0f);
		((TMP_Text)text).fontMaterial.SetFloat("_OutlineSoftness", 0f);
		((TMP_Text)text).fontMaterial.DisableKeyword("OUTLINE_ON");
	}

	[HarmonyPostfix]
	[HarmonyPatch(typeof(EventMessage), "SetName")]
	public static void SetMessageNameFont(EventMessage __instance)
	{
		//IL_0023: Unknown result type (might be due to invalid IL or missing references)
		//IL_0012: Unknown result type (might be due to invalid IL or missing references)
		if (Config.ModifyText.Value)
		{
			ApplyTextStyle(__instance.MessageName, Config.NameTextColor);
		}
		else
		{
			ResetTextStyle(__instance.MessageName, Defaults.NameColor);
		}
	}

	[HarmonyPostfix]
	[HarmonyPatch(typeof(EventText), "SetRuby")]
	public static void SetMessageTextFont(GameObject go, EventText.Letter letter, ref TextMeshProUGUI text)
	{
		//IL_000e: Unknown result type (might be due to invalid IL or missing references)
		if (Config.ModifyText.Value)
		{
			ApplyTextStyle(text, Config.MessageTextColor);
		}
	}

	[HarmonyPostfix]
	[HarmonyPatch(typeof(EventMessage), "Init")]
	public static void SaveTextBackground(EventMessage __instance)
	{
		foreach (Image componentsInChild in ((Component)__instance).GetComponentsInChildren<Image>())
		{
			if (((Object)componentsInChild).name == "Normal")
			{
				ApplyImageAlpha(componentsInChild, Config.NormalAlpha.Value);
				NormalFrame = componentsInChild;
			}
			if (((Object)componentsInChild).name == "MessageWindow")
			{
				ApplyImageAlpha(componentsInChild, Config.CgModeAlpha.Value);
				CgModeFrame = componentsInChild;
			}
		}
		foreach (Image componentsInChild2 in __instance.NameImage.GetComponentsInChildren<Image>())
		{
			if (((Object)componentsInChild2).name == "BaseName")
			{
				ApplyImageAlpha(componentsInChild2, Config.NormalAlpha.Value);
				BaseNameFrame = componentsInChild2;
			}
		}
	}
}
