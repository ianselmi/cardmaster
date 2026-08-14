## 1. Uno storico vero su cui misurare, prima di scrivere le query

- [ ] 1.1 Generare uno storico di prova consistente (ordine delle centinaia di scontrini e delle migliaia di righe, distribuito su ≥ 18 mesi, con prodotti ricorrenti, righe senza categoria, righe senza prezzo unitario e scontrini senza righe): serve **prima** delle query, perché è l'unica cosa che rende visibile il costo di un'aggregazione sbagliata
- [ ] 1.2 Caricarlo sull'emulatore e annotare la dimensione del database: è il metro con cui si giudicheranno i tempi ai punti 6.x
- [ ] 1.3 Mantenere il generatore fuori dall'APK (script o percorso di sviluppo, non codice dell'app): non deve esistere alcun modo di popolare il database dell'utente con dati finti

## 2. Lettura aggregata

- [ ] 2.1 `IReceiptAnalytics` in `Services/Receipts/`, con un metodo per vista e **tipi di ritorno propri** (nome + conteggio + spesa, non `ReceiptItem`)
- [ ] 2.2 Ogni risultato porta con sé la **copertura**: scontrini e righe entrati nel calcolo, quanti esclusi e per quale motivo — calcolata dalla stessa lettura, non da una seconda passata
- [ ] 2.3 Implementazione SQL con `GROUP BY`: nessun metodo carica lo storico in memoria per aggregarlo nel codice
- [ ] 2.4 Indici necessari alle aggregazioni creati all'apertura del database, **senza incrementare `SchemaVersion`** (non cambiano la forma dei dati e non riguardano la guardia del ripristino)
- [ ] 2.5 Registrare il servizio in `MauiProgram.cs`
- [ ] 2.6 Verificare sui piani di esecuzione (`EXPLAIN QUERY PLAN`) che gli indici siano davvero usati: un indice che c'è ma non viene scelto è peso morto

## 3. Le quattro viste

- [ ] 3.1 **Top prodotti**: aggregazione su `NormalizedDescription`, ordinabile per numero di acquisti e per spesa; righe di sconto escluse dai prodotti; **nessun raggruppamento fuzzy dei nomi** — è `receipt-ai-normalize`, e mescolarlo qui falserebbe la misura che questa change deve produrre
- [ ] 3.2 **Top categorie**: spesa per categoria, con la quota **senza categoria** dichiarata e distinta dalle categorie vere, mai attribuita a una di esse
- [ ] 3.3 **Spesa per negozio e mese**: aggregazione su tutto lo storico, con scelta del mese; scontrini senza data o senza totale fuori dai totali e individuabili
- [ ] 3.4 **Andamento prezzo**: serie per (`NormalizedDescription`, `Unit`), **solo** da `UnitPriceCents` stampato — mai importo diviso quantità; righe senza prezzo unitario escluse e contate nella copertura
- [ ] 3.5 Serie con troppi pochi punti: dichiarata come insufficiente invece di disegnare una linea priva di significato

## 4. Interfaccia

- [ ] 4.1 Sezione **Analisi** raggiungibile dalla sezione Scontrini, **non** come terza voce della barra di navigazione
- [ ] 4.2 Pagine e ViewModel delle quattro viste, con la **copertura visibile accanto ai risultati** e il motivo delle esclusioni, non il solo numero
- [ ] 4.3 Sparkline con SkiaSharp (polilinea, minimo, massimo, ultimo punto): nessuna libreria di grafici, il cui peso l'auto-update riscaricherebbe a ogni versione
- [ ] 4.4 Stato "dati insufficienti" per ogni vista, distinto da un errore e distinto da un risultato pari a zero
- [ ] 4.5 Formattazione **euro e italiano** indipendente dalla cultura del device, riusando la formattazione già in uso negli scontrini invece di riscriverla
- [ ] 4.6 Tema chiaro e scuro, sparkline compresa

## 5. Spostare la spesa del mese, non duplicarla

- [ ] 5.1 Annotare i valori mostrati oggi da `ReceiptListViewModel` su un piccolo insieme di scontrini: è il confronto prima/dopo, come i test invariati che hanno protetto il refactoring di `ReceiptTextLayout`
- [ ] 5.2 Far consumare alla lista scontrini l'aggregazione di `IReceiptAnalytics`, rimuovendo il calcolo in memoria
- [ ] 5.3 Verificare che i valori coincidano con quelli annotati in 5.1, e che il riepilogo accanto alla lista e la vista estesa mostrino lo **stesso** numero per lo stesso mese

## 6. Test e verifica

- [ ] 6.1 Collegare in `tests/CardMaster.Tests` la sola **logica pura** nuova: calcolo della copertura, costruzione delle serie da dati già letti, formattazione. Le query non entrano — richiedono un database, e il progetto di test compila i sorgenti dell'app senza toccare la piattaforma (`CLAUDE.md`)
- [ ] 6.2 Test della copertura: righe escluse contate e attribuite al motivo giusto; nessuna vista che dichiari copertura piena quando non lo è
- [ ] 6.3 Test delle serie: unità diverse non mescolate, prezzo unitario mai dedotto dall'importo, serie troppo corta dichiarata insufficiente
- [ ] 6.4 `dotnet test` verde e `dotnet build` con 0 errori (criterio di accettazione, non opzionale)
- [ ] 6.5 Verifica su emulatore sullo storico generato in 1.1: **riportare i tempi** di apertura di ciascuna vista, non un giudizio
- [ ] 6.6 Verifica in **modalità aereo** di tutte e quattro le viste
- [ ] 6.7 Verifica sullo storico **reale** (i pochi scontrini veri già acquisiti): le viste devono reggere anche con 3 scontrini, non solo con 300
- [ ] 6.8 Verifica su un database **senza righe** (scontrini salvati prima di `receipt-items`): viste vuote **con la spiegazione**, non errori
- [ ] 6.9 Verifica dell'**aggiornamento** sopra la versione precedente: storico intatto, viste popolate senza rifotografare nulla, `SchemaVersion` invariata

## 7. La misura che è metà del senso di questa change

- [ ] 7.1 Guardare le classifiche popolate con lo storico **reale** e riportare, con esempi concreti, **quanto i nomi grezzi frammentano lo stesso prodotto**: è il criterio di successo di `receipt-ai-normalize`, e va scritto qui mentre lo si vede
- [ ] 7.2 Riportare **quanta spesa finisce senza categoria** in pratica: decide anche la seconda domanda aperta in `design.md` (quota come voce della classifica o come nota)
- [ ] 7.3 Riportare **quante righe fuse** compaiono tra i top prodotti: è il giudizio sulla ricostruzione che la sola quadratura non poteva dare
- [ ] 7.4 Decidere, con le viste popolate sotto gli occhi, la **finestra temporale predefinita** (tutto lo storico o ultimi 12 mesi) — prima domanda aperta in `design.md`

## 8. Chiusura

- [ ] 8.1 Confermare che la change non ha introdotto rete, dipendenze, permessi, segreti né modifiche allo schema
- [ ] 8.2 Rivedere il `git diff` prima del commit — repository pubblico — escludendo segreti, percorsi personali, immagini di scontrini reali e **dati di spesa personali negli screenshot** delle viste
- [ ] 8.3 Aggiornare `PLAN.md` con le misure di 7.x, comprese quelle che non lusingano
