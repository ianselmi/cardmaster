## ADDED Requirements

### Requirement: File di contesto per agenti AI alla radice del repository

Il sistema SHALL fornire un file `CLAUDE.md` nella radice del repository, con una descrizione sintetica del progetto (cos'è CardMaster, stato attuale) e un elenco di riferimenti ai documenti esistenti (`PLAN.md`, `docs/*.md`, `openspec/`) per gli approfondimenti, senza duplicarne il contenuto.

#### Scenario: Panoramica del progetto disponibile

- **WHEN** un agente apre `CLAUDE.md` alla radice del repository
- **THEN** trova una descrizione sintetica del progetto e del suo stato attuale

#### Scenario: Riferimenti ai documenti di dettaglio

- **WHEN** un agente cerca un vincolo architetturale, una decisione presa, una trappola tecnica nota o le istruzioni di setup
- **THEN** `CLAUDE.md` indica quale documento consultare (`PLAN.md`, `docs/technical-notes.md`, `docs/ci-release.md`, `docs/google-drive-backup.md`, `openspec/`) invece di ripeterne il contenuto

### Requirement: Promemoria sulla revisione pre-commit

Il sistema SHALL includere in `CLAUDE.md` il promemoria di rivedere ogni diff prima di commit/push per escludere segreti o dati sensibili, conseguente alla decisione che il repository è pubblico.

#### Scenario: Promemoria visibile

- **WHEN** un agente legge `CLAUDE.md`
- **THEN** trova l'indicazione di rivedere il diff prima di ogni commit/push, dato che il repository è pubblico
