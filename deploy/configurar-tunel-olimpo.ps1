# ============================================================
#  configurar-tunel-olimpo.ps1   (ejecutar UNA vez, como Administrador)
#
#  Deja el tunel Cloudflare (servicio Windows "cloudflared") levantandose
#  solo y recuperandose de caidas:
#   1) Inicio automatico (retrasado, para que la red ya este lista)
#   2) Recuperacion ante fallos: reiniciar a los 10 s, 30 s y 60 s, sin
#      limite de veces (el contador se reinicia cada 24 h)
#   3) Permiso a los usuarios con sesion en el equipo (Interactive Users)
#      para iniciar/detener el servicio SIN ser Administrador. Lo usan
#      iniciar-servicios-olimpo.cmd y el boton "Tunel" de la app.
#   4) Tarea programada "SistemaGestion - Vigilante del tunel" (cuenta
#      SYSTEM): al arrancar Windows y cada 5 minutos ejecuta
#      deploy\vigilante-tunel.ps1 (rehabilita e inicia el servicio si hace falta)
#   5) Inicia el servicio ahora y muestra el estado
#
#  Ejecutar en OLIMPO (PowerShell 7 como Administrador):
#      & 'C:\SistemaGestion\deploy\configurar-tunel-olimpo.ps1'
# ============================================================
$ErrorActionPreference = "Stop"
function Info($m) { Write-Host "  [..] $m" -ForegroundColor Cyan }
function Ok($m)   { Write-Host "  [OK] $m" -ForegroundColor Green }
function Warn($m) { Write-Host "  [!]  $m" -ForegroundColor Yellow }
function Fail($m) { Write-Host "  [ERROR] $m" -ForegroundColor Red; exit 1 }

$servicio  = "cloudflared"
$vigilante = "C:\SistemaGestion\deploy\vigilante-tunel.ps1"
$tarea     = "SistemaGestion - Vigilante del tunel"

Write-Host ""
Write-Host "  === Configurar tunel Cloudflare (servicio $servicio) ===" -ForegroundColor Cyan
Write-Host ""

$esAdmin = ([Security.Principal.WindowsPrincipal][Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
if (-not $esAdmin) { Fail "Esta ventana no es de Administrador. Abre PowerShell 7 como Administrador." }
if (-not (Get-Service -Name $servicio -ErrorAction SilentlyContinue)) { Fail "No existe el servicio '$servicio' en este equipo." }
if (-not (Test-Path $vigilante)) { Fail "No existe $vigilante (haz git pull en C:\SistemaGestion)." }

# 1. Inicio automatico retrasado
Info "1/5  Inicio automatico (retrasado)..."
& sc.exe config $servicio start= delayed-auto | Out-Null
if ($LASTEXITCODE -ne 0) { Fail "sc config fallo (codigo $LASTEXITCODE)." }
Ok "Inicio: automatico (retrasado)."

# 2. Recuperacion ante fallos
Info "2/5  Recuperacion ante fallos..."
& sc.exe failure $servicio reset= 86400 actions= restart/10000/restart/30000/restart/60000 | Out-Null
& sc.exe failureflag $servicio 1 | Out-Null
Ok "Si se cae: reinicia a los 10 s, 30 s y 60 s."

# 3. Permiso de iniciar/detener para usuarios con sesion (IU)
Info "3/5  Permiso para iniciar el servicio sin ser Administrador..."
$sddl = ((& sc.exe sdshow $servicio) | Where-Object { $_ -match '^D:' } | Select-Object -First 1)
if (-not $sddl) { Fail "No se pudo leer el descriptor de seguridad del servicio." }
$sddl = $sddl.Trim()
$ace  = "(A;;RPWPLCRC;;;IU)"
if ($sddl -like "*$ace*") {
    Ok "El permiso ya estaba aplicado."
} else {
    $idxS = $sddl.IndexOf("S:")
    $nuevo = if ($idxS -ge 0) { $sddl.Substring(0, $idxS) + $ace + $sddl.Substring($idxS) } else { $sddl + $ace }
    & sc.exe sdset $servicio $nuevo | Out-Null
    if ($LASTEXITCODE -ne 0) { Fail "sc sdset fallo (codigo $LASTEXITCODE). Descriptor original: $sddl" }
    Ok "Usuarios con sesion en el equipo pueden iniciar/detener $servicio."
}

# 4. Tarea programada vigilante (SYSTEM, al arrancar y cada 5 minutos)
Info "4/5  Tarea programada '$tarea'..."
$accion = New-ScheduledTaskAction -Execute "powershell.exe" `
    -Argument "-NoProfile -NonInteractive -ExecutionPolicy Bypass -WindowStyle Hidden -File `"$vigilante`""
$tArranque = New-ScheduledTaskTrigger -AtStartup
$tArranque.Delay = "PT1M"
$tCiclo = New-ScheduledTaskTrigger -Once -At (Get-Date).AddMinutes(1) `
    -RepetitionInterval (New-TimeSpan -Minutes 5)
$principal = New-ScheduledTaskPrincipal -UserId "SYSTEM" -LogonType ServiceAccount -RunLevel Highest
$ajustes = New-ScheduledTaskSettingsSet -StartWhenAvailable -AllowStartIfOnBatteries `
    -DontStopIfGoingOnBatteries -ExecutionTimeLimit (New-TimeSpan -Minutes 2) -MultipleInstances IgnoreNew
Register-ScheduledTask -TaskName $tarea -Action $accion -Trigger @($tArranque, $tCiclo) `
    -Principal $principal -Settings $ajustes -Force | Out-Null
Ok "Tarea creada: al arrancar Windows (1 min despues) y cada 5 minutos."

# 5. Iniciar ahora
Info "5/5  Iniciando el servicio..."
$svc = Get-Service -Name $servicio
if ($svc.Status -ne 'Running') {
    Start-Service -Name $servicio
    Start-Sleep -Seconds 8
    $svc.Refresh()
}
if ($svc.Status -eq 'Running') { Ok "cloudflared en ejecucion." } else { Warn "cloudflared quedo en estado '$($svc.Status)'." }

Write-Host ""
Get-Service $servicio | Format-List Name, Status, StartType
Write-Host "  Registro del vigilante: C:\SistemaGestion\logs\vigilante-tunel.log" -ForegroundColor DarkGray
Write-Host "  Siguiente paso: & 'C:\SistemaGestion\deploy\verificar-tunel.ps1'" -ForegroundColor Cyan
Write-Host ""
