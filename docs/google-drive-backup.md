# Backup su Google Drive — setup OAuth

Il backup (`maui-backup-drive`, vedi `openspec/changes/maui-backup-drive`) autentica l'utente con OAuth 2.0 **Authorization Code + PKCE** (nessun client secret nell'APK) e scrive solo nella cartella applicativa nascosta di Drive (`drive.appdata`). Prima di poter testare il flusso serve creare l'OAuth client su Google Cloud Console — passo manuale, non automatizzabile da CI.

## 1. Crea il progetto e abilita la Drive API

1. [Google Cloud Console](https://console.cloud.google.com/) → crea (o riusa) un progetto.
2. **APIs & Services → Library** → abilita **Google Drive API**.

## 2. Configura la consent screen

**APIs & Services → OAuth consent screen**:

- **User type**: External.
- **Scopes**: aggiungi `.../auth/drive.appdata` e `openid`/`email`. Sono scope **non sensibili/non restricted**: non serve la security assessment annuale a pagamento richiesta invece da `drive` o `drive.file`.
- **Publishing status**: portalo su **In production** appena possibile. In stato *Testing* i refresh token scadono dopo **7 giorni**, il che rompe il backup schedulato silenziosamente (l'utente si ritroverebbe a dover ri-autenticarsi ogni settimana).

## 3. Crea l'OAuth client (tipo Android)

**APIs & Services → Credentials → Create credentials → OAuth client ID → Android**:

- **Package name**: `com.cardmaster.app` (da `CardMaster.csproj`, `ApplicationId`).
- **SHA-1 del certificato di firma**: vanno registrati **entrambi**, debug e release (SHA-1 diversi → redirect OAuth fallisce se manca quello della build in uso).

### SHA-1 di debug

```powershell
keytool -list -v -keystore "$env:USERPROFILE\.android\debug.keystore" -alias androiddebugkey -storepass android -keypass android
```

### SHA-1 di release

Quello del keystore CI (vedi `docs/ci-release.md`), lo stesso con cui l'APK viene firmato in produzione:

```powershell
keytool -list -v -keystore cardmaster.keystore -alias cardmaster -storepass "LA_TUA_STORE_PASSWORD"
```

Copia la riga `SHA1:` (formato `AA:BB:CC:...`) di entrambi gli output e registrali come due voci separate sull'OAuth client Android (Google Cloud permette un solo package name ma più SHA-1 sullo stesso client).

## 4. Riporta il client id nell'app

Copia il **client ID** generato (termina con `.apps.googleusercontent.com`) in `src/CardMaster/Services/Backup/GoogleOAuthConfig.cs`, sostituendo il placeholder `REPLACE_WITH_ANDROID_OAUTH_CLIENT_ID.apps.googleusercontent.com`. Non è un secret (il flusso è PKCE, senza client secret), quindi può restare nel codice sorgente.

Il redirect URI (`com.cardmaster.app:/oauth2redirect`) è già cablato nel client — non richiede configurazione lato Google Cloud oltre alla registrazione del package/SHA-1: `WebAuthenticator` intercetta lo scheme custom via l'intent filter di `WebAuthenticationCallbackActivity` (`Platforms/Android`).

## Caveat di background (Doze / battery killer OEM)

I backup schedulati (Giornaliero/Settimanale) girano su **Android WorkManager**, con vincolo di rete connessa. Alcuni comportamenti sono **best-effort**, non garantiti:

- **Doze mode** (standby prolungato): WorkManager rispetta le finestre di manutenzione del sistema — un job può slittare di ore rispetto all'orario "ideale".
- **Battery killer OEM** (Xiaomi/MIUI, Huawei/EMUI, Oppo/ColorOS, e simili): uccidono aggressivamente i processi in background anche con WorkManager corretto, spesso richiedendo che l'utente disabiliti manualmente l'ottimizzazione batteria per l'app (impostazione fuori dal controllo dell'app stessa).
- **Mitigazione**: il backup manuale ("Fai backup ora") è sempre disponibile e non dipende da alcuna schedulazione di sistema; è il modo affidabile per garantire un backup aggiornato.

Non c'è azione da fare in-app oltre a quanto già implementato (constraint minimi, `PeriodicWorkRequest`): è un limite noto della piattaforma, comunicato in UI come aspettativa ("come WhatsApp"), non un bug da inseguire.
