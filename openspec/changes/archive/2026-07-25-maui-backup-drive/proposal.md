## Why

Oggi i dati delle carte vivono solo nel database locale del device: reinstallando l'app, cambiando telefono o perdendolo, l'utente perde tutte le carte. Serve un modo per mettere al sicuro il database e ripristinarlo, senza introdurre un backend applicativo. Google Drive offre lo storage cloud dell'utente stesso: un backup opt-in su Drive risolve il problema restando 100% nel perimetro del device + account personale dell'utente, senza server nostri.

Questa change **rimpiazza** la precedente idea `maui-backup-local` (backup su file locale esportabile): il backup su file richiede comunque all'utente di custodire e non perdere il file, mentre il cloud dell'utente è più robusto e automatizzabile (schedulazione stile WhatsApp).

## What Changes

- **Reframe di v1**: il core dell'app resta 100% offline; il backup su Google Drive è l'**unica** funzione di rete, **opt-in** e disattivabile. **BREAKING** rispetto al posizionamento "100% offline": aggiornare `PLAN.md` (rimuovere `maui-backup-local`, portare il backup Drive in v1, adeguare il wording).
- Nuova **sezione "Backup su Google Drive"** nelle Impostazioni: abilita/disabilita (con autenticazione Google), account collegato, data e dimensione dell'ultimo backup, spazio disponibile su Drive, frequenza di backup, azioni "Fai backup ora" e "Ripristina da un backup…".
- **Autenticazione Google** OAuth 2.0 Authorization Code + PKCE (nessun client secret nell'APK), con `refresh_token` in `SecureStorage`. Scope **minimo**: `drive.appdata` (cartella nascosta per-app) + `openid email` per mostrare l'account.
- **Backup**: snapshot consistente del DB (`VACUUM INTO`) caricato come blob db3 opaco nella cartella `appdata`, con nome versionato (timestamp + versione schema). Ritenzione: si conservano gli **ultimi 3** backup, i più vecchi vengono eliminati.
- **Ripristino**: lista in-app dei backup disponibili (i file `appdata` non sono visibili nella UI di Drive), download del backup scelto e **replace** dell'intero database, con conferma distruttiva e guardia di versione schema (no downgrade).
- **Schedulazione**: "Mai / A ogni apertura / Giornaliero / Settimanale". I backup periodici usano Android WorkManager (vincolo: rete connessa).
- **Notifica di avanzamento** stile WhatsApp: durante il backup viene mostrata una notifica "Backup in corso…" tramite foreground service, con notifica di completamento.

## Capabilities

### New Capabilities
- `cloud-backup`: backup e ripristino del database su Google Drive (autenticazione Google, snapshot/upload, ritenzione, lista/restore, schedulazione, notifica di avanzamento). Copre l'intera feature online opt-in.

### Modified Capabilities
- `app-settings`: aggiunta di una sezione dedicata "Backup su Google Drive" raggiungibile dalle Impostazioni, che espone stato, informazioni (ultimo backup, spazio, account) e azioni di backup/ripristino/schedulazione.

## Impact

- **Codice nuovo** (`src/CardMaster`): servizi dietro interfacce testabili — `IGoogleAuth` (OAuth PKCE via `WebAuthenticator`), `IDriveBackupClient` (Drive v3 REST su `HttpClient`), `IBackupScheduler` (WorkManager). Nuova pagina/sezione + ViewModel nelle Impostazioni. Foreground service Android per la notifica di avanzamento.
- **`DatabaseService`**: deve esporre snapshot (`VACUUM INTO`) e **reset/chiusura della connessione singleton** per consentire lo swap del file in fase di restore.
- **Manifest Android**: permessi `INTERNET`, `POST_NOTIFICATIONS` (13+), `FOREGROUND_SERVICE` + `FOREGROUND_SERVICE_DATA_SYNC` (14+). Nessun permesso di storage (lo snapshot vive nella cache app-privata).
- **Dipendenze**: nessun SDK `Google.Apis` pesante — Drive v3 via `HttpClient` grezzo + `System.Text.Json` source-gen (robusto a trimming/AOT, coerente con `CardShareCodec`).
- **Configurazione/CI**: OAuth client di tipo Android legato a package name + **SHA-1 del certificato di firma di release** (keystore-secret CI). Nuova documentazione in `docs/` per il setup OAuth (client id, SHA-1) e i caveat di background (Doze / battery-killer OEM).
- **`PLAN.md`**: aggiornato per il reframe (v1 = core offline + backup Drive opt-in; `maui-backup-local` rimosso).
- **Non impattato**: la semantica di `local-storage` (Id client-generati, tombstone, mai DELETE fisico) resta invariata; il backup tratta il DB come blob opaco e il restore è un replace dell'intero file.
