# Backup su Google Drive — setup OAuth

Il backup (`maui-backup-drive`, vedi `openspec/changes/maui-backup-drive`) autentica l'utente con OAuth 2.0 **Authorization Code + PKCE** (nessun client secret nell'APK) e scrive solo nella cartella applicativa nascosta di Drive (`drive.appdata`). Prima di poter testare il flusso serve creare l'OAuth client su Google Cloud Console — passo manuale, non automatizzabile da CI.

## 1. Crea il progetto e abilita la Drive API

1. [Google Cloud Console](https://console.cloud.google.com/) → crea (o riusa) un progetto.
2. **APIs & Services → Library** → abilita **Google Drive API**.

## 2. Configura la consent screen

**APIs & Services → OAuth consent screen**:

- **User type**: External.
- **Scopes**: aggiungi `.../auth/drive.appdata` e `openid`/`email`. Google classifica `drive.appdata` come **non-sensitive** (come `drive.file`, lo scope che raccomanda): niente verifica obbligatoria dell'app, e niente security assessment annuale a pagamento — quella riguarda gli scope **restricted** (`drive`, `drive.readonly`, `drive.metadata`…), che qui non servono.
- **Publishing status**: portalo su **In production** appena possibile. In stato *Testing* i refresh token scadono dopo **7 giorni**, il che rompe il backup schedulato silenziosamente (l'utente si ritroverebbe a dover ri-autenticarsi ogni settimana). Con soli scope non-sensitive **basta il bottone "Publish app"**: nessuna verifica da attendere, e non compaiono né la schermata "app non verificata" né il tetto dei 100 utenti (scattano solo per scope sensitive/restricted non verificati). Vanno però compilati i campi obbligatori della consent screen (nome app, email di supporto, contatto sviluppatore); mostrare **nome e logo** nella schermata di consenso richiede in più la *brand verification*, che è estetica e non blocca il backup.
  - **Dopo aver pubblicato, riconnetti l'account una volta.** Il refresh token già in mano all'app è stato emesso sotto *Testing* e muore comunque al settimo giorno: cambiare stato non lo rinnova. Serve un nuovo consenso (azione "Riconnetti l'account Google" nella schermata backup) perché ne venga emesso uno senza scadenza fissa. Saltare questo passaggio fa sembrare che la pubblicazione non abbia avuto effetto.
  - In produzione il refresh token non scade a tempo, ma resta revocabile: revoca dell'utente dall'account Google, **sei mesi** senza usarlo, cambio password. È il motivo per cui lo stato "riconnessione necessaria" serve comunque.

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

### Abilita il custom URI scheme

Sullo stesso client Android, apri **Advanced settings** e attiva **"Enable custom URI scheme"** (poi Save). Senza questo flag il login fallisce con `Error 400: invalid_request` / "Custom URI scheme is not enabled for your Android client", anche con package name e SHA-1 corretti — Google disabilita di default gli scheme custom (come `com.cardmaster.app:/oauth2redirect`) per motivi di sicurezza (rischio app impersonation). Il cambio può richiedere da qualche minuto a un paio d'ore per propagarsi.

## 4. Riporta il client id nell'app

Copia il **client ID** generato (termina con `.apps.googleusercontent.com`) in `src/CardMaster/Services/Backup/GoogleOAuthConfig.cs`, sostituendo il placeholder `REPLACE_WITH_ANDROID_OAUTH_CLIENT_ID.apps.googleusercontent.com`. Non è un secret (il flusso è PKCE, senza client secret), quindi può restare nel codice sorgente.

Il redirect URI (`com.cardmaster.app:/oauth2redirect`) è già cablato nel client — non richiede configurazione lato Google Cloud oltre alla registrazione del package/SHA-1: `WebAuthenticator` intercetta lo scheme custom via l'intent filter di `WebAuthenticationCallbackActivity` (`Platforms/Android`).

## Caveat di background (Doze / battery killer OEM)

I backup schedulati (Giornaliero/Settimanale) girano su **Android WorkManager**, con vincolo di rete connessa. Alcuni comportamenti sono **best-effort**, non garantiti:

- **Doze mode** (standby prolungato): WorkManager rispetta le finestre di manutenzione del sistema — un job può slittare di ore rispetto all'orario "ideale".
- **Battery killer OEM** (Xiaomi/MIUI, Huawei/EMUI, Oppo/ColorOS, e simili): uccidono aggressivamente i processi in background anche con WorkManager corretto, spesso richiedendo che l'utente disabiliti manualmente l'ottimizzazione batteria per l'app (impostazione fuori dal controllo dell'app stessa).
- **Mitigazione**: il backup manuale ("Fai backup ora") è sempre disponibile e non dipende da alcuna schedulazione di sistema; è il modo affidabile per garantire un backup aggiornato.

Non c'è azione da fare in-app oltre a quanto già implementato (constraint minimi, `PeriodicWorkRequest`): è un limite noto della piattaforma, comunicato in UI come aspettativa ("come WhatsApp"), non un bug da inseguire.
