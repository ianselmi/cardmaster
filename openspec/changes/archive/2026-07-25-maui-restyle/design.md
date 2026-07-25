# Design — maui-restyle

## Contesto

Il branding è ancora quello del template MAUI: `#512BD4` viola in `Colors.xaml`, `Platforms/Android/Resources/values/colors.xml`, e nelle `Color=` di `MauiIcon`/`MauiSplashScreen` nel csproj; icona/splash con le lettere "NET" (`appicon.svg` rect viola + `appiconfg.svg`/`splash.svg` glifi "NET"). La `CardTilePalette` (10 colori curati) è già separata e va solo riallineata. Non ci sono nuove dipendenze.

Direzione decisa con l'utente: **palette ambra/arancio caldo**, **logo = tessera arrotondata con barre di barcode stilizzate**.

## Palette di brand (valori concreti)

Colori primari/accent e neutri caldi. Definiti in `Colors.xaml` come sorgente di verità e riflessi nelle altre superfici.

| Ruolo | Chiave | Hex | Note |
|---|---|---|---|
| Primario | `Primary` | `#E07B1A` | ambra, usato su superfici chiare |
| Primario (dark) | `PrimaryDark` | `#F59E0B` | variante più chiara/luminosa per dark mode |
| Testo su primario | `PrimaryText` / `OnPrimary` | `#FFFFFF` | contrasto AA con `#E07B1A` |
| Accent | `Accent` / `Tertiary` | `#F59E0B` | evidenziazioni |
| Secondario (superficie tenue) | `Secondary` | `#FDEBD0` | ambra desaturato chiaro |
| Testo su secondario | `SecondaryDarkText` | `#8A4B0A` | |

Neutri: si riusa la scala `Gray100…Gray950` esistente (invariata, è neutra). Superfici scure calde per dark mode dove servono sfondi custom: base `#17140F`.

**Contrasto**: `#FFFFFF` su `#E07B1A` ≈ 3.0:1 → adeguato per **testo grande/bold e componenti UI** (AA large / non-text). Per label piccole su fondo ambra usare testo scuro `#3A2408` o fondo primario più scuro. Il testo del pulsante "Salva" è bold ≥14 → ok bianco. Da verificare a video nel passo di build/preview.

### Mappatura superfici

- `Resources/Styles/Colors.xaml`: aggiornare `Primary`, `PrimaryDark`, `Secondary`, `SecondaryDarkText`, `Tertiary`; rimuovere/riusare `Magenta`/`MidnightBlue` non pertinenti se non referenziati. Mantenere la scala Gray e i brush.
- `Platforms/Android/Resources/values/colors.xml`: `colorPrimary=#E07B1A`, `colorPrimaryDark=#B4610F`, `colorAccent=#F59E0B`.
- `CardMaster.csproj`: `MauiIcon ... Color="#E07B1A"`, `MauiSplashScreen ... Color="#E07B1A"`.
- `Styles.xaml`: i `Setter` che puntano a `{StaticResource Primary}` ereditano automaticamente il nuovo valore; verificare che Button/ActivityIndicator/ecc. restino leggibili (già `AppThemeBinding` Light=Primary / Dark=PrimaryDark).

## CardTilePalette allineata

Riordino/sostituzione dei 10 colori per armonizzarli col brand ambra, mantenendo varietà e leggibilità con testo bianco (i valori restano scuri/saturi). Il colore primario ambra dei controlli NON deve coincidere con un tile per non confondere azione e carta. Proposta (deterministica, invariata la logica FNV-1a):

```
#C2410C arancio bruciato   #B45309 ambra scuro     #0F766E teal
#1D4ED8 blu                #15803D verde           #7C3AED viola
#BE185D magenta            #0369A1 azzurro         #4338CA indaco
#334155 slate
```

Nota: `card-list` richiede solo "palette definita + deterministico + contrasto"; nessun cambiamento di requisito, solo valori. Il testo dei tile resta bianco (già così in `CardListPage.xaml`).

## Logo (tessera + barcode)

Concept: **tessera arrotondata** (rounded rect) con **barre verticali** di larghezza variabile (barcode stilizzato) al centro. Monocromatico, coerente con le regole degli adaptive icon Android (foreground entro la "safe zone" centrale ~66%, background pieno).

- `appicon.svg` (background): rect pieno `#E07B1A` a tutto viewBox 456×456. (Il `Color=` del csproj lo tinge comunque; teniamo il rect coerente.)
- `appiconfg.svg` (foreground): su viewBox 456×456, glifo centrato entro ~300×300:
  - tessera arrotondata bianca (rounded rect, corner ~10% del lato) con leggero margine interno;
  - dentro, 5–7 barre verticali di larghezza variabile ritagliate/negative nel colore di brand (o barre scure) per leggere come "codice a barre";
  - forme semplici e spesse per restare nitide a 48–72 dp.
- `splash.svg`: stessa tessera+barcode centrata; `MauiSplashScreen BaseSize="128,128"` invariato, `Color="#E07B1A"`.
- Rimuovere `Resources/Images/dotnet_bot.png` **solo se** un `grep` conferma che non è più referenziato (attualmente c'è un `MauiImage Update=...dotnet_bot.png` nel csproj: rimuovere anche quella riga se si elimina l'asset).

Gli SVG sono scritti a mano (path/rect semplici), nessun tool esterno. Renderizzati in build da MAUI (Resizetizer) verso le densità Android.

## Tipografia e spaziatura condivise

Definire in `Styles.xaml` risorse riutilizzabili (implicite o con `x:Key`), evitando refactor invasivi delle pagine:

- Scala font (chiavi `x:Key` `Double`): `FontSizeTitle=22`, `FontSizeBody=16`, `FontSizeCaption=13`.
- Spaziature (`x:Key` `Double`/`Thickness`): `PagePadding=20`, `SectionSpacing=16`, `ItemSpacing=8`, `TileCorner=20`, `CardCorner=16`.
- Applicazione: sostituire nei XAML esistenti i valori hard-coded (`Padding="20"`, `Spacing="16"`, `FontSize="18/13/22"`, `CornerRadius=20/16`) con `{StaticResource ...}` dove migliora la coerenza. Le label caption "Opacity 0.7 FontSize 13" diventano uno stile `CaptionLabel` opzionale. Non cambiare la struttura dei layout né i binding.

Pagine toccate: `CardListPage.xaml`, `ShowCardPage.xaml`, `AddCardPage.xaml`, `ScanPage.xaml`. `AppShell.xaml` può ricevere `Shell.BackgroundColor`/`TitleColor` di brand.

## Dark mode

La maggior parte dei colori usa già `AppThemeBinding` in `Styles.xaml`. Verificare: pulsante (Light=Primary #E07B1A testo bianco / Dark=PrimaryDark #F59E0B testo scuro `PrimaryDarkText`), titoli Shell, e che i tile (colori fissi scuri) restino leggibili su entrambi i temi. Barcode: l'area resta `BackgroundColor="White"` hard-coded in `ShowCardPage` — invariata per specifica `card-display`.

## Alternative considerate

- **Ridefinire i colori solo in Colors.xaml** senza toccare `colors.xml`/csproj: scartata — icona/splash e chrome nativo resterebbero viola. Serve toccare tutte e tre le superfici.
- **Introdurre un font custom**: fuori scope; si resta su OpenSans del template per non aggiungere asset/pesare sull'APK. La coerenza si ottiene con la scala di dimensioni.
- **Logo con monogramma "C"**: scartato dall'utente a favore di tessera+barcode (più letterale sullo scopo dell'app).

## Rischi

- Contrasto testo bianco su ambra `#E07B1A` è al limite per testo piccolo: mitigato usando ambra su testo bold/grande o testo scuro su superfici ambra chiare. Verifica visiva nel passo finale.
- Adaptive icon Android ritaglia il foreground: tenere il glifo entro la safe zone centrale per non troncare il barcode.
- Verifica obbligatoria `dotnet build` 0 errori + avvio app con nuova icona/splash e lista con nuovi colori.
