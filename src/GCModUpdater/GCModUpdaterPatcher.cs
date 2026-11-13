using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using BepInEx.Configuration;
using BepInEx.Logging;
using BepInEx.Preloader.Core.Patching;

namespace GCModUpdater;

[PatcherPluginInfo("com.kristina1q.gcmod.updater", "GCMod Auto Updater", "1.0.0")]
public class GCModUpdaterPatcher : BasePatcher
{
    private ConfigEntry<bool> _enabled;
    private ConfigEntry<string> _manifestUrl;
    private ConfigEntry<int> _timeoutSeconds;

    private const string DefaultManifestUrl = "https://pub-187293b65a6a475db6e4e2e5dcf1c552.r2.dev/update_manifest.json";

    public override void Initialize()
    {
        _enabled = Config.Bind("General", "Enabled", true, "是否启用在线增量热更新");
        _manifestUrl = Config.Bind("General", "ManifestUrl", DefaultManifestUrl, "更新清单的在线地址 (支持 Cloudflare R2 / CDN / GitHub 等)");
        _timeoutSeconds = Config.Bind("General", "TimeoutSeconds", 8, "检查更新清单的超时秒数 (超时自动跳过以确保正常进入游戏)");

        if (!_enabled.Value)
        {
            Log.LogInfo("GCMod 在线更新已关闭，跳过检查。");
            return;
        }

        string manifestUrl = _manifestUrl.Value?.Trim();
        if (string.IsNullOrWhiteSpace(manifestUrl) || manifestUrl.Contains("example.com"))
        {
            Log.LogInfo("未配置有效的 ManifestUrl，跳过在线更新检查。");
            return;
        }

        try
        {
            int checkTimeout = Math.Max(3, _timeoutSeconds.Value);
            Log.LogInfo($"正在检查在线更新: {manifestUrl} (清单超时上限: {checkTimeout}秒)...");
            Task.Run(() => CheckAndSyncAsync(manifestUrl, checkTimeout)).GetAwaiter().GetResult();
        }
        catch (OperationCanceledException)
        {
            Log.LogWarning("检查或下载更新超时，已自动跳过以保证游戏正常启动。");
        }
        catch (Exception ex)
        {
            Log.LogWarning($"检查更新过程中发生异常: {ex.Message}");
        }
    }

    private async Task CheckAndSyncAsync(string manifestUrl, int checkTimeoutSeconds)
    {
        using var client = new HttpClient();
        client.Timeout = TimeSpan.FromSeconds(120);

        string manifestJson;
        using (var checkCts = new CancellationTokenSource(TimeSpan.FromSeconds(checkTimeoutSeconds)))
        {
            manifestJson = await client.GetStringAsync(manifestUrl, checkCts.Token).ConfigureAwait(false);
        }
        var manifest = JsonSerializer.Deserialize<UpdateManifest>(manifestJson);

        if (manifest?.Files == null || manifest.Files.Count == 0)
        {
            Log.LogInfo("未在清单中检测到有效文件，更新跳过。");
            return;
        }

        string baseUrl = manifestUrl.Substring(0, manifestUrl.LastIndexOf('/'));
        string gameRoot = AppDomain.CurrentDomain.BaseDirectory;
        int updatedCount = 0;

        using var downloadCts = new CancellationTokenSource(TimeSpan.FromSeconds(180));
        CancellationToken ct = downloadCts.Token;

        foreach (var item in manifest.Files)
        {
            ct.ThrowIfCancellationRequested();
            if (string.IsNullOrWhiteSpace(item.Path)) continue;

            string localRelative = item.Path.Replace('/', Path.DirectorySeparatorChar);
            string localFullPath = Path.Combine(gameRoot, localRelative);

            // 如果本地文件存在且哈希一致，跳过
            if (File.Exists(localFullPath))
            {
                string localHash = GetFileMd5(localFullPath);
                if (localHash.Equals(item.Hash, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }
            }

            // 需要下载或更新
            string downloadUrl = $"{baseUrl}/{item.Path.TrimStart('/')}";
            Log.LogInfo($"[增量下载] {item.Path} ...");

            byte[] data = await client.GetByteArrayAsync(downloadUrl, ct).ConfigureAwait(false);

            string targetDir = Path.GetDirectoryName(localFullPath);
            if (!string.IsNullOrEmpty(targetDir))
            {
                Directory.CreateDirectory(targetDir);
            }

            string tempFile = localFullPath + ".tmp";
            await File.WriteAllBytesAsync(tempFile, data, ct).ConfigureAwait(false);
            File.Move(tempFile, localFullPath, overwrite: true);
            updatedCount++;
        }

        if (updatedCount > 0)
        {
            Log.LogInfo($"[更新完成] 成功更新/下载了 {updatedCount} 个文件，版本: {manifest.Version}");
        }
        else
        {
            Log.LogInfo($"[已是最新] 本地所有组件均与线上 {manifest.Version} 一致。");
        }
    }

    private static string GetFileMd5(string filePath)
    {
        try
        {
            using var md5 = MD5.Create();
            using var stream = File.OpenRead(filePath);
            byte[] hash = md5.ComputeHash(stream);
            return Convert.ToHexString(hash).ToLowerInvariant();
        }
        catch
        {
            return string.Empty;
        }
    }

    private class UpdateManifest
    {
        [JsonPropertyName("version")]
        public string Version { get; set; }

        [JsonPropertyName("notice")]
        public string Notice { get; set; }

        [JsonPropertyName("files")]
        public List<UpdateFileInfo> Files { get; set; }
    }

    private class UpdateFileInfo
    {
        [JsonPropertyName("path")]
        public string Path { get; set; }

        [JsonPropertyName("hash")]
        public string Hash { get; set; }

        [JsonPropertyName("size")]
        public long Size { get; set; }
    }
}
