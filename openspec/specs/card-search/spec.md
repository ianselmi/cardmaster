# card-search

## Purpose

Ricerca e accesso rapido alle carte salvate nella pagina della lista carte: filtro testuale su nome ed emittente, filtro per label a chip multi-selezione (in OR, combinato in AND con la ricerca), indicatore del numero di carte visibili e barra delle carte usate di recente, basata sul tracciamento dell'ultimo utilizzo di ciascuna carta.

## Requirements

### Requirement: Ricerca testuale tra le carte

Il sistema SHALL offrire un campo di ricerca sulla pagina della lista carte che filtra le carte visibili in base al nome (`DisplayName`) e all'emittente (`IssuerName`). Il confronto SHALL essere **case-insensitive** e **accent-insensitive** (es. "citta" trova "Città").

#### Scenario: Filtro per nome

- **WHEN** l'utente digita un testo che corrisponde (anche parzialmente) al nome di una o più carte
- **THEN** la griglia mostra solo le carte il cui nome contiene il testo cercato

#### Scenario: Filtro per emittente

- **WHEN** l'utente digita un testo che corrisponde (anche parzialmente) all'emittente di una o più carte
- **THEN** la griglia mostra solo le carte il cui emittente contiene il testo cercato

#### Scenario: Ricerca senza distinzione di maiuscole/minuscole o accenti

- **WHEN** l'utente digita "citta" e una carta ha nome "Città"
- **THEN** la carta compare tra i risultati

#### Scenario: Nessun risultato

- **WHEN** il testo cercato non corrisponde a nessuna carta
- **THEN** la griglia mostra uno stato vuoto distinto da quello di "nessuna carta salvata" (es. "Nessuna carta trovata")

#### Scenario: Campo di ricerca vuoto

- **WHEN** il campo di ricerca è vuoto
- **THEN** la griglia mostra tutte le carte attive, come oggi

### Requirement: Filtro per label nella lista carte

Il sistema SHALL mostrare nella pagina della lista carte una riga orizzontale di **chip**, una per ciascuna label in uso su almeno una carta attiva, ordinate alfabeticamente. I chip SHALL essere selezionabili in modo **multiplo**: toccandone uno lo si attiva, toccandolo di nuovo lo si disattiva. Con almeno un chip attivo la griglia SHALL mostrare le carte che hanno **almeno una** delle label selezionate (OR). Quando non esiste ancora nessuna label, la riga di chip MUST NOT essere mostrata.

#### Scenario: Filtro con una label

- **WHEN** l'utente attiva il chip di una label
- **THEN** la griglia mostra solo le carte a cui quella label è assegnata

#### Scenario: Filtro con più label in OR

- **WHEN** l'utente attiva i chip di due label diverse
- **THEN** la griglia mostra le carte a cui è assegnata almeno una delle due label

#### Scenario: Disattivazione del filtro

- **WHEN** l'utente tocca di nuovo un chip attivo finché nessun chip è selezionato
- **THEN** la griglia torna a mostrare tutte le carte attive compatibili con la sola ricerca testuale

#### Scenario: Nessuna label esistente

- **WHEN** nessuna carta ha label assegnate
- **THEN** la riga di chip non viene mostrata e la lista si comporta come oggi

#### Scenario: Chip aggiornati dopo una modifica

- **WHEN** l'utente assegna una nuova label a una carta e torna alla lista
- **THEN** il chip di quella label compare tra i filtri disponibili

#### Scenario: Selezione rimasta orfana

- **WHEN** un filtro attivo riguarda una label che nessuna carta usa più
- **THEN** al ricaricamento della lista quella selezione viene rimossa e non lascia la griglia vuota senza chip a cui attribuirlo

### Requirement: Combinazione tra filtro per label e ricerca testuale

Il sistema SHALL combinare il filtro per label e la ricerca testuale in **AND**: con entrambi attivi la griglia SHALL mostrare solo le carte che soddisfano sia il testo cercato (su nome o emittente) sia almeno una delle label selezionate. Le due funzioni SHALL restare indipendenti: modificare il testo MUST NOT azzerare i chip selezionati, e viceversa.

#### Scenario: Testo e label insieme

- **WHEN** l'utente ha attivato una label e digita un testo di ricerca
- **THEN** la griglia mostra solo le carte che hanno quella label e il cui nome o emittente contiene il testo

#### Scenario: Selezione preservata durante la ricerca

- **WHEN** l'utente modifica o svuota il campo di ricerca mentre dei chip sono attivi
- **THEN** i chip restano selezionati e il filtro per label continua ad applicarsi

#### Scenario: Nessun risultato con filtro attivo

- **WHEN** la combinazione di testo e label non corrisponde a nessuna carta
- **THEN** viene mostrato lo stato vuoto dei risultati, distinto da quello di "nessuna carta salvata", con un testo che indica che sono attivi dei filtri

### Requirement: Indicatore del numero di carte

Il sistema SHALL mostrare vicino al campo di ricerca il numero di carte visibili. A riposo (nessuna ricerca testuale e nessuna label selezionata) SHALL mostrare il totale delle carte salvate. Con un filtro attivo — testuale, per label, o entrambi — SHALL mostrare il numero di carte visibili rispetto al totale.

#### Scenario: Conteggio a riposo

- **WHEN** il campo di ricerca è vuoto, nessun chip è selezionato e ci sono carte salvate
- **THEN** l'indicatore mostra il numero totale di carte (es. "30 carte")

#### Scenario: Conteggio durante il filtro

- **WHEN** l'utente ha digitato un testo di ricerca
- **THEN** l'indicatore mostra il numero di carte trovate sul totale (es. "5/30")

#### Scenario: Conteggio con filtro per label

- **WHEN** l'utente ha selezionato una o più label, con o senza testo di ricerca
- **THEN** l'indicatore mostra il numero di carte visibili sul totale delle carte salvate

### Requirement: Barra delle carte usate di recente

Il sistema SHALL mostrare una barra orizzontale con le ultime 3 carte aperte dall'utente, ordinate dalla più recente. La barra SHALL restare visibile anche mentre è attivo un filtro di ricerca. Se nessuna carta è mai stata aperta, la barra SHALL essere assente.

#### Scenario: Carte usate mostrate in ordine di recenza

- **WHEN** l'utente ha aperto più carte in momenti diversi
- **THEN** la barra mostra le ultime 3 aperte, con la più recente per prima

#### Scenario: Meno di 3 carte usate

- **WHEN** l'utente ha aperto meno di 3 carte distinte
- **THEN** la barra mostra solo le carte effettivamente aperte, senza segnaposto vuoti

#### Scenario: Nessuna carta mai aperta

- **WHEN** nessuna carta è mai stata aperta dall'utente
- **THEN** la barra "Usate di recente" non viene mostrata

#### Scenario: Barra visibile durante la ricerca

- **WHEN** l'utente sta filtrando la lista con il campo di ricerca
- **THEN** la barra delle carte usate di recente resta visibile, indipendentemente dal filtro

#### Scenario: Apertura di una carta dalla barra dei recenti

- **WHEN** l'utente apre una carta selezionandola dalla barra "Usate di recente"
- **THEN** la navigazione porta alla stessa pagina di visualizzazione barcode che si otterrebbe aprendola dalla griglia

### Requirement: Tracciamento dell'ultimo utilizzo di una carta

Il sistema SHALL registrare la data/ora dell'ultima apertura riuscita della pagina di visualizzazione barcode per ciascuna carta. Un semplice ritorno alla pagina già caricata (es. dopo una modifica) MUST NOT contare come nuovo utilizzo.

#### Scenario: Apertura di una carta aggiorna l'ultimo utilizzo

- **WHEN** l'utente apre una carta e la pagina di visualizzazione barcode carica correttamente i dati
- **THEN** il sistema registra il momento corrente come ultimo utilizzo di quella carta

#### Scenario: Ricaricamento dopo una modifica non conta come nuovo utilizzo

- **WHEN** l'utente modifica una carta e torna alla pagina di visualizzazione, che ricarica i dati aggiornati
- **THEN** l'ultimo utilizzo registrato non viene aggiornato da questo ricaricamento
