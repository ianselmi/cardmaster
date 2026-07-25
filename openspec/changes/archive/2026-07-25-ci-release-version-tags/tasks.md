## 1. Compute version

- [x] 1.1 Nel passo *Compute version*, ramo senza tag: impostare `name=${GITHUB_RUN_NUMBER}` (era `1.0.${GITHUB_RUN_NUMBER}`)
- [x] 1.2 Aggiungere output `version_tag` (ramo main = `${GITHUB_RUN_NUMBER}`; ramo tag = `${GITHUB_REF_NAME}`) e `is_main` (`true` senza tag)

## 2. Pubblicazione

- [x] 2.1 Rinominare/adeguare il passo esistente a *Publish versioned release*: `tag_name=${{ steps.ver.outputs.version_tag }}`, `name=${{ steps.ver.outputs.name }}`, `prerelease=${{ steps.ver.outputs.prerelease }}`, files = APK
- [x] 2.2 Aggiungere il passo *Update latest* con `if: steps.ver.outputs.is_main == 'true'`: `tag_name=latest`, `name=${{ steps.ver.outputs.name }}`, `prerelease=true`, files = APK
- [x] 2.3 Verificare che il ramo tag `v*` resti una singola Release stabile (nessun aggiornamento di `latest`)

## 3. Verifica

- [x] 3.1 Validare la sintassi YAML del workflow
- [x] 3.2 `openspec validate ci-release-version-tags --strict` senza errori
- [ ] 3.3 Dopo il push su `main`: esistono la Release taggata `<run>` (con APK) e la prerelease `latest`, entrambe col numero di build; l'app in Impostazioni mostra lo stesso numero
