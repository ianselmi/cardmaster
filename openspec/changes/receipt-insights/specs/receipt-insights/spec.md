## ADDED Requirements

### Requirement: Sezione di analisi della spesa

Il sistema SHALL offrire una sezione di **analisi** raggiungibile dalla sezione Scontrini, che raccoglie le viste ricavate dallo storico locale. La sezione MUST NOT comparire come voce di pari livello nella barra di navigazione principale, riservata alle carte e agli scontrini.

Le viste SHALL funzionare interamente offline e MUST NOT introdurre alcuna chiamata di rete.

#### Scenario: Accesso alle viste

- **WHEN** l'utente apre la sezione Scontrini e sceglie l'analisi
- **THEN** raggiunge le viste di spesa senza uscire dalla sezione Scontrini

#### Scenario: Analisi senza rete

- **WHEN** il device è in modalità aereo
- **THEN** tutte le viste si calcolano e si mostrano senza alcuna differenza

#### Scenario: Storico vuoto

- **WHEN** non esiste alcuno scontrino con righe
- **THEN** ogni vista dichiara di non avere dati sufficienti e ne spiega il motivo, senza mostrare un errore e senza mostrare valori a zero come se fossero un risultato

### Requirement: Copertura dei dati dichiarata su ogni vista

I dati su cui le viste si basano sono **incompleti per costruzione**: le righe si ricostruiscono da una fotografia e una parte non si ricostruisce. Ogni vista SHALL quindi dichiarare **su quanta parte dello storico è calcolata**: quanti scontrini e quante righe hanno contribuito, e quanti sono rimasti fuori.

Il sistema MUST NOT presentare una vista calcolata su una parte dei dati come se fosse calcolata su tutti. La copertura SHALL essere ricavata dalla stessa lettura che produce i dati, non da una stima separata.

#### Scenario: Copertura visibile accanto ai risultati

- **WHEN** l'utente apre una vista costruita su una parte delle righe disponibili
- **THEN** vede dichiarato quante righe o quanti scontrini sono entrati nel calcolo e quanti no

#### Scenario: Motivo dell'esclusione dichiarato

- **WHEN** delle righe restano fuori da una vista perché prive del dato che quella vista richiede
- **THEN** il motivo dell'esclusione è indicato, non solo il numero

#### Scenario: Scontrini senza righe

- **WHEN** lo storico contiene scontrini salvati prima che le righe venissero estratte
- **THEN** quegli scontrini risultano fuori dalle viste basate sulle righe, dichiarati come tali, e continuano a contribuire alle viste basate sulla sola testata

### Requirement: Top prodotti per frequenza e per spesa

Il sistema SHALL mostrare i prodotti acquistati più spesso e quelli su cui si è speso di più, aggregando le righe sulla **descrizione normalizzata** già persistita su ciascuna riga. Il sistema MUST NOT applicare in questa vista alcun raggruppamento o normalizzazione ulteriore dei nomi: due descrizioni normalizzate diverse SHALL restare due voci distinte.

Le righe di sconto MUST NOT comparire come prodotti.

#### Scenario: Classifica per numero di acquisti

- **WHEN** l'utente apre la vista dei prodotti più acquistati
- **THEN** vede i prodotti ordinati per quante volte sono stati acquistati, con la spesa complessiva di ciascuno

#### Scenario: Classifica per spesa

- **WHEN** l'utente ordina per spesa invece che per frequenza
- **THEN** vede i prodotti ordinati per importo totale speso

#### Scenario: Sconti non confusi con prodotti

- **WHEN** lo storico contiene righe di sconto a importo negativo
- **THEN** quelle righe non compaiono tra i prodotti

### Requirement: Spesa per categoria

Il sistema SHALL mostrare la spesa aggregata per **categoria di prodotto**. La quota di spesa su righe **senza categoria** SHALL essere dichiarata esplicitamente e MUST NOT essere omessa dal totale né attribuita a una categoria esistente.

#### Scenario: Ripartizione per categoria

- **WHEN** l'utente apre la vista delle categorie
- **THEN** vede quanto è stato speso in ciascuna categoria, ordinate per importo

#### Scenario: Righe senza categoria dichiarate

- **WHEN** una parte delle righe non ha categoria assegnata
- **THEN** la loro spesa è mostrata come quota non categorizzata, distinta dalle categorie vere

### Requirement: Spesa per negozio e per mese su tutto lo storico

Il sistema SHALL mostrare la spesa aggregata per **esercente** e per **mese** estesa a tutto lo storico, non al solo mese corrente. Gli scontrini privi di data o di totale MUST NOT falsare i totali e SHALL restare individuabili come incompleti.

#### Scenario: Confronto tra mesi

- **WHEN** lo storico contiene scontrini di mesi diversi
- **THEN** l'utente vede la spesa di ciascun mese e può confrontarli

#### Scenario: Spesa per esercente in un mese scelto

- **WHEN** l'utente sceglie un mese
- **THEN** vede quanto ha speso presso ciascun esercente in quel mese

#### Scenario: Scontrino incompleto non conteggiato

- **WHEN** uno scontrino non ha data o non ha totale
- **THEN** non entra nei totali e resta segnalato come incompleto

### Requirement: Andamento del prezzo di un prodotto

Il sistema SHALL mostrare l'andamento nel tempo del **prezzo unitario** di un prodotto, come serie storica dei prezzi letti sugli scontrini, accompagnata da una rappresentazione grafica compatta.

Il prezzo unitario SHALL provenire **esclusivamente** dal valore stampato sullo scontrino. Il sistema MUST NOT ricavarlo dividendo l'importo per la quantità: su una riga con quantità letta male o contenente due prodotti quel quoziente sarebbe un valore plausibile e sbagliato. Le righe prive di prezzo unitario MUST essere escluse dalla serie e contate nella copertura.

Le serie SHALL essere costruite per prodotto **e unità di misura**: un prodotto acquistato sia a peso sia a pezzo produce due serie distinte, mai una media tra unità diverse.

#### Scenario: Serie storica di un prodotto ricorrente

- **WHEN** l'utente sceglie un prodotto acquistato più volte con il prezzo unitario stampato
- **THEN** vede i prezzi nel tempo in ordine cronologico con la loro rappresentazione grafica

#### Scenario: Prezzo unitario mai dedotto

- **WHEN** una riga riporta l'importo ma non il prezzo unitario
- **THEN** quella riga non produce alcun punto nella serie e rientra tra le righe escluse dichiarate

#### Scenario: Unità di misura diverse non mescolate

- **WHEN** lo stesso prodotto risulta acquistato sia a peso sia a pezzo
- **THEN** le due serie restano distinte

#### Scenario: Dati insufficienti per una serie

- **WHEN** un prodotto ha meno punti di quanti ne servano per mostrare un andamento
- **THEN** il sistema lo dichiara invece di disegnare una linea priva di significato

### Requirement: Le viste non correggono e non nascondono i dati

Le viste SHALL rappresentare quello che il database contiene. Il sistema MUST NOT escludere righe perché ritenute anomale, MUST NOT correggere importi per far tornare i conti e MUST NOT accorpare voci per rendere una classifica più presentabile.

#### Scenario: Riga ricostruita male comunque rappresentata

- **WHEN** lo storico contiene una riga la cui descrizione unisce due prodotti
- **THEN** quella riga compare nelle viste come è stata salvata, senza essere scartata né corretta automaticamente

#### Scenario: Nessun aggiustamento dei totali

- **WHEN** la somma delle righe di uno scontrino non coincide con il suo totale
- **THEN** le viste non introducono alcuna correzione per farle coincidere

### Requirement: Formattazione italiana indipendente dal device

Importi, date e mesi mostrati nelle viste SHALL essere formattati in **euro e in italiano**, indipendentemente dalla lingua e dalle impostazioni regionali configurate sul device.

#### Scenario: Device configurato in un'altra lingua

- **WHEN** il device è configurato in una lingua diversa dall'italiano
- **THEN** importi, date e nomi dei mesi nelle viste restano nel formato italiano in euro
