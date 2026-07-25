## 1. Codec dello snapshot (payload)

- [x] 1.1 Definire il `record CardShareSnapshot` (nome, emittente?, barcode, formato, colore?, logoId?) in `Data/` o `Services/`
- [x] 1.2 Definire l'esito tipizzato `CardShareDecodeResult` con stati `Recognized(snapshot)`, `NotCardMaster`, `Unsupported`, `Corrupt` (nessuna eccezione verso il chiamante)
- [x] 1.3 Creare `ICardShareCodec` con `string Encode(CardShareSnapshot)` e `CardShareDecodeResult TryDecode(string text)`
- [x] 1.4 Implementare `CardShareCodec`: magic prefix `CMC` + cifra versione + JSON compatto con chiavi corte (`n/i/b/f/c/l`), omettendo i campi vuoti; serializzazione con `System.Text.Json`
- [x] 1.5 Implementare `TryDecode`: quick-check del prefisso, parsing robusto, gestione versione non supportata / JSON corrotto → esito gestito senza throw
- [x] 1.6 Registrare `ICardShareCodec` come singleton in `MauiProgram`

## 2. Generazione del QR di condivisione

- [x] 2.1 Creare `ShareCardViewModel`: carica la carta via `ICardRepository.GetByIdAsync`, costruisce lo snapshot, chiama `codec.Encode` e `IBarcodeRenderer.Render(payload, "QR_CODE")`; espone immagine QR + flag disponibilità/fallback
- [x] 2.2 Creare `SharePage.xaml`/`.cs`: QR su sfondo bianco (stile coerente con `ShowCardPage`), messaggio di fallback se non generabile, breve istruzione d'uso; `IQueryAttributable` per l'`id` carta
- [x] 2.3 Registrare `SharePage`/`ShareCardViewModel` (transient) in `MauiProgram` e la rotta `SharePage` in `AppShell`
- [x] 2.4 Aggiungere la `ToolbarItem` "Condividi" a `ShowCardPage` che naviga a `SharePage` passando l'`id` della carta

## 3. Ricezione via scansione

- [x] 3.1 In `ScanPage.OnDetectionFinished`, per i QR passare il valore a `codec.TryDecode` prima della navigazione
- [x] 3.2 Esito `Recognized` → `GoToAsync("AddCardPage", ...)` con l'intero snapshot (barcode, format, name, issuer, color, logo)
- [x] 3.3 Esito `NotCardMaster` → comportamento attuale (naviga con solo `barcode` + `format` = QR)
- [x] 3.4 Esito `Unsupported`/`Corrupt` → mostra avviso "codice CardMaster non leggibile", non naviga, riprende la scansione

## 4. Conferma/salvataggio pre-compilato

- [x] 4.1 Estendere `AddCardViewModel.ApplyQueryAttributes` per leggere `name`, `issuer`, `color`, `logo` oltre a `barcode`/`format`; memorizzare i valori ricevuti
- [x] 4.2 Risolvere l'emittente ricevuto dopo `InitializeAsync`: match case-insensitive col catalogo → opzione corrispondente; altrimenti `OtherLabel` + `CustomIssuerName`, senza sovrascrivere colore/logo ricevuti con l'arricchimento del catalogo
- [x] 4.3 Impostare `_colorHex`/`_logoId` dai valori ricevuti così che `SaveAsync` li persista
- [x] 4.4 Verificare che l'avviso duplicati (`BarcodeExistsAsync`) e il salvataggio come copia indipendente (`AddAsync`, nuovo Id) funzionino invariati in ricezione

## 5. Verifica

- [x] 5.1 `dotnet build` senza errori (criterio di accettazione)
- [x] 5.2 Round-trip del codec: encode→decode ricostruisce fedelmente lo snapshot (inclusi nomi con accenti/caratteri speciali)
- [ ] 5.3 Prova su emulatore/device: condividi una carta → mostra QR → scansiona con un secondo device/istanza → conferma pre-compilata → salvataggio come copia; verifica avviso duplicati e casi payload non-CardMaster/corrotto
- [x] 5.4 `openspec validate maui-share-qr --strict` senza errori
