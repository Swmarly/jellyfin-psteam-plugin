#!/usr/bin/env bash
set -euo pipefail

ROOT_DIR="$(cd "$(dirname "$0")/.." && pwd)"
PROJECT="$ROOT_DIR/src/Jellyfin.Plugin.PStream/Jellyfin.Plugin.PStream.csproj"
VERSION="${1:-1.0.0}"
PUBLISH_DIR="$ROOT_DIR/dist/publish"
ZIP_PATH="$ROOT_DIR/dist/jellyfin-plugin-pstream_${VERSION}.zip"
MANIFEST_PATH="$ROOT_DIR/manifest.json"
RAW_BASE_URL="https://raw.githubusercontent.com/Swmarly/jellyfin-psteam-plugin/main"

rm -rf "$PUBLISH_DIR"
mkdir -p "$PUBLISH_DIR"

# Build and publish the plugin
DOTNET_CLI_TELEMETRY_OPTOUT=1 dotnet publish "$PROJECT" -c Release -o "$PUBLISH_DIR"

# Create the distributable zip
rm -f "$ZIP_PATH"
(
  cd "$PUBLISH_DIR"
  zip -rq "$ZIP_PATH" .
)

# Compute checksum and update manifest
CHECKSUM=$(sha256sum "$ZIP_PATH" | awk '{print $1}')
python - <<PY
import json, pathlib
manifest_path = pathlib.Path("$MANIFEST_PATH")
data = json.loads(manifest_path.read_text())
entry = data[0]["versions"][0]
entry["version"] = "$VERSION"
entry["sourceUrl"] = f"{RAW_BASE_URL}/dist/jellyfin-plugin-pstream_{'$'}{VERSION}.zip"
entry["checksum"] = f"sha256:{'$'}{CHECKSUM}"
entry["timestamp"] = __import__("datetime").datetime.utcnow().replace(microsecond=0).isoformat() + "Z"
manifest_path.write_text(json.dumps(data, indent=2))
PY

echo "Built package at $ZIP_PATH"
echo "Updated manifest checksum to sha256:$CHECKSUM"
