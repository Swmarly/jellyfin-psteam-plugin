# Jellyfin P-Stream Plugin

This plugin adds p-stream as a searchable channel inside Jellyfin. Once installed, it surfaces a **P-Stream** tab where you can browse and search the catalog directly from the Jellyfin UI and play results through the native player.

## Features
- Search the p-stream catalog from Jellyfin's channel view
- Stream items directly through Jellyfin using the p-stream stream URLs
- Configurable API endpoint, optional API key, and page size

## Configuration
1. Install the plugin build from this repository into your Jellyfin instance.
2. In the Jellyfin dashboard, open **Plugins → P-Stream**.
3. Set the p-stream API base URL (for example, `https://pstream.local/api/`).
4. Provide an API key if your server requires one and adjust the page size if desired.

## Building
Run `dotnet build` in the repository root to produce the plugin assembly. The output DLL can be copied to Jellyfin's `plugins` directory.
