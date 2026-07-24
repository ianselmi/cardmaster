## Context

`maui-shell` ha posato progetto, DI e storage cifrato. La prossima feature utente sarà `maui-scan-card`, che al momento della scansione deve **riconoscere l'emittente** per popolare nome/logo/colore della carta e per il controllo duplicati. In v1 (offline, nessun server) il catalogo emittenti è un **seed statico incluso nell'app**; in v2 sarà sostituito/esteso da un catalogo sincronizzato lato server (change `issuer-catalog`).

Questa change prepara solo il catalogo e il servizio di accesso: non tocca la scansione né la UI di visualizzazione.

## Goals / Non-Goals

**Goals:**
- Modello `Issuer` e catalogo statico bundle nell'app, versionato.
- Servizio `IIssuerCatalog` con: elenco completo, lookup per id, match per nome/alias (case-insensitive), caricamento idempotente.
- Zero rete, zero sync; nessun impatto sul DB cifrato.
- Compilazione senza errori (criterio di accettazione).

**Non-Goals:**
- Scansione e riconoscimento a runtime dal barcode → `maui-scan-card`.
- Visualizzazione (logo/colore in pagina carta) → `maui-show-card`.
- Catalogo sincronizzato lato server, pull incrementale → v2 `issuer-catalog`.
- Editing del catalogo da parte dell'utente (è read-only).
- Set completo e definitivo di emittenti reali con loghi ufficiali (i loghi reali sono soggetti a copyright; qui si include un seed minimo di esempio con placeholder).

## Decisions

### Formato del seed — JSON bundle come `MauiAsset` in `Resources/Raw/`
Il catalogo è un file `Resources/Raw/issuers.json` incluso come asset dell'app, letto a runtime con `FileSystem.OpenAppPackageFileAsync("issuers.json")`. JSON perché è facile da editare, versionare in git e diffare; caricarlo da asset lo tiene fuori dal codice compilato e fuori dal DB.
- **Alternative considerate**: lista hardcoded in C# → scomoda da mantenere e da diffare; seed nella tabella SQLite → inutile mescolare dato statico read-only con i dati utente cifrati, complica le migrazioni. Scartate.

### L'emittente è opzionale — il catalogo dà suggerimenti, non vincoli
L'associazione di una carta a un emittente è **facoltativa**. Al momento dell'aggiunta (UI in `maui-scan-card`) l'utente potrà: scegliere un emittente dal catalogo, **digitarne uno libero** non presente in catalogo, oppure **non indicarne alcuno**. Di conseguenza `IIssuerCatalog` è una sorgente di *proposte*: `MatchAsync` restituisce `null` senza errori quando non c'è corrispondenza, e il modello `Card` (già con `IssuerName` nullable) accetta un emittente libero o assente. Nessun vincolo di integrità verso il catalogo.

### Il catalogo NON entra nel DB cifrato
Gli emittenti sono dato statico pubblico, non dati sensibili dell'utente. Restano un catalogo read-only in memoria caricato dall'asset. Le carte dell'utente (nel DB) fanno riferimento all'emittente tramite id/nome, ma il catalogo resta separato.

### Modello `Issuer`
Campi: `Id` (string stabile, es. `"esselunga"`), `Name` (visualizzato), `ColorHex` (string, es. `"#0055A4"`), `ExpectedBarcodeFormat` (string nullable, es. `"EAN13"`), `LogoAsset` (string nullable, nome dell'immagine bundle), `Aliases` (lista di string per il match). Deserializzato dal JSON con `System.Text.Json`.

### Servizio `IIssuerCatalog` — caricamento idempotente e lazy
Interfaccia: `Task<IReadOnlyList<Issuer>> GetAllAsync()`, `Task<Issuer?> GetByIdAsync(string id)`, `Task<Issuer?> MatchAsync(string text)`. L'implementazione carica il JSON una sola volta (guardia con `SemaphoreSlim`, come `DatabaseService`) e mantiene in memoria l'elenco più un indice per id. Il match confronta `text` con `Name` e `Aliases` in modo case-insensitive (`StringComparer.OrdinalIgnoreCase`), con normalizzazione basilare (trim). Registrato come singleton in DI.
- **Alternative considerate**: caricamento sincrono nel costruttore → l'I/O da asset è async su Android; meglio lazy async coerente con `DatabaseService`.

### Loghi — immagini bundle referenziate per nome
`LogoAsset` referenzia un'immagine in `Resources/Images/`. In questa change si includono placeholder (o si lascia `LogoAsset` nullo per gli esempi) per non introdurre loghi protetti da copyright; il rendering del logo è comunque competenza di `maui-show-card`.

### Versionamento del seed
Il JSON ha una struttura `{ "version": 1, "issuers": [ ... ] }`. Il servizio legge `version` e, per ora, supporta la sola v1; versioni sconosciute → errore chiaro / fallback documentato. Prepara la sostituzione in v2.

## Risks / Trade-offs

- **Loghi reali e copyright** → in v1 si usano placeholder / campi logo opzionali; l'aggiunta di loghi ufficiali è una decisione separata, fuori scope qui.
- **Qualità del match testuale** (nomi con varianti, accenti) → per ora match semplice su nome+alias case-insensitive; se `maui-scan-card` richiederà matching più ricco, si potrà estendere senza cambiare il contratto.
- **Asset non trovato / JSON malformato** → il servizio deve fallire in modo esplicito e diagnosticabile (eccezione chiara), non silenziosamente con catalogo vuoto.
- **Dimensione del catalogo** → trascurabile in v1 (poche decine di emittenti); nessun problema di performance a caricarlo tutto in memoria.

## Migration Plan

Nessuna migrazione dati: solo aggiunta di un asset e di un servizio. Rollback = rimozione del servizio e dell'asset. Il seed parte da `version: 1`.

## Open Questions

- Quali emittenti includere nel seed iniziale? Da confermare in fase di apply: si parte con un piccolo set di esempio (senza loghi ufficiali) sufficiente a validare il servizio; l'ampliamento è incrementale.
