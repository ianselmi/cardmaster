## Why

CardMaster non ha ancora uno scheletro applicativo: serve la fondazione su cui poggeranno tutte le feature v1 (unlock, scan, show, share). Questa change crea il progetto .NET MAUI (solo Android) con navigazione, dependency injection e lo storage locale cifrato (SQLite + SQLCipher, chiave in Android Keystore). Farlo bene ora — con Id client-generati e tombstone già dal primo giorno — evita migrazioni dolorose quando in v2 arriverà la sincronizzazione.

## What Changes

- Nuovo progetto .NET MAUI configurato **solo per Android**, che compila e si avvia su emulatore/device.
- **Host DI** applicativo (`MauiProgram`) con registrazione dei servizi e delle pagine/ViewModel.
- **Navigazione** basata su .NET MAUI Shell con una pagina placeholder di lista carte (segnaposto: le carte vere arrivano con `maui-scan-card`).
- **Storage locale cifrato**: SQLite via SQLCipher, con chiave generata e custodita nell'**Android Keystore**. In questa change la chiave non è ancora protetta da autenticazione utente (biometria/PIN): quel gate arriva con `maui-unlock`, che si innesterà su questa base.
- **Modello dati base** con le convenzioni obbligatorie: `Id` client-generati (GUID/ULID), timestamp, e cancellazione logica via **tombstone** (mai DELETE fisico).
- **Layer di accesso ai dati** (repository/servizio) con l'inizializzazione e la migrazione dello schema all'avvio.
- Struttura di progetto e convenzioni (cartelle, namespace, `.editorconfig`/gitignore) pronte a ospitare le feature successive.

## Capabilities

### New Capabilities
- `app-shell`: scheletro dell'app MAUI Android — host DI, ciclo di vita dell'app, navigazione Shell e struttura di progetto su cui si innestano le feature v1.
- `local-storage`: database SQLite cifrato con SQLCipher, chiave gestita in Android Keystore, modello dati base con Id client-generati e tombstone, e layer di accesso ai dati con inizializzazione/migrazione dello schema.

### Modified Capabilities
- Nessuna (prima change del progetto).

## Impact

- **Nuovo codice**: soluzione/progetto MAUI Android, `MauiProgram`, `AppShell`, pagina placeholder + ViewModel, entità dati base, servizio DB e provider della chiave Keystore.
- **Dipendenze (NuGet)**: `sqlite-net-pcl` o `SQLitePCLRaw` con provider **SQLCipher** (`SQLitePCLRaw.bundle_e_sqlcipher`); binding Android per Keystore (API nativa). ML Kit / ZXing **non** rientrano in questa change.
- **Piattaforma**: target Android; `TargetFrameworks` limitato ad Android.
- **Vincolo di qualità**: la soluzione deve **compilare senza errori** — è un criterio di accettazione della change, verificato con `dotnet build`.
- **Change successive abilitate**: `maui-unlock` (protegge la chiave con biometria/PIN), `issuer-seed`, `maui-scan-card`, `maui-show-card`, `maui-share-qr`.
