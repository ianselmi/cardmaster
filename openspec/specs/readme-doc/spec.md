# readme-doc Specification

## Purpose

README pubblico alla radice del repository, rivolto a chi visita il repo su GitHub: presentazione sintetica del progetto, istruzioni di download dell'ultima build e di build locale, con rimando al piano di sviluppo per i dettagli.

## Requirements
### Requirement: README pubblico alla radice del repository

Il sistema SHALL fornire un file `README.md` nella radice del repository, con una descrizione sintetica di CardMaster, le istruzioni per scaricare/installare l'ultima build e le istruzioni minime per compilare il progetto in locale.

#### Scenario: Descrizione del progetto disponibile

- **WHEN** un visitatore apre `README.md` (es. dalla pagina GitHub del repository)
- **THEN** trova una descrizione sintetica di cos'è CardMaster

#### Scenario: Istruzioni di download disponibili

- **WHEN** un visitatore vuole installare l'app
- **THEN** `README.md` indica dove scaricare l'ultima build (Release GitHub)

#### Scenario: Istruzioni di build locale disponibili

- **WHEN** uno sviluppatore vuole compilare il progetto in locale
- **THEN** `README.md` elenca i prerequisiti (SDK .NET, workload MAUI Android) e il comando di build

### Requirement: Rimando al piano di sviluppo

Il sistema SHALL far riferimento, da `README.md`, a `PLAN.md` per il piano di sviluppo completo e le decisioni architetturali, senza duplicarne il contenuto.

#### Scenario: Rimando presente

- **WHEN** un visitatore cerca dettagli su piano di sviluppo o decisioni architetturali
- **THEN** `README.md` rimanda a `PLAN.md` invece di ripeterne il contenuto

