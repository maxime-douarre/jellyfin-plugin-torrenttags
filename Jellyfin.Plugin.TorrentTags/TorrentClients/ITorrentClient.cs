using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Plugin.TorrentTags.Configuration;
using Jellyfin.Plugin.TorrentTags.Models;

namespace Jellyfin.Plugin.TorrentTags.TorrentClients;

public interface ITorrentClient
{
    public Task<TorrentDirectoryIndex> GetTorrentDirectoryIndexAsync(PluginConfiguration pluginConfiguration, CancellationToken cancellationToken);
}
