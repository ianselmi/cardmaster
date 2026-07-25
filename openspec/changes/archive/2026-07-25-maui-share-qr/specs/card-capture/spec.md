## ADDED Requirements

### Requirement: Ricezione di una carta condivisa via scansione

Durante la scansione, il sistema SHALL riconoscere quando il QR inquadrato è un **payload di condivisione CardMaster** (identificato dal prefisso/versione dello schema) e, in tal caso, SHALL decodificarlo e aprire la schermata di conferma **pre-compilata con l'intero snapshot** ricevuto (nome, emittente, colore, logo, barcode, formato), anziché trattarlo come un barcode QR grezzo. Un QR che NON è un payload CardMaster SHALL continuare a essere trattato come un normale barcode QR fedeltà (comportamento esistente).

#### Scenario: QR di condivisione riconosciuto

- **WHEN** la camera aggancia un QR che è un payload di condivisione CardMaster valido
- **THEN** il sistema lo decodifica e apre la conferma pre-compilata con nome, emittente, colore, logo, barcode e formato dello snapshot

#### Scenario: QR fedeltà normale

- **WHEN** la camera aggancia un QR che non è un payload CardMaster
- **THEN** il sistema lo tratta come un normale barcode QR (conferma con solo valore e formato QR)

#### Scenario: Payload corrotto o versione non supportata

- **WHEN** la camera aggancia un QR con prefisso CardMaster ma payload corrotto o di versione non supportata
- **THEN** il sistema segnala che il codice non è leggibile e resta stabile, senza crashare né creare una carta

### Requirement: Carta ricevuta salvata come copia indipendente

Una carta ricevuta tramite QR di condivisione SHALL essere salvata come **nuova copia locale** (Id client-generato, timestamp, semantica tombstone), senza alcun legame persistente col device mittente. Prima del salvataggio il sistema SHALL applicare il consueto **avviso duplicati non bloccante** (stesso barcode di una carta attiva) proponendo di saltare invece di duplicare.

#### Scenario: Copia indipendente creata

- **WHEN** l'utente conferma il salvataggio di una carta ricevuta via QR
- **THEN** viene creata una nuova carta locale con Id client-generato, senza riferimenti al mittente, e compare nella lista carte

#### Scenario: Duplicato in ricezione

- **WHEN** la carta ricevuta ha lo stesso barcode di una carta attiva già presente
- **THEN** viene mostrato l'avviso duplicati non bloccante che consente di saltare o aggiungere comunque
