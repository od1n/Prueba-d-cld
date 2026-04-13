using System;
using System.Collections.Generic;
using Microsoft.Data.Sqlite;

namespace SistemaGestion
{
    // -------------------------------------------------------------------------
    // MODELOS
    // -------------------------------------------------------------------------

    public class LineaMerma
    {
        public string  MateriaCod      { get; set; } = "";
        public string  MateriaDesc     { get; set; } = "";
        /// <summary>Consumo teorico = CantidadReq * CantidadReal (proporcional)</summary>
        public decimal ConsumoTeorico  { get; set; }
        /// <summary>Consumo real segun Kardex (entradas de tipo SALIDA por la OP)</summary>
        public decimal ConsumoReal     { get; set; }
        public decimal Merma           => ConsumoTeorico - ConsumoReal;
        public decimal PctMerma        => ConsumoTeorico > 0
            ? Math.Max(0, Merma / ConsumoTeorico) : 0;
        public string  Nivel           => PctMerma > 0.15m ? "CRITICO"
                                        : PctMerma > 0.10m ? "ALTO"
                                        : PctMerma > 0.05m ? "MEDIO"
                                        : "NORMAL";
    }

    public class ResumenMerma
    {
        public int      IdOrden       { get; set; }
        public string   NumOrden      { get; set; } = "";
        public string   ProductoCod   { get; set; } = "";
        public string   ProductoDesc  { get; set; } = "";
        public decimal  CantidadPlan  { get; set; }
        public decimal  CantidadReal  { get; set; }
        public DateTime FechaFin      { get; set; }
        public string   Responsable   { get; set; } = "";
        public List<LineaMerma> Lineas { get; set; } = new();

        public decimal MermaTotal     => Lineas.Sum(l => l.Merma);
        public decimal TeoricoTotal   => Lineas.Sum(l => l.ConsumoTeorico);
        public decimal PctMermaTotal  => TeoricoTotal > 0
            ? Math.Max(0, MermaTotal / TeoricoTotal) : 0;
    }

    // -------------------------------------------------------------------------
    // METODOS DE DATOS
    // -------------------------------------------------------------------------

    public static partial class DatabaseService
    {
        /// <summary>
        /// Calcula mermas de todas las ordenes Completadas en el periodo.
        /// Consumo teorico = CantidadReq * CantidadReal (proporcional al cierre).
        /// Consumo real = movimientos SALIDA en Kardex referenciados a la OP.
        /// </summary>
        public static List<ResumenMerma> ObtenerMermas(DateTime desde, DateTime hasta)
        {
            var lista = new List<ResumenMerma>();
            string sDesde = desde.ToString("yyyy-MM-dd");
            string sHasta = hasta.ToString("yyyy-MM-dd");

            using var conn = Conn();

            // 1. Obtener ordenes completadas en el periodo
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = @"
                    SELECT Id, NumOrden, ProductoCod, ProductoDesc,
                           CantidadPlan, CantidadReal, FechaFin, Responsable
                    FROM OrdenesProduccion
                    WHERE Estado = 'Completada'
                      AND FechaFin >= $desde
                      AND FechaFin <= $hasta
                    ORDER BY FechaFin DESC";
                cmd.Parameters.AddWithValue("$desde", sDesde);
                cmd.Parameters.AddWithValue("$hasta", sHasta);

                using var r = cmd.ExecuteReader();
                while (r.Read())
                {
                    lista.Add(new ResumenMerma
                    {
                        IdOrden      = r.GetInt32(0),
                        NumOrden     = S(r, 1),
                        ProductoCod  = S(r, 2),
                        ProductoDesc = S(r, 3),
                        CantidadPlan = D(r, 4),
                        CantidadReal = D(r, 5),
                        FechaFin     = DateTime.TryParse(S(r, 6), out var f) ? f : DateTime.Today,
                        Responsable  = S(r, 7)
                    });
                }
            }

            // 2. Para cada orden, calcular merma por materia prima
            foreach (var op in lista)
            {
                // Lineas de la orden (consumo planificado por unidad)
                var lineas = new List<(string cod, string desc, decimal reqXunidad)>();
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = @"
                        SELECT MateriaCod, MateriaDesc, CantidadReq
                        FROM LineasOrden
                        WHERE IdOrden = $id";
                    cmd.Parameters.AddWithValue("$id", op.IdOrden);
                    using var r = cmd.ExecuteReader();
                    while (r.Read())
                        lineas.Add((S(r, 0), S(r, 1), D(r, 2)));
                }

                foreach (var (cod, desc, reqXunidad) in lineas)
                {
                    // Consumo teorico proporcional a la cantidad real producida
                    decimal consumoTeorico = reqXunidad * op.CantidadReal;

                    // Consumo real: movimientos SALIDA en Kardex referenciados a esta OP
                    decimal consumoReal = 0;
                    using (var cmd = conn.CreateCommand())
                    {
                        cmd.CommandText = @"
                            SELECT COALESCE(SUM(Cantidad), 0)
                            FROM MovimientosInventario
                            WHERE CodigoProducto = $cod
                              AND TipoMovimiento  = 'SALIDA'
                              AND Referencia      = $ref";
                        cmd.Parameters.AddWithValue("$cod", cod);
                        cmd.Parameters.AddWithValue("$ref", op.NumOrden);
                        consumoReal = Convert.ToDecimal(cmd.ExecuteScalar() ?? 0m);
                    }

                    // Si no hay movimientos en Kardex, usar consumo teorico como real
                    // (significa que el Kardex no registra movimientos por OP)
                    if (consumoReal == 0)
                        consumoReal = consumoTeorico;

                    op.Lineas.Add(new LineaMerma
                    {
                        MateriaCod     = cod,
                        MateriaDesc    = desc,
                        ConsumoTeorico = consumoTeorico,
                        ConsumoReal    = consumoReal
                    });
                }
            }

            return lista;
        }
    }
}
