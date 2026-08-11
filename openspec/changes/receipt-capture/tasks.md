## 1. Dipendenza OCR — il rischio, prima di tutto il resto

- [x] 1.1 Aggiungere `Xamarin.Google.MLKit.TextRecognition` (variante con modello incluso) a `CardMaster.csproj` e verificare che `dotnet build` completi con 0 errori — nessun altro codice, solo il pacchetto: è qui che la change può rompersi per conflitto con ML Kit barcode di `BarcodeScanning.Native.Maui` — **passato**: `116.0.1.8`, build 0 errori, `libmlkit_google_ocr_pipeline.so` presente nel pacchetto (conferma che il modello è incluso)
- [x] 1.2 Se la build fallisce per conflitti AndroidX/Play Services: risolvere allineando le versioni; solo se irrisolvibile, valutare la variante `Xamarin.GooglePlayServices.MLKit.Text.Recognition` e **registrare la rinuncia al primo avvio offline** in `design.md` e `docs/technical-notes.md` — **non necessario**: nessun conflitto bloccante (restano warning `NU1608` su AndroidX già presenti prima della change e warning `XA4301` di libreria nativa duplicata, della stessa famiglia di quelli già emessi da ML Kit barcode)
- [x] 1.3 Misurare la dimensione dell'APK Release prima e dopo l'aggiunta del pacchetto, annotare la differenza (incide sul download dell'auto-update) — **misurato**: 48,9 MB senza OCR → 58,7 MB con OCR, cioè **+9,8 MB (+20%)**, che l'auto-update riscarica a ogni versione
- [x] 1.4 Verificare su emulatore **senza rete e con installazione pulita** che il riconoscimento funzioni al primo utilizzo: è il requisito che giustifica la variante bundled — **verificato**: disinstallazione, installazione con device già in modalità aereo, primo utilizzo in assoluto → riconoscimento completo (LIDL, P.IVA, data, totale). Nessun download di modello a runtime.

## 2. Servizio di riconoscimento testo

- [x] 2.1 Definire `IReceiptOcr` in `Services/Receipts/` con un modello di ritorno che espone blocchi/righe **con testo e rettangolo** (la geometria serve alla change `receipt-items`, non a questa)
- [x] 2.2 Implementare `MlKitReceiptOcr` in `Platforms/Android/Services/`, registrandola in `MauiProgram.cs`
- [x] 2.3 Gestire il caso "nessun testo riconosciuto" come esito normale e non come eccezione

## 3. Modello dati e schema

- [x] 3.1 Creare `Data/Receipt.cs` derivando da `EntityBase` (Id client-generato, tombstone): `MerchantName`, `MerchantVatId`, `PurchasedAt`, `TotalCents` (intero), `Currency`, `RawText`, `ImagePath`
- [x] 3.2 Aggiungere `CreateTableAsync<Receipt>()` in `DatabaseService.GetConnectionAsync` e portare `SchemaVersion` da 2 a 3, con il commento che spiega il perché (guardia `BackupNaming.CanRestore`, come in `maui-card-color-labels`)
- [x] 3.3 Creare `IReceiptRepository` + implementazione: elenco escludendo i tombstone, lettura singola, salvataggio, aggiornamento (`UpdatedAt`), cancellazione logica
- [x] 3.4 Verificare l'avvio su un database esistente v2: le carte restano intatte e la tabella nuova viene creata — **verificato** installando la build sopra la versione precedente (worktree su HEAD) con una carta già presente: carta intatta, tabella `Receipt` creata, `user_version` passato a 3 (letto dal DB estratto con `run-as`).

## 4. Estrazione dei dati di testata

- [x] 4.1 Scrivere `ReceiptHeaderParser` come **classe pura** (testo in, campi out), senza dipendenze da MAUI o da ML Kit
- [x] 4.2 Totale: parole chiave (`TOTALE`, `TOT. EURO`, `TOTALE COMPLESSIVO`, `IMPORTO PAGATO`) e importo in formato italiano, restituito in centesimi
- [x] 4.3 Data: separatori `/ - .`, anno a 2 o 4 cifre, **scarto delle date implausibili** (future o troppo remote) lasciando il campo vuoto
- [x] 4.4 Partita IVA (11 cifre) ed esercente dai blocchi in cima, scartando righe di solo indirizzo o numeri
- [x] 4.5 Ogni campo non riconosciuto resta **vuoto e marcato come tale**: nessun valore inventato o dedotto
- [x] 4.6 Provare il parser su testo OCR **reale** di almeno 3 catene diverse e correggere le regole sui casi trovati — *parziale*: provato su 8 casi di testo scontrino realistico (supermercato con `SUBTOTALE`, discount con importo a capo, importo oltre mille, data futura, totale assente, `IMPORTO PAGATO`, riga di rumore in testa, testo vuoto), tutti passano; **trovato e corretto un bug reale**: il pattern dell'ora agganciava i primi due gruppi di una data puntata (`05.08.2026` letto come le 05:08) perché accettava il punto come separatore. **Poi provato su OCR reale** (emulatore, 3 catene): sono emersi due difetti che il testo sintetico non poteva mostrare — (1) ML Kit separa le colonne, quindi la riga del totale arriva senza importo: risolto ricostruendo le righe dalla geometria (`ReceiptTextLayout`); (2) l'OCR spezza i gruppi (`11/08/ 2026`), quindi le regex ora tollerano spazi attorno ai separatori. Dopo le correzioni: 3 catene su 3 con esercente, P.IVA, data e totale corretti

## 5. Acquisizione

- [x] 5.1 Percorso fotocamera con `MediaPicker.CapturePhotoAsync`, richiesta permesso e pannello esplicativo se negato (stesso trattamento di `ScanPage.OnAppearing`)
- [x] 5.2 Percorso immagine esistente con `FilePicker.PickAsync(FilePickerFileType.Images)` — stessa scelta di `maui-scan-from-image` per non introdurre permessi di storage
- [x] 5.3 Verificare il **manifest unito** dell'APK: `READ_EXTERNAL_STORAGE` e `READ_MEDIA_IMAGES` devono restare assenti — **verificato**: nel manifest unito restano solo CAMERA, INTERNET, ACCESS_NETWORK_STATE, FOREGROUND_SERVICE*, POST_NOTIFICATIONS, REQUEST_INSTALL_PACKAGES, VIBRATE
- [x] 5.4 Annullamento dell'acquisizione: nessuno scontrino creato, nessuna immagine lasciata sul device, nessun messaggio d'errore — verificato uscendo dal selettore e dalla schermata di conferma senza salvare.

## 6. Conferma, correzione e salvataggio

- [x] 6.1 Pagina di conferma con tutti i campi di testata modificabili e distinzione visiva tra riconosciuto e non riconosciuto
- [x] 6.2 Salvataggio dello scontrino con `RawText` integrale
- [x] 6.3 Conservazione dell'immagine in una sottocartella di `FileSystem.AppDataDirectory` con `ImagePath` relativo; opzione per non conservarla
- [x] 6.4 Uscita senza confermare: nessuna scrittura su database e nessuna immagine conservata

## 7. Sezione Scontrini

- [x] 7.1 Aggiungere la seconda `ShellContent` "Scontrini" in `AppShell.xaml` e registrare le rotte nuove
- [x] 7.2 Lista ordinata dal più recente con esercente, data e totale; stato vuoto che spiega come acquisire il primo scontrino invece di mostrare una lista vuota
- [x] 7.3 Pagina di dettaglio: dati, immagine se conservata, testo riconosciuto consultabile
- [x] 7.4 Modifica dei dati di uno scontrino già salvato ed eliminazione (tombstone + rimozione dell'immagine dal device)
- [x] 7.5 Verificare che la comparsa della barra di navigazione inferiore non copra l'ultima riga della griglia carte né il FAB "+" di `maui-card-list-add-fab` — **verificato**: FAB visibile e toccabile sopra la barra. **Corretta per strada una deviazione dal design**: due `ShellContent` nudi producevano un menu a panino invece della barra in basso; servono dentro un `<TabBar>`. Tema scuro non provato.

## 8. Spesa per esercente e per mese

- [x] 8.1 Query di aggregazione per mese e per esercente sui soli dati di testata, in centesimi
- [x] 8.2 Vista con totale del mese e ripartizione per esercente
- [x] 8.3 Scontrini senza data o senza totale esclusi dai totali e individuabili per essere completati

## 9. Spazio occupato e limiti dichiarati

- [x] 9.1 Mostrare lo spazio occupato dalle immagini degli scontrini e permettere di liberarlo conservando dati e `RawText`
- [x] 9.2 Dichiarare nella pagina Backup che le immagini degli scontrini **non** sono comprese nel backup Drive e non tornano dopo un ripristino

## 10. Verifica finale

- [x] 10.1 `dotnet build` con 0 errori (criterio di accettazione, non opzionale)
- [x] 10.2 Verifica su emulatore con scontrini di 3 catene diverse (layout Esselunga/Lidl/Conad): acquisizione da immagine, riconoscimento, conferma, storico, totali. **Percorso fotocamera e modifica/eliminazione non esercitati** (la camera dell'emulatore non inquadra uno scontrino reale).
- [x] 10.3 Verifica in **modalità aereo**: l'intero giro (acquisizione, riconoscimento, salvataggio, storico, spesa del mese) è stato eseguito con il device offline, senza alcuna differenza.
- [x] 10.4 Verifica che le carte fedeltà siano rimaste invariate — carta preesistente intatta dopo l'aggiornamento, sezione carte invariata con FAB e ricerca. **Scansione, condivisione e filtri non riesercitati.**
- [x] 10.5 Confermare che la change non ha introdotto **alcuna chiamata di rete** e **alcuna chiave o credenziale** nel codice o nel pacchetto — repo pubblico e APK scaricabile da chiunque — **verificato**: nessun `HttpClient`/URL nel codice degli scontrini, nessuna occorrenza di chiave/segreto/token nel diff
- [x] 10.6 Rivedere il `git diff` prima del commit per escludere segreti, percorsi personali e immagini di scontrini reali usate nelle prove — **fatto**: 38 file, nessuna chiave/token/credenziale, nessun percorso personale, nessuna immagine di prova o database finiti nel repo (le prove sono rimaste fuori dall'albero di lavoro); `bin/` e `obj/` ignorati.
