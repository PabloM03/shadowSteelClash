param()

$dl = $env:DOWNLOAD_URL
$target = 'C:\GameServer'
$tempZip = "$target\build.zip"

# 1) Borrar carpeta antigua
Remove-Item -Path "$target\*" -Recurse -Force -ErrorAction SilentlyContinue

# 2) Descargar build directamente en el servidor
Write-Host "⬇️ Descargando en servidor: $dl"
Start-BitsTransfer -Source $dl -Destination $tempZip

# 3) Descomprimir con 7z
Write-Host "� Descomprimiendo..."
& 'C:\Program Files\7-Zip\7z.exe' x $tempZip "-o$target" -y

# 4) Reiniciar servicio
Write-Host "� Reiniciando servicio"
Restart-Service -Name 'MyGameServerService' -Force

Write-Host "✅ Despliegue completado."
