## ADDED Requirements

### Requirement: Azione di condivisione di una carta

Il sistema SHALL offrire, dalla pagina di visualizzazione di una carta, un'azione di **condivisione** che apre una schermata dedicata mostrando un **QR code** che incapsula lo snapshot completo della carta. La condivisione SHALL funzionare interamente offline, senza rete né account.

#### Scenario: Apertura della schermata di condivisione

- **WHEN** l'utente sceglie "Condividi" dalla pagina di una carta
- **THEN** si apre una schermata che mostra il QR code di condivisione di quella carta

#### Scenario: Condivisione senza rete

- **WHEN** l'utente apre la condivisione senza connessione di rete
- **THEN** il QR viene comunque generato e mostrato (nessuna dipendenza da server)

### Requirement: Payload self-contained e versionato

Il QR di condivisione SHALL contenere uno **snapshot completo e autosufficiente** della carta: nome visualizzato, emittente (se presente), valore del barcode, formato del barcode, colore e riferimento logo (se presenti). Il payload SHALL includere un **prefisso identificativo** (magic) che lo distingue da un normale QR fedeltà e un **numero di versione** dello schema, così da consentire l'evoluzione futura del formato. Il payload NON MUST contenere riferimenti remoti né l'Id locale della carta di origine.

#### Scenario: Snapshot completo

- **WHEN** viene generato il QR di una carta con nome, emittente, barcode, formato ed eventuali colore/logo
- **THEN** il payload codificato contiene tutti questi campi, sufficienti a ricostruire una copia della carta senza rete

#### Scenario: Payload riconoscibile e versionato

- **WHEN** viene generato un payload di condivisione
- **THEN** esso include il prefisso identificativo CardMaster e il numero di versione dello schema

#### Scenario: Nessun legame col mittente

- **WHEN** viene generato il payload
- **THEN** esso NON contiene l'Id locale della carta di origine né alcun riferimento che leghi la copia al device mittente

### Requirement: Rendering del QR con fallback

Il sistema SHALL rendere il payload come QR code tramite il renderer barcode esistente. Se il payload eccede la capacità del QR o non è generabile, il sistema SHALL mostrare un messaggio chiaro anziché fallire silenziosamente o crashare.

#### Scenario: QR generato

- **WHEN** il payload rientra nella capacità del QR
- **THEN** l'immagine del QR viene mostrata all'utente

#### Scenario: QR non generabile

- **WHEN** il payload non può essere codificato in un QR
- **THEN** viene mostrato un messaggio di errore e l'app resta stabile

### Requirement: Codec dello snapshot robusto

Il sistema SHALL fornire una serializzazione e deserializzazione dello snapshot che non lancia mai eccezioni verso il chiamante: un testo che non è un payload CardMaster, un payload corrotto o di **versione non supportata** SHALL produrre un esito di fallimento gestito, non un crash.

#### Scenario: Round-trip fedele

- **WHEN** uno snapshot viene serializzato e poi deserializzato
- **THEN** i campi ricostruiti coincidono con quelli di partenza

#### Scenario: Testo non CardMaster

- **WHEN** viene deserializzato un testo privo del prefisso identificativo CardMaster
- **THEN** il codec restituisce un esito "non riconosciuto" senza lanciare eccezioni

#### Scenario: Payload corrotto o versione non supportata

- **WHEN** viene deserializzato un payload CardMaster corrotto o con una versione di schema non supportata
- **THEN** il codec restituisce un esito di fallimento gestito e l'app non crasha
