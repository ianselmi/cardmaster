## 1. Palette di brand (sorgente di verità)

- [x] 1.1 Aggiornare `Resources/Styles/Colors.xaml`: `Primary=#E07B1A`, `PrimaryDark=#F59E0B`, `Secondary=#FDEBD0`, `Tertiary=#F59E0B`; `Magenta`/`MidnightBlue` (ancora referenziati in Styles.xaml) ri-puntati al brand invece di rimossi; `SecondaryDarkText=#F0B267` (ambra chiaro: è titolo NavBar in dark mode, deve restare chiaro). Scala Gray e brush invariati.
- [x] 1.2 Aggiornare `Platforms/Android/Resources/values/colors.xml`: `colorPrimary=#E07B1A`, `colorPrimaryDark=#B4610F`, `colorAccent=#F59E0B`.
- [x] 1.3 Aggiornare in `CardMaster.csproj` le `Color=` di `MauiIcon` e `MauiSplashScreen` a `#E07B1A`.
- [x] 1.4 Verificato con grep: nessun residuo `#512BD4`/`#2B0B98`/altri viola template in `src` (resta solo `appicon.svg`, riscritto in 2.1).

## 2. Logo (app icon + splash)

- [x] 2.1 Riscritto `Resources/AppIcon/appicon.svg`: rect pieno `#E07B1A` a tutto viewBox 456×456.
- [x] 2.2 Riscritto `Resources/AppIcon/appiconfg.svg`: tessera arrotondata bianca centrata (safe zone) con 13 barre verticali di larghezza variabile (barcode) in `#B4610F`.
- [x] 2.3 Riscritto `Resources/Splash/splash.svg` con la stessa tessera+barcode centrata.
- [x] 2.4 Rimosso `Resources/Images/dotnet_bot.png` (non referenziato in codice) e la relativa riga `MauiImage Update` nel csproj.

## 3. Colori dei riquadri (CardTilePalette)

- [x] 3.1 Sostituiti i 10 colori in `Views/CardTilePalette.cs` con la palette allineata al brand; logica FNV-1a invariata; nessun tile coincide col primario ambra.

## 4. Tipografia e spaziatura condivise

- [x] 4.1 Aggiunte in `Styles.xaml` risorse condivise: `FontSizeTitle/Body/Caption`, `PagePadding`, `SectionSpacing`, `ItemSpacing` e stile `CaptionLabel`. `TileCorner`/`CardCorner` NON aggiunti come risorsa: il binding di un `double` a `CornerRadius` non ha converter garantito → corner lasciati letterali.
- [x] 4.2 Applicate le risorse all'empty view di `CardListPage.xaml` (padding/spacing/font + `CaptionLabel`), layout e binding invariati.
- [x] 4.3 Applicate le risorse in `ShowCardPage.xaml` (padding/spacing, caption issuer, valore barcode = `FontSizeTitle`); area barcode resta su fondo bianco.
- [x] 4.4 Applicate le risorse in `AddCardPage.xaml` (padding/spacing + label caption via `CaptionLabel`).
- [x] 4.5 Applicate le risorse al pannello permesso negato di `ScanPage.xaml` (padding/spacing/font).
- [x] 4.6 `AppShell.xaml`: nav bar con `Shell.BackgroundColor=Primary` e testo bianco (foreground/title).

## 5. Verifica tema chiaro/scuro

- [x] 5.1 Verificato a livello di risorse: pulsante/indicatori/titoli usano `AppThemeBinding` (Light=Primary ambra testo bianco bold / Dark=PrimaryDark testo scuro); nav bar ambra con testo bianco. Contrasto testo piccolo su ambra evitato (caption su fondo neutro). Resa finale da confermare a schermo (5.x visivo → 6.2).
- [x] 5.2 Confermato nel markup: l'area barcode in `ShowCardPage` è `BackgroundColor="White"` hard-coded, invariata → nero su bianco anche in dark mode.

## 6. Build e verifica finale

- [x] 6.1 `dotnet build` (net10.0-android, Debug): **0 errori**, 31 warning preesistenti (NU1608/XA4301, non legati al restyle).
- [ ] 6.2 Avviare l'app (device/emulatore) e verificare visivamente: nuova icona di lancio, nuovo splash, lista carte con nuovi colori dei tile, nav bar ambra, pagine coerenti, nessun residuo viola. **Da fare su device (non eseguito qui).**
