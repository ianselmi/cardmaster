# CardMaster

App Android offline-first per conservare e mostrare i codici a barre delle carte fedeltà: scansiona una carta, tienila in lista, mostrala al volo alla cassa. Nessun account, nessun server richiesto per l'uso quotidiano — funziona interamente sul telefono.

## Download

L'app non è distribuita tramite Play Store. L'ultimo APK firmato è sempre disponibile sulla [Release GitHub `latest`](https://github.com/ianselmi/cardmaster/releases/tag/latest). Dopo l'installazione, l'app stessa propone gli aggiornamenti successivi dalla sezione **Impostazioni → Controllo aggiornamenti**.

## Stack tecnico

.NET MAUI (Android) · SQLite locale · ML Kit per la scansione dei barcode · ZXing.Net + SkiaSharp per il rendering · GitHub Actions per build/firma/rilascio.

## Build locale

Prerequisiti: .NET SDK nella versione indicata in [`global.json`](global.json), con il workload `maui-android` installato (`dotnet workload install maui-android`).

```bash
dotnet build src/CardMaster/CardMaster.csproj -f net10.0-android
```

Le build locali/Debug non richiedono il keystore di firma (usato solo in CI per le build Release).

## Piano di sviluppo e decisioni

Per il piano di sviluppo completo, i vincoli architetturali e le decisioni prese, vedi [`PLAN.md`](PLAN.md). Le singole feature sono tracciate con [OpenSpec](openspec/) (`openspec/specs/` per il comportamento corrente, `openspec/changes/` per le change in corso o archiviate).
