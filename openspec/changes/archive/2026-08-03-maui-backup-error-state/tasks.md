## 1. Modello dell'errore

- [x] 1.1 Aggiungere `enum BackupErrorKind { None, Network, StorageFull, ReauthRequired, Service, Local }` in `Services/Backup/BackupModels.cs`
- [x] 1.2 Aggiungere `BackupErrorKind Kind` a `BackupResult` e `RestoreResult`, mantenendo `ErrorMessage` come dettaglio diagnostico non mostrato in primo piano
- [x] 1.3 Sostituire `DriveBackupException.RequiresReauth` con `BackupErrorKind Kind` (unica fonte di verità) e aggiornare i punti che la costruiscono in `DriveBackupClient`

## 2. Distinguere rete e credenziali in GoogleAuth

- [x] 2.1 Aggiungere `enum TokenFailure { None, NoAccount, Rejected, Network }` e il record `AccessTokenResult(string? Token, TokenFailure Failure)`
- [x] 2.2 Cambiare `IGoogleAuth.GetValidAccessTokenAsync` per restituire `AccessTokenResult` e aggiornare la documentazione XML dell'interfaccia
- [x] 2.3 In `GoogleAuth`: `PostTokenAsync` distingue rifiuto del server (400/401, es. `invalid_grant`) da errore di trasporto; `GetValidAccessTokenAsync` mappa i tre casi (`NoAccount` senza refresh token salvato, `Rejected` su rifiuto, `Network` su `HttpRequestException`)

## 3. Classificazione degli errori Drive

- [x] 3.1 Aggiungere al `BackupJsonContext` (source-gen, no reflection) il DTO per il payload d'errore Google (`error.code`, `error.errors[].reason`)
- [x] 3.2 In `DriveBackupClient.EnsureSuccessAsync`: classificare 403 `storageQuotaExceeded` → `StorageFull`, 403 di rate limit / 429 / 5xx / altri 4xx → `Service`, con fallback a `Service` se il corpo non è il JSON atteso; il messaggio tecnico resta solo come dettaglio interno all'eccezione
- [x] 3.3 In `DriveBackupClient.SendAsync`: `HttpRequestException`/timeout → `Network`; token non ottenibile → `ReauthRequired` solo per `TokenFailure.NoAccount`/`Rejected`, altrimenti `Network`; 401 dopo retry → `ReauthRequired`
- [x] 3.4 Verificare che il ramo di `GetStorageQuotaAsync` che tratta 403/404 come "quota non leggibile con scope appdata" resti prima della classificazione e continui a restituire `null` senza generare un errore `StorageFull`

## 4. Persistenza dell'esito

- [x] 4.1 Aggiungere a `ISettingsStore` (e all'implementazione su `Preferences`) `DateTimeOffset? LastBackupAttemptUtc` e `BackupErrorKind LastBackupError`, serializzando l'enum come stringa; valori assenti = nessun errore noto
- [x] 4.2 In `BackupService.BackupNowAsync`: registrare l'esito di **ogni** tentativo (successo → `LastBackupError = None` + `LastBackupAttemptUtc`; fallimento → categoria + timestamp), lasciando `LastBackupUtc`/`LastBackupSize` come ultimo successo
- [x] 4.3 Mappare gli errori locali (`IOException` dello snapshot, fallimento di `SnapshotAsync`) su `BackupErrorKind.Local`
- [x] 4.4 In `BackupService.RefreshQuotaAsync`: registrare `LastBackupError = ReauthRequired` quando l'errore è di credenziali (senza toccare `LastBackupAttemptUtc`), continuando a ignorare le altre categorie
- [x] 4.5 In `BackupService.RestoreAsync`: propagare la categoria in `RestoreResult` e registrare lo stato di riconnessione necessaria solo per `ReauthRequired`
- [x] 4.6 Esporre su `IBackupService` lo stato osservabile dalla UI: `BackupErrorKind LastError`, `DateTimeOffset? LastAttemptUtc`

## 5. Riconnessione dell'account

- [x] 5.1 Aggiungere `Task<bool> ReconnectAsync(CancellationToken)` a `IBackupService`: chiama `SignInAsync` senza `SignOutAsync`, non tocca `BackupEnabled`, frequenza, schedulazione né cache
- [x] 5.2 Su riconnessione riuscita azzerare `LastBackupError`; su annullamento/fallimento lasciare invariati stato e configurazione

## 6. Pagina Backup

- [x] 6.1 In `BackupViewModel`: esporre `BackupHealth { Ok, Failed, ReauthRequired }`, `StatusTitle`, `StatusDetail`, `IsStatusBannerVisible`, `IsReconnectVisible` e `ReconnectCommand`
- [x] 6.2 Scrivere i testi per categoria (rete, spazio esaurito, credenziali, servizio, errore locale) con "cosa è successo" + "cosa fare"; nessun codice HTTP o payload JSON nei testi mostrati
- [x] 6.3 Mostrare l'ultimo tentativo fallito distinto dall'ultimo backup riuscito (entrambi visibili, senza far credere che i dati siano aggiornati)
- [x] 6.4 In `BackupPage.xaml`: banner di stato sopra i dati dell'account, visibile solo quando `Health != Ok`, con pulsante "Riconnetti l'account Google" solo in `ReauthRequired`; riusare `#B00020` già presente in pagina
- [x] 6.5 Sostituire nei messaggi di alert di backup/ripristino l'uso di `ErrorMessage` grezzo con i testi per categoria
- [x] 6.6 Aggiornare lo stato mostrato dopo backup, riconnessione e `RefreshAsync` (notifica delle proprietà interessate)

## 7. Segnale in Impostazioni

- [x] 7.1 In `SettingsViewModel`: sostituire `IsBackupEnabled` (bool) con uno stato a tre valori e aggiornare `BackupStatusText` ("Backup non attivo" / "Backup attivo" / "Backup da riconnettere" / "Ultimo backup non riuscito")
- [x] 7.2 Estendere `BackupStatusToColorConverter` al caso "attivo ma in errore" e aggiornare il binding in `SettingsPage.xaml`
- [x] 7.3 Verificare che il segnale si aggiorni al ritorno dalla pagina Backup (backup riuscito → il segnale di problema sparisce)

## 8. Verifica

- [x] 8.1 `dotnet build` senza errori (criterio di accettazione, vedi `PLAN.md`)
- [x] 8.2 Prova su emulatore: verificata la **persistenza** dell'esito fallito (banner presente dopo la chiusura dell'alert e dopo il riavvio dell'app, con "ultimo tentativo" distinto da "ultimo backup riuscito"). Categoria esercitata dal vivo: credenziali assenti, non `Network` — senza un account collegato la modalita aereo non arriva mai alla chiamata di rete.
- [ ] 8.3 Prova su emulatore con revoca dell'accesso dall'account Google — **non eseguibile senza un account Google reale**. Verificato quanto possibile: stato "riconnessione necessaria" raggiunto dal percorso d'errore reale (backup senza credenziali valide), banner e pulsante "Riconnetti l'account Google" mostrati, messaggio comprensibile senza JSON. **Da fare a mano dall'utente**: completare il consenso OAuth e confermare che frequenza e cronologia dei backup restino intatte.
- [x] 8.4 Verificare il segnale in Impostazioni nei tre stati e il ritorno a "attivo" dopo un backup riuscito
- [x] 8.5 Aggiornare `PLAN.md` con la change e rivedere il diff prima del commit (repo pubblico: nessun segreto, nessun dato personale — attenzione a screenshot/log con email dell'account Google)
