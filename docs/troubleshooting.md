# Troubleshooting

*[Wersja polska](troubleshooting.pl.md)*

Real issues hit while building and running the launcher, with the fix for each.

## Linux: run the launcher directly, not through Wine

The Linux launcher (`MumainLauncher`, or whatever `LAUNCHER_NAME` you chose) is a
**native ELF**. Run it directly:

```sh
./MumainLauncher
```

Do **not** run `wine MumainLauncher`. It's not a Windows program, and running .NET
networking under Wine fails (see below), so updates won't download. The launcher
then starts the *game client* through Wine for you — that part is automatic.

The `.exe` build is only for real Windows.

## "Could not download the patch manifest"

The launcher reached the update step but couldn't fetch `version.json`. Usual
causes, most common first:

1. **Running the Linux launcher under Wine.** .NET's HTTP stack fails under Wine
   with `GetAddrInfoExW … Unsupported`. Run the native binary directly (above).
2. **Wrong or unreachable URL.** Check `ManifestUrl` baked into the build
   (`LauncherConfig.cs`) and that the host serves it — test with
   `curl https://patch.yourserver.pl/version.json`.
3. **HTTPS certificate problems.** A self-signed or expired cert makes the request
   fail. Use a valid certificate, or test over plain `http://` first.
4. **Server not running / firewall.** Confirm the patch host is up and reachable
   from the player's machine (`ping`, `curl`).

## Linux: client executable name is case-sensitive

Linux filesystems are case-sensitive; Windows isn't. The client executable is
`Main.exe` (capital **M**). `LauncherConfig.WindowsClientExecutableName` must match
the real file exactly, or Wine will report "file not found" when you press **PLAY**.

## Linux: the native client won't start

On Linux the launcher offers two clients — the **native** build (`Main` + `.so`, run
directly) or the **Windows** client through Wine — chosen on first run and changeable
with the **Client version…** button (bottom-left). The choice is stored in
`launcher.local.json` (`"mode": "native"` or `"wine"`).

If the native client fails to start, it's almost always a missing runtime library.
Install its dependencies:

```sh
sudo apt install libturbojpeg0 libglu1-mesa libglvnd0
```

Still no luck? Switch to **Windows client via Wine** with the **Client version…**
button — it runs a different client that many systems start more readily. The
downloaded native `Main` is marked executable automatically after the update, so a
missing execute bit is not the cause here.

## Choosing a Wine prefix or binary (Linux)

By default the launcher starts the client with `wine Main.exe`, using whatever
`WINEPREFIX` is in the environment — so `WINEPREFIX=/path ./MumainLauncher` just
works. To pin a prefix or a specific Wine build without environment variables,
drop a `launcher.local.json` next to the launcher:

```json
{ "winePrefix": "/home/user/.wine-mu", "wineCommand": "wine" }
```

Both fields are optional. The file is per-machine and never downloaded or
overwritten by the updater. They apply to the **Windows client via Wine** only — the
native Linux client ignores them and runs `./Main` directly. This same file also
stores the client choice (`"mode": "native"` or `"wine"`).

## Linux: the window border / corners look off

The window is borderless with our own gold frame. It keeps
`TransparencyLevelHint="Transparent"` even with square corners: on some window
managers an opaque borderless window gets a thin WM-drawn line on top that hides
our frame, and the transparent surface prevents that. Rounded corners were
dropped because they render badly on some WMs. If your WM still draws its own
border, that's the WM's decoration and is outside the app's control (it won't
appear on Windows).

## "DllNotFoundException: libSkiaSharp" / missing native libraries

The published binary is self-contained: native libraries (Skia, HarfBuzz) are
embedded and unpacked on first run. If you ever see this, you shipped only part
of the output or rebuilt without single-file packaging. Ship the binary produced
by `./build.sh publish` as-is, from `out/launcher/`.

## Linux: "Permission denied" launching the binary

A binary copied or downloaded over HTTP loses its execute bit. Restore it:

```sh
chmod +x MumainLauncher
```

(The self-updater already re-sets the execute bit on binaries it downloads.)

## Which .NET does the client need?

None for the launcher — it's self-contained. The MuMain client itself is built
for .NET 10; build and run it as you normally do (on Linux, through Wine).
