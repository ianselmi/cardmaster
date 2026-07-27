---
name: android-emulator
description: Avvia l'emulatore Android e ci installa l'app CardMaster, per verificare una change sul dispositivo. Da usare quando serve provare l'app davvero (screenshot, verifica funzionale di una change, riproduzione di un bug) e non basta `dotnet build`. Gestisce la scelta dell'SDK giusto tra i piu' installati sulla macchina.
---

# Emulatore Android per CardMaster

Avvia un emulatore, ci deploya l'app e la guida via `adb`, senza passare da Visual Studio.

## Perche' serve questo script

Sulla macchina convivono **piu' SDK Android** (Visual Studio, Android Studio, standalone) e gli AVD
non stanno dentro nessuno di essi: stanno in `%USERPROFILE%\.android\avd`, e ciascuno dichiara
l'immagine di sistema che gli serve come percorso **relativo** (`image.sysdir.1`, es.
`system-images\android-36\google_apis_playstore\x86_64\`).

Quel percorso viene risolto rispetto all'SDK da cui lanci `emulator.exe`. Lanciare l'emulatore
dell'SDK sbagliato fallisce con `PANIC: Cannot find AVD system path` **anche se l'immagine e'
installata**, solo in un altro SDK. Lo script legge il `config.ini` dell'AVD, trova l'SDK che
contiene davvero quell'immagine e lancia quell'`emulator.exe` con `ANDROID_SDK_ROOT` coerente.

Su questa macchina (27 lug 2026): l'SDK di **Visual Studio**, `C:\Program Files (x86)\Android\android-sdk`,
e' quello con le immagini piu' complete (API 26/29/31/34/35/36). Non dare per scontato che valga
ancora: usa `-List`, che lo verifica.

## Uso

```powershell
# quali AVD ci sono e quale SDK li puo' avviare
.\.claude\skills\android-emulator\scripts\emulator.ps1 -List

# avvia (sceglie l'API piu' alta avviabile) e aspetta il boot completo
.\.claude\skills\android-emulator\scripts\emulator.ps1

# avvia un AVD preciso
.\.claude\skills\android-emulator\scripts\emulator.ps1 -Avd pixel_7_-_api_36_0

# spegni
.\.claude\skills\android-emulator\scripts\emulator.ps1 -Stop
```

Lo script e' idempotente: se un emulatore e' gia' acceso lo riusa e restituisce il serial, invece
di avviarne un altro. Ritorna solo quando `sys.boot_completed` e' `1`, quindi dopo puoi usare
`adb` senza altre attese.

## Deploy dell'app

```powershell
dotnet build src\CardMaster\CardMaster.csproj -t:Run -f net10.0-android
```

`-t:Run` compila, installa **e** lancia l'app. Per simulare una versione diversa da quella del
`.csproj` (utile per i test di aggiornamento) si passano le proprieta' sulla riga di comando,
senza toccare il file:

```powershell
dotnet build src\CardMaster\CardMaster.csproj -t:Run -f net10.0-android `
  -p:ApplicationDisplayVersion=28 -p:ApplicationVersion=28
```

## Guidare l'app via adb

`adb.exe` sta in `<sdk>\platform-tools\adb.exe` (lo script lo trova da solo; per uso manuale,
quello dell'SDK di Visual Studio va bene per qualunque emulatore).

```powershell
$adb = "C:\Program Files (x86)\Android\android-sdk\platform-tools\adb.exe"

& $adb exec-out screencap -p > screenshot.png   # screenshot
& $adb shell input tap <x> <y>                  # tocco
& $adb shell input text "testo"                 # solo ASCII: gli accenti non passano
& $adb shell input keyevent 111                 # ESC: chiude la tastiera
& $adb shell input keyevent 4                   # BACK
& $adb shell dumpsys package com.cardmaster.app | Select-String versionName
& $adb shell cmd connectivity airplane-mode enable   # per i test offline
```

**Coordinate degli screenshot**: le immagini sono 1080x2400 ma vengono mostrate ridimensionate.
Moltiplica le coordinate lette sullo screenshot per il fattore di scala indicato prima di passarle
a `input tap`, altrimenti i tocchi finiscono altrove.

**Dopo ogni tocco che cambia il layout** (aggiunta di un chip, comparsa di una sezione) rifai lo
screenshot prima del tocco successivo: i pulsanti si spostano, e un tap a coordinate vecchie
sembra "non aver fatto nulla" quando in realta' ha colpito il vuoto.

## Verificare una migrazione di schema o un aggiornamento

Per provare cosa succede *aggiornando* l'app invece che installandola pulita, serve la versione
precedente installata per prima. Usa un worktree, cosi' il working tree resta intatto:

```powershell
git worktree add "$env:TEMP\cardmaster-main" main
dotnet build "$env:TEMP\cardmaster-main\src\CardMaster\CardMaster.csproj" -t:Run -f net10.0-android
# ... crea dati, poi installa sopra la build corrente ...
dotnet build src\CardMaster\CardMaster.csproj -t:Run -f net10.0-android
git worktree remove "$env:TEMP\cardmaster-main" --force
```

Le build di debug sono firmate con la stessa chiave, quindi l'installazione sopra e' un vero
aggiornamento e **i dati dell'app vengono conservati** — che e' esattamente cio' che si vuole
verificare.

## Trappole gia' incontrate

**Non usare `adb shell pm clear` sulle build di debug.** Il debug usa *Fast Deployment*: gli
assembly stanno nella cartella dati dell'app (`files/.__override__/`), non nell'APK. `pm clear`
li cancella e l'app non parte piu', con un `SIGABRT` e in logcat:

```
monodroid: No assemblies found in '...__override__/x86_64'. Assuming this is part of Fast Deployment. Exiting...
```

Per ripartire da dati puliti: `adb uninstall com.cardmaster.app` e poi il solito
`dotnet build -t:Run` — non `pm clear`.

**Lavori periodici di WorkManager (backup, controllo aggiornamenti).**

```powershell
# elenco dei job dell'app (l'id cambia a ogni ri-registrazione!)
& $adb shell dumpsys jobscheduler | Select-String "JOB androidx.work.systemjobscheduler.*cardmaster"

# forzare un job: serve il namespace di WorkManager, non basta il package
& $adb shell cmd jobscheduler run -f -n androidx.work.systemjobscheduler com.cardmaster.app <jobId>
```

Due comportamenti che sembrano bug e non lo sono:

- **`am force-stop` mette il pacchetto in stato *stopped***: Android non esegue piu' i suoi job, e
  al riavvio WorkManager logga `Application was force-stopped, rescheduling` e **riprogramma** il
  job con un id nuovo invece di eseguirlo. Per chiudere l'app senza questo effetto usare
  `am kill` (che pero' spesso produce comunque il reschedule al riavvio del processo).
- **Un `PeriodicWorkRequest` non si puo' far scattare prima della sua finestra**, nemmeno con
  `-f`: quel flag aggira i vincoli di JobScheduler, non il periodo di WorkManager. In logcat si
  vede `Delaying execution for <Worker> because it is being executed before schedule`. Per
  verificare davvero il corpo del worker, ridurre temporaneamente il periodo al minimo consentito
  (15 minuti) e aspettare lo scatto naturale, poi rimettere il periodo vero.

Che il worker sia stato *risolto* si vede comunque in logcat (`WM-WorkerWrapper: ... <Worker>`),
utile per distinguere "worker non registrato" da "worker non ancora eseguito".

## Se non parte

- `-List` dice `NON AVVIABILE`: l'immagine di sistema di quell'AVD non e' installata in nessun SDK.
  Installala dal SDK Manager (Visual Studio: *Tools > Android > Android SDK Manager*), oppure usa
  un altro AVD.
- `dsound`/`Vulkan` fra i messaggi d'errore: sono avvisi innocui, l'emulatore parte lo stesso.
- L'AVD risulta acceso ma `adb devices` e' vuoto: `-Stop` e riprova; l'avvio a freddo
  (default) evita gli snapshot corrotti.
