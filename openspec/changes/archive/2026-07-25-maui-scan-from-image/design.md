## Context

`ScanPage` è oggi l'unico punto di acquisizione di un barcode: monta la `CameraView` di `BarcodeScanning.Native.Maui`, filtra le simbologie con `BarcodeFormatCatalog.ScannerSymbologies` e, alla prima lettura valida (`OnDetectionFinished`), decide se il codice è un payload di condivisione CardMaster (`ICardShareCodec.TryDecode`) o un barcode fedeltà grezzo, e naviga verso `AddCardPage` con i parametri corrispondenti. L'unica alternativa è il pulsante "Inserisci a mano", che apre `AddCardPage` vuota.

La libreria già referenziata (`BarcodeScanning.Native.Maui` 3.0.6) espone, oltre alla `CameraView`, i metodi statici `BarcodeScanning.Methods.ScanFromImageAsync(...)` con overload per `byte[]`, `FileResult`, path/URL e `Stream`: stesso motore ML Kit, applicato a un'immagine statica invece che al flusso della camera. Questo permette di aggiungere il percorso "da immagine" senza nuove dipendenze e restando offline.

Vincoli rilevanti: app 100% offline nel core (nessuna analisi remota dell'immagine), nessun nuovo permesso Android persistente (oggi il manifest non dichiara alcun permesso di storage), `dotnet build` senza errori come criterio di accettazione. Non esiste un progetto di test: la verifica è build + prova manuale su emulatore/device.

## Goals / Non-Goals

**Goals:**

- Aggiungere un terzo percorso di acquisizione ("da immagine") che sbocchi nella **stessa** schermata di conferma degli altri due, senza duplicare la logica di interpretazione del codice.
- Coprire anche il caso "QR di condivisione CardMaster ricevuto come immagine/screenshot", riusando `ICardShareCodec` già esistente.
- Non introdurre nuove dipendenze, nuovi permessi o traffico di rete.

**Non-Goals:**

- Nessuna modifica al flusso di conferma/salvataggio (`AddCardPage`), all'avviso duplicati, al modello dati o alla capability `card-sharing`.
- Nessuna acquisizione da immagine ritagliata/ruotata dall'utente in-app, nessun editor di immagini, nessun miglioramento di contrasto/preprocessing.
- Nessuna importazione multipla (più carte da una sola immagine o da più immagini in un colpo solo).
- Nessun percorso di ingresso dall'esterno dell'app (share sheet di Android verso CardMaster): resta un'azione avviata da dentro `ScanPage`.

## Decisions

### 1. Motore di riconoscimento: `Methods.ScanFromImageAsync` (ML Kit), non ZXing

**Scelta**: usare `BarcodeScanning.Methods.ScanFromImageAsync` della libreria già presente.

**Perché**: mantiene un solo motore di lettura per tutta l'app, coerente con la nota di `PLAN.md` ("lettura con ML Kit, più affidabile di ZXing su Android per codici stampati/plastificati"); nessuna nuova dipendenza; gli overload accettano direttamente il risultato del picker.

**Alternativa scartata**: decodificare con `ZXing.Net` + `SkiaSharp`, entrambi già referenziati per il *rendering*. Funzionerebbe, ma introdurrebbe un secondo motore di lettura con caratteristiche di riconoscimento diverse da quelle della camera — a parità di immagine l'esito potrebbe divergere dalla scansione live, e sarebbe una discrepanza difficile da spiegare all'utente.

### 2. Filtro dei formati lato applicativo

`ScanFromImageAsync` non prende in ingresso le simbologie da abilitare (a differenza di `CameraView.BarcodeSymbologies`): restituisce l'insieme dei codici trovati. Il filtro sui formati supportati SHALL quindi avvenire nel codice dell'app, scorrendo i risultati e scartando quelli per cui `BarcodeFormatCatalog.FromScanner(...)` restituisce `null` — esattamente come già fa `OnDetectionFinished` per i risultati della camera. Con più codici validi nell'immagine si prende il primo utile: la conferma resta modificabile, e l'alternativa (una UI di scelta tra i codici trovati) non vale la complessità per un caso di bordo.

### 3. Selettore immagini: `FilePicker` come prima scelta, per non aggiungere permessi

**Scelta**: aprire il selettore con `FilePicker.PickAsync(new PickOptions { FileTypes = FilePickerFileType.Images })`, che su Android passa dal Storage Access Framework: l'utente concede l'accesso alla **singola** immagine scelta, l'app non ottiene alcun permesso persistente sull'archivio, e sono raggiungibili sia la galleria sia Download/Drive (dove tipicamente atterra un'immagine ricevuta in chat).

**Alternativa considerata**: `MediaPicker.PickPhotoAsync()`, più orientato alla galleria fotografica. È accettabile solo se non introduce permessi: in alcune combinazioni MAUI/Android il suo uso porta in dote `READ_EXTERNAL_STORAGE`/`READ_MEDIA_IMAGES` nel manifest unito. **In implementazione va verificato il manifest unito** (`obj/Debug/net10.0-android/AndroidManifest.xml` dopo la build): se compaiono permessi di storage che oggi non ci sono, la scelta è sbagliata e si resta su `FilePicker`. Il requisito "nessun permesso persistente sull'archivio" della spec è il criterio dirimente.

L'utente annulla il picker → `PickAsync` restituisce `null`: nessun messaggio, si resta sulla pagina.

### 4. Un solo punto di interpretazione del codice, condiviso tra camera e immagine

Oggi la logica "questo QR è un payload CardMaster? altrimenti è un barcode grezzo" vive dentro `OnDetectionFinished`, intrecciata con la gestione della camera (`_handled`, `CameraEnabled`, `_lastRejectedValue`). Va estratta in un metodo privato di `ScanPage` che, dato `(valore, formato)`, decide l'esito: **snapshot condiviso**, **barcode grezzo**, oppure **payload CardMaster non leggibile** (corrotto/versione non supportata). Entrambi i percorsi lo chiamano; solo il chiamante camera continua a gestire spegnimento anteprima e anti-ripetizione dell'avviso.

**Perché non un servizio a sé**: la logica è tre righe di dispatch verso `Shell.Current.GoToAsync` con i parametri di navigazione — spostarla in un servizio iniettato aggiungerebbe indirezione senza guadagno, dato che non c'è un progetto di test che possa esercitarla in isolamento.

### 5. Comportamento sulla pagina durante e dopo l'analisi

L'analisi di un'immagine grande può richiedere qualche centinaio di ms: durante l'operazione la camera viene disabilitata (evita che una lettura live faccia navigare mentre l'immagine è in analisi) e il pulsante viene disabilitato per non lanciare due analisi sovrapposte. Esiti:

- **Codice supportato trovato** → navigazione a `AddCardPage`, come dalla camera.
- **Nessun codice supportato / immagine illeggibile** → `DisplayAlert` con messaggio distinto per i due casi, poi la pagina torna operativa (camera riabilitata se il permesso c'è, altrimenti resta il pannello del permesso negato) e l'utente può riprovare.

Le eccezioni di lettura del file e di decodifica vengono catturate attorno alla chiamata: un file non-immagine o illeggibile deve produrre l'avviso, non un crash.

### 6. L'immagine non viene copiata né conservata

Si legge lo stream fornito dal picker e lo si passa al decoder; l'app non salva copie in cache né in `FilesDir`. Questo tiene la funzione fuori dal perimetro del backup Drive e dal rischio di lasciare sul device copie di immagini personali dell'utente.

## Risks / Trade-offs

- **Riconoscimento meno affidabile che dal vivo** (foto sfocata, codice piccolo, prospettiva): dal vivo l'utente aggiusta l'inquadratura fino alla lettura, sull'immagine c'è un solo tentativo → il messaggio di esito negativo deve suggerire l'alternativa (riprova con un'altra immagine, usa la camera, inserisci a mano), non presentarsi come errore definitivo.
- **`MediaPicker` porterebbe permessi di storage nel manifest** → mitigazione: `FilePicker` come scelta di default e verifica esplicita del manifest unito tra i task.
- **Immagine molto grande in memoria** (foto da 50+ MP): la decodifica avviene su bitmap in memoria → mitigazione: l'analisi è dentro try/catch e l'esito negativo è già un percorso previsto; se emergesse un problema reale su device, il ridimensionamento preventivo è un'aggiunta successiva, non necessaria ora.
- **Screenshot di QR di condivisione di versione futura**: già coperto dal percorso esistente "versione non supportata" di `ICardShareCodec`, ora raggiungibile anche da immagine.
- **Superficie UI**: `ScanPage` passa da uno a due pulsanti sovrapposti all'anteprima → tenerli su una riga in fondo, senza coprire l'area utile di inquadratura.

## Open Questions

Nessuna: la funzione riusa motore, catalogo formati, codec di condivisione e schermata di conferma già esistenti. L'unico punto da confermare in implementazione è la scelta del picker (decisione 3), risolvibile guardando il manifest unito dopo la build.
