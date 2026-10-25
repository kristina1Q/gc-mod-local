using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace GCMod.Services;

public sealed class TranslationSession : IDisposable, IAsyncDisposable
{
	private static readonly Dictionary<string, string> EmptyNames = new Dictionary<string, string>();

	private readonly TranslationCache _cache;

	private readonly object _gate = new object();

	private readonly CancellationTokenSource _lifetime = new CancellationTokenSource();

	private readonly CancellationToken _token;

	private readonly TaskCompletionSource _disposal = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

	private readonly Dictionary<int, Task<Dictionary<string, string>>> _novels = new Dictionary<int, Task<Dictionary<string, string>>>();

	private Task<Manifest> _manifest;

	private Task<Dictionary<string, string>> _names;

	private Task<MasterDataTranslator> _master;

	private Task<MasterDataTranslator> _cachedMaster;

	private bool _disposed;

	public IReadOnlyDictionary<string, string> Names
	{
		get
		{
			lock (_gate)
			{
				return Ready(_names) ?? EmptyNames;
			}
		}
	}

	public TranslationSession(TranslationCache cache)
	{
		_cache = cache;
		_token = _lifetime.Token;
	}

	public bool TryGetNovel(int id, out Dictionary<string, string> translations)
	{
		lock (_gate)
		{
			_novels.TryGetValue(id, out var value);
			translations = Ready(value);
			return translations != null;
		}
	}

	public Task<Dictionary<string, string>> GetNamesAsync()
	{
		lock (_gate)
		{
			ThrowIfDisposed();
			return _names = ReuseOrStart(_names, () => LoadAsync<Dictionary<string, string>>("names"));
		}
	}

	public Task<Dictionary<string, string>> GetNovelAsync(int id)
	{
		lock (_gate)
		{
			ThrowIfDisposed();
			_novels.TryGetValue(id, out var value);
			return _novels[id] = ReuseOrStart(value, () => LoadAsync<Dictionary<string, string>>("novels", id.ToString()));
		}
	}

	public Task<MasterDataTranslator> GetMasterAsync()
	{
		lock (_gate)
		{
			ThrowIfDisposed();
			return _master = ReuseOrStart(_master, LoadMasterAsync);
		}
	}

	public async Task<MasterDataTranslator> GetMasterForUseAsync()
	{
		Task<MasterDataTranslator> latest = GetMasterAsync();
		MasterDataTranslator masterDataTranslator = Ready(latest);
		if (masterDataTranslator != null)
		{
			return masterDataTranslator;
		}
		Task<MasterDataTranslator> task;
		lock (_gate)
		{
			ThrowIfDisposed();
			task = (_cachedMaster = ReuseOrStart(_cachedMaster, LoadCachedMasterAsync));
		}
		MasterDataTranslator masterDataTranslator2 = await task.ConfigureAwait(continueOnCapturedContext: false);
		_token.ThrowIfCancellationRequested();
		MasterDataTranslator masterDataTranslator3 = Ready(latest);
		if (masterDataTranslator3 == null)
		{
			MasterDataTranslator masterDataTranslator4 = masterDataTranslator2;
			if (masterDataTranslator4 == null)
			{
				masterDataTranslator4 = await latest.ConfigureAwait(continueOnCapturedContext: false);
			}
			masterDataTranslator3 = masterDataTranslator4;
		}
		return masterDataTranslator3;
	}

	private async Task<T> LoadAsync<T>(string type, string id = null) where T : class
	{
		Task<Manifest> task;
		lock (_gate)
		{
			task = (_manifest = ReuseOrStart(_manifest, () => _cache.FetchManifestAsync(_token)));
		}
		await task.ConfigureAwait(continueOnCapturedContext: false);
		return await _cache.LoadAsync<T>(type, id, _token).ConfigureAwait(continueOnCapturedContext: false);
	}

	private async Task<MasterDataTranslator> LoadMasterAsync()
	{
		Dictionary<string, Dictionary<string, Dictionary<string, string>>> dictionary = await LoadAsync<Dictionary<string, Dictionary<string, Dictionary<string, string>>>>("master").ConfigureAwait(continueOnCapturedContext: false);
		return (dictionary == null) ? null : new MasterDataTranslator(dictionary);
	}

	private async Task<MasterDataTranslator> LoadCachedMasterAsync()
	{
		Dictionary<string, Dictionary<string, Dictionary<string, string>>> dictionary = await _cache.LoadLocalAsync<Dictionary<string, Dictionary<string, Dictionary<string, string>>>>("master", null, _token).ConfigureAwait(continueOnCapturedContext: false);
		return (dictionary == null) ? null : new MasterDataTranslator(dictionary);
	}

	private static T Ready<T>(Task<T> task) where T : class
	{
		if (task == null || !task.IsCompletedSuccessfully)
		{
			return null;
		}
		return task.Result;
	}

	private static Task<T> ReuseOrStart<T>(Task<T> task, Func<Task<T>> load) where T : class
	{
		if (task == null || (task.IsCompleted && Ready(task) == null))
		{
			return load();
		}
		return task;
	}

	private void ThrowIfDisposed()
	{
		if (_disposed)
		{
			throw new ObjectDisposedException("TranslationSession");
		}
	}

	public void Dispose()
	{
		Task[] pending;
		lock (_gate)
		{
			if (_disposed)
			{
				return;
			}
			_disposed = true;
			pending = (from task in _novels.Values.Cast<Task>().Concat(new Task[4] { _manifest, _names, _master, _cachedMaster })
				where task != null
				select task).ToArray();
		}
		_lifetime.Cancel();
		FinishDisposalAsync(pending);
	}

	public ValueTask DisposeAsync()
	{
		Dispose();
		return new ValueTask(_disposal.Task);
	}

	private async Task FinishDisposalAsync(Task[] pending)
	{
		try
		{
			await Task.WhenAll(pending).ConfigureAwait(continueOnCapturedContext: false);
		}
		catch (OperationCanceledException)
		{
		}
		catch (Exception ex2)
		{
			Logger.Warn("Translation session ended with an error: " + ex2.Message);
		}
		finally
		{
			_lifetime.Dispose();
			_disposal.TrySetResult();
		}
	}
}
