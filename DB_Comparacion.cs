using System;
using System.Linq;
using Microsoft.Data.Sqlite;

namespace SistemaGestion
{
    public class ComparacionData
    {
        // Ventas
        public decimal VentasPer1    { get; set; }
        public decimal VentasPer2    { get; set; }
        public decimal NFacturasPer1 { get; set; }
        public decimal NFacturasPer2 { get; set; }
        public decimal TicketPer1    => NFacturasPer1 > 0 ? VentasPer1 / NFacturasPer1 : 0;
        public decimal TicketPer2    => NFacturasPer2 > 0 ? VentasPer2 / NFacturasPer2 : 0;
        public decimal IvaVentasPer1 { get; set; }
        public decimal IvaVentasPer2 { get; set; }

        // Compras
        public decimal ComprasPer1   { get; set; }
        public decimal ComprasPer2   { get; set; }
        public decimal NComprasPer1  { get; set; }
        public decimal NComprasPer2  { get; set; }
        public decimal IvaComprasPer1{ get; set; }
        public decimal IvaComprasPer2{ get; set; }

        // Gastos
        public decimal GastosPer1    { get; set; }
        public decimal GastosPer2    { get; set; }

        // Nomina
        public decimal NominaPer1    { get; set; }
        public decimal NominaPer2    { get; set; }
        public decimal NPeriodosPer1 { get; set; }
        public decimal NPeriodosPer2 { get; set; }

        // Calculados
        public decimal UtilidadPer1  => VentasPer1 - ComprasPer1 - GastosPer1;
        public decimal UtilidadPer2  => VentasPer2 - ComprasPer2 - GastosPer2;
        public decimal MargenPer1    => VentasPer1 > 0 ? UtilidadPer1 / VentasPer1 * 100 : 0;
        public decimal MargenPer2    => VentasPer2 > 0 ? UtilidadPer2 / VentasPer2 * 100 : 0;
    }

    public static partial class DatabaseService
    {
        // Compras del período: módulo Compras + egresos de materia prima.
        internal const string SqlComprasPeriodo = @"
            SELECT
              (SELECT COALESCE(SUM(TotalC),0) FROM Compras WHERE FechaC >= $s AND FechaC <= $e)
              + (SELECT COALESCE(SUM(g.MontoUSD),0) FROM Gastos g
                   JOIN CategoriasGasto cg ON cg.Id = g.IdCategoria
                  WHERE COALESCE(cg.EsMateriaPrima,0) = 1 AND COALESCE(g.Estado,'') <> 'Anulado'
                    AND g.Fecha >= $s AND g.Fecha <= $e),
              (SELECT COUNT(*) FROM Compras WHERE FechaC >= $s AND FechaC <= $e)
              + (SELECT COUNT(*) FROM Gastos g
                   JOIN CategoriasGasto cg ON cg.Id = g.IdCategoria
                  WHERE COALESCE(cg.EsMateriaPrima,0) = 1 AND COALESCE(g.Estado,'') <> 'Anulado'
                    AND g.Fecha >= $s AND g.Fecha <= $e),
              (SELECT COALESCE(SUM(IvaC),0) FROM Compras WHERE FechaC >= $s AND FechaC <= $e)";

        // Gastos operativos del período: egresos que no son materia prima.
        internal const string SqlGastosOperativosPeriodo = @"
            SELECT COALESCE(SUM(g.MontoUSD),0) FROM Gastos g
              LEFT JOIN CategoriasGasto cg ON cg.Id = g.IdCategoria
             WHERE COALESCE(cg.EsMateriaPrima,0) = 0 AND COALESCE(g.Estado,'') <> 'Anulado'
               AND g.Fecha >= $s AND g.Fecha <= $e";

        public static ComparacionData ObtenerComparacion(
            DateTime p1Desde, DateTime p1Hasta,
            DateTime p2Desde, DateTime p2Hasta)
        {
            var data = new ComparacionData();
            using var conn = Conn();

            string s1 = p1Desde.ToString("yyyy-MM-dd");
            string e1 = p1Hasta.ToString("yyyy-MM-dd");
            string s2 = p2Desde.ToString("yyyy-MM-dd");
            string e2 = p2Hasta.ToString("yyyy-MM-dd");

            // ── Ventas Periodo 1
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = @"SELECT COALESCE(SUM(TotalF),0), COUNT(*), COALESCE(SUM(IvaF),0)
                    FROM Facturas WHERE Fecha >= $s AND Fecha <= $e AND COALESCE(Anulada,0) = 0";
                cmd.Parameters.AddWithValue("$s", s1);
                cmd.Parameters.AddWithValue("$e", e1);
                using var r = cmd.ExecuteReader();
                if (r.Read()) {
                    data.VentasPer1    = D(r, 0);
                    data.NFacturasPer1 = D(r, 1);
                    data.IvaVentasPer1 = D(r, 2);
                }
            }

            // ── Ventas Periodo 2
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = @"SELECT COALESCE(SUM(TotalF),0), COUNT(*), COALESCE(SUM(IvaF),0)
                    FROM Facturas WHERE Fecha >= $s AND Fecha <= $e AND COALESCE(Anulada,0) = 0";
                cmd.Parameters.AddWithValue("$s", s2);
                cmd.Parameters.AddWithValue("$e", e2);
                using var r = cmd.ExecuteReader();
                if (r.Read()) {
                    data.VentasPer2    = D(r, 0);
                    data.NFacturasPer2 = D(r, 1);
                    data.IvaVentasPer2 = D(r, 2);
                }
            }

            // ── Compras Periodo 1
            using (var cmd = conn.CreateCommand())
            {
                // Compras = módulo Compras + egresos de materia prima (no anulados)
                cmd.CommandText = SqlComprasPeriodo;
                cmd.Parameters.AddWithValue("$s", s1);
                cmd.Parameters.AddWithValue("$e", e1);
                using var r = cmd.ExecuteReader();
                if (r.Read()) {
                    data.ComprasPer1    = D(r, 0);
                    data.NComprasPer1   = D(r, 1);
                    data.IvaComprasPer1 = D(r, 2);
                }
            }

            // ── Compras Periodo 2
            using (var cmd = conn.CreateCommand())
            {
                // Compras = módulo Compras + egresos de materia prima (no anulados)
                cmd.CommandText = SqlComprasPeriodo;
                cmd.Parameters.AddWithValue("$s", s2);
                cmd.Parameters.AddWithValue("$e", e2);
                using var r = cmd.ExecuteReader();
                if (r.Read()) {
                    data.ComprasPer2    = D(r, 0);
                    data.NComprasPer2   = D(r, 1);
                    data.IvaComprasPer2 = D(r, 2);
                }
            }

            // ── Gastos Periodo 1
            using (var cmd = conn.CreateCommand())
            {
                // Gastos = egresos que no son materia prima (no anulados)
                cmd.CommandText = SqlGastosOperativosPeriodo;
                cmd.Parameters.AddWithValue("$s", s1);
                cmd.Parameters.AddWithValue("$e", e1);
                data.GastosPer1 = Convert.ToDecimal(cmd.ExecuteScalar() ?? 0m);
            }

            // ── Gastos Periodo 2
            using (var cmd = conn.CreateCommand())
            {
                // Gastos = egresos que no son materia prima (no anulados)
                cmd.CommandText = SqlGastosOperativosPeriodo;
                cmd.Parameters.AddWithValue("$s", s2);
                cmd.Parameters.AddWithValue("$e", e2);
                data.GastosPer2 = Convert.ToDecimal(cmd.ExecuteScalar() ?? 0m);
            }

            // ── Nomina Periodo 1 (periodos cerrados cuya FechaDesde cae en el rango)
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = @"SELECT
                    COALESCE(SUM(d.DevengadoSueldo+d.DevengadoPiezas+d.DevengadoVentas+d.OtrosIngresos
                        -d.DeduccionIVSS-d.DeduccionRPVH-d.DescuentoFaltas-d.CuotaPrestamo-d.OtrasDeduccion),0),
                    COUNT(DISTINCT p.Id)
                    FROM DetallesNomina d
                    INNER JOIN PeriodosNomina p ON p.Id = d.IdPeriodo
                    WHERE p.Estado = 'Cerrado'
                      AND p.FechaDesde >= $s AND p.FechaDesde <= $e";
                cmd.Parameters.AddWithValue("$s", s1);
                cmd.Parameters.AddWithValue("$e", e1);
                using var r = cmd.ExecuteReader();
                if (r.Read()) {
                    data.NominaPer1   = D(r, 0);
                    data.NPeriodosPer1 = D(r, 1);
                }
            }

            // ── Nomina Periodo 2
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = @"SELECT
                    COALESCE(SUM(d.DevengadoSueldo+d.DevengadoPiezas+d.DevengadoVentas+d.OtrosIngresos
                        -d.DeduccionIVSS-d.DeduccionRPVH-d.DescuentoFaltas-d.CuotaPrestamo-d.OtrasDeduccion),0),
                    COUNT(DISTINCT p.Id)
                    FROM DetallesNomina d
                    INNER JOIN PeriodosNomina p ON p.Id = d.IdPeriodo
                    WHERE p.Estado = 'Cerrado'
                      AND p.FechaDesde >= $s AND p.FechaDesde <= $e";
                cmd.Parameters.AddWithValue("$s", s2);
                cmd.Parameters.AddWithValue("$e", e2);
                using var r = cmd.ExecuteReader();
                if (r.Read()) {
                    data.NominaPer2    = D(r, 0);
                    data.NPeriodosPer2 = D(r, 1);
                }
            }

            return data;
        }
    }
}
