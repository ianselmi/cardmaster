## 1. Modello dati e repository

- [x] 1.1 Aggiungere `DateTimeOffset? LastUsedAt` a `Card` (src/CardMaster/Data/Card.cs)
- [x] 1.2 Aggiungere `Task TouchLastUsedAsync(string id)` a `ICardRepository` e implementarla in `CardRepository` (aggiorna solo `LastUsedAt`, non `UpdatedAt`)
- [x] 1.3 Aggiungere `Task<List<Card>> GetRecentlyUsedAsync(int count)` a `ICardRepository` e implementarla in `CardRepository` (carte attive con `LastUsedAt` non nullo, ordinate discendente, limitate a `count`)

## 2. Tracciamento dell'ultimo utilizzo

- [x] 2.1 In `ShowCardViewModel.LoadAsync()`, dopo il caricamento riuscito della carta, chiamare `TouchLastUsedAsync(_cardId)`
- [x] 2.2 Verificare che `ReloadAsync()` (usato dopo la modifica) non generi un secondo touch (flag dedicato `_lastUsedTouched`, mai resettato da `ReloadAsync`)

## 3. Refactor di CardListViewModel

- [x] 3.1 Convertire `CardListViewModel` da classe semplice a `ObservableObject`
- [x] 3.2 Aggiungere proprietà `SearchText` (settable, ricalcola il filtro on-change)
- [x] 3.3 Aggiungere `ObservableCollection<Card> FilteredCards`, popolata dalle carte filtrate per nome/emittente con confronto case/accent-insensitive (normalizzazione Unicode, vedi design.md)
- [x] 3.4 Aggiungere `ObservableCollection<Card> RecentCards`, popolata da `GetRecentlyUsedAsync(3)` in `LoadAsync()`
- [x] 3.5 Aggiungere `bool HasRecentCards` (true se `RecentCards` non è vuota)
- [x] 3.6 Aggiungere `string CountText` che restituisce `"{totale} carte"` a riposo o `"{filtrate}/{totale}"` con `SearchText` non vuoto

## 4. UI della pagina lista carte

- [x] 4.1 Aggiungere una `SearchBar` sopra la griglia in `CardListPage.xaml`, bindata a `SearchText`
- [x] 4.2 Aggiungere una `Label` per `CountText` sotto la `SearchBar`
- [x] 4.3 Aggiungere una `CollectionView` orizzontale per `RecentCards` (visibile solo se `HasRecentCards`), con lo stesso stile tile della griglia principale ma in dimensione ridotta
- [x] 4.4 Cablare la selezione nella barra dei recenti alla stessa navigazione verso `ShowCardPage` già usata per la griglia (stesso handler `OnCardSelected` su entrambe le `CollectionView`)
- [x] 4.5 Aggiornare il binding della griglia principale da `Cards` a `FilteredCards`
- [x] 4.6 Aggiungere uno stato vuoto distinto per "nessun risultato di ricerca" (diverso da "nessuna carta salvata")

## 5. Verifica

- [x] 5.1 `dotnet build` senza errori
- [x] 5.2 Verifica manuale: cercare per nome ed emittente, con e senza accenti/maiuscole (su emulatore: "CARTA" → 1/1, "xyz" → stato vuoto distinto "Nessuna carta trovata")
- [x] 5.3 Verifica manuale: aprire una carta, controllare che compaia nella barra "Usate di recente" al ritorno alla lista
- [x] 5.4 Verifica manuale: contatore corretto a riposo ("1 carta") e durante il filtro ("0/1", "1/1")
