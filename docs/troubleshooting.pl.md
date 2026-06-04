# Rozwiązywanie problemów

*[English version](troubleshooting.md)*

Realne problemy napotkane przy budowaniu i uruchamianiu launchera, z rozwiązaniem.

## Linux: uruchamiaj launcher bezpośrednio, nie przez Wine

`Launcher.App` (wersja linuksowa) to **natywny ELF**. Uruchamiaj go wprost:

```sh
./MumainLauncher
```

**Nie** uruchamiaj `wine MumainLauncher`. To nie jest program Windows, a sieć .NET
pod Wine nie działa (patrz niżej), więc aktualizacje się nie pobiorą. Launcher
samodzielnie uruchomi *klienta gry* przez Wine — to dzieje się automatycznie.

Wersja `.exe` jest tylko dla prawdziwego Windows.

## „Could not download the patch manifest"

Launcher doszedł do etapu aktualizacji, ale nie pobrał `version.json`. Typowe
przyczyny, od najczęstszej:

1. **Uruchomienie linuksowego launchera pod Wine.** Stos HTTP .NET zawodzi pod
   Wine z `GetAddrInfoExW … Unsupported`. Uruchom natywną binarkę wprost (wyżej).
2. **Zły lub nieosiągalny URL.** Sprawdź `ManifestUrl` wbudowany w build
   (`LauncherConfig.cs`) i czy serwer to serwuje — przetestuj
   `curl https://patch.twojserwer.pl/version.json`.
3. **Problemy z certyfikatem HTTPS.** Samopodpisany lub wygasły certyfikat psuje
   żądanie. Użyj poprawnego certyfikatu albo najpierw przetestuj po `http://`.
4. **Serwer nie działa / firewall.** Upewnij się, że host patchy działa i jest
   osiągalny z maszyny gracza (`ping`, `curl`).

## Linux: nazwa pliku klienta jest wrażliwa na wielkość liter

Systemy plików Linuksa rozróżniają wielkość liter; Windows nie. Plik wykonywalny
klienta to `Main.exe` (wielkie **M**). `LauncherConfig.ClientExecutableName` musi
dokładnie odpowiadać prawdziwemu plikowi, inaczej Wine zgłosi „nie znaleziono
pliku" po kliknięciu **GRAJ**.

## Wybór prefixu lub binarki Wine (Linux)

Domyślnie launcher uruchamia klienta przez `wine Main.exe`, używając `WINEPREFIX`
ze środowiska — więc `WINEPREFIX=/sciezka ./MumainLauncher` po prostu działa. Aby
ustalić prefix lub konkretny build Wine bez zmiennych środowiskowych, połóż obok
launchera plik `launcher.local.json`:

```json
{ "winePrefix": "/home/user/.wine-mu", "wineCommand": "wine" }
```

Oba pola są opcjonalne. Plik jest lokalny dla maszyny i nigdy nie jest pobierany
ani nadpisywany przez updater.

## Linux: ramka / rogi okna wyglądają źle

Okno jest bezramkowe z naszą złotą ramką. Zachowuje
`TransparencyLevelHint="Transparent"` nawet przy kanciastych rogach: na niektórych
menedżerach okien nieprzezroczyste okno bezramkowe dostaje cienką linię rysowaną
przez WM na górze, która zasłania naszą ramkę — przezroczysta powierzchnia temu
zapobiega. Zaokrąglone rogi porzucono, bo źle się renderują na części WM-ów. Jeśli
Twój WM i tak rysuje własną obwódkę, to jego dekoracja, poza kontrolą aplikacji
(na Windows tego nie będzie).

## „DllNotFoundException: libSkiaSharp" / brak bibliotek natywnych

Opublikowana binarka jest samodzielna: biblioteki natywne (Skia, HarfBuzz) są
wbudowane i rozpakowywane przy pierwszym uruchomieniu. Jeśli to widzisz, wysłałeś
tylko część wyniku albo zbudowałeś bez pakowania single-file. Wysyłaj binarkę z
`./build.sh publish` taką, jaka jest, z `out/launcher/`.

## Linux: „Permission denied" przy uruchamianiu binarki

Binarka skopiowana lub pobrana po HTTP traci bit wykonywalności. Przywróć go:

```sh
chmod +x MumainLauncher
```

(Samo-aktualizator sam ustawia bit wykonywalności na binarkach, które pobiera.)

## Jakiego .NET potrzebuje klient?

Launcher żadnego — jest samodzielny. Sam klient MuMain jest budowany pod .NET 10;
buduj i uruchamiaj go jak zwykle (na Linuksie przez Wine).
