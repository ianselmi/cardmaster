## Why

Condividere una carta fedeltà con un'altra persona è una delle funzioni cardine della v1, ma oggi manca del tutto: l'unico modo per avere la stessa carta su due device è riscansionarne il barcode fisico. Il PLAN prevede una condivisione **peer-to-peer, 100% offline**, in cui un device mostra un **QR code self-contained** (che incapsula tutti i dati della carta) e l'altro lo scansiona creando una **copia indipendente**. Tutti i mattoni sono già pronti: rendering QR (`IBarcodeRenderer`), scansione ML Kit (`card-capture`) e flusso di conferma/salvataggio riusabile (`AddCardPage`/`AddCardViewModel`).

## What Changes

- **Generazione del QR di condivisione**: dalla pagina di visualizzazione carta (`ShowCardPage`) una nuova azione **"Condividi"** apre una schermata che mostra un **QR code** contenente uno *snapshot* completo della carta (nome, emittente, barcode, formato, colore, logo id).
- **Payload self-contained e versionato**: i dati della carta vengono serializzati in un formato **compatto** con un **prefisso riconoscibile** (magic) e un **numero di versione** (`v`), così da restare leggibili anche da versioni future dell'app e distinguibili da un normale QR fedeltà. Nessun riferimento remoto: lo snapshot funziona anche se il mittente cancella poi la carta.
- **Ricezione tramite scansione**: il flusso di scansione esistente (`card-capture`) **riconosce** il payload CardMaster quando inquadra un QR di condivisione e, invece di trattarlo come un barcode fedeltà grezzo, lo **decodifica** e apre la schermata di conferma **pre-compilata con tutti i campi** dello snapshot.
- **Copia indipendente + controllo duplicati alla ricezione**: la carta ricevuta viene salvata come **nuova copia locale** (Id client-generato, tombstone), senza legame col mittente. Prima del salvataggio si riusa l'avviso duplicati (stesso barcode di una carta attiva) per proporre di **saltare** invece di duplicare.
- **Robustezza del parsing**: un QR non-CardMaster o un payload corrotto/di versione non supportata NON deve far crashare l'app; nel flusso di scansione un QR non riconosciuto come condivisione resta trattato come normale barcode QR (comportamento attuale).

## Capabilities

### New Capabilities
- `card-sharing`: generazione di un QR code self-contained che incapsula lo snapshot completo di una carta (payload compatto, versionato, con magic prefix) e sua serializzazione/deserializzazione robusta. Copre la schermata di condivisione e il contratto del payload; la ricezione (scansione + conferma) è coperta dall'estensione di `card-capture`.

### Modified Capabilities
- `card-capture`: il flusso di scansione riconosce un payload di condivisione CardMaster e apre la conferma **pre-compilata con l'intero snapshot** (nome, emittente, colore, logo, barcode, formato), anziché col solo barcode+formato; la carta ricevuta è salvata come copia indipendente con il consueto avviso duplicati non bloccante.

## Impact

- **Nuovo codice**: `SharePage` + `ShareCardViewModel` (rendering del QR di condivisione); servizio `ICardShareCodec` per serializzare/deserializzare lo snapshot (payload versionato con magic prefix); azione "Condividi" in `ShowCardPage`.
- **Codice modificato**: `ScanPage`/`OnDetectionFinished` per intercettare e decodificare il payload di condivisione; `AddCardViewModel.ApplyQueryAttributes` esteso per accettare i campi aggiuntivi dello snapshot (nome, emittente, colore, logo) oltre a barcode/formato.
- **Riuso**: `IBarcodeRenderer.Render(payload, "QR_CODE")` per il QR; `AddCardPage` per la conferma; `ICardRepository.AnyActiveByBarcodeAsync` per il duplicato.
- **Dipendenze**: nessuna nuova NuGet (ZXing + ML Kit già presenti). Serializzazione con `System.Text.Json` della BCL.
- **Nessuna rete**: generazione e ricezione sono interamente locali e offline; nessun server, nessun legame persistente tra i device.
- **Vincolo di qualità**: la soluzione deve **compilare senza errori** (`dotnet build`, 0 errori) come criterio di accettazione.
- **Attenzione dimensione QR**: il payload va tenuto compatto (campi corti) per non generare un QR troppo denso; la scelta di compressione/encoding è dettagliata in `design.md`.
- **Change abilitate**: completa il nucleo di condivisione offline della v1.
