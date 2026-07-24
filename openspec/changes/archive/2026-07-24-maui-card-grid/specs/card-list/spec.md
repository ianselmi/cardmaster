## ADDED Requirements

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

Il sistema SHALL assegnare a ogni riquadro un colore di sfondo derivato in modo **deterministico** dal nome della carta, da una palette definita. La stessa carta SHALL produrre sempre lo stesso colore; carte con nomi diversi SHALL distribuirsi sulla palette.

#### Scenario: Colore stabile per la stessa carta

- **WHEN** la stessa carta viene mostrata in momenti diversi
- **THEN** il suo riquadro ha sempre lo stesso colore di sfondo

#### Scenario: Colore derivato dal nome

- **WHEN** vengono mostrate carte con nomi diversi
- **THEN** i colori dei riquadri sono scelti dalla palette in base al nome (non tutti uguali)
