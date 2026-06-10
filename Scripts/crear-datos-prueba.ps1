# ============================================================
#  Crear datos de prueba: productos + cliente + factura + nota
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

# ── 2. Verificar inventario, crear productos si esta vacio ──
Write-Host "`n=== VERIFICAR INVENTARIO ===" -ForegroundColor Cyan
$inventario = ApiGet "/api/inventario"

if ($inventario.Count -lt 2) {
    Write-Host "  Inventario vacio. Insertando productos de prueba en la BD..." -ForegroundColor Yellow

    # Buscar la ruta de la BD desde las rutas conocidas
    $dbPaths = @(
        "C:\Users\PC\AppData\Roaming\SistemaGestion\sistema_gestion.db",
        "C:\SistemaGestion\sistema_gestion.db",
        "$env:APPDATA\SistemaGestion\sistema_gestion.db"
    )
    $dbPath = $null
    foreach ($p in $dbPaths) {
        if (Test-Path $p) { $dbPath = $p; break }
    }

    if (-not $dbPath) {
        Write-Host "  No se encontro la BD. Rutas buscadas:" -ForegroundColor Red
        $dbPaths | ForEach-Object { Write-Host "    $_" }
        exit 1
    }
    Write-Host "  BD encontrada: $dbPath" -ForegroundColor Green

    # Cargar el DLL de SQLite desde el publish
    $sqliteDll = Get-ChildItem -Path "C:\Users\PC\AppData\Local\SistemaGestion\publish\Api" -Filter "Microsoft.Data.Sqlite.dll" -Recurse | Select-Object -First 1
    if (-not $sqliteDll) {
        $sqliteDll = Get-ChildItem -Path "C:\SistemaGestion\publish\Api" -Filter "Microsoft.Data.Sqlite.dll" -Recurse | Select-Object -First 1
    }
    if ($sqliteDll) {
        Add-Type -Path $sqliteDll.FullName -ErrorAction SilentlyContinue
    }

    # Usar sqlite3 si esta disponible, sino usar ADO.NET
    $sqlite3 = Get-Command sqlite3 -ErrorAction SilentlyContinue

    $productosSQL = @(
        "INSERT OR IGNORE INTO Inventario (Codigo,Descripcion,Departamento,Unidad,Existencia,ExistenMin,ExistenMax,Precio,CostoActual,UltimaActualz) VALUES ('TEST-001','Tubo PVC 1/2 pulg','General','UND',500,50,1000,8.50,5.00,'2026-06-10');",
        "INSERT OR IGNORE INTO Inventario (Codigo,Descripcion,Departamento,Unidad,Existencia,ExistenMin,ExistenMax,Precio,CostoActual,UltimaActualz) VALUES ('TEST-002','Codo PVC 1/2 pulg','General','UND',300,30,600,3.25,1.80,'2026-06-10');",
        "INSERT OR IGNORE INTO Inventario (Codigo,Descripcion,Departamento,Unidad,Existencia,ExistenMin,ExistenMax,Precio,CostoActual,UltimaActualz) VALUES ('TEST-003','Tee PVC 1/2 pulg','General','UND',200,20,400,4.75,2.50,'2026-06-10');"
    )

    if ($sqlite3) {
        $sql = $productosSQL -join "`n"
        $sql | & sqlite3 $dbPath
        Write-Host "  3 productos insertados via sqlite3" -ForegroundColor Green
    } else {
        # Usar System.Data.SQLite via connection string directo
        try {
            $connStr = "Data Source=$dbPath"
            $conn = New-Object Microsoft.Data.Sqlite.SqliteConnection($connStr)
            $conn.Open()
            foreach ($sql in $productosSQL) {
                $cmd = $conn.CreateCommand()
                $cmd.CommandText = $sql
                $cmd.ExecuteNonQuery() | Out-Null
            }
            $conn.Close()
            Write-Host "  3 productos insertados via ADO.NET" -ForegroundColor Green
        } catch {
            Write-Host "  Error insertando productos: $($_.Exception.Message)" -ForegroundColor Red
            Write-Host "  Intente instalar sqlite3: winget install SQLite.SQLite" -ForegroundColor Yellow
            exit 1
        }
    }

    # Recargar inventario
    $inventario = ApiGet "/api/inventario"
    if ($inventario.Count -lt 2) {
        Write-Host "  Aun sin productos despues de insertar. Reinicie la API y vuelva a intentar." -ForegroundColor Red
        exit 1
    }
}

$prods = $inventario | Select-Object -First 3
foreach ($p in $prods) {
    if ($p.precioActual -le 0) { $p.precioActual = 15.00 }
    Write-Host "  $($p.codigo) - $($p.descripcion) @ `$$($p.precioActual)"
}

# ── 3. Crear cliente de prueba ──
Write-Host "`n=== CREAR CLIENTE DE PRUEBA ===" -ForegroundColor Cyan
$cliente = @{
    Codigo      = "PRUEBA-001"
    Descripcion = "DISTRIBUIDORA TEST C.A."
    CiRif       = "J-12345678-9"
    Telefono    = "0244-555-1234"
    Ciudad      = "Villa de Cura"
    Direccion   = "Av. Bolivar, Centro Comercial Plaza, Local 15"
    Email       = "test@distribuidora.com"
    Contacto    = "Juan Perez"
}
try {
    $rc = ApiPost "/api/clientes" $cliente
    Write-Host "  Cliente creado: $($cliente.Descripcion)" -ForegroundColor Green
} catch {
    if ($_.Exception.Response.StatusCode -eq 409) {
        Write-Host "  Cliente ya existe, continuando..." -ForegroundColor Yellow
    } else {
        Write-Host "  Error: $($_.Exception.Message)" -ForegroundColor Red
    }
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
    Write-Host "  Factura fiscal (CARTA):       $Server/api/facturas/$numFactura/imprimir" -ForegroundColor Green
}
if ($numNota) {
    Write-Host "  Nota entrega (MEDIA CARTA):   $Server/api/facturas/$numNota/imprimir" -ForegroundColor Green
}
Write-Host "`n  Abre esas URLs en el navegador y presiona Ctrl+P para imprimir." -ForegroundColor Yellow
Write-Host "  La factura sale en CARTA, la nota en MEDIA CARTA.`n"
