using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.RegularExpressions;
using BepInEx;
using HarmonyLib;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GCMod.Patches;

[HarmonyPatch]
public static class UiPatch
{
	private static Dictionary<string, string> _translations = new Dictionary<string, string>(StringComparer.Ordinal);

	private static readonly ConcurrentDictionary<string, string> _cache = new ConcurrentDictionary<string, string>(StringComparer.Ordinal);

	[ThreadStatic]
	private static bool _isTranslating;

	public static void Initialize(string transDir, string language)
	{
		try
		{
			Dictionary<string, string> dict = new Dictionary<string, string>(StringComparer.Ordinal);

			string uiPath = Path.Combine(transDir, language, "ui.json");

			if (File.Exists(uiPath))
			{
				string json = File.ReadAllText(uiPath);
				Dictionary<string, string> loaded = JsonSerializer.Deserialize<Dictionary<string, string>>(json);
				if (loaded != null)
				{
					foreach (KeyValuePair<string, string> kvp in loaded)
					{
						if (!string.IsNullOrEmpty(kvp.Key) && !string.IsNullOrEmpty(kvp.Value) && kvp.Key != kvp.Value)
						{
							dict[kvp.Key] = kvp.Value;
						}
					}
				}
				Plugin.Log.LogInfo($"[UiPatch] Loaded {dict.Count} UI translations from {uiPath}");
			}
			else
			{
				Plugin.Log.LogWarning($"[UiPatch] ui.json not found at {uiPath}");
			}

			_translations = dict;
			_cache.Clear();
		}
		catch (Exception ex)
		{
			Plugin.Log.LogError($"[UiPatch] Failed to load UI translations: {ex}");
		}
	}

	public static bool TryTranslate(string original, out string translated)
	{
		translated = null;
		if (string.IsNullOrEmpty(original) || original.Length > 200 || _translations.Count == 0)
		{
			return false;
		}

		if (_cache.TryGetValue(original, out translated))
		{
			return translated != null;
		}

		if (_translations.TryGetValue(original, out translated))
		{
			_cache[original] = translated;
			return true;
		}

		string trimmed = original.Trim();
		if (trimmed.Length > 0 && trimmed.Length != original.Length && _translations.TryGetValue(trimmed, out string transTrimmed))
		{
			int start = original.IndexOf(trimmed, StringComparison.Ordinal);
			translated = original.Substring(0, start) + transTrimmed + original.Substring(start + trimmed.Length);
			_cache[original] = translated;
			return true;
		}

		if (original.IndexOf('<') >= 0 && original.IndexOf('>') > 0)
		{
			Match match = Regex.Match(original, @"^(<[^>]+>)(.+?)(</[^>]+>)$", RegexOptions.Singleline);
			if (match.Success)
			{
				string prefix = match.Groups[1].Value;
				string inner = match.Groups[2].Value;
				string suffix = match.Groups[3].Value;
				if (TryTranslate(inner, out string innerTrans))
				{
					translated = prefix + innerTrans + suffix;
					_cache[original] = translated;
					return true;
				}
			}
		}

		_cache[original] = null;
		return false;
	}

	[HarmonyPatch(typeof(TMP_Text), "text", MethodType.Setter)]
	[HarmonyPrefix]
	public static void SetTextPrefix(TMP_Text __instance, ref string value)
	{
		if (_isTranslating || value == null || value.Length <= 1 || !Config.Translation.Value)
		{
			return;
		}

		if (TryTranslate(value, out string translated))
		{
			value = translated;
		}
	}

	[HarmonyPatch(typeof(TextMeshProUGUI), "OnEnable")]
	[HarmonyPostfix]
	public static void OnEnablePostfix(TextMeshProUGUI __instance)
	{
		if (_isTranslating || (UnityEngine.Object)(object)__instance == (UnityEngine.Object)null || !Config.Translation.Value)
		{
			return;
		}

		try
		{
			_isTranslating = true;
			string current = ((TMP_Text)__instance).text;
			if (!string.IsNullOrEmpty(current) && current.Length > 1 && TryTranslate(current, out string translated))
			{
				((TMP_Text)__instance).text = translated;
			}
		}
		catch
		{
		}
		finally
		{
			_isTranslating = false;
		}
	}

	[HarmonyPatch(typeof(Text), "text", MethodType.Setter)]
	[HarmonyPrefix]
	public static void SetLegacyTextPrefix(Text __instance, ref string value)
	{
		if (_isTranslating || value == null || value.Length <= 1 || !Config.Translation.Value)
		{
			return;
		}

		if (TryTranslate(value, out string translated))
		{
			value = translated;
		}
	}
}
