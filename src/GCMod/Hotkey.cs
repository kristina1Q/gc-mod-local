using System;
using System.Collections.Generic;
using BepInEx.Configuration;
using GCMod.Patches;
using GCMod.Services;
using UnityEngine;

namespace GCMod;

public class Hotkey : MonoBehaviour
{
	private const float DebounceInterval = 0.15f;

	private readonly Dictionary<KeyCode, float> _lastPressTime = new Dictionary<KeyCode, float>();

	private void Update()
	{
		CheckToggle((KeyCode)284, () => Config.IsSkipCutin);
		CheckToggle((KeyCode)286, () => Config.ModifyText);
		CheckToggle((KeyCode)289, () => Config.Translation);
		if (Input.GetKeyDown((KeyCode)287) && CanTrigger((KeyCode)287))
		{
			Config.NormalAlpha.Value = AlphaController.ClampAlpha(Config.NormalAlpha.Value + AlphaController.GetDelta(IsAltPressed()));
			AlphaController.ApplyAlpha(Config.NormalAlpha.Value, VisualPatch.NormalFrame, VisualPatch.BaseNameFrame);
		}
		if (Input.GetKeyDown((KeyCode)288) && CanTrigger((KeyCode)288))
		{
			Config.CgModeAlpha.Value = AlphaController.ClampAlpha(Config.CgModeAlpha.Value + AlphaController.GetDelta(IsAltPressed()));
			AlphaController.ApplyAlpha(Config.CgModeAlpha.Value, VisualPatch.CgModeFrame);
		}
		if (Input.GetKeyDown((KeyCode)291) && CanTrigger((KeyCode)291))
		{
			Plugin.ConfigFile.Reload();
			Plugin.Trans.Refresh();
			Logger.Info("Config reloaded");
		}
	}

	private void CheckToggle(KeyCode key, Func<ConfigEntry<bool>> getter)
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_0009: Unknown result type (might be due to invalid IL or missing references)
		if (Input.GetKeyDown(key) && CanTrigger(key))
		{
			ConfigEntry<bool> obj = getter();
			obj.Value = !obj.Value;
			if (obj == Config.Translation && Config.Translation.Value)
			{
				Plugin.Trans.LoadTranslationAsync();
			}
		}
	}

	private bool CanTrigger(KeyCode key)
	{
		//IL_000c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0028: Unknown result type (might be due to invalid IL or missing references)
		float time = Time.time;
		if (_lastPressTime.TryGetValue(key, out var value) && time - value < 0.15f)
		{
			return false;
		}
		_lastPressTime[key] = time;
		return true;
	}

	private static bool IsAltPressed()
	{
		if (!Input.GetKey((KeyCode)308))
		{
			return Input.GetKey((KeyCode)307);
		}
		return true;
	}
}
