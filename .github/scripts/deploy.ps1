param(
    [Parameter(Mandatory=$true)]
    [string]$DOWNLOAD_URL
)

# -------- Configuración --------
$targetDir    = 'C:\GameServer'
$zipPath      = Join-Path $targetDir 'build.zip'
$exePath      = Join-Path $targetDir 'ShadowSteelServer.exe'
$serviceName  = 'ShadowSteelServer'
$sevenZipExe  = 'C:\Program Files\7-Zip\7z.exe'
$chocoExe     = 'C:\ProgramData\chocolatey\bin\choco.exe'
# --------------------------------

$ErrorActionPreference='Stop'; $VerbosePreference='Continue'

function Ensure-Choco {
    if (-not (Test-Path $chocoExe)) {
        Write-Host 'Installing Chocolatey...'
        Set-ExecutionPolicy Bypass -Scope Process -Force
        Invoke-Expression (Invoke-WebRequest https://community.chocolatey.org/install.ps1 -UseBasicParsing).Content
    }
    # añadir al PATH para la sesión actual
    if (-not ($env:Path -split ';' | Where-Object { $_ -eq (Split-Path $chocoExe) })) {
        $env:Path += ';' + (Split-Path $chocoExe)
    }
}

function Ensure-7zip {
    if (-not (Test-Path $sevenZipExe)) {
        Ensure-Choco
        & $chocoExe install 7zip -y --no-progress
    }
}

function Ensure-Nssm {
    if (-not (Get-Command nssm.exe -Ea SilentlyContinue)) {
        Ensure-Choco
        & $chocoExe install nssm -y --no-progress
        # añadir ruta donde chocó puso el shim por si no está
        $env:Path += ';C:\ProgramData\chocolatey\bin'
    }
}

# 1) Carpeta destino
if (-not (Test-Path $targetDir)) {
    New-Item -ItemType Directory -Path $targetDir -Force | Out-Null
    Write-Host "Created $targetDir"
}

# 2) Herramientas necesarias
Ensure-7zip
Ensure-Nssm

# 3) Descargar y extraer
Write-Host "Downloading: $DOWNLOAD_URL"
Start-BitsTransfer -Source $DOWNLOAD_URL -Destination $zipPath

Write-Host 'Extracting...'
& $sevenZipExe x $zipPath "-o$targetDir" -y

# 4) Registrar o actualizar el servicio
if (Get-Service -Name $serviceName -ErrorAction SilentlyContinue) {
    Write-Host "Service $serviceName already exists, updating path"
    nssm set $serviceName Application $exePath
    nssm set $serviceName AppParameters "-batchmode -nographics"
} else {
    Write-Host "Creating service $serviceName"
    nssm install $serviceName $exePath -batchmode -nographics
}

# 5) Reiniciar el servicio
Write-Host "Restarting $serviceName ..."
Restart-Service -Name $serviceName -Force

Write-Host 'Deploy complete.'
