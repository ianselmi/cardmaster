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

### Requirement: Sezione Controllo aggiornamenti nelle Impostazioni

Il sistema SHALL fornire, all'interno delle Impostazioni, una sezione dedicata al controllo degli aggiornamenti, raggiungibile dalla pagina Impostazioni. La sezione SHALL mostrare l'esito dell'ultimo controllo effettuato e SHALL esporre l'azione "Verifica aggiornamenti". La sezione SHALL inoltre esporre uno switch **"Avvisami di nuove versioni"** (default disattivato) per abilitare/disabilitare il controllo automatico opt-in, il segnale in-app e la **notifica di sistema**, il cui comportamento di dettaglio è definito dalla capability `app-update-notify`. La descrizione dello switch SHALL dichiarare che il controllo avviene anche **ad app chiusa**, così che l'utente sappia che sta abilitando un'attività di rete periodica. All'attivazione dello switch il sistema SHALL richiedere il permesso di inviare notifiche. Il comportamento di dettaglio del controllo, download e installazione è definito nella capability `app-update`.

L'esito mostrato NON MUST annunciare come disponibile una versione che coincide con quella **installata**: in quel caso la sezione SHALL riportare l'ultimo controllo come "nessun aggiornamento disponibile", conservando la data/ora in cui il controllo è avvenuto.

#### Scenario: Apertura della sezione controllo aggiornamenti

- **WHEN** l'utente apre la sezione "Controllo aggiornamenti" dalle Impostazioni
- **THEN** il sistema mostra l'esito dell'ultimo controllo (se presente), l'azione "Verifica aggiornamenti" e lo switch "Avvisami di nuove versioni"

#### Scenario: Nessun controllo ancora effettuato

- **WHEN** l'utente apre la sezione senza aver mai avviato un controllo aggiornamenti
- **THEN** la sezione non mostra un esito precedente e offre comunque l'azione "Verifica aggiornamenti", con lo switch "Avvisami di nuove versioni" disattivato di default

#### Scenario: Attivazione dello switch

- **WHEN** l'utente attiva lo switch "Avvisami di nuove versioni"
- **THEN** il sistema persiste la preferenza, richiede il permesso di inviare notifiche e da quel momento abilita il controllo automatico descritto da `app-update-notify`

#### Scenario: Descrizione dello switch esplicita sul controllo in background

- **WHEN** l'utente legge la descrizione dello switch "Avvisami di nuove versioni"
- **THEN** la descrizione dichiara che il controllo avviene anche ad app chiusa e che verrà mostrata una notifica

#### Scenario: Esito dopo l'installazione dell'aggiornamento annunciato

- **WHEN** l'ultimo controllo aveva annunciato la versione N e l'utente ha nel frattempo installato la versione N
- **THEN** la sezione riporta l'ultimo controllo come "nessun aggiornamento disponibile", con la data/ora del controllo realmente effettuato, e non annuncia la versione N come disponibile

