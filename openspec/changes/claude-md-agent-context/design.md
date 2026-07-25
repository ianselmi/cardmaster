## Context

Il progetto usa OpenSpec per tracciare le decisioni (`PLAN.md` + `openspec/config.yaml` + change archiviate) e ha già documentazione operativa in `docs/` (setup CI, backup Drive, trappole tecniche). Manca un punto d'ingresso breve che un agente legga per primo per orientarsi, senza dover aprire/interpretare tutti quei file per capire dove guardare.

## Goals / Non-Goals

**Goals:**
- Un solo file, alla radice, che un agente trovi e legga per primo (convenzione Claude Code: `CLAUDE.md` in root).
- Contenuto breve: cos'è il progetto, stato attuale in una riga, e una tabella/lista di riferimenti "per sapere X vai in Y".
- Includere il promemoria "rivedi il diff prima di commit/push" (repo pubblico dal 25 lug 2026).

**Non-Goals:**
- Non duplicare i vincoli architetturali già in `PLAN.md`/`openspec/config.yaml` — solo puntarci.
- Non documentare qui il workflow OpenSpec passo-passo (già implicito negli slash command `/opsx:*`).

## Decisions

### Contenuto: riferimenti, non duplicazione
`CLAUDE.md` elenca dove trovare le informazioni (`PLAN.md` per piano/decisioni/vincoli, `docs/technical-notes.md` per le trappole tecniche già incontrate, `docs/ci-release.md` e `docs/google-drive-backup.md` per setup specifici, `openspec/specs/` per il comportamento corrente delle capability, `openspec/changes/` per le change in corso) invece di copiarne il contenuto. Motivazione: un contenuto duplicato si disallinea alla prima modifica di uno dei documenti sorgente; un indice resta valido più a lungo.

### Formato: lista puntata con una riga di contesto per link
Non una tabella (il repo non ne usa altrove in `docs/`), ma una lista breve nello stile già usato in `PLAN.md`, per coerenza di tono.

## Risks / Trade-offs

- **[Rischio] Il file può invecchiare se si aggiungono nuovi documenti senza aggiornare i riferimenti** → Mitigazione: nessuna automazione prevista in questa change (fuori scope); revisione manuale quando si aggiungono nuovi doc rilevanti.
