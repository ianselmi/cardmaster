## Context

`receipt-items` ha consegnato la lettura locale delle righe e, con essa, la misura del proprio limite: 21 righe su 29 su uno scontrino reale fotografato storto, con otto prodotti persi perché appaiati a due a due. Le correzioni fatte in corsa — tetto all'altezza di riga, stima dell'inclinazione dalla geometria — hanno portato da 13 a 21; il residuo richiede di seguire una **curvatura** della carta, non solo una pendenza.

Nello stesso momento esiste un fatto scomodo: un modello multimodale legge quello scontrino per intero. Non perché sia più preciso sui pixel, ma perché riconosce la *pagina* — sa che uno scontrino ha una colonna di descrizioni, una di aliquote e una di prezzi, e che le righe si susseguono. È conoscenza che il parser non ha e non avrà.

Il resto del contesto è quello di sempre, e questa change ne mette in discussione due pezzi: l'app è offline e nessun dato dello scontrino lascia il device. La proposta non li cancella — li rende **condizionati a una scelta dell'utente**, su una funzione che nasce spenta.

## Goals / Non-Goals

**Goals:**

- Chiudere il divario tra 21/29 e 29/29 sugli scontrini dove il parser locale fallisce, **senza toccare** quelli dove funziona.
- Non spendere niente, non inviare niente e non chiedere niente quando lo scontrino quadra.
- Rendere il costo e il trasferimento dei dati **visibili prima**, non scoperti dopo.
- Tenere la chiave dell'utente fuori dal pacchetto, dal repository e dai log.
- Restare un'app che funziona offline: il ramo con il modello è un'aggiunta, mai un prerequisito.
- Lasciare a `receipt-ai-normalize` chiave, client e gestione degli errori già pronti.

**Non-Goals:**

- Sostituire il parser deterministico.
- Chiave gestita da noi, proxy, backend.
- Inviare l'immagine quando la quadratura torna.
- Fine-tuning, prompt caching cross-scontrino, batch: ottimizzazioni premature su un volume di qualche scontrino a settimana.

## Decisions

### La rilettura si attiva sulla quadratura fallita, e su nient'altro

L'app ha già un giudice: la somma delle righe contro il totale stampato, a tolleranza zero. Quando quadra, le righe sono attendibili e non c'è niente da migliorare — nessuna chiamata. Quando non quadra, l'app **sa** di avere torto, ed è l'unico momento in cui vale la pena spendere soldi dell'utente e far uscire una foto.

Questo rende il costo proporzionale al problema invece che al numero di scontrini, e mantiene vera la frase "l'app funziona offline" per il caso normale.

Alternativa scartata: chiamare sempre quando la funzione è accesa. Più semplice da spiegare, ma paga anche quando non serve e fa uscire immagini che potevano restare a casa.

### Si invia l'immagine, non il testo dell'OCR

È il punto che rompe un vincolo dichiarato in `PLAN.md`, e va giustificato: **il testo OCR ha già perso l'informazione che serve**. Nel testo ricostruito dello scontrino MD, sette prodotti erano già fusi in una riga con tutte le aliquote e tutti i prezzi in coda; nessun modello, per quanto bravo, può separarli con certezza perché l'associazione descrizione↔prezzo non è più nel dato. E il totale che l'OCR aveva letto `41,14` era `47,74` sulla carta: solo l'immagine lo corregge.

Il costo di questa scelta è reale — la foto di uno scontrino dice dove si fa la spesa, quando, e cosa si mangia — e si paga con il consenso esplicito, non nascondendolo.

### Output strutturato imposto da uno schema

La risposta arriva conforme a uno **schema JSON** dichiarato nella richiesta (`output_config.format`), non a un formato chiesto nel prompt e sperato. Il parsing non è difensivo perché non deve esserlo: se lo schema è rispettato, i campi ci sono e sono del tipo giusto.

Lo schema riusa le unità del dominio già stabilite — **centesimi interi**, **millesimi** per le quantità, **punti base** per le aliquote — così l'esito del modello entra nelle stesse strutture del parser locale e passa per la stessa quadratura. Nessuna conversione in virgola mobile lungo il percorso.

### L'esito del modello non è verità: passa per la stessa quadratura

Le righe rilette vengono confrontate con il totale esattamente come quelle locali, e mostrate nella stessa schermata di conferma. Se il modello quadra e il parser no, si propone il primo; se **nessuno dei due** quadra, si mostra il migliore e lo si dice. L'utente corregge a mano come sempre.

Non si sostituiscono in silenzio righe corrette con righe di un modello: la sostituzione avviene solo quando c'è un motivo misurabile per farlo.

### La chiave è dell'utente e sta in `SecureStorage`

Il repository è pubblico e l'APK scaricabile: una chiave nostra nel pacchetto sarebbe estraibile da chiunque in cinque minuti, e la pagherebbe l'autore per tutti. L'unica architettura compatibile senza server è la chiave dell'utente, incollata nelle impostazioni e conservata in `SecureStorage` (Keystore Android), mai in `Preferences`, mai nel database, mai nel backup su Drive, mai nei log.

Conseguenza dichiarata: l'utente deve procurarsi una chiave. È un attrito vero, e la funzione è opt-in anche per questo.

### Modello: `claude-opus-5` come default, la scelta all'utente

Il default è `claude-opus-5` — è il caso in cui la lettura conta più del costo, visto che si arriva qui solo dopo un fallimento. L'utente può scegliere un modello più economico, con il costo per scontrino dichiarato accanto:

| Modello | Prezzo (in / out per milione di token) | Ordine di grandezza per scontrino |
|---|---|---|
| `claude-opus-5` (default) | $5 / $25 | ~4 centesimi |
| `claude-sonnet-5` | $3 / $15 | ~2,5 centesimi |
| `claude-haiku-4-5` | $1 / $5 | meno di 1 centesimo |

La stima assume un'immagine ridimensionata (~2.000 token), un prompt breve e un JSON di trenta righe (~1.200 token in uscita). Va **verificata sui token reali** con il conteggio restituito dalla risposta, non lasciata come promessa.

### L'immagine si ridimensiona prima di inviarla

Un'immagine a piena risoluzione può costare fino a ~4.800 token; ridimensionata sul lato lungo ne costa circa la metà, e uno scontrino resta perfettamente leggibile. Il ridimensionamento è anche l'unico punto in cui si può ridurre il dato che esce dal device senza perdere la funzione.

### Errori con categorie comprensibili, sul modello del backup

Chiave assente, chiave rifiutata, credito esaurito, limite di frequenza, rete assente, risposta non conforme, timeout: ciascuno con un messaggio che dice **cosa è successo e cosa può fare l'utente**, come già fa `maui-backup-error-state`. In tutti i casi lo scontrino resta salvabile con le righe locali — un errore del modello non deve mai far perdere il lavoro fatto.

## Risks / Trade-offs

**La foto di uno scontrino esce dal device** → è il costo reale della funzione. Mitigazioni: spenta per default, consenso informato che dice cosa esce, chiamata solo quando la quadratura fallisce, immagine ridimensionata, nessun invio in blocco dello storico. Quello che resta non si mitiga: l'utente che accende sa e accetta.

**La chiave dell'utente è un segreto sul device** → `SecureStorage`, esclusa dal backup Drive, mai stampata nei log né mostrata dopo l'inserimento. Il rischio residuo è quello del device compromesso, uguale a quello di ogni altra app.

**Il pacchetto `Anthropic` potrebbe non compilare per `net10.0-android`** → è il rischio che può fermare la change, e va verificato **per primo**, prima di scrivere altro codice, come si fece con ML Kit. Se non compila, il ripiego è HTTP diretto contro l'API — più codice nostro, stessa funzione.

**Il modello può sbagliare con sicurezza** → un'estrazione plausibile e falsa è peggio di un'estrazione mancata. Per questo l'esito passa per la quadratura e non sostituisce righe corrette, e per questo si mostra all'utente prima di salvare.

**Il costo può sorprendere** → dichiarato prima, per modello, e il conteggio dei token della risposta permette di mostrare il costo **effettivo** dell'ultima chiamata invece di una stima. Nessuna chiamata automatica in background.

**Doppia verità nel codice** → da qui in avanti esistono due modi di leggere uno scontrino. Il rischio è che il parser locale smetta di essere curato. Contromisura: la misura "quante righe su quante" resta sul parser locale, e la rilettura si conta a parte.

## Migration Plan

Nessuna migrazione di dati e nessun cambio di schema: la funzione produce le stesse righe che l'app già sa salvare. Chi non accende la funzione non vede alcuna differenza — stesso comportamento, stesse schermate, stesso funzionamento offline.

Rollback: spegnere l'interruttore. Disinstallando o tornando indietro di versione, la chiave in `SecureStorage` va rimossa esplicitamente dalle impostazioni prima, perché il ripristino di un backup non la riporta e nulla la cancella per conto dell'utente.

## Open Questions

- **Rilettura anche a richiesta, su uno scontrino che quadra?** Un totale corretto non garantisce descrizioni corrette. Un comando esplicito "rileggi con l'AI" nel dettaglio sarebbe utile, ma apre la porta all'uso indiscriminato che la quadratura serve a evitare. Da decidere dopo aver visto quanti scontrini finiscono davvero nel ramo AI.
- **Quanto ridimensionare l'immagine?** Il compromesso tra token e leggibilità va misurato sugli scontrini reali, non deciso a tavolino: si parte dal lato lungo a 1568 px e si verifica se 29 righe su 29 reggono anche lì.
- **Il costo effettivo va mostrato dopo ogni chiamata o solo in un totale mensile?** Per chiamata è più onesto, ma può diventare rumore su una funzione che scatta di rado. Probabilmente entrambi.
