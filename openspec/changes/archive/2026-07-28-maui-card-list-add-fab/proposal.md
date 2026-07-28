## Why

L'unico modo per aggiungere una carta è la voce testuale "Aggiungi" nella toolbar in alto a destra: è l'azione più frequente dell'app, ma sta nell'angolo più scomodo da raggiungere col pollice su un telefono tenuto in una mano, condivide lo spazio con "Impostazioni" (rischio di tocco sbagliato) ed è resa come testo mentre tutto il resto dell'app è visuale. La convenzione Android per l'azione primaria di una lista è un **floating action button** tondo in basso: più raggiungibile, immediatamente riconoscibile e — liberando la toolbar — coerente con la direzione già presa da `maui-card-list-compact-header` di dare più spazio alle carte.

## What Changes

- Rimossa la voce di toolbar "Aggiungi" dalla pagina lista carte. La toolbar resta con la sola voce "Impostazioni" (badge di aggiornamento invariato).
- Aggiunto in **basso al centro** della lista carte un **bottone tondo con il simbolo `+`**, sovrapposto alla griglia (non ruba altezza alle carte), sempre visibile durante lo scorrimento e presente anche sull'empty state.
- Il bottone apre lo stesso flusso di acquisizione di prima (schermata di scansione): nessun cambiamento al percorso di creazione carta, cambia solo il punto d'ingresso.
- Aspetto e dimensioni derivati dalla palette e dalla scala di spaziatura condivise (colore d'accento di brand, `+` a contrasto), non da valori inventati sulla pagina.

## Capabilities

### New Capabilities

Nessuna.

### Modified Capabilities

- `card-list`: nuovo requisito sul **punto d'ingresso all'aggiunta di una carta** presentato come bottone tondo flottante in basso al centro, sovrapposto alla griglia e presente anche a lista vuota (prima non specificato a livello di spec, realizzato di fatto come voce di toolbar).

## Impact

- `src/CardMaster/Views/CardListPage.xaml` — via il `ToolbarItem` "Aggiungi"; la griglia e il nuovo bottone diventano sovrapposti nella stessa cella di `Grid`.
- `src/CardMaster/Views/CardListPage.xaml.cs` — l'handler `OnAddClicked` resta, cambia solo l'elemento che lo invoca.
- `src/CardMaster/Resources/Styles/Styles.xaml` — eventuali risorse di dimensione/stile del bottone, accanto a quelle già introdotte da `maui-card-list-compact-header`.
- Nessuna modifica a dati, repository, permessi o dipendenze; nessun impatto sulla rete e sul flusso di scansione.
