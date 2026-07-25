## Context

Workflow `.github/workflows/build-apk.yml`. Il passo `Compute version` produce oggi:
- `name` = versionName (`${GITHUB_REF_NAME#v}` sui tag, altrimenti `1.0.${GITHUB_RUN_NUMBER}`)
- `code` = versionCode (`${GITHUB_RUN_NUMBER}`)
- `prerelease`, `release_tag` (`latest` o il tag)

Il passo `Publish Release` pubblica **una sola** release (la `latest` prerelease su main, o la stabile su tag). Non esiste una release per singola build né un tag di versione automatico.

## Goals / Non-Goals

**Goals:**
- versionName (build non da tag) = numero di build incrementale (`${GITHUB_RUN_NUMBER}`).
- Ogni build su `main` crea una Release versionata taggata col numero di build (APK allegato) **e** aggiorna la prerelease `latest`.
- Tag di versione senza prefisso `v`.

**Non-Goals:**
- Cambiare il ramo dei tag git `v*` (Release stabili manuali restano come sono).
- Cambiare il versionCode (resta il run number).

## Decisions

### D1 — versionName = numero di build

Nel passo `Compute version`, il ramo "senza tag" imposta `name=${GITHUB_RUN_NUMBER}` (era `1.0.${GITHUB_RUN_NUMBER}`). Il ramo tag `v*` resta `name=${GITHUB_REF_NAME#v}`. Aggiungo un output `version_tag`: sul ramo main = `${GITHUB_RUN_NUMBER}`; sul ramo tag = `${GITHUB_REF_NAME}`. Aggiungo `is_main` = `true` sul ramo senza tag.

### D2 — Due pubblicazioni su main, una su tag

Sostituisco l'unico passo `Publish Release` con:

1. **Publish versioned release** (sempre): `tag_name=${version_tag}`, `name=${name}`, `prerelease=${prerelease}`, `files=APK`. Su main crea/aggiorna la release `${run}`; su tag crea la stabile.
2. **Update latest** (solo `if: is_main == 'true'`): `tag_name=latest`, `name=${name}`, `prerelease=true`, `files=APK`.

Entrambi con `softprops/action-gh-release@v2` e `fail_on_unmatched_files: true`.

*Perché due passi*: `action-gh-release` pubblica una release per invocazione; servono due target (versionata + `latest`).

### D3 — Nessun re-trigger

Il tag di versione è numerico (`8`), non combacia con il trigger `tags: ['v*']`, quindi la creazione del tag da parte dell'action non riavvia la pipeline.

## Risks / Trade-offs

- [L'APK viene caricato due volte su main (release versionata + `latest`)] → Accettato: storage trascurabile per un APK; il beneficio è avere sia lo storico sia il puntatore stabile.
- [Accumulo di molte release `8, 9, 10…`] → Atteso e voluto (è lo storico). Eventuale pulizia periodica delle prerelease vecchie è fuori scope.
- [`action-gh-release` crea il tag git numerico sul commit] → Comportamento desiderato ("metti anche i tag delle versioni"); non interferisce col ramo `v*`.
