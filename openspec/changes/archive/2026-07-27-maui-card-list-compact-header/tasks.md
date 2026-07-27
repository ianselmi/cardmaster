## 1. Misura di partenza

- [x] 1.1 Su emulatore, con lo stato attuale, misurare la posizione della prima riga di riquadri e quante righe complete si vedono a riposo — serve come termine di paragone per la verifica finale
- [x] 1.2 Ripetere la misura nei tre casi: senza label, con label, con banner aggiornamento visibile

## 2. Struttura della testata

- [x] 2.1 `CardListPage.xaml`: sostituire le sei righe `Auto` della `Grid` con una `Grid` a due righe (testata `Auto`, griglia `*`) e una `VerticalStackLayout` che contiene banner, ricerca e riga filtro
- [x] 2.2 Verificare che gli elementi condizionali (banner, chip, recenti) non lascino spaziatura residua quando nascosti — è il motivo per cui si passa a `VerticalStackLayout`
- [x] 2.3 Unire conteggio e chip in una sola riga: conteggio a sinistra, chip a scorrimento orizzontale accanto, con visibilità indipendenti
- [x] 2.4 Rendere invisibile l'intera riga filtro quando non ci sono né label né filtro attivo

## 3. Conteggio solo sotto filtro

- [x] 3.1 `CardListViewModel`: esporre una proprietà di visibilità del conteggio legata alla presenza di un filtro (testo o label)
- [x] 3.2 Semplificare `CountText` alla sola forma "trovate/totale", ora che il caso a riposo non si mostra più
- [x] 3.3 Verificare che la proprietà si aggiorni sia digitando nella ricerca sia attivando/disattivando i chip

## 4. Recenti e densità

- [x] 4.1 Rimuovere la riga di intestazione "Usate di recente", lasciando la barra invariata nel comportamento (sempre visibile, anche durante ricerca e filtro)
- [x] 4.2 Ridurre le altezze di chip e barra recenti, prendendo i valori dalla scala di spaziatura condivisa
- [x] 4.3 Aggiungere alla scala condivisa (`Resources/Styles/Styles.xaml`) i valori mancanti, invece di usare numeri isolati nel XAML

## 5. Verifica

- [x] 5.1 `dotnet build` con 0 errori
- [x] 5.2 Verifica su emulatore: a riposo la testata occupa **al massimo un terzo** dell'altezza sotto la barra del titolo (misura di partenza: 38%), e due righe complete di riquadri restano visibili anche col banner
- [x] 5.3 Verifica su emulatore: senza label la riga filtro è del tutto assente e non lascia spazio
- [x] 5.4 Verifica su emulatore: il conteggio compare digitando nella ricerca e attivando un chip, e sparisce quando si azzerano entrambi
- [x] 5.5 Verifica di non-regressione: ricerca, filtro OR per label, combinazione dei due, stato vuoto, apertura di una carta dalla barra dei recenti
- [x] 5.6 Verifica di non-regressione: il banner di aggiornamento compare e scompare correttamente nella nuova struttura
- [x] 5.7 Screenshot prima/dopo per documentare il guadagno nella PR
- [x] 5.8 Rivedere il diff prima del commit (repository pubblico) e aggiornare `PLAN.md` con la voce della change
