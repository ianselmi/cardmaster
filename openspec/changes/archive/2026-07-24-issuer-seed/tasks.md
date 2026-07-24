## 1. Modello e seed

- [x] 1.1 Definire il modello `Issuer` (`Id`, `Name`, `ColorHex`, `ExpectedBarcodeFormat?`, `LogoAsset?`, `Aliases`)
- [x] 1.2 Creare l'asset seed `Resources/Raw/issuers.json` con struttura versionata `{ "version": 1, "issuers": [...] }` e un piccolo set di esempio (senza loghi ufficiali)
- [x] 1.3 Verificare che `issuers.json` sia incluso come `MauiAsset` (già coperto dalla glob `Resources/Raw/**` nel csproj)

## 2. Servizio catalogo

- [x] 2.1 Definire `IIssuerCatalog` (`GetAllAsync`, `GetByIdAsync`, `MatchAsync`)
- [x] 2.2 Implementare il caricamento del seed da `FileSystem.OpenAppPackageFileAsync("issuers.json")` con `System.Text.Json`, guardia idempotente (`SemaphoreSlim`), indice per id
- [x] 2.3 Implementare `MatchAsync`: confronto case-insensitive di `text` con `Name` e `Aliases` (trim/normalizzazione basilare)
- [x] 2.4 Gestire errori espliciti: asset mancante o JSON malformato → eccezione chiara; versione sconosciuta → errore diagnosticabile
- [x] 2.5 Registrare `IIssuerCatalog` come singleton in `MauiProgram`

## 3. Verifica

- [x] 3.1 `dotnet build`: compila senza errori
- [x] 3.2 Verifica runtime su emulatore: il catalogo si carica dall'asset; `GetAllAsync` restituisce il set di esempio; `GetByIdAsync` trova/della mancata corrispondenza restituisce null; `MatchAsync` trova per nome e per alias (case-insensitive) — *verificato: count=5, byIdOk, byIdMissingNull, matchByName, matchByAlias, matchNoneNull tutti True*
- [x] 3.3 `openspec validate issuer-seed` senza errori
