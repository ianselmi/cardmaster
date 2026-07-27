## MODIFIED Requirements

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
