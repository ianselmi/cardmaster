## 1. Geometria delle righe — aprire `ReceiptTextLayout` senza cambiarne il comportamento

- [x] 1.1 Aggiungere `ReceiptVisualLine` in `Services/Receipts/`: testo della riga, frammenti con il loro rettangolo, rettangolo complessivo della riga
- [x] 1.2 Estrarre in `ReceiptTextLayout` un metodo che restituisce `IReadOnlyList<ReceiptVisualLine>` e reimplementare `ToVisualText`/`ToVisualLines` **sopra di esso**, senza modificarne l'output
- [x] 1.3 Far girare `ReceiptTextLayoutTests` e `ReceiptHeaderParserTests` invariati: sono la rete che dimostra il punto 1.2. Se un test va cambiato, la modifica non è più una riorganizzazione
- [x] 1.4 Esporre da `ReceiptHeaderParser` l'**indice della riga** riconosciuta come totale (oggi `FindTotalCents` restituisce il solo importo), senza cambiare il valore restituito né le regole di riconoscimento

## 2. Ricostruzione delle righe prodotto (logica pura)

- [x] 2.1 Definire `ReceiptItemLine` (descrizione, quantità in millesimi, prezzo unitario in centesimi, importo in centesimi, tipo riga prodotto/sconto, flag di incoerenza) — nessuna dipendenza da MAUI, ML Kit o database
- [x] 2.2 In `ReceiptItemsParser`, calcolare la **soglia della colonna prezzo** sullo scontrino intero dalla distribuzione dei bordi destri degli importi candidati; un numero fuori soglia resta descrizione
- [x] 2.3 Delimitare il corpo: dalla prima riga con importo in colonna dopo l'intestazione fino alla riga del totale (o del primo `SUBTOTALE` che la precede). Se il totale non è individuabile, **nessuna riga proposta**
- [x] 2.4 Regola **quantità esplicita** (`2 X 1,50`, `2 PZ x 1,50`): quantità e prezzo unitario; se lo scontrino riporta anche il totale di riga e non coincide, marcare la riga incoerente invece di correggerla
- [x] 2.5 Regola **peso** (`0,432 kg x 2,99 €/kg`): quantità in millesimi interi, mai in virgola mobile
- [x] 2.6 Regola **sconto/promozione** (importo negativo o riga marcata `SCONTO`/`PROMO`/`OFFERTA`): riga di sconto, non prodotto, con importo negativo che entra nella somma
- [x] 2.7 Regola **continuazione**: riga senza importo in colonna subito sotto una riga prodotto → accodata alla descrizione precedente, nessuna riga a prezzo nullo
- [x] 2.8 Regola **riga di servizio** (reparto, codice, `PEZZI N.`): scartata per parola chiave
- [x] 2.9 Quantità implicita = 1; nessun valore dedotto o inventato quando lo scontrino non lo riporta
- [x] 2.10 Conservare sulla riga l'**unità di misura** (pezzo/peso) e la **descrizione normalizzata** con `TextNormalizer`, accanto a quella grezza: senza la prima `2 pz` e `0,002 kg` sono lo stesso numero, senza la seconda le aggregazioni per prodotto dipendono da una tabella riscrivibile dall'utente

## 3. Aliquota IVA

- [x] 3.1 Leggere il **terzo campo** tra descrizione e prezzo, distinguendo aliquota per esteso (`4,00`) da codice di reparto a una cifra; aliquota conservata in **punti base interi**
- [x] 3.2 Parsare il **riepilogo IVA** a piè di scontrino (fuori dal corpo, nessuna riga prodotto generata): mappa codice → aliquota, imponibile per aliquota, totale imposta
- [x] 3.3 Riga senza aliquota leggibile o con codice non risolvibile: campo **vuoto**, mai dedotto dalla categoria né assunto a 22%
- [x] 3.4 Estrarre il **totale imposta** ("di cui IVA") in `ReceiptHeaderParser`, senza calcolarlo dal totale quando non è stampato

## 4. Quadratura

- [x] 4.1 `ReceiptTotalsCheck`: somma delle righe (sconti compresi) confrontata con il totale di testata, tolleranza **zero centesimi**, esito quadrato / scarto di N centesimi / non validato quando manca il totale
- [x] 4.2 Quadratura **per aliquota** quando il riepilogo è leggibile: imponibile di ciascuna aliquota contro la somma delle righe che la portano, e totale imposta contro quello stampato; l'esito dice **quale** aliquota non torna
- [x] 4.3 Righe rimaste senza aliquota dichiarate come tali, mai attribuite a un'aliquota per far quadrare i conti
- [x] 4.4 Nessuna correzione automatica: niente riga "differenza", niente aggiustamento dell'ultimo prezzo, nessun blocco del salvataggio

## 5. Categorie

- [x] 5.1 Creare `Resources/Raw/categories.json` (parola-chiave → categoria), poche categorie larghe, versionato come `issuers.json`
- [x] 5.2 `ICategoryCatalog` + implementazione che carica il seed dal bundle, sul modello di `IssuerCatalog`; registrare in `MauiProgram.cs`
- [x] 5.3 `CategoryMatcher` come classe pura che riceve il dizionario **come dato**: confronto per token contenuti e prefisso sulla descrizione normalizzata con `TextNormalizer` — nessuna distanza di edit generica
- [x] 5.4 Ordine di consultazione: mappature apprese prima del seed; nessuna corrispondenza → riga **senza categoria**, mai una categoria di ripiego

## 6. Modello dati e schema v3 → v4

- [x] 6.1 `Data/ReceiptItem.cs` da `EntityBase`: `ReceiptId`, descrizione grezza, `NormalizedDescription`, `QuantityMilli`, `Unit` (pezzo/peso), `UnitPriceCents`, `AmountCents`, `VatRateBasisPoints` (nullable), tipo riga, categoria, ordinamento
- [x] 6.2 `Data/ProductMapping.cs` da `EntityBase`: `NormalizedDescription` (chiave), `Category`, `DisplayName` (vuoto in questa change), `Origin` (`Seed`/`User`/`Ai`) — le due colonne in più esistono per `receipt-ai-normalize`, che userà questa tabella come cache
- [x] 6.3 Aggiungere `TaxCents` (nullable) a `Data/Receipt.cs`: unica modifica a una tabella già in uso, senza migrazione da scrivere perché il provider aggiunge la colonna mancante all'apertura
- [x] 6.4 `CreateTableAsync<ReceiptItem>()` e `CreateTableAsync<ProductMapping>()` in `DatabaseService.GetConnectionAsync`; `SchemaVersion` da 3 a 4 con il commento che spiega il perché (guardia `BackupNaming.CanRestore`)
- [x] 6.5 Indice su `ReceiptId` nella tabella delle righe: `receipt-insights` farà `GROUP BY` su tutto lo storico, non su uno scontrino per volta
- [x] 6.6 Estendere `IReceiptRepository`: lettura delle righe di uno scontrino, **sostituzione in blocco** al salvataggio (tombstone delle precedenti + inserimento delle nuove), tombstone delle righe all'eliminazione dello scontrino
- [x] 6.7 Repository delle mappature: lettura per descrizione normalizzata, scrittura che **riscrive** la mappatura esistente invece di accumularne una seconda, e non sovrascrive una mappatura di origine `User` con una automatica
- [x] 6.8 Verificare l'avvio su un database esistente v3: scontrini e carte intatti, tabelle nuove create, colonna `TaxCents` aggiunta, `user_version` a 4
      → Verificato su emulatore (API 36, 14 ago 2026) come **vero aggiornamento**: installata prima la versione al commit `31e9fbe` (l'ultimo prima di questa change), creati con quella una carta e uno scontrino, poi installata sopra la build corrente senza disinstallare. Esito letto dal file del database, non dedotto dall'interfaccia: `user_version` **3 → 4**, tabelle `ReceiptItem` e `ProductMapping` **create**, colonna `TaxCents` **aggiunta**, carta e scontrino **al loro posto**.
      → Il primo tentativo non valeva niente e va detto: il database sull'emulatore era **già a v4** da una sessione precedente, e installarci sopra la vecchia versione non produce un database v3 — un downgrade non riporta indietro lo schema. Serve `adb uninstall` prima, altrimenti si verifica una migrazione che non sta avvenendo.

## 7. Conferma e correzione nell'interfaccia

- [x] 7.1 `ReceiptFormViewModel`/`ReceiptFormPage`: esito della quadratura **in cima**, righe sotto — se il totale torna si conferma senza scorrerle
- [x] 7.2 Correzione di descrizione, quantità, aliquota e importo di una riga, aggiunta di una riga persa, eliminazione di una riga inventata; quadratura ricalcolata a ogni modifica
- [x] 7.3 Correzione della categoria di una riga, che scrive la mappatura appresa e vale dagli scontrini successivi (nessuna riscrittura retroattiva)
- [x] 7.4 Uscita senza confermare: nessuna riga e nessuna mappatura persistita
- [x] 7.5 `ReceiptDetailViewModel`/`ReceiptDetailPage`: righe con descrizione, quantità, **aliquota**, importo e categoria — la stessa tabella descrizione/IVA/prezzo dello scontrino cartaceo — più il segnale di quadratura complessiva e per aliquota; scontrino senza righe consultabile come oggi, senza sezione vuota né errori
- [x] 7.6 Riga senza aliquota mostrata con campo vuoto, distinguibile da un'aliquota letta
- [x] 7.7 Modifica delle righe su uno scontrino **già salvato**, persistita e visibile alla riapertura

## 8. Test

- [x] 8.1 Collegare in `tests/CardMaster.Tests/CardMaster.Tests.csproj` con `<Compile Include>` i soli file puri nuovi (`ReceiptVisualLine`, `ReceiptItemsParser`, `ReceiptItemLine`, `ReceiptVatSummary`, `ReceiptTotalsCheck`, `CategoryMatcher`, `TextNormalizer` se serve)
- [x] 8.2 Test di ricostruzione: colonne separate, numero nella descrizione non scambiato per prezzo, riga senza importo in colonna
- [x] 8.3 Test delle regole: quantità esplicita, totale di riga incoerente, peso in millesimi, sconto negativo, continuazione, riga di servizio scartata
- [x] 8.4 Test di delimitazione: coda dopo il totale esclusa, intestazione esclusa, totale non individuato → nessuna riga
- [x] 8.5 Test dell'aliquota: colonna con aliquota per esteso, colonna con codice risolto dal riepilogo, codice non risolvibile → riga senza aliquota, riepilogo non confuso con i prodotti
- [x] 8.6 Test di quadratura: somma esatta, somma con scarto (di cui uno da virgola spostata, che è il caso pericoloso), totale di testata assente, **errori che si compensano** — passano il totale e devono fallire la quadratura per aliquota
- [x] 8.7 Test del `CategoryMatcher`: corrispondenza per token, per prefisso su abbreviazione, nessuna corrispondenza su parole simili ma diverse, precedenza della mappatura appresa sul seed
- [x] 8.8 Test di non-regressione della testata: gli stessi input dei test esistenti producono gli stessi campi estratti
- [x] 8.9 Test end-to-end sulla logica pura con il testo di uno scontrino MD reale a 29 righe, sconti e tre aliquote: righe, aliquote e totale devono ricostruire la stessa tabella del cartaceo
- [x] 8.10 `dotnet test` verde

## 9. Verifica finale

- [x] 9.1 `dotnet build` con 0 errori (criterio di accettazione, non opzionale)
- [~] 9.2 Verifica su emulatore con gli **stessi scontrini reali** di 3 catene già usati in `receipt-capture`, riportando **quante righe su quante** sono state ricostruite correttamente e **quante aliquote su quante** — numeri, non un giudizio qualitativo — **misurato su 1 scontrino (MD, 29 righe, 47,74 €), fotografato storto**: 21 righe ricostruite su 29, somma 36,38 € contro 47,74 €, 19 aliquote lette su 21 righe. Dieci righe sono corrette dall'inizio alla fine (descrizione + aliquota + importo + categoria); le altre otto **appaiano due prodotti** in una riga sola e ne perdono il secondo importo. Trovati e corretti per strada tre difetti che i test sintetici non potevano mostrare: (1) **concatenazione** delle bande verticali su foto storta — sette prodotti in una riga, ora limitata dall'altezza massima di riga e dalla stima dell'inclinazione; (2) `Subtot` **abbreviato** non riconosciuto come fine del corpo, che faceva entrare il subtotale tra i prodotti; (3) il totale letto come **5,34** (l'IVA) perché `di cui IVA` finiva sulla stessa riga visiva del totale. Restano da verificare le altre 2 catene
      → **Rimisurato dopo 9.2b** (14 ago 2026, stessa fotografia): 24 righe su 29, somma 41,85 € e totale letto correttamente. Le altre 2 catene **restano non misurate**: quelle fotografie non sono sul device e non sono ricostruibili — servono gli scontrini veri. È l'unico punto della verifica finale che non si può chiudere senza materiale che ha solo l'utente.
- [x] 9.2b Ridurre l'appaiamento residuo di due prodotti adiacenti su foto storta: la stima globale dell'inclinazione non basta quando lo scontrino è anche **incurvato** (la pendenza cambia lungo lo scontrino). Prossimo passo naturale: stimare la pendenza **a fasce** invece che una sola per l'intero scontrino
      → Fatto in `ReceiptTextLayout`: dopo la pendenza generale, una pendenza **residua per fascia** (fasce a pari numero di frammenti, non a pari altezza — testata e riepilogo IVA sono molto più radi del corpo), **interpolata** fra i centri delle fasce ed **estrapolata** oltre la prima e l'ultima. L'interpolazione non è un raffinamento: a gradini, due frammenti della stessa riga a cavallo di un confine riceverebbero correzioni diverse e la riga si spezzerebbe dove il rimedio doveva ricomporla. L'estrapolazione serve perché ogni fascia parla per la propria quota centrale, e la curvatura si accumula proprio agli estremi.
      → **Misura, sullo scontrino MD sintetico a 29 righe deformato a tavolino** (la deformazione è un parametro del test, così il difetto è riproducibile senza rifotografare): prima la ricostruzione era completa fino a una curvatura di **0,06** e cedeva a 0,09 (18 righe su 29 con inclinazione 0,08); ora è completa e quadra a 47,74 € **per ogni curvatura provata fino a 0,18**, a tutte le inclinazioni fino a 0,08 (~4,5°). Nessun caso peggiorato rispetto a prima, nemmeno fuori dal campo di validità.
      → **Due tentativi falliti, tenuti come commento nel codice perché sembrano entrambi la mossa giusta:** (1) *allentare* la tolleranza verticale sulle coppie da cui si stima la pendenza di fascia — su una riga inclinata verso il basso fa entrare l'importo della riga **successiva**, che ha il segno opposto e trascina la mediana a zero: le fasce stimavano `-0,012`/`+0,006` dove il vero era `-0,045`/`+0,045`; (2) *iterare anche la stima generale* come si fa con quella di fascia — su uno scontrino incurvato converge verso l'inclinazione di una metà e peggiora l'altra, e il caso incurvato tornava a spezzarsi.
      → Resta fuori portata l'inclinazione oltre ~8,5°, dove però cede già la lettura del **totale**: lo scontrino è comunque da rifotografare o da rileggere con `receipt-ai-scan`. Non è un limite introdotto qui — c'era prima, e in quella zona la ricostruzione è comunque migliorata.
      → Tre test nuovi, tutti sulla logica pura: foto storta a pendenza costante (rete che tiene ferma la correzione preesistente), scontrino incurvato end-to-end nel caso più deformato che regge, e il caso stretto sul solo layout in `ReceiptTextLayoutTests`. `dotnet test` **95 verdi**, `dotnet build` 0 errori. Trappole in `docs/technical-notes.md`.
      → **Riverificato sulla foto reale** (emulatore API 36, 14 ago 2026, stessa fotografia di 9.2): da **21 righe su 29 a 24**, somma da **36,38 € a 41,85 €** su 47,74 €, e il **totale ora è letto giusto** (47,74 invece di 41,14). Restano 5 righe che ne inghiottono due. Il guadagno reale è quindi minore di quello sintetico, e il motivo si vede nel dato: in alcune di quelle righe **è ML Kit a restituire un frammento unico** con due descrizioni già fuse (`*AFFET VEGETALE GUSTO COTTO MINIBURGER VEGETAL CAROTE/CECI…`), e nessuna correzione geometrica può separare ciò che arriva già attaccato. Quello è il caso che `receipt-ai-scan` risolve mandando l'immagine, non il testo.
- [x] 9.3 Provare il percorso di correzione a mano su uno scontrino ricostruito male: deve restare salvabile e corretto per intero
      → Verificato su emulatore (14 ago 2026) proprio sullo scontrino MD reale mal ricostruito: **eliminata** la riga spuria (`FABIO R.`, il nome del cassiere letto come prodotto), **corretta** la descrizione di una riga che ne conteneva due, **aggiunta** la riga persa (`SGRASSATORE OXY MOUSSE`, 1,69 €, IVA 22%). La quadratura si ricalcola **a ogni modifica** senza salvare: 41,85 → 39,85 alla cancellazione, 41,54 dopo l'aggiunta. Salvato e riaperto: tutte e tre le correzioni sono al loro posto.
      → Verificata dal vivo anche la **sostituzione in blocco** del task 6.6: dopo il salvataggio il database ha **24 righe attive e 24 con tombstone** — le precedenti sono marcate cancellate, non eliminate fisicamente.
      → *Trappola della verifica, non dell'app:* `adb shell input text` non passa gli spazi (servono `%s`) e **scarta la virgola decimale**, per cui `1,69` era entrato come `169,00 €`. Il campo accetta correttamente il punto. Da tenere presente alla prossima verifica guidata da `adb`, altrimenti si scambia un errore di digitazione per un difetto dell'app.
- [x] 9.4 Verifica in **modalità aereo** dell'intero giro: ricostruzione, classificazione, correzione, salvataggio, dettaglio
      → Verificato su emulatore (14 ago 2026) con `airplane_mode_on=1`: acquisizione da immagine, OCR, ricostruzione righe, aliquote, categorie, quadratura, salvataggio, storico e dettaglio **identici a rete accesa**, stesso risultato al centesimo (41,85 € su 47,74 €). Nessun messaggio di errore, nessuna attesa: non c'è nulla che aspetti la rete perché non c'è nulla che la usi.
- [x] 9.5 Verificare che testata, storico, totali per mese/esercente e le carte fedeltà siano rimasti invariati
      → Verificato **nel modo che conta**, cioè aggiornando la versione precedente invece di installare pulito (vedi 6.8): la carta creata con la v3 è intatta e con lo stesso colore, lo scontrino salvato dalla v3 — che non ha righe — resta consultabile senza sezione vuota né errori. Totali per mese e per esercente corretti man mano che gli scontrini crescevano: 6,00 € → 53,74 € (47,74 + 6,00) → 101,48 €, con l'esercente MD aggregato a 95,48 € su due scontrini.
      → *Falso allarme chiarito:* un importo appariva formattato `1.09 €` col punto mentre gli altri usavano la virgola. Ingrandito lo screenshot e spostata la riga più in alto, la virgola c'è: era la sua **coda tagliata dal bordo inferiore** dello schermo. Formattazione euro coerente su tutte le righe.
- [x] 9.6 Confermare che la change non ha introdotto alcuna chiamata di rete, alcuna dipendenza nuova, alcun permesso nuovo, né alcuna chiave o credenziale nel codice o nel pacchetto
      → Verificato sul commit della change (`3565ad3`), non a memoria: **`CardMaster.csproj` non compare nel diff** (nessun `PackageReference` nuovo) e **nemmeno `AndroidManifest.xml`** (nessun permesso nuovo). Permessi dell'APK installato: gli 8 di sempre più `RECEIVE_BOOT_COMPLETED`/`WAKE_LOCK` di WorkManager, tutti preesistenti; **nessun permesso di storage**, coerente con l'uso di `FilePicker`/SAF. L'unico riferimento di rete in un file toccato è l'`HttpClient` singleton in `MauiProgram.cs`, che c'era già per backup Drive e auto-update: le righe aggiunte dalla change sono le due registrazioni di `ICategoryCatalog` e `IProductMappingRepository`. Nessuna chiave né credenziale.
- [x] 9.7 Rivedere il `git diff` prima del commit per escludere segreti, percorsi personali e immagini di scontrini reali usate nelle prove
      → Riletto a ogni commit. Nessuna chiave, nessun percorso personale, nessuna immagine di scontrino: le fotografie usate nelle prove sono rimaste **solo sull'emulatore**, e i casi finiti nei test sono ricostruiti da testo, non da immagini.
- [x] 9.8 Alla sincronizzazione delle spec, aggiornare anche il **Purpose** di `openspec/specs/receipt-scan/spec.md`: non dichiara più di lasciare fuori righe e categorie
      → Sincronizzate le spec: due capability nuove (`receipt-items`, `receipt-categories`) e quattro requirement modificati in `receipt-scan`. Il Purpose diceva *«Non copre le righe dei prodotti, le categorie né alcuna normalizzazione»*, che era diventato falso per i primi due: ora nomina la ricostruzione delle righe visive e l'imposta fra ciò che copre, **rimanda** righe prodotto e categorie alle due capability nuove, e tiene fuori la sola normalizzazione dei nomi. `openspec validate --all`: 23 su 23.
- [x] 9.9 Con i risultati di 9.2 sotto gli occhi, decidere le due domande lasciate aperte in `design.md`: se offrire la ri-estrazione delle righe dal `RawText` di uno scontrino già salvato, e se la granularità del seed di categorie va rivista
      → **Ri-estrazione dal `RawText`: no**, e il motivo non è che la ricostruzione funziona male ma che **il dato conservato non lo permette**. Letto il `RawText` di uno scontrino vero dal database: è **già il testo ricostruito in righe**, con le colonne appiattite e la geometria buttata via. `ReceiptItemsParser` vive sui bounding box, quindi non potrebbe nemmeno girarci sopra, ed erediterebbe le righe già fuse. Darebbe lo stesso risultato della prima lettura, mai migliore. Il prerequisito sarebbe conservare la geometria, non aggiungere un comando; chi ha uno scontrino letto male ha già la correzione a mano e `receipt-ai-scan`, che riparte dall'immagine e quindi recupera informazione davvero.
      → **Granularità del seed: confermata larga, 11 categorie, nessuna revisione.** 19 righe su 24 categorizzate sullo scontrino reale, con le assegnazioni giuste nette. I due errori osservati non vengono dalla granularità: `BURGER VEGETALI CON MELANZANE` → Ortofrutta è il match per token che pesca *melanzane*, e `TOFU BIO SPRAY VETRI ECOLOGICO` → Cura della casa è una **riga fusa** classificata sul secondo prodotto. Spaccare le categorie non correggerebbe né l'uno né l'altro.
      → Entrambe le risposte, con la loro motivazione, sono in `design.md`.
