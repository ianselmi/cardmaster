## 1. Dipendenza e permessi

- [x] 1.1 Aggiungere il pacchetto NuGet `BarcodeScanning.Native.Maui` e inizializzarlo in `MauiProgram` (es. `UseBarcodeScanning()`)
- [x] 1.2 Aggiungere il permesso `CAMERA` nell'AndroidManifest e `uses-feature` camera non obbligatoria
- [x] 1.3 `dotnet build`: compila senza errori con la nuova dipendenza

## 2. Dati e mappatura formati

- [x] 2.1 Definire la lista stabile dei formati `BarcodeFormat` supportati (EAN13, EAN8, UPCA, UPCE, CODE128, CODE39, ITF, CODABAR, QR_CODE, PDF417) e la mappatura da/verso l'enum della libreria ML Kit
- [x] 2.2 Aggiungere a `ICardRepository` + `CardRepository` il metodo `AnyActiveByBarcodeAsync(string barcode)` (esclude i tombstone)

## 3. Schermata di scansione

- [x] 3.1 Creare `ScanPage` + ViewModel con `CameraView`, configurata sui formati supportati
- [x] 3.2 Richiesta runtime del permesso camera all'ingresso; gestione del diniego (messaggio + scorciatoia all'inserimento manuale)
- [x] 3.3 Stop alla prima lettura valida e navigazione a `AddCardPage` con barcode+formato (Shell query params)
- [x] 3.4 Gestione ciclo di vita camera: start su `OnAppearing`, stop su `OnDisappearing` e dopo la lettura
- [x] 3.5 Pulsante "inserisci a mano" che naviga a `AddCardPage` senza barcode pre-compilato

## 4. Schermata conferma/modifica e salvataggio

- [x] 4.1 Creare `AddCardPage` + ViewModel: campi barcode (editable), formato (picker per inserimento manuale), emittente, nome, colore
- [x] 4.2 Selettore emittente: catalogo (`IIssuerCatalog.GetAllAsync`) + opzioni "altro (digita)" e "nessuno"; arricchimento (colore/logo/formato atteso, default nome) alla selezione dal catalogo
- [x] 4.3 Validazione campi obbligatori: barcode, formato, nome visualizzato (default = nome emittente quando presente)
- [x] 4.4 Avviso duplicati non bloccante via `AnyActiveByBarcodeAsync` prima del salvataggio (aggiungi comunque / annulla)
- [x] 4.5 Salvataggio via `ICardRepository.AddAsync` (Id client-generato, tombstone) e ritorno alla lista
- [x] 4.6 Azione "+" in `CardListPage` (ToolbarItem) che apre `ScanPage`; ricarica lista al ritorno
- [x] 4.7 Registrare pagine e ViewModel nel container DI

## 5. Verifica

- [x] 5.1 `dotnet build`: compila senza errori (criterio di accettazione) — *0 errori; warning residui solo lato-libreria ML Kit (NU1608/XA4301, dichiarati ignorabili nel README)*
- [x] 5.2 Verifica runtime su emulatore del percorso MANUALE: inserisci barcode+formato → conferma → carta salvata e visibile in lista — *verificato: self-check inList=True + screenshot della lista con la carta; navigazione lista→scan→manuale→conferma confermata via screenshot*
- [x] 5.3 Verifica arricchimento emittente dal catalogo (colore/nome di default) e avviso duplicati (barcode ripetuto) — *verificato: self-check enriched=True (nome default da catalogo), dupAfter=True (rilevazione duplicati)*
- [x] 5.4 Verifica scansione reale: su scena virtuale della camera dell'emulatore o su device fisico — *VERIFICATO end-to-end: con un poster barcode nella scena virtuale, ML Kit ha letto un UPC-A (`225001353133`) in continua; l'app si è fermata alla prima lettura, ha aperto la conferma con valore+formato, emittente "Coop" dal catalogo, e la carta è comparsa in lista. UPC-A/UPC-E aggiunti ai formati supportati.*
- [x] 5.5 `openspec validate maui-scan-card` senza errori
