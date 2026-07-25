## Context

La v1 di CardMaster è offline-first: le carte sono locali al device, con Id client-generati e tombstone (`local-storage`), e si acquisiscono via scansione ML Kit o inserimento manuale (`card-capture`, flusso `ScanPage` → `AddCardPage`). Il rendering barcode/QR è già disponibile (`IBarcodeRenderer` con ZXing.Net + SkiaSharp, che supporta `QR_CODE`). Manca la condivisione: il PLAN prevede un **QR self-contained** mostrato da un device e **scansionato** dall'altro, che ne crea una **copia indipendente**, senza server né legame persistente.

Questo design descrive come (a) serializzare lo snapshot di una carta in un payload adatto al QR, (b) generarlo/mostrarlo, e (c) riconoscerlo e importarlo riusando il flusso di conferma esistente.

## Goals / Non-Goals

**Goals:**
- Generare un QR che incapsula uno snapshot completo e autosufficiente della carta (nome, emittente, barcode, formato, colore, logo id).
- Rendere il payload **riconoscibile** (magic prefix) e **versionato** per l'evoluzione futura, e distinguibile da un normale QR fedeltà.
- Ricezione via lo stesso `ScanPage`: se il QR è un payload CardMaster, aprire `AddCardPage` **pre-compilata con l'intero snapshot**, altrimenti comportamento attuale.
- Salvare la carta ricevuta come copia indipendente, riusando l'avviso duplicati già esistente.
- Parsing robusto: nessun crash su testo non-CardMaster, payload corrotto o versione non supportata.
- Zero nuove dipendenze NuGet; solo `System.Text.Json` della BCL.

**Non-Goals:**
- Condivisione tramite **share sheet / link / file** (resta il solo QR device-to-device in v1). Un eventuale export testuale è rimandabile.
- Sincronizzazione o legame persistente tra device (è v2).
- Trasferire il **binario del logo**: si condivide solo il `LogoId` (stringa); l'asset è risolto localmente dal catalogo del ricevente, con fallback grafico se assente.
- Cifratura/firma del payload (non c'è segreto in gioco: è la stessa carta fedeltà, il cui barcode è pubblico per costruzione).

## Decisions

### D1 — Formato del payload: magic prefix + JSON compatto, senza compressione

Il testo codificato nel QR è: **`CMC` + `<versione>` + JSON compatto** con chiavi corte. Esempio (v1):

```
CMC1{"n":"Esselunga Fìdaty","i":"Esselunga","b":"20481234567","f":"EAN13","c":"#0A7D2C","l":"esselunga"}
```

- **Magic prefix `CMC` + cifra di versione**: la rilevazione in scansione è un semplice `StartsWith("CMC")`; la cifra dà la versione dello schema (ridondante ma comoda con il campo, vedi sotto). Il resto è JSON.
- **Chiavi corte** (`n` name, `i` issuer, `b` barcode, `f` format, `c` color, `l` logoId; `v` opzionale ridondante con il prefisso) per contenere la dimensione. Campi assenti (emittente/colore/logo opzionali) vengono omessi.
- **Serializzazione con `System.Text.Json`**: gestisce escaping di nomi con caratteri speciali/accentati e Unicode gratuitamente.

**Alternative considerate:**
- *Base64URL(GZip(JSON))* — **scartata**: a queste dimensioni (tipicamente < 200 byte) GZip non comprime in modo utile e anzi può gonfiare; rende il payload opaco e non ispezionabile, in cambio di nessun beneficio reale. Riconsiderabile solo se in futuro il payload crescesse.
- *Formato pipe-delimited custom* (`CMC1|nome|issuer|...`) — **scartata**: richiederebbe escaping manuale dei separatori; JSON è più robusto e già supportato dalla BCL.
- *URI scheme `cardmaster://...`* — **scartata** per la v1: non serve deep-link (la ricezione è per scansione, non per apertura di link), e i query-string andrebbero comunque URL-encoded.

### D2 — `ICardShareCodec`: un servizio di encode/decode che non lancia

Nuovo servizio (registrato singleton in `MauiProgram`) con due operazioni:
- `string Encode(CardShareSnapshot snapshot)` → produce il testo del QR.
- `CardShareDecodeResult TryDecode(string text)` → esito tipizzato: `Recognized` (snapshot ricostruito), `NotCardMaster` (prefisso assente → trattare come QR normale), `Unsupported`/`Corrupt` (prefisso presente ma versione ignota o JSON illeggibile → messaggio d'errore). **Non lancia mai** verso il chiamante (coerente con lo stile di `IBarcodeRenderer`).

`CardShareSnapshot` è un `record` con i campi dello snapshot; è la struttura di scambio tra `ShareCardViewModel`, il codec e il flusso di ricezione.

### D3 — Generazione: `SharePage` + `ShareCardViewModel`, riuso di `IBarcodeRenderer`

- Nuova rotta `SharePage` (pagina + VM transient). Ingresso dalla toolbar di `ShowCardPage` con una `ToolbarItem` "Condividi" → `GoToAsync("SharePage", { id })`.
- Il VM carica la carta via `ICardRepository.GetByIdAsync`, costruisce lo `CardShareSnapshot`, chiama `codec.Encode` e poi `IBarcodeRenderer.Render(payload, "QR_CODE")`. Se il render fallisce (payload troppo denso), mostra il messaggio di fallback (stessa semantica di `ShowCardPage`).
- La pagina mostra il QR su sfondo bianco (come `ShowCardPage`) e una breve istruzione ("Fai scansionare questo codice dall'altro telefono").

### D4 — Ricezione: rilevazione nel `ScanPage`, riuso di `AddCardPage`

- In `ScanPage.OnDetectionFinished`, quando il formato rilevato è QR, si passa il valore a `codec.TryDecode`:
  - `Recognized` → naviga a `AddCardPage` passando **l'intero snapshot** nel dizionario di query (barcode, format, name, issuer, color, logo).
  - `NotCardMaster` → comportamento attuale (naviga con solo `barcode` + `format` = QR).
  - `Unsupported`/`Corrupt` → mostra un avviso ("Codice CardMaster non leggibile / versione non supportata"), **non** naviga e riprende la scansione.
- Nessun nuovo entry point separato per l'import: scansionare il QR di un amico dal normale flusso "+" funziona direttamente. (Alternativa valutata: una voce "Importa da QR" dedicata — non necessaria, aggiungerebbe superficie UI a parità di funzione.)

### D5 — `AddCardViewModel`: prefill esteso e ordine con `InitializeAsync`

`ApplyQueryAttributes` viene esteso per leggere i campi opzionali dello snapshot (`name`, `issuer`, `color`, `logo`) oltre a `barcode`/`format`. Poiché `ApplyQueryAttributes` può essere invocato **prima** che `InitializeAsync` popoli `IssuerOptions`, i valori ricevuti (in particolare l'emittente) vengono **memorizzati** e risolti **dopo** l'inizializzazione:
- Se l'emittente dello snapshot coincide (case-insensitive) con un emittente del catalogo → `SelectedIssuerOption` = quel nome (mantenendo però colore/logo **ricevuti**, non sovrascritti dall'arricchimento del catalogo).
- Altrimenti → `SelectedIssuerOption` = `OtherLabel` con `CustomIssuerName` = emittente ricevuto.
- `color`/`logo` ricevuti impostano i campi privati `_colorHex`/`_logoId` usati al salvataggio.

Il resto del flusso (validazione, `BarcodeExistsAsync` per il duplicato, `SaveAsync` con `AddAsync`) è **invariato**: la carta ricevuta ottiene un nuovo Id e compare in lista come qualunque altra.

### D6 — Passaggio parametri via dizionario oggetti (non query-string)

Come già fa `ScanPage` oggi, i parametri di navigazione si passano con `GoToAsync(route, IDictionary<string,object>)`, evitando l'URL-encoding manuale di valori con spazi/accenti/`#` (il colore è un hex con `#`).

## Risks / Trade-offs

- **QR troppo denso / illeggibile** con nomi lunghi → Mitigazione: chiavi corte, omissione dei campi vuoti, nessun binario del logo; se l'encode fallisce si mostra il fallback invece di generare un QR inservibile. Riconsiderare la compressione solo se emergono payload grandi.
- **`LogoId` non presente nel catalogo del ricevente** (versione app diversa) → Mitigazione: il logo è un riferimento, non un binario; l'assenza dell'asset ricade sul rendering di default del tile (colore deterministico), senza errori. Il `color` viaggia comunque nel payload ed è autosufficiente.
- **Falso positivo del magic prefix** (un QR fedeltà il cui contenuto inizia per `CMC`) → Mitigazione: il riconoscimento richiede prefisso `CMC` + cifra versione **e** corpo JSON valido; in caso di JSON non valido l'esito è `Corrupt` e non si crea nulla. Probabilità pratica trascurabile.
- **Evoluzione dello schema** → Mitigazione: versione esplicita nel prefisso/campo; `TryDecode` distingue "versione non supportata" da "corrotto", permettendo in futuro messaggi mirati ("aggiorna l'app").
- **Nessuna cifratura del payload** → Accettato per definizione: il barcode fedeltà è di per sé pubblico (viene mostrato alla cassa); non c'è segreto da proteggere.

## Migration Plan

Nessuna migrazione dati: lo schema `Card` e il DB non cambiano. La feature è puramente additiva (nuova pagina, nuovo servizio, estensione di due punti esistenti). Rollback = rimozione della rotta/azione "Condividi" e del ramo di rilevazione in `ScanPage`, senza impatto sui dati.

## Open Questions

- Nessuna bloccante. Possibile estensione futura (fuori scope): offrire anche la **condivisione del payload come testo** via share sheet Android, utile quando i due device non sono fisicamente vicini.
