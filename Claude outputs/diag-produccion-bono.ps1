# ============================================================
#  diag-produccion-bono.ps1  —  SOLO LECTURA
#  EJECUTAR EN OLIMPO, PowerShell 7:
#      & 'C:\SistemaGestion\diag-produccion-bono.ps1'
#  Muestra: productos y tarifas de bono (produccion y ventas),
#  obreros/oficina activos, y produccion ya registrada.
#  No modifica nada.
# ============================================================
$ErrorActionPreference='Stop'
function Titulo($t){ Write-Host ""; Write-Host "==== $t ====" -ForegroundColor Cyan }

$db=[Environment]::GetEnvironmentVariable('SG_DB_PATH','Machine')
if(-not $db){ $db=$env:SG_DB_PATH }
if(-not $db){ $db=Join-Path $env:APPDATA 'SistemaGestion\sistema_gestion.db' }
Write-Host "Base de datos: $db" -ForegroundColor Yellow
if(-not (Test-Path $db)){ Write-Host "  [ERROR] No existe esa BD." -ForegroundColor Red; exit 1 }

$appDir='C:\SistemaGestion\publish\App'
Set-Location $appDir; $env:PATH="$appDir;$env:PATH"
foreach($d in 'SQLitePCLRaw.core.dll','SQLitePCLRaw.provider.e_sqlite3.dll','SQLitePCLRaw.batteries_v2.dll','Microsoft.Data.Sqlite.dll'){ $p=Join-Path $appDir $d; if(Test-Path $p){ Add-Type -Path $p } }
try{ [SQLitePCL.Batteries_V2]::Init() }catch{}
$cn=New-Object Microsoft.Data.Sqlite.SqliteConnection("Data Source=$db;Mode=ReadOnly;Cache=Shared"); $cn.Open()

function Q($t,$sql){
  Titulo $t
  try{
    $c=$cn.CreateCommand(); $c.CommandText=$sql; $r=$c.ExecuteReader(); $n=0
    while($r.Read()){ $n++; $f=@()
      for($i=0;$i -lt $r.FieldCount;$i++){ $v = if($r.IsDBNull($i)){'NULL'}else{$r.GetValue($i)}; $f += ($r.GetName($i)+'='+$v) }
      Write-Host ('  ' + ($f -join '  |  ')) }
    if($n -eq 0){ Write-Host "  (sin filas)" -ForegroundColor DarkGray }
    $r.Close()
  }catch{ Write-Host ("  [error] "+$_.Exception.Message) -ForegroundColor Red }
}

Q "TARIFAS DE BONO PRODUCCION (por producto, vigentes)" @"
SELECT t.CodigoProducto, COALESCE(i.Descripcion,'(no en Inventario)') AS Desc, t.TarifaBs, t.FechaDesde, COALESCE(t.FechaHasta,'vigente') AS Hasta
FROM TarifaProductoProduccion t LEFT JOIN Inventario i ON i.Codigo=t.CodigoProducto
ORDER BY t.CodigoProducto, t.FechaDesde
"@

Q "BONO PRODUCCION alterno (Inventario.BonoProduccionBs <> 0)" @"
SELECT Codigo, Descripcion, BonoProduccionBs, COALESCE(Existencia,0) AS Existencia
FROM Inventario WHERE COALESCE(BonoProduccionBs,0) <> 0 ORDER BY Codigo
"@

Q "TARIFAS DE BONO VENTAS (por producto, vigentes)" @"
SELECT t.CodigoProducto, COALESCE(i.Descripcion,'(no en Inventario)') AS Desc, t.TarifaBs, t.FechaDesde, COALESCE(t.FechaHasta,'vigente') AS Hasta
FROM TarifaProductoVenta t LEFT JOIN Inventario i ON i.Codigo=t.CodigoProducto
ORDER BY t.CodigoProducto, t.FechaDesde
"@

Q "BONO VENTAS alterno (Inventario.BonoVentaBs <> 0)" @"
SELECT Codigo, Descripcion, BonoVentaBs FROM Inventario WHERE COALESCE(BonoVentaBs,0) <> 0 ORDER BY Codigo
"@

Q "PRODUCTOS que parecen SILLA / ESTRIBO en Inventario" @"
SELECT Codigo, Descripcion, COALESCE(Existencia,0) AS Existencia, COALESCE(BonoProduccionBs,0) AS BonoProd, COALESCE(BonoVentaBs,0) AS BonoVenta
FROM Inventario
WHERE lower(Descripcion) LIKE '%silla%' OR lower(Descripcion) LIKE '%estribo%'
   OR lower(Descripcion) LIKE '%fuste%' OR lower(Codigo) LIKE '%sill%' OR lower(Codigo) LIKE '%estr%'
ORDER BY Descripcion
"@

Q "OBREROS activos (elegibles bono produccion)" @"
SELECT Id, Cedula, Nombre||' '||Apellido AS Nombre, TipoEmpleado, FechaIngreso, COALESCE(EligibleBonoProd,0) AS BonoProd
FROM Empleados WHERE Activo=1 AND (TipoEmpleado='Obrero' OR COALESCE(EligibleBonoProd,0)=1)
ORDER BY FechaIngreso
"@

Q "OFICINA / elegibles BONO VENTAS activos" @"
SELECT Id, Cedula, Nombre||' '||Apellido AS Nombre, TipoEmpleado, FechaIngreso, COALESCE(EligibleBonoVenta,0) AS BonoVenta
FROM Empleados WHERE Activo=1 AND (TipoEmpleado='Oficina' OR COALESCE(EligibleBonoVenta,0)=1)
ORDER BY FechaIngreso
"@

Q "PRODUCCION YA REGISTRADA (RegistroProduccionDia)" "SELECT COUNT(*) AS Registros, COALESCE(MIN(Fecha),'-') AS Desde, COALESCE(MAX(Fecha),'-') AS Hasta FROM RegistroProduccionDia"
Q "DETALLE MOLDE existente" "SELECT COUNT(*) AS Filas FROM DetalleProduccionMolde"
Q "FACTURAS (para bono ventas)" "SELECT COUNT(*) AS Facturas, COALESCE(MIN(Fecha),'-') AS Desde, COALESCE(MAX(Fecha),'-') AS Hasta FROM Facturas WHERE COALESCE(Anulada,0)=0"

$cn.Close()
Write-Host ""
Write-Host "==== Fin del diagnostico (no se modifico nada) ====" -ForegroundColor Green
