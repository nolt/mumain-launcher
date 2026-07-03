# Wydawanie aktualizacji

*[English version](releasing-updates.md)*

Jak administrator publikuje aktualizacje klienta i nowe wersje launchera. Wszystko
buduje się w kontenerze .NET SDK przez `./build.sh`, więc host potrzebuje tylko
Dockera.

## Konfiguracja jednorazowa

Launcher buduje się pod jeden konkretny serwer, więc adresy patchy są wbudowane.
Przed budową ustaw je w
[`src/Launcher.Core/LauncherConfig.cs`](../src/Launcher.Core/LauncherConfig.cs):

- `ManifestUrl` — pełny URL manifestu klienta Windows, np. `https://patch.twojserwer.pl/version.json`
- `LinuxManifestUrl` — pełny URL manifestu natywnego klienta Linux, np. `https://patch.twojserwer.pl/version-linux.json`
- `LauncherManifestUrl` — pełny URL manifestu launchera, np. `https://patch.twojserwer.pl/launcher.json`

Użyj domeny, którą kontrolujesz (nie surowego IP). Jeśli kiedyś przeniesiesz host
patchy, zmieniasz tylko DNS — wbudowany URL działa dalej.

### Klient Windows i natywny Linux

Sam launcher działa natywnie na Windows i na Linux. *Klient gry*, który pobiera,
może być buildem Windows (uruchamianym wprost na Windows albo przez Wine na
Linuksie) albo natywnym buildem Linux (`Main` + `.so`, uruchamianym wprost). Przy
pierwszym starcie gracz na Linuksie jest raz pytany, którego chce; odpowiedź trafia
do `launcher.local.json` obok launchera i można ją zmienić później z launchera
(przycisk *Wersja klienta…* na dole po lewej). Gracze Windows nie są pytani —
zawsze dostają klienta Windows.

Oba klienty współdzielą assety `Data/` (te same ścieżki i hashe), więc publikacja
obu kosztuje assety tylko raz — na dysku i w transferze: gracz przełączający się
między native a Wine dociąga tylko różniące się binarki.

## Układ serwera

Pliki klienta leżą obok manifestów w jednym katalogu webowym:

```
https://patch.twojserwer.pl/
├── version.json          ← manifest klienta Windows
├── version-linux.json    ← manifest natywnego klienta Linux (opcjonalnie; dodaj gdy wydajesz)
├── launcher.json         ← manifest launchera
├── MumainLauncher.exe    ← binarka launchera (Windows; nazwa = LAUNCHER_NAME)
├── MumainLauncher        ← binarka launchera (Linux; nazwa = LAUNCHER_NAME)
├── Main.exe              ← binarki klienta Windows…
├── Main                  ← binarka natywnego klienta Linux (ELF)
├── MUnique.Client.Library.so
└── Data/                 ← assety, współdzielone przez oba klienty
```

Działa dowolny statyczny host (nginx, Apache, object storage). HTTPS zalecane. Oba
manifesty leżą w tym samym katalogu i wskazują te same pliki `Data/`, więc
współdzielone assety wgrywasz raz.

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

### Wydanie także natywnego klienta Linux

Zbuduj natywnego klienta Linux (w repo MuMain), tak by mieć katalog z `Main`,
`MUnique.Client.Library.so` i tym samym `Data/`. Wygeneruj jego manifest pod nazwą,
której oczekuje launcher, i wgraj binarki obok windowsowych:

```sh
./build.sh manifest --input /sciezka/do/klienta/linux --output /sciezka/do/klienta/linux/version-linux.json
```

Ponieważ wpisy `Data/` mają hashe zgodne z `version.json`, wystarczy wgrać dwie
natywne binarki (`Main`, `*.so`) oraz `version-linux.json`; assety już tam są z
wydania Windows. `Main` serwowany jest jako zwykły plik — launcher nadaje mu bit
wykonywalny po pobraniu.

## Wydanie nowego launchera

1. Opublikuj obie binarki i manifest launchera:

   ```sh
   ./build.sh publish 2026.06.10
   ```

   To tworzy `out/launcher/` z `MumainLauncher.exe`, `MumainLauncher` oraz
   `launcher.json` (nazwa binarki zależy od `LAUNCHER_NAME`, domyślnie
   `MumainLauncher`; wersja wpisana w binarki). Nadpisz per serwer:
   `LAUNCHER_NAME=MojSerwer ./build.sh publish 2026.06.10`.
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
klient startuje bezpośrednio. Na Linuksie gracz wybiera raz (native lub Wine):
klient natywny startuje bezpośrednio, klient Windows przez Wine. (Szczegóły:
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
aktualizacje się nie pobiorą.) Przy pierwszym starcie launcher pyta, czy
uruchomić natywnego klienta Linux, czy klienta Windows przez Wine, i przygotowuje
wybrany wariant.

### Linux: wybór prefixu lub binarki Wine

Gdy gracz uruchamia **klienta Windows przez Wine**, launcher startuje go przez
`wine Main.exe`, rozwiązywane w folderze klienta. Domyślnie używa `wine` i
`WINEPREFIX` ze środowiska (więc `WINEPREFIX=… ./MumainLauncher` po prostu działa —
launcher biegnie natywnie i przekazuje prefix do Wine). Klient **natywny** to
wszystko ignoruje — uruchamia `./Main` bezpośrednio.

Wybór typu klienta zapisuje się tu również (`"mode": "native"` lub `"wine"`);
zapisywany jest, gdy gracz wybiera w launcherze, więc zwykle nie edytujesz go
ręcznie. Dla graczy uruchamiających z ikony na pulpicie lub chcących konkretnego
buildu Wine, połóż obok launchera plik `launcher.local.json`:

```json
{
  "mode": "wine",
  "winePrefix": "/home/user/.winetestowe",
  "wineCommand": "wine"
}
```

Każde pole jest opcjonalne. Wstępne ustawienie `mode` pomija pytanie przy
pierwszym starcie (przydatne w zarządzanej instalacji). Plik jest lokalny dla
maszyny i nigdy nie jest pobierany ani nadpisywany przez updater.
