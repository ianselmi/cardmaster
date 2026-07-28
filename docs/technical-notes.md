# Note tecniche — CardMaster

Registro di decisioni tecniche e trappole scoperte durante lo sviluppo, verificate a runtime.
Complementare a `PLAN.md` (piano/roadmap) e agli artifact OpenSpec (spec/design per change).

---

## Convenzioni trasversali

### La compilazione senza errori è obbligatoria
Ogni change/deliverable DEVE compilare senza errori: la build pulita (`dotnet build`, 0 errori)
è un **criterio di accettazione**, non un dettaglio. Ogni change OpenSpec include un passo finale
di verifica build e non è considerata completa finché non compila.

---

## Storage locale (SQLite) — client MAUI

> Origine: `maui-shell`; cifratura rimossa con `storage-plain-sqlite` (24 lug 2026).

### Provider SQLite: `sqlite-net-base` + `SQLitePCLRaw.bundle_e_sqlite3`
- Pacchetti: **`sqlite-net-base`** (ORM, senza bundle proprio) + **`SQLitePCLRaw.bundle_e_sqlite3`** (provider SQLite in chiaro, mantenuto).
- All'avvio (`MauiProgram.CreateMauiApp`) chiamare `SQLitePCL.Batteries_V2.Init()` per attivare il provider (unico bundle referenziato).

**Il DB v1 è in chiaro** (nessuna cifratura at-rest). L'header del file `.db3` è quindi il consueto
`SQLite format 3`.

### Perché niente SQLCipher (storico)
La v1 usava `SQLitePCLRaw.bundle_e_sqlcipher` per cifrare il DB, ma quel pacchetto è **deprecato**
(legacy, non mantenuto da SQLitePCLRaw 3.0) e senza rimpiazzo drop-in gratuito. Decisione: cifratura
non essenziale per la v1 offline → SQLite in chiaro. **Trappola storica ancora valida come principio:**
non usare `sqlite-net-pcl` (trascina un secondo provider e crea ambiguità sul provider attivo); con
`sqlite-net-base` si controlla esattamente quale bundle è referenziato.

### Se in futuro servisse di nuovo la cifratura
Usare **`SQLite3MC.PCLRaw.bundle`** (SQLite3 Multiple Ciphers, di utelle) — mantenuto e gratuito,
supporta la cifratura via `PRAGMA key`. NON riusare `bundle_e_sqlcipher` (deprecato).

### minSdk Android 23
`SupportedOSPlatformVersion` per android è `23.0` (Android 6.0): un minimo moderno ragionevole
(in origine richiesto dalle API Keystore, ora rimosse; lasciato invariato).

---

## UI MAUI — trappole di layout

> Origine: `maui-card-list-add-fab` (28 lug 2026).

### Lo stile implicito di `BoxView` dipinge `BackgroundColor`, non `Color`
`Styles.xaml` definisce uno stile implicito per `BoxView` che imposta **`BackgroundColor`**
(`Gray950` in tema chiaro). Un `BoxView` usato come spaziatore con il solo `Color="Transparent"`
resta quindi **nero**: `Color` e `BackgroundColor` sono due proprietà diverse e lo stile agisce
sulla seconda. Per uno spaziatore invisibile servono **entrambe** a `Transparent`.

Vale lo stesso principio per `Border`, il cui stile implicito impone `Stroke` e `StrokeThickness="1"`:
un bordo "senza contorno" richiede `StrokeThickness="0"` esplicito.

### `FlexLayout` comprime i figli invece di mandarli a capo
> Origine: `maui-show-card-labels` (28 lug 2026).

In un `FlexLayout` con `Wrap="Wrap"`, lo `Shrink` di default (`1`) permette di **restringere gli item
sotto la loro larghezza naturale** pur di farceli stare in riga: dei chip con testo mostrano
"spesa" come "spes". Per avere davvero l'andare a capo serve `FlexLayout.Shrink="0"` sui figli.

Correlato: un `Label` che arriva al bordo del suo contenitore può perdere l'ultima lettera per
arrotondamento nella misura del testo su Android (`"farmacia"` → `"farmaci"`). Qualche unità di
padding orizzontale in più lo assorbe.

### `CollectionView` non ha `Padding`
`ItemsView` non espone `Padding` (l'uso dà `error MAUIX2002` in compilazione). Per riservare spazio
in fondo alla lista — es. perché l'ultima riga non finisca sotto a un elemento sovrapposto — usare
un `CollectionView.Footer` con un elemento trasparente dell'altezza voluta: fa parte del contenuto
**scrollabile**. Un `Margin` inferiore compila ma riduce il viewport, creando una fascia in cui la
lista non scorre più.

---

## Build Android — trappole

### Crash a `Theme.MaterialComponents` dopo build incrementali: pulire `obj/`
Sintomo: l'app crasha all'avvio, prima di mostrare qualunque pagina, con

```
java.lang.IllegalArgumentException: This component requires that you specify a valid
TextAppearance attribute. Update your app theme to inherit from Theme.MaterialComponents
    at com.google.android.material.navigation.NavigationBarView.<init>
    at com.microsoft.maui.PlatformInterop.createNavigationBar
    at ...ShellItemRenderer.onCreateView
```

La traccia punta al tema Android e alla creazione della bottom navigation di Shell, quindi **non**
al codice appena modificato: è fuorviante. Nel caso incontrato la causa era **stato di build
incrementale stale** in `src/CardMaster/obj` — la stessa `HEAD` compilata da zero in un worktree
partiva senza crash, e `Remove-Item -Recurse src\CardMaster\obj, src\CardMaster\bin` seguito da una
build pulita ha risolto senza toccare una riga di codice.

Prima di attribuire un crash di questo tipo alla change in corso, provare una build pulita.

### `-t:Run` può non reinstallare
`dotnet build -t:Run` a volte **non ripubblica** gli assembly se il suo stato incrementale ritiene
il device già aggiornato: si lancia la app vecchia e la modifica sembra "non aver avuto effetto".
Se lo screenshot non mostra la modifica appena fatta, `adb uninstall com.cardmaster.app` e
ridistribuire (**non** `pm clear`, vedi la skill `android-emulator`).

### Spazio esaurito sull'emulatore
`error ADB0010: ... java.io.IOException: Requested internal only, but not enough space` significa
partizione `/data` piena. `pm trim-caches` in genere non libera nulla e le immagini playstore non
sono rootabili: usare un altro AVD, oppure disinstallare app di terze parti da quello pieno
(`pm list packages -3` per vederle).
