## Context

L'app è distribuita fuori dal Play Store: `ci-release` pubblica a ogni push su `main` una Release GitHub taggata col numero di build (versionName = `ApplicationDisplayVersion`) **e** aggiorna in-place la prerelease col tag fisso `latest`, che punta sempre all'ultima build disponibile con lo stesso nome. Questa change aggiunge, lato client, il controllo di questa Release, il download dell'APK e l'avvio dell'installazione.

Il repository (`ianselmi/cardmaster`) era inizialmente privato, il che avrebbe impedito l'accesso anonimo alle API Releases di GitHub e al download diretto degli asset (richiedono autenticazione su repo privato; un token incorporato nell'app era escluso a priori — un APK è decompilabile). Le alternative valutate (GitHub Pages, un repository separato pubblico solo per le release) introducevano complessità non necessaria (GitHub Pages per repo privati è a pagamento; un repo separato richiede un PAT gestito in CI). **Decisione 25 lug 2026: il repository passa da privato a pubblico** (verificato che repo e cronologia non contengono segreti — vedi `proposal.md`), eliminando il problema alla radice: l'app interroga direttamente le API Releases pubbliche di GitHub, senza infrastruttura aggiuntiva.

Pattern esistenti da riusare (rilevati in `Services/Backup/`):
- `HttpClient` condiviso, registrato singleton in `MauiProgram.cs`, iniettato in servizi con pattern interfaccia/implementazione (es. `IDriveBackupClient`/`DriveBackupClient`).
- Notifica di avanzamento stile WhatsApp tramite coppia **notifier + foreground service Android** (`IBackupNotifier`/`AndroidBackupNotifier` + `BackupForegroundService`, `ForegroundServiceType=dataSync`), con fallback Noop su piattaforme non Android, registrazione condizionale `#if ANDROID` in `MauiProgram.cs`.
- Versione mostrata oggi in Impostazioni via `AppInfo.Current.VersionString`/`BuildString` (`SettingsViewModel.cs`), nessun wrapper custom.
- Permessi runtime richiesti inline con `Permissions.RequestAsync<T>()` di MAUI Essentials (es. `Permissions.Camera`, `Permissions.PostNotifications`), nessuna astrazione dedicata.

## Goals / Non-Goals

**Goals:**
- Controllare, su azione utente dalle Impostazioni, se esiste una build più recente della Release `latest` su GitHub.
- Scaricare l'APK con avanzamento visibile (in-app e via notifica), verificarne l'integrità quando possibile, e avviare l'installazione tramite il package installer di sistema.
- Isolare la funzione dietro un'interfaccia (`IUpdateService`), coerente col vincolo "solo un tassello online opt-in": nessun controllo automatico in background, nessuna chiamata di rete senza interazione esplicita dell'utente.

**Non-Goals:**
- Nessuna modifica alla pipeline `ci-release`: si riusa la Release `latest` già pubblicata, nessun nuovo artifact/manifest da generare in CI.
- Nessun controllo periodico/automatico all'avvio in questa change (può essere una preferenza futura, non richiesta ora).
- Nessuna gestione di canali multipli (beta/stable): un solo canale, la Release `latest`.
- Nessun supporto ad altre piattaforme oltre Android (l'intera app è Android-only).

## Decisions

### Sorgente della versione: Release GitHub con tag `latest`
`GET https://api.github.com/repos/ianselmi/cardmaster/releases/tags/latest`, senza autenticazione (repo pubblico, rate limit anonimo di 60 richieste/ora per IP più che sufficiente per controlli manuali). Dalla risposta si usano `name` (= versionName remoto) e l'asset con nome che termina in `.apk` (`assets[].browser_download_url`, `assets[].size`, `assets[].digest` se presente). Le API GitHub richiedono un header `User-Agent` anche per richieste anonime: impostato esplicitamente nella richiesta.

Alternativa scartata: endpoint `/releases/latest` (restituisce la release stabile non-prerelease più recente per data di pubblicazione) — non riflette le build continue su `main`, che sono marcate `prerelease: true`; il tag fisso `latest` è stato costruito apposta in `ci-release` per questo scopo.

Alternativa scartata: manifest `latest.json` dedicato pubblicato su GitHub Pages — valutata quando il repository era ancora privato, poi abbandonata insieme alla scelta di rendere pubblico il repository (vedi Context): l'oggetto Release restituito dalla API di GitHub contiene già tutte le informazioni necessarie (nome versione, URL asset, opzionalmente digest), senza bisogno di un passo CI aggiuntivo.

### Confronto versione: uguaglianza, non ordinamento
Il sistema considera disponibile un aggiornamento quando `name` della Release remota è **diverso** (confronto ordinale) dalla versione installata (`AppInfo.Current.VersionString`), senza tentare di stabilire un ordine "maggiore di". Motivazione: il tag `latest` è per costruzione sempre allineato all'ultima build pubblicata su `main` (vedi `ci-release`), quindi "diverso" implica già "più recente" nel flusso normale; evita la fragilità di parsare/confrontare numericamente formati eterogenei (numero di build incrementale per le build da `main` vs tag `v*` in stile semver per le release stabili). Limite noto: se l'utente avesse installato manualmente una build più recente della `latest` (scenario anomalo, non previsto dal flusso di distribuzione), l'app proporrebbe comunque un "aggiornamento" che di fatto sarebbe un downgrade — l'utente vede comunque il numero di versione proposto prima di confermare.

### Verifica di integrità: checksum best-effort + firma del package installer come garanzia primaria
Dopo il download, se l'asset API espone `digest` (`sha256:<hex>`), si calcola lo SHA-256 del file scaricato e si confronta; in caso di mismatch il file viene scartato e l'installazione non parte. Se `digest` non è presente nella risposta, si salta il controllo checksum e si procede: la garanzia di integrità primaria resta comunque la **verifica di firma del package installer di Android**, che rifiuta l'installazione di un APK non firmato con lo stesso certificato dell'app già installata (stesso keystore CI usato da `ci-release`). Questo rende il checksum una difesa aggiuntiva (fallisce prima, con un messaggio più chiaro, in caso di download corrotto/manomesso) e non l'unico presidio.

### Download e installazione: `FileProvider` + intent `ACTION_VIEW`
L'APK viene scaricato nella cache dell'app (`FileSystem.CacheDirectory`) e installato tramite `Intent.ActionView` con MIME `application/vnd.android.package-archive`, esponendo il file via un nuovo `androidx.core.content.FileProvider` (autorità `com.cardmaster.app.fileprovider`, non esistente oggi — nessun altro flusso dell'app condivide file). Richiede il permesso `REQUEST_INSTALL_PACKAGES` nel manifest e, su Android 8+, il consenso esplicito "Installa app sconosciute" concesso dall'utente per l'app (verificato con `PackageManager.CanRequestPackageInstalls()` prima di avviare l'intent; se assente, si apre le impostazioni di sistema dedicate).

### Notifica di avanzamento: nuova coppia notifier + foreground service, sul modello del backup
Si introduce `IUpdateNotifier`/`AndroidUpdateNotifier` (canale di notifica dedicato, es. `cardmaster_update`) e `UpdateDownloadForegroundService` (`ForegroundServiceType=dataSync`), speculari a `IBackupNotifier`/`AndroidBackupNotifier`/`BackupForegroundService` ma con progresso **determinato** (percentuale nota dal `Content-Length` della risposta HTTP, a differenza del backup che è indeterminato). Motivazione: riusa un pattern già validato in produzione per download di rete che devono sopravvivere a un eventuale passaggio in background dell'app, invece di introdurre un meccanismo nuovo; si duplica codice minimo anziché forzare un'astrazione condivisa prematura tra due feature diverse (backup vs update).

### Struttura del servizio
`IUpdateService` (in `Services/Update/`, condiviso) espone `CheckForUpdateAsync()` → esito (nessun aggiornamento / versione disponibile con dati dalla Release) e `DownloadAsync(...)`, con avanzamento riportato sia alla notifica di sistema sia via l'evento `StateChanged` (osservato dalla UI mentre la pagina è visibile). L'installazione vera e propria (intent Android, `FileProvider`, controllo `CanRequestPackageInstalls`) è isolata dietro `IApkInstaller`, con implementazione Android in `Platforms/Android/Services/` e registrazione condizionale `#if ANDROID` in `MauiProgram.cs` (nessun Noop necessario: l'intera app è Android-only, ma si segue comunque la convenzione del progetto per coerenza con `IBackupNotifier` ecc.).

## Risks / Trade-offs

- **[Rischio] Il confronto per uguaglianza non rileva un "downgrade" se l'utente ha installato manualmente una build più recente della `latest`** → Mitigazione: scenario non previsto dal flusso di distribuzione normale (l'unica fonte di installazione documentata è la Release GitHub); l'utente vede comunque il numero di versione proposto e conferma esplicitamente prima di installare.
- **[Rischio] Rate limit anonimo delle API GitHub (60/h per IP)** → Mitigazione: il controllo è manuale (azione utente), frequenza d'uso trascurabile rispetto al limite; in caso di rate limit si mostra un errore chiaro con invito a riprovare più tardi.
- **[Rischio] `digest` sha256 potrebbe non essere presente nella risposta API per asset caricati prima dell'introduzione di quel campo o in determinate condizioni GitHub** → Mitigazione: checksum trattato come best-effort, non bloccante; la firma verificata dal package installer resta comunque garanzia sufficiente.
- **[Rischio] Utente nega il permesso "Installa app sconosciute" o `POST_NOTIFICATIONS`** → Mitigazione: messaggi che spiegano perché il permesso serve e collegamento diretto alle impostazioni di sistema; il download funziona comunque, l'installazione resta bloccata finché il permesso non viene concesso (nessun crash).
- **[Trade-off] Duplicazione del pattern notifier/foreground-service invece di generalizzarlo tra backup e update** → Accettato: le due feature hanno owner concettuali diversi (backup vs update) e il codice duplicato è minimo; una condivisione prematura rischierebbe un accoppiamento non necessario.
- **[Rischio] Il repository essendo ora pubblico espone codice sorgente e cronologia a chiunque** → Accettato consapevolmente (vedi Context/decisione 25 lug 2026): nessun segreto presente; da qui in avanti va mantenuta la disciplina di revisionare ogni commit/push per evitare di introdurne (vedi `PLAN.md`).

## Migration Plan

Nessuna migrazione dati. Change puramente additiva: nuovo permesso manifest (`REQUEST_INSTALL_PACKAGES`), nuovo `provider` `FileProvider` nel manifest con relativo `file_paths.xml`, nuovi servizi registrati in DI, nuova sezione UI in Impostazioni. Nessuna modifica a schema DB, nessun impatto sulle feature esistenti. Il cambio di visibilità del repository (privato → pubblico) è un'azione GitHub separata, fuori dal codice, senza rollback automatico. Rollback (lato codice): rimuovere la sezione UI e i servizi (nessuno stato persistente introdotto oltre a un eventuale timestamp "ultimo controllo" nelle preferenze).

## Open Questions

- Nessuna aperta: le decisioni sopra coprono il flusso end-to-end richiesto dalla proposal.
