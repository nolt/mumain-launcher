# Format manifestu

*[English version](manifest-format.md)*

Referencja dwóch plików JSON, które launcher czyta z hosta patchy.

## `version.json` — manifest klienta

Lista wszystkich plików klienta z hashem, który launcher ma dopasować.

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

Zasady:

- **`path`** — ścieżka względna klienta, zawsze z separatorami `/`. Launcher
  rozwiązuje ją w swoim folderze, a URL pobierania względem manifestu.
- **`hash`** — małe litery hex SHA-256 zawartości pliku. To jedyne, co decyduje,
  czy plik jest pobierany; rozmiar to tylko szybki wstępny test.
- **`baseUrl`** — gdzie leżą pliki względem manifestu. Pusty oznacza „obok
  `version.json`" (układ domyślny); `"files/"` umieściłby je w podfolderze.
- **`version`** / **`generatedAtUtc`** — informacyjne, pokazywane w UI i logach.
- **`files`** jest sortowane po ścieżce, więc niezmienione wydanie daje identyczny
  manifest.

Czego generator celowo nie umieszcza (aby launcher nigdy nie nadpisał własnych
plików gracza): `config.ini`, `*.log`, `imgui.ini` oraz sam manifest.
`config.ini.template` *jest* dołączany — dostarczany razem z klientem.

Launcher tylko dodaje i aktualizuje pliki z tej listy; nigdy nic nie usuwa.

## `launcher.json` — manifest launchera

Opisuje najnowszy build launchera, jedna binarka na identyfikator środowiska.

```json
{
  "version": "2026.06.10",
  "files": {
    "win-x64":   { "path": "Launcher.App.exe", "hash": "…", "size": 84283805 },
    "linux-x64": { "path": "Launcher.App",      "hash": "…", "size": 84378611 }
  }
}
```

Zasady:

- **`version`** — musi odpowiadać wersji wpisanej w opublikowane binarki
  (`build.sh publish [WERSJA]` trzyma je w zgodzie). Launcher aktualizuje siebie,
  ilekroć jego wersja różni się od tej.
- **`files`** — klucze to identyfikatory środowiska (`win-x64`, `linux-x64`). Każdy
  `path` jest rozwiązywany obok `launcher.json`. `hash` jest weryfikowany po
  pobraniu.
