## MODIFIED Requirements

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
