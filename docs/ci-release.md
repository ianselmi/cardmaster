# Rilascio APK (CI)

Pipeline: `.github/workflows/build-apk.yml` — compila l'app MAUI Android in Release, **firma** l'APK e lo pubblica come GitHub Release.

- **Push su `main`** (o avvio manuale da *Actions → Build APK → Run workflow*) → aggiorna la prerelease **`latest`** con l'APK più recente.
- **Push di un tag `v*`** (es. `v1.0.0`) → crea una **Release stabile** con quel tag.

Versionamento automatico: `versionCode` = numero di run CI (monotono); `versionName` = tag senza `v` (oppure `1.0.<run>` come fallback su main).

## Prerequisito una tantum: keystore + secret

L'APK va firmato con un keystore che **non deve stare nel repo**. Va generato una volta e caricato come secret.

> ⚠️ **Custodisci il keystore e le password con cura.** Android richiede la **stessa** chiave di firma per tutti gli aggiornamenti: se lo perdi, non potrai più aggiornare un'app già installata (gli utenti dovrebbero disinstallare/reinstallare).

### 1. Genera il keystore

```bash
keytool -genkeypair -v \
  -keystore cardmaster.keystore \
  -alias cardmaster \
  -keyalg RSA -keysize 2048 -validity 10000 \
  -storepass "LA_TUA_STORE_PASSWORD" \
  -keypass "LA_TUA_KEY_PASSWORD" \
  -dname "CN=CardMaster, O=CardMaster, C=IT"
```

### 2. Codificalo in base64

```bash
# Linux/macOS
base64 -w0 cardmaster.keystore > cardmaster.keystore.b64
# Windows (PowerShell)
[Convert]::ToBase64String([IO.File]::ReadAllBytes("cardmaster.keystore")) | Set-Content cardmaster.keystore.b64
```

### 3. Aggiungi i secret su GitHub

Repo → **Settings → Secrets and variables → Actions → New repository secret**:

| Secret | Valore |
|---|---|
| `ANDROID_KEYSTORE_BASE64` | contenuto di `cardmaster.keystore.b64` |
| `ANDROID_KEYSTORE_PASSWORD` | la store password |
| `ANDROID_KEY_ALIAS` | `cardmaster` (o l'alias scelto) |
| `ANDROID_KEY_PASSWORD` | la key password |

## Produrre il primo APK

1. Completa i secret (sopra).
2. Vai su **Actions → Build APK → Run workflow** (branch `main`), oppure fai un push su `main`.
3. Al termine, scarica l'APK dalla release **`latest`** (sezione *Releases* del repo).
4. Sul telefono, abilita *"Installa app sconosciute"* per il browser/file manager e installa l'APK.

## Pubblicare una versione "vera"

```bash
git tag v1.0.0
git push origin v1.0.0
```

La pipeline crea una Release stabile `v1.0.0` con l'APK, `versionName = 1.0.0`.

## Note

- Le build locali/Debug **non** richiedono il keystore: la firma è condizionale (attiva solo quando `AndroidSigningKeyStore` è fornito, es. dalla CI).
- Il "server" di rilascio è solo hosting statico (GitHub Releases), non il backend applicativo.
- L'auto-update in-app (`maui-auto-update`) consumerà questi APK + un manifest `latest.json` (change futura).
