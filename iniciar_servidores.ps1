#Requires -Version 5.1
<#
.SYNOPSIS
    Arranca el digitalizador Python (:8080) y la API/Portal C# (:5050).
    Cierra la ventana o presiona Ctrl+C para apagar todo limpiamente.
#>

$Host.UI.RawUI.WindowTitle = "Sistema de Gestión"
$BASE = Split-Path -Parent $MyInvocation.MyCommand.Path

# ── Paleta de colores ────────────────────────────────────────────────────
function Write-Header {
    Write-Host ""
    Write-Host "  ╔══════════════════════════════════════════╗" -ForegroundColor Cyan
    Write-Host "  ║       SISTEMA DE GESTIÓN — ARRANQUE      ║" -ForegroundColor Cyan
    Write-Host "  ╚══════════════════════════════════════════╝" -ForegroundColor Cyan
    Write-Host ""
}
function Write-Ok  ($msg) { Write-Host "  [✓] $msg" -ForegroundColor Green  }
function Write-Err ($msg) { Write-Host "  [✗] $msg" -ForegroundColor Red    }
function Write-Info($msg) { Write-Host "  [·] $msg" -ForegroundColor Yellow }
function Write-Step($n,$msg) { Write-Host "  [$n] $msg" -ForegroundColor White }

# ── Verificaciones previas ────────────────────────────────────────────────
Write-Header

Write-Step "1/4" "Verificando dependencias..."

if (-not (Get-Command python -ErrorAction SilentlyContinue)) {
    Write-Err "Python no encontrado. Instala Python 3.10+ desde https://python.org"
    Read-Host "  Presiona Enter para salir"
    exit 1
}
Write-Ok "Python: $(python --version 2>&1)"

if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) {
    Write-Err ".NET SDK no encontrado. Instala .NET 8 desde https://dot.net"
    Read-Host "  Presiona Enter para salir"
    exit 1
}
Write-Ok ".NET:   $(dotnet --version)"

# ── Instalar deps Python si hace falta ───────────────────────────────────
Write-Step "2/4" "Verificando dependencias Python..."
$reqFile = Join-Path $BASE "digitalizador\requirements.txt"
$checkPkg = pip show fastapi 2>&1
if ($LASTEXITCODE -ne 0) {
    Write-Info "Instalando paquetes (primera vez, puede tardar un momento)..."
    pip install -r $reqFile -q
    if ($LASTEXITCODE -ne 0) {
        Write-Err "Falló pip install. Revisa los errores de arriba."
        Read-Host "  Presiona Enter para salir"
        exit 1
    }
}
Write-Ok "Dependencias Python listas."

# ── Iniciar digitalizador ─────────────────────────────────────────────────
Write-Step "3/4" "Iniciando digitalizador IA (puerto 8080)..."
$digPath = Join-Path $BASE "digitalizador"
$procDig = Start-Process -FilePath "python" `
    -ArgumentList "main.py" `
    -WorkingDirectory $digPath `
    -WindowStyle Minimized `
    -PassThru `
    -Environment @{ SG_DB_PATH = $env:SG_DB_PATH; SG_INTERNAL_TOKEN = $env:SG_INTERNAL_TOKEN }
Write-Ok "Digitalizador iniciado (PID $($procDig.Id))"

Start-Sleep -Seconds 3

# ── Publicar y arrancar API C# ────────────────────────────────────────────
Write-Step "4/4" "Publicando y arrancando portal/API (puertos 5050 y 5443)..."
$apiProj = Join-Path $BASE "SistemaGestion.Api"
$apiPublish = Join-Path $BASE "publish\api"
$env:SG_HTTPS_REDIRECT = "false"

# Publicar siempre para que el binario y los archivos estáticos estén al día
$publishResult = & dotnet publish $apiProj -c Release -o $apiPublish --nologo -v q 2>&1
if ($LASTEXITCODE -ne 0) {
    Write-Err "Falló dotnet publish:"
    Write-Host $publishResult -ForegroundColor Red
    Read-Host "  Presiona Enter para salir"
    exit 1
}
Write-Ok "API publicada en $apiPublish"

# Ejecutar desde el directorio de publish (no dotnet run)
$procApi = Start-Process -FilePath "dotnet" `
    -ArgumentList "SistemaGestion.Api.dll" `
    -WorkingDirectory $apiPublish `
    -WindowStyle Minimized `
    -PassThru `
    -Environment @{ SG_HTTPS_REDIRECT = "false"; SG_DB_PATH = $env:SG_DB_PATH; SG_INTERNAL_TOKEN = $env:SG_INTERNAL_TOKEN; SG_CAM_KEY = $env:SG_CAM_KEY }
Write-Ok "API iniciada (PID $($procApi.Id))"

Start-Sleep -Seconds 5

# ── Detectar IP local ─────────────────────────────────────────────────────
$ipLocal = (
    Get-NetIPAddress -AddressFamily IPv4 |
    Where-Object { $_.IPAddress -notlike "127.*" -and $_.PrefixOrigin -ne "WellKnown" } |
    Select-Object -First 1
).IPAddress

# ── Mostrar URLs ──────────────────────────────────────────────────────────
Write-Host ""
Write-Host "  ╔══════════════════════════════════════════════════════╗" -ForegroundColor Cyan
Write-Host "  ║              SISTEMA LISTO                           ║" -ForegroundColor Cyan
Write-Host "  ╠══════════════════════════════════════════════════════╣" -ForegroundColor Cyan
Write-Host "  ║  PC (navegador)  →  " -ForegroundColor Cyan -NoNewline
Write-Host "http://localhost:5050     " -ForegroundColor Green -NoNewline
Write-Host "   ║" -ForegroundColor Cyan
if ($ipLocal) {
    $url = "http://$($ipLocal):5050"
    $pad = " " * [Math]::Max(0, 28 - $url.Length)
    Write-Host "  ║  Celular/tablet  →  " -ForegroundColor Cyan -NoNewline
    Write-Host "$url$pad" -ForegroundColor Green -NoNewline
    Write-Host "   ║" -ForegroundColor Cyan
}
Write-Host "  ╠══════════════════════════════════════════════════════╣" -ForegroundColor Cyan
Write-Host "  ║  Cierra esta ventana o presiona Ctrl+C para apagar.  ║" -ForegroundColor Cyan
Write-Host "  ╚══════════════════════════════════════════════════════╝" -ForegroundColor Cyan
Write-Host ""

# Abrir navegador (solo si hay uno asociado; en servidores sin navegador se omite sin error)
try { Start-Process "http://localhost:5050" -ErrorAction Stop }
catch { Write-Host "  [.] Sin navegador en este equipo. Abre el portal manualmente: http://localhost:5050" -ForegroundColor DarkGray }

# ── Esperar Ctrl+C o cierre de ventana ───────────────────────────────────
try {
    # Monitorear que ambos procesos sigan vivos
    while ($true) {
        Start-Sleep -Seconds 5
        if ($procApi.HasExited -and -not $procApi.HasExited) {
            Write-Err "La API se cerró inesperadamente."
            break
        }
        if ($procDig.HasExited) {
            Write-Info "El digitalizador se cerró. Puede reiniciarse desde el portal."
        }
    }
}
finally {
    # ── Apagado limpio ────────────────────────────────────────────────────
    Write-Host ""
    Write-Info "Apagando servidores..."

    if (-not $procApi.HasExited) {
        Stop-Process -Id $procApi.Id -Force -ErrorAction SilentlyContinue
        Write-Ok "Portal/API detenido."
    }
    if (-not $procDig.HasExited) {
        Stop-Process -Id $procDig.Id -Force -ErrorAction SilentlyContinue
        Write-Ok "Digitalizador detenido."
    }

    # Por si acaso quedó algún proceso huérfano
    Get-Process -Name "SistemaGestion.Api" -ErrorAction SilentlyContinue |
        Stop-Process -Force -ErrorAction SilentlyContinue

    Write-Host ""
    Write-Host "  Hasta luego." -ForegroundColor Cyan
    Start-Sleep -Seconds 2
}
