# ============================================================
#  cargar-empleados.ps1  —  Carga inicial de empleados en OLIMPO
#  EJECUTAR EN OLIMPO, PowerShell 7:
#      & 'C:\SistemaGestion\cargar-empleados.ps1'
#  Escribe directo en la base local de OLIMPO. Idempotente:
#  si la cédula ya existe, la salta (no duplica).
# ============================================================
$ErrorActionPreference='Stop'
$appDir='C:\SistemaGestion\publish\App'
$db=[Environment]::GetEnvironmentVariable('SG_DB_PATH','Machine'); if(-not $db){ $db='C:\SistemaGestion\sistema_gestion.db' }
Write-Host "BD: $db" -ForegroundColor Yellow
if(-not (Test-Path $db)){ Write-Host "  [ERROR] No existe la BD." -ForegroundColor Red; exit 1 }
Set-Location $appDir; $env:PATH="$appDir;$env:PATH"
foreach($d in 'SQLitePCLRaw.core.dll','SQLitePCLRaw.provider.e_sqlite3.dll','SQLitePCLRaw.batteries_v2.dll','Microsoft.Data.Sqlite.dll'){ $p=Join-Path $appDir $d; if(Test-Path $p){ Add-Type -Path $p } }
try{ [SQLitePCL.Batteries_V2]::Init() }catch{}
$cn=New-Object Microsoft.Data.Sqlite.SqliteConnection("Data Source=$db"); $cn.Open()
$pc=$cn.CreateCommand(); $pc.CommandText='PRAGMA busy_timeout=4000'; [void]$pc.ExecuteNonQuery()

# Cédula sin puntos, con prefijo V- ; Nombre = primer bloque, Apellido = resto
$emps=@(
 @{ci='V-14636839';no='JOSÉ';ap='ROMÁN';te='Obrero';fi='2021-09-06'},
 @{ci='V-30081542';no='JEISON';ap='PAREDES';te='Obrero';fi='2025-08-18'},
 @{ci='V-16098601';no='JUAN';ap='SILVA';te='Obrero';fi='2026-05-11'},
 @{ci='V-13493225';no='OMAR JAVIER';ap='HERRERA';te='Obrero';fi='2026-08-03'},
 @{ci='V-2522009';no='RAMÓN';ap='MONTESINOS';te='Obrero';fi='2016-01-15'},
 @{ci='V-12926016';no='LENNY';ap='BANDRES';te='Oficina';fi='2015-01-19'}
)

$ins=0;$skip=0
foreach($e in $emps){
  $chk=$cn.CreateCommand(); $chk.CommandText='SELECT COUNT(*) FROM Empleados WHERE Cedula=$ci'; [void]$chk.Parameters.AddWithValue('$ci',$e.ci)
  if([int64]$chk.ExecuteScalar() -gt 0){ Write-Host "  = ya existe: $($e.ci) $($e.no) $($e.ap)" -ForegroundColor DarkYellow; $skip++; continue }
  $ep= if($e.te -eq 'Obrero'){1}else{0}
  try{
    $c=$cn.CreateCommand()
    $c.CommandText='INSERT INTO Empleados (Cedula,Nombre,Apellido,TipoEmpleado,FechaIngreso,Activo,FrecuenciaPago,SueldoBaseUSD,EligibleBonoProd,EligibleBonoVenta) VALUES ($ci,$no,$ap,$te,$fi,1,$fp,0,$ep,0)'
    [void]$c.Parameters.AddWithValue('$ci',$e.ci); [void]$c.Parameters.AddWithValue('$no',$e.no); [void]$c.Parameters.AddWithValue('$ap',$e.ap)
    [void]$c.Parameters.AddWithValue('$te',$e.te); [void]$c.Parameters.AddWithValue('$fi',$e.fi); [void]$c.Parameters.AddWithValue('$fp','Semanal'); [void]$c.Parameters.AddWithValue('$ep',$ep)
    [void]$c.ExecuteNonQuery()
    Write-Host "  + cargado: $($e.ci) $($e.no) $($e.ap) [$($e.te)]" -ForegroundColor Green; $ins++
  } catch { Write-Host "  ! error con $($e.ci): $($_.Exception.Message)" -ForegroundColor Red }
}
$tc=$cn.CreateCommand(); $tc.CommandText='SELECT COUNT(*) FROM Empleados'
Write-Host ""; Write-Host "Cargados: $ins | Ya existían: $skip | Total empleados: $([int64]$tc.ExecuteScalar())" -ForegroundColor Cyan
$cn.Close()
