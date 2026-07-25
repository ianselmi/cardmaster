# CardMaster — Piano di sviluppo

App per conservare e mostrare i codici a barre delle **carte fedeltà**. Offline-first.

**Strategia di rilascio:**
- **v1 — core offline, con un solo tassello online opt-in.** Aprire, scansionare e condividere le carte gira interamente sul device: salvataggio locale, sblocco biometrico, condivisione tramite **QR code self-contained** (contiene tutti i dati della carta, letto da un altro device). Nessuna auth online, nessuna sincronizzazione. L'unica funzione di rete è il **backup su Google Drive** (`maui-backup-drive`): opt-in, disattivabile, isolato dietro interfacce — se disabilitato o senza rete l'app resta 100% funzionale offline. *(Decisione 25 lug 2026: il backup Drive, inizialmente rimandato a v2, rimpiazza `maui-backup-local` e torna in v1 — vedi `openspec/changes/maui-backup-drive`.)*
- **v2 — backend e sincronizzazione (rimandato).** Si aggiunge il backend .NET 10 per auth online, sincronizzazione multi-device e account applicativo. Progettiamo la v1 in modo che questa evoluzione sia possibile senza migrazioni dolorose.

> Fonte: analisi architetturale del 23–24 lug 2026 ([chat condivisa](https://claude.ai/share/f4ff8eb8-1470-4a8a-8103-43019873384c)), con la decisione di partire da una v1 100% offline. Le feature vengono tracciate con **OpenSpec**. Spunta i punti man mano che vengono completati.

---

## Vincoli architetturali (decisi — non rimettere in discussione)

Questi vincoli vanno nel file di contesto di progetto di OpenSpec; ogni change deve rispettarli.

### v1 (core offline + backup Drive opt-in)
- **Prodotto**: app Android. Il core (aprire una carta, scansionarne una nuova, condividerla) funziona sempre senza rete e senza account. L'unica funzione online è il **backup su Google Drive**, opt-in e disattivabile (vedi `maui-backup-drive`).
- **Client**: .NET MAUI (solo Android). SQLite locale **in chiaro** (`SQLitePCLRaw.bundle_e_sqlite3`). Scansione con **ML Kit** (`BarcodeScanning.Native.Maui`), rendering barcode con **ZXing.Net + SkiaSharp**. *(Decisione 24 lug 2026: rimossa la cifratura SQLCipher — il pacchetto `bundle_e_sqlcipher` è deprecato e la cifratura non è ritenuta essenziale per la v1. Eventuale reintroduzione: `SQLite3MC.PCLRaw.bundle`, mantenuto.)*
- **Autenticazione v1**: **nessuna gate di sblocco applicativa**. L'app si apre direttamente sulla lista carte (nessun account, nessun server, nessun prompt biometrico o PIN). La protezione "telefono in mano ad altri" è delegata al lockscreen di Android. *(Decisione del 24 lug 2026: rimossi biometria e PIN applicativi rispetto all'ipotesi iniziale — l'app deve mostrare subito le carte.)*
- **Modello dati**: le carte sono locali al device. `Id` carte generati dal **client** (GUID/ULID) — così restano validi quando in v2 arriverà la sync. Cancellazioni logiche con **tombstone** (mai DELETE fisico), già ora, per non complicare la futura sincronizzazione.
- **Condivisione v1**: una carta si condivide mostrando un **QR code self-contained** che incapsula tutti i dati necessari (emittente/nome, `barcode`, `barcodeFormat`, eventuale colore/logo id). L'altro device lo **scansiona** e crea una **copia indipendente** (nessun legame persistente — il barcode fedeltà è immutabile). Peer-to-peer, **nessun server**. Alla ricezione: controllo duplicati (stesso emittente + barcode) per proporre di saltare invece di duplicare. Payload versionato (`v` nello schema) per compatibilità futura.
- **Riconoscimento emittente**: catalogo emittenti **bundle locale statico** (seed nell'app) per nome/logo/formato atteso; nessuna sync del catalogo in v1.
- **Sicurezza v1**: DB SQLite locale **in chiaro** (nessuna cifratura at-rest in v1); la protezione è delegata al lockscreen di Android. Niente TLS/E2E perché non c'è rete in gioco.
- **Repository GitHub pubblico**: *(Decisione 25 lug 2026, change `maui-auto-update`)* il repository `ianselmi/cardmaster` è **pubblico** — necessario perché l'auto-update legge direttamente le API Releases di GitHub (repo privato le renderebbe inaccessibili senza un token, non incorporabile in sicurezza nell'APK; GitHub Pages come alternativa è a pagamento per repo privati). Verificato che repo e cronologia non contengono segreti prima del cambio di visibilità. **Da qui in avanti: prima di ogni commit/push, rivedere il diff per escludere segreti o dati sensibili** (chiavi, token, credenziali, percorsi/dati personali) — il repo è ora visibile a chiunque. Il keystore di firma e le credenziali CI restano *solo* come secret GitHub Actions, mai nel repo.

### v2 (backend — riferimento, per non chiudere porte ora)
- **Backend**: .NET 10, **Minimal API**, **CQRS a vertical slice** (una cartella per feature: comando + handler + validator). EF Core + SQLite, **WAL**, scritture **serializzate da una coda** (SQLite = un solo writer). Niente read model separato, event sourcing o MediatR. **Docker + docker compose**, reverse proxy per il TLS.
- **Autenticazione v2**: primo accesso online (credenziali o **Google** con Authorization Code + PKCE, nessun client secret nell'APK) → access token breve + refresh token lungo con rotazione in `SecureStorage`. La biometria resta solo sblocco locale. Refresh token scaduto → app usabile in sola lettura, si blocca solo la sync.
- **Sincronizzazione v2**: cursore = contatore monotono `Seq` assegnato dal server (`INTEGER PRIMARY KEY AUTOINCREMENT` su `ChangeLog` append-only). Timestamp client solo come tie-breaker per il **last-write-wins**, mai cursore. `OperationId` client-generato per l'idempotenza sui retry.
- **Condivisione v2**: oltre al QR, eventuale link con snapshot lato server (token 128 bit da `RandomNumberGenerator`, TTL breve, rate limit, Android App Links + `assetlinks.json`).

---

## Fase 0 — Setup OpenSpec

- [ ] Verificare Node.js ≥ 20.19.0
- [ ] `npm install -g @fission-ai/openspec@latest`
- [ ] `openspec init --tools claude` nella cartella di progetto
- [ ] `openspec --help` per verificare che i comandi slash corrispondano (OpenSpec si muove veloce)
- [ ] Popolare il **file di contesto di progetto** con i vincoli architetturali qui sopra (senza scrivere codice né creare change in questo passaggio)
- [ ] Verificare il risultato con `git diff` prima di procedere

**Ciclo per ogni change** (una per sessione, contesto pulito tra una e l'altra):
`/opsx:propose <nome>` → leggi e correggi la proposta → `openspec validate <nome>` → `/opsx:apply` → `openspec archive <nome>`.
Per le change dove il "come" non è ovvio (es. `maui-unlock`) partire da `/opsx:explore`.
⚠️ Non proporre tutte le change in una volta: ognuna va scritta conoscendo le precedenti già implementate.

---

## v1 — App offline + backup Drive opt-in

- [x] **`maui-shell`** — progetto MAUI Android, navigazione, DI, SQLite locale con SQLCipher e chiave in Keystore
- [~] **`maui-unlock`** — ~~biometria via `BiometricPrompt`, fallback PIN, gestione invalidazione della chiave al cambio impronte~~ **ANNULLATA** (24 lug 2026): decisa nessuna gate di sblocco: l'app apre subito le carte. La chiave del DB resta nel Keystore senza binding all'autenticazione utente (come già in `maui-shell`).
- [x] **`issuer-seed`** — catalogo emittenti come seed statico bundle nell'app (nome, logo, colore, formato barcode atteso); nessuna sync
- [x] **`maui-scan-card`** — scansione ML Kit, formati EAN-13/EAN-8/UPC-A/UPC-E/Code128/Code39/ITF/Codabar/QR/PDF417, inserimento manuale, emittente opzionale dal seed, avviso duplicati, salvataggio locale (Id client-generato, tombstone)
- [x] **`maui-card-grid`** — lista carte come griglia di riquadri (2 colonne, quadrettoni con angoli arrotondati), colore di sfondo generato in modo deterministico per carta
- [x] **`maui-show-card`** — rendering del barcode (ZXing.Net + SkiaSharp), luminosità al massimo e blocco spegnimento schermo, codice in chiaro come fallback, avviso filtro luce blu best-effort
- [x] **`maui-card-search`** — ricerca tra le carte (per nome/emittente) e barra aggiuntiva con le **ultime 3 carte usate** (richiede tracciare un timestamp "ultimo utilizzo" all'apertura della carta)
- [x] **`maui-edit-card`** — modifica dei dati di una carta esistente (nome, emittente, colore; eventualmente barcode/formato), con salvataggio via repository (aggiorna `UpdatedAt`)
- [x] **`maui-restyle`** — restyle grafico complessivo: nuovo **logo** (app icon + splash) e **palette colori** (tema/accent, colori dei tile), coerenza di tipografia e spaziature sulle pagine esistenti
- [x] **`maui-share-qr`** — genera un QR code self-contained con i dati della carta (payload versionato); import scansionando il QR di un altro device; controllo duplicati alla ricezione
- [x] **`maui-settings`** — sezione **Impostazioni**: pagina raggiungibile dalla lista carte (icona ruota dentata in toolbar), store delle preferenze (MAUI `Preferences`), info app (versione), e **preferenza tema** Sistema/Chiaro/Scuro persistita e applicata all'avvio. Predisposta come host della futura opzione di backup (implementata da `maui-backup-drive`)
- [x] **`maui-backup-drive`** — backup/ripristino del database su **Google Drive** come **unica funzione di rete della v1**, opt-in e disattivabile: Google Sign-In (OAuth Authorization Code + PKCE, nessun client secret nell'APK), snapshot (`VACUUM INTO`) caricato come blob db3 opaco nella cartella `appdata`, ritenzione ultimi 3, ripristino con guardia di versione schema e snapshot di sicurezza, schedulazione (Mai/A ogni apertura/Giornaliero/Settimanale), notifica di avanzamento stile WhatsApp. *(Decisione 25 lug 2026: rimpiazza l'idea `maui-backup-local` — file esportabile — e viene anticipato da v2 a v1, vedi `openspec/changes/maui-backup-drive`. OAuth client Android creato su Google Cloud Console e login verificato in produzione il 25 lug 2026 — vedi `docs/google-drive-backup.md`, incluso il passaggio "Enable custom URI scheme" necessario oltre a package name/SHA-1.)*
- [x] **`ci-build-apk`** — pipeline di build (GitHub Actions) che compila l'app MAUI Android, **firma** l'APK con keystore (secret CI), e pubblica l'artifact/APK come GitHub Release; versionamento automatico (`ApplicationVersion`/`ApplicationDisplayVersion`). *(Prerequisito utente: creare keystore + secret — vedi `docs/ci-release.md`.)*
  - [x] **`ci-release-app-version`** — il titolo della prerelease `latest` su `main` mostra il versionName dell'app (`ApplicationDisplayVersion`) invece dell'etichetta fissa "Ultima build (main)", così release e versione installata coincidono.
  - [x] **`ci-release-version-tags`** — la versione dell'app diventa il **numero di build incrementale** (`8`, `9`, `10`…); ogni build su `main` crea una Release versionata taggata col numero di build (storico con APK) oltre alla prerelease `latest` sull'ultima. Tag senza prefisso `v`; ramo `v*` invariato.
- [x] **`maui-auto-update`** — controllo aggiornamenti su richiesta dalle Impostazioni, interrogando direttamente la **Release GitHub con tag `latest`** (API pubbliche, repo pubblico — vedi decisione sopra); se diversa dalla versione installata, scarica l'APK con avanzamento (notifica + foreground service), **verifica il checksum** quando disponibile, e lancia l'installazione via package installer intent (`REQUEST_INSTALL_PACKAGES` + `FileProvider`). Funzione online opzionale: non tocca il core offline. Verificato end-to-end su emulatore il 25 lug 2026.
- [x] **`claude-md-agent-context`** — `CLAUDE.md` alla radice come punto d'ingresso per agenti AI, con riferimenti a `PLAN.md`/`docs/`/`openspec/` invece di duplicarli.
- [x] **`readme-doc`** — `README.md` pubblico alla radice: presentazione del progetto, download dell'ultima build, build locale, rimando a questo piano.

---

## v2 — Backend e sincronizzazione (rimandato)

> Da affrontare dopo il rilascio della v1 offline. Elencato qui solo come roadmap; non iniziare finché la v1 non è stabile.

**Backend**
- [ ] **`bootstrap-backend`** — soluzione .NET 10, vertical slice, Dockerfile multi-stage, docker-compose con volume, endpoint `/health`
- [ ] **`data-model-core`** — entità Users, Cards, Issuers, Devices, ChangeLog; migration EF Core; WAL + `busy_timeout`; coda di serializzazione delle scritture
- [ ] **`auth-credentials`** — registrazione e login, hashing password, JWT, refresh token con rotazione, registro device e revoca per device
- [ ] **`auth-google`** — validazione `id_token` via JWKS, controllo `aud`/`iss`/`exp`/`nonce`, account linking
- [ ] **`sync-api`** — query `GetChangesSince`, comando composito `PushChanges` con esito per operazione, idempotenza via `OperationId`, last-write-wins *(partire da `/opsx:explore`)*
- [ ] **`issuer-catalog`** — catalogo emittenti versionato lato server, pull incrementale (sostituisce/estende il seed statico)
- [ ] **`share-links`** *(opzionale)* — link con snapshot della carta, redemption, `assetlinks.json`, landing page, rate limit

**Client v2**
- [ ] **`maui-auth`** — login online (credenziali + Google PKCE), gestione token in `SecureStorage`, sblocco biometrico che decifra i token
- [ ] **`maui-sync-client`** — outbox locale, cursore `Seq`, retry con backoff, gestione refresh token scaduto (sola lettura)

**Chiusura**
- [ ] **`deploy-hardening`** — reverse proxy con TLS automatico, healthcheck, backup schedulato con `VACUUM INTO`, restart policy

---

## Note tecniche di riferimento

- **Scanner vs rendering**: due funzioni distinte. Lettura con ML Kit (più affidabile di ZXing su Android per codici stampati/plastificati); riproduzione con ZXing.Net + luminosità max e blocco autorotazione/timeout. Se il lettore cassa è laser alcuni schermi non vengono letti → prevedere sempre il codice in chiaro come fallback.
- **Condivisione via QR (v1)**: il QR incapsula uno *snapshot* della carta, non un riferimento — quindi funziona anche se il mittente poi la cancella, ed è per costruzione offline. Attenzione alla dimensione del payload (tenerlo compatto: campi corti, eventualmente Base64/GZip) per non generare un QR troppo denso da leggere.
- **Perché tombstone e Id client-generati già in v1**: costano poco ora e rendono indolore l'aggancio della sync in v2; introdurli dopo richiederebbe una migrazione dei dati locali degli utenti.
- **Build & auto-update (v1)**: la distribuzione è **fuori dal Play Store**, quindi serve una firma stabile (keystore custodito come secret CI, mai nel repo) e l'aggiornamento self-hosted. Il "server" per l'update è solo **hosting statico** di un file `latest.json` + APK (può bastare GitHub Releases o il reverse proxy che arriverà in v2) — non è il backend applicativo. L'installazione richiede il permesso *"Installa app sconosciute"* concesso dall'utente. **Sempre** verificare `sha256` (o la firma) dell'APK scaricato prima di installarlo, per evitare update manomessi.
- **SQLite backend (v2)**: un solo writer (WAL + busy_timeout + coda). Niente scalabilità orizzontale senza passare a Postgres. File `.db`+`.db-wal`+`.db-shm` su volume Docker dedicato. Backup a caldo con `VACUUM INTO`.
- **CORS**: non pertinente per il client MAUI nativo. Rilevante solo con un eventuale frontend web futuro.
