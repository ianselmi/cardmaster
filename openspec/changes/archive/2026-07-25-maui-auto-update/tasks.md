## 1. Repository pubblico (prerequisito)

- [x] 1.1 Rendere pubblico il repository `ianselmi/cardmaster` (Settings → General → Danger Zone → Change visibility) — necessario perché l'app legge le API Releases di GitHub senza token incorporato

## 2. Manifest e permessi Android

- [x] 2.1 Aggiungere il permesso `REQUEST_INSTALL_PACKAGES` in `Platforms/Android/AndroidManifest.xml`
- [x] 2.2 Aggiungere il `provider` `androidx.core.content.FileProvider` (autorità `com.cardmaster.app.fileprovider`) nel manifest, con `resource/xml/file_paths.xml` che espone la cartella cache

## 3. Modello e client della Release GitHub

- [x] 3.1 Creare `Services/Update/UpdateRelease.cs` (record) con i campi necessari (versionName, url APK, dimensione, digest sha256 opzionale)
- [x] 3.2 Creare `IUpdateService`/`UpdateService` in `Services/Update/` con `CheckForUpdateAsync()` che chiama `GET https://api.github.com/repos/ianselmi/cardmaster/releases/tags/latest` tramite l'`HttpClient` condiviso (header `User-Agent` richiesto dalle API GitHub anche in anonimo), deserializza la risposta e individua l'asset `.apk`
- [x] 3.3 Implementare il confronto per uguaglianza tra `name` remoto e `AppInfo.Current.VersionString`
- [x] 3.4 Gestire errori di rete/rate limit/timeout con un risultato tipizzato (non eccezioni non gestite)

## 4. Download e verifica integrità

- [x] 4.1 Implementare `DownloadAsync` in `UpdateService`: scarica l'APK in `FileSystem.CacheDirectory` riportando l'avanzamento sia alla notifica di sistema sia via l'evento `StateChanged` (percentuale da `Content-Length`)
- [x] 4.2 Se l'asset espone un digest `sha256:`, calcolare lo SHA-256 del file scaricato e confrontarlo; in caso di mismatch eliminare il file e restituire un errore (best-effort: se il digest manca, si procede affidandosi alla firma del package installer)
- [x] 4.3 Gestire il download interrotto (eliminare il file parziale, esito d'errore riprovabile)

## 5. Notifica di avanzamento e foreground service

- [x] 5.1 Creare `IUpdateNotifier` (condiviso) e `AndroidUpdateNotifier` in `Platforms/Android/Services/`, con canale notifica dedicato (es. `cardmaster_update`) e progresso determinato
- [x] 5.2 Creare `UpdateDownloadForegroundService` (`ForegroundServiceType=dataSync`) speculare a `BackupForegroundService`, che esegue il download tramite `IUpdateService` e aggiorna la notifica
- [x] 5.3 Registrare i nuovi servizi in `MauiProgram.cs` sotto `#if ANDROID`

## 6. Installazione tramite package installer

- [x] 6.1 Creare `IApkInstaller`/implementazione Android in `Platforms/Android/Services/`: verifica `PackageManager.CanRequestPackageInstalls()`, se assente apre le impostazioni di sistema dedicate
- [x] 6.2 Se il permesso è concesso, costruire l'`Intent.ActionView` con URI da `FileProvider` e MIME `application/vnd.android.package-archive` e avviarlo
- [x] 6.3 Gestire il caso di permesso negato senza crash, mantenendo lo stato riprovabile (nessuna installazione avviata, l'utente può riprovare dalla UI)

## 7. UI Impostazioni

- [x] 7.1 Aggiungere alla pagina Impostazioni un punto di ingresso "Controllo aggiornamenti" che apre una pagina dedicata (`UpdatePage`/`UpdateViewModel`, stesso pattern di `BackupPage`): stato ultimo controllo, azione "Verifica aggiornamenti"
- [x] 7.2 Mostrare, quando disponibile, la versione remota e l'azione "Scarica e installa", con barra di avanzamento durante il download
- [x] 7.3 Mostrare messaggi d'errore chiari per i casi: rete assente, rate limit, checksum non corrispondente, permesso negato, con possibilità di riprovare
- [x] 7.4 Persistere l'esito/timestamp dell'ultimo controllo nello store preferenze esistente (per mostrarlo alla riapertura della sezione)

## 8. Verifica finale

- [x] 8.1 `dotnet build` dell'app in configurazione Debug e Release: 0 errori
- [x] 8.2 Verificato dopo il cambio di visibilità: `GET https://api.github.com/repos/ianselmi/cardmaster/releases/tags/latest` risponde `200` senza autenticazione, con asset APK e `digest` sha256 presenti
- [x] 8.3 Verifica manuale end-to-end su emulatore: "Verifica aggiornamenti" rileva la versione 16 disponibile, "Scarica e installa" mostra il dialog di permesso quando non concesso, dopo la concessione il download completa con avanzamento reale (notifica + in-app), il checksum SHA-256 corrisponde e il package installer di sistema Android si apre correttamente ("Vuoi aggiornare questa app?" con icona e nome CardMaster) — nessun crash in nessuna fase
