## 1. Modifica del workflow

- [x] 1.1 In `.github/workflows/build-apk.yml`, passo *Publish Release*: cambiare il campo `name` così che il ramo prerelease usi `steps.ver.outputs.name` invece della stringa "Ultima build (main)"
- [x] 1.2 Verificare che il ramo tag `v*` continui a usare `steps.ver.outputs.release_tag` come nome (invariato)

## 2. Verifica

- [x] 2.1 Validare la sintassi YAML del workflow (indentazione/espressione corretta)
- [x] 2.2 `openspec validate ci-release-app-version --strict` senza errori
- [ ] 2.3 Dopo il push su `main`, controllare che la prerelease `latest` mostri come titolo il numero `1.0.<run>` uguale a quello in Impostazioni dell'app
