## MODIFIED Requirements

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
