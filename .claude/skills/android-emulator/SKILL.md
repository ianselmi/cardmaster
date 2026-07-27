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

## Se non parte

- `-List` dice `NON AVVIABILE`: l'immagine di sistema di quell'AVD non e' installata in nessun SDK.
  Installala dal SDK Manager (Visual Studio: *Tools > Android > Android SDK Manager*), oppure usa
  un altro AVD.
- `dsound`/`Vulkan` fra i messaggi d'errore: sono avvisi innocui, l'emulatore parte lo stesso.
- L'AVD risulta acceso ma `adb devices` e' vuoto: `-Stop` e riprova; l'avvio a freddo
  (default) evita gli snapshot corrotti.
