## MODIFIED Requirements

### Requirement: Esecuzione del backup

Il sistema SHALL produrre uno **snapshot consistente** dell'intero database e caricarlo come singolo file nella cartella applicativa di Drive. Il file caricato SHALL avere un nome che ne indica data/ora e **versione di schema**. Lo snapshot MUST essere coerente anche se l'app sta operando sul database (nessuna corruzione da scrittura concorrente).

Il backup copre **soltanto il database**: i file conservati dall'app fuori dal database — in particolare le **immagini degli scontrini** — non sono inclusi e non vengono riportati indietro da un ripristino. Il sistema SHALL dichiarare questo limite all'utente nella sezione Backup, invece di lasciarlo scoprire dopo un ripristino.

#### Scenario: Backup manuale riuscito

- **WHEN** l'utente sceglie "Fai backup ora" con backup abilitato e rete disponibile
- **THEN** il sistema carica su Drive uno snapshot consistente del database e aggiorna la data/dimensione dell'ultimo backup

#### Scenario: Backup senza rete

- **WHEN** l'utente avvia un backup ma il device è offline
- **THEN** il sistema non carica nulla, segnala l'esito fallito e l'app resta stabile

#### Scenario: Snapshot consistente

- **WHEN** viene creato lo snapshot del database
- **THEN** il file risultante è una copia integra e apribile, senza corruzione dovuta a operazioni in corso

#### Scenario: Limite del backup dichiarato all'utente

- **WHEN** l'utente apre la sezione Backup
- **THEN** legge che le immagini degli scontrini non sono comprese nel backup e non tornano dopo un ripristino

#### Scenario: Ripristino con scontrini presenti

- **WHEN** l'utente ripristina un backup su un device dove le immagini degli scontrini non sono presenti
- **THEN** gli scontrini tornano con i dati di testata e il testo riconosciuto, mentre le immagini risultano assenti e lo scontrino resta consultabile e corretto
