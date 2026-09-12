using System;
using System.Collections.Generic;
using Microsoft.Data.Sqlite;

namespace SistemaGestion
{
    // -------------------------------------------------------------------------
    // MODELOS
    // -------------------------------------------------------------------------

    public class CuentaPagar
    {
        public int      Id             { get; set; }
        public string   CodigoProveedor{ get; set; } = "";
        public string   Proveedor      { get; set; } = "";
        public string   Concepto       { get; set; } = "";
        public string   NumFactura     { get; set; } = "";  // referencia a Compras
        public decimal  Monto          { get; set; }
        public decimal  Pagado         { get; set; }
        public decimal  Saldo          => Monto - Pagado;
        public DateTime FechaEmision   { get; set; }
        public DateTime FechaVence     { get; set; }
        public string   Estado         { get; set; } = "Pendiente"; // Pendiente/Parcial/Pagada/Vencida
        public string   Observaciones  { get; set; } = "";
        public int      DiasVencida    => Estado != "Pagada"
            ? Math.Max(0, (DateTime.Today - FechaVence).Days) : 0;
    }

    public class PagoCxP
    {
        public int      Id          { get; set; }
        public int      IdCuenta    { get; set; }
        public DateTime Fecha       { get; set; }
        public decimal  Monto       { get; set; }
        public string   FormaPago   { get; set; } = "";
        public string   Referencia  { get; set; } = "";
        public string   Observaciones{ get; set; } = "";
    }

    // -------------------------------------------------------------------------
    // MIGRACION v8 + METODOS
    // -------------------------------------------------------------------------

    public static partial class DatabaseService
    {
        // Llamar desde MigrarEsquema() en DB.cs agregando:
        //   if (versionActual < 8) MigrarV8(conn, tx);
        public static void MigrarV8(SqliteConnection conn, SqliteTransaction tx)
        {
            var cmd = conn.CreateCommand();
            cmd.Transaction = tx;
            cmd.CommandText = @"
                CREATE TABLE IF NOT EXISTS CuentasPagar (
                    Id              INTEGER PRIMARY KEY AUTOINCREMENT,
                    CodigoProveedor TEXT,
                    Proveedor       TEXT,
                    Concepto        TEXT NOT NULL,
                    NumFactura      TEXT,
                    Monto           REAL DEFAULT 0,
                    Pagado          REAL DEFAULT 0,
                    FechaEmision    TEXT NOT NULL,
                    FechaVence      TEXT NOT NULL,
                    Estado          TEXT DEFAULT 'Pendiente',
                    Observaciones   TEXT
                );

                CREATE TABLE IF NOT EXISTS PagosCxP (
                    Id           INTEGER PRIMARY KEY AUTOINCREMENT,
                    IdCuenta     INTEGER NOT NULL,
                    Fecha        TEXT NOT NULL,
                    Monto        REAL DEFAULT 0,
                    FormaPago    TEXT,
                    Referencia   TEXT,
                    Observaciones TEXT,
                    FOREIGN KEY(IdCuenta) REFERENCES CuentasPagar(Id)
                );

                PRAGMA user_version = 8;
            ";
            cmd.ExecuteNonQuery();
        }

        // ── CRUD CuentasPagar ────────────────────────────────────────────

        public static List<CuentaPagar> ObtenerCuentasPagar(string estado = null)
        {
            var lista = new List<CuentaPagar>();
            using var conn = Conn();
            var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT * FROM CuentasPagar"
                + (estado != null ? " WHERE Estado=$es" : "")
                + " ORDER BY FechaVence ASC";
            if (estado != null) cmd.Parameters.AddWithValue("$es", estado);

            using var r = cmd.ExecuteReader();
            while (r.Read())
                lista.Add(MapCxP(r));
            return lista;
        }

        public static void InsertarCuentaPagar(CuentaPagar c)
        {
            using var conn = Conn();
            var cmd = conn.CreateCommand();
            cmd.CommandText = @"INSERT INTO CuentasPagar
                (CodigoProveedor,Proveedor,Concepto,NumFactura,Monto,Pagado,
                 FechaEmision,FechaVence,Estado,Observaciones)
                VALUES($cp,$pr,$co,$nf,$mo,$pa,$fe,$fv,$es,$ob)";
            BindCxP(cmd, c);
            cmd.ExecuteNonQuery();
            RegistrarAuditoria("CXP_NUEVA", "CuentasPagar",
                $"{c.Concepto} — {c.Proveedor} — ${c.Monto:N2}");
        }

        public static void ActualizarCuentaPagar(CuentaPagar c)
        {
            using var conn = Conn();
            var cmd = conn.CreateCommand();
            cmd.CommandText = @"UPDATE CuentasPagar SET
                CodigoProveedor=$cp, Proveedor=$pr, Concepto=$co, NumFactura=$nf,
                Monto=$mo, Pagado=$pa, FechaEmision=$fe, FechaVence=$fv,
                Estado=$es, Observaciones=$ob
                WHERE Id=$id";
            BindCxP(cmd, c);
            cmd.Parameters.AddWithValue("$id", c.Id);
            cmd.ExecuteNonQuery();
        }

        /// <summary>
        /// Crea o actualiza la Cuenta por Pagar ligada a un egreso según su saldo.
        /// Si el egreso quedó pagado completo y existía una CxP ligada, la marca pagada.
        /// Es idempotente: una CxP por egreso (ligada por IdEgreso).
        /// </summary>
        public static void SincronizarCxPDesdeEgreso(
            int idEgreso, string proveedor, string concepto, string numRef,
            decimal montoUSD, decimal pagadoUSD, DateTime emision, DateTime vence,
            string codigoProveedor = "")
        {
            if (idEgreso <= 0) return;
            decimal saldo = montoUSD - pagadoUSD;
            using var conn = Conn();

            int idCxP = -1;
            var q = conn.CreateCommand();
            q.CommandText = "SELECT Id FROM CuentasPagar WHERE IdEgreso=$ie LIMIT 1";
            q.Parameters.AddWithValue("$ie", idEgreso);
            var o = q.ExecuteScalar();
            if (o != null && o != DBNull.Value) idCxP = Convert.ToInt32(o);

            if (saldo <= 0.01m)
            {
                if (idCxP > 0)
                {
                    var up = conn.CreateCommand();
                    up.CommandText = "UPDATE CuentasPagar SET Monto=$mo, Pagado=$mo, Estado='Pagada' WHERE Id=$id";
                    up.Parameters.AddWithValue("$mo", (double)montoUSD);
                    up.Parameters.AddWithValue("$id", idCxP);
                    up.ExecuteNonQuery();
                }
                return;
            }

            string estado = pagadoUSD > 0 ? "Parcial" : "Pendiente";
            if (idCxP > 0)
            {
                var up = conn.CreateCommand();
                up.CommandText = @"UPDATE CuentasPagar SET
                    CodigoProveedor=$cp, Proveedor=$pr, Concepto=$co, NumFactura=$nf,
                    Monto=$mo, Pagado=$pa, FechaEmision=$fe, FechaVence=$fv, Estado=$es
                    WHERE Id=$id";
                up.Parameters.AddWithValue("$cp", codigoProveedor ?? "");
                up.Parameters.AddWithValue("$pr", proveedor ?? "");
                up.Parameters.AddWithValue("$co", concepto ?? "");
                up.Parameters.AddWithValue("$nf", numRef ?? "");
                up.Parameters.AddWithValue("$mo", (double)montoUSD);
                up.Parameters.AddWithValue("$pa", (double)pagadoUSD);
                up.Parameters.AddWithValue("$fe", emision.ToString("yyyy-MM-dd"));
                up.Parameters.AddWithValue("$fv", vence.ToString("yyyy-MM-dd"));
                up.Parameters.AddWithValue("$es", estado);
                up.Parameters.AddWithValue("$id", idCxP);
                up.ExecuteNonQuery();
            }
            else
            {
                var ins = conn.CreateCommand();
                ins.CommandText = @"INSERT INTO CuentasPagar
                    (CodigoProveedor,Proveedor,Concepto,NumFactura,Monto,Pagado,
                     FechaEmision,FechaVence,Estado,Observaciones,IdEgreso)
                    VALUES($cp,$pr,$co,$nf,$mo,$pa,$fe,$fv,$es,$ob,$ie)";
                ins.Parameters.AddWithValue("$cp", codigoProveedor ?? "");
                ins.Parameters.AddWithValue("$pr", proveedor ?? "");
                ins.Parameters.AddWithValue("$co", concepto ?? "");
                ins.Parameters.AddWithValue("$nf", numRef ?? "");
                ins.Parameters.AddWithValue("$mo", (double)montoUSD);
                ins.Parameters.AddWithValue("$pa", (double)pagadoUSD);
                ins.Parameters.AddWithValue("$fe", emision.ToString("yyyy-MM-dd"));
                ins.Parameters.AddWithValue("$fv", vence.ToString("yyyy-MM-dd"));
                ins.Parameters.AddWithValue("$es", estado);
                ins.Parameters.AddWithValue("$ob", "Generada automáticamente desde el egreso #" + idEgreso);
                ins.Parameters.AddWithValue("$ie", idEgreso);
                ins.ExecuteNonQuery();
                RegistrarAuditoria("CXP_AUTO_EGRESO", "CuentasPagar",
                    $"{concepto} — {proveedor} — saldo ${saldo:N2} (egreso #{idEgreso})");
            }
        }

        public static void EliminarCuentaPagar(int id)
        {
            using var conn = Conn();
            Exec(conn, "DELETE FROM PagosCxP WHERE IdCuenta=$id",  ("$id", id));
            Exec(conn, "DELETE FROM CuentasPagar WHERE Id=$id",    ("$id", id));
            RegistrarAuditoria("CXP_ELIMINAR", "CuentasPagar", $"Id={id}");
        }

        // ── PAGOS ────────────────────────────────────────────────────────

        public static List<PagoCxP> ObtenerPagosCxP(int idCuenta)
        {
            var lista = new List<PagoCxP>();
            using var conn = Conn();
            var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT * FROM PagosCxP WHERE IdCuenta=$id ORDER BY Fecha DESC";
            cmd.Parameters.AddWithValue("$id", idCuenta);
            using var r = cmd.ExecuteReader();
            while (r.Read())
                lista.Add(new PagoCxP
                {
                    Id           = r.GetInt32(0),
                    IdCuenta     = r.GetInt32(1),
                    Fecha        = DateTime.Parse(S(r, 2)),
                    Monto        = D(r, 3),
                    FormaPago    = S(r, 4),
                    Referencia   = S(r, 5),
                    Observaciones= S(r, 6)
                });
            return lista;
        }

        public static void RegistrarPagoCxP(int idCuenta, PagoCxP pago)
        {
            using var conn = new SqliteConnection(ConnStr);
            conn.Open();
            using var tx = conn.BeginTransaction();
            try
            {
                // Insertar pago
                var cmd = conn.CreateCommand();
                cmd.Transaction = tx;
                cmd.CommandText = @"INSERT INTO PagosCxP
                    (IdCuenta,Fecha,Monto,FormaPago,Referencia,Observaciones)
                    VALUES($ic,$fe,$mo,$fp,$re,$ob)";
                cmd.Parameters.AddWithValue("$ic", idCuenta);
                cmd.Parameters.AddWithValue("$fe", pago.Fecha.ToString("yyyy-MM-dd"));
                cmd.Parameters.AddWithValue("$mo", (double)pago.Monto);
                cmd.Parameters.AddWithValue("$fp", pago.FormaPago ?? "");
                cmd.Parameters.AddWithValue("$re", pago.Referencia ?? "");
                cmd.Parameters.AddWithValue("$ob", pago.Observaciones ?? "");
                cmd.ExecuteNonQuery();

                // Leer monto, pagado y egreso ligado actuales
                var cmdLeer = conn.CreateCommand();
                cmdLeer.Transaction = tx;
                cmdLeer.CommandText = "SELECT Monto, Pagado, IdEgreso FROM CuentasPagar WHERE Id=$id";
                cmdLeer.Parameters.AddWithValue("$id", idCuenta);
                decimal monto, pagado;
                int idEgreso = 0;
                using (var r = cmdLeer.ExecuteReader())
                {
                    r.Read();
                    monto  = (decimal)(double)r["Monto"];
                    pagado = (decimal)(double)r["Pagado"] + pago.Monto;
                    var oe = r["IdEgreso"];
                    if (oe != null && oe != DBNull.Value) idEgreso = Convert.ToInt32(oe);
                }

                if (pagado < 0) pagado = 0;
                string nuevoEstado = pagado >= monto - 0.01m ? "Pagada"
                    : pagado > 0 ? "Parcial" : "Pendiente";

                // Actualizar saldo y estado
                var cmdUpd = conn.CreateCommand();
                cmdUpd.Transaction = tx;
                cmdUpd.CommandText = @"UPDATE CuentasPagar
                    SET Pagado=$pa, Estado=$es WHERE Id=$id";
                cmdUpd.Parameters.AddWithValue("$pa", (double)pagado);
                cmdUpd.Parameters.AddWithValue("$es", nuevoEstado);
                cmdUpd.Parameters.AddWithValue("$id", idCuenta);
                cmdUpd.ExecuteNonQuery();

                // ── Consistencia: sincronizar el egreso ligado ──────────────
                // Si esta CxP nació de un egreso (Gastos), reflejar el pago allí
                // para que el registro del egreso no quede como 'Pendiente' obsoleto.
                if (idEgreso > 0)
                {
                    // El pago de la CxP se expresa en USD; el abonado del egreso
                    // se lleva al mismo valor (topeado a su MontoUSD).
                    var cmdEgr = conn.CreateCommand();
                    cmdEgr.Transaction = tx;
                    cmdEgr.CommandText = "SELECT MontoUSD, Estado FROM Gastos WHERE Id=$id";
                    cmdEgr.Parameters.AddWithValue("$id", idEgreso);
                    decimal montoEgr = 0m; string estadoEgr = "";
                    using (var re = cmdEgr.ExecuteReader())
                    {
                        if (re.Read())
                        {
                            montoEgr  = (decimal)(double)re["MontoUSD"];
                            estadoEgr = re["Estado"]?.ToString() ?? "";
                        }
                    }

                    // No tocar egresos anulados.
                    if (estadoEgr != "Anulado" && montoEgr > 0)
                    {
                        decimal pagadoEgr = Math.Min(pagado, montoEgr);
                        bool completo = pagadoEgr >= montoEgr - 0.01m;
                        string estadoNuevoEgr = completo ? "Pagado" : "Pendiente";

                        var upEgr = conn.CreateCommand();
                        upEgr.Transaction = tx;
                        upEgr.CommandText = completo
                            ? @"UPDATE Gastos SET PagadoUSD=$pa, Estado=$es,
                                   FechaPago=COALESCE(FechaPago,$hoy), FechaVencimiento=NULL
                                WHERE Id=$id"
                            : @"UPDATE Gastos SET PagadoUSD=$pa, Estado=$es WHERE Id=$id";
                        upEgr.Parameters.AddWithValue("$pa", (double)pagadoEgr);
                        upEgr.Parameters.AddWithValue("$es", estadoNuevoEgr);
                        if (completo)
                            upEgr.Parameters.AddWithValue("$hoy", pago.Fecha.ToString("yyyy-MM-dd"));
                        upEgr.Parameters.AddWithValue("$id", idEgreso);
                        upEgr.ExecuteNonQuery();
                    }
                }

                tx.Commit();
                RegistrarAuditoria("CXP_PAGO", "CuentasPagar",
                    $"Id={idCuenta} — Pago ${pago.Monto:N2} — Estado: {nuevoEstado}"
                    + (idEgreso > 0 ? $" (egreso #{idEgreso} sincronizado)" : ""));
            }
            catch { tx.Rollback(); throw; }
        }

        // ── Resumen para FlujoCaja ───────────────────────────────────────

        public static decimal ObtenerTotalCxPPendiente()
        {
            using var conn = Conn();
            var cmd = conn.CreateCommand();
            cmd.CommandText = @"SELECT COALESCE(SUM(Monto - Pagado), 0)
                FROM CuentasPagar WHERE Estado != 'Pagada'";
            return Convert.ToDecimal(cmd.ExecuteScalar() ?? 0m);
        }

        // ── Helpers privados ─────────────────────────────────────────────

        private static CuentaPagar MapCxP(SqliteDataReader r) => new CuentaPagar
        {
            Id              = r.GetInt32(0),
            CodigoProveedor = S(r, 1),
            Proveedor       = S(r, 2),
            Concepto        = S(r, 3),
            NumFactura      = S(r, 4),
            Monto           = D(r, 5),
            Pagado          = D(r, 6),
            FechaEmision    = DateTime.TryParse(S(r, 7), out var fe) ? fe : DateTime.Today,
            FechaVence      = DateTime.TryParse(S(r, 8), out var fv) ? fv : DateTime.Today,
            Estado          = S(r, 9),
            Observaciones   = S(r, 10)
        };

        private static void BindCxP(SqliteCommand cmd, CuentaPagar c)
        {
            cmd.Parameters.AddWithValue("$cp", c.CodigoProveedor ?? "");
            cmd.Parameters.AddWithValue("$pr", c.Proveedor ?? "");
            cmd.Parameters.AddWithValue("$co", c.Concepto ?? "");
            cmd.Parameters.AddWithValue("$nf", c.NumFactura ?? "");
            cmd.Parameters.AddWithValue("$mo", (double)c.Monto);
            cmd.Parameters.AddWithValue("$pa", (double)c.Pagado);
            cmd.Parameters.AddWithValue("$fe", c.FechaEmision.ToString("yyyy-MM-dd"));
            cmd.Parameters.AddWithValue("$fv", c.FechaVence.ToString("yyyy-MM-dd"));
            cmd.Parameters.AddWithValue("$es", c.Estado ?? "Pendiente");
            cmd.Parameters.AddWithValue("$ob", c.Observaciones ?? "");
        }
    }
}
