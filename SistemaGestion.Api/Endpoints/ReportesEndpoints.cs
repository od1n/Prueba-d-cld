namespace SistemaGestion.Api.Endpoints
{
    public static class ReportesEndpoints
    {
        public static void MapReportesEndpoints(this WebApplication app)
        {
            // ── Ventas / Facturas ─────────────────────────────────────────
            app.MapGet("/api/reportes/ventas", (string? desde, string? hasta) =>
            {
                string d = desde ?? DateTime.Today.AddMonths(-1).ToString("yyyy-MM-dd");
                string h = hasta ?? DateTime.Today.ToString("yyyy-MM-dd");
                using var c = Db.Conn();

                // Totales del período
                var cmd = c.CreateCommand();
                cmd.CommandText = @"SELECT COUNT(*),SUM(TotalF),SUM(Abono),SUM(IvaF)
                    FROM Facturas
                    WHERE Fecha BETWEEN $d AND $h
                      AND (Observaciones IS NULL OR Observaciones NOT LIKE 'ANULADA%')";
                cmd.Parameters.AddWithValue("$d",d); cmd.Parameters.AddWithValue("$h",h);
                using var r = cmd.ExecuteReader();
                r.Read();
                var resumen = new { totalFacturas=(int)Db.I2(r,0), totalVentas=Db.D(r,1),
                    totalCobrado=Db.D(r,2), totalIva=Db.D(r,3),
                    saldoPendiente=Db.D(r,1)-Db.D(r,2) };
                r.Close();

                // Detalle por cliente
                var cmd2 = c.CreateCommand();
                cmd2.CommandText = @"SELECT Codigo,COUNT(*) cant,SUM(TotalF) total,SUM(Abono) cobrado
                    FROM Facturas
                    WHERE Fecha BETWEEN $d AND $h
                      AND (Observaciones IS NULL OR Observaciones NOT LIKE 'ANULADA%')
                    GROUP BY Codigo ORDER BY total DESC LIMIT 30";
                cmd2.Parameters.AddWithValue("$d",d); cmd2.Parameters.AddWithValue("$h",h);
                var porCliente = new List<object>();
                using var r2 = cmd2.ExecuteReader();
                while (r2.Read())
                    porCliente.Add(new { cliente=Db.S(r2,0), cantFacturas=Db.I2(r2,1),
                        total=Db.D(r2,2), cobrado=Db.D(r2,3), pendiente=Db.D(r2,2)-Db.D(r2,3) });

                // Facturas recientes del período
                var cmd3 = c.CreateCommand();
                cmd3.CommandText = @"SELECT NumeroF,Fecha,Codigo,TotalF,Abono,FormaDePago
                    FROM Facturas WHERE Fecha BETWEEN $d AND $h
                      AND (Observaciones IS NULL OR Observaciones NOT LIKE 'ANULADA%')
                    ORDER BY Fecha DESC,NumeroF DESC LIMIT 50";
                cmd3.Parameters.AddWithValue("$d",d); cmd3.Parameters.AddWithValue("$h",h);
                var facturas = new List<object>();
                using var r3 = cmd3.ExecuteReader();
                while (r3.Read()) {
                    decimal tot=Db.D(r3,3), ab=Db.D(r3,4);
                    facturas.Add(new { numeroF=Db.S(r3,0), fecha=Db.S(r3,1), cliente=Db.S(r3,2),
                        total=tot, abono=ab, saldo=tot-ab, formaDePago=Db.S(r3,5), pagada=ab>=tot });
                }

                return Results.Ok(new { desde=d, hasta=h, resumen, porCliente, facturas });
            }).RequireAuthorization();

            // ── Producción ────────────────────────────────────────────────
            app.MapGet("/api/reportes/produccion", (string? desde, string? hasta) =>
            {
                string d = desde ?? DateTime.Today.AddMonths(-1).ToString("yyyy-MM-dd");
                string h = hasta ?? DateTime.Today.ToString("yyyy-MM-dd");
                using var c = Db.Conn();

                // Resumen total
                var cmd = c.CreateCommand();
                cmd.CommandText = @"SELECT COUNT(DISTINCT Fecha),
                    SUM(CantPrimeraCalidad+CantSegundaCalidad+CantDefectuosas),
                    SUM(CantPrimeraCalidad),SUM(CantSegundaCalidad),SUM(CantDefectuosas)
                    FROM RegistroProduccionDia WHERE Fecha BETWEEN $d AND $h";
                cmd.Parameters.AddWithValue("$d",d); cmd.Parameters.AddWithValue("$h",h);
                using var r = cmd.ExecuteReader();
                r.Read();
                var resumen = new { diasRegistrados=Db.I2(r,0),
                    totalProducido=Db.D(r,1), primeraCalidad=Db.D(r,2),
                    segundaCalidad=Db.D(r,3), defectuosas=Db.D(r,4) };
                r.Close();

                // Por molde
                var cmd2 = c.CreateCommand();
                cmd2.CommandText = @"SELECT CodigoMolde,
                    SUM(CantPrimeraCalidad+CantSegundaCalidad+CantDefectuosas) total,
                    SUM(CantPrimeraCalidad) p1, SUM(CantDefectuosas) def
                    FROM RegistroProduccionDia
                    WHERE Fecha BETWEEN $d AND $h
                    GROUP BY CodigoMolde ORDER BY total DESC";
                cmd2.Parameters.AddWithValue("$d",d); cmd2.Parameters.AddWithValue("$h",h);
                var porMolde = new List<object>();
                using var r2 = cmd2.ExecuteReader();
                while (r2.Read())
                    porMolde.Add(new { molde=Db.S(r2,0), total=Db.D(r2,1),
                        primeraCalidad=Db.D(r2,2), defectuosas=Db.D(r2,3) });

                // Últimos registros
                var cmd3 = c.CreateCommand();
                cmd3.CommandText = @"SELECT Fecha,CodigoMolde,Material,Operario,
                    CantPrimeraCalidad,CantSegundaCalidad,CantDefectuosas
                    FROM RegistroProduccionDia
                    WHERE Fecha BETWEEN $d AND $h
                    ORDER BY Fecha DESC, Id DESC LIMIT 60";
                cmd3.Parameters.AddWithValue("$d",d); cmd3.Parameters.AddWithValue("$h",h);
                var registros = new List<object>();
                using var r3 = cmd3.ExecuteReader();
                while (r3.Read())
                    registros.Add(new { fecha=Db.S(r3,0), molde=Db.S(r3,1), material=Db.S(r3,2),
                        operario=Db.S(r3,3), p1=Db.D(r3,4), p2=Db.D(r3,5), def=Db.D(r3,6) });

                return Results.Ok(new { desde=d, hasta=h, resumen, porMolde, registros });
            }).RequireAuthorization();

            // ── Inventario / Kardex ───────────────────────────────────────
            app.MapGet("/api/reportes/inventario", () =>
            {
                using var c = Db.Conn();

                var cmd = c.CreateCommand();
                cmd.CommandText = @"SELECT Codigo,Descripcion,Departamento,Unidad,
                    Existencia,ExistenMin,ExistenMax,Precio,CostoActual,UltimaActualz
                    FROM Inventario ORDER BY Departamento,Descripcion";
                var items = new List<object>();
                decimal valorTotal = 0;
                int bajMin = 0;
                using var r = cmd.ExecuteReader();
                while (r.Read()) {
                    decimal stock=Db.D(r,4), min=Db.D(r,5), costo=Db.D(r,8);
                    bool bajo = min>0 && stock<min;
                    if (bajo) bajMin++;
                    valorTotal += stock * costo;
                    items.Add(new { codigo=Db.S(r,0), descripcion=Db.S(r,1),
                        departamento=Db.S(r,2), unidad=Db.S(r,3),
                        stock, stockMin=min, stockMax=Db.D(r,6),
                        precio=Db.D(r,7), costo, valorStock=stock*costo,
                        ultimaActualz=Db.S(r,9), bajMin=bajo });
                }

                return Results.Ok(new { fecha=DateTime.Today.ToString("yyyy-MM-dd"),
                    totalProductos=items.Count, productosBajoMin=bajMin,
                    valorTotalInventario=valorTotal, items });
            }).RequireAuthorization();

            // ── Egresos / Compras ─────────────────────────────────────────
            app.MapGet("/api/reportes/egresos", (string? desde, string? hasta) =>
            {
                string d = desde ?? DateTime.Today.AddMonths(-1).ToString("yyyy-MM-dd");
                string h = hasta ?? DateTime.Today.ToString("yyyy-MM-dd");
                using var c = Db.Conn();

                // Gastos generales
                var cmd = c.CreateCommand();
                cmd.CommandText = @"SELECT COUNT(*),SUM(TotalUSD),SUM(TotalBs)
                    FROM Gastos WHERE Fecha BETWEEN $d AND $h";
                cmd.Parameters.AddWithValue("$d",d); cmd.Parameters.AddWithValue("$h",h);
                using var r = cmd.ExecuteReader();
                r.Read();
                var resGastos = new { count=Db.I2(r,0), totalUSD=Db.D(r,1), totalBs=Db.D(r,2) };
                r.Close();

                // Por categoría
                var cmd2 = c.CreateCommand();
                cmd2.CommandText = @"SELECT Categoria,COUNT(*) cnt,SUM(TotalUSD) total
                    FROM Gastos WHERE Fecha BETWEEN $d AND $h
                    GROUP BY Categoria ORDER BY total DESC";
                cmd2.Parameters.AddWithValue("$d",d); cmd2.Parameters.AddWithValue("$h",h);
                var porCategoria = new List<object>();
                using var r2 = cmd2.ExecuteReader();
                while (r2.Read())
                    porCategoria.Add(new { categoria=Db.S(r2,0), cantidad=Db.I2(r2,1), total=Db.D(r2,2) });

                // Compras de materia prima
                var cmd3 = c.CreateCommand();
                cmd3.CommandText = @"SELECT COUNT(*),SUM(TotalC) FROM Compras
                    WHERE FechaC BETWEEN $d AND $h";
                cmd3.Parameters.AddWithValue("$d",d); cmd3.Parameters.AddWithValue("$h",h);
                using var r3 = cmd3.ExecuteReader();
                r3.Read();
                var resCompras = new { count=Db.I2(r3,0), total=Db.D(r3,1) };
                r3.Close();

                // Detalle gastos recientes
                var cmd4 = c.CreateCommand();
                cmd4.CommandText = @"SELECT Fecha,Proveedor,NumeroDocumento,Categoria,TotalUSD
                    FROM Gastos WHERE Fecha BETWEEN $d AND $h
                    ORDER BY Fecha DESC LIMIT 50";
                cmd4.Parameters.AddWithValue("$d",d); cmd4.Parameters.AddWithValue("$h",h);
                var gastos = new List<object>();
                using var r4 = cmd4.ExecuteReader();
                while (r4.Read())
                    gastos.Add(new { fecha=Db.S(r4,0), proveedor=Db.S(r4,1),
                        nroDoc=Db.S(r4,2), categoria=Db.S(r4,3), totalUSD=Db.D(r4,4) });

                return Results.Ok(new { desde=d, hasta=h, resGastos, resCompras, porCategoria, gastos });
            }).RequireAuthorization();

            // ── Página HTML imprimible de cualquier reporte ───────────────
            app.MapGet("/api/reportes/imprimir", async (string tipo, string? desde, string? hasta, HttpContext ctx) =>
            {
                string d = desde ?? DateTime.Today.AddMonths(-1).ToString("yyyy-MM-dd");
                string h = hasta ?? DateTime.Today.ToString("yyyy-MM-dd");

                string titulo = tipo switch {
                    "ventas"     => "Reporte de Ventas",
                    "produccion" => "Reporte de Producción",
                    "inventario" => "Inventario General",
                    "egresos"    => "Reporte de Egresos y Compras",
                    _ => "Reporte"
                };

                // Construir tabla según tipo
                string tabla = tipo switch {
                    "ventas"     => BuildTablaVentas(d, h),
                    "produccion" => BuildTablaProduccion(d, h),
                    "inventario" => BuildTablaInventario(),
                    "egresos"    => BuildTablaEgresos(d, h),
                    _ => "<p>Tipo de reporte no válido.</p>"
                };

                string periodo = tipo == "inventario"
                    ? $"Al {DateTime.Today:dd/MM/yyyy}"
                    : $"Del {d} al {h}";

                string html = $$"""
<!DOCTYPE html><html lang="es"><head>
<meta charset="UTF-8"><title>{{titulo}}</title>
<style>
*{box-sizing:border-box;margin:0;padding:0;font-family:'Segoe UI',Arial,sans-serif;font-size:10pt}
body{padding:15mm 12mm;color:#111}
h1{font-size:18pt;font-weight:700;margin-bottom:1mm}
.periodo{color:#555;margin-bottom:6mm;font-size:10pt}
table{width:100%;border-collapse:collapse;margin-bottom:6mm;font-size:9pt}
thead tr{background:#1A4A8A;color:#fff}
th,td{padding:2mm 3mm;text-align:left;border-bottom:1px solid #ddd}
.r{text-align:right}.bold{font-weight:700}
tr:nth-child(even){background:#f8f9fa}
.chip-rojo{background:#f8d7da;color:#721c24;padding:0 3mm;border-radius:2mm;font-size:8pt}
.chip-verde{background:#d4edda;color:#155724;padding:0 3mm;border-radius:2mm;font-size:8pt}
@media print{
body{padding:8mm}
.no-print{display:none!important}
}
</style></head><body>
<button class="no-print" onclick="window.print()"
  style="margin-bottom:8mm;padding:2mm 8mm;background:#1A4A8A;color:#fff;border:none;
         border-radius:4px;cursor:pointer;font-size:11pt">🖨 Imprimir</button>
<h1>{{titulo}}</h1>
<div class="periodo">{{periodo}} — Generado {{DateTime.Now:dd/MM/yyyy HH:mm}}</div>
{{tabla}}
</body></html>
""";
                ctx.Response.ContentType = "text/html; charset=utf-8";
                await ctx.Response.WriteAsync(html);
            }).RequireAuthorization();
        }

        // ── Helpers de generación de tablas ───────────────────────────────
        private static string BuildTablaVentas(string d, string h)
        {
            using var c = Db.Conn();
            var cmd = c.CreateCommand();
            cmd.CommandText = @"SELECT NumeroF,Fecha,Codigo,TotalF,Abono,FormaDePago
                FROM Facturas WHERE Fecha BETWEEN $d AND $h
                  AND (Observaciones IS NULL OR Observaciones NOT LIKE 'ANULADA%')
                ORDER BY Fecha DESC,NumeroF DESC";
            cmd.Parameters.AddWithValue("$d",d); cmd.Parameters.AddWithValue("$h",h);
            var filas = new System.Text.StringBuilder();
            decimal sumaTotal=0, sumaCobrado=0;
            using var r = cmd.ExecuteReader();
            while (r.Read()) {
                decimal tot=Db.D(r,3), ab=Db.D(r,4);
                sumaTotal += tot; sumaCobrado += ab;
                filas.Append($"""
                <tr><td>{H(Db.S(r,0))}</td><td>{H(Db.S(r,1))}</td><td>{H(Db.S(r,2))}</td>
                <td class="r">{tot:N2}</td><td class="r">{ab:N2}</td>
                <td class="r">{tot-ab:N2}</td><td>{H(Db.S(r,5))}</td>
                <td><span class="{(ab>=tot?"chip-verde":"chip-rojo")}">{(ab>=tot?"Pagada":"Pendiente")}</span></td></tr>
                """);
            }
            return $"""
            <table><thead><tr><th>N° Factura</th><th>Fecha</th><th>Cliente</th>
            <th class="r">Total</th><th class="r">Cobrado</th><th class="r">Saldo</th>
            <th>Forma Pago</th><th>Estado</th></tr></thead>
            <tbody>{filas}</tbody>
            <tfoot><tr class="bold"><td colspan="3">TOTAL</td>
            <td class="r">{sumaTotal:N2}</td><td class="r">{sumaCobrado:N2}</td>
            <td class="r">{sumaTotal-sumaCobrado:N2}</td><td colspan="2"></td></tr></tfoot></table>
            """;
        }

        private static string BuildTablaProduccion(string d, string h)
        {
            using var c = Db.Conn();
            var cmd = c.CreateCommand();
            cmd.CommandText = @"SELECT Fecha,CodigoMolde,Material,Operario,
                CantPrimeraCalidad,CantSegundaCalidad,CantDefectuosas
                FROM RegistroProduccionDia WHERE Fecha BETWEEN $d AND $h
                ORDER BY Fecha DESC, Id DESC";
            cmd.Parameters.AddWithValue("$d",d); cmd.Parameters.AddWithValue("$h",h);
            var filas = new System.Text.StringBuilder();
            decimal sumP1=0,sumP2=0,sumDef=0;
            using var r = cmd.ExecuteReader();
            while (r.Read()) {
                decimal p1=Db.D(r,4), p2=Db.D(r,5), def=Db.D(r,6);
                sumP1+=p1; sumP2+=p2; sumDef+=def;
                filas.Append($"""
                <tr><td>{H(Db.S(r,0))}</td><td>{H(Db.S(r,1))}</td>
                <td>{H(Db.S(r,2))}</td><td>{H(Db.S(r,3))}</td>
                <td class="r">{p1:N0}</td><td class="r">{p2:N0}</td>
                <td class="r">{def:N0}</td><td class="r bold">{p1+p2+def:N0}</td></tr>
                """);
            }
            return $"""
            <table><thead><tr><th>Fecha</th><th>Molde</th><th>Material</th><th>Operario</th>
            <th class="r">1ª Cal.</th><th class="r">2ª Cal.</th>
            <th class="r">Defect.</th><th class="r">Total</th></tr></thead>
            <tbody>{filas}</tbody>
            <tfoot><tr class="bold"><td colspan="4">TOTAL</td>
            <td class="r">{sumP1:N0}</td><td class="r">{sumP2:N0}</td>
            <td class="r">{sumDef:N0}</td><td class="r">{sumP1+sumP2+sumDef:N0}</td></tr></tfoot></table>
            """;
        }

        private static string BuildTablaInventario()
        {
            using var c = Db.Conn();
            var cmd = c.CreateCommand();
            cmd.CommandText = @"SELECT Codigo,Descripcion,Departamento,Unidad,
                Existencia,ExistenMin,Precio,CostoActual
                FROM Inventario ORDER BY Departamento,Descripcion";
            var filas = new System.Text.StringBuilder();
            decimal valorTotal = 0;
            using var r = cmd.ExecuteReader();
            while (r.Read()) {
                decimal stock=Db.D(r,4), min=Db.D(r,5), costo=Db.D(r,7);
                bool bajo = min>0 && stock<min;
                valorTotal += stock * costo;
                filas.Append($"""
                <tr><td>{H(Db.S(r,0))}</td><td>{H(Db.S(r,1))}</td><td>{H(Db.S(r,2))}</td>
                <td>{H(Db.S(r,3))}</td>
                <td class="r {(bajo?"chip-rojo":"")}">{stock:N2}</td>
                <td class="r">{min:N2}</td><td class="r">{Db.D(r,6):N2}</td>
                <td class="r">{costo:N2}</td><td class="r">{stock*costo:N2}</td></tr>
                """);
            }
            return $"""
            <table><thead><tr><th>Código</th><th>Descripción</th><th>Dpto.</th><th>Unidad</th>
            <th class="r">Stock</th><th class="r">Mín.</th>
            <th class="r">Precio</th><th class="r">Costo</th><th class="r">Valor Stock</th></tr></thead>
            <tbody>{filas}</tbody>
            <tfoot><tr class="bold"><td colspan="8">VALOR TOTAL INVENTARIO</td>
            <td class="r">{valorTotal:N2}</td></tr></tfoot></table>
            """;
        }

        private static string BuildTablaEgresos(string d, string h)
        {
            using var c = Db.Conn();
            var cmd = c.CreateCommand();
            cmd.CommandText = @"SELECT Fecha,Proveedor,NumeroDocumento,Categoria,TotalUSD,TotalBs
                FROM Gastos WHERE Fecha BETWEEN $d AND $h ORDER BY Fecha DESC LIMIT 100";
            cmd.Parameters.AddWithValue("$d",d); cmd.Parameters.AddWithValue("$h",h);
            var filas = new System.Text.StringBuilder();
            decimal sumUSD=0, sumBs=0;
            using var r = cmd.ExecuteReader();
            while (r.Read()) {
                decimal usd=Db.D(r,4), bs=Db.D(r,5);
                sumUSD+=usd; sumBs+=bs;
                filas.Append($"""
                <tr><td>{H(Db.S(r,0))}</td><td>{H(Db.S(r,1))}</td>
                <td>{H(Db.S(r,2))}</td><td>{H(Db.S(r,3))}</td>
                <td class="r">{usd:N2}</td><td class="r">{bs:N2}</td></tr>
                """);
            }
            return $"""
            <table><thead><tr><th>Fecha</th><th>Proveedor</th><th>N° Doc.</th>
            <th>Categoría</th><th class="r">USD</th><th class="r">Bs.</th></tr></thead>
            <tbody>{filas}</tbody>
            <tfoot><tr class="bold"><td colspan="4">TOTAL</td>
            <td class="r">{sumUSD:N2}</td><td class="r">{sumBs:N2}</td></tr></tfoot></table>
            """;
        }

        private static string H(string? s) => System.Web.HttpUtility.HtmlEncode(s ?? "");
    }
}
