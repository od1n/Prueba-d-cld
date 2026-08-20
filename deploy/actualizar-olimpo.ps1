# ============================================================
#  actualizar-olimpo.ps1
#  Actualiza TODO en OLIMPO de forma consistente:
#   1) git pull  (trae los cambios del repo)
#   2) cierra la API, el digitalizador y la app de escritorio
#   3) republica la API a la UNICA ubicacion canonica:
#        %LOCALAPPDATA%\SistemaGestion\publish\Api
#   4) republica la APP DE ESCRITORIO (WinForms) a:
#        %LOCALAPPDATA%\SistemaGestion\publish\App
#   5) relanza los servicios (iniciar-servicios-olimpo.cmd)
#
#  Ejecutar en OLIMPO (PowerShell). NO deja ninguna API en primer plano.
#      & 'C:\SistemaGestion\deploy\actualizar-olimpo.ps1'
# ============================================================
$ErrorActionPreference = "Stop"
function Info($m) { Write-Host "  [..] $m" -ForegroundColor Cyan }
function Ok($m)   { Write-Host "  [OK] $m" -ForegroundColor Green }
function Warn($m) { Write-Host "  [!] $m" -ForegroundColor Yellow }
function Fail($m) { Write-Host "  [ERROR] $m" -ForegroundColor Red; exit 1 }

$repo     = "C:\SistemaGestion"
$apiProj  = Join-Path $repo "SistemaGestion.Api"
$apiPub   = Join-Path $env:LOCALAPPDATA "SistemaGestion\publish\Api"
$appProj  = Join-Path $repo "SistemaGestion.csproj"        # WinForms (raiz)
# OJO: en OLIMPO el acceso directo de la app corre desde C:\SistemaGestion\publish\App
# (NO desde AppData, a diferencia de la API). Publicamos donde REALMENTE se ejecuta.
$appPub   = Join-Path $repo "publish\App"
$lanzador = Join-Path $repo "Scripts\iniciar-servicios-olimpo.cmd"

Write-Host ""
Write-Host "  === Actualizar OLIMPO (API + app de escritorio) ===" -ForegroundColor Cyan
Write-Host "  API: $apiPub" -ForegroundColor DarkGray
Write-Host "  App: $appPub" -ForegroundColor DarkGray
Write-Host ""

# 1. git pull
Info "1/5  Trayendo cambios del repositorio (git pull)..."
Set-Location $repo
Remove-Item (Join-Path $repo ".git\index.lock") -Force -ErrorAction SilentlyContinue
git pull
if ($LASTEXITCODE -ne 0) { Fail "Fallo git pull." }

# 2. Cerrar API, digitalizador y app de escritorio (para no dejar archivos bloqueados)
Info "2/5  Cerrando API, digitalizador y app de escritorio..."
Get-Process -Name "SistemaGestion.Api" -ErrorAction SilentlyContinue |
    Stop-Process -Force -ErrorAction SilentlyContinue
Get-CimInstance Win32_Process -Filter "Name='dotnet.exe'" -ErrorAction SilentlyContinue |
    Where-Object { $_.CommandLine -like "*SistemaGestion.Api.dll*" } |
    ForEach-Object { Stop-Process -Id $_.ProcessId -Force -ErrorAction SilentlyContinue }
Get-CimInstance Win32_Process -Filter "Name='python.exe'" -ErrorAction SilentlyContinue |
    Where-Object { $_.CommandLine -like "*digitalizador*" } |
    ForEach-Object { Stop-Process -Id $_.ProcessId -Force -ErrorAction SilentlyContinue }
Get-Process -Name "SistemaGestion" -ErrorAction SilentlyContinue |
    Stop-Process -Force -ErrorAction SilentlyContinue    # app de escritorio (WinForms)
Start-Sleep -Seconds 2
Ok "Procesos detenidos."

# 3. Republicar API (Sdk.Web)
Info "3/5  Publicando API a $apiPub ..."
New-Item -ItemType Directory -Force -Path $apiPub | Out-Null
& dotnet publish $apiProj -c Release -r win-x64 --self-contained false -o $apiPub --nologo -v minimal
if ($LASTEXITCODE -ne 0) { Fail "Fallo la publicacion de la API." }
Ok "API publicada: $apiPub"

# 4. Republicar la app de escritorio (WinForms) — best-effort, no bloquea la API
Info "4/5  Publicando la app de escritorio (WinForms) a $appPub ..."
try {
    New-Item -ItemType Directory -Force -Path $appPub | Out-Null
    & dotnet publish $appProj -c Release -r win-x64 --self-contained false -o $appPub --nologo -v minimal
    if ($LASTEXITCODE -eq 0) { Ok "App de escritorio publicada: $appPub" }
    else { Warn "La app de escritorio no compilo; la API SI quedo actualizada. Revisa el error arriba." }
}
catch { Warn "No se pudo publicar la app de escritorio: $($_.Exception.Message)" }

# 5. Relanzar servicios
Info "5/5  Relanzando servicios con iniciar-servicios-olimpo.cmd ..."
if (-not (Test-Path $lanzador)) { Fail "No encontre el lanzador: $lanzador" }
& cmd /c "`"$lanzador`""

Write-Host ""
Ok "Actualizacion completa (API + app de escritorio)."
Write-Host "  Portal: https://localhost:5443/pages/contenido.html  (Ctrl+Shift+R para recargar)" -ForegroundColor Cyan
Write-Host "  App de escritorio: abre el acceso directo 'SistemaGestion App'." -ForegroundColor DarkGray
