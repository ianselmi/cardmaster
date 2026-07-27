## Context

`CardListPage.xaml` è oggi una `Grid` con **sette righe** (`RowDefinitions="Auto,Auto,Auto,Auto,Auto,Auto,*"`) e `RowSpacing` pari a `ItemSpacing` (8):

| Riga | Contenuto | Costo verticale |
|---|---|---|
| 0 | banner aggiornamento (condizionale) | ~70dp quando visibile |
| 1 | `SearchBar` | ~48dp |
| 2 | conteggio ("3 carte" / "5/30") | ~20dp + riga propria |
| 3 | chip delle label (condizionale, `HeightRequest=44`) | ~52dp |
| 4 | didascalia "Usate di recente" | ~20dp + riga propria |
| 5 | barra recenti (`HeightRequest=90`) | ~98dp |
| 6 | griglia dei riquadri | `*` |

Con i margini (`PagePadding=20`) e cinque `RowSpacing`, la testata costa **~280dp** su ~800dp utili: la prima riga di riquadri comincia poco sopra metà schermo, e se ne vede una sola per intero.

Due dettagli rilevanti emersi dalle change precedenti:

- La riga dei chip è nascosta quando non esiste nessuna label, ma **la sua `RowSpacing` resta**: chi non usa le label paga comunque 8dp. Era già stato notato come difetto cosmetico in `maui-card-color-labels` e lasciato lì.
- La barra dei recenti ha già riquadri più piccoli (110×90) di quelli della griglia (160 di altezza), quindi si distingue da sé anche senza didascalia.

## Goals / Non-Goals

**Goals:**

- Restituire alla griglia lo spazio che la testata occupa senza guadagnarselo.
- A riposo la testata non deve superare **un terzo** dell'altezza sotto la barra del titolo. Misurato sull'emulatore prima della change: **38%** senza banner (810px di 2116), **51%** con banner. Il criterio inizialmente ipotizzato — "due righe di riquadri visibili" — è stato scartato perché la misura ha mostrato che era **già soddisfatto** senza banner: non avrebbe discriminato nulla.
- Nessuna funzione persa: ricerca, filtro per label, conteggio e recenti restano tutti disponibili.
- Gli elementi non visibili non devono costare nulla.

**Non-Goals:**

- Rendere la testata scorrevole con la griglia (scartata, §1).
- Cambiare numero di colonne, dimensione dei riquadri o il loro contenuto.
- Toccare il banner degli aggiornamenti, che ha una sua logica di visibilità e appartiene a `app-update-notify`.
- Cambiare la logica di ricerca e filtro: cambia solo *quando* il conteggio si mostra.

## Decisions

### 1. Compattare, non far scorrere via

La strada alternativa era spostare l'intera testata dentro l'`Header` della `CollectionView`, così da farla scorrere via con il contenuto: recupero massimo, e a riposo tutto resta visibile.

È stata scartata: la ricerca diventerebbe raggiungibile solo risalendo in cima alla lista, e su una lista lunga — proprio il caso in cui cercare serve — sarebbe il momento peggiore per farlo. La compattazione recupera meno spazio ma non sposta nessun costo sull'utente. *(Decisione 27 lug 2026.)*

### 2. Testata come `VerticalStackLayout`, non come righe di `Grid`

La testata diventa un `VerticalStackLayout` dentro una `Grid` a due sole righe (testata `Auto`, griglia `*`).

Non è un riordino cosmetico: è ciò che risolve il problema delle righe nascoste. In una `Grid` una riga alta 0 continua a produrre `RowSpacing` verso le adiacenti, mentre un `VerticalStackLayout` dispone **solo i figli visibili** e non applica spaziatura per quelli collassati. Con la struttura attuale servirebbe bindare l'altezza di ogni `RowDefinition` alla visibilità del contenuto — una regola in più da mantenere per ogni riga condizionale futura.

### 3. Una sola riga per conteggio e chip, con visibilità indipendenti

Conteggio e chip condividono una riga: il conteggio a sinistra, i chip a scorrimento orizzontale accanto. Le due visibilità restano indipendenti:

| Label esistenti | Filtro attivo | Riga |
|---|---|---|
| no | no | assente |
| no | sì | solo conteggio |
| sì | no | solo chip |
| sì | sì | conteggio + chip |

Così la riga costa qualcosa solo quando ha qualcosa da dire. Nel caso più comune di chi non usa le label e non sta cercando, sparisce del tutto.

### 4. Il conteggio solo sotto filtro

A riposo il conteggio diventa rumore: chi guarda le proprie carte le sta già vedendo, e sapere che sono trenta non aggiunge nulla. Sotto filtro invece è l'unica cosa che dice *quanto* si sta escludendo, quindi resta nella forma "trovate/totale".

Conseguenza sul requisito esistente, che oggi prescrive il totale a riposo: va riscritto, non aggirato.

### 5. Niente didascalia per i recenti

I riquadri dei recenti sono già visibilmente più piccoli di quelli della griglia e stanno su una riga orizzontale a scorrimento: la didascalia costa una riga intera per ripetere qualcosa che la forma già dice. Se all'uso l'assenza risultasse disorientante, rimetterla è una riga di XAML — per questo la scelta è isolata in un requisito suo.

### 6. Le dimensioni vengono dalla scala condivisa

Le altezze ridotte di chip e recenti non finiscono come numeri sparsi nel XAML ma passano dalla scala di spaziatura condivisa di `visual-identity`, che esiste proprio per questo. Se domani si vorrà una densità diversa, si cambia in un posto solo.

## Risks / Trade-offs

- **Il conteggio a riposo sparisce** → chi lo usava per sapere a colpo d'occhio quante carte ha deve digitare qualcosa o attivare un chip. È la scelta esplicita del 27 lug 2026; l'informazione non è persa, solo non più permanente.
- **La didascalia "Usate di recente" sparisce** → un utente nuovo potrebbe non capire subito perché le prime carte sono più piccole e ripetute più sotto. Mitigato dalla differenza di dimensione, e reversibile a costo quasi nullo.
- **Meno spazio bianco** → una testata più densa è meno arieggiata di quella attuale. È il prezzo consapevole di questa change: lo spazio va alle carte.
- **`VerticalStackLayout` invece di righe di `Grid`** → cambia la struttura del file più toccato della pagina, quindi va riverificato che tutto ciò che era condizionale (banner, chip, recenti) continui a comparire e sparire correttamente.
- **L'obiettivo "due righe di riquadri"** dipende dall'altezza dello schermo: su un dispositivo molto piccolo potrebbe non essere raggiungibile. Va verificato su uno schermo tipico, non promesso su qualunque device.

## Migration Plan

Nessuna migrazione: la change è interamente di presentazione, non tocca dati né preferenze. Al primo avvio dopo l'aggiornamento la lista appare più densa; nessuna azione richiesta all'utente e nessuna possibilità di rollback dei dati da gestire.

## Open Questions

Nessuna: strategia (compattare), sorte della barra recenti (resta) e comportamento del conteggio (solo sotto filtro) sono stati decisi prima della stesura.
