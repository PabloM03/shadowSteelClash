param([string]$DOWNLOAD_URL)

$target  = 'C:\GameServer'
$zipPath = "$target\build.zip"
$sevenZip = 'C:\Program Files\7-Zip\7z.exe'

# Crear carpeta destino si falta
if (-not (Test-Path $target)) {
    New-Item -ItemType Directory -Path $target -Force | Out-Null
    Write-Host "Created $target"
}

# Asegurar 7-Zip
if (-not (Test-Path $sevenZip)) {
    Write-Host "7-Zip not found. Installing via Chocolatey..."
    if (-not (Get-Command choco -ErrorAction SilentlyContinue)) {
        Set-ExecutionPolicy Bypass -Scope Process -Force
        iwr https://chocolatey.org/install.ps1 -UseBasicParsing | iex
    }
    choco install 7zip -y --no-progress
}

Write-Host "Downloading: $DOWNLOAD_URL"
Start-BitsTransfer -Source $DOWNLOAD_URL -Destination $zipPath

Write-Host "Extracting..."
& $sevenZip x $zipPath "-o$target" -y

Write-Host "Restarting service"
Restart-Service -Name 'MyGameServerService' -Force

Write-Host "Deploy complete."
