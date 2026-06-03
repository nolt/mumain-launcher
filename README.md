# MuMain Launcher

Cross-platform launcher and auto-updater for the [MuMain](https://github.com/sven-n/MuMain)
game client. It keeps a player's client files in sync with a patch server over
HTTP(S), then starts the client.

## How updating works

1. The launcher downloads a **manifest** (`version.json`) from the patch server.
   The manifest lists every client file with its SHA-256 hash and size.
2. It compares each file against the local copy and downloads only what is new
   or changed, verifying the hash of every download.
3. It launches the client — directly on Windows, via Wine on Linux.

The launcher never deletes local files; it only adds and updates. Files such as
`config.ini`, logs and caches are therefore left untouched.

## Projects

| Project          | Role                                                                |
| ---------------- | ------------------------------------------------------------------- |
| `PatchManifest`  | Console tool: scans a release directory and writes `version.json`.  |
| `Launcher.Core`  | UI-free core: manifest parsing, diffing, downloading, verification. |
| `Launcher.App`   | Avalonia GUI: progress window and client launch.                    |

## Building

The build runs inside a .NET SDK container, so the host needs only Docker — no
local .NET SDK install.

```sh
./build.sh                 # build the whole solution (Release)
./build.sh manifest ARGS…  # run the manifest generator
./build.sh publish         # self-contained launcher for win-x64 + linux-x64 into ./out
./build.sh <dotnet args…>  # passthrough to dotnet inside the container
```
