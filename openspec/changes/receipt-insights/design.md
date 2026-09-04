## Context

Il database locale contiene già tutto ciò che serve: `Receipt` (esercente, data, totale, imposta) e `ReceiptItem` (descrizione grezza e **normalizzata**, quantità in millesimi, unità, prezzo unitario, importo, aliquota, categoria). Nessuna colonna va aggiunta. Ciò che manca è leggerli.

Lo stato attuale delle letture è il punto di partenza da cui questa change si distacca. `ReceiptListViewModel.LoadAsync()` chiama `GetAllAsync()` e poi aggrega **in memoria** con LINQ per ricavare il totale del mese e la spesa per esercente. Su un mese di scontrini è irrilevante; su un `GROUP BY` di tutte le righe di tutti gli scontrini è la cosa sbagliata da fare — e diventa più sbagliata ogni mese che passa, in modo invisibile finché l'app non è lenta su un device vero con due anni di spesa dentro.

Il vincolo che modella tutto il resto è la **qualità nota e imperfetta** del dato: 24 righe ricostruite su 29 sull'ultimo scontrino reale misurato, alcune righe fuse, alcune senza categoria, alcune senza prezzo unitario. Le viste non possono fingere che il dato sia completo, e non possono nemmeno rifiutarsi di esistere finché non lo è.

## Goals / Non-Goals

**Goals:**
- Quattro viste che rispondono a domande vere: cosa compro, quanto pesa ogni categoria, dove spendo, come cambia il prezzo di una cosa nel tempo.
- Aggregazioni calcolate dal **database**, con un costo che non cresce con l'intero storico caricato in memoria.
- **Copertura dichiarata** su ogni vista: quanti scontrini e quante righe hanno contribuito, e quanto è rimasto fuori. Una classifica costruita su metà dei dati va detto che lo è.
- Restare offline, senza dipendenze nuove, senza permessi nuovi, senza toccare lo schema.

**Non-Goals:**
- Normalizzare o raggruppare i nomi prodotto oltre la `NormalizedDescription` già persistita: è `receipt-ai-normalize`, e questa change serve anche a misurarne il bisogno.
- Migliorare la ricostruzione delle righe: è già stata misurata e chiusa in `receipt-items`.
- Previsioni, budget, avvisi di spesa, esportazioni. Sono altre change, se mai.
- Grafici interattivi. La sparkline è un disegno, non un componente con zoom e tooltip.

## Decisions

### Le aggregazioni si scrivono in SQL, e le viste ricevono righe già aggregate

Ogni vista corrisponde a una query con `GROUP BY` che restituisce **poche decine di righe**, non allo storico intero filtrato dopo. Un `IReceiptAnalytics` espone un metodo per vista, con tipi di ritorno propri (non entità del dominio): la vista "top prodotti" non ha bisogno di `ReceiptItem`, ha bisogno di *nome, conteggio, spesa totale*.

*Alternativa scartata:* continuare con `GetAllAsync()` + LINQ come fa oggi la lista. È la strada più corta e il motivo per cui non si prende è che il costo non si vede mai durante lo sviluppo — funziona benissimo con dieci scontrini di prova — e si presenta tutto insieme sul device di chi usa l'app da un anno.

### La copertura è un risultato della query, non una stima

Ogni metodo restituisce, insieme ai dati, **quanti scontrini e quante righe** sono entrati nel calcolo e quanti sono rimasti fuori e perché (scontrino senza righe, riga senza categoria, riga senza prezzo unitario). Si calcola con la stessa lettura, non con una seconda passata approssimata.

Serve perché il dato è imperfetto **per costruzione nota**: le righe si ricostruiscono da una fotografia, e una parte non si ricostruisce. Una vista che mostra "Top prodotti" senza dire che è costruita sul 70% delle righe induce una fiducia che i dati non meritano. Dichiararlo costa una riga di interfaccia ed è l'unica cosa che rende la vista onesta.

### Il prezzo unitario per la serie storica: solo quando è stampato, mai ricavato

`UnitPriceCents` è nullable proprio perché molti scontrini non stampano il prezzo unitario. La tentazione è dividere l'importo per la quantità e riempire il buco. Non si fa: su una riga con quantità non letta bene, o su una riga fusa che contiene due prodotti, quel quoziente è un numero plausibile e sbagliato — esattamente il tipo di errore silenzioso che questo dominio produce. Le righe senza prezzo unitario **non entrano** nella serie storica e vengono contate nella copertura.

*Conseguenza accettata:* la quarta vista sarà la più magra delle quattro, e su alcuni prodotti non avrà abbastanza punti per dire niente. È il comportamento giusto: meglio una serie vuota che una serie inventata.

### Confronto tra prodotti diversi solo a parità di unità

Un prezzo unitario al chilo e uno al pezzo non stanno sullo stesso grafico né nella stessa classifica per spesa unitaria: `Unit` esiste in tabella per questa ragione. Le serie si costruiscono per (`NormalizedDescription`, `Unit`), e un prodotto comprato sia a peso sia a pezzo produce due serie, non una media senza significato.

### Sparkline disegnata con SkiaSharp, senza libreria di grafici

SkiaSharp è già una dipendenza (rendering dei barcode). Una sparkline è una polilinea con un minimo, un massimo e l'ultimo punto marcato: si disegna in poche decine di righe. Aggiungere una libreria di charting per questo significherebbe pagare megabyte di APK — che l'auto-update riscarica a ogni versione, come misurato per ML Kit (+9,8 MB) e per l'SDK Anthropic (+3,96 MB) — per un disegno che non li vale.

### Le viste vivono sotto Scontrini, non in una terza tab

La barra ha due voci (Carte, Scontrini) e l'analisi appartiene agli scontrini: ci si arriva dalla sezione Scontrini, non da un terzo posto di pari livello. Una terza tab dichiarerebbe che l'analisi è importante quanto le carte fedeltà, che è il motivo per cui l'app esiste.

### La spesa per esercente/mese si sposta, non si duplica

Oggi vive in `ReceiptListViewModel` calcolata in memoria e limitata al mese corrente. Diventa una query di `IReceiptAnalytics` su tutto lo storico, e la lista scontrini consuma quella. Due implementazioni della stessa aggregazione divergerebbero al primo aggiustamento — è la stessa ragione per cui `ToVisualText` è costruito sopra `ToVisualLayout` in `ReceiptTextLayout`.

## Risks / Trade-offs

**Le classifiche sono illeggibili perché i nomi grezzi frammentano lo stesso prodotto** (`PRIMOSALE S/LATTOSIO V M4,00` e `PRIMOSALE S/LATTOSIO` come due voci) → è il rischio principale, ed è anche **l'informazione che questa change deve produrre**: se le prime tre viste risultano inutilizzabili coi nomi grezzi, `receipt-ai-normalize` ha il suo criterio di successo scritto nero su bianco; se risultano leggibili, quella change vale meno di quanto sembrasse. Non si mitiga inventando un raggruppamento fuzzy qui, che duplicherebbe male il lavoro della change successiva.

**Le righe fuse inquinano le classifiche** (`TOFU BIO SPRAY VETRI ECOLOGICO` come singolo "prodotto") → non si filtrano con euristiche sulla lunghezza della descrizione, che scarterebbero anche prodotti dal nome lungo e legittimo. Restano visibili, e la loro visibilità è essa stessa il segnale: una classifica piena di righe fuse dice quanto vale la ricostruzione meglio di qualsiasi percentuale.

**Le query si scoprono lente solo con molti dati** → si verifica su emulatore con uno storico **generato apposta** (centinaia di scontrini, migliaia di righe), non con i tre scontrini di prova. Gli indici si decidono sui piani di esecuzione reali, non per intuizione.

**Le aggregazioni SQL non entrano nel progetto di test** perché richiedono un database, e il progetto di test compila i sorgenti dell'app senza toccare la piattaforma (`CLAUDE.md`) → si testa la logica pura (formattazione, calcolo della copertura, costruzione delle serie da dati già letti) e si verificano le query su emulatore. Se il bisogno crescesse, la strada dichiarata è estrarre una libreria condivisa, non allungare la lista dei `<Compile Include>`.

**Spostare la spesa del mese dalla lista scontrini rischia di cambiarne il comportamento** → il valore mostrato oggi è verificabile a mano su pochi scontrini: si confronta prima e dopo con gli stessi dati, come è stato fatto per il refactoring di `ReceiptTextLayout` con i suoi test.

## Migration Plan

Nessuna migrazione di dati e nessun cambio di schema: la change legge ciò che c'è. Gli indici eventualmente aggiunti sono creati all'apertura del database, come già avviene per l'indice su `ReceiptId`, e non richiedono di incrementare `SchemaVersion` — non cambiano la forma dei dati e la guardia del backup Drive non è coinvolta.

Chi installa sopra una versione precedente trova le viste popolate con lo storico che ha già, senza dover rifotografare niente. Chi non ha righe (scontrini salvati prima di `receipt-items`) vede viste vuote **con la spiegazione del perché**, non un errore.

Rollback: reinstallando una versione precedente le viste spariscono e nulla si rompe, perché nessun dato è stato modificato.

## Open Questions

- **Quale finestra temporale mostrano le viste per default?** Tutto lo storico è la risposta più semplice, ma su due anni di spesa una classifica "top prodotti" diventa una fotografia del passato più che del presente. L'alternativa (ultimi 12 mesi, con la possibilità di allargare) si decide **guardando le viste popolate**, non prima: con lo storico di prova generato si vede quale delle due risponde alla domanda che ci si fa davvero.
- **La quota "senza categoria" va mostrata come voce nella classifica o come nota accanto?** Come voce compete con le categorie vere e le schiaccia se è grossa; come nota rischia di passare inosservata. Dipende da quanto è grossa in pratica — misurabile alla verifica.
