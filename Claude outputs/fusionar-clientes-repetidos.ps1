# ============================================================
#  fusionar-clientes-repetidos.ps1
#  Une los clientes repetidos en uno solo.
#
#  Dos clientes son el mismo si tienen la misma cédula/RIF, sin importar
#  la letra V que falte, los puntos, guiones o espacios, ni el dígito
#  verificador del RIF:  "10 100 000" = "V10100000" = "V-10.100.000",
#  y "V-23601914-0" = "23.601.914".
#
#  Por cada grupo queda el cliente con más facturas (si empatan, el de
#  código más bajo). A ese cliente se pasan las facturas, abonos, cuentas
#  por cobrar y retenciones de los demás; se completan sus datos vacíos
#  (teléfono, dirección, etc.) con los de los repetidos, y los repetidos
#  se borran. Además deja todas las cédulas/RIF en formato estándar
#  (V-10100000, J-12345678-9).
#
#  Con -FusionarPorNombre también une clientes SIN cédula que tienen
#  exactamente el mismo nombre (p. ej. dos "PEDRO GOMEZ (BARINAS)").
#
#  EJECUTAR EN OLIMPO, en PowerShell 7:
#   1) Revisar (no modifica nada):
#        & 'C:\SistemaGestion\Claude outputs\fusionar-clientes-repetidos.ps1'
#   2) Aplicar (respalda la BD y guarda un registro de lo que unió):
#        & 'C:\SistemaGestion\Claude outputs\fusionar-clientes-repetidos.ps1' -Aplicar
#        & 'C:\SistemaGestion\Claude outputs\fusionar-clientes-repetidos.ps1' -Aplicar -FusionarPorNombre
# ============================================================
param([switch]$Aplicar, [switch]$FusionarPorNombre)
$ErrorActionPreference = 'Stop'

$db = [Environment]::GetEnvironmentVariable('SG_DB_PATH','Machine')
if (-not $db) { $db = 'C:\SistemaGestion\sistema_gestion.db' }
$appDir = 'C:\SistemaGestion\publish\App'
Set-Location $appDir; $env:PATH = "$appDir;$env:PATH"
foreach ($d in 'SQLitePCLRaw.core.dll','SQLitePCLRaw.provider.e_sqlite3.dll','SQLitePCLRaw.batteries_v2.dll','Microsoft.Data.Sqlite.dll') {
  $p = Join-Path $appDir $d; if (Test-Path $p) { Add-Type -Path $p } }
try { [SQLitePCL.Batteries_V2]::Init() } catch {}
Write-Host "Base de datos: $db" -ForegroundColor Yellow
Write-Host ("Modo: " + $(if ($Aplicar) {'APLICAR'} else {'REVISAR (solo lectura)'}) + $(if ($FusionarPorNombre) {' + unir sin cédula por nombre'} else {''})) -ForegroundColor Yellow

$cs = if ($Aplicar) { "Data Source=$db" } else { "Data Source=$db;Mode=ReadOnly;Cache=Shared" }
$cn = New-Object Microsoft.Data.Sqlite.SqliteConnection($cs); $cn.Open()
$k = $cn.CreateCommand(); $k.CommandText = 'PRAGMA busy_timeout=8000'; [void]$k.ExecuteNonQuery()
$script:tx = $null
function Cmd([string]$sql, [hashtable]$prm) {
  $c = $cn.CreateCommand(); $c.CommandText = $sql; if ($script:tx) { $c.Transaction = $script:tx }
  if ($prm) { foreach ($n in $prm.Keys) { $v = $prm[$n]; if ($null -eq $v) { $v = [DBNull]::Value }; [void]$c.Parameters.AddWithValue($n, $v) } }; return $c }
function Filas([string]$sql, [hashtable]$prm) {
  $r = (Cmd $sql $prm).ExecuteReader(); $out = New-Object System.Collections.Generic.List[object]
  while ($r.Read()) { $o = [ordered]@{}; for ($i=0; $i -lt $r.FieldCount; $i++) { $o[$r.GetName($i)] = if ($r.IsDBNull($i)) { $null } else { $r.GetValue($i) } }; $out.Add([pscustomobject]$o) }
  $r.Close(); return ,$out }
function Ejec([string]$sql, [hashtable]$prm) { (Cmd $sql $prm).ExecuteNonQuery() }

# ── Misma regla que ClienteDocumento.cs ────────────────────────────────
function Separar([string]$t) {
  if ([string]::IsNullOrWhiteSpace($t)) { return $null }
  $s = ($t.Trim().ToUpperInvariant() -replace '[\s\.\-,_/]', '')
  if ($s.Length -eq 0) { return $null }
  $letra = 'V'
  if ([char]::IsLetter($s[0])) { if ('VEJGCP'.IndexOf($s[0]) -lt 0) { return $null }; $letra = [string]$s[0]; $s = $s.Substring(1) }
  if ($s.Length -eq 0 -or $s -notmatch '^\d+$') { return $null }
  return @{ L = $letra; D = $s }
}
function Normalizar([string]$t) {
  if ([string]::IsNullOrWhiteSpace($t)) { return '' }
  $p = Separar $t; if (-not $p) { return $t.Trim().ToUpperInvariant() }
  if ('VEJGC'.Contains($p.L) -and $p.D.Length -eq 9) { return "$($p.L)-$($p.D.Substring(0,8))-$($p.D[8])" }
  return "$($p.L)-$($p.D)"
}
function Clave([string]$t) {
  if ([string]::IsNullOrWhiteSpace($t)) { return '' }
  $p = Separar $t
  if (-not $p) { return ($t.ToUpperInvariant() -replace '[^\p{L}\p{N}]', '') }
  $d = $p.D; if ('VEJGC'.Contains($p.L) -and $d.Length -eq 9) { $d = $d.Substring(0,8) }
  $d = $d.TrimStart('0'); if ($d.Length -eq 0) { return '' }
  return "$($p.L)$d"
}
function NombreClave([string]$n) { if (-not $n) { return '' }; return (($n.ToUpperInvariant() -replace '[\.,;:]', '' -replace '\s+', ' ').Trim()) }

# ── Datos ──────────────────────────────────────────────────────────────
$colsCli = @((Filas 'PRAGMA table_info(Clientes)' $null) | ForEach-Object { $_.name })
$clientes = Filas 'SELECT * FROM Clientes' $null
$nFact = @{}; (Filas 'SELECT Codigo, COUNT(*) AS N FROM Facturas GROUP BY Codigo' $null) | ForEach-Object { $nFact[[string]$_.Codigo] = [long]$_.N }

# Tablas que guardan el código del cliente
$refs = New-Object System.Collections.Generic.List[object]
$tablas = @((Filas "SELECT name FROM sqlite_master WHERE type='table'" $null) | ForEach-Object { $_.name })
$fijas = @{ 'Facturas'='Codigo'; 'Abonos'='Cliente'; 'CuentasCobrar'='Cliente'; 'RetencionesISLR'='Cliente' }
foreach ($t in $tablas) {
  $cols = @((Filas "PRAGMA table_info([$t])" $null) | ForEach-Object { $_.name })
  if ($fijas.ContainsKey($t) -and $cols -contains $fijas[$t]) { $refs.Add(@{ T=$t; C=$fijas[$t] }) }
  foreach ($c in $cols) { if ($c -in 'CodigoCliente','CodCliente') { $refs.Add(@{ T=$t; C=$c }) } }
}
Write-Host ("Clientes: {0}. Tablas que se actualizan: {1}" -f $clientes.Count, (($refs | ForEach-Object { "$($_.T).$($_.C)" }) -join ', '))

# ── Grupos de repetidos ────────────────────────────────────────────────
foreach ($c in $clientes) {
  $c | Add-Member -NotePropertyName _Clave  -NotePropertyValue (Clave ([string]$c.CiRif)) -Force
  $c | Add-Member -NotePropertyName _Nombre -NotePropertyValue (NombreClave ([string]$c.Descripcion)) -Force
  $c | Add-Member -NotePropertyName _NF     -NotePropertyValue ($(if ($nFact.ContainsKey([string]$c.Codigo)) { $nFact[[string]$c.Codigo] } else { 0 })) -Force
}
$grupos = New-Object System.Collections.Generic.List[object]
$clientes | Where-Object { $_._Clave -ne '' } | Group-Object _Clave | Where-Object { $_.Count -gt 1 } |
  ForEach-Object { $grupos.Add([pscustomobject]@{ Motivo = "cédula/RIF $($_.Name)"; Items = @($_.Group) }) }
$porNombre = @($clientes | Where-Object { $_._Clave -eq '' -and $_._Nombre -ne '' } | Group-Object _Nombre | Where-Object { $_.Count -gt 1 })

function NumCod($c) { $n = 0L; if ([long]::TryParse([string]$c.Codigo, [ref]$n)) { return $n }; return [long]::MaxValue }
function Elegir($items) { return @($items | Sort-Object @{e={$_._NF};Descending=$true}, @{e={ NumCod $_ }}, @{e={[string]$_.Codigo}})[0] }

Write-Host ''; Write-Host "==== Repetidos por cédula/RIF: $($grupos.Count) grupos ====" -ForegroundColor Cyan
function Mostrar($g) {
  $q = Elegir $g.Items
  Write-Host ("  {0}" -f $g.Motivo) -ForegroundColor Yellow
  foreach ($c in ($g.Items | Sort-Object @{e={ NumCod $_ }})) {
    $marca = if ($c.Codigo -eq $q.Codigo) { 'QUEDA ' } else { 'se une' }
    Write-Host ("     {0}  código {1,-6} {2,-38} CI/RIF {3,-16} facturas {4,3}  tel {5}" -f $marca, $c.Codigo, ([string]$c.Descripcion), ([string]$c.CiRif), $c._NF, ([string]$c.Telefono))
  }
}
foreach ($g in $grupos) { Mostrar $g }

Write-Host ''; Write-Host "==== Sin cédula y con el mismo nombre: $($porNombre.Count) grupos ====" -ForegroundColor Cyan
foreach ($pn in $porNombre) {
  $g = [pscustomobject]@{ Motivo = "nombre $($pn.Name) (sin cédula)"; Items = @($pn.Group) }
  Mostrar $g
  if ($FusionarPorNombre) { $grupos.Add($g) }
}
if (-not $FusionarPorNombre -and $porNombre.Count -gt 0) {
  Write-Host '  (Estos NO se unen a menos que agregues -FusionarPorNombre)' -ForegroundColor DarkGray }

$aNormalizar = @($clientes | Where-Object { [string]$_.CiRif -ne '' -and (Normalizar ([string]$_.CiRif)) -ne [string]$_.CiRif })
Write-Host ''; Write-Host "Cédulas/RIF que se pasan al formato estándar: $($aNormalizar.Count)"
$aNormalizar | Select-Object -First 15 | ForEach-Object { Write-Host ("   {0,-6} {1,-20} -> {2}" -f $_.Codigo, ([string]$_.CiRif), (Normalizar ([string]$_.CiRif))) }
$nBorrar = ($grupos | ForEach-Object { $_.Items.Count - 1 } | Measure-Object -Sum).Sum
Write-Host ''; Write-Host ("Se unirían {0} grupos y se borrarían {1} clientes repetidos." -f $grupos.Count, [long]$nBorrar) -ForegroundColor Green

if (-not $Aplicar) {
  Write-Host 'Nada fue modificado. Para aplicar:' -ForegroundColor Green
  Write-Host "  & 'C:\SistemaGestion\Claude outputs\fusionar-clientes-repetidos.ps1' -Aplicar" -ForegroundColor Green
  $cn.Close(); exit 0 }

# ── APLICAR ────────────────────────────────────────────────────────────
$resp = Join-Path (Split-Path $db) ("respaldo-antes-fusion-clientes-{0}.db" -f (Get-Date -Format 'yyyyMMdd-HHmmss'))
[void](Ejec "VACUUM INTO '$($resp.Replace("'","''"))'" $null)
Write-Host "Respaldo: $resp" -ForegroundColor Green
$logDir = 'C:\SistemaGestion\logs'; if (-not (Test-Path $logDir)) { New-Item -ItemType Directory -Path $logDir | Out-Null }
$log = Join-Path $logDir ("fusion-clientes-{0}.csv" -f (Get-Date -Format 'yyyyMMdd-HHmmss'))
$lineasLog = New-Object System.Collections.Generic.List[string]
$lineasLog.Add('Motivo;CodigoQueda;NombreQueda;CodigoBorrado;NombreBorrado;CiRifBorrado;Facturas;Telefono')

$camposCompletar = @('Direccion','Ciudad','Estado','Nit','Fax','Telefono','Telefono2','TipoCliente','Observaciones','Email','Contacto','PinBot') | Where-Object { $colsCli -contains $_ }
$script:tx = $cn.BeginTransaction()
try {
  foreach ($g in $grupos) {
    $q = Elegir $g.Items
    $otros = @($g.Items | Where-Object { $_.Codigo -ne $q.Codigo })
    # Completar datos vacíos del que queda
    $sets = [ordered]@{}
    foreach ($campo in $camposCompletar) {
      if ([string]::IsNullOrWhiteSpace([string]$q.$campo)) {
        $v = ($otros | ForEach-Object { [string]$_.$campo } | Where-Object { -not [string]::IsNullOrWhiteSpace($_) } | Select-Object -First 1)
        if ($v) { $sets[$campo] = $v } } }
    # Cédula/RIF: la más completa (con dígito verificador si alguno lo tiene)
    $rifs = @($g.Items | ForEach-Object { Normalizar ([string]$_.CiRif) } | Where-Object { $_ -ne '' } | Sort-Object Length -Descending)
    if ($rifs.Count -gt 0) { $sets['CiRif'] = $rifs[0] }
    if ($colsCli -contains 'AFavor') {
      $af = ($g.Items | ForEach-Object { [double]($(if ($_.AFavor) { $_.AFavor } else { 0 })) } | Measure-Object -Sum).Sum
      if ($af -ne 0) { $sets['AFavor'] = $af } }
    if ($sets.Count -gt 0) {
      $h = @{ '$cod' = [string]$q.Codigo }; $i = 0; $partes = @()
      foreach ($kv in $sets.GetEnumerator()) { $i++; $partes += "[$($kv.Key)]=`$p$i"; $h["`$p$i"] = $kv.Value }
      [void](Ejec ("UPDATE Clientes SET " + ($partes -join ', ') + " WHERE Codigo=`$cod") $h) }
    foreach ($o in $otros) {
      foreach ($r in $refs) { [void](Ejec "UPDATE [$($r.T)] SET [$($r.C)]=`$n WHERE [$($r.C)]=`$v" @{ '$n'=[string]$q.Codigo; '$v'=[string]$o.Codigo }) }
      [void](Ejec 'DELETE FROM Clientes WHERE Codigo=$c' @{ '$c'=[string]$o.Codigo })
      $lineasLog.Add(("{0};{1};{2};{3};{4};{5};{6};{7}" -f $g.Motivo, $q.Codigo, ([string]$q.Descripcion -replace ';',','), $o.Codigo, ([string]$o.Descripcion -replace ';',','), $o.CiRif, $o._NF, $o.Telefono))
    }
    Write-Host ("  {0}: queda {1} ({2}); se unieron {3}" -f $g.Motivo, $q.Codigo, $q.Descripcion, (($otros | ForEach-Object { $_.Codigo }) -join ', ')) -ForegroundColor Green
  }
  # Formato estándar de cédula/RIF en todos los clientes
  $nn = 0
  foreach ($c in (Filas "SELECT Codigo, CiRif FROM Clientes WHERE COALESCE(CiRif,'') <> ''" $null)) {
    $norm = Normalizar ([string]$c.CiRif)
    if ($norm -ne [string]$c.CiRif) { [void](Ejec 'UPDATE Clientes SET CiRif=$r WHERE Codigo=$c' @{ '$r'=$norm; '$c'=[string]$c.Codigo }); $nn++ } }
  $script:tx.Commit()
  [IO.File]::WriteAllLines($log, $lineasLog, (New-Object System.Text.UTF8Encoding($true)))
  Write-Host ''; Write-Host "LISTO: $($grupos.Count) grupos unidos, $nn cédulas/RIF normalizadas." -ForegroundColor Green
  Write-Host "Registro de lo que se unió: $log" -ForegroundColor Green
}
catch { $script:tx.Rollback(); Write-Host "[ERROR] $($_.Exception.Message) — no se modificó nada." -ForegroundColor Red; exit 1 }
finally { $cn.Close() }
