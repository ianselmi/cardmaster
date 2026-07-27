## Context

Il flusso attuale, ricostruito dal codice:

- `UpdateService.CheckForUpdateAsync` confronta il nome della Release `latest` con `AppInfo.Current.VersionString`. Se coincidono azzera `LastCheckedRelease` e `_settings.LastUpdateCheckAvailableVersion`; altrimenti li valorizza (`UpdateService.cs:107-116`).
- `LastCheckedRelease` è **in memoria** (vive quanto il processo); `LastUpdateCheckAvailableVersion` è una **preferenza persistita** (`SettingsStore.cs:113`).
- I consumatori leggono `LastCheckedRelease?.VersionName ?? _settings.LastUpdateCheckAvailableVersion` e considerano disponibile un aggiornamento se il risultato **non è null**:
  - `CardListViewModel.cs:110` e `:146` — banner in lista e badge sull'icona Impostazioni;
  - `UpdateViewModel.cs:72,74` — pagina Aggiornamenti;
  - `UpdateViewModel.cs:104` — riga "Ultimo controllo: … versione N disponibile" in Impostazioni.
- Il controllo automatico (`CheckForUpdateIfDueAsync`, `UpdateService.cs:119`) esce subito se `UpdateNotifyEnabled` è falso (default) o se non sono passate 24 ore da `LastUpdateCheckUtc`.

**La sequenza che produce il bug:** il controllo rileva la versione N e persiste `"N"` → l'utente scarica e installa → l'app riparte **come versione N** → nel processo nuovo `LastCheckedRelease` è `null`, quindi si ricade sulla preferenza `"N"` → il segnale annuncia la versione N mentre la versione installata è N. Non si auto-ripara perché l'unico punto che azzera la preferenza è un controllo di rete che risponde "sei aggiornato", e quel controllo o non parte mai (opzione disattivata) o è bloccato per 24 ore dall'intervallo minimo appena consumato.

Vincoli: nessuna nuova dipendenza; il core resta offline-first (la correzione non deve richiedere rete); nessuna modifica al confronto di versione né al flusso di download/installazione.

## Goals / Non-Goals

**Goals:**

- Il segnale di aggiornamento sparisce da sé quando l'aggiornamento annunciato risulta installato, **senza rete** e **senza attendere** l'intervallo del controllo automatico.
- Un solo punto di verità per "esiste un aggiornamento disponibile", condiviso da banner, badge, pagina Aggiornamenti e riga di riepilogo.
- Lo stato già sporco sui device che oggi mostrano il banner bloccato viene sanato al primo avvio dopo la correzione, senza migrazione esplicita.

**Non-Goals:**

- Cambiare il criterio di confronto tra versioni (resta l'uguaglianza col nome della Release `latest`; l'ordinamento semantico non è oggetto di questa change).
- Cambiare la frequenza o le condizioni del controllo automatico.
- Rilevare l'installazione dell'aggiornamento tramite eventi di sistema (broadcast `MY_PACKAGE_REPLACED` o simili).

## Decisions

### 1. La versione installata entra nella condizione, invece di essere ignorata

Il predicato "aggiornamento disponibile" passa da `AvailableVersion is not null` a `AvailableVersion is not null && AvailableVersion != versione installata`. È la traduzione diretta del requisito già esistente («finché non viene chiuso dall'utente **o installato l'aggiornamento**») in una condizione che il codice può valutare da solo, in qualsiasi momento e senza rete: la versione installata è sempre disponibile localmente.

Alternative scartate:
- *Azzerare la preferenza al termine dell'installazione*: l'app viene sostituita e riavviata dal package installer, il codice che ha lanciato l'intent non ha un momento affidabile in cui girare dopo l'installazione; inoltre non sanerebbe i device già oggi in stato sporco.
- *Ascoltare il broadcast `MY_PACKAGE_REPLACED`*: richiede un receiver registrato nel manifest per un problema che si risolve con un confronto di stringhe, e resterebbe comunque da sanare lo stato preesistente.
- *Ridurre l'intervallo del controllo automatico*: non risolve (l'opzione è disattivata per default), costa rete e contraddice il requisito dei 24 minimi.

### 2. Riconciliazione esplicita all'avvio, non solo un filtro in lettura

Filtrare in lettura basterebbe a non mostrare il segnale, ma lascerebbe nelle preferenze una versione "disponibile" che non lo è: la riga di riepilogo dell'ultimo controllo continuerebbe a leggere quel valore, e la versione silenziata resterebbe a mascherare aggiornamenti futuri. Serve quindi anche una **riconciliazione**: quando la versione annunciata coincide con quella installata, lo stato persistito dell'ultimo controllo viene azzerato e l'eventuale silenziamento di quella versione dimenticato.

La riconciliazione è un'operazione **locale e senza rete**, invocata all'avvio e alla ripresa dal background **prima** dell'eventuale controllo automatico, così vale anche con l'opzione disattivata. Deve essere idempotente: eseguirla due volte non cambia il risultato.

Perché sta in `UpdateService` e non nei ViewModel: è la stessa entità che scrive quello stato, e i tre consumatori devono vederne l'effetto senza duplicare la regola.

### 3. `LastUpdateCheckUtc` non viene toccato

L'istante dell'ultimo controllo resta valido: un controllo **è** avvenuto, ha solo perso rilevanza il suo esito. Azzerarlo farebbe ripartire subito il controllo automatico (contraddicendo l'intervallo minimo) e mostrerebbe "Nessun controllo ancora effettuato" a chi il controllo l'ha appena fatto. Dopo la riconciliazione la riga di riepilogo mostra l'orario dell'ultimo controllo con l'esito "nessun aggiornamento disponibile".

### 4. Il silenziamento si dimentica solo per la versione installata

`UpdateNotifyDismissedVersion` viene azzerata **solo** quando coincide con la versione installata. Un silenziamento relativo a una versione remota che non è stata installata resta valido: l'utente ha scelto di non essere disturbato per quella versione e la scelta va rispettata finché non ne esce una nuova.

## Risks / Trade-offs

- **Il confronto resta per uguaglianza di stringhe** → se un giorno la Release `latest` esponesse un nome in un formato diverso da `ApplicationDisplayVersion` (oggi entrambi sono il numero di build), la riconciliazione non riconoscerebbe l'installazione e il banner tornerebbe a bloccarsi. È lo stesso assunto già usato dal controllo, quindi non introduce un rischio nuovo, ma lega le due cose: va ricordato se si tocca il formato del titolo della Release in `ci-release`.
- **Un utente che installa manualmente un APK più recente di quello annunciato** vedrebbe azzerato lo stato solo se le versioni coincidono; se ha installato una versione ancora diversa, il vecchio annuncio resta finché non parte un controllo. Caso marginale (l'installazione manuale fuori dal flusso in-app), e comunque non peggiore di oggi.
- **La riconciliazione gira a ogni foreground** → è un confronto tra due stringhe in memoria più, al più, una scrittura di preferenza una tantum: costo trascurabile e nessun accesso di rete.

## Migration Plan

Nessuna migrazione dati. Al primo avvio dopo l'aggiornamento correttivo, i device che oggi mostrano il banner bloccato eseguono la riconciliazione e ripuliscono le preferenze `update_last_check_available_version` e — se relativa alla versione installata — `update_notify_dismissed_version`. Chi non è in stato sporco non vede alcun cambiamento.

## Open Questions

Nessuna: il comportamento atteso è già fissato dal requisito esistente di `app-update-notify`, questa change lo rende esplicito e verificabile.
