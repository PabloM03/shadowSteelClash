<#
    Despliega el build Unity que llega como ZIP, lo extrae en C:\GameServer
    y (re)configura un servicio NSSM llamado WindowsServer.
    Se invoca desde el workflow:  .\deploy.ps1 <URL_DEL_ZIP>
#>

param(
    [Parameter(Mandatory = $true)]
    [string]$DOWNLOAD_URL
)

# ---------- CONFIG -------------------------------
$targetDir   = 'C:\GameServer'                       # Carpeta de despliegue
$zipPath     = Join-Path $targetDir 'build.zip'
$exePath     = Join-Path $targetDir 'windows-server.exe'
$serviceName = 'WindowsServer'                       # Nombre del servicio
$appArgs     = '-batchmode -nographics'              # Flags para headless
$sevenZipExe = 'C:\Program Files\7-Zip\7z.exe'
$chocoExe    = 'C:\ProgramData\chocolatey\bin\choco.exe'
# -------------------------------------------------

$ErrorActionPreference = 'Stop'
$VerbosePreference     = 'Continue'

# ---------- helpers ------------------------------
function Ensure-Choco {
    if (-not (Test-Path $chocoExe)) {
        Write-Host 'Installing Chocolatey...'
        Set-ExecutionPolicy Bypass -Scope Process -Force
        Invoke-Expression (Invoke-WebRequest https://community.chocolatey.org/install.ps1 -UseBasicParsing).Content
    }
    $chocoDir = Split-Path $chocoExe
    if (-not ($env:Path -split ';' | Where-Object { $_ -eq $chocoDir })) {
        $env:Path += ";$chocoDir"
    }
}
function Ensure-Pkg ([string]$pkg) {
    Ensure-Choco
    if (-not (choco list --local-only | Select-String "^$pkg")) {
        & $chocoExe install $pkg -y --no-progress
    }
}
function Ensure-7Zip { Ensure-Pkg 7zip }
function Ensure-Nssm { Ensure-Pkg nssm }
# -----------------------------------------------

# 1) Crear carpeta de destino
if (-not (Test-Path $targetDir)) {
    New-Item -Type Directory -Path $targetDir -Force | Out-Null
    Write-Host "Created $targetDir"
}

# 2) Herramientas necesarias
Ensure-7Zip
Ensure-Nssm

# 3) Descargar ZIP
Write-Host "Downloading: $DOWNLOAD_URL"
Start-BitsTransfer -Source $DOWNLOAD_URL -Destination $zipPath

# 4) Extraer ZIP
Write-Host 'Extracting...'
& $sevenZipExe x $zipPath "-o$targetDir" -y

# 5) Crear o actualizar servicio NSSM
if (Get-Service -Name $serviceName -ErrorAction SilentlyContinue) {

    Write-Host "Service $serviceName exists – updating path and parameters"
    nssm set $serviceName Application    $exePath
    nssm set $serviceName AppDirectory   $targetDir
    nssm set $serviceName AppParameters  $appArgs

} else {

    Write-Host "Creating service $serviceName"
    nssm install $serviceName $exePath
    nssm set     $serviceName AppDirectory  $targetDir
    nssm set     $serviceName AppParameters $appArgs
}

# 6) Reiniciar el servicio
Write-Host "Restarting $serviceName ..."
Restart-Service -Name $serviceName -Force

Write-Host 'Deploy complete.'
