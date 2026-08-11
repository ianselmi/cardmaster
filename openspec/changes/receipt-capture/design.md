## Context

CardMaster oggi è un'app di carte fedeltà: SQLite locale in chiaro, ML Kit per la scansione barcode, ZXing+SkiaSharp per il rendering, un'unica sezione di navigazione, e come sola funzione di rete il backup Drive opt-in. Questa change apre un dominio nuovo — gli scontrini — e lo fa partendo dal pezzo su cui il progetto può fallire: la comprensione dell'immagine.

Vincoli che non si discutono e che hanno guidato ogni decisione qui sotto:

- **Offline-first.** Le carte funzionano senza rete; gli scontrini devono comportarsi allo stesso modo. L'unica rete della v1 resta il backup Drive.
- **Repository pubblico e APK distribuito fuori dal Play Store.** Nessun segreto nel codice, nessun segreto estraibile dal pacchetto. Questa change non introduce rete, quindi non introduce credenziali: è il momento buono per fissare il principio prima che serva davvero (normalizzazione IA, change successiva).
- **Id client-generati e tombstone su tutto**, per non pagare una migrazione dolorosa quando arriverà la sync v2.
- **`dotnet build` a 0 errori** come criterio di accettazione.

Una nota di realtà: questa è una feature grande, di fatto un'app dentro l'app. La strategia è dividerla in quattro passi indipendentemente verificabili — acquisizione e testata (questa change), righe prodotto, normalizzazione dei nomi, analisi — e non iniziare il passo successivo finché il precedente non ha retto su scontrini veri.

## Goals / Non-Goals

**Goals:**

- Acquisire uno scontrino da foto o immagine e riconoscerne il testo **interamente sul device**, al primo avvio e senza rete.
- Estrarre esercente, partita IVA, data e totale con regole deterministiche, e rendere la correzione manuale un percorso di prima classe, non un ripiego.
- Conservare il **testo riconosciuto integrale**, così che migliorare le regole di estrazione non richieda di rifotografare nulla.
- Consegnare un valore autonomo anche se ci si fermasse qui: storico consultabile e spesa per negozio/mese.
- Non toccare in alcun modo il comportamento delle carte fedeltà.

**Non-Goals:**

- Righe dei prodotti, quantità, sconti, categorie: change `receipt-items`.
- Qualunque chiamata a un modello linguistico e qualunque gestione di chiavi API: change `receipt-ai-normalize`.
- Rilevamento bordi, deskew, correzione prospettica, gestione dei riflessi. Si chiede all'utente una foto dritta e leggibile; se la qualità di cattura si rivelasse il collo di bottiglia, sarà una change a sé con la sua motivazione misurata.
- Scontrini non italiani, valute diverse dall'euro, fatture.

## Decisions

### Riconoscimento testo: ML Kit Text Recognition, variante con modello incluso

Si usa il binding Microsoft `Xamarin.Google.MLKit.TextRecognition` (MIT), **non** `Xamarin.GooglePlayServices.MLKit.Text.Recognition`.

La differenza è dove sta il modello: la variante *bundled* lo mette nell'APK, quella *Play Services* lo scarica al primo utilizzo. La seconda produce un APK più leggero, ma introduce un primo avvio che **richiede rete** e dipende dai Play Services — inaccettabile per un'app che si dichiara offline-first e che viene distribuita fuori dal Play Store, dove non si può dare per scontato nulla dell'ambiente Google del device. Si paga in dimensione dell'APK, che va misurata e riportata perché incide sull'auto-update (scarica il pacchetto intero).

Alternative scartate:
- **Scanbot / Microblink e simili**: risolvono anche cattura e struttura, on-device, ma sono prodotti B2B con licenze enterprise. Fuori scala per un progetto personale.
- **Tesseract**: qualità nettamente inferiore su testo fotografato, e nessun bounding box utile quanto quello di ML Kit.
- **OCR via servizio cloud**: contraddice il vincolo offline e manderebbe fuori dal device l'immagine di uno scontrino, che è un dato personale dettagliato.

### L'OCR restituisce testo **e** geometria, già da ora

L'interfaccia di riconoscimento espone blocchi/righe con il loro rettangolo, non una stringa piatta — anche se questa change usa solo il testo.

Il motivo è la change successiva: ricostruire le righe prodotto significa separare la colonna descrizione da quella prezzo usando le coordinate `x` sulla stessa banda `y`. Se il contratto nasce piatto, la change `receipt-items` deve riscriverlo e ri-verificarlo; se nasce con la geometria, si estende senza rompere nulla. Costo oggi: qualche campo in più in un record di ritorno.

### Il binding Android sta dietro un'interfaccia

`IReceiptOcr` nel progetto condiviso, implementazione in `Platforms/Android/Services/`, registrazione in `MauiProgram`. È lo stesso schema già usato per `IBackupNotifier` / `IBackupScheduler`, che hanno anche un'implementazione `Noop` di default.

Non è astrazione preventiva per un iOS che non arriverà: serve a poter testare parser e allineamento su liste di rettangoli sintetiche, senza emulatore e senza fotografie. È la parte che nella change 2 diventerà l'euristica centrale.

### Le righe si ricostruiscono dalla geometria, non si leggono dal testo

*(Decisione presa in corso d'opera, 11 ago 2026, dopo la prova su OCR reale.)*

ML Kit **non** restituisce lo scontrino riga per riga: raggruppa il testo in blocchi, e su uno
scontrino a colonne questo significa prima tutte le descrizioni e poi tutti i prezzi. Nel testo
grezzo di una prova reale `TOTALE COMPLESSIVO` e `6,61` finiscono a quindici righe di distanza:
nessuna regola basata sull'ordine del testo può riaccoppiarli, e infatti il totale non veniva
riconosciuto pur essendo perfettamente leggibile.

`ReceiptTextLayout` rimette insieme le righe raggruppando i frammenti per **banda verticale** e
ordinandoli per `x`. Era l'algoritmo previsto per le righe prodotto (`receipt-items`): arriva qui
perché senza di esso **non è estraibile nemmeno la testata**. È anche la ragione per cui il
contratto di `IReceiptOcr` espone la geometria fin da subito — la previsione ha pagato prima del
previsto.

Conseguenza: in `RawText` si conserva il testo **ricostruito**, non quello grezzo. È più leggibile
per l'utente nel dettaglio, ed è l'unico che resti ri-parsabile quando le regole miglioreranno.

### Estrazione della testata con regole, non con un modello

Totale, data, partita IVA ed esercente si ricavano con espressioni regolari e parole chiave. Non serve intelligenza: la testata è la parte **regolare** dello scontrino, quella dove le regole vincono. L'intelligenza servirà sulle descrizioni dei prodotti, che sono la parte irregolare — ed è per questo che le due cose stanno in change diverse.

Scelte concrete:
- **Totale**: si cerca la parola chiave (`TOTALE`, `TOT. EURO`, `TOTALE COMPLESSIVO`, `IMPORTO PAGATO`) e si prende l'importo sulla stessa riga o sulla riga sotto. Formato italiano: virgola decimale, punto opzionale per le migliaia.
- **Partita IVA**: undici cifre. Vale come chiave stabile del negozio molto più del nome, che cambia grafia tra un'insegna e l'altra.
- **Esercente**: blocchi di testo in cima allo scontrino, scartando le righe che sono solo indirizzo o numeri.
- **Data**: separatori `/ - .`, anno a 2 o 4 cifre, scarto delle date implausibili — futuro o troppo remote. Una data sbagliata è peggio di una data mancante: falsa i totali mensili in silenzio, mentre un campo vuoto si vede.

**Nessun campo viene inventato.** Se non si riconosce, resta vuoto e segnalato: è il presupposto perché la correzione manuale sia credibile.

### Il testo riconosciuto si conserva

`RawText` sullo scontrino non è ridondanza: è la possibilità di **ri-estrarre** la testata quando le regole miglioreranno — e miglioreranno, perché è l'unica strada per adattarle ai formati che si incontrano davvero — senza chiedere all'utente di rifotografare scontrini che non ha più. Costa qualche KB per scontrino.

### Importi in centesimi, interi

Mai virgola mobile per il denaro. Somme e confronti devono essere esatti al centesimo, e la vista di spesa mensile è la prima cosa che renderebbe visibile un errore di arrotondamento.

### Immagini su filesystem, non nel database

Le immagini vanno in una sottocartella dell'area dati privata dell'app; nel database resta il percorso relativo. Un BLOB per immagine gonfierebbe il file `.db3` e, con esso, ogni snapshot di backup caricato su Drive — dove la ritenzione è di 3 copie e la quota è quella dell'utente.

**La conseguenza va detta, non nascosta:** il backup Drive fa `VACUUM INTO` del solo database, quindi le immagini **non sono nel backup** e un ripristino non le riporta. Si sceglie di accettarlo — mettere gli scontrini nel backup significherebbe moltiplicare per tre la quota Drive consumata — e di dichiararlo nella pagina Backup, con la delta spec su `cloud-backup` che rende il limite un requisito verificabile invece di una sorpresa. Restano nel backup i dati che contano davvero: testata e testo riconosciuto.

Alternative scartate:
- **Immagini nel backup**: costo di quota e di banda sproporzionato rispetto al valore (l'immagine serve a rileggere un dubbio, non è il dato).
- **Non conservare affatto le immagini**: toglie all'utente la possibilità di verificare una cifra dubbia. Meglio conservarle con un'opzione per non farlo.

### Sezione di primo livello, non una voce nascosta

Gli scontrini diventano una seconda `ShellContent` accanto a "Le mie carte", **dentro un
`TabBar`**. Il `TabBar` non è decorativo: con due `ShellContent` nudi Shell costruisce un
**menu a panino** (flyout), verificato su emulatore l'11 ago 2026 — cioè nasconde la sezione
dietro un gesto, che è esattamente ciò che questa scelta voleva evitare. Una feature di questa dimensione dietro un'icona in toolbar sarebbe invisibile; e la barra di navigazione inferiore è il posto dove un utente Android si aspetta di trovare due funzioni pari-grado.

Costo noto: compare la bottom navigation di Shell, che l'app finora non aveva. `docs/technical-notes.md` registra un crash a `createNavigationBar` con traccia fuorviante su `Theme.MaterialComponents`, la cui causa reale era stato di build incrementale stale. **Se compare, prima si prova una build pulita**, poi si indaga.

### Nessuna rete, nessun segreto

Questa change non apre connessioni. Il principio da fissare qui, perché varrà per tutte le change successive del dominio: **nessuna chiave, token o credenziale nel codice sorgente o nel pacchetto**, né in chiaro né offuscata. Il repository è pubblico e l'APK è scaricabile da chiunque: un segreto incorporato è un segreto compromesso, e offuscarlo sposta solo di qualche minuto il momento in cui viene estratto.

Quando servirà una chiave (normalizzazione IA), sarà **la chiave dell'utente**, incollata da lui, conservata in `SecureStorage` come il refresh token Google, mai in `Preferences` e mai nel database — quindi nemmeno nel backup Drive.

## Risks / Trade-offs

**Conflitto di dipendenze Android tra ML Kit text e ML Kit barcode** → è il rischio numero uno di questa change, e si manifesta in build, non a runtime. `BarcodeScanning.Native.Maui` porta già dentro ML Kit e AndroidX. Primo passo dell'implementazione: aggiungere il pacchetto e compilare, prima di scrivere qualunque codice applicativo. Se la convivenza non regge, si valuta la variante Play Services (rinunciando al primo avvio offline) prima di rinunciare alla feature.

**Crescita dell'APK e quindi del download di aggiornamento** → misurare la dimensione prima e dopo e riportarla nel riepilogo della change. Se l'aumento fosse sproporzionato, la variante Play Services torna sul tavolo come compromesso esplicito.

**Le regole di estrazione funzionano sui formati provati e non su altri** → è previsto, non un difetto: `RawText` conservato permette di ri-estrarre a regole migliorate, e la correzione manuale garantisce che uno scontrino sia sempre salvabile e corretto anche quando il riconoscimento sbaglia tutto. La verifica su emulatore deve usare scontrini reali di almeno 2-3 catene diverse, non un caso felice.

**Foto storte, sfocate o con riflessi** → nessun deskew in questa change. L'app deve dire chiaramente quando non riconosce nulla, invece di salvare uno scontrino vuoto. Se questo diventa il motivo principale di fallimento, il rilevamento bordi diventa una change con una motivazione misurata alle spalle.

**Le immagini degli scontrini crescono senza limite** → lo spazio occupato è visibile e liberabile, e la conservazione è disattivabile. Nessuna cancellazione automatica: eliminare dati dell'utente a sua insaputa è peggio del problema che risolve.

**Uno storico di scontrini è un dato personale dettagliato** → resta interamente sul device, e nel backup Drive dell'utente sul suo account. Nessun terzo, nessun servizio, nessuna telemetria. È esattamente la ragione per cui l'OCR è on-device e non cloud.

**La bottom navigation cambia l'aspetto dell'app anche per chi gli scontrini non li userà mai** → è il prezzo di rendere la feature trovabile. Verifica su emulatore in tema chiaro e scuro, controllando che la griglia carte e il FAB "+" introdotto da `maui-card-list-add-fab` non finiscano sotto la barra.

## Migration Plan

Nessuna migrazione dati: si aggiunge una tabella, non se ne modifica nessuna. `CreateTableAsync` la crea al primo avvio della versione nuova, sulle installazioni esistenti come su quelle nuove.

La **versione di schema del database** va incrementata (v2 → v3). Non serve alla creazione della tabella: serve alla guardia del ripristino Drive, perché un backup prodotto da questa versione non venga ripristinato da una versione più vecchia dell'app che non conosce gli scontrini. È lo stesso motivo per cui è stata incrementata in `maui-card-color-labels`.

Rollback: reinstallando una versione precedente le carte restano intatte e gli scontrini semplicemente non sono più visibili, perché nessuna tabella esistente viene modificata.

## Open Questions

- ~~**Quanto pesa davvero il modello di riconoscimento nell'APK?**~~ **Risolta (11 ago 2026).** Misurato su build Release: **48,9 MB → 58,7 MB, +9,8 MB (+20%)**. Decisione: **si tiene il modello incluso**, accettando che l'auto-update riscarichi ~59 MB a ogni versione, in cambio del riconoscimento funzionante al primo avvio senza rete e senza dipendere dai Play Services del device. La dimensione complessiva dell'APK (già 49 MB prima di questa change) resta un tema aperto a sé, da affrontare eventualmente con split per ABI o trimming in una change dedicata — non qui.
- **L'esercente va normalizzato per raggruppare le insegne?** Lo stesso negozio può comparire come `ESSELUNGA S.P.A.` e `Esselunga Spa`. La partita IVA risolve il caso quando c'è. Si rimanda alla vista di spesa: se il raggruppamento risulta sporco all'uso, si affronta con dati reali sotto gli occhi.
- **Serve poter ri-estrarre la testata su richiesta?** Il `RawText` conservato lo rende possibile, ma un comando "rileggi i dati" in dettaglio ha senso solo quando le regole saranno cambiate almeno una volta. Non in questa change.
