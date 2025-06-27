param(
    [Parameter(Mandatory=$true)]
    [string]$DOWNLOAD_URL
)

# --- CONFIG ---------------------------------------------------------------
$targetDir   = 'C:\GameServer'
$zipPath     = Join-Path $targetDir 'build.zip'
$exePath     = Join-Path $targetDir 'windows-server.exe'
$serviceName = 'WindowsServer'
$appArgs     = '-batchmode -nographics'
$sevenZipExe = 'C:\Program Files\7-Zip\7z.exe'
$chocoExe    = 'C:\ProgramData\chocolatey\bin\choco.exe'
# -------------------------------------------------------------------------

$ErrorActionPreference = 'Stop'
$VerbosePreference     = 'Continue'

function Ensure-Choco {
    if (-not (Test-Path $chocoExe)) {
        Write-Host 'Installing Chocolatey ...'
        Set-ExecutionPolicy Bypass -Scope Process -Force
        Invoke-Expression (Invoke-WebRequest `
            https://community.chocolatey.org/install.ps1 -UseBasicParsing).Content
    }
    $dir = Split-Path $chocoExe
    if (-not ($env:Path -split ';' | Where-Object { $_ -eq $dir })) {
        $env:Path += ';' + $dir
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

# 1) Folder
if (-not (Test-Path $targetDir)) {
    New-Item -ItemType Directory -Path $targetDir -Force | Out-Null
    Write-Host "Created $targetDir"
}

# 2) Tools
Ensure-7Zip
Ensure-Nssm

# 3) Download
Write-Host "Downloading: $DOWNLOAD_URL"
Start-BitsTransfer -Source $DOWNLOAD_URL -Destination $zipPath

# 4) Extract
Write-Host 'Extracting ...'
& $sevenZipExe x $zipPath "-o$targetDir" -y

# 5) NSSM service
if (Get-Service -Name $serviceName -ErrorAction SilentlyContinue) {
    Write-Host 'Service exists – updating'
    nssm set $serviceName Application    $exePath
    nssm set $serviceName AppDirectory   $targetDir
    nssm set $serviceName AppParameters  $appArgs
} else {
    Write-Host 'Creating service'
    nssm install $serviceName $exePath
    nssm set     $serviceName AppDirectory  $targetDir
    nssm set     $serviceName AppParameters $appArgs
}

# 6) Restart
Write-Host ('Restarting {0} ...' -f $serviceName)
Restart-Service -Name $serviceName -Force

Write-Host 'Deploy complete.'
