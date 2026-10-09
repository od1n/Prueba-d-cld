# ============================================================
#  desbloquear-usuarios.ps1  —  Quita el bloqueo de login (OLIMPO o SERVO)
#  EJECUTAR en OLIMPO (recomendado) o SERVO, PowerShell 7:
#      & 'C:\SistemaGestion\desbloquear-usuarios.ps1'
#  Pone IntentosHoy=0 y BloqueadoHasta=NULL para TODOS los usuarios.
#  No cambia contraseñas. Muestra antes/después.
# ============================================================
$ErrorActionPreference='Stop'
$db=[Environment]::GetEnvironmentVariable('SG_DB_PATH','Machine'); if(-not $db){ $db='C:\SistemaGestion\sistema_gestion.db' }
Write-Host "BD: $db" -ForegroundColor Yellow
if(-not (Test-Path $db)){ Write-Host "  [ERROR] No existe la BD." -ForegroundColor Red; exit 1 }

# DLLs: en OLIMPO estan en publish\App; en SERVO en publish\Api
$dllDir=$null
foreach($d in 'C:\SistemaGestion\publish\App','C:\SistemaGestion\publish\Api'){
  if(Test-Path (Join-Path $d 'Microsoft.Data.Sqlite.dll')){ $dllDir=$d; break }
}
if(-not $dllDir){ Write-Host "  [ERROR] No encontre Microsoft.Data.Sqlite.dll en publish\App ni publish\Api." -ForegroundColor Red; exit 1 }
Set-Location $dllDir; $env:PATH="$dllDir;$env:PATH"
foreach($f in 'SQLitePCLRaw.core.dll','SQLitePCLRaw.provider.e_sqlite3.dll','SQLitePCLRaw.batteries_v2.dll','Microsoft.Data.Sqlite.dll'){ $p=Join-Path $dllDir $f; if(Test-Path $p){ Add-Type -Path $p } }
try{ [SQLitePCL.Batteries_V2]::Init() }catch{}
$cn=New-Object Microsoft.Data.Sqlite.SqliteConnection("Data Source=$db"); $cn.Open()
$pc=$cn.CreateCommand(); $pc.CommandText='PRAGMA busy_timeout=5000'; [void]$pc.ExecuteNonQuery()

function Show($t){ Write-Host ""; Write-Host "==== $t ====" -ForegroundColor Cyan
  $c=$cn.CreateCommand()
  $c.CommandText="SELECT NombreUsuario, COALESCE(IntentosHoy,0) AS Intentos, COALESCE(BloqueadoHasta,'-') AS BloqueadoHasta, Activo FROM Usuarios ORDER BY NombreUsuario"
  $r=$c.ExecuteReader()
  while($r.Read()){ Write-Host ("  {0,-14}  intentos={1}  bloqueado_hasta={2}  activo={3}" -f $r.GetString(0),$r.GetValue(1),$r.GetValue(2),$r.GetValue(3)) }
  $r.Close() }

Show "ANTES"
$up=$cn.CreateCommand(); $up.CommandText="UPDATE Usuarios SET IntentosHoy=0, BloqueadoHasta=NULL"
$n=$up.ExecuteNonQuery()
Write-Host ""; Write-Host "Usuarios desbloqueados: $n" -ForegroundColor Green
Show "DESPUES"
$cn.Close()
Write-Host ""; Write-Host "Listo. Intenta iniciar sesion de nuevo (con la contraseña correcta)." -ForegroundColor Green
