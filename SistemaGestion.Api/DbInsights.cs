using Microsoft.Data.Sqlite;

namespace SistemaGestion.Api
{
    // ── Tipos de salida (se serializan a camelCase por defecto en Minimal API) ──
    public record MesVenta(string Mes, decimal VentasTotales, int NumFacturas, double? DeltaPct, bool EsEvento);
    public record SerieResult(string Desde, string Hasta, double UmbralPct, List<MesVenta> Meses);
    public record ProductoDelta(string Codigo, string Descripcion, decimal VentaAnterior, decimal VentaActual, decimal Delta, double ContribPct);
    public record ClienteVenta(string Codigo, string Nombre, decimal VentaTotalMes);
    public record DesgloseResult(string Mes, decimal ValorAnterior, decimal ValorActual, decimal DeltaAbs, double DeltaPct,
                                 List<ProductoDelta> TopProductos, List<ClienteVenta> TopClientes, string Categoria);

    /// <summary>
    /// Agente de Insights + RCA — Capas 1 a 3 (cálculo determinístico, SIN IA).
    ///   Capa 1: métrica (ventas por mes).
    ///   Capa 2: detección de anomalías (umbral calibrado al percentil 80 del propio negocio).
    ///   Capa 3: descomposición por driver (producto / cliente / categoría) de un mes-evento.
    /// La Capa 4 (narración con Claude) vive en InsightsEndpoints; acá NO se llama a ninguna IA.
    /// Todo es de solo lectura sobre Facturas / DetallesFactura / Clientes / Inventario.
    /// </summary>
    public static class DbInsights
    {
        // ── Detección de la columna de precio de venta ─────────────────────
        //   La columna donde vive el precio real cambia según la base:
        //     - solo PrecioF (base desplegada hoy)         → PrecioF
        //     - PrecioF + PrecioUnit (base migrada v27)     → COALESCE(NULLIF(PrecioF,0), PrecioUnit, i.Precio)
        //     - solo PrecioUnit (instalación nueva)         → COALESCE(NULLIF(PrecioUnit,0), i.Precio)
        //   Nunca se usa i.Precio (catálogo actual) como precio histórico salvo último recurso.
        private static string? _precioExpr;
        private static string PrecioExpr()
        {
            if (_precioExpr != null) return _precioExpr;
            var cols = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            using (var c = Db.Conn())
            {
                var cmd = c.CreateCommand();
                cmd.CommandText = "PRAGMA table_info(DetallesFactura)";
                using var r = cmd.ExecuteReader();
                while (r.Read()) cols.Add(r.GetString(1)); // col 1 = nombre
            }
            bool f = cols.Contains("PrecioF");
            bool u = cols.Contains("PrecioUnit");
            if (f && u)      _precioExpr = "COALESCE(NULLIF(df.PrecioF,0), df.PrecioUnit, i.Precio)";
            else if (f)      _precioExpr = "df.PrecioF";
            else if (u)      _precioExpr = "COALESCE(NULLIF(df.PrecioUnit,0), i.Precio)";
            else             _precioExpr = "i.Precio";
            Console.WriteLine($"[INSIGHTS] DetallesFactura: PrecioF={f} PrecioUnit={u} → precio={_precioExpr}");
            return _precioExpr;
        }

        // ═══════════════════════════════════════════════════════════════════
        //  CAPA 1 + CAPA 2 — serie mensual con marca de evento
        // ═══════════════════════════════════════════════════════════════════
        public static SerieResult Serie(string desde, string hasta, double percentil = 0.80)
        {
            var meses = new List<(string Mes, decimal Ventas, int N)>();
            using (var c = Db.Conn())
            {
                var cmd = c.CreateCommand();
                cmd.CommandText = @"
                    SELECT substr(Fecha,1,7) AS Mes, SUM(TotalF) AS Ventas, COUNT(*) AS N
                    FROM Facturas
                    WHERE Anulada = 0 AND Fecha BETWEEN $d AND $h
                    GROUP BY Mes ORDER BY Mes";
                cmd.Parameters.AddWithValue("$d", desde);
                cmd.Parameters.AddWithValue("$h", hasta);
                using var r = cmd.ExecuteReader();
                while (r.Read())
                    meses.Add((Db.S(r, 0), Db.D(r, 1), Db.I2(r, 2)));
            }

            // Capa 2: variación mes a mes y umbral calibrado (percentil de |Δ%|)
            var variaciones = new List<double>();
            for (int i = 1; i < meses.Count; i++)
            {
                double prev = (double)meses[i - 1].Ventas;
                if (prev == 0) continue;
                double dpct = ((double)meses[i].Ventas - prev) / prev;
                variaciones.Add(Math.Abs(dpct));
            }
            double umbral = Percentil(variaciones, percentil);

            var salida = new List<MesVenta>();
            for (int i = 0; i < meses.Count; i++)
            {
                double? dpct = null;
                bool evento = false;
                if (i > 0)
                {
                    double prev = (double)meses[i - 1].Ventas;
                    if (prev != 0)
                    {
                        double d = ((double)meses[i].Ventas - prev) / prev;
                        dpct = d;
                        evento = Math.Abs(d) >= umbral;
                    }
                }
                salida.Add(new MesVenta(meses[i].Mes, meses[i].Ventas, meses[i].N, dpct, evento));
            }
            return new SerieResult(desde, hasta, umbral, salida);
        }

        // Percentil con interpolación lineal (mismo criterio que pandas .quantile por defecto)
        private static double Percentil(List<double> datos, double q)
        {
            if (datos == null || datos.Count == 0) return 0;
            var a = new List<double>(datos);
            a.Sort();
            if (a.Count == 1) return a[0];
            double pos = q * (a.Count - 1);
            int lo = (int)Math.Floor(pos);
            int hi = (int)Math.Ceiling(pos);
            double frac = pos - lo;
            return a[lo] + (a[hi] - a[lo]) * frac;
        }

        // ═══════════════════════════════════════════════════════════════════
        //  CAPA 3 — descomposición por driver de un mes-evento
        // ═══════════════════════════════════════════════════════════════════
        public static DesgloseResult Desglose(string mes)
        {
            var (ini, fin)         = MesARango(mes);
            var (iniPrev, finPrev) = MesARango(MesAnterior(mes));

            decimal valorActual   = TotalMes(ini, fin);
            decimal valorAnterior = TotalMes(iniPrev, finPrev);
            decimal deltaAbs      = valorActual - valorAnterior;
            double  deltaPct      = valorAnterior != 0 ? (double)(deltaAbs / valorAnterior) : 0;

            var prodActual = VentaPorProducto(ini, fin);
            var prodPrev   = VentaPorProducto(iniPrev, finPrev);

            // Unión de códigos de ambos meses → delta por producto
            var codigos = new HashSet<string>(prodActual.Keys);
            codigos.UnionWith(prodPrev.Keys);
            var productos = new List<ProductoDelta>();
            foreach (var cod in codigos)
            {
                var (descA, vA) = prodActual.TryGetValue(cod, out var pa) ? pa : ("", 0m);
                var (descP, vP) = prodPrev.TryGetValue(cod, out var pp) ? pp : ("", 0m);
                string desc = !string.IsNullOrEmpty(descA) ? descA : descP;
                decimal delta = vA - vP;
                double contrib = deltaAbs != 0 ? (double)(delta / deltaAbs) : 0;
                productos.Add(new ProductoDelta(cod, desc, vP, vA, delta, contrib));
            }
            var topProductos = productos
                .OrderByDescending(p => Math.Abs(p.Delta))
                .Take(5).ToList();

            var topClientes = ClientesDelMes(ini, fin).Take(3).ToList();
            string categoria = CategoriaDelMes(ini, fin);

            return new DesgloseResult(mes, valorAnterior, valorActual, deltaAbs, deltaPct,
                                      topProductos, topClientes, categoria);
        }

        private static decimal TotalMes(string ini, string fin)
        {
            using var c = Db.Conn();
            var cmd = c.CreateCommand();
            cmd.CommandText = @"SELECT COALESCE(SUM(TotalF),0) FROM Facturas
                                WHERE Anulada = 0 AND Fecha >= $i AND Fecha < $f";
            cmd.Parameters.AddWithValue("$i", ini);
            cmd.Parameters.AddWithValue("$f", fin);
            using var r = cmd.ExecuteReader();
            r.Read();
            return Db.D(r, 0);
        }

        private static Dictionary<string, (string Desc, decimal Venta)> VentaPorProducto(string ini, string fin)
        {
            var d = new Dictionary<string, (string, decimal)>();
            using var c = Db.Conn();
            var cmd = c.CreateCommand();
            cmd.CommandText = $@"
                SELECT df.Codigo, i.Descripcion, SUM(df.Cantidad * {PrecioExpr()}) AS Venta
                FROM DetallesFactura df
                JOIN Facturas f   ON f.NumeroF = df.IdFactura
                JOIN Inventario i ON i.Codigo  = df.Codigo
                WHERE f.Anulada = 0 AND f.Fecha >= $i AND f.Fecha < $f
                GROUP BY df.Codigo, i.Descripcion";
            cmd.Parameters.AddWithValue("$i", ini);
            cmd.Parameters.AddWithValue("$f", fin);
            using var r = cmd.ExecuteReader();
            while (r.Read())
                d[Db.S(r, 0)] = (Db.S(r, 1), Db.D(r, 2));
            return d;
        }

        private static List<ClienteVenta> ClientesDelMes(string ini, string fin)
        {
            var lista = new List<ClienteVenta>();
            using var c = Db.Conn();
            var cmd = c.CreateCommand();
            cmd.CommandText = @"
                SELECT f.Codigo, c.Descripcion, SUM(f.TotalF) AS Venta
                FROM Facturas f
                JOIN Clientes c ON c.Codigo = f.Codigo
                WHERE f.Anulada = 0 AND f.Fecha >= $i AND f.Fecha < $f
                GROUP BY f.Codigo, c.Descripcion
                ORDER BY Venta DESC";
            cmd.Parameters.AddWithValue("$i", ini);
            cmd.Parameters.AddWithValue("$f", fin);
            using var r = cmd.ExecuteReader();
            while (r.Read())
                lista.Add(new ClienteVenta(Db.S(r, 0), Db.S(r, 1), Db.D(r, 2)));
            return lista;
        }

        // Devuelve el desglose por Departamento; si solo hay una categoría (o todo "General"),
        // se marca como no informativo — hallazgo real: Inventario.Departamento no discrimina hoy.
        private static string CategoriaDelMes(string ini, string fin)
        {
            var cats = new List<(string Cat, decimal Venta)>();
            using (var c = Db.Conn())
            {
                var cmd = c.CreateCommand();
                cmd.CommandText = $@"
                    SELECT COALESCE(NULLIF(i.Departamento,''), '(sin departamento)') AS Categoria,
                           SUM(df.Cantidad * {PrecioExpr()}) AS Venta
                    FROM DetallesFactura df
                    JOIN Facturas f   ON f.NumeroF = df.IdFactura
                    JOIN Inventario i ON i.Codigo  = df.Codigo
                    WHERE f.Anulada = 0 AND f.Fecha >= $i AND f.Fecha < $f
                    GROUP BY Categoria ORDER BY Venta DESC";
                cmd.Parameters.AddWithValue("$i", ini);
                cmd.Parameters.AddWithValue("$f", fin);
                using var r = cmd.ExecuteReader();
                while (r.Read()) cats.Add((Db.S(r, 0), Db.D(r, 1)));
            }
            if (cats.Count <= 1)
                return "no informativo (todos los productos bajo una sola categoría)";
            return string.Join(", ", cats.Take(5).Select(x => $"{x.Cat}: {x.Venta:N0}"));
        }

        // ── Helpers de fechas (mes = "YYYY-MM") ────────────────────────────
        private static (string ini, string fin) MesARango(string mes)
        {
            var partes = mes.Split('-');
            int anio = int.Parse(partes[0]);
            int m    = int.Parse(partes[1]);
            string ini = $"{anio:D4}-{m:D2}-01";
            string fin = m == 12 ? $"{anio + 1:D4}-01-01" : $"{anio:D4}-{m + 1:D2}-01";
            return (ini, fin);
        }

        private static string MesAnterior(string mes)
        {
            var partes = mes.Split('-');
            int anio = int.Parse(partes[0]);
            int m    = int.Parse(partes[1]);
            return m == 1 ? $"{anio - 1:D4}-12" : $"{anio:D4}-{m - 1:D2}";
        }
    }
}
