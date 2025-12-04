This directory contains the packaged P-Stream plugin artifact and supporting release assets.

Binary artifacts are not checked in; run scripts/package.sh locally to generate a release zip and upload
it to a GitHub release (tagged as v<version>). After uploading, commit the manifest.json change that
references the release download URL and checksum produced by the script.
