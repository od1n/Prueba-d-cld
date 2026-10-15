# ============================================================
#  vigilante-tunel.ps1
#  Lo ejecuta la tarea programada "SistemaGestion - Vigilante del tunel"
#  (cuenta SYSTEM) al arrancar Windows y cada 5 minutos. La tarea se crea
#  con deploy\configurar-tunel-olimpo.ps1.
#
#  Si el servicio cloudflared esta deshabilitado, lo vuelve a poner en
#  inicio automatico (retrasado). Si esta detenido, lo inicia.
#  Registra cada accion en C:\SistemaGestion\logs\vigilante-tunel.log
# ============================================================
$ErrorActionPreference = "Continue"
$servicio = "cloudflared"
$logDir   = "C:\SistemaGestion\logs"
$log      = Join-Path $logDir "vigilante-tunel.log"

function Log($m) {
    try {
        if (-not (Test-Path $logDir)) { New-Item -ItemType Directory -Path $logDir -Force | Out-Null }
        if ((Test-Path $log) -and (Get-Item $log).Length -gt 1MB) {
            Move-Item $log "$log.anterior" -Force
        }
        Add-Content -Path $log -Value ("{0:yyyy-MM-dd HH:mm:ss}  {1}" -f (Get-Date), $m)
    } catch { }
}

$svc = Get-Service -Name $servicio -ErrorAction SilentlyContinue
if (-not $svc) { Log "El servicio $servicio no existe en este equipo."; exit 0 }

if ($svc.StartType -eq 'Disabled') {
    & sc.exe config $servicio start= delayed-auto | Out-Null
    Log "El servicio estaba DESHABILITADO: se cambio a inicio automatico (retrasado)."
}

$svc.Refresh()
if ($svc.Status -ne 'Running') {
    Log "El servicio estaba '$($svc.Status)': iniciando..."
    try {
        Start-Service -Name $servicio -ErrorAction Stop
        Start-Sleep -Seconds 5
        $svc.Refresh()
        Log "Estado despues de iniciar: $($svc.Status)"
    } catch {
        Log "ERROR al iniciar: $($_.Exception.Message)"
    }
}
