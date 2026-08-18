# ============================================================
#  actualizar-olimpo.ps1
#  Actualiza el sistema en OLIMPO de forma consistente:
#   1) git pull  (trae los cambios del repo)
#   2) cierra la API y el digitalizador en ejecucion
#   3) republica la API a la UNICA ubicacion canonica:
#        %LOCALAPPDATA%\SistemaGestion\publish\Api
#      (la misma que arranca el enlace directo / el arranque del PC)
#   4) relanza con el lanzador de siempre (iniciar-servicios-olimpo.cmd)
#
#  Ejecutar en OLIMPO (PowerShell). NO deja ninguna API en primer plano.
#      & 'C:\SistemaGestion\deploy\actualizar-olimpo.ps1'
# ============================================================
$ErrorActionPreference = "Stop"
function Info($m) { Write-Host "  [..] $m" -ForegroundColor Cyan }
function Ok($m)   { Write-Host "  [OK] $m" -ForegroundColor Green }
function Fail($m) { Write-Host "  [ERROR] $m" -ForegroundColor Red; exit 1 }

$repo     = "C:\SistemaGestion"
$apiProj  = Join-Path $repo "SistemaGestion.Api"
$apiPub   = Join-Path $env:LOCALAPPDATA "SistemaGestion\publish\Api"
$lanzador = Join-Path $repo "Scripts\iniciar-servicios-olimpo.cmd"

Write-Host ""
Write-Host "  === Actualizar OLIMPO ===" -ForegroundColor Cyan
Write-Host "  Publicacion canonica: $apiPub" -ForegroundColor DarkGray
Write-Host ""

# 1. git pull
Info "1/4  Trayendo cambios del repositorio (git pull)..."
Set-Location $repo
Remove-Item (Join-Path $repo ".git\index.lock") -Force -ErrorAction SilentlyContinue
git pull
if ($LASTEXITCODE -ne 0) { Fail "Fallo git pull." }

# 2. Cerrar API y digitalizador en ejecucion
Info "2/4  Cerrando API y digitalizador en ejecucion..."
Get-Process -Name "SistemaGestion.Api" -ErrorAction SilentlyContinue |
    Stop-Process -Force -ErrorAction SilentlyContinue
Get-CimInstance Win32_Process -Filter "Name='dotnet.exe'" -ErrorAction SilentlyContinue |
    Where-Object { $_.CommandLine -like "*SistemaGestion.Api.dll*" } |
    ForEach-Object { Stop-Process -Id $_.ProcessId -Force -ErrorAction SilentlyContinue }
Get-CimInstance Win32_Process -Filter "Name='python.exe'" -ErrorAction SilentlyContinue |
    Where-Object { $_.CommandLine -like "*digitalizador*" } |
    ForEach-Object { Stop-Process -Id $_.ProcessId -Force -ErrorAction SilentlyContinue }
Start-Sleep -Seconds 2
Ok "Servicios detenidos."

# 3. Republicar API a la ubicacion canonica (AppData)
Info "3/4  Publicando API a $apiPub ..."
New-Item -ItemType Directory -Force -Path $apiPub | Out-Null
& dotnet publish $apiProj -c Release -r win-x64 --self-contained false -o $apiPub --nologo -v minimal
if ($LASTEXITCODE -ne 0) { Fail "Fallo la publicacion de la API." }
Ok "API publicada (unica ubicacion): $apiPub"

# 4. Relanzar con el lanzador de siempre
Info "4/4  Relanzando servicios con iniciar-servicios-olimpo.cmd ..."
if (-not (Test-Path $lanzador)) { Fail "No encontre el lanzador: $lanzador" }
& cmd /c "`"$lanzador`""

Write-Host ""
Ok "Actualizacion completa. La UNICA API corre desde: $apiPub"
Write-Host "  Abre el portal en: https://localhost:5443/pages/contenido.html" -ForegroundColor Cyan
Write-Host "  (haz Ctrl+Shift+R para recargar el portal en el navegador)" -ForegroundColor DarkGray
