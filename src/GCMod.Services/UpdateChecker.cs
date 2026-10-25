using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Utility.Notifications;

namespace GCMod.Services;

internal static class UpdateChecker
{
	private const string LatestReleaseUrl = "https://github.com/anosu/GCMod/releases/latest";

	private const string TagPath = "/anosu/GCMod/releases/tag/";

	public static async Task CheckAsync(HttpClient httpClient, string currentVersion, CancellationToken cancellationToken = default(CancellationToken))
	{
		try
		{
			using HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Head, "https://github.com/anosu/GCMod/releases/latest");
			using HttpResponseMessage httpResponseMessage = await httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
			cancellationToken.ThrowIfCancellationRequested();
			if (!httpResponseMessage.IsSuccessStatusCode)
			{
				Logger.Warn($"Update check failed: {httpResponseMessage.StatusCode} {httpResponseMessage.StatusCode}");
				return;
			}
			Uri uri = httpResponseMessage.RequestMessage?.RequestUri;
			string text = uri?.AbsolutePath.TrimEnd('/');
			if (uri?.Host != "github.com" || text == null || !text.StartsWith("/anosu/GCMod/releases/tag/", StringComparison.OrdinalIgnoreCase))
			{
				Logger.Warn($"Update check returned an unexpected URL: {uri}");
				return;
			}
			string text2 = text;
			int length = "/anosu/GCMod/releases/tag/".Length;
			string text3 = Uri.UnescapeDataString(text2.Substring(length, text2.Length - length));
			if (!TryParseVersion(text3, out var version) || !TryParseVersion(currentVersion, out var version2))
			{
				Logger.Warn("Update check could not compare versions: " + currentVersion + " / " + text3);
				return;
			}
			if (version <= version2)
			{
				return;
			}
			Logger.Info($"New GCMod version available: {currentVersion} -> {text3}. Release: {uri}");
			Toast.Info("GCMod 发现新版本", $"当前：{currentVersion} → 最新：{text3}\n请到 GitHub Releases 下载更新");
		}
		catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
		{
		}
		catch (TaskCanceledException)
		{
			Logger.Warn("Update check timed out");
		}
		catch (Exception ex3)
		{
			Logger.Warn("Update check failed: " + ex3.Message);
		}
	}

	private static bool TryParseVersion(string text, out Version version)
	{
		version = null;
		text = text?.Trim();
		if (!string.IsNullOrEmpty(text) && (text[0] == 'v' || text[0] == 'V'))
		{
			string text2 = text;
			text = text2.Substring(1, text2.Length - 1);
		}
		if (!Version.TryParse(text, out Version result))
		{
			return false;
		}
		version = new Version(result.Major, result.Minor, Math.Max(0, result.Build), Math.Max(0, result.Revision));
		return true;
	}
}
