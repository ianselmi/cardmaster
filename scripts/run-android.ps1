<#
.SYNOPSIS
  Build (ed eventuale deploy) dell'app CardMaster su Android.

.DESCRIPTION
  Compila il progetto MAUI per net10.0-android. Di default, se un emulatore/device
  e' connesso, installa e avvia l'app; concede il permesso CAMERA per evitare prompt
  nei test. Con -BuildOnly compila soltanto.

.PARAMETER BuildOnly
  Compila senza installare/avviare.

.PARAMETER Configuration
  Debug (default) o Release.

.PARAMETER Screenshot
  Dopo l'avvio, salva uno screenshot in scripts/last-screenshot.png.

.EXAMPLE
  ./scripts/run-android.ps1
  ./scripts/run-android.ps1 -BuildOnly
  ./scripts/run-android.ps1 -Screenshot
#>
param(
    [switch]$BuildOnly,
    [ValidateSet('Debug', 'Release')]
    [string]$Configuration = 'Debug',
    [switch]$Screenshot
)

$ErrorActionPreference = 'Stop'

$Root = Split-Path -Parent $PSScriptRoot
$Csproj = Join-Path $Root 'src/CardMaster/CardMaster.csproj'
$Tfm = 'net10.0-android'
$AppId = 'com.cardmaster.app'

function Resolve-Adb {
    $roots = @($env:LOCALAPPDATA, $env:ANDROID_HOME, $env:ANDROID_SDK_ROOT) | Where-Object { $_ }
    foreach ($r in $roots) {
        $p1 = Join-Path $r 'Android/Sdk/platform-tools/adb.exe'
        if (Test-Path $p1) { return $p1 }
        $p2 = Join-Path $r 'platform-tools/adb.exe'
        if (Test-Path $p2) { return $p2 }
    }
    if (Get-Command adb -ErrorAction SilentlyContinue) { return 'adb' }
    return $null
}

Write-Host "==> Build ($Configuration) $Tfm" -ForegroundColor Cyan

if ($BuildOnly) {
    dotnet build $Csproj -c $Configuration -f $Tfm
    if ($LASTEXITCODE -ne 0) { throw "Build fallita (exit $LASTEXITCODE)" }
    Write-Host "==> Build OK" -ForegroundColor Green
    return
}

$Adb = Resolve-Adb
$deviceConnected = $false
if ($Adb) {
    $devices = & $Adb devices 2>$null | Select-String "device$"
    $deviceConnected = [bool]$devices
}

if (-not $deviceConnected) {
    Write-Host "==> Nessun emulatore/device: eseguo solo la build" -ForegroundColor Yellow
    dotnet build $Csproj -c $Configuration -f $Tfm
    if ($LASTEXITCODE -ne 0) { throw "Build fallita (exit $LASTEXITCODE)" }
    Write-Host "==> Build OK (nessun deploy)" -ForegroundColor Green
    return
}

# Evita il prompt permesso camera nei test.
& $Adb shell pm grant $AppId android.permission.CAMERA 2>$null

Write-Host "==> Build + deploy + avvio su device" -ForegroundColor Cyan
dotnet build $Csproj -c $Configuration -f $Tfm -t:Run -p:AndroidAttachDebugger=false
if ($LASTEXITCODE -ne 0) { throw "Build/deploy fallita (exit $LASTEXITCODE)" }

if ($Screenshot) {
    Start-Sleep -Seconds 5
    $shot = Join-Path $PSScriptRoot 'last-screenshot.png'
    & $Adb exec-out screencap -p > $shot
    Write-Host "==> Screenshot: $shot" -ForegroundColor Green
}

Write-Host "==> Fatto" -ForegroundColor Green
