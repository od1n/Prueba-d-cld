# ============================================================================
#  actualizar-bonos-productos.ps1
# ----------------------------------------------------------------------------
#  Carga masiva del BONO POR PRODUCTO (en Bs) segun el primer digito del
#  codigo del producto. Actualiza la columna vigente en Inventario y ancla el
#  historial fechado (TarifaProductoProduccion / TarifaProductoVenta) desde una
#  fecha base, para que el recalculo de semanas pasadas use estas tarifas y,
#  si mañana cambias una tarifa desde el sistema, las semanas viejas conserven
#  la tarifa vieja.
#
#  Tarifas aplicadas (primer digito del codigo):
#     1        -> Produccion 135 Bs/u   |  Venta 15 Bs/u   (fustes)
#     4        -> Produccion  30 Bs/par |  Venta  5 Bs/par
#     5 o 6    -> Produccion 200 Bs/u   |  Venta 15 Bs/u
#     7 o 8    -> Produccion 100 Bs/u   |  Venta 10 Bs/u
#  Los demas codigos NO se tocan.
#
#  Uso (PowerShell 7):
#     pwsh -File 'C:\SistemaGestion\deploy\actualizar-bonos-productos.ps1'
#     pwsh -File 'C:\SistemaGestion\deploy\actualizar-bonos-productos.ps1' -DbPath 'C:\ruta\sistema_gestion.db'
#     pwsh -File 'C:\SistemaGestion\deploy\actualizar-bonos-productos.ps1' -ApiDir 'C:\SistemaGestion\api'
#
#  Parametros (todos opcionales):
#     -DbPath  Ruta exacta al archivo sistema_gestion.db. Si no se indica, se
#              detecta con SG_DB_PATH (variable de maquina) y, si no existe,
#              buscando en ubicaciones habituales.
#     -ApiDir  Carpeta donde esta publicada la API (de ahi se cargan las DLL de
#              SQLite). Si no se indica, se busca automaticamente.
#     -FechaBase  Fecha de inicio del historial (yyyy-MM-dd). Por defecto 2000-01-01.
# ============================================================================

[CmdletBinding()]
param(
    [string]$DbPath,
    [string]$ApiDir,
    [string]$FechaBase = '2000-01-01'
)

$ErrorActionPreference = 'Stop'

function Write-Paso($t) { Write-Host ""; Write-Host "== $t ==" -ForegroundColor Cyan }

# ---------------------------------------------------------------------------
# 1) Resolver la ruta de la base de datos (la MISMA que usa la API/escritorio)
# ---------------------------------------------------------------------------
Write-Paso "Ubicando la base de datos"

if (-not $DbPath) {
    $DbPath = [Environment]::GetEnvironmentVariable('SG_DB_PATH','Machine')
    if (-not $DbPath) { $DbPath = [Environment]::GetEnvironmentVariable('SG_DB_PATH') }
}

if (-not $DbPath) {
    # Buscar en ubicaciones habituales (incluye el AppData de cualquier cuenta,
    # que es donde suele quedar cuando la API corre como servicio).
    $candidatas = @()
    $raices = @(
        'C:\SistemaGestion',
        'C:\ProgramData\SistemaGestion',
        (Join-Path $env:APPDATA 'SistemaGestion')
    )
    Get-ChildItem 'C:\Users' -Directory -ErrorAction SilentlyContinue | ForEach-Object {
        $raices += (Join-Path $_.FullName 'AppData\Roaming\SistemaGestion')
    }
    foreach ($r in $raices) {
        $p = Join-Path $r 'sistema_gestion.db'
        if (Test-Path $p) { $candidatas += (Resolve-Path $p).Path }
    }
    $candidatas = $candidatas | Select-Object -Unique
    if     ($candidatas.Count -eq 1) { $DbPath = $candidatas[0] }
    elseif ($candidatas.Count -gt 1) {
        Write-Host "Se encontro mas de una base de datos. Vuelve a ejecutar indicando -DbPath con la correcta:" -ForegroundColor Yellow
        $candidatas | ForEach-Object { Write-Host "   $_" }
        exit 1
    }
}

if (-not $DbPath -or -not (Test-Path $DbPath)) {
    Write-Host "No se pudo ubicar sistema_gestion.db." -ForegroundColor Red
    Write-Host "Ejecuta de nuevo indicando la ruta exacta, por ejemplo:" -ForegroundColor Yellow
    Write-Host "   pwsh -File '<este_script>' -DbPath 'C:\ruta\sistema_gestion.db'"
    exit 1
}
$DbPath = (Resolve-Path $DbPath).Path
Write-Host "Base de datos: $DbPath"

# ---------------------------------------------------------------------------
# 2) Cargar las DLL de SQLite (Microsoft.Data.Sqlite) desde la API publicada
# ---------------------------------------------------------------------------
Write-Paso "Cargando el motor SQLite"

function Buscar-Dll($nombre, $dirs) {
    foreach ($d in $dirs) {
        if ($d -and (Test-Path $d)) {
            $hit = Get-ChildItem -Path $d -Recurse -Filter $nombre -ErrorAction SilentlyContinue |
                   Sort-Object LastWriteTime -Descending | Select-Object -First 1
            if ($hit) { return $hit.FullName }
        }
    }
    return $null
}

$dirsBusqueda = @()
if ($ApiDir) { $dirsBusqueda += $ApiDir }
$dirsBusqueda += @(
    (Join-Path $env:LOCALAPPDATA 'SistemaGestion\publish\Api'),   # olimpo: API publicada (ruta canonica)
    (Join-Path $env:LOCALAPPDATA 'SistemaGestion\publish'),
    'C:\SistemaGestion',
    (Join-Path $env:USERPROFILE '.nuget\packages\microsoft.data.sqlite.core'),
    (Join-Path $env:USERPROFILE '.nuget\packages')
)

$mds = Buscar-Dll 'Microsoft.Data.Sqlite.dll' $dirsBusqueda
if (-not $mds) {
    Write-Host "No se encontro Microsoft.Data.Sqlite.dll." -ForegroundColor Red
    Write-Host "Indica la carpeta de la API publicada con -ApiDir 'C:\ruta\a\la\api'" -ForegroundColor Yellow
    exit 1
}
$baseDll = Split-Path $mds -Parent

# Cargar dependencias en orden. Si estan junto a Microsoft.Data.Sqlite.dll las usa;
# si no, las busca en el arbol de la API / cache de NuGet.
foreach ($n in @('SQLitePCLRaw.core.dll','SQLitePCLRaw.provider.e_sqlite3.dll','SQLitePCLRaw.batteries_v2.dll')) {
    $ruta = Join-Path $baseDll $n
    if (-not (Test-Path $ruta)) { $ruta = Buscar-Dll $n $dirsBusqueda }
    if ($ruta) { Add-Type -Path $ruta -ErrorAction SilentlyContinue }
}
Add-Type -Path $mds

# Asegurar que la libreria nativa e_sqlite3.dll sea localizable
$nativa = Buscar-Dll 'e_sqlite3.dll' (@($baseDll) + $dirsBusqueda)
if ($nativa) {
    $dirNat = Split-Path $nativa -Parent
    if ($env:PATH -notlike "*$dirNat*") { $env:PATH = "$dirNat;$env:PATH" }
}
try { [SQLitePCL.Batteries_V2]::Init() } catch { }

Write-Host "Motor SQLite cargado desde: $baseDll"

# ---------------------------------------------------------------------------
# 3) Respaldo de la base de datos ANTES de tocar nada
# ---------------------------------------------------------------------------
Write-Paso "Creando respaldo"

$sello   = Get-Date -Format 'yyyyMMdd_HHmmss'
$respaldo = Join-Path (Split-Path $DbPath -Parent) ("sistema_gestion_backup_bonos_$sello.db")

$cn = New-Object Microsoft.Data.Sqlite.SqliteConnection("Data Source=$DbPath")
$cn.Open()
function EjecSql([string]$sql) {
    $c = $cn.CreateCommand(); $c.CommandText = $sql; return $c.ExecuteNonQuery()
}
function EscalarSql([string]$sql) {
    $c = $cn.CreateCommand(); $c.CommandText = $sql; return $c.ExecuteScalar()
}

[void](EjecSql "PRAGMA busy_timeout=8000;")
$respaldoSql = $respaldo.Replace("'","''")
[void](EjecSql "VACUUM INTO '$respaldoSql';")
Write-Host "Respaldo creado: $respaldo"

# ---------------------------------------------------------------------------
# 4) Aplicar las tarifas dentro de una transaccion
# ---------------------------------------------------------------------------
Write-Paso "Aplicando tarifas de bono por producto"

$prefijos = "'1','4','5','6','7','8'"   # prefijos afectados

$tx = $cn.BeginTransaction()
try {
    # 4.1  Columna vigente en Inventario (fallback y visualizacion)
    $u1 = EjecSql "UPDATE Inventario SET BonoProduccionBs=135, BonoVentaBs=15 WHERE substr(Codigo,1,1)='1';"
    $u4 = EjecSql "UPDATE Inventario SET BonoProduccionBs=30,  BonoVentaBs=5  WHERE substr(Codigo,1,1)='4';"
    $u5 = EjecSql "UPDATE Inventario SET BonoProduccionBs=200, BonoVentaBs=15 WHERE substr(Codigo,1,1) IN ('5','6');"
    $u7 = EjecSql "UPDATE Inventario SET BonoProduccionBs=100, BonoVentaBs=10 WHERE substr(Codigo,1,1) IN ('7','8');"

    # 4.2  Historial fechado de PRODUCCION
    #      a) corregir la tarifa de la fila abierta (si ya existe)
    [void](EjecSql @"
UPDATE TarifaProductoProduccion
   SET TarifaBs = (SELECT i.BonoProduccionBs FROM Inventario i WHERE i.Codigo = TarifaProductoProduccion.CodigoProducto)
 WHERE FechaHasta IS NULL
   AND CodigoProducto IN (SELECT Codigo FROM Inventario WHERE substr(Codigo,1,1) IN ($prefijos));
"@)
    #      b) crear fila abierta anclada a la fecha base para los que no la tienen
    [void](EjecSql @"
INSERT INTO TarifaProductoProduccion (CodigoProducto, TarifaBs, FechaDesde, FechaHasta, Usuario)
SELECT i.Codigo, i.BonoProduccionBs, '$FechaBase', NULL, 'Script-Bonos'
  FROM Inventario i
 WHERE substr(i.Codigo,1,1) IN ($prefijos)
   AND i.BonoProduccionBs > 0
   AND NOT EXISTS (SELECT 1 FROM TarifaProductoProduccion t WHERE t.CodigoProducto = i.Codigo AND t.FechaHasta IS NULL);
"@)

    # 4.3  Historial fechado de VENTA
    [void](EjecSql @"
UPDATE TarifaProductoVenta
   SET TarifaBs = (SELECT i.BonoVentaBs FROM Inventario i WHERE i.Codigo = TarifaProductoVenta.CodigoProducto)
 WHERE FechaHasta IS NULL
   AND CodigoProducto IN (SELECT Codigo FROM Inventario WHERE substr(Codigo,1,1) IN ($prefijos));
"@)
    [void](EjecSql @"
INSERT INTO TarifaProductoVenta (CodigoProducto, TarifaBs, FechaDesde, FechaHasta, Usuario)
SELECT i.Codigo, i.BonoVentaBs, '$FechaBase', NULL, 'Script-Bonos'
  FROM Inventario i
 WHERE substr(i.Codigo,1,1) IN ($prefijos)
   AND i.BonoVentaBs > 0
   AND NOT EXISTS (SELECT 1 FROM TarifaProductoVenta t WHERE t.CodigoProducto = i.Codigo AND t.FechaHasta IS NULL);
"@)

    # 4.4  Registro en auditoria
    $det = "Carga masiva de bonos por prefijo (1=135/15, 4=30/5, 5-6=200/15, 7-8=100/10 Bs)"
    $cAud = $cn.CreateCommand()
    $cAud.CommandText = "INSERT INTO LogAuditoria (FechaHora, Usuario, Accion, Tabla, Detalle) VALUES (datetime('now','localtime'),'Script-Bonos','BONO_PRODUCTO_CARGA_MASIVA','Inventario',@d);"
    [void]$cAud.Parameters.AddWithValue("@d", $det)
    [void]$cAud.ExecuteNonQuery()

    $tx.Commit()
    Write-Host ("Inventario actualizado -> prefijo 1: {0} | prefijo 4: {1} | prefijos 5-6: {2} | prefijos 7-8: {3}" -f $u1,$u4,$u5,$u7)
}
catch {
    $tx.Rollback()
    Write-Host "ERROR: se revirtieron los cambios. Detalle: $($_.Exception.Message)" -ForegroundColor Red
    $cn.Close()
    exit 1
}

# ---------------------------------------------------------------------------
# 5) Resumen de verificacion
# ---------------------------------------------------------------------------
Write-Paso "Resultado"

$cVer = $cn.CreateCommand()
$cVer.CommandText = @"
SELECT substr(Codigo,1,1) AS d,
       BonoProduccionBs,
       BonoVentaBs,
       COUNT(*) AS n
  FROM Inventario
 WHERE substr(Codigo,1,1) IN ($prefijos)
 GROUP BY d, BonoProduccionBs, BonoVentaBs
 ORDER BY d;
"@
$rd = $cVer.ExecuteReader()
Write-Host ("{0,-8} {1,14} {2,12} {3,8}" -f 'Prefijo','BonoProd(Bs)','BonoVenta(Bs)','Productos')
Write-Host ("-" * 46)
while ($rd.Read()) {
    Write-Host ("{0,-8} {1,14} {2,12} {3,8}" -f $rd['d'], $rd['BonoProduccionBs'], $rd['BonoVentaBs'], $rd['n'])
}
$rd.Close()
$cn.Close()

Write-Host ""
Write-Host "Listo. Bonos por producto cargados correctamente." -ForegroundColor Green
Write-Host "Respaldo previo disponible en: $respaldo"
