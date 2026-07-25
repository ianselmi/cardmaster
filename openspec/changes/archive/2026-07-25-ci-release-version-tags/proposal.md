## Why

Oggi ogni build su `main` aggiorna in-place la sola prerelease mobile `latest`: non resta uno **storico** navigabile delle build (per scaricare una versione precedente o confrontare) e non c'è un tag di versione per ciascuna. Inoltre la versione dell'app (`1.0.<run>`) è più verbosa del necessario. Vogliamo che **ogni build sia identificata dal suo numero incrementale** (`8`, `9`, `10`…), coerente tra app e release, con `latest` che punta sempre all'ultima.

## What Changes

- La versione dell'app diventa il **numero di build incrementale**: `ApplicationDisplayVersion = <run_number>` (es. `8`), così in Impostazioni l'app mostra lo stesso numero delle release. `ApplicationVersion` (versionCode) resta il run number (invariato).
- Ogni build su `main`/avvio manuale SHALL creare una **Release versionata** taggata col numero di build (es. `8`), con l'APK allegato — costruendo uno storico per versione.
- La **prerelease `latest`** continua a essere aggiornata in-place e a puntare all'ultima build, col titolo uguale al numero di build.
- I tag di versione sono **senza prefisso `v`** (solo il numero), identici al versionName dell'app.
- Il ramo dei tag git `v*` (Release stabili manuali) resta invariato.

## Capabilities

### New Capabilities
- Nessuna.

### Modified Capabilities
- `ci-release`: (1) il **versionName** di fallback (build non da tag) diventa il numero di build incrementale invece di `1.0.<run>`; (2) la pubblicazione crea, per ogni build su `main`, una **Release versionata** taggata col numero di build **oltre** alla prerelease `latest`.

## Impact

- **File modificato**: `.github/workflows/build-apk.yml` — passo *Compute version* (versionName = run number; output del tag di versione) e passi di pubblicazione (una Release versionata + aggiornamento di `latest`).
- **Storico release**: da un push su `main` nasceranno release taggate `8`, `9`, `10`… ciascuna con l'APK; `latest` resta il puntatore all'ultima.
- **Nessun re-trigger**: i tag numerici (`8`) non combaciano con `v*`, quindi non riavviano la pipeline.
- **Ramo `v*` invariato**: le Release stabili da tag git continuano come prima.
- **Verificabile**: dopo un push su `main`, esistono sia la release `<run>` sia `latest`, entrambe col numero di build; l'app in Impostazioni mostra lo stesso numero.
