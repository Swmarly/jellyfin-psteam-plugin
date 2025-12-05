#!/usr/bin/env bash
set -euo pipefail

ROOT_DIR="$(cd "$(dirname "$0")/.." && pwd)"
PROJECT="$ROOT_DIR/src/Jellyfin.Plugin.PStream/Jellyfin.Plugin.PStream.csproj"
VERSION="${1:-1.0.0}"
DIST_DIR="$ROOT_DIR/dist"
PUBLISH_DIR="$DIST_DIR/publish"
ZIP_NAME="jellyfin-plugin-pstream_${VERSION}.zip"
ZIP_PATH="$DIST_DIR/$ZIP_NAME"
MANIFEST_PATH="$ROOT_DIR/manifest.json"
RAW_BASE_URL="https://raw.githubusercontent.com/Swmarly/jellyfin-psteam-plugin/main/dist"
export PUBLISH_DIR ZIP_PATH RAW_BASE_URL ZIP_NAME MANIFEST_PATH

rm -rf "$PUBLISH_DIR"
mkdir -p "$PUBLISH_DIR"
mkdir -p "$DIST_DIR"

# Build and publish the plugin
DOTNET_CLI_TELEMETRY_OPTOUT=1 dotnet publish "$PROJECT" -c Release -o "$PUBLISH_DIR"

# Create the distributable zip
rm -f "$ZIP_PATH"
python3 - <<'PY'
import os
import pathlib
import shutil

publish = pathlib.Path(os.environ["PUBLISH_DIR"])
zip_path = pathlib.Path(os.environ["ZIP_PATH"])
if zip_path.exists():
    zip_path.unlink()
shutil.make_archive(zip_path.with_suffix(""), "zip", publish)
PY

# Compute checksum and update manifest
CHECKSUM=$(sha256sum "$ZIP_PATH" | awk '{print $1}')
export CHECKSUM VERSION
python3 - <<'PY'
import json
import os
import pathlib
from datetime import datetime

manifest_path = pathlib.Path(os.environ["MANIFEST_PATH"])
raw_base = os.environ["RAW_BASE_URL"].rstrip("/")
zip_name = os.environ["ZIP_NAME"]
version = os.environ["VERSION"]
checksum = os.environ["CHECKSUM"]

data = json.loads(manifest_path.read_text())
entry = data[0]["versions"][0]
entry["version"] = version
entry["sourceUrl"] = f"{raw_base}/{zip_name}"
entry["checksum"] = f"sha256:{checksum}"
entry["timestamp"] = datetime.utcnow().replace(microsecond=0).isoformat() + "Z"
manifest_path.write_text(json.dumps(data, indent=2))
PY

echo "Built package at $ZIP_PATH"
echo "Updated manifest checksum to sha256:$CHECKSUM"
