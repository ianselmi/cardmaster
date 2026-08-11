# cloud-backup Specification

## Purpose
TBD - created by syncing change maui-backup-drive. Update Purpose after archive.

## Requirements

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

### Requirement: Ritenzione dei backup

Il sistema SHALL conservare nella cartella applicativa di Drive al massimo gli **ultimi 3** backup. Quando un nuovo backup porta il totale oltre 3, il sistema SHALL eliminare i backup più vecchi fino a rientrare nel limite.

#### Scenario: Rotazione oltre il limite

- **WHEN** un nuovo backup viene completato e sono già presenti 3 backup
- **THEN** il sistema elimina il backup più vecchio così da conservarne al massimo 3

#### Scenario: Sotto il limite nessuna eliminazione

- **WHEN** un nuovo backup viene completato e sono presenti meno di 3 backup
- **THEN** il sistema non elimina alcun backup esistente

### Requirement: Informazioni di stato del backup

Il sistema SHALL mostrare all'utente: l'account collegato, l'**esito dell'ultimo tentativo di backup**, la **data e la dimensione dell'ultimo backup riuscito**, e lo **spazio disponibile** sull'account Google Drive. Questi valori SHALL essere aggiornati dopo ogni backup e all'apertura della sezione; in assenza di rete SHALL essere mostrato l'ultimo valore noto. Quando l'ultimo tentativo è fallito, lo stato mostrato SHALL renderlo evidente a colpo d'occhio, distinguendolo dallo stato normale.

#### Scenario: Dati aggiornati all'apertura

- **WHEN** l'utente apre la sezione backup con rete disponibile
- **THEN** il sistema mostra account, esito dell'ultimo tentativo, data/dimensione dell'ultimo backup riuscito e spazio disponibile aggiornati

#### Scenario: Spazio illimitato

- **WHEN** l'account Google non espone un limite di spazio
- **THEN** il sistema mostra lo spazio come illimitato anziché un valore errato

#### Scenario: Valori dalla cache offline

- **WHEN** l'utente apre la sezione backup senza rete
- **THEN** il sistema mostra gli ultimi valori noti senza bloccarsi né mostrare errori bloccanti

#### Scenario: Stato di errore evidente

- **WHEN** l'utente apre la sezione backup e l'ultimo tentativo è fallito
- **THEN** lo stato di fallimento è distinguibile a colpo d'occhio da quello di un backup riuscito

### Requirement: Esito dell'ultimo backup persistito e visibile

Il sistema SHALL registrare in modo persistente l'**esito dell'ultimo tentativo di backup** — riuscito o fallito, con data/ora del tentativo e, in caso di fallimento, la **categoria dell'errore** — e SHALL mostrarlo nella sezione backup come stato permanente, non solo come messaggio momentaneo. L'esito SHALL essere registrato allo stesso modo per i backup manuali, quelli all'apertura e quelli **schedulati in background**. Un backup riuscito SHALL azzerare lo stato di errore precedente.

#### Scenario: Fallimento visibile anche dopo la chiusura del messaggio

- **WHEN** un backup manuale fallisce e l'utente chiude il messaggio di errore
- **THEN** la sezione backup continua a mostrare che l'ultimo backup non è riuscito, con la data/ora del tentativo

#### Scenario: Fallimento di un backup schedulato

- **WHEN** un backup schedulato in background fallisce mentre l'app non è in primo piano
- **THEN** alla successiva apertura della sezione backup l'utente vede che l'ultimo tentativo è fallito, con data/ora e motivo

#### Scenario: Esito persistito tra i riavvii

- **WHEN** l'ultimo tentativo di backup è fallito e l'utente riavvia l'app
- **THEN** la sezione backup mostra ancora lo stato di fallimento

#### Scenario: Ritorno allo stato di normalità

- **WHEN** dopo uno o più fallimenti un backup viene completato con successo
- **THEN** il sistema rimuove lo stato di errore e mostra l'ultimo backup come riuscito

#### Scenario: Distinzione tra ultimo backup riuscito e ultimo tentativo

- **WHEN** l'ultimo tentativo è fallito ma esiste un backup riuscito precedente
- **THEN** la sezione mostra sia la data dell'ultimo backup riuscito sia il fallimento dell'ultimo tentativo, senza far credere che i dati siano aggiornati

### Requirement: Ripristino da un backup precedente

Il sistema SHALL elencare **in-app** i backup disponibili su Drive (data e dimensione) e permettere all'utente di sceglierne uno da ripristinare. Il ripristino SHALL **sostituire l'intero database** con il contenuto del backup scelto. Prima di sostituire, il sistema SHALL chiedere una **conferma esplicita** dell'operazione distruttiva, dichiarando che la situazione corrente viene prima salvata su Drive come backup. Il sistema MUST NOT ripristinare un backup con **versione di schema più recente** di quella supportata dall'app installata.

#### Scenario: Lista dei backup in-app

- **WHEN** l'utente apre "Ripristina da un backup…"
- **THEN** il sistema mostra l'elenco dei backup disponibili con data e dimensione, letti dalla cartella applicativa di Drive

#### Scenario: Ripristino con conferma

- **WHEN** l'utente sceglie un backup e conferma la sostituzione
- **THEN** il sistema salva su Drive la situazione corrente, scarica il backup, sostituisce l'intero database e i dati ripristinati diventano visibili nell'app

#### Scenario: Conferma richiesta prima della sostituzione

- **WHEN** l'utente sceglie un backup ma non conferma
- **THEN** il database corrente resta invariato e nessun backup viene caricato su Drive

#### Scenario: La conferma dichiara il backup della situazione corrente

- **WHEN** il sistema chiede conferma del ripristino
- **THEN** il messaggio dichiara che la situazione corrente viene salvata su Drive come backup prima della sostituzione

#### Scenario: Blocco del downgrade di schema

- **WHEN** l'utente sceglie un backup la cui versione di schema è più recente di quella dell'app installata
- **THEN** il sistema rifiuta il ripristino con un messaggio chiaro e lascia invariato il database corrente

### Requirement: Backup della situazione corrente prima del ripristino

Prima di sostituire il database durante un ripristino, il sistema SHALL eseguire un **backup ordinario della situazione corrente** nella cartella applicativa di Drive: stesso contenuto, stessa convenzione di nome e stessa ritenzione dei backup normali, così da comparire nella lista dei backup ed essere ripristinabile in qualsiasi momento come qualunque altro. Se questo backup non riesce, il sistema MUST NOT sostituire il database: SHALL interrompere il ripristino lasciando invariato il database corrente e SHALL mostrare il messaggio della categoria d'errore corrispondente.

#### Scenario: Backup della situazione corrente prima della sostituzione

- **WHEN** l'utente conferma un ripristino
- **THEN** il sistema carica su Drive un backup del database corrente e solo dopo sostituisce il database con il backup scelto

#### Scenario: Il backup pre-ripristino compare nella lista

- **WHEN** l'utente riapre la lista dei backup dopo un ripristino
- **THEN** il backup della situazione precedente al ripristino è elencato come tutti gli altri e può essere ripristinato a sua volta

#### Scenario: Backup pre-ripristino non riuscito

- **WHEN** il backup della situazione corrente fallisce (rete assente, spazio Drive esaurito, credenziali non più valide o errore locale)
- **THEN** il ripristino non viene eseguito, il database corrente resta invariato e l'utente vede il messaggio della categoria d'errore

#### Scenario: Il backup pre-ripristino rientra nella ritenzione

- **WHEN** il backup della situazione corrente porta il totale oltre il limite di ritenzione
- **THEN** il sistema elimina i backup più vecchi come per un backup qualsiasi

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

Le operazioni verso Google Drive SHALL gestire in modo controllato assenza di rete, errori del servizio e credenziali scadute/revocate, **senza mai far crashare l'app**. In caso di credenziali non più valide il sistema SHALL portarsi in uno stato dichiarato di "riconnessione necessaria", mostrarlo all'utente e offrire l'azione per ripetere l'autenticazione, invece di fallire silenziosamente o di presentarsi come pienamente operativo. Il sistema MUST NOT restare in uno stato in cui l'utente crede che i backup automatici stiano avvenendo mentre in realtà ogni tentativo fallisce.

#### Scenario: Errore di rete gestito

- **WHEN** un'operazione di backup o ripristino fallisce per rete o servizio non disponibile
- **THEN** il sistema mostra un messaggio di errore comprensibile e l'app resta stabile

#### Scenario: Credenziali revocate

- **WHEN** un'operazione fallisce perché le credenziali Google sono scadute o revocate
- **THEN** il sistema entra in stato "riconnessione necessaria" e propone l'azione per ripetere l'autenticazione anziché fallire silenziosamente

#### Scenario: Stato di riconnessione persistente

- **WHEN** il sistema è in stato "riconnessione necessaria" e l'utente riapre la sezione backup in un momento successivo
- **THEN** lo stato è ancora dichiarato e l'azione di riconnessione è ancora offerta

#### Scenario: Credenziali non valide rilevate da un backup schedulato

- **WHEN** un backup schedulato fallisce per credenziali non più valide
- **THEN** il sistema registra lo stato "riconnessione necessaria" e lo rende visibile alla successiva apertura della sezione backup

### Requirement: Messaggi di errore per categoria comprensibile

Ogni fallimento di un'operazione di backup o ripristino SHALL essere classificato in una **categoria** e presentato all'utente con un messaggio in linguaggio comune che dica **cosa è successo** e **cosa può fare**. Le categorie SHALL coprire almeno: assenza di rete, spazio Google Drive esaurito, credenziali Google non più valide, servizio Drive non disponibile o errore del servizio, errore locale (creazione o sostituzione dello snapshot del database). Il sistema MUST NOT mostrare come messaggio principale il testo grezzo restituito dal servizio (codici HTTP, payload JSON).

#### Scenario: Backup senza rete

- **WHEN** un backup fallisce perché il dispositivo è offline
- **THEN** il messaggio dichiara che manca la connessione e che il backup verrà ritentato quando la rete torna disponibile

#### Scenario: Spazio Drive esaurito

- **WHEN** il caricamento fallisce perché lo spazio dell'account Google Drive è esaurito
- **THEN** il messaggio dichiara che lo spazio su Drive è finito e invita a liberarne

#### Scenario: Errore del servizio Drive

- **WHEN** il servizio Drive risponde con un errore non riconducibile a rete, spazio o credenziali
- **THEN** il messaggio dichiara che Google Drive non è al momento disponibile e invita a riprovare più tardi

#### Scenario: Nessun payload tecnico in primo piano

- **WHEN** un'operazione fallisce con una risposta di errore del servizio
- **THEN** il messaggio mostrato all'utente non contiene codici di stato HTTP né il corpo JSON della risposta

### Requirement: Riconnessione dell'account senza perdere la configurazione

Quando l'errore è dovuto a credenziali Google non più valide (scadute o revocate), il sistema SHALL offrire un'azione esplicita di **riconnessione dell'account**, che ripete il consenso Google mantenendo l'account collegato, la frequenza scelta e la cronologia dei backup già presenti su Drive. L'utente MUST NOT essere obbligato a disabilitare e riabilitare il backup per tornare operativo. Una riconnessione riuscita SHALL azzerare lo stato di errore; una riconnessione annullata o fallita SHALL lasciare invariati lo stato di errore e la configurazione.

#### Scenario: Riconnessione riuscita

- **WHEN** il backup è in stato "credenziali non più valide" e l'utente completa la riconnessione dell'account Google
- **THEN** il sistema torna operativo mantenendo frequenza e configurazione, e lo stato di errore viene rimosso

#### Scenario: Riconnessione annullata

- **WHEN** l'utente avvia la riconnessione ma annulla il consenso Google
- **THEN** la configurazione del backup resta invariata e lo stato "credenziali non più valide" resta mostrato

#### Scenario: La cronologia dei backup sopravvive alla riconnessione

- **WHEN** l'utente riconnette lo stesso account Google
- **THEN** i backup già presenti nella cartella applicativa di Drive restano elencabili e ripristinabili

#### Scenario: Riconnessione con un account diverso

- **WHEN** durante la riconnessione l'utente sceglie un account Google diverso da quello collegato
- **THEN** il sistema collega il nuovo account come unico account e lo stato mostrato riflette il nuovo account
