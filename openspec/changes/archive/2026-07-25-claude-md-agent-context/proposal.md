## Why

Il contesto di progetto è oggi sparso tra `PLAN.md`, `openspec/config.yaml`, `docs/*.md` e le change archiviate: un agente (Claude Code o altro) che apre il repo per la prima volta deve scoprire da solo dove sono i vincoli architetturali, le decisioni prese e le trappole tecniche note, rischiando di rimetterle in discussione o di ripetere errori già risolti. Un `CLAUDE.md` alla radice, letto automaticamente dagli strumenti agentici, dà un punto d'ingresso unico con i riferimenti giusti invece di duplicare il contenuto.

## What Changes

- Nuovo file `CLAUDE.md` nella root del repository: riassume in poche righe cos'è CardMaster, lo stato attuale (v1 offline + auto-update, repo pubblico), e rimanda per i dettagli a `PLAN.md` (piano/decisioni), `docs/technical-notes.md` (trappole tecniche), `docs/ci-release.md` e `docs/google-drive-backup.md` (setup), `openspec/` (workflow delle change).
- Include il promemoria operativo introdotto con la decisione "repo pubblico" (25 lug 2026): rivedere ogni diff prima di commit/push per escludere segreti o dati sensibili.
- Nessuna duplicazione di contenuto: il file punta ai documenti esistenti invece di ripeterli, per non disallinearsi quando quei documenti cambiano.

## Capabilities

### New Capabilities
- `agent-context-doc`: presenza e contenuto minimo di un file di contesto (`CLAUDE.md`) per agenti AI alla radice del repository, con riferimenti ai documenti di progetto.

### Modified Capabilities
(nessuna: non tocca comportamento dell'app, solo documentazione di repo)

## Impact

- Nuovo file `CLAUDE.md` in root. Nessun impatto su codice, build o pipeline CI.
