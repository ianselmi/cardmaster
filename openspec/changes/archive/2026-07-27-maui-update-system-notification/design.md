## Context

Stato attuale rilevante:

- `AndroidUpdateNotifier` (`Platforms/Android/Services/`) possiede già il canale `cardmaster_update` (importanza **Low**) e due notifiche: avanzamento del download (`ProgressNotificationId = 4301`) ed esito. Nessuna delle due ha un `ContentIntent`: toccarle non porta da nessuna parte.
- `IUpdateNotifier` espone solo `NotifyProgress`/`NotifyResult`; l'implementazione non-Android è `NoopUpdateNotifier`.
- `UpdateService.CheckForUpdateIfDueAsync` è già il punto in cui "controlla se dovuto": esce se `UpdateNotifyEnabled` è falso o se non è passato l'intervallo minimo da `LastUpdateCheckUtc` (oggi 24 ore, `MinAutoCheckInterval`). Oggi è chiamato solo da `App.OnStart`/`OnResume`.
- `AndroidBackupScheduler` + `BackupWorker` sono un precedente completo di lavoro periodico: `PeriodicWorkRequest` con `NetworkType.Connected`, `EnqueueUniquePeriodicWork` con `ExistingPeriodicWorkPolicy.Update`, e `CancelUniqueWork` per spegnerlo.
- `POST_NOTIFICATIONS` è dichiarato nel manifest ma richiesto a runtime **solo** in `BackupViewModel` quando si abilita il backup Drive.
- `maui-update-stale-signal` ha appena introdotto `ReconcileInstalledVersion()` e `AvailableUpdateVersion` (già filtrata sulla versione installata): sono il punto di verità da cui deve dipendere anche la notifica.

Vincolo di prodotto da rispettare: con l'opzione disattivata — che è il **default** — l'app non deve fare nulla in rete se non su richiesta esplicita.

## Goals / Non-Goals

**Goals:**

- L'utente scopre un aggiornamento senza dover aprire l'app.
- Un solo interruttore governa tutto il comportamento "avvisami": in-app e notifica.
- Con l'opzione disattivata, nessun lavoro periodico e nessuna rete: identico a oggi.
- Riuso dell'infrastruttura esistente (canale notifiche, WorkManager, client di controllo): nessuna dipendenza nuova.

**Non-Goals:**

- Scaricare o installare l'aggiornamento automaticamente: restano azioni esplicite dell'utente.
- Notifiche per eventi diversi dall'aggiornamento.
- Supporto notifiche su piattaforme diverse da Android (resta `NoopUpdateNotifier`).
- Cambiare il criterio di confronto tra versioni o la riconciliazione introdotta da `maui-update-stale-signal`.

## Decisions

### 1. Lavoro periodico orario con WorkManager, non un servizio sempre attivo

Il controllo in background è un `PeriodicWorkRequest` **orario** con vincolo `NetworkType.Connected`, registrato come *unique work* e annullato quando l'opzione viene disattivata — esattamente il modello di `AndroidBackupScheduler`, che è già in produzione e di cui conosciamo il comportamento.

Il periodo e l'**intervallo minimo tra due controlli** devono restare **allineati**: il worker chiama `CheckForUpdateIfDueAsync`, che applica il minimo, quindi un minimo più lungo del periodo farebbe girare il lavoro a vuoto quasi ogni volta. Per questo il minimo passa da 24 ore a 1 ora insieme al periodo. Resta un solo guardiano per tutte le strade (foreground e background): se un controllo è appena avvenuto, il worker non ripete la richiesta di rete.

Alternative scartate:
- *Foreground service o allarme esatto*: sproporzionato per un controllo che può slittare senza danno, e su Android moderno costerebbe permessi aggiuntivi.
- *Push da server*: non esiste un backend in v1, ed è esattamente ciò che l'architettura evita.
- *Periodo orario con minimo lasciato a 24 ore*: il worker si sveglierebbe 24 volte al giorno per non fare nulla 23 volte — il costo in batteria senza il beneficio.

### 2. La notifica nasce dallo stesso punto di verità del banner

La notifica viene emessa quando un controllo — in foreground o dal worker — porta `AvailableUpdateVersion` a un valore non nullo e quella versione non è stata silenziata dall'utente. Non c'è una seconda logica "c'è un aggiornamento?": banner, badge e notifica leggono la stessa proprietà, introdotta da `maui-update-stale-signal`.

Ne discende gratis il comportamento corretto sui casi limite: se l'utente installa l'aggiornamento, la riconciliazione azzera lo stato e la notifica pendente va rimossa; se la versione è già stata silenziata, non viene emessa.

### 3. Il silenziamento per versione vale anche per la notifica

"Ripetere la notifica a ogni controllo" è stato interpretato come *finché l'utente non ha silenziato esplicitamente quella versione*. Altrimenti il comando "chiudi il segnale" — che la spec già prevede — non avrebbe alcun effetto sul canale più invadente dei due, e chi ha deciso di ignorare la versione N se la ritroverebbe ogni giorno.

Quindi: nessun silenziamento → la notifica viene riemessa a ogni controllo che trova l'aggiornamento ancora non installato; silenziamento della versione N → niente notifica per la N, ma la N+1 torna a notificare.

### 4. La notifica porta dove serve, con un id proprio

La notifica "nuova versione disponibile" usa il canale esistente `cardmaster_update` ma un **id distinto** da quello del download (`4301`): altrimenti l'avvio di un download sostituirebbe la notifica di disponibilità, o viceversa. Ha un `ContentIntent` che apre l'app sul flusso di aggiornamento — la prima notifica dell'app che porta da qualche parte — ed è `AutoCancel`, così scompare al tocco.

Importanza del canale: resta **Low**. Un aggiornamento disponibile non è un evento che deve interrompere l'utente con suono e heads-up; deve solo essere lì quando guarda il pannello.

### 5. Il permesso si chiede quando si attiva l'opzione, non al primo invio

`POST_NOTIFICATIONS` (Android 13+) viene richiesto contestualmente all'attivazione dello switch, come già fa il backup Drive alla sua abilitazione. Chiederlo al momento del primo invio significherebbe chiederlo da un worker in background, dove non c'è un'Activity per mostrare il dialogo: la richiesta fallirebbe silenziosamente e l'utente non vedrebbe mai una notifica pur avendo attivato l'opzione.

Se l'utente nega il permesso, l'opzione resta comunque attiva e il **segnale in-app continua a funzionare**: la notifica è un canale in più, non l'unico, e disattivare l'opzione per un permesso negato toglierebbe anche ciò che l'utente aveva già.

### 6. Spegnere l'opzione ripulisce tutto

Disattivando "Avvisami di nuove versioni": il work periodico viene annullato e la notifica di disponibilità eventualmente pendente rimossa. Un lavoro periodico orfano continuerebbe a consumare rete per un'opzione che l'utente crede spenta — il caso peggiore per la fiducia in un'app che si dichiara offline-first.

## Risks / Trade-offs

- **Si ribalta un requisito esistente** (`app-update-notify`: "il sistema NON MUST effettuare controlli mentre l'app è in background o chiusa") → è la decisione del 27 lug 2026, presa sapendo che senza background la notifica sarebbe arrivata solo ad app aperta. Il delta spec la riscrive esplicitamente invece di lasciarla in contraddizione con il codice.
- **Fino a una richiesta di rete all'ora anche quando l'utente non apre l'app** → vincolata a rete connessa, su un endpoint pubblico e senza dati inviati oltre allo User-Agent. Con l'opzione disattivata (default) resta zero. È il punto più caro di questa change per un'app che si presenta come offline-first, ed è il motivo per cui la descrizione dello switch dichiara esplicitamente il controllo ad app chiusa: l'utente deve sapere cosa sta accendendo.
- **WorkManager non garantisce la puntualità**: il sistema può rimandare o raggruppare il lavoro (Doze, batteria bassa, batching). Nella pratica le esecuzioni reali saranno meno di una all'ora, e non si può promettere una cadenza precisa. Accettabile: un aggiornamento scoperto più tardi non cambia nulla.
- **Notifica ripetuta a ogni controllo** → con periodo orario può voler dire più notifiche al giorno per la stessa versione, finché non si aggiorna o non si silenzia. È la combinazione delle due scelte esplicite dell'utente (ripetizione + periodo orario), e il silenziamento per versione resta l'unica valvola di sfogo: vale la pena rivalutarla se all'uso risultasse molesta.
- **Permesso negato su Android 13+** → l'opzione resta attiva e il banner continua a funzionare, ma l'utente potrebbe credere di ricevere notifiche che non arriveranno. Mitigazione minima: non ripetere la richiesta a ogni attivazione se già negata dal sistema.

## Migration Plan

Nessuna migrazione dati. Chi ha già attivato "Avvisami di nuove versioni" si ritrova, al primo avvio dopo l'aggiornamento, il lavoro periodico registrato e — se il permesso notifiche è già concesso — le notifiche attive: è l'estensione della scelta che aveva già fatto. Chi non l'ha attivata non vede alcun cambiamento.

Rollback: disattivare l'opzione annulla il work periodico; reinstallare una versione precedente lascerebbe un unique work orfano registrato, che però non troverebbe più il worker e verrebbe scartato dal sistema.

## Open Questions

Nessuna bloccante. Resta da confermare in revisione l'interpretazione della §3 (il silenziamento per versione vale anche per la notifica), segnalata anche nella proposta.
