# MuMain Launcher

*[English version](README.md)*

Wieloplatformowy launcher i auto-updater dla klienta gry
[MuMain](https://github.com/sven-n/MuMain). Synchronizuje pliki klienta gracza
z serwerem patchy przez HTTP(S), a następnie uruchamia klienta.

## Jak działa aktualizacja

1. Launcher pobiera **manifest** (`version.json`) z serwera patchy. Manifest
   zawiera listę wszystkich plików klienta z ich hashami SHA-256 i rozmiarami.
2. Porównuje każdy plik z lokalną kopią i pobiera tylko to, co nowe lub
   zmienione, weryfikując hash każdego pobrania.
3. Uruchamia klienta — bezpośrednio na Windows, przez Wine na Linuksie.

Launcher nigdy nie usuwa lokalnych plików; tylko dodaje i aktualizuje. Pliki
takie jak `config.ini`, logi i cache pozostają nietknięte.

## Projekty

| Projekt          | Rola                                                                      |
| ---------------- | ------------------------------------------------------------------------- |
| `PatchManifest`  | Narzędzie konsolowe: skanuje katalog wydania i zapisuje `version.json`.    |
| `Launcher.Core`  | Rdzeń bez UI: parsowanie manifestu, porównywanie, pobieranie, weryfikacja. |
| `Launcher.App`   | GUI Avalonia: okno postępu i uruchamianie klienta.                        |

## Budowanie

Build odbywa się w kontenerze .NET SDK, więc na hoście potrzebny jest tylko
Docker — bez instalowania .NET SDK lokalnie.

```sh
./build.sh                   # zbuduj całe rozwiązanie (Release)
./build.sh manifest ARGS…    # uruchom generator manifestu
./build.sh publish [WERSJA]  # self-contained launcher dla win-x64 + linux-x64 do ./out,
                             #   plus out/launcher/launcher.json (WERSJA domyślnie = dzisiejsza data)
./build.sh <argumenty dotnet> # przekazanie polecenia do dotnet w kontenerze
```

`publish` tworzy `out/launcher/` zawierający obie binarki launchera oraz
`launcher.json`. Wgraj ten katalog na serwer patchy: launcher czyta
`launcher.json`, by zaktualizować samego siebie przed aktualizacją klienta.

## Dokumentacja

- [Wydawanie aktualizacji](docs/releasing-updates.md) — workflow administratora:
  konfiguracja adresów patchy, wydanie aktualizacji klienta, wydanie nowego
  launchera oraz układ katalogów na serwerze. *(w języku angielskim)*
- [Format manifestu](docs/manifest-format.md) — referencja `version.json` i
  `launcher.json`. *(w języku angielskim)*
