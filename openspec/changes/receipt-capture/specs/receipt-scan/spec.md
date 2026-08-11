## ADDED Requirements

### Requirement: Acquisizione di uno scontrino da foto o da immagine

Il sistema SHALL permettere di acquisire uno scontrino in due modi: scattando una **foto** con la fotocamera, oppure scegliendo un'**immagine già presente** sul device tramite il selettore di sistema. Il sistema MUST NOT richiedere permessi di accesso allo storage per il secondo percorso.

#### Scenario: Acquisizione da fotocamera

- **WHEN** l'utente sceglie di scattare la foto di uno scontrino e concede il permesso fotocamera
- **THEN** il sistema acquisisce l'immagine e prosegue con il riconoscimento del testo

#### Scenario: Acquisizione da immagine esistente

- **WHEN** l'utente sceglie un'immagine già sul device dal selettore di sistema
- **THEN** il sistema la analizza allo stesso modo di una foto appena scattata

#### Scenario: Permesso fotocamera negato

- **WHEN** l'utente nega il permesso fotocamera
- **THEN** il sistema spiega perché serve e lascia disponibile il percorso da immagine esistente, senza chiudersi né bloccarsi

#### Scenario: Acquisizione annullata

- **WHEN** l'utente annulla lo scatto o la scelta dell'immagine
- **THEN** il sistema torna allo stato precedente senza creare alcuno scontrino e senza messaggi di errore

### Requirement: Riconoscimento del testo interamente sul device

Il sistema SHALL riconoscere il testo dell'immagine **sul device**, senza alcuna connessione di rete. L'immagine e il testo riconosciuto MUST NOT essere inviati a nessun servizio esterno. Il riconoscimento SHALL funzionare al primo utilizzo anche senza rete, senza scaricare componenti a runtime.

#### Scenario: Riconoscimento senza rete

- **WHEN** l'utente acquisisce uno scontrino con il device offline
- **THEN** il riconoscimento del testo si completa normalmente e lo scontrino può essere salvato

#### Scenario: Primo utilizzo offline

- **WHEN** l'utente usa la funzione per la prima volta su un device che non è mai stato online dopo l'installazione
- **THEN** il riconoscimento funziona senza richiedere il download di alcun modello

#### Scenario: Immagine non leggibile

- **WHEN** l'immagine non contiene testo riconoscibile
- **THEN** il sistema lo comunica in modo comprensibile e propone di riprovare con un'altra immagine, senza salvare nulla

### Requirement: Estrazione automatica dei dati di testata

Dal testo riconosciuto il sistema SHALL tentare di estrarre **esercente**, **partita IVA**, **data/ora dell'acquisto** e **totale**. Ogni campo non riconosciuto SHALL restare vuoto: il sistema MUST NOT inventare o dedurre valori non presenti nello scontrino. Il totale SHALL essere interpretato nel formato numerico italiano (virgola decimale, punto opzionale come separatore delle migliaia).

#### Scenario: Testata riconosciuta

- **WHEN** lo scontrino contiene esercente, data e totale in forma leggibile
- **THEN** i tre campi risultano precompilati con i valori letti dallo scontrino

#### Scenario: Campo non riconosciuto

- **WHEN** un campo della testata non è riconoscibile nel testo
- **THEN** quel campo resta vuoto ed è segnalato come non riconosciuto, mentre gli altri restano valorizzati

#### Scenario: Data implausibile scartata

- **WHEN** il testo contiene una data futura o molto remota rispetto a oggi
- **THEN** il sistema non la propone come data dell'acquisto e lascia il campo vuoto

### Requirement: Conferma e correzione prima del salvataggio

Il sistema SHALL mostrare i dati estratti in una schermata di conferma dove **ogni campo è modificabile a mano** prima del salvataggio, distinguendo visivamente i campi riconosciuti da quelli rimasti vuoti. Nessuno scontrino SHALL essere salvato senza il passaggio di conferma.

#### Scenario: Correzione di un campo riconosciuto male

- **WHEN** l'utente corregge il totale proposto e conferma
- **THEN** lo scontrino viene salvato con il valore corretto dall'utente

#### Scenario: Completamento di un campo vuoto

- **WHEN** l'utente compila a mano un campo che il riconoscimento aveva lasciato vuoto e conferma
- **THEN** lo scontrino viene salvato con quel valore

#### Scenario: Conferma abbandonata

- **WHEN** l'utente esce dalla schermata di conferma senza confermare
- **THEN** nessuno scontrino viene salvato e nessuna immagine viene conservata

### Requirement: Persistenza dello scontrino

Il sistema SHALL persistere ogni scontrino confermato nel database locale, con `Id` generato dal client e cancellazione logica tramite tombstone, come le altre entità. Gli importi SHALL essere conservati come valori interi in centesimi. Il sistema SHALL conservare anche il **testo riconosciuto integrale**, così da poter ri-estrarre i dati di testata in futuro senza richiedere una nuova acquisizione.

#### Scenario: Scontrino salvato

- **WHEN** l'utente conferma i dati di uno scontrino
- **THEN** lo scontrino è persistito localmente e compare nello storico

#### Scenario: Testo riconosciuto conservato

- **WHEN** uno scontrino salvato viene aperto in dettaglio
- **THEN** il testo riconosciuto è consultabile insieme ai dati estratti

#### Scenario: Nessuna perdita di precisione sugli importi

- **WHEN** si sommano gli importi di più scontrini
- **THEN** il risultato è esatto al centesimo, senza errori di arrotondamento

### Requirement: Conservazione dell'immagine dello scontrino

Il sistema SHALL conservare per default l'immagine acquisita nell'area dati privata dell'app, associata allo scontrino, e SHALL permettere di **non conservarla**. Il sistema SHALL mostrare lo spazio occupato dalle immagini degli scontrini e SHALL permettere di liberarlo eliminando le immagini senza perdere i dati estratti né il testo riconosciuto.

#### Scenario: Immagine consultabile dal dettaglio

- **WHEN** l'utente apre uno scontrino di cui è stata conservata l'immagine
- **THEN** l'immagine è visualizzabile insieme ai dati

#### Scenario: Acquisizione senza conservare l'immagine

- **WHEN** l'utente ha scelto di non conservare le immagini e acquisisce uno scontrino
- **THEN** lo scontrino viene salvato con i dati e il testo riconosciuto, ma nessuna immagine resta sul device

#### Scenario: Spazio liberato senza perdere i dati

- **WHEN** l'utente elimina le immagini conservate
- **THEN** lo spazio viene liberato e gli scontrini restano nello storico con dati e testo riconosciuto intatti

### Requirement: Storico degli scontrini

Il sistema SHALL presentare gli scontrini acquisiti in una lista ordinata dal più recente, con esercente, data e totale visibili, e SHALL permettere di aprirne il dettaglio, modificarne i dati di testata ed eliminarli.

#### Scenario: Lista ordinata

- **WHEN** l'utente apre la sezione Scontrini
- **THEN** vede gli scontrini dal più recente al meno recente con esercente, data e totale

#### Scenario: Storico vuoto

- **WHEN** non è stato ancora acquisito alcuno scontrino
- **THEN** la sezione spiega come acquisirne uno invece di mostrare una lista vuota

#### Scenario: Modifica di uno scontrino salvato

- **WHEN** l'utente corregge un campo di uno scontrino già salvato
- **THEN** la modifica è persistita e si riflette nella lista e nei totali

#### Scenario: Eliminazione di uno scontrino

- **WHEN** l'utente elimina uno scontrino
- **THEN** lo scontrino sparisce dallo storico e dai totali, la sua eventuale immagine viene rimossa dal device, e la riga resta come tombstone nel database

### Requirement: Spesa per esercente e per mese

Il sistema SHALL mostrare, dai soli dati di testata, il totale speso per **mese** e la sua ripartizione per **esercente**. Gli scontrini privi di data o di totale MUST NOT falsare i totali e SHALL essere segnalati come incompleti.

#### Scenario: Totale mensile per esercente

- **WHEN** l'utente apre la vista di spesa con più scontrini di negozi diversi nello stesso mese
- **THEN** vede il totale del mese e quanto è stato speso presso ciascun esercente

#### Scenario: Scontrino incompleto escluso dai totali

- **WHEN** uno scontrino salvato non ha data o non ha totale
- **THEN** non viene conteggiato nei totali e l'utente può individuarlo per completarlo

### Requirement: Nessuna funzione di rete e nessun segreto nell'applicazione

L'acquisizione, il riconoscimento, il salvataggio e la consultazione degli scontrini MUST funzionare interamente offline. Questa capability MUST NOT introdurre alcuna chiamata di rete. Il sistema MUST NOT contenere alcuna credenziale, chiave o token incorporato nel codice sorgente o nel pacchetto dell'applicazione: nessun segreto ricavabile scaricando il repository pubblico o estraendo l'APK installato.

#### Scenario: Funzionamento completo in modalità aereo

- **WHEN** il device è in modalità aereo
- **THEN** acquisizione, riconoscimento, salvataggio, consultazione e viste di spesa funzionano senza alcuna differenza

#### Scenario: Nessun dato dello scontrino lascia il device

- **WHEN** uno scontrino viene acquisito e salvato
- **THEN** né l'immagine, né il testo riconosciuto, né i dati estratti vengono trasmessi ad alcun servizio esterno

#### Scenario: Nessun segreto estraibile dall'applicazione

- **WHEN** si ispeziona il repository pubblico o il pacchetto dell'applicazione distribuito
- **THEN** non è presente alcuna chiave o credenziale utilizzabile da terzi
