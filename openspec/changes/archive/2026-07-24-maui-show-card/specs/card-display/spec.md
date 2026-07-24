## ADDED Requirements

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
