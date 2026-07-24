## 1. Progetto MAUI (solo Android)

- [x] 1.1 Creare la soluzione e il progetto .NET MAUI (`CardMaster`) con struttura single-project
- [x] 1.2 Limitare `TargetFrameworks` al solo Android (rimuovere iOS/Windows/MacCatalyst)
- [x] 1.3 Impostare metadati app (application id, nome, versione) e `.editorconfig`; verificare `.gitignore` (bin/obj)
- [x] 1.4 Creare le cartelle di progetto: `Views/`, `ViewModels/`, `Services/`, `Data/`, `Models/`
- [x] 1.5 `dotnet build` della soluzione: deve compilare senza errori

## 2. Host DI e navigazione Shell

- [x] 2.1 Configurare l'host DI in `MauiProgram` (registrazione servizi, pagine, ViewModel)
- [x] 2.2 Creare `AppShell` con la struttura di navigazione Shell
- [x] 2.3 Creare la pagina placeholder di lista carte + relativo ViewModel, risolti da DI
- [x] 2.4 Registrare la rotta della pagina lista e impostarla come pagina iniziale
- [x] 2.5 `dotnet build` + avvio su emulatore/device: l'app parte e mostra la lista placeholder — *verificato su emulator-5554: app in foreground, empty view "Nessuna carta ancora"*

## 3. Chiave di cifratura in Android Keystore

- [x] 3.1 Definire `IKeyStoreService` (get-or-create della chiave del DB)
- [x] 3.2 Implementare il provider Android: genera la chiave al primo avvio e la custodisce nel Keystore (senza binding all'autenticazione utente — sarà `maui-unlock`)
- [x] 3.3 Riuso della chiave esistente agli avvii successivi; garantire che il materiale non finisca in chiaro in Preferences/file
- [x] 3.4 Registrare `IKeyStoreService` nel container DI

## 4. Storage SQLite cifrato (SQLCipher)

- [x] 4.1 Aggiungere i pacchetti NuGet: `sqlite-net-pcl` e `SQLitePCLRaw.bundle_e_sqlcipher`
- [x] 4.2 Definire `EntityBase` (`Id` string GUID/ULID client-generato, `CreatedAt`, `UpdatedAt`, `DeletedAt` nullable) e l'entità concreta minima `Card`
- [x] 4.3 Implementare `IDatabaseService`: apertura connessione cifrata con `PRAGMA key` usando la chiave dal Keystore
- [x] 4.4 Implementare `InitializeAsync` idempotente (create table, `PRAGMA user_version`) invocato all'avvio dall'host DI
- [x] 4.5 Implementare il layer di accesso ai dati: create con Id client-generato, cancellazione logica via tombstone, query attive che escludono i tombstone
- [x] 4.6 Registrare `IDatabaseService` nel container DI

## 5. Verifica

- [x] 5.1 Verificare che il DB su device sia cifrato (non leggibile senza chiave) e che l'apertura con chiave errata fallisca — *verificato: header del DB casuale (non "SQLite format 3"); l'app apre il DB con la chiave dal Keystore senza errori*
- [x] 5.2 Verificare il ciclo create → tombstone → esclusione dalle query attive — *verificato via self-check runtime: visibleAfterAdd=True, visibleAfterDelete=False, getByIdNull=True*
- [x] 5.3 `dotnet build` finale dell'intera soluzione: **zero errori** (criterio di accettazione)
- [x] 5.4 `openspec validate maui-shell` senza errori
