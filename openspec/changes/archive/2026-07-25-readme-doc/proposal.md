## Why

Il repository è ora pubblico ma non ha un `README.md`: chi lo apre su GitHub (o clona il progetto) non trova una presentazione del progetto, non sa come scaricare/installare l'app né come compilarla in locale. `CLAUDE.md` è pensato per gli agenti AI e `PLAN.md` è il piano di sviluppo interno con le decisioni — nessuno dei due è il punto d'ingresso naturale per un visitatore umano del repo.

## What Changes

- Nuovo `README.md` alla radice: cos'è CardMaster (in breve), come scaricare/installare l'ultima build (link alla Release GitHub), stack tecnico essenziale, come compilare il progetto in locale (prerequisiti: .NET SDK da `global.json`, workload MAUI Android).
- Rimanda a `PLAN.md` per il piano di sviluppo completo e le decisioni architetturali, invece di duplicarle.

## Capabilities

### New Capabilities
- `readme-doc`: presenza e contenuto minimo di un `README.md` pubblico alla radice del repository.

### Modified Capabilities
(nessuna: solo documentazione di repo, nessun impatto sul comportamento dell'app)

## Impact

- Nuovo file `README.md` in root. Nessun impatto su codice, build o pipeline CI.
