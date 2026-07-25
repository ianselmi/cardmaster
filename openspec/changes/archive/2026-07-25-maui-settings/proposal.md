## Why

L'app non ha un posto dove esporre opzioni e informazioni: non c'è una pagina Impostazioni, né un modo persistente per memorizzare preferenze utente. Serve stabilire questa "sezione Impostazioni" come contenitore e infrastruttura, così che le opzioni future (a partire dal **backup**) abbiano dove vivere e un meccanismo di persistenza già pronto. È il prerequisito naturale della prossima change `maui-backup-local`.

## What Changes

- **Nuova pagina Impostazioni** raggiungibile dalla lista carte (voce nella toolbar), con route Shell registrata come le altre pagine.
- **Store delle preferenze**: servizio applicativo che incapsula l'API `Preferences` di MAUI (chiave/valore locale), registrato in DI, come unica porta per leggere/scrivere le impostazioni. Nessun dato sensibile, nessuna rete.
- **Info app**: la pagina mostra nome e versione/build dell'app (da `AppInfo`), utile per il supporto e per la distribuzione fuori store.
- **Preferenza Tema**: primo toggle reale — l'utente sceglie l'aspetto tra **Sistema / Chiaro / Scuro**; la scelta è persistita e applicata all'avvio (`Application.UserAppTheme`), coerente con la palette di brand introdotta da `maui-restyle`.
- **Predisposizione backup**: la pagina è progettata come host della futura opzione di backup; la voce "Backup e ripristino" vera e propria sarà aggiunta dalla change `maui-backup-local` (qui non si implementa il backup).

Nessun cambiamento a dati carte, scansione, rendering barcode o persistenza del DB.

## Capabilities

### New Capabilities
- `app-settings`: sezione Impostazioni dell'app — pagina dedicata raggiungibile dalla navigazione, store persistente delle preferenze (chiave/valore locale) come infrastruttura, visualizzazione delle info app, e preferenza di tema (Sistema/Chiaro/Scuro) applicata e persistita. Fa da contenitore per le opzioni future (backup).

### Modified Capabilities
<!-- Nessun requisito a livello di spec cambia. app-shell resta lo scaffold di navigazione; qui si aggiunge un nuovo target di navigazione come parte di app-settings, non si modifica il requisito esistente. -->

## Impact

- **UI/navigazione**: nuova `Views/SettingsPage.xaml(.cs)` e relativo ViewModel; `AppShell.xaml.cs` registra la route `SettingsPage`; `CardListPage` aggiunge una voce toolbar "Impostazioni".
- **Servizi**: nuovo `ISettingsStore`/`SettingsStore` (wrapper su `Microsoft.Maui.Storage.Preferences`), registrato in `MauiProgram`.
- **Avvio**: applicazione del tema persistito all'avvio (in `App` o `MauiProgram`).
- **Dipendenze**: nessuna nuova dipendenza NuGet (si usano `Preferences` e `AppInfo` di MAUI, già disponibili).
- **Fuori scope**: implementazione del backup/ripristino (change separata `maui-backup-local`) ed eventuale backup su Google Drive (rimandato, `maui-backup-drive` in v2).
- **Vincolo di accettazione**: `dotnet build` con 0 errori; la pagina Impostazioni si apre dalla lista, mostra la versione, e il cambio tema si applica e sopravvive al riavvio.
