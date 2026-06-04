# Wydawanie aktualizacji

*[English version](releasing-updates.md)*

Jak administrator publikuje aktualizacje klienta i nowe wersje launchera. Wszystko
buduje się w kontenerze .NET SDK przez `./build.sh`, więc host potrzebuje tylko
Dockera.

## Konfiguracja jednorazowa

Launcher buduje się pod jeden konkretny serwer, więc adresy patchy są wbudowane.
Przed budową ustaw je w
[`src/Launcher.Core/LauncherConfig.cs`](../src/Launcher.Core/LauncherConfig.cs):

- `ManifestUrl` — pełny URL manifestu klienta, np. `https://patch.twojserwer.pl/version.json`
- `LauncherManifestUrl` — pełny URL manifestu launchera, np. `https://patch.twojserwer.pl/launcher.json`

Użyj domeny, którą kontrolujesz (nie surowego IP). Jeśli kiedyś przeniesiesz host
patchy, zmieniasz tylko DNS — wbudowany URL działa dalej.

## Układ serwera

Pliki klienta leżą obok manifestów w jednym katalogu webowym:

```
https://patch.twojserwer.pl/
├── version.json          ← manifest klienta
├── launcher.json         ← manifest launchera
├── Launcher.App.exe      ← binarka launchera (Windows)
├── Launcher.App          ← binarka launchera (Linux)
├── main.exe              ← pliki klienta…
└── Data/
```

Działa dowolny statyczny host (nginx, Apache, object storage). HTTPS zalecane.

## Wydanie aktualizacji klienta

1. Zbuduj klienta (w repo MuMain), by uzyskać jego katalog wydania.
2. Wygeneruj manifest nad tym katalogiem:

   ```sh
   ./build.sh manifest --input /sciezka/do/buildu/klienta
   ```

   To zapisze `version.json` w tym katalogu. Wersja domyślnie = dzisiejsza data;
   nadpisz przez `--version 2026.06.10`.
3. Wgraj **zawartość** katalogu klienta (łącznie z `version.json`) do katalogu
   webowego powyżej.

Launchery graczy porównują hash każdego pliku i pobierają tylko to, co się
zmieniło. Pliki specyficzne dla gracza (`config.ini`, logi) nigdy nie są na
liście, więc pozostają nietknięte. Launcher tylko dodaje i aktualizuje — nigdy nie
usuwa.

## Wydanie nowego launchera

1. Opublikuj obie binarki i manifest launchera:

   ```sh
   ./build.sh publish 2026.06.10
   ```

   To tworzy `out/launcher/` z `Launcher.App.exe`, `Launcher.App` oraz
   `launcher.json` (wersja wpisana w binarki).
2. Wgraj zawartość `out/launcher/` do katalogu webowego.

Przy następnym starcie każdy launcher porównuje swoją wersję z `launcher.json`;
jeśli się różni, pobiera odpowiednią binarkę, weryfikuje ją, podmienia siebie i
restartuje — przed aktualizacją klienta. Samo-aktualizacja jest best-effort: jeśli
się nie powiedzie, launcher działa dalej i nadal aktualizuje klienta.

Przy **przełomowej** zmianie launchera opublikuj też nowy launcher na stronie www,
aby gracze mogli pobrać go bezpośrednio.

## Doświadczenie gracza

Gracze pobierają launcher raz ze strony www i uruchamiają z folderu klienta.
Aktualizuje siebie, aktualizuje klienta, po czym odblokowuje **GRAJ**. Na Windows
klient startuje bezpośrednio; na Linuksie przez Wine. (Szczegóły:
[Przewodnik gracza](player-guide.pl.md).)

### Linux: uruchamiaj natywny launcher bezpośrednio

Na Linuksie używaj natywnej binarki `MumainLauncher` i uruchamiaj ją
**bezpośrednio**:

```sh
./MumainLauncher
```

**Nie** uruchamiaj przez Wine. `MumainLauncher` to natywny program Linux — `wine
MumainLauncher` go nie uruchomi, a build `MumainLauncher.exe` jest tylko dla
prawdziwego Windows. (Uruchomienie launchera pod Wine psuje też jego sieć, więc
aktualizacje się nie pobiorą.) Launcher sam uruchomi *klienta* przez Wine.

### Linux: wybór prefixu lub binarki Wine

Na Linuksie launcher startuje klienta przez `wine Main.exe`, rozwiązywane w
folderze klienta. Domyślnie używa `wine` i `WINEPREFIX` ze środowiska (więc
`WINEPREFIX=… ./MumainLauncher` po prostu działa — launcher biegnie natywnie i
przekazuje prefix do Wine).

Dla graczy uruchamiających z ikony na pulpicie lub chcących konkretnego buildu
Wine, połóż obok launchera plik `launcher.local.json`:

```json
{
  "winePrefix": "/home/user/.winetestowe",
  "wineCommand": "wine"
}
```

Oba pola są opcjonalne. Plik jest lokalny dla maszyny i nigdy nie jest pobierany
ani nadpisywany przez updater.
