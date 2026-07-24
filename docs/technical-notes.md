# Note tecniche — CardMaster

Registro di decisioni tecniche e trappole scoperte durante lo sviluppo, verificate a runtime.
Complementare a `PLAN.md` (piano/roadmap) e agli artifact OpenSpec (spec/design per change).

---

## Convenzioni trasversali

### La compilazione senza errori è obbligatoria
Ogni change/deliverable DEVE compilare senza errori: la build pulita (`dotnet build`, 0 errori)
è un **criterio di accettazione**, non un dettaglio. Ogni change OpenSpec include un passo finale
di verifica build e non è considerata completa finché non compila.

---

## Storage cifrato (SQLCipher) — client MAUI

> Origine: change `maui-shell` (archiviata in `openspec/changes/archive/2026-07-24-maui-shell/`).

### Provider SQLite: usare `sqlite-net-base`, NON `sqlite-net-pcl`
Per cifrare davvero il database:

- Pacchetti: **`sqlite-net-base`** + **`SQLitePCLRaw.bundle_e_sqlcipher`**.
- All'avvio (`MauiProgram.CreateMauiApp`) chiamare `SQLitePCL.Batteries_V2.Init()` per attivare il provider.

**Perché non `sqlite-net-pcl`:** trascina transitivamente `SQLitePCLRaw.bundle_green`
(provider `e_sqlite3`, SQLite in chiaro). Con due provider presenti vince quello non cifrato:
`PRAGMA key` diventa un **no-op** e il DB nasce **non cifrato** (header `SQLite format 3` leggibile).
`sqlite-net-base` è lo stesso ORM ma senza il bundle in chiaro, così `bundle_e_sqlcipher` resta
l'unico provider.

### Come verificare che il DB sia davvero cifrato (a runtime)
I primi byte del file `.db3` devono essere **casuali**, non `SQLite format 3`:

```bash
# emulatore/device, app debuggable
adb shell run-as <package> od -A x -t x1 -N 16 files/<db>.db3
# atteso: byte casuali (es. cb b7 c4 5f ...), NON "SQLite format 3"
```

Se l'app apre il DB con la chiave dal Keystore senza errori, la chiave è corretta
(altrimenti SQLCipher lancerebbe "file is not a database").

### Chiave di cifratura nell'Android Keystore
L'Android Keystore non restituisce il materiale delle chiavi che custodisce, ma SQLCipher ha
bisogno di una passphrase come byte/stringa. Pattern usato:

1. Nel Keystore vive una chiave **AES-GCM** (il suo materiale non lascia mai il Keystore).
2. Si genera una passphrase casuale per il DB, la si **cifra** con la chiave del Keystore e si salva
   SOLO il ciphertext (IV + dati) nelle `Preferences`. La passphrase non è mai in chiaro.
3. All'apertura si **decifra** la passphrase tramite la chiave del Keystore e la si passa a `PRAGMA key`.

**Punto di innesto per `maui-unlock`:** aggiungere `SetUserAuthenticationRequired(true)` alla
`KeyGenParameterSpec` della chiave AES lega la decifratura all'autenticazione utente (biometria/PIN).
In `maui-shell` questo binding NON è presente (l'app deve avviarsi senza gate). L'interfaccia
`IKeyStoreService` isola il provider così che `maui-unlock` lo estenda senza toccare il resto.

### minSdk Android 23
Le API Keystore AES-GCM (`KeyGenParameterSpec`, block mode GCM, ecc.) richiedono **API 23**
(Android 6.0). `SupportedOSPlatformVersion` per android è impostato a `23.0` nel `.csproj`.
