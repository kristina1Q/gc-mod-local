using Gc;
using Gc.Battle.SkillWidget;
using HarmonyLib;

namespace GCMod.Patches;

[HarmonyPatch]
public static class EnhancePatch
{
	[HarmonyPostfix]
	[HarmonyPatch(typeof(GcOptionData), "SetPowerSaving")]
	public static void ChangeFrameRate()
	{
		int value = Config.FrameRate.Value;
		if (value > 0)
		{
			GcOptionData.ChangeApplicationTargetFrameRate(value);
		}
	}

	[HarmonyPostfix]
	[HarmonyPatch(typeof(CutInMoviePlayer), "PlayAsync")]
	public static void SkipCutin(CutInMoviePlayer __instance)
	{
		if (Config.IsSkipCutin.Value)
		{
			__instance.Skip();
		}
	}
}
