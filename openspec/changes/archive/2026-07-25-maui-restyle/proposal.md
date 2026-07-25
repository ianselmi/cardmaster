## Why

L'app ha ancora l'identità visiva del template MAUI di default: icona e splash con le lettere **"NET"** su viola `#512BD4`, la stessa palette viola in `Colors.xaml`, nei colori Android e nel csproj. Non c'è un logo di CardMaster, il colore d'accento non è una scelta di prodotto e le pagine (lista, dettaglio, aggiungi) hanno spaziature e tipografia disomogenee perché composte una alla volta. Prima di distribuire l'APK v1 fuori dallo store, l'app deve avere un'identità propria, riconoscibile e coerente.

## What Changes

- **Nuovo logo** di CardMaster come **app icon** (foreground + background) e **splash screen**, che rimpiazza il segnaposto "NET" del template. Un solo asset SVG concettuale, declinato nelle superfici richieste da MAUI.
- **Nuova palette di brand**: colore primario/accent, colori di superficie e testo, ridefiniti in `Colors.xaml`, in `Platforms/Android/Resources/values/colors.xml` e nelle proprietà `Color` di `MauiIcon`/`MauiSplashScreen` del csproj. Rimozione dei riferimenti al viola `#512BD4` del template.
- **Allineamento della `CardTilePalette`** (colori dei riquadri della lista) alla nuova identità, mantenendo il requisito già specificato in `card-list`: colore deterministico per carta, leggibile con testo a contrasto. Nessuna modifica ai requisiti di `card-list`, solo ai valori concreti.
- **Scala tipografica e di spaziatura coerente**: definizione di stili/risorse condivise (dimensioni titolo/corpo/caption, spaziature e padding standard) applicati in modo uniforme alle pagine esistenti (`CardListPage`, `ShowCardPage`, `AddCardPage`, `ScanPage`), senza cambiarne la funzione o il layout logico.
- **Coerenza chiaro/scuro**: la nuova palette e gli stili devono restare leggibili sia in tema chiaro sia scuro (l'area barcode resta sempre su fondo bianco, come già specificato in `card-display`).

Nessun cambiamento funzionale: navigazione, dati, scansione, rendering barcode e persistenza restano invariati.

## Capabilities

### New Capabilities
- `visual-identity`: identità visiva dell'app — logo (app icon + splash), palette di brand (primario/accent, superfici, testo) come singola sorgente di verità, scala tipografica e di spaziatura condivisa, e coerenza tema chiaro/scuro. Copre gli asset e le risorse di stile, non la logica delle singole pagine.

### Modified Capabilities
<!-- Nessun requisito a livello di spec cambia. card-list continua a richiedere "colore deterministico da una palette definita"; qui cambiano solo i valori concreti della palette, non il requisito. -->

## Impact

- **Asset**: `Resources/AppIcon/appicon.svg`, `Resources/AppIcon/appiconfg.svg`, `Resources/Splash/splash.svg` (nuovo logo). Eventuale rimozione di `Resources/Images/dotnet_bot.png` se non più referenziato.
- **Risorse di stile**: `Resources/Styles/Colors.xaml`, `Resources/Styles/Styles.xaml`, `Platforms/Android/Resources/values/colors.xml`.
- **Build config**: proprietà `Color` di `MauiIcon` e `MauiSplashScreen` in `CardMaster.csproj`.
- **Codice**: `Views/CardTilePalette.cs` (valori palette allineati al brand). Le pagine XAML esistenti vengono aggiornate solo per adottare stili/spaziature condivisi.
- **Dipendenze**: nessuna nuova dipendenza NuGet. Nessun impatto su dati, sync futura o pipeline CI (la firma APK resta invariata).
- **Vincolo di accettazione**: `dotnet build` con 0 errori e app che si avvia mostrando la nuova icona/splash e la lista carte con i nuovi colori.
