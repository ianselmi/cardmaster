# CardMaster

App .NET MAUI (Android, offline-first) per conservare e mostrare i codici a barre delle carte fedeltà. Distribuita fuori dal Play Store via GitHub Releases; controllo aggiornamenti in-app. Repository **pubblico**.

## Dove trovare cosa

- **`PLAN.md`** — piano di sviluppo, vincoli architetturali v1/v2, decisioni prese e la loro motivazione, elenco delle change (fatte e da fare). **Prima fonte da consultare** prima di proporre qualunque change: i vincoli lì elencati sono già decisi, non rimetterli in discussione.
- **`openspec/config.yaml`** — lo stesso contesto architetturale di `PLAN.md`, nel formato letto automaticamente da OpenSpec quando si genera una nuova change.
- **`openspec/specs/`** — comportamento corrente delle singole capability (una cartella per feature), fonte di verità su cosa fa oggi l'app.
- **`openspec/changes/`** — change in corso o archiviate; ognuna con `proposal.md` (perché), `design.md` (come, se non ovvio) e `tasks.md` (checklist di implementazione).
- **`docs/technical-notes.md`** — trappole tecniche già incontrate (es. provider SQLite corretto da usare) — controllare prima di ripetere un errore già risolto.
- **`docs/ci-release.md`** — setup della pipeline di build/firma/pubblicazione APK (keystore, secret CI).
- **`docs/google-drive-backup.md`** — setup OAuth per il backup su Google Drive.

## Workflow delle change

Le feature si propongono e implementano con OpenSpec (slash command `/opsx:propose`, `/opsx:apply`, `/opsx:archive`, ecc.). Una change per volta, con contesto pulito tra una e l'altra. Per il "come" non ovvio, partire da `/opsx:explore`.

## Prima di ogni commit/push

Il repository è **pubblico** (decisione 25 lug 2026, vedi `PLAN.md`): **rivedere sempre il diff prima di committare o pushare**, per escludere segreti o dati sensibili (chiavi, token, credenziali, percorsi/dati personali). Il keystore di firma e le credenziali CI restano *solo* come secret GitHub Actions, mai nel repository.

## Build

`dotnet build` deve completare senza errori — criterio di accettazione per ogni change (vedi `PLAN.md`).
