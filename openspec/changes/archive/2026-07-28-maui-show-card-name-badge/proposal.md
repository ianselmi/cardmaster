## Why

Nel dettaglio della carta il nome compare **solo nella barra del titolo**: una posizione che il sistema tronca sui nomi lunghi, che scompare dagli screenshot ritagliati e che alla cassa — con il telefono girato verso il cassiere — è la parte dello schermo che si guarda meno. Il riquadro bianco col barcode è invece l'elemento che si porge, e non dice a quale carta appartiene: due carte dello stesso emittente si distinguono solo dal numero. Mettere il nome **dentro** quel riquadro rende identificabile ciò che si sta mostrando, senza aggiungere un elemento altrove nella pagina.

## What Changes

- Il nome della carta compare **dentro il riquadro** del barcode, sopra il codice, nella pagina di dettaglio.
- Il nome sta su una **fascia con sfondo proprio** che lo stacca dal bianco del riquadro: colore del riquadro della carta (lo stesso della griglia: scelto dall'utente o derivato dal nome) con testo a contrasto.
- Lo sfondo riguarda **solo il testo del nome**: l'area di rendering del barcode e il codice in chiaro restano su fondo bianco come oggi, anche in tema scuro.
- La barra del titolo continua a mostrare il nome: non si rimuove nulla.

## Capabilities

### New Capabilities

Nessuna.

### Modified Capabilities

- `card-display`: nuovo requisito sulla presenza del nome della carta dentro il riquadro del barcode, su una fascia colorata che lo distingue, con il vincolo che il fondo bianco dell'area barcode resti invariato.

## Impact

- `src/CardMaster/Views/ShowCardPage.xaml` — nuova fascia col nome in cima al contenuto del `Border` bianco.
- `src/CardMaster/ViewModels/ShowCardViewModel.cs` — esposizione del colore del riquadro della carta caricata, derivato da `CardTilePalette.ForCard` (lo stesso punto di verità usato da griglia e barra dei recenti).
- Nessuna modifica a dati, schema DB, repository, permessi o dipendenze: nome e colore sono già sulla carta caricata.
- Nessun impatto su rendering del barcode, luminosità/keep-awake, condivisione QR.
