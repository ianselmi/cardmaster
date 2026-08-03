## Why

Quando un backup su Google Drive fallisce, o quando il refresh token OAuth viene revocato/scade, l'app non lo comunica in modo comprensibile: la pagina Backup resta identica (account collegato, ultimo backup vecchio) e l'unico segnale è un alert momentaneo con il messaggio tecnico grezzo — `Drive ha risposto 403: {"error":{...}}` — da cui l'utente dovrebbe dedurre da solo qual è il problema e cosa fare. Peggio con i backup schedulati in background: l'esito fallito passa in una notifica di sistema e poi sparisce, così l'utente resta convinto di avere un backup aggiornato mentre in realtà non ne viene più fatto uno da settimane.

## What Changes

- **Esito dell'ultimo backup persistito** (riuscito / fallito, quando, con quale categoria di errore) e mostrato in pagina Backup come stato permanente, non più solo come alert effimero. Vale anche per i backup schedulati in background.
- **Categorie di errore in linguaggio comprensibile** al posto del testo grezzo di Drive: rete assente, spazio Drive esaurito, credenziali Google non più valide, servizio Drive non disponibile, errore locale (snapshot del database). Ogni categoria porta con sé cosa può fare l'utente.
- **Stato "riconnessione necessaria"**: quando l'errore è di autenticazione (il flag `RequiresReauth` che il client Drive già produce ma la UI oggi ignora), la pagina lo dichiara esplicitamente e offre l'azione "Riconnetti l'account Google", che ripete il consenso OAuth **mantenendo** account, frequenza e cronologia dei backup — non è un "disabilita e riabilita".
- **Segnale di backup non funzionante fuori dalla pagina Backup**: la voce Backup in Impostazioni riflette lo stato di errore, così il problema si nota senza dover entrare nella sezione.
- Lo stato di errore **si azzera da solo** al primo backup riuscito (manuale o schedulato).

## Capabilities

### New Capabilities

Nessuna: la change interviene sul comportamento di una capability esistente.

### Modified Capabilities

- `cloud-backup`: aggiunta della persistenza e della presentazione dell'esito dell'ultimo backup (nuovo requisito), delle categorie di errore comprensibili e dello stato esplicito di riconnessione necessaria con azione dedicata (requisito "Robustezza degli errori di rete e autenticazione" oggi troppo debole: chiede solo di "proporre di ripetere l'autenticazione"); estensione del requisito "Informazioni di stato del backup" perché lo stato mostrato includa anche l'esito, non solo data/dimensione/quota.
- `app-settings`: la voce "Backup su Google Drive" in Impostazioni segnala lo stato di errore, non solo attivo/non attivo.

## Impact

- `src/CardMaster/Services/Backup/BackupModels.cs` — nuova classificazione dell'errore (categoria) e stato dell'ultimo esito.
- `src/CardMaster/Services/Backup/DriveBackupClient.cs` — l'eccezione Drive porta la categoria (401/403 quota/5xx/rete) oltre a `RequiresReauth`.
- `src/CardMaster/Services/Backup/BackupService.cs` — registra l'esito di ogni backup (manuale, all'apertura, schedulato) nello store; azione di riconnessione senza perdita di stato.
- `src/CardMaster/Services/ISettingsStore.cs` + implementazione — nuove preferenze per l'ultimo esito (timestamp, categoria).
- `src/CardMaster/ViewModels/BackupViewModel.cs`, `src/CardMaster/Views/BackupPage.xaml` — banner di stato, messaggi per categoria, pulsante di riconnessione.
- `src/CardMaster/ViewModels/SettingsViewModel.cs`, `src/CardMaster/Views/SettingsPage.xaml` — segnale di errore sulla voce Backup.
- Nessuna nuova dipendenza, nessuna modifica allo schema del database, nessun cambiamento agli scope OAuth richiesti.
