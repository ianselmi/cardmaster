## ADDED Requirements

### Requirement: Nome della carta dentro il riquadro del barcode

La pagina di dettaglio SHALL mostrare il **nome della carta dentro il riquadro** che contiene il barcode e il codice in chiaro, sopra il barcode. Il nome SHALL stare su uno **sfondo proprio**, limitato al testo, che lo distingua visivamente dal resto del riquadro; lo sfondo SHALL usare il **colore del riquadro della carta** definito da `card-list` (colore scelto dall'utente quando presente, altrimenti derivato dal nome) con testo a contrasto leggibile. Lo sfondo del nome MUST NOT estendersi all'area di rendering del barcode né al codice in chiaro, che MUST restare su fondo bianco anche in tema scuro. Un nome troppo lungo per una riga MUST restare leggibile senza deformare il riquadro né coprire il barcode.

#### Scenario: Nome dentro il riquadro

- **WHEN** l'utente apre una carta
- **THEN** il nome della carta compare dentro il riquadro del barcode, sopra il barcode, su una fascia con sfondo proprio

#### Scenario: Sfondo del nome col colore della carta

- **WHEN** una carta ha un colore del riquadro scelto dall'utente
- **THEN** la fascia del nome nel dettaglio usa quel colore, con il testo leggibile a contrasto

#### Scenario: Colore derivato in assenza di scelta

- **WHEN** una carta non ha un colore scelto dall'utente
- **THEN** la fascia del nome usa il colore derivato dal nome, lo stesso che il riquadro della carta ha nella griglia

#### Scenario: Fondo bianco del barcode invariato

- **WHEN** il device è in tema scuro e l'utente apre una carta
- **THEN** l'area del barcode e il codice in chiaro restano su fondo bianco, e solo la fascia del nome è colorata

#### Scenario: Nome lungo

- **WHEN** la carta ha un nome che non sta su una sola riga
- **THEN** il nome resta leggibile (va a capo o viene troncato in modo esplicito) senza allargare il riquadro oltre la pagina né sovrapporsi al barcode

#### Scenario: Titolo della pagina invariato

- **WHEN** l'utente apre una carta
- **THEN** la barra del titolo continua a mostrare il nome della carta come prima
