// ════════════════════════════════════════════════════════════════════════
// ARCHIVO: DB_ProductosProveedor.cs
// ════════════════════════════════════════════════════════════════════════

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Data.Sqlite;

namespace SistemaGestion
{
    public class ProductoProveedor
    {
        public int      Id               { get; set; }
        public string   CodigoInterno    { get; set; }
        public string   CodigoProveedor  { get; set; }
        public string   IdProveedorPrinc { get; set; }
        public string   NombreProveedor  { get; set; }  // JOIN
        public string   Descripcion      { get; set; }
        public string   TipoInsumo       { get; set; }
        public string   Unidad           { get; set; }
        public decimal  PrecioUltCompra  { get; set; }
        public decimal  PrecioPromedio   { get; set; }
        public decimal  StockActual      { get; set; }
        public decimal  StockMinimo      { get; set; }
        public bool     Activo           { get; set; } = true;
        public string   Observaciones    { get; set; }

        public bool BajoMinimo => StockActual < StockMinimo;

        // Alias — FrmKardex usa CostoUnitario, la BD guarda PrecioPromedio
        public decimal CostoUnitario
        {
            get => PrecioPromedio;
            set => PrecioPromedio = value;
        }

        public static readonly string[] Tipos = {
            "Materia prima", "Material de oficina", "Eléctricos",
            "Ferretería", "Repuestos", "Limpieza", "Otros"
        };
    }

    public static partial class DatabaseService
    {
        // ── Obtener ───────────────────────────────────────────────────────
        public static List<ProductoProveedor> ObtenerProductosProveedor(
            bool soloActivos = false, string tipo = null)
        {
            var list = new List<ProductoProveedor>();
            using var c = Conn();
            var cmd = c.CreateCommand();
            cmd.CommandText = @"
                SELECT pp.Id, pp.CodigoInterno, pp.CodigoProveedor,
                       pp.IdProveedorPrinc,
                       COALESCE(p.Descripcion,'') AS NomProv,
                       pp.Descripcion, pp.TipoInsumo, pp.Unidad,
                       pp.PrecioUltCompra, pp.PrecioPromedio,
                       pp.StockActual, pp.StockMinimo,
                       pp.Activo, COALESCE(pp.Observaciones,'')
                FROM ProductosProveedor pp
                LEFT JOIN Proveedores p ON p.Codigo = pp.IdProveedorPrinc
                WHERE 1=1"
                + (soloActivos ? " AND pp.Activo = 1" : "")
                + (tipo != null ? " AND pp.TipoInsumo = $tipo" : "")
                + " ORDER BY pp.TipoInsumo, pp.Descripcion";
            if (tipo != null) cmd.Parameters.AddWithValue("$tipo", tipo);
            using var r = cmd.ExecuteReader();
            while (r.Read())
                list.Add(LeerPP(r));
            return list;
        }

        /// <summary>
        /// Empareja una descripción de OCR (p. ej. "POLIETILENO DE ALTA DENSIDAD
        /// (2706) MARCA SNETOR HD-4454") contra el catálogo de insumos, buscando
        /// que el CÓDIGO o un token distintivo del insumo (6200B, 2706, …) aparezca
        /// dentro del texto. Devuelve el insumo con la coincidencia más larga
        /// (la más específica) o null si no hay match.
        /// </summary>
        public static ProductoProveedor BuscarInsumoOcr(string descripcionOcr)
        {
            if (string.IsNullOrWhiteSpace(descripcionOcr)) return null;
            string objetivo = SoloAlnumMayus(descripcionOcr);
            if (objetivo.Length == 0) return null;

            ProductoProveedor mejor = null;
            int mejorLen = 0;
            foreach (var pp in ObtenerProductosProveedor(soloActivos: true))
            {
                foreach (var clave in ClavesInsumo(pp))
                {
                    if (clave.Length >= 4 && clave.Length > mejorLen && objetivo.Contains(clave))
                    {
                        mejor = pp; mejorLen = clave.Length;
                    }
                }
            }
            return mejor;
        }

        // Claves distintivas de un insumo para el match de OCR: su código interno,
        // su código de proveedor y los tokens de la descripción que contengan al
        // menos un dígito (los códigos de resina tipo 6200B / 2706).
        private static IEnumerable<string> ClavesInsumo(ProductoProveedor pp)
        {
            var claves = new List<string>();
            void AddCodigo(string s)
            {
                var k = SoloAlnumMayus(s);
                if (k.Length >= 4) claves.Add(k);
            }
            AddCodigo(pp.CodigoInterno);
            AddCodigo(pp.CodigoProveedor);
            if (!string.IsNullOrEmpty(pp.Descripcion))
            {
                foreach (var tok in System.Text.RegularExpressions.Regex.Split(pp.Descripcion, "[^A-Za-z0-9]+"))
                {
                    var k = tok.ToUpperInvariant();
                    bool tieneDigito = false;
                    foreach (var ch in k) if (char.IsDigit(ch)) { tieneDigito = true; break; }
                    if (k.Length >= 4 && tieneDigito) claves.Add(k);
                }
            }
            return claves;
        }

        private static string SoloAlnumMayus(string s)
        {
            if (string.IsNullOrEmpty(s)) return "";
            var sb = new System.Text.StringBuilder(s.Length);
            foreach (var c in s)
                if (char.IsLetterOrDigit(c)) sb.Append(char.ToUpperInvariant(c));
            return sb.ToString();
        }

        public static ProductoProveedor ObtenerProductoProveedorPorId(int id)
        {
            using var c = Conn();
            var cmd = c.CreateCommand();
            cmd.CommandText = @"
                SELECT pp.Id, pp.CodigoInterno, pp.CodigoProveedor,
                       pp.IdProveedorPrinc, COALESCE(p.Descripcion,''),
                       pp.Descripcion, pp.TipoInsumo, pp.Unidad,
                       pp.PrecioUltCompra, pp.PrecioPromedio,
                       pp.StockActual, pp.StockMinimo,
                       pp.Activo, COALESCE(pp.Observaciones,'')
                FROM ProductosProveedor pp
                LEFT JOIN Proveedores p ON p.Codigo = pp.IdProveedorPrinc
                WHERE pp.Id = $id";
            cmd.Parameters.AddWithValue("$id", id);
            using var r = cmd.ExecuteReader();
            return r.Read() ? LeerPP(r) : null;
        }

        // ── Codigo automatico ─────────────────────────────────────────────
        public static string GenerarCodigoInsumo()
        {
            using var c = Conn();
            var cmd = c.CreateCommand();
            cmd.CommandText = @"
                SELECT COALESCE(MAX(CAST(SUBSTR(CodigoInterno,5) AS INTEGER)),0)
                FROM ProductosProveedor
                WHERE CodigoInterno LIKE 'INS-%'";
            int max = (int)(long)(cmd.ExecuteScalar() ?? 0L);
            return $"INS-{(max + 1):D3}";
        }

        // ── Insertar ──────────────────────────────────────────────────────
        public static void InsertarProductoProveedor(ProductoProveedor pp)
        {
            using var c = Conn();
            var cmd = c.CreateCommand();
            cmd.CommandText = @"
                INSERT INTO ProductosProveedor
                    (CodigoInterno,CodigoProveedor,IdProveedorPrinc,Descripcion,
                     TipoInsumo,Unidad,PrecioUltCompra,PrecioPromedio,
                     StockActual,StockMinimo,Activo,Observaciones)
                VALUES
                    ($ci,$cp,$ip,$de,$ti,$un,$pu,$pp,$sa,$sm,$ac,$ob)";
            BindPP(cmd, pp);
            cmd.ExecuteNonQuery();
        }

        // ── Actualizar ────────────────────────────────────────────────────
        public static void ActualizarProductoProveedor(ProductoProveedor pp)
        {
            using var c = Conn();
            var cmd = c.CreateCommand();
            cmd.CommandText = @"
                UPDATE ProductosProveedor
                SET CodigoProveedor=$cp, IdProveedorPrinc=$ip, Descripcion=$de,
                    TipoInsumo=$ti, Unidad=$un, PrecioUltCompra=$pu,
                    PrecioPromedio=$pp, CostoPromedio=$pp, StockActual=$sa, StockMinimo=$sm,
                    Activo=$ac, Observaciones=$ob
                WHERE Id=$id";
            cmd.Parameters.AddWithValue("$id", pp.Id);
            BindPP(cmd, pp);
            cmd.ExecuteNonQuery();
        }

        // ── Eliminar ──────────────────────────────────────────────────────
        public static void EliminarProductoProveedor(int id)
        {
            using var c = Conn();
            Exec(c, "DELETE FROM ProductosProveedor WHERE Id=$id", ("$id", id));
        }

        // ── Actualizar stock al registrar compra/uso ──────────────────────
        public static void ActualizarStockInsumo(int id, decimal delta, decimal precioUlt = 0)
        {
            using var c = Conn();
            var cmd = c.CreateCommand();
            if (precioUlt > 0)
            {
                cmd.CommandText = @"
                    UPDATE ProductosProveedor
                    SET StockActual     = StockActual + $d,
                        PrecioUltCompra = $pu,
                        PrecioPromedio  = ROUND(
                            (PrecioPromedio * StockActual + $pu * $d)
                            / (StockActual + $d), 2)
                    WHERE Id = $id";
                cmd.Parameters.AddWithValue("$pu", (double)precioUlt);
            }
            else
            {
                cmd.CommandText =
                    "UPDATE ProductosProveedor SET StockActual=StockActual+$d WHERE Id=$id";
            }
            cmd.Parameters.AddWithValue("$d",  (double)delta);
            cmd.Parameters.AddWithValue("$id", id);
            cmd.ExecuteNonQuery();
        }

        // ── Helpers ───────────────────────────────────────────────────────
        private static ProductoProveedor LeerPP(SqliteDataReader r) =>
            new ProductoProveedor {
                Id               = r.GetInt32(0),
                CodigoInterno    = S(r,1),
                CodigoProveedor  = S(r,2),
                IdProveedorPrinc = r.IsDBNull(3) ? null : S(r,3),
                NombreProveedor  = S(r,4),
                Descripcion      = S(r,5),
                TipoInsumo       = S(r,6),
                Unidad           = S(r,7),
                PrecioUltCompra  = D(r,8),
                PrecioPromedio   = D(r,9),
                StockActual      = D(r,10),
                StockMinimo      = D(r,11),
                Activo           = r.GetInt32(12) == 1,
                Observaciones    = S(r,13)
            };

        private static void BindPP(SqliteCommand cmd, ProductoProveedor pp)
        {
            cmd.Parameters.AddWithValue("$ci", pp.CodigoInterno ?? "");
            cmd.Parameters.AddWithValue("$cp", pp.CodigoProveedor ?? "");
            cmd.Parameters.AddWithValue("$ip",
                string.IsNullOrWhiteSpace(pp.IdProveedorPrinc)
                    ? (object)DBNull.Value : pp.IdProveedorPrinc);
            cmd.Parameters.AddWithValue("$de", pp.Descripcion ?? "");
            cmd.Parameters.AddWithValue("$ti", pp.TipoInsumo ?? "Materia prima");
            cmd.Parameters.AddWithValue("$un", pp.Unidad ?? "kg");
            cmd.Parameters.AddWithValue("$pu", (double)pp.PrecioUltCompra);
            cmd.Parameters.AddWithValue("$pp", (double)pp.PrecioPromedio);
            cmd.Parameters.AddWithValue("$sa", (double)pp.StockActual);
            cmd.Parameters.AddWithValue("$sm", (double)pp.StockMinimo);
            cmd.Parameters.AddWithValue("$ac", pp.Activo ? 1 : 0);
            cmd.Parameters.AddWithValue("$ob", pp.Observaciones ?? "");
        }

        // ── Kardex A — Entradas de insumos con Costo Promedio Ponderado ──────
        public static MovimientoInventario RegistrarEntradaInsumo(
            string codigoInsumo, decimal cantidad, decimal costoUnitario,
            string concepto, string referencia = "", string usuario = "Sistema")
        {
            if (cantidad <= 0) throw new ArgumentException("La cantidad debe ser mayor que cero.");
            if (costoUnitario < 0) throw new ArgumentException("El costo no puede ser negativo.");

            // Conn() aplica WAL + busy_timeout; con una conexión cruda, si la
            // conexión principal del egreso sigue abierta, esto fallaba con
            // "database is locked" y la entrada al Kardex no se creaba.
            using var conn = Conn();
            using var tx = conn.BeginTransaction();
            try
            {
                var cmdR = conn.CreateCommand(); cmdR.Transaction = tx;
                cmdR.CommandText = "SELECT StockActual, PrecioPromedio FROM ProductosProveedor WHERE CodigoInterno=$cod";
                cmdR.Parameters.AddWithValue("$cod", codigoInsumo);
                using var rs = cmdR.ExecuteReader();
                if (!rs.Read()) throw new Exception($"Insumo '{codigoInsumo}' no encontrado.");
                decimal stockAnt = rs.IsDBNull(0) ? 0m : (decimal)rs.GetDouble(0);
                decimal costoAnt = rs.IsDBNull(1) ? 0m : (decimal)rs.GetDouble(1);
                rs.Close();

                decimal nuevoStock = stockAnt + cantidad;
                // CPP = (Stock anterior × Costo anterior + Cant. nueva × Costo nuevo) / Nuevo stock
                decimal costoPP = nuevoStock > 0
                    ? Math.Round((stockAnt * costoAnt + cantidad * costoUnitario) / nuevoStock, 4)
                    : costoUnitario;

                var cmdIns = conn.CreateCommand(); cmdIns.Transaction = tx;
                cmdIns.CommandText = @"INSERT INTO MovimientosInventario
                    (FechaHora,CodigoProducto,TipoMovimiento,Concepto,
                     Cantidad,SaldoAnterior,NuevoSaldo,Referencia,Usuario,TipoKardex,CostoUnitario)
                    VALUES($fh,$cp,'ENTRADA',$co,$ca,$sa,$ns,$re,$us,'MATERIA_PRIMA',$cu)";
                cmdIns.Parameters.AddWithValue("$fh", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                cmdIns.Parameters.AddWithValue("$cp", codigoInsumo);
                cmdIns.Parameters.AddWithValue("$co", concepto ?? "");
                cmdIns.Parameters.AddWithValue("$ca", (double)cantidad);
                cmdIns.Parameters.AddWithValue("$sa", (double)stockAnt);
                cmdIns.Parameters.AddWithValue("$ns", (double)nuevoStock);
                cmdIns.Parameters.AddWithValue("$re", referencia ?? "");
                cmdIns.Parameters.AddWithValue("$us", usuario ?? "Sistema");
                cmdIns.Parameters.AddWithValue("$cu", (double)costoUnitario);
                cmdIns.ExecuteNonQuery();

                var cmdLast = conn.CreateCommand(); cmdLast.Transaction = tx;
                cmdLast.CommandText = "SELECT last_insert_rowid()";
                int idMov = (int)(long)cmdLast.ExecuteScalar();

                var cmdUpd = conn.CreateCommand(); cmdUpd.Transaction = tx;
                cmdUpd.CommandText = @"UPDATE ProductosProveedor
                    SET StockActual=$ns, PrecioPromedio=$cpp, CostoPromedio=$cpp, PrecioUltCompra=$pu
                    WHERE CodigoInterno=$cod";
                cmdUpd.Parameters.AddWithValue("$ns",  (double)nuevoStock);
                cmdUpd.Parameters.AddWithValue("$cpp", (double)costoPP);
                cmdUpd.Parameters.AddWithValue("$pu",  (double)costoUnitario);
                cmdUpd.Parameters.AddWithValue("$cod", codigoInsumo);
                cmdUpd.ExecuteNonQuery();

                tx.Commit();
                return new MovimientoInventario {
                    IdMovimiento=idMov, FechaHora=DateTime.Now, CodigoProducto=codigoInsumo,
                    TipoMovimiento="ENTRADA", Concepto=concepto??"", Cantidad=cantidad,
                    SaldoAnterior=stockAnt, NuevoSaldo=nuevoStock, Referencia=referencia??"",
                    Usuario=usuario??"Sistema", TipoKardex="MATERIA_PRIMA", CostoUnitario=costoUnitario
                };
            }
            catch { tx.Rollback(); throw; }
        }

        /// <summary>
        /// Fase 2 — Ajusta el costo de una entrada de insumo que se registró con
        /// costo PROVISIONAL (precio pendiente). Corrige el CostoUnitario del
        /// movimiento de Kardex y recalcula el costo promedio del insumo cargando
        /// la diferencia de valor sobre el stock actual. Cuando la mercancía no se
        /// ha consumido, el promedio resultante coincide con el ponderado exacto.
        /// </summary>
        public static void AjustarCostoEntradaKardex(
            string codigoInsumo, string referencia, decimal costoRealUnit)
        {
            if (string.IsNullOrEmpty(referencia)) return;
            using var conn = Conn();   // WAL + busy_timeout (evita "database is locked")
            using var tx = conn.BeginTransaction();
            try
            {
                // Localizar el movimiento de entrada provisional por referencia.
                var cmb = conn.CreateCommand(); cmb.Transaction = tx;
                cmb.CommandText = @"SELECT IdMovimiento, Cantidad, CostoUnitario, COALESCE(Concepto,'')
                    FROM MovimientosInventario
                    WHERE TipoKardex='MATERIA_PRIMA' AND TipoMovimiento='ENTRADA'
                      AND CodigoProducto=$cod AND Referencia=$re
                    ORDER BY IdMovimiento DESC LIMIT 1";
                cmb.Parameters.AddWithValue("$cod", codigoInsumo);
                cmb.Parameters.AddWithValue("$re", referencia);
                int idMov = 0; decimal cantidad = 0m, costoProv = 0m; string concepto = "";
                using (var rm = cmb.ExecuteReader())
                {
                    if (rm.Read())
                    {
                        idMov     = rm.GetInt32(0);
                        cantidad  = rm.IsDBNull(1) ? 0m : (decimal)rm.GetDouble(1);
                        costoProv = rm.IsDBNull(2) ? 0m : (decimal)rm.GetDouble(2);
                        concepto  = rm.IsDBNull(3) ? "" : rm.GetString(3);
                    }
                }
                if (idMov == 0) { tx.Rollback(); return; }  // sin movimiento que ajustar

                // Estado actual del insumo.
                var crp = conn.CreateCommand(); crp.Transaction = tx;
                crp.CommandText = "SELECT StockActual, PrecioPromedio FROM ProductosProveedor WHERE CodigoInterno=$cod";
                crp.Parameters.AddWithValue("$cod", codigoInsumo);
                decimal stock = 0m, avg = 0m;
                using (var rp = crp.ExecuteReader())
                {
                    if (rp.Read())
                    {
                        stock = rp.IsDBNull(0) ? 0m : (decimal)rp.GetDouble(0);
                        avg   = rp.IsDBNull(1) ? 0m : (decimal)rp.GetDouble(1);
                    }
                }

                // Diferencia de valor total y nuevo promedio (cargado al stock actual).
                decimal delta  = cantidad * (costoRealUnit - costoProv);
                decimal avgNew = stock > 0 ? Math.Round(avg + delta / stock, 4) : costoRealUnit;
                if (avgNew < 0) avgNew = 0m;

                // Corregir el movimiento (costo real) y anotar en el concepto.
                var um = conn.CreateCommand(); um.Transaction = tx;
                um.CommandText = "UPDATE MovimientosInventario SET CostoUnitario=$cu, Concepto=$co WHERE IdMovimiento=$id";
                um.Parameters.AddWithValue("$cu", (double)costoRealUnit);
                um.Parameters.AddWithValue("$co", concepto + " [precio completado]");
                um.Parameters.AddWithValue("$id", idMov);
                um.ExecuteNonQuery();

                // Actualizar costo promedio y último precio de compra del insumo.
                var up = conn.CreateCommand(); up.Transaction = tx;
                up.CommandText = "UPDATE ProductosProveedor SET PrecioPromedio=$pp, CostoPromedio=$pp, PrecioUltCompra=$pu WHERE CodigoInterno=$cod";
                up.Parameters.AddWithValue("$pp", (double)avgNew);
                up.Parameters.AddWithValue("$pu", (double)costoRealUnit);
                up.Parameters.AddWithValue("$cod", codigoInsumo);
                up.ExecuteNonQuery();

                tx.Commit();
                RegistrarAuditoria("KARDEX_MP_AJUSTE_COSTO", "MovimientosInventario",
                    $"{codigoInsumo} ref={referencia} costo {costoProv:N4}→{costoRealUnit:N4} (prom→{avgNew:N4})");
            }
            catch { tx.Rollback(); throw; }
        }

        // ── Kardex A — Salidas manuales de insumos ───────────────────────────
        public static MovimientoInventario RegistrarSalidaInsumo(
            string codigoInsumo, decimal cantidad, string concepto,
            string referencia = "", string usuario = "Sistema")
        {
            if (cantidad <= 0) throw new ArgumentException("Cantidad debe ser > 0.");

            using var conn = Conn();   // WAL + busy_timeout (evita "database is locked")
            using var tx = conn.BeginTransaction();
            try
            {
                var cmdR = conn.CreateCommand(); cmdR.Transaction = tx;
                cmdR.CommandText = "SELECT StockActual, PrecioPromedio FROM ProductosProveedor WHERE CodigoInterno=$cod";
                cmdR.Parameters.AddWithValue("$cod", codigoInsumo);
                using var rs = cmdR.ExecuteReader();
                if (!rs.Read()) throw new Exception($"Insumo '{codigoInsumo}' no encontrado.");
                decimal stockAnt = rs.IsDBNull(0) ? 0m : (decimal)rs.GetDouble(0);
                decimal costo    = rs.IsDBNull(1) ? 0m : (decimal)rs.GetDouble(1);
                rs.Close();

                decimal nuevoStock = stockAnt - cantidad;
                if (nuevoStock < 0)
                    throw new Exception($"Stock insuficiente: disponible {stockAnt:N2}, solicitado {cantidad:N2}.");

                var cmdIns = conn.CreateCommand(); cmdIns.Transaction = tx;
                cmdIns.CommandText = @"INSERT INTO MovimientosInventario
                    (FechaHora,CodigoProducto,TipoMovimiento,Concepto,
                     Cantidad,SaldoAnterior,NuevoSaldo,Referencia,Usuario,TipoKardex,CostoUnitario)
                    VALUES($fh,$cp,'SALIDA',$co,$ca,$sa,$ns,$re,$us,'MATERIA_PRIMA',$cu)";
                cmdIns.Parameters.AddWithValue("$fh", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                cmdIns.Parameters.AddWithValue("$cp", codigoInsumo);
                cmdIns.Parameters.AddWithValue("$co", concepto ?? "");
                cmdIns.Parameters.AddWithValue("$ca", (double)cantidad);
                cmdIns.Parameters.AddWithValue("$sa", (double)stockAnt);
                cmdIns.Parameters.AddWithValue("$ns", (double)nuevoStock);
                cmdIns.Parameters.AddWithValue("$re", referencia ?? "");
                cmdIns.Parameters.AddWithValue("$us", usuario ?? "Sistema");
                cmdIns.Parameters.AddWithValue("$cu", (double)costo);
                cmdIns.ExecuteNonQuery();

                var cmdLast = conn.CreateCommand(); cmdLast.Transaction = tx;
                cmdLast.CommandText = "SELECT last_insert_rowid()";
                int idMov = (int)(long)cmdLast.ExecuteScalar();

                var cmdUpd = conn.CreateCommand(); cmdUpd.Transaction = tx;
                cmdUpd.CommandText = "UPDATE ProductosProveedor SET StockActual=$ns WHERE CodigoInterno=$cod";
                cmdUpd.Parameters.AddWithValue("$ns",  (double)nuevoStock);
                cmdUpd.Parameters.AddWithValue("$cod", codigoInsumo);
                cmdUpd.ExecuteNonQuery();

                tx.Commit();
                return new MovimientoInventario {
                    IdMovimiento=idMov, FechaHora=DateTime.Now, CodigoProducto=codigoInsumo,
                    TipoMovimiento="SALIDA", Concepto=concepto??"", Cantidad=cantidad,
                    SaldoAnterior=stockAnt, NuevoSaldo=nuevoStock, Referencia=referencia??"",
                    Usuario=usuario??"Sistema", TipoKardex="MATERIA_PRIMA", CostoUnitario=costo
                };
            }
            catch { tx.Rollback(); throw; }
        }

        // ── Kardex A — Consulta ───────────────────────────────────────────────
        public static List<MovimientoInventario> ObtenerKardexInsumos(
            string codigoInsumo = null, DateTime? desde = null, DateTime? hasta = null)
        {
            var list = new List<MovimientoInventario>();
            using var c = Conn();
            var cmd = c.CreateCommand();
            var where = new List<string> { "TipoKardex = 'MATERIA_PRIMA'" };
            if (codigoInsumo != null) where.Add("CodigoProducto = $cod");
            if (desde != null) where.Add("FechaHora >= $desde");
            if (hasta != null) where.Add("FechaHora <= $hasta");
            cmd.CommandText = "SELECT IdMovimiento,FechaHora,CodigoProducto,TipoMovimiento," +
                "Concepto,Cantidad,SaldoAnterior,NuevoSaldo,Referencia,Usuario,TipoKardex,CostoUnitario " +
                "FROM MovimientosInventario WHERE " + string.Join(" AND ", where) +
                " ORDER BY IdMovimiento DESC";
            if (codigoInsumo != null) cmd.Parameters.AddWithValue("$cod",   codigoInsumo);
            if (desde != null)        cmd.Parameters.AddWithValue("$desde", desde.Value.ToString("yyyy-MM-dd"));
            if (hasta != null)        cmd.Parameters.AddWithValue("$hasta", hasta.Value.ToString("yyyy-MM-dd") + " 23:59:59");
            using var r = cmd.ExecuteReader();
            while (r.Read())
                list.Add(new MovimientoInventario {
                    IdMovimiento=r.GetInt32(0), FechaHora=DateTime.Parse(r.GetString(1)),
                    CodigoProducto=S(r,2), TipoMovimiento=S(r,3), Concepto=S(r,4),
                    Cantidad=D(r,5), SaldoAnterior=D(r,6), NuevoSaldo=D(r,7),
                    Referencia=S(r,8), Usuario=S(r,9), TipoKardex=S(r,10), CostoUnitario=D(r,11)
                });
            return list;
        }
    }
}
