## ADDED Requirements

### Requirement: Catalogo emittenti come seed statico bundle

Il sistema SHALL includere nell'app un catalogo di emittenti come dato statico (bundle), senza alcuna sincronizzazione o accesso di rete. Ogni emittente MUST avere un identificativo stabile e i metadati per la presentazione: nome visualizzato, colore, formato barcode atteso (opzionale) e riferimento al logo (opzionale).

#### Scenario: Catalogo disponibile offline

- **WHEN** l'app viene usata senza connessione di rete
- **THEN** il catalogo emittenti è disponibile e leggibile

#### Scenario: Ogni emittente ha id stabile e metadati

- **WHEN** si legge un emittente dal catalogo
- **THEN** l'emittente espone un id stabile, un nome visualizzato, un colore e (se presenti) formato barcode atteso e riferimento al logo

#### Scenario: Nessun accesso di rete

- **WHEN** il catalogo viene caricato
- **THEN** il caricamento avviene esclusivamente da asset locali dell'app, senza richieste di rete

### Requirement: Seed versionato

Il file seed del catalogo SHALL includere un campo di versione dello schema, così che evoluzioni future del formato (o la sostituzione con un catalogo sincronizzato in v2) siano gestibili in modo retrocompatibile.

#### Scenario: Versione presente nel seed

- **WHEN** si ispeziona il file seed del catalogo
- **THEN** è presente un campo `version` che identifica la versione dello schema del catalogo

### Requirement: Lookup e matching degli emittenti

Il sistema SHALL fornire un servizio che carica il catalogo e permette di: ottenere l'elenco completo, cercare un emittente per id, e cercare un emittente per nome o alias (match case-insensitive). Il servizio SHALL caricare il seed una sola volta (caricamento idempotente).

#### Scenario: Elenco completo

- **WHEN** si richiede l'elenco degli emittenti
- **THEN** il servizio restituisce tutti gli emittenti del seed

#### Scenario: Ricerca per id

- **WHEN** si cerca un emittente per un id esistente
- **THEN** il servizio restituisce l'emittente corrispondente

#### Scenario: Ricerca per id inesistente

- **WHEN** si cerca un emittente per un id non presente nel catalogo
- **THEN** il servizio restituisce nessun risultato (null) senza errori

#### Scenario: Match per nome o alias

- **WHEN** si cerca un emittente con un testo che corrisponde al suo nome o a uno dei suoi alias, ignorando maiuscole/minuscole
- **THEN** il servizio restituisce l'emittente corrispondente

#### Scenario: Nessuna corrispondenza nel match

- **WHEN** si cerca un emittente con un testo che non corrisponde ad alcun nome o alias
- **THEN** il servizio restituisce nessun risultato (null) senza errori, così che l'emittente possa restare libero o assente

#### Scenario: Caricamento idempotente

- **WHEN** il servizio viene interrogato più volte
- **THEN** il seed viene caricato una sola volta e le interrogazioni successive riusano i dati già caricati
