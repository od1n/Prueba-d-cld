using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Data.Sqlite;

namespace SistemaGestion
{
    public static partial class DatabaseService
    {
        public static void MigrarV14(SqliteConnection conn, SqliteTransaction tx)
        {
            AlterTableSafe(conn, tx, "ALTER TABLE MovimientosInventario ADD COLUMN TipoKardex    TEXT DEFAULT 'PRODUCTO'");
            AlterTableSafe(conn, tx, "ALTER TABLE MovimientosInventario ADD COLUMN CostoUnitario REAL DEFAULT 0");
            AlterTableSafe(conn, tx, "ALTER TABLE ProductosProveedor ADD COLUMN CostoPromedio REAL DEFAULT 0");
            var cmd = conn.CreateCommand(); cmd.Transaction = tx;
            cmd.CommandText = "CREATE INDEX IF NOT EXISTS idx_mov_tipokardex ON MovimientosInventario(TipoKardex); CREATE INDEX IF NOT EXISTS idx_mov_tipo_fecha ON MovimientosInventario(TipoKardex, FechaHora);";
            cmd.ExecuteNonQuery();
            var upd = conn.CreateCommand(); upd.Transaction = tx;
            upd.CommandText = "UPDATE MovimientosInventario SET TipoKardex='PRODUCTO' WHERE TipoKardex IS NULL OR TipoKardex=''";
            upd.ExecuteNonQuery();
            SetSchemaVersion(conn, tx, 14);
        }

        public static MovimientoInventario RegistrarEntradaMP(int idProductoProveedor, decimal cantidad, decimal costoUnitarioCompra, string concepto, string referencia = "", string usuario = "Sistema")
        {
            if (cantidad <= 0) throw new ArgumentException("La cantidad debe ser mayor que cero.");
            using var conn = new SqliteConnection(ConnStr); conn.Open();
            using var tx = conn.BeginTransaction();
            try
            {
                var cr = conn.CreateCommand(); cr.Transaction = tx;
                cr.CommandText = "SELECT CodigoInterno, StockActual, CostoPromedio FROM ProductosProveedor WHERE Id = $id";
                cr.Parameters.AddWithValue("$id", idProductoProveedor);
                using var r = cr.ExecuteReader();
                if (!r.Read()) throw new Exception($"Insumo Id={idProductoProveedor} no existe.");
                string codigo = r.GetString(0); decimal sa = r.IsDBNull(1)?0m:(decimal)r.GetDouble(1); decimal cp = r.IsDBNull(2)?0m:(decimal)r.GetDouble(2); r.Close();
                decimal ns = sa + cantidad; decimal ncp = ns > 0 ? Math.Round((sa*cp + cantidad*costoUnitarioCompra)/ns,4) : costoUnitarioCompra;
                var ci = conn.CreateCommand(); ci.Transaction = tx;
                ci.CommandText = "INSERT INTO MovimientosInventario (FechaHora,CodigoProducto,TipoMovimiento,Concepto,Cantidad,SaldoAnterior,NuevoSaldo,Referencia,Usuario,TipoKardex,CostoUnitario) VALUES ($fh,$cp,'ENTRADA',$co,$ca,$sa,$ns,$re,$us,'MATERIA_PRIMA',$cu)";
                ci.Parameters.AddWithValue("$fh",DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")); ci.Parameters.AddWithValue("$cp",codigo); ci.Parameters.AddWithValue("$co",concepto??"Compra");
                ci.Parameters.AddWithValue("$ca",(double)cantidad); ci.Parameters.AddWithValue("$sa",(double)sa); ci.Parameters.AddWithValue("$ns",(double)ns);
                ci.Parameters.AddWithValue("$re",referencia??""); ci.Parameters.AddWithValue("$us",usuario); ci.Parameters.AddWithValue("$cu",(double)ncp);
                ci.ExecuteNonQuery();
                var cid = conn.CreateCommand(); cid.Transaction = tx; cid.CommandText = "SELECT last_insert_rowid()"; int idMov = (int)(long)cid.ExecuteScalar();
                var cu = conn.CreateCommand(); cu.Transaction = tx;
                cu.CommandText = "UPDATE ProductosProveedor SET StockActual=$ns, CostoPromedio=$ncp, PrecioUltCompra=$pu WHERE Id=$id";
                cu.Parameters.AddWithValue("$ns",(double)ns); cu.Parameters.AddWithValue("$ncp",(double)ncp); cu.Parameters.AddWithValue("$pu",(double)costoUnitarioCompra); cu.Parameters.AddWithValue("$id",idProductoProveedor);
                cu.ExecuteNonQuery(); tx.Commit();
                RegistrarAuditoria("KARDEX_MP_ENTRADA","MovimientosInventario",$"{codigo} +{cantidad}");
                return new MovimientoInventario{IdMovimiento=idMov,FechaHora=DateTime.Now,CodigoProducto=codigo,TipoMovimiento="ENTRADA",Concepto=concepto??"Compra",Cantidad=cantidad,SaldoAnterior=sa,NuevoSaldo=ns,Referencia=referencia??"",Usuario=usuario,TipoKardex="MATERIA_PRIMA",CostoUnitario=ncp};
            }
            catch { tx.Rollback(); throw; }
        }

        public static MovimientoInventario RegistrarSalidaMP(int idProductoProveedor, decimal cantidad, string concepto, string referencia = "", string usuario = "Sistema")
        {
            if (cantidad <= 0) throw new ArgumentException("La cantidad debe ser mayor que cero.");
            using var conn = new SqliteConnection(ConnStr); conn.Open();
            using var tx = conn.BeginTransaction();
            try
            {
                var cr = conn.CreateCommand(); cr.Transaction = tx;
                cr.CommandText = "SELECT CodigoInterno, StockActual, CostoPromedio FROM ProductosProveedor WHERE Id = $id";
                cr.Parameters.AddWithValue("$id", idProductoProveedor);
                using var r = cr.ExecuteReader();
                if (!r.Read()) throw new Exception($"Insumo Id={idProductoProveedor} no existe.");
                string codigo = r.GetString(0); decimal sa = r.IsDBNull(1)?0m:(decimal)r.GetDouble(1); decimal cp = r.IsDBNull(2)?0m:(decimal)r.GetDouble(2); r.Close();
                if (sa - cantidad < 0) throw new Exception($"Stock insuficiente de '{codigo}'. Disponible: {sa:N2}, solicitado: {cantidad:N2}.");
                decimal ns = sa - cantidad;
                var ci = conn.CreateCommand(); ci.Transaction = tx;
                ci.CommandText = "INSERT INTO MovimientosInventario (FechaHora,CodigoProducto,TipoMovimiento,Concepto,Cantidad,SaldoAnterior,NuevoSaldo,Referencia,Usuario,TipoKardex,CostoUnitario) VALUES ($fh,$cp,'SALIDA',$co,$ca,$sa,$ns,$re,$us,'MATERIA_PRIMA',$cu)";
                ci.Parameters.AddWithValue("$fh",DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")); ci.Parameters.AddWithValue("$cp",codigo); ci.Parameters.AddWithValue("$co",concepto??"Consumo producción");
                ci.Parameters.AddWithValue("$ca",(double)cantidad); ci.Parameters.AddWithValue("$sa",(double)sa); ci.Parameters.AddWithValue("$ns",(double)ns);
                ci.Parameters.AddWithValue("$re",referencia??""); ci.Parameters.AddWithValue("$us",usuario); ci.Parameters.AddWithValue("$cu",(double)cp);
                ci.ExecuteNonQuery();
                var cid = conn.CreateCommand(); cid.Transaction = tx; cid.CommandText = "SELECT last_insert_rowid()"; int idMov = (int)(long)cid.ExecuteScalar();
                var cu = conn.CreateCommand(); cu.Transaction = tx;
                cu.CommandText = "UPDATE ProductosProveedor SET StockActual=$ns WHERE Id=$id";
                cu.Parameters.AddWithValue("$ns",(double)ns); cu.Parameters.AddWithValue("$id",idProductoProveedor);
                cu.ExecuteNonQuery(); tx.Commit();
                RegistrarAuditoria("KARDEX_MP_SALIDA","MovimientosInventario",$"{codigo} -{cantidad}");
                return new MovimientoInventario{IdMovimiento=idMov,FechaHora=DateTime.Now,CodigoProducto=codigo,TipoMovimiento="SALIDA",Concepto=concepto??"Consumo producción",Cantidad=cantidad,SaldoAnterior=sa,NuevoSaldo=ns,Referencia=referencia??"",Usuario=usuario,TipoKardex="MATERIA_PRIMA",CostoUnitario=cp};
            }
            catch { tx.Rollback(); throw; }
        }

        public static void RegistrarAjusteMP(int idProductoProveedor, decimal nuevoStockReal, string motivo, string usuario = "Admin")
        {
            using var conn = new SqliteConnection(ConnStr); conn.Open();
            var cmd = conn.CreateCommand(); cmd.CommandText = "SELECT StockActual FROM ProductosProveedor WHERE Id=$id"; cmd.Parameters.AddWithValue("$id", idProductoProveedor);
            using var r = cmd.ExecuteReader(); if (!r.Read()) throw new Exception("Insumo no encontrado.");
            decimal sa = r.IsDBNull(0)?0m:(decimal)r.GetDouble(0); r.Close();
            decimal d = nuevoStockReal - sa; if (d == 0) return;
            if (d > 0) RegistrarEntradaMP(idProductoProveedor, d, 0, $"AJUSTE: {motivo}", "AJUSTE", usuario);
            else       RegistrarSalidaMP(idProductoProveedor, Math.Abs(d), $"AJUSTE: {motivo}", "AJUSTE", usuario);
        }

        public static List<MovimientoInventario> ObtenerKardexMP(string codigoInsumo = null, DateTime? desde = null, DateTime? hasta = null)
            => ObtenerKardexInterno("MATERIA_PRIMA", codigoInsumo, desde, hasta);

        public static MovimientoInventario RegistrarEntradaProduccion(string codigoProducto, decimal cantidad, decimal costoFabricacionUnit, string concepto, string referencia = "", string usuario = "Sistema")
        {
            if (cantidad <= 0) throw new ArgumentException("La cantidad debe ser mayor que cero.");
            using var conn = new SqliteConnection(ConnStr); conn.Open();
            using var tx = conn.BeginTransaction();
            try
            {
                var cs = conn.CreateCommand(); cs.Transaction = tx; cs.CommandText = "SELECT Existencia FROM Inventario WHERE Codigo=$cod"; cs.Parameters.AddWithValue("$cod", codigoProducto);
                var obj = cs.ExecuteScalar(); if (obj == null) throw new Exception($"Producto '{codigoProducto}' no existe en Inventario.");
                decimal sa = (decimal)(double)obj; decimal ns = sa + cantidad;
                var ci = conn.CreateCommand(); ci.Transaction = tx;
                ci.CommandText = "INSERT INTO MovimientosInventario (FechaHora,CodigoProducto,TipoMovimiento,Concepto,Cantidad,SaldoAnterior,NuevoSaldo,Referencia,Usuario,TipoKardex,CostoUnitario) VALUES ($fh,$cp,'ENTRADA',$co,$ca,$sa,$ns,$re,$us,'PRODUCTO',$cu)";
                ci.Parameters.AddWithValue("$fh",DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")); ci.Parameters.AddWithValue("$cp",codigoProducto); ci.Parameters.AddWithValue("$co",concepto??"Ingreso producción");
                ci.Parameters.AddWithValue("$ca",(double)cantidad); ci.Parameters.AddWithValue("$sa",(double)sa); ci.Parameters.AddWithValue("$ns",(double)ns);
                ci.Parameters.AddWithValue("$re",referencia??""); ci.Parameters.AddWithValue("$us",usuario); ci.Parameters.AddWithValue("$cu",(double)costoFabricacionUnit);
                ci.ExecuteNonQuery();
                var cid = conn.CreateCommand(); cid.Transaction = tx; cid.CommandText = "SELECT last_insert_rowid()"; int idMov = (int)(long)cid.ExecuteScalar();
                var cu = conn.CreateCommand(); cu.Transaction = tx;
                cu.CommandText = "UPDATE Inventario SET Existencia=$ns, CostoActual=$cu, UltimaActualz=$f WHERE Codigo=$cod";
                cu.Parameters.AddWithValue("$ns",(double)ns); cu.Parameters.AddWithValue("$cu",(double)costoFabricacionUnit); cu.Parameters.AddWithValue("$f",DateTime.Today.ToString("yyyy-MM-dd")); cu.Parameters.AddWithValue("$cod",codigoProducto);
                cu.ExecuteNonQuery(); tx.Commit();
                RegistrarAuditoria("KARDEX_PT_ENTRADA","MovimientosInventario",$"{codigoProducto} +{cantidad}");
                return new MovimientoInventario{IdMovimiento=idMov,FechaHora=DateTime.Now,CodigoProducto=codigoProducto,TipoMovimiento="ENTRADA",Concepto=concepto??"Ingreso producción",Cantidad=cantidad,SaldoAnterior=sa,NuevoSaldo=ns,Referencia=referencia??"",Usuario=usuario,TipoKardex="PRODUCTO",CostoUnitario=costoFabricacionUnit};
            }
            catch { tx.Rollback(); throw; }
        }

        public static MovimientoInventario RegistrarSalidaVenta(SqliteConnection conn, SqliteTransaction tx, string codigoProducto, decimal cantidad, string numeroFactura, string usuario = "Sistema")
        {
            var cs = conn.CreateCommand(); cs.Transaction = tx; cs.CommandText = "SELECT Existencia, CostoActual FROM Inventario WHERE Codigo=$cod"; cs.Parameters.AddWithValue("$cod", codigoProducto);
            using var r = cs.ExecuteReader(); if (!r.Read()) throw new Exception($"Producto '{codigoProducto}' no existe.");
            decimal sa = r.IsDBNull(0)?0m:(decimal)r.GetDouble(0); decimal cu = r.IsDBNull(1)?0m:(decimal)r.GetDouble(1); r.Close();
            decimal ns = sa - cantidad; if (ns < 0) throw new Exception($"Stock insuficiente de '{codigoProducto}'. Disponible: {sa:N2}, solicitado: {cantidad:N2}.");
            var ci = conn.CreateCommand(); ci.Transaction = tx;
            ci.CommandText = "INSERT INTO MovimientosInventario (FechaHora,CodigoProducto,TipoMovimiento,Concepto,Cantidad,SaldoAnterior,NuevoSaldo,Referencia,Usuario,TipoKardex,CostoUnitario) VALUES ($fh,$cp,'SALIDA',$co,$ca,$sa,$ns,$re,$us,'PRODUCTO',$cu)";
            ci.Parameters.AddWithValue("$fh",DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")); ci.Parameters.AddWithValue("$cp",codigoProducto); ci.Parameters.AddWithValue("$co",$"Venta Factura {numeroFactura}");
            ci.Parameters.AddWithValue("$ca",(double)cantidad); ci.Parameters.AddWithValue("$sa",(double)sa); ci.Parameters.AddWithValue("$ns",(double)ns);
            ci.Parameters.AddWithValue("$re",numeroFactura); ci.Parameters.AddWithValue("$us",usuario); ci.Parameters.AddWithValue("$cu",(double)cu);
            ci.ExecuteNonQuery();
            var cid = conn.CreateCommand(); cid.Transaction = tx; cid.CommandText = "SELECT last_insert_rowid()"; int idMov = (int)(long)cid.ExecuteScalar();
            var upd = conn.CreateCommand(); upd.Transaction = tx;
            upd.CommandText = "UPDATE Inventario SET Existencia=$ns, UltimaActualz=$f WHERE Codigo=$cod";
            upd.Parameters.AddWithValue("$ns",(double)ns); upd.Parameters.AddWithValue("$f",DateTime.Today.ToString("yyyy-MM-dd")); upd.Parameters.AddWithValue("$cod",codigoProducto);
            upd.ExecuteNonQuery();
            return new MovimientoInventario{IdMovimiento=idMov,FechaHora=DateTime.Now,CodigoProducto=codigoProducto,TipoMovimiento="SALIDA",Concepto=$"Venta Factura {numeroFactura}",Cantidad=cantidad,SaldoAnterior=sa,NuevoSaldo=ns,Referencia=numeroFactura,Usuario=usuario,TipoKardex="PRODUCTO",CostoUnitario=cu};
        }

        public static void RegistrarCorreccionPT(string codigoProducto, decimal nuevoStockReal, string motivo, string usuario = "Admin")
            => RegistrarCorreccion(codigoProducto, nuevoStockReal, motivo, usuario);

        public static List<MovimientoInventario> ObtenerKardexPT(string codigoProducto = null, DateTime? desde = null, DateTime? hasta = null)
            => ObtenerKardexInterno("PRODUCTO", codigoProducto, desde, hasta);

        private static List<MovimientoInventario> ObtenerKardexInterno(string tipoKardex, string codigoProd = null, DateTime? desde = null, DateTime? hasta = null)
        {
            var list = new List<MovimientoInventario>(); using var c = Conn(); var cmd = c.CreateCommand();
            var where = new List<string>{"TipoKardex = $tk"};
            if (codigoProd != null) where.Add("CodigoProducto = $cod");
            if (desde != null)      where.Add("FechaHora >= $desde");
            if (hasta != null)      where.Add("FechaHora <= $hasta");
            cmd.CommandText = "SELECT IdMovimiento,FechaHora,CodigoProducto,TipoMovimiento,Concepto,Cantidad,SaldoAnterior,NuevoSaldo,Referencia,Usuario,TipoKardex,CostoUnitario FROM MovimientosInventario WHERE " + string.Join(" AND ", where) + " ORDER BY IdMovimiento DESC";
            cmd.Parameters.AddWithValue("$tk", tipoKardex);
            if (codigoProd != null) cmd.Parameters.AddWithValue("$cod", codigoProd);
            if (desde != null)      cmd.Parameters.AddWithValue("$desde", desde.Value.ToString("yyyy-MM-dd"));
            if (hasta != null)      cmd.Parameters.AddWithValue("$hasta", hasta.Value.ToString("yyyy-MM-dd") + " 23:59:59");
            using var r = cmd.ExecuteReader();
            while (r.Read()) list.Add(new MovimientoInventario{IdMovimiento=r.GetInt32(0),FechaHora=DateTime.Parse(r.GetString(1)),CodigoProducto=S(r,2),TipoMovimiento=S(r,3),Concepto=S(r,4),Cantidad=D(r,5),SaldoAnterior=D(r,6),NuevoSaldo=D(r,7),Referencia=S(r,8),Usuario=S(r,9),TipoKardex=S(r,10),CostoUnitario=D(r,11)});
            return list;
        }

        public static decimal ObtenerCostoPromedioMP(int idProductoProveedor)
        {
            using var c = Conn(); var cmd = c.CreateCommand();
            cmd.CommandText = "SELECT CostoPromedio FROM ProductosProveedor WHERE Id=$id";
            cmd.Parameters.AddWithValue("$id", idProductoProveedor);
            var obj = cmd.ExecuteScalar(); return obj == null ? 0m : (decimal)(double)obj;
        }

        public static decimal CalcularCostoFabricacion(List<(int idInsumo, decimal cantidadKg)> insumosUsados, decimal costoManoObraTotal, decimal unidadesProducidas)
        {
            if (unidadesProducidas <= 0) return 0;
            decimal costoMP = insumosUsados.Sum(x => x.cantidadKg * ObtenerCostoPromedioMP(x.idInsumo));
            return Math.Round((costoMP + costoManoObraTotal) / unidadesProducidas, 4);
        }

        public static void RegistrarMermaProduccion(int idInsumo, decimal mermaKg, DateTime fecha, string referencia, string usuario = "Sistema")
        {
            if (mermaKg <= 0) return;
            using var conn = new SqliteConnection(ConnStr); conn.Open();
            using var tx = conn.BeginTransaction();
            try
            {
                var cr = conn.CreateCommand(); cr.Transaction = tx; cr.CommandText = "SELECT CodigoInterno, StockActual, CostoPromedio FROM ProductosProveedor WHERE Id=$id"; cr.Parameters.AddWithValue("$id", idInsumo);
                using var r = cr.ExecuteReader(); if (!r.Read()) { tx.Rollback(); return; }
                string codigo = r.GetString(0); decimal sa = r.IsDBNull(1)?0m:(decimal)r.GetDouble(1); decimal cp = r.IsDBNull(2)?0m:(decimal)r.GetDouble(2); r.Close();
                decimal ns = Math.Max(0, sa - mermaKg);
                var ci = conn.CreateCommand(); ci.Transaction = tx;
                ci.CommandText = "INSERT INTO MovimientosInventario (FechaHora,CodigoProducto,TipoMovimiento,Concepto,Cantidad,SaldoAnterior,NuevoSaldo,Referencia,Usuario,TipoKardex,CostoUnitario) VALUES ($fh,$cp,'SALIDA',$co,$ca,$sa,$ns,$re,$us,'MATERIA_PRIMA',$cu)";
                ci.Parameters.AddWithValue("$fh",fecha.ToString("yyyy-MM-dd HH:mm:ss")); ci.Parameters.AddWithValue("$cp",codigo); ci.Parameters.AddWithValue("$co",$"MERMA producción {fecha:dd/MM/yyyy}");
                ci.Parameters.AddWithValue("$ca",(double)mermaKg); ci.Parameters.AddWithValue("$sa",(double)sa); ci.Parameters.AddWithValue("$ns",(double)ns);
                ci.Parameters.AddWithValue("$re",referencia??""); ci.Parameters.AddWithValue("$us",usuario); ci.Parameters.AddWithValue("$cu",(double)cp); ci.ExecuteNonQuery();
                var cu = conn.CreateCommand(); cu.Transaction = tx; cu.CommandText = "UPDATE ProductosProveedor SET StockActual=$ns WHERE Id=$id"; cu.Parameters.AddWithValue("$ns",(double)ns); cu.Parameters.AddWithValue("$id",idInsumo); cu.ExecuteNonQuery();
                var cm = conn.CreateCommand(); cm.Transaction = tx;
                cm.CommandText = "INSERT INTO Mermas (CodigoProducto,Fecha,KgMerma,CostoMerma,Referencia,Usuario) VALUES ($cp,$fe,$kg,$cm,$re,$us) ON CONFLICT DO NOTHING";
                cm.Parameters.AddWithValue("$cp",codigo); cm.Parameters.AddWithValue("$fe",fecha.ToString("yyyy-MM-dd")); cm.Parameters.AddWithValue("$kg",(double)mermaKg);
                cm.Parameters.AddWithValue("$cm",(double)(mermaKg*cp)); cm.Parameters.AddWithValue("$re",referencia??""); cm.Parameters.AddWithValue("$us",usuario); cm.ExecuteNonQuery();
                tx.Commit(); RegistrarAuditoria("MERMA_REGISTRADA","Mermas",$"{codigo} {mermaKg:N2} kg");
            }
            catch { tx.Rollback(); throw; }
        }
    }
}
