## Why

Con più di poche carte salvate, la griglia a 2 colonne (`card-list`) diventa lunga da scorrere: non c'è modo di cercare una carta per nome/emittente, né di arrivare rapidamente a quelle usate più spesso. Serve una ricerca testuale e una scorciatoia verso le carte aperte di recente.

## What Changes

- Aggiunge una `SearchBar` sopra la griglia che filtra le carte per nome ed emittente, in-memory sulla lista già caricata, con confronto case/accent-insensitive.
- Aggiunge un indicatore del numero di carte: il totale a riposo ("30 carte"), il rapporto trovate/totale mentre si filtra ("5/30").
- Aggiunge una barra orizzontale "Usate di recente" con le ultime 3 carte aperte, sempre visibile, assente se nessuna carta è mai stata aperta.
- Introduce il tracciamento dell'ultimo utilizzo di una carta (`LastUsedAt`), valorizzato alla prima apertura della pagina di visualizzazione barcode (non sui reload, es. dopo una modifica).
- Converte `CardListViewModel` da classe semplice a `ObservableObject` per esporre `SearchText` bindabile e le collezioni derivate (lista filtrata, lista recenti).

## Capabilities

### New Capabilities
- `card-search`: ricerca testuale tra le carte, indicatore del conteggio filtrato, e barra delle carte usate di recente basata su un nuovo timestamp di ultimo utilizzo.

### Modified Capabilities
(nessuna: `card-list` mantiene invariati i propri requisiti sulla griglia; il nuovo campo `LastUsedAt` è un dettaglio di modello aggiuntivo e non tocca i requisiti di `local-storage`, che riguardano solo Id/tombstone/inizializzazione)

## Impact

- `src/CardMaster/Data/Card.cs`: nuovo campo `LastUsedAt` (nullable), auto-aggiunto da sqlite-net alla riapertura del DB (nessuna migrazione manuale, coerente col comportamento già osservato in `ReplaceFromAsync`/restore).
- `src/CardMaster/ViewModels/CardListViewModel.cs`: refactor a `ObservableObject`, nuove proprietà `SearchText`, `FilteredCards`, `RecentCards`, `CountText`.
- `src/CardMaster/ViewModels/ShowCardViewModel.cs`: valorizza `LastUsedAt` alla prima apertura riuscita della carta.
- `src/CardMaster/Services/ICardRepository.cs` / `CardRepository.cs`: nuovo metodo per marcare l'uso (touch `LastUsedAt`) e/o per leggere le carte più recenti.
- `src/CardMaster/Views/CardListPage.xaml`: nuova `SearchBar`, label del contatore, `CollectionView` orizzontale per le recenti.
