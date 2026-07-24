# local-storage

## Purpose

Storage locale del device: database SQLite (in chiaro in v1). Definisce il modello dati base (Id client-generati, tombstone) e il layer di accesso ai dati con inizializzazione/migrazione idempotente dello schema.

## Requirements

### Requirement: Database SQLite locale

Il sistema SHALL persistere i dati in un database SQLite locale sul device, tramite un provider SQLite mantenuto. In v1 il database NON è cifrato: la protezione dei dati è delegata al lockscreen del dispositivo.

#### Scenario: Persistenza locale

- **WHEN** l'app salva o legge dati
- **THEN** le operazioni avvengono su un database SQLite locale, senza necessità di rete

#### Scenario: Apertura del database

- **WHEN** l'app apre il database all'avvio
- **THEN** la connessione viene stabilita senza richiedere una chiave di cifratura

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
