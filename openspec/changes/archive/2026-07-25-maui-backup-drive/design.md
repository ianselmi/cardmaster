## Context

CardMaster è un'app MAUI Android offline-first: le carte vivono in un database SQLite locale in chiaro (`cardmaster.db3`), aperto come connessione singleton da `DatabaseService`. Non esiste alcun backend applicativo (rimandato a v2). Finora l'unica perdita di dati possibile è irreversibile: reinstallazione o cambio device = carte perse.

Questa change introduce l'**unica funzione di rete della v1**: un backup opt-in del database su Google Drive, con schedulazione e UX di avanzamento stile WhatsApp. Il core resta offline; il backup è isolato dietro interfacce e disattivabile. È il primo pezzo di infrastruttura OAuth del progetto e anticipa parte dell'auth di v2 (PKCE + refresh token in `SecureStorage`).

Vincoli architetturali ereditati e non in discussione: Id carte client-generati, tombstone (mai DELETE fisico), DB in chiaro sul device, build AOT/trimming-friendly (nessuna reflection fragile, come già fatto in `CardShareCodec`), APK firmato in CI con keystore-secret.

## Goals / Non-Goals

**Goals:**
- Mettere al sicuro l'intero database su Google Drive e poterlo ripristinare, senza server nostri.
- Ambito di accesso a Drive **minimo** (`drive.appdata`): nessun accesso ai file dell'utente.
- Backup manuale + schedulato (Mai / A ogni apertura / Giornaliero / Settimanale) con notifica di avanzamento.
- Ritenzione degli ultimi 3 backup.
- Tutto dietro interfacce testabili; il core offline resta indipendente.

**Non-Goals:**
- Nessuna sincronizzazione multi-device né merge dei dati (è v2). Il backup è un **blob db3 opaco**; il restore è un **replace** dell'intero DB.
- Nessuna scelta della cartella di destinazione (`appdata` è unica e nascosta) né browsing di Drive.
- Nessuna cifratura applicativa del backup (il DB è già in chiaro sul device; il file vive nella cartella privata dell'app su Drive dell'utente).
- Nessun backup di preferenze/impostazioni: solo il database delle carte.
- Niente backup su file locale esportabile (idea `maui-backup-local` abbandonata e rimpiazzata da questa change).

## Decisions

### D1 — Scope Google `drive.appdata` (non `drive.file` né `drive` completo)

Il backup vive nella **cartella applicativa nascosta** di Drive. Alternative considerate:
- `drive` (completo): permetterebbe la scelta di una cartella arbitraria, ma è uno scope **restricted** → per un'app verificata Google richiede una security assessment annuale a pagamento. Scartato.
- `drive.file`: accesso ai soli file creati dall'app, con una cartella visibile; comunque scope "sensitive". Più del necessario dato che non serve mostrare i backup nella UI di Drive.
- `drive.appdata` (**scelto**): cartella nascosta per-app, ambito minimo, l'utente non vede/gestisce i file da Drive. Conseguenza accettata: la lista e la gestione dei backup vivono **interamente in-app**.

Scope aggiuntivo `openid email` solo per mostrare l'account collegato nella UI.

### D2 — OAuth 2.0 Authorization Code + PKCE via `WebAuthenticator`, niente SDK Google

Autenticazione con `Microsoft.Maui.Authentication.WebAuthenticator` (Authorization Code + PKCE, nessun client secret nell'APK). Scambio token e refresh fatti a mano con `HttpClient` sugli endpoint OAuth di Google. Il `refresh_token` è persistito in `SecureStorage` (Keystore-backed); l'`access_token` sta in memoria e viene rinnovato on-demand e su `401`.

Alternativa scartata: pacchetti `Google.Apis.Auth` / `Xamarin.Google.*` — pesanti, con rischi di trimming/AOT e dipendenze native non necessarie per un flusso PKCE semplice.

OAuth client di **tipo Android** legato a package name + **SHA-1 del certificato di firma di release** (quello del keystore CI). Debug e release hanno SHA-1 diversi: entrambi vanno registrati per poter testare. Documentato in `docs/`.

### D3 — Drive v3 via REST su `HttpClient` grezzo + `System.Text.Json` source-gen

Nessun SDK `Google.Apis.Drive`. Si usano direttamente:
- `about.get?fields=storageQuota` — spazio (limit/usage; `limit` assente = illimitato).
- `files.list?spaces=appDataFolder&fields=files(id,name,modifiedTime,size)&orderBy=modifiedTime desc` — lista/ultimo backup.
- upload **multipart** (`uploadType=multipart`) con `parents:["appDataFolder"]` — creazione backup (payload da KB, no resumable).
- `files.get?alt=media` — download per il restore.
- `files.delete` — ritenzione.

Serializzazione/deserializzazione con `System.Text.Json` **source-generated** (no reflection), coerente con `CardShareCodec`. Astrazione: `IDriveBackupClient`.

### D4 — Snapshot con `VACUUM INTO`, backup = blob db3 opaco

Il backup è prodotto con `VACUUM INTO '<tempfile>'` su un file nella **cache app-privata**: copia transazionalmente consistente e compattata, con checkpoint del WAL, senza bloccare l'app. Nome file versionato: `cardmaster-<timestampUtc>-v<schemaVersion>.db3`. La versione di schema nel nome è la guardia per il restore.

Alternativa scartata: export JSON con merge-by-Id/last-write-wins. Utile solo per multi-device (v2); qui aggiungerebbe complessità senza valore, dato che il restore è un ripristino "torna a quello stato".

### D5 — Restore = replace dell'intero DB, con reset della connessione singleton e snapshot di sicurezza

Il restore scarica il file scelto e **sostituisce** `cardmaster.db3`. Poiché `DatabaseService` mantiene una `SQLiteAsyncConnection` singleton, il servizio deve esporre una chiusura/reset atomica: `CloseAsync()` → swap del file → riapertura lazy (che riapplica lo schema/migrazione come all'avvio). Prima dello swap: **conferma distruttiva** obbligatoria, **guardia di versione** (rifiuto se lo schema del backup è più recente di quello supportato) e **snapshot di sicurezza** del DB corrente (`VACUUM INTO` in cache locale) per consentire un **undo immediato** in caso di errore o ripensamento. Le pagine/liste vanno ricaricate dopo il restore.

### D6 — Schedulazione: WorkManager (periodici) + hook d'avvio ("a ogni apertura")

- "A ogni apertura": trigger all'avvio dell'app (in `App`/`AppShell`), nessun job di sistema.
- "Giornaliero"/"Settimanale": Android **WorkManager** periodic work (periodo minimo di sistema ~15 min, quindi giornaliero/settimanale sono largamente sopra la soglia) con `Constraints` di rete connessa. MAUI non wrappa WorkManager → implementazione in `Platforms/Android` dietro `IBackupScheduler` (astrazione cross-platform, no-op fuori Android).

Il worker risolve i servizi (auth, drive client) e riusa lo stesso percorso del backup manuale.

### D7 — Notifica di avanzamento via foreground service

Backup (manuale e schedulato) eseguito entro un **foreground service** Android con notifica "Backup in corso…" e notifica finale di esito. Serve canale notifiche + permesso `POST_NOTIFICATIONS` (13+) richiesto all'abilitazione, e dichiarazione FGS type `FOREGROUND_SERVICE_DATA_SYNC` (14+). Per payload da KB l'upload è quasi istantaneo: il foreground service è soprattutto UX ("come WhatsApp") e garanzia di esecuzione del job schedulato.

### D8 — Stato locale del backup

Stato persistito localmente (via lo store preferenze esistente / `Preferences`): abilitato sì/no, email account, frequenza, e cache di last-backup (timestamp, dimensione) e quota. La cache alimenta la UI offline; viene aggiornata dopo ogni backup e all'apertura della sezione con rete.

## Risks / Trade-offs

- **OAuth in "Testing" mode → refresh token a 7 giorni** → mitigazione: portare la app in publishing status "In production" sul consent screen (gli scope `appdata`/`email` non sono *restricted*, quindi niente security assessment a pagamento); documentare il passo in `docs/`.
- **SHA-1 di firma sbagliato → redirect OAuth fallisce** → mitigazione: registrare gli SHA-1 di debug e release; documentare in `docs/` e collegarli al keystore CI di `ci-build-apk`.
- **Doze / battery-killer OEM (Xiaomi, Huawei, …) ritardano o uccidono i job periodici** → mitigazione: comunicare che la schedulazione è "best-effort" (come WhatsApp), offrire sempre il backup manuale, usare i constraint minimi.
- **Restore distruttivo cancella i dati correnti** → mitigazione: conferma esplicita **+ snapshot di sicurezza pre-restore** nella cache locale (sempre, non opzionale) che permette un annullamento immediato del ripristino.
- **Backup non cifrato nel cloud dell'utente** → accettato: coerente col DB in chiaro sul device (decisione v1) e confinato alla cartella app-privata dell'account dell'utente; nessun dato lascia il perimetro utente+Google.
- **Trimming/AOT rompe la (de)serializzazione** → mitigazione: `System.Text.Json` source-gen e nessuna reflection, come in `CardShareCodec`.
- **`about.get` con solo scope `appdata`** → da verificare in fase di implementazione che lo scope basti per leggere `storageQuota`; in caso contrario, degradare mostrando solo dimensione dell'ultimo backup senza quota.

## Migration Plan

1. Nessuna migrazione dati: il database e lo schema restano invariati; si aggiungono solo snapshot/restore.
2. `DatabaseService` retro-compatibile: si aggiungono `SnapshotAsync`/`CloseAsync` senza cambiare il comportamento d'avvio esistente.
3. Feature completamente opt-in e disattivata di default: chi non abilita il backup non vede alcun cambiamento funzionale.
4. Rollback: disabilitando il backup si rimuovono credenziali e schedulazione; i backup su Drive restano e non influenzano il core offline.
5. Aggiornare `PLAN.md` per il reframe (v1 = core offline + backup Drive opt-in; rimozione di `maui-backup-local`).

## Open Questions

_Nessuna aperta. Decisioni chiuse:_

- **Snapshot di sicurezza pre-restore**: **incluso in v1**. Prima di ogni replace il DB corrente viene copiato in cache locale (`VACUUM INTO`) per un undo immediato (vedi D5).
- **Multi-account Google**: **un solo account collegato alla volta**. Il cambio account avviene tramite disconnetti + riconnetti; non è previsto il collegamento simultaneo di più account in v1.
