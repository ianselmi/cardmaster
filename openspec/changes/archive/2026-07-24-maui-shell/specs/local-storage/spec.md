## ADDED Requirements

### Requirement: Database SQLite cifrato con SQLCipher

Il sistema SHALL persistere i dati in un database SQLite locale cifrato tramite SQLCipher. Il database MUST essere illeggibile senza la chiave di cifratura corretta.

#### Scenario: Il database è cifrato a riposo

- **WHEN** si ispeziona il file del database sul filesystem del device
- **THEN** il contenuto è cifrato e non leggibile in chiaro senza la chiave

#### Scenario: Apertura con chiave corretta

- **WHEN** l'app apre il database fornendo la chiave di cifratura corretta
- **THEN** le operazioni di lettura e scrittura funzionano correttamente

#### Scenario: Apertura con chiave errata

- **WHEN** si tenta di aprire il database con una chiave errata
- **THEN** l'apertura fallisce e nessun dato viene esposto

### Requirement: Chiave di cifratura nell'Android Keystore

Il sistema SHALL generare una chiave di cifratura del database al primo avvio e SHALL custodirla nell'Android Keystore. La chiave NON MUST essere memorizzata in chiaro nello storage applicativo o nel codice. In questa change la chiave non è ancora vincolata all'autenticazione utente (biometria/PIN): tale vincolo viene aggiunto dalla change `maui-unlock`.

#### Scenario: Generazione della chiave al primo avvio

- **WHEN** l'app viene avviata per la prima volta e non esiste ancora una chiave
- **THEN** viene generata una chiave di cifratura e custodita nell'Android Keystore

#### Scenario: Riuso della chiave esistente

- **WHEN** l'app viene avviata e una chiave esiste già nel Keystore
- **THEN** viene riutilizzata la chiave esistente per aprire il database, senza rigenerarla

#### Scenario: La chiave non è in chiaro

- **WHEN** si ispezionano le preferenze e i file dell'app
- **THEN** il materiale della chiave non è presente in chiaro fuori dal Keystore

### Requirement: Modello dati base con Id client-generati e tombstone

Il sistema SHALL definire un modello dati base le cui entità hanno `Id` generati dal client (GUID o ULID) e timestamp di creazione/modifica. Le cancellazioni SHALL essere logiche tramite tombstone: il sistema MUST NOT eseguire DELETE fisici delle righe.

#### Scenario: Id generato dal client

- **WHEN** viene creata una nuova entità
- **THEN** il suo `Id` è generato dal client (GUID/ULID) prima della persistenza

#### Scenario: Cancellazione logica via tombstone

- **WHEN** un'entità viene cancellata
- **THEN** la riga viene marcata come tombstone (es. campo `DeletedAt`/`IsDeleted`) e non viene rimossa fisicamente dal database

#### Scenario: Le entità cancellate sono escluse dalle query normali

- **WHEN** si interrogano le entità attive
- **THEN** le righe marcate come tombstone sono escluse dai risultati per default

### Requirement: Inizializzazione e migrazione dello schema

Il sistema SHALL inizializzare il database e applicare lo schema all'avvio, in modo idempotente, tramite un layer di accesso ai dati registrato nel container DI.

#### Scenario: Schema creato al primo avvio

- **WHEN** l'app si avvia e il database non esiste ancora
- **THEN** il database viene creato e lo schema base applicato

#### Scenario: Avvio idempotente su database esistente

- **WHEN** l'app si avvia e il database esiste già con lo schema aggiornato
- **THEN** l'inizializzazione non altera né corrompe i dati esistenti
