## Why

È la prima feature con cui l'utente **crea** una carta. Finora l'app apre una lista vuota; serve poterci aggiungere carte scansionando il barcode (o digitandolo). Poggia su tutto ciò che è già pronto: repository cifrato (`maui-shell`) e catalogo emittenti (`issuer-seed`).

## What Changes

- Nuovo **flusso di acquisizione carta**: dalla lista un'azione "+" apre la **scansione** con anteprima camera live.
- Integrazione **ML Kit** via `BarcodeScanning.Native.Maui`, configurata per i formati: EAN-13, EAN-8, UPC-A, UPC-E, Code128, Code39, ITF, Codabar, QR, PDF417.
- Alla **prima lettura valida** la camera si ferma e si passa alla schermata di **conferma/modifica** con barcode e formato pre-compilati.
- **Inserimento manuale** del barcode (numero + scelta formato) come percorso di pari livello: usabile senza camera, con permesso negato, o con codice rovinato.
- **Arricchimento emittente dal catalogo**: l'emittente è opzionale; se scelto dal catalogo, la carta eredita colore/logo/formato atteso via `IIssuerCatalog.MatchAsync`. Emittente libero o assente restano validi.
- **Avviso duplicati**: se esiste già una carta *attiva* con lo stesso barcode, avviso non bloccante ("Hai già questa carta — aggiungere comunque?").
- **Salvataggio locale** con le regole esistenti: Id client-generato, timestamp, tombstone (via `ICardRepository.AddAsync`). Nuovo metodo di query per il check duplicati.
- **Gestione permesso camera** (runtime `CAMERA`): richiesto entrando nella scansione; se negato, si resta operativi con l'inserimento manuale.

## Capabilities

### New Capabilities
- `card-capture`: acquisizione di una carta tramite scansione barcode o inserimento manuale, con arricchimento opzionale dell'emittente dal catalogo, avviso duplicati e salvataggio locale. Il flusso di conferma/salvataggio sarà riusabile da `maui-share-qr` in ricezione.

### Modified Capabilities
- Nessuna. (Il metodo di query duplicati è un'aggiunta al layer dati, coperta dai requisiti di `card-capture`.)

## Impact

- **Nuovo codice**: `ScanPage` (camera) e `AddCardPage` (conferma/modifica) con relativi ViewModel; mappatura del formato ML Kit → stringa `BarcodeFormat`; metodo repository `AnyActiveByBarcodeAsync`; azione "+" nella lista.
- **Dipendenze (NuGet)**: `BarcodeScanning.Native.Maui` (ML Kit). Inizializzazione in `MauiProgram`.
- **Permessi Android**: `CAMERA` nel manifest + richiesta runtime.
- **Nessuna rete**: tutto locale e offline.
- **Vincolo di qualità**: la soluzione deve **compilare senza errori** (`dotnet build`), criterio di accettazione.
- **Test**: il percorso manuale → salvataggio → comparsa in lista è verificabile su emulatore; la scansione ML Kit reale può richiedere la scena virtuale della camera o un device fisico (annotato nelle tasks).
- **Change successive abilitate**: `maui-show-card` (visualizza la carta salvata), `maui-share-qr` (riusa il salvataggio in ricezione).
