## Why

L'app è distribuita **fuori dal Play Store**, quindi serve un modo ripetibile per produrre un **APK firmato** installabile. Una pipeline CI (GitHub Actions) che compila, firma e pubblica l'APK come Release elimina i passaggi manuali e prepara il terreno per l'auto-update (`maui-auto-update`), che leggerà gli APK pubblicati.

## What Changes

- Nuovo workflow **GitHub Actions** che compila l'app MAUI Android in Release e produce un **APK**.
- **Firma** dell'APK con un keystore custodito come **secret CI** (mai nel repo), decodificato a runtime.
- **Versionamento automatico**: `ApplicationVersion` (versionCode) dal numero di run CI (monotono); `ApplicationDisplayVersion` (versionName) dal tag git se presente, altrimenti un fallback.
- **Pubblicazione**:
  - push su `main` (o avvio manuale) → APK caricato su una **prerelease "latest"** aggiornata in-place;
  - push di un tag `v*` → **Release stabile** con quel tag.
- Configurazione di firma nel `.csproj` **condizionale** (attiva solo quando il keystore è presente), così le build locali/Debug restano invariate.
- Documentazione per generare il keystore e impostare i secret.

## Capabilities

### New Capabilities
- `ci-release`: pipeline di build, firma e pubblicazione dell'APK Android come artifact/Release, con versionamento automatico. Nessuna dipendenza dal backend: è solo hosting statico dell'APK.

### Modified Capabilities
- Nessuna.

## Impact

- **Nuovi file**: `.github/workflows/build-apk.yml`; sezione di firma condizionale nel `.csproj`; istruzioni (`docs/ci-release.md`).
- **Secret richiesti (GitHub → Settings → Secrets)**: `ANDROID_KEYSTORE_BASE64`, `ANDROID_KEYSTORE_PASSWORD`, `ANDROID_KEY_ALIAS`, `ANDROID_KEY_PASSWORD`.
- **Prerequisito utente**: generare un keystore (una tantum) e caricarne i secret. Il keystore va **conservato con cura**: gli aggiornamenti Android richiedono la stessa chiave di firma.
- **Nessun impatto sul codice dell'app** (solo build/config).
- **Vincolo di qualità**: la build Release deve compilare senza errori.
- **Change successive abilitate**: `maui-auto-update` (consuma gli APK pubblicati + un manifest `latest.json`).
