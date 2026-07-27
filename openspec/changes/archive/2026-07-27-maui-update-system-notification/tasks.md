## 1. Notifica "nuova versione disponibile"

- [x] 1.1 Estendere `IUpdateNotifier` con `NotifyUpdateAvailable(string version)` e `CancelUpdateAvailable()`; allineare `NoopUpdateNotifier`
- [x] 1.2 `AndroidUpdateNotifier`: costruire la notifica sul canale esistente `cardmaster_update` con un **id distinto** da `ProgressNotificationId`, importanza invariata (Low), `AutoCancel` attivo
- [x] 1.3 Aggiungere il `ContentIntent` che apre l'app sul flusso di aggiornamento (`UpdatePage`), verificando che funzioni sia ad app chiusa sia ad app in background
- [x] 1.4 Verificare che avviare un download non sostituisca né cancelli la notifica di disponibilità (id diversi)

## 2. Emissione legata al punto di verità esistente

- [x] 2.1 In `UpdateService`, emettere la notifica quando un controllo porta `AvailableUpdateVersion` a un valore non nullo e quella versione non è in `UpdateNotifyDismissedVersion`
- [x] 2.2 Riemettere la notifica a ogni controllo che trova l'aggiornamento ancora pendente (nessuna soppressione "una volta per versione")
- [x] 2.3 Cancellare la notifica quando `ReconcileInstalledVersion()` rileva che l'aggiornamento è stato installato
- [x] 2.4 Cancellare la notifica quando l'utente silenzia la versione (`DismissUpdateBanner` / "Ignora questa versione")
- [x] 2.5 Verificare che con l'opzione disattivata non venga emessa alcuna notifica automatica

## 3. Controllo periodico in background

- [x] 3.1 Creare `IUpdateCheckScheduler` (`Schedule()`/`Cancel()`) con implementazione Android su WorkManager e no-op altrove, sul modello di `IBackupScheduler`
- [x] 3.2 `AndroidUpdateCheckScheduler`: `PeriodicWorkRequest` **orario**, vincolo `NetworkType.Connected`, `EnqueueUniquePeriodicWork` con `ExistingPeriodicWorkPolicy.Update` e nome univoco dedicato
- [x] 3.6 Allineare `UpdateService.MinAutoCheckInterval` al periodo del lavoro (1 ora): un minimo più lungo del periodo farebbe girare il worker a vuoto quasi ogni volta
- [x] 3.3 Creare `UpdateCheckWorker` che risolve `IUpdateService` dal container e chiama `CheckForUpdateIfDueAsync` (che applica già l'intervallo minimo e l'opt-in), restituendo `Result.Success` anche in caso di errore di rete
- [x] 3.4 Registrare scheduler e worker nel container DI (`MauiProgram`)
- [x] 3.5 Attivare la schedulazione quando l'opzione viene attivata e annullarla quando viene disattivata; riallineare la schedulazione all'avvio dell'app per chi aveva già l'opzione attiva

## 4. Permesso notifiche e Impostazioni

- [x] 4.1 `UpdateViewModel`: richiedere `Permissions.PostNotifications` all'attivazione dello switch, senza disattivare l'opzione né sopprimere il banner se negato
- [x] 4.2 Alla disattivazione dello switch: annullare il work periodico e cancellare la notifica pendente
- [x] 4.3 Aggiornare il testo descrittivo dello switch in `UpdatePage.xaml` per dichiarare il controllo **ad app chiusa** e la notifica
- [x] 4.4 Verificare che il manifest non richieda permessi nuovi (`POST_NOTIFICATIONS` e `INTERNET` già presenti)

## 5. Verifica

- [x] 5.1 `dotnet build` con 0 errori
- [x] 5.2 Verifica su emulatore: attivando l'opzione viene chiesto il permesso notifiche; con permesso concesso e aggiornamento disponibile compare la notifica
- [x] 5.3 Verifica su emulatore: toccando la notifica si apre il flusso di aggiornamento e la notifica sparisce
- [x] 5.4 Verifica su emulatore: notifica ripetuta al controllo successivo; nessuna notifica dopo aver silenziato la versione; nessuna dopo aver installato l'aggiornamento
- [x] 5.5 Verifica del controllo in background forzando l'esecuzione del worker con `adb shell cmd jobscheduler run -f com.cardmaster.app <jobId>` (o equivalente), ad app chiusa
- [x] 5.6 Verifica che disattivando l'opzione il work periodico venga annullato (`adb shell dumpsys jobscheduler` non elenca più il job) e la notifica pendente rimossa
- [x] 5.7 Verifica di non-regressione: con l'opzione disattivata nessun controllo di rete automatico e nessun job pianificato
- [x] 5.8 Rivedere il diff prima del commit (repository pubblico) e aggiornare `PLAN.md` con la voce della change
