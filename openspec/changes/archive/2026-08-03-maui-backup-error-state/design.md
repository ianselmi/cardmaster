## Context

Il backup su Drive è già completo dal punto di vista funzionale (`maui-backup-drive`): auth PKCE, upload nella `appDataFolder`, ritenzione, ripristino, schedulazione con WorkManager. Quello che manca è la **comunicazione del guasto**.

Stato attuale del codice:

- `DriveBackupClient.EnsureSuccessAsync` costruisce il messaggio d'errore come `$"Drive ha risposto {(int)response.StatusCode}: {Truncate(body)}"` — cioè il JSON grezzo di Google finisce dritto nell'alert MAUI.
- `DriveBackupException.RequiresReauth` esiste già ed è valorizzato correttamente nei tre punti in cui l'autenticazione non è recuperabile, ma **nessuno lo legge**: `BackupService` lo appiattisce in `new BackupResult(false, ex.Message)` e `BackupViewModel` stampa `$"Backup non riuscito. {result.ErrorMessage}"`.
- Nessuna traccia persistente del fallimento: `ISettingsStore` conserva solo `LastBackupUtc`/`LastBackupSize`, che vengono scritti **solo in caso di successo**. Un backup fallito non lascia nulla; l'unico segnale è la notifica di sistema (`NotifyResult(false)`) che l'utente scarta.
- `IBackupService.IsEnabled` = `BackupEnabled && IsSignedIn`, e `IsSignedIn` guarda solo l'email in `Preferences`. Con refresh token revocato la pagina resta quindi identica a quella di un backup perfettamente funzionante.
- `BackupService.RefreshQuotaAsync` ingoia `DriveBackupException` e ritorna la quota in cache: all'apertura della pagina un token revocato passa completamente inosservato.

Vincoli: app offline-first, nessun server, nessuna nuova dipendenza, serializzazione JSON solo via source-gen (`BackupJsonContext`), nessun test project nel repo (verifica = `dotnet build` + prova su emulatore).

## Goals / Non-Goals

**Goals:**

- Rendere lo stato di guasto del backup **persistente e visibile** in pagina, non solo un alert effimero, e valido anche per i backup eseguiti in background.
- Classificare gli errori in poche categorie stabili e tradurle in messaggi comprensibili, eliminando i payload tecnici dalla UI.
- Distinguere "backup fallito" da "credenziali non più valide", e offrire per quest'ultimo una **riconnessione che non smonta la configurazione**.
- Non produrre falsi allarmi: mancanza di rete ≠ credenziali revocate.

**Non-Goals:**

- Retry automatici o coda di backup differiti: il backup schedulato successivo è già il retry naturale.
- Cambiare il testo delle notifiche di sistema con il dettaglio dell'errore (`IBackupNotifier.NotifyResult(bool)` resta com'è): il dettaglio vive in-app.
- Storico dei fallimenti: si conserva solo l'**ultimo** esito.
- Toccare gli scope OAuth, lo schema del database o il formato dei file di backup.

## Decisions

### 1. Categoria d'errore come enum, prodotta alla fonte

Nuovo `enum BackupErrorKind { None, Network, StorageFull, ReauthRequired, Service, Local }` in `BackupModels.cs`. `DriveBackupException` guadagna una `Kind` (e `RequiresReauth` diventa ridondante: si deriva da `Kind == ReauthRequired`, così esiste una sola verità). `BackupResult` e `RestoreResult` portano la categoria fino alla UI, che sceglie il testo.

Mappatura in `DriveBackupClient`:

| Situazione | Kind |
|---|---|
| `HttpRequestException` / `TaskCanceledException` non annullato | `Network` |
| 401 anche dopo refresh, oppure nessun access token ottenibile con refresh token rifiutato | `ReauthRequired` |
| 403 con `error.errors[].reason == storageQuotaExceeded` | `StorageFull` |
| 403 di rate limit, 429, 5xx, altri 4xx | `Service` |
| `IOException` / errore snapshot o swap del database (in `BackupService`) | `Local` |

Alternativa scartata: classificare nella UI ispezionando il testo del messaggio. Fragile (dipende da stringhe localizzate di Google) e mette logica di protocollo nel ViewModel.

Per leggere `error.errors[].reason` serve un DTO aggiunto a `BackupJsonContext` (source-gen obbligatoria: niente `JsonSerializer` reflection-based, l'app gira con trimming Android). Il parsing è best-effort: se il corpo non è il JSON atteso, si degrada a `Service`.

Attenzione a non rompere il comportamento esistente di `GetStorageQuotaAsync`, che tratta 403/404 come "quota non leggibile con lo scope appdata" e ritorna `null`: quel ramo resta prima della classificazione, perché lì un 403 **non** significa spazio esaurito.

### 2. Rete assente ≠ credenziali revocate

Oggi `GoogleAuth.GetValidAccessTokenAsync` ritorna `null` in tre casi diversi (nessun refresh token salvato, refresh rifiutato da Google, `HttpRequestException`) e il client li traduce tutti in `RequiresReauth`. È la causa principale di falsi "riconnetti l'account" quando semplicemente non c'è campo.

`IGoogleAuth.GetValidAccessTokenAsync` cambia firma e restituisce un `AccessTokenResult(string? Token, TokenFailure Failure)` con `TokenFailure { None, NoAccount, Rejected, Network }`. Solo `NoAccount`/`Rejected` producono `ReauthRequired`; `Network` produce `Network`. `PostTokenAsync` deve quindi distinguere una risposta HTTP di rifiuto (400/401, tipicamente `invalid_grant`) da un errore di trasporto.

Alternativa scartata: lasciare `string?` e aggiungere una proprietà di stato sull'istanza — stato implicito condiviso, peggiore da leggere e da usare in modo concorrente (il refresh è già serializzato da un `SemaphoreSlim`).

### 3. Persistenza dell'esito nelle Preferences, accanto ai valori già presenti

`ISettingsStore` guadagna:

- `DateTimeOffset? LastBackupAttemptUtc` — quando è avvenuto l'ultimo tentativo, riuscito o no;
- `BackupErrorKind LastBackupError` — `None` se l'ultimo tentativo è riuscito.

`LastBackupUtc`/`LastBackupSize` restano il **successo** più recente e non vengono toccati dai fallimenti: è proprio la coppia "ultimo backup riuscito il 12/07" + "ultimo tentativo fallito ieri" a smontare l'illusione che i backup stiano funzionando.

Stesso store (MAUI `Preferences`) usato per tutto il resto della sezione: nessuna migrazione, i valori assenti significano "nessun errore noto". `BackupErrorKind` va serializzato come stringa (o int) perché `Preferences` non gestisce enum direttamente.

La scrittura dell'esito avviene in **un solo punto**, `BackupService.BackupNowAsync`, che è già l'imbuto attraversato anche da `RunScheduledBackupAsync` e `MaybeBackupOnOpenAsync`: così i backup in background sono coperti senza codice dedicato al worker Android.

In più, `RefreshQuotaAsync` smette di ingoiare in silenzio: se fallisce con `ReauthRequired` registra lo stato (non tocca `LastBackupAttemptUtc`, che riguarda i backup). Così il token revocato emerge già all'apertura della pagina, senza aspettare il prossimo backup schedulato. Gli altri `Kind` restano ignorati lì: una quota non aggiornata per mancanza di rete non è un guasto del backup.

### 4. Riconnessione: `SignInAsync` senza `SignOutAsync`

Nuovo `IBackupService.ReconnectAsync()` che chiama direttamente `_auth.SignInAsync()`. Il flusso OAuth è già `prompt=consent` + `access_type=offline`, quindi restituisce un nuovo refresh token che **sovrascrive** quello morto in `SecureStorage`; `BackupEnabled`, `BackupFrequency`, la schedulazione e i file su Drive non vengono toccati. In caso di successo: `LastBackupError = None`. In caso di annullamento: nessuna scrittura, lo stato di errore resta.

Alternativa scartata: riusare `DisableAsync` + `EnableAsync`. Cancella frequenza (`Never`), quota in cache e `LastBackupUtc`, e tenta una revoca di rete inutile su un token già invalido — cioè fa esattamente il "riparti da zero" che la change vuole eliminare.

Se l'utente sceglie un account Google diverso, l'email salvata cambia e la UI mostra il nuovo account: coerente con il requisito "un solo account alla volta", nessun caso speciale da gestire.

### 5. UI: banner di stato in cima alla sezione abilitata

`BackupViewModel` espone `BackupHealth { Ok, Failed, ReauthRequired }` derivato dallo store, più `StatusTitle`/`StatusDetail` (testi per categoria) e `IsReconnectVisible`. `BackupPage.xaml` mostra un banner sopra i dati dell'account, visibile solo quando `Health != Ok`, con il pulsante "Riconnetti l'account Google" solo in `ReauthRequired`.

Gli alert post-azione restano (feedback immediato dell'azione appena richiesta) ma usano gli stessi testi per categoria, non più `ex.Message`.

In Impostazioni il binding del pulsante Backup passa da `bool IsBackupEnabled` a un valore a tre stati, e `BackupStatusToColorConverter` guadagna il caso "attivo ma in errore" (rosso `#B00020`, già usato in `BackupPage` per il pulsante di disabilitazione) accanto ai due colori esistenti; `BackupStatusText` diventa "Backup non attivo" / "Backup attivo" / "Backup da riconnettere" / "Ultimo backup non riuscito".

I testi per categoria (bozza, da rifinire in implementazione):

- `Network` — "Backup non riuscito: nessuna connessione. Verrà ritentato appena torna la rete."
- `StorageFull` — "Spazio su Google Drive esaurito. Libera spazio sul tuo account per completare il backup."
- `ReauthRequired` — "L'accesso a Google è scaduto. Riconnetti l'account per riprendere i backup."
- `Service` — "Google Drive non è al momento disponibile. Riprova più tardi."
- `Local` — "Non è stato possibile preparare i dati da salvare. Riprova."

### 6. Il ripristino usa gli stessi messaggi, non lo stesso stato

`RestoreResult` porta anch'esso `BackupErrorKind` per mostrare messaggi comprensibili, ma un ripristino fallito **non** scrive `LastBackupAttemptUtc`/`LastBackupError` — non è un backup. Unica eccezione: `ReauthRequired`, che registra lo stato di riconnessione necessaria perché riguarda le credenziali, non l'operazione.

## Risks / Trade-offs

- **Classificazione sbagliata di un 403** (rate limit letto come spazio esaurito, o viceversa) → si decide sul campo `reason` del payload Google, non sul solo status; qualsiasi valore non riconosciuto ricade su `Service`, che è il messaggio generico e non colpevolizza l'utente.
- **Falso "riconnetti l'account" in condizioni di rete instabile** → mitigato dalla decisione 2 (`TokenFailure.Network` non produce mai `ReauthRequired`); un rifiuto vero del refresh token da parte di Google è invece definitivo e va segnalato.
- **Allarme che resta acceso più del dovuto** (es. errore di rete di ieri, oggi tutto a posto) → lo stato si azzera al primo backup riuscito, e il backup manuale è sempre a un tocco di distanza dal banner. Non si aggiunge un probe di rete all'apertura per non fare traffico a sorpresa.
- **Cambio di firma di `IGoogleAuth.GetValidAccessTokenAsync`** → interfaccia interna con un solo implementatore e un solo chiamante (`DriveBackupClient`), rottura contenuta e verificata da `dotnet build`.
- **Scrittura di Preferences dal worker in background** → `Preferences` è già usato dal percorso di backup schedulato per `LastBackupUtc`, quindi non introduce un accesso nuovo; nessun cambiamento di thread rispetto a oggi.
- **Verifica dei casi di errore sull'emulatore**: revocare l'accesso dell'app dalla pagina "Account Google → Sicurezza → App di terze parti" riproduce il caso `ReauthRequired` in modo realistico; `Network` si riproduce in modalità aereo; `StorageFull` e `Service` restano difficili da riprodurre e vanno verificati per costruzione (percorso di classificazione), non a mano.
