# ============================================================
#  cargar-produccion-2026.ps1  —  Carga de produccion semanal 2026 (OLIMPO)
#  EJECUTAR EN OLIMPO, PowerShell 7:
#      & 'C:\SistemaGestion\cargar-produccion-2026.ps1'
#
#  - 1 registro de produccion por semana (fecha = cierre).
#  - Sillas -> producto 101202 ; Estribos -> producto 441000.
#  - Participantes = obreros activos elegibles con fecha de ingreso <= cierre
#    (cada obrero cobra el total de la semana).
#  - NO mueve inventario, NI materia prima, NI kardex.
#  - Idempotente: si ya existe registro para esa fecha, la salta.
#  - Todo en una transaccion. Al final muestra el bono calculado por obrero.
# ============================================================
$ErrorActionPreference='Stop'
$MARCA='Carga historica agregada semanal 2026'
$COD_SILLA='101202'
$COD_ESTRIBO='441000'

$db=[Environment]::GetEnvironmentVariable('SG_DB_PATH','Machine'); if(-not $db){ $db='C:\SistemaGestion\sistema_gestion.db' }
Write-Host "BD: $db" -ForegroundColor Yellow
if(-not (Test-Path $db)){ Write-Host "  [ERROR] No existe la BD." -ForegroundColor Red; exit 1 }

$appDir='C:\SistemaGestion\publish\App'
Set-Location $appDir; $env:PATH="$appDir;$env:PATH"
foreach($d in 'SQLitePCLRaw.core.dll','SQLitePCLRaw.provider.e_sqlite3.dll','SQLitePCLRaw.batteries_v2.dll','Microsoft.Data.Sqlite.dll'){ $p=Join-Path $appDir $d; if(Test-Path $p){ Add-Type -Path $p } }
try{ [SQLitePCL.Batteries_V2]::Init() }catch{}
$cn=New-Object Microsoft.Data.Sqlite.SqliteConnection("Data Source=$db"); $cn.Open()
$pc=$cn.CreateCommand(); $pc.CommandText='PRAGMA busy_timeout=6000'; [void]$pc.ExecuteNonQuery()

function ScalarLit($sql){ $c=$cn.CreateCommand(); $c.CommandText=$sql; return $c.ExecuteScalar() }
function HasCol($tabla,$col){ $c=$cn.CreateCommand(); $c.CommandText="PRAGMA table_info($tabla)"; $r=$c.ExecuteReader(); $ok=$false; while($r.Read()){ if($r.GetString(1) -eq $col){$ok=$true} }; $r.Close(); return $ok }

# --- Validar productos/tarifas ---
foreach($cod in @($COD_SILLA,$COD_ESTRIBO)){
  $n=[int64](ScalarLit "SELECT COUNT(*) FROM Inventario WHERE Codigo='$cod'")
  $t=[int64](ScalarLit "SELECT COUNT(*) FROM TarifaProductoProduccion WHERE CodigoProducto='$cod'")
  Write-Host ("  producto {0}: en Inventario={1}, tarifas produccion={2}" -f $cod,$n,$t) -ForegroundColor DarkCyan
  if($n -eq 0){ Write-Host "  [ERROR] El producto $cod no existe en Inventario. Abortado." -ForegroundColor Red; $cn.Close(); exit 1 }
}
$regTieneTipoMat = HasCol 'RegistroProduccionDia' 'TipoMaterial'

# --- Obreros elegibles (activos) con su fecha de ingreso ---
$obreros=@()
$oc=$cn.CreateCommand(); $oc.CommandText="SELECT Id, FechaIngreso, Nombre||' '||Apellido FROM Empleados WHERE Activo=1 AND TipoEmpleado='Obrero' AND COALESCE(EligibleBonoProd,0)=1"
$or=$oc.ExecuteReader(); while($or.Read()){ $obreros += [pscustomobject]@{ Id=$or.GetInt32(0); Ingreso=$or.GetString(1); Nombre=$or.GetString(2) } }; $or.Close()
Write-Host "  Obreros elegibles: $($obreros.Count)" -ForegroundColor DarkCyan
foreach($ob in $obreros){ Write-Host ("     - id {0}  ingreso {1}  {2}" -f $ob.Id,$ob.Ingreso,$ob.Nombre) -ForegroundColor DarkGray }
if($obreros.Count -eq 0){ Write-Host "  [ERROR] No hay obreros elegibles. Abortado." -ForegroundColor Red; $cn.Close(); exit 1 }

# --- Data semanal 2026 (cierre yyyy-MM-dd ; sillas ; estribos) ---
$sem=@(
 @{f='2026-01-09';s=237;e=59}, @{f='2026-01-16';s=250;e=55}, @{f='2026-01-23';s=271;e=68}, @{f='2026-01-30';s=275;e=69},
 @{f='2026-02-06';s=77;e=0},   @{f='2026-02-13';s=93;e=0},   @{f='2026-02-20';s=100;e=0},  @{f='2026-02-27';s=330;e=65},
 @{f='2026-03-06';s=320;e=64}, @{f='2026-03-13';s=200;e=40}, @{f='2026-03-20';s=114;e=28}, @{f='2026-03-27';s=0;e=0},
 @{f='2026-04-01';s=80;e=8},   @{f='2026-04-10';s=94;e=0},   @{f='2026-04-17';s=280;e=64}, @{f='2026-04-24';s=248;e=114},
 @{f='2026-04-30';s=204;e=47}, @{f='2026-05-08';s=230;e=41}, @{f='2026-05-15';s=233;e=53}, @{f='2026-05-22';s=301;e=113},
 @{f='2026-05-29';s=290;e=110},@{f='2026-06-05';s=275;e=130},@{f='2026-06-12';s=232;e=103},@{f='2026-06-19';s=398;e=152},
 @{f='2026-06-26';s=297;e=92}, @{f='2026-07-03';s=245;e=85}, @{f='2026-07-10';s=319;e=117},@{f='2026-07-17';s=260;e=100},
 @{f='2026-07-24';s=236;e=112},@{f='2026-07-31';s=290;e=100},@{f='2026-08-07';s=295;e=139},@{f='2026-08-14';s=340;e=163},
 @{f='2026-08-21';s=342;e=163},@{f='2026-08-28';s=152;e=58}, @{f='2026-09-04';s=276;e=0},  @{f='2026-09-11';s=340;e=0}
)

# Prepara comandos parametrizados (SQL en comillas SIMPLES: los $x son parametros de SQLite, no de PowerShell)
$sqlHeadTM = 'INSERT INTO RegistroProduccionDia (Fecha,TipoMaterial,Observacion) VALUES ($f,''Virgen'',$o)'
$sqlHead   = 'INSERT INTO RegistroProduccionDia (Fecha,Observacion) VALUES ($f,$o)'
$sqlDet    = 'INSERT INTO DetalleProduccionMolde (IdRegistro,CodigoMolde,CodigoProducto,Cantidad) VALUES ($ir,$cm,$cp,$ca)'
$sqlPart   = 'INSERT INTO ParticipantesProduccionDia (IdRegistro,IdEmpleado) VALUES ($ir,$ie)'

$ins=0;$skip=0;$vacias=0;$detalles=0;$parts=0
$tx=$cn.BeginTransaction()
try{
  foreach($w in $sem){
    if($w.s -le 0 -and $w.e -le 0){ $vacias++; continue }
    $exc=$cn.CreateCommand(); $exc.Transaction=$tx; $exc.CommandText="SELECT COUNT(*) FROM RegistroProduccionDia WHERE Fecha='$($w.f)'"
    $ya=[int64]$exc.ExecuteScalar()
    if($ya -gt 0){ Write-Host "  = ya existe registro para $($w.f), se salta" -ForegroundColor DarkYellow; $skip++; continue }

    # Cabecera
    $hc=$cn.CreateCommand(); $hc.Transaction=$tx
    $hc.CommandText = if($regTieneTipoMat){ $sqlHeadTM } else { $sqlHead }
    [void]$hc.Parameters.AddWithValue('$f',$w.f); [void]$hc.Parameters.AddWithValue('$o',$MARCA)
    [void]$hc.ExecuteNonQuery()
    $idc=$cn.CreateCommand(); $idc.Transaction=$tx; $idc.CommandText='SELECT last_insert_rowid()'
    $regId=[int64]$idc.ExecuteScalar()

    # Detalle silla / estribo
    foreach($det in @(@{cod=$COD_SILLA;mol='AGREGADO-SILLA';cant=$w.s}, @{cod=$COD_ESTRIBO;mol='AGREGADO-ESTRIBO';cant=$w.e})){
      if($det.cant -le 0){ continue }
      $dc=$cn.CreateCommand(); $dc.Transaction=$tx; $dc.CommandText=$sqlDet
      [void]$dc.Parameters.AddWithValue('$ir',$regId); [void]$dc.Parameters.AddWithValue('$cm',$det.mol)
      [void]$dc.Parameters.AddWithValue('$cp',$det.cod); [void]$dc.Parameters.AddWithValue('$ca',[double]$det.cant)
      [void]$dc.ExecuteNonQuery(); $detalles++
    }

    # Participantes: obreros ingresados a mas tardar en el cierre de la semana
    foreach($ob in $obreros){
      if($ob.Ingreso -le $w.f){
        $ppc=$cn.CreateCommand(); $ppc.Transaction=$tx; $ppc.CommandText=$sqlPart
        [void]$ppc.Parameters.AddWithValue('$ir',$regId); [void]$ppc.Parameters.AddWithValue('$ie',$ob.Id)
        [void]$ppc.ExecuteNonQuery(); $parts++
      }
    }
    Write-Host ("  + {0}: sillas={1} estribos={2}" -f $w.f,$w.s,$w.e) -ForegroundColor Green; $ins++
  }
  $tx.Commit()
} catch { $tx.Rollback(); Write-Host "  [ERROR] $($_.Exception.Message) - se revirtio todo." -ForegroundColor Red; $cn.Close(); exit 1 }

Write-Host ""
Write-Host ("Semanas cargadas: {0} | ya existian: {1} | vacias (sin produccion): {2}" -f $ins,$skip,$vacias) -ForegroundColor Cyan
Write-Host ("Detalles insertados: {0} | participantes: {1}" -f $detalles,$parts) -ForegroundColor Cyan

# --- Verificacion: bono de produccion por obrero (misma formula que la nomina) ---
Write-Host ""
Write-Host "==== BONO DE PRODUCCION CALCULADO (Bs, todo 2026 cargado) ====" -ForegroundColor Cyan
$vc=$cn.CreateCommand()
$vc.CommandText=@'
SELECT e.Nombre||' '||e.Apellido AS Obrero,
       SUM(d.Cantidad * COALESCE(t.TarifaBs, i.BonoProduccionBs, 0)) AS BonoBs
FROM DetalleProduccionMolde d
JOIN RegistroProduccionDia r ON r.Id=d.IdRegistro
JOIN ParticipantesProduccionDia pp ON pp.IdRegistro=r.Id
JOIN Empleados e ON e.Id=pp.IdEmpleado
LEFT JOIN Inventario i ON i.Codigo=d.CodigoProducto
LEFT JOIN TarifaProductoProduccion t ON t.CodigoProducto=d.CodigoProducto
     AND r.Fecha>=t.FechaDesde AND (t.FechaHasta IS NULL OR r.Fecha<=t.FechaHasta)
WHERE r.Observacion=$marca
GROUP BY pp.IdEmpleado ORDER BY BonoBs DESC
'@
[void]$vc.Parameters.AddWithValue('$marca',$MARCA)
$vr=$vc.ExecuteReader()
while($vr.Read()){ Write-Host ("  {0,-24} {1,15:N2} Bs" -f $vr.GetString(0), [double]$vr.GetDouble(1)) }
$vr.Close()
$cn.Close()
Write-Host ""
Write-Host "Listo. La produccion quedo cargada; la nomina calculara estos bonos por semana." -ForegroundColor Green
