## ADDED Requirements

### Requirement: Sezione Lettura assistita nelle Impostazioni

Le Impostazioni SHALL contenere una sezione per la rilettura degli scontrini tramite modello, con: l'**interruttore** della funzione (spento per default), l'inserimento e la rimozione della **chiave** dell'utente, la **verifica** della chiave, la scelta del **modello** con il costo indicativo per scontrino accanto a ciascuno, e la dichiarazione di **che cosa lascia il device** quando la funzione è attiva. La sezione SHALL indicare chiaramente lo stato corrente: funzione spenta, oppure attiva con o senza chiave configurata.

#### Scenario: Stato iniziale

- **WHEN** l'utente apre le Impostazioni su un'installazione nuova
- **THEN** la sezione esiste, la funzione risulta spenta e nessuna chiave risulta configurata

#### Scenario: Attivazione senza chiave

- **WHEN** l'utente attiva la funzione senza aver inserito una chiave
- **THEN** la sezione dichiara che serve una chiave e la funzione resta inutilizzabile finché non viene fornita

#### Scenario: Scelta del modello con il costo accanto

- **WHEN** l'utente apre la scelta del modello
- **THEN** vede per ciascuno un ordine di grandezza del costo per scontrino

#### Scenario: Cosa lascia il device, dichiarato nelle impostazioni

- **WHEN** l'utente legge la sezione
- **THEN** trova scritto che con la funzione attiva l'immagine dello scontrino viene inviata a un servizio esterno, su sua richiesta e a sue spese
