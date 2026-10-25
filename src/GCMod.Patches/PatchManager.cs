using System.Collections.Generic;
using HarmonyLib;

namespace GCMod.Patches;

public static class PatchManager
{
	private static readonly List<Harmony> Installed = new List<Harmony>();

	public static void Initialize()
	{
		if (Installed.Count > 0)
		{
			return;
		}
		try
		{
			Installed.Add(Harmony.CreateAndPatchAll(typeof(EnhancePatch), (string)null));
			Installed.Add(Harmony.CreateAndPatchAll(typeof(TranslationPatch), (string)null));
			Installed.Add(Harmony.CreateAndPatchAll(typeof(VisualPatch), (string)null));
			Installed.Add(Harmony.CreateAndPatchAll(typeof(UiPatch), (string)null));
			MasterDataPatch.Install();
		}
		catch
		{
			Uninstall();
			throw;
		}
	}

	public static bool Uninstall()
	{
		if (!MasterDataPatch.TryUninstall())
		{
			return false;
		}
		foreach (Harmony item in Installed)
		{
			item.UnpatchSelf();
		}
		Installed.Clear();
		return true;
	}

	public static bool TryGetCurrentNovel(out Dictionary<string, string> translation)
	{
		translation = null;
		if (Config.Translation.Value)
		{
			return Plugin.Trans.TryGetNovelTranslation(Plugin.Trans.CurrentNovelId, out translation);
		}
		return false;
	}
}
