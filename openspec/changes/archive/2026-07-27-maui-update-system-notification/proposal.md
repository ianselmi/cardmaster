## Why

Oggi la disponibilità di un aggiornamento si scopre solo **aprendo l'app**: banner in lista carte e badge sulle Impostazioni. Chi apre CardMaster una volta ogni tanto — cioè l'uso tipico di un'app di carte fedeltà, che si apre alla cassa — può restare indietro di parecchie versioni senza accorgersene, perché il segnale esiste solo nel momento in cui l'app è già in mano.

Una notifica di sistema è il canale giusto per un evento che l'utente deve conoscere *senza* essere nell'app. L'infrastruttura c'è già: canale notifiche e `POST_NOTIFICATIONS` sono usati dal download aggiornamenti, e il lavoro periodico in background con WorkManager è già in produzione per il backup Drive.

## What Changes

- **Notifica di sistema quando un controllo rileva una nuova versione**, in aggiunta a banner e badge. Toccarla apre l'app direttamente sul flusso di aggiornamento.
- **Il controllo automatico può avvenire ad app chiusa**, come lavoro periodico **orario** con WorkManager e vincolo di rete connessa — sullo stesso modello del backup Drive. **BREAKING rispetto alla spec corrente**: `app-update-notify` oggi vieta esplicitamente i controlli fuori dal foreground. *(Decisione 27 lug 2026: senza controllo in background la notifica arriverebbe solo mentre l'app è già aperta, cioè quando non serve.)*
- **L'intervallo minimo tra due controlli passa da 24 ore a 1 ora**, per stare allineato al periodo del lavoro pianificato: un minimo più lungo del periodo farebbe girare il worker a vuoto quasi ogni volta. *(Decisione 27 lug 2026.)*
- **Nessuna nuova opzione**: la notifica è governata dallo switch esistente **"Avvisami di nuove versioni"**, che da questa change abilita segnale in-app *e* notifica di sistema. *(Decisione 27 lug 2026: una sola scelta per l'utente.)*
- **Il permesso notifiche viene richiesto all'attivazione dell'opzione.** Oggi `POST_NOTIFICATIONS` è richiesto solo abilitando il backup Drive: chi non usa il backup, su Android 13+, non riceverebbe alcuna notifica senza accorgersene.
- **La notifica è ripetuta a ogni controllo** finché l'aggiornamento non è installato, invece di essere emessa una volta sola. *(Decisione 27 lug 2026.)* Resta però soggetta al silenziamento esplicito per versione già previsto: se l'utente ha chiuso il segnale per la versione N, la notifica per la N non viene più emessa.
- **Disattivando l'opzione** il lavoro periodico viene annullato e le notifiche pendenti rimosse: nessun residuo che continui a consumare rete o a comparire.

Fuori scope: notifiche per eventi diversi dall'aggiornamento, download automatico dell'aggiornamento in background (resta un'azione dell'utente), notifiche su piattaforme diverse da Android.

## Capabilities

### New Capabilities
Nessuna.

### Modified Capabilities
- `app-update-notify`: il segnale di aggiornamento disponibile comprende anche una **notifica di sistema**; il controllo automatico non è più limitato al foreground ma può avvenire come lavoro periodico in background; l'opzione esistente governa entrambi i canali e ne richiede il permesso notifiche.
- `app-settings`: lo switch "Avvisami di nuove versioni" dichiara che abilita anche le notifiche di sistema e ne richiede il permesso al momento dell'attivazione.

## Impact

- **Codice**: nuovo worker periodico e scheduler su `Platforms/Android/Services/` (sul modello di `BackupWorker`/`AndroidBackupScheduler`); `Services/Update/IUpdateNotifier.cs` e `AndroidUpdateNotifier` estesi con la notifica "nuova versione disponibile" (nuovo id notifica, canale esistente `cardmaster_update`, con `ContentIntent` verso il flusso di aggiornamento); `UpdateViewModel` per la richiesta del permesso e l'attivazione/annullamento della schedulazione; `NoopUpdateNotifier` allineato.
- **Manifest**: nessun permesso nuovo — `POST_NOTIFICATIONS` e `INTERNET` sono già dichiarati; cambia solo *quando* il permesso viene richiesto a runtime.
- **Consumo**: fino a una richiesta HTTP **all'ora** alle API GitHub quando l'opzione è attiva, con vincolo di rete connessa (in pratica meno, perché il sistema batcha e rimanda il lavoro periodico). Con l'opzione disattivata (default) il comportamento resta **zero rete non richiesta**, come oggi.
- **Non toccati**: download/checksum/installazione dell'APK, riconciliazione della versione installata (`maui-update-stale-signal`), backup Drive, tutte le capability delle carte.

> **Assunzione da confermare**: "notifica ripetuta a ogni controllo" è stata interpretata come *finché l'utente non ha silenziato quella versione*. Il silenziamento esplicito per versione, già previsto da `app-update-notify`, continua a valere per entrambi i canali — altrimenti chiudere il segnale non servirebbe a nulla e la notifica tornerebbe ogni ora.
