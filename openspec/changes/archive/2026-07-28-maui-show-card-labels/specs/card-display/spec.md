## ADDED Requirements

### Requirement: Label della carta visibili nel dettaglio

La pagina di dettaglio della carta SHALL mostrare le **label assegnate** alla carta, in **sola lettura**: da questa pagina le label MUST NOT essere modificabili o rimovibili. Quando la carta non ha nessuna label, la sezione MUST NOT occupare spazio né mostrare intestazioni o messaggi. La presentazione delle label MUST NOT alterare la posizione né la prominenza dell'area del barcode, che resta l'elemento principale della pagina. Le label mostrate SHALL riflettere lo stato corrente della carta, incluse le modifiche appena salvate dalla schermata di modifica.

#### Scenario: Carta con label

- **WHEN** l'utente apre una carta che ha una o più label assegnate
- **THEN** la pagina di dettaglio mostra tutte le label della carta

#### Scenario: Carta senza label

- **WHEN** l'utente apre una carta senza label
- **THEN** nella pagina di dettaglio non compare nessuna sezione label, nessuna intestazione e nessuno spazio vuoto aggiuntivo

#### Scenario: Sola lettura

- **WHEN** l'utente tocca una label nella pagina di dettaglio
- **THEN** la label non viene rimossa né modificata e la carta resta invariata

#### Scenario: Barcode invariato

- **WHEN** si confronta la pagina di dettaglio di una carta con label con quella di una carta senza label
- **THEN** l'area del barcode occupa la stessa posizione e la stessa dimensione in entrambi i casi

#### Scenario: Label aggiornate dopo una modifica

- **WHEN** l'utente modifica le label della carta dalla schermata di modifica, salva e torna al dettaglio
- **THEN** il dettaglio mostra le label aggiornate senza bisogno di riaprire la carta

#### Scenario: Molte label

- **WHEN** una carta ha un numero di label che non sta su una sola riga
- **THEN** le label vanno a capo e restano tutte leggibili, senza troncare la pagina né comprimere il barcode
