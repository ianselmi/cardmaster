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

## Storage locale (SQLite) — client MAUI

> Origine: `maui-shell`; cifratura rimossa con `storage-plain-sqlite` (24 lug 2026).

### Provider SQLite: `sqlite-net-base` + `SQLitePCLRaw.bundle_e_sqlite3`
- Pacchetti: **`sqlite-net-base`** (ORM, senza bundle proprio) + **`SQLitePCLRaw.bundle_e_sqlite3`** (provider SQLite in chiaro, mantenuto).
- All'avvio (`MauiProgram.CreateMauiApp`) chiamare `SQLitePCL.Batteries_V2.Init()` per attivare il provider (unico bundle referenziato).

**Il DB v1 è in chiaro** (nessuna cifratura at-rest). L'header del file `.db3` è quindi il consueto
`SQLite format 3`.

### Perché niente SQLCipher (storico)
La v1 usava `SQLitePCLRaw.bundle_e_sqlcipher` per cifrare il DB, ma quel pacchetto è **deprecato**
(legacy, non mantenuto da SQLitePCLRaw 3.0) e senza rimpiazzo drop-in gratuito. Decisione: cifratura
non essenziale per la v1 offline → SQLite in chiaro. **Trappola storica ancora valida come principio:**
non usare `sqlite-net-pcl` (trascina un secondo provider e crea ambiguità sul provider attivo); con
`sqlite-net-base` si controlla esattamente quale bundle è referenziato.

### Se in futuro servisse di nuovo la cifratura
Usare **`SQLite3MC.PCLRaw.bundle`** (SQLite3 Multiple Ciphers, di utelle) — mantenuto e gratuito,
supporta la cifratura via `PRAGMA key`. NON riusare `bundle_e_sqlcipher` (deprecato).

### minSdk Android 23
`SupportedOSPlatformVersion` per android è `23.0` (Android 6.0): un minimo moderno ragionevole
(in origine richiesto dalle API Keystore, ora rimosse; lasciato invariato).
