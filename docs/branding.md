# Branding the launcher

*[Wersja polska](branding.pl.md)*

![Default theme](assets/screenshot.png)

## Rebrand in 3 steps

1. **Colours, name, size** → edit
   [`Branding/Branding.axaml`](../src/Launcher.App/Branding/Branding.axaml).
2. **Background** → replace
   [`Assets/background.jpg`](../src/Launcher.App/Assets) (keep the file name).
3. **Rebuild** → `./build.sh publish <version>`.

Details below.

---

Everything that controls how the launcher *looks* lives in two places. Change
them, rebuild with `./build.sh`, and you have a launcher skinned for your server.
None of this touches the update/launch logic — it is isolated to `Launcher.App`.

```
src/Launcher.App/
├── Branding/Branding.axaml   ← name, size, colours (the one file to edit)
└── Assets/
    └── background.jpg         ← background art (replace the file)
```

## 1. Text, size and colours — `Branding/Branding.axaml`

Open [`src/Launcher.App/Branding/Branding.axaml`](../src/Launcher.App/Branding/Branding.axaml).
It is a flat list of values with comments:

| Key | What it controls |
|-----|------------------|
| `BrandServerName` | Window title and the big heading. Type it **UPPERCASE** for the styled look. |
| `BrandSubtitle` | Small line under the title (e.g. `LAUNCHER`). |
| `WindowWidth` / `WindowHeight` | Window size in logical pixels (the OS scales for DPI). Default `760 × 475`. |
| `BackgroundOffsetY` | Vertical nudge of the background art (logical px). Negative lifts it up — handy to push a centred watermark above the bottom panel. `0` = centred. |
| `WindowCornerRadius` | Rounding of the window's gold frame. `0` = square corners. |
| `WindowBorderThickness` | Width of the gold frame around the window. |
| `BrandGoldColor` | Primary accent — title, progress bar, PLAY button, outlines. |
| `BrandGoldDarkColor` | End of the gold gradient on the button/progress bar. |
| `BrandGoldHighlightColor` | Hover colour. |
| `BrandTextColor` | Status text. |
| `BrandTextDimColor` | Subtitle. |
| `BrandButtonTextColor` | Label colour on the gold PLAY button (keep it dark for contrast). |
| `BrandPanelColor` | Bottom panel fill. `#B3141414` = black at 70 % opacity. |
| `BrandWindowColor` | Shown behind the background image (e.g. while it loads). |
| `BrandArtDimColor` | Overall dim laid over the background art so it isn't too loud. `#40000000` ≈ 25 % black; raise the first hex pair to dim more, `#00000000` for none. |

Colours are `#AARRGGBB` — the first two hex digits are opacity (`FF` = solid,
`00` = invisible). The brushes lower in the file are derived from these colours;
you normally don't need to touch them.

### Switch to a different accent (not gold)

Change the three `BrandGold*` colours. Example — a red theme:

```xml
<Color x:Key="BrandGoldColor">#FFC0392B</Color>
<Color x:Key="BrandGoldDarkColor">#FF8B0000</Color>
<Color x:Key="BrandGoldHighlightColor">#FFE74C3C</Color>
```

## 2. Background art — `Assets/background.jpg`

Replace [`src/Launcher.App/Assets/background.jpg`](../src/Launcher.App/Assets)
with your own image, **keeping the file name**. It is embedded into the binary
at build time.

- The image is drawn `UniformToFill`: it covers the whole window and is centre-
  cropped, so anything important should sit near the middle.
- Match the window aspect (default `760 × 475` ≈ 16∶10) to avoid heavy cropping.
- The title sits over a dark scrim at the top, so busy artwork there is fine.

To use a different file name or format (e.g. `.png`), update the `Image`
`Source` in [`MainWindow.axaml`](../src/Launcher.App/MainWindow.axaml)
(`avares://Launcher.App/Assets/<your-file>`).

## 3. Font (optional)

The launcher uses the bundled **Inter** font, which renders identically on
Windows and under Wine. To change it, add the font package and set it as the
default — ask before doing this: a font loaded at runtime is the one thing that
can misbehave under Wine, so it needs a build to verify.

## Language (UI text)

All button labels and status messages live in `Branding.axaml` as `Ui*` strings
(default English). Translate them to ship the launcher in another language. The
`{0}`, `{1}`… placeholders are filled at runtime (counts, version, sizes) — keep
them in place. Example (Polish):

```xml
<x:String x:Key="UiPlay">GRAJ</x:String>
<x:String x:Key="UiRetry">Ponów</x:String>
<x:String x:Key="UiDownloading">Pobieranie {0}/{1}  ({2} / {3})</x:String>
```

The whole UI then renders in one consistent language; this is build-time, not a
runtime switch.

## Build after changing

```sh
./build.sh publish <version>
```

Produces `out/launcher/` with the Windows and Linux binaries carrying your
branding. Then release as described in
[`releasing-updates.md`](releasing-updates.md).

## Previewing without a build

`docs/mockups/` holds Python scripts that render the layout to a PNG using
Pillow (run in a container, no .NET needed). They are an approximation of the
real Avalonia window — handy for comparing colours/backgrounds before a build,
not a substitute for running the launcher.
