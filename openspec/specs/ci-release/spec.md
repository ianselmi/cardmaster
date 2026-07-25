# ci-release

## Purpose

Pipeline di build, firma e pubblicazione dell'APK Android come Release GitHub, con versionamento automatico. Distribuzione fuori dal Play Store (hosting statico dell'APK).

## Requirements

### Requirement: Build dell'APK Android in CI

Il sistema SHALL fornire una pipeline CI (GitHub Actions) che compila l'app MAUI Android in configurazione Release e produce un file **APK** (non solo AAB) installabile via sideload.

#### Scenario: Build su push in main

- **WHEN** viene effettuato un push sul branch `main`
- **THEN** la pipeline compila l'app in Release e produce un APK

#### Scenario: Avvio manuale

- **WHEN** la pipeline viene avviata manualmente (workflow_dispatch)
- **THEN** compila l'app in Release e produce un APK

### Requirement: Firma dell'APK con keystore da secret

Il sistema SHALL firmare l'APK con un keystore custodito come **secret CI**. Il keystore NON MUST essere presente nel repository. La configurazione di firma nel progetto SHALL essere condizionale, così che le build locali senza keystore continuino a funzionare.

#### Scenario: Firma in CI

- **WHEN** la pipeline compila con i secret del keystore configurati
- **THEN** l'APK prodotto è firmato con quel keystore

#### Scenario: Build locale senza keystore

- **WHEN** si compila localmente senza i secret del keystore
- **THEN** la build non fallisce per la mancanza del keystore (firma di release non richiesta)

#### Scenario: Nessun keystore nel repository

- **WHEN** si ispeziona il repository
- **THEN** non è presente alcun file keystore né password in chiaro

### Requirement: Versionamento automatico

Il sistema SHALL assegnare automaticamente le versioni dell'APK: `ApplicationVersion` (versionCode) da un contatore monotono (numero di run CI) e `ApplicationDisplayVersion` (versionName) dal tag git quando presente, con un fallback altrimenti.

#### Scenario: versionCode monotono

- **WHEN** la pipeline produce un APK
- **THEN** il versionCode è un valore monotono derivato dal numero di run CI

#### Scenario: versionName da tag

- **WHEN** la pipeline è avviata dal push di un tag `v*`
- **THEN** il versionName dell'APK corrisponde al tag

#### Scenario: versionName di fallback senza tag

- **WHEN** la pipeline è avviata senza un tag (push su main o avvio manuale)
- **THEN** il versionName usa un valore di fallback definito, senza fallire

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
