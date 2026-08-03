## Why

Oggi, prima di sostituire il database con un backup scelto, l'app crea uno **snapshot di sicurezza locale** nella cache del device e lo tiene in memoria per un comando "Annulla ripristino". È una seconda rete di sicurezza, con regole tutte sue, parallela a quella che l'app già offre: i backup su Drive. Vive solo in cache (Android può ripulirla), sopravvive solo finché il processo è vivo, non compare nella lista dei backup e non è ripristinabile in nessun altro modo — l'utente non sa che esiste finché non gli viene proposto l'undo subito dopo il ripristino.

Se invece la situazione corrente viene salvata come **backup ordinario su Drive** prima della sostituzione, la rete di sicurezza diventa quella che l'utente già conosce e vede: un backup nella lista, ripristinabile in qualsiasi momento, anche dopo giorni o da un altro device.

## What Changes

- Prima di sostituire il database, il ripristino esegue un **backup ordinario della situazione corrente** su Google Drive (stesso snapshot, stesso upload nella cartella applicativa, stessa ritenzione ≤3 dei backup normali).
- **BREAKING** (comportamento utente): rimossi lo **snapshot di sicurezza locale** e il comando **"Annulla ripristino"** proposto al termine di un ripristino riuscito. Per tornare indietro si ripristina il backup pre-ripristino dalla lista, come qualunque altro backup.
- Se il backup pre-ripristino **fallisce** (rete, spazio Drive esaurito, credenziali non più valide, errore locale), il ripristino **non viene eseguito**: il database corrente resta invariato e l'utente vede il messaggio della categoria d'errore. Un ripristino richiede comunque la rete, quindi non c'è caso in cui bloccarlo tolga all'utente qualcosa che avrebbe potuto fare.
- Il testo di conferma del ripristino cambia di conseguenza: dichiara che la situazione corrente viene salvata su Drive come backup prima della sostituzione.
- Sparisce anche il **rollback automatico** dallo snapshot locale quando la sostituzione del file fallisce a metà: il caso resta segnalato come errore, con il backup appena caricato su Drive come via di recupero.

## Capabilities

### New Capabilities

Nessuna.

### Modified Capabilities

- `cloud-backup`: il requisito "Snapshot di sicurezza prima del ripristino" viene sostituito da un requisito che impone un **backup su Drive della situazione corrente** prima della sostituzione, con il ripristino **bloccato** se quel backup non riesce; il requisito "Ripristino da un backup precedente" recepisce la precondizione e il nuovo testo di conferma.

## Impact

- `src/CardMaster/Services/Backup/BackupService.cs` — `RestoreAsync` (snapshot di sicurezza, rollback, cache del path), `UndoLastRestoreAsync` e il campo `_safetySnapshotPath` spariscono; al loro posto una chiamata al backup ordinario come precondizione.
- `src/CardMaster/Services/Backup/IBackupService.cs` — rimozione di `UndoLastRestoreAsync`.
- `src/CardMaster/Services/Backup/BackupModels.cs` — `RestoreResult`/`RestoreOutcome` devono poter esprimere "backup pre-ripristino non riuscito" con la categoria d'errore.
- `src/CardMaster/ViewModels/BackupViewModel.cs` — `RestoreAsync`: testo di conferma, rimozione del prompt di undo, messaggio del nuovo esito di fallimento.
- Nessuna modifica a Drive client, schema del database, formato dei file di backup o autenticazione. La ritenzione ≤3 resta invariata: un ripristino consuma uno slot come un backup qualsiasi.
