## Context

Il repo ha già due documenti di contesto con pubblici diversi: `CLAUDE.md` (agenti AI, punta ai documenti di dettaglio) e `PLAN.md` (piano di sviluppo interno, vincoli e decisioni). Manca il documento per il pubblico più ovvio di un repo GitHub pubblico: una persona che lo trova, vuole capire cos'è e magari scaricare l'APK.

## Goals / Non-Goals

**Goals:**
- Un `README.md` orientato a un visitatore umano (non un agente): cosa fa l'app, come installarla, come compilarla.
- Coerenza con `CLAUDE.md`/`PLAN.md`: nessuna duplicazione dei vincoli architetturali, solo un rimando a `PLAN.md` per chi vuole approfondire.

**Non-Goals:**
- Nessuna licenza open source da definire in questa change (il repo non ne ha una; non è nello scope introdurla).
- Nessuna immagine/screenshot (nessun asset pronto; rimandabile a una change futura).
- Nessuna guida "contributing" (progetto personale, non pensato per contributi esterni al momento).

## Decisions

### Contenuto: presentazione + download + build locale, rimando a PLAN.md per il resto
Sezioni: titolo/descrizione breve, come scaricare l'ultima build (link alla Release GitHub `latest`, coerente con come l'app stessa si aggiorna — vedi `maui-auto-update`), stack tecnico in 2-3 righe, istruzioni minime di build locale (SDK da `global.json`, workload `maui-android`, comando `dotnet build`), e un rimando esplicito a `PLAN.md` per piano/decisioni.

### Formato: stile coerente con CLAUDE.md
Lista puntata/sezioni brevi, stesso registro sintetico già usato in `CLAUDE.md`, non una guida esaustiva.

## Risks / Trade-offs

- **[Rischio] Il link alla Release "latest" o le istruzioni di build possono disallinearsi se cambia il workflow CI** → Accettato: stesso rischio già presente per `docs/ci-release.md`, nessuna automazione prevista in questa change.
