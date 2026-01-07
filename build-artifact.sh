#!/usr/bin/env bash
set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
IMAGE_NAME="jellyfin-plugin-anidb:artifact"
ARTIFACT_NAME="Jellyfin.Plugin.AniDB.dll"
OUT_DIR="${OUT_DIR:-$ROOT_DIR/artifacts}"
OUT_PATH="$OUT_DIR/$ARTIFACT_NAME"
GITIGNORE="$ROOT_DIR/.gitignore"
IGNORE_ENTRY="artifacts/$ARTIFACT_NAME"

docker build --target artifact -t "$IMAGE_NAME" "$ROOT_DIR"

mkdir -p "$OUT_DIR"
container_id="$(docker create "$IMAGE_NAME")"
cleanup() {
  docker rm -f "$container_id" >/dev/null
}
trap cleanup EXIT

docker cp "$container_id:/plugin/$ARTIFACT_NAME" "$OUT_PATH"

if ! grep -qxF "$IGNORE_ENTRY" "$GITIGNORE"; then
  printf '\n%s\n' "$IGNORE_ENTRY" >> "$GITIGNORE"
fi

echo "Wrote $OUT_PATH"
