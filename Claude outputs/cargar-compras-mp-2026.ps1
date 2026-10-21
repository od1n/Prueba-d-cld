# ============================================================
#  cargar-compras-mp-2026.ps1
#  Carga las compras de materia prima (resina PEAD 6200B y 2706)
#  de 2026 que todavía no están en el sistema, y recalcula el
#  Kardex de materia prima (saldos y costo promedio ponderado).
#
#  Registra cada compra igual que la pantalla de Egresos:
#    Gastos + DetallesGasto + MovimientosInventario (MATERIA_PRIMA)
#    + ProductosProveedor (stock y costo) + CuentasPagar si queda saldo.
#
#  EJECUTAR EN OLIMPO, en PowerShell 7:
#
#   1) Revisar (NO modifica nada; muestra qué ya está cargado y qué falta):
#        & 'C:\SistemaGestion\Claude outputs\cargar-compras-mp-2026.ps1'
#
#   2) Aplicar (hace respaldo de la BD y carga lo que falta):
#        & 'C:\SistemaGestion\Claude outputs\cargar-compras-mp-2026.ps1' -Aplicar
#
#  Además corrige los egresos de resina ya cargados para que queden
#  igual que la lista (precios, flete, estado de pago, fecha en el
#  Kardex) y anula los que estaban mal cargados (ver $anularEgresos).
#
#  Es idempotente: si se corre dos veces, la segunda no carga nada,
#  porque reconoce las compras ya registradas (mismo insumo, mismos kg,
#  fecha con diferencia de hasta 7 días).
# ============================================================
param([switch]$Aplicar)
$ErrorActionPreference = 'Stop'
$inv = [Globalization.CultureInfo]::InvariantCulture

# ── Datos: lista de compras 2026 (USD sin IVA) ──────────────────────
# Insumos: INS-001 = PEAD 6200B, INS-002 = PEAD 2706
# Flete: va en la primera línea de cada compra (se reparte por kg al costo).
$lineas = @(
 @{c=1; f='2026-01-02'; rif='J-41109313-0'; doc='';                  ins='INS-001'; marca='';                   kg=1375; p=2.10; fl=50;  obs='Sin nota de entrega'}
 @{c=1; f='2026-01-02'; rif='J-41109313-0'; doc='';                  ins='INS-002'; marca='';                   kg=1375; p=2.10; fl=0;   obs='Sin nota de entrega'}
 @{c=2; f='2026-01-23'; rif='J-41109313-0'; doc='';                  ins='INS-001'; marca='';                   kg=1375; p=2.10; fl=0;   obs='Sin nota de entrega'}
 @{c=2; f='2026-01-23'; rif='J-41109313-0'; doc='';                  ins='INS-002'; marca='';                   kg=1375; p=2.10; fl=0;   obs='Sin nota de entrega'}
 @{c=3; f='2026-03-04'; rif='J-41109313-0'; doc='';                  ins='INS-001'; marca='';                   kg=1375; p=2.10; fl=0;   obs='Sin nota de entrega'}
 @{c=3; f='2026-03-04'; rif='J-41109313-0'; doc='';                  ins='INS-002'; marca='';                   kg=1375; p=2.10; fl=0;   obs='Sin nota de entrega'}
 @{c=4; f='2026-03-30'; rif='J-50039186-2'; doc='NE #138';           ins='INS-002'; marca='BPA';                kg=1500; p=2.20; fl=0;   obs='Total doc. con IVA $7.656; pagado $4.700 (saldo aprox. $2.956)'}
 @{c=4; f='2026-03-30'; rif='J-50039186-2'; doc='NE #138';           ins='INS-001'; marca='BPA';                kg=1500; p=2.20; fl=0;   obs=''}
 @{c=5; f='2026-04-09'; rif='J-41109313-0'; doc='NE 0620';           ins='INS-001'; marca='Chemiun';            kg=1500; p=2.65; fl=0;   obs='Compra mixta con Materia Alpha. NE 0620 imprime 1.375 kg por tipo'}
 @{c=5; f='2026-04-09'; rif='J-50039186-2'; doc='ND-000113';         ins='INS-002'; marca='BPC';                kg=1500; p=2.30; fl=0;   obs='Compra mixta con Valenk. Precio $2,30 confirmado (doc. imprime REF $1,80)'}
 @{c=6; f='2026-05-08'; rif='J-41109313-0'; doc='NE 0630';           ins='INS-001'; marca='Chemiun';            kg=1375; p=2.75; fl=0;   obs=''}
 @{c=6; f='2026-05-08'; rif='J-41109313-0'; doc='NE 0630';           ins='INS-002'; marca='Osterlene';          kg=1375; p=2.65; fl=0;   obs=''}
 @{c=7; f='2026-06-03'; rif='J-41109313-0'; doc='';                  ins='INS-001'; marca='';                   kg=1375; p=2.75; fl=0;   obs='Sin nota de entrega'}
 @{c=7; f='2026-06-03'; rif='J-41109313-0'; doc='';                  ins='INS-002'; marca='';                   kg=1375; p=2.75; fl=0;   obs='Sin nota de entrega'}
 @{c=8; f='2026-06-22'; rif='J-41109313-0'; doc='';                  ins='INS-001'; marca='';                   kg=1500; p=2.75; fl=60;  obs='Sin nota de entrega'}
 @{c=8; f='2026-06-22'; rif='J-41109313-0'; doc='';                  ins='INS-002'; marca='';                   kg=1500; p=2.75; fl=0;   obs='Sin nota de entrega'}
 @{c=9; f='2026-07-15'; rif='J-41109313-0'; doc='NE 0649';           ins='INS-001'; marca='West Polymer';       kg=1500; p=2.75; fl=60;  obs=''}
 @{c=9; f='2026-07-15'; rif='J-41109313-0'; doc='NE 0649';           ins='INS-002'; marca='Osterlene';          kg=1500; p=2.75; fl=0;   obs=''}
 @{c=10;f='2026-08-03'; rif='J-41109313-0'; doc='NE 0710';           ins='INS-001'; marca='West Polymer';       kg=1375; p=2.75; fl=60;  obs=''}
 @{c=10;f='2026-08-03'; rif='J-41109313-0'; doc='NE 0710';           ins='INS-002'; marca='Osterlene';          kg=1375; p=2.75; fl=0;   obs=''}
 @{c=11;f='2026-09-10'; rif='J-41109313-0'; doc='NE 0747';           ins='INS-001'; marca='West Polymer LD035'; kg=2750; p=2.60; fl=160; obs='Flete $160 por todo el despacho'}
 @{c=11;f='2026-09-10'; rif='J-41109313-0'; doc='NE 0747';           ins='INS-002'; marca='Snetor HD-4454';     kg=2750; p=2.60; fl=0;   obs=''}
 @{c=12;f='2026-10-14'; rif='J-41109313-0'; doc='Resumen 14/10/2026';ins='INS-001'; marca='';                   kg=1375; p=2.65; fl=120; obs=''}
 @{c=12;f='2026-10-14'; rif='J-41109313-0'; doc='Resumen 14/10/2026';ins='INS-002'; marca='';                   kg=1375; p=2.65; fl=0;   obs=''}
)

$lineas = @($lineas | ForEach-Object { $_.estado=''; $_.donde=''; $_.cu=0; $_.idGasto=$null; $_.idDet=$null; $_.idKx=$null; [pscustomobject]$_ })

$proveedoresLista = @{
  'J-41109313-0' = 'INVERSIONES VALENK, C.A'
  'J-50039186-2' = 'MATERIA ALPHA, C.A'
}
$palabraClave = @{ 'J-41109313-0' = 'VALENK'; 'J-50039186-2' = 'ALPHA' }

# Pagos: por defecto cada compra se registra como PAGADA completa.
# Excepción: compra 4 (Materia Alpha, NE #138) quedó con saldo de $2.956.
# Se registra el gasto a $6.600 (sin IVA, como la lista) con pagado $3.644,
# para que la cuenta por pagar quede en $2.956, que es la deuda real.
$pagadoEspecial = @{ '4|J-50039186-2' = 3644.00 }
# Compras que ya están pagadas completas aunque el sistema las tenga pendientes.
$pagadaCompleta = @('11|J-41109313-0')

# Egresos mal cargados que se anulan (y se cargan de nuevo según la lista).
# Solo se anulan si coinciden id, fecha y kg de cada línea; si no, no se tocan.
$anularEgresos = @(
  @{ Id=6; Fecha='2026-04-09'; Kg=1375; Motivo='Compra 5 cargada como Valenk 1.375 + 1.375 kg a 2,30. La real es Valenk 6200B 1.500 kg a 2,65 + Materia Alpha 2706 1.500 kg a 2,30 (se carga de nuevo según la lista de compras 2026)' }
)
$MARCA_CORR = 'Corregido según lista de compras MP 2026'

$TOL_KG   = 1.0   # tolerancia en kg para reconocer una compra ya cargada
$TOL_DIAS = 7     # tolerancia en días
$USUARIO  = 'Carga compras MP 2026'

# ── Conexión ─────────────────────────────────────────────────────────
$db = [Environment]::GetEnvironmentVariable('SG_DB_PATH','Machine')
if (-not $db) { $db = 'C:\SistemaGestion\sistema_gestion.db' }
if (-not (Test-Path $db)) { Write-Host "[ERROR] No existe la BD: $db" -ForegroundColor Red; exit 1 }
$appDir = 'C:\SistemaGestion\publish\App'
Set-Location $appDir; $env:PATH = "$appDir;$env:PATH"
foreach ($d in 'SQLitePCLRaw.core.dll','SQLitePCLRaw.provider.e_sqlite3.dll','SQLitePCLRaw.batteries_v2.dll','Microsoft.Data.Sqlite.dll') {
  $p = Join-Path $appDir $d; if (Test-Path $p) { Add-Type -Path $p } }
try { [SQLitePCL.Batteries_V2]::Init() } catch {}

$modo = if ($Aplicar) { 'APLICAR' } else { 'REVISAR (solo lectura)' }
Write-Host "Base de datos: $db" -ForegroundColor Yellow
Write-Host "Modo: $modo" -ForegroundColor Yellow

$cs = if ($Aplicar) { "Data Source=$db" } else { "Data Source=$db;Mode=ReadOnly;Cache=Shared" }
$cn = New-Object Microsoft.Data.Sqlite.SqliteConnection($cs); $cn.Open()
$pr = $cn.CreateCommand(); $pr.CommandText = 'PRAGMA busy_timeout=8000'; [void]$pr.ExecuteNonQuery()
$script:tx = $null

function Cmd([string]$sql, [hashtable]$prm) {
  $k = $cn.CreateCommand(); $k.CommandText = $sql
  if ($script:tx) { $k.Transaction = $script:tx }
  if ($prm) { foreach ($n in $prm.Keys) { $v = $prm[$n]; if ($null -eq $v) { $v = [DBNull]::Value }; [void]$k.Parameters.AddWithValue($n, $v) } }
  return $k
}
function Filas([string]$sql, [hashtable]$prm) {
  $k = Cmd $sql $prm; $r = $k.ExecuteReader(); $out = New-Object System.Collections.Generic.List[object]
  while ($r.Read()) { $o = [ordered]@{}; for ($i=0; $i -lt $r.FieldCount; $i++) { $o[$r.GetName($i)] = if ($r.IsDBNull($i)) { $null } else { $r.GetValue($i) } }; $out.Add([pscustomobject]$o) }
  $r.Close(); return ,$out
}
function Escalar([string]$sql, [hashtable]$prm) { $v = (Cmd $sql $prm).ExecuteScalar(); if ($v -is [DBNull]) { $null } else { $v } }
function Ejec([string]$sql, [hashtable]$prm) { [void](Cmd $sql $prm).ExecuteNonQuery() }
function Columnas([string]$tabla) { (Filas "PRAGMA table_info($tabla)" $null) | ForEach-Object { $_.name } }
function Fecha($s) { if (-not $s) { return $null }; $t = [datetime]::MinValue
  if ([datetime]::TryParse(([string]$s), $inv, [Globalization.DateTimeStyles]::None, [ref]$t)) { return $t } ; return $null }
function SoloRif([string]$s) { if (-not $s) { return '' }; return (($s.ToUpper()) -replace '[^A-Z0-9]','') }
function N2($x) { ([decimal]$x).ToString('N2', [Globalization.CultureInfo]::GetCultureInfo('es-VE')) }
function Titulo($t) { Write-Host ''; Write-Host "==== $t ====" -ForegroundColor Cyan }

# ── 1) Insumos, proveedores, categoría ───────────────────────────────
Titulo 'Insumos (ProductosProveedor)'
$insumos = @{}
foreach ($ci in 'INS-001','INS-002') {
  $f = Filas 'SELECT Id, CodigoInterno, Descripcion, COALESCE(StockActual,0) AS Stock, COALESCE(NULLIF(PrecioPromedio,0),CostoPromedio,0) AS Cpp, COALESCE(PrecioUltCompra,0) AS Ult FROM ProductosProveedor WHERE CodigoInterno=$c' @{'$c'=$ci}
  if ($f.Count -eq 0) { Write-Host "[ERROR] No existe el insumo $ci en ProductosProveedor." -ForegroundColor Red; exit 1 }
  $insumos[$ci] = $f[0]
  Write-Host ("  {0}  Id={1}  {2}  | stock {3} kg  costo prom. {4}  últ. compra {5}" -f $ci, $f[0].Id, $f[0].Descripcion, (N2 $f[0].Stock), $f[0].Cpp, $f[0].Ult)
}

Titulo 'Proveedores'
$provs = Filas 'SELECT Codigo, Descripcion, CiRif FROM Proveedores' $null
$provPorRif = @{}
foreach ($rif in $proveedoresLista.Keys) {
  $m = $provs | Where-Object { (SoloRif $_.CiRif) -eq (SoloRif $rif) } | Select-Object -First 1
  if (-not $m) {
    $m = $provs | Where-Object { $_.Descripcion -and $_.Descripcion.ToUpper().Contains($palabraClave[$rif]) } | Select-Object -First 1
  }
  if ($m) { $provPorRif[$rif] = @{ Codigo=[string]$m.Codigo; Nombre=[string]$m.Descripcion; Nuevo=$false }
            Write-Host "  $rif -> existe: código $($m.Codigo)  $($m.Descripcion)" }
  else    { $provPorRif[$rif] = @{ Codigo=(SoloRif $rif); Nombre=$proveedoresLista[$rif]; Nuevo=$true }
            Write-Host "  $rif -> NO existe; se creará con código $(SoloRif $rif)  $($proveedoresLista[$rif])" -ForegroundColor Yellow }
}

Titulo 'Categoría de gasto de materia prima'
$cat = Filas 'SELECT Id, Nombre FROM CategoriasGasto WHERE COALESCE(EsMateriaPrima,0)=1 ORDER BY COALESCE(Orden,0), Id' $null
if ($cat.Count -eq 0) { Write-Host '[ERROR] No hay categoría de gasto marcada como materia prima.' -ForegroundColor Red; exit 1 }
$idCat = [long]$cat[0].Id
Write-Host "  Se usará: Id=$idCat  $($cat[0].Nombre)"

# ── 2) Lo que ya existe en el sistema ────────────────────────────────
$codes = "'INS-001','INS-002'"
$ids   = "$($insumos['INS-001'].Id),$($insumos['INS-002'].Id)"
$det = Filas @"
SELECT g.Id AS IdGasto, g.Fecha, g.Proveedor, COALESCE(g.MontoUSD,0) AS MontoUSD, g.Estado,
       COALESCE(g.PagadoUSD,0) AS PagadoUSD, COALESCE(g.TasaUsada,0) AS TasaUsada,
       COALESCE(g.MonedaOriginal,'USD') AS MonedaOriginal, g.FechaPago, COALESCE(g.Observaciones,'') AS Obs,
       d.Id AS IdDet, COALESCE(d.CodigoInterno, pp.CodigoInterno) AS Cod, d.Cantidad, d.PrecioUnitario
FROM DetallesGasto d JOIN Gastos g ON g.Id=d.IdGasto
LEFT JOIN ProductosProveedor pp ON pp.Id=d.IdArticulo
WHERE (d.CodigoInterno IN ($codes) OR d.IdArticulo IN ($ids))
  AND COALESCE(g.Estado,'') <> 'Anulado'
ORDER BY g.Fecha, g.Id
"@ $null
$kx = Filas @"
SELECT IdMovimiento, FechaHora, CodigoProducto AS Cod, TipoMovimiento, Concepto, Cantidad, SaldoAnterior, NuevoSaldo,
       COALESCE(Referencia,'') AS Referencia, COALESCE(CostoUnitario,0) AS CostoUnitario
FROM MovimientosInventario
WHERE TipoKardex='MATERIA_PRIMA' AND CodigoProducto IN ($codes)
ORDER BY FechaHora, IdMovimiento
"@ $null
$compras = Filas @"
SELECT c.CompraN, c.FechaC, c.CodigoP, d.CodigoI AS Cod, d.Cantidad, d.PrecioC
FROM DetallesCompra d JOIN Compras c ON c.rowid=d.IdCompra
WHERE d.CodigoI IN ($codes)
"@ $null

# ── Egresos a anular (verificando su huella) ─────────────────────────
$aAnular = @()
foreach ($a in $anularEgresos) {
  $lin = @($det | Where-Object { [long]$_.IdGasto -eq $a.Id })
  if ($lin.Count -eq 0) { continue }   # ya anulado o no existe
  $ok = (([string]$lin[0].Fecha).StartsWith($a.Fecha)) -and -not ($lin | Where-Object { [Math]::Abs([double]$_.Cantidad - $a.Kg) -gt $TOL_KG })
  if ($ok) { $aAnular += $a } else {
    Write-Host "[AVISO] El Gasto #$($a.Id) no coincide con lo esperado (fecha $($a.Fecha), $($a.Kg) kg por línea); no se anula." -ForegroundColor Red }
}
$idsAnular = @($aAnular | ForEach-Object { [long]$_.Id })
if ($idsAnular.Count -gt 0) {
  Titulo 'Egresos que se ANULAN (mal cargados)'
  foreach ($a in $aAnular) { Write-Host "  Gasto #$($a.Id)  $($a.Fecha): $($a.Motivo)" -ForegroundColor Yellow }
  $det = @($det | Where-Object { $idsAnular -notcontains [long]$_.IdGasto })
  $kx  = @($kx  | Where-Object { -not ($_.Referencia -match '^Gasto#(\d+)$' -and $idsAnular -contains [long]$Matches[1]) })
}

Titulo "Egresos con resina ya registrados ($($det.Count) líneas)"
$det | ForEach-Object { Write-Host ("  Gasto #{0,-5} {1}  {2,-8} {3,8} kg  x {4,6}  | {5}  ({6})" -f $_.IdGasto, ([string]$_.Fecha).Substring(0,[Math]::Min(10,([string]$_.Fecha).Length)), $_.Cod, (N2 $_.Cantidad), $_.PrecioUnitario, $_.Proveedor, $_.Estado) }
Titulo "Kardex de materia prima ($($kx.Count) movimientos)"
$kx | Group-Object Cod, TipoMovimiento | ForEach-Object {
  Write-Host ("  {0,-22} {1,4} movimientos  {2,12} kg" -f $_.Name, $_.Count, (N2 (($_.Group | Measure-Object Cantidad -Sum).Sum))) }
$kx | Where-Object { $_.TipoMovimiento -eq 'ENTRADA' } | ForEach-Object {
  Write-Host ("    ENTRADA #{0,-6} {1}  {2}  {3,8} kg  costo {4}  ref '{5}'  {6}" -f $_.IdMovimiento, $_.FechaHora, $_.Cod, (N2 $_.Cantidad), $_.CostoUnitario, $_.Referencia, $_.Concepto) }
if ($compras.Count -gt 0) {
  Titulo "Resina en el módulo Compras (no debería haber)"
  $compras | ForEach-Object { Write-Host "  Compra $($_.CompraN) $($_.FechaC) $($_.Cod) $($_.Cantidad) kg x $($_.PrecioC)" -ForegroundColor Yellow } }

# ── 3) Reconocer qué líneas ya están cargadas ────────────────────────
$usadosDet = @{}; $usadosKx = @{}
foreach ($l in $lineas) {
  $l.estado = 'FALTA'; $l.donde = ''
  $fl = [datetime]::ParseExact($l.f,'yyyy-MM-dd',$inv)
  $cand = $det | Where-Object { -not $usadosDet.ContainsKey([string]$_.IdDet) -and $_.Cod -eq $l.ins -and
            [Math]::Abs([double]$_.Cantidad - $l.kg) -le $TOL_KG -and (Fecha $_.Fecha) -and
            [Math]::Abs(((Fecha $_.Fecha) - $fl).TotalDays) -le $TOL_DIAS } |
          Sort-Object { [Math]::Abs(((Fecha $_.Fecha) - $fl).TotalDays) } | Select-Object -First 1
  if ($cand) {
    $usadosDet[[string]$cand.IdDet] = 1; $l.estado = 'CARGADA'; $l.donde = "Gasto #$($cand.IdGasto) ($($cand.Fecha))"
    $l.idGasto = [long]$cand.IdGasto; $l.idDet = [long]$cand.IdDet
    $kxg = $kx | Where-Object { $_.TipoMovimiento -eq 'ENTRADA' -and $_.Cod -eq $l.ins -and $_.Referencia -eq "Gasto#$($cand.IdGasto)" -and -not $usadosKx.ContainsKey([string]$_.IdMovimiento) } | Select-Object -First 1
    if ($kxg) { $usadosKx[[string]$kxg.IdMovimiento] = 1; $l.idKx = [long]$kxg.IdMovimiento } else { $l.donde += ' [sin movimiento en Kardex: se agrega]' }
    continue
  }
  $candK = $kx | Where-Object { $_.TipoMovimiento -eq 'ENTRADA' -and -not $usadosKx.ContainsKey([string]$_.IdMovimiento) -and $_.Cod -eq $l.ins -and
             $_.Referencia -notlike 'PROD-*' -and [Math]::Abs([double]$_.Cantidad - $l.kg) -le $TOL_KG -and (Fecha $_.FechaHora) -and
             [Math]::Abs(((Fecha $_.FechaHora).Date - $fl).TotalDays) -le $TOL_DIAS } |
           Sort-Object { [Math]::Abs(((Fecha $_.FechaHora).Date - $fl).TotalDays) } | Select-Object -First 1
  if ($candK) { $usadosKx[[string]$candK.IdMovimiento] = 1; $l.estado = 'CARGADA'; $l.idKx = [long]$candK.IdMovimiento; $l.donde = "Kardex #$($candK.IdMovimiento) ($($candK.FechaHora)) sin egreso" }
}
# Marcar como usadas las entradas de Kardex ligadas a egresos ya reconocidos
foreach ($k in $kx) { if ($k.Referencia -match '^Gasto#(\d+)$') { $g = $Matches[1]
  if ($det | Where-Object { [string]$_.IdGasto -eq $g -and $usadosDet.ContainsKey([string]$_.IdDet) }) { $usadosKx[[string]$k.IdMovimiento] = 1 } } }

# Agrupar por compra + proveedor (un egreso por cada uno)
$grupos = $lineas | Group-Object { "$($_.c)|$($_.rif)" } | ForEach-Object {
  $g = $_.Group; $est = @($g | ForEach-Object { $_.estado } | Sort-Object -Unique)
  [pscustomobject]@{ Clave=$_.Name; Compra=$g[0].c; Fecha=$g[0].f; Rif=$g[0].rif; Lineas=$g
    Estado = if ($est.Count -gt 1) { 'PARCIAL' } else { $est[0] } }
} | Sort-Object Compra, Rif

Titulo 'Resultado por compra'
foreach ($g in $grupos) {
  $color = switch ($g.Estado) { 'CARGADA' {'Green'} 'FALTA' {'Yellow'} default {'Red'} }
  $kg = ($g.Lineas | Measure-Object kg -Sum).Sum
  $tot = ($g.Lineas | ForEach-Object { $_.kg * $_.p + $_.fl } | Measure-Object -Sum).Sum
  Write-Host ("  Compra {0,2}  {1}  {2,-26} {3,7} kg  `${4,10}  -> {5}" -f $g.Compra, $g.Fecha, $provPorRif[$g.Rif].Nombre, (N2 $kg), (N2 $tot), $g.Estado) -ForegroundColor $color
  foreach ($l in $g.Lineas) { Write-Host ("      {0}  {1,7} kg x {2}  {3}  {4}" -f $l.ins, (N2 $l.kg), $l.p, $l.estado, $l.donde) }
}

$sobrantesDet = $det | Where-Object { -not $usadosDet.ContainsKey([string]$_.IdDet) }
$sobrantesKx  = $kx  | Where-Object { $_.TipoMovimiento -eq 'ENTRADA' -and -not $usadosKx.ContainsKey([string]$_.IdMovimiento) -and $_.Referencia -notlike 'PROD-*' }
if ($sobrantesDet.Count -gt 0 -or $sobrantesKx.Count -gt 0) {
  Titulo 'ATENCIÓN: entradas de resina en el sistema que NO coinciden con ninguna línea de la lista'
  $sobrantesDet | ForEach-Object { Write-Host "  Gasto #$($_.IdGasto)  $($_.Fecha)  $($_.Cod)  $($_.Cantidad) kg x $($_.PrecioUnitario)  $($_.Proveedor)" -ForegroundColor Red }
  $sobrantesKx  | ForEach-Object { Write-Host "  Kardex #$($_.IdMovimiento)  $($_.FechaHora)  $($_.Cod)  $($_.Cantidad) kg  ref '$($_.Referencia)'  $($_.Concepto)" -ForegroundColor Red }
  Write-Host '  (Pueden ser compras cargadas con otra fecha o cantidad, o compras que no están en la lista.)' -ForegroundColor Red
}

$aCargar = @($grupos | Where-Object { $_.Estado -eq 'FALTA' })

function CostoAterrizado($g) {   # precio + flete repartido por kg en toda la compra
  $kgT = ($g.Lineas | Measure-Object kg -Sum).Sum; $flT = ($g.Lineas | Measure-Object fl -Sum).Sum
  $h = @{}; foreach ($l in $g.Lineas) { $h[$l.ins] = [Math]::Round([decimal]$l.p + [decimal]$flT / [decimal]$kgT, 4) }; return $h
}
function TotalGrupo($g) {
  $sub = [decimal](($g.Lineas | ForEach-Object { [decimal]$_.kg * [decimal]$_.p } | Measure-Object -Sum).Sum)
  return [Math]::Round($sub + [decimal](($g.Lineas | Measure-Object fl -Sum).Sum), 2)
}
function PagadoGrupo($g, [decimal]$tot, [decimal]$pagadoActual, [string]$estadoActual) {
  if ($pagadaCompleta -contains $g.Clave) { return $tot }
  if ($pagadoEspecial.ContainsKey($g.Clave)) { return [decimal]$pagadoEspecial[$g.Clave] }
  if ($estadoActual -and $estadoActual -ne 'Pagado') { return [Math]::Min($pagadoActual, $tot) }
  return $tot
}

# ── Correcciones a egresos ya cargados ───────────────────────────────
$fletes = Filas 'SELECT IdGasto, COALESCE(SUM(Subtotal),0) AS Flete FROM DetallesGasto WHERE EsInventariable=0 AND Descripcion LIKE ''Flete%'' GROUP BY IdGasto' $null
$fletePorGasto = @{}; foreach ($f in $fletes) { $fletePorGasto[[long]$f.IdGasto] = [decimal]$f.Flete }
$correcciones = @()
foreach ($g in ($grupos | Where-Object { $_.Estado -eq 'CARGADA' })) {
  $idsG = @($g.Lineas | ForEach-Object { $_.idGasto } | Where-Object { $_ } | Sort-Object -Unique)
  if ($idsG.Count -ne 1 -or @($g.Lineas | Where-Object { -not $_.idGasto }).Count -gt 0) { continue }   # solo egresos completos
  $idG = [long]$idsG[0]; $cab = $det | Where-Object { [long]$_.IdGasto -eq $idG } | Select-Object -First 1
  $ca = CostoAterrizado $g; $tot = TotalGrupo $g
  $pag = PagadoGrupo $g $tot ([decimal]$cab.PagadoUSD) ([string]$cab.Estado)
  $est = if ($pag -ge $tot - 0.01) { 'Pagado' } else { 'Pendiente' }
  $cambios = @()
  foreach ($l in $g.Lineas) {
    $l.cu = $ca[$l.ins]
    $d0 = $det | Where-Object { [long]$_.IdDet -eq $l.idDet } | Select-Object -First 1
    if ([Math]::Abs([double]$d0.PrecioUnitario - $l.p) -gt 0.00001 -or [Math]::Abs([double]$d0.Cantidad - $l.kg) -gt 0.001) {
      $cambios += "$($l.ins): $(N2 $d0.Cantidad) kg x $($d0.PrecioUnitario) -> $(N2 $l.kg) kg x $($l.p)" }
    if (-not $l.idKx) { $cambios += "$($l.ins): falta la entrada en el Kardex (se agrega)" }
    else { $k0 = $kx | Where-Object { [long]$_.IdMovimiento -eq $l.idKx } | Select-Object -First 1
      if ([Math]::Abs([double]$k0.CostoUnitario - [double]$l.cu) -gt 0.00001) { $cambios += "$($l.ins): costo Kardex $($k0.CostoUnitario) -> $($l.cu)" }
      if (-not ([string]$k0.FechaHora).StartsWith($g.Fecha)) { $cambios += "$($l.ins): fecha Kardex $($k0.FechaHora) -> $($g.Fecha)" } }
  }
  $flT = [decimal](($g.Lineas | Measure-Object fl -Sum).Sum)
  $flA = if ($fletePorGasto.ContainsKey($idG)) { $fletePorGasto[$idG] } else { [decimal]0 }
  if ($flT -gt 0 -and $flA -eq 0) { $cambios += "Flete `$$(N2 $flT) (se agrega)" }
  if ([Math]::Abs([decimal]$cab.MontoUSD - $tot) -gt 0.01) { $cambios += "Monto `$$(N2 $cab.MontoUSD) -> `$$(N2 $tot)" }
  if ($cab.Estado -ne $est -or [Math]::Abs([decimal]$cab.PagadoUSD - $pag) -gt 0.01) { $cambios += "Pago: $($cab.Estado) `$$(N2 $cab.PagadoUSD) -> $est `$$(N2 $pag)" }
  if ($cambios.Count -gt 0) {
    $correcciones += [pscustomobject]@{ Grupo=$g; IdGasto=$idG; Cab=$cab; Total=$tot; Pagado=$pag; Estado=$est; Flete=$flT; FleteActual=$flA; Cambios=$cambios } }
}
if ($correcciones.Count -gt 0) {
  Titulo 'Egresos ya cargados que se CORRIGEN según la lista'
  foreach ($c in $correcciones) {
    Write-Host ("  Compra {0,2}  {1}  Gasto #{2}" -f $c.Grupo.Compra, $c.Grupo.Fecha, $c.IdGasto) -ForegroundColor Yellow
    $c.Cambios | ForEach-Object { Write-Host "      $_" } }
}
# Ajustes de Kardex que implican las correcciones (para la simulación)
$ajusteKx = @{}; $faltaKx = New-Object System.Collections.Generic.List[object]
foreach ($c in $correcciones) { foreach ($l in $c.Grupo.Lineas) {
  if ($l.idKx) { $ajusteKx[[long]$l.idKx] = @{ Fh="$($c.Grupo.Fecha) 08:00:00"; Costo=[decimal]$l.cu } } else { $faltaKx.Add($l) } } }
$parciales = @($grupos | Where-Object { $_.Estado -eq 'PARCIAL' })

# ── 4) Simulación del Kardex recalculado ─────────────────────────────
function Recalcular($movs) {   # movs: lista ordenada con Tipo, Cantidad, Costo
  $saldo = [decimal]0; $cpp = [decimal]0; $ult = [decimal]0; $negativo = $null
  foreach ($m in $movs) {
    $q = [decimal]$m.Cantidad; $m.SA = $saldo
    if ($m.Tipo -eq 'ENTRADA') {
      $cu = if ([decimal]$m.Costo -gt 0) { [decimal]$m.Costo } else { $cpp }
      $base = [Math]::Max($saldo, [decimal]0)
      $cpp = if (($base + $q) -gt 0 -and $cpp -gt 0) { [Math]::Round(($base*$cpp + $q*$cu)/($base+$q), 4) } else { $cu }
      if ([decimal]$m.Costo -gt 0 -and $m.EsCompra) { $ult = [decimal]$m.Costo }
      $saldo += $q; $m.CU = $cu
    } else { $saldo -= $q; $m.CU = $cpp; if ($saldo -lt -0.01 -and -not $negativo) { $negativo = $m.FechaHora } }
    $m.NS = $saldo
  }
  return @{ Saldo=$saldo; Cpp=$cpp; Ult=$ult; Negativo=$negativo }
}
function ArmarMovs([string]$cod, $nuevos) {
  $l = New-Object System.Collections.Generic.List[object]
  foreach ($k in ($kx | Where-Object { $_.Cod -eq $cod })) {
    $fh = [string]$k.FechaHora; $co = [decimal]$k.CostoUnitario
    if ($ajusteKx.ContainsKey([long]$k.IdMovimiento)) { $fh = $ajusteKx[[long]$k.IdMovimiento].Fh; $co = $ajusteKx[[long]$k.IdMovimiento].Costo }
    $l.Add([pscustomobject]@{ Id=[long]$k.IdMovimiento; FechaHora=$fh; Fh=(Fecha $fh); Tipo=[string]$k.TipoMovimiento
      Cantidad=[decimal]$k.Cantidad; Costo=$co; EsCompra=($k.Referencia -notlike 'PROD-*' -and $k.Referencia -ne 'AJUSTE'); SA=0; NS=0; CU=0 }) }
  $i = 0
  foreach ($n in $nuevos) { $i++; if ($n.ins -ne $cod) { continue }
    $l.Add([pscustomobject]@{ Id=[long](9e15 + $i); FechaHora="$($n.f) 08:00:00"; Fh=[datetime]::ParseExact($n.f,'yyyy-MM-dd',$inv).AddHours(8); Tipo='ENTRADA'
      Cantidad=[decimal]$n.kg; Costo=[decimal]$n.cu; EsCompra=$true; SA=0; NS=0; CU=0; Nuevo=$n }) }
  return ,@($l | Sort-Object Fh, Id)
}
$raros = $kx | Where-Object { $_.TipoMovimiento -notin 'ENTRADA','SALIDA' }
if ($raros.Count -gt 0) { Titulo 'ATENCIÓN: movimientos con tipo distinto de ENTRADA/SALIDA'
  $raros | ForEach-Object { Write-Host "  #$($_.IdMovimiento) $($_.FechaHora) $($_.TipoMovimiento) $($_.Cantidad)" -ForegroundColor Red } }

$nuevasLineas = New-Object System.Collections.Generic.List[object]
foreach ($g in $aCargar) { $ca = CostoAterrizado $g; foreach ($l in $g.Lineas) { $l.cu = $ca[$l.ins]; $nuevasLineas.Add($l) } }
$simLineas = New-Object System.Collections.Generic.List[object]
foreach ($l in $nuevasLineas) { $simLineas.Add($l) }; foreach ($l in $faltaKx) { $simLineas.Add($l) }

Titulo 'Kardex de materia prima: actual vs. recalculado con la carga'
$sim = @{}
foreach ($cod in 'INS-001','INS-002') {
  $movs = ArmarMovs $cod $simLineas
  $r = Recalcular $movs; $sim[$cod] = @{ Movs=$movs; Res=$r }
  $a = $insumos[$cod]
  Write-Host ("  {0}  actual: {1,10} kg a {2}   ->  recalculado: {3,10} kg a {4}  (últ. compra {5})" -f $cod, (N2 $a.Stock), $a.Cpp, (N2 $r.Saldo), $r.Cpp, $r.Ult)
  if ($r.Negativo) { Write-Host "      El saldo queda negativo desde $($r.Negativo): hay consumos registrados antes de tener compras suficientes." -ForegroundColor Yellow }
}
$kgCompradoLista = ($lineas | Measure-Object kg -Sum).Sum
Write-Host ("  Total comprado según la lista: {0} kg" -f (N2 $kgCompradoLista))

if ($parciales.Count -gt 0) {
  Write-Host ''; Write-Host 'Hay compras PARCIALES (una línea cargada y otra no). No se tocan; hay que revisarlas a mano.' -ForegroundColor Red }

if (-not $Aplicar) {
  Write-Host ''
  Write-Host ("Se anularían {0} egresos, se corregirían {1} y se cargarían {2} nuevos ({3} líneas). Nada fue modificado." -f $idsAnular.Count, $correcciones.Count, $aCargar.Count, $nuevasLineas.Count) -ForegroundColor Green
  Write-Host "Para aplicar:  & 'C:\SistemaGestion\Claude outputs\cargar-compras-mp-2026.ps1' -Aplicar" -ForegroundColor Green
  $cn.Close(); exit 0
}
if ($raros.Count -gt 0) { Write-Host '[ALTO] Hay movimientos de tipo desconocido; no se aplica.' -ForegroundColor Red; $cn.Close(); exit 1 }

# ── 5) APLICAR ───────────────────────────────────────────────────────
$resp = Join-Path (Split-Path $db) ("respaldo-antes-compras-mp-{0}.db" -f (Get-Date -Format 'yyyyMMdd-HHmmss'))
Titulo "Respaldo: $resp"
Ejec "VACUUM INTO '$($resp.Replace("'","''"))'" $null
Write-Host '  Respaldo creado.' -ForegroundColor Green

$colG = Columnas 'Gastos'; $colC = Columnas 'CuentasPagar'; $colP = Columnas 'Proveedores'
function Insertar([string]$tabla, $cols, [System.Collections.Specialized.OrderedDictionary]$valores) {
  $usar = @($valores.Keys | Where-Object { $cols -contains $_ })
  $sql = "INSERT INTO $tabla (" + ($usar -join ',') + ') VALUES (' + (($usar | ForEach-Object { '$' + $_ }) -join ',') + ')'
  $h = @{}; foreach ($k in $usar) { $h['$' + $k] = $valores[$k] }
  Ejec $sql $h; return [long](Escalar 'SELECT last_insert_rowid()' $null)
}
function Tasa([string]$f) { Escalar 'SELECT TasaBCV FROM TasaCambio WHERE Fecha<=$f AND TasaBCV>0 ORDER BY Fecha DESC LIMIT 1' @{'$f'=$f} }

$script:tx = $cn.BeginTransaction()
try {
  $ahora = (Get-Date).ToString('yyyy-MM-dd HH:mm:ss')
  # Proveedores faltantes
  foreach ($rif in $provPorRif.Keys) { $p = $provPorRif[$rif]
    if ($p.Nuevo -and ($aCargar | Where-Object { $_.Rif -eq $rif })) {
      [void](Insertar 'Proveedores' $colP ([ordered]@{ Codigo=$p.Codigo; Descripcion=$p.Nombre; CiRif=$rif; DiasCredito=0; FechaInicio='2026-01-01'; Tipo='Materia prima'; Observaciones='Creado por carga de compras MP 2026' }))
      Write-Host "  Proveedor creado: $($p.Codigo) $($p.Nombre)" } }

  # Anular egresos mal cargados: se quitan sus entradas del Kardex (el Kardex
  # se recalcula completo al final), su cuenta por pagar, y se marca Anulado.
  foreach ($a in $aAnular) {
    Ejec 'DELETE FROM MovimientosInventario WHERE TipoKardex=''MATERIA_PRIMA'' AND Referencia=$r' @{ '$r'="Gasto#$($a.Id)" }
    if ($colC -contains 'IdEgreso') {
      Ejec 'DELETE FROM PagosCxP WHERE IdCuenta IN (SELECT Id FROM CuentasPagar WHERE IdEgreso=$g)' @{ '$g'=$a.Id }
      Ejec 'DELETE FROM CuentasPagar WHERE IdEgreso=$g' @{ '$g'=$a.Id } }
    Ejec 'UPDATE Gastos SET Estado=''Anulado'', Observaciones=CASE WHEN COALESCE(Observaciones,'''')='''' THEN ''ANULADO: '' || $m ELSE Observaciones || '' | ANULADO: '' || $m END WHERE Id=$g' @{ '$g'=$a.Id; '$m'=$a.Motivo }
    Write-Host "  Gasto #$($a.Id) anulado." -ForegroundColor Green
  }

  # Corregir egresos ya cargados
  foreach ($c in $correcciones) {
    $g = $c.Grupo; $idG = $c.IdGasto; $tot = $c.Total; $pag = $c.Pagado
    foreach ($l in $g.Lineas) {
      Ejec 'UPDATE DetallesGasto SET Cantidad=$q, PrecioUnitario=$p, Subtotal=$s WHERE Id=$id' @{
        '$q'=[double]$l.kg; '$p'=[double]$l.p; '$s'=[double][Math]::Round([decimal]$l.kg*[decimal]$l.p,2); '$id'=[long]$l.idDet }
      if ($l.idKx) {
        Ejec 'UPDATE MovimientosInventario SET FechaHora=$fh, CostoUnitario=$cu, Cantidad=$q WHERE IdMovimiento=$id' @{
          '$fh'="$($g.Fecha) 08:00:00"; '$cu'=[double]$l.cu; '$q'=[double]$l.kg; '$id'=[long]$l.idKx } }
      else {
        Ejec 'INSERT INTO MovimientosInventario (FechaHora,CodigoProducto,TipoMovimiento,Concepto,Cantidad,SaldoAnterior,NuevoSaldo,Referencia,Usuario,TipoKardex,CostoUnitario) VALUES ($fh,$cp,''ENTRADA'',$co,$q,0,0,$re,$us,''MATERIA_PRIMA'',$cu)' @{
          '$fh'="$($g.Fecha) 08:00:00"; '$cp'=$l.ins; '$co'="Compra — Gasto #$idG"; '$q'=[double]$l.kg; '$re'="Gasto#$idG"; '$us'=$USUARIO; '$cu'=[double]$l.cu } }
    }
    if ($c.Flete -gt 0 -and $c.FleteActual -eq 0) {
      Ejec 'INSERT INTO DetallesGasto (IdGasto,Descripcion,IdArticulo,CodigoInterno,TipoInsumo,EsInventariable,Cantidad,PrecioUnitario,Subtotal) VALUES ($g,''Flete (repartido en el costo por kg)'',NULL,NULL,''Servicio'',0,1,$p,$p)' @{ '$g'=$idG; '$p'=[double]$c.Flete } }
    $tasa = [decimal]$c.Cab.TasaUsada
    $bs = if ($tasa -gt 0) { [double][Math]::Round($tot*$tasa,2) } else { $null }
    $mo = if ($c.Cab.MonedaOriginal -eq 'BS' -and $bs) { $bs } else { [double]$tot }
    $obsN = [string]$c.Cab.Obs; if (-not $obsN.Contains($MARCA_CORR)) { $obsN = ($obsN + " | $MARCA_CORR").TrimStart(' ','|') }
    $fp = if ($pag -gt 0 -and -not $c.Cab.FechaPago) { $g.Fecha } else { $c.Cab.FechaPago }
    $sets = [ordered]@{ MontoUSD=[double]$tot; MontoOriginal=$mo; PagadoUSD=[double]$pag; Estado=$c.Estado; FechaPago=$fp; Observaciones=$obsN
                        Cantidad=[double](($g.Lineas | Measure-Object kg -Sum).Sum) }
    if ($null -ne $bs) { $sets['MontoBs'] = $bs }
    if ($c.Estado -eq 'Pagado') { $sets['FechaVencimiento'] = $null }
    $usar = @($sets.Keys | Where-Object { $colG -contains $_ })
    $h = @{ '$id'=$idG }; foreach ($k in $usar) { $h['$' + $k] = $sets[$k] }
    Ejec ('UPDATE Gastos SET ' + (($usar | ForEach-Object { "$_=`$$_" }) -join ', ') + ' WHERE Id=$id') $h
    if ($colC -contains 'IdEgreso') {
      if ($c.Estado -eq 'Pagado') {
        Ejec 'UPDATE CuentasPagar SET Monto=$m, Pagado=$m, Estado=''Pagada'' WHERE IdEgreso=$g' @{ '$m'=[double]$tot; '$g'=$idG } }
      else {
        $existe = Escalar 'SELECT COUNT(*) FROM CuentasPagar WHERE IdEgreso=$g' @{ '$g'=$idG }
        $estC = if ($pag -gt 0) {'Parcial'} else {'Pendiente'}
        if ([long]$existe -gt 0) { Ejec 'UPDATE CuentasPagar SET Monto=$m, Pagado=$p, Estado=$e WHERE IdEgreso=$g' @{ '$m'=[double]$tot; '$p'=[double]$pag; '$e'=$estC; '$g'=$idG } }
        else {
          $p = $provPorRif[$g.Rif]
          $vence = ([datetime]::ParseExact($g.Fecha,'yyyy-MM-dd',$inv)).AddDays(30).ToString('yyyy-MM-dd')
          [void](Insertar 'CuentasPagar' $colC ([ordered]@{ CodigoProveedor=$p.Codigo; Proveedor=$p.Nombre; Concepto="Compra a $($p.Nombre)"; NumFactura="Egreso #$idG"
            Monto=[double]$tot; Pagado=[double]$pag; FechaEmision=$g.Fecha; FechaVence=$vence; Estado=$estC
            Observaciones="Generada desde el egreso #$idG (carga compras MP 2026)"; IdEgreso=$idG })) } } }
    Write-Host ("  Compra {0,2} {1} -> Gasto #{2} corregido: `${3}  {4}" -f $g.Compra, $g.Fecha, $idG, (N2 $tot), $c.Estado) -ForegroundColor Green
  }

  foreach ($g in $aCargar) {
    $p = $provPorRif[$g.Rif]
    $fle = [decimal](($g.Lineas | Measure-Object fl -Sum).Sum)
    $tot = TotalGrupo $g
    $pag = PagadoGrupo $g $tot ([decimal]0) ''
    $estado = if ($pag -ge $tot - 0.01) { 'Pagado' } else { 'Pendiente' }
    $tasa = Tasa $g.Fecha
    $docs = ($g.Lineas | ForEach-Object { $_.doc } | Where-Object { $_ } | Sort-Object -Unique) -join ', '
    $notas = ($g.Lineas | ForEach-Object { $_.obs } | Where-Object { $_ -and $_ -ne 'Sin nota de entrega' } | Sort-Object -Unique) -join ' | '
    $obs = @(); if ($docs) { $obs += "Doc: $docs" } else { $obs += 'Sin nota de entrega' }
    $obs += "Carga histórica compras MP 2026 (compra $($g.Compra))"; if ($notas) { $obs += $notas }
    $vence = ([datetime]::ParseExact($g.Fecha,'yyyy-MM-dd',$inv)).AddDays(30).ToString('yyyy-MM-dd')
    $concepto = "Compra a $($p.Nombre)"

    $idG = Insertar 'Gastos' $colG ([ordered]@{
      Fecha=$g.Fecha; IdCategoria=$idCat; Concepto=$concepto; Proveedor=$p.Nombre; CodigoProveedor=$p.Codigo; IdInsumo=$null
      Cantidad=[double](($g.Lineas | Measure-Object kg -Sum).Sum); MonedaOriginal='USD'
      TipoTasa= if ($tasa) {'BCV'} else {'USD_DIRECTO'}; TasaUsada= if ($tasa) {[double]$tasa} else {0.0}; IdTasaCambio=$null
      MontoOriginal=[double]$tot; MontoUSD=[double]$tot; MontoBs= if ($tasa) {[double][Math]::Round($tot*[decimal]$tasa,2)} else {0.0}
      PagadoUSD=[double]$pag; PrecioPendiente=0; Estado=$estado
      FechaVencimiento= if ($estado -eq 'Pendiente') {$vence} else {$null}; FechaPago= if ($pag -gt 0) {$g.Fecha} else {$null}
      RutaComprobante=''; Usuario=$USUARIO; FechaRegistro=$ahora; Observaciones=($obs -join ' | ')
      # columnas del esquema antiguo de Gastos, por si existen
      Categoria='Materia prima'; Descripcion=$concepto; Monto=[double]$tot })

    foreach ($l in $g.Lineas) {
      $a = $insumos[$l.ins]
      $desc = "$($a.Descripcion)" + $(if ($l.marca) { " ($($l.marca))" } else { '' })
      Ejec 'INSERT INTO DetallesGasto (IdGasto,Descripcion,IdArticulo,CodigoInterno,TipoInsumo,EsInventariable,Cantidad,PrecioUnitario,Subtotal) VALUES ($g,$d,$ia,$ci,''Materia prima'',1,$q,$p,$s)' @{
        '$g'=$idG; '$d'=$desc; '$ia'=[long]$a.Id; '$ci'=$l.ins; '$q'=[double]$l.kg; '$p'=[double]$l.p; '$s'=[double][Math]::Round([decimal]$l.kg*[decimal]$l.p,2) }
      Ejec 'INSERT INTO MovimientosInventario (FechaHora,CodigoProducto,TipoMovimiento,Concepto,Cantidad,SaldoAnterior,NuevoSaldo,Referencia,Usuario,TipoKardex,CostoUnitario) VALUES ($fh,$cp,''ENTRADA'',$co,$q,0,0,$re,$us,''MATERIA_PRIMA'',$cu)' @{
        '$fh'="$($g.Fecha) 08:00:00"; '$cp'=$l.ins; '$co'="Compra — Gasto #$idG"; '$q'=[double]$l.kg; '$re'="Gasto#$idG"; '$us'=$USUARIO; '$cu'=[double]$l.cu }
    }
    if ($fle -gt 0) {
      Ejec 'INSERT INTO DetallesGasto (IdGasto,Descripcion,IdArticulo,CodigoInterno,TipoInsumo,EsInventariable,Cantidad,PrecioUnitario,Subtotal) VALUES ($g,''Flete (repartido en el costo por kg)'',NULL,NULL,''Servicio'',0,1,$p,$p)' @{ '$g'=$idG; '$p'=[double]$fle } }

    if ($estado -eq 'Pendiente') {
      [void](Insertar 'CuentasPagar' $colC ([ordered]@{ CodigoProveedor=$p.Codigo; Proveedor=$p.Nombre; Concepto=$concepto; NumFactura="Egreso #$idG"
        Monto=[double]$tot; Pagado=[double]$pag; FechaEmision=$g.Fecha; FechaVence=$vence; Estado= if ($pag -gt 0) {'Parcial'} else {'Pendiente'}
        Observaciones="Generada desde el egreso #$idG (carga compras MP 2026). Doc: $docs"; IdEgreso=$idG })) }
    Write-Host ("  Compra {0,2} {1} -> Gasto #{2}  `${3}  {4}" -f $g.Compra, $g.Fecha, $idG, (N2 $tot), $estado) -ForegroundColor Green
  }

  # Recalcular Kardex MP con los movimientos reales (ya incluidos los nuevos)
  $kx = Filas @"
SELECT IdMovimiento, FechaHora, CodigoProducto AS Cod, TipoMovimiento, Concepto, Cantidad, SaldoAnterior, NuevoSaldo,
       COALESCE(Referencia,'') AS Referencia, COALESCE(CostoUnitario,0) AS CostoUnitario
FROM MovimientosInventario WHERE TipoKardex='MATERIA_PRIMA' AND CodigoProducto IN ($codes)
"@ $null
  foreach ($cod in 'INS-001','INS-002') {
    $movs = ArmarMovs $cod @(); $r = Recalcular $movs
    foreach ($m in $movs) {
      Ejec 'UPDATE MovimientosInventario SET SaldoAnterior=$sa, NuevoSaldo=$ns, CostoUnitario=$cu WHERE IdMovimiento=$id' @{
        '$sa'=[double]$m.SA; '$ns'=[double]$m.NS; '$cu'=[double]$m.CU; '$id'=$m.Id } }
    Ejec 'UPDATE ProductosProveedor SET StockActual=$s, CostoPromedio=$c, PrecioPromedio=$c, PrecioUltCompra=CASE WHEN $u>0 THEN $u ELSE PrecioUltCompra END WHERE CodigoInterno=$ci' @{
      '$s'=[double]$r.Saldo; '$c'=[double]$r.Cpp; '$u'=[double]$r.Ult; '$ci'=$cod }
    Write-Host ("  {0}: stock {1} kg, costo promedio {2}" -f $cod, (N2 $r.Saldo), $r.Cpp) -ForegroundColor Green
  }
  $script:tx.Commit()
  Write-Host ''; Write-Host 'LISTO: compras cargadas y Kardex de materia prima recalculado.' -ForegroundColor Green
  Write-Host "Si algo no cuadra, el respaldo está en: $resp" -ForegroundColor Green
}
catch {
  $script:tx.Rollback()
  Write-Host "[ERROR] $($_.Exception.Message)" -ForegroundColor Red
  Write-Host 'No se modificó nada (se deshizo la transacción).' -ForegroundColor Red
  exit 1
}
finally { $cn.Close() }
