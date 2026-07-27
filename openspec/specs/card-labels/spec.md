# card-labels

## Purpose

Le label come attributo di una carta: etichette testuali libere assegnate dall'utente per raggruppare le carte per uso ("spesa", "benzina", "palestra"). Creazione al volo digitandole, con suggerimenti dalle label già in uso, normalizzazione e deduplicazione case/accent-insensitive, limiti su lunghezza e numero. Non esiste un'anagrafica separata: una label vive finché almeno una carta attiva la usa. L'assegnazione avviene dalle schermate di `card-editing` e `card-capture`; il filtro per label nella lista è definito da `card-search`.

## Requirements

### Requirement: Label assegnate a una carta

Il sistema SHALL consentire di associare a una carta un insieme di **label** testuali libere, eventualmente vuoto. Le label SHALL essere persistite insieme alla carta e SHALL sopravvivere al riavvio dell'app. L'assegnazione SHALL funzionare offline.

#### Scenario: Carta senza label

- **WHEN** una carta non ha nessuna label assegnata
- **THEN** la carta è valida e si comporta in tutto come oggi, senza segnalazioni né campi obbligatori

#### Scenario: Label persistite

- **WHEN** l'utente assegna una o più label a una carta e salva
- **THEN** le label restano associate a quella carta anche dopo il riavvio dell'app

#### Scenario: Assegnazione offline

- **WHEN** l'utente assegna label senza connessione di rete
- **THEN** l'assegnazione viene comunque persistita localmente

### Requirement: Creazione di una label al volo

Il sistema SHALL consentire di creare una label digitandone il testo al momento dell'assegnazione, senza passare da una schermata di gestione dedicata. Il sistema SHALL proporre come **suggerimenti** le label già usate su altre carte e non ancora assegnate alla carta corrente, selezionabili con un solo tocco.

#### Scenario: Nuova label digitata

- **WHEN** l'utente digita un testo di label mai usato prima e lo conferma
- **THEN** la label viene creata e assegnata alla carta corrente, senza altri passaggi

#### Scenario: Label esistente suggerita

- **WHEN** l'utente apre l'editor delle label di una carta e altre carte hanno già delle label
- **THEN** quelle label compaiono come suggerimenti e possono essere assegnate con un tocco

#### Scenario: Suggerimenti senza duplicati

- **WHEN** una label è già assegnata alla carta corrente
- **THEN** quella label non compare tra i suggerimenti

#### Scenario: Rimozione di una label dalla carta

- **WHEN** l'utente rimuove una label assegnata alla carta
- **THEN** la label non risulta più associata a quella carta dopo il salvataggio

### Requirement: Normalizzazione e deduplicazione delle label

Il sistema SHALL normalizzare il testo di una label prima di assegnarla: rimozione degli spazi iniziali/finali, collasso degli spazi interni, rimozione dei caratteri di controllo e del carattere separatore usato internamente. Una label normalizzata vuota MUST NOT essere assegnata. All'interno della stessa carta le label SHALL essere deduplicate in modo **case-insensitive e accent-insensitive**, conservando la grafia della prima occorrenza.

#### Scenario: Spazi superflui rimossi

- **WHEN** l'utente digita "  spesa  " come label
- **THEN** viene assegnata la label "spesa"

#### Scenario: Testo vuoto ignorato

- **WHEN** l'utente conferma una label composta solo da spazi
- **THEN** nessuna label viene assegnata e non viene mostrato alcun errore bloccante

#### Scenario: Duplicato con maiuscole diverse

- **WHEN** la carta ha già la label "Spesa" e l'utente digita "spesa"
- **THEN** la carta conserva una sola label, con la grafia già presente

#### Scenario: Duplicato con accenti diversi

- **WHEN** la carta ha già la label "Città" e l'utente digita "citta"
- **THEN** la carta conserva una sola label, con la grafia già presente

### Requirement: Limiti su lunghezza e numero di label

Il sistema SHALL limitare la lunghezza di una singola label e il numero di label assegnabili a una carta, in modo che l'interfaccia resti leggibile. Raggiunto il limite di label per carta il sistema SHALL impedire ulteriori assegnazioni segnalandolo all'utente, senza perdere le label già assegnate.

#### Scenario: Label troppo lunga

- **WHEN** l'utente digita una label più lunga del limite consentito
- **THEN** il testo viene troncato al limite oppure l'inserimento oltre il limite viene impedito, senza errori né perdita delle altre label

#### Scenario: Numero massimo di label raggiunto

- **WHEN** la carta ha già il numero massimo di label e l'utente tenta di aggiungerne un'altra
- **THEN** l'aggiunta non avviene, viene segnalato il limite e le label esistenti restano invariate

### Requirement: Ciclo di vita implicito delle label

Le label SHALL esistere solo in quanto assegnate ad almeno una carta attiva: il sistema MUST NOT mantenere un'anagrafica separata delle label. L'insieme delle label disponibili (per suggerimenti e filtro) SHALL essere derivato dalle carte attive, ordinato alfabeticamente, con i tombstone esclusi.

#### Scenario: Label senza più carte

- **WHEN** l'ultima carta che usava una label viene modificata togliendo quella label
- **THEN** la label non compare più tra i suggerimenti né tra i filtri disponibili

#### Scenario: Label di una carta cancellata

- **WHEN** l'ultima carta che usava una label viene eliminata (tombstone)
- **THEN** la label non compare più tra i suggerimenti né tra i filtri disponibili

#### Scenario: Elenco ordinato

- **WHEN** più carte usano label diverse
- **THEN** l'elenco delle label disponibili le mostra una sola volta ciascuna, in ordine alfabetico
