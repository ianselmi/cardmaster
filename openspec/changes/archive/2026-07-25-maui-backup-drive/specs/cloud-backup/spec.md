## ADDED Requirements

### Requirement: Backup su Google Drive opt-in

Il sistema SHALL offrire un backup del database su Google Drive come funzione **opt-in**, disattivata di default. L'abilitazione SHALL richiedere l'autenticazione dell'utente al proprio account Google. Il core dell'app (aprire, scansionare, condividere carte) SHALL continuare a funzionare interamente offline anche quando il backup è disabilitato o l'autenticazione fallisce.

#### Scenario: Abilitazione con autenticazione riuscita

- **WHEN** l'utente abilita il backup e completa l'autenticazione Google
- **THEN** il sistema memorizza le credenziali in modo sicuro e segna il backup come abilitato

#### Scenario: Autenticazione annullata

- **WHEN** l'utente avvia l'abilitazione ma annulla il consenso Google
- **THEN** il backup resta disabilitato e l'app non registra alcun account, senza errori

#### Scenario: Core offline indipendente dal backup

- **WHEN** il backup è disabilitato oppure il device è offline
- **THEN** l'utente può comunque aprire, scansionare e condividere le carte senza alcuna degradazione

### Requirement: Disabilitazione del backup senza perdita del cloud

Il sistema SHALL permettere all'utente di disabilitare il backup. Alla disabilitazione il sistema SHALL rimuovere le credenziali locali e annullare qualsiasi backup schedulato, ma NON MUST cancellare i backup già presenti su Google Drive.

#### Scenario: Disconnessione dell'account

- **WHEN** l'utente disabilita il backup
- **THEN** il sistema elimina le credenziali locali e disattiva la schedulazione

#### Scenario: I backup nel cloud sopravvivono alla disconnessione

- **WHEN** l'utente disabilita il backup
- **THEN** i file di backup già caricati su Google Drive restano disponibili per un futuro ripristino

### Requirement: Ambito di accesso minimo a Google Drive

Il sistema SHALL richiedere il **minimo** ambito necessario: accesso alla sola cartella applicativa nascosta di Drive (`appdata`) più l'identità dell'account (email) per mostrarla all'utente. Il sistema MUST NOT richiedere accesso in lettura o scrittura al resto dei file dell'utente su Drive.

#### Scenario: Solo cartella applicativa

- **WHEN** il sistema richiede l'autorizzazione a Google
- **THEN** l'ambito concesso consente di operare esclusivamente sulla cartella applicativa nascosta e di leggere l'email dell'account

#### Scenario: Nessun accesso ai file dell'utente

- **WHEN** il backup è attivo
- **THEN** il sistema non accede ad alcun file di Drive al di fuori della propria cartella applicativa

### Requirement: Esecuzione del backup

Il sistema SHALL produrre uno **snapshot consistente** dell'intero database e caricarlo come singolo file nella cartella applicativa di Drive. Il file caricato SHALL avere un nome che ne indica data/ora e **versione di schema**. Lo snapshot MUST essere coerente anche se l'app sta operando sul database (nessuna corruzione da scrittura concorrente).

#### Scenario: Backup manuale riuscito

- **WHEN** l'utente sceglie "Fai backup ora" con backup abilitato e rete disponibile
- **THEN** il sistema carica su Drive uno snapshot consistente del database e aggiorna la data/dimensione dell'ultimo backup

#### Scenario: Backup senza rete

- **WHEN** l'utente avvia un backup ma il device è offline
- **THEN** il sistema non carica nulla, segnala l'esito fallito e l'app resta stabile

#### Scenario: Snapshot consistente

- **WHEN** viene creato lo snapshot del database
- **THEN** il file risultante è una copia integra e apribile, senza corruzione dovuta a operazioni in corso

### Requirement: Ritenzione dei backup

Il sistema SHALL conservare nella cartella applicativa di Drive al massimo gli **ultimi 3** backup. Quando un nuovo backup porta il totale oltre 3, il sistema SHALL eliminare i backup più vecchi fino a rientrare nel limite.

#### Scenario: Rotazione oltre il limite

- **WHEN** un nuovo backup viene completato e sono già presenti 3 backup
- **THEN** il sistema elimina il backup più vecchio così da conservarne al massimo 3

#### Scenario: Sotto il limite nessuna eliminazione

- **WHEN** un nuovo backup viene completato e sono presenti meno di 3 backup
- **THEN** il sistema non elimina alcun backup esistente

### Requirement: Informazioni di stato del backup

Il sistema SHALL mostrare all'utente: l'account collegato, la **data e la dimensione dell'ultimo backup**, e lo **spazio disponibile** sull'account Google Drive. Questi valori SHALL essere aggiornati dopo ogni backup e all'apertura della sezione; in assenza di rete SHALL essere mostrato l'ultimo valore noto.

#### Scenario: Dati aggiornati all'apertura

- **WHEN** l'utente apre la sezione backup con rete disponibile
- **THEN** il sistema mostra account, data/dimensione dell'ultimo backup e spazio disponibile aggiornati

#### Scenario: Spazio illimitato

- **WHEN** l'account Google non espone un limite di spazio
- **THEN** il sistema mostra lo spazio come illimitato anziché un valore errato

#### Scenario: Valori dalla cache offline

- **WHEN** l'utente apre la sezione backup senza rete
- **THEN** il sistema mostra gli ultimi valori noti senza bloccarsi né mostrare errori bloccanti

### Requirement: Ripristino da un backup precedente

Il sistema SHALL elencare **in-app** i backup disponibili su Drive (data e dimensione) e permettere all'utente di sceglierne uno da ripristinare. Il ripristino SHALL **sostituire l'intero database** con il contenuto del backup scelto. Prima di sostituire, il sistema SHALL chiedere una **conferma esplicita** dell'operazione distruttiva. Il sistema MUST NOT ripristinare un backup con **versione di schema più recente** di quella supportata dall'app installata.

#### Scenario: Lista dei backup in-app

- **WHEN** l'utente apre "Ripristina da un backup…"
- **THEN** il sistema mostra l'elenco dei backup disponibili con data e dimensione, letti dalla cartella applicativa di Drive

#### Scenario: Ripristino con conferma

- **WHEN** l'utente sceglie un backup e conferma la sostituzione
- **THEN** il sistema scarica il backup, sostituisce l'intero database e i dati ripristinati diventano visibili nell'app

#### Scenario: Conferma richiesta prima della sostituzione

- **WHEN** l'utente sceglie un backup ma non conferma
- **THEN** il database corrente resta invariato

#### Scenario: Blocco del downgrade di schema

- **WHEN** l'utente sceglie un backup la cui versione di schema è più recente di quella dell'app installata
- **THEN** il sistema rifiuta il ripristino con un messaggio chiaro e lascia invariato il database corrente

### Requirement: Snapshot di sicurezza prima del ripristino

Prima di sostituire il database durante un ripristino, il sistema SHALL creare uno **snapshot di sicurezza** del database corrente in locale. In caso di errore durante il ripristino, oppure se l'utente sceglie di annullare subito dopo, il sistema SHALL poter **ripristinare lo stato precedente** da questo snapshot di sicurezza.

#### Scenario: Snapshot creato prima della sostituzione

- **WHEN** l'utente conferma un ripristino
- **THEN** il sistema crea uno snapshot locale del database corrente prima di sostituirlo

#### Scenario: Annullamento del ripristino

- **WHEN** un ripristino appena eseguito viene annullato dall'utente, oppure fallisce a metà
- **THEN** il sistema riporta il database allo stato precedente usando lo snapshot di sicurezza

### Requirement: Un solo account Google alla volta

Il sistema SHALL gestire **un solo account Google collegato alla volta**. Il collegamento di un account diverso SHALL richiedere prima la disconnessione dell'account corrente. Il sistema MUST NOT mantenere collegati più account simultaneamente.

#### Scenario: Cambio account tramite disconnessione

- **WHEN** l'utente vuole collegare un account diverso da quello corrente
- **THEN** il sistema richiede prima la disconnessione dell'account corrente e poi consente il nuovo collegamento

#### Scenario: Nessun collegamento simultaneo

- **WHEN** un account è già collegato
- **THEN** il sistema non consente di collegarne un secondo senza prima disconnettere il primo

### Requirement: Schedulazione del backup

Il sistema SHALL permettere di scegliere la frequenza del backup automatico tra: **Mai**, **A ogni apertura**, **Giornaliero**, **Settimanale**. I backup periodici SHALL essere eseguiti in background quando è disponibile la rete. Alla disabilitazione del backup la schedulazione SHALL essere annullata.

#### Scenario: Selezione della frequenza

- **WHEN** l'utente imposta una frequenza diversa da "Mai"
- **THEN** il sistema pianifica i backup automatici secondo quella frequenza

#### Scenario: Backup a ogni apertura

- **WHEN** la frequenza è "A ogni apertura" e l'app viene avviata con backup abilitato
- **THEN** il sistema esegue un backup se la rete è disponibile

#### Scenario: Disattivazione della schedulazione

- **WHEN** l'utente imposta "Mai" oppure disabilita il backup
- **THEN** il sistema non esegue più backup automatici

### Requirement: Notifica di avanzamento del backup

Durante l'esecuzione di un backup il sistema SHALL mostrare una **notifica di avanzamento** ("Backup in corso…") e, al termine, una notifica di **completamento** o di esito negativo. La notifica di avanzamento SHALL essere presente sia per i backup manuali sia per quelli schedulati in background.

#### Scenario: Notifica durante il backup

- **WHEN** un backup è in esecuzione
- **THEN** l'utente vede una notifica che indica che il backup è in corso

#### Scenario: Notifica di completamento

- **WHEN** un backup termina
- **THEN** la notifica riflette l'esito (completato o fallito)

### Requirement: Robustezza degli errori di rete e autenticazione

Le operazioni verso Google Drive SHALL gestire in modo controllato assenza di rete, errori del servizio e credenziali scadute/revocate, **senza mai far crashare l'app**. In caso di credenziali non più valide il sistema SHALL richiedere una nuova autenticazione invece di fallire silenziosamente.

#### Scenario: Errore di rete gestito

- **WHEN** un'operazione di backup o ripristino fallisce per rete o servizio non disponibile
- **THEN** il sistema mostra un messaggio di errore e l'app resta stabile

#### Scenario: Credenziali revocate

- **WHEN** un'operazione fallisce perché le credenziali Google sono scadute o revocate
- **THEN** il sistema propone di ripetere l'autenticazione anziché fallire silenziosamente
