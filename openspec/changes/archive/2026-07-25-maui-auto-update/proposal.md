## Why

L'app è distribuita fuori dal Play Store (APK firmato pubblicato come GitHub Release, vedi `ci-release`), quindi non esiste un canale di aggiornamento automatico dello store. Senza un controllo integrato, l'utente deve ricordarsi di controllare manualmente le Release su GitHub e scaricare l'APK a mano: l'app resta ferma alla versione installata e le correzioni/nuove funzionalità non arrivano. Chiude l'ultimo punto aperto della v1 (vedi `PLAN.md`).

## What Changes

- Nuova pagina/sezione "Controllo aggiornamenti" nelle Impostazioni: mostra la versione installata (già presente), l'esito dell'ultimo controllo ed espone l'azione "Verifica aggiornamenti".
- Controllo versione: il repository GitHub è **pubblico** (decisione 25 lug 2026, vedi Impact), quindi l'app interroga direttamente le API pubbliche di GitHub — la Release con tag `latest` (già pubblicata da `ci-release` a ogni push su `main`) — senza bisogno di token né di infrastruttura aggiuntiva.
- Confronto versione installata vs versione remota; se la remota è diversa, l'utente vede un avviso con l'opzione di scaricare e installare l'aggiornamento.
- Download dell'APK con avanzamento visibile, verifica di integrità del file scaricato prima dell'installazione (checksum SHA-256 quando l'API GitHub lo espone per l'asset; in ogni caso la firma dell'APK è verificata dal package installer di Android contro il certificato dell'app già installata).
- Avvio dell'installazione tramite intent del package installer di sistema (`REQUEST_INSTALL_PACKAGES`), con richiesta del permesso "Installa app sconosciute" se non ancora concesso.
- Gestione errori (rete assente, rate limit GitHub, download interrotto, verifica fallita, permesso negato) con messaggi chiari e possibilità di riprovare.
- Funzione interamente opt-in su azione utente: nessun controllo automatico in background che consumi rete senza un'interazione esplicita (in linea col vincolo v1 "solo un tassello online opt-in").

## Capabilities

### New Capabilities
- `app-update`: controllo di nuove versioni via GitHub Releases, download dell'APK, verifica di integrità e avvio dell'installazione tramite package installer di Android.

### Modified Capabilities
- `app-settings`: aggiunge una sezione "Controllo aggiornamenti" nella pagina Impostazioni (stato ultimo controllo, azione "Verifica aggiornamenti"), analoga per pattern alla sezione backup Google Drive già presente.

## Impact

- **Decisione 25 lug 2026: il repository GitHub (`ianselmi/cardmaster`) passa da privato a pubblico.** Motivo: GitHub Pages per repository privati richiede un piano a pagamento; reso pubblico il repo, l'app può interrogare direttamente le API/Release pubbliche di GitHub senza bisogno di alcuna infrastruttura aggiuntiva (niente manifest, niente GitHub Pages, niente step CI extra). Verificato che il repository e la sua cronologia non contengono segreti (nessun keystore, password o token; il Google OAuth Client ID presente in `GoogleOAuthConfig.cs` non è un segreto per design — flusso PKCE — ed è comunque già esposto in ogni APK distribuito).
- Nuovo servizio client che interroga `GET https://api.github.com/repos/ianselmi/cardmaster/releases/tags/latest`, senza necessità di token (rate limit anonimo di 60 richieste/ora per IP più che sufficiente per controlli manuali).
- Nuovo permesso Android nel manifest: `REQUEST_INSTALL_PACKAGES` (e gestione del relativo consenso utente su Android 8+).
- Nessun impatto sulla pipeline `ci-release`: si riusa la Release "latest" già pubblicata, nessun nuovo artifact/manifest da generare in CI.
- Nessun impatto sul core offline: la funzione è isolata dietro un'interfaccia, raggiungibile solo dalle Impostazioni, e non viene mai invocata automaticamente.
