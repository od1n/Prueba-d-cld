# ============================================================
#  diag-moldes-mp.ps1  —  SOLO LECTURA
#  EJECUTAR EN OLIMPO, PowerShell 7:
#      & 'C:\SistemaGestion\diag-moldes-mp.ps1'
#  Muestra: moldes de silla/estribo, pesos/costo de los productos,
#  y catalogo de materia prima (resina). No modifica nada.
# ============================================================
$ErrorActionPreference='Stop'
function Titulo($t){ Write-Host ""; Write-Host "==== $t ====" -ForegroundColor Cyan }
$db=[Environment]::GetEnvironmentVariable('SG_DB_PATH','Machine'); if(-not $db){ $db=$env:SG_DB_PATH }
if(-not $db){ $db='C:\SistemaGestion\sistema_gestion.db' }
Write-Host "BD: $db" -ForegroundColor Yellow
if(-not (Test-Path $db)){ Write-Host "  [ERROR] No existe esa BD." -ForegroundColor Red; exit 1 }
$appDir='C:\SistemaGestion\publish\App'; Set-Location $appDir; $env:PATH="$appDir;$env:PATH"
foreach($d in 'SQLitePCLRaw.core.dll','SQLitePCLRaw.provider.e_sqlite3.dll','SQLitePCLRaw.batteries_v2.dll','Microsoft.Data.Sqlite.dll'){ $p=Join-Path $appDir $d; if(Test-Path $p){ Add-Type -Path $p } }
try{ [SQLitePCL.Batteries_V2]::Init() }catch{}
$cn=New-Object Microsoft.Data.Sqlite.SqliteConnection("Data Source=$db;Mode=ReadOnly;Cache=Shared"); $cn.Open()
function Q($t,$sql){ Titulo $t; try{ $c=$cn.CreateCommand(); $c.CommandText=$sql; $r=$c.ExecuteReader(); $n=0
  while($r.Read()){ $n++; $f=@(); for($i=0;$i -lt $r.FieldCount;$i++){ $v = if($r.IsDBNull($i)){'NULL'}else{$r.GetValue($i)}; $f += ($r.GetName($i)+'='+$v) }; Write-Host ('  '+($f -join '  |  ')) }
  if($n -eq 0){ Write-Host "  (sin filas)" -ForegroundColor DarkGray }; $r.Close() }catch{ Write-Host ("  [error] "+$_.Exception.Message) -ForegroundColor Red } }

Q "MOLDES que producen 101202 / 441100 / 441000" @"
SELECT mp.CodigoMolde, mp.CodPrimera, mp.CodSegunda, mp.CodReciclado
FROM MoldeProductos mp
WHERE mp.CodPrimera IN ('101202','441100','441000')
   OR mp.CodSegunda IN ('101202-2da','441100-2da','441000-2da')
ORDER BY mp.CodPrimera, mp.CodigoMolde
"@

Q "TODOS los moldes (por si los codigos difieren)" "SELECT CodigoMolde, CodPrimera, CodSegunda, CodReciclado FROM MoldeProductos ORDER BY CodPrimera, CodigoMolde"

Q "PESO y COSTO de los productos clave" @"
SELECT Codigo, Descripcion, COALESCE(PesoKg,0) AS PesoKg, COALESCE(CostoActual,0) AS Costo, COALESCE(Existencia,0) AS Existencia
FROM Inventario WHERE Codigo IN ('101202','101202-2da','101202-rec','441100','441100-2da','441100-rec','441000')
ORDER BY Codigo
"@

Q "MATERIA PRIMA (ProductosProveedor) — resina/PEAD y todo el catalogo" @"
SELECT CodigoInterno, Descripcion, COALESCE(StockActual,0) AS Stock, COALESCE(CostoPromedio,0) AS CostoProm
FROM ProductosProveedor ORDER BY Descripcion
"@

$cn.Close(); Write-Host ""; Write-Host "==== Fin (no se modifico nada) ====" -ForegroundColor Green
