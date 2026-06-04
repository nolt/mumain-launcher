# Przewodnik gracza

*[English version](player-guide.md)*

Jak zainstalować i uruchomić launcher jako gracz. (Dla administratorów budujących
i publikujących: [Budowanie](building.pl.md) i [Wydawanie aktualizacji](releasing-updates.pl.md).)

## Instalacja

1. Pobierz launcher ze strony serwera:
   - **Windows:** `MumainLauncher.exe`
   - **Linux:** `MumainLauncher`
2. Umieść go **w folderze klienta** (tam, gdzie pliki gry). Launcher aktualizuje
   pliki obok siebie, więc jego lokalizacja to klient.
3. Uruchom. Sprawdzi aktualizacje, pobierze tylko to, co się zmieniło, a potem
   odblokuje **GRAJ**.

Launcher pobierasz tylko raz — później aktualizuje się sam.

## Windows

Kliknij dwukrotnie `MumainLauncher.exe`. Po zakończeniu aktualizacji kliknij
**GRAJ**; klient uruchomi się bezpośrednio.

Jeśli Windows SmartScreen ostrzega o nieznanej aplikacji, wybierz *Więcej
informacji → Uruchom mimo to* (launcher nie jest podpisany cyfrowo).

## Linux

Klient działa przez **Wine**, więc najpierw zainstaluj Wine (np.
`sudo apt install wine`). Następnie uruchom launcher **natywnie** — nie przez Wine:

```sh
chmod +x MumainLauncher   # raz, w razie potrzeby
./MumainLauncher
```

Kliknij **GRAJ** po zakończeniu aktualizacji; launcher sam uruchomi klienta przez
Wine.

### Wybór prefixu Wine (opcjonalnie)

Domyślnie launcher używa `WINEPREFIX` ze środowiska:

```sh
WINEPREFIX=/home/ja/.wine-mu ./MumainLauncher
```

Aby ustawić go na stałe (wygodne przy skrócie na pulpicie), utwórz obok launchera
plik `launcher.local.json`:

```json
{ "winePrefix": "/home/ja/.wine-mu", "wineCommand": "wine" }
```

Oba pola są opcjonalne, a ten plik jest Twój — updater go nie rusza.

## Uwagi

- Launcher **nigdy nie usuwa** Twoich plików i nie nadpisuje `config.ini`, logów
  ani ustawień — tylko dodaje i aktualizuje pliki klienta.
- Jeśli widzisz **„Could not download the patch manifest"**, zobacz
  [Rozwiązywanie problemów](troubleshooting.pl.md) — na Linuksie najczęstszą
  przyczyną jest przypadkowe uruchomienie przez Wine zamiast bezpośrednio.
