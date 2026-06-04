# Roadmap & status

Working notes so we can pick the project back up. The launcher is **functional and
tested end-to-end** (fresh install, update of an existing client, custom Wine
prefix, client launch). Local repo only — nothing pushed yet.

## Done

1. **Manifest contract** — `version.json` (SHA-256 per file, empty `baseUrl` = files
   sit next to the manifest, date version).
2. **Manifest generator** — `Tools`-style console app `PatchManifest`; excludes
   player files (`config.ini`, `*.log`, `imgui.ini`).
3. **Update engine** (`Launcher.Core`) — fetch manifest, diff with a size+mtime+hash
   cache, download changed files with verification + atomic replace, retry.
4. **Avalonia UI + client launch** — auto-update on open, progress, PLAY button,
   then launches the client (directly on Windows, via Wine on Linux); closes after.
5. **Self-update** — separate `launcher.json` (per-RID), silent rename+restart swap.
6. **Build integration + docs** — side-by-side server layout, `releasing-updates.md`,
   `manifest-format.md`.

Plus fixes from testing: native libraries embedded in the single file
(`IncludeNativeLibrariesForSelfExtract` + compression, ~47 MB), client executable
name `Main.exe` (case-sensitive), `wine Main.exe` by bare name, optional
`launcher.local.json` (Wine prefix/binary), and a docs note that on Linux the
native launcher must be run directly (`./MumainLauncher`), not through Wine.

## Pending — Step 7: cosmetics / branding (deferred)

Not started. When we return:

- Branding: logo, background image, window title/icon.
- Theme: dark/light, accent colours, button styling.
- Fonts.
- Window chrome: optional borderless window with custom title bar.
- Window size/proportions.
- Optional: enable trimming to shrink the ~47 MB binary (needs testing — Avalonia +
  trimming can break reflection-based XAML; verify the GUI still renders).

Decision recorded earlier: do function first, cosmetics as a separate round. UI is
fully separated from logic, so re-skinning is isolated to `Launcher.App` (XAML,
themes, assets) without touching `Launcher.Core`.

## Before a production release

1. Set real URLs in `src/Launcher.Core/LauncherConfig.cs` (`ManifestUrl`,
   `LauncherManifestUrl`) — currently `patch.example.com` placeholders.
2. `./build.sh publish <version>` → `out/launcher/`.
3. Upload client files + `version.json` (from `./build.sh manifest`) and
   `out/launcher/*` into one HTTPS directory.
4. Distribute `MumainLauncher` (run with `./`) to Linux, `MumainLauncher.exe` to Windows.

All builds run in the .NET SDK container via `./build.sh` (host needs only Docker).
