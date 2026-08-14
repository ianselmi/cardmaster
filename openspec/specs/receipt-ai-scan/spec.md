# receipt-ai-scan

## Purpose

Rilettura di uno scontrino tramite un modello multimodale come **rete di sicurezza, non come sostituto** della lettura locale: si propone soltanto quando la quadratura fallisce, cioè esattamente quando l'app sa già di non potersi fidare di quello che ha letto.

Opt-in e spenta per default, con la chiave dell'utente, il consenso informato prima che l'immagine lasci il device e il costo dichiarato prima e misurato dopo. L'esito è **confrontato** con la lettura locale e mai imposto: su uno scontrino che quadra non parte nessuna chiamata e non si spende niente.

## Requirements

### Requirement: Rilettura con modello solo quando la quadratura fallisce

Il sistema SHALL offrire la rilettura dello scontrino tramite modello **soltanto** quando la lettura locale non quadra con il totale, o quando non ha prodotto righe. Quando le righe quadrano, il sistema MUST NOT inviare alcun dato e MUST NOT proporre la rilettura. La funzione SHALL essere **spenta per default** e attivabile solo dalle impostazioni.

#### Scenario: Scontrino che quadra

- **WHEN** la somma delle righe lette in locale coincide con il totale
- **THEN** nessuna chiamata viene effettuata, nessun dato lascia il device e all'utente non viene proposto niente

#### Scenario: Scontrino che non quadra con la funzione attiva

- **WHEN** le righe non quadrano e l'utente ha attivato la funzione e inserito una chiave
- **THEN** il sistema propone di rileggere lo scontrino con il modello, dichiarando che l'immagine verrà inviata

#### Scenario: Funzione spenta

- **WHEN** le righe non quadrano e la funzione non è stata attivata
- **THEN** l'app si comporta come oggi: mostra lo scarto e invita alla correzione manuale, senza menzionare costi né inviare nulla

#### Scenario: Nessuna riga ricostruita

- **WHEN** la lettura locale non produce alcuna riga e la funzione è attiva
- **THEN** la rilettura viene proposta come per una quadratura fallita

### Requirement: Consenso informato prima che l'immagine lasci il device

Prima del primo invio il sistema SHALL dichiarare esplicitamente **che cosa** viene inviato (l'immagine dello scontrino, con prodotti, prezzi, esercente e data), **a chi**, e **che il costo è a carico dell'utente**. L'attivazione MUST essere un'azione esplicita: il sistema MUST NOT attivare la funzione per default, né come effetto collaterale dell'inserimento della chiave.

#### Scenario: Prima attivazione

- **WHEN** l'utente attiva la funzione
- **THEN** legge che cosa lascerà il device, verso quale servizio e a spese di chi, e deve confermare

#### Scenario: Chiave inserita senza attivazione

- **WHEN** l'utente inserisce una chiave ma non attiva la funzione
- **THEN** nessuna chiamata viene mai effettuata

#### Scenario: Nessun invio silenzioso

- **WHEN** si esamina il comportamento dell'app con la funzione attiva
- **THEN** ogni invio dell'immagine segue una scelta dell'utente su quello scontrino, e nessun invio avviene in background o su scontrini già salvati

### Requirement: Esito strutturato del modello

La richiesta SHALL imporre al modello uno **schema** per la risposta, e il sistema SHALL leggere l'esito secondo quello schema invece di interpretare testo libero. L'esito SHALL usare le stesse unità del resto del dominio: importi in **centesimi interi**, quantità in **millesimi**, aliquote in **punti base**. Una risposta non conforme allo schema SHALL essere trattata come un errore dichiarato, e MUST NOT produrre righe parziali o inventate.

#### Scenario: Risposta conforme

- **WHEN** il modello risponde secondo lo schema
- **THEN** le righe entrano nelle stesse strutture della lettura locale, senza conversioni in virgola mobile

#### Scenario: Risposta non conforme

- **WHEN** la risposta non rispetta lo schema
- **THEN** il sistema lo segnala come errore e mantiene le righe della lettura locale

#### Scenario: Nessuna riga inventata da una risposta parziale

- **WHEN** la risposta è troncata o incompleta
- **THEN** il sistema non ne ricava righe parziali

### Requirement: L'esito del modello è confrontato, non imposto

Le righe prodotte dal modello SHALL passare per la **stessa verifica di quadratura** delle righe locali e SHALL essere presentate nella stessa schermata di conferma, modificabili come le altre. Il sistema SHALL proporre l'esito del modello quando quadra e quello locale no; quando **nessuno dei due** quadra, SHALL dirlo. Il sistema MUST NOT sostituire in silenzio righe che quadravano.

#### Scenario: Il modello quadra e il parser no

- **WHEN** le righe del modello coincidono con il totale e quelle locali no
- **THEN** il sistema propone le righe del modello, dicendo che vengono dalla rilettura

#### Scenario: Nessuno dei due quadra

- **WHEN** né le righe locali né quelle del modello coincidono con il totale
- **THEN** il sistema lo dichiara e lascia all'utente la correzione manuale

#### Scenario: Righe del modello correggibili

- **WHEN** l'utente riceve le righe rilette
- **THEN** può modificarle, aggiungerne ed eliminarne esattamente come quelle lette in locale

### Requirement: Degradazione senza rete, senza chiave e in caso di errore

Ogni fallimento del percorso con il modello — chiave assente o rifiutata, credito esaurito, limite di frequenza raggiunto, rete assente, timeout, risposta non valida — SHALL essere comunicato con una causa **riconoscibile** e un'indicazione di cosa fare. In tutti i casi lo scontrino SHALL restare salvabile con le righe della lettura locale, e l'errore MUST NOT far perdere le correzioni già fatte.

#### Scenario: Device offline

- **WHEN** l'utente chiede la rilettura senza rete
- **THEN** il sistema lo dice, e lo scontrino resta salvabile con le righe locali

#### Scenario: Chiave rifiutata

- **WHEN** il servizio rifiuta la chiave
- **THEN** il sistema distingue questo caso dagli altri errori e indica di verificare la chiave nelle impostazioni

#### Scenario: Credito esaurito o limite raggiunto

- **WHEN** il servizio rifiuta la richiesta per credito esaurito o troppe richieste
- **THEN** il sistema lo dichiara come tale, distinguendolo da un errore dell'app

#### Scenario: Correzioni conservate dopo un errore

- **WHEN** una rilettura fallisce dopo che l'utente aveva già corretto alcune righe
- **THEN** le correzioni restano

### Requirement: Costo dichiarato prima e misurato dopo

Il sistema SHALL mostrare, accanto a ogni modello selezionabile, un **ordine di grandezza del costo per scontrino**, e SHALL riportare dopo ogni chiamata il **consumo effettivo** ricavato dalla risposta. Il sistema MUST NOT effettuare chiamate a pagamento senza che l'utente le abbia chieste per quello scontrino.

#### Scenario: Costo visibile alla scelta del modello

- **WHEN** l'utente sceglie il modello nelle impostazioni
- **THEN** vede accanto a ciascuno quanto costa all'incirca leggere uno scontrino

#### Scenario: Consumo effettivo dopo la chiamata

- **WHEN** una rilettura si conclude
- **THEN** l'utente può vedere quanto è costata davvero, non solo la stima

#### Scenario: Nessuna spesa non richiesta

- **WHEN** si esamina il comportamento dell'app
- **THEN** nessuna chiamata a pagamento parte senza una richiesta esplicita dell'utente su uno scontrino specifico

### Requirement: L'immagine inviata è ridotta al minimo utile

Il sistema SHALL ridimensionare l'immagine prima dell'invio, alla risoluzione minima che mantiene lo scontrino leggibile, e MUST NOT inviare dati non necessari alla lettura — né lo storico, né altre immagini, né il database.

#### Scenario: Immagine ridimensionata

- **WHEN** viene inviata la foto di uno scontrino
- **THEN** è una versione ridimensionata, non l'originale a piena risoluzione

#### Scenario: Nessun altro dato inviato

- **WHEN** si esamina il contenuto di una richiesta
- **THEN** contiene l'immagine di quello scontrino e le istruzioni, e nient'altro dell'utente
