## Why

Chi usa le carte fedeltà vuole sapere anche **quanto e dove spende**: lo scontrino è già in mano al momento in cui si apre l'app alla cassa, ma finisce nel cestino e con esso l'unico dato che dice davvero cosa si compra. Oggi CardMaster conserva la carta e basta.

Questa change apre il tema con il pezzo più rischioso e più isolabile: **acquisire uno scontrino e capirne la testata** (esercente, data, totale), senza toccare le righe dei prodotti. Se il riconoscimento non regge sugli scontrini reali dei negozi che l'utente frequenta, si scopre qui — dopo poche settimane d'uso e poche centinaia di righe di codice — invece che dopo aver costruito righe prodotto, normalizzazione e analisi sopra fondamenta che non tengono.

Il valore è autonomo anche se ci si fermasse qui: uno storico degli scontrini consultabile e "quanto ho speso da chi, questo mese" si ricavano dalla sola testata.

## What Changes

- **Nuova sezione "Scontrini"** nell'app, accanto a "Le mie carte": lista degli scontrini acquisiti (più recenti in alto) e pagina di dettaglio.
- **Due percorsi di acquisizione**, gli stessi già offerti per le carte: **scattare una foto** oppure **scegliere un'immagine** già sul device. Nessun permesso di storage nuovo (selettore di sistema).
- **OCR on-device** dell'immagine: nessuna rete, nessun servizio esterno, l'immagine non lascia il telefono. Il testo riconosciuto viene conservato con lo scontrino.
- **Estrazione automatica della testata**: esercente, partita IVA, data/ora, totale. Ogni campo è **correggibile a mano** in una schermata di conferma prima del salvataggio, che distingue visivamente ciò che è stato riconosciuto da ciò che non lo è stato.
- **Conservazione dell'immagine** dello scontrino insieme ai dati, con la possibilità di non conservarla.
- **Nuova voce di analisi minima**: spesa per negozio e per mese, ricavata dalla sola testata.
- **Nessuna riga prodotto in questa change** e nessuna categorizzazione: è materia della change successiva.

## Capabilities

### New Capabilities

- `receipt-scan`: acquisizione di uno scontrino da foto o immagine, riconoscimento del testo on-device, estrazione e correzione dei dati di testata, persistenza e storico degli scontrini.

### Modified Capabilities

- `app-shell`: la navigazione passa da una sola sezione di primo livello ("Le mie carte") a due, con l'aggiunta di "Scontrini"; il requisito di navigazione va esteso per descrivere una struttura a sezioni invece di una pagina iniziale unica.
- `cloud-backup`: il backup produce oggi uno snapshot dell'**intero database**, e quel requisito è tuttora vero — ma da questa change esistono dati dell'app **fuori** dal database (le immagini degli scontrini), che il backup non copre e che un ripristino non riporta indietro. Il requisito va reso esplicito su cosa il backup **non** include, e l'utente va informato nella pagina Backup invece di scoprirlo dopo un ripristino.

## Impact

**Dipendenze**
- Nuovo pacchetto di riconoscimento testo ML Kit (variante con modello incluso nell'APK, per non dipendere da un download a runtime e restare coerenti con l'offline-first e con la distribuzione fuori dal Play Store). Convive con ML Kit barcode, già presente per la scansione delle carte: il rischio principale della change è un conflitto di versioni tra le dipendenze Android delle due librerie.
- Aumento della dimensione dell'APK, da misurare e riportare: incide sull'aggiornamento in-app, che scarica l'intero pacchetto.

**Modello dati**
- Nuova entità scontrino, con Id client-generato e tombstone come tutte le altre; incremento della versione di schema del database, che il backup Drive usa come guardia di compatibilità in ripristino.
- Importi conservati in centesimi come interi, non in virgola mobile.

**Storage**
- Le immagini degli scontrini occupano spazio nell'area dati privata dell'app e crescono nel tempo: servono un'indicazione dello spazio occupato e un modo per liberarlo.

**Codice esistente toccato**
- Struttura di navigazione dell'app (comparsa della barra di navigazione inferiore).
- Inizializzazione del database e versione di schema.
- Pagina Backup, per dichiarare il limite sulle immagini.
- Nessuna modifica al comportamento delle carte fedeltà: la funzione esistente resta invariata e indipendente.

**Non-goals**
- Righe dei prodotti, categorie, normalizzazione dei nomi, classifiche degli acquisti: change successive.
- Qualunque chiamata di rete: questa change è interamente offline.
- Rilevamento bordi, raddrizzamento e correzione prospettica dell'immagine: si chiede all'utente una foto dritta e leggibile.
- Formati esteri, valute diverse dall'euro, scontrini non italiani.
