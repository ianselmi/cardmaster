## ADDED Requirements

### Requirement: Sezione Backup su Google Drive nelle Impostazioni

Il sistema SHALL fornire, all'interno delle Impostazioni, una **sezione dedicata al backup su Google Drive**, raggiungibile dalla pagina Impostazioni. La sezione SHALL esporre lo stato del backup (abilitato/disabilitato e account collegato), le informazioni di stato (data/dimensione dell'ultimo backup, spazio disponibile su Drive, frequenza) e le azioni di **abilita/disabilita**, **"Fai backup ora"**, **"Ripristina da un backup…"** e scelta della **frequenza**. Il comportamento di dettaglio di queste operazioni è definito nella capability `cloud-backup`.

#### Scenario: Apertura della sezione backup

- **WHEN** l'utente apre la sezione "Backup su Google Drive" dalle Impostazioni
- **THEN** il sistema mostra lo stato del backup, le informazioni di stato e le azioni disponibili

#### Scenario: Stato disabilitato di default

- **WHEN** l'utente apre la sezione backup senza aver mai abilitato il backup
- **THEN** la sezione mostra il backup come disabilitato e offre l'azione per abilitarlo

#### Scenario: Azioni riflettono lo stato

- **WHEN** il backup è abilitato con un account collegato
- **THEN** la sezione mostra l'account, le informazioni dell'ultimo backup e rende disponibili le azioni di backup, ripristino, scelta frequenza e disabilitazione
