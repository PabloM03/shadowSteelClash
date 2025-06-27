param([string]$DOWNLOAD_URL)

$target  = 'C:\GameServer'
$zipPath = "$target\build.zip"

# Crear carpeta destino si no existe
if (-not (Test-Path $target)) {
    New-Item -ItemType Directory -Path $target -Force | Out-Null
    Write-Host "Created $target"
}

Write-Host "Downloading: $DOWNLOAD_URL"
Start-BitsTransfer -Source $DOWNLOAD_URL -Destination $zipPath

Write-Host "Extracting..."
& 'C:\Program Files\7-Zip\7z.exe' x $zipPath "-o$target" -y

Write-Host "Restarting service"
Restart-Service -Name 'MyGameServerService' -Force

Write-Host "Deploy complete."
