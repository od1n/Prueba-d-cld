# ============================================================
#  diag-produccion-inventario.ps1
#  Diagnóstico (SOLO LECTURA) para planificar la carga de la
#  producción 2026: productos y pesos, ventas 2026 por producto,
#  Kardex de producto terminado, producción registrada y moldes.
#
#  EJECUTAR EN OLIMPO, en PowerShell 7:
#      & 'C:\SistemaGestion\Claude outputs\diag-produccion-inventario.ps1'
#
#  No modifica nada: abre la base de datos en solo lectura.
# ============================================================
$ErrorActionPreference = 'Stop'
$db = [Environment]::GetEnvironmentVariable('SG_DB_PATH','Machine')
if (-not $db) { $db = 'C:\SistemaGestion\sistema_gestion.db' }
$appDir = 'C:\SistemaGestion\publish\App'
Set-Location $appDir; $env:PATH = "$appDir;$env:PATH"
foreach ($d in 'SQLitePCLRaw.core.dll','SQLitePCLRaw.provider.e_sqlite3.dll','SQLitePCLRaw.batteries_v2.dll','Microsoft.Data.Sqlite.dll') {
  $p = Join-Path $appDir $d; if (Test-Path $p) { Add-Type -Path $p } }
try { [SQLitePCL.Batteries_V2]::Init() } catch {}
$cn = New-Object Microsoft.Data.Sqlite.SqliteConnection("Data Source=$db;Mode=ReadOnly;Cache=Shared"); $cn.Open()
$k = $cn.CreateCommand(); $k.CommandText = 'PRAGMA busy_timeout=5000'; [void]$k.ExecuteNonQuery()
Write-Host "Base de datos: $db" -ForegroundColor Yellow

function Q($titulo, $sql) {
  Write-Host ''; Write-Host "==== $titulo ====" -ForegroundColor Cyan
  try {
    $c = $cn.CreateCommand(); $c.CommandText = $sql; $r = $c.ExecuteReader()
    $filas = New-Object System.Collections.Generic.List[object]
    while ($r.Read()) { $o = [ordered]@{}; for ($i=0; $i -lt $r.FieldCount; $i++) { $o[$r.GetName($i)] = if ($r.IsDBNull($i)) { '' } else { $r.GetValue($i) } }; $filas.Add([pscustomobject]$o) }
    $r.Close()
    if ($filas.Count -eq 0) { Write-Host '  (sin filas)' } else { $filas | Format-Table -AutoSize | Out-String -Width 250 | Write-Host }
  } catch { Write-Host "  [ERROR] $($_.Exception.Message)" -ForegroundColor Red }
}

Q 'Formato de fechas en Facturas (muestra)' "SELECT MIN(Fecha) AS Primera, MAX(Fecha) AS Ultima, COUNT(*) AS Facturas FROM Facturas"

Q 'Ventas 2026 por producto (facturas no anuladas)' @"
SELECT d.Codigo, COALESCE(i.Descripcion,'(no está en Inventario)') AS Descripcion, COALESCE(i.Departamento,'') AS Depto,
       ROUND(SUM(d.Cantidad),2) AS Vendido, COUNT(DISTINCT f.NumeroF) AS Facturas,
       MIN(f.Fecha) AS Primera, MAX(f.Fecha) AS Ultima,
       COALESCE(i.PesoKg,0) AS PesoKg, ROUND(SUM(d.Cantidad)*COALESCE(i.PesoKg,0),1) AS KgResina,
       COALESCE(i.Existencia,0) AS ExistSistema
FROM DetallesFactura d JOIN Facturas f ON f.rowid=d.IdFactura
LEFT JOIN Inventario i ON i.Codigo=d.Codigo
WHERE f.Fecha >= '2026-01-01' AND COALESCE(f.Anulada,0)=0
GROUP BY d.Codigo ORDER BY Vendido DESC
"@

Q 'Ventas 2026 por mes (unidades)' @"
SELECT substr(f.Fecha,1,7) AS Mes, COUNT(DISTINCT f.NumeroF) AS Facturas, ROUND(SUM(d.Cantidad),0) AS Unidades
FROM DetallesFactura d JOIN Facturas f ON f.rowid=d.IdFactura
WHERE f.Fecha >= '2026-01-01' AND COALESCE(f.Anulada,0)=0 GROUP BY Mes ORDER BY Mes
"@

Q 'Productos del inventario (activos o con existencia)' @"
SELECT Codigo, Descripcion, COALESCE(Departamento,'') AS Depto, COALESCE(PesoKg,0) AS PesoKg,
       COALESCE(Existencia,0) AS Existencia, COALESCE(CostoActual,0) AS Costo, COALESCE(Precio,0) AS Precio
FROM Inventario WHERE COALESCE(Activo,1)=1 OR COALESCE(Existencia,0)<>0 ORDER BY Departamento, Codigo
"@

Q 'Kardex de producto terminado 2026, por producto y concepto' @"
SELECT CodigoProducto AS Codigo, TipoMovimiento AS Tipo,
       CASE WHEN Concepto LIKE 'Venta%' THEN 'Venta' WHEN Concepto LIKE 'Produccion%' OR Concepto LIKE 'Producción%' THEN 'Produccion'
            WHEN Concepto LIKE 'Compra%' THEN 'Compra' ELSE substr(COALESCE(Concepto,''),1,25) END AS Concepto,
       COUNT(*) AS Movs, ROUND(SUM(Cantidad),2) AS Cantidad, MIN(FechaHora) AS Desde, MAX(FechaHora) AS Hasta
FROM MovimientosInventario
WHERE COALESCE(TipoKardex,'PRODUCTO')='PRODUCTO' AND FechaHora >= '2026-01-01'
GROUP BY 1,2,3 ORDER BY 1,2,3
"@

Q 'Producción registrada 2026 por mes y producto' @"
SELECT substr(r.Fecha,1,7) AS Mes,
       CASE WHEN r.Observacion LIKE 'Carga historica agregada%' THEN 'agregada semanal' ELSE 'diaria' END AS Origen,
       d.CodigoProducto, COALESCE(i.Descripcion,'') AS Descripcion, COUNT(DISTINCT r.Id) AS Registros, ROUND(SUM(d.Cantidad),0) AS Cantidad
FROM RegistroProduccionDia r JOIN DetalleProduccionMolde d ON d.IdRegistro=r.Id
LEFT JOIN Inventario i ON i.Codigo=d.CodigoProducto
WHERE r.Fecha >= '2026-01-01' GROUP BY 1,2,3 ORDER BY 1,2,3
"@

Q 'Producción diaria (no agregada) con detalle por molde' @"
SELECT r.Id, r.Fecha, COALESCE(r.TipoMaterial,'') AS Material, d.CodigoMolde, d.CodigoProducto,
       d.Cantidad, COALESCE(d.CantPrimera,0) AS Primera, COALESCE(d.CantSegunda,0) AS Segunda,
       COALESCE(d.CantReciclado,0) AS Reciclado, COALESCE(d.CantDefectuosas,0) AS Defect
FROM RegistroProduccionDia r JOIN DetalleProduccionMolde d ON d.IdRegistro=r.Id
WHERE r.Fecha >= '2026-01-01' AND COALESCE(r.Observacion,'') NOT LIKE 'Carga historica agregada%'
ORDER BY r.Fecha LIMIT 80
"@

Q 'Material consumido registrado en producción' @"
SELECT m.CodigoInsumo, COUNT(*) AS Registros, ROUND(SUM(m.Cantidad),1) AS Kg, MIN(r.Fecha) AS Desde, MAX(r.Fecha) AS Hasta
FROM MaterialProduccionDia m JOIN RegistroProduccionDia r ON r.Id=m.IdRegistro GROUP BY m.CodigoInsumo
"@

Q 'Moldes y productos que producen' @"
SELECT mp.CodigoMolde, COALESCE(t.Descripcion,'') AS Molde, COALESCE(t.CantMatXunid,0) AS KgMatXUnid, mp.CodPrimera, i1.Descripcion AS Primera, COALESCE(i1.PesoKg,0) AS PesoKg,
       mp.CodSegunda, mp.CodReciclado
FROM MoldeProductos mp LEFT JOIN TbMoldes t ON t.CodigoMold=mp.CodigoMolde LEFT JOIN Inventario i1 ON i1.Codigo=mp.CodPrimera
ORDER BY mp.CodigoMolde
"@

Q 'Moldes registrados (TbMoldes)' "SELECT CodigoMold, Descripcion, COALESCE(ModeloQfabrica,'') AS Fabrica, COALESCE(CantMatXunid,0) AS KgMatXUnid FROM TbMoldes ORDER BY CodigoMold"

Q 'Insumos de materia prima' "SELECT CodigoInterno, Descripcion, TipoInsumo, Unidad, ROUND(StockActual,1) AS Stock, CostoPromedio FROM ProductosProveedor ORDER BY CodigoInterno"

$cn.Close()
Write-Host ''; Write-Host 'Fin del diagnóstico. No se modificó nada.' -ForegroundColor Green
