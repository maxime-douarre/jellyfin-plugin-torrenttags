# jellyfin-plugin-torrenttags

This plugin imports tags from your torrent client and adds them to the corresponding media in your Jellyfin libraries.

**TorrentTags** supports the following torrent clients:

- qBittorrent

Please open an issue if you would like support for another torrent client.

## How to install

Add the following repository to Jellyfin's plugin repositories:

`https://raw.githubusercontent.com/maxime-douarre/jellyfin-plugin-torrenttags/refs/heads/main/manifest.json`

This plugin follows the release cycle of [Jellyfin](https://github.com/jellyfin/jellyfin/releases).

## How to use

Configure the selected torrent client in the plugin's settings and trigger its scheduled task from the Jellyfin dashboard.

## How it works

1. **TorrentTags** builds a directory index using the torrents retrieved from the selected torrent client.
2. For each direct child item in every Jellyfin media library, **TorrentTags** navigates the previously built torrent directory index to find the tags to import.
3. The tags to import are applied to their item and stored in a dedicated SQLite database. This allows **TorrentTags** to remove them from media during future imports.

**Note**: Uninstalling **TorrentTags** deletes its dedicated SQLite database.

## How to build

Install .NET 10.0 and run the following command from the `Jellyfin.Plugin.TorrentTags` directory:

`dotnet build --configuration=Debug --framework=net10.0`

## How to debug

Build the plugin and place its `.dll` and `.pdb` files inside the [Jellyfin plugins folder](https://jellyfin.org/docs/general/server/plugins/#installing).

Then, you can either:

- Clone the [Jellyfin repository](https://github.com/jellyfin/jellyfin) and follow the instructions to run a server, add the plugin to the Jellyfin solution as an *Existing project...* and start debugging `Jellyfin.Server` from the Jellyfin solution.
- Attach the plugin to an existing Jellyfin process.

## How to publish

Install .NET 10.0 and run the following commands from the `Jellyfin.Plugin.TorrentTags` directory:

- `dotnet clean --configuration=Release --framework=net10.0`
- `dotnet restore --no-http-cache`
- `dotnet publish --no-restore --configuration=Release --framework=net10.0 -p:Version=<PLUGIN_VERSION>`

## Torrent clients

### qBittorrent

**TorrentTags** uses qBittorrent username/password authentication because qBittorrent does not support OAuth 2.0 authentication and API key authentication has only been made available recently (starting from qBittorrent v5.2.0).

qBittorrent username and password are stored by Jellyfin in the plugin's settings file. Read access to this file is restricted on Unix systems.
