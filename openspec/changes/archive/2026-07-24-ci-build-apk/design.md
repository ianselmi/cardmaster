## Context

Distribuzione fuori dal Play Store → serve un APK firmato riproducibile. Il "server" di rilascio è solo hosting statico (GitHub Releases), non il backend applicativo. La pipeline prepara anche il terreno per `maui-auto-update`.

Il progetto usa .NET 10 (SDK preview) e il workload MAUI Android; la CI deve installarli.

## Goals / Non-Goals

**Goals:**
- Workflow GitHub Actions che compila Release e produce un APK firmato.
- Firma con keystore da secret, mai nel repo; config di firma condizionale (build locali non richiedono keystore).
- Versionamento automatico (versionCode da run number, versionName da tag con fallback).
- Pubblicazione: prerelease "latest" su main, Release stabile su tag `v*`.
- Istruzioni chiare per keystore + secret.

**Non-Goals:**
- Manifest `latest.json` e download/verifica/installazione in-app → `maui-auto-update`.
- Pubblicazione su Play Store / AAB.
- Build iOS/altri target (l'app è solo Android).
- Test automatici in CI (non ci sono ancora test).

## Decisions

### Trigger e pubblicazione
`on: push (main), workflow_dispatch, push tags v*`. Un solo job.
- main / dispatch → `softprops/action-gh-release` con `tag_name: latest`, `prerelease: true` (aggiorna in-place).
- tag `v*` → release con `tag_name = <tag>`, `prerelease: false`.
- **Alternative considerate**: Release nuova a ogni push → intasa le Release; scartata a favore della prerelease rolling.

### Formato pacchetto — APK (non AAB)
`dotnet publish -f net10.0-android -c Release -p:AndroidPackageFormat=apk`. Serve un APK sideloadabile (l'AAB è per lo Store).

### Firma condizionale nel csproj
Proprietà di firma attive solo in Release e solo se `AndroidSigningKeyStore` è valorizzato, così le build locali/Debug non richiedono keystore. In CI si passano via `-p:` le proprietà `AndroidKeyStore=true`, `AndroidSigningKeyStore`, `AndroidSigningStorePass`, `AndroidSigningKeyAlias`, `AndroidSigningKeyPass`.
- Il keystore arriva come secret **base64** (`ANDROID_KEYSTORE_BASE64`), decodificato in un file nel workspace CI (fuori dal repo).

### Versionamento
- `ApplicationVersion` = `${{ github.run_number }}` (monotono).
- `ApplicationDisplayVersion` = tag senza prefisso `v` se il ref è un tag `v*`, altrimenti `1.0.${{ github.run_number }}` (fallback).
- Passati via `-p:ApplicationVersion=... -p:ApplicationDisplayVersion=...`.

### Ambiente CI
`ubuntu-latest`. Passi: checkout; `actions/setup-java` (Temurin 17); `actions/setup-dotnet` con la versione .NET 10 del progetto (quality preview / `global.json`); `dotnet workload install maui-android`; decode keystore; `dotnet publish`; individua l'APK firmato in `bin/Release/net10.0-android/publish/` (o `*-Signed.apk`); pubblica.
- **Rischio**: allineamento della versione SDK .NET 10 preview su runner hosted → si fissa con `global.json` o `dotnet-version` esplicita.

### Secret
`ANDROID_KEYSTORE_BASE64`, `ANDROID_KEYSTORE_PASSWORD`, `ANDROID_KEY_ALIAS`, `ANDROID_KEY_PASSWORD`. Documentati in `docs/ci-release.md` con il comando `keytool` per generare il keystore e `base64` per codificarlo.

## Risks / Trade-offs

- **SDK .NET 10 preview su CI** → fissare con `global.json`; il workload install può allungare i tempi (accettabile).
- **Keystore da custodire** → se perso, impossibile aggiornare l'app (Android richiede la stessa chiave). Documentato con enfasi.
- **Verifica limitata in locale** → non posso eseguire GitHub Actions da qui; si valida la build Release firmata localmente (con keystore temporaneo) e la sintassi del workflow. Il primo run reale lo fa l'utente dopo aver impostato i secret.
- **Prerelease "latest" sovrascritta** → per definizione mostra sempre l'ultimo build di main; le versioni "vere" restano nei tag.

## Migration Plan

Solo aggiunta di CI/config; nessun impatto sui dati o sul codice app. Rollback = rimozione del workflow e della config di firma.

## Open Questions

- Nome/prefisso esatti dei tag di versione (`v1.0.0`): convenzione `v*` assunta.
- Se in futuro serve l'AAB per lo Store, si aggiunge un job separato.
