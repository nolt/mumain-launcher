#!/usr/bin/env bash
#
# Build and publish the launcher inside a .NET SDK container, so the host
# never needs the .NET SDK installed (same approach as the OpenMU docker build).
#
# Usage:
#   ./build.sh                   # build the whole solution (Release)
#   ./build.sh manifest ARGS…    # run the manifest generator with ARGS
#   ./build.sh publish [VERSION] # publish self-contained launcher for win-x64 + linux-x64
#                                #   (VERSION defaults to today's date) and write launcher.json
#   ./build.sh <dotnet args…>    # passthrough: run any dotnet command in the container
#
# Name the distributable launcher per server, either by editing LAUNCHER_NAME
# below or per run:  LAUNCHER_NAME=MyServer ./build.sh publish 1.0.0
#
set -euo pipefail

IMAGE="mcr.microsoft.com/dotnet/sdk:10.0"
ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
SOLUTION="MumainLauncher.slnx"
RIDS=(win-x64 linux-x64)
LAUNCHER_DIR="$ROOT/out/launcher"
# File name of the distributable launcher (no extension). Override via env, e.g.
#   LAUNCHER_NAME=MyServer ./build.sh publish
LAUNCHER_NAME="${LAUNCHER_NAME:-MumainLauncher}"

run() {
    docker run --rm -v "$ROOT":/src -w /src "$IMAGE" "$@"
}

# Writes out/launcher/launcher.json describing both published binaries (siblings of the manifest).
write_launcher_manifest() {
    local version="$1"
    local win_hash win_size lin_hash lin_size
    win_hash=$(sha256sum "$LAUNCHER_DIR/${LAUNCHER_NAME}.exe" | cut -d' ' -f1)
    win_size=$(stat -c%s "$LAUNCHER_DIR/${LAUNCHER_NAME}.exe")
    lin_hash=$(sha256sum "$LAUNCHER_DIR/${LAUNCHER_NAME}" | cut -d' ' -f1)
    lin_size=$(stat -c%s "$LAUNCHER_DIR/${LAUNCHER_NAME}")
    cat > "$LAUNCHER_DIR/launcher.json" <<EOF
{
  "version": "$version",
  "files": {
    "win-x64": { "path": "${LAUNCHER_NAME}.exe", "hash": "$win_hash", "size": $win_size },
    "linux-x64": { "path": "${LAUNCHER_NAME}", "hash": "$lin_hash", "size": $lin_size }
  }
}
EOF
}

cmd="${1:-build}"
case "$cmd" in
    build)
        run dotnet build "$SOLUTION" -c Release
        ;;
    manifest)
        shift
        run dotnet run --project src/PatchManifest -c Release -- "$@"
        ;;
    publish)
        version="${2:-$(date +%Y.%m.%d)}"
        rm -rf "$LAUNCHER_DIR"          # start clean so no stale-named binaries linger
        mkdir -p "$LAUNCHER_DIR"
        for rid in "${RIDS[@]}"; do
            echo "==> publishing $LAUNCHER_NAME $version for $rid"
            run dotnet publish src/Launcher.App -c Release -r "$rid" \
                --self-contained -p:PublishSingleFile=true -p:Version="$version" -o "out/$rid"
        done
        cp "$ROOT/out/win-x64/Launcher.App.exe" "$LAUNCHER_DIR/${LAUNCHER_NAME}.exe"
        cp "$ROOT/out/linux-x64/Launcher.App" "$LAUNCHER_DIR/${LAUNCHER_NAME}"
        write_launcher_manifest "$version"
        echo "==> out/launcher/: ${LAUNCHER_NAME}.exe, ${LAUNCHER_NAME}, launcher.json (version $version)"
        ;;
    *)
        run dotnet "$@"
        ;;
esac
