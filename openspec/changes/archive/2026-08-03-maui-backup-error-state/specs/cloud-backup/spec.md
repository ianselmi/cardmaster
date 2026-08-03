## ADDED Requirements

### Requirement: Esito dell'ultimo backup persistito e visibile

Il sistema SHALL registrare in modo persistente l'**esito dell'ultimo tentativo di backup** — riuscito o fallito, con data/ora del tentativo e, in caso di fallimento, la **categoria dell'errore** — e SHALL mostrarlo nella sezione backup come stato permanente, non solo come messaggio momentaneo. L'esito SHALL essere registrato allo stesso modo per i backup manuali, quelli all'apertura e quelli **schedulati in background**. Un backup riuscito SHALL azzerare lo stato di errore precedente.

#### Scenario: Fallimento visibile anche dopo la chiusura del messaggio

- **WHEN** un backup manuale fallisce e l'utente chiude il messaggio di errore
- **THEN** la sezione backup continua a mostrare che l'ultimo backup non è riuscito, con la data/ora del tentativo

#### Scenario: Fallimento di un backup schedulato

- **WHEN** un backup schedulato in background fallisce mentre l'app non è in primo piano
- **THEN** alla successiva apertura della sezione backup l'utente vede che l'ultimo tentativo è fallito, con data/ora e motivo

#### Scenario: Esito persistito tra i riavvii

- **WHEN** l'ultimo tentativo di backup è fallito e l'utente riavvia l'app
- **THEN** la sezione backup mostra ancora lo stato di fallimento

#### Scenario: Ritorno allo stato di normalità

- **WHEN** dopo uno o più fallimenti un backup viene completato con successo
- **THEN** il sistema rimuove lo stato di errore e mostra l'ultimo backup come riuscito

#### Scenario: Distinzione tra ultimo backup riuscito e ultimo tentativo

- **WHEN** l'ultimo tentativo è fallito ma esiste un backup riuscito precedente
- **THEN** la sezione mostra sia la data dell'ultimo backup riuscito sia il fallimento dell'ultimo tentativo, senza far credere che i dati siano aggiornati

### Requirement: Messaggi di errore per categoria comprensibile

Ogni fallimento di un'operazione di backup o ripristino SHALL essere classificato in una **categoria** e presentato all'utente con un messaggio in linguaggio comune che dica **cosa è successo** e **cosa può fare**. Le categorie SHALL coprire almeno: assenza di rete, spazio Google Drive esaurito, credenziali Google non più valide, servizio Drive non disponibile o errore del servizio, errore locale (creazione o sostituzione dello snapshot del database). Il sistema MUST NOT mostrare come messaggio principale il testo grezzo restituito dal servizio (codici HTTP, payload JSON).

#### Scenario: Backup senza rete

- **WHEN** un backup fallisce perché il dispositivo è offline
- **THEN** il messaggio dichiara che manca la connessione e che il backup verrà ritentato quando la rete torna disponibile

#### Scenario: Spazio Drive esaurito

- **WHEN** il caricamento fallisce perché lo spazio dell'account Google Drive è esaurito
- **THEN** il messaggio dichiara che lo spazio su Drive è finito e invita a liberarne

#### Scenario: Errore del servizio Drive

- **WHEN** il servizio Drive risponde con un errore non riconducibile a rete, spazio o credenziali
- **THEN** il messaggio dichiara che Google Drive non è al momento disponibile e invita a riprovare più tardi

#### Scenario: Nessun payload tecnico in primo piano

- **WHEN** un'operazione fallisce con una risposta di errore del servizio
- **THEN** il messaggio mostrato all'utente non contiene codici di stato HTTP né il corpo JSON della risposta

### Requirement: Riconnessione dell'account senza perdere la configurazione

Quando l'errore è dovuto a credenziali Google non più valide (scadute o revocate), il sistema SHALL offrire un'azione esplicita di **riconnessione dell'account**, che ripete il consenso Google mantenendo l'account collegato, la frequenza scelta e la cronologia dei backup già presenti su Drive. L'utente MUST NOT essere obbligato a disabilitare e riabilitare il backup per tornare operativo. Una riconnessione riuscita SHALL azzerare lo stato di errore; una riconnessione annullata o fallita SHALL lasciare invariati lo stato di errore e la configurazione.

#### Scenario: Riconnessione riuscita

- **WHEN** il backup è in stato "credenziali non più valide" e l'utente completa la riconnessione dell'account Google
- **THEN** il sistema torna operativo mantenendo frequenza e configurazione, e lo stato di errore viene rimosso

#### Scenario: Riconnessione annullata

- **WHEN** l'utente avvia la riconnessione ma annulla il consenso Google
- **THEN** la configurazione del backup resta invariata e lo stato "credenziali non più valide" resta mostrato

#### Scenario: La cronologia dei backup sopravvive alla riconnessione

- **WHEN** l'utente riconnette lo stesso account Google
- **THEN** i backup già presenti nella cartella applicativa di Drive restano elencabili e ripristinabili

#### Scenario: Riconnessione con un account diverso

- **WHEN** durante la riconnessione l'utente sceglie un account Google diverso da quello collegato
- **THEN** il sistema collega il nuovo account come unico account e lo stato mostrato riflette il nuovo account

## MODIFIED Requirements

### Requirement: Informazioni di stato del backup

Il sistema SHALL mostrare all'utente: l'account collegato, l'**esito dell'ultimo tentativo di backup**, la **data e la dimensione dell'ultimo backup riuscito**, e lo **spazio disponibile** sull'account Google Drive. Questi valori SHALL essere aggiornati dopo ogni backup e all'apertura della sezione; in assenza di rete SHALL essere mostrato l'ultimo valore noto. Quando l'ultimo tentativo è fallito, lo stato mostrato SHALL renderlo evidente a colpo d'occhio, distinguendolo dallo stato normale.

#### Scenario: Dati aggiornati all'apertura

- **WHEN** l'utente apre la sezione backup con rete disponibile
- **THEN** il sistema mostra account, esito dell'ultimo tentativo, data/dimensione dell'ultimo backup riuscito e spazio disponibile aggiornati

#### Scenario: Spazio illimitato

- **WHEN** l'account Google non espone un limite di spazio
- **THEN** il sistema mostra lo spazio come illimitato anziché un valore errato

#### Scenario: Valori dalla cache offline

- **WHEN** l'utente apre la sezione backup senza rete
- **THEN** il sistema mostra gli ultimi valori noti senza bloccarsi né mostrare errori bloccanti

#### Scenario: Stato di errore evidente

- **WHEN** l'utente apre la sezione backup e l'ultimo tentativo è fallito
- **THEN** lo stato di fallimento è distinguibile a colpo d'occhio da quello di un backup riuscito

### Requirement: Robustezza degli errori di rete e autenticazione

Le operazioni verso Google Drive SHALL gestire in modo controllato assenza di rete, errori del servizio e credenziali scadute/revocate, **senza mai far crashare l'app**. In caso di credenziali non più valide il sistema SHALL portarsi in uno stato dichiarato di "riconnessione necessaria", mostrarlo all'utente e offrire l'azione per ripetere l'autenticazione, invece di fallire silenziosamente o di presentarsi come pienamente operativo. Il sistema MUST NOT restare in uno stato in cui l'utente crede che i backup automatici stiano avvenendo mentre in realtà ogni tentativo fallisce.

#### Scenario: Errore di rete gestito

- **WHEN** un'operazione di backup o ripristino fallisce per rete o servizio non disponibile
- **THEN** il sistema mostra un messaggio di errore comprensibile e l'app resta stabile

#### Scenario: Credenziali revocate

- **WHEN** un'operazione fallisce perché le credenziali Google sono scadute o revocate
- **THEN** il sistema entra in stato "riconnessione necessaria" e propone l'azione per ripetere l'autenticazione anziché fallire silenziosamente

#### Scenario: Stato di riconnessione persistente

- **WHEN** il sistema è in stato "riconnessione necessaria" e l'utente riapre la sezione backup in un momento successivo
- **THEN** lo stato è ancora dichiarato e l'azione di riconnessione è ancora offerta

#### Scenario: Credenziali non valide rilevate da un backup schedulato

- **WHEN** un backup schedulato fallisce per credenziali non più valide
- **THEN** il sistema registra lo stato "riconnessione necessaria" e lo rende visibile alla successiva apertura della sezione backup
