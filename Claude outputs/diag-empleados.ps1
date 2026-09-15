# ============================================================
#  diag-empleados.ps1  —  ¿Se guardaron empleados? ¿Dónde?
#  EJECUTAR EN OLIMPO, PowerShell 7:
#      Set-Location 'C:\SistemaGestion'
#      & 'C:\SistemaGestion\diag-empleados.ps1'
#  Solo lectura.
# ============================================================
$ErrorActionPreference = 'Stop'
function Titulo($t){ Write-Host ""; Write-Host "==== $t ====" -ForegroundColor Cyan }

$db = [Environment]::GetEnvironmentVariable('SG_DB_PATH','Machine')
if(-not $db){ $db = $env:SG_DB_PATH }
if(-not $db){ $db = Join-Path $env:APPDATA 'SistemaGestion\sistema_gestion.db' }
Write-Host "Base de datos (la del escritorio): $db" -ForegroundColor Yellow
if(-not (Test-Path $db)){ Write-Host "  [ERROR] No existe esa BD." -ForegroundColor Red; exit 1 }

$appDir = 'C:\SistemaGestion\publish\App'
Set-Location $appDir; $env:PATH = "$appDir;$env:PATH"
foreach($d in @('SQLitePCLRaw.core.dll','SQLitePCLRaw.provider.e_sqlite3.dll','SQLitePCLRaw.batteries_v2.dll','Microsoft.Data.Sqlite.dll')){
    $p = Join-Path $appDir $d; if(Test-Path $p){ Add-Type -Path $p }
}
try{ [SQLitePCL.Batteries_V2]::Init() }catch{}
$conn = New-Object Microsoft.Data.Sqlite.SqliteConnection("Data Source=$db;Mode=ReadOnly;Cache=Shared")
$conn.Open()

function Q($t,$sql){
    Titulo $t
    try{
        $c=$conn.CreateCommand(); $c.CommandText=$sql; $r=$c.ExecuteReader(); $n=0
        while($r.Read()){ $n++; $f=@()
            for($i=0;$i -lt $r.FieldCount;$i++){ $v = if($r.IsDBNull($i)){'NULL'}else{$r.GetValue($i)}; $f += ($r.GetName($i)+'='+$v) }
            Write-Host ('  ' + ($f -join '  |  ')) }
        if($n -eq 0){ Write-Host "  (sin filas)" -ForegroundColor DarkGray }
        $r.Close()
    }catch{ Write-Host ("  [error] "+$_.Exception.Message) -ForegroundColor Red }
}

Q "CUANTOS EMPLEADOS HAY"           "SELECT COUNT(*) AS TotalEmpleados FROM Empleados"
Q "ULTIMOS EMPLEADOS (si hay)"      "SELECT Id,Cedula,Nombre,Apellido,TipoEmpleado,FechaIngreso,Activo FROM Empleados ORDER BY Id DESC LIMIT 15"
Q "AUDITORIA de empleados"          "SELECT FechaHora,Usuario,Accion,Detalle FROM LogAuditoria WHERE Accion LIKE 'EMPLEADO%' ORDER BY Id DESC LIMIT 20"

$conn.Close()
Write-Host ""
Write-Host "==== Como leerlo ====" -ForegroundColor Green
Write-Host "  - Si TotalEmpleados > 0: SI se guardaron aqui (revisa por que no los ves en pantalla)." -ForegroundColor Green
Write-Host "  - Si TotalEmpleados = 0 y NO hay auditoria EMPLEADO_NUEVO: la web nunca llego a insertar (permiso/validacion/front-end)." -ForegroundColor Green
Write-Host "  - Si en el log de la API aparece 'BD conectada' con una RUTA DISTINTA a la de arriba: la API escribio en otro archivo (arreglamos la variable SG_DB_PATH)." -ForegroundColor Green
