using System;
using System.Collections.Generic;
using Microsoft.Data.Sqlite;

namespace SistemaGestion
{
    public static partial class DatabaseService
    {
        // ══════════════════════════════════════════════════════════════
        //  COMPRAS CON DETALLES + KARDEX
        // ══════════════════════════════════════════════════════════════

        /// <summary>
        /// Inserta compra + detalles + actualiza stock + registra Kardex A
        /// todo en una sola transacción.
        /// </summary>
        public static void InsertarCompraConDetallesYKardex(
            Compra cp, List<DetalleCompra> lineas)
        {
            using var c = Conn();
            using var tx = c.BeginTransaction();
            try
            {
                // 1. Insertar cabecera
                var cmd = c.CreateCommand(); cmd.Transaction = tx;
                cmd.CommandText =
                    "INSERT OR REPLACE INTO Compras VALUES ($cn,$fc,$cp,$dc,$fp,$ab,$iv,$dd,$tc,$tr,$ob)";
                BindCompra(cmd, cp);
                cmd.ExecuteNonQuery();

                // 2. Obtener rowid para DetallesCompra
                var cmdId = c.CreateCommand(); cmdId.Transaction = tx;
                cmdId.CommandText = "SELECT rowid FROM Compras WHERE CompraN=$cn";
                cmdId.Parameters.AddWithValue("$cn", cp.CompraN);
                int rowid = (int)(long)cmdId.ExecuteScalar();

                // 3. Insertar detalles + stock + kardex
                GuardarLineasCompra(c, tx, rowid, cp.CompraN, lineas);

                tx.Commit();
                RegistrarAuditoria("COMPRA_NUEVA", "Compras",
                    $"Compra {cp.CompraN} con {lineas.Count} líneas");
            }
            catch { tx.Rollback(); throw; }
        }

        /// <summary>
        /// Actualiza compra existente: recalcula detalles, stock y kardex.
        /// </summary>
        public static void ActualizarCompraConDetallesYKardex(
            Compra cp, List<DetalleCompra> lineas)
        {
            using var c = Conn();
            using var tx = c.BeginTransaction();
            try
            {
                // 1. Actualizar cabecera
                var cmd = c.CreateCommand(); cmd.Transaction = tx;
                cmd.CommandText = @"UPDATE Compras SET FechaC=$fc,CodigoP=$cp,DescuentoC=$dc,
                    FormaDePago=$fp,AbonoC=$ab,IvaC=$iv,DiasDeCred=$dd,TotalC=$tc,
                    Transporte=$tr,Observaciones=$ob WHERE CompraN=$cn";
                BindCompra(cmd, cp);
                cmd.ExecuteNonQuery();

                // 2. Obtener rowid
                var cmdId = c.CreateCommand(); cmdId.Transaction = tx;
                cmdId.CommandText = "SELECT rowid FROM Compras WHERE CompraN=$cn";
                cmdId.Parameters.AddWithValue("$cn", cp.CompraN);
                int rowid = (int)(long)cmdId.ExecuteScalar();

                // 3. Revertir stock de detalles anteriores
                RevertirStockCompraAnterior(c, tx, rowid);

                // 4. Borrar detalles anteriores
                var del = c.CreateCommand(); del.Transaction = tx;
                del.CommandText = "DELETE FROM DetallesCompra WHERE IdCompra=$id";
                del.Parameters.AddWithValue("$id", rowid);
                del.ExecuteNonQuery();

                // 5. Insertar nuevos detalles + stock + kardex
                GuardarLineasCompra(c, tx, rowid, cp.CompraN, lineas);

                tx.Commit();
                RegistrarAuditoria("COMPRA_EDIT", "Compras",
                    $"Compra {cp.CompraN} actualizada con {lineas.Count} líneas");
            }
            catch { tx.Rollback(); throw; }
        }

        /// <summary>
        /// Guarda cada línea de detalle, actualiza stock en Inventario y
        /// registra entrada en Kardex (MATERIA_PRIMA).
        /// </summary>
        private static void GuardarLineasCompra(
            SqliteConnection c, SqliteTransaction tx,
            int idCompra, string numCompra, List<DetalleCompra> lineas)
        {
            foreach (var linea in lineas)
            {
                if (linea.Cantidad <= 0) continue;

                // a) Insertar detalle
                var ins = c.CreateCommand(); ins.Transaction = tx;
                ins.CommandText = @"INSERT OR REPLACE INTO DetallesCompra
                    (IdCompra, CodigoI, Cantidad, PrecioC)
                    VALUES ($id,$cod,$cant,$p)";
                ins.Parameters.AddWithValue("$id",   idCompra);
                ins.Parameters.AddWithValue("$cod",  linea.CodigoI);
                ins.Parameters.AddWithValue("$cant", (double)linea.Cantidad);
                ins.Parameters.AddWithValue("$p",    (double)linea.PrecioC);
                ins.ExecuteNonQuery();

                // b) Leer saldo actual
                var cmdS = c.CreateCommand(); cmdS.Transaction = tx;
                cmdS.CommandText =
                    "SELECT COALESCE(Existencia,0) FROM Inventario WHERE Codigo=$cod";
                cmdS.Parameters.AddWithValue("$cod", linea.CodigoI);
                var stockObj = cmdS.ExecuteScalar();
                decimal saldoAnt   = stockObj != null ? (decimal)(double)stockObj : 0;
                decimal nuevoSaldo = saldoAnt + linea.Cantidad;

                // c) Actualizar stock
                var upd = c.CreateCommand(); upd.Transaction = tx;
                upd.CommandText = @"UPDATE Inventario
                    SET Existencia = Existencia + $q,
                        CostoActual = $p,
                        UltimaActualz = date('now','localtime')
                    WHERE Codigo = $cod";
                upd.Parameters.AddWithValue("$q",   (double)linea.Cantidad);
                upd.Parameters.AddWithValue("$p",   (double)linea.PrecioC);
                upd.Parameters.AddWithValue("$cod", linea.CodigoI);
                upd.ExecuteNonQuery();

                // d) Registrar movimiento Kardex A (MATERIA_PRIMA)
                var km = c.CreateCommand(); km.Transaction = tx;
                km.CommandText = @"INSERT INTO MovimientosInventario
                    (FechaHora, CodigoProducto, TipoMovimiento, Concepto,
                     Cantidad, SaldoAnterior, NuevoSaldo, Referencia, Usuario,
                     TipoKardex, CostoUnitario)
                    VALUES ($fh,$cp,'ENTRADA',$co,$ca,$sa,$ns,$ref,$usr,
                            'MATERIA_PRIMA',$cu)";
                km.Parameters.AddWithValue("$fh",  DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                km.Parameters.AddWithValue("$cp",  linea.CodigoI);
                km.Parameters.AddWithValue("$co",  "Compra " + numCompra);
                km.Parameters.AddWithValue("$ca",  (double)linea.Cantidad);
                km.Parameters.AddWithValue("$sa",  (double)saldoAnt);
                km.Parameters.AddWithValue("$ns",  (double)nuevoSaldo);
                km.Parameters.AddWithValue("$ref", numCompra);
                km.Parameters.AddWithValue("$usr", Sesion.NombreUsuario ?? "Sistema");
                km.Parameters.AddWithValue("$cu",  (double)linea.PrecioC);
                km.ExecuteNonQuery();
            }
        }

        /// <summary>
        /// Al editar una compra, revierte el stock sumado por los detalles anteriores.
        /// </summary>
        private static void RevertirStockCompraAnterior(
            SqliteConnection c, SqliteTransaction tx, int idCompra)
        {
            var cmd = c.CreateCommand(); cmd.Transaction = tx;
            cmd.CommandText =
                "SELECT CodigoI, Cantidad FROM DetallesCompra WHERE IdCompra=$id";
            cmd.Parameters.AddWithValue("$id", idCompra);
            using var r = cmd.ExecuteReader();
            var anteriores = new List<(string cod, decimal cant)>();
            while (r.Read())
                anteriores.Add((S(r, 0), D(r, 1)));
            r.Close();

            foreach (var (cod, cant) in anteriores)
            {
                var upd = c.CreateCommand(); upd.Transaction = tx;
                upd.CommandText =
                    "UPDATE Inventario SET Existencia = Existencia - $q WHERE Codigo = $cod";
                upd.Parameters.AddWithValue("$q",   (double)cant);
                upd.Parameters.AddWithValue("$cod", cod);
                upd.ExecuteNonQuery();
            }
        }
    }
}
