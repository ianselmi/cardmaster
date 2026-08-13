## 1. La dipendenza, prima di tutto il resto

- [x] 1.1 Aggiungere il pacchetto NuGet `Anthropic` a `CardMaster.csproj` e verificare che `dotnet build` completi con 0 errori per `net10.0-android` — nessun altro codice: è qui che la change può fermarsi, come fu per ML Kit in `receipt-capture`
      → `Anthropic` 12.40.0. Debug e Release compilano con **0 errori**, e il conteggio dei warning resta a 109, identico a prima: il pacchetto non ne introduce. Verificato **referenziando davvero i tipi** (`AnthropicClient`, `MessageCreateParams`, `Role`) con una sonda temporanea poi rimossa — un build che non tocca l'assembly non prova che sopravviva al linker. Nessun warning di trimming (IL2xxx) in Release.
- [x] 1.2 ~~Se non è compatibile con il target Android: ripiegare su chiamata HTTP diretta~~ — **non si applica**: l'SDK è compatibile, nessuna rinuncia da registrare. Il ripiego HTTP resta l'alternativa se emergessero problemi a runtime.
- [x] 1.3 Misurare la dimensione dell'APK Release prima e dopo, e annotare la differenza (la riscarica l'auto-update a ogni versione)
      → **59,18 MB → 63,14 MB: +3,96 MB** (+6,7%) sull'APK non firmato; identica differenza sul firmato (59,33 → 63,29 MB). Misura presa con i tipi dell'SDK effettivamente referenziati: senza codice che lo usi il linker lo eliminerebbe e la differenza sarebbe zero. È il costo che l'auto-update riscarica a ogni versione.
- [x] 1.4 Verificare che il manifest unito non guadagni permessi oltre a `INTERNET`, già presente
      → Manifest unito: 8 permessi, **esattamente gli 8 dichiarati nel sorgente** (`INTERNET`, `ACCESS_NETWORK_STATE`, `CAMERA`, `VIBRATE`, `POST_NOTIFICATIONS`, `FOREGROUND_SERVICE`, `FOREGROUND_SERVICE_DATA_SYNC`, `REQUEST_INSTALL_PACKAGES`). L'SDK non ne aggiunge nessuno.

## 2. Credenziale dell'utente

- [x] 2.1 `IAiCredentialStore` + implementazione su `SecureStorage`: lettura, scrittura, rimozione, "è configurata?" senza esporre il valore
      → `Services/Ai/IAiCredentialStore.cs` + `AiCredentialStore.cs`, sul modello del `refresh_token` di `GoogleAuth`. `IsConfigured` è **sincrono** per l'interfaccia e legge un solo indicatore booleano in `Preferences`: nelle preferenze non finisce né il valore né un suo frammento. L'indicatore può disallinearsi (Keystore invalidato dopo un ripristino), quindi `GetKeyAsync` lo riallinea quando scopre che la chiave non c'è più — altrimenti l'interfaccia mentirebbe.
- [x] 2.2 Escludere esplicitamente la chiave dal backup Drive e dal database — verificarlo, non assumerlo
      → Verificato leggendo `BackupService`: il backup carica **unicamente uno snapshot del file SQLite** (`_database.SnapshotAsync` → upload dei byte), non tocca `Preferences` né `SecureStorage`. La chiave vive in `SecureStorage` e non viene mai scritta nel database: l'esclusione è **strutturale**, non una regola da ricordare.
- [x] 2.3 Verifica della chiave con una richiesta minima, distinguendo chiave rifiutata da problema di rete
      → `IAiKeyVerifier` / `AnthropicKeyVerifier`: elenca i modelli (`client.Models.List()`), la richiesta autenticata più piccola dell'API e **senza costo in token** — provare una chiave non costa nulla. La distinzione è nel tipo restituito: `AiErrorKind.KeyRejected` (401/403) contro `AiErrorKind.Network`, perché con la rete assente non sappiamo se la chiave sia buona e dichiararla non valida sarebbe una bugia.
- [x] 2.4 Rimozione della chiave che disattiva la funzione senza toccare i dati salvati
      → `RemoveKeyAsync` cancella la sola voce di `SecureStorage` e spegne l'indicatore; non tocca database, scontrini né immagini. Se l'archivio protetto non risponde, l'indicatore viene spento comunque: la funzione deve risultare disattivata, che è ciò che l'utente ha chiesto. Il collegamento all'interruttore nelle impostazioni arriva in 5.1.
- [x] 2.5 Verificare che la chiave non compaia in log, messaggi d'errore o interfaccia dopo l'inserimento
      → Audit sull'intero sorgente: il valore compare in **4 punti**, tutti necessari — scrittura e lettura in `SecureStorage`, e assegnazione a `AnthropicClient.ApiKey` nel verificatore e nel reader. Nessuna chiamata di log lo tocca, nessuna proprietà lo espone, e **tutti i messaggi mostrati sono stringhe letterali** costruite dall'enum `AiErrorKind`, mai dal testo dell'eccezione. `AiErrorMapper` legge il messaggio dell'eccezione solo per *classificare* il credito esaurito, e non lo mostra mai.

## 3. Client del modello

- [x] 3.1 `IReceiptAiReader` in `Services/Receipts/`: prende un'immagine e restituisce testata e righe, oppure una **causa d'errore riconoscibile**
      → `IReceiptAiReader` + `AnthropicReceiptAiReader`. Restituisce `ReceiptAiResult`, che è **o** una lettura **o** un `AiErrorKind`: mai entrambi, mai nessuno dei due. La chiave si rilegge da `SecureStorage` a ogni chiamata invece di tenerla in un client di lunga vita — se l'utente la rimuove, la chiamata dopo deve trovare che non c'è più.
- [x] 3.2 Schema JSON della risposta con le unità del dominio — centesimi, millesimi, punti base — imposto via `output_config.format`, non chiesto nel prompt
      → `ReceiptAiSchema` in `ReceiptAiJson.cs`, passato come `OutputConfig.Format`. Campi facoltativi scritti con `anyOf`+`null` (costrutto dichiarato supportato dagli output strutturati) invece di un elenco di tipi. `additionalProperties: false` e `required` completo su ogni oggetto, come richiesto dagli output strutturati.
- [x] 3.3 Prompt: scontrino italiano, una riga per prodotto, sconti negativi, aliquota per riga, campi non leggibili lasciati vuoti e mai inventati
      → `ReceiptAiPrompt`. Descrive **cosa** leggere e le convenzioni dello scontrino italiano (prezzi IVA inclusa, codice di reparto → aliquota dal riepilogo, righe a peso); il **formato** non è chiesto qui, lo impone lo schema. Chiude sul punto che conta: un dato inventato è indistinguibile da uno letto, un campo vuoto si corregge a mano mentre uno sbagliato passa inosservato.
- [x] 3.4 Modello selezionabile con `claude-opus-5` come default; nessun altro identificativo di modello inventato o costruito a mano
      → `ReceiptAiModels.All`: `claude-opus-5` (default), `claude-sonnet-5`, `claude-haiku-4-5`, presi dal listino ufficiale senza suffissi di data. `Resolve` ricade sul default se una preferenza salvata indica un modello non più noto, così una vecchia impostazione non blocca la funzione.
- [x] 3.5 Ridimensionamento dell'immagine prima dell'invio, alla risoluzione minima che tiene lo scontrino leggibile
      → `ReceiptAiImage.Downscale` (SkiaSharp, già in progetto per i barcode): lato lungo a **1568 px**, JPEG qualità 85. Non è solo risparmio: è l'unico punto in cui si riduce il dato che esce dal device senza perdere la funzione.
- [x] 3.6 Categorie d'errore: chiave assente, chiave rifiutata, credito esaurito, troppe richieste, rete assente, timeout, risposta non conforme — ciascuna con un messaggio che dice cosa fare
      → `AiErrorKind` + `AiErrorMapper`, condiviso tra verifica della chiave e rilettura: due tabelle separate sarebbero divergute. L'annullamento chiesto dall'utente **non** diventa un errore, si propaga. Il credito esaurito arriva come 400 senza tipo dedicato e si riconosce dal messaggio: euristica dichiarata nel codice, e se non la riconosciamo resta "errore di servizio" — mai "chiave non valida", che manderebbe l'utente a correggere la cosa sbagliata. I testi per l'utente arrivano in 5.x con l'interfaccia.
- [x] 3.7 Leggere dal risultato il **consumo effettivo di token** e riportarlo, invece di lasciare solo la stima
      → `ReceiptAiUsage` legge `response.Usage` e il costo si calcola dal listino in `ReceiptAiModelOption`, in millesimi di centesimo interi. Stima mostrata prima e costo effettivo mostrato dopo vengono dalla **stessa** fonte di prezzi, altrimenti divergono alla prima variazione di listino.

## 4. Innesto nel flusso esistente

- [x] 4.1 La rilettura si propone **solo** quando la quadratura fallisce o non ci sono righe, e solo con funzione attiva e chiave presente
      → `ReceiptFormViewModel.CanRescanWithAi`: tutte le condizioni insieme — funzione accesa, chiave presente, un'immagine da inviare, quadratura fallita (o nessuna riga, che è l'altro caso in cui l'app sa di non aver letto). Ricalcolata a ogni `UpdateBalance`, così **la proposta sparisce da sola** appena una correzione a mano fa tornare i conti.
      → **Confermato su emulatore**: sullo scontrino MD che non torna il pulsante compare subito sotto il messaggio di quadratura fallita; spenta la funzione, sparisce.
- [x] 4.2 Nessuna chiamata, nessun invio e nessuna proposta quando lo scontrino quadra
      → Il pulsante è legato a `CanRescanWithAi` e non compare. `RescanWithAiAsync` ricontrolla comunque la condizione in testa: rete di sicurezza contro una chiamata partita da uno stato nel frattempo cambiato.
- [x] 4.3 Consenso informato prima del primo invio: che cosa esce, verso chi, a spese di chi
      → Doppio: all'accensione dell'interruttore nelle impostazioni (e se l'utente rifiuta, **l'interruttore torna indietro**) e prima di ogni invio nel form, con il costo indicativo del modello scelto. Un "Annulla" non invia niente e non cambia niente.
      → **Confermato su emulatore**: rifiutando il consenso all'accensione l'interruttore è tornato spento e lo stato è rimasto «spenta». Il consenso prima dell'invio dichiara «Costo indicativo: 4 ¢ con Claude Opus 5».
- [x] 4.4 Le righe rilette passano per la **stessa** `ReceiptTotalsCheck` e finiscono nella stessa schermata di conferma, correggibili come le altre
      → `ApplyAiReadingAsync` chiama la stessa `ReceiptTotalsCheck.Verify`, e le righe adottate ripassano dalla **stessa classificazione in categorie** delle locali. **Una scelta che merita revisione:** le righe del modello si verificano contro il totale che *il modello* ha letto, non contro quello nel form — perché quando l'OCR sbaglia il totale (41,14 contro 47,74) confrontarle con quel totale le boccerebbe pur essendo giuste. Di conseguenza, se si adottano le righe si adotta anche il totale da cui sono state giudicate.
- [x] 4.5 Si propongono le righe del modello quando quadrano e quelle locali no; se non quadra nessuno dei due, dirlo
      → `ReceiptAiComparison` (logica pura, testata in 6.4) decide; il ViewModel traduce in messaggi. I campi di testata si completano **solo dove erano vuoti**: quello che l'utente ha già scritto a mano vale più di quello che il modello ha letto.
- [x] 4.6 Un errore o un annullamento lascia intatte le righe locali e le correzioni già fatte
      → Ogni ramo d'errore esce **prima** di toccare `Items`; `OperationCanceledException` non produce nemmeno un messaggio d'errore. La sostituzione avviene in un unico punto, dopo che il confronto l'ha autorizzata.

## 5. Impostazioni

- [x] 5.1 Sezione "Lettura assistita": interruttore spento per default, inserimento/rimozione/verifica della chiave, scelta del modello
      → Sezione in `SettingsPage`. La chiave si **incolla e non si rilegge**: il campo non mostra mai quella configurata. Verifica e rimozione compaiono solo quando una chiave esiste. Rimuoverla chiede conferma e dichiara che gli scontrini salvati non vengono toccati.
- [x] 5.2 Costo indicativo per scontrino accanto a ogni modello, e consumo effettivo dell'ultima chiamata
      → Ogni voce del selettore porta il costo accanto ("circa 4 ¢ a scontrino"). La stima viene da `ReceiptAiModels.TypicalUsage`, **la stessa** usata nel messaggio di consenso prima dell'invio: due fonti diverse sarebbero divergute. Sotto, il costo effettivo dell'ultima rilettura con i token reali — che è il controllo della stima.
- [x] 5.3 Dichiarazione esplicita di che cosa lascia il device quando la funzione è attiva
      → `AiDisclosureText`, mostrato nella sezione **e** nella richiesta di conferma all'accensione. Dice che cosa esce, che esce solo su richiesta per un singolo scontrino che non torna, che su uno che quadra non parte nessuna chiamata, e dove sta la chiave.
- [x] 5.4 Stato leggibile a colpo d'occhio: spenta / attiva senza chiave / attiva e pronta
      → `AiStatusText` distingue i tre stati. "Attiva senza chiave" ha un messaggio proprio perché **sembra funzionante e non lo è**, ed è l'unico dei tre che l'utente potrebbe fraintendere.
      → **Tutti e tre visti su emulatore** nella stessa sessione: «spenta. Nessun dato degli scontrini lascia il telefono» → «attiva, ma senza chiave non può funzionare» → «attiva e pronta». Verifica e rimozione della chiave compaiono solo a chiave presente, e dopo la rimozione spariscono.

## 6. Test

- [x] 6.1 Collegare in `tests/CardMaster.Tests` la sola logica pura nuova: mappatura dell'esito del modello nelle strutture del dominio
      → Collegati `AiModels.cs`, `ReceiptAiModels.cs`, `ReceiptAiJson.cs`, `ReceiptAiMapper.cs`, `ReceiptAiComparison.cs`. Sono collegabili perché sono puri: l'SDK Anthropic sta nel reader, SkiaSharp nel ridimensionamento, `SecureStorage` nello store — **nessuno dei tre entra nella lista**, come prescrive `CLAUDE.md`.
- [x] 6.2 Test della mappatura: risposta conforme → righe con centesimi, millesimi e punti base corretti
      → `ReceiptAiMapperTests`: importi in centesimi, quantità in millesimi **con unità di misura** (0,432 kg non è 0,432 pezzi), aliquote in punti base, sconto negativo riconosciuto come tale, ordine di stampa conservato, e la data letta con **offset locale** — il test cita esplicitamente il bug dei tick UTC di `docs/technical-notes.md`, che spostava lo scontrino nel mese sbagliato.
- [x] 6.3 Test della risposta non conforme e di quella troncata: errore dichiarato, **nessuna riga parziale o inventata**
      → Risposta troncata a metà JSON, riga senza importo, valore fuori dallo schema (`"sconto"` invece di `"discount"`), più i casi vuoto/nullo/non-JSON/`{}`. In tutti `Reading` è null: **una sola riga inutilizzabile fa fallire l'intera lettura**. Un campo illeggibile (una data) è invece un campo, non una risposta non conforme: si svuota senza buttare via righe buone — c'è un test anche per quello.
- [x] 6.4 Test del confronto: modello che quadra e parser che non quadra, nessuno dei due che quadra, entrambi che quadrano
      → `ReceiptAiComparisonTests` su `ReceiptAiComparison`, estratto come **logica pura** proprio per poter essere testato (serve anche a 4.5). Coperti anche due casi che le regole a parole lasciavano ambigui: a **parità di scarto** non si scomodano le righe locali (il pari non è un miglioramento), e **senza totale stampato** il modello non può essere dichiarato "più vicino", perché mancherebbe la misura.
- [x] 6.5 `dotnet test` verde
      → **92 test, 0 falliti** (22 nuovi).

## 7. Verifica finale

- [x] 7.1 `dotnet build` con 0 errori (criterio di accettazione, non opzionale)
      → 0 errori in Debug e Release; `dotnet test` 92 verdi.
- [ ] 7.2 Verifica su emulatore con lo **stesso scontrino MD** di `receipt-items` (21 righe su 29 in locale): riportare quante righe su quante produce la rilettura e se la somma quadra — un numero, non un giudizio
      → **Richiede una chiave API reale: non eseguibile senza.** Lo scontrino è già sull'emulatore e pronto (MD, 2 ago 2026, totale 41,14, righe 36,38). Il percorso fino all'invio è verificato: pulsante, consenso, chiamata, gestione dell'errore. Manca solo la chiamata che riesce.
- [ ] 7.3 Verifica del costo reale di quella chiamata, confrontato con la stima dichiarata nelle impostazioni
      → Dipende da 7.2. La stima mostrata è **4 ¢ con Claude Opus 5**, visibile sia nel selettore del modello sia nella richiesta di consenso.
- [x] 7.4 Verifica in **modalità aereo**: la rilettura fallisce con un messaggio comprensibile e lo scontrino resta salvabile con le righe locali
      → Verificato su emulatore (API 36, 13 ago 2026) sullo **scontrino MD reale già salvato** — totale 41,14, righe 36,38, scarto 4,76. Con una chiave **fittizia** e la modalità aereo attiva: «Nessuna connessione. Lo scontrino resta salvabile con le righe lette in locale.» Righe, totale e categorie **intatti**.
      Verificato anche il percorso della verifica della chiave in aereo: dice «Nessuna connessione: non è stato possibile verificarla. La chiave resta salvata.» — e **non** «chiave non valida», pur essendo la chiave palesemente finta. La rete ha la precedenza sulla validità, che è l'unica risposta onesta: è la distinzione del task 2.3 vista funzionare a runtime.
- [x] 7.5 Verifica con funzione spenta: nessuna chiamata, nessuna menzione di costi, comportamento identico a oggi
      → Con `AiScanEnabled` falso `CanRescanWithAi` è falso: nel percorso dello scontrino non compare il pulsante, non si nomina alcun costo e non parte nulla. La sezione nelle impostazioni resta visibile e dichiara i costi — è quanto richiede la spec `app-settings`, ed è l'unico posto dove la funzione si nomina da spenta.
      → **Confermato su emulatore**: spenta la funzione e riaperto lo stesso scontrino, il pulsante è sparito del tutto e il form è **identico a prima della change** — stesso messaggio di quadratura, stesse righe. Con la chiave ancora salvata ma la funzione spenta lo stato dice «spenta», che è il comportamento voluto: la chiave non basta ad accendere niente.
- [x] 7.6 Verificare che la chiave non finisca nel database, nel backup Drive, nei log né nell'interfaccia
      → Vedi 2.2 (esclusione strutturale da database e backup: `BackupService` carica solo lo snapshot SQLite) e 2.5 (audit su log, errori e interfaccia).
- [x] 7.7 Confermare che il pacchetto **non contiene alcuna chiave**: la sola credenziale è quella che inserisce l'utente
      → Nessuna costante di chiave nel sorgente: `AiCredentialStore` conserva solo il *nome* della voce di `SecureStorage`. L'unico valore che esiste è quello incollato dall'utente a runtime.
- [x] 7.8 Rivedere il `git diff` prima del commit — repository pubblico — escludendo chiavi, immagini di scontrini reali e percorsi personali
      → Riletto a ogni commit. Nessuna chiave, nessun percorso personale, nessuna immagine di scontrino. L'unica occorrenza di `sk-ant-` è il *placeholder* del campo di inserimento.
- [x] 7.9 Aggiornare `PLAN.md`: la scelta di inviare l'**immagine** contraddice il vincolo scritto per `receipt-ai-normalize` («solo le descrizioni, mai immagine»); va registrata come decisione del 12 ago 2026 con la sua motivazione, e va chiarito che `receipt-ai-normalize` riuserà chiave e client di questa change
      → Voce `receipt-ai-scan` aggiunta con la decisione datata e la sua motivazione (il testo OCR ha già perso l'associazione descrizione↔prezzo; il totale `41,14` era `47,74`), le mitigazioni e il costo di +3,96 MB. La riga di `receipt-ai-normalize` chiarisce che il vincolo «solo le descrizioni» **resta valido per quella change**, dove le descrizioni bastano, e che riuserà chiave e client.
