## 1. Provider SQLite

- [x] 1.1 Rimuovere il pacchetto `SQLitePCLRaw.bundle_e_sqlcipher`; aggiungere `SQLitePCLRaw.bundle_e_sqlite3`
- [x] 1.2 `DatabaseService`: rimuovere il parametro `key` e la dipendenza da `IKeyStoreService`; aprire la connessione in chiaro
- [x] 1.3 Rimuovere `IKeyStoreService` e `Platforms/Android/Services/KeyStoreService.cs`; rimuovere la registrazione DI

## 2. Aggiornamento pacchetti

- [x] 2.1 Aggiornare `Microsoft.Maui.Controls` (10.0.20→10.0.90) e `Microsoft.Extensions.Logging.Debug` (10.0.0→10.0.10)
- [x] 2.2 Verificare che non restino altri pacchetti aggiornabili non intenzionali — *le altre dipendenze erano già all'ultima*

## 3. Documentazione/vincoli

- [x] 3.1 Aggiornare `openspec/config.yaml` e `PLAN.md` (storage in chiaro, niente SQLCipher/Keystore)
- [x] 3.2 Aggiornare `docs/technical-notes.md` (la trappola SQLCipher non è più applicabile; annotare la scelta e l'alternativa SQLite3MC)

## 4. Verifica

- [x] 4.1 `dotnet build`: compila senza errori (criterio di accettazione)
- [x] 4.2 Clear dati app + runtime su emulatore: l'app parte, si può aggiungere una carta e rivederla (DB in chiaro funzionante) — *verificato: carta "CartaInChiaro" aggiunta e visibile in griglia*
- [x] 4.3 Verificare che il file DB ora abbia header `SQLite format 3` in chiaro (conferma provider non cifrato) — *verificato via adb: header `SQLite format 3`*
- [x] 4.4 `openspec validate storage-plain-sqlite` senza errori
