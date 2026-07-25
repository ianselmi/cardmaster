# Design — maui-settings

## Contesto e pattern esistenti

- Navigazione: **Shell** con route registrate in `AppShell.xaml.cs` (`Routing.RegisterRoute("ScanPage", ...)` ecc.); si naviga con `Shell.Current.GoToAsync("Route")`. La lista carte apre le altre pagine da handler in `CardListPage.xaml.cs` e da `ToolbarItem`.
- DI: servizi e pagine registrati in `MauiProgram.RegisterServices`. Pagine "di stato" transient, singleton per lista/shell.
- Stile: risorse condivise di `maui-restyle` (`PagePadding`, `SectionSpacing`, `CaptionLabel`, `FontSize*`) da riusare per coerenza.

## Store delle preferenze

Interfaccia sottile su `Microsoft.Maui.Storage.Preferences` (già disponibile, nessun pacchetto nuovo):

```csharp
public interface ISettingsStore
{
    AppThemePreference Theme { get; set; }   // enum: System, Light, Dark
}
```

- Implementazione `SettingsStore` legge/scrive `Preferences.Default` con chiavi costanti (`"theme"`), default `System`.
- Registrato `services.AddSingleton<ISettingsStore, SettingsStore>()`.
- Tenere l'interfaccia **estendibile**: la futura change `maui-backup-local` aggiungerà proprietà (es. `bool BackupEnabled`) senza rompere questa.
- Enum `AppThemePreference` mappato a `Microsoft.Maui.ApplicationModel.AppTheme`/`Application.UserAppTheme`:
  - `System → AppTheme.Unspecified`, `Light → AppTheme.Light`, `Dark → AppTheme.Dark`.

## Applicazione del tema

- All'avvio (in `App` constructor dopo `InitializeComponent`, o in `MauiProgram` dopo build): leggere `settingsStore.Theme` e impostare `Application.Current.UserAppTheme`.
- Al cambio dalla pagina: il ViewModel scrive `settingsStore.Theme` e aggiorna subito `Application.Current.UserAppTheme` → l'app applica il tema immediatamente (i colori usano già `AppThemeBinding`).

Nota: applicare in `App` è più semplice di `MauiProgram` perché `Application.Current` esiste già; risolvere `ISettingsStore` dal service provider (`Handler?.MauiContext?.Services` o iniettandolo nel costruttore di `App`). Preferito: iniettare `ISettingsStore` nel costruttore di `App` (registrare `App` in DI o passare via `MauiProgram`). Se l'iniezione in `App` risulta scomoda, applicare il tema in `MauiProgram.CreateMauiApp` subito dopo `builder.Build()` leggendo il servizio dal container.

## Pagina Impostazioni

- `Views/SettingsPage.xaml(.cs)` + `ViewModels/SettingsViewModel`:
  - Sezione **Aspetto**: un `Picker` (o segmenti) con Sistema/Chiaro/Scuro legato a `SelectedTheme`.
  - Sezione **Info app**: `AppInfo.Current.Name`, `AppInfo.Current.VersionString` (+ `BuildString`).
  - Predisposizione: uno spazio/sezione "Backup" documentato come punto d'innesto per `maui-backup-local` (in questa change **non** si aggiunge UI di backup funzionante, per non lasciare voci morte — la sezione la introdurrà quella change).
- Layout con le risorse di stile condivise (`PagePadding`, `SectionSpacing`, `CaptionLabel`).
- Registrazione: `Routing.RegisterRoute("SettingsPage", typeof(SettingsPage))` in `AppShell.xaml.cs`; DI `AddTransient<SettingsPage>()` + `AddTransient<SettingsViewModel>()`.

## Entry point dalla lista

- `CardListPage.xaml`: aggiungere un `ToolbarItem Text="Impostazioni"` (accanto ad "Aggiungi"); handler `OnSettingsClicked` → `await Shell.Current.GoToAsync("SettingsPage")`.

## Alternative considerate

- **SecureStorage invece di Preferences**: scartata — le impostazioni non sono segrete; `Preferences` è più semplice e adatto a chiave/valore non sensibili. (SecureStorage resta per i token in v2.)
- **Tabella settings in SQLite**: scartata — overkill per poche preferenze; `Preferences` evita migration e query.
- **Includere già il toggle di backup**: scartata — il backup è una change separata (`maui-backup-local`); un toggle senza meccanismo dietro sarebbe una voce morta. Qui si predispone solo l'infrastruttura.
- **Tema forzato dall'app senza opzione**: scartata — il tema di sistema è il default; l'override è una preferenza utente esplicita.

## Rischi

- Iniezione di `ISettingsStore` in `App`: se problematica, fallback all'applicazione del tema in `MauiProgram` post-build (entrambe le strade sono valide, scegliere la più pulita in fase di implementazione).
- `Application.UserAppTheme = Unspecified` deve far seguire il tema di sistema: verificare il comportamento a runtime.
- Verifica obbligatoria `dotnet build` 0 errori + apertura pagina, versione mostrata, cambio tema persistente al riavvio.
