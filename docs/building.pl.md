# Budowanie i wyciąganie launchera

*[English version](building.md)*

Wszystko buduje się w kontenerze .NET SDK w **Dockerze**, sterowane przez
`build.sh`. Na hoście potrzebny jest tylko Docker — bez .NET SDK, bez Avalonii,
bez Wine do samego budowania.

## Wymagania

- **Docker** zainstalowany i uruchomiony.
  - Linux: Docker Engine (`docker` w `$PATH`).
  - Windows/macOS: Docker Desktop. `build.sh` uruchamiaj z powłoki bash — Git Bash
    lub WSL na Windows.
- To wszystko. Pierwsze budowanie pobiera obraz `mcr.microsoft.com/dotnet/sdk:10.0`
  (jednorazowo, kilkaset MB) i cache'uje pakiety NuGet w obrębie uruchomienia.

`build.sh` montuje repozytorium do kontenera i tam uruchamia `dotnet`, więc wyniki
budowania pojawiają się w Twoim drzewie roboczym, jakby budowane lokalnie.

## Komendy `build.sh`

```sh
./build.sh                    # zbuduj całe rozwiązanie (Release) — szybki test poprawności
./build.sh publish [WERSJA]   # samodzielne binarki launchera → ./out (patrz niżej)
./build.sh manifest ARGI…     # generator manifestu klienta (patrz releasing-updates)
./build.sh <argumenty dotnet> # przekazanie, np. ./build.sh dotnet test
```

- `WERSJA` domyślnie = dzisiejsza data (`yyyy.MM.dd`). Jest wpisywana w binarki
  i do `launcher.json`, dzięki czemu samo-aktualizacja może porównywać wersje.
- Uruchamiaj z katalogu głównego repo (tam, gdzie `build.sh`).

## Co tworzy `publish`

`./build.sh publish 2026.06.10` zapisuje:

```
out/
├── win-x64/Launcher.App.exe       # surowy single-file publish (Windows)
├── linux-x64/Launcher.App         # surowy single-file publish (Linux)
└── launcher/
    ├── Launcher.App.exe           # ← launcher Windows do rozdania
    ├── Launcher.App               # ← launcher Linux do rozdania
    └── launcher.json              # ← manifest samo-aktualizacji (wersja + hashe)
```

**Używaj folderu `out/launcher/`** — ma obie gotowe binarki plus manifest
samo-aktualizacji, wszystko ze zgodną wersją/hashami.

Każda binarka jest **samodzielna** (~47 MB): środowisko .NET i biblioteki natywne
(Skia, HarfBuzz) są wbudowane i rozpakowywane przy pierwszym uruchomieniu. Gracze
nie muszą nic instalować — poza **Wine** na Linuksie, które uruchamia *klienta
gry*, a nie launcher.

## Wyciąganie i dystrybucja

| Plik | Kto uruchamia | Uwagi |
| ---- | ------------- | ----- |
| `Launcher.App.exe` | gracze Windows | Zmieniaj nazwę dowolnie, np. `MumainLauncher.exe`. |
| `Launcher.App` | gracze Linux | Zmieniaj nazwę dowolnie, np. `MumainLauncher`. Natywny ELF — **uruchamiaj bezpośrednio**, nie przez Wine (patrz [Rozwiązywanie problemów](troubleshooting.pl.md)). |
| `launcher.json` | serwer patchy | Wgraj obok binarek, aby launcher mógł się samo-aktualizować. |

Gracze umieszczają launcher **w folderze klienta** i stamtąd go uruchamiają;
launcher rozwiązuje pliki klienta względem własnej lokalizacji.

Launcher rozdaj raz ze swojej strony www. Potem aktualizuje się sam z
`launcher.json` na serwerze patchy. Układ serwera i przepływ aktualizacji/samo-
aktualizacji opisuje [Wydawanie aktualizacji](releasing-updates.pl.md).

## Test lokalny przed publikacją

Cały przepływ można przećwiczyć na jednej maszynie z tymczasowym serwerem HTTP:

```sh
# 1. Zbuduj launcher z ManifestUrl wskazującym na Twoją maszynę, np.
#    http://127.0.0.1:8000/version.json  (edytuj LauncherConfig.cs, potem publish)

# 2. W folderze z kopią klienta wygeneruj manifest:
./build.sh manifest --input /sciezka/do/kopii-klienta

# 3. Serwuj ten folder po HTTP:
cd /sciezka/do/kopii-klienta && python3 -m http.server 8000

# 4. Uruchom launcher z (pustego lub częściowego) folderu klienta i patrz, jak synchronizuje.
```

To dokładnie tak, jak launcher zachowa się wobec prawdziwego serwera patchy, tylko
po zwykłym HTTP na localhost.

## Uruchamianie testów

```sh
./build.sh dotnet test -c Release
```

Zestaw pokrywa `Launcher.Core` (budowanie URL-i, porównywanie, komenda
uruchomienia i operacje plikowe samo-aktualizacji) bez sieci i bez GUI.
