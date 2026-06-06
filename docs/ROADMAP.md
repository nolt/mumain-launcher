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

## Step 7: cosmetics / branding — done

Skinned to match the server website (gold `#d4af37` on dark, text `#e0e0e0`),
fully isolated to `Launcher.App`:

- **Central branding file** `Branding/Branding.axaml` — server name, subtitle,
  window size, and all colours as editable values. One file to rebrand.
- **Background art** `Assets/background.jpg` (currently the website's `mubg.jpg`),
  embedded as `AvaloniaResource`, drawn `UniformToFill`. Swap the file to rebrand.
- **Layout** — background image, dark scrim + gold title/subtitle on top, bottom
  panel (`rgba(20,20,20,.7)` with a gold top border) holding status, a gold
  progress bar, ghost **Ponów** and gold-gradient **GRAJ** buttons.
- **Theme** — Fluent Dark; PLAY/Retry/ProgressBar styled via `DynamicResource`.
- **Window** — fixed `760 × 475` (chosen to stay comfortable on FHD incl. 150 %
  scaling); `CanResize=False`. **Borderless** (`WindowDecorations="None"`) with a
  square gold frame (`WindowCornerRadius`/`WindowBorderThickness` in branding),
  custom gold minimise/close buttons, and drag-to-move from the title bar
  (`BeginMoveDrag` in `MainWindow.axaml.cs`). Square corners (rounded looked bad on
  the tester's WM) but `TransparencyLevelHint="Transparent"` is kept — the opaque
  root `Border` fills the window so it still looks solid, while the transparent
  surface stops the WM from drawing its own thin top border over our gold frame.
  Bottom panel is near-opaque (`#F2141414`) so the background watermark doesn't
  bleed through. Background art is lifted with `BackgroundOffsetY` to keep the
  centred watermark above the panel. The gold frame is a separate top-most
  `Border` (painted last, `IsHitTestVisible=False`) so no background image can
  ever cover it; `ClipToBounds` also keeps the offset art inside the window.
- Docs: `docs/branding.md` (how to rebrand). Mockup generators in `docs/mockups/`.

Decided against for now: runtime/remote-configurable branding (build-time only,
per the "someone building for themselves" requirement).

### Done since

- **Icon** — `Assets/icon.ico` (multi-size 16–256) wired to both the window/taskbar
  (`Icon` in `MainWindow.axaml`) and the Windows `.exe` (`<ApplicationIcon>`). One
  swappable file, documented in `branding.md`.
- **Linux/GNOME desktop integration** — GNOME shows the app icon from a `.desktop`
  file, not the window icon. The binary sets its X11 `WM_CLASS` to its own process
  name (Program.cs), and `build.sh publish` emits `out/launcher/icon.png` +
  `install-linux.sh`, which registers a matching `StartupWMClass` `.desktop` entry
  under `~/.local/share/`. Source PNG: `packaging/linux/icon.png` (replace alongside
  `icon.ico` when rebranding).
- **Logo title** — the text title can be swapped for an image via the
  `BrandTitleMode` branding resource (`Text` | `Logo` | `None`). `Logo` shows
  `Assets/logo.png` (`ShowLogo` in `MainWindowViewModel`, wired in
  `MainWindow.axaml`); replace it with your own transparent PNG. Documented in
  `branding.md`.

### Still optional / not done

- Custom embedded font (kept Inter — safest under Wine).
- Trimming to shrink the ~47 MB binary (needs a GUI test — Avalonia + trimming
  can break reflection-based XAML).

## Before a production release

1. Set real URLs in `src/Launcher.Core/LauncherConfig.cs` (`ManifestUrl`,
   `LauncherManifestUrl`) — currently `patch.example.com` placeholders.
2. `./build.sh publish <version>` → `out/launcher/`.
3. Upload client files + `version.json` (from `./build.sh manifest`) and
   `out/launcher/*` into one HTTPS directory.
4. Distribute `MumainLauncher` (run with `./`) to Linux, `MumainLauncher.exe` to Windows.

All builds run in the .NET SDK container via `./build.sh` (host needs only Docker).
