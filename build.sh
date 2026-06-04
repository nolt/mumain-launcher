#!/usr/bin/env bash
#
# Build and publish the launcher inside a .NET SDK container, so the host
# never needs the .NET SDK installed (same approach as the OpenMU docker build).
#
# Usage:
#   ./build.sh                   # build the whole solution (Release)
#   ./build.sh manifest ARGS...    # run the manifest generator with ARGS
#   ./build.sh publish [VERSION] # publish self-contained launcher for win-x64 + linux-x64
#                                #   (VERSION defaults to today's date) and write launcher.json
#   ./build.sh <dotnet args...>    # passthrough: run any dotnet command in the container
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

# Writes out/launcher/icon.png + install-linux.sh so Linux players can register
# the app with their desktop (GNOME needs a .desktop file to show the icon).
# APPID is the launcher's own name, which equals the X11 WM_CLASS the binary sets
# (Program.cs uses the process name), so StartupWMClass always matches.
write_linux_desktop_kit() {
    cp "$ROOT/packaging/linux/icon.png" "$LAUNCHER_DIR/icon.png"
    local script="$LAUNCHER_DIR/install-linux.sh"
    {
        printf '%s\n' '#!/usr/bin/env sh'
        printf '%s\n' '# Registers or removes the launcher icon + .desktop entry for the current user.'
        printf '%s\n' '# Usage:  ./install-linux.sh [--install|--uninstall]   (default: --install)'
        printf 'APPID="%s"\n' "$LAUNCHER_NAME"
        cat <<'INNER'
set -e
HERE=$(CDPATH= cd "$(dirname "$0")" && pwd)
BIN="$HERE/$APPID"
ICON_SRC="$HERE/icon.png"
DATA="${XDG_DATA_HOME:-$HOME/.local/share}"
ICON_DIR="$DATA/icons/hicolor/256x256/apps"
APP_DIR="$DATA/applications"
DESKTOP_FILE="$APP_DIR/$APPID.desktop"
ICON_FILE="$ICON_DIR/$APPID.png"

refresh() {
    update-desktop-database "$APP_DIR" 2>/dev/null || true
    gtk-update-icon-cache "$DATA/icons/hicolor" 2>/dev/null || true
}

install_it() {
    [ -f "$BIN" ] || { echo "Binary not found next to this script: $BIN"; exit 1; }
    [ -f "$ICON_SRC" ] || { echo "icon.png not found next to this script"; exit 1; }
    mkdir -p "$ICON_DIR" "$APP_DIR"
    cp "$ICON_SRC" "$ICON_FILE"
    chmod +x "$BIN"
    cat > "$DESKTOP_FILE" <<DESKTOP
[Desktop Entry]
Type=Application
Name=$APPID
Comment=MU client launcher / updater
Exec=$BIN
Icon=$APPID
StartupWMClass=$APPID
Terminal=false
Categories=Game;
DESKTOP
    refresh
    echo "Installed $APPID. If the icon doesn't appear immediately, log out and back in."
}

uninstall_it() {
    rm -f "$DESKTOP_FILE" "$ICON_FILE"
    refresh
    echo "Uninstalled $APPID (removed its .desktop entry and icon)."
}

case "${1:-}" in
    ""|--install) install_it ;;
    --uninstall)  uninstall_it ;;
    *) echo "Usage: $(basename "$0") [--install|--uninstall]"; exit 1 ;;
esac
INNER
    } > "$script"
    chmod +x "$script"
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
        write_linux_desktop_kit
        echo "==> out/launcher/: ${LAUNCHER_NAME}.exe, ${LAUNCHER_NAME}, launcher.json, icon.png, install-linux.sh (version $version)"
        ;;
    *)
        run dotnet "$@"
        ;;
esac
