using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Plugin.TorrentTags.Configuration;
using Jellyfin.Plugin.TorrentTags.Models;
using Jellyfin.Plugin.TorrentTags.Repositories;
using Jellyfin.Plugin.TorrentTags.Services;
using Jellyfin.Plugin.TorrentTags.TorrentClients;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Globalization;
using MediaBrowser.Model.Tasks;
using Microsoft.Extensions.DependencyInjection;

namespace Jellyfin.Plugin.TorrentTags.ScheduledTasks;

public class ImportTorrentTagsTask : IScheduledTask
{
    private readonly ILocalizationManager _localizationManager;
    private readonly ILibraryManager _libraryManager;
    private readonly IServiceScopeFactory _serviceScopeFactory;

    public ImportTorrentTagsTask(
        ILocalizationManager localizationManager,
        ILibraryManager libraryManager,
        IServiceScopeFactory serviceScopeFactory)
    {
        _localizationManager = localizationManager;
        _libraryManager = libraryManager;
        _serviceScopeFactory = serviceScopeFactory;
    }

    public string Name => "Import torrent tags";

    public string Description => "Imports tags from your torrent client and adds them to the corresponding media in your Jellyfin libraries.";

    public string Key => "ImportTorrentTags";

    public string Category => _localizationManager.GetLocalizedString("TasksLibraryCategory");

    [SuppressMessage("Style", "IDE0028:Simplify collection initialization", Justification = "HashSet has a constructor that uses 'private void ConstructFrom(HashSet<T> source)'.")]
    [SuppressMessage("Style", "IDE0306:Simplify collection initialization", Justification = "HashSet has a constructor that uses 'private void ConstructFrom(HashSet<T> source)'.")]
    public async Task ExecuteAsync(IProgress<double> progress, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(progress);

        progress.Report(0.0);

        IReadOnlyCollection<BaseItem> baseItems = [.. _libraryManager.GetUserRootFolder().Children.OfType<CollectionFolder>().SelectMany(collectionFolder => collectionFolder.Children)];

        if (baseItems.Count == 0)
        {
            progress.Report(100.0);
            return;
        }

        Plugin plugin = Plugin.Instance
            ?? throw new InvalidOperationException($"Plugin instance is null.");

        PluginConfiguration pluginConfiguration = plugin.Configuration
            ?? throw new InvalidOperationException("Plugin configuration is null.");

        using IServiceScope serviceScope = _serviceScopeFactory.CreateScope();

        ITorrentClient torrentClient = serviceScope.ServiceProvider.GetRequiredKeyedService<ITorrentClient>(pluginConfiguration.TorrentClient);
        DatabaseMigrationService databaseMigrationService = serviceScope.ServiceProvider.GetRequiredService<DatabaseMigrationService>();
        TorrentTagRepository torrentTagRepository = serviceScope.ServiceProvider.GetRequiredService<TorrentTagRepository>();

        Task<TorrentDirectoryIndex> getTorrentDirectoryIndexTask = torrentClient.GetTorrentDirectoryIndexAsync(pluginConfiguration, cancellationToken);

        Task prepareDatabaseTask = Task.Run(() =>
        {
            databaseMigrationService.ApplyMigrations();
            torrentTagRepository.DeleteOrphanTorrentTags(baseItems.Select(baseItem => baseItem.Id));
        }, cancellationToken);

        await Task.WhenAll(getTorrentDirectoryIndexTask, prepareDatabaseTask).ConfigureAwait(false);

#pragma warning disable CA1849 // Call async methods when in an async method
        TorrentDirectoryIndex torrentDirectoryIndex = getTorrentDirectoryIndexTask.Result;
#pragma warning restore CA1849 // Call async methods when in an async method

        int completeBaseItemsCount = 0;

        foreach (BaseItem baseItem in baseItems)
        {
            HashSet<string> importTags = torrentDirectoryIndex.GetTags(new DirectoryInfo(baseItem.Path));
            HashSet<string> existingTags = [.. torrentTagRepository.GetBaseItemTorrentTags(baseItem.Id)];

            // Using the HashSet constructor instead of the collection expression is more efficient.
            HashSet<string> tagsToDelete = new(existingTags);
            tagsToDelete.ExceptWith(importTags);

            HashSet<string> baseItemTagsBefore = [.. baseItem.Tags];
            HashSet<string> baseItemTagsAfter = new(baseItemTagsBefore);
            baseItemTagsAfter.UnionWith(importTags);
            baseItemTagsAfter.ExceptWith(tagsToDelete);

            if (!baseItemTagsAfter.SetEquals(baseItemTagsBefore))
            {
                baseItem.Tags = [.. baseItemTagsAfter];
                await baseItem.UpdateToRepositoryAsync(ItemUpdateType.MetadataEdit, cancellationToken).ConfigureAwait(false);
            }

            HashSet<string> tagsToAdd = importTags;
            tagsToAdd.ExceptWith(existingTags);

            torrentTagRepository.UpdateBaseItemTorrentTags(baseItem.Id, tagsToAdd, tagsToDelete);

            progress.Report(++completeBaseItemsCount * 100.0 / baseItems.Count);
        }

        progress.Report(100.0);
    }

    public IEnumerable<TaskTriggerInfo> GetDefaultTriggers() => [];
}
