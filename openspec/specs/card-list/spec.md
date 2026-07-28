# card-list

## Purpose

Presentazione delle carte salvate nella pagina principale come griglia di riquadri (tile) colorati: colore scelto dall'utente quando presente, altrimenti generato in modo deterministico dal nome della carta. Definisce il layout e la resa visiva della lista, distinta dallo scaffolding di navigazione (`app-shell`) e dalla creazione carte (`card-capture`).

## Requirements

### Requirement: Griglia di riquadri a 2 colonne

Il sistema SHALL presentare le carte salvate come una griglia a **2 colonne** di riquadri con **angoli arrotondati**. I riquadri SHALL avere forma tendenzialmente quadrata.

#### Scenario: Le carte sono mostrate come griglia

- **WHEN** ci sono carte salvate e si apre la lista
- **THEN** le carte sono disposte in una griglia a 2 colonne di riquadri con angoli arrotondati

#### Scenario: Empty state invariato

- **WHEN** non ci sono carte salvate
- **THEN** viene mostrato il messaggio di lista vuota ("Nessuna carta ancora")

### Requirement: Contenuto del riquadro

Ogni riquadro SHALL mostrare il nome della carta e, quando presente, l'emittente. Il testo SHALL usare un colore a contrasto leggibile rispetto allo sfondo del riquadro.

#### Scenario: Nome e emittente

- **WHEN** un riquadro rappresenta una carta con nome ed emittente
- **THEN** il riquadro mostra il nome e l'emittente in modo leggibile

#### Scenario: Solo nome

- **WHEN** un riquadro rappresenta una carta senza emittente
- **THEN** il riquadro mostra almeno il nome, senza spazi vuoti fuorvianti

### Requirement: Colore di sfondo generato per carta

Il sistema SHALL usare come colore di sfondo del riquadro il **colore scelto dall'utente** per quella carta, quando presente. In assenza di una scelta esplicita il sistema SHALL assegnare un colore derivato in modo **deterministico** dal nome della carta, dalla stessa palette definita: la stessa carta SHALL produrre sempre lo stesso colore, e carte con nomi diversi SHALL distribuirsi sulla palette. La regola SHALL valere allo stesso modo per la griglia principale e per la barra delle carte usate di recente.

#### Scenario: Colore scelto dall'utente

- **WHEN** una carta ha un colore scelto dall'utente
- **THEN** il suo riquadro usa quel colore, sia nella griglia sia nella barra delle carte usate di recente

#### Scenario: Colore stabile per la stessa carta

- **WHEN** la stessa carta senza colore scelto viene mostrata in momenti diversi
- **THEN** il suo riquadro ha sempre lo stesso colore di sfondo

#### Scenario: Colore derivato dal nome

- **WHEN** vengono mostrate carte senza colore scelto e con nomi diversi
- **THEN** i colori dei riquadri sono scelti dalla palette in base al nome (non tutti uguali)

#### Scenario: Carte esistenti invariate

- **WHEN** l'app viene aggiornata e nessuna carta ha ancora un colore scelto
- **THEN** tutti i riquadri mantengono esattamente il colore che avevano prima dell'aggiornamento

#### Scenario: Leggibilità del testo

- **WHEN** un riquadro usa un colore scelto dall'utente dalla palette
- **THEN** il testo del riquadro resta leggibile come sui colori assegnati automaticamente

### Requirement: Bottone flottante per aggiungere una carta

La lista carte SHALL presentare l'azione di aggiunta carta come un **bottone tondo** con il simbolo `+`, posizionato **in basso al centro** della pagina e **sovrapposto** al contenuto della lista, così da non ridurre lo spazio verticale disponibile per la griglia. Il bottone MUST restare visibile e nella stessa posizione mentre si scorre la lista, MUST essere presente anche quando la lista è vuota o filtrata a zero risultati, e MUST usare il colore d'accento di brand con il simbolo a contrasto leggibile in tema chiaro e scuro. Il tocco SHALL aprire lo stesso flusso di acquisizione carta raggiunto in precedenza dalla toolbar, senza alcuna modifica al flusso stesso.

#### Scenario: Bottone presente in basso al centro

- **WHEN** l'utente apre la lista carte
- **THEN** in basso al centro compare un bottone tondo con il simbolo `+`, sopra il contenuto della lista

#### Scenario: Il bottone apre l'acquisizione carta

- **WHEN** l'utente tocca il bottone tondo `+`
- **THEN** si apre la schermata di scansione/acquisizione di una nuova carta, come faceva la voce "Aggiungi" della toolbar

#### Scenario: Posizione stabile durante lo scorrimento

- **WHEN** l'utente scorre la griglia delle carte
- **THEN** il bottone resta fermo in basso al centro e tocabile, senza scorrere via con la lista

#### Scenario: Disponibile anche a lista vuota

- **WHEN** non ci sono carte salvate, oppure ricerca e filtri non producono risultati
- **THEN** il bottone tondo `+` è comunque presente e permette di aggiungere una carta

#### Scenario: La griglia non perde altezza

- **WHEN** si confronta lo spazio verticale occupato dalla griglia prima e dopo l'introduzione del bottone
- **THEN** la griglia dispone della stessa altezza utile (il bottone è sovrapposto, non incolonnato sotto la lista)

### Requirement: Toolbar della lista senza la voce "Aggiungi"

La toolbar della lista carte SHALL NOT contenere una voce testuale "Aggiungi": l'aggiunta carta è raggiungibile solo dal bottone flottante. La voce **Impostazioni** e il suo segnale di aggiornamento disponibile MUST restare invariati.

#### Scenario: Nessuna voce "Aggiungi" in toolbar

- **WHEN** l'utente guarda la toolbar della lista carte
- **THEN** non compare la voce "Aggiungi"; l'aggiunta di una carta si fa dal bottone tondo in basso

#### Scenario: Impostazioni e badge invariati

- **WHEN** è disponibile un aggiornamento e l'utente guarda la toolbar
- **THEN** la voce Impostazioni è presente col suo badge di aggiornamento e si comporta come prima
