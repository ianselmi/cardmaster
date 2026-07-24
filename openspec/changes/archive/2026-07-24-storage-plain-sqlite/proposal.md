## Why

Il pacchetto `SQLitePCLRaw.bundle_e_sqlcipher` usato per la cifratura del DB è **deprecato** ("legacy, no longer maintained" da SQLitePCLRaw 3.0) e non ha un rimpiazzo drop-in gratuito ufficiale. Decisione (24 lug 2026): la cifratura at-rest **non è essenziale** per la v1 offline, quindi si passa a **SQLite in chiaro** con un provider mantenuto, rimuovendo la dipendenza deprecata. Colti l'occasione, si aggiornano i pacchetti NuGet all'ultima versione.

## What Changes

- **Rimozione** del pacchetto deprecato `SQLitePCLRaw.bundle_e_sqlcipher`; adozione di `SQLitePCLRaw.bundle_e_sqlite3` (provider SQLite in chiaro, mantenuto).
- Il database locale **non è più cifrato**: `DatabaseService` apre la connessione senza `PRAGMA key`.
- **Rimozione** dell'uso della chiave e dell'Android Keystore per lo storage (`IKeyStoreService` e implementazione Android) — non più necessari.
- **Aggiornamento** pacchetti NuGet all'ultima versione disponibile (es. `Microsoft.Maui.Controls`, `Microsoft.Extensions.Logging.Debug`).
- Aggiornamento della documentazione/vincoli (PLAN.md, config.yaml, docs/technical-notes.md) per riflettere lo storage in chiaro.

## Capabilities

### New Capabilities
- Nessuna.

### Modified Capabilities
- `local-storage`: rimossa la cifratura SQLCipher e la chiave nel Keystore; il database resta locale ma **in chiaro**. Restano invariati Id client-generati, tombstone e inizializzazione/migrazione dello schema.

## Impact

- **Codice**: `DatabaseService` (niente key), rimozione `IKeyStoreService`/`KeyStoreService` e relativa registrazione DI; `Batteries_V2.Init()` ora attiva il provider in chiaro.
- **Dipendenze**: `-SQLitePCLRaw.bundle_e_sqlcipher`, `+SQLitePCLRaw.bundle_e_sqlite3`; aggiornamento pacchetti aggiornabili.
- **Sicurezza**: il DB non è più cifrato a riposo. La protezione resta delegata al lockscreen di Android (coerente con la v1 senza gate di sblocco).
- **Dati esistenti**: i DB cifrati già presenti sui device di sviluppo non sono leggibili dal provider in chiaro → vanno ricreati (clear dati app). Nessun utente in produzione.
- **Vincolo di qualità**: compilazione senza errori (`dotnet build`), criterio di accettazione.
