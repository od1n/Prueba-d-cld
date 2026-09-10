# ============================================================
#  actualizar-servo.ps1
#  Actualiza TODO en SERVO de forma consistente (igual que OLIMPO):
#   1) git pull
#   2) cierra la API y la app de escritorio en ejecucion
#   3) republica la API a la UNICA ubicacion canonica de SERVO:
#        C:\SistemaGestion\publish\Api
#   4) relanza con el lanzador de siempre (Scripts\iniciar-servicios-servo.cmd)
#
#  SERVO es la maquina de oficina: la BD y el digitalizador estan en OLIMPO
#  (por red). SERVO NO corre la app de escritorio (WinForms): solo la API web,
#  por eso este script publica UNICAMENTE la API. Este script NO deja ninguna
#  API en primer plano.
#
#  Ejecutar en SERVO (PowerShell):
#      & 'C:\SistemaGestion\deploy\actualizar-servo.ps1'
# ============================================================
$ErrorActionPreference = "Stop"
function Info($m) { Write-Host "  [..] $m" -ForegroundColor Cyan }
function Ok($m)   { Write-Host "  [OK] $m" -ForegroundColor Green }
function Warn($m) { Write-Host "  [!] $m" -ForegroundColor Yellow }
function Fail($m) { Write-Host "  [ERROR] $m" -ForegroundColor Red; exit 1 }

$repo     = "C:\SistemaGestion"
$apiProj  = Join-Path $repo "SistemaGestion.Api"
$apiPub   = Join-Path $repo "publish\Api"
$lanzador = Join-Path $repo "Scripts\iniciar-servicios-servo.cmd"

Write-Host ""
Write-Host "  === Actualizar SERVO (solo API web) ===" -ForegroundColor Cyan
Write-Host "  API: $apiPub" -ForegroundColor DarkGray
Write-Host ""

# 1. git pull
Info "1/4  Trayendo cambios del repositorio (git pull)..."
Set-Location $repo
Remove-Item (Join-Path $repo ".git\index.lock") -Force -ErrorAction SilentlyContinue
git pull
if ($LASTEXITCODE -ne 0) { Fail "Fallo git pull." }

# 2. Cerrar la API en ejecucion (para no dejar archivos bloqueados)
Info "2/4  Cerrando la API..."
Get-Process -Name "SistemaGestion.Api" -ErrorAction SilentlyContinue |
    Stop-Process -Force -ErrorAction SilentlyContinue
Get-CimInstance Win32_Process -Filter "Name='dotnet.exe'" -ErrorAction SilentlyContinue |
    Where-Object { $_.CommandLine -like "*SistemaGestion.Api.dll*" } |
    ForEach-Object { Stop-Process -Id $_.ProcessId -Force -ErrorAction SilentlyContinue }
Get-Process -Name "SistemaGestion" -ErrorAction SilentlyContinue |
    Stop-Process -Force -ErrorAction SilentlyContinue    # app de escritorio (WinForms)
Start-Sleep -Seconds 2
Ok "Procesos detenidos."

# 2b. Refuerzo: matar cualquier proceso que AUN tenga tomado el DLL de la API y esperar a que se libere
$apiDll = Join-Path $apiPub "SistemaGestion.Api.dll"
function Stop-ApiHolders {
    Get-CimInstance Win32_Process -ErrorAction SilentlyContinue |
        Where-Object { $_.ExecutablePath -like (Join-Path $apiPub '*') -or $_.CommandLine -like '*SistemaGestion.Api*' } |
        ForEach-Object { Stop-Process -Id $_.ProcessId -Force -ErrorAction SilentlyContinue }
}
function Test-Locked($p) {
    if (-not (Test-Path $p)) { return $false }
    try { $fs = [System.IO.File]::Open($p,'Open','ReadWrite','None'); $fs.Close(); return $false }
    catch { return $true }
}
Stop-ApiHolders
$intentos = 0
while ((Test-Locked $apiDll) -and $intentos -lt 15) { Start-Sleep -Seconds 1; Stop-ApiHolders; $intentos++ }
if (Test-Locked $apiDll) {
    Fail "La API sigue usando '$apiDll' y no se pudo detener. Cierra la API manualmente o ejecuta este script en una PowerShell COMO ADMINISTRADOR, y vuelve a intentar."
}
Ok "DLL de la API libre; listo para publicar."

# 3. Republicar API a la UNICA ubicacion canonica
Info "3/4  Publicando API a $apiPub ..."
New-Item -ItemType Directory -Force -Path $apiPub | Out-Null
& dotnet publish $apiProj -c Release -r win-x64 --self-contained false -o $apiPub --nologo -v minimal
if ($LASTEXITCODE -ne 0) {
    Warn "Fallo la publicacion (posible red/NuGet). Reintentando SIN restaurar, con la cache local de paquetes..."
    & dotnet publish $apiProj -c Release -r win-x64 --self-contained false --no-restore -o $apiPub --nologo -v minimal
    if ($LASTEXITCODE -ne 0) { Fail "Fallo la publicacion de la API (tambien sin conexion). Revisa la conexion a internet / NuGet." }
    Ok "API publicada desde cache (sin conexion): $apiPub"
} else {
    Ok "API publicada: $apiPub"
}

# 4. Relanzar con el lanzador (no bloquea: usa Start-Process)
Info "4/4  Relanzando la API con iniciar-servicios-servo.cmd ..."
if (-not (Test-Path $lanzador)) { Fail "No encontre el lanzador: $lanzador" }
Start-Process -FilePath "cmd.exe" -ArgumentList '/c', "`"$lanzador`""

Write-Host ""
Ok "Actualizacion completa (solo API web)."
Write-Host "  Portal: http://localhost:5050/pages/contenido.html  (Ctrl+Shift+R para recargar)" -ForegroundColor Cyan
