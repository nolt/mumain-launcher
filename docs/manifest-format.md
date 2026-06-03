# Manifest format

Reference for the two JSON files the launcher reads from the patch host.

## `version.json` — client manifest

Lists every client file with the hash the launcher should match.

```json
{
  "version": "2026.06.10",
  "generatedAtUtc": "2026-06-03T21:57:05Z",
  "baseUrl": "",
  "files": [
    { "path": "Data/Player/player.bmd", "hash": "cd34…", "size": 2048 },
    { "path": "main.exe", "hash": "ab12…", "size": 8123456 }
  ]
}
```

Rules:

- **`path`** — client-relative path, always with `/` separators. The launcher
  resolves it under its own folder, and the download URL relative to the manifest.
- **`hash`** — lowercase hex SHA-256 of the file contents. This is the only thing
  that decides whether a file is downloaded; size is just a fast pre-check.
- **`baseUrl`** — where files live relative to the manifest. Empty means "next to
  `version.json`" (the default layout); `"files/"` would put them in a subfolder.
- **`version`** / **`generatedAtUtc`** — informational, shown in the UI and logs.
- **`files`** is sorted by path so an unchanged release produces an identical
  manifest.

What the generator deliberately leaves out (so the launcher never overwrites a
player's own files): `config.ini`, `*.log`, `imgui.ini`, and the manifest itself.
`config.ini.template` *is* included — it ships with the client.

The launcher only adds and updates files listed here; it never deletes anything.

## `launcher.json` — launcher manifest

Describes the latest launcher build, one binary per runtime identifier.

```json
{
  "version": "2026.06.10",
  "files": {
    "win-x64":   { "path": "Launcher.App.exe", "hash": "…", "size": 84283805 },
    "linux-x64": { "path": "Launcher.App",      "hash": "…", "size": 84378611 }
  }
}
```

Rules:

- **`version`** — must match the version stamped into the published binaries
  (`build.sh publish [VERSION]` keeps them in sync). The launcher updates itself
  whenever its own version differs from this.
- **`files`** keys are runtime identifiers (`win-x64`, `linux-x64`). Each `path`
  is resolved next to `launcher.json`. `hash` is verified after download.
