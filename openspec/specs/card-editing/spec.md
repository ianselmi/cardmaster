# card-editing

## Purpose

Modifica dei campi editabili di una carta esistente (nome visualizzato, associazione emittente, formato del barcode) mantenendo il valore del barcode immutabile come identità della carta, con persistenza locale offline (preservando `Id` e `CreatedAt`, rinnovando `UpdatedAt`) ed eliminazione logica (tombstone) previa conferma.

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

Il sistema SHALL consentire di modificare il **nome visualizzato**, l'**associazione emittente** (dal catalogo, libero, o assente) e il **formato del barcode** di una carta esistente. Quando l'utente sceglie un emittente dal catalogo, il sistema SHALL poter ri-arricchire i metadati disponibili (colore, riferimento logo, formato atteso) come avviene in creazione.

#### Scenario: Modifica del nome visualizzato

- **WHEN** l'utente cambia il nome visualizzato e salva
- **THEN** la carta viene aggiornata con il nuovo nome e la lista lo riflette

#### Scenario: Cambio emittente dal catalogo

- **WHEN** l'utente associa un emittente presente nel catalogo
- **THEN** la carta eredita i metadati dell'emittente disponibili (colore, logo, formato atteso quando presenti)

#### Scenario: Emittente rimosso o libero

- **WHEN** l'utente rimuove l'emittente o ne digita uno non presente nel catalogo
- **THEN** la carta viene salvata con quell'emittente (o senza), senza arricchimento e senza errori

#### Scenario: Correzione del formato barcode

- **WHEN** l'utente seleziona un formato barcode diverso e salva
- **THEN** la carta viene aggiornata con il nuovo formato

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

Il sistema SHALL salvare la modifica tramite aggiornamento locale della carta, preservando `Id` e `CreatedAt` e aggiornando `UpdatedAt`, senza creare una nuova carta. Il salvataggio SHALL funzionare offline.

#### Scenario: Modifica persistita

- **WHEN** l'utente conferma una modifica valida
- **THEN** la stessa carta (stesso Id) viene aggiornata con i nuovi valori e `UpdatedAt` rinnovato, senza duplicati

#### Scenario: Modifica offline

- **WHEN** la modifica avviene senza connessione di rete
- **THEN** l'aggiornamento viene comunque persistito localmente

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
