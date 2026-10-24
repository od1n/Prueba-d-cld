using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Data.Sqlite;

namespace SistemaGestion
{
    /// <summary>Una línea de venta (producto dentro de una factura) con su costo.</summary>
    public class LineaAnalisisVenta
    {
        public string   NumeroF     { get; set; } = "";
        public DateTime Fecha       { get; set; }
        public string   TipoDoc     { get; set; } = "";
        public string   CodCliente  { get; set; } = "";
        public string   Cliente     { get; set; } = "";
        public string   Codigo      { get; set; } = "";
        public string   Descripcion { get; set; } = "";
        public decimal  Cantidad    { get; set; }
        public decimal  Precio      { get; set; }   // USD por unidad
        public decimal  CostoUnit   { get; set; }   // costo promedio del Kardex al vender
        public bool     CostoDeKardex { get; set; } // false = se usó el costo actual del producto
        public decimal  Venta       => Cantidad * Precio;
        public decimal  Costo       => Cantidad * CostoUnit;
        public decimal  Utilidad    => Venta - Costo;
    }

    /// <summary>Estadísticas de venta de un producto (o del total) en un período.</summary>
    public class EstadisticaVentaProducto
    {
        public string   Codigo         { get; set; } = "";
        public string   Descripcion    { get; set; } = "";
        public decimal  Unidades       { get; set; }
        public int      Facturas       { get; set; }
        public int      Clientes       { get; set; }
        public decimal  Ventas         { get; set; }
        public decimal  PctVentas      { get; set; }   // participación en el total del reporte
        public decimal  PrecioPromPond { get; set; }   // ventas / unidades
        public decimal  PrecioPromSimple { get; set; } // promedio de los precios de cada línea
        public decimal  PrecioMin      { get; set; }
        public decimal  PrecioMax      { get; set; }
        public decimal  PrecioMediana  { get; set; }
        public decimal  PrecioModa     { get; set; }
        public decimal  DesvEstandar   { get; set; }
        public decimal  CoefVariacion  { get; set; }   // desviación / promedio, en %
        public decimal  UltimoPrecio   { get; set; }
        public decimal  CostoProm      { get; set; }   // costo promedio ponderado por unidades
        public decimal  CostoTotal     { get; set; }
        public decimal  Utilidad       { get; set; }
        public decimal  UtilidadUnidad { get; set; }
        public decimal  MargenPct      { get; set; }   // utilidad / ventas
        public decimal  MarkupPct      { get; set; }   // utilidad / costo
        public decimal  UnidadesPorFactura { get; set; }
        public DateTime PrimeraVenta   { get; set; }
        public DateTime UltimaVenta    { get; set; }
        public int      LineasSinCostoKardex { get; set; }
    }

    public static partial class DatabaseService
    {
        /// <summary>
        /// Líneas de venta de los productos indicados en el período (facturas no
        /// anuladas). tipoDoc: "TODAS", "IVA" (solo facturas legales) o "NOTA"
        /// (solo notas de entrega). El costo de cada línea es el costo promedio que
        /// registró el Kardex de producto en la salida de esa factura; si no hay
        /// salida en el Kardex, se usa el costo actual del producto.
        /// </summary>
        public static List<LineaAnalisisVenta> ObtenerLineasAnalisisVentas(
            List<string> codigos, DateTime desde, DateTime hasta, string tipoDoc = "TODAS")
        {
            var list = new List<LineaAnalisisVenta>();
            if (codigos == null || codigos.Count == 0) return list;

            var inParams = new List<string>();
            for (int i = 0; i < codigos.Count; i++) inParams.Add("$c" + i);
            string filtroTipo = tipoDoc switch
            {
                "IVA"  => "AND COALESCE(f.TipoDocumento,'FacturaConIVA') = 'FacturaConIVA' ",
                "NOTA" => "AND COALESCE(f.TipoDocumento,'FacturaConIVA') = 'NotaEntrega' ",
                _      => ""
            };

            using var c = Conn();
            var cmd = c.CreateCommand();
            cmd.CommandText = @"
                SELECT f.NumeroF, f.Fecha, COALESCE(f.TipoDocumento,'FacturaConIVA'),
                       COALESCE(f.Codigo,''), COALESCE(cl.Descripcion, f.Codigo, ''),
                       df.Codigo, COALESCE(i.Descripcion, df.Codigo),
                       df.Cantidad,
                       COALESCE(NULLIF(df.PrecioUnit,0), i.Precio, 0),
                       (SELECT m.CostoUnitario FROM MovimientosInventario m
                         WHERE m.CodigoProducto = df.Codigo
                           AND m.TipoMovimiento = 'SALIDA'
                           AND COALESCE(m.TipoKardex,'PRODUCTO') = 'PRODUCTO'
                           AND (m.Referencia = f.NumeroF
                                OR m.Concepto = 'Venta Factura ' || f.NumeroF
                                OR m.Concepto = 'Factura ' || f.NumeroF)
                         ORDER BY m.IdMovimiento DESC LIMIT 1),
                       COALESCE(i.CostoActual, 0)
                FROM Facturas f
                INNER JOIN DetallesFactura df ON df.IdFactura = f.rowid
                LEFT  JOIN Inventario      i  ON i.Codigo     = df.Codigo
                LEFT  JOIN Clientes        cl ON cl.Codigo    = f.Codigo
                WHERE f.Fecha >= $desde AND f.Fecha <= $hasta
                  AND COALESCE(f.Anulada,0) = 0
                  AND df.Cantidad > 0
                  AND df.Codigo IN (" + string.Join(",", inParams) + @")
                  " + filtroTipo + @"
                ORDER BY f.Fecha, f.NumeroF";
            cmd.Parameters.AddWithValue("$desde", desde.ToString("yyyy-MM-dd"));
            cmd.Parameters.AddWithValue("$hasta", hasta.ToString("yyyy-MM-dd"));
            for (int i = 0; i < codigos.Count; i++)
                cmd.Parameters.AddWithValue("$c" + i, codigos[i]);

            using var r = cmd.ExecuteReader();
            while (r.Read())
            {
                bool hayKardex = !r.IsDBNull(9) && D(r, 9) > 0;
                list.Add(new LineaAnalisisVenta
                {
                    NumeroF       = S(r, 0),
                    Fecha         = DateTime.TryParse(S(r, 1), out var f) ? f : DateTime.MinValue,
                    TipoDoc       = S(r, 2) == "NotaEntrega" ? "Nota de entrega" : "Factura",
                    CodCliente    = S(r, 3),
                    Cliente       = S(r, 4),
                    Codigo        = S(r, 5),
                    Descripcion   = S(r, 6),
                    Cantidad      = D(r, 7),
                    Precio        = D(r, 8),
                    CostoUnit     = hayKardex ? D(r, 9) : D(r, 10),
                    CostoDeKardex = hayKardex
                });
            }
            return list;
        }

        /// <summary>Calcula las estadísticas de un grupo de líneas (un producto o el total).</summary>
        public static EstadisticaVentaProducto CalcularEstadisticaVentas(
            IEnumerable<LineaAnalisisVenta> lineasEntrada, string codigo, string descripcion,
            decimal ventasTotalesReporte)
        {
            var lineas = lineasEntrada.ToList();
            var e = new EstadisticaVentaProducto { Codigo = codigo, Descripcion = descripcion };
            if (lineas.Count == 0) return e;

            e.Unidades   = lineas.Sum(l => l.Cantidad);
            e.Facturas   = lineas.Select(l => l.NumeroF).Distinct().Count();
            e.Clientes   = lineas.Select(l => l.CodCliente).Where(x => x != "").Distinct().Count();
            e.Ventas     = lineas.Sum(l => l.Venta);
            e.CostoTotal = lineas.Sum(l => l.Costo);
            e.Utilidad   = e.Ventas - e.CostoTotal;
            e.PctVentas  = ventasTotalesReporte > 0 ? e.Ventas / ventasTotalesReporte * 100 : 0;

            e.PrecioPromPond = e.Unidades != 0 ? e.Ventas / e.Unidades : 0;
            e.CostoProm      = e.Unidades != 0 ? e.CostoTotal / e.Unidades : 0;
            e.UtilidadUnidad = e.PrecioPromPond - e.CostoProm;
            e.MargenPct      = e.Ventas != 0 ? e.Utilidad / e.Ventas * 100 : 0;
            e.MarkupPct      = e.CostoTotal != 0 ? e.Utilidad / e.CostoTotal * 100 : 0;
            e.UnidadesPorFactura = e.Facturas > 0 ? e.Unidades / e.Facturas : 0;

            // Estadísticas de precio: cada línea de factura es una observación.
            var precios = lineas.Select(l => l.Precio).OrderBy(p => p).ToList();
            int n = precios.Count;
            e.PrecioMin        = precios[0];
            e.PrecioMax        = precios[n - 1];
            e.PrecioPromSimple = precios.Average();
            e.PrecioMediana    = n % 2 == 1 ? precios[n / 2] : (precios[n / 2 - 1] + precios[n / 2]) / 2;
            e.PrecioModa       = precios.GroupBy(p => Math.Round(p, 2))
                                        .OrderByDescending(g => g.Count()).ThenBy(g => g.Key)
                                        .First().Key;
            if (n > 1)
            {
                double prom = (double)e.PrecioPromSimple;
                double var  = precios.Sum(p => Math.Pow((double)p - prom, 2)) / (n - 1);
                e.DesvEstandar = (decimal)Math.Sqrt(var);
            }
            e.CoefVariacion = e.PrecioPromSimple != 0 ? e.DesvEstandar / e.PrecioPromSimple * 100 : 0;

            var ordenFecha = lineas.OrderBy(l => l.Fecha).ThenBy(l => l.NumeroF).ToList();
            e.PrimeraVenta = ordenFecha.First().Fecha;
            e.UltimaVenta  = ordenFecha.Last().Fecha;
            e.UltimoPrecio = ordenFecha.Last().Precio;
            e.LineasSinCostoKardex = lineas.Count(l => !l.CostoDeKardex);
            return e;
        }
    }
}
