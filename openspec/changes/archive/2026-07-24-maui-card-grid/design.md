## Context

`CardListPage` mostra oggi una `CollectionView` a lista testuale (nome + emittente), ereditata da `maui-shell`. Con `maui-scan-card` l'utente crea carte reali; serve una resa più riconoscibile: una griglia di riquadri colorati. È una modifica di sola presentazione: dati, DB, servizi e navigazione non cambiano.

## Goals / Non-Goals

**Goals:**
- Griglia a 2 colonne di riquadri quadrati con angoli arrotondati.
- Riquadro con nome (+ emittente) e testo a contrasto.
- Colore di sfondo deterministico per carta (palette curata), stabile e distribuito.
- Empty state invariato. Compilazione senza errori.

**Non-Goals:**
- Apertura carta / rendering barcode al tap → `maui-show-card`.
- Riordino, ricerca, filtri, animazioni.
- Loghi emittente nei riquadri (il seed non ha loghi ufficiali in v1).
- Uso del colore dell'emittente (scelta esplicita: colore generato dal nome).

## Decisions

### Layout — `CollectionView` con `GridItemsLayout(Span=2, Vertical)`
Si sostituisce il layout a lista con `GridItemsLayout` a 2 colonne. Ogni item è un riquadro (`Border` con `StrokeShape` `RoundRectangle` per gli angoli arrotondati) con padding interno e spaziatura tra i tile.

### Forma quadrata dei riquadri
MAUI non ha un aspect-ratio nativo per gli item. Per "quadrettoni" si usa un `HeightRequest` fisso ragionevole sul contenuto del tile (es. ~150–170), che su 2 colonne dà riquadri percettivamente quadrati sulla maggior parte dei telefoni. Evita binding larghezza→altezza complessi; sufficiente per lo scopo visivo.

### Colore generato — palette + hash del nome
Un helper `CardTilePalette` espone una palette curata di N colori (mid/saturi, leggibili con testo bianco). Il colore di una carta si ottiene con `indice = stableHash(DisplayName) % N`. Lo hash deve essere **stabile** (non `string.GetHashCode()`, che varia tra run/processi): si usa un hash deterministico semplice (es. FNV-1a sui caratteri). Un `IValueConverter` (`NameToTileColorConverter`) applica la logica in binding su `DisplayName`.
- **Alternative considerate**: colore dell'emittente → scartato per scelta dell'utente (molte carte senza emittente resterebbero neutre); colore casuale a runtime → non stabile tra riaperture.

### Testo a contrasto
La palette è scelta con colori sufficientemente scuri/saturi da usare **testo bianco** in modo leggibile, evitando il calcolo della luminanza per ogni item. Nome in evidenza, emittente in secondaria (opacità ridotta).

## Risks / Trade-offs

- **Riquadri non perfettamente quadrati su tutti i display** (per l'altezza fissa) → accettabile; l'effetto "quadrettone arrotondato" è comunque reso. Se necessario si affinerà.
- **Contrasto testo su alcuni colori** → palette curata con testo bianco; se un colore risultasse critico, si aggiusta la palette.
- **Collisioni di colore** (nomi diversi, stesso indice) → inevitabili con palette finita; accettabile, non è un identificatore.

## Migration Plan

Nessuna migrazione: modifica di sola UI. Rollback = ripristino del template a lista. Nessun cambiamento a dati o schema.

## Open Questions

- Dimensione esatta dei tile e numero di colori in palette: da rifinire in fase di apply/verifica visiva sull'emulatore.
