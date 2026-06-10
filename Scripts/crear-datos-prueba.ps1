# ============================================================
#  Crear datos de prueba: cliente + factura fiscal + nota de entrega
#  Ejecutar en PowerShell desde OLIMPO o SERVO
# ============================================================

param(
    [string]$Server = "https://localhost:5443",
    [string]$Usuario = "admin",
    [string]$Clave = "admin1234"
)

# Ignorar certificado auto-firmado
Add-Type @"
using System.Net;
using System.Security.Cryptography.X509Certificates;
public class TrustAll : ICertificatePolicy {
    public bool CheckValidationResult(ServicePoint sp, X509Certificate cert, WebRequest req, int problem) { return true; }
}
"@
[System.Net.ServicePointManager]::CertificatePolicy = New-Object TrustAll
[Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12

# ── 1. Login ──
Write-Host "`n=== LOGIN ===" -ForegroundColor Cyan
$session = New-Object Microsoft.PowerShell.Commands.WebRequestSession
$loginBody = @{ usuario = $Usuario; contrasena = $Clave } | ConvertTo-Json
try {
    $login = Invoke-WebRequest -Uri "$Server/api/login" -Method POST -Body $loginBody `
        -ContentType "application/json" -WebSession $session -UseBasicParsing
    Write-Host "  Login OK" -ForegroundColor Green
} catch {
    Write-Host "  ERROR login: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}

# Helper para GET/POST
function ApiGet($url) {
    $r = Invoke-WebRequest -Uri "$Server$url" -WebSession $session -UseBasicParsing
    return $r.Content | ConvertFrom-Json
}
function ApiPost($url, $body) {
    $json = $body | ConvertTo-Json -Depth 5
    $r = Invoke-WebRequest -Uri "$Server$url" -Method POST -Body $json `
        -ContentType "application/json" -WebSession $session -UseBasicParsing
    return $r.Content | ConvertFrom-Json
}

# ── 2. Crear cliente de prueba ──
Write-Host "`n=== CREAR CLIENTE DE PRUEBA ===" -ForegroundColor Cyan
$cliente = @{
    Codigo      = "PRUEBA-001"
    Nombre      = "DISTRIBUIDORA TEST C.A."
    CiRif       = "J-12345678-9"
    Telefono    = "0244-555-1234"
    Ciudad      = "Villa de Cura"
    Direccion   = "Av. Bolivar, Centro Comercial Plaza, Local 15"
    Email       = "test@distribuidora.com"
    Contacto    = "Juan Perez"
}
try {
    $rc = ApiPost "/api/clientes" $cliente
    Write-Host "  Cliente creado: $($cliente.Nombre)" -ForegroundColor Green
} catch {
    if ($_.Exception.Response.StatusCode -eq 409) {
        Write-Host "  Cliente ya existe, continuando..." -ForegroundColor Yellow
    } else {
        Write-Host "  Error: $($_.Exception.Message)" -ForegroundColor Red
    }
}

# ── 3. Obtener productos con precio ──
Write-Host "`n=== PRODUCTOS DISPONIBLES ===" -ForegroundColor Cyan
$inventario = ApiGet "/api/inventario"
$prods = $inventario | Where-Object { $_.precioActual -gt 0 } | Select-Object -First 3
foreach ($p in $prods) {
    Write-Host "  $($p.codigo) - $($p.descripcion) @ `$$($p.precioActual)"
}

if ($prods.Count -lt 2) {
    Write-Host "  No hay suficientes productos con precio. Abortando." -ForegroundColor Red
    exit 1
}

# ── 4. Crear FACTURA FISCAL (con IVA 16%, carta) ──
Write-Host "`n=== CREAR FACTURA FISCAL (IVA 16%) ===" -ForegroundColor Cyan
$factura = @{
    Fecha         = (Get-Date -Format "yyyy-MM-dd")
    CodigoCliente = "PRUEBA-001"
    FormaDePago   = "Contado"
    PctIva        = 16
    Descuento     = 0
    DiasCredito   = 0
    Observaciones = "Factura fiscal de prueba - IVA 16%"
    Items         = @(
        @{ Codigo = $prods[0].codigo; Cantidad = 15; PrecioUnit = $prods[0].precioActual },
        @{ Codigo = $prods[1].codigo; Cantidad = 8;  PrecioUnit = $prods[1].precioActual }
    )
}
try {
    $rf = ApiPost "/api/facturas" $factura
    $numFactura = $rf.numeroF
    Write-Host "  Factura creada: $numFactura - Total: `$$($rf.total) (IVA: `$$($rf.iva))" -ForegroundColor Green
    Write-Host "  Imprimir: $Server/api/facturas/$numFactura/imprimir" -ForegroundColor White
} catch {
    Write-Host "  Error: $($_.Exception.Message)" -ForegroundColor Red
    $numFactura = $null
}

# ── 5. Crear NOTA DE ENTREGA (sin IVA, media carta) ──
Write-Host "`n=== CREAR NOTA DE ENTREGA (sin IVA) ===" -ForegroundColor Cyan
$nota = @{
    Fecha         = (Get-Date -Format "yyyy-MM-dd")
    CodigoCliente = "PRUEBA-001"
    FormaDePago   = "Credito"
    PctIva        = 0
    Descuento     = 5
    DiasCredito   = 30
    Observaciones = "Nota de entrega de prueba - media carta"
    Items         = @(
        @{ Codigo = $prods[0].codigo; Cantidad = 25; PrecioUnit = $prods[0].precioActual },
        @{ Codigo = $prods[1].codigo; Cantidad = 12; PrecioUnit = $prods[1].precioActual }
    )
}
if ($prods.Count -ge 3) {
    $nota.Items += @{ Codigo = $prods[2].codigo; Cantidad = 6; PrecioUnit = $prods[2].precioActual }
}
try {
    $rn = ApiPost "/api/facturas" $nota
    $numNota = $rn.numeroF
    Write-Host "  Nota creada: $numNota - Total: `$$($rn.total)" -ForegroundColor Green
    Write-Host "  Imprimir: $Server/api/facturas/$numNota/imprimir" -ForegroundColor White
} catch {
    Write-Host "  Error: $($_.Exception.Message)" -ForegroundColor Red
    $numNota = $null
}

# ── Resumen ──
Write-Host "`n========================================" -ForegroundColor Cyan
Write-Host "  RESUMEN" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  Cliente: PRUEBA-001 - DISTRIBUIDORA TEST C.A."
if ($numFactura) {
    Write-Host "  Factura fiscal: $Server/api/facturas/$numFactura/imprimir" -ForegroundColor Green
}
if ($numNota) {
    Write-Host "  Nota entrega:   $Server/api/facturas/$numNota/imprimir" -ForegroundColor Green
}
Write-Host "`n  Abre esas URLs en el navegador y presiona Ctrl+P para imprimir." -ForegroundColor Yellow
Write-Host "  La factura sale en tamano CARTA, la nota en MEDIA CARTA.`n"
