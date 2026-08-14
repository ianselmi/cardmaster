## Why

Le righe di uno scontrino ricostruite dalla geometria dell'OCR arrivano a **21 su 29** su una foto reale storta e stropicciata (misura di `receipt-items`, scontrino MD dell'11 ago 2026, somma 36,38 € contro 47,74 €). Lo stesso scontrino, letto da un modello multimodale, dà **29 righe su 29** con aliquote e totale corretti.

Il divario non si chiude con un'euristica in più. Il parser deterministico vede rettangoli e deve *dedurre* la struttura della pagina; su carta piegata, inclinata e con l'inchiostro consumato la deduzione cede — e cede in modo silenzioso, perché due prodotti appaiati sembrano una riga qualunque. Il modello vede la pagina.

La decisione presa è di usare il modello **come rete di sicurezza, non come sostituto**: lo scontrino si legge prima in locale e gratis, e il modello interviene **solo quando la quadratura fallisce**, cioè esattamente quando l'app già sa di non potersi fidare. Su uno scontrino che quadra non parte nessuna chiamata, non esce nessun dato e non si spende niente.

## What Changes

- **Rilettura dell'immagine con Claude quando la quadratura fallisce**: l'app invia la foto dello scontrino e riceve **JSON strutturato** — testata e righe, con aliquota per riga — imposto da uno schema, non chiesto per cortesia al modello.
- **Opt-in esplicito e informato**: la funzione è spenta. Si accende nelle impostazioni, dove l'utente legge *che cosa* esce dal device (la foto dello scontrino: prodotti, prezzi, esercente, data), *verso chi* (l'API di Anthropic) e *a quale costo* (il suo, sulla sua chiave).
- **Chiave API dell'utente**, incollata nelle impostazioni e conservata in `SecureStorage`. Nessuna chiave nel pacchetto, nel repository o in un server: è l'unica forma compatibile con un APK scaricabile da chiunque.
- **Scelta del modello** tra quelli adatti, con il costo per scontrino dichiarato accanto a ciascuno. Default `claude-opus-5`.
- **L'esito del modello si confronta, non si impone**: le righe rilette passano per la stessa quadratura delle altre e finiscono nella stessa schermata di conferma, dove l'utente le corregge come sempre. Se il modello sbaglia, si vede.
- **Degrada a quello che l'app fa oggi**: senza chiave, senza rete, con la funzione spenta o con la chiamata fallita, lo scontrino si salva con le righe del parser locale. Nessun percorso diventa obbligatorio.

## Capabilities

### New Capabilities

- `receipt-ai-scan`: rilettura di uno scontrino tramite modello multimodale — quando si attiva, cosa viene inviato, formato dell'esito, confronto con la lettura locale, comportamento in assenza di chiave o di rete.
- `ai-credentials`: chiave API fornita dall'utente — inserimento, conservazione protetta, verifica, revoca, e la garanzia che nessuna credenziale viaggi nel pacchetto dell'applicazione.

### Modified Capabilities

- `receipt-scan`: la capability dichiara oggi che **nessun dato dello scontrino lascia il device** e che l'acquisizione funziona **interamente offline**. Il primo diventa condizionato al consenso esplicito dell'utente; il secondo resta vero, perché il percorso offline continua a funzionare da solo e la rete entra solo nel ramo opzionale.
- `app-settings`: nuova sezione per chiave, modello e interruttore della funzione.

## Impact

**Vincoli di progetto toccati** — vanno dichiarati, non aggirati:
- `PLAN.md` prevede per `receipt-ai-normalize` che si inviino «**solo le descrizioni** (mai immagine, totale, esercente o data)». Questa change **invia l'immagine**, che è ciò che rende il salto di qualità possibile: è una decisione presa consapevolmente il 12 ago 2026, e va riportata in `PLAN.md` con la sua motivazione.
- `receipt-scan` promette che nulla lascia il device. La promessa si trasforma in: nulla lascia il device **finché l'utente non lo chiede esplicitamente**, per una funzione che nasce spenta.

**Nuove dipendenze**
- Pacchetto NuGet `Anthropic` (SDK ufficiale C#). Prima cosa da verificare: che compili per `net10.0-android` — è il rischio che può fermare la change, come lo fu ML Kit per `receipt-capture`.
- Permesso `INTERNET`: già presente per l'auto-update.

**Codice esistente toccato**
- `ReceiptFormViewModel`: dopo la quadratura fallita, offre la rilettura.
- Impostazioni: chiave, modello, interruttore.
- Nessuna modifica al parser deterministico, che resta la via normale e l'unica offline.

**Costi**
- A carico dell'utente, sulla sua chiave. L'app li dichiara prima: ordine di grandezza **4 centesimi per scontrino** con `claude-opus-5`, meno di **1 centesimo** con un modello più piccolo. Nessuna chiamata su uno scontrino che quadra.

**Non-goals**
- Normalizzazione dei nomi prodotto e categorie via modello: resta `receipt-ai-normalize`, che riuserà chiave e infrastruttura di questa change.
- Backend proprio, proxy, chiave gestita da noi: v2, se mai.
- Sostituzione del parser locale. Se un giorno il modello risultasse migliore *sempre*, sarà una decisione separata, presa su numeri.
- Invio dell'immagine per scontrini che quadrano.
