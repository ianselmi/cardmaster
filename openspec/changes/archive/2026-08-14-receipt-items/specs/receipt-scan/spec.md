## MODIFIED Requirements

### Requirement: Estrazione automatica dei dati di testata

Dal testo riconosciuto il sistema SHALL tentare di estrarre **esercente**, **partita IVA**, **data/ora dell'acquisto**, **totale** e **totale dell'imposta** ("di cui IVA"). Ogni campo non riconosciuto SHALL restare vuoto: il sistema MUST NOT inventare o dedurre valori non presenti nello scontrino — in particolare, il totale dell'imposta MUST NOT essere calcolato dal totale quando non è stampato. Il totale SHALL essere interpretato nel formato numerico italiano (virgola decimale, punto opzionale come separatore delle migliaia). Il riconoscimento SHALL tollerare gli spazi che il motore inserisce all'interno di date e importi.

#### Scenario: Testata riconosciuta

- **WHEN** lo scontrino contiene esercente, data e totale in forma leggibile
- **THEN** i tre campi risultano precompilati con i valori letti dallo scontrino

#### Scenario: Campo non riconosciuto

- **WHEN** un campo della testata non è riconoscibile nel testo
- **THEN** quel campo resta vuoto ed è segnalato come non riconosciuto, mentre gli altri restano valorizzati

#### Scenario: Data implausibile scartata

- **WHEN** il testo contiene una data futura o molto remota rispetto a oggi
- **THEN** il sistema non la propone come data dell'acquisto e lascia il campo vuoto

#### Scenario: Totale distinto da subtotale, sconti e resto

- **WHEN** lo scontrino contiene anche subtotale, sconti, importo pagato e resto
- **THEN** viene proposto il totale dello scontrino e non uno degli altri importi

#### Scenario: Gruppi di cifre spezzati dal riconoscimento

- **WHEN** il testo riconosciuto contiene una data o un importo con spazi interni introdotti dal motore
- **THEN** il valore viene comunque riconosciuto

#### Scenario: Totale imposta letto

- **WHEN** lo scontrino riporta il totale dell'imposta ("di cui IVA")
- **THEN** il valore viene estratto in centesimi e proposto insieme al totale

#### Scenario: Totale imposta assente

- **WHEN** lo scontrino non riporta il totale dell'imposta
- **THEN** il campo resta vuoto e non viene calcolato a partire dal totale

### Requirement: Conferma e correzione prima del salvataggio

Il sistema SHALL mostrare i dati estratti in una schermata di conferma dove **ogni campo è modificabile a mano** prima del salvataggio, distinguendo visivamente i campi riconosciuti da quelli rimasti vuoti. Nessuno scontrino SHALL essere salvato senza il passaggio di conferma.

La schermata di conferma SHALL presentare, oltre ai campi di testata, anche le **righe prodotto ricostruite** e l'esito della loro **quadratura** rispetto al totale. L'esito della quadratura SHALL essere il primo elemento visibile, prima dell'elenco delle righe: quando le righe quadrano, la conferma MUST essere possibile senza scorrerle una per una. Le righe SHALL essere correggibili come i campi di testata, e la mancata quadratura MUST NOT impedire il salvataggio.

#### Scenario: Correzione di un campo riconosciuto male

- **WHEN** l'utente corregge il totale proposto e conferma
- **THEN** lo scontrino viene salvato con il valore corretto dall'utente

#### Scenario: Completamento di un campo vuoto

- **WHEN** l'utente compila a mano un campo che il riconoscimento aveva lasciato vuoto e conferma
- **THEN** lo scontrino viene salvato con quel valore

#### Scenario: Conferma abbandonata

- **WHEN** l'utente esce dalla schermata di conferma senza confermare
- **THEN** nessuno scontrino viene salvato, nessuna immagine viene conservata e nessuna riga viene persistita

#### Scenario: Quadratura in evidenza

- **WHEN** l'utente arriva alla schermata di conferma di uno scontrino le cui righe sono state ricostruite
- **THEN** vede subito se la somma delle righe coincide con il totale, prima dell'elenco delle righe

#### Scenario: Conferma rapida quando le righe quadrano

- **WHEN** le righe quadrano con il totale
- **THEN** l'utente può confermare senza dover esaminare né toccare le singole righe

#### Scenario: Correzione di una riga in conferma

- **WHEN** l'utente corregge, aggiunge o elimina una riga nella schermata di conferma
- **THEN** la quadratura viene ricalcolata e lo scontrino viene salvato con le righe corrette

### Requirement: Persistenza dello scontrino

Il sistema SHALL persistere ogni scontrino confermato nel database locale, con `Id` generato dal client e cancellazione logica tramite tombstone, come le altre entità. Gli importi SHALL essere conservati come valori interi in centesimi. La data d'acquisto SHALL essere conservata come **data di calendario**, senza subire spostamenti dovuti al fuso orario del device. Il sistema SHALL conservare anche il **testo riconosciuto integrale**, così da poter ri-estrarre i dati di testata in futuro senza richiedere una nuova acquisizione.

Il sistema SHALL persistere insieme allo scontrino anche le sue **righe prodotto**, che gli appartengono: vivono e muoiono con lo scontrino, sono sostituite in blocco a ogni modifica ed eliminate logicamente insieme a esso. Uno scontrino salvato **senza righe** — perché acquisito prima di questa capability o perché nessuna riga è stata ricostruita né inserita — SHALL restare valido e consultabile.

#### Scenario: Scontrino salvato

- **WHEN** l'utente conferma i dati di uno scontrino
- **THEN** lo scontrino è persistito localmente con le sue righe e compare nello storico

#### Scenario: Testo riconosciuto conservato

- **WHEN** uno scontrino salvato viene aperto in dettaglio
- **THEN** il testo riconosciuto è consultabile insieme ai dati estratti

#### Scenario: Nessuna perdita di precisione sugli importi

- **WHEN** si sommano gli importi di più scontrini
- **THEN** il risultato è esatto al centesimo, senza errori di arrotondamento

#### Scenario: Data stabile rispetto al fuso orario

- **WHEN** uno scontrino viene salvato e riletto su un device con fuso orario diverso da UTC
- **THEN** la data mostrata è la stessa confermata dall'utente, senza slittamenti di un giorno

#### Scenario: Scontrino senza righe

- **WHEN** uno scontrino è stato salvato senza righe prodotto
- **THEN** resta consultabile e modificabile come qualunque altro, senza errori

### Requirement: Storico degli scontrini

Il sistema SHALL presentare gli scontrini acquisiti in una lista ordinata dal più recente, con esercente, data e totale visibili, e SHALL permettere di aprirne il dettaglio, modificarne i dati di testata **e le righe prodotto**, ed eliminarli. Il dettaglio SHALL mostrare, oltre ai dati di testata e al testo riconosciuto, le **righe prodotto** dello scontrino con l'esito della loro **quadratura** rispetto al totale.

#### Scenario: Lista ordinata

- **WHEN** l'utente apre la sezione Scontrini
- **THEN** vede gli scontrini dal più recente al meno recente con esercente, data e totale

#### Scenario: Storico vuoto

- **WHEN** non è stato ancora acquisito alcuno scontrino
- **THEN** la sezione spiega come acquisirne uno invece di mostrare una lista vuota

#### Scenario: Modifica di uno scontrino salvato

- **WHEN** l'utente corregge un campo di uno scontrino già salvato
- **THEN** la modifica è persistita e si riflette nella lista e nei totali

#### Scenario: Modifica delle righe di uno scontrino salvato

- **WHEN** l'utente corregge, aggiunge o elimina una riga di uno scontrino già salvato
- **THEN** la modifica è persistita e il dettaglio mostra le righe aggiornate con la quadratura ricalcolata

#### Scenario: Eliminazione di uno scontrino

- **WHEN** l'utente elimina uno scontrino
- **THEN** lo scontrino sparisce dallo storico e dai totali, la sua eventuale immagine viene rimossa dal device, e sia la sua riga sia quelle dei suoi prodotti restano come tombstone nel database
