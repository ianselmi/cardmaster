# app-update-notify

## Purpose

Segnalazione della presenza di un aggiornamento fuori dalla pagina Impostazioni: opzione opt-in "Avvisami di nuove versioni" (default disattivata) che abilita un controllo automatico limitato in frequenza — in foreground e periodicamente ad app chiusa — badge e banner in-app e una **notifica di sistema**, tutti diretti al flusso di aggiornamento di `app-update`, più il silenziamento del segnale per versione. Non introduce infrastruttura propria: riusa il controllo di versione definito da `app-update`.

## Requirements

### Requirement: Opzione per il controllo automatico degli aggiornamenti

Il sistema SHALL fornire in Impostazioni un'opzione "Avvisami di nuove versioni" (default **disattivata**) che, se attivata, permette al sistema di controllare automaticamente la presenza di un aggiornamento — all'apertura dell'app e **periodicamente in background** — e di segnalarlo sia in-app sia con una **notifica di sistema**. Se l'opzione è disattivata, il sistema NON MUST effettuare alcun controllo di rete se non su richiesta esplicita dell'utente, né alcun lavoro periodico.

#### Scenario: Opzione disattivata (default)

- **WHEN** l'utente non ha mai attivato "Avvisami di nuove versioni" e apre l'app
- **THEN** il sistema non effettua alcun controllo automatico di rete e non registra alcun lavoro periodico

#### Scenario: Opzione attivata

- **WHEN** l'utente attiva "Avvisami di nuove versioni" nelle Impostazioni
- **THEN** da quel momento il sistema può effettuare controlli automatici come descritto dal requisito "Controllo automatico limitato in frequenza", e segnalare gli aggiornamenti anche con una notifica di sistema

#### Scenario: Opzione disattivata dopo essere stata attiva

- **WHEN** l'utente disattiva "Avvisami di nuove versioni" dopo averla attivata
- **THEN** il sistema smette di effettuare controlli automatici e annulla il lavoro periodico, senza rimuovere l'esito dell'ultimo controllo già mostrato in Impostazioni

### Requirement: Controllo automatico limitato in frequenza

Quando l'opzione è attivata, il sistema SHALL effettuare il controllo automatico sia quando l'app passa in foreground (avvio o ripresa da background) sia **periodicamente ad app chiusa**, tramite un lavoro pianificato con vincolo di **rete connessa**. In entrambi i casi il sistema SHALL evitare di ripetere il controllo se non è trascorsa almeno **1 ora** dall'ultimo controllo (manuale o automatico): l'intervallo minimo è unico e vale per tutte le strade, e SHALL essere allineato al periodo del lavoro pianificato — un minimo più lungo del periodo farebbe girare il lavoro a vuoto.

Il sistema NON MUST garantire la puntualità del controllo periodico: il sistema operativo può rimandarlo (risparmio energetico, assenza di rete) e questo SHALL essere accettabile senza effetti collaterali.

#### Scenario: App aperta entro l'intervallo minimo

- **WHEN** l'opzione è attivata e l'utente apre l'app meno di 1 ora dopo l'ultimo controllo
- **THEN** il sistema non effettua un nuovo controllo di rete

#### Scenario: App aperta dopo l'intervallo minimo

- **WHEN** l'opzione è attivata e l'utente apre l'app almeno 1 ora dopo l'ultimo controllo
- **THEN** il sistema effettua un controllo automatico riusando la stessa logica di verifica versione di `app-update`

#### Scenario: Controllo periodico ad app chiusa

- **WHEN** l'opzione è attivata, l'app non è in esecuzione, c'è rete ed è trascorsa almeno 1 ora dall'ultimo controllo
- **THEN** il sistema effettua il controllo in background e, se rileva un aggiornamento, emette la notifica di sistema

#### Scenario: Controllo periodico entro l'intervallo minimo

- **WHEN** il lavoro periodico viene eseguito ma un controllo è già avvenuto da meno di 1 ora
- **THEN** il sistema non effettua una nuova richiesta di rete

#### Scenario: Nessun lavoro periodico con opzione disattivata

- **WHEN** l'opzione non è attivata
- **THEN** il sistema non pianifica né esegue alcun controllo in background

#### Scenario: Controllo periodico rimandato dal sistema

- **WHEN** il sistema operativo rimanda l'esecuzione del lavoro periodico
- **THEN** il controllo avviene più tardi senza errori né segnalazioni all'utente

### Requirement: Segnale visibile di aggiornamento disponibile

Quando un controllo (manuale o automatico) rileva una versione remota diversa da quella installata, il sistema SHALL mostrare un segnale visibile fuori dalla pagina Impostazioni (es. badge sulla voce di navigazione "Impostazioni") che indica la disponibilità di un aggiornamento, finché non viene chiuso dall'utente o installato l'aggiornamento. Toccare il segnale SHALL portare l'utente al flusso di aggiornamento esistente (`app-update`).

Il segnale SHALL essere mostrato **solo se la versione rilevata è diversa da quella attualmente installata**: la sola presenza di un esito di controllo memorizzato NON MUST essere sufficiente a mostrarlo. La condizione SHALL essere rivalutata a ogni apertura dell'app, così che l'installazione dell'aggiornamento faccia sparire il segnale **senza richiedere un nuovo controllo di rete** né l'intervento dell'utente.

#### Scenario: Segnale mostrato dopo rilevazione

- **WHEN** un controllo rileva che la versione remota è diversa dalla versione installata
- **THEN** il sistema mostra il segnale di aggiornamento disponibile fuori dalla pagina Impostazioni

#### Scenario: Nessun segnale se nessun aggiornamento

- **WHEN** l'ultimo controllo effettuato non ha rilevato una versione remota diversa da quella installata
- **THEN** il sistema non mostra alcun segnale

#### Scenario: Attivazione del flusso di aggiornamento dal segnale

- **WHEN** l'utente tocca il segnale di aggiornamento disponibile
- **THEN** il sistema porta l'utente al flusso di controllo/download/installazione già definito da `app-update`

#### Scenario: Segnale sparito dopo l'installazione dell'aggiornamento

- **WHEN** l'utente installa l'aggiornamento segnalato e riapre l'app
- **THEN** né il banner né il badge segnalano più un aggiornamento disponibile, senza che l'utente debba chiudere il segnale o avviare un nuovo controllo

#### Scenario: Segnale sparito anche senza rete

- **WHEN** l'utente installa l'aggiornamento segnalato e riapre l'app senza connessione di rete
- **THEN** il segnale non viene mostrato ugualmente, perché la condizione si basa sulla versione installata e non su un nuovo controllo

#### Scenario: Segnale sparito anche con controllo automatico disattivato

- **WHEN** l'utente installa l'aggiornamento segnalato con l'opzione "Avvisami di nuove versioni" disattivata e riapre l'app
- **THEN** il segnale non viene mostrato, senza attendere un controllo manuale

### Requirement: Silenziamento del segnale per versione

Il sistema SHALL permettere all'utente di chiudere il segnale di aggiornamento disponibile. Una volta chiuso, il segnale NON MUST ricomparire per la stessa versione remota, ma SHALL ricomparire se un controllo successivo rileva una versione remota più recente di quella già silenziata.

Il silenziamento SHALL essere dimenticato quando la versione silenziata risulta **installata**, così da non lasciare uno stato residuo capace di mascherare segnalazioni successive. Un silenziamento relativo a una versione **non** installata SHALL restare valido.

#### Scenario: Chiusura del segnale

- **WHEN** l'utente chiude il segnale relativo alla versione remota corrente
- **THEN** il sistema non mostra più il segnale per quella versione

#### Scenario: Nuova versione dopo silenziamento

- **WHEN** l'utente ha silenziato una versione remota e un controllo successivo rileva una versione remota ulteriore, più recente
- **THEN** il sistema mostra di nuovo il segnale per la nuova versione

#### Scenario: Riapertura app dopo silenziamento senza nuove versioni

- **WHEN** l'utente ha silenziato la versione remota corrente e riapre l'app senza che sia stata pubblicata una versione più recente
- **THEN** il sistema non ripropone il segnale per la versione già silenziata

#### Scenario: Silenziamento dimenticato dopo l'installazione

- **WHEN** l'utente ha silenziato la versione N e successivamente installa proprio la versione N
- **THEN** il silenziamento di N viene dimenticato, e una futura versione remota diversa da quella installata torna a produrre il segnale normalmente

#### Scenario: Silenziamento di una versione non installata conservato

- **WHEN** l'utente ha silenziato la versione N e la versione installata è ancora diversa da N
- **THEN** il silenziamento resta valido e il segnale per N non ricompare

### Requirement: Notifica di sistema per una nuova versione

Quando un controllo (manuale, automatico in foreground o automatico in background) rileva una versione remota **diversa da quella installata** e non silenziata dall'utente, il sistema SHALL emettere una **notifica di sistema** che comunica la disponibilità dell'aggiornamento. La notifica SHALL essere emessa in aggiunta — non in sostituzione — al segnale in-app. Toccarla SHALL aprire l'app sul flusso di aggiornamento definito da `app-update` e SHALL rimuovere la notifica.

La notifica SHALL usare un'importanza che non interrompa l'utente (nessun avviso invadente) e SHALL essere distinta da quella di avanzamento del download, così che le due non si sostituiscano a vicenda.

#### Scenario: Notifica alla rilevazione di una nuova versione

- **WHEN** un controllo rileva una versione remota diversa da quella installata e non silenziata
- **THEN** il sistema emette una notifica di sistema che indica la versione disponibile

#### Scenario: Apertura del flusso di aggiornamento dalla notifica

- **WHEN** l'utente tocca la notifica
- **THEN** l'app si apre sul flusso di controllo/download/installazione di `app-update` e la notifica viene rimossa

#### Scenario: Nessuna notifica se la versione è già installata

- **WHEN** l'esito memorizzato di un controllo annuncia una versione che risulta già installata
- **THEN** il sistema non emette alcuna notifica

#### Scenario: Notifica e download non si sovrappongono

- **WHEN** l'utente avvia il download mentre la notifica di disponibilità è presente
- **THEN** la notifica di avanzamento del download non sostituisce né cancella quella di disponibilità

#### Scenario: Segnale in-app comunque presente

- **WHEN** il sistema emette la notifica di una nuova versione
- **THEN** banner e badge in-app continuano a comportarsi come previsto, indipendentemente dalla notifica

### Requirement: Ripetizione della notifica finché l'aggiornamento è pendente

Il sistema SHALL riemettere la notifica a **ogni controllo** che rileva l'aggiornamento ancora non installato, invece di emetterla una sola volta per versione. La ripetizione SHALL cessare quando l'aggiornamento viene installato oppure quando l'utente silenzia quella versione.

#### Scenario: Notifica ripetuta al controllo successivo

- **WHEN** un controllo successivo rileva che la stessa versione è ancora disponibile e non installata né silenziata
- **THEN** il sistema riemette la notifica

#### Scenario: Ripetizione interrotta dall'installazione

- **WHEN** l'utente installa l'aggiornamento
- **THEN** il sistema non emette più notifiche per quella versione e rimuove quella eventualmente pendente

#### Scenario: Ripetizione interrotta dal silenziamento

- **WHEN** l'utente silenzia la versione segnalata
- **THEN** il sistema non emette più notifiche per quella versione, mentre una versione successiva torna a produrle

### Requirement: Permesso notifiche richiesto all'attivazione dell'opzione

Il sistema SHALL richiedere il permesso di inviare notifiche al momento in cui l'utente **attiva** l'opzione "Avvisami di nuove versioni", e non al primo invio di una notifica. Se il permesso viene negato, l'opzione SHALL restare attiva e il **segnale in-app SHALL continuare a funzionare**: il sistema MUST NOT disattivare l'opzione né sopprimere il banner per un permesso negato.

#### Scenario: Permesso concesso all'attivazione

- **WHEN** l'utente attiva "Avvisami di nuove versioni" e concede il permesso notifiche
- **THEN** da quel momento i controlli che rilevano un aggiornamento producono anche la notifica di sistema

#### Scenario: Permesso negato

- **WHEN** l'utente attiva l'opzione ma nega il permesso notifiche
- **THEN** l'opzione resta attiva, il segnale in-app continua a funzionare e nessuna notifica viene emessa

#### Scenario: Permesso non richiesto da un controllo in background

- **WHEN** un controllo automatico in background rileva un aggiornamento e il permesso non è stato concesso
- **THEN** il sistema non tenta di richiedere il permesso in quel contesto e si limita a non emettere la notifica

### Requirement: Disattivazione dell'opzione senza residui

Quando l'utente disattiva "Avvisami di nuove versioni", il sistema SHALL annullare il controllo periodico in background e SHALL rimuovere l'eventuale notifica di disponibilità ancora presente. Dopo la disattivazione il sistema MUST NOT effettuare alcuna richiesta di rete non esplicitamente richiesta dall'utente.

#### Scenario: Nessun lavoro periodico residuo

- **WHEN** l'utente disattiva l'opzione
- **THEN** il controllo periodico in background viene annullato e non produce più richieste di rete

#### Scenario: Notifica pendente rimossa

- **WHEN** l'utente disattiva l'opzione mentre una notifica di disponibilità è presente
- **THEN** la notifica viene rimossa
