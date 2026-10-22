// ════════════════════════════════════════════════════════════════════════
// ARCHIVO: DB_ProduccionDiaria.cs  →  Models\  (reemplaza version anterior)
// ════════════════════════════════════════════════════════════════════════

using System;
using System.Collections.Generic;
using Microsoft.Data.Sqlite;

namespace SistemaGestion
{
    public class ProduccionDiaria
    {
        public int      Id             { get; set; }
        public string   Codigo         { get; set; }
        public string   Descripcion    { get; set; }    // JOIN Inventario
        public DateTime Fecha          { get; set; }
        public decimal  Cantidad       { get; set; }
        public decimal  CostoUnitario  { get; set; }    // costo al momento del registro
        public string   Observacion    { get; set; }
        public int      IdEmpleado     { get; set; }    // 0 = sin asignar
        public string   NombreEmpleado { get; set; }    // JOIN Empleados

        public decimal CostoTotal => Cantidad * CostoUnitario;
    }

    public static partial class DatabaseService
    {
        public static List<ProduccionDiaria> ObtenerProduccionDiaria(DateTime fecha)
        {
            var list = new List<ProduccionDiaria>();
            using var c = Conn();
            var cmd = c.CreateCommand();
            cmd.CommandText = @"
                SELECT p.Id, p.Codigo,
                       COALESCE(i.Descripcion,'') ,
                       p.Fecha, p.Cantidad, p.CostoUnitario,
                       COALESCE(p.Observacion,''),
                       COALESCE(p.IdEmpleado,0),
                       COALESCE(e.Nombre||' '||e.Apellido,'')
                FROM ProduccionDiaria p
                LEFT JOIN Inventario i ON i.Codigo = p.Codigo
                LEFT JOIN Empleados  e ON e.Id     = p.IdEmpleado
                WHERE p.Fecha = $f
                ORDER BY p.Id DESC";
            cmd.Parameters.AddWithValue("$f", fecha.ToString("yyyy-MM-dd"));
            using var r = cmd.ExecuteReader();
            while (r.Read()) list.Add(LeerProd(r));
            return list;
        }

        public static List<ProduccionDiaria> ObtenerProduccionDiariaRango(
            DateTime desde, DateTime hasta)
        {
            var list = new List<ProduccionDiaria>();
            using var c = Conn();
            var cmd = c.CreateCommand();
            cmd.CommandText = @"
                SELECT p.Id, p.Codigo,
                       COALESCE(i.Descripcion,''),
                       p.Fecha, p.Cantidad, p.CostoUnitario,
                       COALESCE(p.Observacion,''),
                       COALESCE(p.IdEmpleado,0),
                       COALESCE(e.Nombre||' '||e.Apellido,'')
                FROM ProduccionDiaria p
                LEFT JOIN Inventario i ON i.Codigo = p.Codigo
                LEFT JOIN Empleados  e ON e.Id     = p.IdEmpleado
                WHERE p.Fecha >= $d AND p.Fecha <= $h

                UNION ALL

                -- Registro de producción por día (módulo actual): una fila por producto
                -- y día, con el costo de fabricación que quedó en el Kardex (entrada).
                SELECT -d.Id, d.CodigoProducto,
                       COALESCE(i.Descripcion,''),
                       r.Fecha, d.Cantidad,
                       COALESCE((SELECT m.CostoUnitario FROM MovimientosInventario m
                                  WHERE m.Referencia = 'PROD-' || r.Fecha
                                    AND m.CodigoProducto = d.CodigoProducto
                                    AND m.TipoMovimiento = 'ENTRADA'
                                    AND COALESCE(m.TipoKardex,'PRODUCTO') = 'PRODUCTO'
                                  ORDER BY m.IdMovimiento DESC LIMIT 1), 0),
                       TRIM(COALESCE(r.TipoMaterial,'') || ' ' || COALESCE(d.CodigoMolde,'')),
                       0, ''
                FROM DetalleProduccionMolde d
                JOIN RegistroProduccionDia r ON r.Id = d.IdRegistro
                LEFT JOIN Inventario i ON i.Codigo = d.CodigoProducto
                WHERE r.Fecha >= $d AND r.Fecha <= $h AND d.Cantidad > 0

                ORDER BY 4 DESC, 1 DESC";
            cmd.Parameters.AddWithValue("$d", desde.ToString("yyyy-MM-dd"));
            cmd.Parameters.AddWithValue("$h", hasta.ToString("yyyy-MM-dd"));
            using var r = cmd.ExecuteReader();
            while (r.Read()) list.Add(LeerProd(r));
            return list;
        }

        public static void InsertarProduccionDiaria(ProduccionDiaria p)
        {
            using var c = Conn();
            var cmd = c.CreateCommand();
            cmd.CommandText = @"
                INSERT INTO ProduccionDiaria
                    (Codigo,Fecha,Cantidad,CostoUnitario,Observacion,IdEmpleado)
                VALUES ($co,$fe,$ca,$cu,$ob,$ie)";
            BindProd(cmd, p); cmd.ExecuteNonQuery();
        }

        public static void ActualizarProduccionDiaria(ProduccionDiaria p)
        {
            using var c = Conn();
            var cmd = c.CreateCommand();
            cmd.CommandText = @"
                UPDATE ProduccionDiaria
                SET Codigo=$co,Fecha=$fe,Cantidad=$ca,
                    CostoUnitario=$cu,Observacion=$ob,IdEmpleado=$ie
                WHERE Id=$id";
            cmd.Parameters.AddWithValue("$id", p.Id);
            BindProd(cmd, p); cmd.ExecuteNonQuery();
        }

        public static void EliminarProduccionDiaria(int id)
        {
            using var c = Conn();
            Exec(c, "DELETE FROM ProduccionDiaria WHERE Id=$id", ("$id", id));
        }

        private static ProduccionDiaria LeerProd(SqliteDataReader r) => new ProduccionDiaria
        {
            Id             = r.GetInt32(0),
            Codigo         = S(r,1),
            Descripcion    = S(r,2),
            Fecha          = DateTime.Parse(r.GetString(3)),
            Cantidad       = D(r,4),
            CostoUnitario  = D(r,5),
            Observacion    = S(r,6),
            IdEmpleado     = r.IsDBNull(7) ? 0 : r.GetInt32(7),
            NombreEmpleado = S(r,8)
        };

        private static void BindProd(SqliteCommand cmd, ProduccionDiaria p)
        {
            cmd.Parameters.AddWithValue("$co", p.Codigo);
            cmd.Parameters.AddWithValue("$fe", p.Fecha.ToString("yyyy-MM-dd"));
            cmd.Parameters.AddWithValue("$ca", (double)p.Cantidad);
            cmd.Parameters.AddWithValue("$cu", (double)p.CostoUnitario);
            cmd.Parameters.AddWithValue("$ob", p.Observacion ?? "");
            cmd.Parameters.AddWithValue("$ie",
                p.IdEmpleado > 0 ? (object)p.IdEmpleado : DBNull.Value);
        }
    }
}
