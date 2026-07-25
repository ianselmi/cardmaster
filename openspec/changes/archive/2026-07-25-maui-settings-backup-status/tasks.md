## 1. ViewModel

- [x] 1.1 In `SettingsViewModel`, iniettare/riusare `ISettingsStore` (già iniettato) e aggiungere una proprietà `BackupStatusText` (es. "Backup attivo" / "Backup non attivo") letta da `_settings.BackupEnabled`.
- [x] 1.2 Aggiungere una proprietà `IsBackupEnabled` (bool) per pilotare lo stile del pulsante.

## 2. UI

- [x] 2.1 In `SettingsPage.xaml`, aggiungere sotto il pulsante "Backup su Google Drive" un `Label` di stato (stile `CaptionLabel`, come già usato per `AppVersion`) legato a `BackupStatusText`.
- [x] 2.2 Cambiare il colore di sfondo del pulsante "Backup su Google Drive" quando `IsBackupEnabled` è vero (es. `PrimaryDark` invece del colore di default del pulsante), riusando colori già presenti in `Colors.xaml` — nessun nuovo colore.

## 3. Verifica

- [x] 3.1 `dotnet build` senza errori.
- [x] 3.2 Verifica manuale su emulatore: confermato lo stato di default (backup non attivo) — pulsante grigio + sottotitolo "Backup non attivo". Lo stato "attivo" non è verificabile end-to-end sull'emulatore (nessun account Google reale per completare l'OAuth di `cloud-backup`); il binding (`IsBackupEnabled`/`BackupStatusText` su `ISettingsStore.BackupEnabled`) è lo stesso pattern già usato e verificato per `Theme`/`AppVersion` nella stessa pagina.
- [x] 3.3 Aggiornare `PLAN.md` con la nuova change completata, seguendo lo stile delle voci già presenti.
