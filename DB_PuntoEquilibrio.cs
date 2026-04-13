using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Data.Sqlite;

namespace SistemaGestion
{
    // -------------------------------------------------------------------------
    // MODELOS
    // -------------------------------------------------------------------------

    public class DatoPE
    {
        public string Concepto { get; set; } = "";
        /// <summary>"Fijo" | "Variable" | "Ingreso"</summary>
        public string Tipo { get; set; } = "Fijo";
        public decimal MontoMensual { get; set; }
        /// <summary>"Auto" | "Manual"</summary>
        public string Origen { get; set; } = "Auto";
    }

    public class ResultadoPE
    {
        public decimal IngresosMensuales { get; set; }
        public decimal CostosVariables { get; set; }
        public decimal CostosFijos { get; set; }
        public decimal CostosTotales => CostosFijos + CostosVariables;
        public decimal MargenContribucion { get; set; }   // porcentaje 0-1
        public decimal PuntoEquilibrio { get; set; }       // en USD de ventas
        public decimal MargenSeguridad { get; set; }       // porcentaje 0-1
        public decimal UtilidadOperacional => IngresosMensuales - CostosTotales;
    }

    // -------------------------------------------------------------------------
    // METODOS DE DATOS (partial de DatabaseService)
    // -------------------------------------------------------------------------

    public static partial class DatabaseService
    {
        /// <summary>
        /// Lee datos automaticos de la BD para los ultimos <paramref name="meses"/> meses
        /// y los combina con conceptos manuales.
        /// </summary>
        public static List<DatoPE> ObtenerDatosPE(int meses, List<DatoPE> manuales)
        {
            var lista = new List<DatoPE>();
            var desde = DateTime.Today.AddMonths(-meses).ToString("yyyy-MM-dd");

            using var conn = Conn();
            conn.Open();

            // --- INGRESOS: ventas facturadas ---
            decimal totalVentas = 0;
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = @"
                    SELECT COALESCE(SUM(TotalF), 0)
                    FROM Facturas
                    WHERE Fecha >= $desde";
                cmd.Parameters.AddWithValue("$desde", desde);
                totalVentas = Convert.ToDecimal(cmd.ExecuteScalar() ?? 0m);
            }
            if (totalVentas > 0)
            {
                lista.Add(new DatoPE
                {
                    Concepto = "Ventas (promedio mensual)",
                    Tipo = "Ingreso",
                    MontoMensual = totalVentas / meses,
                    Origen = "Auto"
                });
            }

            // --- COSTOS VARIABLES: compras de materias primas ---
            decimal totalCompras = 0;
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = @"
                    SELECT COALESCE(SUM(TotalC), 0)
                    FROM Compras
                    WHERE FechaC >= $desde";
                cmd.Parameters.AddWithValue("$desde", desde);
                totalCompras = Convert.ToDecimal(cmd.ExecuteScalar() ?? 0m);
            }
            if (totalCompras > 0)
            {
                lista.Add(new DatoPE
                {
                    Concepto = "Compras / Materias Primas",
                    Tipo = "Variable",
                    MontoMensual = totalCompras / meses,
                    Origen = "Auto"
                });
            }

            // --- COSTOS FIJOS: gastos agrupados por categoria ---
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = @"
                    SELECT Categoria, COALESCE(SUM(Monto), 0) AS Total
                    FROM Gastos
                    WHERE Fecha >= $desde
                    GROUP BY Categoria
                    ORDER BY Total DESC";
                cmd.Parameters.AddWithValue("$desde", desde);
                using var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    string cat = S(reader, 0);
                    decimal monto = D(reader, 1);
                    if (monto <= 0) continue;
                    lista.Add(new DatoPE
                    {
                        Concepto = string.IsNullOrWhiteSpace(cat) ? "Gastos Generales" : $"Gastos: {cat}",
                        Tipo = "Fijo",
                        MontoMensual = monto / meses,
                        Origen = "Auto"
                    });
                }
            }

            // --- COSTOS FIJOS: nomina promedio (periodos cerrados) ---
            decimal totalNomina = 0;
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = @"
                    SELECT COALESCE(SUM(
                        d.DevengadoSueldo + d.DevengadoPiezas + d.DevengadoVentas + d.OtrosIngresos
                        - d.DeduccionIVSS - d.DeduccionRPVH - d.DescuentoFaltas - d.CuotaPrestamo - d.OtrasDeduccion
                    ), 0)
                    FROM DetallesNomina d
                    INNER JOIN PeriodosNomina p ON p.Id = d.IdPeriodo
                    WHERE p.Estado = 'Cerrado'
                      AND p.FechaDesde >= $desde";
                cmd.Parameters.AddWithValue("$desde", desde);
                totalNomina = Convert.ToDecimal(cmd.ExecuteScalar() ?? 0m);
            }
            if (totalNomina > 0)
            {
                lista.Add(new DatoPE
                {
                    Concepto = "Nomina (promedio mensual)",
                    Tipo = "Fijo",
                    MontoMensual = totalNomina / meses,
                    Origen = "Auto"
                });
            }

            // --- Agregar manuales (respetando los ya existentes) ---
            foreach (var m in manuales)
                lista.Add(m);

            return lista;
        }

        /// <summary>
        /// Calcula el punto de equilibrio a partir de la lista de datos clasificados.
        /// </summary>
        public static ResultadoPE CalcularPuntoEquilibrio(List<DatoPE> datos)
        {
            var r = new ResultadoPE();

            r.IngresosMensuales = datos
                .Where(d => d.Tipo == "Ingreso")
                .Sum(d => d.MontoMensual);

            r.CostosVariables = datos
                .Where(d => d.Tipo == "Variable")
                .Sum(d => d.MontoMensual);

            r.CostosFijos = datos
                .Where(d => d.Tipo == "Fijo")
                .Sum(d => d.MontoMensual);

            if (r.IngresosMensuales > 0)
            {
                r.MargenContribucion = (r.IngresosMensuales - r.CostosVariables) / r.IngresosMensuales;

                if (r.MargenContribucion > 0)
                    r.PuntoEquilibrio = r.CostosFijos / r.MargenContribucion;
                else
                    r.PuntoEquilibrio = 0;

                if (r.IngresosMensuales > 0)
                    r.MargenSeguridad = (r.IngresosMensuales - r.PuntoEquilibrio) / r.IngresosMensuales;
            }

            return r;
        }
    }
}
