# ===========================================================================
# Verifica que los parches de seguridad estan activos en runtime.
# Compatible con PowerShell 5.1 (Windows) y PowerShell 7+.
# Uso:
#   1. dotnet run --project SistemaGestion.Api    (en otra ventana)
#   2. .\verificar_portal.ps1                       (HTTPS por defecto)
#   3. .\verificar_portal.ps1 -Url http://localhost:5050    (forzar HTTP)
#
# Opcional:
#   .\verificar_portal.ps1 -Usuario admin -Password "tu_pass"
#   .\verificar_portal.ps1 -SkipRateLimit
# ===========================================================================
param(
    [string]$Url      = "https://localhost:5443",
    [string]$Usuario  = "",
    [string]$Password = "",
    [switch]$SkipRateLimit
)

$ErrorActionPreference = 'Continue'
$script:ok    = 0
$script:fail  = 0
$script:warn  = 0

# ---- TLS + cert callbacks (PS5.1 / .NET Framework) ------------------------
# WebRequest.Create() respeta ServerCertificateValidationCallback fielmente,
# por eso aqui no necesitamos HttpClient. El bloque diagnostico confirmo que
# este camino funciona contra el cert auto-firmado del API.
[System.Net.ServicePointManager]::SecurityProtocol = [System.Net.SecurityProtocolType]::Tls12
[System.Net.ServicePointManager]::ServerCertificateValidationCallback = { $true }
[System.Net.ServicePointManager]::Expect100Continue = $false

function New-SgClient {
    # Wrapper minimo: solo guarda CookieContainer (compartido entre requests).
    return [PSCustomObject]@{
        Cookies = New-Object System.Net.CookieContainer
    }
}

function Test-Result {
    param([string]$Name, [bool]$Passed, [string]$Detail = "")
    if ($Passed) {
        Write-Host ("  [OK]   " + $Name) -ForegroundColor Green
        $script:ok++
    } else {
        Write-Host ("  [FAIL] " + $Name) -ForegroundColor Red
        if ($Detail) { Write-Host ("         " + $Detail) -ForegroundColor DarkGray }
        $script:fail++
    }
}
function Warn { param([string]$Msg)
    Write-Host ("  [WARN] " + $Msg) -ForegroundColor Yellow
    $script:warn++
}

# ---- Invoke-Raw: usa WebRequest directo (PS5.1 friendly) -----------------
function Invoke-Raw {
    param(
        [string]$Uri,
        [string]$Method  = 'Get',
        [hashtable]$Headers,
        [string]$Body,
        [string]$ContentType,
        $WebSession,
        [int]$TimeoutSec = 30,
        [int]$MaxRedir   = 5
    )
    try {
        $req = [System.Net.HttpWebRequest][System.Net.WebRequest]::Create($Uri)
        $req.Method            = $Method.ToUpper()
        $req.Timeout           = $TimeoutSec * 1000
        $req.ReadWriteTimeout  = $TimeoutSec * 1000
        $req.UserAgent         = 'verificar_portal.ps1'
        $req.AllowAutoRedirect = ($MaxRedir -gt 0)
        if ($MaxRedir -gt 0) { $req.MaximumAutomaticRedirections = $MaxRedir }
        $req.KeepAlive         = $false
        if ($WebSession -and $WebSession.Cookies) {
            $req.CookieContainer = $WebSession.Cookies
        } else {
            $req.CookieContainer = New-Object System.Net.CookieContainer
        }
        if ($Headers) {
            foreach ($k in $Headers.Keys) {
                # Algunos headers son "restringidos" y necesitan asignacion directa.
                switch ($k.ToLower()) {
                    'accept'         { $req.Accept    = [string]$Headers[$k] }
                    'content-type'   { $req.ContentType = [string]$Headers[$k] }
                    'user-agent'     { $req.UserAgent = [string]$Headers[$k] }
                    'host'           { $req.Host      = [string]$Headers[$k] }
                    default          { $req.Headers.Add($k, [string]$Headers[$k]) }
                }
            }
        }
        if ($Body) {
            if ($ContentType) { $req.ContentType = $ContentType }
            elseif (-not $req.ContentType) { $req.ContentType = 'application/json' }
            $bytes = [System.Text.Encoding]::UTF8.GetBytes($Body)
            $req.ContentLength = $bytes.Length
            $rs = $req.GetRequestStream()
            $rs.Write($bytes, 0, $bytes.Length)
            $rs.Close()
        }

        try {
            $resp = $req.GetResponse()
        } catch [System.Net.WebException] {
            # 4xx/5xx llegan por aqui — leemos del Response del WebException.
            if ($_.Exception.Response) {
                $resp = $_.Exception.Response
            } else {
                throw
            }
        }

        $code = [int]$resp.StatusCode
        $headersDict = @{}
        foreach ($k in $resp.Headers.AllKeys) { $headersDict[$k] = $resp.Headers[$k] }
        $bodyText = ""
        try {
            $sr = New-Object System.IO.StreamReader($resp.GetResponseStream())
            $bodyText = $sr.ReadToEnd()
            $sr.Dispose()
        } catch {}
        $resp.Close()

        return [PSCustomObject]@{
            StatusCode = $code
            Headers    = $headersDict
            Content    = $bodyText
            Raw        = $null
        }
    } catch {
        $msgs = @()
        $e = $_.Exception
        while ($e) {
            $msgs += ($e.GetType().Name + ': ' + $e.Message)
            $e = $e.InnerException
        }
        if ($msgs.Count -eq 0) { $msgs = @($_.ToString()) }
        return [PSCustomObject]@{
            StatusCode = 0
            Headers    = @{}
            Content    = ($msgs -join ' | ')
            Raw        = $null
        }
    }
}

Write-Host ""
Write-Host ("=== Verificando " + $Url + " ===") -ForegroundColor Cyan
Write-Host ""

# --- 0. Si el URL es HTTPS, verificar tambien el redirect HTTP -> HTTPS ----
if ($Url -match '^https://') {
    Write-Host "[0] Redirect HTTP -> HTTPS" -ForegroundColor White
    $urlHttp = $Url -replace '^https://', 'http://' -replace ':5443', ':5050'
    $r = Invoke-Raw -Uri ($urlHttp + "/index.html") -MaxRedir 0
    $isRedir = ($r.StatusCode -ge 300 -and $r.StatusCode -lt 400)
    $loc = [string]$r.Headers['Location']
    Test-Result "HTTP responde 30x"          $isRedir ("Status " + $r.StatusCode)
    Test-Result "Location apunta a HTTPS"    ($loc -match '^https://') ("Location: " + $loc)
    Write-Host ""
}

# --- 1. Headers de seguridad -----------------------------------------------
Write-Host "[1] Headers de seguridad" -ForegroundColor White
$r = Invoke-Raw -Uri ($Url + "/index.html")
if ($r.StatusCode -eq 0) {
    Test-Result "API responde en /index.html" $false $r.Content
    Write-Host ""
    Write-Host "No se puede continuar sin el API. Levantalo con:" -ForegroundColor Red
    Write-Host "  dotnet run --project SistemaGestion.Api" -ForegroundColor Red
    exit 1
}
$h = $r.Headers
Test-Result "X-Content-Type-Options = nosniff"     ($h['X-Content-Type-Options'] -eq 'nosniff')        ([string]$h['X-Content-Type-Options'])
Test-Result "X-Frame-Options = DENY"               ($h['X-Frame-Options']        -eq 'DENY')           ([string]$h['X-Frame-Options'])
Test-Result "Referrer-Policy presente"             ($null -ne $h['Referrer-Policy'])                   ([string]$h['Referrer-Policy'])
Test-Result "Permissions-Policy presente"          ($null -ne $h['Permissions-Policy'])                ([string]$h['Permissions-Policy'])
Test-Result "Content-Security-Policy presente"     ($null -ne $h['Content-Security-Policy'])
Test-Result "CSP frame-ancestors none"             ($h['Content-Security-Policy'] -match "frame-ancestors 'none'")
Test-Result "Cache-Control no-store en HTML"       ($h['Cache-Control']           -match "no-store")   ([string]$h['Cache-Control'])
if ($Url -match '^https://') {
    Test-Result "Strict-Transport-Security presente (HSTS)" ($null -ne $h['Strict-Transport-Security']) ([string]$h['Strict-Transport-Security'])
}

# --- 2. CORS - rechazo de origen externo -----------------------------------
Write-Host ""
Write-Host "[2] CORS - rechazo de origen externo" -ForegroundColor White
$r = Invoke-Raw -Uri ($Url + "/api/me") -Method Options `
    -Headers @{ 'Origin' = 'https://evil.com'; 'Access-Control-Request-Method' = 'GET' }
$aco = $r.Headers['Access-Control-Allow-Origin']
Test-Result "Origin externo NO recibe Allow-Origin" ($null -eq $aco -or $aco -ne 'https://evil.com') ("Allow-Origin: " + [string]$aco)

# --- 3. Rate limit en /api/login -------------------------------------------
if (-not $SkipRateLimit) {
    Write-Host ""
    Write-Host "[3] Rate limit en login (10 por minuto)" -ForegroundColor White
    $got429 = $false
    $codes  = @()
    for ($i = 1; $i -le 12; $i++) {
        $body = @{ usuario = "no_existe_$i"; contrasena = "x" } | ConvertTo-Json
        $r = Invoke-Raw -Uri ($Url + "/api/login") -Method Post `
            -ContentType 'application/json' -Body $body -TimeoutSec 5
        $codes += $r.StatusCode
        if ($r.StatusCode -eq 429) { $got429 = $true; break }
    }
    Test-Result "Tras 11 intentos rapidos -> 429 Too Many Requests" $got429 ("Codigos: " + ($codes -join ','))

    if ($got429) {
        Write-Host "  Esperando 65s para que expire la ventana del rate-limit..." -ForegroundColor DarkGray
        Start-Sleep -Seconds 65
    }
} else {
    Write-Host ""
    Write-Host "[3] Rate limit - omitido (-SkipRateLimit)" -ForegroundColor DarkYellow
}

# --- 4. Login con credenciales invalidas - timing y respuesta --------------
Write-Host ""
Write-Host "[4] Login con credenciales invalidas" -ForegroundColor White
$body = @{ usuario = "definitivamente_no_existe_xyz"; contrasena = "xxxxxxx" } | ConvertTo-Json
$sw = [System.Diagnostics.Stopwatch]::StartNew()
$r = Invoke-Raw -Uri ($Url + "/api/login") -Method Post `
    -ContentType 'application/json' -Body $body
$sw.Stop()
Test-Result "Usuario inexistente devuelve 401"           ($r.StatusCode -eq 401) ("Status " + $r.StatusCode)
Test-Result "Respuesta tarda al menos 200ms"             ($sw.ElapsedMilliseconds -ge 200) ("Tardo " + $sw.ElapsedMilliseconds + " ms")
Test-Result "Body NO contiene token"                     ($r.Content -notmatch '"token"')
Test-Result "Body NO contiene stack trace"               ($r.Content -notmatch 'Stack|Trace|Exception')

# --- 5. Login exitoso + revocacion (si se proporciono credencial) ----------
if ($Usuario -and $Password) {
    Write-Host ""
    Write-Host ("[5] Login exitoso con " + $Usuario) -ForegroundColor White
    $body = @{ usuario = $Usuario; contrasena = $Password } | ConvertTo-Json
    $session = New-SgClient
    $r = Invoke-Raw -Uri ($Url + "/api/login") -Method Post `
        -ContentType 'application/json' -Body $body -WebSession $session
    if ($r.StatusCode -eq 200) {
        Test-Result "Login devuelve 200" $true
        Test-Result "Body NO contiene token"        ($r.Content -notmatch '"token"')
        Test-Result "Body contiene nombreUsuario"   ($r.Content -match '"nombreUsuario"')
        $cookie = $session.Cookies.GetCookies($Url) | Where-Object { $_.Name -eq 'sg_token' }
        if ($cookie) {
            Test-Result "Cookie sg_token presente"  $true
            Test-Result "Cookie HttpOnly"           ($cookie.HttpOnly)
            $setCookie = [string]$r.Headers['Set-Cookie']
            Test-Result "Cookie SameSite Strict"    ($setCookie -match 'SameSite=Strict')
            if ($Url -match '^https://') {
                Test-Result "Cookie Secure (HTTPS)" ($cookie.Secure -or $setCookie -match 'Secure')
            }
        } else {
            Test-Result "Cookie sg_token presente"  $false
        }

        $rm = Invoke-Raw -Uri ($Url + "/api/me") -WebSession $session
        Test-Result "/api/me con cookie devuelve 200" ($rm.StatusCode -eq 200) ("Status " + $rm.StatusCode)

        # ---- Revocacion: tras logout, /api/me debe responder 401 ----
        Write-Host ""
        Write-Host "[5b] Revocacion de token tras logout" -ForegroundColor White
        $rl = Invoke-Raw -Uri ($Url + "/api/logout") -Method Post -WebSession $session
        Test-Result "/api/logout devuelve 200"      ($rl.StatusCode -eq 200) ("Status " + $rl.StatusCode)

        $cookieDel = $session.Cookies.GetCookies($Url) | Where-Object { $_.Name -eq 'sg_token' }
        $borrada = ($null -eq $cookieDel) -or [string]::IsNullOrEmpty($cookieDel.Value)
        Test-Result "Logout limpia la cookie sg_token" $borrada

        # Reinyectar la cookie (simulando un atacante que la robo antes del logout)
        # para confirmar que la revocacion server-side la rechaza.
        if (-not $borrada -or $cookieDel) {
            # ya no esta, no podemos seguir
        } else {
            # simular reuso del jti revocado
            $sessionRobada = New-Object Microsoft.PowerShell.Commands.WebRequestSession
            $cookieRobada = New-Object System.Net.Cookie('sg_token', $cookie.Value, '/', $cookie.Domain)
            $sessionRobada.Cookies.Add($cookieRobada)
            $rRevoked = Invoke-Raw -Uri ($Url + "/api/me") -WebSession $sessionRobada
            Test-Result "/api/me con jti revocado -> 401" ($rRevoked.StatusCode -eq 401) ("Status " + $rRevoked.StatusCode + " - " + $rRevoked.Content.Substring(0, [Math]::Min(80, $rRevoked.Content.Length)))
        }
    } else {
        Test-Result "Login devuelve 200" $false ("Codigo " + $r.StatusCode + ": " + $r.Content)
    }
} else {
    Write-Host ""
    Warn "Saltando pruebas de login y revocacion. Pasa -Usuario y -Password para incluirlas."
}

# --- 6. /api/me sin token -> 401 -------------------------------------------
Write-Host ""
Write-Host "[6] /api/me sin token" -ForegroundColor White
$r = Invoke-Raw -Uri ($Url + "/api/me")
Test-Result "/api/me sin token -> 401" ($r.StatusCode -eq 401) ("Status " + $r.StatusCode)

# --- 7. Pagina interna (dashboard) -----------------------------------------
Write-Host ""
Write-Host "[7] Pagina interna" -ForegroundColor White
$r = Invoke-Raw -Uri ($Url + "/pages/dashboard.html")
Test-Result "/pages/dashboard.html servida"      ($r.StatusCode -eq 200) ("Status " + $r.StatusCode)
Test-Result "CSP tambien en pagina interna"      ($null -ne $r.Headers['Content-Security-Policy'])
$contenido = [string]$r.Content
Test-Result "Sin maximum-scale en viewport"      ($contenido -notmatch 'maximum-scale')
Test-Result "Sin user-scalable=no en viewport"   ($contenido -notmatch 'user-scalable\s*=\s*no')

# --- Resumen ---------------------------------------------------------------
Write-Host ""
Write-Host "===========================================================" -ForegroundColor Cyan
$linea = (" RESUMEN: " + $script:ok + " OK | " + $script:fail + " FAIL | " + $script:warn + " WARN")
$color = "Green"
if ($script:fail -ne 0) { $color = "Red" }
Write-Host $linea -ForegroundColor $color
Write-Host "===========================================================" -ForegroundColor Cyan
Write-Host ""
if ($script:fail -eq 0) {
    Write-Host "Todos los parches verificados en runtime." -ForegroundColor Green
    exit 0
} else {
    Write-Host "Hay verificaciones fallidas - revisa los detalles arriba." -ForegroundColor Red
    exit 1
}
