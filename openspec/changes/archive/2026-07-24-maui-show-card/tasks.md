## 1. Dipendenze e rendering barcode

- [x] 1.1 Aggiungere i pacchetti NuGet `ZXing.Net` e `ZXing.Net.Bindings.SkiaSharp`
- [x] 1.2 Aggiungere a `BarcodeFormatCatalog` la mappatura `BarcodeFormat` (stringa) → `ZXing.BarcodeFormat`
- [x] 1.3 Creare `IBarcodeRenderer` + implementazione: genera `SKBitmap` nero-su-bianco (dimensioni per 1D/2D), codifica PNG → `ImageSource`; esito "non generabile" su eccezione (nessun crash)
- [x] 1.4 Registrare `IBarcodeRenderer` in DI
- [x] 1.5 `dotnet build`: compila senza errori con le nuove dipendenze (verifica compat SkiaSharp su Android) — *SkiaSharp 3 compila e rende a runtime su Android*

## 2. Piattaforma Android: luminosità e filtro luce blu

- [x] 2.1 `IScreenBrightnessController` + impl Android: set max (`ScreenBrightness = 1f`) e ripristino default (`-1f`); registrare in DI
- [x] 2.2 `IReadingFilterProbe` + impl Android: leggere `night_display_activated` (best-effort, esito attivo/non attivo/sconosciuto); registrare in DI

## 3. Pagina di visualizzazione

- [x] 3.1 Creare `ShowCardPage` + `ShowCardViewModel`; rotta `ShowCardPage` con parametro `id`; caricare la carta con `GetByIdAsync` (null → torna alla lista)
- [x] 3.2 Layout: barcode grande e centrato su sfondo bianco (anche in dark), codice in chiaro sempre visibile sotto, nome/emittente in testa
- [x] 3.3 Caso barcode non generabile: nascondere l'immagine, mostrare codice in chiaro + messaggio ("mostra il numero al cassiere")
- [x] 3.4 In `OnAppearing`: `KeepScreenOn = true`, luminosità al massimo, controllo filtro luce blu → avviso non bloccante se attivo
- [x] 3.5 In `OnDisappearing`: `KeepScreenOn = false`, luminosità a default di sistema
- [x] 3.6 Aggancio navigazione dal tile in `CardListPage` (tap → `ShowCardPage?id=<Id>`)
- [x] 3.7 Registrare pagina e ViewModel in DI

## 4. Verifica

- [x] 4.1 `dotnet build`: compila senza errori (criterio di accettazione)
- [x] 4.2 Verifica runtime su emulatore: tap su una carta → pagina con barcode reso (1D) + codice in chiaro; sfondo bianco; ritorno alla lista (screenshot) — *verificato: carta "Coop" → barcode UPC-A reso da ZXing+SkiaSharp, codice 225001353133 in chiaro, sfondo bianco*
- [x] 4.3 Verifica caso non generabile (es. carta con valore non conforme) → nessun crash, codice in chiaro + messaggio — *verificato via self-check renderer: valido→ok, valore non conforme→fail, formato ignoto→fail (nessuna eccezione)*
- [x] 4.4 Verifica luminosità al massimo all'apertura e ripristino all'uscita (per quanto osservabile su emulatore) — *codice applicato in OnAppearing/OnDisappearing; indicatore di sistema visibile sulle sole pagine carta. Valore esatto non asseribile via dumpsys su emulatore*
- [x] 4.5 `openspec validate maui-show-card` senza errori
