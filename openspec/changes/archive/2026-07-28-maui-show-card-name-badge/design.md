## Context

Nella pagina di dettaglio (`ShowCardPage.xaml`) il `Border` bianco contiene un `VerticalStackLayout` con: immagine del barcode, messaggio di fallback quando il barcode non è generabile, codice in chiaro. Il nome della carta non è dentro il riquadro: sta solo come `Title` della pagina.

Il colore del riquadro di una carta è già centralizzato in `CardTilePalette.ForCard(card)` — "colore scelto dall'utente se valorizzato, altrimenti derivato dal nome" — usato da griglia e barra dei recenti tramite `CardToTileColorConverter`, e da `CardFormViewModel` per la palette di scelta. `ForCard(null)` è definito e ritorna un colore deterministico.

Vincolo forte già in spec (`card-display`, ribadito da `visual-identity`): l'area di rendering del barcode **deve restare su fondo bianco anche in tema scuro**, perché è ciò che il lettore alla cassa deve poter leggere.

## Goals / Non-Goals

**Goals:**

- Capire a colpo d'occhio, guardando il riquadro che si porge al cassiere, di quale carta si tratta.
- Nome distinguibile dal resto del riquadro tramite uno sfondo proprio, col colore identitario della carta.
- Barcode e codice in chiaro assolutamente invariati.

**Non-Goals:**

- Non si rimuove il nome dalla barra del titolo.
- Non si tocca il colore del riquadro nella griglia né la regola che lo determina.
- Non si rende il nome modificabile da questa pagina: si modifica dalla schermata di modifica.
- Non si aggiunge l'emittente dentro il riquadro: resta la caption sopra il riquadro, come oggi.

## Decisions

### Fascia in cima al contenuto del riquadro, non sopra o sotto il riquadro

Il nome va come **primo figlio** del `VerticalStackLayout` dentro il `Border` bianco: sopra l'immagine del barcode. Sopra il riquadro sarebbe una duplicazione della caption dell'emittente e non sarebbe "dentro"; sotto il codice finirebbe in coda, dopo l'informazione che serve alla cassa, cioè nel punto che si guarda per ultimo. In cima invece si legge nell'ordine naturale — *quale carta*, poi *il suo codice* — e resta dentro il rettangolo che si porge.

### Fascia a tutta larghezza del riquadro, non pillola aggrappata al testo

Una pillola centrata larga quanto il testo sarebbe più letterale rispetto a "sfondo solo per il testo", ma con un nome lungo crescerebbe comunque fino a tutta la larghezza, con un risultato incoerente tra carte dal nome corto e lungo. Una fascia a tutta larghezza (dentro il `Padding="16"` del riquadro, quindi con l'aria bianca intorno) è stabile per qualunque nome e legge come intestazione del riquadro. "Solo per il testo" resta rispettato nel senso che conta: il colore riguarda la riga del nome, non l'area del barcode.

### Colore dal ViewModel via `CardTilePalette.ForCard`, non `CardToTileColorConverter` sulla carta

Il `ShowCardViewModel` proietta già i campi che servono alla pagina (`DisplayName`, `IssuerName`, `BarcodeValue`, immagine) invece di esporre l'entità `Card`. Si aggiunge nello stesso stile un `Color TileColor` valorizzato in `LoadAsync()` con `CardTilePalette.ForCard(card)`. Usare il converter richiederebbe di esporre la carta intera alla view solo per farla riattraversare da un converter: più accoppiamento per lo stesso risultato. La regola "colore utente altrimenti derivato" **non viene duplicata**: resta dove è, in `ForCard`, che è il punto di verità dichiarato da `card-list`.

Il ViewModel che referenzia `CardMaster.Views` non è una novità di questa change: `CardFormViewModel` e `TileColorOption` già lo fanno per la stessa palette.

### Testo bianco, come sui riquadri della griglia

La palette dei riquadri è tarata per il testo bianco — è così che `card-list` soddisfa già il proprio requisito di leggibilità, sia sui colori automatici sia su quelli scelti dall'utente. Il nome nella fascia usa quindi `White` in grassetto, senza `AppThemeBinding`: la fascia ha lo stesso colore in tema chiaro e scuro, quindi il colore del testo non deve cambiare con il tema.

### Nome lungo: va a capo, non si tronca

`LineBreakMode` di default su più righe con un tetto di righe (`MaxLines`) evita sia il taglio secco a metà parola sia una fascia che divora il riquadro su nomi assurdamente lunghi. Preferito al troncamento perché il nome è l'informazione identificativa: meglio due righe che "Carta fedeltà supermerc…".

### Carta non trovata: colore azzerato come le label

Nel ramo `card is null` di `LoadAsync` il colore va riportato al valore di `ForCard(null)`, per la stessa ragione per cui `maui-show-card-labels` azzera le label: un `ReloadAsync` su una carta appena eliminata non deve lasciare in scena residui della carta che non c'è più.

## Risks / Trade-offs

- **Il riquadro bianco diventa meno "neutro"** e acquista un colore forte in cima → è l'effetto richiesto; il vincolo che conta (barcode su bianco) resta intatto e verificato da uno scenario apposta.
- **La fascia consuma altezza dentro il riquadro**, riducendo lo spazio verticale dell'immagine su schermi bassi → il barcode ha `HeightRequest` fisso (240) e la pagina è in `ScrollView`, quindi il caso peggiore è dover scorrere, non un barcode compresso.
- **Un colore di palette molto chiaro potrebbe rendere il bianco meno leggibile** → il vincolo di contrasto è già un requisito di `card-list`, che vale per la stessa palette; nessun nuovo colore viene introdotto qui.

## Migration Plan

Modifica di sola presentazione: nessuna migrazione di dati, nessun incremento di versione di schema. Si applica con l'aggiornamento dell'APK e si annulla rimuovendo la fascia dal XAML.

## Open Questions

Nessuna: pagina (dettaglio) e stile dello sfondo (colore del riquadro della carta) sono stati confermati.
