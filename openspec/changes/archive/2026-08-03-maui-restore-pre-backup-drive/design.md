## Context

`BackupService.RestoreAsync` oggi esegue: lista dei backup su Drive → guardia di versione di schema → download del backup scelto in cache → **snapshot di sicurezza locale** del DB corrente (`safety-*.db3` in cache) → `ReplaceFromAsync`. Lo snapshot serve a due cose: il rollback automatico se lo swap fallisce a metà, e l'`UndoLastRestoreAsync` esposto dopo un ripristino riuscito (il path resta nel campo `_safetySnapshotPath`, quindi solo finché il processo è vivo).

La change sostituisce quello snapshot con un **backup ordinario su Drive** della situazione corrente. Il vincolo che guida quasi tutte le decisioni qui sotto è la **ritenzione ≤3** già in vigore: aggiungere un backup durante il ripristino può eliminarne uno più vecchio — potenzialmente proprio quello che l'utente ha scelto di ripristinare.

## Goals / Non-Goals

**Goals:**
- La situazione precedente a un ripristino finisce su Drive come backup normale, elencabile e ripristinabile come tutti gli altri.
- Nessuna copia di sicurezza locale e nessun comando "Annulla ripristino".
- Un ripristino non parte mai se la situazione corrente non è stata messa al sicuro.
- Riuso del percorso di backup esistente, senza un secondo formato di file o un secondo percorso di upload da mantenere.

**Non-Goals:**
- Cambiare il limite di ritenzione (resta ≤3) o marcare in modo speciale i backup pre-ripristino.
- Introdurre un backup locale esportabile fuori da Drive.
- Toccare autenticazione, formato del file di backup, schema del database o il flusso di backup manuale/schedulato.

## Decisions

### 1. Il backup pre-ripristino è `BackupNowAsync`, non un upload dedicato

Il ripristino chiama lo stesso percorso del backup manuale. Conseguenze volute: stessa convenzione di nome (`BackupNaming.Build` con la versione di schema corrente), stessa ritenzione, stessa notifica di avanzamento, e aggiornamento di `LastBackupUtc`/`LastBackupSize`/`LastBackupError` come per qualunque altro backup.

*Alternativa scartata*: un upload con nome distinto (es. prefisso `pre-restore-`) escluso dalla ritenzione. Avrebbe richiesto di estendere `BackupNaming`, la lista in-app e le regole di rotazione, e avrebbe ricreato — su Drive invece che in cache — la stessa "copia speciale invisibile" che questa change vuole eliminare.

Effetto collaterale accettato: un backup pre-ripristino fallito registra lo stato di errore del backup (banner "ultimo backup non riuscito"). È corretto — è stato un vero tentativo di backup, fallito per un vero motivo — e differisce dall'attuale `RestoreFailure`, che tocca lo stato solo per `ReauthRequired`.

### 2. Il download del backup scelto precede il backup della situazione corrente

Ordine nuovo: lista → target trovato → guardia di schema → **download in cache** → **backup pre-ripristino** → `ReplaceFromAsync`.

Il download va prima perché la ritenzione può cancellare proprio il file che stiamo per ripristinare: con 3 backup su Drive e l'utente che sceglie il più vecchio, il backup pre-ripristino porta il totale a 4 e la rotazione elimina il più vecchio — il target. Scaricandolo prima, il ripristino lavora sulla copia locale e la sua sparizione da Drive è irrilevante. L'ordine inverso avrebbe reso non ripristinabile il backup più vecchio, in silenzio e proprio nel caso in cui serve di più.

Il download resta comunque dopo la guardia di schema, per non consumare banda su un backup che verrà rifiutato.

*Costo*: se il backup pre-ripristino fallisce, il download è stato inutile. Il file temporaneo viene cancellato e non resta nulla di sporco: banda sprecata, nessun danno.

### 3. Il fallimento del backup pre-ripristino ferma il ripristino

Nuovo esito `RestoreOutcome.PreBackupFailed`, che porta con sé la `BackupErrorKind` del backup fallito. Il database corrente non viene toccato. Bloccare non toglie niente all'utente: il ripristino richiede comunque la rete per scaricare il backup, quindi ogni categoria che fa fallire l'upload (rete, quota, credenziali) avrebbe fatto fallire anche il download o lo avrebbe reso inutile.

Il messaggio riusa `MessageFor(kind)` già presente nel ViewModel, premesso da una frase che dice quale passo è saltato ("Non è stato possibile salvare la situazione attuale su Drive: il ripristino non è stato eseguito").

### 4. Niente rollback automatico se lo swap fallisce a metà

`ReplaceFromAsync` fa `File.Copy(overwrite: true)`: un fallimento a metà può lasciare il file del database inconsistente. Oggi si rimedia ricopiando lo snapshot locale; senza quello snapshot, la via di recupero è il backup appena caricato su Drive. L'esito resta `RestoreOutcome.Failed` con `BackupErrorKind.Local`, e il messaggio indirizza esplicitamente a ripetere il ripristino scegliendo il backup più recente (quello appena creato).

È il punto in cui la change perde qualcosa rispetto a oggi: il recupero passa da automatico a manuale e richiede rete. In cambio non esiste più uno stato in cui l'unica copia della situazione precedente è un file in cache che Android può cancellare.

### 5. `UndoLastRestoreAsync` e `_safetySnapshotPath` spariscono dall'interfaccia

`IBackupService.UndoLastRestoreAsync` viene rimosso invece di lasciarlo restituire sempre `false`: un metodo che non fa mai niente è un invito a ricostruire l'undo. L'unico consumer è `BackupViewModel.RestoreAsync`, che dopo un ripristino riuscito passa da un `DisplayAlert` con scelta a un semplice avviso di conferma.

## Risks / Trade-offs

- **Ogni ripristino consuma uno slot di ritenzione** → con ≤3, due ripristini consecutivi possono spazzare via lo storico dei backup periodici. Accettato: la ritenzione è una decisione già presa e alzarla è fuori scope; il backup che conta di più (la situazione appena precedente) è sempre il più recente, quindi mai il primo a essere eliminato.
- **Recupero manuale se lo swap fallisce a metà** (decisione 4) → mitigato dal messaggio d'errore, che dice esattamente cosa fare, e dal fatto che il backup della situazione precedente è già su Drive quando lo swap parte.
- **Il ripristino diventa più lento e consuma più banda/quota Drive** (un upload in più) → accettato: è il prezzo della rete di sicurezza, e il volume di un backup di carte fedeltà è nell'ordine delle decine di KB.
- **Notifica "Backup in corso…" durante un ripristino** → conseguenza del riuso di `BackupNowAsync`. Informativa, non fuorviante: un backup sta davvero avvenendo.
- **Il banner di stato può passare a "ultimo backup non riuscito" dopo un tentativo di ripristino** → è la registrazione corretta di un backup realmente fallito; la data dell'ultimo backup riuscito resta accanto, come già previsto dal requisito sull'esito persistito.
