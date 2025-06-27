<#
    Despliegue Unity headless como servicio NSSM.
    Uso: .\deploy.ps1 <URL_DEL_ZIP>
#>

param(
    [Parameter(Mandatory=$true)]
    [string]$DOWNLOAD_URL
)

# ---------- CONFIGURACIÓN ----------
$targetDir   = 'C:\GameServer'                       # Carpeta de despliegue
$zipPath     = Join-Path $targetDir 'build.zip'
$exePath     = Join-Path $targetDir 'windows-server.exe'  # EXE real
$serviceName = 'WindowsServer'                       # Nombre del servicio
$chocoExe    = 'C:\ProgramData\chocolatey\bin\choco.exe'
$sevenZipExe = 'C:\Program Files\7-Zip\7z.exe'
# -----------------------------------

$ErrorActionPreference = 'Stop'
$VerbosePreference     = 'Continue'

#--- helpers ---------------------------------------------------------------
function Ensure-Choco {
    if (-not (Test-Path $chocoExe)) {
        Write-Host 'Installing Chocolatey...'
        Set-ExecutionPolicy Bypass -Scope Process -Force
        Invoke-Expression (Invoke-WebRequest https://community.chocolatey.org/install.ps1 -UseBasicParsing).Content
    }
    # añadir al PATH para esta sesión
    $chocoDir = Split-Path $chocoExe
    if (-not ($env:Path -split ';' | Where-Object { $_ -eq $chocoDir })) {
        $env:Path += ";$chocoDir"
    }
}
function Ensure-Package([string]$pkg) {
    Ensure-Choco
    if (-not (choco list --local-only | Select-String "^$pkg")) {
        & $chocoExe install $pkg -y --no-progress
    }
}
function Ensure-7Zip { Ensure-Package 7zip }
function Ensure-Nssm { Ensure-Package nssm }
#-------------------------------------------------------------------------

# 1) Carpeta destino
if (-not (Test-Path $targetDir)) {
    New-Item -ItemType Directory -Path $targetDir -Force | Out-Null
    Write-Host "Created $targetDir"
}

# 2) Herramientas necesarias
Ensure-7Zip
Ensure-Nssm

# 3) Descargar build
Write-Host "Downloading: $DOWNLOAD_URL"
Start-BitsTransfer -Source $DOWNLOAD_URL -Destination $zipPath

# 4) Extraer
Write-Host 'Extracting...'
& $sevenZipExe x $zipPath "-o$targetDir" -y

# 5) Registrar / actualizar servicio NSSM
if (Get-Service -Name $serviceName -ErrorAction SilentlyContinue) {
    Write-Host "Service $serviceName exists - updating path"
    nssm set $serviceName Application  $exePath
    nssm set $serviceName AppDirectory $targetDir
    nssm set $serviceName AppParameters "-batchmode -nographics"
} else {
    Write-Host "Creating service $serviceName"
    nssm install $serviceName $exePath "-batchmode -nographics"
    nssm set     $serviceName AppDirectory $targetDir
}

# 6) Reiniciar servicio
Write-Host "Restarting $serviceName ..."
Restart-Service -Name $serviceName -Force

Write-Host 'Deploy complete.'
