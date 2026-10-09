# ============================================================
#  arreglar-costo-resina.ps1  —  Corrige el costo de la resina (OLIMPO)
#  EJECUTAR EN OLIMPO, PowerShell 7:
#      & 'C:\SistemaGestion\arreglar-costo-resina.ps1'
#  Pone PrecioUltCompra = PrecioPromedio = CostoPromedio = 2.75 para las
#  dos resinas (INS-001 6200B, INS-002 2706). Muestra antes/despues y el
#  rastro de movimientos. Idempotente.
# ============================================================
$ErrorActionPreference='Stop'
$PRECIO = 2.75          # $/kg (cambia aqui si hiciera falta)
$CODIGOS = @('INS-001','INS-002')

$db=[Environment]::GetEnvironmentVariable('SG_DB_PATH','Machine'); if(-not $db){ $db='C:\SistemaGestion\sistema_gestion.db' }
Write-Host "BD: $db" -ForegroundColor Yellow
if(-not (Test-Path $db)){ Write-Host "  [ERROR] No existe la BD." -ForegroundColor Red; exit 1 }
$appDir='C:\SistemaGestion\publish\App'; Set-Location $appDir; $env:PATH="$appDir;$env:PATH"
foreach($d in 'SQLitePCLRaw.core.dll','SQLitePCLRaw.provider.e_sqlite3.dll','SQLitePCLRaw.batteries_v2.dll','Microsoft.Data.Sqlite.dll'){ $p=Join-Path $appDir $d; if(Test-Path $p){ Add-Type -Path $p } }
try{ [SQLitePCL.Batteries_V2]::Init() }catch{}
$cn=New-Object Microsoft.Data.Sqlite.SqliteConnection("Data Source=$db"); $cn.Open()
$pc=$cn.CreateCommand(); $pc.CommandText='PRAGMA busy_timeout=5000'; [void]$pc.ExecuteNonQuery()
function Show($t,$sql){ Write-Host ""; Write-Host "==== $t ====" -ForegroundColor Cyan
  $c=$cn.CreateCommand(); $c.CommandText=$sql; $r=$c.ExecuteReader(); $n=0
  while($r.Read()){ $n++; $f=@(); for($i=0;$i -lt $r.FieldCount;$i++){ $v = if($r.IsDBNull($i)){'NULL'}else{$r.GetValue($i)}; $f += ($r.GetName($i)+'='+$v) }; Write-Host ('  '+($f -join '  |  ')) }
  if($n -eq 0){ Write-Host "  (sin filas)" -ForegroundColor DarkGray }; $r.Close() }

$inList = "'" + ($CODIGOS -join "','") + "'"

Show "ANTES - insumos" "SELECT CodigoInterno, Descripcion, PrecioUltCompra, PrecioPromedio, COALESCE(CostoPromedio,0) AS CostoPromedio, StockActual FROM ProductosProveedor WHERE CodigoInterno IN ($inList)"
Show "RASTRO de movimientos (de donde salio el costo 0)" "SELECT FechaHora, CodigoProducto, TipoMovimiento, Cantidad, CostoUnitario, SUBSTR(COALESCE(Concepto,''),1,40) AS Concepto FROM MovimientosInventario WHERE CodigoProducto IN ($inList) ORDER BY FechaHora"

# --- Actualizar (parametro SQLite $p escapado con backtick para PowerShell) ---
$up=$cn.CreateCommand()
$up.CommandText = "UPDATE ProductosProveedor SET PrecioUltCompra=`$p, PrecioPromedio=`$p, CostoPromedio=`$p WHERE CodigoInterno IN ($inList)"
[void]$up.Parameters.AddWithValue('$p',[double]$PRECIO)
$filas=$up.ExecuteNonQuery()
Write-Host ""; Write-Host "Actualizados: $filas insumos a $PRECIO/kg" -ForegroundColor Green

Show "DESPUES - insumos" "SELECT CodigoInterno, Descripcion, PrecioUltCompra, PrecioPromedio, COALESCE(CostoPromedio,0) AS CostoPromedio, StockActual FROM ProductosProveedor WHERE CodigoInterno IN ($inList)"
$cn.Close()
Write-Host ""; Write-Host "Listo. El costo de la resina quedo en $PRECIO/kg en las dos columnas." -ForegroundColor Green
