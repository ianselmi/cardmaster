## Context

L'utente crea e vede le carte in griglia (`maui-card-grid`), ma non può ancora usarle alla cassa. Questa change apre una carta e mostra il barcode a schermo intero, ottimizzato per la lettura del cassiere. Poggia sul repository cifrato (`GetByIdAsync`) e sui formati stabili definiti in `BarcodeFormatCatalog`.

Decisioni prese in esplorazione (24 lug 2026): rendering con ZXing.Net + SkiaSharp; codice in chiaro sempre visibile; sfondo bianco anche in dark; luminosità al massimo con ripristino al default di sistema; keep-awake; rilevamento best-effort del filtro luce blu con avviso (non è possibile disattivarlo da app senza permesso privilegiato).

## Goals / Non-Goals

**Goals:**
- Pagina di visualizzazione carta aperta dal tap sul tile.
- Rendering barcode 1D/2D nero-su-bianco, grande e centrato, sfondo bianco anche in dark.
- Codice in chiaro sempre visibile; gestione robusta del barcode non generabile.
- Luminosità max + keep-awake durante la visualizzazione, con ripristino all'uscita.
- Rilevamento best-effort del Night Light + avviso non bloccante.
- Compilazione senza errori.

**Non-Goals:**
- Modifica/eliminazione carta; condivisione via QR (`maui-share-qr`).
- Disattivazione automatica del filtro luce blu (richiede `WRITE_SECURE_SETTINGS`, privilegiato).
- Rotazione landscape dedicata.
- Tracciamento "ultimo utilizzo" per la barra recenti → futura `maui-card-search`.

## Decisions

### Rendering — ZXing.Net + ZXing.Net.Bindings.SkiaSharp
Si usa `ZXing.SkiaSharp.BarcodeWriter` per generare un `SKBitmap` (nero su bianco), lo si codifica in PNG e lo si espone come `ImageSource` (via `ImageSource.FromStream`). Incapsulato in un `IBarcodeRenderer` (servizio) per isolare la libreria e permettere il fallback.
- **Alternative considerate**: ZXing.Net.MAUI generator → accoppia una libreria di scansione non usata (scansioniamo con ML Kit), compat .NET 10 incerta. Scartata. ZXing.Net core con writer manuale → possibile fallback se il binding SkiaSharp desse problemi su Android.
- **Da verificare in apply**: che `ZXing.Net.Bindings.SkiaSharp` compili e renda correttamente su net10.0-android.

### Mappatura formati → enum ZXing
`BarcodeFormat` (stringa nostra) → `ZXing.BarcodeFormat`: EAN13→EAN_13, EAN8→EAN_8, UPCA→UPC_A, UPCE→UPC_E, CODE128→CODE_128, CODE39→CODE_39, ITF→ITF, CODABAR→CODABAR, QR_CODE→QR_CODE, PDF417→PDF_417. Dimensioni: 1D ~600×240, QR ~500×500, PDF417 ~600×300 (margine "quiet zone" incluso).

### Robustezza del rendering
Il writer di ZXing lancia se il valore non è conforme (es. EAN-13 con lunghezza/checksum errati — possibile perché alla cattura non validiamo). Il servizio cattura l'eccezione e restituisce un esito "non generabile"; la pagina mostra allora **solo il codice in chiaro** + messaggio. Nessun crash. Il codice in chiaro sempre visibile è la rete di sicurezza.

### Sfondo bianco a prescindere dal tema
L'immagine è nero-su-bianco; il contenitore del barcode ha `BackgroundColor` bianco esplicito (non legato al tema), così in dark mode il contrasto resta.

### Luminosità e keep-awake (ciclo di vita pagina)
- **Keep-awake**: `DeviceDisplay.Current.KeepScreenOn = true` in `OnAppearing`, `false` in `OnDisappearing` (API cross-platform, niente codice nativo).
- **Luminosità**: helper di piattaforma Android (`IScreenBrightnessController`) che imposta `Activity.Window.Attributes.ScreenBrightness = 1f` in `OnAppearing` e `-1f` (default di sistema) in `OnDisappearing`. Interfaccia astratta + implementazione in `Platforms/Android`, registrata in DI (come `IKeyStoreService`).

### Rilevamento filtro luce blu (best-effort)
Helper Android che legge `Settings.Secure.GetInt(resolver, "night_display_activated", 0)` (chiave AOSP). Se = 1 → filtro attivo → avviso non bloccante (es. banner/label). Se la chiave non esiste o la lettura fallisce (OEM diversi) → nessun avviso. Documentato come best-effort: **niente falsi allarmi**. Esposto dietro `IReadingFilterProbe` con esito tri-stato (attivo / non attivo / sconosciuto).

### Navigazione
Rotta `ShowCardPage` con parametro `id`. Il tap sul tile (in `card-list`) usa `SelectionChanged` o un `TapGestureRecognizer` per navigare a `ShowCardPage?id=<Id>`. `ShowCardViewModel` carica la carta con `ICardRepository.GetByIdAsync`; se null → torna alla lista.

## Risks / Trade-offs

- **Compat `ZXing.Net.Bindings.SkiaSharp` su .NET 10 Android** → verifica in apply; fallback: writer manuale su ZXing.Net core. [Rischio principale]
- **Rilevamento Night Light non affidabile tra OEM** → mitigato dalla scelta "nessun falso allarme": in dubbio non si avvisa.
- **Valori barcode non conformi** → gestiti con fallback al codice in chiaro, mai crash.
- **Luminosità non ripristinata se la pagina non riceve OnDisappearing** (kill improvviso) → accettabile: al riavvio l'app non forza nulla; la luminosità è per-window.

## Migration Plan

Nessuna migrazione dati: si aggiungono pagina, servizio di rendering, helper di piattaforma e una dipendenza NuGet. Lo schema del DB non cambia. Rollback = rimozione della pagina/servizi e della dipendenza.

## Open Questions

- Dimensioni esatte del barcode e margini ottimali per la lettura: da rifinire con prova a video in apply.
- Conferma sperimentale che il binding SkiaSharp renda correttamente su Android (altrimenti fallback writer manuale).
