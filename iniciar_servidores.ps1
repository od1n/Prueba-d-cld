#Requires -Version 5.1
<#
.SYNOPSIS
    Arranca el digitalizador Python (:8080) y la API/Portal C# (:5050).
    Cerrá la ventana o presioná Ctrl+C para apagar todo limpiamente.
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
    Write-Err "Python no encontrado. Instalá Python 3.10+ desde https://python.org"
    Read-Host "  Presioná Enter para salir"
    exit 1
}
Write-Ok "Python: $(python --version 2>&1)"

if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) {
    Write-Err ".NET SDK no encontrado. Instalá .NET 8 desde https://dot.net"
    Read-Host "  Presioná Enter para salir"
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
        Write-Err "Falló pip install. Revisá los errores de arriba."
        Read-Host "  Presioná Enter para salir"
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
    -PassThru
Write-Ok "Digitalizador iniciado (PID $($procDig.Id))"

Start-Sleep -Seconds 3

# ── Iniciar API C# ────────────────────────────────────────────────────────
Write-Step "4/4" "Compilando y arrancando portal/API (puertos 5050 y 5443)..."
$apiPath = Join-Path $BASE "SistemaGestion.Api"
$env:SG_HTTPS_REDIRECT = "false"
$procApi = Start-Process -FilePath "dotnet" `
    -ArgumentList "run" `
    -WorkingDirectory $apiPath `
    -WindowStyle Minimized `
    -PassThru `
    -Environment @{ SG_HTTPS_REDIRECT = "false" }
Write-Ok "API iniciada (PID $($procApi.Id)) — compilando, espera ~15 seg..."

Start-Sleep -Seconds 15

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
Write-Host "  ║  Cerrá esta ventana o presioná Ctrl+C para apagar.  ║" -ForegroundColor Cyan
Write-Host "  ╚══════════════════════════════════════════════════════╝" -ForegroundColor Cyan
Write-Host ""

# Abrir navegador
Start-Process "http://localhost:5050"

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
