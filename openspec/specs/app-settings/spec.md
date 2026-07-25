# app-settings Specification

## Purpose
TBD - created by archiving change maui-settings. Update Purpose after archive.
## Requirements
### Requirement: Pagina Impostazioni raggiungibile

Il sistema SHALL fornire una pagina **Impostazioni** dedicata, raggiungibile dalla lista carte tramite una voce di navigazione (toolbar). La pagina MUST aprirsi e chiudersi senza errori e MUST usare la navigazione Shell come le altre pagine dell'app.

#### Scenario: Apertura dalla lista carte
- **WHEN** l'utente tocca la voce "Impostazioni" dalla lista carte
- **THEN** il sistema apre la pagina Impostazioni

#### Scenario: Ritorno alla lista
- **WHEN** l'utente esce dalla pagina Impostazioni
- **THEN** il sistema riporta l'utente alla schermata precedente senza errori

### Requirement: Store persistente delle preferenze

Il sistema SHALL fornire un meccanismo unico per leggere e scrivere le preferenze dell'app come coppie chiave/valore **locali al device**, persistenti tra i riavvii. Le preferenze NON MUST richiedere rete né account. Una preferenza mai impostata SHALL restituire un valore di default definito.

#### Scenario: Persistenza tra riavvii
- **WHEN** l'utente imposta una preferenza e poi riavvia l'app
- **THEN** al riavvio la preferenza mantiene il valore impostato

#### Scenario: Valore di default
- **WHEN** una preferenza non è mai stata impostata
- **THEN** il sistema restituisce il valore di default previsto per quella preferenza

### Requirement: Informazioni sull'app

Il sistema SHALL mostrare nella pagina Impostazioni almeno il **nome** e la **versione** (versione visualizzata e/o build) dell'app, letti dalle informazioni di piattaforma.

#### Scenario: Versione visibile
- **WHEN** l'utente apre la pagina Impostazioni
- **THEN** vede il nome dell'app e la sua versione corrente

### Requirement: Preferenza del tema

Il sistema SHALL permettere all'utente di scegliere l'aspetto tra **Sistema**, **Chiaro** e **Scuro**. La scelta SHALL essere persistita nello store delle preferenze e SHALL essere applicata immediatamente e di nuovo all'avvio successivo dell'app. Il default SHALL essere **Sistema** (segue il tema del dispositivo).

#### Scenario: Cambio tema immediato
- **WHEN** l'utente seleziona "Chiaro" o "Scuro" nelle Impostazioni
- **THEN** l'aspetto dell'app cambia subito di conseguenza

#### Scenario: Tema persistito all'avvio
- **WHEN** l'utente ha scelto un tema e riavvia l'app
- **THEN** all'avvio l'app applica il tema salvato

#### Scenario: Default segue il sistema
- **WHEN** l'utente non ha mai cambiato il tema
- **THEN** l'app segue il tema chiaro/scuro del dispositivo

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

### Requirement: Sezione Controllo aggiornamenti nelle Impostazioni

Il sistema SHALL fornire, all'interno delle Impostazioni, una sezione dedicata al controllo degli aggiornamenti, raggiungibile dalla pagina Impostazioni. La sezione SHALL mostrare l'esito dell'ultimo controllo effettuato e SHALL esporre l'azione "Verifica aggiornamenti". Il comportamento di dettaglio del controllo, download e installazione è definito nella capability `app-update`.

#### Scenario: Apertura della sezione controllo aggiornamenti

- **WHEN** l'utente apre la sezione "Controllo aggiornamenti" dalle Impostazioni
- **THEN** il sistema mostra l'esito dell'ultimo controllo (se presente) e l'azione "Verifica aggiornamenti"

#### Scenario: Nessun controllo ancora effettuato

- **WHEN** l'utente apre la sezione senza aver mai avviato un controllo aggiornamenti
- **THEN** la sezione non mostra un esito precedente e offre comunque l'azione "Verifica aggiornamenti"

