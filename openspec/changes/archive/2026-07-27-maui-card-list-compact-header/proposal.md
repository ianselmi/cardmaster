## Why

Nella lista carte l'area sopra la griglia — barra di ricerca, conteggio, chip delle label, intestazione "Usate di recente" e barra dei recenti — occupa circa **280dp**, oltre un terzo dell'altezza utile su un telefono tipico. Con il banner degli aggiornamenti visibile si arriva a sfiorare la metà dello schermo. Il risultato è che le carte, che sono il contenuto per cui l'app esiste, entrano in scena a metà pagina: a riposo se ne vedono poco più di una riga.

L'area è cresciuta per accumulo, una change alla volta (ricerca e recenti con `maui-card-search`, chip con `maui-card-color-labels`), senza che nessuno l'abbia mai guardata nel suo insieme.

## What Changes

- **Il conteggio perde la riga dedicata** e compare **solo quando è attivo un filtro** (testo o label), condividendo la riga dei chip. A riposo sparisce: il totale non serve a chi sta solo guardando le proprie carte. *(Decisione 27 lug 2026.)*
- **La barra "Usate di recente" perde la riga di intestazione testuale**, restando per il resto **invariata**: sempre visibile, anche durante ricerca e filtro, come previsto oggi. *(Decisione 27 lug 2026: la barra resta, si toglie solo la didascalia.)*
- **Le altezze di chip e barra recenti si riducono**, attingendo alla scala di spaziatura condivisa di `visual-identity` invece che a valori sparsi nel XAML.
- **Le righe non visibili smettono di occupare spazio.** Oggi una riga nascosta (chip senza label, banner assente) lascia comunque la spaziatura tra righe della `Grid`: qualche pixel per riga, che si sommano proprio nel caso più comune, quello di chi non usa le label.
- **Obiettivo verificabile**: a riposo la testata NON MUST occupare più di **un terzo** dello spazio sotto la barra del titolo. Misura di partenza sull'emulatore: **38%** (810px di 2116) senza banner, che sale al 51% con il banner visibile.

Fuori scope: rendere la testata scorrevole insieme alla griglia (valutata e scartata, vedi `design.md`), cambiare il numero di colonne o la dimensione dei riquadri, toccare il banner degli aggiornamenti.

## Capabilities

### New Capabilities
Nessuna.

### Modified Capabilities
- `card-search`: il conteggio compare solo con un filtro attivo e non ha più una riga propria; la barra dei recenti non ha più un'intestazione testuale; l'insieme di ricerca, conteggio, chip e recenti è vincolato a occupare il minimo spazio verticale, e gli elementi non visibili non ne occupano affatto.

## Impact

- **Codice**: `Views/CardListPage.xaml` (ristrutturazione della testata), `ViewModels/CardListViewModel.cs` (visibilità del conteggio legata alla presenza di un filtro).
- **Risorse**: eventuali nuovi valori nella scala di spaziatura condivisa (`Resources/Styles/Styles.xaml`), coerenti con `visual-identity`.
- **Non toccati**: modello dati, ricerca e filtro (la logica resta identica: cambia solo *quando* il conteggio si mostra), apertura delle carte, tutte le altre pagine.
- **Nessun rischio di perdita dati**: la change è interamente di presentazione.
