# app-update Specification

## Purpose

Controllo, download e installazione degli aggiornamenti dell'app tramite la Release GitHub con tag `latest`, senza alcuna infrastruttura server: verifica della versione su richiesta esplicita dell'utente, download con avanzamento e verifica di integrità best-effort, installazione tramite il package installer di sistema Android.

## Requirements
### Requirement: Controllo di nuove versioni su richiesta o su opzione attivata

Il sistema SHALL permettere all'utente di avviare, tramite azione esplicita, un controllo della presenza di una versione più recente dell'app, interrogando la Release GitHub con tag `latest` del repository di distribuzione. Il sistema NON MUST eseguire questo controllo automaticamente all'avvio o in background, **a meno che l'utente non abbia attivato l'opzione "Avvisami di nuove versioni"** descritta dalla capability `app-update-notify`; in tal caso il controllo automatico è ammesso solo con le limitazioni di frequenza e di foreground definite da quella capability. In assenza di tale opzione attivata, il comportamento resta invariato: nessun controllo senza azione esplicita dell'utente.

#### Scenario: Nessun aggiornamento disponibile

- **WHEN** l'utente avvia il controllo aggiornamenti e il nome della Release remota coincide con la versione installata
- **THEN** il sistema informa l'utente che sta usando l'ultima versione disponibile

#### Scenario: Aggiornamento disponibile

- **WHEN** l'utente avvia il controllo aggiornamenti e il nome della Release remota è diverso dalla versione installata
- **THEN** il sistema mostra la versione disponibile e offre l'azione per scaricarla e installarla

#### Scenario: Errore di rete durante il controllo

- **WHEN** il controllo aggiornamenti fallisce per assenza di rete, timeout o errore della API GitHub (incluso rate limit)
- **THEN** il sistema mostra un messaggio d'errore comprensibile e permette di riprovare, senza bloccare il resto dell'app

#### Scenario: Nessun controllo automatico senza opzione attivata

- **WHEN** l'utente non ha attivato "Avvisami di nuove versioni"
- **THEN** il sistema non esegue alcun controllo aggiornamenti all'avvio o in background, solo su richiesta esplicita

#### Scenario: Controllo automatico con opzione attivata

- **WHEN** l'utente ha attivato "Avvisami di nuove versioni" e sono soddisfatte le condizioni di frequenza/foreground di `app-update-notify`
- **THEN** il sistema esegue il controllo automaticamente, riusando la stessa interrogazione della Release GitHub `latest` usata dal controllo manuale

### Requirement: Decadenza dell'esito del controllo una volta installato l'aggiornamento

Il sistema SHALL considerare l'esito di un controllo aggiornamenti **non più valido** quando la versione che quell'esito annunciava come disponibile coincide con la versione **attualmente installata**. In tal caso il sistema SHALL trattare l'aggiornamento come già installato e NON MUST presentarlo come disponibile in nessun punto dell'interfaccia.

#### Scenario: Aggiornamento installato dopo la rilevazione

- **WHEN** un controllo ha rilevato la versione N come disponibile e successivamente l'utente installa quella versione, riaprendo l'app
- **THEN** il sistema non presenta più la versione N come disponibile, in nessun punto dell'interfaccia

#### Scenario: Aggiornamento non ancora installato

- **WHEN** un controllo ha rilevato la versione N come disponibile e la versione installata è ancora diversa da N
- **THEN** il sistema continua a presentare la versione N come disponibile

### Requirement: Riconciliazione dello stato dell'ultimo controllo senza rete

Il sistema SHALL riconciliare lo stato persistito dell'ultimo controllo con la versione installata **all'apertura dell'app e alla ripresa dal background**, prima di ogni eventuale controllo di rete. La riconciliazione SHALL avvenire **senza alcuna richiesta di rete** e SHALL applicarsi indipendentemente dal fatto che il controllo automatico sia attivo. Quando la versione annunciata come disponibile risulta installata, il sistema SHALL azzerare lo stato persistito dell'ultimo controllo. La riconciliazione SHALL essere idempotente.

#### Scenario: Riconciliazione senza connessione di rete

- **WHEN** l'app si apre senza connessione di rete e lo stato persistito annuncia come disponibile la versione già installata
- **THEN** lo stato viene comunque azzerato e nessun aggiornamento risulta disponibile, senza tentare richieste di rete

#### Scenario: Riconciliazione con controllo automatico disattivato

- **WHEN** l'opzione di controllo automatico è disattivata e lo stato persistito annuncia come disponibile la versione già installata
- **THEN** lo stato viene comunque azzerato all'apertura dell'app, senza attendere un controllo manuale

#### Scenario: Riconciliazione ripetuta

- **WHEN** la riconciliazione viene eseguita più volte di seguito
- **THEN** l'esito è lo stesso della prima esecuzione, senza effetti collaterali

#### Scenario: Stato coerente lasciato invariato

- **WHEN** lo stato persistito annuncia una versione diversa da quella installata
- **THEN** la riconciliazione non modifica nulla e l'aggiornamento resta disponibile

### Requirement: Istante dell'ultimo controllo preservato dalla riconciliazione

La riconciliazione MUST NOT alterare la data/ora dell'ultimo controllo effettuato: un controllo è comunque avvenuto, ha solo perso rilevanza il suo esito. Dopo la riconciliazione il sistema SHALL continuare a mostrare quando è stato fatto l'ultimo controllo, riportandone l'esito come "nessun aggiornamento disponibile".

#### Scenario: Orario dell'ultimo controllo conservato

- **WHEN** la riconciliazione azzera l'esito perché la versione annunciata risulta installata
- **THEN** la data/ora dell'ultimo controllo resta quella registrata e non viene presentata come "nessun controllo ancora effettuato"

#### Scenario: Intervallo del controllo automatico non riavviato

- **WHEN** la riconciliazione azzera l'esito dell'ultimo controllo
- **THEN** l'intervallo minimo tra controlli automatici continua a decorrere dall'ultimo controllo realmente effettuato, senza farne partire subito uno nuovo

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

