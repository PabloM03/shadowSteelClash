param([string]$DOWNLOAD_URL)

$targetDir    = 'C:\GameServer'
$zipPath      = "$targetDir\build.zip"
$exePath      = "$targetDir\ShadowSteelServer.exe"
$serviceName  = 'ShadowSteelServer'
$sevenZipExe  = 'C:\Program Files\7-Zip\7z.exe'
$chocoExe     = 'C:\ProgramData\chocolatey\bin\choco.exe'

$ErrorActionPreference='Stop'; $VerbosePreference='Continue'

function Ensure-Choco {
    if (-not (Test-Path $chocoExe)) {
        Set-ExecutionPolicy Bypass -Scope Process -Force
        iex (iwr https://community.chocolatey.org/install.ps1 -UseBasicParsing).Content
    }
    if (-not ($env:Path -split ';' | Where-Object { $_ -eq (Split-Path $chocoExe) })) {
        $env:Path += ';' + (Split-Path $chocoExe)
    }
}
function Ensure-Pkg([string]$pkg){
    Ensure-Choco
    if (-not (choco list --local-only | Select-String "^$pkg")) {
        & $chocoExe install $pkg -y --no-progress
    }
}
Ensure-Pkg 7zip
Ensure-Pkg nssm

# 1) crear carpeta destino
if (-not (Test-Path $targetDir)) { New-Item -Item Directory -Path $targetDir -Force | Out-Null }

# 2) descargar y extraer
Start-BitsTransfer -Source $DOWNLOAD_URL -Destination $zipPath
& $sevenZipExe x $zipPath "-o$targetDir" -y

# 3) registrar / actualizar servicio
if (Get-Service $serviceName -ErrorAction SilentlyContinue) {
    nssm set $serviceName Application    $exePath
    nssm set $serviceName AppDirectory   $targetDir
    nssm set $serviceName AppParameters "-batchmode -nographics"
} else {
    nssm install $serviceName $exePath "-batchmode -nographics"
    nssm set     $serviceName AppDirectory $targetDir
}

# 4) reiniciar
Restart-Service -Name $serviceName -Force
Write-Host 'Deploy complete.'
