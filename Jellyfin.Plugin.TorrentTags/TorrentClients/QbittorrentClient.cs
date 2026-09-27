using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Plugin.TorrentTags.Configuration;
using Jellyfin.Plugin.TorrentTags.Models;

namespace Jellyfin.Plugin.TorrentTags.TorrentClients;

public sealed class QbittorrentClient : ITorrentClient, IDisposable
{
    public const string ServiceKey = "qbittorrent";

    private readonly HttpClient _httpClient;

    public QbittorrentClient()
    {
        // QbittorrentClient does not use IHttpClientFactory because qBittorrent uses cookie-based authentication.
        _httpClient = new();
    }

    public async Task<TorrentDirectoryIndex> GetTorrentDirectoryIndexAsync(PluginConfiguration pluginConfiguration, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(pluginConfiguration);

        pluginConfiguration.EnsureQbittorrentSettingsAreSet();

        _httpClient.BaseAddress = new Uri(pluginConfiguration.QbittorrentBaseAddress);

        Dictionary<string, string> loginForm = new()
        {
            ["username"] = pluginConfiguration.QbittorrentUsername,
            ["password"] = pluginConfiguration.QbittorrentPassword
        };

        using (FormUrlEncodedContent formUrlEncodedContent = new(loginForm))
        {
            using HttpResponseMessage loginResponseMessage = await _httpClient.PostAsync("api/v2/auth/login", formUrlEncodedContent, cancellationToken).ConfigureAwait(false);
            loginResponseMessage.EnsureSuccessStatusCode();
        }

        TorrentDirectoryIndex torrentDirectoryIndex = new();

        await foreach (TorrentDto torrentDto in _httpClient.GetFromJsonAsAsyncEnumerable<TorrentDto>("api/v2/torrents/info", cancellationToken)
            .OfType<TorrentDto>()
            .Where(torrentDto => !string.IsNullOrEmpty(torrentDto.Tags))
            .ConfigureAwait(false))
        {
            DirectoryInfo contentPath = new(torrentDto.ContentPath);
            HashSet<string> tags = [.. torrentDto.Tags.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)];

            torrentDirectoryIndex.AddTorrent(contentPath, tags);
        }

        return torrentDirectoryIndex;
    }

    public void Dispose()
        => _httpClient.Dispose();

    private record TorrentDto(
        [property: JsonPropertyName("content_path")] string ContentPath,
        [property: JsonPropertyName("tags")] string Tags);
}
