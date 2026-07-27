# app-update-notify

## Purpose

Segnale in-app della presenza di un aggiornamento, fuori dalla pagina Impostazioni: opzione opt-in "Avvisami di nuove versioni" (default disattivata) che abilita un controllo automatico limitato in frequenza e legato al solo foreground, badge e banner che portano al flusso di aggiornamento di `app-update`, e silenziamento del segnale per versione. Non introduce infrastruttura propria: riusa integralmente il controllo di versione definito da `app-update`.

## Requirements

### Requirement: Opzione per il controllo automatico degli aggiornamenti

Il sistema SHALL fornire in Impostazioni un'opzione "Avvisami di nuove versioni" (default **disattivata**) che, se attivata, permette al sistema di controllare automaticamente la presenza di un aggiornamento all'apertura dell'app, oltre al controllo manuale già disponibile. Se l'opzione è disattivata, il sistema NON MUST effettuare alcun controllo di rete se non su richiesta esplicita dell'utente.

#### Scenario: Opzione disattivata (default)

- **WHEN** l'utente non ha mai attivato "Avvisami di nuove versioni" e apre l'app
- **THEN** il sistema non effettua alcun controllo automatico di rete

#### Scenario: Opzione attivata

- **WHEN** l'utente attiva "Avvisami di nuove versioni" nelle Impostazioni
- **THEN** da quel momento il sistema può effettuare controlli automatici come descritto dal requisito "Controllo automatico limitato in frequenza"

#### Scenario: Opzione disattivata dopo essere stata attiva

- **WHEN** l'utente disattiva "Avvisami di nuove versioni" dopo averla attivata
- **THEN** il sistema smette di effettuare controlli automatici, senza rimuovere l'esito dell'ultimo controllo già mostrato in Impostazioni

### Requirement: Controllo automatico limitato in frequenza e legato al foreground

Quando l'opzione è attivata, il sistema SHALL effettuare il controllo automatico solo quando l'app passa in foreground (avvio o ripresa da background) e SHALL evitare di ripetere il controllo se non sono trascorse almeno 24 ore dall'ultimo controllo (manuale o automatico). Il sistema NON MUST effettuare controlli mentre l'app è in background o chiusa.

#### Scenario: App aperta entro l'intervallo minimo

- **WHEN** l'opzione è attivata e l'utente apre l'app meno di 24 ore dopo l'ultimo controllo
- **THEN** il sistema non effettua un nuovo controllo di rete

#### Scenario: App aperta dopo l'intervallo minimo

- **WHEN** l'opzione è attivata e l'utente apre l'app almeno 24 ore dopo l'ultimo controllo
- **THEN** il sistema effettua un controllo automatico riusando la stessa logica di verifica versione di `app-update`

#### Scenario: App in background o chiusa

- **WHEN** l'app non è in foreground
- **THEN** il sistema non effettua alcun controllo automatico, indipendentemente dal tempo trascorso

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
