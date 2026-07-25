## 1. Store delle preferenze

- [x] 1.1 Creare `Services/ISettingsStore.cs` con enum `AppThemePreference { System, Light, Dark }` e proprietà `Theme` (estendibile per il futuro backup).
- [x] 1.2 Creare `Services/SettingsStore.cs` che legge/scrive `Preferences.Default` (chiave `"theme"`, default `System`), con mappatura enum ↔ stringa.
- [x] 1.3 Registrare `services.AddSingleton<ISettingsStore, SettingsStore>()` in `MauiProgram`.

## 2. Applicazione del tema

- [x] 2.1 All'avvio, leggere `ISettingsStore.Theme` e impostare `Application.Current.UserAppTheme` con la mappatura System→Unspecified / Light→Light / Dark→Dark (in `App` via iniezione, o in `MauiProgram` post-`Build()` — scegliere la via più pulita).

## 3. Pagina Impostazioni + ViewModel

- [x] 3.1 Creare `ViewModels/SettingsViewModel.cs`: espone le opzioni tema, `SelectedTheme` (get da store, set → scrive store + aggiorna `Application.Current.UserAppTheme`), e le info app (`AppInfo.Current.Name`, `VersionString`, `BuildString`).
- [x] 3.2 Creare `Views/SettingsPage.xaml`: sezione "Aspetto" (Picker Sistema/Chiaro/Scuro legato a `SelectedTheme`) e sezione "Info app" (nome + versione), usando le risorse di stile condivise (`PagePadding`, `SectionSpacing`, `CaptionLabel`).
- [x] 3.3 Creare `Views/SettingsPage.xaml.cs` con iniezione del `SettingsViewModel` e `BindingContext`.
- [x] 3.4 Registrare in `MauiProgram`: `AddTransient<SettingsPage>()` e `AddTransient<SettingsViewModel>()`.

## 4. Navigazione

- [x] 4.1 Registrare la route in `AppShell.xaml.cs`: `Routing.RegisterRoute("SettingsPage", typeof(SettingsPage))`.
- [x] 4.2 Aggiungere in `CardListPage.xaml` un `ToolbarItem Text="Impostazioni"` e in `CardListPage.xaml.cs` l'handler `OnSettingsClicked` → `Shell.Current.GoToAsync("SettingsPage")`.

## 5. Build e verifica finale

- [x] 5.1 `dotnet build` (net10.0-android) con **0 errori**.
- [x] 5.2 Verifica funzionale: la voce "Impostazioni" apre la pagina; è visibile la versione app; cambiando tema l'aspetto cambia subito e resta dopo il riavvio. **Verifica a schermo su device.**
