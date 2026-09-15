# ============================================================
#  diag-produccion.ps1  —  Verifica qué hay cargado en la base
#  de datos: tarifas de bonos vs. datos de producción.
#
#  EJECUTAR EN OLIMPO (donde vive la base de datos), PowerShell 7:
#      Set-Location 'C:\SistemaGestion'
#      & 'C:\SistemaGestion\diag-produccion.ps1'
#
#  Es de SOLO LECTURA. No modifica nada.
# ============================================================
$ErrorActionPreference = 'Stop'
function Titulo($t) { Write-Host ""; Write-Host "==== $t ====" -ForegroundColor Cyan }

# ── 1) Localizar la base de datos ───────────────────────────────────
$db = [Environment]::GetEnvironmentVariable('SG_DB_PATH','Machine')
if (-not $db) { $db = $env:SG_DB_PATH }
if (-not $db) { $db = Join-Path $env:APPDATA 'SistemaGestion\sistema_gestion.db' }
Write-Host "Base de datos: $db" -ForegroundColor Yellow
if (-not (Test-Path $db)) { Write-Host "  [ERROR] No existe la BD en esa ruta." -ForegroundColor Red; exit 1 }

# ── 2) Cargar Microsoft.Data.Sqlite desde el publish de la app ──────
$appDir = 'C:\SistemaGestion\publish\App'
if (-not (Test-Path (Join-Path $appDir 'Microsoft.Data.Sqlite.dll'))) {
    Write-Host "  [ERROR] No encontré Microsoft.Data.Sqlite.dll en $appDir" -ForegroundColor Red
    Write-Host "  Ajusta la variable `$appDir a la carpeta donde está publicada la app." -ForegroundColor Red
    exit 1
}
Set-Location $appDir
$env:PATH = "$appDir;$env:PATH"
foreach ($dep in @('SQLitePCLRaw.core.dll','SQLitePCLRaw.provider.e_sqlite3.dll','SQLitePCLRaw.batteries_v2.dll','Microsoft.Data.Sqlite.dll')) {
    $p = Join-Path $appDir $dep
    if (Test-Path $p) { Add-Type -Path $p }
}
try { [SQLitePCL.Batteries_V2]::Init() } catch { }

$conn = New-Object Microsoft.Data.Sqlite.SqliteConnection("Data Source=$db;Mode=ReadOnly;Cache=Shared")
$conn.Open()
$pr = $conn.CreateCommand(); $pr.CommandText = "PRAGMA busy_timeout=3000"; [void]$pr.ExecuteNonQuery()

function Contar($tabla) {
    try {
        $cmd = $conn.CreateCommand(); $cmd.CommandText = "SELECT COUNT(*) FROM `"$tabla`""
        return [int64]$cmd.ExecuteScalar()
    } catch { return "-1 (no existe)" }
}
function Muestra($tabla, $limit = 5) {
    Titulo "Muestra de $tabla (primeras $limit filas)"
    try {
        $cmd = $conn.CreateCommand(); $cmd.CommandText = "SELECT * FROM `"$tabla`" LIMIT $limit"
        $r = $cmd.ExecuteReader(); $n = 0
        while ($r.Read()) {
            $n++; $campos = @()
            for ($i = 0; $i -lt $r.FieldCount; $i++) {
                $v = if ($r.IsDBNull($i)) { 'NULL' } else { $r.GetValue($i) }
                $campos += ($r.GetName($i) + '=' + $v)
            }
            Write-Host ('  ' + ($campos -join '  |  '))
        }
        if ($n -eq 0) { Write-Host "  (sin filas)" -ForegroundColor DarkGray }
        $r.Close()
    } catch { Write-Host ("  [no se pudo leer] " + $_.Exception.Message) -ForegroundColor Red }
}

# ── 3) Conteo de las tablas clave ───────────────────────────────────
Titulo "BONOS / TARIFAS (deberían tener datos)"
"{0,-28} {1}" -f "TarifasBonoProduccion:", (Contar "TarifasBonoProduccion") | Write-Host
"{0,-28} {1}" -f "TarifasBonoVentas:",     (Contar "TarifasBonoVentas")     | Write-Host

Titulo "PRODUCCIÓN (esto es lo que quieres verificar)"
"{0,-28} {1}" -f "RegistroProduccionDia:",      (Contar "RegistroProduccionDia")      | Write-Host
"{0,-28} {1}" -f "ProduccionDiaria:",           (Contar "ProduccionDiaria")           | Write-Host
"{0,-28} {1}" -f "ParticipantesProduccionDia:", (Contar "ParticipantesProduccionDia") | Write-Host
"{0,-28} {1}" -f "DetalleProduccionMolde:",     (Contar "DetalleProduccionMolde")     | Write-Host
"{0,-28} {1}" -f "MaterialProduccionDia:",      (Contar "MaterialProduccionDia")      | Write-Host

# ── 4) Todas las tablas con su conteo (por si algo se cargó en otra) ─
Titulo "TODAS LAS TABLAS Y SU CANTIDAD DE FILAS"
$cmd = $conn.CreateCommand()
$cmd.CommandText = "SELECT name FROM sqlite_master WHERE type='table' AND name NOT LIKE 'sqlite_%' ORDER BY name"
$rd = $cmd.ExecuteReader(); $tablas = @()
while ($rd.Read()) { $tablas += $rd.GetString(0) }
$rd.Close()
foreach ($t in $tablas) { "{0,-32} {1}" -f $t, (Contar $t) | Write-Host }

# ── 5) Muestras rápidas ─────────────────────────────────────────────
Muestra "TarifasBonoProduccion" 5
Muestra "RegistroProduccionDia" 5

$conn.Close()
Write-Host ""
Write-Host "==== Listo ====" -ForegroundColor Green
Write-Host "Si las tablas de PRODUCCIÓN salen en 0, significa que aún no cargaste data de producción." -ForegroundColor Green
