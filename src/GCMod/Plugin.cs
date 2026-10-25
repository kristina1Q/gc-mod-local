using System;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Core.Logging.Interpolation;
using BepInEx.Logging;
using BepInEx.Unity.IL2CPP;
using DMM.OLG.Unity.Engine;
using GCMod.Patches;
using GCMod.Services;
using TMPro;
using UnityEngine;
using Utility.Assets;
using Utility.Notifications;

namespace GCMod;

[BepInPlugin("GCMod", "GCMod", "7.0.0")]
public class Plugin : BasePlugin
{
	private const int HttpTimeoutSeconds = 30;

	private const int PooledConnectionLifetimeMinutes = 5;

	private const int PooledConnectionIdleTimeoutMinutes = 2;

	private static HttpClient _httpClient;

	private static CancellationTokenSource _updateCheckCancellation;

	public static ConfigFile ConfigFile;

	public static ManualLogSource Log;

	public static MonoBehaviour Instance;

	public static TranslationManager Trans;

	public override void Load()
	{
		//IL_002f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0035: Expected O, but got Unknown
		try
		{
			Console.OutputEncoding = Encoding.UTF8;
		}
		catch
		{
		}
		Log = ((BasePlugin)this).Log;
		ConfigFile = ((BasePlugin)this).Config;
		Log.LogInfo("Plugin GCMod is loaded!");
		Toast.Initialize();
		GCMod.Config.Initialize();
		Instance = (MonoBehaviour)(object)((BasePlugin)this).AddComponent<Hotkey>();
		try
		{
			Initialize();
			PatchManager.Initialize();
			Trans.Initialize();
		}
		catch
		{
			((BasePlugin)this).Unload();
			throw;
		}
		Toast.Success("GCMod", "Mod 加载成功，版本: 7.0.0");
		if (GCMod.Config.CheckUpdate.Value)
		{
			_updateCheckCancellation = new CancellationTokenSource();
			UpdateChecker.CheckAsync(_httpClient, "7.0.0", _updateCheckCancellation.Token);
		}
	}

	private static void Initialize()
	{
		_httpClient = new HttpClient(new SocketsHttpHandler
		{
			PooledConnectionLifetime = TimeSpan.FromMinutes(5.0),
			PooledConnectionIdleTimeout = TimeSpan.FromMinutes(2.0)
		})
		{
			Timeout = TimeSpan.FromSeconds(30.0)
		};
		string value = GCMod.Config.FontBundlePath.Value;
		AssetBundleLoader<TMP_FontAsset> font = new AssetBundleLoader<TMP_FontAsset>(Path.IsPathRooted(value) ? value : Path.Combine(Paths.PluginPath, value));
		string transDir = GCMod.Config.CustomTranslationPath.Value;
		if (string.IsNullOrWhiteSpace(transDir) || !Directory.Exists(transDir))
		{
			transDir = Path.Combine(Paths.PluginPath, "GCMod", "translations");
		}
		Log.LogInfo("Using translations path: " + transDir);
		Trans = new TranslationManager(() => new TranslationCache(GCMod.Config.TranslationCDN.Value, transDir, GCMod.Config.TranslationLanguage.Value, _httpClient), font);
		UiPatch.Initialize(transDir, GCMod.Config.TranslationLanguage.Value);
		MasterDataPatch.JsonRewriter = delegate(MasterAttribute attribute, string json)
		{
			if (attribute != null)
			{
				return Trans.TranslateMasterData(attribute.Object, json);
			}
			return json;
		};
	}

	public override bool Unload()
	{
		if (!PatchManager.Uninstall())
		{
			Log.LogWarning((object)"Cannot unload while a master data callback is active; retry after loading finishes");
			return false;
		}
		Trans?.Dispose();
		MasterDataPatch.JsonRewriter = null;
		_updateCheckCancellation?.Cancel();
		_updateCheckCancellation?.Dispose();
		_updateCheckCancellation = null;
		_httpClient?.Dispose();
		_httpClient = null;
		if ((UnityEngine.Object)(object)Instance != (UnityEngine.Object)null)
		{
			UnityEngine.Object.Destroy((UnityEngine.Object)(object)Instance);
		}
		Instance = null;
		Toast.Clear();
		return true;
	}
}
