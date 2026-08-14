## Context

`receipt-capture` ha consegnato la testata: acquisizione, OCR on-device, esercente/P.IVA/data/totale, storico, spesa per negozio/mese. Ha anche consegnato — in corso d'opera e prima del previsto — `ReceiptTextLayout`, la ricostruzione delle righe visive dalla geometria dell'OCR, perché senza di essa non era estraibile nemmeno il totale.

Quella classe è il punto di partenza di questa change, ma **appiattisce**: prende i frammenti di una banda verticale, li ordina per `x` e li concatena in una stringa separata da due spazi. Per la testata basta (`TOTALE COMPLESSIVO  6,61` è una riga leggibile da una regex); per le righe prodotto no, perché la distanza orizzontale tra descrizione e prezzo è **l'informazione che dice quali sono le due colonne**, e concatenando la si butta via.

Il resto del contesto è quello già fissato e non in discussione: offline puro, nessun segreto nel pacchetto, Id client-generati e tombstone, importi in centesimi interi, `dotnet build` a 0 errori. In più, da `receipt-capture`, esiste `tests/CardMaster.Tests`: la logica pura di questa change **nasce testabile**, perché è la parte del progetto dove un errore non fa rumore — una riga letta 15,00 invece di 1,50 sparisce dentro un totale che a occhio quadra.

Nota di realtà: questa è la change che decide se il dominio scontrini vale la pena. Le regole della testata operano su un testo regolare; qui si opera sulla parte irregolare dello scontrino, e la percentuale di righe lette bene non arriverà mai a 100. Il design è costruito attorno a questo fatto, non contro.

## Goals / Non-Goals

**Goals:**

- Ricostruire le righe prodotto dalla **geometria** dell'OCR, separando descrizione e importo per colonna, non per ordine del testo.
- Interpretare i casi che sugli scontrini italiani sono la norma: quantità per prezzo unitario, prodotti a peso, sconti, descrizioni mandate a capo.
- Leggere l'**aliquota IVA** di ogni riga, dalla colonna o dal riepilogo a piè di scontrino, perché senza di essa la spesa alimentare e quella non alimentare non sono separabili se non indovinando dalla descrizione.
- Rendere **visibile** l'incertezza: la somma delle righe si confronta con il totale già noto, e la discrepanza si dichiara.
- Rendere la **correzione manuale** un percorso di prima classe, come già per la testata: se il riconoscimento sbaglia tutto, lo scontrino resta salvabile e corretto.
- Classificare le righe con un dizionario locale, e far sì che **una correzione dell'utente valga anche per il futuro**.
- Lasciare a `receipt-insights` righe **interrogabili in SQL**, e a `receipt-ai-normalize` una tabella di mappature **già pronta a fare da cache**.
- Non cambiare di una virgola il comportamento della testata già in produzione.

**Non-Goals:**

- Chiamate a un modello linguistico, chiavi API, normalizzazione semantica dei nomi: change `receipt-ai-normalize`.
- Classifiche, andamento prezzi, grafici: change `receipt-insights`.
- Codici EAN stampati, punti fedeltà, metodo di pagamento, resi.
- Scontrini non italiani, valute diverse dall'euro.
- Rilevamento bordi e deskew dell'immagine: fuori scopo come in `receipt-capture`.

## Decisions

### `ReceiptTextLayout` restituisce righe **strutturate**, non stringhe

Si aggiunge un tipo `ReceiptVisualLine` (testo della riga, frammenti con i loro rettangoli, rettangolo complessivo) e un metodo che lo produce. `ToVisualText`/`ToVisualLines` restano **esattamente com'è oggi**, implementati sopra il nuovo metodo: `ReceiptHeaderParser` non deve accorgersi di nulla, e i test esistenti su testata e layout sono la rete che lo dimostra.

Alternativa scartata: un parser di righe che rifà da capo il raggruppamento per banda verticale. Duplicherebbe l'unica euristica del progetto già verificata su OCR reale, e le due copie divergerebbero al primo aggiustamento.

### La colonna prezzo si riconosce dalla posizione, non dall'ultimo numero della riga

Dentro una riga visiva, l'importo è il frammento numerico **più a destra** che superi una soglia di `x` calcolata **sullo scontrino intero**, non riga per riga: si prende la distribuzione dei bordi destri degli importi candidati e si stabilisce dove cade la colonna dei prezzi. Questo è ciò che distingue `PROSCIUTTO 100 GR   4,50` (dove `100 GR` è descrizione e `4,50` è prezzo) da una riga in cui l'unico numero è dentro il nome del prodotto.

Alternativa scartata: "ultimo numero della riga = prezzo", che è ciò che fa oggi `LastAmountCents` per il totale. Sul totale è sicuro perché la riga è ancorata a una parola chiave; sul corpo produrrebbe prezzi da `PASTA 500 GR` senza che nulla lo segnali.

### Il corpo si delimita, non si indovina

Le righe prodotto stanno tra la fine della testata e la riga del totale. Il confine inferiore è già noto: è la riga che `ReceiptHeaderParser` ha riconosciuto come totale (o il primo `SUBTOTALE`, se precede). Il confine superiore è la prima riga che ha un importo in colonna prezzo dopo l'intestazione. Le righe fuori da questo intervallo non sono candidate, per quanto assomiglino a prodotti.

Conseguenza organizzativa: il parser delle righe **riusa** l'individuazione della riga del totale già scritta, che va quindi esposta (oggi `FindTotalCents` restituisce solo l'importo, non l'indice della riga). È una piccola apertura di `ReceiptHeaderParser`, non una riscrittura.

### Casi sporchi: regole nominate, ciascuna con il suo test

Non un'unica euristica onnicomprensiva, ma regole distinte applicate in ordine, ognuna riconoscibile per nome nel codice e nei test:

- **Quantità esplicita** (`2 X 1,50`, `2 x 1,50`, `2 PZ x 1,50`): la riga porta quantità e prezzo unitario; l'importo di riga è il prodotto, e se sullo scontrino c'è anche il totale di riga si verifica che coincida.
- **Peso** (`0,432 kg x 2,99 €/kg`): stessa forma, quantità frazionaria. La quantità si conserva in **millesimi interi** (`0,432 kg` → `432`), mai in virgola mobile — stesso principio dei centesimi.
- **Sconto/promozione** (riga con importo negativo, o marcata `SCONTO`/`PROMO`/`OFFERTA`): riga con importo negativo, **non** un prodotto. Conta nella somma, non nelle classifiche di `receipt-insights`.
- **Continuazione**: riga senza importo in colonna prezzo, subito sotto una riga prodotto → è il seguito della descrizione, si accoda. È il caso che senza trattamento genera prodotti fantasma con prezzo nullo.
- **Riga di servizio** (reparto, codice, `PEZZI N.`): scartata per parola chiave. Il **riepilogo IVA** è l'eccezione: non diventa una riga prodotto, ma non viene nemmeno buttato — si legge (vedi sotto).

Le quantità implicite restano 1: non si inventa nulla, come per la testata.

### L'aliquota IVA si legge in colonna, e il riepilogo a piè di scontrino la decodifica

Tra la descrizione e il prezzo gli scontrini italiani stampano un terzo campo, che è **l'aliquota** (`4,00`, `10,00`, `22,00`) oppure un **codice di reparto** a una cifra (`1`, `2`, `3`). Nel secondo caso il codice da solo non significa niente: la corrispondenza sta nel **riepilogo IVA** a piè di scontrino, che elenca per ogni reparto l'imponibile, l'imposta e l'aliquota. Si legge quel blocco — che è fuori dal corpo, quindi non produce righe prodotto — e se ne ricava la mappa codice → aliquota, più il **totale dell'imposta**.

L'aliquota si conserva in **punti base interi** (`4,00%` → `400`), stessa regola dei centesimi: le aliquote italiane hanno due decimali e non devono passare da un `double`.

Quando la colonna non è leggibile, o il codice non compare nel riepilogo, l'aliquota della riga resta **vuota**. Non si deduce dalla categoria e non si assume 22%: sarebbe un valore inventato che nelle aggregazioni è indistinguibile da uno letto.

Alternativa scartata: dedurre l'aliquota dalla categoria del prodotto. Andrebbe storta esattamente dove serve — sugli stessi scaffali convivono aliquote diverse, e un prodotto in promozione può cambiare reparto.

### La quadratura per aliquota è più severa di quella sul totale

Quando il riepilogo IVA è leggibile, si confronta l'imponibile di **ciascuna aliquota** con la somma delle righe che la portano, e il totale imposta con quello stampato. È un controllo che il totale da solo non fa: due prezzi letti male che si compensano passano la quadratura complessiva e non quella per aliquota. Vale le stesse regole dell'altra — tolleranza zero, si dichiara lo scarto, non si corregge nulla d'ufficio.

### La quadratura è un segnale, non una correzione

Somma delle righe (sconti compresi) confrontata con il totale della testata. Se coincide, le righe sono attendibili e lo si dice. Se non coincide, si mostra **di quanto** e si invita alla correzione — senza aggiungere una riga fittizia "differenza", senza aggiustare l'ultimo prezzo, senza rifiutare il salvataggio.

La tolleranza è **zero centesimi** con un'eccezione dichiarata: se manca il totale di testata non c'è quadratura da fare, e le righe restano non validate. Un margine di tolleranza sembrerebbe indulgente e invece nasconderebbe esattamente l'errore che questa verifica esiste per trovare: un prezzo letto con una cifra di troppo produce uno scarto grande, uno letto con la virgola spostata ne produce uno piccolo, ed è il secondo quello pericoloso.

### Le righe sono una tabella, non una colonna serializzata

`ReceiptItem` è una tabella figlia con `ReceiptId`, `Id` client-generato e tombstone come ogni altra entità. Diverso dalla scelta fatta per le label delle carte (una colonna CSV) e per una ragione precisa: `receipt-insights` deve fare `GROUP BY` su prodotti e categorie e leggere serie storiche di prezzi. Con le righe dentro una stringa, quelle quattro viste diventerebbero quattro scansioni in memoria di tutto lo storico.

Il prezzo per la sync v2 si paga volentieri: le righe **appartengono** allo scontrino e si sostituiscono in blocco quando lo scontrino cambia (salvataggio = tombstone delle righe precedenti + inserimento delle nuove). Il last-write-wins per riga resta applicabile all'unità che conta, lo scontrino, che è l'unica cosa che un utente modifica per intero.

### Categorie: seed bundle + mappature apprese, con precedenza alle seconde

Due sorgenti, interrogate in quest'ordine:

1. **Mappature apprese** (`ProductMapping`, tabella locale): chiave = descrizione **normalizzata** con `TextNormalizer` — stessa regola già usata da ricerca e label, non una seconda. Nasce dalle correzioni dell'utente e vince sempre sul seed, perché è l'utente ad aver visto quel prodotto.
2. **Dizionario seed** (`Resources/Raw/categories.json`, versionato come `issuers.json`): parola chiave → categoria, confrontata per **token contenuti e prefisso** sulla descrizione normalizzata. `PAST.BARILLA 500` contiene il token `past` che è prefisso di `pasta`.

Senza corrispondenza la riga resta **senza categoria**, visibile come tale. Nessuna categoria "Altro" assegnata d'ufficio: sarebbe indistinguibile da una classificazione riuscita e renderebbe illeggibili le viste di `receipt-insights`.

Sul fuzzy match: si sta sul confronto per token e prefisso, **non** su una distanza di edit generica. Una distanza di Levenshtein libera su descrizioni di 8 caratteri accoppia `MELE` e `MIELE`, che sono due categorie diverse — e un falso positivo silenzioso in classificazione è peggio di una riga non classificata, che almeno si vede.

### `ProductMapping` nasce già come cache della change successiva

La tabella ha `NormalizedDescription` (chiave), `Category`, un `DisplayName` normalizzato — vuoto in questa change — e l'**origine** della mappatura (`Seed` corretto dall'utente / `User` / in futuro `Ai`). Il `DisplayName` e il valore `Ai` non servono oggi: esistono perché `receipt-ai-normalize` è dichiarata come **cache-first su questa tabella**, e nascere con due colonne in più costa zero mentre migrarla dopo costa una migrazione su dati dell'utente.

L'origine serve anche a una regola futura non ambigua: una mappatura scritta dall'utente non deve mai essere sovrascritta da una prodotta da un modello.

### La logica pura sta fuori dai ViewModel, e nei test

`ReceiptItemsParser`, `ReceiptItemLine`, `ReceiptTotalsCheck`, `CategoryMatcher`: classi senza dipendenze da MAUI, ML Kit o database, collegate in `CardMaster.Tests` con `<Compile Include>` come le tre già presenti. Il `CategoryMatcher` prende il dizionario **come dato**, non lo carica: il caricamento dal bundle sta in un `ICategoryCatalog` di contorno, sul modello di `IssuerCatalog`.

I test coprono almeno: colonne separate correttamente, quantità esplicita, peso, sconto negativo, continuazione, riga di servizio scartata, corpo delimitato tra testata e totale, quadratura esatta e quadratura fallita, e il fatto che la testata continui a essere estratta identica.

### Schema v3 → v4

Due tabelle nuove e **una colonna in più** su `Receipt` (il totale imposta). `CreateTableAsync` crea le tabelle al primo avvio della versione nuova e aggiunge la colonna mancante a quella esistente — è lo stesso `ALTER TABLE ADD COLUMN` implicito già sfruttato dalle carte, quindi nessuna migrazione da scrivere e nessun dato da toccare. L'incremento di versione serve, come sempre in questo progetto, solo alla guardia del ripristino Drive: un backup con le righe non deve essere ripristinato da un'app che non le conosce.

## Risks / Trade-offs

**La percentuale di righe lette correttamente è bassa sugli scontrini reali** → è il rischio che questa change esiste per misurare, ed è la ragione per cui la quadratura è un requisito e non un dettaglio: dice all'utente, scontrino per scontrino, se può fidarsi. La verifica su emulatore deve usare gli stessi scontrini reali di catene diverse già usati in `receipt-capture`, e il riepilogo della change deve riportare **quante righe su quante** sono state ricostruite correttamente, non un giudizio qualitativo.

**La soglia della colonna prezzo non regge su scontrini con layout diverso** (importo centrato, colonna quantità intermedia, font proporzionale) → la soglia si calcola sullo scontrino corrente, non è una costante; e la correzione manuale resta la via d'uscita. Se un layout comune risultasse sistematicamente inservibile, si affronta con quel layout sotto gli occhi, non prevedendolo ora.

**Il dizionario seed è tarato su ciò che l'autore compra** → è previsto: il seed copre le categorie larghe della spesa alimentare e domestica, e l'apprendimento locale è precisamente il meccanismo che lo adatta all'utente reale. Il seed è un punto di partenza, non una pretesa di completezza.

**Una mappatura appresa sbagliata si propaga a tutti gli scontrini futuri** → la mappatura è modificabile: correggere di nuovo la categoria di quel prodotto riscrive la mappatura, non ne accumula una seconda.

**La schermata di conferma diventa lunga e faticosa** → il carrello della spesa ha decine di righe, e chiedere di verificarle una per una farebbe abbandonare la funzione al terzo scontrino. Per questo la quadratura è in cima e le righe sotto: se il totale torna, si conferma e basta; si scende nel dettaglio solo quando qualcosa non torna. È una decisione di prodotto, non solo di interfaccia.

**Aprire `ReceiptHeaderParser` per riusare l'individuazione del totale rischia di romperlo** → i test esistenti sulla testata girano prima e dopo. Se il riuso richiedesse di cambiarne il comportamento e non solo la superficie, si preferisce non riusare.

**Le righe moltiplicano le dimensioni del database** → decine di righe per scontrino invece di una. Restano testo breve e interi; incide sullo snapshot di backup molto meno delle immagini, che infatti stanno fuori dal database per la stessa ragione.

## Migration Plan

Nessuna migrazione di dati: si aggiungono due tabelle e non se ne modifica nessuna. Gli scontrini già salvati **restano senza righe** e continuano a funzionare come oggi — testata, storico, totali mensili.

Il `RawText` conservato da `receipt-capture` rende possibile ricavare le righe di uno scontrino vecchio senza rifotografarlo. Si valuta se offrirlo come comando esplicito nel dettaglio ("leggi le righe") o non offrirlo in questa change: è la stessa domanda già lasciata aperta lì sulla ri-estrazione della testata, e la risposta dipende da quanto bene funziona la ricostruzione — deciderlo prima di averlo misurato sarebbe indovinare.

Rollback: reinstallando una versione precedente gli scontrini e le carte restano intatti; le righe smettono di essere visibili senza che nulla si rompa, perché nessuna tabella esistente cambia.

## Open Questions

- **Le righe di uno scontrino già salvato si possono ricavare a posteriori dal `RawText`?** ~~Tecnicamente sì.~~ **Deciso il 14 ago 2026: no, e non per come funziona la ricostruzione ma per che cosa conserviamo.** Il `RawText` salvato **è già il testo ricostruito in righe visive**, non i frammenti dell'OCR: le colonne sono appiattite in una stringa e **la geometria non c'è più**. Letto dal database di uno scontrino vero: `'FABIO R.  2,00'`, `'*PRIMOSALE S/LATTOSIO V M4,00  10,00  1,39'`. Ma `ReceiptItemsParser` vive sui bounding box — la soglia della colonna prezzo si calcola sui bordi destri — quindi ri-estrarre dal `RawText` non potrebbe usarlo, e soprattutto **erediterebbe gli errori già commessi**: le righe che l'OCR ha fuso sono fuse anche lì. Darebbe lo stesso risultato della prima lettura, mai migliore, perché l'informazione è stata persa a monte. Il prerequisito non è un comando in interfaccia ma **conservare la geometria**, che oggi non conserviamo e che peserebbe molto più del testo. Chi ha uno scontrino letto male ha già due strade che recuperano informazione davvero: correggere a mano, o `receipt-ai-scan`, che riparte dall'immagine.
- **Quante categorie deve avere il seed?** **Confermato il 14 ago 2026: si resta larghi, 11 categorie, il seed non va rivisto.** Sullo scontrino MD reale 19 righe su 24 hanno preso una categoria, e le assegnazioni giuste sono nette (`CAROTE`→Ortofrutta, `MOZZARELLA`→Latticini e uova, `SGRASSATORE`→Cura della casa). Gli errori osservati **non vengono dalla granularità**: `BURGER VEGETALI CON MELANZANE` finisce in Ortofrutta perché *melanzane* è una parola chiave, e `TOFU BIO SPRAY VETRI ECOLOGICO` prende Cura della casa perché è una **riga fusa** che contiene due prodotti. Spaccare le categorie non correggerebbe nessuno dei due: il primo è il match per token che pesca una parola qualsiasi della descrizione, il secondo è un difetto delle righe. Si migliora dove nasce il problema — le righe, e la correzione manuale che scrive la mappatura appresa — non moltiplicando le categorie.
- **La correzione della categoria si applica retroattivamente agli scontrini già salvati?** No in questa change — la mappatura vale da lì in avanti. Applicarla all'indietro è una riscrittura di dati storici dell'utente e merita una decisione sua, eventualmente in `receipt-insights` dove se ne vedrebbe l'effetto.
