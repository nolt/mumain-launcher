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

## `version-linux.json` — manifest natywnego klienta Linux

Opcjonalny drugi manifest, w **tym samym formacie** co `version.json` i tworzony
tym samym generatorem (`build.sh manifest --input <kat> --output <kat>/version-linux.json`).
Wymienia **natywnego klienta Linux** — goły ELF `Main` i `MUnique.Client.Library.so`
— zamiast `Main.exe` i jego windowsowych DLL-i; wpisy `Data/` są poza tym takie same.

Te wpisy `Data/` mają identyczne ścieżki **i hashe** jak w `version.json`, więc gracz,
który ma już assety (np. z klienta Wine), przy przełączeniu pobiera tylko dwie natywne
binarki. Trzymaj wspólne pliki bit-w-bit identyczne między manifestami — jeśli plik
`Data/` się różni, będzie się pobierał przy każdym przełączeniu natywny↔Wine. Zobacz
[Wydawanie aktualizacji](releasing-updates.pl.md).

## `launcher.json` — manifest launchera

Opisuje najnowszy build launchera, jedna binarka na identyfikator środowiska.

```json
{
  "version": "2026.06.10",
  "files": {
    "win-x64":   { "path": "MumainLauncher.exe", "hash": "…", "size": 49655545 },
    "linux-x64": { "path": "MumainLauncher",      "hash": "…", "size": 49332195 }
  }
}
```

Zasady:

- **`version`** — musi odpowiadać wersji wpisanej w opublikowane binarki
  (`build.sh publish [WERSJA]` trzyma je w zgodzie). Launcher aktualizuje siebie,
  ilekroć jego wersja różni się od tej.
- **`files`** — klucze to identyfikatory środowiska (`win-x64`, `linux-x64`). Każdy
  `path` jest rozwiązywany obok `launcher.json` i zależy od `LAUNCHER_NAME`
  (domyślnie `MumainLauncher`); wgrane pliki muszą mieć pasującą nazwę. `hash` jest
  weryfikowany po pobraniu.
