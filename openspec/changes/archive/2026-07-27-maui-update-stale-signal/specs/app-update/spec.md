## ADDED Requirements

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
