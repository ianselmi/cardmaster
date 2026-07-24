## 1. Configurazione di firma nel progetto

- [x] 1.1 Aggiungere al `.csproj` un `PropertyGroup` di firma **condizionale** (Release + `AndroidSigningKeyStore` valorizzato): `AndroidKeyStore=true` e le proprietà di firma; non deve rompere build Debug/local senza keystore
- [x] 1.2 Assicurare `AndroidPackageFormat=apk` per la produzione dell'APK in Release

## 2. Workflow GitHub Actions

- [x] 2.1 Creare `.github/workflows/build-apk.yml` con trigger `push` (main), `workflow_dispatch`, `push` tag `v*`
- [x] 2.2 Step ambiente: checkout, setup-java (Temurin 17), setup-dotnet (.NET 10 del progetto via global.json), `dotnet workload install maui-android`
- [x] 2.3 Step firma: decodificare `ANDROID_KEYSTORE_BASE64` in un file keystore nel workspace
- [x] 2.4 Step build: `dotnet publish -c Release -f net10.0-android -p:AndroidPackageFormat=apk` con proprietà di firma e versionamento (versionCode=run_number, versionName=tag/fallback)
- [x] 2.5 Step pubblicazione: individuare l'APK firmato e pubblicarlo (prerelease `latest` su main/dispatch; release stabile su tag `v*`)

## 3. `global.json` e documentazione

- [x] 3.1 Aggiungere un `global.json` che fissa la versione dell'SDK .NET 10 (allineata al progetto) per la riproducibilità in CI
- [x] 3.2 Creare `docs/ci-release.md`: generazione keystore (`keytool`), codifica base64, elenco secret, come lanciare la pipeline e taggare una release

## 4. Verifica

- [x] 4.1 Verificare la sintassi/coerenza del workflow YAML — *YAML valido: job `build`, trigger push/tag/dispatch*
- [x] 4.2 Build Release locale firmata (keystore temporaneo, NON committato) → conferma che la config di firma e `AndroidPackageFormat=apk` producono un APK firmato senza errori — *verificato: APK firmato, `apksigner` conferma Signer DN = keystore di test*
- [x] 4.3 Verificare che una build locale **senza** keystore non fallisca (config condizionale) — *verificato: build Release senza keystore OK, 0 errori*
- [x] 4.4 `openspec validate ci-build-apk` senza errori
