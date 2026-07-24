## Context

Prima change del progetto: non esiste ancora codice. CardMaster è un'app v1 100% offline (solo Android, .NET MAUI). Questa change posa la fondazione — progetto MAUI, DI, navigazione — e lo storage locale cifrato su cui poggeranno tutte le feature successive.

I vincoli architetturali sono già decisi (vedi `openspec/config.yaml` e `PLAN.md`) e non vanno rimessi in discussione: SQLite cifrato con SQLCipher, chiave in Android Keystore, Id client-generati (GUID/ULID), tombstone al posto dei DELETE fisici. Il gate di autenticazione utente (biometria/PIN che protegge la chiave) è di competenza della change successiva `maui-unlock`: qui la chiave viene generata e custodita nel Keystore, ma senza binding all'autenticazione.

Vincolo di qualità trasversale: **la soluzione deve compilare senza errori** — è un criterio di accettazione, non un dettaglio.

## Goals / Non-Goals

**Goals:**
- Progetto .NET MAUI che compila e si avvia su Android, con `TargetFrameworks` limitato ad Android.
- Host DI configurato in `MauiProgram`; pagine, ViewModel e servizi risolti dal container.
- Navigazione con .NET MAUI Shell e una pagina placeholder di lista carte.
- Database SQLite cifrato con SQLCipher, con chiave generata/custodita in Android Keystore.
- Modello dati base con Id client-generati, timestamp e tombstone; layer di accesso ai dati con init/migrazione idempotente all'avvio.
- Struttura di cartelle/namespace e convenzioni pronte per le feature successive.

**Non-Goals:**
- Autenticazione utente (biometria/PIN, invalidazione chiave al cambio impronte) → `maui-unlock`.
- Scansione barcode (ML Kit) → `maui-scan-card`; rendering barcode (ZXing) → `maui-show-card`.
- Catalogo emittenti → `issuer-seed`; condivisione QR → `maui-share-qr`.
- Qualsiasi funzionalità di rete, backend o sincronizzazione (v2).
- UI rifinita: la pagina lista è un segnaposto funzionale, non un design finale.

## Decisions

### Struttura di progetto — single-project MAUI
Progetto MAUI single-project standard con cartelle per area: `Views/`, `ViewModels/`, `Services/`, `Data/` (entità + DbContext/connection), `Models/`. Namespace radice `CardMaster`. Semplice e idiomatico; evita over-engineering a vertical slice (quello è per il backend v2, non per il client MAUI).

### Accesso a SQLite + SQLCipher — `sqlite-net-base` + `SQLitePCLRaw.bundle_e_sqlcipher`
Uso `sqlite-net-base` come ORM leggero con il bundle `SQLitePCLRaw.bundle_e_sqlcipher` che porta il nativo SQLCipher su Android. La chiave si passa via `PRAGMA key` (SQLCipher) all'apertura della connessione, incapsulata in un `IDatabaseService`.
- **IMPORTANTE (scoperto in fase di apply)**: NON usare `sqlite-net-pcl`. Esso trascina transitivamente `SQLitePCLRaw.bundle_green` (provider `e_sqlite3`, SQLite in chiaro): con due provider presenti vince quello non cifrato, `PRAGMA key` diventa un no-op e il DB nasce **non cifrato** (header `SQLite format 3` in chiaro). `sqlite-net-base` è lo stesso ORM senza il bundle in chiaro, così `bundle_e_sqlcipher` resta l'unico provider. All'avvio si chiama `SQLitePCL.Batteries_V2.Init()` per attivarlo.
- **Alternative considerate**: EF Core + SQLite → più pesante e la cifratura SQLCipher è meno diretta sul client; scartato (EF Core resta per il backend v2). `sqlite-net-pcl` → scartato per il conflitto di provider descritto sopra.

### Gestione della chiave — Android Keystore, senza binding utente (per ora)
Al primo avvio genero una chiave simmetrica robusta e la custodisco nel Keystore Android (via API native tramite binding). La chiave usata come passphrase SQLCipher non deve mai finire in chiaro in `Preferences`/file.
- Decisione di confine: **niente `setUserAuthenticationRequired`** in questa change. Aggiungerlo ora romperebbe l'avvio (non c'è ancora il flusso biometrico/PIN). `maui-unlock` innesterà il binding utente sulla chiave. Il provider della chiave è isolato dietro un'interfaccia (`IKeyStoreService`) così che `maui-unlock` possa estenderlo senza toccare il resto.
- **Alternative considerate**: `SecureStorage` di MAUI → sotto il cofano usa Keystore ma dà meno controllo sui flag di protezione che serviranno a `maui-unlock`; preferisco l'accesso diretto al Keystore isolato dietro interfaccia.

### Modello dati base — entità con audit + tombstone
Una classe base (`EntityBase`) con `Id` (string, GUID/ULID generato dal client), `CreatedAt`, `UpdatedAt`, `DeletedAt` (nullable → tombstone). Le query attive filtrano `DeletedAt == null`. In questa change basta un'entità concreta minima (es. `Card` con i campi essenziali) per validare lo schema; l'arricchimento avviene in `maui-scan-card`.
- **ULID vs GUID**: ULID è ordinabile per tempo (comodo per liste), ma GUID è nativo in .NET senza dipendenze. Decisione: `Guid.NewGuid().ToString()` per non aggiungere dipendenze ora; il tipo `string` dell'`Id` lascia aperta la migrazione a ULID senza cambiare schema.

### Init/migrazione schema — idempotente all'avvio
`IDatabaseService.InitializeAsync()` apre la connessione cifrata e chiama `CreateTableAsync` (idempotente in sqlite-net). Un semplice `PRAGMA user_version` traccia la versione dello schema per future migrazioni. Invocato una volta all'avvio dall'host DI.

## Risks / Trade-offs

- **Binding nativo Keystore fragile su alcune API level** → isolare dietro `IKeyStoreService`, testare su emulatore con API level di riferimento; mantenere la logica minima finché `maui-unlock` non la estende.
- **SQLCipher bundle aumenta la dimensione dell'APK e ha nativi per-ABI** → accettabile per v1; verificare che la build Android includa gli ABI necessari.
- **Rischio di anticipare troppo `maui-unlock`** → limite netto: nessun flag di user-authentication sulla chiave in questa change. Documentato come confine esplicito.
- **`dotnet build` di MAUI Android richiede il workload installato** → prerequisito ambiente; la task di verifica build deve girare con `maui-android` workload presente.

## Migration Plan

Prima change: nessun dato pregresso, nessuna migrazione da eseguire. Rollback = rimozione del progetto. Lo schema parte da `user_version = 1`; le migrazioni future incrementeranno la versione e applicheranno gli step condizionali.

## Open Questions

- Nessuna bloccante. Da confermare in fase di apply: l'API level Android minimo di riferimento per il Keystore (default: quello di MAUI corrente).
