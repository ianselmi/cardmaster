# card-editing

## Purpose

Modifica dei campi editabili di una carta esistente (nome visualizzato, associazione emittente, formato del barcode, colore del riquadro e label) mantenendo il valore del barcode immutabile come identità della carta, con persistenza locale offline (preservando `Id` e `CreatedAt`, rinnovando `UpdatedAt`) ed eliminazione logica (tombstone) previa conferma. Le regole delle label sono definite dalla capability `card-labels`.

## Requirements

### Requirement: Avvio della modifica da una carta aperta

Il sistema SHALL offrire, dalla pagina di visualizzazione di una carta, un comando "Modifica" che apre una schermata dedicata pre-compilata con i dati correnti della carta.

#### Scenario: Apertura della modifica

- **WHEN** l'utente, con una carta aperta, seleziona "Modifica"
- **THEN** si apre la schermata di modifica con nome, emittente e formato già valorizzati dai dati correnti della carta

#### Scenario: Carta non più esistente

- **WHEN** si tenta di aprire la modifica di una carta che non esiste più (o è un tombstone)
- **THEN** il sistema non va in errore e riporta l'utente alla lista

### Requirement: Campi modificabili di una carta

Il sistema SHALL consentire di modificare il **nome visualizzato**, l'**associazione emittente** (dal catalogo, libero, o assente), il **formato del barcode**, il **colore del riquadro** e le **label** di una carta esistente. Quando l'utente sceglie un emittente dal catalogo, il sistema SHALL poter ri-arricchire i metadati disponibili (colore di brand, riferimento logo, formato atteso) come avviene in creazione, senza sovrascrivere il colore del riquadro scelto esplicitamente dall'utente né le sue label.

#### Scenario: Modifica del nome visualizzato

- **WHEN** l'utente cambia il nome visualizzato e salva
- **THEN** la carta viene aggiornata con il nuovo nome e la lista lo riflette

#### Scenario: Cambio emittente dal catalogo

- **WHEN** l'utente associa un emittente presente nel catalogo
- **THEN** la carta eredita i metadati dell'emittente disponibili (colore di brand, logo, formato atteso quando presenti)

#### Scenario: Emittente rimosso o libero

- **WHEN** l'utente rimuove l'emittente o ne digita uno non presente nel catalogo
- **THEN** la carta viene salvata con quell'emittente (o senza), senza arricchimento e senza errori

#### Scenario: Correzione del formato barcode

- **WHEN** l'utente seleziona un formato barcode diverso e salva
- **THEN** la carta viene aggiornata con il nuovo formato

#### Scenario: Colore e label modificabili

- **WHEN** l'utente cambia il colore del riquadro o le label e salva
- **THEN** la carta viene aggiornata con i nuovi valori, senza toccare gli altri campi

### Requirement: Scelta del colore della carta in modifica

Il sistema SHALL consentire, dalla schermata di modifica, di scegliere il **colore del riquadro** della carta da una **palette predefinita** — la stessa da cui l'app attinge per il colore automatico — oppure di selezionare l'opzione **"Automatico"**, che riporta la carta al colore derivato dal nome. Il colore scelto dall'utente SHALL essere persistito insieme agli altri campi editabili e SHALL prevalere sul colore automatico. Il colore scelto dall'utente è distinto dal colore di *brand* ereditato dall'emittente del catalogo: cambiare emittente MUST NOT sovrascrivere una scelta esplicita dell'utente.

#### Scenario: Colore scelto dalla palette

- **WHEN** l'utente seleziona un colore dalla palette e salva
- **THEN** il riquadro di quella carta nella lista usa il colore scelto

#### Scenario: Ritorno al colore automatico

- **WHEN** l'utente seleziona "Automatico" e salva
- **THEN** il riquadro torna a usare il colore derivato dal nome della carta

#### Scenario: Anteprima dell'opzione automatica

- **WHEN** l'utente è nel selettore di colore
- **THEN** l'opzione "Automatico" mostra il colore che la carta assumerebbe in base al nome corrente

#### Scenario: Cambio emittente con colore scelto

- **WHEN** la carta ha un colore scelto dall'utente e l'utente cambia l'emittente associandone uno dal catalogo
- **THEN** il colore scelto dall'utente resta invariato

#### Scenario: Carta senza colore scelto

- **WHEN** si apre la modifica di una carta a cui non è mai stato scelto un colore
- **THEN** il selettore risulta sull'opzione "Automatico"

### Requirement: Assegnazione delle label in modifica

Il sistema SHALL consentire, dalla schermata di modifica, di aggiungere e rimuovere le **label** della carta secondo le regole della capability `card-labels` (creazione al volo, suggerimenti dalle label già in uso, normalizzazione, limiti). Le label SHALL essere pre-compilate con quelle correnti della carta all'apertura della schermata e SHALL essere persistite al salvataggio insieme agli altri campi. Le label MUST NOT essere obbligatorie per salvare.

#### Scenario: Label correnti pre-compilate

- **WHEN** l'utente apre la modifica di una carta che ha delle label
- **THEN** le label correnti sono già mostrate come assegnate

#### Scenario: Label aggiunta e salvata

- **WHEN** l'utente aggiunge una label e salva
- **THEN** la carta risulta associata anche a quella label e la label diventa disponibile come filtro nella lista

#### Scenario: Label rimossa e salvata

- **WHEN** l'utente rimuove una label e salva
- **THEN** la carta non risulta più associata a quella label

#### Scenario: Salvataggio senza label

- **WHEN** l'utente salva una modifica valida senza assegnare nessuna label
- **THEN** il salvataggio riesce, senza segnalazioni

### Requirement: Immutabilità del valore del barcode

Il sistema SHALL mostrare il valore del barcode in sola lettura nella schermata di modifica e NON MUST consentirne l'alterazione: il valore del barcode è l'identità della carta fedeltà.

#### Scenario: Valore barcode non modificabile

- **WHEN** l'utente è nella schermata di modifica
- **THEN** il valore del barcode è visibile ma non editabile, e resta invariato dopo il salvataggio

### Requirement: Validazione dei campi in modifica

Il sistema SHALL impedire il salvataggio della modifica se manca il nome visualizzato o il formato, segnalando il campo mancante.

#### Scenario: Salvataggio bloccato senza nome o formato

- **WHEN** l'utente tenta di salvare la modifica con nome visualizzato vuoto o senza formato
- **THEN** il salvataggio è impedito e viene segnalato il campo mancante

### Requirement: Persistenza della modifica

Il sistema SHALL salvare la modifica tramite aggiornamento locale della carta, preservando `Id` e `CreatedAt` e aggiornando `UpdatedAt`, senza creare una nuova carta. L'aggiornamento SHALL comprendere anche colore del riquadro e label come parte della stessa scrittura. Il salvataggio SHALL funzionare offline.

#### Scenario: Modifica persistita

- **WHEN** l'utente conferma una modifica valida
- **THEN** la stessa carta (stesso Id) viene aggiornata con i nuovi valori e `UpdatedAt` rinnovato, senza duplicati

#### Scenario: Modifica offline

- **WHEN** la modifica avviene senza connessione di rete
- **THEN** l'aggiornamento viene comunque persistito localmente

#### Scenario: Colore e label nella stessa scrittura

- **WHEN** l'utente modifica contemporaneamente nome, colore e label e salva
- **THEN** tutti i valori vengono persistiti insieme sulla stessa carta, con un solo `UpdatedAt` rinnovato

### Requirement: Ritorno alla visualizzazione dopo la modifica

Dopo un salvataggio riuscito il sistema SHALL riportare l'utente alla carta, mostrando i dati aggiornati.

#### Scenario: Dati aggiornati visibili

- **WHEN** l'utente salva la modifica
- **THEN** torna alla pagina di visualizzazione (o alla lista) e i dati mostrati riflettono la modifica

### Requirement: Eliminazione di una carta

Il sistema SHALL consentire di eliminare una carta esistente tramite un comando dalla pagina di visualizzazione, previa **conferma** esplicita. L'eliminazione SHALL essere **logica** (tombstone): la riga non viene rimossa fisicamente, coerentemente con la semantica di local-storage. Dopo l'eliminazione la carta NON MUST comparire nella lista carte.

#### Scenario: Eliminazione confermata

- **WHEN** l'utente sceglie "Elimina" e conferma
- **THEN** la carta viene marcata come cancellata (tombstone), l'utente torna alla lista e la carta non è più presente

#### Scenario: Eliminazione annullata

- **WHEN** l'utente sceglie "Elimina" ma annulla alla richiesta di conferma
- **THEN** nessuna modifica viene applicata e la carta resta invariata
