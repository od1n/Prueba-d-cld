# ============================================================
#  borrar-egresos-prueba.ps1
#  Elimina los egresos de PRUEBA del sistema (proveedor "juancito",
#  concepto con la palabra "prueba"): el egreso, su detalle, su cuenta
#  por pagar con sus pagos y cualquier movimiento de Kardex ligado.
#
#  EJECUTAR EN OLIMPO, en PowerShell 7:
#   1) Revisar (no modifica nada):
#        & 'C:\SistemaGestion\Claude outputs\borrar-egresos-prueba.ps1'
#   2) Borrar (respalda la BD antes):
#        & 'C:\SistemaGestion\Claude outputs\borrar-egresos-prueba.ps1' -Aplicar
# ============================================================
param([switch]$Aplicar)
$ErrorActionPreference = 'Stop'

$db = [Environment]::GetEnvironmentVariable('SG_DB_PATH','Machine')
if (-not $db) { $db = 'C:\SistemaGestion\sistema_gestion.db' }
$appDir = 'C:\SistemaGestion\publish\App'
Set-Location $appDir; $env:PATH = "$appDir;$env:PATH"
foreach ($d in 'SQLitePCLRaw.core.dll','SQLitePCLRaw.provider.e_sqlite3.dll','SQLitePCLRaw.batteries_v2.dll','Microsoft.Data.Sqlite.dll') {
  $p = Join-Path $appDir $d; if (Test-Path $p) { Add-Type -Path $p } }
try { [SQLitePCL.Batteries_V2]::Init() } catch {}
Write-Host "Base de datos: $db" -ForegroundColor Yellow
Write-Host ("Modo: " + $(if ($Aplicar) {'APLICAR'} else {'REVISAR (solo lectura)'})) -ForegroundColor Yellow

$cs = if ($Aplicar) { "Data Source=$db" } else { "Data Source=$db;Mode=ReadOnly;Cache=Shared" }
$cn = New-Object Microsoft.Data.Sqlite.SqliteConnection($cs); $cn.Open()
$k = $cn.CreateCommand(); $k.CommandText = 'PRAGMA busy_timeout=8000'; [void]$k.ExecuteNonQuery()
$script:tx = $null
function Cmd([string]$sql, [hashtable]$prm) {
  $c = $cn.CreateCommand(); $c.CommandText = $sql; if ($script:tx) { $c.Transaction = $script:tx }
  if ($prm) { foreach ($n in $prm.Keys) { [void]$c.Parameters.AddWithValue($n, $prm[$n]) } }; return $c }
function Escalar([string]$sql, [hashtable]$prm) { (Cmd $sql $prm).ExecuteScalar() }
function Ejec([string]$sql, [hashtable]$prm) { (Cmd $sql $prm).ExecuteNonQuery() }

# Egresos de prueba: proveedor "juancito" y concepto con "prueba"
$r = (Cmd @"
SELECT Id, Fecha, Proveedor, Concepto, COALESCE(MontoUSD,0), COALESCE(Estado,'')
FROM Gastos
WHERE LOWER(TRIM(COALESCE(Proveedor,''))) = 'juancito'
  AND LOWER(COALESCE(Concepto,'')) LIKE '%prueba%'
ORDER BY Id
"@ $null).ExecuteReader()
$ids = @()
Write-Host ''; Write-Host '==== Egresos de prueba encontrados ====' -ForegroundColor Cyan
while ($r.Read()) {
  $ids += [long]$r.GetValue(0)
  Write-Host ("  Egreso #{0}  {1}  {2}  {3}  `${4:N2}  ({5})" -f $r.GetValue(0), $r.GetValue(1), $r.GetValue(2), $r.GetValue(3), [double]$r.GetValue(4), $r.GetValue(5))
}
$r.Close()
if ($ids.Count -eq 0) { Write-Host '  Ninguno (ya fueron borrados).' -ForegroundColor Green; $cn.Close(); exit 0 }

$lista = ($ids -join ',')
$nDet = Escalar "SELECT COUNT(*) FROM DetallesGasto WHERE IdGasto IN ($lista)" $null
$nCxP = 0; $nPag = 0
$tieneIdEgreso = [long](Escalar "SELECT COUNT(*) FROM pragma_table_info('CuentasPagar') WHERE name='IdEgreso'" $null) -gt 0
if ($tieneIdEgreso) {
  $nCxP = Escalar "SELECT COUNT(*) FROM CuentasPagar WHERE IdEgreso IN ($lista)" $null
  $nPag = Escalar "SELECT COUNT(*) FROM PagosCxP WHERE IdCuenta IN (SELECT Id FROM CuentasPagar WHERE IdEgreso IN ($lista))" $null }
$refs = ($ids | ForEach-Object { "'Gasto#$_','Anul-Gasto#$_'" }) -join ','
$nKx = Escalar "SELECT COUNT(*) FROM MovimientosInventario WHERE Referencia IN ($refs)" $null
Write-Host ("  Detalle: {0} líneas | Cuentas por pagar: {1} (pagos: {2}) | Movimientos de Kardex: {3}" -f $nDet, $nCxP, $nPag, $nKx)

if (-not $Aplicar) {
  Write-Host ''; Write-Host 'Nada fue modificado. Para borrar:' -ForegroundColor Green
  Write-Host "  & 'C:\SistemaGestion\Claude outputs\borrar-egresos-prueba.ps1' -Aplicar" -ForegroundColor Green
  $cn.Close(); exit 0 }

$resp = Join-Path (Split-Path $db) ("respaldo-antes-borrar-pruebas-{0}.db" -f (Get-Date -Format 'yyyyMMdd-HHmmss'))
[void](Ejec "VACUUM INTO '$($resp.Replace("'","''"))'" $null)
Write-Host "Respaldo: $resp" -ForegroundColor Green

$script:tx = $cn.BeginTransaction()
try {
  if ($tieneIdEgreso) {
    [void](Ejec "DELETE FROM PagosCxP WHERE IdCuenta IN (SELECT Id FROM CuentasPagar WHERE IdEgreso IN ($lista))" $null)
    [void](Ejec "DELETE FROM CuentasPagar WHERE IdEgreso IN ($lista)" $null) }
  [void](Ejec "DELETE FROM MovimientosInventario WHERE Referencia IN ($refs)" $null)
  [void](Ejec "DELETE FROM DetallesGasto WHERE IdGasto IN ($lista)" $null)
  $n = Ejec "DELETE FROM Gastos WHERE Id IN ($lista)" $null
  $script:tx.Commit()
  Write-Host "LISTO: $n egresos de prueba eliminados, con su detalle y cuentas por pagar." -ForegroundColor Green
}
catch { $script:tx.Rollback(); Write-Host "[ERROR] $($_.Exception.Message) — no se modificó nada." -ForegroundColor Red; exit 1 }
finally { $cn.Close() }
