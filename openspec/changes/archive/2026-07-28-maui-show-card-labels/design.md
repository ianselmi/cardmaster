## Context

`ShowCardPage.xaml` è uno `ScrollView` con un `VerticalStackLayout`: emittente in caption, banner filtro luce blu (nascosto per default), area barcode su fondo bianco (immagine + codice in chiaro), riga di tre pulsanti (Condividi / Modifica / Elimina).

`ShowCardViewModel.LoadAsync()` carica la carta da `ICardRepository.GetByIdAsync` e ne proietta i campi (`DisplayName`, `IssuerName`, `BarcodeValue`, immagine renderizzata). `ShowCardPage.OnAppearing` chiama `ReloadAsync()`, che azzera il flag `_loaded` e rilegge la carta: è già così che il dettaglio si aggiorna dopo un salvataggio in modifica.

Le label sono già persistite sulla carta (`Card.LabelsCsv`, esposte tipizzate da `Card.Labels`) e arrivano quindi **gratis** con la carta già caricata: nessuna query aggiuntiva, nessun servizio nuovo.

I chip esistenti nell'app sono due, con look e comportamenti diversi:

- **editor label** (`CardLabelEditorView`): `FlexLayout` con `Wrap="Wrap"`, chip `Border` pieni color `Secondary` con testo `PrimaryDarkText` e una ✕ per rimuovere;
- **filtri lista** (`CardListPage`): chip in `CollectionView` orizzontale, contorno grigio e stato selezionato color `Primary`.

## Goals / Non-Goals

**Goals:**

- Vedere le label di una carta senza entrare in modifica.
- Costo zero quando la carta non ha label: nessuna intestazione, nessuno spazio.
- Barcode invariato per posizione e dimensione — resta il motivo per cui la pagina esiste.

**Non-Goals:**

- Non si assegnano né si rimuovono label dal dettaglio: resta un'operazione della schermata di modifica.
- Le label del dettaglio non sono toccabili come scorciatoia per filtrare la lista (sarebbe una navigazione nuova, non richiesta qui).
- Non si toccano normalizzazione, limiti, suggerimenti o filtro per label.
- Nessuna label nel payload QR di condivisione: resta come è.

## Decisions

### Le label vanno **sotto** l'area barcode, sopra i pulsanti

Il posto più naturale a leggerlo sarebbe subito sotto l'emittente, in cima. Ma `card-display` esiste per mostrare il barcode alla cassa: qualunque cosa messa sopra all'area bianca la spinge in basso, e su una carta con molte label la spinge di parecchio. Sotto l'area barcode e sopra la riga dei pulsanti, invece, la sezione cresce verso il fondo dello `ScrollView` senza muovere di un pixel ciò che sta sopra — che è esattamente ciò che chiede lo scenario "Barcode invariato".

### Chip replicati in sola lettura, non un controllo condiviso estratto

I chip del dettaglio sono un terzo caso: stesso look di quelli dell'editor, ma senza ✕, senza gesture e con un binding diverso (`ShowCardViewModel` invece di `CardFormViewModel`). Estrarre un `ContentView` condiviso significherebbe parametrizzare rimozione, comando e sorgente per un guadagno di poche righe di XAML, su un controllo che i due usi tirerebbero in direzioni diverse. Si replica il `FlexLayout` con `Wrap="Wrap"` e i chip `Border`/`Secondary` dell'editor, che dà **coerenza visiva** — l'obiettivo vero — senza accoppiare due schermate.

`FlexLayout` con `Wrap="Wrap"` (e non uno `HorizontalStackLayout`) è ciò che soddisfa lo scenario "Molte label": i chip vanno a capo invece di uscire dallo schermo.

Due scostamenti dal markup dell'editor, entrambi emersi in verifica su emulatore e non prevedibili a tavolino:

- **`FlexLayout.Shrink="0"` sui chip.** Con lo `Shrink` di default (1) `FlexLayout` comprime gli item sotto la loro larghezza naturale pur di farli stare in riga, invece di mandarli a capo: il testo si tronca ("spesa" → "spes"). A 0 il chip tiene la sua misura e la riga va a capo, che è il comportamento voluto dallo scenario "Molte label".
- **Padding orizzontale 16 invece di 12.** Nell'editor il testo del chip è seguito dalla ✕, qui arriva al bordo: l'arrotondamento della misura del testo su Android mangiava l'ultima lettera di alcune label ("farmacia" → "farmaci"). Il padding più largo assorbe l'arrotondamento. È anche il motivo per cui il difetto non si vede nell'editor, che resta invariato.

### `HasLabels` come flag di visibilità, non un binding su `Count`

La sezione si nasconde con `IsVisible="{Binding HasLabels}"` sul `FlexLayout`, come già fa l'editor. Dentro un `VerticalStackLayout` un figlio non visibile non occupa spazio **né spaziatura** — è la stessa proprietà su cui si è appoggiato `maui-card-list-compact-header` per la testata della lista, e il motivo per cui qui non serve né una riga di `Grid` né logica aggiuntiva per lo scenario "Carta senza label".

Niente etichetta "Label" sopra i chip: nel dettaglio i chip si spiegano da soli, e un'intestazione sarebbe una riga in più da nascondere insieme a loro.

### Aggiornamento dopo la modifica: già coperto da `ReloadAsync`

`OnAppearing` chiama `ReloadAsync()`, che rilegge la carta dal repository. Popolando le label dentro `LoadAsync()` insieme agli altri campi, lo scenario "Label aggiornate dopo una modifica" è soddisfatto **senza codice nuovo**: al ritorno dalla schermata di modifica la pagina ricarica e i chip riflettono il salvataggio. Non serve alcuna sottoscrizione a eventi o messaggistica tra ViewModel.

## Risks / Trade-offs

- **Le label in fondo alla pagina si vedono solo scorrendo**, su schermi bassi o con molte label → accettato: la priorità dichiarata è che il barcode non si muova, e chi cerca le label sta facendo una consultazione, non un'operazione alla cassa.
- **Duplicazione del markup dei chip tra editor e dettaglio** → contenuta (una manciata di righe) e deliberata; se in futuro nascesse un terzo uso *identico*, quello sarà il momento per estrarre il controllo.
- **Una label molto lunga potrebbe non stare in un chip su schermi stretti** → il limite di lunghezza per label è già imposto da `card-labels` (`MaxLength` in inserimento), quindi il caso è già circoscritto a monte.

## Migration Plan

Modifica di sola presentazione: nessuna migrazione di dati, nessun incremento della versione di schema, nessuna preferenza coinvolta. Si applica con l'aggiornamento dell'APK e si annulla rimuovendo la sezione dal XAML.

## Open Questions

Nessuna.
