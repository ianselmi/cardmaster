## Context

`maui-shell` aveva introdotto SQLite cifrato con SQLCipher (`bundle_e_sqlcipher`) e una chiave in Android Keystore. Quel pacchetto è ora deprecato senza rimpiazzo drop-in gratuito. Decisione: rimuovere la cifratura e usare SQLite in chiaro con un provider mantenuto. Cambio di sola infrastruttura di storage; il modello dati (Id client-generati, tombstone) e l'API del repository restano identici.

## Goals / Non-Goals

**Goals:**
- Sostituire il bundle SQLCipher deprecato con `SQLitePCLRaw.bundle_e_sqlite3` (mantenuto).
- Aprire il DB senza `PRAGMA key`; rimuovere chiave/Keystore dallo storage.
- Aggiornare i pacchetti NuGet aggiornabili.
- Nessuna modifica all'API pubblica di `IDatabaseService`/`ICardRepository`.
- Compilazione senza errori.

**Non-Goals:**
- Cambiare il modello dati o l'API del repository.
- Migrare dati esistenti (solo device di sviluppo → clear).
- Reintrodurre cifratura (eventuale ripensamento userebbe `SQLite3MC.PCLRaw.bundle`, mantenuto).

## Decisions

### Provider — `SQLitePCLRaw.bundle_e_sqlite3`
Provider SQLite in chiaro, open-source e mantenuto (a differenza degli ex bundle di cifratura). `sqlite-net-base` resta l'ORM. `SQLitePCL.Batteries_V2.Init()` in `MauiProgram` ora attiva questo provider (unico bundle referenziato).

### `DatabaseService` senza chiave
Si rimuove il parametro `key` da `SQLiteConnectionString` e la dipendenza da `IKeyStoreService`. Il resto (path, flag, `CreateTable`, `PRAGMA user_version`) invariato.

### Rimozione Keystore
`IKeyStoreService` e `Platforms/Android/Services/KeyStoreService.cs` vengono rimossi (codice morto senza cifratura), insieme alla registrazione DI. Se in futuro servisse di nuovo una chiave (es. reintroduzione cifratura), si ripristina.

### Aggiornamento pacchetti
`Microsoft.Maui.Controls` e `Microsoft.Extensions.Logging.Debug` alle ultime versioni disponibili; verifica build. Le altre dipendenze sono già all'ultima.

## Risks / Trade-offs

- **Perdita cifratura at-rest** → accettata: v1 offline, protezione delegata al lockscreen; decisione esplicita dell'utente.
- **DB cifrati esistenti illeggibili** → clear dati app sui device di sviluppo; nessun utente in produzione.
- **Aggiornamento Maui.Controls (10.0.20→ultima)** → possibile regressione; mitigato da build + prova runtime.

## Migration Plan

Sui device di sviluppo: clear dati app (il vecchio file cifrato non è apribile dal provider in chiaro). Nessuna migrazione di produzione. Rollback = ripristino del bundle precedente (ma è deprecato) o passaggio a `SQLite3MC.PCLRaw.bundle`.

## Open Questions

- Nessuna. Eventuale reintroduzione futura della cifratura → `SQLite3MC.PCLRaw.bundle` (mantenuto, gratuito).
