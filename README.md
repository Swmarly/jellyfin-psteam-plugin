# Jellyfin P-Stream Plugin

This plugin adds p-stream as a searchable channel inside Jellyfin. Once installed, it surfaces a **P-Stream** tab where you can browse and search the catalog directly from the Jellyfin UI and play results through the native player.

## Features
- Search the p-stream catalog from Jellyfin's channel view
- Stream items directly through Jellyfin using the p-stream stream URLs
- Configurable API endpoint, optional API key, and page size

## Configuration
1. Install the plugin build from this repository into your Jellyfin instance.
2. In the Jellyfin dashboard, open **Plugins → P-Stream**.
3. Set the p-stream API base URL (defaults to `https://server.fifthwit.net`).
4. Provide an API key if your server requires one and adjust the page size if desired.

## Plugin repository

This repository now includes a Jellyfin plugin manifest (`manifest.json`) so it can be added directly as a custom repository in
the Jellyfin admin dashboard. Build and package the plugin, then point Jellyfin at the raw URL for `manifest.json`.

### Building a distributable package

1. Ensure the .NET SDK (8.0+) is available.
2. Run `./scripts/package.sh` to publish the plugin, zip the output to `dist/jellyfin-plugin-pstream_<version>.zip`, and update
   the manifest checksum automatically.
3. Commit and push the updated zip + manifest so Jellyfin can download the plugin when installing from the repository.

## Building
Run `dotnet build` in the repository root to produce the plugin assembly. The output DLL can be copied to Jellyfin's `plugins` directory.
