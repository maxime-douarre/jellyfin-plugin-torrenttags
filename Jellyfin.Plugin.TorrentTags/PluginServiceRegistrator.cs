using System.IO;
using Jellyfin.Plugin.TorrentTags.Repositories;
using Jellyfin.Plugin.TorrentTags.Services;
using Jellyfin.Plugin.TorrentTags.TorrentClients;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Plugins;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.DependencyInjection;

namespace Jellyfin.Plugin.TorrentTags;

public class PluginServiceRegistrator : IPluginServiceRegistrator
{
    public void RegisterServices(IServiceCollection serviceCollection, IServerApplicationHost applicationHost)
    {
        serviceCollection.AddKeyedScoped<ITorrentClient, QbittorrentClient>(QbittorrentClient.ServiceKey);

        serviceCollection.AddKeyedTransient<SqliteConnection>(Plugin.DatabaseFileName, (serviceProvider, serviceKey) =>
        {
            IServerApplicationPaths serverApplicationPaths = serviceProvider.GetRequiredService<IServerApplicationPaths>();
            // Pooling=False because Jellyfin must be able to delete the database between imports.
            return new($"Data Source={Path.Combine(serverApplicationPaths.DataPath, Plugin.DatabaseFileName)};Pooling=False");
        });

        serviceCollection.AddScoped<DatabaseMigrationService>();
        serviceCollection.AddScoped<TorrentTagRepository>();
    }
}
