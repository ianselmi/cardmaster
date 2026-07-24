## ADDED Requirements

### Requirement: Database SQLite locale

Il sistema SHALL persistere i dati in un database SQLite locale sul device, tramite un provider SQLite mantenuto. In v1 il database NON è cifrato: la protezione dei dati è delegata al lockscreen del dispositivo.

#### Scenario: Persistenza locale

- **WHEN** l'app salva o legge dati
- **THEN** le operazioni avvengono su un database SQLite locale, senza necessità di rete

#### Scenario: Apertura del database

- **WHEN** l'app apre il database all'avvio
- **THEN** la connessione viene stabilita senza richiedere una chiave di cifratura

## REMOVED Requirements

### Requirement: Database SQLite cifrato con SQLCipher

**Reason**: Il pacchetto `SQLitePCLRaw.bundle_e_sqlcipher` è deprecato (legacy, non mantenuto) e senza rimpiazzo drop-in gratuito; la cifratura at-rest non è ritenuta essenziale per la v1 offline.

**Migration**: Si adotta un provider SQLite in chiaro mantenuto (`SQLitePCLRaw.bundle_e_sqlite3`). I database cifrati esistenti sui device di sviluppo non sono leggibili dal nuovo provider e vanno ricreati (clear dati app). Nessun utente in produzione.

### Requirement: Chiave di cifratura nell'Android Keystore

**Reason**: Senza cifratura del database la chiave non serve più.

**Migration**: Rimozione di `IKeyStoreService` e dell'implementazione Android dallo storage; nessun dato utente da migrare.
