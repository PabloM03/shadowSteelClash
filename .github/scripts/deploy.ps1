param([string]$DOWNLOAD_URL)

$target       = 'C:\GameServer'
$zipPath      = Join-Path $target 'build.zip'
$exePath      = Join-Path $target 'ShadowSteelServer.exe'
$serviceName  = 'ShadowSteelServer'
$sevenZipExe  = 'C:\Program Files\7-Zip\7z.exe'

$ErrorActionPreference='Stop'; $VerbosePreference='Continue'

#--------- helpers ---------------------------------
function Ensure-Choco {[CmdletBinding()] param()
  if (-not (Get-Command choco -Ea SilentlyContinue)) {
    Set-ExecutionPolicy Bypass -Scope Process -Force
    Invoke-Expression (Invoke-WebRequest https://community.chocolatey.org/install.ps1 -UseBasicParsing).Content
  }
}
function Ensure-7zip {
  if (-not (Test-Path $sevenZipExe)) {
    Ensure-Choco; choco install 7zip -y --no-progress
  }
}
function Ensure-Nssm {
  if (-not (Get-Command nssm -Ea SilentlyContinue)) {
    Ensure-Choco; choco install nssm -y --no-progress
  }
}
#---------------------------------------------------

# 1) Carpeta destino
if (-not (Test-Path $target)) { New-Item -Item Directory -Path $target -Force | Out-Null }

# 2) Herramientas
Ensure-7zip; Ensure-Nssm

# 3) Descargar y extraer
Write-Host "Downloading: $DOWNLOAD_URL"
Start-BitsTransfer -Source $DOWNLOAD_URL -Destination $zipPath
Write-Host 'Extracting...'
& $sevenZipExe x $zipPath "-o$target" -y

# 4) (Re)crear servicio con NSSM
if (Get-Service -Name $serviceName -ErrorAction SilentlyContinue) {
  Write-Host "Service exists -> updating path"
  nssm set $serviceName Application $exePath
  nssm set $serviceName AppParameters "-batchmode -nographics"
} else {
  Write-Host "Creating service $serviceName"
  nssm install $serviceName $exePath -batchmode -nographics
}

# 5) Reiniciar
Write-Host "Restarting $serviceName ..."
Restart-Service -Name $serviceName -Force

Write-Host 'Deploy complete.'
