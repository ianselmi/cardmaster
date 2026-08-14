# ai-credentials

## Purpose

Gestione della **chiave del modello fornita dall'utente**: conservazione nell'archivio protetto del device, verifica, revoca, e la garanzia che nessuna credenziale viaggi nel pacchetto dell'applicazione.

Il repository è pubblico e l'APK è scaricabile da chiunque: una chiave incorporata sarebbe una chiave compromessa. L'unica credenziale che esiste è quella che l'utente incolla, e non finisce nel database né nel backup.

## Requirements

### Requirement: La chiave è dell'utente e non viaggia nel pacchetto

Il sistema SHALL usare esclusivamente una chiave API **fornita dall'utente**. Il codice sorgente e il pacchetto dell'applicazione MUST NOT contenere alcuna chiave, token o credenziale utilizzabile: nessun segreto ricavabile scaricando il repository pubblico o estraendo l'APK installato. Il sistema MUST NOT instradare le richieste attraverso un server dell'autore.

#### Scenario: Nessun segreto estraibile

- **WHEN** si ispeziona il repository pubblico o il pacchetto distribuito
- **THEN** non è presente alcuna chiave utilizzabile da terzi

#### Scenario: Senza chiave dell'utente non si chiama nessuno

- **WHEN** l'utente non ha inserito una chiave
- **THEN** nessuna richiesta al servizio viene effettuata, e la funzione che la richiede è indisponibile e spiegata come tale

### Requirement: Conservazione protetta della chiave

La chiave SHALL essere conservata nell'**archivio protetto del sistema operativo**, non nelle preferenze in chiaro né nel database dell'app. La chiave MUST NOT comparire nel backup su Drive, nei log, nei messaggi d'errore, né essere rileggibile dall'interfaccia dopo l'inserimento.

#### Scenario: Chiave non nel database né nel backup

- **WHEN** si ispeziona il database o un backup prodotto dall'app
- **THEN** la chiave non è presente

#### Scenario: Chiave non rileggibile dall'interfaccia

- **WHEN** l'utente torna nelle impostazioni dopo aver inserito la chiave
- **THEN** vede che una chiave è configurata, ma non il suo valore

#### Scenario: Chiave assente dai log e dagli errori

- **WHEN** una chiamata fallisce e il sistema mostra o registra l'errore
- **THEN** la chiave non compare nel testo

### Requirement: Verifica e revoca della chiave

Il sistema SHALL permettere di **verificare** la chiave inserita con una richiesta minima, riportando l'esito in modo comprensibile, e di **rimuoverla** in ogni momento. Rimuovere la chiave SHALL disattivare le funzioni che la richiedono, senza intaccare i dati già salvati.

#### Scenario: Chiave valida

- **WHEN** l'utente verifica una chiave valida
- **THEN** il sistema lo conferma

#### Scenario: Chiave non valida

- **WHEN** l'utente verifica una chiave rifiutata dal servizio
- **THEN** il sistema lo dice distinguendolo da un problema di rete

#### Scenario: Rimozione della chiave

- **WHEN** l'utente rimuove la chiave
- **THEN** le funzioni che la richiedono tornano indisponibili e gli scontrini già salvati restano intatti
