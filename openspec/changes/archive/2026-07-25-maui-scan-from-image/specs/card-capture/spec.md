## ADDED Requirements

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

## MODIFIED Requirements

### Requirement: Gestione del permesso camera

Il sistema SHALL richiedere il permesso camera a runtime quando l'utente entra nella scansione. Se il permesso è negato, il sistema SHALL restare utilizzabile tramite l'**inserimento manuale** e l'**acquisizione da un'immagine esistente**, senza bloccare l'app.

#### Scenario: Permesso concesso

- **WHEN** l'utente entra nella scansione e concede il permesso camera
- **THEN** l'anteprima camera si avvia e la scansione è operativa

#### Scenario: Permesso negato

- **WHEN** l'utente nega il permesso camera
- **THEN** viene mostrato un messaggio chiaro e restano disponibili sia l'inserimento manuale del barcode sia la scelta di un'immagine da cui estrarlo

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
