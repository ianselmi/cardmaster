## Why

Oggi il colore di un riquadro della lista carte è calcolato dall'app (hash del nome) e l'utente non può cambiarlo: due carte che l'utente percepisce come simili possono finire con colori scorrelati, e la carta cercata a colpo d'occhio non è dove il colpo d'occhio se l'aspetta. Allo stesso tempo, con l'aumentare delle carte la ricerca testuale non basta più: manca un modo per raggruppare le carte per uso ("spesa", "benzina", "palestra") e vederne solo un gruppo.

Le due cose si risolvono nello stesso punto — la scheda di una carta — e agiscono sulla stessa pagina (la lista): il colore rende la carta riconoscibile, le label la rendono filtrabile.

## What Changes

- **Colore scelto dall'utente**: nella schermata di modifica (e in quella di conferma dopo l'acquisizione) si sceglie il colore del riquadro da una **palette curata**, con l'opzione **"Automatico"** che ripristina il colore derivato dal nome (comportamento attuale, e default per tutte le carte esistenti).
- **Label per carta**: da modifica e da creazione si assegnano a una carta zero o più **label** testuali libere, create al volo digitandole e riproposte come suggerimenti dalle label già usate su altre carte. Le label esistono solo in quanto assegnate a una carta: nessuna anagrafica da gestire, nessuna schermata di amministrazione.
- **Filtro per label nella lista**: sopra la griglia compare una riga di **chip**, una per label esistente. Selezionandone una o più si vedono le carte che hanno **almeno una** delle label scelte (OR). Il filtro si combina in **AND** con la ricerca testuale già presente e alimenta l'indicatore del conteggio e lo stato vuoto esistenti.
- **Il riquadro onora il colore scelto**: la lista (griglia e barra "usate di recente") usa il colore esplicito della carta quando c'è, altrimenti quello derivato dal nome. Nessuna carta esistente cambia aspetto.
- Nessuna nuova dipendenza, nessuna funzione di rete, nessun impatto sul flusso di scansione o sul rendering del barcode.

Fuori scope (deliberatamente): rinomina/eliminazione globale di una label da una schermata dedicata, label mostrate sul riquadro, label trasportate nel QR di condivisione.

## Capabilities

### New Capabilities
- `card-labels`: le label come attributo di una carta — assegnazione, creazione al volo con suggerimenti dalle label già in uso, normalizzazione e deduplicazione, limiti, ciclo di vita implicito (una label sparisce quando nessuna carta la usa più).

### Modified Capabilities
- `card-editing`: la schermata di modifica acquisisce due campi editabili — colore del riquadro (palette + Automatico) e label della carta.
- `card-capture`: la schermata di conferma dopo l'acquisizione (camera, immagine, inserimento manuale) acquisisce gli stessi due campi, entrambi opzionali e con default che non rallentano il flusso.
- `card-list`: il colore di sfondo del riquadro è quello scelto per la carta quando presente; il colore derivato dal nome resta il comportamento di default.
- `card-search`: la lista offre il filtro per label a chip multi-selezione (OR), combinato in AND con la ricerca testuale; conteggio e stato vuoto tengono conto anche di questo filtro.

## Impact

- **Modello dati** (`Data/Card.cs`): due nuovi campi persistiti — colore scelto dall'utente e label della carta. Colonne aggiunte al volo da `CreateTableAsync<Card>()` sulle installazioni esistenti (valori nulli = comportamento attuale); nessuna migrazione dei dati, nessun DELETE. Versione di schema del database incrementata, così un backup nuovo non viene ripristinato da una versione vecchia dell'app.
- **UI**: `Views/EditCardPage.xaml`, `Views/AddCardPage.xaml` (editor colore + label), `Views/CardListPage.xaml` (riga di chip di filtro, binding del colore del riquadro).
- **ViewModel**: `EditCardViewModel`, `AddCardViewModel` (nuovi campi e salvataggio), `CardListViewModel` (raccolta delle label, stato del filtro, filtro combinato, conteggio).
- **Servizi**: nessuna nuova interfaccia; `ICardRepository` invariato (il salvataggio passa da `AddAsync`/`UpdateAsync` come oggi). `Views/CardTilePalette.cs` estesa per esporre la palette selezionabile.
- **Non toccati**: scansione e catalogo formati, rendering del barcode, condivisione QR (payload invariato), backup Drive (il file db3 resta un blob opaco), aggiornamenti in-app.
