## MODIFIED Requirements

### Requirement: Navigazione con Shell

Il sistema SHALL usare .NET MAUI Shell per la navigazione e SHALL presentare le funzioni principali come **sezioni di primo livello** selezionabili dall'utente: la **lista carte**, mostrata all'avvio, e gli **scontrini**. Il passaggio da una sezione all'altra MUST NOT alterare lo stato dell'altra sezione.

#### Scenario: Pagina iniziale mostrata

- **WHEN** l'app si avvia
- **THEN** viene mostrata la sezione lista carte

#### Scenario: Struttura di navigazione estendibile

- **WHEN** una nuova pagina viene registrata come rotta di navigazione
- **THEN** è raggiungibile tramite la navigazione Shell senza modificare l'infrastruttura esistente

#### Scenario: Passaggio tra sezioni di primo livello

- **WHEN** l'utente passa dalla lista carte agli scontrini e torna indietro
- **THEN** entrambe le sezioni sono raggiungibili direttamente e la lista carte si ripresenta senza perdere ricerca e filtri attivi
