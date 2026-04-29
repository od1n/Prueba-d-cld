# ===========================================================================
# Diagnostica el handshake TLS contra el API.
# Aisla las posibles causas de "Error inesperado de envio".
# ===========================================================================
[System.Net.ServicePointManager]::SecurityProtocol = [System.Net.SecurityProtocolType]::Tls12
[System.Net.ServicePointManager]::ServerCertificateValidationCallback = { $true }

Write-Host ""
Write-Host "=== Diagnostico HTTPS https://localhost:5443 ===" -ForegroundColor Cyan
Write-Host ""

# 1. TCP basico
Write-Host "[1] Conexion TCP al puerto 5443" -ForegroundColor White
$tcp = Test-NetConnection -ComputerName localhost -Port 5443 -WarningAction SilentlyContinue
if ($tcp.TcpTestSucceeded) {
    Write-Host "   OK - Puerto abierto" -ForegroundColor Green
} else {
    Write-Host "   FAIL - Kestrel no escucha en 5443" -ForegroundColor Red
    Write-Host "   Revisa la consola del API: deberia decir 'Now listening on: https://[::]:5443'" -ForegroundColor Yellow
    exit 1
}

# 2. TLS handshake con SslStream directo
Write-Host ""
Write-Host "[2] Handshake TLS directo (SslStream)" -ForegroundColor White
try {
    $tcpClient = New-Object System.Net.Sockets.TcpClient('localhost', 5443)
    $sslStream = New-Object System.Net.Security.SslStream($tcpClient.GetStream(), $false,
        ([System.Net.Security.RemoteCertificateValidationCallback]{ param($s,$c,$ch,$e) $true }))
    $sslStream.AuthenticateAsClient('localhost', $null,
        [System.Security.Authentication.SslProtocols]::Tls12, $false)
    Write-Host "   OK - Handshake TLS completado" -ForegroundColor Green
    Write-Host "   Protocolo: $($sslStream.SslProtocol)" -ForegroundColor DarkGray
    Write-Host "   Cipher:    $($sslStream.CipherAlgorithm) $($sslStream.CipherStrength) bits" -ForegroundColor DarkGray
    if ($sslStream.RemoteCertificate) {
        $cert = New-Object System.Security.Cryptography.X509Certificates.X509Certificate2($sslStream.RemoteCertificate)
        Write-Host "   Cert subject: $($cert.Subject)" -ForegroundColor DarkGray
        Write-Host "   Cert issuer:  $($cert.Issuer)" -ForegroundColor DarkGray
        Write-Host "   Cert valido:  $($cert.NotBefore.ToString('yyyy-MM-dd')) -> $($cert.NotAfter.ToString('yyyy-MM-dd'))" -ForegroundColor DarkGray
        $san = $cert.Extensions | Where-Object { $_.Oid.Value -eq '2.5.29.17' }
        if ($san) { Write-Host "   SAN: $($san.Format($false))" -ForegroundColor DarkGray }
    }
    $sslStream.Dispose()
    $tcpClient.Close()
} catch {
    Write-Host "   FAIL - $($_.Exception.Message)" -ForegroundColor Red
    if ($_.Exception.InnerException) {
        Write-Host "   Inner: $($_.Exception.InnerException.Message)" -ForegroundColor DarkRed
    }
    Write-Host "   El cert que envia Kestrel no es aceptable para SChannel." -ForegroundColor Yellow
}

# 3. HTTP request basico via WebRequest (lo que usa Invoke-WebRequest internamente)
Write-Host ""
Write-Host "[3] HTTP GET via WebRequest" -ForegroundColor White
try {
    $req = [System.Net.WebRequest]::Create('https://localhost:5443/index.html')
    $req.Method = 'GET'
    $req.Timeout = 10000
    $resp = $req.GetResponse()
    Write-Host "   OK - $($resp.StatusCode)" -ForegroundColor Green
    $resp.Close()
} catch {
    Write-Host "   FAIL - $($_.Exception.Message)" -ForegroundColor Red
}

# 4. HTTP request via HttpClient (camino moderno, .NET 4.5+)
Write-Host ""
Write-Host "[4] HTTP GET via HttpClient" -ForegroundColor White
try {
    Add-Type -AssemblyName System.Net.Http
    $handler = New-Object System.Net.Http.HttpClientHandler
    $handler.ServerCertificateCustomValidationCallback = { param($a,$b,$c,$d) $true }
    $client  = New-Object System.Net.Http.HttpClient($handler)
    $client.Timeout = [TimeSpan]::FromSeconds(10)
    $resp = $client.GetAsync('https://localhost:5443/index.html').Result
    Write-Host "   OK - $($resp.StatusCode)" -ForegroundColor Green
    $client.Dispose()
} catch {
    Write-Host "   FAIL - $($_.Exception.Message)" -ForegroundColor Red
    if ($_.Exception.InnerException) {
        Write-Host "   Inner: $($_.Exception.InnerException.Message)" -ForegroundColor DarkRed
    }
}

# 5. curl si esta disponible
Write-Host ""
Write-Host "[5] curl -k (si esta instalado)" -ForegroundColor White
$curl = Get-Command curl.exe -ErrorAction SilentlyContinue
if ($curl) {
    try {
        & curl.exe -k -s -o NUL -w "   Status: %{http_code}, tiempo: %{time_total}s`n" `
            https://localhost:5443/index.html
    } catch {
        Write-Host "   FAIL: $($_.Exception.Message)" -ForegroundColor Red
    }
} else {
    Write-Host "   (curl no esta en el PATH, omitido)" -ForegroundColor DarkGray
}

Write-Host ""
Write-Host "=== Fin diagnostico ===" -ForegroundColor Cyan
Write-Host ""
Write-Host "Interpretacion:" -ForegroundColor Yellow
Write-Host "  - Si [1] FAIL  => Kestrel no esta escuchando. Revisa logs del API."
Write-Host "  - Si [1] OK pero [2] FAIL => problema con el cert (SChannel lo rechaza)."
Write-Host "  - Si [2] OK pero [3] FAIL => bug PS5.1+WebRequest. Usa el script con HttpClient."
Write-Host "  - Si [4] OK pero [3] FAIL => migrar verificar_portal.ps1 a HttpClient."
Write-Host "  - Si [5] OK pero [3]/[4] FAIL => problema solo en .NET Framework, usa curl."
