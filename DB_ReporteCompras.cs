using System;
using System.Collections.Generic;
using Microsoft.Data.Sqlite;

namespace SistemaGestion
{
    /// <summary>Fila del reporte de compras: módulo Compras + egresos.</summary>
    public class FilaReporteCompra
    {
        public DateTime Fecha       { get; set; }
        public string   Origen      { get; set; } = "";   // "Egreso" | "Compra"
        public string   Documento   { get; set; } = "";
        public string   Proveedor   { get; set; } = "";
        public string   Categoria   { get; set; } = "";
        public string   Concepto    { get; set; } = "";
        public decimal  Kg          { get; set; }
        public decimal  TotalUSD    { get; set; }
        public decimal  PagadoUSD   { get; set; }
        public decimal  SaldoUSD    => TotalUSD - PagadoUSD;
        public string   Estado      { get; set; } = "";
        public bool     EsMateriaPrima { get; set; }
    }

    public static partial class DatabaseService
    {
        /// <summary>
        /// Compras del período. Las compras de materia prima y demás gastos se
        /// registran como egresos (tabla Gastos); el módulo Compras (tabla Compras)
        /// se usa para compras de mercancía. Este reporte junta ambos.
        /// Excluye egresos anulados.
        /// </summary>
        public static List<FilaReporteCompra> ObtenerComprasReporte(
            DateTime desde, DateTime hasta, bool soloMateriaPrima = false)
        {
            var list = new List<FilaReporteCompra>();
            using var c = Conn();
            string d = desde.ToString("yyyy-MM-dd"), h = hasta.ToString("yyyy-MM-dd");

            // 1) Egresos (Gastos)
            using (var cmd = c.CreateCommand())
            {
                cmd.CommandText = @"
                    SELECT g.Id, g.Fecha, COALESCE(g.Proveedor,''), COALESCE(cg.Nombre,''),
                           COALESCE(g.Concepto,''), COALESCE(g.Observaciones,''),
                           COALESCE((SELECT SUM(dg.Cantidad) FROM DetallesGasto dg
                                     WHERE dg.IdGasto = g.Id AND COALESCE(dg.EsInventariable,0) = 1), 0),
                           COALESCE(g.MontoUSD,0), COALESCE(g.PagadoUSD,0),
                           COALESCE(g.Estado,''), COALESCE(cg.EsMateriaPrima,0)
                    FROM Gastos g
                    LEFT JOIN CategoriasGasto cg ON cg.Id = g.IdCategoria
                    WHERE g.Fecha >= $d AND g.Fecha <= $h
                      AND COALESCE(g.Estado,'') <> 'Anulado'
                      AND ($mp = 0 OR COALESCE(cg.EsMateriaPrima,0) = 1)
                    ORDER BY g.Fecha, g.Id";
                cmd.Parameters.AddWithValue("$d", d);
                cmd.Parameters.AddWithValue("$h", h);
                cmd.Parameters.AddWithValue("$mp", soloMateriaPrima ? 1 : 0);
                using var r = cmd.ExecuteReader();
                while (r.Read())
                {
                    string obs = S(r, 5);
                    string doc = "";
                    // "Doc: NE 0747 | ..." → "NE 0747"
                    if (obs.StartsWith("Doc:", StringComparison.OrdinalIgnoreCase))
                        doc = obs.Substring(4).Split('|')[0].Trim();
                    string estado = S(r, 9);
                    decimal total = D(r, 7), pagado = D(r, 8);
                    if (estado == "Pagado" && pagado <= 0) pagado = total;   // egresos viejos sin PagadoUSD
                    list.Add(new FilaReporteCompra
                    {
                        Fecha     = DateTime.TryParse(S(r, 1), out var f) ? f : DateTime.MinValue,
                        Origen    = "Egreso",
                        Documento = "Egreso #" + r.GetInt32(0) + (doc != "" ? " (" + doc + ")" : ""),
                        Proveedor = S(r, 2),
                        Categoria = S(r, 3),
                        Concepto  = S(r, 4),
                        Kg        = D(r, 6),
                        TotalUSD  = total,
                        PagadoUSD = pagado,
                        Estado    = estado,
                        EsMateriaPrima = D(r, 10) == 1
                    });
                }
            }

            // 2) Módulo Compras
            using (var cmd = c.CreateCommand())
            {
                cmd.CommandText = @"
                    SELECT c.CompraN, c.FechaC, COALESCE(p.Descripcion, c.CodigoP, ''),
                           COALESCE(c.FormaDePago,''), COALESCE(c.TotalC,0), COALESCE(c.AbonoC,0),
                           COALESCE(c.Observaciones,'')
                    FROM Compras c
                    LEFT JOIN Proveedores p ON p.Codigo = c.CodigoP
                    WHERE c.FechaC >= $d AND c.FechaC <= $h
                    ORDER BY c.FechaC";
                cmd.Parameters.AddWithValue("$d", d);
                cmd.Parameters.AddWithValue("$h", h);
                using var r = cmd.ExecuteReader();
                while (r.Read())
                {
                    decimal total = D(r, 4), abono = D(r, 5);
                    string fp = S(r, 3);
                    decimal pagado = fp.Equals("Contado", StringComparison.OrdinalIgnoreCase) ? total : Math.Min(abono, total);
                    list.Add(new FilaReporteCompra
                    {
                        Fecha     = DateTime.TryParse(S(r, 1), out var f) ? f : DateTime.MinValue,
                        Origen    = "Compra",
                        Documento = "Compra " + S(r, 0),
                        Proveedor = S(r, 2),
                        Categoria = "Compras",
                        Concepto  = S(r, 6),
                        TotalUSD  = total,
                        PagadoUSD = pagado,
                        Estado    = total - pagado > 0.01m ? "Pendiente" : "Pagado"
                    });
                }
            }

            list.Sort((a, b) => a.Fecha.CompareTo(b.Fecha));
            return list;
        }
    }
}
