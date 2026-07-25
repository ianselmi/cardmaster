## Why

Oggi l'unico modo per acquisire il codice di una carta senza digitarlo a mano è inquadrarlo con la camera. Ma il codice spesso arriva già come immagine: lo screenshot di una carta digitale, la foto della tessera fatta in precedenza, il QR di condivisione CardMaster ricevuto su WhatsApp. In tutti questi casi oggi l'utente deve o stampare/mostrare il codice su un secondo schermo per inquadrarlo, o ricopiarlo a mano — con il rischio di errori di battitura sulle cifre.

Il motore di riconoscimento già in uso (ML Kit tramite `BarcodeScanning.Native.Maui`) sa decodificare anche immagini statiche: la funzione si ottiene riusando lo stesso catalogo di formati e lo stesso flusso di conferma, senza nuove dipendenze e restando completamente offline.

## What Changes

- Nuovo percorso di acquisizione **"Scegli da un'immagine"** accanto a "Inserisci a mano" nella schermata di scansione: l'utente seleziona un'immagine dalla galleria (o dai file) e il sistema ne estrae barcode e formato.
- L'immagine selezionata viene analizzata **in locale** con ML Kit, con gli stessi formati supportati dalla scansione live (EAN-13, EAN-8, UPC-A, UPC-E, Code128, Code39, ITF, Codabar, QR, PDF417).
- In caso di riconoscimento riuscito si apre la **stessa schermata di conferma** della scansione live, pre-compilata con valore e formato: da lì in poi il flusso (emittente, nome, avviso duplicati, salvataggio) è identico e invariato.
- Se l'immagine contiene un **QR di condivisione CardMaster**, viene riconosciuto e decodificato esattamente come dalla camera, aprendo la conferma pre-compilata con l'intero snapshot ricevuto. Questo rende importabile una carta condivisa ricevuta come immagine tramite chat.
- Se nell'immagine non viene trovato alcun codice supportato, il sistema lo comunica in modo chiaro e lascia l'utente sulla scansione, dove può riprovare con un'altra immagine, con la camera o con l'inserimento manuale.
- Il percorso da immagine resta disponibile **anche quando il permesso camera è negato**, insieme all'inserimento manuale.
- L'app non chiede nuovi permessi persistenti sull'archivio: la selezione avviene tramite il selettore di sistema, che concede l'accesso alla sola immagine scelta.

## Capabilities

### New Capabilities

Nessuna nuova capability: la funzione estende il percorso di acquisizione esistente.

### Modified Capabilities

- `card-capture`: si aggiunge l'acquisizione del barcode da **un'immagine già esistente** come terzo percorso a fianco di scansione live e inserimento manuale, con esito negativo gestito esplicitamente; il riconoscimento del payload di condivisione CardMaster si estende alle immagini oltre che alla camera.

## Impact

- **Codice**: `src/CardMaster/Views/ScanPage.xaml` e `ScanPage.xaml.cs` (nuovo pulsante e handler di selezione/analisi); estrazione della logica comune di decodifica oggi inline nell'handler della camera (barcode grezzo vs payload CardMaster), così che entrambi i percorsi la condividano.
- **Dipendenze**: nessuna nuova. Si usano `Methods.ScanFromImageAsync` di `BarcodeScanning.Native.Maui` (già referenziato, 3.0.6) e il selettore immagini di MAUI Essentials.
- **Dati e sicurezza**: nessuna modifica al modello dati, nessuna scrittura in DB aggiuntiva rispetto al normale salvataggio carta. L'immagine viene solo letta e analizzata in memoria, mai copiata o conservata dall'app. Nessuna rete: la funzione resta interamente offline.
- **Capability non toccate**: `card-display`, `card-sharing` (la generazione del QR resta invariata), `local-storage`, `card-list`.
