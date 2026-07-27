## 1. Preferenze

- [x] 1.1 Aggiungere a `ISettingsStore`/`SettingsStore` le nuove proprietà: `UpdateNotifyEnabled` (bool, default `false`), `UpdateNotifyDismissedVersion` (string?, default null). Riusare le proprietà già esistenti `LastUpdateCheckUtc` e `LastUpdateCheckAvailableVersion` come "ultimo controllo/versione disponibile" invece di introdurne di nuove.
- [x] 1.2 Verificare che i default (opt-in disattivato) siano coerenti con lo store `Preferences` esistente (nessuna migrazione dati necessaria).

## 2. Innesco del controllo automatico

- [x] 2.1 In `IUpdateService`/`UpdateService`, aggiungere un metodo (es. `CheckForUpdateIfDueAsync`) che esegue `CheckForUpdateAsync` solo se `UpdateNotifyEnabled == true` e sono trascorse almeno 24h da `LastUpdateCheckUtc` (o mai eseguito).
- [x] 2.2 Agganciare la chiamata al passaggio dell'app in foreground (`App.xaml.cs` / lifecycle `OnResume`/`OnStart`), in modo che non blocchi l'avvio (fire-and-forget con gestione errori silenziosa, coerente con lo scenario "Errore di rete" di `app-update` che non deve bloccare il resto dell'app).
- [x] 2.3 Aggiornare `LastUpdateCheckUtc`/`LastUpdateCheckAvailableVersion` al termine del controllo automatico, come già avviene per quello manuale.

## 3. Segnale in-app

- [x] 3.1 Esporre uno stato osservabile (es. proprietà bindabile su un servizio/viewmodel condiviso, o messaggio `WeakReferenceMessenger`) che indica "aggiornamento disponibile e non silenziato" = `LastUpdateCheckAvailableVersion` non vuota, diversa dalla versione installata, e diversa da `UpdateNotifyDismissedVersion`.
- [x] 3.2 In `CardListPage.xaml`/`.xaml.cs`, aggiungere un badge/indicatore sul `ToolbarItem` "Impostazioni" (src/CardMaster/Views/CardListPage.xaml:16) legato allo stato di 3.1.
- [x] 3.3 Il tocco del badge/voce naviga alla rotta esistente `UpdatePage` (già registrata in `AppShell.xaml.cs`) — nessuna nuova pagina per il flusso di download/installazione.
- [x] 3.4 (Opzionale, non bloccante) Valutare un banner discreto anche nella lista carte, riusando lo stesso stato di 3.1. Implementato: banner dismissibile (✕) in cima alla lista carte, tap → `UpdatePage`, riusa `IsUpdateAvailable`/`UpdateAvailableVersion` di `CardListViewModel`.

## 4. Silenziamento per versione

- [x] 4.1 Aggiungere in `UpdatePage`/`UpdateViewModel` (o dove più appropriato) un'azione "Chiudi"/dismiss che imposta `UpdateNotifyDismissedVersion = LastUpdateCheckAvailableVersion`.
- [x] 4.2 Verificare che un nuovo controllo che rileva una versione diversa da `UpdateNotifyDismissedVersion` faccia ricomparire il segnale (nessuna logica aggiuntiva necessaria se lo stato di 3.1 confronta correttamente le due stringhe).

## 5. Impostazioni

- [x] 5.1 In `SettingsPage`/`UpdatePage` (sezione "Controllo aggiornamenti"), aggiungere lo switch "Avvisami di nuove versioni" collegato a `UpdateNotifyEnabled`, con testo esplicativo breve (interroga GitHub per verificare nuove versioni).
- [x] 5.2 Verificare che disattivare lo switch fermi i controlli automatici senza cancellare l'esito dell'ultimo controllo già mostrato.

## 6. Verifica

- [x] 6.1 `dotnet build` senza errori.
- [x] 6.2 Verifica manuale su emulatore/dispositivo: opzione disattivata di default, nessuna chiamata di rete all'apertura; attivazione opzione → controllo al foreground rispettando l'intervallo minimo; badge compare/scompare correttamente; dismiss silenzia la versione corrente e non oltre. Verificato su emulatore Android il 25 lug 2026 (badge + banner mostrati dopo il primo controllo automatico, dismiss disabilita "Ignora questa versione" e nasconde badge/banner senza bloccare "Scarica e installa").
- [x] 6.3 Aggiornare `PLAN.md` con la nuova change completata, seguendo lo stile delle voci già presenti.
