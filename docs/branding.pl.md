# Wygląd / branding launchera

*[English version](branding.md)*

![Domyślny motyw](assets/screenshot.pl.png)

## Rebrand w 3 krokach

1. **Kolory, nazwa, rozmiar** → edytuj
   [`Branding/Branding.axaml`](../src/Launcher.App/Branding/Branding.axaml).
2. **Tło** → podmień
   [`Assets/background.jpg`](../src/Launcher.App/Assets) (zachowaj nazwę pliku).
3. **Przebuduj** → `./build.sh publish <wersja>`.

Szczegóły niżej.

---

Wszystko, co decyduje o *wyglądzie* launchera, mieści się w dwóch miejscach.
Zmień je, przebuduj `./build.sh` i masz launcher zbrandowany pod Twój serwer.
Nic z tego nie dotyka logiki aktualizacji/uruchamiania — jest odizolowane w
`Launcher.App`.

```
src/Launcher.App/
├── Branding/Branding.axaml   ← nazwa, rozmiar, kolory (jedyny plik do edycji)
└── Assets/
    └── background.jpg         ← grafika tła (podmień plik)
```

## 1. Tekst, rozmiar i kolory — `Branding/Branding.axaml`

Otwórz [`src/Launcher.App/Branding/Branding.axaml`](../src/Launcher.App/Branding/Branding.axaml).
To płaska lista wartości z komentarzami:

| Klucz | Co kontroluje |
|-----|------------------|
| `BrandServerName` | Tytuł okna i duży nagłówek. Wpisz **WIELKIMI LITERAMI** dla stylizowanego wyglądu. |
| `BrandSubtitle` | Mały napis pod tytułem (np. `LAUNCHER`). |
| `WindowWidth` / `WindowHeight` | Rozmiar okna w pikselach logicznych (system skaluje wg DPI). Domyślnie `760 × 475`. |
| `BackgroundOffsetY` | Pionowe przesunięcie grafiki tła (px logiczne). Ujemne podnosi ją w górę — przydatne, by wypchnąć wyśrodkowany watermark nad dolny panel. `0` = wyśrodkowane. |
| `WindowCornerRadius` | Zaokrąglenie złotej ramki okna. `0` = kanciaste rogi. |
| `WindowBorderThickness` | Grubość złotej ramki wokół okna. |
| `BrandGoldColor` | Główny akcent — tytuł, pasek postępu, przycisk GRAJ, obwódki. |
| `BrandGoldDarkColor` | Koniec złotego gradientu na przycisku/pasku postępu. |
| `BrandGoldHighlightColor` | Kolor po najechaniu (hover). |
| `BrandTextColor` | Tekst statusu. |
| `BrandTextDimColor` | Podtytuł. |
| `BrandButtonTextColor` | Kolor napisu na złotym przycisku GRAJ (trzymaj ciemny dla kontrastu). |
| `BrandPanelColor` | Wypełnienie dolnego panelu. `#B3141414` = czerń przy 70 % krycia. |
| `BrandWindowColor` | Widoczny za grafiką tła (np. zanim się załaduje). |
| `BrandArtDimColor` | Ogólne przyciemnienie nałożone na grafikę tła, by nie była zbyt krzykliwa. `#40000000` ≈ 25 % czerni; podnieś pierwszą parę hex, by przyciemnić mocniej, `#00000000` = brak. |

Kolory to `#AARRGGBB` — pierwsze dwie cyfry hex to krycie (`FF` = pełne,
`00` = niewidoczne). Pędzle (brushes) niżej w pliku wywodzą się z tych kolorów;
zwykle nie trzeba ich ruszać.

### Zmiana akcentu na inny (nie złoty)

Zmień trzy kolory `BrandGold*`. Przykład — motyw czerwony:

```xml
<Color x:Key="BrandGoldColor">#FFC0392B</Color>
<Color x:Key="BrandGoldDarkColor">#FF8B0000</Color>
<Color x:Key="BrandGoldHighlightColor">#FFE74C3C</Color>
```

## 2. Grafika tła — `Assets/background.jpg`

Podmień [`src/Launcher.App/Assets/background.jpg`](../src/Launcher.App/Assets)
na własny obraz, **zachowując nazwę pliku**. Jest wbudowywany w binarkę podczas
budowania.

- Obraz rysowany jest `UniformToFill`: pokrywa całe okno i jest przycinany od
  środka, więc to, co ważne, powinno być blisko środka.
- Dopasuj proporcję okna (domyślnie `760 × 475` ≈ 16∶10), aby uniknąć mocnego
  przycięcia.
- Tytuł leży na ciemnym przyciemnieniu (scrim) u góry, więc gęsta grafika tam
  jest OK.

Aby użyć innej nazwy lub formatu pliku (np. `.png`), zmień `Source` elementu
`Image` w [`MainWindow.axaml`](../src/Launcher.App/MainWindow.axaml)
(`avares://Launcher.App/Assets/<twój-plik>`).

## 3. Czcionka (opcjonalnie)

Launcher używa dołączonej czcionki **Inter**, która renderuje się identycznie na
Windows i pod Wine. Aby ją zmienić, dodaj pakiet czcionki i ustaw ją jako
domyślną — zapytaj przed tym: czcionka ładowana w runtime to jedyna rzecz, która
potrafi sprawiać problemy pod Wine, więc wymaga weryfikacji na buildzie.

## Język (napisy UI)

Wszystkie etykiety przycisków i komunikaty statusu są w `Branding.axaml` jako
`x:String` z kluczami `Ui*` (domyślnie angielski). Przetłumacz je, by wydać
launcher w innym języku. Wstawki `{0}`, `{1}`… są wypełniane w runtime (liczby,
wersja, rozmiary) — zostaw je na miejscu. Przykład (polski):

```xml
<x:String x:Key="UiPlay">GRAJ</x:String>
<x:String x:Key="UiRetry">Ponów</x:String>
<x:String x:Key="UiDownloading">Pobieranie {0}/{1}  ({2} / {3})</x:String>
```

Całe UI renderuje się wtedy w jednym, spójnym języku; to konfiguracja przy
budowaniu, nie przełącznik w locie.

## Budowanie po zmianach

```sh
./build.sh publish <wersja>
```

Tworzy `out/launcher/` z binarkami Windows i Linux niosącymi Twój branding.
Następnie wydaj zgodnie z opisem w
[`releasing-updates.pl.md`](releasing-updates.pl.md).

## Podgląd bez budowania

`docs/mockups/` zawiera skrypty Python renderujące układ do PNG za pomocą Pillow
(uruchamiane w kontenerze, bez .NET). To przybliżenie prawdziwego okna Avalonii —
przydatne do porównywania kolorów/teł przed budową, ale nie zastępuje uruchomienia
launchera.
