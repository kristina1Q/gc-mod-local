using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using BepInEx.Unity.IL2CPP.Utils.Collections;
using TMPro;
using UnityEngine;
using Utility.Assets;

namespace GCMod.Services;

public sealed class TranslationManager : IDisposable
{
	private static readonly IReadOnlyDictionary<string, string> EmptyNames = new Dictionary<string, string>();

	private static readonly TimeSpan WaitLimit = TimeSpan.FromSeconds(10.0);

	private readonly Func<TranslationCache> _createCache;

	private readonly AssetBundleLoader<TMP_FontAsset> _font;

	private TranslationSession _session;

	private IEnumerator _fontLoad;

	private Coroutine _fontCoroutine;

	private bool _disposed;

	public IReadOnlyDictionary<string, string> Names => Volatile.Read(ref _session)?.Names ?? EmptyNames;

	public AssetBundleLoader<TMP_FontAsset> Font => _font;

	public int CurrentNovelId { get; private set; }

	public TranslationManager(Func<TranslationCache> createCache, AssetBundleLoader<TMP_FontAsset> font)
	{
		_createCache = createCache;
		_font = font;
		_session = new TranslationSession(createCache());
	}

	public void Initialize()
	{
		_fontLoad = _font.Load(delegate
		{
			if (!_disposed && !TMP_Settings.fallbackFontAssets.Contains(_font.Asset))
			{
				TMP_Settings.fallbackFontAssets.Add(_font.Asset);
				Logger.Info("Font loaded: " + ((UnityEngine.Object)_font.Asset).name);
			}
		}, delegate(Exception error)
		{
			Logger.Error("Font load failed: " + error.Message);
		});
		_fontCoroutine = Plugin.Instance.StartCoroutine(BepInEx.Unity.IL2CPP.Utils.Collections.CollectionExtensions.WrapToIl2Cpp(_fontLoad));
		LoadTranslationAsync();
	}

	public void Refresh()
	{
		if (!_disposed)
		{
			TranslationSession value = new TranslationSession(_createCache());
			Interlocked.Exchange(ref _session, value)?.Dispose();
			LoadTranslationAsync();
		}
	}

	public async Task LoadTranslationAsync()
	{
		TranslationSession translationSession = Volatile.Read(ref _session);
		if (translationSession == null || !Config.Translation.Value)
		{
			return;
		}
		try
		{
			Task namesAsync = translationSession.GetNamesAsync();
			Task task = ((Config.MasterDataTables.Value.Length != 0) ? translationSession.GetMasterAsync() : Task.CompletedTask);
			Task task2 = ((CurrentNovelId > 0) ? translationSession.GetNovelAsync(CurrentNovelId) : Task.CompletedTask);
			await Task.WhenAll(namesAsync, task, task2).ConfigureAwait(continueOnCapturedContext: false);
		}
		catch (OperationCanceledException)
		{
		}
		catch (Exception ex2)
		{
			Logger.Error("Translation preload failed: " + ex2.Message);
		}
	}

	public bool TryGetNovelTranslation(int id, out Dictionary<string, string> translations)
	{
		translations = null;
		return Volatile.Read(ref _session)?.TryGetNovel(id, out translations) ?? false;
	}

	public void PrepareNovel(int id)
	{
		CurrentNovelId = id;
		TranslationSession translationSession = Volatile.Read(ref _session);
		if (translationSession != null && Config.Translation.Value)
		{
			Task<Dictionary<string, string>> novelAsync = translationSession.GetNovelAsync(id);
			if (Config.AsyncMode.Value)
			{
				ObserveAsync(novelAsync, $"novel/{id}");
			}
			else
			{
				WaitFor(novelAsync, $"novel/{id}");
			}
		}
	}

	public string TranslateMasterData(string tableName, string json)
	{
		string[] value = Config.MasterDataTables.Value;
		TranslationSession translationSession = Volatile.Read(ref _session);
		if (translationSession == null || !Config.Translation.Value || !MasterDataTranslator.IsEnabled(tableName, value))
		{
			return json;
		}
		string obj = WaitFor(translationSession.GetMasterForUseAsync(), "master")?.Translate(tableName, json, value) ?? json;
		if ((object)obj != json)
		{
			Logger.Info("Master data translated: " + tableName);
		}
		return obj;
	}

	private static T WaitFor<T>(Task<T> task, string resource) where T : class
	{
		try
		{
			return task.WaitAsync(WaitLimit).GetAwaiter().GetResult();
		}
		catch (TimeoutException)
		{
			Logger.Warn("Translation wait timed out: " + resource + "; keeping original text while loading continues");
			ObserveAsync(task, resource);
		}
		catch (OperationCanceledException)
		{
		}
		catch (Exception ex3)
		{
			Logger.Error("Translation load failed: " + resource + ": " + ex3.Message);
		}
		return null;
	}

	private static async Task ObserveAsync(Task task, string resource)
	{
		try
		{
			await task.ConfigureAwait(continueOnCapturedContext: false);
		}
		catch (OperationCanceledException)
		{
		}
		catch (Exception ex2)
		{
			Logger.Error("Translation load failed: " + resource + ": " + ex2.Message);
		}
	}

	public void Dispose()
	{
		if (!_disposed)
		{
			_disposed = true;
			Interlocked.Exchange(ref _session, null)?.Dispose();
			if (_fontCoroutine != null)
			{
				Plugin.Instance.StopCoroutine(_fontCoroutine);
			}
			(_fontLoad as IDisposable)?.Dispose();
			if (_font.IsLoaded)
			{
				TMP_Settings.fallbackFontAssets.Remove(_font.Asset);
			}
		}
	}
}
