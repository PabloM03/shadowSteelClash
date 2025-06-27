<#
    Despliega el ZIP de Unity en C:\GameServer y lo ejecuta como
    servicio NSSM (WindowsServer).

    Uso: .\deploy.ps1 <URL_ZIP>

    Si quieres emplear una cuenta distinta a LocalSystem,
    descomenta $serviceUser / $servicePass y pasa las credenciales
    (o usa una Managed Service Account).
#>

param(
    [Parameter(Mandatory = $true)]
    [string]$DOWNLOAD_URL
)

# -- CONFIG --------------------------------------------------------------
$targetDir    = 'C:\GameServer'
$zipPath      = Join-Path $targetDir 'build.zip'
$exePath      = Join-Path $targetDir 'windows-server.exe'
$serviceName  = 'WindowsServer'
$logFile      = Join-Path $targetDir 'server.log'
$appArgs      = "-batchmode -nographics -logfile $logFile"

# Cuenta para el servicio  (por defecto LocalSystem = cadena vacía)
$serviceUser  = ''           # ejemplo: 'opc'
$servicePass  = ''           # ejemplo: 'StrongP@ss!'
# -----------------------------------------------------------------------

$chocoExe     = 'C:\ProgramData\chocolatey\bin\choco.exe'
$sevenZipExe  = 'C:\Program Files\7-Zip\7z.exe'
$nssmLog      = Join-Path $targetDir 'service.log'

$ErrorActionPreference = 'Stop'
$VerbosePreference     = 'Continue'

# ---------- helpers ----------------------------------------------------
function Ensure-Choco {
    if (-not (Test-Path $chocoExe)) {
        Write-Host 'Installing Chocolatey ...'
        Set-ExecutionPolicy Bypass -Scope Process -Force
        Invoke-Expression (Invoke-WebRequest `
            https://community.chocolatey.org/install.ps1 -UseBasicParsing).Content
    }
    $dir = Split-Path $chocoExe
    if (-not ($env:Path -split ';' | Where-Object { $_ -eq $dir })) {
        $env:Path += ";$dir"
    }
}
function Ensure-Pkg([string]$pkg){
    Ensure-Choco
    if (-not (choco list --local-only | Select-String "^$pkg")) {
        & $chocoExe install $pkg -y --no-progress
    }
}
function Ensure-7Zip { Ensure-Pkg 7zip }
function Ensure-Nssm { Ensure-Pkg nssm }
# -----------------------------------------------------------------------

# 1) Crear carpeta destino
if (-not (Test-Path $targetDir)) {
    New-Item -ItemType Directory -Path $targetDir -Force | Out-Null
    Write-Host "Created $targetDir"
}

# 2) Herramientas
Ensure-7Zip
Ensure-Nssm

# 3) Descargar zip
Write-Host "Downloading: $DOWNLOAD_URL"
Start-BitsTransfer -Source $DOWNLOAD_URL -Destination $zipPath

# 4) Extraer
Write-Host 'Extracting ...'
& $sevenZipExe x $zipPath "-o$targetDir" -y

# 5) Crear / actualizar servicio NSSM
if (Get-Service -Name $serviceName -ErrorAction SilentlyContinue) {
    Write-Host 'Service exists – updating'
    nssm set $serviceName Application    $exePath
} else {
    Write-Host 'Creating service'
    nssm install $serviceName $exePath
}

# — ajustes comunes —
nssm set $serviceName AppDirectory   $targetDir
nssm set $serviceName AppParameters  $appArgs
nssm set $serviceName AppStdout      $nssmLog
nssm set $serviceName AppStderr      $nssmLog
nssm set $serviceName AppRotateFiles 1
nssm set $serviceName AppExit        Default Restart
nssm set $serviceName AppRestartDelay 5000
nssm set $serviceName AppThrottle    10000
nssm set $serviceName AppStartTimeout 90000

if ($serviceUser) {
    nssm set $serviceName ObjectName "$serviceUser" "$servicePass"
}

# 6) Reiniciar servicio
Write-Host "Restarting $serviceName ..."
Restart-Service -Name $serviceName -Force

Write-Host 'Deploy complete.'
