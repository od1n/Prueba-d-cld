# Diagnostico minimo de HttpClient contra https://localhost:5443
[System.Net.ServicePointManager]::SecurityProtocol = [System.Net.SecurityProtocolType]::Tls12
Add-Type -AssemblyName System.Net.Http -ErrorAction SilentlyContinue

Write-Host ""
Write-Host "[A] HttpClient simple sin redirect" -ForegroundColor Cyan
$h = New-Object System.Net.Http.HttpClientHandler
$h.AllowAutoRedirect = $false
$h.ServerCertificateCustomValidationCallback = { param($a,$b,$c,$d) $true }
$c = New-Object System.Net.Http.HttpClient($h)
$c.Timeout = [TimeSpan]::FromSeconds(10)
try {
    $r = $c.GetAsync('https://localhost:5443/index.html').Result
    Write-Host "  OK Status: $([int]$r.StatusCode) $($r.StatusCode)" -ForegroundColor Green
    Write-Host "  Headers de seguridad presentes:"
    foreach ($k in 'X-Frame-Options','Content-Security-Policy','Strict-Transport-Security') {
        $v = $r.Headers.GetValues($k) -join ', '
        Write-Host "    $k = $v"
    }
} catch {
    Write-Host "  FAIL: $($_.Exception.GetType().FullName)" -ForegroundColor Red
    $e = $_.Exception
    while ($e) {
        Write-Host "    $($e.GetType().FullName): $($e.Message)" -ForegroundColor DarkRed
        $e = $e.InnerException
    }
}

Write-Host ""
Write-Host "[B] HttpClient con AutoRedirect (que es lo que hace verificar_portal)" -ForegroundColor Cyan
$h2 = New-Object System.Net.Http.HttpClientHandler
$h2.AllowAutoRedirect = $true
$h2.MaxAutomaticRedirections = 5
$h2.ServerCertificateCustomValidationCallback = { param($a,$b,$c,$d) $true }
$c2 = New-Object System.Net.Http.HttpClient($h2)
$c2.Timeout = [TimeSpan]::FromSeconds(10)
try {
    $r = $c2.GetAsync('https://localhost:5443/index.html').Result
    Write-Host "  OK Status: $([int]$r.StatusCode)" -ForegroundColor Green
} catch {
    Write-Host "  FAIL: $($_.Exception.GetType().FullName)" -ForegroundColor Red
    $e = $_.Exception
    while ($e) {
        Write-Host "    $($e.GetType().FullName): $($e.Message)" -ForegroundColor DarkRed
        $e = $e.InnerException
    }
}

Write-Host ""
Write-Host "[C] Mismo HttpClient con HttpRequestMessage (lo que usa Invoke-Raw)" -ForegroundColor Cyan
try {
    $msg = New-Object System.Net.Http.HttpRequestMessage(
        [System.Net.Http.HttpMethod]::Get,
        [Uri]'https://localhost:5443/index.html')
    $r = $c2.SendAsync($msg).Result
    Write-Host "  OK Status: $([int]$r.StatusCode)" -ForegroundColor Green
} catch {
    Write-Host "  FAIL: $($_.Exception.GetType().FullName)" -ForegroundColor Red
    $e = $_.Exception
    while ($e) {
        Write-Host "    $($e.GetType().FullName): $($e.Message)" -ForegroundColor DarkRed
        $e = $e.InnerException
    }
}
Write-Host ""
