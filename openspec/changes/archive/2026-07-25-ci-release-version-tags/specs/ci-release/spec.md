## MODIFIED Requirements

### Requirement: Versionamento automatico

Il sistema SHALL assegnare automaticamente le versioni dell'APK: `ApplicationVersion` (versionCode) da un contatore monotono (numero di run CI) e `ApplicationDisplayVersion` (versionName) dal tag git quando presente; in assenza di tag (build da `main`/avvio manuale) il versionName SHALL essere il **numero di build incrementale** (il numero di run CI), cioè lo stesso valore del versionCode.

#### Scenario: versionCode monotono

- **WHEN** la pipeline produce un APK
- **THEN** il versionCode è un valore monotono derivato dal numero di run CI

#### Scenario: versionName da tag

- **WHEN** la pipeline è avviata dal push di un tag `v*`
- **THEN** il versionName dell'APK corrisponde al tag

#### Scenario: versionName dal numero di build senza tag

- **WHEN** la pipeline è avviata senza un tag (push su `main` o avvio manuale)
- **THEN** il versionName è il numero di build incrementale (il numero di run CI), senza prefisso di versione

### Requirement: Pubblicazione dell'APK come Release

Il sistema SHALL pubblicare l'APK come GitHub Release. Un push su `main` (o avvio manuale) SHALL creare una **Release versionata** taggata con il **numero di build incrementale** (es. `8`) e l'APK allegato, **e in più** aggiornare in-place una **prerelease "latest"** che punta all'ultima build; il push di un tag git `v*` SHALL creare una **Release stabile** con quel tag. Il tag della Release versionata e il **nome sia della Release versionata sia della prerelease "latest"** SHALL riportare il versionName dell'app (`ApplicationDisplayVersion`), cioè lo stesso valore con cui l'APK è stato compilato, così che release e versione installata siano sempre allineate.

#### Scenario: Release versionata su main

- **WHEN** la pipeline gira per un push su `main`
- **THEN** viene creata una Release taggata con il numero di build (es. `8`), con l'APK allegato e il nome uguale al versionName dell'app

#### Scenario: Prerelease latest aggiornata su main

- **WHEN** la pipeline gira per un push su `main`
- **THEN** la prerelease con tag "latest" è aggiornata in-place all'ultima build, col nome uguale al versionName dell'app

#### Scenario: Tag di versione senza prefisso

- **WHEN** viene creata la Release versionata da un push su `main`
- **THEN** il tag usato è il solo numero di build (nessun prefisso `v`), identico al versionName dell'app

#### Scenario: Release stabile su tag

- **WHEN** la pipeline gira per un tag `v*`
- **THEN** viene creata una Release stabile con quel tag e l'APK allegato
