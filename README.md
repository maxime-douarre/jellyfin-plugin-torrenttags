# jellyfin-plugin-torrenttags

This plugin imports tags from your torrent client and adds them to the corresponding media in your Jellyfin libraries.

**Torrent Tags** supports the following torrent clients:

- qBittorrent

Please open an issue to request support for another torrent client.

## How to install

1. From Jellyfin's `Dashboard`, go to the `Plugins` menu and click on `Manage Repositories`.
2. Create a `New Repository` with the following `Repository URL`: `https://raw.githubusercontent.com/maxime-douarre/jellyfin-plugin-torrenttags/refs/heads/main/manifest.json`.
3. Go back to the `Plugins` menu, find and install the latest available version of **Torrent Tags**.

**Note**: **Torrent Tags** follows the release cycle of [Jellyfin](https://github.com/jellyfin/jellyfin/releases). Update Jellyfin if you wish to install the latest version of **Torrent Tags**.

## How to use

1. From Jellyfin's `Dashboard`, go to the `Plugins` menu and click on `Torrent Tags`.
2. Under `Settings`, select and configure a `Torrent client`.
3. From Jellyfin's `Dashboard`, go to `Scheduled Tasks` and trigger the `Import torrent tags` task to import tags from the selected torrent client.

## How it works

1. **Torrent Tags** builds a directory index from the torrents retrieved from the selected torrent client.
2. For each direct child item of every Jellyfin media library, **Torrent Tags** searches the directory index for the tags to import.
3. The tags to import are applied to the corresponding item and stored in a dedicated SQLite database. This database allows **Torrent Tags** to remove deleted tags from media during subsequent imports.

**Note**: Uninstalling **Torrent Tags** deletes its dedicated SQLite database.

## How to build

Install .NET 10.0 and run the following command from the `Jellyfin.Plugin.TorrentTags.csproj` directory:

`dotnet build --configuration=Debug --framework=net10.0`

## How to debug in Visual Studio

1. [Install Jellyfin Server](https://jellyfin.org/docs/general/installation/).
2. Create a `TorrentTags` directory inside the [Jellyfin plugin directory](https://jellyfin.org/docs/general/server/plugins/).
3. Build `Jellyfin.Plugin.TorrentTags.csproj` and place its `.dll` and `.pdb` files inside the `TorrentTags` plugin directory.
4. Create a new `Executable` launch profile for `Jellyfin.Plugin.TorrentTags.csproj` and set the executable path to `jellyfin.exe`.
5. Start Debugging.

## How to publish

Install .NET 10.0 and run the following commands from the `Jellyfin.Plugin.TorrentTags.csproj` directory:

- `dotnet clean --configuration=Release --framework=net10.0`
- `dotnet restore --no-http-cache`
- `dotnet publish --no-restore --configuration=Release --framework=net10.0 -p:Version=<PLUGIN_VERSION>`

## Torrent clients

### qBittorrent

**Torrent Tags** only supports [qBittorrent's username/password authentication](https://github.com/qbittorrent/qBittorrent/wiki/WebUI-API-\(qBittorrent-5.0\)#authentication) for the following reasons:

- qBittorrent does not support OAuth 2.0 authentication.
- API key authentication has only been available since qBittorrent v5.2.0.

Jellyfin stores the qBittorrent username and password in **Torrent Tags**' settings file. Read access to this file is restricted on Unix systems.
