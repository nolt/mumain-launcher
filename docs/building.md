# Building & extracting the launcher

*[Wersja polska](building.pl.md)*

Everything builds inside a .NET SDK **Docker** container, driven by `build.sh`.
The host only needs Docker — no .NET SDK, no Avalonia, no Wine to build.

## Prerequisites

- **Docker** installed and running.
  - Linux: Docker Engine (`docker` in `$PATH`).
  - Windows/macOS: Docker Desktop. Run `build.sh` from a bash shell — Git Bash or
    WSL on Windows.
- That's it. The first build pulls the `mcr.microsoft.com/dotnet/sdk:10.0` image
  (a few hundred MB, once) and caches NuGet packages inside the container run.

`build.sh` mounts the repository into the container and runs `dotnet` there, so
build outputs appear in your working tree as if built locally.

## `build.sh` commands

```sh
./build.sh                    # build the whole solution (Release) — a quick sanity check
./build.sh publish [VERSION]  # self-contained launcher binaries → ./out (see below)
./build.sh manifest ARGS…     # run the client manifest generator (see releasing-updates.md)
./build.sh <dotnet args…>     # passthrough, e.g. ./build.sh dotnet test
```

- `VERSION` defaults to today's date (`yyyy.MM.dd`). It is stamped into the
  binaries and written into `launcher.json`, so self-update can compare versions.
- Run from the repository root (the folder containing `build.sh`).

## What `publish` produces

`./build.sh publish 2026.06.10` writes:

```
out/
├── win-x64/Launcher.App.exe       # raw single-file publish (internal name)
├── linux-x64/Launcher.App         # raw single-file publish (internal name)
└── launcher/
    ├── MumainLauncher.exe         # ← the Windows launcher to ship
    ├── MumainLauncher             # ← the Linux launcher to ship
    └── launcher.json              # ← self-update manifest (version + hashes)
```

**Use the `out/launcher/` folder** — it holds both ready binaries plus the
self-update manifest, all with matching name, version and hashes. It is wiped at
the start of every `publish`, so it never keeps stale binaries.

## Naming the launcher

The shipped file name comes from `LAUNCHER_NAME` (default `MumainLauncher`). Brand
it per server, either by editing the default near the top of `build.sh` or per run:

```sh
LAUNCHER_NAME=MyServer ./build.sh publish 1.0.0
# → out/launcher/MyServer.exe, MyServer, launcher.json (paths point to MyServer*)
```

The internal assembly name stays `Launcher.App` (invisible to players); only the
distributable file name and the `launcher.json` paths change, so embedded assets
keep working. Whatever name you choose, the `launcher.json` paths match it — keep
the uploaded files named the same so self-update finds them.

Each binary is **self-contained** (~47 MB): the .NET runtime and native libraries
(Skia, HarfBuzz) are embedded and unpacked on first run. Players need nothing
pre-installed — except **Wine** on Linux, which runs the *game client*, not the
launcher.

## Extracting & shipping

(File names below assume the default `MumainLauncher`; they follow `LAUNCHER_NAME`.)

| File | Who runs it | Notes |
| ---- | ----------- | ----- |
| `MumainLauncher.exe` | Windows players | Ready to ship as-is. |
| `MumainLauncher` | Linux players | Native ELF — **run it directly**, not through Wine (see [Troubleshooting](troubleshooting.md)). |
| `launcher.json` | the patch host | Upload next to the binaries so the launcher can self-update. |

Players put the launcher **inside the client folder** and run it from there; it
resolves the client files relative to its own location.

Distribute the launcher once from your website. After that it updates itself from
`launcher.json` on the patch host. See [Releasing updates](releasing-updates.md)
for the server layout and the update/self-update flow.

## Test it locally before publishing

You can exercise the whole flow on one machine with a throwaway HTTP server:

```sh
# 1. Build the launcher with ManifestUrl pointing at your machine, e.g.
#    http://127.0.0.1:8000/version.json  (edit LauncherConfig.cs, then publish)

# 2. In a folder that contains a copy of the client, generate the manifest:
./build.sh manifest --input /path/to/client-copy

# 3. Serve that folder over HTTP:
cd /path/to/client-copy && python3 -m http.server 8000

# 4. Run the launcher from an (empty or partial) client folder and watch it sync.
```

This is exactly how the launcher behaves against a real patch host, just over
plain HTTP on localhost.

## Running the tests

```sh
./build.sh dotnet test -c Release
```

The suite covers `Launcher.Core` (URL building, diffing, the launch command, and
the self-update file dance) without needing a network or a GUI.
