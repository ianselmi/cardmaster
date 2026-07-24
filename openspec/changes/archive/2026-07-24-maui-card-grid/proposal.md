## Why

La lista carte è oggi una semplice lista testuale (segnaposto ereditato da `maui-shell`). Ora che l'utente può creare carte (`maui-scan-card`), serve una presentazione più riconoscibile a colpo d'occhio: una **griglia di riquadri colorati** ("quadrettoni" con angoli arrotondati), più adatta a scorrere e distinguere le carte fedeltà.

## What Changes

- La pagina lista carte passa da lista testuale a **griglia a 2 colonne** di riquadri quadrati con **angoli arrotondati**.
- Ogni riquadro mostra il **nome** della carta (ed eventuale emittente) su uno sfondo colorato.
- Il **colore di sfondo** è generato in modo **deterministico** dal nome della carta (stessa carta → sempre lo stesso colore), da una palette curata. Non usa il colore dell'emittente.
- Il **testo** sul riquadro usa un colore a contrasto leggibile.
- L'**empty state** ("Nessuna carta ancora") resta invariato.

## Capabilities

### New Capabilities
- `card-list`: presentazione delle carte salvate come griglia di riquadri (tile) con colore generato per carta. Definisce il layout e la resa visiva della lista, distinta dallo scaffolding di navigazione (`app-shell`) e dalla creazione carte (`card-capture`).

### Modified Capabilities
- Nessuna. (Il segnaposto lista di `app-shell` resta valido come scaffolding; questa change ne definisce la resa reale in una capability dedicata.)

## Impact

- **Codice modificato**: `CardListPage.xaml` (da `CollectionView` a lista → `CollectionView` con `GridItemsLayout` a 2 colonne + template a riquadri).
- **Nuovo codice**: helper della palette + converter per derivare il colore dal nome; eventuale piccolo stile per i riquadri.
- **Nessun impatto** su dati, DB, servizi o navigazione: è una modifica di sola presentazione.
- **Vincolo di qualità**: la soluzione deve **compilare senza errori** (`dotnet build`), criterio di accettazione.
- **Fuori scope**: apertura della carta al tap (rendering barcode) → `maui-show-card`.
