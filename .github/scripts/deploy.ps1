<#  
    Despliega un build Unity headless (zip) en C:\GameServer  
    y lo ejecuta como servicio NSSM llamado WindowsServer.  
  
    · Descarga y extrae el zip.  
    · Asegura Chocolatey, 7-Zip y NSSM.  
    · Busca automáticamente windows-server.exe donde esté dentro del zip.  
    · Crea o actualiza el servicio WindowsServer (LocalSystem).  
    · Redirige stdout/err a service.log y genera server.log.  
    · Reinicia el servicio y muestra cualquier error.  
  
    Se invoca desde el workflow:  
        .\deploy.ps1 <URL_ZIP>  
  
    Requisitos:  
        - El ZIP debe contener windows-server.exe + carpeta *_Data.  
        - El build debe haberse generado con “Windows Dedicated/Headless Server”.  
#>  
  
param(  
    [Parameter(Mandatory = $true)]  
    [string]$DOWNLOAD_URL  
)  
  
# ---------- CONFIG -------------------------------------------------------  
$targetDir   = 'C:\GameServer'  
$zipPath     = Join-Path $targetDir 'build.zip'  
  
$serviceName = 'WindowsServer'  
  
$sevenZipExe = 'C:\Program Files\7-Zip\7z.exe'  
$chocoExe    = 'C:\ProgramData\chocolatey\bin\choco.exe'  
# ------------------------------------------------------------------------  
  
$ErrorActionPreference = 'Stop'  
$VerbosePreference     = 'Continue'  
  
# ---------------- helpers ------------------------------------------------  
function Ensure-Choco {  
    if (-not (Test-Path $chocoExe)) {  
        Write-Host 'Installing Chocolatey...'  
        Set-ExecutionPolicy Bypass -Scope Process -Force  
        Invoke-Expression (Invoke-WebRequest `  
            https://community.chocolatey.org/install.ps1 -UseBasicParsing).Content  
    }  
    $dir = Split-Path $chocoExe  
    if (-not ($env:Path -split ';' | Where-Object { $_ -eq $dir })) {  
        $env:Path += ";$dir"  
    }  
}  
  
function Ensure-Pkg([string]$pkg) {  
    Ensure-Choco  
    if (-not (choco list --local-only | Select-String "^$pkg")) {  
        & $chocoExe install $pkg -y --no-progress  
    }  
}  
  
Ensure-Pkg 7zip  
Ensure-Pkg nssm  
# ------------------------------------------------------------------------  
  
# 1) Crear carpeta destino  
if (-not (Test-Path $targetDir)) {  
    New-Item -ItemType Directory -Path $targetDir -Force | Out-Null  
    Write-Host "Created $targetDir"  
}  
  
# 2) Descargar build  
Write-Host "Downloading: $DOWNLOAD_URL"  
Start-BitsTransfer -Source $DOWNLOAD_URL -Destination $zipPath  
  
# 3) Extraer  
Write-Host 'Extracting...'  
& $sevenZipExe x $zipPath "-o$targetDir" -y  
  
# 3b) Localizar el .exe correcto  
$exePath = Get-ChildItem -Path $targetDir -Filter 'windows-server.exe' -Recurse |  
           Sort-Object FullName | Select-Object -First 1 |  
           ForEach-Object { $_.FullName }  
  
if (-not $exePath) { throw "No se encontró windows-server.exe tras la extracción." }  
  
$exeDir   = Split-Path $exePath  
$appArgs  = "-batchmode -nographics -logfile `"$exeDir\server.log`""  
$nssmLog  = Join-Path $exeDir 'service.log'  
  
Write-Host "Executable found at: $exePath"  
  
# 4) Crear / actualizar servicio NSSM  
if (Get-Service -Name $serviceName -ErrorAction SilentlyContinue) {  
    Write-Host 'Service exists – updating'  
    nssm set $serviceName Application  $exePath  
} else {  
    Write-Host 'Creating service'  
    nssm install $serviceName $exePath  
}  
  
# Ajustes comunes (siempre)  
nssm set $serviceName AppDirectory    $exeDir  
nssm set $serviceName AppParameters   $appArgs  
nssm set $serviceName AppStdout       $nssmLog  
nssm set $serviceName AppStderr       $nssmLog  
nssm set $serviceName AppRotateFiles  5             # conserva 5 logs  
nssm set $serviceName AppExit         Default Restart  
nssm set $serviceName AppRestartDelay 5000  
nssm set $serviceName AppThrottle     10000  
nssm set $serviceName AppNoConsole    0            # consola disponible  
nssm set $serviceName ObjectName      'LocalSystem'  
  
# 5) Reiniciar servicio con manejo de errores  
Write-Host "Restarting $serviceName..."  
try {  
    Restart-Service -Name $serviceName -Force -ErrorAction Stop  
    Write-Host 'Deploy complete ✅'  
} catch {  
    Write-Warning "Error al iniciar el servicio: $($_.Exception.Message)"  
    Write-Warning 'Revisa service.log y los eventos del SCM para más detalles.'  
    throw  
}  
