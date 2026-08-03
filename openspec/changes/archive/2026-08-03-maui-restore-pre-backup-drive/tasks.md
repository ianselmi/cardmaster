## 1. Modello degli esiti

- [x] 1.1 In `src/CardMaster/Services/Backup/BackupModels.cs` aggiungere `RestoreOutcome.PreBackupFailed`, con commento che ne dichiara il significato (situazione corrente non salvata su Drive → ripristino non eseguito)
- [x] 1.2 Verificare che `RestoreResult` porti già la `BackupErrorKind` necessaria a questo esito; se serve, adeguarne il commento

## 2. Servizio di backup

- [x] 2.1 In `IBackupService` rimuovere `UndoLastRestoreAsync` e aggiornare il commento XML del tipo e di `RestoreAsync` (niente più snapshot di sicurezza e undo)
- [x] 2.2 In `BackupService` rimuovere `UndoLastRestoreAsync`, il campo `_safetySnapshotPath` e la creazione dello snapshot `safety-*.db3`
- [x] 2.3 In `BackupService.RestoreAsync` riordinare i passi: lista → target trovato → guardia di schema → download in cache → backup pre-ripristino → `ReplaceFromAsync` (il download precede il backup perché la ritenzione può eliminare proprio il file scelto — vedi design)
- [x] 2.4 Eseguire il backup pre-ripristino chiamando `BackupNowAsync`; se `Success` è false, cancellare il file scaricato e restituire `PreBackupFailed` con la `BackupErrorKind` del backup, senza toccare il database
- [x] 2.5 Rimuovere il rollback automatico dallo snapshot locale nel `catch` di `ReplaceFromAsync`: l'esito resta `Failed` con `BackupErrorKind.Local`
- [x] 2.6 Verificare che tutti i percorsi di uscita cancellino il file temporaneo di download (nessun residuo in cache) — `TryDelete(downloadPath)` in un `finally` che copre backup pre-ripristino, swap riuscito e swap fallito

## 3. ViewModel e testi

- [x] 3.1 In `BackupViewModel.RestoreAsync` aggiornare il testo di conferma: dichiarare che la situazione attuale viene prima salvata su Drive come backup (via lo snapshot locale citato oggi)
- [x] 3.2 Sostituire il prompt "Annulla ripristino" mostrato dopo un ripristino riuscito con un semplice avviso di completamento, e rimuovere la chiamata a `UndoLastRestoreAsync`
- [x] 3.3 Gestire `RestoreOutcome.PreBackupFailed` con un messaggio che dice che il ripristino non è stato eseguito, seguito dal testo di `MessageFor(kind)`, e ri-notificare lo stato (il banner riflette il backup fallito)
- [x] 3.4 Aggiornare il messaggio di `RestoreOutcome.Failed` con `BackupErrorKind.Local` indirizzando al recupero manuale: ripetere il ripristino scegliendo il backup più recente (quello appena creato)

## 4. Verifica

- [x] 4.1 `dotnet build` senza errori
- [ ] 4.2 Verifica sull'emulatore (skill `android-emulator`): ripristino riuscito → il backup della situazione precedente compare nella lista con data/ora del ripristino ed è a sua volta ripristinabile
- [ ] 4.3 Verifica sull'emulatore: con rete disattivata, il ripristino si ferma con il messaggio di backup pre-ripristino non riuscito e le carte restano quelle di prima
- [ ] 4.4 Verifica che dopo un ripristino non venga più proposta l'azione "Annulla ripristino"

> **4.2–4.4 restano da fare all'utente.** Il flusso di ripristino parte solo con un account Google
> collegato e almeno un backup nella cartella applicativa di Drive: il consenso OAuth richiede le
> credenziali personali dell'utente, che l'agent non ha e non deve avere. Emulatore avviato
> (`pixel_7_-_api_36_0`, API 36) e app non installabile per spazio esaurito sulla partizione
> `/data` (93% pieno, 406 MB liberi): da liberare prima di riprovare, ma resta comunque il vincolo
> dell'account.

- [x] 4.5 Rivedere il diff prima del commit (repository pubblico: nessun segreto o dato personale)
