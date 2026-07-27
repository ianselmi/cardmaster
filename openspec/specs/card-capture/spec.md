# card-capture

## Purpose

Acquisizione di una carta fedeltà tramite scansione barcode (ML Kit) o inserimento manuale, con arricchimento opzionale dell'emittente dal catalogo, scelta opzionale di colore del riquadro e label, avviso duplicati e salvataggio locale nel database cifrato. Il flusso di conferma/salvataggio è riusabile in ricezione da `maui-share-qr`.
## Requirements
### Requirement: Scansione barcode con camera

Il sistema SHALL offrire una schermata di scansione con anteprima camera live che riconosce i formati EAN-13, EAN-8, UPC-A, UPC-E, Code128, Code39, ITF, Codabar, QR e PDF417. Alla **prima lettura valida** il sistema SHALL fermare la scansione e procedere alla schermata di conferma con barcode e formato pre-compilati.

#### Scenario: Rilevazione e stop alla prima lettura

- **WHEN** la camera aggancia un barcode di un formato supportato
- **THEN** la scansione si ferma e si apre la schermata di conferma con il valore del barcode e il formato rilevato già compilati

#### Scenario: Formati supportati

- **WHEN** viene inquadrato un barcode di uno dei formati supportati (EAN-13, EAN-8, UPC-A, UPC-E, Code128, Code39, ITF, Codabar, QR, PDF417)
- **THEN** il sistema lo riconosce e ne estrae valore e formato

#### Scenario: Formato non supportato ignorato

- **WHEN** viene inquadrato un barcode di un formato non incluso tra quelli supportati
- **THEN** il sistema non procede alla conferma (la lettura viene ignorata)

### Requirement: Gestione del permesso camera

Il sistema SHALL richiedere il permesso camera a runtime quando l'utente entra nella scansione. Se il permesso è negato, il sistema SHALL restare utilizzabile tramite l'**inserimento manuale** e l'**acquisizione da un'immagine esistente**, senza bloccare l'app.

#### Scenario: Permesso concesso

- **WHEN** l'utente entra nella scansione e concede il permesso camera
- **THEN** l'anteprima camera si avvia e la scansione è operativa

#### Scenario: Permesso negato

- **WHEN** l'utente nega il permesso camera
- **THEN** viene mostrato un messaggio chiaro e restano disponibili sia l'inserimento manuale del barcode sia la scelta di un'immagine da cui estrarlo

### Requirement: Inserimento manuale del barcode

Il sistema SHALL permettere di inserire manualmente il barcode (valore + formato scelto tra quelli supportati) come percorso alternativo alla scansione, portando alla stessa schermata di conferma/modifica.

#### Scenario: Inserimento manuale

- **WHEN** l'utente sceglie l'inserimento manuale e digita un valore e seleziona un formato
- **THEN** si apre la schermata di conferma con quei dati, pronti per il salvataggio

### Requirement: Arricchimento opzionale dell'emittente

Il sistema SHALL consentire di associare la carta a un emittente in modo facoltativo: scelto dal catalogo, digitato liberamente, o assente. Se l'emittente è scelto dal catalogo, la carta SHALL ereditarne i metadati disponibili (colore, riferimento logo, formato barcode atteso). Un emittente libero o assente NON MUST impedire il salvataggio.

#### Scenario: Emittente dal catalogo

- **WHEN** l'utente seleziona un emittente presente nel catalogo
- **THEN** la carta eredita i metadati dell'emittente (colore, logo, formato atteso quando presenti)

#### Scenario: Emittente libero

- **WHEN** l'utente digita un nome di emittente non presente nel catalogo
- **THEN** la carta viene salvata con quel nome, senza arricchimento e senza errori

#### Scenario: Nessun emittente

- **WHEN** l'utente non indica alcun emittente
- **THEN** la carta viene salvata comunque, purché sia presente un nome visualizzato

### Requirement: Campi obbligatori per il salvataggio

Il sistema SHALL impedire il salvataggio se manca il valore del barcode, il formato o il nome visualizzato. Il nome visualizzato SHALL avere come default il nome dell'emittente quando questo è indicato.

#### Scenario: Salvataggio bloccato senza dati minimi

- **WHEN** l'utente tenta di salvare senza barcode, senza formato o senza nome visualizzato
- **THEN** il salvataggio è impedito e viene segnalato il campo mancante

#### Scenario: Nome di default dall'emittente

- **WHEN** l'utente seleziona un emittente e non ha ancora digitato un nome
- **THEN** il nome visualizzato viene impostato di default al nome dell'emittente (resta modificabile)

### Requirement: Avviso duplicati alla creazione

Il sistema SHALL verificare, prima di salvare, se esiste già una carta **attiva** (non tombstone) con lo stesso valore di barcode. In tal caso SHALL mostrare un avviso **non bloccante** che consente all'utente di aggiungere comunque o annullare.

#### Scenario: Barcode già presente

- **WHEN** l'utente conferma una carta il cui barcode coincide con una carta attiva esistente
- **THEN** viene mostrato un avviso ("Hai già questa carta") con la scelta di aggiungere comunque o annullare

#### Scenario: Barcode non presente

- **WHEN** il barcode non coincide con alcuna carta attiva
- **THEN** la carta viene salvata senza avvisi

### Requirement: Salvataggio locale della carta

Il sistema SHALL salvare la carta nel database locale cifrato con Id generato dal client, timestamp e semantica tombstone (come da capability local-storage). Dopo il salvataggio la carta SHALL comparire nella lista carte.

#### Scenario: Carta salvata e visibile

- **WHEN** l'utente conferma il salvataggio di una carta valida
- **THEN** la carta viene persistita con Id client-generato e compare nella lista carte

#### Scenario: Persistenza offline

- **WHEN** il salvataggio avviene senza connessione di rete
- **THEN** la carta viene comunque salvata localmente

### Requirement: Colore e label opzionali alla creazione della carta

Nella schermata di conferma che precede il salvataggio — qualunque sia il percorso di acquisizione (camera, immagine esistente, inserimento manuale) — il sistema SHALL consentire di scegliere il **colore del riquadro** dalla palette predefinita (o lasciare "Automatico") e di assegnare **label** secondo le regole della capability `card-labels`. Entrambi SHALL essere **opzionali**: il flusso di acquisizione MUST NOT richiedere di compilarli e i valori di default ("Automatico", nessuna label) SHALL consentire il salvataggio come oggi.

#### Scenario: Salvataggio senza toccare colore e label

- **WHEN** l'utente conferma il salvataggio di una carta valida senza scegliere colore né label
- **THEN** la carta viene salvata come oggi, con colore automatico derivato dal nome e nessuna label

#### Scenario: Colore scelto in creazione

- **WHEN** l'utente sceglie un colore dalla palette prima di salvare
- **THEN** la carta viene salvata con quel colore e il suo riquadro nella lista lo usa

#### Scenario: Label assegnate in creazione

- **WHEN** l'utente assegna una o più label prima di salvare
- **THEN** la carta viene salvata con quelle label, disponibili subito come filtro nella lista

#### Scenario: Suggerimenti disponibili in creazione

- **WHEN** l'utente apre l'editor delle label mentre crea una carta e altre carte hanno già delle label
- **THEN** quelle label compaiono come suggerimenti selezionabili con un tocco

#### Scenario: Carta ricevuta via QR di condivisione

- **WHEN** la schermata di conferma è pre-compilata dallo snapshot di un QR di condivisione ricevuto
- **THEN** colore ed etichette si comportano come per una carta nuova (colore automatico, nessuna label), e l'utente può impostarli prima di salvare

### Requirement: Ricezione di una carta condivisa via scansione

Durante l'acquisizione — sia con la camera sia da **un'immagine esistente** — il sistema SHALL riconoscere quando il QR letto è un **payload di condivisione CardMaster** (identificato dal prefisso/versione dello schema) e, in tal caso, SHALL decodificarlo e aprire la schermata di conferma **pre-compilata con l'intero snapshot** ricevuto (nome, emittente, colore, logo, barcode, formato), anziché trattarlo come un barcode QR grezzo. Un QR che NON è un payload CardMaster SHALL continuare a essere trattato come un normale barcode QR fedeltà (comportamento esistente).

#### Scenario: QR di condivisione riconosciuto

- **WHEN** la camera aggancia un QR che è un payload di condivisione CardMaster valido
- **THEN** il sistema lo decodifica e apre la conferma pre-compilata con nome, emittente, colore, logo, barcode e formato dello snapshot

#### Scenario: QR di condivisione ricevuto come immagine

- **WHEN** l'utente sceglie un'immagine (per esempio lo screenshot ricevuto in chat) che contiene un payload di condivisione CardMaster valido
- **THEN** il sistema lo decodifica e apre la conferma pre-compilata con l'intero snapshot, esattamente come se fosse stato inquadrato con la camera

#### Scenario: QR fedeltà normale

- **WHEN** la camera aggancia un QR che non è un payload CardMaster
- **THEN** il sistema lo tratta come un normale barcode QR (conferma con solo valore e formato QR)

#### Scenario: Payload corrotto o versione non supportata

- **WHEN** viene letto — dalla camera o da un'immagine — un QR con prefisso CardMaster ma payload corrotto o di versione non supportata
- **THEN** il sistema segnala che il codice non è leggibile e resta stabile, senza crashare né creare una carta

### Requirement: Carta ricevuta salvata come copia indipendente

Una carta ricevuta tramite QR di condivisione SHALL essere salvata come **nuova copia locale** (Id client-generato, timestamp, semantica tombstone), senza alcun legame persistente col device mittente. Prima del salvataggio il sistema SHALL applicare il consueto **avviso duplicati non bloccante** (stesso barcode di una carta attiva) proponendo di saltare invece di duplicare.

#### Scenario: Copia indipendente creata

- **WHEN** l'utente conferma il salvataggio di una carta ricevuta via QR
- **THEN** viene creata una nuova carta locale con Id client-generato, senza riferimenti al mittente, e compare nella lista carte

#### Scenario: Duplicato in ricezione

- **WHEN** la carta ricevuta ha lo stesso barcode di una carta attiva già presente
- **THEN** viene mostrato l'avviso duplicati non bloccante che consente di saltare o aggiungere comunque

### Requirement: Acquisizione del barcode da un'immagine esistente

Il sistema SHALL offrire, dalla schermata di acquisizione, un percorso alternativo alla camera che permette di selezionare **un'immagine già presente sul device** (galleria o file) e di estrarne il barcode. L'analisi SHALL avvenire **interamente in locale**, senza rete, e SHALL riconoscere gli stessi formati della scansione live (EAN-13, EAN-8, UPC-A, UPC-E, Code128, Code39, ITF, Codabar, QR, PDF417). In caso di riconoscimento riuscito il sistema SHALL aprire la **stessa schermata di conferma** della scansione live, con valore e formato pre-compilati.

#### Scenario: Barcode riconosciuto nell'immagine

- **WHEN** l'utente sceglie un'immagine che contiene un barcode di un formato supportato
- **THEN** si apre la schermata di conferma con il valore del barcode e il formato rilevato già compilati, come dopo una scansione con la camera

#### Scenario: Analisi offline

- **WHEN** l'utente analizza un'immagine senza connessione di rete
- **THEN** il riconoscimento avviene comunque, in locale

#### Scenario: Selezione annullata

- **WHEN** l'utente apre il selettore di immagini e lo chiude senza scegliere nulla
- **THEN** il sistema torna alla schermata di acquisizione nello stato precedente, senza messaggi di errore

#### Scenario: Più barcode nella stessa immagine

- **WHEN** l'immagine scelta contiene più di un barcode di formato supportato
- **THEN** il sistema ne seleziona uno e procede alla conferma, dove il valore resta modificabile dall'utente

### Requirement: Esito negativo dell'analisi di un'immagine

Il sistema SHALL comunicare in modo esplicito quando nell'immagine scelta non viene trovato alcun barcode di formato supportato, o quando l'immagine non è leggibile. In questi casi il sistema SHALL restare stabile e sulla schermata di acquisizione, lasciando disponibili gli altri percorsi (nuova immagine, camera, inserimento manuale), senza creare alcuna carta.

#### Scenario: Nessun codice trovato

- **WHEN** l'immagine scelta non contiene alcun barcode di formato supportato
- **THEN** viene mostrato un messaggio che spiega che non è stato trovato alcun codice e l'utente resta sulla schermata di acquisizione, potendo riprovare

#### Scenario: Immagine non leggibile

- **WHEN** il file scelto non è un'immagine valida o non può essere letto
- **THEN** il sistema segnala il problema e resta stabile, senza crashare né creare una carta

### Requirement: Nessun permesso persistente sull'archivio

Il sistema SHALL ottenere l'immagine tramite il **selettore di sistema**, che concede l'accesso alla sola immagine scelta, senza richiedere all'utente un permesso persistente di lettura dell'archivio o della galleria. L'immagine selezionata SHALL essere solo letta e analizzata; il sistema NON MUST copiarla, conservarla o inserirla nel backup.

#### Scenario: Selezione senza permesso archivio

- **WHEN** l'utente sceglie un'immagine dal selettore di sistema
- **THEN** l'app la analizza senza aver richiesto un permesso di accesso all'intero archivio

#### Scenario: Immagine non conservata

- **WHEN** l'analisi dell'immagine è terminata (con o senza successo)
- **THEN** l'app non conserva copie dell'immagine

