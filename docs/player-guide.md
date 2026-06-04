# Player guide

*[Wersja polska](player-guide.pl.md)*

How to install and run the launcher as a player. (For server admins building and
publishing it, see [Building](building.md) and [Releasing updates](releasing-updates.md).)

## Install

1. Download the launcher from the server's website:
   - **Windows:** `MumainLauncher.exe`
   - **Linux:** `MumainLauncher`
2. Put it **inside your client folder** (the folder with the game files). The
   launcher updates the files next to itself, so its location is the client.
3. Run it. It checks for updates, downloads only what changed, then enables
   **PLAY**.

You only download the launcher once — afterwards it updates itself automatically.

## Windows

Double-click `MumainLauncher.exe`. When the update finishes, click **PLAY**; the
client starts directly.

If Windows SmartScreen warns about an unrecognised app, choose *More info →
Run anyway* (the launcher isn't code-signed).

## Linux

The client runs through **Wine**, so install Wine first (e.g.
`sudo apt install wine`). Then run the launcher **natively** — not through Wine:

```sh
chmod +x MumainLauncher   # once, if needed
./MumainLauncher
```

Press **PLAY** when the update completes; the launcher starts the client through
Wine for you.

### Choosing a Wine prefix (optional)

By default the launcher uses your environment's `WINEPREFIX`:

```sh
WINEPREFIX=/home/me/.wine-mu ./MumainLauncher
```

To set it permanently (handy for a desktop shortcut), create `launcher.local.json`
next to the launcher:

```json
{ "winePrefix": "/home/me/.wine-mu", "wineCommand": "wine" }
```

Both fields are optional, and this file is yours — the updater never touches it.

## Notes

- The launcher **never deletes** your files and never overwrites your
  `config.ini`, logs or settings — it only adds and updates client files.
- If you see **"Could not download the patch manifest"**, see
  [Troubleshooting](troubleshooting.md) — on Linux the most common cause is
  accidentally running it through Wine instead of directly.
