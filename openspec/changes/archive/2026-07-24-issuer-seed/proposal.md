## Why

Quando l'utente scansiona o aggiunge una carta, l'app deve poter **riconoscere l'emittente** (es. il supermercato) per mostrarne nome, logo e colore e per suggerire il formato barcode atteso. In v1 (offline, nessun server) questo catalogo è un **seed statico incluso nell'app**. Serve prima di `maui-scan-card`, che lo userà per il riconoscimento e per il controllo duplicati.

## What Changes

- Introduzione di un **catalogo emittenti** come dato statico **bundle nell'app** (nessuna sync, nessuna rete).
- Nuovo modello `Issuer` con: id stabile, nome visualizzato, colore, formato barcode atteso (opzionale), riferimento al logo, e alias per il matching testuale.
- File **seed versionato** (`issuers.json`) incluso come asset dell'app, con un campo `version` per la compatibilità futura (v2 lo sostituirà con un catalogo sincronizzato).
- Servizio `IIssuerCatalog` che carica il seed una volta e offre lookup: elenco completo, ricerca per id e **match** per nome/alias (case-insensitive).
- Loghi degli emittenti come **immagini bundle** referenziate dall'emittente (placeholder in questa change; asset reali aggiunti nel tempo).
- Registrazione del servizio nel container DI.

## Capabilities

### New Capabilities
- `issuer-catalog`: catalogo statico degli emittenti (nome, logo, colore, formato barcode atteso) incluso nell'app, con lookup e matching, senza alcuna sincronizzazione. In v2 la change `issuer-catalog` estenderà questa capability con un catalogo lato server.

### Modified Capabilities
- Nessuna.

## Impact

- **Nuovo codice**: modello `Issuer`, servizio `IIssuerCatalog` + implementazione, asset seed `Resources/Raw/issuers.json`, eventuali loghi in `Resources/Images/`.
- **DI**: registrazione di `IIssuerCatalog` in `MauiProgram`.
- **Nessun impatto sul DB cifrato**: il catalogo è dato statico read-only, non entra nella base dati utente (SQLCipher).
- **Vincolo di qualità**: la soluzione deve **compilare senza errori** (`dotnet build`), criterio di accettazione.
- **Change successive abilitate**: `maui-scan-card` (riconoscimento emittente e controllo duplicati), `maui-show-card` (nome/colore/logo in visualizzazione).
