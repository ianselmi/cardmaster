## ADDED Requirements

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

## MODIFIED Requirements

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
