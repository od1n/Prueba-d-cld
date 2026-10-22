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
        /// <summary>Merma = lo que se gastó de más sobre lo teórico (kg).</summary>
        public decimal Merma           => ConsumoReal - ConsumoTeorico;
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

            // 3. Registro de producción por día (módulo actual, sin órdenes):
            //    consumo real = material registrado del día (MaterialProduccionDia);
            //    consumo teórico = piezas de material virgen × peso de cada producto,
            //    repartido entre los insumos en la misma proporción del consumo real.
            var dias = new List<(int id, string fecha, string tipo)>();
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = @"
                    SELECT Id, Fecha, COALESCE(TipoMaterial,'Virgen')
                    FROM RegistroProduccionDia
                    WHERE Fecha >= $desde AND Fecha <= $hasta
                    ORDER BY Fecha DESC";
                cmd.Parameters.AddWithValue("$desde", sDesde);
                cmd.Parameters.AddWithValue("$hasta", sHasta);
                try
                {
                    using var r = cmd.ExecuteReader();
                    while (r.Read()) dias.Add((r.GetInt32(0), S(r, 1), S(r, 2)));
                }
                catch { /* BD sin módulo de producción por día */ }
            }
            foreach (var (id, fecha, tipo) in dias)
            {
                decimal piezas = 0, kgTeorico = 0;
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = @"
                        SELECT COALESCE(SUM(d.Cantidad),0),
                               COALESCE(SUM(CASE WHEN COALESCE(d.CantPrimera,0)+COALESCE(d.CantSegunda,0) > 0
                                                 THEN COALESCE(d.CantPrimera,0)+COALESCE(d.CantSegunda,0)
                                                 WHEN COALESCE(d.CantReciclado,0) > 0 THEN 0
                                                 ELSE d.Cantidad END * COALESCE(i.PesoKg,0)),0)
                        FROM DetalleProduccionMolde d
                        LEFT JOIN Inventario i ON i.Codigo = d.CodigoProducto
                        WHERE d.IdRegistro = $id";
                    cmd.Parameters.AddWithValue("$id", id);
                    using var r = cmd.ExecuteReader();
                    if (r.Read()) { piezas = D(r, 0); kgTeorico = D(r, 1); }
                }
                var mats = new List<(string cod, string desc, decimal kg)>();
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = @"
                        SELECT m.CodigoInsumo, COALESCE(pp.Descripcion, m.CodigoInsumo), COALESCE(SUM(m.Cantidad),0)
                        FROM MaterialProduccionDia m
                        LEFT JOIN ProductosProveedor pp ON pp.CodigoInterno = m.CodigoInsumo
                        WHERE m.IdRegistro = $id
                        GROUP BY m.CodigoInsumo";
                    cmd.Parameters.AddWithValue("$id", id);
                    using var r = cmd.ExecuteReader();
                    while (r.Read()) mats.Add((S(r, 0), S(r, 1), D(r, 2)));
                }
                decimal kgReal = mats.Sum(m => m.kg);
                if (piezas <= 0 && kgReal <= 0) continue;
                var res = new ResumenMerma
                {
                    IdOrden      = -id,
                    NumOrden     = "PROD-" + fecha,
                    ProductoCod  = "",
                    ProductoDesc = "Producción del " + (DateTime.TryParse(fecha, out var fd) ? fd.ToString("dd/MM/yyyy") : fecha) + " (" + tipo + ")",
                    CantidadPlan = piezas,
                    CantidadReal = piezas,
                    FechaFin     = DateTime.TryParse(fecha, out var ff) ? ff : DateTime.Today,
                    Responsable  = ""
                };
                foreach (var (cod, desc, kg) in mats)
                    res.Lineas.Add(new LineaMerma
                    {
                        MateriaCod     = cod,
                        MateriaDesc    = desc,
                        ConsumoTeorico = kgReal > 0 ? Math.Round(kgTeorico * kg / kgReal, 2) : 0,
                        ConsumoReal    = kg
                    });
                lista.Add(res);
            }

            lista.Sort((a, b) => b.FechaFin.CompareTo(a.FechaFin));
            return lista;
        }
    }
}
