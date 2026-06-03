#!/usr/bin/env bash
#
# Build and publish the launcher inside a .NET SDK container, so the host
# never needs the .NET SDK installed (same approach as the OpenMU docker build).
#
# Usage:
#   ./build.sh                 # build the whole solution (Release)
#   ./build.sh manifest ARGS…  # run the manifest generator with ARGS
#   ./build.sh publish         # publish self-contained launcher for win-x64 + linux-x64 into ./out
#   ./build.sh <dotnet args…>  # passthrough: run any dotnet command in the container
#
set -euo pipefail

IMAGE="mcr.microsoft.com/dotnet/sdk:10.0"
ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
SOLUTION="MumainLauncher.slnx"
RIDS=(win-x64 linux-x64)

run() {
    docker run --rm -v "$ROOT":/src -w /src "$IMAGE" "$@"
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
        for rid in "${RIDS[@]}"; do
            echo "==> publishing Launcher.App for $rid"
            run dotnet publish src/Launcher.App -c Release -r "$rid" \
                --self-contained -p:PublishSingleFile=true -o "out/$rid"
        done
        ;;
    *)
        run dotnet "$@"
        ;;
esac
