using System;
using System.Collections.Generic;
using Microsoft.Data.Sqlite;

namespace SistemaGestion
{
    /// <summary>
    /// Fila del libro IVA (ventas o compras).
    /// </summary>
    public class FilaLibroIVA
    {
        public DateTime Fecha          { get; set; }
        public string   NumeroDoc      { get; set; } = "";
        public string   Nombre         { get; set; } = "";  // cliente o proveedor
        public string   FormaPago      { get; set; } = "";
        public decimal  BaseImponible  { get; set; }        // Total - IVA
        public decimal  MontoIVA       { get; set; }        // IvaF o IvaC
        public decimal  Total          { get; set; }        // TotalF o TotalC
    }

    public static partial class DatabaseService
    {
        /// <summary>
        /// Libro de Ventas del mes/anio indicado.
        /// Base imponible = TotalF - IvaF
        /// </summary>
        public static List<FilaLibroIVA> ObtenerLibroVentas(int mes, int anio)
        {
            var lista = new List<FilaLibroIVA>();
            string desde = new DateTime(anio, mes, 1).ToString("yyyy-MM-dd");
            string hasta = new DateTime(anio, mes, DateTime.DaysInMonth(anio, mes)).ToString("yyyy-MM-dd");

            using var conn = Conn();
            conn.Open();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                SELECT f.NumeroF, f.Fecha, f.Codigo, f.FormaDePago, f.IvaF, f.TotalF,
                       COALESCE(c.Descripcion, f.Codigo) AS NombreCliente
                FROM Facturas f
                LEFT JOIN Clientes c ON c.Codigo = f.Codigo
                WHERE f.Fecha >= $desde
                  AND f.Fecha <= $hasta
                ORDER BY f.Fecha ASC, f.NumeroF ASC";
            cmd.Parameters.AddWithValue("$desde", desde);
            cmd.Parameters.AddWithValue("$hasta", hasta);

            using var r = cmd.ExecuteReader();
            while (r.Read())
            {
                decimal iva   = D(r, 4);
                decimal total = D(r, 5);
                lista.Add(new FilaLibroIVA
                {
                    Fecha         = DateTime.Parse(S(r, 1)),
                    NumeroDoc     = S(r, 0),
                    Nombre        = S(r, 6),
                    FormaPago     = S(r, 3),
                    MontoIVA      = iva,
                    BaseImponible = total - iva,
                    Total         = total
                });
            }
            return lista;
        }

        /// <summary>
        /// Libro de Compras del mes/anio indicado.
        /// Base imponible = TotalC - IvaC
        /// </summary>
        public static List<FilaLibroIVA> ObtenerLibroCompras(int mes, int anio)
        {
            var lista = new List<FilaLibroIVA>();
            string desde = new DateTime(anio, mes, 1).ToString("yyyy-MM-dd");
            string hasta = new DateTime(anio, mes, DateTime.DaysInMonth(anio, mes)).ToString("yyyy-MM-dd");

            using var conn = Conn();
            conn.Open();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                SELECT c.CompraN, c.FechaC, c.CodigoP, c.FormaDePago, c.IvaC, c.TotalC,
                       COALESCE(p.Descripcion, c.CodigoP) AS NombreProveedor
                FROM Compras c
                LEFT JOIN Proveedores p ON p.Codigo = c.CodigoP
                WHERE c.FechaC >= $desde
                  AND c.FechaC <= $hasta
                ORDER BY c.FechaC ASC, c.CompraN ASC";
            cmd.Parameters.AddWithValue("$desde", desde);
            cmd.Parameters.AddWithValue("$hasta", hasta);

            using var r = cmd.ExecuteReader();
            while (r.Read())
            {
                decimal iva   = D(r, 4);
                decimal total = D(r, 5);
                lista.Add(new FilaLibroIVA
                {
                    Fecha         = DateTime.Parse(S(r, 1)),
                    NumeroDoc     = S(r, 0),
                    Nombre        = S(r, 6),
                    FormaPago     = S(r, 3),
                    MontoIVA      = iva,
                    BaseImponible = total - iva,
                    Total         = total
                });
            }
            return lista;
        }
    }
}
