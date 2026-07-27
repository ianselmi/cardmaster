## Why

Dopo aver installato un aggiornamento, l'app continua a segnalare che *quello stesso* aggiornamento è disponibile: banner in lista carte, badge sull'icona Impostazioni e riga "Ultimo controllo: … versione N disponibile" annunciano la versione N mentre la versione installata **è** già la N.

La causa è che l'esito dell'ultimo controllo (`LastUpdateCheckAvailableVersion`) è una preferenza **persistita** che sopravvive all'aggiornamento dell'app, mentre il fatto che l'aggiornamento sia stato installato non viene mai riconosciuto: il segnale viene derivato da "esiste una versione rilevata", non da "la versione rilevata è diversa da quella installata". Al riavvio dopo l'installazione il servizio non ha più in memoria l'ultimo controllo e ricade sul valore persistito, che è ormai obsoleto.

Non si corregge da sé: il controllo automatico è opt-in e disattivato per default, quindi senza un controllo manuale il segnale falso resta indefinitamente; con l'opzione attiva resta comunque fino a 24 ore, perché l'intervallo minimo è appena stato consumato dal controllo che aveva rilevato l'aggiornamento.

Il comportamento corretto è già scritto nel requisito esistente — il segnale dura «finché non viene chiuso dall'utente **o installato l'aggiornamento**» — ma non è mai stato reso esplicito né verificabile: questa change lo trasforma in una regola normativa, con gli scenari che oggi fallirebbero.

## What Changes

- **La versione installata diventa parte della condizione del segnale.** Un aggiornamento è considerato disponibile solo se la versione rilevata è **diversa da quella attualmente installata**. Vale per tutti i punti che mostrano il segnale: banner in lista carte, badge sulle Impostazioni, pagina Aggiornamenti e riga di riepilogo dell'ultimo controllo.
- **L'esito persistito viene riconciliato all'avvio.** Quando l'app rileva che la versione annunciata come disponibile coincide con quella installata, considera l'aggiornamento come **installato**: azzera lo stato persistito dell'ultimo controllo, invece di aspettare un nuovo controllo di rete. La riconciliazione avviene **senza rete**, quindi funziona anche offline e a controllo automatico disattivato.
- **Il silenziamento non lascia residui.** La versione eventualmente silenziata dall'utente viene dimenticata quando quella versione risulta installata, così non resta a mascherare aggiornamenti successivi né a sporcare le preferenze.
- **L'esito "nessun aggiornamento" resta leggibile.** Dopo la riconciliazione la pagina Aggiornamenti dice che si sta usando l'ultima versione nota, senza far sembrare che non sia mai stato fatto un controllo.

Fuori scope: cambiare la logica di confronto tra versioni (resta il confronto per uguaglianza col nome della Release `latest`), la frequenza del controllo automatico, o il comportamento di download e installazione.

## Capabilities

### New Capabilities
Nessuna.

### Modified Capabilities
- `app-update`: l'esito di un controllo perde validità quando la versione che annunciava risulta installata; lo stato persistito dell'ultimo controllo va riconciliato con la versione installata senza richiedere un nuovo controllo di rete.
- `app-update-notify`: il segnale visibile (banner e badge) è condizionato alla differenza tra versione rilevata e versione installata, e sparisce da sé una volta installato l'aggiornamento; il silenziamento per versione viene dimenticato quando quella versione è installata.
- `app-settings`: la riga di riepilogo dell'ultimo controllo nelle Impostazioni non annuncia come disponibile una versione che risulta installata.

## Impact

- **Codice**: `Services/Update/UpdateService.cs` (riconciliazione dello stato persistito, esposta come operazione senza rete), `ViewModels/CardListViewModel.cs` (`UpdateAvailableVersion`/`RefreshUpdateBadge`), `ViewModels/UpdateViewModel.cs` (`AvailableVersion`, `IsUpdateAvailable`, `IsDismissed`, `LastCheckText`), `App.xaml.cs` (riconciliazione all'avvio e alla ripresa dal background, prima dell'eventuale controllo di rete).
- **Preferenze**: `update_last_check_available_version` e `update_notify_dismissed_version` vengono ripulite quando si riferiscono alla versione installata. Nessuna migrazione: il primo avvio dopo la correzione sana lo stato già sporco sui device che hanno il banner bloccato.
- **Non toccati**: modello dati e database, download/checksum/installazione dell'APK, catalogo emittenti, backup Drive, tutte le capability delle carte.

> **Dipendenza di ordine**: la capability `app-update-notify` non è ancora in `openspec/specs/` perché la change `maui-update-notify` è implementata ma **non archiviata**. Va archiviata (o sincronizzata) prima di questa, altrimenti il delta qui sotto creerebbe la capability da una vista parziale.
