## 1. Riconoscimento dell'aggiornamento installato

- [x] 1.1 Aggiungere a `IUpdateService` un'operazione di riconciliazione **senza rete** (es. `ReconcileInstalledVersion()`) che, se la versione persistita in `LastUpdateCheckAvailableVersion` coincide con `AppInfo.Current.VersionString`, azzera quello stato e `LastCheckedRelease`
- [x] 1.2 Nella stessa operazione, azzerare `UpdateNotifyDismissedVersion` **solo** quando coincide con la versione installata; lasciarla intatta negli altri casi
- [x] 1.3 Verificare che la riconciliazione NON tocchi `LastUpdateCheckUtc` e sia idempotente (due esecuzioni consecutive danno lo stesso stato)
- [x] 1.4 Sollevare `StateChanged` solo se la riconciliazione ha effettivamente modificato qualcosa, per non far ridisegnare la UI a vuoto a ogni foreground

## 2. Punto di verità condiviso per "aggiornamento disponibile"

- [x] 2.1 Esporre su `IUpdateService` la versione disponibile **già filtrata** rispetto a quella installata (una versione uguale a quella installata non è un aggiornamento), così che i tre consumatori non ripetano la regola
- [x] 2.2 `CardListViewModel`: far derivare `UpdateAvailableVersion` e `RefreshUpdateBadge` da quel punto di verità, mantenendo il comportamento del silenziamento per versione
- [x] 2.3 `UpdateViewModel`: allineare `AvailableVersion`, `IsUpdateAvailable` e `IsDismissed` alla stessa regola
- [x] 2.4 `UpdateViewModel.LastCheckText`: dopo la riconciliazione mostrare "Ultimo controllo: … nessun aggiornamento disponibile" con l'orario conservato, mai "Nessun controllo ancora effettuato"

## 3. Innesco all'avvio e alla ripresa

- [x] 3.1 `App.xaml.cs`: invocare la riconciliazione in `OnStart` e `OnResume` **prima** di `CheckForUpdateIfDueAsync`, così da valere anche con il controllo automatico disattivato
- [x] 3.2 Verificare che la riconciliazione non introduca attese o chiamate di rete sul percorso di avvio

## 4. Verifica

- [x] 4.1 `dotnet build` con 0 errori
- [x] 4.2 Riprodurre il bug sulla build attuale: installare una versione precedente, far rilevare l'aggiornamento, installare la nuova versione e osservare il banner falso al riavvio
- [x] 4.3 Verificare la correzione sullo stesso percorso: dopo l'installazione, né banner né badge né riga in Impostazioni annunciano la versione installata
- [x] 4.4 Verificare la correzione **senza rete** e con "Avvisami di nuove versioni" **disattivata** (il caso che oggi non si ripara mai da solo)
- [x] 4.5 Verificare che uno stato già sporco preesistente venga sanato al primo avvio dopo l'aggiornamento correttivo
- [x] 4.6 Verificare la non-regressione: con una versione remota realmente diversa da quella installata, banner, badge e pagina Aggiornamenti si comportano come prima, e il silenziamento per versione continua a funzionare
- [x] 4.7 Rivedere il diff prima del commit (repository pubblico) e aggiornare `PLAN.md` con la voce della change
