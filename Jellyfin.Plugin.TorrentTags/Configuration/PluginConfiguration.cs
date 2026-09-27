using System;
using System.Diagnostics.CodeAnalysis;
using Jellyfin.Plugin.TorrentTags.TorrentClients;
using MediaBrowser.Model.Plugins;

namespace Jellyfin.Plugin.TorrentTags.Configuration;

public class PluginConfiguration : BasePluginConfiguration
{
    public string TorrentClient { get; set; } = QbittorrentClient.ServiceKey;
    public string? QbittorrentBaseAddress { get; set; }
    public string? QbittorrentUsername { get; set; }
    public string? QbittorrentPassword { get; set; }

    [MemberNotNull(nameof(QbittorrentBaseAddress), nameof(QbittorrentUsername), nameof(QbittorrentPassword))]
    public void EnsureQbittorrentSettingsAreSet()
    {
        if (string.IsNullOrEmpty(QbittorrentBaseAddress))
            throw new InvalidOperationException($"Plugin setting '{nameof(QbittorrentBaseAddress)}' is not set.");
        if (string.IsNullOrEmpty(QbittorrentUsername))
            throw new InvalidOperationException($"Plugin setting '{nameof(QbittorrentUsername)}' is not set.");
        if (string.IsNullOrEmpty(QbittorrentPassword))
            throw new InvalidOperationException($"Plugin setting '{nameof(QbittorrentPassword)}' is not set.");
    }
}
