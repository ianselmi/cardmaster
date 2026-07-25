## 1. Configurazione OAuth e documentazione

- [ ] 1.1 Creare l'OAuth client di tipo Android su Google Cloud (package name + SHA-1 debug e release), abilitare la Drive API e configurare il consent screen con gli scope `drive.appdata` e `openid email`
- [x] 1.2 Documentare in `docs/` il setup OAuth (client id, SHA-1 legati al keystore CI, publishing status del consent screen) e i caveat di background (Doze / battery-killer OEM, schedulazione best-effort)
- [x] 1.3 Aggiungere il client id come costante di configurazione dell'app (non è un secret con PKCE) e il redirect URI custom-scheme

## 2. Permessi e manifest Android

- [x] 2.1 Aggiungere al manifest `INTERNET`, `POST_NOTIFICATIONS`, `FOREGROUND_SERVICE`, `FOREGROUND_SERVICE_DATA_SYNC`
- [x] 2.2 Registrare il custom-scheme intent filter per il callback di `WebAuthenticator`
- [x] 2.3 Richiedere a runtime `POST_NOTIFICATIONS` (Android 13+) in fase di abilitazione del backup

## 3. Autenticazione Google (IGoogleAuth)

- [x] 3.1 Definire `IGoogleAuth` (SignInAsync, SignOutAsync, GetValidAccessTokenAsync, AccountEmail) e registrarla nel DI
- [x] 3.2 Implementare il flusso Authorization Code + PKCE con `WebAuthenticator` (code_verifier/challenge, scambio code→token via `HttpClient`)
- [x] 3.3 Persistere il `refresh_token` in `SecureStorage`; tenere l'`access_token` in memoria con refresh silenzioso on-demand e su 401
- [x] 3.4 Implementare `SignOutAsync`: revoca token (`oauth2/revoke`) e cancellazione credenziali locali
- [x] 3.5 Gestire annullamento consenso e assenza rete senza eccezioni verso il chiamante

## 4. Client Google Drive (IDriveBackupClient)

- [x] 4.1 Definire `IDriveBackupClient` (GetStorageQuotaAsync, ListBackupsAsync, UploadBackupAsync, DownloadBackupAsync, DeleteBackupAsync) e i modelli con `System.Text.Json` source-gen (no reflection)
- [x] 4.2 Implementare `about.get?fields=storageQuota` (gestire `limit` assente = illimitato); se lo scope `appdata` non basta, degradare senza quota
- [x] 4.3 Implementare `files.list` su `spaces=appDataFolder` ordinato per `modifiedTime desc` con `fields` id/name/modifiedTime/size
- [x] 4.4 Implementare upload multipart con `parents:["appDataFolder"]`
- [x] 4.5 Implementare `files.get?alt=media` (download) e `files.delete`
- [x] 4.6 Applicare a tutte le chiamate: retry su 401 con refresh token, gestione errori di rete/servizio senza crash

## 5. Snapshot e restore del database (DatabaseService)

- [x] 5.1 Aggiungere a `DatabaseService`/`IDatabaseService` `SnapshotAsync(destPath)` con `VACUUM INTO` verso un file nella cache app-privata
- [x] 5.2 Aggiungere `CloseAsync()` che chiude e azzera la connessione singleton in modo atomico (thread-safe col gate esistente)
- [x] 5.3 Implementare lo swap del file `cardmaster.db3` + riapertura lazy (schema/migrazione come all'avvio)
- [x] 5.4 Implementare la guardia di versione schema dal nome file (rifiuto downgrade da backup più recente)

## 6. Orchestrazione backup/restore (BackupService)

- [x] 6.1 Implementare `BackupNowAsync`: snapshot → nome `cardmaster-<utc>-v<schema>.db3` → upload → aggiornamento cache stato
- [x] 6.2 Implementare la ritenzione: dopo l'upload, mantenere al massimo 3 backup (`files.delete` dei più vecchi)
- [x] 6.3 Implementare `RestoreAsync(backupId)`: download → guardia versione → conferma → **snapshot di sicurezza del DB corrente in cache** → `CloseAsync` → swap → riapertura → ricarica dati
- [x] 6.4 Implementare l'undo del ripristino (ripristino dallo snapshot di sicurezza) su errore a metà o su richiesta immediata dell'utente
- [x] 6.5 Implementare lettura/aggiornamento dello stato locale (abilitato, email, frequenza, cache last-backup e quota) via lo store preferenze; garantire **un solo account collegato alla volta** (cambio account = disconnetti + riconnetti)

## 7. Schedulazione (IBackupScheduler)

- [x] 7.1 Definire `IBackupScheduler` (Schedule(frequency), Cancel) cross-platform, no-op fuori Android
- [x] 7.2 Implementare in `Platforms/Android` un WorkManager periodic worker con constraint di rete connessa per Giornaliero/Settimanale
- [x] 7.3 Implementare il trigger "A ogni apertura" all'avvio dell'app (con backup abilitato e rete disponibile)
- [x] 7.4 Annullare la schedulazione su "Mai" e alla disabilitazione del backup

## 8. Notifica di avanzamento (foreground service)

- [x] 8.1 Creare il canale notifiche e un foreground service Android (type dataSync) che avvolge l'esecuzione del backup
- [x] 8.2 Mostrare la notifica "Backup in corso…" e una notifica finale di esito (completato/fallito), per backup manuali e schedulati

## 9. UI — Sezione Backup nelle Impostazioni

- [x] 9.1 Creare `BackupViewModel` (stato, account, ultimo backup, spazio, frequenza; comandi abilita/disabilita/backup ora/ripristina)
- [x] 9.2 Creare la pagina/sezione "Backup su Google Drive" raggiungibile dalle Impostazioni e registrarla nella navigazione Shell + DI
- [x] 9.3 Mostrare stato disabilitato di default, e in stato abilitato: account, data/dimensione ultimo backup, spazio (o "illimitato"), selettore frequenza
- [x] 9.4 Implementare la UI di ripristino: lista in-app dei backup (data/dimensione) + conferma distruttiva prima del replace
- [x] 9.5 Mostrare i valori dalla cache offline senza errori bloccanti quando manca la rete

## 10. Test e verifica

- [ ] 10.1 Test unitari su `IDriveBackupClient` (parsing risposte, quota illimitata, retry 401) e sulla logica di ritenzione (≤3) con fake dell'auth/HTTP
- [ ] 10.2 Test unitari sulla guardia di versione schema, sul percorso di restore e sull'undo dallo snapshot di sicurezza (con `IDatabaseService` fittizio)
- [x] 10.3 Aggiornare `PLAN.md` per il reframe v1 (rimuovere `maui-backup-local`, spostare il backup Drive in v1, adeguare il wording "100% offline")
- [x] 10.4 `dotnet build` con 0 errori (criterio di accettazione obbligatorio)
