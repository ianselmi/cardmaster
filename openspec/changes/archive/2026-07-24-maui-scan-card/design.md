## Context

`maui-shell` fornisce repository cifrato e navigazione Shell; `issuer-seed` fornisce il catalogo emittenti con `MatchAsync`. Questa change aggiunge il primo flusso che **crea** carte: scansione barcode (ML Kit) o inserimento manuale, arricchimento emittente opzionale, avviso duplicati, salvataggio locale.

Decisioni prese in esplorazione (24 lug 2026):
- Il "riconoscimento emittente" non si deriva dal barcode (un numero di carta non identifica il negozio): l'utente indica l'emittente e la carta viene arricchita dal catalogo. L'emittente resta **opzionale**.
- Alla prima lettura valida la camera **si ferma** e si va alla conferma.
- L'**inserimento manuale** è un percorso di pari livello.
- **Avviso duplicati** sul barcode (non bloccante).

## Goals / Non-Goals

**Goals:**
- Scansione ML Kit dei formati EAN-13/EAN-8/Code128/Code39/ITF/Codabar/QR/PDF417, stop alla prima lettura.
- Inserimento manuale (valore + formato) come alternativa completa.
- Schermata conferma/modifica: barcode, formato, emittente (catalogo/libero/assente), nome, colore.
- Arricchimento dal catalogo via `MatchAsync`; avviso duplicati; salvataggio via repository esistente.
- Gestione permesso camera con fallback all'inserimento manuale.
- Compilazione senza errori (criterio di accettazione).

**Non-Goals:**
- Visualizzazione/rendering della carta salvata → `maui-show-card`.
- Condivisione/ricezione via QR → `maui-share-qr` (che riuserà il salvataggio).
- Validazione del checksum del barcode (es. EAN-13) → si salva ciò che è letto/digitato.
- Modifica/eliminazione di una carta esistente (solo creazione qui).
- Riconoscimento emittente da prefissi GS1.

## Decisions

### Libreria di scansione — `BarcodeScanning.Native.Maui` (ML Kit)
Come da vincoli di progetto: ML Kit è più affidabile di ZXing su Android per codici stampati/plastificati. La libreria espone un `CameraView` e un evento di rilevazione; va inizializzata in `MauiProgram` (es. `UseBarcodeScanning()`), configurato l'insieme dei formati, e gestito il ciclo di vita camera (start su `OnAppearing`, stop su `OnDisappearing` e alla prima lettura).
- **Alternative considerate**: ZXing per la scansione → meno affidabile su Android per codici reali; resta invece la scelta per il *rendering* in `maui-show-card`.

### Due pagine: `ScanPage` → `AddCardPage`
`ScanPage` ospita la camera e un pulsante "inserisci a mano"; alla prima lettura naviga a `AddCardPage` passando barcode+formato. L'inserimento manuale naviga a `AddCardPage` con barcode vuoto (l'utente digita valore e sceglie il formato). `AddCardPage` è l'unico punto di salvataggio, così scan e manuale condividono la stessa logica (conferma, arricchimento, duplicati, save).
- **Alternative considerate**: pagina unica con camera + form → intreccia ciclo di vita camera e editing, più fragile. Scartata.

### Passaggio dati tra pagine
Navigazione Shell con parametri (query properties) per portare barcode e formato dalla scansione alla conferma. I ViewModel sono risolti da DI (come in `maui-shell`).

### Arricchimento emittente
In `AddCardPage` l'emittente si sceglie da una lista che espone il catalogo (`GetAllAsync`) più le opzioni "altro (digita)" e "nessuno". Alla selezione di un emittente del catalogo si popolano `IssuerName`, `Color` (da `ColorHex`), `LogoId` (da `LogoAsset`) e si propone il formato atteso; il nome visualizzato prende default dal nome emittente. Per emittente libero si usa `MatchAsync` sul testo per un eventuale arricchimento se coincide con un alias; altrimenti resta libero.

### Avviso duplicati — nuovo metodo repository
Aggiunta a `ICardRepository` di `Task<bool> AnyActiveByBarcodeAsync(string barcode)` che verifica l'esistenza di una carta con `DeletedAt == null` e stesso `Barcode`. In `AddCardPage`, al momento del salvataggio, se il metodo ritorna true si mostra un avviso non bloccante (aggiungi comunque / annulla). Criterio: **solo barcode** (in scan l'emittente è scelto dopo; il barcode è il segnale forte).

### Mappatura formati
La libreria usa un proprio enum di formato; serve una mappatura verso la stringa `BarcodeFormat` della carta (valori stabili: `EAN13`, `EAN8`, `UPCA`, `UPCE`, `CODE128`, `CODE39`, `ITF`, `CODABAR`, `QR_CODE`, `PDF417`). La stessa lista alimenta il picker dell'inserimento manuale.
- **UPC-A/UPC-E aggiunti in fase di verifica (24 lug 2026)**: non erano nell'elenco iniziale del PLAN.md, ma sono formati fedeltà molto comuni (e il barcode di test dell'emulatore è UPC-A). Aggiunta banale (mapping + stringhe); ZXing li supporta anche in rendering per `maui-show-card`.

### Permesso camera
Richiesta runtime `Permissions.RequestAsync<Permissions.Camera>()` all'ingresso in `ScanPage`. Manifest Android con `<uses-permission android:name="android.permission.CAMERA"/>` e `uses-feature` camera non obbligatoria. Negato → messaggio + scorciatoia all'inserimento manuale.

### Entry point nella lista
Un `ToolbarItem`/pulsante "+" in `CardListPage` naviga a `ScanPage`. Al ritorno dopo il salvataggio, la lista si ricarica (`LoadAsync` in `OnAppearing`, già presente).

## Risks / Trade-offs

- **ML Kit su emulatore capriccioso** → validare il percorso manuale su emulatore con certezza; per la scansione reale usare la scena virtuale della camera o un device fisico. Esplicitato nelle tasks.
- **Dimensione APK e dipendenze native ML Kit** → accettabile per v1.
- **Ciclo di vita camera** (non rilasciata → spreco batteria/lock) → stop deterministico su `OnDisappearing` e alla prima lettura.
- **Barcode non validi da inserimento manuale** → accettati qui; l'impatto sul rendering è di `maui-show-card`.
- **Duplicati solo su barcode** → potrebbe unire due emittenti che condividono un numero (raro); accettabile come avviso non bloccante, l'utente decide.

## Migration Plan

Nessuna migrazione dati: solo nuove pagine, una dipendenza NuGet, un permesso e un metodo repository additivo. Rollback = rimozione delle pagine, della dipendenza e del metodo. Lo schema del DB non cambia (la `Card` esiste già da `maui-shell`).

## Open Questions

- Il valore preciso dei nomi di formato ML Kit → stringa `BarcodeFormat` va confermato in apply leggendo l'enum della libreria (mappatura 1:1 con i formati elencati).
- Verifica della scansione reale: su emulatore (scena virtuale) o rimandata a device fisico? Da decidere in fase di test.
