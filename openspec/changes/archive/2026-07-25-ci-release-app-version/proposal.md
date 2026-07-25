## Why

Su push in `main` la CI pubblica una **prerelease** con tag `latest` e titolo fisso *"Ultima build (main)"*, mentre l'app viene compilata con `ApplicationDisplayVersion = 1.0.<run_number>` (la versione visibile in Impostazioni). Il numero mostrato nella pagina della release GitHub quindi **non coincide** con la versione installata sul telefono: guardando la Release non si capisce quale build si sta scaricando. Vogliamo che il titolo della prerelease riporti **lo stesso numero di versione dell'app**.

## What Changes

- Il **titolo della prerelease `latest`** (build da `main`/avvio manuale) SHALL riportare il **versionName dell'app** (`ApplicationDisplayVersion`, es. `1.0.42`) invece della stringa fissa "Ultima build (main)".
- Il numero mostrato SHALL essere **lo stesso** valore usato per compilare l'app (`steps.ver.outputs.name`), così che release e app siano sempre allineate.
- Resta invariato tutto il resto: la prerelease continua a usare il tag `latest` aggiornato in-place; i tag `v*` continuano a creare Release stabili col nome del tag; versionCode e firma non cambiano.

## Capabilities

### New Capabilities
- Nessuna.

### Modified Capabilities
- `ci-release`: il requisito di pubblicazione dell'APK precisa che il **nome della prerelease `latest`** riflette il versionName dell'app (coerente con `ApplicationDisplayVersion`), non più un'etichetta fissa.

## Impact

- **File modificato**: `.github/workflows/build-apk.yml` — passo *Publish Release*, espressione del campo `name` (usa `steps.ver.outputs.name` per il ramo prerelease).
- **Nessun impatto** su build locale, firma, versionCode o sul comportamento dei tag `v*`.
- **Verificabile**: dopo un push su `main`, la prerelease `latest` mostra come titolo il numero `1.0.<run>` uguale a quello visibile in Impostazioni dell'app.
