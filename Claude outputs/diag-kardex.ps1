# ============================================================
#  diag-kardex.ps1  —  Diagnóstico: por qué la materia prima
#  no aparece en el Kardex de insumos.
#
#  EJECUTAR EN OLIMPO (la máquina donde corre la app y vive la BD),
#  en PowerShell 7, con la app abierta o cerrada (da igual, solo lee):
#      & 'C:\SistemaGestion\diag-kardex.ps1'
#
#  No modifica nada: abre la base de datos en SOLO LECTURA y muestra
#  el estado de insumos, egresos, detalles, movimientos de Kardex y
#  los registros de auditoría de los últimos egresos.
# ============================================================
$ErrorActionPreference = 'Stop'

function Titulo($t) { Write-Host ""; Write-Host "==== $t ====" -ForegroundColor Cyan }

# ── 1) Localizar la base de datos (misma lógica que la app) ──────────
$db = [Environment]::GetEnvironmentVariable('SG_DB_PATH','Machine')
if (-not $db) { $db = $env:SG_DB_PATH }
if (-not $db) { $db = Join-Path $env:APPDATA 'SistemaGestion\sistema_gestion.db' }

Write-Host "Base de datos: $db" -ForegroundColor Yellow
if (-not (Test-Path $db)) {
    Write-Host "  [ERROR] No existe la BD en esa ruta." -ForegroundColor Red
    Write-Host "  Revisa que estés en OLIMPO y con el mismo usuario de Windows con que abres la app." -ForegroundColor Red
    exit 1
}

# ── 2) Cargar Microsoft.Data.Sqlite desde el publish de la app ──────
$appDir = 'C:\SistemaGestion\publish\App'
$msdata = Join-Path $appDir 'Microsoft.Data.Sqlite.dll'
if (-not (Test-Path $msdata)) {
    Write-Host "  [ERROR] No encontré Microsoft.Data.Sqlite.dll en $appDir" -ForegroundColor Red
    Write-Host "  Ajusta la variable `$appDir en el script a la carpeta donde está publicada la app." -ForegroundColor Red
    exit 1
}

# El .dll nativo e_sqlite3.dll debe poder resolverse: nos paramos en la
# carpeta de la app y la agregamos al PATH del proceso.
Set-Location $appDir
$env:PATH = "$appDir;$env:PATH"

foreach ($dep in @(
    'SQLitePCLRaw.core.dll',
    'SQLitePCLRaw.provider.e_sqlite3.dll',
    'SQLitePCLRaw.batteries_v2.dll',
    'Microsoft.Data.Sqlite.dll')) {
    $p = Join-Path $appDir $dep
    if (Test-Path $p) { Add-Type -Path $p }
}
try { [SQLitePCL.Batteries_V2]::Init() } catch { }

# ── 3) Abrir en SOLO LECTURA ────────────────────────────────────────
$cs = "Data Source=$db;Mode=ReadOnly;Cache=Shared"
$conn = New-Object Microsoft.Data.Sqlite.SqliteConnection($cs)
$conn.Open()
$pragma = $conn.CreateCommand(); $pragma.CommandText = "PRAGMA busy_timeout=3000"; [void]$pragma.ExecuteNonQuery()

function Q($titulo, $sql) {
    Titulo $titulo
    try {
        $cmd = $conn.CreateCommand(); $cmd.CommandText = $sql
        $r = $cmd.ExecuteReader()
        $n = 0
        while ($r.Read()) {
            $n++
            $campos = @()
            for ($i = 0; $i -lt $r.FieldCount; $i++) {
                $val = if ($r.IsDBNull($i)) { 'NULL' } else { $r.GetValue($i) }
                $campos += ($r.GetName($i) + '=' + $val)
            }
            Write-Host ('  ' + ($campos -join '  |  '))
        }
        if ($n -eq 0) { Write-Host "  (sin filas)" -ForegroundColor DarkGray }
        $r.Close()
    }
    catch {
        Write-Host ("  [consulta falló] " + $_.Exception.Message) -ForegroundColor Red
    }
}

Q "INSUMOS (ProductosProveedor)" `
  "SELECT Id, CodigoInterno, Descripcion, StockActual, PrecioPromedio, PrecioUltCompra, Activo FROM ProductosProveedor ORDER BY Id"

Q "ÚLTIMOS 6 EGRESOS (Gastos)" `
  "SELECT Id, Fecha, Concepto, PrecioPendiente, Estado, IdInsumo, Cantidad, MontoUSD FROM Gastos ORDER BY Id DESC LIMIT 6"

Q "LÍNEAS DE LOS ÚLTIMOS EGRESOS (DetallesGasto)" `
  "SELECT IdGasto, IdArticulo, CodigoInterno, EsInventariable, Cantidad, PrecioUnitario, Subtotal, Descripcion FROM DetallesGasto ORDER BY IdGasto DESC, rowid DESC LIMIT 12"

Q "TOTAL DE MOVIMIENTOS DE KARDEX (materia prima)" `
  "SELECT COUNT(*) AS TotalMovimientosMP FROM MovimientosInventario WHERE TipoKardex='MATERIA_PRIMA'"

Q "ÚLTIMOS MOVIMIENTOS DE KARDEX (materia prima)" `
  "SELECT Id, FechaHora, CodigoProducto, TipoMovimiento, Cantidad, CostoUnitario, Referencia FROM MovimientosInventario WHERE TipoKardex='MATERIA_PRIMA' ORDER BY Id DESC LIMIT 12"

Q "AUDITORÍA reciente de egresos (LogAuditoria)" `
  "SELECT FechaHora, Accion, Tabla, Detalle FROM LogAuditoria WHERE Accion LIKE 'GASTO%' ORDER BY Id DESC LIMIT 20"

$conn.Close()
Write-Host ""
Write-Host "==== Diagnóstico terminado ====" -ForegroundColor Green
Write-Host "Copia y pega TODO lo que salió arriba para revisarlo." -ForegroundColor Green
