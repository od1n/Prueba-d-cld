# ============================================================
#  verificar-tunel.ps1
#  Verifica en OLIMPO que el tunel Cloudflare este levantado y que el ERP
#  responda por el dominio publico con el control de acceso correcto.
#
#  Revisa, en orden:
#   1) Servicio Windows "cloudflared" (si esta detenido, intenta iniciarlo;
#      para eso PowerShell debe estar abierto como Administrador)
#   2) La API escuchando en el puerto 5443 (origen del tunel)
#   3) La API respondiendo localmente: https://localhost:5443
#   4) SG_PUBLIC_HOST configurado (variable de entorno Machine/User o
#      sg-env.cmd) y cargado en el proceso de la API (activa la puerta)
#   5) El portal respondiendo por el tunel: https://api-sg.hato-pascher.com
#      (y que ya tenga la version nueva de facturas.js)
#   6) Sin sesion, el ERP por el tunel debe responder 403 (puerta activa)
#   7) OPCIONAL (-Usuario): inicia sesion por el tunel con ese usuario y
#      prueba que /api/facturas responda 200 (Admin, Ventas o Contabilidad)
#
#  Ejecutar en OLIMPO (PowerShell 7):
#      & 'C:\SistemaGestion\deploy\verificar-tunel.ps1'
#      & 'C:\SistemaGestion\deploy\verificar-tunel.ps1' -Usuario 'nombre_usuario'
# ============================================================
param(
    [string]$Usuario = ""
)
$ErrorActionPreference = "Continue"
function Info($m) { Write-Host "  [..] $m" -ForegroundColor Cyan }
function Ok($m)   { Write-Host "  [OK] $m" -ForegroundColor Green }
function Warn($m) { Write-Host "  [!]  $m" -ForegroundColor Yellow }
function Bad($m)  { Write-Host "  [X]  $m" -ForegroundColor Red; $script:fallas++ }
$script:fallas = 0

$dominio = "api-sg.hato-pascher.com"
$base    = "https://$dominio"
$sgEnv   = Join-Path $env:LOCALAPPDATA "SistemaGestion\publish\Api\sg-env.cmd"

function Codigo($url, $session = $null, [switch]$Local) {
    try {
        $p = @{ Uri = $url; Method = 'Get'; TimeoutSec = 20; SkipHttpErrorCheck = $true }
        if ($Local)   { $p.SkipCertificateCheck = $true }
        if ($session) { $p.WebSession = $session }
        $r = Invoke-WebRequest @p
        return @{ Code = [int]$r.StatusCode; Body = [string]$r.Content }
    } catch {
        return @{ Code = 0; Body = $_.Exception.Message }
    }
}

Write-Host ""
Write-Host "  === Verificar tunel Cloudflare ($dominio) ===" -ForegroundColor Cyan
$esAdmin = ([Security.Principal.WindowsPrincipal][Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
Write-Host "  Usuario: $([Security.Principal.WindowsIdentity]::GetCurrent().Name)   Administrador: $esAdmin" -ForegroundColor DarkGray
Write-Host ""

# 1. Servicio cloudflared
Info "1/7  Servicio cloudflared..."
$svc = Get-Service -Name "cloudflared" -ErrorAction SilentlyContinue
if (-not $svc) {
    Bad "No existe el servicio 'cloudflared'. Hay que reinstalarlo (ver claude/acceso_tunel / reference_cloudflare_tunnel)."
} elseif ($svc.Status -ne 'Running') {
    Warn "cloudflared esta '$($svc.Status)'. Intentando iniciarlo..."
    try {
        Start-Service -Name "cloudflared" -ErrorAction Stop
        Start-Sleep -Seconds 8
        $svc.Refresh()
        if ($svc.Status -eq 'Running') { Ok "cloudflared iniciado." }
        else { Bad "cloudflared no quedo en ejecucion (estado: $($svc.Status))." }
    } catch {
        if (-not $esAdmin) {
            Bad "No se pudo iniciar cloudflared: esta ventana NO es de Administrador. Abre PowerShell 7 como Administrador y vuelve a correr el script."
        } else {
            Bad "No se pudo iniciar cloudflared: $($_.Exception.InnerException.Message) $($_.Exception.Message)"
            Write-Host "       Ultimos eventos del servicio cloudflared:" -ForegroundColor DarkGray
            Get-WinEvent -FilterHashtable @{ LogName = 'System'; ProviderName = 'Service Control Manager'; StartTime = (Get-Date).AddDays(-7) } -ErrorAction SilentlyContinue |
                Where-Object { $_.Message -match 'cloudflared|Cloudflared' } | Select-Object -First 5 |
                ForEach-Object { Write-Host "       $($_.TimeCreated)  $($_.Message)" -ForegroundColor DarkGray }
            Get-WinEvent -FilterHashtable @{ LogName = 'Application'; ProviderName = 'cloudflared'; StartTime = (Get-Date).AddDays(-7) } -ErrorAction SilentlyContinue |
                Select-Object -First 5 |
                ForEach-Object { Write-Host "       $($_.TimeCreated)  $($_.Message)" -ForegroundColor DarkGray }
        }
    }
} else {
    Ok "cloudflared en ejecucion (inicio: $((Get-Service cloudflared).StartType))."
}

# 2. Puerto 5443
Info "2/7  API escuchando en el puerto 5443..."
$escucha = Get-NetTCPConnection -LocalPort 5443 -State Listen -ErrorAction SilentlyContinue
if ($escucha) {
    $pid5443 = ($escucha | Select-Object -First 1).OwningProcess
    $proc = Get-Process -Id $pid5443 -ErrorAction SilentlyContinue
    Ok "Puerto 5443 en escucha (proceso: $($proc.ProcessName), PID $pid5443)."
} else {
    Bad "Nadie escucha en 5443: la API no esta corriendo. Ejecuta: & 'C:\SistemaGestion\Scripts\iniciar-servicios-olimpo.cmd'"
}

# 3. API local
Info "3/7  API local (https://localhost:5443)..."
$r = Codigo "https://localhost:5443/index.html" -Local
if ($r.Code -eq 200) { Ok "API local responde 200." }
else { Bad "API local no responde bien (codigo $($r.Code)). $($r.Body)" }

# 4. SG_PUBLIC_HOST (puede venir de la variable Machine/User o de sg-env.cmd)
Info "4/7  SG_PUBLIC_HOST..."
$vMachine = [Environment]::GetEnvironmentVariable('SG_PUBLIC_HOST', 'Machine')
$vUser    = [Environment]::GetEnvironmentVariable('SG_PUBLIC_HOST', 'User')
$vArchivo = $null
if (Test-Path $sgEnv) {
    $linea = Select-String -Path $sgEnv -Pattern 'SG_PUBLIC_HOST' -SimpleMatch | Select-Object -First 1
    if ($linea -and $linea.Line -notmatch '^\s*rem') { $vArchivo = ($linea.Line -split '=', 2)[1].Trim() }
}
$origenes = @()
if ($vMachine) { $origenes += "Machine=$vMachine" }
if ($vUser)    { $origenes += "User=$vUser" }
if ($vArchivo) { $origenes += "sg-env.cmd=$vArchivo" }
$valor = @($vArchivo, $vUser, $vMachine) | Where-Object { $_ } | Select-Object -First 1
if (-not $valor) {
    Bad "SG_PUBLIC_HOST no esta definida (ni Machine, ni User, ni sg-env.cmd): la puerta del tunel NO esta activa."
} elseif ($valor -ne $dominio) {
    Warn "SG_PUBLIC_HOST tiene otro valor: $($origenes -join ' | ')"
} else {
    Ok "SG_PUBLIC_HOST = $dominio  ($($origenes -join ' | '))"
}

# 5. Portal por el tunel
Info "5/7  Portal por el tunel ($base)..."
$r = Codigo "$base/index.html"
if ($r.Code -eq 200) { Ok "El tunel responde 200." }
elseif ($r.Code -eq 530) { Bad "Cloudflare responde 530: el tunel no llega a OLIMPO (cloudflared caido o sin conexion)." }
elseif ($r.Code -eq 502) { Bad "Cloudflare responde 502: el tunel esta arriba pero la API (5443) no contesta." }
else { Bad "El tunel responde codigo $($r.Code)." }
$js = Codigo "$base/js/pages/facturas.js"
if ($js.Code -eq 200 -and $js.Body -match 'toggleCalcBcv') { Ok "Version nueva desplegada (calculadora BCV presente en facturas.js)." }
elseif ($js.Code -eq 200) { Warn "facturas.js responde, pero es la version vieja: falta correr actualizar-olimpo.ps1." }

# 6. Puerta del tunel sin sesion
Info "6/7  ERP por el tunel SIN sesion (debe ser 401 o 403)..."
$r = Codigo "$base/api/facturas"
if ($r.Code -eq 403 -or $r.Code -eq 401) { Ok "Sin sesion el ERP queda bloqueado por el tunel ($($r.Code))." }
elseif ($r.Code -eq 200) { Bad "El ERP responde 200 SIN sesion por el tunel: la puerta NO esta activa." }
else { Warn "Respuesta inesperada: $($r.Code)." }

# 7. Prueba con usuario (opcional)
if ($Usuario) {
    Info "7/7  Inicio de sesion por el tunel como '$Usuario'..."
    $pass = Read-Host "  Contrasena de $Usuario" -AsSecureString
    $plano = [System.Net.NetworkCredential]::new("", $pass).Password
    $ses = New-Object Microsoft.PowerShell.Commands.WebRequestSession
    try {
        $body = @{ usuario = $Usuario; contrasena = $plano } | ConvertTo-Json
        $lr = Invoke-WebRequest -Uri "$base/api/login" -Method Post -Body $body `
              -ContentType 'application/json' -WebSession $ses -SkipHttpErrorCheck -TimeoutSec 20
        if ([int]$lr.StatusCode -ne 200) {
            Bad "Login fallo (codigo $([int]$lr.StatusCode)): $($lr.Content)"
        } else {
            $rol = try { ($lr.Content | ConvertFrom-Json).rol } catch { "?" }
            Ok "Sesion iniciada (rol principal: $rol)."
            $f = Codigo "$base/api/facturas" $ses
            if ($f.Code -eq 200)     { Ok "/api/facturas por el tunel: 200 (acceso al ERP correcto)." }
            elseif ($f.Code -eq 403) { Warn "/api/facturas: 403. Correcto solo si el usuario NO es Admin, Ventas ni Contabilidad." }
            else                     { Bad "/api/facturas respondio $($f.Code)." }
            $null = Invoke-WebRequest -Uri "$base/api/logout" -Method Post -WebSession $ses -SkipHttpErrorCheck -TimeoutSec 20
        }
    } catch {
        Bad "Error en la prueba de inicio de sesion: $($_.Exception.Message)"
    } finally { $plano = $null }
} else {
    Info "7/7  (omitido) Para probar un usuario: -Usuario 'nombre_usuario'"
}

Write-Host ""
if ($script:fallas -eq 0) { Write-Host "  RESULTADO: todo correcto." -ForegroundColor Green }
else { Write-Host "  RESULTADO: $($script:fallas) problema(s) encontrado(s). Revisa las lineas [X]." -ForegroundColor Red }
Write-Host ""
