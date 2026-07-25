# app-update Specification

## Purpose

Controllo, download e installazione degli aggiornamenti dell'app tramite la Release GitHub con tag `latest`, senza alcuna infrastruttura server: verifica della versione su richiesta esplicita dell'utente, download con avanzamento e verifica di integrità best-effort, installazione tramite il package installer di sistema Android.

## Requirements
### Requirement: Controllo di nuove versioni su richiesta

Il sistema SHALL permettere all'utente di avviare, tramite azione esplicita, un controllo della presenza di una versione più recente dell'app, interrogando la Release GitHub con tag `latest` del repository di distribuzione. Il sistema NON MUST eseguire questo controllo automaticamente all'avvio o in background senza un'azione esplicita dell'utente.

#### Scenario: Nessun aggiornamento disponibile

- **WHEN** l'utente avvia il controllo aggiornamenti e il nome della Release remota coincide con la versione installata
- **THEN** il sistema informa l'utente che sta usando l'ultima versione disponibile

#### Scenario: Aggiornamento disponibile

- **WHEN** l'utente avvia il controllo aggiornamenti e il nome della Release remota è diverso dalla versione installata
- **THEN** il sistema mostra la versione disponibile e offre l'azione per scaricarla e installarla

#### Scenario: Errore di rete durante il controllo

- **WHEN** il controllo aggiornamenti fallisce per assenza di rete, timeout o errore della API GitHub (incluso rate limit)
- **THEN** il sistema mostra un messaggio d'errore comprensibile e permette di riprovare, senza bloccare il resto dell'app

### Requirement: Download dell'APK con avanzamento visibile

Il sistema SHALL scaricare l'APK dell'aggiornamento mostrando l'avanzamento del download sia nell'interfaccia sia tramite una notifica di sistema, e SHALL permettere che il download prosegua se l'app va in background.

#### Scenario: Avanzamento mostrato durante il download

- **WHEN** l'utente conferma il download di un aggiornamento disponibile
- **THEN** il sistema mostra una percentuale di avanzamento aggiornata sia in-app sia nella notifica di sistema

#### Scenario: Download interrotto

- **WHEN** il download dell'APK si interrompe per un errore di rete
- **THEN** il sistema segnala l'errore e permette di riprovare, senza lasciare notifiche di avanzamento bloccate

### Requirement: Verifica di integrità prima dell'installazione

Il sistema SHALL verificare l'integrità dell'APK scaricato prima di proporne l'installazione: se la Release espone un checksum SHA-256 per l'asset, il sistema SHALL calcolare lo SHA-256 del file scaricato e confrontarlo, rifiutando l'installazione in caso di mismatch. Indipendentemente dalla disponibilità del checksum, l'installazione SHALL avvenire tramite il package installer di sistema, che verifica autonomamente la firma dell'APK contro il certificato dell'app già installata.

#### Scenario: Checksum disponibile e corrispondente

- **WHEN** l'APK scaricato ha uno SHA-256 uguale al checksum pubblicato dalla Release
- **THEN** il sistema procede con l'installazione

#### Scenario: Checksum disponibile e non corrispondente

- **WHEN** l'APK scaricato ha uno SHA-256 diverso dal checksum pubblicato dalla Release
- **THEN** il sistema scarta il file, non avvia l'installazione e mostra un errore

#### Scenario: Checksum non disponibile

- **WHEN** la Release non espone un checksum per l'asset APK
- **THEN** il sistema procede comunque con l'installazione, affidandosi alla verifica di firma del package installer di Android

### Requirement: Installazione tramite package installer di sistema

Il sistema SHALL avviare l'installazione dell'APK scaricato tramite l'intent del package installer di Android, richiedendo all'utente il permesso "Installa app sconosciute" se non ancora concesso per l'app.

#### Scenario: Permesso già concesso

- **WHEN** l'utente conferma l'installazione e il permesso "Installa app sconosciute" è già concesso
- **THEN** il sistema avvia direttamente l'intent di installazione del sistema

#### Scenario: Permesso non ancora concesso

- **WHEN** l'utente conferma l'installazione e il permesso "Installa app sconosciute" non è concesso
- **THEN** il sistema spiega perché serve il permesso e guida l'utente alla schermata di sistema per concederlo, senza avviare l'installazione finché non è concesso

#### Scenario: Permesso negato

- **WHEN** l'utente nega il permesso "Installa app sconosciute"
- **THEN** il sistema resta in uno stato consistente (nessun crash) e permette di riprovare in seguito

