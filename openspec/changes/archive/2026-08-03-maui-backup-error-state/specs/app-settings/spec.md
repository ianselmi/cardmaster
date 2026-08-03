## MODIFIED Requirements

### Requirement: Sezione Backup su Google Drive nelle Impostazioni

Il sistema SHALL fornire, all'interno delle Impostazioni, una **sezione dedicata al backup su Google Drive**, raggiungibile dalla pagina Impostazioni. La sezione SHALL esporre lo stato del backup (abilitato/disabilitato e account collegato), le informazioni di stato (esito dell'ultimo tentativo, data/dimensione dell'ultimo backup riuscito, spazio disponibile su Drive, frequenza) e le azioni di **abilita/disabilita**, **"Fai backup ora"**, **"Ripristina da un backup…"** e scelta della **frequenza**. Il comportamento di dettaglio di queste operazioni è definito nella capability `cloud-backup`. Il pulsante che apre la sezione dalla pagina Impostazioni SHALL segnalare visivamente, senza dover entrare nella sezione, se il backup è attualmente attivo o no (es. sottotitolo di stato e/o stile del pulsante diverso), riflettendo lo stato corrente della preferenza di abilitazione. Il pulsante SHALL inoltre distinguere il caso di backup **attivo ma non funzionante** (ultimo tentativo fallito o riconnessione necessaria) da quello di backup attivo e funzionante, così che il problema sia percepibile dalla pagina Impostazioni.

#### Scenario: Apertura della sezione backup

- **WHEN** l'utente apre la sezione "Backup su Google Drive" dalle Impostazioni
- **THEN** il sistema mostra lo stato del backup, le informazioni di stato e le azioni disponibili

#### Scenario: Stato disabilitato di default

- **WHEN** l'utente apre la sezione backup senza aver mai abilitato il backup
- **THEN** la sezione mostra il backup come disabilitato e offre l'azione per abilitarlo

#### Scenario: Azioni riflettono lo stato

- **WHEN** il backup è abilitato con un account collegato
- **THEN** la sezione mostra l'account, le informazioni dell'ultimo backup e rende disponibili le azioni di backup, ripristino, scelta frequenza e disabilitazione

#### Scenario: Segnale di stato sul pulsante quando il backup è attivo

- **WHEN** l'utente apre la pagina Impostazioni e il backup su Google Drive è abilitato
- **THEN** il pulsante "Backup su Google Drive" mostra un segnale visivo di stato attivo, senza che l'utente debba aprire la sezione dedicata

#### Scenario: Nessun segnale di stato quando il backup non è attivo

- **WHEN** l'utente apre la pagina Impostazioni e il backup su Google Drive non è mai stato abilitato o è stato disabilitato
- **THEN** il pulsante "Backup su Google Drive" non mostra alcun segnale di stato attivo

#### Scenario: Il segnale riflette un cambiamento di stato al ritorno da Backup

- **WHEN** l'utente abilita o disabilita il backup dalla sezione dedicata e torna alla pagina Impostazioni
- **THEN** il pulsante "Backup su Google Drive" mostra il segnale di stato coerente con la nuova preferenza

#### Scenario: Segnale di backup non funzionante

- **WHEN** l'utente apre la pagina Impostazioni con il backup abilitato ma l'ultimo tentativo fallito o in stato di riconnessione necessaria
- **THEN** il pulsante "Backup su Google Drive" segnala il problema in modo distinguibile dallo stato attivo e funzionante

#### Scenario: Il segnale di problema sparisce dopo un backup riuscito

- **WHEN** dopo un fallimento l'utente esegue un backup riuscito e torna alla pagina Impostazioni
- **THEN** il pulsante "Backup su Google Drive" torna a mostrare il segnale di stato attivo e funzionante
