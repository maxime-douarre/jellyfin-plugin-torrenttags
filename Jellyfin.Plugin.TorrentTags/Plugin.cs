using System;
using System.Collections.Generic;
using System.IO;
using Jellyfin.Plugin.TorrentTags.Configuration;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Plugins;
using MediaBrowser.Controller;
using MediaBrowser.Model.Plugins;
using MediaBrowser.Model.Serialization;

namespace Jellyfin.Plugin.TorrentTags;

public class Plugin : BasePlugin<PluginConfiguration>, IHasWebPages
{
    public const string DatabaseFileName = "jellyfin-plugin-torrenttags.db";

    private readonly IServerApplicationPaths _serverApplicationPaths;

    public Plugin(
        IServerApplicationPaths serverApplicationPaths,
        IApplicationPaths applicationPaths,
        IXmlSerializer xmlSerializer)
        : base(applicationPaths, xmlSerializer)
    {
        _serverApplicationPaths = serverApplicationPaths;

        Instance = this;
    }

    public static Plugin? Instance { get; private set; }

    public override Guid Id => Guid.Parse("8a569ff4-48e9-4ab9-bc87-fcc7854dd69a");

    public override string Name => "Torrent Tags";

    public override string Description => "Imports tags from your torrent client and adds them to the corresponding media in your Jellyfin libraries.";

    public IEnumerable<PluginPageInfo> GetPages()
    {
        yield return new PluginPageInfo
        {
            Name = Name,
            EmbeddedResourcePath = GetType().Namespace + ".Configuration.config.html"
        };
    }

    public override void OnUninstalling()
        => File.Delete(Path.Combine(_serverApplicationPaths.DataPath, DatabaseFileName));
}
