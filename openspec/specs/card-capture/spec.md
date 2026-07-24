# card-capture

## Purpose

Acquisizione di una carta fedeltà tramite scansione barcode (ML Kit) o inserimento manuale, con arricchimento opzionale dell'emittente dal catalogo, avviso duplicati e salvataggio locale nel database cifrato. Il flusso di conferma/salvataggio è riusabile in ricezione da `maui-share-qr`.

## Requirements

### Requirement: Scansione barcode con camera

Il sistema SHALL offrire una schermata di scansione con anteprima camera live che riconosce i formati EAN-13, EAN-8, UPC-A, UPC-E, Code128, Code39, ITF, Codabar, QR e PDF417. Alla **prima lettura valida** il sistema SHALL fermare la scansione e procedere alla schermata di conferma con barcode e formato pre-compilati.

#### Scenario: Rilevazione e stop alla prima lettura

- **WHEN** la camera aggancia un barcode di un formato supportato
- **THEN** la scansione si ferma e si apre la schermata di conferma con il valore del barcode e il formato rilevato già compilati

#### Scenario: Formati supportati

- **WHEN** viene inquadrato un barcode di uno dei formati supportati (EAN-13, EAN-8, UPC-A, UPC-E, Code128, Code39, ITF, Codabar, QR, PDF417)
- **THEN** il sistema lo riconosce e ne estrae valore e formato

#### Scenario: Formato non supportato ignorato

- **WHEN** viene inquadrato un barcode di un formato non incluso tra quelli supportati
- **THEN** il sistema non procede alla conferma (la lettura viene ignorata)

### Requirement: Gestione del permesso camera

Il sistema SHALL richiedere il permesso camera a runtime quando l'utente entra nella scansione. Se il permesso è negato, il sistema SHALL restare utilizzabile tramite l'inserimento manuale, senza bloccare l'app.

#### Scenario: Permesso concesso

- **WHEN** l'utente entra nella scansione e concede il permesso camera
- **THEN** l'anteprima camera si avvia e la scansione è operativa

#### Scenario: Permesso negato

- **WHEN** l'utente nega il permesso camera
- **THEN** viene mostrato un messaggio chiaro e resta disponibile l'inserimento manuale del barcode

### Requirement: Inserimento manuale del barcode

Il sistema SHALL permettere di inserire manualmente il barcode (valore + formato scelto tra quelli supportati) come percorso alternativo alla scansione, portando alla stessa schermata di conferma/modifica.

#### Scenario: Inserimento manuale

- **WHEN** l'utente sceglie l'inserimento manuale e digita un valore e seleziona un formato
- **THEN** si apre la schermata di conferma con quei dati, pronti per il salvataggio

### Requirement: Arricchimento opzionale dell'emittente

Il sistema SHALL consentire di associare la carta a un emittente in modo facoltativo: scelto dal catalogo, digitato liberamente, o assente. Se l'emittente è scelto dal catalogo, la carta SHALL ereditarne i metadati disponibili (colore, riferimento logo, formato barcode atteso). Un emittente libero o assente NON MUST impedire il salvataggio.

#### Scenario: Emittente dal catalogo

- **WHEN** l'utente seleziona un emittente presente nel catalogo
- **THEN** la carta eredita i metadati dell'emittente (colore, logo, formato atteso quando presenti)

#### Scenario: Emittente libero

- **WHEN** l'utente digita un nome di emittente non presente nel catalogo
- **THEN** la carta viene salvata con quel nome, senza arricchimento e senza errori

#### Scenario: Nessun emittente

- **WHEN** l'utente non indica alcun emittente
- **THEN** la carta viene salvata comunque, purché sia presente un nome visualizzato

### Requirement: Campi obbligatori per il salvataggio

Il sistema SHALL impedire il salvataggio se manca il valore del barcode, il formato o il nome visualizzato. Il nome visualizzato SHALL avere come default il nome dell'emittente quando questo è indicato.

#### Scenario: Salvataggio bloccato senza dati minimi

- **WHEN** l'utente tenta di salvare senza barcode, senza formato o senza nome visualizzato
- **THEN** il salvataggio è impedito e viene segnalato il campo mancante

#### Scenario: Nome di default dall'emittente

- **WHEN** l'utente seleziona un emittente e non ha ancora digitato un nome
- **THEN** il nome visualizzato viene impostato di default al nome dell'emittente (resta modificabile)

### Requirement: Avviso duplicati alla creazione

Il sistema SHALL verificare, prima di salvare, se esiste già una carta **attiva** (non tombstone) con lo stesso valore di barcode. In tal caso SHALL mostrare un avviso **non bloccante** che consente all'utente di aggiungere comunque o annullare.

#### Scenario: Barcode già presente

- **WHEN** l'utente conferma una carta il cui barcode coincide con una carta attiva esistente
- **THEN** viene mostrato un avviso ("Hai già questa carta") con la scelta di aggiungere comunque o annullare

#### Scenario: Barcode non presente

- **WHEN** il barcode non coincide con alcuna carta attiva
- **THEN** la carta viene salvata senza avvisi

### Requirement: Salvataggio locale della carta

Il sistema SHALL salvare la carta nel database locale cifrato con Id generato dal client, timestamp e semantica tombstone (come da capability local-storage). Dopo il salvataggio la carta SHALL comparire nella lista carte.

#### Scenario: Carta salvata e visibile

- **WHEN** l'utente conferma il salvataggio di una carta valida
- **THEN** la carta viene persistita con Id client-generato e compare nella lista carte

#### Scenario: Persistenza offline

- **WHEN** il salvataggio avviene senza connessione di rete
- **THEN** la carta viene comunque salvata localmente
