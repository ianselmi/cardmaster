## Context

Il workflow `.github/workflows/build-apk.yml` calcola in un passo `Compute version` gli output:
- `name` = versionName (`${GITHUB_REF_NAME#v}` sui tag, altrimenti `1.0.${GITHUB_RUN_NUMBER}`)
- `code` = versionCode (`${GITHUB_RUN_NUMBER}`)
- `release_tag` = `latest` (prerelease) oppure il tag `v*`

Questi valori alimentano già `-p:ApplicationDisplayVersion=${{ steps.ver.outputs.name }}` nella publish. Il passo `Publish Release` però imposta il campo `name` così:

```yaml
name: ${{ steps.ver.outputs.release_tag == 'latest' && 'Ultima build (main)' || steps.ver.outputs.release_tag }}
```

cioè per la prerelease usa una stringa fissa, scollegata dalla versione compilata nell'app.

## Goals / Non-Goals

**Goals:**
- Il titolo della prerelease `latest` mostra il versionName dell'app (`steps.ver.outputs.name`).

**Non-Goals:**
- Cambiare tag, versionCode, firma, o il comportamento dei tag `v*` (restano col nome del tag).
- Rimuovere la prerelease o passare ad artifact-only (scelta esplicita: si mantiene la prerelease `latest`).

## Decisions

Sostituire l'espressione del campo `name` nel passo `Publish Release` così che il ramo prerelease usi `steps.ver.outputs.name`:

```yaml
name: ${{ steps.ver.outputs.release_tag == 'latest' && steps.ver.outputs.name || steps.ver.outputs.release_tag }}
```

Risultato: prerelease titolata `1.0.<run>` (uguale ad `ApplicationDisplayVersion`); Release stabile su tag titolata col tag (invariato). Nessun altro passo cambia.

*Alternativa considerata*: aggiungere un prefisso ("Build 1.0.42"). Scartata per semplicità e per coincidenza esatta col numero mostrato in Impostazioni; eventualmente rifinibile in seguito.

## Risks / Trade-offs

- [Il tag resta `latest`, mentre il titolo cambia a ogni build] → Atteso e voluto: il tag è il puntatore stabile "ultima build", il titolo comunica la versione. Nessun rischio funzionale.
