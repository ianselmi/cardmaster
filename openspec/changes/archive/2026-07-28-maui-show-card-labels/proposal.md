## Why

Le label si assegnano a una carta e si usano per filtrare la lista, ma **non si vedono da nessuna parte a lettura**: l'unico posto dove sono visibili è la schermata di modifica, cioè il posto dove si cambiano. Per sapere se una carta è taggata "spesa" bisogna aprirla, toccare Modifica e poi tornare indietro — un giro completo in scrittura per una domanda di sola lettura, con il rischio di uscirne avendo cambiato qualcosa per sbaglio. Il dettaglio della carta è il posto naturale dove mostrarle: è già la pagina che risponde a "cos'è questa carta".

## What Changes

- La pagina di dettaglio della carta mostra le **label assegnate**, in sola lettura, con la stessa resa a chip già usata nell'editor (senza la ✕ di rimozione).
- Se la carta non ha label, la sezione **non compare affatto**: nessuna intestazione vuota, nessuno spazio sprecato.
- Le label sono mostrate **sotto l'area del barcode**, sopra i pulsanti di azione: il barcode resta nella stessa posizione di oggi, senza essere spinto in basso.
- Nessun cambiamento al modo in cui le label si assegnano, si normalizzano o si usano come filtro nella lista.

## Capabilities

### New Capabilities

Nessuna.

### Modified Capabilities

- `card-display`: nuovo requisito sulla presenza delle label assegnate nella pagina di dettaglio della carta, in sola lettura e senza alterare posizione e prominenza del barcode.

## Impact

- `src/CardMaster/Views/ShowCardPage.xaml` — nuova sezione chip tra l'area barcode e la riga dei pulsanti.
- `src/CardMaster/ViewModels/ShowCardViewModel.cs` — esposizione delle label della carta caricata (e di un flag di presenza per la visibilità).
- Nessuna modifica a dati, schema DB, repository, permessi o dipendenze; le label sono già persistite sulla carta (`LabelsCsv`/`Labels`) e già caricate insieme ad essa.
- Nessun impatto sul payload QR di condivisione, che continua a non includere le label.
