## ADDED Requirements

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
