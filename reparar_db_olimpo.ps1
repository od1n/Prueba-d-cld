# reparar_db_olimpo.ps1
# Ejecutar UNA SOLA VEZ en OLIMPO con la aplicacion cerrada.
# Agrega columnas faltantes a CategoriasGasto directamente en el SQLite DB.
# -----------------------------------------------------------------------

$appDir = "C:\Users\PC\AppData\Local\SistemaGestion\publish\App"
$dbDir  = "C:\Users\PC\AppData\Roaming\SistemaGestion"

Write-Host "=== Diagnostico + Reparacion DB OLIMPO ===" -ForegroundColor Cyan

# Buscar el archivo .db
$dbFile = Get-ChildItem "$dbDir\*.db" -ErrorAction SilentlyContinue |
          Sort-Object LastWriteTime -Descending | Select-Object -First 1
if (-not $dbFile) {
    Write-Host "ERROR: No se encontro archivo .db en $dbDir" -ForegroundColor Red
    exit 1
}
Write-Host "BD: $($dbFile.FullName)  ($([math]::Round($dbFile.Length/1KB,1)) KB)" -ForegroundColor Green

# Cargar SQLite via Microsoft.Data.Sqlite.dll del app publicado
$dll = "$appDir\Microsoft.Data.Sqlite.dll"
if (-not (Test-Path $dll)) {
    Write-Host "ERROR: No encontre $dll" -ForegroundColor Red
    Write-Host "Asegurate de que la aplicacion fue publicada al menos una vez en $appDir" -ForegroundColor Yellow
    exit 1
}
try { Add-Type -Path $dll } catch { <# ya cargada #> }

$conn = New-Object Microsoft.Data.Sqlite.SqliteConnection("Data Source=$($dbFile.FullName)")
$conn.Open()

function Exec-Scalar($sql) {
    $c = $conn.CreateCommand(); $c.CommandText = $sql
    return $c.ExecuteScalar()
}
function Get-Columns($table) {
    $c = $conn.CreateCommand(); $c.CommandText = "PRAGMA table_info(`"$table`")"
    $r = $c.ExecuteReader(); $cols = @()
    while ($r.Read()) { $cols += $r.GetString(1) }; $r.Close()
    return $cols
}
function Add-IfMissing($table, $col, $def) {
    $cols = Get-Columns $table
    if ($cols -contains $col) {
        Write-Host "  $col : ya existe" -ForegroundColor Gray
    } else {
        $c = $conn.CreateCommand()
        $c.CommandText = "ALTER TABLE `"$table`" ADD COLUMN $col $def"
        try {
            $c.ExecuteNonQuery() | Out-Null
            Write-Host "  $col : AGREGADO" -ForegroundColor Green
        } catch {
            Write-Host "  $col : ERROR — $_" -ForegroundColor Red
        }
    }
}

# --- Informacion general ---
$ver = Exec-Scalar "PRAGMA user_version"
Write-Host "`nuser_version actual = $ver" -ForegroundColor White

$types = @(); $cmd2 = $conn.CreateCommand()
$cmd2.CommandText = "SELECT type||': '||name FROM sqlite_master WHERE type IN ('table','view') ORDER BY type,name"
$r2 = $cmd2.ExecuteReader()
while ($r2.Read()) { $types += $r2.GetString(0) }; $r2.Close()
Write-Host "Objetos en DB: $($types -join ' | ')" -ForegroundColor White

# --- Columnas de CategoriasGasto ---
$catCols = Get-Columns "CategoriasGasto"
if ($catCols.Count -gt 0) {
    Write-Host "`nColumnas actuales de CategoriasGasto: $($catCols -join ', ')" -ForegroundColor White
    Write-Host "`nReparando columnas faltantes:" -ForegroundColor Cyan
    Add-IfMissing "CategoriasGasto" "Color"           "TEXT DEFAULT '#1A4A8A'"
    Add-IfMissing "CategoriasGasto" "Activa"          "INTEGER DEFAULT 1"
    Add-IfMissing "CategoriasGasto" "Orden"           "INTEGER DEFAULT 0"
    Add-IfMissing "CategoriasGasto" "EsMateriaPrima"  "INTEGER DEFAULT 0"
    Write-Host "`nColumnas tras reparacion: $((Get-Columns 'CategoriasGasto') -join ', ')" -ForegroundColor Green
} else {
    Write-Host "`nCategoriasGasto no existe — sera creada por la migracion con schema correcto." -ForegroundColor Yellow
}

$conn.Close()
Write-Host "`n=== Reparacion completa. Ahora inicia la aplicacion normalmente. ===" -ForegroundColor Cyan
