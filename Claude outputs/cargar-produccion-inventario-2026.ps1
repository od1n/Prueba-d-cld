# ============================================================
#  cargar-produccion-inventario-2026.ps1
#  Carga la producción 2026 (semanas pagadas del 09/01 al 16/10/2026,
#  hoja Nominas) repartida por modelo, y deja cuadrados los Kardex:
#
#   - Un registro de producción por semana (fecha = viernes de pago; la
#     semana es de lunes a viernes), con el detalle por modelo y calidad
#     (primera, segunda, reciclado) y los obreros que trabajaron, para que
#     el sistema calcule el bono de esas semanas igual que la hoja Nominas.
#   - Reemplaza las tarifas de bono 2026 de esos productos por el historial
#     de la hoja Nominas (75, 100, 135 y 175 Bs por fuste según la fecha).
#   - Participantes según la plantilla real de obreros 2026 (ver $plantilla);
#     crea a Nicar (retirado) como obrero inactivo si no existe.
#   - Descuenta la resina virgen (peso x unidades, mitad 6200B y mitad
#     2706). Los reciclados no consumen resina comprada.
#   - Suma el producto terminado al costo de fabricación de la semana:
#     resina al costo promedio + mano de obra de producción de la nómina.
#   - Rehace las salidas por ventas con la fecha real de cada factura
#     (antes solo algunas tenían movimiento y con la fecha de carga).
#   - Ajusta la resina al inventario físico de hoy (la diferencia se
#     registra como existencia inicial al 01/01/2026, que no se conocía).
#   - Recalcula saldos y costo promedio de ambos Kardex y actualiza
#     existencias y costos en Inventario y ProductosProveedor.
#   - Actualiza los pesos (PesoKg) de los modelos indicados.
#
#  EJECUTAR EN OLIMPO, en PowerShell 7:
#   1) Revisar (no modifica nada):
#        & 'C:\SistemaGestion\Claude outputs\cargar-produccion-inventario-2026.ps1'
#   2) Aplicar (respalda la BD y carga):
#        & 'C:\SistemaGestion\Claude outputs\cargar-produccion-inventario-2026.ps1' -Aplicar
#
#  Se puede correr de nuevo: primero borra lo que cargó la vez anterior
#  (registros con la marca de esta carga y sus movimientos) y lo rehace.
# ============================================================
param([switch]$Aplicar)
$ErrorActionPreference = 'Stop'
$inv = [Globalization.CultureInfo]::InvariantCulture
$MARCA   = 'Carga producción 2026 (nómina semanal repartida por modelo)'
$USUARIO = 'Carga producción 2026'
$FECHA_FISICO = '2026-10-21'

# ── Producción semanal (hoja Nominas) ────────────────────────────────
# f=viernes de pago, fu=fustes, es=estribos (pares), emp=empleados de producción,
# tf/te = tarifa Bs por fuste/estribo, tasa = Bs por USD
$semanas = @(
 @{f='2026-01-09';fu=237;es=59;emp=3;tf=75;te=14;tasa=700}, @{f='2026-01-16';fu=250;es=55;emp=3;tf=75;te=14;tasa=470},
 @{f='2026-01-23';fu=271;es=68;emp=3;tf=75;te=14;tasa=510}, @{f='2026-01-30';fu=275;es=69;emp=3;tf=75;te=14;tasa=520},
 @{f='2026-02-06';fu=77;es=0;emp=3;tf=100;te=14;tasa=520},  @{f='2026-02-13';fu=93;es=0;emp=3;tf=100;te=14;tasa=540},
 @{f='2026-02-20';fu=100;es=0;emp=3;tf=100;te=20;tasa=570}, @{f='2026-02-27';fu=330;es=65;emp=3;tf=75;te=20;tasa=610},
 @{f='2026-03-06';fu=320;es=64;emp=3;tf=75;te=20;tasa=610}, @{f='2026-03-13';fu=200;es=40;emp=3;tf=75;te=20;tasa=610},
 @{f='2026-03-20';fu=114;es=28;emp=3;tf=100;te=25;tasa=653},@{f='2026-03-27';fu=0;es=0;emp=3;tf=75;te=20;tasa=660},
 @{f='2026-04-01';fu=80;es=8;emp=3;tf=100;te=25;tasa=640},  @{f='2026-04-10';fu=94;es=0;emp=3;tf=100;te=25;tasa=630},
 @{f='2026-04-17';fu=280;es=64;emp=3;tf=100;te=25;tasa=635},@{f='2026-04-24';fu=248;es=114;emp=3;tf=100;te=25;tasa=615},
 @{f='2026-04-30';fu=204;es=47;emp=3;tf=100;te=25;tasa=640},@{f='2026-05-08';fu=230;es=41;emp=3;tf=135;te=30;tasa=635},
 @{f='2026-05-15';fu=233;es=53;emp=3;tf=135;te=30;tasa=660},@{f='2026-05-22';fu=301;es=113;emp=3;tf=135;te=30;tasa=750},
 @{f='2026-05-29';fu=290;es=110;emp=3;tf=135;te=30;tasa=750},@{f='2026-06-05';fu=275;es=130;emp=3;tf=135;te=30;tasa=750},
 @{f='2026-06-12';fu=232;es=103;emp=3;tf=135;te=30;tasa=750},@{f='2026-06-19';fu=398;es=152;emp=4;tf=135;te=30;tasa=805},
 @{f='2026-06-26';fu=297;es=92;emp=4;tf=135;te=30;tasa=805}, @{f='2026-07-03';fu=245;es=85;emp=3;tf=135;te=30;tasa=830},
 @{f='2026-07-10';fu=319;es=117;emp=4;tf=135;te=30;tasa=830},@{f='2026-07-17';fu=260;es=100;emp=4;tf=135;te=30;tasa=830},
 @{f='2026-07-24';fu=236;es=112;emp=4;tf=135;te=30;tasa=850},@{f='2026-07-31';fu=290;es=100;emp=4;tf=135;te=25;tasa=850},
 @{f='2026-08-07';fu=295;es=139;emp=4;tf=135;te=30;tasa=840},@{f='2026-08-14';fu=340;es=163;emp=4;tf=135;te=30;tasa=880},
 @{f='2026-08-21';fu=342;es=163;emp=4;tf=135;te=30;tasa=960},@{f='2026-08-28';fu=152;es=58;emp=4;tf=135;te=200;tasa=960},
 @{f='2026-09-04';fu=276;es=0;emp=4;tf=135;te=200;tasa=960}, @{f='2026-09-11';fu=340;es=0;emp=4;tf=135;te=200;tasa=940},
 @{f='2026-09-18';fu=300;es=0;emp=4;tf=175;te=50;tasa=945}, @{f='2026-09-25';fu=400;es=99;emp=4;tf=175;te=50;tasa=960},
 @{f='2026-10-02';fu=350;es=87;emp=4;tf=175;te=50;tasa=960}, @{f='2026-10-09';fu=416;es=104;emp=4;tf=175;te=50;tasa=995},
 @{f='2026-10-16';fu=0;es=0;emp=4;tf=175;te=50;tasa=1000}
)

# ── Reparto por modelo: ventas 2026 + existencia física de hoy ───────
# c=calidad: P=primera, S=segunda, R=reciclado (no consume resina comprada)
$mixFustes = @(
 @{cod='101202';     u=4760+800; peso=3.65; c='P'; molde='Mc 12T 01'}
 @{cod='101202-2da'; u=1646+750; peso=3.65; c='S'; molde='Mc 12T 01'}
 @{cod='101202-rec'; u=116+700;  peso=3.65; c='R'; molde='Mc 12T 01'}
 @{cod='101201';     u=39;       peso=3.65; c='R'; molde='Mc 12T 01'}
 @{cod='101214';     u=278+50;   peso=3.95; c='P'; molde='Mc 14 01'}
 @{cod='101224-2da'; u=61;       peso=3.95; c='S'; molde='Mc 14 01'}
 @{cod='101224';     u=7;        peso=3.95; c='R'; molde='Mc 14 01'}
 @{cod='101001';     u=75+50;    peso=3.05; c='P'; molde='Mc 10 01'}
 @{cod='101400';     u=50+60;    peso=4.50; c='P'; molde='Mx gr 01'}
 @{cod='102110';     u=23;       peso=3.60; c='P'; molde='Tx peq 01'}
 @{cod='102100';     u=4;        peso=3.60; c='P'; molde='Tx peq 01'}
 @{cod='102101';     u=0+30;     peso=3.80; c='P'; molde='Tx West 01'}
 @{cod='102102';     u=1;        peso=3.80; c='P'; molde='Tx Ws H 01'}
 @{cod='101102-rec'; u=2;        peso=3.15; c='R'; molde='Mc 11 01'}
)
$mixEstribos = @(
 @{cod='441100';     u=2059+200; peso=0.5; c='P'; molde='Estr 4p 01'}
 @{cod='441100-2da'; u=18;       peso=0.5; c='S'; molde='Estr 4p 01'}
 @{cod='441000';     u=70;       peso=0.5; c='P'; molde='Estr 3p 01'}
)
$mixFustes = @($mixFustes | ForEach-Object { [pscustomobject]$_ }); $mixEstribos = @($mixEstribos | ForEach-Object { [pscustomobject]$_ })
# Existencia física al 21/10/2026 (solo para comparar al final)
$fisicoPT = @{ '101202'=800; '101202-2da'=750; '101202-rec'=700; '101001'=50; '101214'=50; '101400'=60; '102101'=30; '441100'=200 }
$fisicoResina = @{ 'INS-001'=1625; 'INS-002'=1625 }
$costoInicialResina = 2.10   # precio de la primera compra del año, para la existencia inicial estimada

# Pesos a actualizar en Inventario (mismo peso para primera, segunda y reciclado)
$pesos = @{}
foreach ($b in '101214','101224') { foreach ($s in '','-2da','-rec') { $pesos["$b$s"] = 3.95 } }
foreach ($s in '','-2da','-rec') { $pesos["101400$s"] = 4.50; $pesos["102100$s"] = 3.60; $pesos["102101$s"] = 3.80; $pesos["102102$s"] = 3.80
                                   $pesos["441000$s"] = 0.50; $pesos["441100$s"] = 0.50 }
$pesos['102110'] = 3.60

# ── Conexión ─────────────────────────────────────────────────────────
$db = [Environment]::GetEnvironmentVariable('SG_DB_PATH','Machine')
if (-not $db) { $db = 'C:\SistemaGestion\sistema_gestion.db' }
if (-not (Test-Path $db)) { Write-Host "[ERROR] No existe la BD: $db" -ForegroundColor Red; exit 1 }
$appDir = 'C:\SistemaGestion\publish\App'
Set-Location $appDir; $env:PATH = "$appDir;$env:PATH"
foreach ($d in 'SQLitePCLRaw.core.dll','SQLitePCLRaw.provider.e_sqlite3.dll','SQLitePCLRaw.batteries_v2.dll','Microsoft.Data.Sqlite.dll') {
  $p = Join-Path $appDir $d; if (Test-Path $p) { Add-Type -Path $p } }
try { [SQLitePCL.Batteries_V2]::Init() } catch {}
Write-Host "Base de datos: $db" -ForegroundColor Yellow
Write-Host ("Modo: " + $(if ($Aplicar) {'APLICAR'} else {'REVISAR (solo lectura)'})) -ForegroundColor Yellow
$cs = if ($Aplicar) { "Data Source=$db" } else { "Data Source=$db;Mode=ReadOnly;Cache=Shared" }
$cn = New-Object Microsoft.Data.Sqlite.SqliteConnection($cs); $cn.Open()
$pr = $cn.CreateCommand(); $pr.CommandText = 'PRAGMA busy_timeout=8000'; [void]$pr.ExecuteNonQuery()
$script:tx = $null

function Cmd([string]$sql, [hashtable]$prm) {
  $k = $cn.CreateCommand(); $k.CommandText = $sql; if ($script:tx) { $k.Transaction = $script:tx }
  if ($prm) { foreach ($n in $prm.Keys) { $v = $prm[$n]; if ($null -eq $v) { $v = [DBNull]::Value }; [void]$k.Parameters.AddWithValue($n, $v) } }
  return $k }
function Filas([string]$sql, [hashtable]$prm) {
  $r = (Cmd $sql $prm).ExecuteReader(); $out = New-Object System.Collections.Generic.List[object]
  while ($r.Read()) { $o = [ordered]@{}; for ($i=0; $i -lt $r.FieldCount; $i++) { $o[$r.GetName($i)] = if ($r.IsDBNull($i)) { $null } else { $r.GetValue($i) } }; $out.Add([pscustomobject]$o) }
  $r.Close(); return ,$out }
function Escalar([string]$sql, [hashtable]$prm) { $v = (Cmd $sql $prm).ExecuteScalar(); if ($v -is [DBNull]) { $null } else { $v } }
function Ejec([string]$sql, [hashtable]$prm) { [void](Cmd $sql $prm).ExecuteNonQuery() }
function Columnas([string]$t) { (Filas "PRAGMA table_info($t)" $null) | ForEach-Object { $_.name } }
function Fecha($s) { $t = [datetime]::MinValue; if ($s -and [datetime]::TryParse([string]$s, $inv, [Globalization.DateTimeStyles]::None, [ref]$t)) { $t } else { $null } }
function N0($x) { ([decimal]$x).ToString('N0', [Globalization.CultureInfo]::GetCultureInfo('es-VE')) }
function N2($x) { ([decimal]$x).ToString('N2', [Globalization.CultureInfo]::GetCultureInfo('es-VE')) }
function Titulo($t) { Write-Host ''; Write-Host "==== $t ====" -ForegroundColor Cyan }

# ── 1) Validaciones ──────────────────────────────────────────────────
$invCods = @{}; (Filas 'SELECT Codigo, Descripcion FROM Inventario' $null) | ForEach-Object { $invCods[[string]$_.Codigo] = [string]$_.Descripcion }
$faltan = @(($mixFustes + $mixEstribos) | Where-Object { -not $invCods.ContainsKey($_.cod) } | ForEach-Object { $_.cod })
if ($faltan.Count -gt 0) { Write-Host "[ERROR] No existen en Inventario: $($faltan -join ', ')" -ForegroundColor Red; exit 1 }
$colDet = Columnas 'DetalleProduccionMolde'; $colReg = Columnas 'RegistroProduccionDia'

# Carga anterior de este mismo script (se rehace)
$prev = Filas 'SELECT Id, Fecha FROM RegistroProduccionDia WHERE Observacion=$m' @{'$m'=$MARCA}
$fechasPrev = @($prev | ForEach-Object { [string]$_.Fecha })
$otrosReg = Filas 'SELECT Id, Fecha, COALESCE(Observacion,'''') AS Obs FROM RegistroProduccionDia WHERE Fecha>=''2026-01-01'' AND COALESCE(Observacion,'''')<>$m' @{'$m'=$MARCA}
$choques = @($otrosReg | Where-Object { $f = [string]$_.Fecha; $semanas | Where-Object { $_.f -eq $f } })
if ($otrosReg.Count -gt 0) {
  Titulo 'ATENCIÓN: ya hay registros de producción 2026 que no son de esta carga'
  $otrosReg | ForEach-Object { Write-Host "  #$($_.Id) $($_.Fecha) $($_.Obs)" -ForegroundColor Yellow } }
if ($choques.Count -gt 0) { Write-Host '[ALTO] Hay registros en fechas de esta carga; revísalos antes de seguir.' -ForegroundColor Red; exit 1 }
if ($prev.Count -gt 0) { Write-Host "Se encontró una carga anterior de este script ($($prev.Count) semanas): se borrará y se hará de nuevo." -ForegroundColor Yellow }
$refsProd = @{}; foreach ($w in $semanas) { $refsProd["PROD-$($w.f)"] = 1 }
$REF_AJ_INI = 'AJ-INI-2026'; $REF_AJ_FIS = "AJ-FIS-$FECHA_FISICO"

# ── 2) Reparto semanal por modelo (acumulado, sin perder unidades) ───
function Repartir($mix, [string]$campo) {
  $tot = ($mix | Measure-Object u -Sum).Sum; $acum = 0; $prevAs = @{}; foreach ($m in $mix) { $prevAs[$m.cod] = 0 }
  $res = @{}
  foreach ($w in $semanas) {
    $acum += $w[$campo]; $asig = @{}; $suma = 0
    $cuotas = foreach ($m in $mix) { $x = $acum * $m.u / $tot; [pscustomobject]@{ cod=$m.cod; ent=[Math]::Floor($x); frac=$x - [Math]::Floor($x) } }
    foreach ($q in $cuotas) { $asig[$q.cod] = $q.ent; $suma += $q.ent }
    foreach ($q in ($cuotas | Sort-Object frac -Descending | Select-Object -First ($acum - $suma))) { $asig[$q.cod]++ }
    $sem = @{}; foreach ($m in $mix) { $sem[$m.cod] = $asig[$m.cod] - $prevAs[$m.cod]; $prevAs[$m.cod] = $asig[$m.cod] }
    $res[$w.f] = $sem
  }
  return $res
}
$repF = Repartir $mixFustes 'fu'; $repE = Repartir $mixEstribos 'es'
$mixTodo = @{}; foreach ($m in ($mixFustes + $mixEstribos)) { $mixTodo[$m.cod] = $m }

# Detalle de producción por semana
$prod = New-Object System.Collections.Generic.List[object]
foreach ($w in $semanas) {
  if ($w.fu -le 0 -and $w.es -le 0) { continue }
  $lin = @(); $kgV = [decimal]0
  foreach ($m in $mixFustes)   { $q = $repF[$w.f][$m.cod]; if ($q -gt 0) { $lin += [pscustomobject]@{ cod=$m.cod; q=$q; m=$m; mo=[decimal]$w.emp*$w.tf/$w.tasa }; if ($m.c -ne 'R') { $kgV += [decimal]$q*[decimal]$m.peso } } }
  foreach ($m in $mixEstribos) { $q = $repE[$w.f][$m.cod]; if ($q -gt 0) { $lin += [pscustomobject]@{ cod=$m.cod; q=$q; m=$m; mo=[decimal]$w.emp*$w.te/$w.tasa }; $kgV += [decimal]$q*[decimal]$m.peso } }
  $prod.Add([pscustomobject]@{ f=$w.f; w=$w; lineas=$lin; kg=[Math]::Round($kgV,2); fh="$($w.f) 17:00:00" })
}

# ── 3) Kardex de resina: compras + consumo + ajuste a físico ─────────
$codesMP = @('INS-001','INS-002')
$kxMP = Filas @"
SELECT IdMovimiento, FechaHora, CodigoProducto AS Cod, TipoMovimiento, Cantidad, COALESCE(CostoUnitario,0) AS CostoUnitario, COALESCE(Referencia,'') AS Referencia
FROM MovimientosInventario WHERE TipoKardex='MATERIA_PRIMA' AND CodigoProducto IN ('INS-001','INS-002')
"@ $null
$kxMPbase = @($kxMP | Where-Object { -not $refsProd.ContainsKey($_.Referencia) -and $_.Referencia -ne $REF_AJ_INI -and $_.Referencia -ne $REF_AJ_FIS })
$raras = @($kxMPbase | Where-Object { $_.Referencia -like 'PROD-*' })
if ($raras.Count -gt 0) { Write-Host "[AVISO] Hay $($raras.Count) consumos de resina de otros registros de producción; se mantienen." -ForegroundColor Yellow }

$ajuste = @{}
foreach ($c in $codesMP) {
  $ent = [decimal](($kxMPbase | Where-Object { $_.Cod -eq $c -and $_.TipoMovimiento -eq 'ENTRADA' } | Measure-Object Cantidad -Sum).Sum)
  $sal = [decimal](($kxMPbase | Where-Object { $_.Cod -eq $c -and $_.TipoMovimiento -eq 'SALIDA' } | Measure-Object Cantidad -Sum).Sum)
  $cons = [decimal](($prod | ForEach-Object { $_.kg / 2 } | Measure-Object -Sum).Sum)
  $ajuste[$c] = [Math]::Round([decimal]$fisicoResina[$c] - ($ent - $sal - $cons), 4)
}

# Movimientos a simular (orden cronológico)
function MovsMP([string]$cod) {
  $l = New-Object System.Collections.Generic.List[object]
  foreach ($k in ($kxMPbase | Where-Object { $_.Cod -eq $cod })) {
    $l.Add([pscustomobject]@{ Id=[long]$k.IdMovimiento; Fh=(Fecha $k.FechaHora); Tipo=[string]$k.TipoMovimiento; Cantidad=[decimal]$k.Cantidad; Costo=[decimal]$k.CostoUnitario; Ref=[string]$k.Referencia; SA=0; NS=0; CU=0 }) }
  $i = 0
  foreach ($p in $prod) { $i++; $l.Add([pscustomobject]@{ Id=[long](9e15+$i); Fh=(Fecha $p.fh); Tipo='SALIDA'; Cantidad=[decimal]($p.kg/2); Costo=0; Ref="PROD-$($p.f)"; SA=0; NS=0; CU=0 }) }
  $a = $ajuste[$cod]
  if ($a -gt 0) { $l.Add([pscustomobject]@{ Id=[long]1; Fh=[datetime]'2026-01-01'; Tipo='ENTRADA'; Cantidad=$a; Costo=[decimal]$costoInicialResina; Ref=$REF_AJ_INI; SA=0; NS=0; CU=0 }) }
  elseif ($a -lt 0) { $l.Add([pscustomobject]@{ Id=[long](9.9e15); Fh=(Fecha "$FECHA_FISICO 18:00:00"); Tipo='SALIDA'; Cantidad=-$a; Costo=0; Ref=$REF_AJ_FIS; SA=0; NS=0; CU=0 }) }
  return ,@($l | Sort-Object Fh, Id)
}
function Recalc($movs) {
  $saldo = [decimal]0; $cpp = [decimal]0; $neg = $null
  foreach ($m in $movs) {
    $q = [decimal]$m.Cantidad; $m.SA = $saldo
    if ($m.Tipo -eq 'ENTRADA') {
      $cu = if ([decimal]$m.Costo -gt 0) { [decimal]$m.Costo } else { $cpp }
      $base = [Math]::Max($saldo, [decimal]0)
      $cpp = if (($base + $q) -gt 0 -and $cpp -gt 0 -and $base -gt 0) { [Math]::Round(($base*$cpp + $q*$cu)/($base+$q), 4) } else { $cu }
      $saldo += $q; $m.CU = $cu
    } else { $saldo -= $q; $m.CU = $cpp; if ($saldo -lt -0.01 -and -not $neg) { $neg = $m.Fh } }
    $m.NS = $saldo
  }
  return @{ Saldo=$saldo; Cpp=$cpp; Neg=$neg }
}
$simMP = @{}; $cppPorFecha = @{}
foreach ($c in $codesMP) { $mv = MovsMP $c; $r = Recalc $mv; $simMP[$c] = @{ Movs=$mv; Res=$r }
  foreach ($m in ($mv | Where-Object { $_.Ref -like 'PROD-*' -and $refsProd.ContainsKey($_.Ref) })) { $cppPorFecha["$c|$($m.Ref)"] = [decimal]$m.CU } }

# Costo de fabricación por pieza: resina (promedio de los dos tipos) + mano de obra
foreach ($p in $prod) {
  $cppMezcla = ([decimal]$cppPorFecha["INS-001|PROD-$($p.f)"] + [decimal]$cppPorFecha["INS-002|PROD-$($p.f)"]) / 2
  foreach ($l in $p.lineas) {
    $mat = if ($l.m.c -eq 'R') { [decimal]0 } else { [decimal]$l.m.peso * $cppMezcla }
    $l | Add-Member -NotePropertyName costo -NotePropertyValue ([Math]::Round($mat + $l.mo, 4)) -Force
  }
}

# ── 4) Ventas: salidas del Kardex de producto con la fecha de la factura ─
$fact = Filas @"
SELECT f.NumeroF, substr(f.Fecha,1,10) AS Fecha, d.Codigo, d.Cantidad, COALESCE(f.Anulada,0) AS Anulada
FROM DetallesFactura d JOIN Facturas f ON f.rowid=d.IdFactura WHERE f.Fecha >= '2026-01-01'
"@ $null
$numF = @{}; foreach ($x in $fact) { $numF[[string]$x.NumeroF] = 1 }
$ventas = @($fact | Where-Object { [long]$_.Anulada -eq 0 -and [double]$_.Cantidad -gt 0 -and $invCods.ContainsKey([string]$_.Codigo) })

$kxPT = Filas @"
SELECT IdMovimiento, FechaHora, CodigoProducto AS Cod, TipoMovimiento, Cantidad, COALESCE(CostoUnitario,0) AS CostoUnitario,
       COALESCE(Referencia,'') AS Referencia, COALESCE(Concepto,'') AS Concepto
FROM MovimientosInventario WHERE COALESCE(TipoKardex,'PRODUCTO')='PRODUCTO' AND FechaHora >= '2026-01-01'
"@ $null
function LigadoFactura($k) {
  if ($numF.ContainsKey([string]$k.Referencia)) { return $true }
  if ($k.Concepto -match 'Factura\s+(\S+)\s*$' -and $numF.ContainsKey($Matches[1])) { return $true }
  return $false }
$kxPTfact = @($kxPT | Where-Object { LigadoFactura $_ })
$kxPTprod = @($kxPT | Where-Object { $refsProd.ContainsKey([string]$_.Referencia) })
$kxPTotros = @($kxPT | Where-Object { -not (LigadoFactura $_) -and -not $refsProd.ContainsKey([string]$_.Referencia) })
$antes2026 = [long](Escalar "SELECT COUNT(*) FROM MovimientosInventario WHERE COALESCE(TipoKardex,'PRODUCTO')='PRODUCTO' AND FechaHora < '2026-01-01'" $null)

$codsPT = @{}; foreach ($m in ($mixFustes + $mixEstribos)) { $codsPT[$m.cod] = 1 }; foreach ($v in $ventas) { $codsPT[[string]$v.Codigo] = 1 }
foreach ($k in $kxPTotros) { $codsPT[[string]$k.Cod] = 1 }

function MovsPT([string]$cod) {
  $l = New-Object System.Collections.Generic.List[object]
  foreach ($k in ($kxPTotros | Where-Object { $_.Cod -eq $cod })) {
    $l.Add([pscustomobject]@{ Id=[long]$k.IdMovimiento; Fh=(Fecha $k.FechaHora); Tipo=[string]$k.TipoMovimiento; Cantidad=[decimal]$k.Cantidad; Costo=[decimal]$k.CostoUnitario; Ref=[string]$k.Referencia; Conc=[string]$k.Concepto; Nuevo=$false; SA=0; NS=0; CU=0 }) }
  $i = 0
  foreach ($p in $prod) { foreach ($x in ($p.lineas | Where-Object { $_.cod -eq $cod })) { $i++
    $l.Add([pscustomobject]@{ Id=[long](8e15+$i); Fh=(Fecha $p.fh); Tipo='ENTRADA'; Cantidad=[decimal]$x.q; Costo=[decimal]$x.costo; Ref="PROD-$($p.f)"
      Conc="Produccion $(([datetime]$p.f).ToString('dd/MM/yyyy')) (carga semanal nómina)"; Nuevo=$true; SA=0; NS=0; CU=0 }) } }
  foreach ($v in ($ventas | Where-Object { $_.Codigo -eq $cod })) { $i++
    $l.Add([pscustomobject]@{ Id=[long](8.5e15+$i); Fh=(Fecha "$($v.Fecha) 12:00:00"); Tipo='SALIDA'; Cantidad=[decimal]$v.Cantidad; Costo=0; Ref=[string]$v.NumeroF
      Conc="Venta Factura $($v.NumeroF)"; Nuevo=$true; SA=0; NS=0; CU=0 }) }
  return ,@($l | Sort-Object Fh, Id)
}
$simPT = @{}
foreach ($c in $codsPT.Keys) { $mv = MovsPT $c; $simPT[$c] = @{ Movs=$mv; Res=(Recalc $mv) } }

# ── 4b) Obreros participantes y tarifas de bono ───────────────────────
# Plantilla de obreros de producción 2026 (confirmada por Jacques):
#   José Román y Jeison Paredes todo el año; Nicar (retirado) hasta la semana del 31/07;
#   Juan Silva desde la semana del 19/06; Omar Javier desde la semana del 07/08 (entró por Nicar).
$plantilla = @(
  [pscustomobject]@{ clave='ROMAN';  desde=$null;        hasta=$null;        orden=1 }
  [pscustomobject]@{ clave='JEISON'; desde=$null;        hasta=$null;        orden=2 }
  [pscustomobject]@{ clave='NICAR';  desde=$null;        hasta='2026-07-31'; orden=3 }
  [pscustomobject]@{ clave='SILVA';  desde='2026-06-19'; hasta=$null;        orden=4 }
  [pscustomobject]@{ clave='OMAR';   desde='2026-08-07'; hasta=$null;        orden=5 }
)
function SinAcentos([string]$t) { $n = $t.Normalize([Text.NormalizationForm]::FormD)
  (-join ($n.ToCharArray() | Where-Object { [Globalization.CharUnicodeInfo]::GetUnicodeCategory($_) -ne 'NonSpacingMark' })).ToUpper() }
$emps = Filas 'SELECT Id, COALESCE(Nombre,'''') || '' '' || COALESCE(Apellido,'''') AS Nombre, COALESCE(Activo,1) AS Activo, COALESCE(TipoEmpleado,'''') AS Tipo FROM Empleados ORDER BY Id' $null
$crearNicar = $false
foreach ($x in $plantilla) {
  $m = @($emps | Where-Object { (SinAcentos $_.Nombre) -match "\b$($x.clave)\b" })
  if ($m.Count -gt 1) { $m = @($m | Where-Object { $_.Tipo -eq 'Obrero' }) }
  if ($m.Count -eq 1) { $x | Add-Member Id ([long]$m[0].Id); $x | Add-Member Nombre ([string]$m[0].Nombre).Trim() }
  elseif ($m.Count -eq 0 -and $x.clave -eq 'NICAR') { $crearNicar = $true; $x | Add-Member Id ([long]-1); $x | Add-Member Nombre 'NICAR (se crea como obrero inactivo)' }
  else { Write-Host "[ERROR] No pude identificar un único empleado para '$($x.clave)' (encontrados: $($m.Count))." -ForegroundColor Red; $m | ForEach-Object { Write-Host "   Id $($_.Id) $($_.Nombre)" }; exit 1 }
}
foreach ($p in $prod) {
  $part = @($plantilla | Where-Object { (-not $_.desde -or $_.desde -le $p.f) -and (-not $_.hasta -or $_.hasta -ge $p.f) } | Sort-Object orden)
  # Si la hoja Nominas usó menos obreros esa semana (alguien faltó), se reparte entre esa
  # cantidad: sale el de ingreso más reciente.
  if ($part.Count -gt $p.w.emp) { $part = @($part | Select-Object -First $p.w.emp) }
  $p | Add-Member -NotePropertyName part -NotePropertyValue $part -Force
}
# Tarifas: periodos consecutivos con la misma tarifa (desde el lunes de la primera semana)
function Periodos([string]$campo) {
  $per = @(); $act = $null
  foreach ($w in ($semanas | Where-Object { $_.fu -gt 0 -or $_.es -gt 0 })) {
    $lun = ([datetime]::ParseExact($w.f,'yyyy-MM-dd',$inv)).AddDays(-4).ToString('yyyy-MM-dd')
    if ($act -and $act.t -eq $w[$campo]) { $act.hasta = $w.f } else { if ($act) { $per += $act }; $act = @{ t=$w[$campo]; desde=$lun; hasta=$w.f } } }
  if ($act) { $per += $act }
  # que los periodos queden pegados (cada uno empieza el lunes siguiente al anterior) y el último abierto
  for ($i=0; $i -lt $per.Count - 1; $i++) { $per[$i].hasta = ([datetime]::ParseExact($per[$i+1].desde,'yyyy-MM-dd',$inv)).AddDays(-1).ToString('yyyy-MM-dd') }
  $per[-1].hasta = $null
  return ,$per }
$perF = Periodos 'tf'; $perE = Periodos 'te'
$tarifasExist = Filas 'SELECT CodigoProducto, TarifaBs, FechaDesde, FechaHasta FROM TarifaProductoProduccion' $null
$tarifasNuevas = @(); $tarifasRecortar = @(); $tarifasBorrar = @()
$INI_TARIFA = (@($perF) + @($perE) | ForEach-Object { $_.desde } | Sort-Object | Select-Object -First 1)
foreach ($m in ($mixFustes + $mixEstribos)) {
  $per = if ($mixEstribos.cod -contains $m.cod) { $perE } else { $perF }
  # Se reemplaza el historial 2026 por el de la hoja: lo que empieza desde el primer lunes se borra
  # y lo que venía de antes y seguía vigente se cierra el día anterior.
  foreach ($e in @($tarifasExist | Where-Object { $_.CodigoProducto -eq $m.cod })) {
    if ([string]$e.FechaDesde -ge $INI_TARIFA) { $tarifasBorrar += $e }
    elseif (-not $e.FechaHasta -or [string]$e.FechaHasta -ge $INI_TARIFA) { $tarifasRecortar += $e } }
  foreach ($x in $per) { $tarifasNuevas += [pscustomobject]@{ cod=$m.cod; t=$x.t; desde=$x.desde; hasta=$x.hasta } }
}
$FIN_TARIFA_PREVIA = ([datetime]::ParseExact($INI_TARIFA,'yyyy-MM-dd',$inv)).AddDays(-1).ToString('yyyy-MM-dd')

# ── 5) Reporte ───────────────────────────────────────────────────────
Titulo 'Obreros participantes por semana (vs. empleados de la hoja Nominas)'
foreach ($p in $prod) { $n = $p.part.Count
  $col = if ($n -ne $p.w.emp) { 'Yellow' } else { 'Gray' }
  Write-Host ("  {0}  sistema {1}  nómina {2}  {3}" -f $p.f, $n, $p.w.emp, (($p.part | ForEach-Object { $_.Nombre.Trim() }) -join ', ')) -ForegroundColor $col }
Titulo 'Tarifas de bono de producción (se reemplaza el historial 2026 por el de la hoja Nominas)'
if ($tarifasBorrar.Count -gt 0 -or $tarifasRecortar.Count -gt 0) {
  Write-Host "  Tarifas actuales que se quitan: $($tarifasBorrar.Count); que se cierran el ${FIN_TARIFA_PREVIA}: $($tarifasRecortar.Count)"
  (@($tarifasBorrar) + @($tarifasRecortar)) | Group-Object CodigoProducto | ForEach-Object { Write-Host ("    {0}: {1}" -f $_.Name, (($_.Group | ForEach-Object { "$($_.TarifaBs) Bs desde $($_.FechaDesde)" + $(if ($_.FechaHasta) {" hasta $($_.FechaHasta)"} else {''}) }) -join '; ')) } }
Write-Host "  Historial que se crea para $(@($tarifasNuevas | Select-Object -ExpandProperty cod -Unique).Count) productos:"
Write-Host ("    Fustes:   " + (($perF | ForEach-Object { "$($_.t) Bs desde $($_.desde)" }) -join '; '))
Write-Host ("    Estribos: " + (($perE | ForEach-Object { "$($_.t) Bs desde $($_.desde)" }) -join '; '))
Write-Host ''
Write-Host '  Bono de producción que calculará el sistema (piezas x tarifa, igual para cada obrero):'
foreach ($p in $prod) {
  $bs = [decimal]0; foreach ($l in $p.lineas) { $bs += [decimal]$l.q * $(if ($mixEstribos.cod -contains $l.cod) { $p.w.te } else { $p.w.tf }) }
  Write-Host ("    {0}  {1,9} Bs por obrero x {2} = {3,10} Bs" -f $p.f, (N0 $bs), $p.part.Count, (N0 ($bs * $p.part.Count))) }

Titulo 'Producción por semana (repartida por modelo)'
foreach ($p in $prod) {
  $fu = ($p.lineas | Where-Object { $mixFustes.cod -contains $_.cod } | Measure-Object q -Sum).Sum
  $es = ($p.lineas | Where-Object { $mixEstribos.cod -contains $_.cod } | Measure-Object q -Sum).Sum
  $c12 = ($p.lineas | Where-Object { $_.cod -eq '101202' } | Select-Object -First 1).costo
  $mo = ($p.lineas | ForEach-Object { [decimal]$_.q * $_.mo } | Measure-Object -Sum).Sum
  Write-Host ("  {0}  fustes {1,4}  estribos {2,4}  resina {3,9} kg  mano de obra `${4,8}  costo Mc 12`" 1ra `${5}" -f $p.f, $fu, $es, (N2 $p.kg), (N2 $mo), $c12)
}
Titulo 'Producción total por modelo'
foreach ($m in ($mixFustes + $mixEstribos)) {
  $q = ($prod | ForEach-Object { $_.lineas } | Where-Object { $_.cod -eq $m.cod } | Measure-Object q -Sum).Sum
  Write-Host ("  {0,-11} {1,-42} {2,6}  ({3})" -f $m.cod, $invCods[$m.cod], (N0 $q), $(switch ($m.c) { 'P' {'primera'} 'S' {'segunda'} 'R' {'reciclado'} }))
}
$kgTot = ($prod | Measure-Object kg -Sum).Sum
Titulo 'Resina'
Write-Host ("  Consumo según pesos: {0} kg ({1} kg de cada tipo)" -f (N2 $kgTot), (N2 ($kgTot/2)))
foreach ($c in $codesMP) { $r = $simMP[$c].Res; $a = $ajuste[$c]
  $txt = if ($a -gt 0) { "existencia inicial estimada al 01/01/2026: $(N2 $a) kg a $costoInicialResina" } elseif ($a -lt 0) { "merma / ajuste a físico: $(N2 (-$a)) kg menos" } else { 'sin ajuste' }
  Write-Host ("  {0}: queda {1} kg (físico {2}) a costo promedio {3}  |  {4}" -f $c, (N2 $r.Saldo), (N2 $fisicoResina[$c]), $r.Cpp, $txt)
  if ($r.Neg) { Write-Host "      Saldo negativo desde $($r.Neg)" -ForegroundColor Yellow } }

Titulo 'Producto terminado: existencia resultante vs. física'
$vendidoPor = @{}; foreach ($v in $ventas) { $vendidoPor[[string]$v.Codigo] = [decimal]$vendidoPor[[string]$v.Codigo] + [decimal]$v.Cantidad }
foreach ($c in ($codsPT.Keys | Sort-Object)) {
  $r = $simPT[$c].Res; $prodQ = ($prod | ForEach-Object { $_.lineas } | Where-Object { $_.cod -eq $c } | Measure-Object q -Sum).Sum
  $fis = if ($fisicoPT.ContainsKey($c)) { N0 $fisicoPT[$c] } else { '-' }
  $color = if ($r.Saldo -lt 0) { 'Yellow' } else { 'Gray' }
  Write-Host ("  {0,-11} {1,-40} producido {2,6}  vendido {3,6}  queda {4,6}  físico {5,5}  costo {6}" -f $c, ([string]$invCods[$c]).Substring(0,[Math]::Min(40,([string]$invCods[$c]).Length)), (N0 $prodQ), (N0 $vendidoPor[$c]), (N0 $r.Saldo), $fis, $r.Cpp) -ForegroundColor $color
  if ($r.Neg) { Write-Host "      saldo negativo desde $($r.Neg.ToString('yyyy-MM-dd'))" -ForegroundColor Yellow } }
Write-Host ''
Write-Host "  Faltan en el sistema las facturas desde el 23/09/2026: por eso lo que queda es mayor que lo físico." -ForegroundColor DarkGray
Write-Host ("  Salidas por ventas que se rehacen: {0} movimientos actuales se reemplazan por {1} (una por línea de factura, con su fecha)." -f $kxPTfact.Count, $ventas.Count)
if ($antes2026 -gt 0) { Write-Host "  Hay $antes2026 movimientos de producto anteriores a 2026: no se tocan y el saldo 2026 arranca en 0." -ForegroundColor Yellow }

if (-not $Aplicar) {
  Write-Host ''; Write-Host 'Nada fue modificado. Para aplicar:' -ForegroundColor Green
  Write-Host "  & 'C:\SistemaGestion\Claude outputs\cargar-produccion-inventario-2026.ps1' -Aplicar" -ForegroundColor Green
  $cn.Close(); exit 0 }

# ── 6) APLICAR ───────────────────────────────────────────────────────
$resp = Join-Path (Split-Path $db) ("respaldo-antes-produccion-2026-{0}.db" -f (Get-Date -Format 'yyyyMMdd-HHmmss'))
Ejec "VACUUM INTO '$($resp.Replace("'","''"))'" $null
Titulo "Respaldo creado: $resp"

$script:tx = $cn.BeginTransaction()
try {
  # a) Borrar la carga anterior de este script
  foreach ($r in $prev) {
    Ejec 'DELETE FROM DetalleProduccionMolde WHERE IdRegistro=$i' @{'$i'=$r.Id}
    Ejec 'DELETE FROM MaterialProduccionDia WHERE IdRegistro=$i' @{'$i'=$r.Id}
    Ejec 'DELETE FROM ParticipantesProduccionDia WHERE IdRegistro=$i' @{'$i'=$r.Id}
    Ejec 'DELETE FROM RegistroProduccionDia WHERE Id=$i' @{'$i'=$r.Id} }
  foreach ($ref in $refsProd.Keys) { Ejec 'DELETE FROM MovimientosInventario WHERE Referencia=$r' @{'$r'=$ref} }
  Ejec 'DELETE FROM MovimientosInventario WHERE TipoKardex=''MATERIA_PRIMA'' AND Referencia IN ($a,$b)' @{'$a'=$REF_AJ_INI; '$b'=$REF_AJ_FIS}
  # b) Quitar las salidas por ventas actuales (se rehacen con la fecha de la factura)
  foreach ($k in $kxPTfact) { Ejec 'DELETE FROM MovimientosInventario WHERE IdMovimiento=$i' @{'$i'=[long]$k.IdMovimiento} }

  # c0) Nicar (retirado) como obrero inactivo, si no existe
  if ($crearNicar) {
    $colE = Columnas 'Empleados'
    $v = [ordered]@{ Cedula='SIN-CEDULA-NICAR'; Nombre='NICAR'; Apellido=''; Cargo='Obrero (retirado)'; TipoEmpleado='Obrero'; FechaIngreso='2026-01-01'; Activo=0; EligibleBonoProd=1 }
    $usar = @($v.Keys | Where-Object { $colE -contains $_ }); $h = @{}; foreach ($k in $usar) { $h['$' + $k] = $v[$k] }
    Ejec ('INSERT INTO Empleados (' + ($usar -join ',') + ') VALUES (' + (($usar | ForEach-Object { '$' + $_ }) -join ',') + ')') $h
    $idN = [long](Escalar 'SELECT last_insert_rowid()' $null)
    ($plantilla | Where-Object { $_.clave -eq 'NICAR' }).Id = $idN
    Write-Host "  Empleado creado: NICAR (Id $idN, inactivo)" -ForegroundColor Green }

  # c1) Tarifas de bono: historial 2026 de la hoja Nominas
  foreach ($e in $tarifasBorrar) { Ejec 'DELETE FROM TarifaProductoProduccion WHERE CodigoProducto=$c AND FechaDesde=$d AND TarifaBs=$t' @{'$c'=$e.CodigoProducto; '$d'=$e.FechaDesde; '$t'=$e.TarifaBs} }
  foreach ($e in $tarifasRecortar) { Ejec 'UPDATE TarifaProductoProduccion SET FechaHasta=$h WHERE CodigoProducto=$c AND FechaDesde=$d AND TarifaBs=$t' @{'$h'=$FIN_TARIFA_PREVIA; '$c'=$e.CodigoProducto; '$d'=$e.FechaDesde; '$t'=$e.TarifaBs} }
  foreach ($t in $tarifasNuevas) {
    Ejec 'INSERT INTO TarifaProductoProduccion (CodigoProducto,TarifaBs,FechaDesde,FechaHasta,Usuario) VALUES ($c,$t,$d,$h,$u)' @{'$c'=$t.cod; '$t'=[double]$t.t; '$d'=$t.desde; '$h'=$t.hasta; '$u'=$USUARIO} }

  # c) Pesos
  foreach ($c in $pesos.Keys) { Ejec 'UPDATE Inventario SET PesoKg=$p WHERE Codigo=$c' @{'$p'=[double]$pesos[$c]; '$c'=$c} }

  # d) Registros de producción + detalle + material
  foreach ($p in $prod) {
    if ($colReg -contains 'TipoMaterial') { Ejec 'INSERT INTO RegistroProduccionDia (Fecha,TipoMaterial,Observacion) VALUES ($f,''Virgen'',$o)' @{'$f'=$p.f; '$o'=$MARCA} }
    else { Ejec 'INSERT INTO RegistroProduccionDia (Fecha,Observacion) VALUES ($f,$o)' @{'$f'=$p.f; '$o'=$MARCA} }
    $idR = [long](Escalar 'SELECT last_insert_rowid()' $null)
    foreach ($l in $p.lineas) {
      $v = [ordered]@{ IdRegistro=$idR; CodigoMolde=$l.m.molde; CodigoProducto=$l.cod; Cantidad=[double]$l.q
        CantPrimera=[double]$(if ($l.m.c -eq 'P') {$l.q} else {0}); CantSegunda=[double]$(if ($l.m.c -eq 'S') {$l.q} else {0})
        CantReciclado=[double]$(if ($l.m.c -eq 'R') {$l.q} else {0}); CantDefectuosas=0.0 }
      $usar = @($v.Keys | Where-Object { $colDet -contains $_ }); $h = @{}; foreach ($k in $usar) { $h['$' + $k] = $v[$k] }
      Ejec ('INSERT INTO DetalleProduccionMolde (' + ($usar -join ',') + ') VALUES (' + (($usar | ForEach-Object { '$' + $_ }) -join ',') + ')') $h }
    foreach ($c in $codesMP) { Ejec 'INSERT INTO MaterialProduccionDia (IdRegistro,CodigoInsumo,Cantidad) VALUES ($i,$c,$q)' @{'$i'=$idR; '$c'=$c; '$q'=[double]($p.kg/2)} }
    foreach ($o in $p.part) { Ejec 'INSERT INTO ParticipantesProduccionDia (IdRegistro,IdEmpleado) VALUES ($i,$e)' @{'$i'=$idR; '$e'=[long]$o.Id} }
  }

  # e) Movimientos nuevos de resina (consumo y ajuste)
  $insMov = 'INSERT INTO MovimientosInventario (FechaHora,CodigoProducto,TipoMovimiento,Concepto,Cantidad,SaldoAnterior,NuevoSaldo,Referencia,Usuario,TipoKardex,CostoUnitario) VALUES ($fh,$cp,$tm,$co,$q,0,0,$re,$us,$tk,$cu)'
  foreach ($c in $codesMP) {
    foreach ($p in $prod) { Ejec $insMov @{'$fh'=$p.fh; '$cp'=$c; '$tm'='SALIDA'; '$co'="Produccion $(([datetime]$p.f).ToString('dd/MM/yyyy')) (Virgen)"; '$q'=[double]($p.kg/2); '$re'="PROD-$($p.f)"; '$us'=$USUARIO; '$tk'='MATERIA_PRIMA'; '$cu'=0.0} }
    $a = $ajuste[$c]
    if ($a -gt 0) { Ejec $insMov @{'$fh'='2026-01-01 00:00:00'; '$cp'=$c; '$tm'='ENTRADA'; '$co'='Existencia inicial estimada al 01/01/2026 (cuadre con inventario físico del 21/10/2026)'; '$q'=[double]$a; '$re'=$REF_AJ_INI; '$us'=$USUARIO; '$tk'='MATERIA_PRIMA'; '$cu'=[double]$costoInicialResina} }
    elseif ($a -lt 0) { Ejec $insMov @{'$fh'="$FECHA_FISICO 18:00:00"; '$cp'=$c; '$tm'='SALIDA'; '$co'='Merma / ajuste a inventario físico 21/10/2026'; '$q'=[double](-$a); '$re'=$REF_AJ_FIS; '$us'=$USUARIO; '$tk'='MATERIA_PRIMA'; '$cu'=0.0} }
  }

  # f) Movimientos nuevos de producto terminado (producción y ventas)
  foreach ($c in $codsPT.Keys) { foreach ($m in ($simPT[$c].Movs | Where-Object { $_.Nuevo })) {
    Ejec $insMov @{'$fh'=$m.Fh.ToString('yyyy-MM-dd HH:mm:ss'); '$cp'=$c; '$tm'=$m.Tipo; '$co'=$m.Conc; '$q'=[double]$m.Cantidad; '$re'=$m.Ref; '$us'=$USUARIO; '$tk'='PRODUCTO'; '$cu'=[double]$m.Costo} } }

  # g) Recalcular ambos Kardex desde la BD y actualizar maestros
  function RecalcBD([string]$cod, [string]$tk) {
    $rows = Filas 'SELECT IdMovimiento, FechaHora, TipoMovimiento, Cantidad, COALESCE(CostoUnitario,0) AS CostoUnitario FROM MovimientosInventario WHERE CodigoProducto=$c AND COALESCE(TipoKardex,''PRODUCTO'')=$tk AND FechaHora >= $d' @{'$c'=$cod; '$tk'=$tk; '$d'=$(if ($tk -eq 'PRODUCTO') {'2026-01-01'} else {'0000'})}
    $mv = @($rows | ForEach-Object { [pscustomobject]@{ Id=[long]$_.IdMovimiento; Fh=(Fecha $_.FechaHora); Tipo=[string]$_.TipoMovimiento; Cantidad=[decimal]$_.Cantidad; Costo=[decimal]$_.CostoUnitario; SA=0; NS=0; CU=0 } } | Sort-Object Fh, Id)
    $r = Recalc $mv
    foreach ($m in $mv) { Ejec 'UPDATE MovimientosInventario SET SaldoAnterior=$sa, NuevoSaldo=$ns, CostoUnitario=$cu WHERE IdMovimiento=$i' @{'$sa'=[double]$m.SA; '$ns'=[double]$m.NS; '$cu'=[double]$m.CU; '$i'=$m.Id} }
    return $r }
  foreach ($c in $codesMP) { $r = RecalcBD $c 'MATERIA_PRIMA'
    Ejec 'UPDATE ProductosProveedor SET StockActual=$s, CostoPromedio=$p, PrecioPromedio=$p WHERE CodigoInterno=$c' @{'$s'=[double]$r.Saldo; '$p'=[double]$r.Cpp; '$c'=$c}
    Write-Host ("  {0}: {1} kg a {2}" -f $c, (N2 $r.Saldo), $r.Cpp) -ForegroundColor Green }
  foreach ($c in $codsPT.Keys) { $r = RecalcBD $c 'PRODUCTO'
    if ($r.Cpp -gt 0) { Ejec 'UPDATE Inventario SET Existencia=$s, CostoActual=$p, UltimaActualz=$f WHERE Codigo=$c' @{'$s'=[double]$r.Saldo; '$p'=[double][Math]::Round($r.Cpp,4); '$f'=$FECHA_FISICO; '$c'=$c} }
    else { Ejec 'UPDATE Inventario SET Existencia=$s, UltimaActualz=$f WHERE Codigo=$c' @{'$s'=[double]$r.Saldo; '$f'=$FECHA_FISICO; '$c'=$c} } }

  $script:tx.Commit()
  Write-Host ''; Write-Host "LISTO: producción 2026 cargada ($($prod.Count) semanas) y Kardex recalculados." -ForegroundColor Green
  Write-Host "Respaldo: $resp" -ForegroundColor Green
}
catch {
  $script:tx.Rollback()
  Write-Host "[ERROR] $($_.Exception.Message)" -ForegroundColor Red
  Write-Host "  en línea $($_.InvocationInfo.ScriptLineNumber)" -ForegroundColor Red
  Write-Host 'No se modificó nada (se deshizo la transacción).' -ForegroundColor Red
  exit 1
}
finally { $cn.Close() }
