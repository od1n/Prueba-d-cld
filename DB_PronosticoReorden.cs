using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Data.Sqlite;

namespace SistemaGestion
{
    // ══════════════════════════════════════════════════════════════
    //  MODELO DE PRONÓSTICO DE REORDEN
    // ══════════════════════════════════════════════════════════════
    public class PronosticoReorden
    {
        public string  CodigoInsumo       { get; set; }
        public string  Descripcion        { get; set; }
        public string  Unidad             { get; set; }
        public decimal StockActual        { get; set; }
        public decimal StockMinimo        { get; set; }
        public decimal ConsumoPromDiario  { get; set; }   // últimos N días
        public int     DiasAnalisis       { get; set; }   // ventana usada
        public decimal DiasCobertura      { get; set; }   // stock / consumo diario
        public DateTime? FechaAgotamiento { get; set; }   // hoy + diasCobertura
        public decimal CantidadSugerida   { get; set; }   // para cubrir LeadTimeDias
        public decimal CostoEstimado      { get; set; }   // cantSugerida * precioUltCompra
        public decimal PrecioUltCompra    { get; set; }
        public string  Estado             { get; set; }   // URGENTE / PRONTO / OK
    }

    public static partial class DatabaseService
    {
        /// <summary>
        /// Calcula pronóstico de reorden para todos los insumos (materias primas)
        /// basándose en el consumo histórico del Kardex A.
        /// </summary>
        /// <param name="diasAnalisis">Ventana de días hacia atrás para calcular promedio</param>
        /// <param name="leadTimeDias">Días que tarda en llegar una compra</param>
        /// <param name="diasCoberturaMeta">Días de stock deseado después de recibir compra</param>
        public static List<PronosticoReorden> CalcularPronosticoReorden(
            int diasAnalisis = 90, int leadTimeDias = 7, int diasCoberturaMeta = 30)
        {
            var resultado = new List<PronosticoReorden>();
            using var c = Conn();

            // 1. Insumos activos de materia prima. Viven en ProductosProveedor
            //    (INS-xxx), no en Inventario (que es producto terminado).
            var cmd = c.CreateCommand();
            cmd.CommandText = @"
                SELECT pp.CodigoInterno, pp.Descripcion, COALESCE(pp.Unidad,'kg'),
                       COALESCE(pp.StockActual,0), COALESCE(pp.StockMinimo,0),
                       COALESCE(NULLIF(pp.CostoPromedio,0), pp.PrecioPromedio, pp.PrecioUltCompra, 0)
                FROM ProductosProveedor pp
                WHERE COALESCE(pp.Activo,1) = 1
                  AND (COALESCE(pp.TipoInsumo,'Materia prima') = 'Materia prima'
                       OR pp.CodigoInterno IN (SELECT DISTINCT CodigoProducto FROM MovimientosInventario
                                               WHERE TipoKardex = 'MATERIA_PRIMA'))
                ORDER BY pp.CodigoInterno";
            using var r = cmd.ExecuteReader();
            var insumos = new List<(string cod, string desc, string unidad,
                                    decimal stock, decimal min, decimal costo)>();
            while (r.Read())
                insumos.Add((S(r,0), S(r,1), S(r,2), D(r,3), D(r,4), D(r,5)));
            r.Close();

            // 2. Para cada insumo, calcular consumo promedio diario
            string fechaDesde = DateTime.Today.AddDays(-diasAnalisis).ToString("yyyy-MM-dd");
            foreach (var (cod, desc, unidad, stock, min, costo) in insumos)
            {
                var cmdC = c.CreateCommand();
                cmdC.CommandText = @"
                    SELECT COALESCE(SUM(Cantidad), 0)
                    FROM MovimientosInventario
                    WHERE CodigoProducto = $cod
                      AND TipoKardex = 'MATERIA_PRIMA'
                      AND TipoMovimiento = 'SALIDA'
                      AND COALESCE(Referencia,'') NOT LIKE 'AJ-%'
                      AND FechaHora >= $desde";
                cmdC.Parameters.AddWithValue("$cod",   cod);
                cmdC.Parameters.AddWithValue("$desde", fechaDesde);
                decimal consumoTotal = D(cmdC.ExecuteScalar());
                decimal consumoDiario = diasAnalisis > 0
                    ? consumoTotal / diasAnalisis : 0;

                // 3. Calcular cobertura y sugerencia
                decimal diasCob = consumoDiario > 0
                    ? stock / consumoDiario : 9999;
                DateTime? fechaAgot = consumoDiario > 0
                    ? DateTime.Today.AddDays((double)diasCob)
                    : (DateTime?)null;

                // Cantidad sugerida: cubrir leadTime + diasCoberturaMeta
                decimal necesidad = consumoDiario * (leadTimeDias + diasCoberturaMeta);
                decimal cantSugerida = necesidad > stock
                    ? necesidad - stock : 0;

                // Buscar precio de última compra
                var cmdP = c.CreateCommand();
                cmdP.CommandText = @"
                    SELECT COALESCE(CostoUnitario, 0)
                    FROM MovimientosInventario
                    WHERE CodigoProducto = $cod
                      AND TipoKardex = 'MATERIA_PRIMA'
                      AND TipoMovimiento = 'ENTRADA'
                      AND COALESCE(Referencia,'') LIKE 'Gasto#%'
                    ORDER BY FechaHora DESC, IdMovimiento DESC LIMIT 1";
                cmdP.Parameters.AddWithValue("$cod", cod);
                decimal precioUlt = D(cmdP.ExecuteScalar());
                if (precioUlt == 0) precioUlt = costo;

                string estado = diasCob <= leadTimeDias ? "URGENTE"
                    : diasCob <= (leadTimeDias + 7) ? "PRONTO"
                    : "OK";

                resultado.Add(new PronosticoReorden {
                    CodigoInsumo      = cod,
                    Descripcion       = desc,
                    Unidad            = unidad,
                    StockActual       = stock,
                    StockMinimo       = min,
                    ConsumoPromDiario = Math.Round(consumoDiario, 4),
                    DiasAnalisis      = diasAnalisis,
                    DiasCobertura     = Math.Round(diasCob, 1),
                    FechaAgotamiento  = fechaAgot,
                    CantidadSugerida  = Math.Round(cantSugerida, 2),
                    CostoEstimado     = Math.Round(cantSugerida * precioUlt, 2),
                    PrecioUltCompra   = precioUlt,
                    Estado            = estado
                });
            }

            return resultado.OrderBy(p => p.DiasCobertura).ToList();
        }
    }
}
