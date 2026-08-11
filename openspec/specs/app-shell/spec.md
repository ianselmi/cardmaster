# app-shell

## Purpose

Scheletro dell'applicazione .NET MAUI (solo Android): host di dependency injection, ciclo di vita dell'app, navigazione Shell e struttura di progetto su cui si innestano le feature v1 (unlock, scan, show, share).

## Requirements

### Requirement: Progetto MAUI solo Android

Il sistema SHALL essere un'applicazione .NET MAUI il cui unico target di compilazione è Android. La soluzione MUST compilare senza errori tramite `dotnet build` e l'app MUST avviarsi su un device o emulatore Android.

#### Scenario: La soluzione compila senza errori

- **WHEN** si esegue `dotnet build` sulla soluzione
- **THEN** la build termina con esito positivo e senza errori di compilazione

#### Scenario: Target limitato ad Android

- **WHEN** si ispezionano i `TargetFrameworks` del progetto
- **THEN** è presente il solo target Android (nessun target iOS, Windows o MacCatalyst)

#### Scenario: Avvio dell'app

- **WHEN** l'app viene installata e lanciata su un device/emulatore Android
- **THEN** l'app si avvia e mostra la schermata iniziale senza crash

### Requirement: Host di dependency injection

Il sistema SHALL configurare un host di dependency injection all'avvio (in `MauiProgram`) e SHALL risolvere pagine, ViewModel e servizi tramite il container.

#### Scenario: Servizi registrati sono risolvibili

- **WHEN** l'app si avvia e una pagina viene navigata
- **THEN** la pagina e il suo ViewModel vengono risolti dal container DI con le loro dipendenze iniettate

#### Scenario: Servizi applicativi registrati

- **WHEN** si ispeziona la configurazione dell'host
- **THEN** i servizi di accesso ai dati sono registrati nel container e disponibili per l'iniezione

### Requirement: Navigazione con Shell

Il sistema SHALL usare .NET MAUI Shell per la navigazione e SHALL presentare le funzioni principali come **sezioni di primo livello** selezionabili dall'utente: la **lista carte**, mostrata all'avvio, e gli **scontrini**. Il passaggio da una sezione all'altra MUST NOT alterare lo stato dell'altra sezione.

#### Scenario: Pagina iniziale mostrata

- **WHEN** l'app si avvia
- **THEN** viene mostrata la sezione lista carte

#### Scenario: Struttura di navigazione estendibile

- **WHEN** una nuova pagina viene registrata come rotta di navigazione
- **THEN** è raggiungibile tramite la navigazione Shell senza modificare l'infrastruttura esistente

#### Scenario: Passaggio tra sezioni di primo livello

- **WHEN** l'utente passa dalla lista carte agli scontrini e torna indietro
- **THEN** entrambe le sezioni sono raggiungibili direttamente e la lista carte si ripresenta senza perdere ricerca e filtri attivi
