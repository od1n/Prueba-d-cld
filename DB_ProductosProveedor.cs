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
                    PrecioPromedio=$pp, StockActual=$sa, StockMinimo=$sm,
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

            using var conn = new SqliteConnection(ConnStr);
            conn.Open();
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
                    SET StockActual=$ns, PrecioPromedio=$cpp, PrecioUltCompra=$pu
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

        // ── Kardex A — Salidas manuales de insumos ───────────────────────────
        public static MovimientoInventario RegistrarSalidaInsumo(
            string codigoInsumo, decimal cantidad, string concepto,
            string referencia = "", string usuario = "Sistema")
        {
            if (cantidad <= 0) throw new ArgumentException("Cantidad debe ser > 0.");

            using var conn = new SqliteConnection(ConnStr);
            conn.Open();
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
