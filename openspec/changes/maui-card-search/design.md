## Context

`CardListPage` mostra oggi tutte le carte attive come griglia a 2 colonne (`card-list`), caricate una volta in `CardListViewModel.LoadAsync()` in una `ObservableCollection<Card>` semplice. Non esiste ricerca, non esiste un concetto di "carta usata di recente", e `CardListViewModel` non implementa `INotifyPropertyChanged` (nessuna proprietà bindabile oltre alla collezione).

Il dataset è locale al device e di dimensione personale (decine di carte, non migliaia): non serve un motore di ricerca, basta un filtro in-memory sulla lista già caricata.

## Goals / Non-Goals

**Goals:**
- Filtrare la griglia per nome/emittente con un match tollerante (case e accent-insensitive).
- Mostrare quante carte sono visibili rispetto al totale.
- Dare un accesso rapido alle ultime 3 carte aperte.
- Tracciare l'ultimo utilizzo di una carta in modo che sopravviva a backup/restore senza migrazioni manuali.

**Non-Goals:**
- Nessuna ricerca full-text avanzata (fuzzy matching, ranking, tokenizzazione): un `Contains` normalizzato basta.
- Nessuna query SQL per il filtro: si opera sulla collezione già caricata in memoria.
- Nessun cambiamento allo schema di `local-storage` (Id, tombstone, init) né alla guardia di versione di `cloud-backup`.
- Nessuna modifica al layout della griglia stessa (resta quella di `card-list`).

## Decisions

### 1. `LastUsedAt` come campo nullable su `Card`, nessuna migrazione manuale
`Card` guadagna `DateTimeOffset? LastUsedAt`. `DatabaseService.GetConnectionAsync()` chiama già `connection.CreateTableAsync<Card>()` a ogni apertura (avvio app e, soprattutto, dopo `ReplaceFromAsync` nel restore da backup Drive): sqlite-net-pcl aggiunge le colonne mancanti con `ALTER TABLE ADD COLUMN` in modo idempotente. Un backup più vecchio ripristinato su un'app con questa change ottiene la colonna con default `NULL` automaticamente — coerente con l'aspettativa "carte mai aperte" per dati storici. Non serve incrementare `SchemaVersion` (resta un'aggiunta additiva, non un cambio incompatibile) e non tocca la guardia "MUST NOT restore schema più recente" di `cloud-backup`, che riguarda il downgrade, non l'upgrade additivo.

**Alternativa scartata**: tabella separata `CardUsage(CardId, LastUsedAt)`. Più "corretta" relazionalmente ma inutile per un solo campo opzionale 1:1 con `Card`; aggiunge un join per un caso d'uso che non lo richiede.

### 2. Punto di aggiornamento: `ShowCardViewModel.LoadAsync`
"Carta usata" = apertura della pagina che mostra il barcode. `LoadAsync()` ha già un guard `_loaded` che impedisce ricariche su semplice `OnAppearing` ripetuto (es. ritorno da background); il touch di `LastUsedAt` va dentro lo stesso ramo che valorizza `DisplayName`/`BarcodeValue` la prima volta, così un `ReloadAsync()` dopo una modifica (che resetta `_loaded` a `false` apposta per ricaricare i dati aggiornati) NON conta come nuovo utilizzo — è un refresh dei dati, non un'apertura da parte dell'utente. Il repository espone un metodo dedicato (`TouchLastUsedAsync(id)`) invece di passare per `UpdateAsync(card)` per evitare di toccare `UpdatedAt` (riservato a modifiche dei dati) per un semplice evento di lettura.

### 3. Filtro in-memory, non a livello di repository
`CardListViewModel` mantiene la lista completa caricata da `GetAllAsync()` e deriva `FilteredCards` applicando il filtro testuale in memoria ad ogni cambio di `SearchText`. Normalizzazione: `string.Normalize(NormalizationForm.FormD)` + rimozione dei combining marks (`UnicodeCategory.NonSpacingMark`) + `ToLowerInvariant()`, sia sul testo cercato sia su `DisplayName`/`IssuerName`, per ottenere il confronto case/accent-insensitive deciso in fase di esplorazione (es. "citta" trova "Città").

**Alternativa scartata**: query SQLite con `LIKE` e `COLLATE NOCASE`. Gestisce il case ma non gli accenti senza estensioni ICU non disponibili nel provider usato; e per poche decine di righe una query ad ogni tasto non offre vantaggi misurabili.

### 4. Collezioni derivate esposte dal ViewModel
`CardListViewModel` diventa `ObservableObject` (come già `ShowCardViewModel`) con:
- `SearchText` (settable, triggera il ricalcolo)
- `FilteredCards: ObservableCollection<Card>` — quello che la griglia mostra
- `RecentCards: ObservableCollection<Card>` — le carte con `LastUsedAt` non nullo, ordinate discendente, prime 3; ricalcolata solo al `LoadAsync()` (non dipende da `SearchText`, resta sempre visibile durante la ricerca come deciso)
- `HasRecentCards: bool` — per nascondere del tutto la barra quando nessuna carta è mai stata aperta
- `CountText: string` — `"{totale} carte"` a riposo, `"{filtrate}/{totale}"` quando `SearchText` non è vuoto

### 5. Formato del contatore
A riposo mostra solo il totale ("30 carte"); con ricerca attiva passa a "N/M" (es. "5/30"), posizionato subito sotto la `SearchBar` — decisioni prese esplicitamente in fase di esplorazione per evitare ridondanza visiva quando non si sta filtrando.

## Risks / Trade-offs

- **[Rischio] Normalizzazione Unicode con nomi non latini** (es. emittenti con caratteri CJK) → `NormalizationForm.FormD` non altera caratteri senza combining marks, quindi il fallback è un confronto case-insensitive semplice: nessuna regressione, solo nessun beneficio aggiuntivo per quell'alfabeto.
- **[Rischio] `RecentCards` non si aggiorna finché non si torna sulla lista** → accettabile: `CardListPage.OnAppearing()` richiama già `LoadAsync()` ad ogni ritorno alla pagina, quindi la barra riflette l'uso più recente ogni volta che l'utente la vede.
- **[Trade-off] Filtro in-memory non scala oltre qualche migliaio di carte** → coerente con il dominio (carte fedeltà personali); se mai diventasse un problema si può reintrodurre una query, ma non ora (non-goal esplicito).

## Migration Plan

Nessuna migrazione dati richiesta: colonna nullable auto-aggiunta da sqlite-net sia su DB esistenti sia su restore di backup precedenti (vedi Decisione 1). Nessun rollback speciale: disinstallare la change equivale a ignorare la colonna, che resta innocua.

## Open Questions

Nessuna al momento: le decisioni di UX (match accent-insensitive, formato contatore, visibilità sempre-on della barra recenti) sono state chiuse in fase di esplorazione con l'utente prima di questa proposta.
