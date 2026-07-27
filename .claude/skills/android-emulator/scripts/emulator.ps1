<#
.SYNOPSIS
    Avvia, elenca o ferma un emulatore Android, risolvendo da solo quale SDK usare.

.DESCRIPTION
    Su questa macchina convivono piu' SDK Android (Visual Studio, Android Studio, standalone) e
    gli AVD non stanno in nessuno di essi: stanno in %USERPROFILE%\.android\avd e ognuno dichiara
    l'immagine di sistema che gli serve come percorso RELATIVO (image.sysdir.1). Lanciare
    l'emulatore dell'SDK sbagliato fallisce con "PANIC: Cannot find AVD system path", anche se
    l'immagine e' installata in un altro SDK.

    Lo script legge il config.ini di ogni AVD, cerca in quale SDK esiste davvero quell'immagine e
    lancia l'emulator.exe di QUELL'SDK con ANDROID_SDK_ROOT impostato di conseguenza.

.PARAMETER Avd
    Nome dell'AVD da avviare. Se omesso, sceglie l'AVD avviabile con l'API piu' alta.

.PARAMETER List
    Elenca gli AVD con l'SDK che li puo' avviare (o il motivo per cui non sono avviabili).

.PARAMETER Stop
    Spegne l'emulatore in esecuzione.

.PARAMETER ColdBoot
    Avvio a freddo, ignorando lo snapshot salvato (default: attivo, e' il piu' affidabile).

.PARAMETER TimeoutSeconds
    Attesa massima del boot completo. Default 300.

.EXAMPLE
    .\emulator.ps1 -List

.EXAMPLE
    .\emulator.ps1                       # avvia l'AVD con l'API piu' alta e aspetta il boot

.EXAMPLE
    .\emulator.ps1 -Avd pixel_7_-_api_36_0
#>
[CmdletBinding()]
param(
    [string]$Avd,
    [switch]$List,
    [switch]$Stop,
    [bool]$ColdBoot = $true,
    [int]$TimeoutSeconds = 300
)

$ErrorActionPreference = 'Stop'

# Candidati in ordine di preferenza. Le variabili d'ambiente vincono, poi i percorsi noti:
# l'SDK di Visual Studio e' quello che di solito ha le immagini di sistema piu' recenti.
function Get-SdkRoots {
    $candidates = @(
        $env:ANDROID_SDK_ROOT
        $env:ANDROID_HOME
        'C:\Program Files (x86)\Android\android-sdk'
        'C:\Program Files\Android\android-sdk'
        'C:\Android\android-sdk'
        (Join-Path $env:LOCALAPPDATA 'Android\Sdk')
    )

    $seen = [System.Collections.Generic.HashSet[string]]::new([StringComparer]::OrdinalIgnoreCase)
    foreach ($c in $candidates) {
        if ([string]::IsNullOrWhiteSpace($c)) { continue }
        if (-not (Test-Path $c)) { continue }
        $full = (Resolve-Path $c).Path
        if ($seen.Add($full)) { $full }
    }
}

function Get-Adb {
    foreach ($root in Get-SdkRoots) {
        $adb = Join-Path $root 'platform-tools\adb.exe'
        if (Test-Path $adb) { return $adb }
    }
    throw "adb.exe non trovato in nessun SDK Android noto."
}

# Ogni AVD e' una coppia <nome>.ini + <nome>.avd\config.ini in %USERPROFILE%\.android\avd.
function Get-Avds {
    $avdHome = if ($env:ANDROID_AVD_HOME) { $env:ANDROID_AVD_HOME } else { Join-Path $env:USERPROFILE '.android\avd' }
    if (-not (Test-Path $avdHome)) { return @() }

    $roots = @(Get-SdkRoots)

    Get-ChildItem $avdHome -Filter '*.ini' -File | ForEach-Object {
        $name = $_.BaseName
        $configPath = Join-Path $avdHome "$name.avd\config.ini"
        $sysdir = $null
        $api = 0

        if (Test-Path $configPath) {
            foreach ($line in Get-Content $configPath) {
                # I config.ini usano sia "chiave=valore" sia "chiave = valore".
                if ($line -match '^\s*image\.sysdir\.1\s*=\s*(.+?)\s*$') { $sysdir = $Matches[1] }
                if ($line -match '^\s*image\.androidVersion\.api\s*=\s*(\d+)\s*$') { $api = [int]$Matches[1] }
            }
        }

        # Fallback: l'API si legge anche dal target del file .ini esterno (es. "android-36").
        if ($api -eq 0) {
            $target = (Get-Content $_.FullName | Where-Object { $_ -match '^target\s*=' }) -replace '^target\s*=\s*', ''
            if ($target -match '(\d+)') { $api = [int]$Matches[1] }
        }

        $sdk = $null
        if ($sysdir) {
            $sdk = $roots | Where-Object { Test-Path (Join-Path $_ $sysdir) } | Select-Object -First 1
        }

        [pscustomobject]@{
            Name     = $name
            Api      = $api
            SysDir   = $sysdir
            SdkRoot  = $sdk
            Bootable = [bool]$sdk
        }
    }
}

$adb = Get-Adb

if ($Stop) {
    & $adb emu kill 2>&1 | Out-Null
    Write-Host "Emulatore fermato."
    return
}

$avds = @(Get-Avds)

if ($List) {
    if ($avds.Count -eq 0) { Write-Host "Nessun AVD trovato."; return }
    $avds | Sort-Object -Property Api -Descending | ForEach-Object {
        if ($_.Bootable) {
            "{0,-24} API {1,-4} -> {2}" -f $_.Name, $_.Api, $_.SdkRoot
        }
        else {
            "{0,-24} API {1,-4} -> NON AVVIABILE (immagine '{2}' assente in tutti gli SDK)" -f $_.Name, $_.Api, $_.SysDir
        }
    }
    return
}

# Se c'e' gia' un emulatore acceso, riusalo invece di avviarne un altro.
$running = (& $adb devices | Select-String -Pattern '^emulator-\d+\s+device' | Select-Object -First 1)
if ($running) {
    $serial = ($running.ToString() -split '\s+')[0]
    Write-Host "Emulatore gia' in esecuzione: $serial"
    return $serial
}

if ($Avd) {
    $target = $avds | Where-Object { $_.Name -eq $Avd } | Select-Object -First 1
    if (-not $target) { throw "AVD '$Avd' non trovato. Usa -List per vedere quelli disponibili." }
    if (-not $target.Bootable) {
        throw "AVD '$Avd' non avviabile: l'immagine '$($target.SysDir)' non e' installata in nessuno degli SDK noti."
    }
}
else {
    $target = $avds | Where-Object { $_.Bootable } | Sort-Object -Property Api -Descending | Select-Object -First 1
    if (-not $target) {
        $detail = ($avds | ForEach-Object { "$($_.Name) richiede $($_.SysDir)" }) -join '; '
        throw "Nessun AVD avviabile. $detail"
    }
}

Write-Host "Avvio '$($target.Name)' (API $($target.Api)) con SDK $($target.SdkRoot)"

$emulator = Join-Path $target.SdkRoot 'emulator\emulator.exe'
if (-not (Test-Path $emulator)) { throw "emulator.exe non trovato in $($target.SdkRoot)." }

# ANDROID_SDK_ROOT deve puntare all'SDK che contiene l'immagine, altrimenti l'emulatore
# la cerca altrove e va in PANIC anche se il binario e' quello giusto.
$env:ANDROID_SDK_ROOT = $target.SdkRoot
$env:ANDROID_HOME = $target.SdkRoot

$emuArgs = @('-avd', $target.Name)
if ($ColdBoot) { $emuArgs += '-no-snapshot-load' }

Start-Process -FilePath $emulator -ArgumentList $emuArgs -WindowStyle Minimized | Out-Null

& $adb wait-for-device
$deadline = (Get-Date).AddSeconds($TimeoutSeconds)
while ((Get-Date) -lt $deadline) {
    $booted = (& $adb shell getprop sys.boot_completed 2>$null) -match '1'
    if ($booted) {
        $serial = ((& $adb devices | Select-String -Pattern '^emulator-\d+\s+device' | Select-Object -First 1).ToString() -split '\s+')[0]
        Write-Host "Boot completato: $serial"
        return $serial
    }
    Start-Sleep -Seconds 5
}

throw "L'emulatore non ha completato il boot entro $TimeoutSeconds secondi."
