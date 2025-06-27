param(
    [Parameter(Mandatory=$true)]
    [string]$DOWNLOAD_URL
)

# --- CONFIG --------------------------------------------------------------
$targetDir    = 'C:\GameServer'
$zipPath      = Join-Path $targetDir 'build.zip'
$exePath      = Join-Path $targetDir 'windows-server.exe'
$serviceName  = 'WindowsServer'
$appArgs      = '-batchmode -nographics -logfile C:\GameServer\server.log'
$sevenZipExe  = 'C:\Program Files\7-Zip\7z.exe'
$chocoExe     = 'C:\ProgramData\chocolatey\bin\choco.exe'
$nssmLog      = 'C:\GameServer\service.log'
# -------------------------------------------------------------------------

$ErrorActionPreference = 'Stop'
$VerbosePreference     = 'Continue'

function Ensure-Choco {
    if (-not (Test-Path $chocoExe)) {
        Set-ExecutionPolicy Bypass -Scope Process -Force
        Invoke-Expression (Invoke-WebRequest `
            https://community.chocolatey.org/install.ps1 -UseBasicParsing).Content
    }
    $dir = Split-Path $chocoExe
    if (-not ($env:Path -split ';' | Where-Object { $_ -eq $dir })) {
        $env:Path += ";$dir"
    }
}
function Ensure-Pkg ([string]$pkg) {
    Ensure-Choco
    if (-not (choco list --local-only | Select-String "^$pkg")) {
        & $chocoExe install $pkg -y --no-progress
    }
}
Ensure-Pkg 7zip
Ensure-Pkg nssm

if (-not (Test-Path $targetDir)) {
    New-Item -ItemType Directory -Path $targetDir -Force | Out-Null
}

Start-BitsTransfer -Source $DOWNLOAD_URL -Destination $zipPath
& $sevenZipExe x $zipPath "-o$targetDir" -y

if (Get-Service $serviceName -ErrorAction SilentlyContinue) {
    Write-Host 'Service exists – updating'
    nssm set $serviceName Application    $exePath
} else {
    Write-Host 'Creating service'
    nssm install $serviceName $exePath
}

# ajustes comunes
nssm set $serviceName AppDirectory   $targetDir
nssm set $serviceName AppParameters  $appArgs
nssm set $serviceName AppStdout      $nssmLog
nssm set $serviceName AppStderr      $nssmLog
nssm set $serviceName AppRotateFiles 1
nssm set $serviceName AppExit        Default Restart
nssm set $serviceName AppRestartDelay 5000
nssm set $serviceName AppThrottle    10000

Restart-Service -Name $serviceName -Force
Write-Host 'Deploy complete.'
