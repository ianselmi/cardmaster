## ADDED Requirements

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

## MODIFIED Requirements

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
