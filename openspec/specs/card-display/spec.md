# card-display

## Purpose

Apertura di una carta e visualizzazione del suo barcode a schermo intero (rendering, codice in chiaro, luminosità e keep-awake, avviso filtro luce blu), ottimizzata per la lettura alla cassa.
## Requirements
### Requirement: Apertura di una carta

Il sistema SHALL aprire una carta selezionandola dalla lista e SHALL mostrare una pagina dedicata alla sua visualizzazione.

#### Scenario: Tap su una carta

- **WHEN** l'utente tocca una carta nella lista
- **THEN** si apre la pagina di visualizzazione della carta corrispondente

#### Scenario: Carta non trovata

- **WHEN** si tenta di aprire una carta che non esiste più (o è un tombstone)
- **THEN** il sistema non va in errore e riporta l'utente alla lista

### Requirement: Rendering del barcode

Il sistema SHALL generare e mostrare il barcode della carta a partire dal suo valore e formato, per i formati supportati (EAN-13, EAN-8, UPC-A, UPC-E, Code128, Code39, ITF, Codabar, QR, PDF417). Il barcode SHALL essere reso nero su sfondo **bianco**, con dimensioni adeguate al tipo (1D largo, 2D quadrato), grande e centrato.

#### Scenario: Barcode 1D

- **WHEN** si apre una carta con un formato 1D (es. EAN-13, Code128)
- **THEN** viene mostrato il barcode lineare nero su bianco, largo e leggibile

#### Scenario: Barcode 2D

- **WHEN** si apre una carta con un formato 2D (QR, PDF417)
- **THEN** viene mostrato il codice 2D nero su bianco, con proporzioni adeguate

#### Scenario: Sfondo bianco anche in dark mode

- **WHEN** il dispositivo è in tema scuro
- **THEN** l'area del barcode resta comunque su sfondo bianco, per preservare il contrasto

### Requirement: Codice in chiaro sempre visibile

Il sistema SHALL mostrare sempre il valore del barcode in chiaro sotto l'immagine, così che il cassiere possa digitarlo se il lettore non aggancia lo schermo.

#### Scenario: Valore in chiaro presente

- **WHEN** si visualizza una carta
- **THEN** il valore del barcode è mostrato in chiaro, leggibile, insieme all'immagine

### Requirement: Gestione del barcode non generabile

Il sistema SHALL gestire senza crash il caso in cui il valore non sia generabile nel formato indicato (es. valore inserito manualmente non conforme). In tal caso SHALL mostrare comunque il codice in chiaro e un messaggio esplicativo, al posto dell'immagine.

#### Scenario: Valore non conforme al formato

- **WHEN** il valore della carta non può essere reso come barcode nel formato scelto
- **THEN** l'app non va in errore, non mostra l'immagine, e mostra il codice in chiaro con un messaggio che invita a comunicare il numero al cassiere

### Requirement: Luminosità e schermo attivo durante la visualizzazione

Mentre una carta è aperta, il sistema SHALL portare la luminosità dello schermo al massimo e SHALL impedire lo spegnimento dello schermo. All'uscita dalla pagina SHALL ripristinare lo spegnimento normale e riportare la luminosità al **default di sistema**.

#### Scenario: All'apertura

- **WHEN** si apre la pagina di visualizzazione di una carta
- **THEN** la luminosità va al massimo e lo schermo non si spegne automaticamente

#### Scenario: All'uscita

- **WHEN** si lascia la pagina di visualizzazione
- **THEN** lo spegnimento automatico torna normale e la luminosità torna al default di sistema

### Requirement: Avviso del filtro luce blu (best-effort)

Il sistema SHOULD rilevare, per quanto possibile, se è attivo un filtro luce blu / modalità notte di sistema e, in tal caso, SHALL mostrare un avviso non bloccante che suggerisce di disattivarlo per una lettura migliore. Se il filtro non è rilevabile sul dispositivo, il sistema NON MUST mostrare falsi avvisi.

#### Scenario: Filtro attivo e rilevato

- **WHEN** si apre una carta e il filtro luce blu risulta attivo (e rilevabile)
- **THEN** viene mostrato un avviso non bloccante che suggerisce di disattivarlo per una lettura migliore

#### Scenario: Filtro non rilevabile

- **WHEN** lo stato del filtro non è determinabile sul dispositivo
- **THEN** non viene mostrato alcun avviso (nessun falso allarme) e la visualizzazione procede normalmente

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

