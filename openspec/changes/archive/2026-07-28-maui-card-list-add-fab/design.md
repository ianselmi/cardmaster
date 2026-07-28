## Context

`CardListPage.xaml` è oggi un `Grid RowDefinitions="Auto,*"`: riga 0 la testata in `VerticalStackLayout` (banner aggiornamento, ricerca, riga filtro, recenti), riga 1 la `CollectionView` a griglia con `GridItemsLayout Span="2"`. L'aggiunta carta è un `ToolbarItem Text="Aggiungi"` con handler `OnAddClicked` che naviga a `ScanPage` via Shell.

Vincoli rilevanti:

- .NET MAUI **non ha un controllo FAB**: va composto con i controlli esistenti.
- `Styles.xaml` ha uno stile **implicito** per `Button` (sfondo `Primary`/`PrimaryDark`, testo bianco, `CornerRadius=8`, padding `14,10`, min 44x44) e uno **implicito** per `Border` (stroke grigio, `StrokeThickness=1`, `StrokeShape=Rectangle`).
- minSdk **Android 23** (vedi `docs/technical-notes.md`).
- `maui-card-list-compact-header` ha stabilito che le dimensioni della cronaca di pagina (altezze testata) vivono come risorse in `Styles.xaml`, non sparse nel XAML.

## Goals / Non-Goals

**Goals:**

- Azione primaria "aggiungi carta" raggiungibile col pollice, in basso al centro, riconoscibile come bottone tondo con `+`.
- Zero altezza sottratta alla griglia: il bottone è sovrapposto, non impilato.
- Nessun cambiamento al flusso di acquisizione, ai dati, ai permessi, alle dipendenze.

**Non-Goals:**

- Non si introduce una libreria di componenti Material/FAB.
- Non si aggiungono azioni secondarie, menu espandibili o FAB "extended" con etichetta.
- Non si tocca la voce Impostazioni né il suo badge di aggiornamento.
- Non si cambia posizione o comportamento del banner di aggiornamento.

## Decisions

### Bottone tondo = `Button` con `CornerRadius` metà del lato, non `Border` + `TapGestureRecognizer`

Un `Button` 56x56 con `CornerRadius="28"` è un cerchio pieno su Android, e porta gratis il feedback al tocco (ripple/`VisualStateManager` già definito nello stile implicito), lo stato disabilitato e la gestione dell'area tocco. L'alternativa `Border` + `Label` + `TapGestureRecognizer` darebbe controllo pixel-perfetto sul centraggio del glifo, ma va contro lo stile implicito di `Border` (servirebbe `StrokeThickness="0"` esplicito, come già fa il resto della pagina), perde il feedback al tocco e richiede di reimplementare a mano ciò che `Button` fa già. Si parte da `Button`; se su emulatore il `+` risultasse visibilmente decentrato in verticale, si corregge con `Padding="0"` e, come ultima risorsa, si passa alla composizione `Border`+`Label`.

Il glifo è il carattere `+` come `Text`, non un'icona PNG/SVG nuova: nessun asset da aggiungere e nessuna dipendenza da un font di icone. Colore di sfondo e testo arrivano dallo stile implicito `Button` (accento di brand + testo a contrasto), quindi tema chiaro/scuro è già coperto; `Padding="0"` e un `FontSize` dedicato servono perché lo stile implicito è tarato su bottoni testuali.

### Sovrapposizione nella stessa cella di `Grid`, non una terza riga

Il bottone va come **secondo figlio di `Grid.Row="1"`** con `HorizontalOptions="Center"` e `VerticalOptions="End"`: due figli nella stessa cella si sovrappongono nell'ordine di dichiarazione, quindi il bottone sta sopra la griglia senza consumare altezza. Una terza riga `Auto` la consumerebbe — esattamente ciò che `maui-card-list-compact-header` ha appena finito di recuperare. Stando dentro la cella della `CollectionView` e non nella `Grid` esterna, il bottone resta fermo mentre la lista scorre (non è figlio dell'area scrollabile).

### Occlusione dell'ultima riga di carte: footer trasparente, non `Padding`

Un elemento sovrapposto in basso copre l'ultima riga a scroll esaurito. Serve quindi spazio **scrollabile** in fondo pari all'ingombro del bottone più il suo margine.

`Padding` sulla `CollectionView` non è una strada: `ItemsView` non espone la proprietà (`error MAUIX2002` in compilazione — verificato). Un `Margin` inferiore compilerebbe ma **ridurrebbe il viewport**, creando una fascia morta in cui le carte non scorrono più: l'opposto dell'obiettivo di non sottrarre altezza. Si usa un `CollectionView.Footer` con un `BoxView` trasparente dell'altezza voluta: fa parte del contenuto scrollabile, quindi l'ultima riga sale sopra al bottone, e a scroll non esaurito non occupa nulla di visibile. L'`EmptyView` è indipendente dal footer, quindi resta centrato come prima.

### Dimensioni come risorse in `Styles.xaml`

Lato (56), raggio (28), margine dal fondo e `FontSize` del glifo vanno come risorse accanto a `FilterChipRowHeight`/`RecentCardsRowHeight`, con la stessa motivazione già scritta lì: la densità si cambia in un posto solo, e il padding di sicurezza della `CollectionView` resta derivabile dagli stessi numeri invece di essere una costante scollegata. 56 dp è la misura standard del FAB Android, sopra la soglia di 48 dp di area tocco.

### Accessibilità: descrizione semantica esplicita

Un bottone con solo `+` come testo non dice nulla a TalkBack. Si imposta `SemanticProperties.Description="Aggiungi carta"`, recuperando il significato che prima portava la parola "Aggiungi" in toolbar.

### `Shadow` sì, ma non essenziale

Un'ombra leggera stacca il bottone dai riquadri colorati sottostanti. `Shadow` su `VisualElement` funziona su Android nel range di API supportato; se in verifica risultasse resa male (artefatti sull'angolo tondo), si rimuove senza conseguenze funzionali — il contrasto del colore d'accento basta già a distinguerlo.

## Risks / Trade-offs

- **Il `+` appare decentrato in verticale sul `Button` Android** (il padding del testo nativo non è simmetrico per un glifo alto) → `Padding="0"` e verifica visiva su emulatore; fallback documentato alla composizione `Border`+`Label`.
- **Il bottone copre la penultima riga in liste corte** (dove non c'è scroll da guadagnare col padding) → col padding inferiore lo spazio è riservato comunque, quindi anche senza scroll la griglia termina sopra al bottone.
- **`CornerRadius` non tondo se larghezza e altezza divergono** (es. `MinimumWidthRequest` dello stile implicito che vince su un `WidthRequest` più piccolo) → il lato scelto (56) è sopra i minimi (44) dello stile implicito, quindi non c'è conflitto; si verifica a schermo che il bottone sia un cerchio e non uno stadio.
- **Perdita di un punto d'ingresso per chi conosceva la toolbar** → è un'app a uso personale con un solo percorso di aggiunta; il FAB è più evidente della voce testuale che sostituisce, non meno.

## Migration Plan

Modifica di sola presentazione, nessun dato o preferenza coinvolta: si applica con l'aggiornamento dell'APK e si annulla ripristinando il `ToolbarItem`. Nessuna migrazione di schema, nessun incremento della versione del DB.

## Open Questions

Nessuna: posizione (basso centro), forma (tondo) e glifo (`+`) sono stati indicati esplicitamente nella richiesta.
