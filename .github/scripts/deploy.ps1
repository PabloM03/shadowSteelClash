param()   # sin parámetros posicionales

$dl      = $env:DOWNLOAD_URL
$target  = 'C:\GameServer'
$tempZip = "$target\build.zip"

Remove-Item -Path "$target\*" -Recurse -Force -ErrorAction SilentlyContinue

Write-Host "⬇️  Downloading: $dl"
Start-BitsTransfer -Source $dl -Destination $tempZip

Write-Host "�  Extracting..."
& 'C:\Program Files\7-Zip\7z.exe' x $tempZip "-o$target" -y

Write-Host "�  Restarting service"
Restart-Service -Name 'MyGameServerService' -Force
Write-Host "✅  Deploy complete."
