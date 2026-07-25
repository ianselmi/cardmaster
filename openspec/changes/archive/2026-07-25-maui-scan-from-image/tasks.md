## 1. Rifattorizzazione del percorso esistente

- [x] 1.1 In `ScanPage.xaml.cs`, estrarre da `OnDetectionFinished` un metodo privato che, dato `(valore, formato)` già validato, decide l'esito: snapshot di condivisione CardMaster, barcode grezzo, oppure payload CardMaster non leggibile (corrotto/versione non supportata)
- [x] 1.2 Riscrivere `OnDetectionFinished` per usare il metodo estratto, mantenendo invariati `_handled`, spegnimento della camera e anti-ripetizione dell'avviso (`_lastRejectedValue`)
- [ ] 1.3 Verificare che la scansione live continui a comportarsi esattamente come prima (barcode normale, QR fedeltà, QR di condivisione valido, QR di condivisione corrotto)

## 2. Acquisizione da immagine

- [x] 2.1 Aggiungere in `ScanPage.xaml` il pulsante "Scegli da un'immagine" accanto a "Inserisci a mano", su una riga in fondo che non copra l'area di inquadratura, visibile anche quando il pannello del permesso camera negato è mostrato
- [x] 2.2 Implementare l'handler: apertura del selettore con `FilePicker.PickAsync` (`FilePickerFileType.Images`); se l'utente annulla, tornare allo stato precedente senza messaggi
- [x] 2.3 Durante l'analisi disabilitare la camera e il pulsante, per evitare analisi sovrapposte o una navigazione innescata da una lettura live
- [x] 2.4 Analizzare l'immagine con `BarcodeScanning.Methods.ScanFromImageAsync`, scartare i risultati il cui formato non è mappato da `BarcodeFormatCatalog.FromScanner` e prendere il primo risultato utile
- [x] 2.5 Passare il risultato al metodo condiviso del punto 1.1, così che barcode grezzo e payload di condivisione seguano lo stesso percorso della camera
- [x] 2.6 Gestire l'esito negativo con messaggi distinti per "nessun codice trovato" e "immagine non leggibile", entrambi dentro try/catch, con ritorno della pagina allo stato operativo (camera riabilitata solo se il permesso è concesso)
- [x] 2.7 Verificare che l'immagine non venga copiata né salvata dall'app (nessuna scrittura in cache o `FilesDir` lungo il percorso)

## 3. Verifica permessi e build

- [x] 3.1 Eseguire `dotnet build` e verificare 0 errori
- [x] 3.2 Ispezionare il manifest Android unito prodotto dalla build e confermare che **non** siano comparsi permessi di storage (`READ_EXTERNAL_STORAGE`, `READ_MEDIA_IMAGES`); se comparissero, correggere la scelta del selettore come da decisione 3 del design

## 4. Verifica funzionale su emulatore/device

- [ ] 4.1 Immagine con barcode 1D supportato (es. EAN-13) → conferma pre-compilata con valore e formato corretti, salvataggio e comparsa nella lista carte
- [ ] 4.2 Screenshot di un QR di condivisione CardMaster generato dall'app → conferma pre-compilata con nome, emittente, colore, logo, barcode e formato dello snapshot; carta salvata come copia indipendente
- [ ] 4.3 Immagine senza alcun codice → messaggio "nessun codice trovato", nessuna carta creata, la pagina resta usabile e si può riprovare
- [ ] 4.4 Selezione annullata dal picker → nessun messaggio, la pagina resta nello stato precedente
- [ ] 4.5 Permesso camera negato → il pulsante "Scegli da un'immagine" è disponibile e funziona, insieme all'inserimento manuale
- [ ] 4.6 Immagine il cui barcode duplica una carta esistente → avviso duplicati non bloccante, come dalla camera

## 5. Chiusura

- [x] 5.1 Aggiornare `PLAN.md` con la voce della change e l'esito della verifica
- [x] 5.2 Rivedere il diff completo prima del commit per escludere segreti o dati personali (repository pubblico), verificando in particolare che non finiscano nel repo immagini di prova con codici reali
- [ ] 5.3 Commit convenzionale e archiviazione della change (`/opsx:archive`)
