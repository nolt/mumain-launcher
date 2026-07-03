# Releasing updates

*[Wersja polska](releasing-updates.pl.md)*

How a server admin publishes client updates and new launcher versions. All builds
run in the .NET SDK container via `./build.sh`, so the host only needs Docker.

## One-time configuration

The launcher is built for one specific server, so the patch URLs are baked in.
Before building, set them in
[`src/Launcher.Core/LauncherConfig.cs`](../src/Launcher.Core/LauncherConfig.cs):

- `ManifestUrl` — full URL of the Windows client manifest, e.g. `https://patch.yourserver.pl/version.json`
- `LinuxManifestUrl` — full URL of the native Linux client manifest, e.g. `https://patch.yourserver.pl/version-linux.json`
- `LauncherManifestUrl` — full URL of the launcher manifest, e.g. `https://patch.yourserver.pl/launcher.json`

Use a domain you control (not a raw IP). If you ever move the patch host, you
only change DNS — the baked URL keeps working.

### Windows and native Linux clients

The launcher itself runs natively on both Windows and Linux. The *game client* it
downloads can be either the Windows build (run directly on Windows, or through Wine
on Linux) or the native Linux build (`Main` + `.so`, run directly). On first launch a
Linux player is asked once which one they want; the answer is stored in
`launcher.local.json` next to the launcher and can be changed later from the
launcher (the *Client version…* button, bottom-left). Windows players are never
asked — they always get the Windows client.

The two clients share their `Data/` assets (same paths and hashes), so publishing
both costs the assets only once on disk and on the wire: a player switching between
native and Wine re-downloads just the differing binaries.

## Server layout

Client files sit next to the manifests in one web directory:

```
https://patch.yourserver.pl/
├── version.json          ← Windows client manifest
├── version-linux.json    ← native Linux client manifest (optional; add when you ship it)
├── launcher.json         ← launcher manifest
├── MumainLauncher.exe    ← launcher binary (Windows; name = LAUNCHER_NAME)
├── MumainLauncher        ← launcher binary (Linux; name = LAUNCHER_NAME)
├── Main.exe              ← Windows client binaries…
├── Main                  ← native Linux client binary (ELF)
├── MUnique.Client.Library.so
└── Data/                 ← assets, shared by both clients
```

Any static host works (nginx, Apache, object storage). HTTPS is recommended. Both
manifests live in the same directory and reference the same `Data/` files, so you
upload the shared assets once.

## Releasing a client update

1. Build the client (in the MuMain repo) to produce its release directory.
2. Generate the manifest over that directory:

   ```sh
   ./build.sh manifest --input /path/to/client/build
   ```

   This writes `version.json` into that directory. The version defaults to
   today's date; override with `--version 2026.06.10`.
3. Upload the **contents** of the client directory (including `version.json`) to
   the web directory above.

Players' launchers compare each file's hash and download only what changed.
Player-specific files (`config.ini`, logs) are never listed, so they are left
untouched. The launcher only adds and updates — it never deletes.

### Also releasing the native Linux client

Build the native Linux client (in the MuMain repo) so you have a directory with
`Main`, `MUnique.Client.Library.so` and the same `Data/`. Generate its manifest
under the name the launcher expects, then upload the binaries alongside the
Windows ones:

```sh
./build.sh manifest --input /path/to/linux/client --output /path/to/linux/client/version-linux.json
```

Because the `Data/` entries hash-match `version.json`, you only need to upload the
two native binaries (`Main`, `*.so`) and `version-linux.json`; the assets are
already there from the Windows release. `Main` is served as a normal file — the
launcher sets its executable bit after downloading.

## Releasing a new launcher

1. Publish both binaries and the launcher manifest:

   ```sh
   ./build.sh publish 2026.06.10
   ```

   This produces `out/launcher/` with `MumainLauncher.exe`, `MumainLauncher` and
   `launcher.json` (the binary name follows `LAUNCHER_NAME`, default
   `MumainLauncher`; version stamped into the binaries). Override per server:
   `LAUNCHER_NAME=MyServer ./build.sh publish 2026.06.10`.
2. Upload the contents of `out/launcher/` to the web directory.

On next start, each launcher compares its own version to `launcher.json`; if it
differs, it downloads the matching binary, verifies it, swaps itself out and
restarts — before updating the client. Self-update is best-effort: if it can't
complete, the launcher keeps running and still updates the client.

For a **breaking** launcher change, also publish the new launcher on your
website so players can re-download it directly.

## Player experience

Players download the launcher once from your website and run it from the client
folder. It updates itself, updates the client, then enables **PLAY**. On Windows
the client starts directly. On Linux the player picks once (native or Wine): the
native client starts directly, the Windows client starts through Wine.

### Linux: run the native launcher directly

On Linux, use the native launcher binary `MumainLauncher` and run it **directly**:

```sh
./MumainLauncher
```

Do **not** run it through Wine. `MumainLauncher` is a native Linux program — `wine
MumainLauncher` cannot run it, and the `MumainLauncher.exe` build is only for real
Windows. (Running the launcher under Wine also breaks its networking, so updates
fail to download.) On first launch it asks whether to run the native Linux client
or the Windows client through Wine, then provisions the one you chose.

### Linux: choosing a Wine prefix or binary

When the player runs the **Windows client through Wine**, the launcher starts it
with `wine Main.exe`, resolved in the client folder. By default it uses `wine` and
whatever `WINEPREFIX` is set in the environment (so `WINEPREFIX=… ./MumainLauncher`
just works — the launcher runs natively and passes the prefix through to Wine). The
**native** client ignores all of this — it runs `./Main` directly.

The client-type choice is stored here too (`"mode": "native"` or `"wine"`); it is
written when the player picks in the launcher, so you don't normally edit it by
hand. For players who launch from a desktop icon, or who want a specific Wine build,
drop a `launcher.local.json` next to the launcher:

```json
{
  "mode": "wine",
  "winePrefix": "/home/user/.winetestowe",
  "wineCommand": "wine"
}
```

Every field is optional. Preseeding `mode` skips the first-run prompt (handy for a
managed install). This file is per-machine and is never downloaded or overwritten
by the updater.
