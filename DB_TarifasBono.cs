// ════════════════════════════════════════════════════════════════════════
// ARCHIVO: DB_TarifasBono.cs  →  Models\  (reemplaza version anterior)
// ════════════════════════════════════════════════════════════════════════

using System;
using System.Collections.Generic;
using Microsoft.Data.Sqlite;

namespace SistemaGestion
{
    public class TarifaBono
    {
        public int       Id              { get; set; }
        public int       IdEmpleado      { get; set; }
        public string    NombreEmpleado  { get; set; }
        public string    CodigoProducto  { get; set; }
        public string    DescProducto    { get; set; }
        public decimal   TarifaBs        { get; set; }
        public DateTime  FechaDesde      { get; set; }
        public DateTime? FechaHasta      { get; set; }
        public bool      Vigente =>
            FechaHasta == null || FechaHasta.Value >= DateTime.Today;
    }

    public static partial class DatabaseService
    {
        // ── Produccion ────────────────────────────────────────────────────
        public static List<TarifaBono> ObtenerTarifasBono(bool soloVigentes = false)
            => _ObtenerTarifas("TarifasBonoProduccion", soloVigentes);

        public static List<TarifaBono> ObtenerTarifasBonoEmpleado(int idEmpleado)
            => _ObtenerTarifasEmp("TarifasBonoProduccion", idEmpleado);

        public static void InsertarTarifaBono(TarifaBono t)
            => _InsertarTarifa("TarifasBonoProduccion", t);

        public static void ActualizarTarifaBono(TarifaBono t)
            => _ActualizarTarifa("TarifasBonoProduccion", t);

        public static void EliminarTarifaBono(int id)
            => _EliminarTarifa("TarifasBonoProduccion", id);

        public static void CerrarTarifaBono(int id)
            => _CerrarTarifa("TarifasBonoProduccion", id);

        // ── Ventas ────────────────────────────────────────────────────────
        public static List<TarifaBono> ObtenerTarifasBonoVentas(bool soloVigentes = false)
            => _ObtenerTarifas("TarifasBonoVentas", soloVigentes);

        public static List<TarifaBono> ObtenerTarifasBonoVentasEmpleado(int idEmpleado)
            => _ObtenerTarifasEmp("TarifasBonoVentas", idEmpleado);

        public static void InsertarTarifaBonoVentas(TarifaBono t)
            => _InsertarTarifa("TarifasBonoVentas", t);

        public static void ActualizarTarifaBonoVentas(TarifaBono t)
            => _ActualizarTarifa("TarifasBonoVentas", t);

        public static void EliminarTarifaBonoVentas(int id)
            => _EliminarTarifa("TarifasBonoVentas", id);

        public static void CerrarTarifaBonoVentas(int id)
            => _CerrarTarifa("TarifasBonoVentas", id);

        // ── Helpers privados ──────────────────────────────────────────────
        private static List<TarifaBono> _ObtenerTarifas(string tabla, bool soloVigentes)
        {
            var list = new List<TarifaBono>();
            using var c = Conn();
            var cmd = c.CreateCommand();
            cmd.CommandText = $@"
                SELECT t.Id, t.IdEmpleado,
                       e.Nombre||' '||e.Apellido,
                       t.CodigoProducto,
                       COALESCE(i.Descripcion, t.CodigoProducto),
                       t.TarifaBs, t.FechaDesde, t.FechaHasta
                FROM {tabla} t
                JOIN Empleados e ON e.Id = t.IdEmpleado
                LEFT JOIN Inventario i ON i.Codigo = t.CodigoProducto
                ORDER BY e.Apellido, e.Nombre, t.CodigoProducto, t.FechaDesde DESC";
            using var r = cmd.ExecuteReader();
            while (r.Read())
            {
                var tb = _LeerTarifa(r);
                if (!soloVigentes || tb.Vigente) list.Add(tb);
            }
            return list;
        }

        private static List<TarifaBono> _ObtenerTarifasEmp(string tabla, int idEmp)
        {
            var list = new List<TarifaBono>();
            using var c = Conn();
            var cmd = c.CreateCommand();
            cmd.CommandText = $@"
                SELECT t.Id, t.IdEmpleado,
                       e.Nombre||' '||e.Apellido,
                       t.CodigoProducto,
                       COALESCE(i.Descripcion, t.CodigoProducto),
                       t.TarifaBs, t.FechaDesde, t.FechaHasta
                FROM {tabla} t
                JOIN Empleados e ON e.Id = t.IdEmpleado
                LEFT JOIN Inventario i ON i.Codigo = t.CodigoProducto
                WHERE t.IdEmpleado = $id
                ORDER BY t.CodigoProducto, t.FechaDesde DESC";
            cmd.Parameters.AddWithValue("$id", idEmp);
            using var r = cmd.ExecuteReader();
            while (r.Read()) list.Add(_LeerTarifa(r));
            return list;
        }

        private static void _InsertarTarifa(string tabla, TarifaBono t)
        {
            using var c = Conn();
            var cmd = c.CreateCommand();
            cmd.CommandText = $@"
                INSERT INTO {tabla}
                    (IdEmpleado,CodigoProducto,TarifaBs,FechaDesde,FechaHasta)
                VALUES ($ie,$cp,$tb,$fd,$fh)";
            _BindTarifa(cmd, t); cmd.ExecuteNonQuery();
        }

        private static void _ActualizarTarifa(string tabla, TarifaBono t)
        {
            using var c = Conn();
            var cmd = c.CreateCommand();
            cmd.CommandText = $@"
                UPDATE {tabla}
                SET IdEmpleado=$ie,CodigoProducto=$cp,TarifaBs=$tb,
                    FechaDesde=$fd,FechaHasta=$fh
                WHERE Id=$id";
            cmd.Parameters.AddWithValue("$id", t.Id);
            _BindTarifa(cmd, t); cmd.ExecuteNonQuery();
        }

        private static void _EliminarTarifa(string tabla, int id)
        {
            using var c = Conn();
            Exec(c, $"DELETE FROM {tabla} WHERE Id=$id", ("$id", id));
        }

        private static void _CerrarTarifa(string tabla, int id)
        {
            using var c = Conn();
            Exec(c, $"UPDATE {tabla} SET FechaHasta=$fh WHERE Id=$id",
                ("$fh", DateTime.Today.AddDays(-1).ToString("yyyy-MM-dd")),
                ("$id", id));
        }

        private static TarifaBono _LeerTarifa(SqliteDataReader r) => new TarifaBono
        {
            Id             = r.GetInt32(0),
            IdEmpleado     = r.GetInt32(1),
            NombreEmpleado = S(r, 2),
            CodigoProducto = S(r, 3),
            DescProducto   = S(r, 4),
            TarifaBs       = D(r, 5),
            FechaDesde     = DateTime.Parse(r.GetString(6)),
            FechaHasta     = r.IsDBNull(7) ? (DateTime?)null
                           : DateTime.Parse(r.GetString(7))
        };

        private static void _BindTarifa(SqliteCommand cmd, TarifaBono t)
        {
            cmd.Parameters.AddWithValue("$ie", t.IdEmpleado);
            cmd.Parameters.AddWithValue("$cp", t.CodigoProducto);
            cmd.Parameters.AddWithValue("$tb", (double)t.TarifaBs);
            cmd.Parameters.AddWithValue("$fd", t.FechaDesde.ToString("yyyy-MM-dd"));
            cmd.Parameters.AddWithValue("$fh",
                t.FechaHasta.HasValue
                    ? (object)t.FechaHasta.Value.ToString("yyyy-MM-dd")
                    : DBNull.Value);
        }
    }
}
