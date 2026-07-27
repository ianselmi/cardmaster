## Context

Stato attuale rilevante:

- `Card` ha già un campo `Color` (hex, opzionale), popolato **automaticamente** dal catalogo emittenti (`issuer.ColorHex`, es. `#E2001A`) in creazione e in modifica, e dal payload QR in ricezione. **Nessuna schermata lo usa**: il riquadro della lista prende il colore da `NameToTileColorConverter` → `CardTilePalette.ForName(DisplayName)`, hash FNV-1a stabile su una palette di 10 tinte scure pensate per il testo bianco.
- La lista carte (`CardListViewModel`) carica **tutte** le carte attive in memoria e filtra in memoria (`ApplyFilter`), con normalizzazione case/accent-insensitive; da lì derivano `CountText` e i due stati vuoti.
- Persistenza con **sqlite-net** (`SQLiteAsyncConnection`), schema applicato da `DatabaseService.GetConnectionAsync()` con `CreateTableAsync<Card>()` e `PRAGMA user_version` (`SchemaVersion = 1`). Il ripristino da backup Drive è consentito solo se `backupSchemaVersion <= CurrentSchemaVersion` (`BackupNaming.CanRestore`).
- Vincoli di progetto: offline-first, nessuna nuova dipendenza necessaria, Id client-generati e cancellazioni logiche, `dotnet build` a zero errori come criterio di accettazione.

Le scelte di prodotto sono state fissate con l'utente: palette curata + "Automatico" per il colore; chip multi-selezione in **OR** per il filtro; colore e label disponibili **sia in modifica sia in creazione**.

## Goals / Non-Goals

**Goals:**

- Colore del riquadro scelto dall'utente da una palette che garantisce il contrasto con il testo bianco già usato dal tile, con ritorno al colore automatico.
- Label libere per carta, create digitandole, riusabili tramite suggerimento, senza schermate di gestione.
- Filtro per label nella lista che convive con la ricerca testuale esistente senza riscriverla.
- Zero cambiamenti d'aspetto per le carte già salvate.
- Nessuna nuova dipendenza; nessun impatto su scansione, rendering barcode, condivisione, backup, update.

**Non-Goals:**

- Anagrafica delle label (rinomina/eliminazione globale, colore per label, ordinamento manuale).
- Label mostrate sul riquadro della griglia.
- Label nel payload QR di condivisione.
- Colore arbitrario (hex/HSV) fuori dalla palette.
- Filtro persistito tra le sessioni.

## Decisions

### 1. Nuovo campo `TileColor`, distinto dal `Color` esistente

Il colore scelto dall'utente va in un **nuovo** campo `TileColor` (hex, nullable; `null` = automatico). Il campo `Color` esistente conserva il suo significato attuale: colore di *brand* dell'emittente, ereditato dal catalogo o dal QR ricevuto.

Perché non riusare `Color`: è già valorizzato su tutte le carte con emittente da catalogo. Farlo diventare il colore del riquadro cambierebbe di colpo l'aspetto delle carte esistenti al primo avvio dopo l'aggiornamento — un cambiamento che l'utente non ha chiesto — e i colori di brand (rossi accesi, gialli, azzurri chiari) non sono selezionati per reggere il testo bianco del tile, a differenza della palette curata. Tenerli separati costa una colonna e rende la regola leggibile: *l'utente vince sull'automatismo, l'automatismo resta il default*.

Alternative scartate:
- *Riusare `Color`*: rottura visiva silenziosa e contrasto non garantito (sopra).
- *Migrare `Color` → `TileColor` una tantum al primo avvio*: stessa rottura visiva, più codice di migrazione una tantum da mantenere.

### 2. Risoluzione del colore del riquadro: `TileColor ?? ForName(DisplayName)`

Un unico punto di verità, `CardTilePalette.ForCard(card)`, usato sia dalla griglia sia dalla barra "usate di recente". Il converter passa da `NameToTileColorConverter` (input: `DisplayName`) a un converter che riceve **la carta intera**, così la regola non si duplica nel XAML. `ForName` resta pubblico: è la definizione del colore automatico ed è ciò che l'anteprima "Automatico" mostra.

La palette selezionabile è la stessa costante `CardTilePalette.Colors` già in uso, esposta come `IReadOnlyList<Color>`: chi sceglie dalla palette ottiene esattamente uno dei colori che l'app assegnerebbe da sola, quindi la lista resta coerente.

### 3. Label serializzate in una colonna della carta, non in tabelle separate

Le label di una carta stanno in **una colonna testuale** di `Card` (`LabelsCsv`, separatore `|`, carattere vietato nel testo di una label). Un campo `[Ignore] Labels` espone la lista tipizzata; `CardLabels` (statica) fa parse/serialize/normalizzazione.

Perché non `Label` + `CardLabel` normalizzate: con carte nell'ordine delle decine e filtro già interamente in memoria, le tabelle non comprano nulla in prestazioni, mentre costano due entità con Id client-generati e **tombstone propri** da riconciliare nella sync v2 — una relazione molti-a-molti è il caso peggiore per il last-write-wins per riga previsto in v2. Con le label sulla carta, modificare le label è una scrittura sulla stessa riga: `UpdatedAt` la copre già e la sync resta quella prevista.

Perché separatore e non JSON: il valore va anche letto a occhio nei backup/db di debug, e la normalizzazione (sotto) vieta già il separatore. `CardShareCodec` usa JSON perché lì conta la compattezza del QR; qui conta la semplicità.

**Normalizzazione**, applicata al salvataggio in un solo punto (`CardLabels.Normalize`): trim, spazi interni collassati, `|` e caratteri di controllo rimossi, lunghezza massima 24 caratteri, massimo 8 label per carta, deduplicazione **case/accent-insensitive** (riusando la stessa `Normalize` della ricerca, estratta in un helper condiviso) che conserva la grafia della prima occorrenza. L'utente digita "Spesa" dopo aver usato "spesa" e ottiene una sola label, non due.

### 4. Le label esistono solo se assegnate; i suggerimenti si derivano dalle carte

Nessuna tabella di anagrafica: l'insieme delle label è `SelectMany` sulle carte attive, distinto e ordinato alfabeticamente. Ne discende che una label sparisce da sé quando l'ultima carta che la usava viene modificata o cancellata — che è il comportamento atteso e non richiede manutenzione. La stessa derivazione alimenta sia i suggerimenti nell'editor sia i chip del filtro nella lista.

### 5. Filtro: OR tra le label selezionate, AND con la ricerca testuale

`ApplyFilter` resta l'unico punto di filtro: alla condizione testuale esistente si aggiunge, quando c'è almeno una label selezionata, `card.Labels.Any(l => selected.Contains(l))` sul confronto normalizzato. Conteggio (`CountText`) e stato vuoto continuano a leggere il risultato di `ApplyFilter`, quindi funzionano senza modifiche strutturali; cambia solo il testo dello stato vuoto per dire che il filtro attivo può essere la causa dello zero risultati.

I chip sono un `CollectionView` orizzontale su una collezione di piccoli view-model `LabelFilterItem { Name, IsSelected }` con `TapGestureRecognizer` e stile guidato da `IsSelected` — anziché `SelectionMode="Multiple"`, che su Android rende male il tocco ripetuto per deselezionare e non dà un aggancio pulito allo stile "chip selezionato". La riga di chip è nascosta quando non esiste ancora nessuna label, così chi non usa la funzione non vede nulla di nuovo.

Selezioni che restano orfane (l'ultima carta con quella label perde la label) vengono potate al ricaricamento della lista, per non lasciare un filtro attivo che nasconde tutto senza chip visibile a cui darne colpa.

### 6. Editor delle label: entry + chip rimovibili + suggerimenti

MAUI non ha un controllo "chip input": la composizione è un `Entry` con pulsante "Aggiungi" (invio da tastiera equivalente), sotto i chip già assegnati ciascuno con una ✕ per rimuoverlo, e sotto ancora i suggerimenti (label usate su altre carte e non ancora su questa) che si aggiungono al tocco. La stessa vista è condivisa fra `AddCardPage` e `EditCardPage`; la logica sta nei due ViewModel, che espongono la stessa forma di API (`Labels`, `Suggestions`, `AddLabel`, `RemoveLabel`).

Il selettore di colore è una riga di pastiglie della palette più una pastiglia "Automatico" che mostra in anteprima il colore derivato dal nome corrente: selezionandola `TileColor` torna a `null`.

### 7. Versione di schema del database a 2

`DatabaseService.SchemaVersion` passa da 1 a 2. Le colonne nuove le aggiunge `CreateTableAsync<Card>()` di sqlite-net sulle installazioni esistenti (`ALTER TABLE ADD COLUMN`, valori `NULL` = comportamento attuale), quindi non serve codice di migrazione; l'incremento serve alla guardia del backup: un backup prodotto da questa versione (`v2`) non viene ripristinato da una copia più vecchia dell'app, mentre i backup `v1` già su Drive restano ripristinabili (`CanRestore: 1 <= 2`).

## Risks / Trade-offs

- **Le carte esistenti con emittente da catalogo non ereditano il colore di brand** → è la scelta di §1 e va detta esplicitamente: il colore di brand resta inutilizzato come oggi. Se un domani lo si vorrà usare, sarà una change a sé con il suo passaggio di conferma, non un effetto collaterale di questa.
- **Label libere → proliferazione di quasi-duplicati** ("spesa", "Spesa ", "Spese") → dedup case/accent-insensitive e suggerimenti in cima all'editor riducono i primi due casi; il terzo è una scelta dell'utente e, senza anagrafica, si corregge solo carta per carta. Accettato: il costo di una schermata di gestione non è giustificato alle dimensioni attuali.
- **Colonna serializzata non interrogabile in SQL** → il filtro è in memoria, che è già come funziona la ricerca; se un domani le carte diventassero migliaia, il collo di bottiglia sarebbe `GetAllAsync`, non le label.
- **Il separatore `|` viene tolto dal testo delle label** → perdita silenziosa di un carattere che nessuno usa in un'etichetta; documentata nella normalizzazione, non segnalata all'utente con un errore.
- **Due campi colore su `Card` (`Color` e `TileColor`)** → ambiguità per chi legge il modello; mitigata dai commenti XML sui due campi e dal fatto che `ForCard` è l'unico consumatore del colore del tile.
- **Payload QR invariato** → una carta condivisa arriva senza label e senza colore scelto (eredita brand color e colore automatico come oggi). Coerente con "il QR è uno snapshot della carta fedeltà, non dell'organizzazione personale di chi la manda", e tiene il QR compatto.

## Migration Plan

1. Aggiornamento dell'app: al primo avvio `CreateTableAsync<Card>()` aggiunge le due colonne; le carte esistenti hanno `TileColor = NULL` e nessuna label, quindi lista e ricerca si comportano esattamente come prima.
2. `PRAGMA user_version` passa a 2 alla stessa apertura.
3. Rollback: reinstallando una versione precedente dell'app il database resta leggibile (colonne ignote a sqlite-net vengono ignorate), ma i backup prodotti nel frattempo (`-v2`) non sono ripristinabili da quella versione — comportamento voluto della guardia.

## Open Questions

Nessuna: colore (palette + Automatico), filtro (chip multi-selezione OR) e ambito (modifica **e** creazione) sono stati decisi con l'utente prima della stesura.
