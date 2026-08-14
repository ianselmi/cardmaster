## Why

Oggi uno scontrino salvato dice **quanto** si è speso e **dove**, ma non **cosa** si è comprato: `receipt-capture` si è fermata alla testata, e la riga "TOTALE 47,80 — Esselunga" è esattamente ciò che si legge già sull'estratto conto della carta. Il dato che l'app non ha ancora, e che nessun'altra fonte possiede, sono le **righe prodotto**: è lì che vive la risposta a "quanto spendo di pane in un mese", "quel detersivo costava così anche a marzo?", "dove finiscono i soldi della spesa".

È il pezzo difficile della feature, quello che decide se il dominio scontrini vale la pena: la testata è la parte regolare dello scontrino, il corpo è la parte irregolare. Va affrontato ora perché le due change successive — normalizzazione dei nomi e viste di analisi — sono entrambe **costruite sopra le righe** e non hanno senso senza. E va affrontato con lo stesso metodo che ha funzionato per la testata: regole deterministiche, geometria dell'OCR, correzione manuale come percorso di prima classe, test su testo reale.

## What Changes

- **Righe prodotto ricostruite dalla geometria dell'OCR**: `ReceiptTextLayout` oggi appiattisce una riga visiva in una stringa; le colonne (descrizione a sinistra, importo a destra) si separano usando le `x` dei frammenti sulla stessa banda `y`. Il corpo dello scontrino viene delimitato tra la fine della testata e la riga del totale.
- **Casi sporchi trattati esplicitamente**, perché sono la norma e non l'eccezione: quantità su riga separata (`2 X 1,50`), prodotti a peso (`0,432 kg x 2,99 €/kg`), sconti e promozioni con importo negativo, descrizioni che vanno a capo su due righe, righe di reparto che non sono prodotti.
- **Aliquota IVA per riga**, letta dalla colonna che gli scontrini italiani stampano accanto al prezzo — come cifra (`4,00`) o come codice di reparto (`1`, `2`, `3`) risolto tramite il **riepilogo IVA** a piè di scontrino. È il campo che separa la spesa alimentare da quella non alimentare senza dover indovinare dalla descrizione, e serve a `receipt-insights` quanto la categoria.
- **Validazione somma righe ≈ totale**, e **per aliquota** quando il riepilogo IVA è leggibile: l'app confronta ciò che ha letto con il totale già noto e **lo dice** quando non torna, invece di presentare righe plausibili e sbagliate. Il controllo per aliquota trova anche gli errori che si compensano tra due righe e che il solo totale lascia passare. Nessuna correzione automatica silenziosa: la discrepanza è un segnale, non un errore da nascondere.
- **Correzione manuale delle righe** nella schermata di conferma e su uno scontrino già salvato: modificare descrizione, quantità e prezzo, aggiungere una riga che l'OCR ha perso, eliminarne una inventata.
- **Categoria per riga**, da un **dizionario locale parola-chiave → categoria** come seed statico bundle nell'app (stesso modello di `issuer-seed`), con confronto tollerante alle abbreviazioni dello scontrino (`PAST.BARILLA`, `LATTE P.S.`).
- **Apprendimento locale**: quando l'utente corregge la categoria di un prodotto, la correzione diventa una **mappatura persistita** che vale per gli scontrini successivi. La spesa è ripetitiva: dopo qualche settimana il grosso del carrello abituale è già classificato.
- **Le righe compaiono nel dettaglio dello scontrino**, con il segnale di quadratura rispetto al totale.
- **Nessuna rete, nessun segreto**: come `receipt-capture`, la change è interamente offline. La normalizzazione via modello linguistico è la change successiva, e sarà opt-in.

## Capabilities

### New Capabilities

- `receipt-items`: righe prodotto di uno scontrino — ricostruzione dalla geometria dell'OCR, interpretazione di quantità/peso/sconti/continuazioni, validazione della somma rispetto al totale, correzione manuale, persistenza.
- `receipt-categories`: classificazione di una riga prodotto in una categoria di spesa — dizionario seed bundle nell'app, confronto tollerante, e mappature apprese dalle correzioni dell'utente, persistite e prioritarie sul seed.

### Modified Capabilities

- `receipt-scan`: la capability dichiara oggi, nel proprio scopo, di **non coprire le righe né le categorie**; quel confine si sposta. Cambiano inoltre tre requisiti: l'**estrazione della testata** aggiunge il totale dell'**imposta** ("di cui IVA"), la **schermata di conferma** non mostra più i soli campi di testata, e il **dettaglio** dello scontrino non mostra più i soli dati estratti. La persistenza dello scontrino diventa persistenza dello scontrino **e delle sue righe**, che vivono e muoiono con lui (eliminazione logica compresa).

## Impact

**Modello dati**
- Nuova tabella delle **righe prodotto**, figlie dello scontrino, con Id client-generati e tombstone come tutte le altre entità. Tabella vera e non colonna serializzata — a differenza delle label delle carte — perché `receipt-insights` dovrà interrogarle in SQL (top alimenti, andamento prezzo di un prodotto, spesa per aliquota).
- Una colonna in più sulla tabella **scontrino** esistente per il totale dell'imposta. È l'unica modifica a una tabella già in uso, e non richiede migrazione: il provider aggiunge la colonna mancante all'apertura, come già avvenuto per le carte.
- Nuova tabella delle **mappature prodotto → categoria** apprese dall'utente. Nasce qui, ma è la stessa tabella che `receipt-ai-normalize` userà come cache: va progettata ora per reggere anche quell'uso, o la change successiva la migra.
- Importi in **centesimi interi** e quantità in unità intere di scala fissa: nessuna virgola mobile lungo il percorso, come già per il totale.
- Versione di schema del database **v3 → v4**, per la guardia di compatibilità del ripristino Drive.

**Codice esistente toccato**
- `ReceiptTextLayout`: deve esporre le righe visive **con la loro geometria**, non solo come stringa. L'uso attuale da parte di `ReceiptHeaderParser` non deve cambiare comportamento — i test esistenti sono la rete che lo garantisce.
- Schermata di conferma, dettaglio e repository degli scontrini: estesi alle righe.
- `DatabaseService`: creazione delle nuove tabelle e incremento della versione di schema.
- Nessuna modifica al comportamento delle carte fedeltà, né alla testata già estratta oggi.

**Test**
- Il progetto `tests/CardMaster.Tests` si estende alla logica pura delle righe (ricostruzione dalle colonne, casi sporchi, quadratura) e del confronto con il dizionario. È la parte della feature dove un errore non fa crashare niente e falsa tutto: una riga letta a 15,00 invece di 1,50 sparisce dentro un totale che quadra a occhio.

**Dipendenze**
- Nessuna nuova dipendenza, nessun nuovo permesso, nessuna crescita dell'APK oltre al seed delle categorie (qualche KB di JSON).

**Non-goals**
- Normalizzazione dei nomi prodotto via modello linguistico e qualunque gestione di chiavi API: change `receipt-ai-normalize`.
- Classifiche, andamento prezzi, viste di analisi: change `receipt-insights`.
- Riconoscimento dei codici EAN stampati sullo scontrino, dei punti fedeltà, del metodo di pagamento.
- Scontrini non italiani, valute diverse dall'euro.
