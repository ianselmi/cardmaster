## MODIFIED Requirements

### Requirement: Pubblicazione dell'APK come Release

Il sistema SHALL pubblicare l'APK come GitHub Release. Un push su `main` (o avvio manuale) SHALL aggiornare una **prerelease "latest"** in-place; il push di un tag `v*` SHALL creare una **Release stabile** con quel tag. Il **nome della prerelease "latest"** SHALL riportare il versionName dell'app (`ApplicationDisplayVersion`, es. `1.0.42`), cioè lo stesso valore con cui l'APK è stato compilato, così che la release e la versione installata siano sempre allineate.

#### Scenario: Prerelease latest su main

- **WHEN** la pipeline gira per un push su `main`
- **THEN** l'APK è pubblicato/aggiornato in una prerelease con tag "latest"

#### Scenario: Nome della prerelease uguale alla versione dell'app

- **WHEN** la pipeline pubblica la prerelease "latest"
- **THEN** il nome della release mostra il versionName dell'app (lo stesso `ApplicationDisplayVersion` usato per compilare l'APK)

#### Scenario: Release stabile su tag

- **WHEN** la pipeline gira per un tag `v*`
- **THEN** viene creata una Release stabile con quel tag e l'APK allegato
