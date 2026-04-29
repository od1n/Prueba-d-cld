// ════════════════════════════════════════════════════════════════════════
// ARCHIVO: PdfService_Reportes.cs  →  Services\
// ════════════════════════════════════════════════════════════════════════

using System;
using System.Collections.Generic;
using System.Linq;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace SistemaGestion
{
    public static partial class PdfService
    {
        // ── Helper: genera y convierte a byte[] (igual que los otros metodos) ─
        private static byte[] Pdf(Action<IDocumentContainer> build)
            => Document.Create(build).GeneratePdf();

        // ── Helper: cabecera comun ────────────────────────────────────────────
        private static void Cabecera(IDocumentContainer container,
            string titulo, string subtitulo, Action<IContainer> contenido)
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4.Landscape());
                page.Margin(1.5f, Unit.Centimetre);
                page.Header().Column(col =>
                {
                    col.Item().Background("#1e6440").Padding(8).Row(row =>
                    {
                        row.RelativeItem().Text(titulo)
                            .FontSize(14).Bold().FontColor(Colors.White);
                        row.ConstantItem(200).AlignRight().Text(subtitulo)
                            .FontSize(9).FontColor(Colors.White);
                    });
                    col.Item().Height(4);
                });
                page.Content().Element(contenido);
                page.Footer().AlignCenter().Text(x =>
                {
                    x.Span("Pagina ").FontSize(8).FontColor(Colors.Grey.Medium);
                    x.CurrentPageNumber().FontSize(8).FontColor(Colors.Grey.Medium);
                    x.Span(" de ").FontSize(8).FontColor(Colors.Grey.Medium);
                    x.TotalPages().FontSize(8).FontColor(Colors.Grey.Medium);
                });
            });
        }

        // ── Helper: celda encabezado ──────────────────────────────────────────
        private static IContainer CeldaHdr(TableDescriptor t, string color = "#1e6440")
            => t.Cell().Background(color).Padding(4);

        // ════════════════════════════════════════════════════════════════
        //  CLIENTES
        // ════════════════════════════════════════════════════════════════
        public static byte[] GenerarReporteClientes(List<Cliente> clientes)
            => Pdf(c => Cabecera(c,
                "Listado de Clientes",
                $"Total: {clientes.Count}  |  {DateTime.Today:dd/MM/yyyy}",
                content => content.Table(table =>
                {
                    table.ColumnsDefinition(cd =>
                    {
                        cd.ConstantColumn(60); cd.RelativeColumn(3); cd.RelativeColumn(2);
                        cd.RelativeColumn(1.5f); cd.ConstantColumn(90);
                        cd.ConstantColumn(70); cd.ConstantColumn(70);
                    });

                    string[] hdrs = { "Codigo","Nombre","Ciudad/Estado",
                                      "Telefono","CI/RIF","Credito","A Favor" };
                    foreach (var h in hdrs)
                        CeldaHdr(table).Text(h).FontSize(9).Bold().FontColor(Colors.White);

                    bool alt = false;
                    foreach (var cl in clientes)
                    {
                        string bg = alt ? "#f0f8f4" : Colors.White; alt = !alt;
                        string[] vals = {
                            cl.Codigo, cl.Descripcion ?? "",
                            (cl.Ciudad ?? "") + " " + (cl.Estado ?? ""),
                            cl.Telefono ?? "", cl.CiRif ?? "",
                            cl.Credito.ToString("C2"), cl.AFavor.ToString("C2")
                        };
                        foreach (var v in vals)
                            table.Cell().Background(bg).Padding(3).Text(v).FontSize(8);
                    }
                })));

        // ════════════════════════════════════════════════════════════════
        //  VENTAS DETALLADO / AGRUPADO
        // ════════════════════════════════════════════════════════════════
        public static byte[] GenerarReporteVentas(List<LineaVentaReporte> lineas,
            DateTime desde, DateTime hasta, string codigoProducto)
        {
            // Detectar si hay tasas cargadas para mostrar columnas Bs
            bool hayTasaBCV      = lineas.Any(l => l.TasaBCV      > 0);
            bool hayTasaParalelo = lineas.Any(l => l.TasaParalelo > 0);

            return Pdf(c => Cabecera(c,
                codigoProducto != null
                    ? $"Ventas — Producto {codigoProducto}"
                    : "Reporte de Ventas",
                $"{desde:dd/MM/yyyy} al {hasta:dd/MM/yyyy}  |  {lineas.Count} lineas",
                content => content.Table(table =>
                {
                    // Columnas base + columnas Bs según disponibilidad
                    table.ColumnsDefinition(cd =>
                    {
                        cd.ConstantColumn(70);  // Factura
                        cd.ConstantColumn(65);  // Fecha
                        cd.ConstantColumn(55);  // Codigo
                        cd.RelativeColumn(3);   // Descripcion
                        cd.ConstantColumn(55);  // Cantidad
                        cd.ConstantColumn(65);  // Precio USD
                        cd.ConstantColumn(75);  // Total USD
                        if (hayTasaBCV)      cd.ConstantColumn(80); // Total BCV
                        if (hayTasaParalelo) cd.ConstantColumn(80); // Total Paralelo
                    });

                    // Encabezados
                    var hdrs = new List<string> {
                        "Factura","Fecha","Codigo","Descripcion",
                        "Cantidad","Precio $","Total $" };
                    if (hayTasaBCV)      hdrs.Add("Total BCV");
                    if (hayTasaParalelo) hdrs.Add("Total Paralelo");

                    foreach (var h in hdrs)
                        CeldaHdr(table).Text(h).FontSize(9).Bold().FontColor(Colors.White);

                    // Filas de datos
                    bool alt = false;
                    foreach (var l in lineas)
                    {
                        string bg = alt ? "#f0f8f4" : Colors.White; alt = !alt;

                        var vals = new List<string> {
                            l.NumFactura,
                            l.Fecha.ToString("dd/MM/yy"),
                            l.Codigo,
                            l.Descripcion,
                            l.Cantidad.ToString("N2"),
                            l.PrecioUnit.ToString("N2"),
                            l.Total.ToString("N2")
                        };
                        if (hayTasaBCV)
                            vals.Add(l.TasaBCV > 0
                                ? l.TotalBCV.ToString("N2")
                                : "—");
                        if (hayTasaParalelo)
                            vals.Add(l.TasaParalelo > 0
                                ? l.TotalParalelo.ToString("N2")
                                : "—");

                        foreach (var v in vals)
                            table.Cell().Background(bg).Padding(3).Text(v).FontSize(8);
                    }

                    // Fila de totales
                    int colsAntes = 6;  // celdas vacías antes de los totales
                    for (int i = 0; i < colsAntes; i++)
                        table.Cell().Background("#e8f4ec").Padding(3)
                            .Text(i == 3 ? "TOTAL" : "").FontSize(8).Bold();

                    // Total USD
                    table.Cell().Background("#e8f4ec").Padding(3)
                        .Text(lineas.Sum(l => l.Total).ToString("N2"))
                        .FontSize(9).Bold();

                    // Total BCV (suma ponderada: cada línea con su tasa del día)
                    if (hayTasaBCV)
                        table.Cell().Background("#e8f4ec").Padding(3)
                            .Text(lineas.Where(l => l.TasaBCV > 0)
                                        .Sum(l => l.TotalBCV).ToString("N2"))
                            .FontSize(9).Bold();

                    // Total Paralelo
                    if (hayTasaParalelo)
                        table.Cell().Background("#e8f4ec").Padding(3)
                            .Text(lineas.Where(l => l.TasaParalelo > 0)
                                        .Sum(l => l.TotalParalelo).ToString("N2"))
                            .FontSize(9).Bold();

                    // Nota sobre tasas si aplica
                    if (hayTasaBCV || hayTasaParalelo)
                    {
                        int totalCols = 7 + (hayTasaBCV ? 1 : 0) + (hayTasaParalelo ? 1 : 0);
                        table.Cell().ColumnSpan((uint)totalCols)
                            .Background("#f5f5f5").Padding(4)
                            .Text("* Los totales en Bs usan la tasa registrada el día de cada factura.")
                            .FontSize(7).Italic().FontColor(Colors.Grey.Medium);
                    }
                })));
        }

        // ════════════════════════════════════════════════════════════════
        //  VENTAS POR CLIENTE
        // ════════════════════════════════════════════════════════════════
        public static byte[] GenerarReporteVentasPorCliente(List<Factura> facturas,
            DateTime desde, DateTime hasta, string cliente)
            => Pdf(c => Cabecera(c,
                cliente != null ? $"Ventas — Cliente {cliente}" : "Ventas por Cliente",
                $"{desde:dd/MM/yyyy} al {hasta:dd/MM/yyyy}  |  {facturas.Count} facturas",
                content => content.Table(table =>
                {
                    table.ColumnsDefinition(cd =>
                    {
                        cd.ConstantColumn(80); cd.ConstantColumn(80); cd.RelativeColumn(2);
                        cd.ConstantColumn(90); cd.ConstantColumn(70); cd.ConstantColumn(80);
                    });

                    foreach (var h in new[] { "Numero","Fecha","Cliente",
                        "Forma Pago","IVA","Total" })
                        CeldaHdr(table).Text(h).FontSize(9).Bold().FontColor(Colors.White);

                    bool alt = false;
                    foreach (var f in facturas)
                    {
                        string bg = alt ? "#f0f8f4" : Colors.White; alt = !alt;
                        foreach (var v in new[] {
                            f.NumeroF, f.Fecha.ToString("dd/MM/yy"), f.Codigo,
                            f.FormaDePago, f.IvaF.ToString("C2"), f.TotalF.ToString("C2") })
                            table.Cell().Background(bg).Padding(3).Text(v).FontSize(8);
                    }

                    for (int i = 0; i < 5; i++)
                        table.Cell().Background("#e8f4ec").Padding(3)
                            .Text(i == 2 ? "TOTAL" : "").FontSize(8).Bold();
                    table.Cell().Background("#e8f4ec").Padding(3)
                        .Text(facturas.Sum(f => f.TotalF).ToString("C2")).FontSize(9).Bold();
                })));

        // ════════════════════════════════════════════════════════════════
        //  ABONOS
        // ════════════════════════════════════════════════════════════════
        public static byte[] GenerarReporteAbonos(List<Abono> abonos,
            DateTime desde, DateTime hasta, string cliente)
            => Pdf(c => Cabecera(c,
                cliente != null ? $"Abonos — Cliente {cliente}" : "Reporte de Abonos",
                $"{desde:dd/MM/yyyy} al {hasta:dd/MM/yyyy}  |  {abonos.Count} registros",
                content => content.Table(table =>
                {
                    table.ColumnsDefinition(cd =>
                    {
                        cd.ConstantColumn(55); cd.ConstantColumn(90);
                        cd.ConstantColumn(80); cd.RelativeColumn(2); cd.ConstantColumn(90);
                    });

                    foreach (var h in new[] { "Num","Factura","Fecha","Cliente","Monto" })
                        CeldaHdr(table, "#287888").Text(h).FontSize(9).Bold().FontColor(Colors.White);

                    bool alt = false;
                    foreach (var a in abonos)
                    {
                        string bg = alt ? "#f0fafa" : Colors.White; alt = !alt;
                        foreach (var v in new[] {
                            a.NumAbono.ToString(), a.NumeroF,
                            a.Fecha.ToString("dd/MM/yy"), a.Cliente ?? "",
                            a.Monto.ToString("C2") })
                            table.Cell().Background(bg).Padding(3).Text(v).FontSize(8);
                    }

                    for (int i = 0; i < 4; i++)
                        table.Cell().Background("#e0f4f4").Padding(3)
                            .Text(i == 2 ? "TOTAL" : "").FontSize(8).Bold();
                    table.Cell().Background("#e0f4f4").Padding(3)
                        .Text(abonos.Sum(a => a.Monto).ToString("C2")).FontSize(9).Bold();
                })));

        // ════════════════════════════════════════════════════════════════
        //  CUENTAS POR COBRAR
        // ════════════════════════════════════════════════════════════════
        public static byte[] GenerarReporteCuentasCobrar(List<CuentaCobrar> cuentas,
            string cliente)
            => Pdf(c => Cabecera(c,
                cliente != null ? $"Cuentas por Cobrar — {cliente}" : "Cuentas por Cobrar",
                $"Generado: {DateTime.Today:dd/MM/yyyy}  |  {cuentas.Count} cuentas",
                content => content.Table(table =>
                {
                    table.ColumnsDefinition(cd =>
                    {
                        cd.RelativeColumn(2); cd.RelativeColumn(3);
                        cd.ConstantColumn(80); cd.ConstantColumn(80);
                        cd.ConstantColumn(80); cd.ConstantColumn(80); cd.ConstantColumn(70);
                    });

                    foreach (var h in new[] { "Cliente","Concepto","Monto",
                        "Pagado","Saldo","Vence","Estado" })
                        CeldaHdr(table, "#287888").Text(h).FontSize(9).Bold().FontColor(Colors.White);

                    bool alt = false;
                    foreach (var cc in cuentas)
                    {
                        string bg = cc.Saldo <= 0 ? "#f0fff0"
                                  : cc.FechaVence < DateTime.Today ? "#fff0f0"
                                  : alt ? "#f0fafa" : Colors.White;
                        alt = !alt;
                        foreach (var v in new[] {
                            cc.Cliente, cc.Concepto,
                            cc.Monto.ToString("C2"), cc.Pagado.ToString("C2"),
                            cc.Saldo.ToString("C2"), cc.FechaVence.ToString("dd/MM/yy"),
                            cc.Estado })
                            table.Cell().Background(bg).Padding(3).Text(v).FontSize(8);
                    }

                    var pend = cuentas.Where(x => x.Saldo > 0).ToList();
                    foreach (var v in new[] {
                        "TOTALES","",
                        cuentas.Sum(x => x.Monto).ToString("C2"),
                        cuentas.Sum(x => x.Pagado).ToString("C2"),
                        pend.Sum(x => x.Saldo).ToString("C2"),
                        "", $"{pend.Count} pendientes" })
                        table.Cell().Background("#e0f0f4").Padding(3).Text(v).FontSize(8).Bold();
                })));

        // ════════════════════════════════════════════════════════════════
        //  PRODUCCION
        // ════════════════════════════════════════════════════════════════
        public static byte[] GenerarReporteProduccion(List<ProduccionDiaria> datos,
            DateTime desde, DateTime hasta, string codigo, bool detallado)
            => Pdf(c => Cabecera(c,
                (codigo != null ? $"Produccion — {codigo}" : "Reporte de Produccion") +
                    (detallado ? " (Detallado)" : " (Totales)"),
                $"{desde:dd/MM/yyyy} al {hasta:dd/MM/yyyy}  |  {datos.Count} registros",
                content =>
                {
                    if (detallado)
                    {
                        content.Table(table =>
                        {
                            table.ColumnsDefinition(cd =>
                            {
                                cd.ConstantColumn(75); cd.ConstantColumn(70);
                                cd.RelativeColumn(3);  cd.ConstantColumn(70);
                                cd.ConstantColumn(80); cd.ConstantColumn(90);
                            });

                            foreach (var h in new[] { "Fecha","Codigo","Descripcion",
                                "Cantidad","Costo Unit.","Costo Total" })
                                CeldaHdr(table, "#147888").Text(h).FontSize(9).Bold().FontColor(Colors.White);

                            bool alt = false;
                            foreach (var p in datos)
                            {
                                string bg = alt ? "#f0fafa" : Colors.White; alt = !alt;
                                foreach (var v in new[] {
                                    p.Fecha.ToString("dd/MM/yy"), p.Codigo, p.Descripcion,
                                    p.Cantidad.ToString("N2"), p.CostoUnitario.ToString("C2"),
                                    p.CostoTotal.ToString("C2") })
                                    table.Cell().Background(bg).Padding(3).Text(v).FontSize(8);
                            }

                            foreach (var v in new[] {
                                "TOTAL","","",
                                datos.Sum(p => p.Cantidad).ToString("N2"),
                                "", datos.Sum(p => p.CostoTotal).ToString("C2") })
                                table.Cell().Background("#e0f4f4").Padding(3).Text(v).FontSize(8).Bold();
                        });
                    }
                    else
                    {
                        var agrupado = datos
                            .GroupBy(p => new { p.Codigo, p.Descripcion })
                            .Select(g => new {
                                g.Key.Codigo, g.Key.Descripcion,
                                TotalCant  = g.Sum(x => x.Cantidad),
                                CostoTotal = g.Sum(x => x.CostoTotal)
                            }).OrderByDescending(x => x.TotalCant).ToList();

                        content.Table(table =>
                        {
                            table.ColumnsDefinition(cd =>
                            {
                                cd.ConstantColumn(80); cd.RelativeColumn(4);
                                cd.ConstantColumn(90); cd.ConstantColumn(100);
                            });

                            foreach (var h in new[] { "Codigo","Descripcion",
                                "Total Unidades","Costo Total" })
                                CeldaHdr(table, "#147888").Text(h).FontSize(9).Bold().FontColor(Colors.White);

                            bool alt = false;
                            foreach (var p in agrupado)
                            {
                                string bg = alt ? "#f0fafa" : Colors.White; alt = !alt;
                                foreach (var v in new[] {
                                    p.Codigo, p.Descripcion,
                                    p.TotalCant.ToString("N2"), p.CostoTotal.ToString("C2") })
                                    table.Cell().Background(bg).Padding(3).Text(v).FontSize(8);
                            }
                        });
                    }
                }));

        // ════════════════════════════════════════════════════════════════
        //  INVENTARIO
        // ════════════════════════════════════════════════════════════════
        public static byte[] GenerarReporteInventario(List<Inventario> items)
            => Pdf(c => Cabecera(c,
                "Reporte de Inventario",
                $"{DateTime.Today:dd/MM/yyyy}  |  {items.Count} productos",
                content => content.Table(table =>
                {
                    table.ColumnsDefinition(cd =>
                    {
                        cd.ConstantColumn(70); cd.RelativeColumn(3); cd.RelativeColumn(1.5f);
                        cd.ConstantColumn(50); cd.ConstantColumn(75); cd.ConstantColumn(75);
                        cd.ConstantColumn(55); cd.ConstantColumn(50); cd.ConstantColumn(50);
                    });

                    foreach (var h in new[] { "Codigo","Descripcion","Depto","Unidad",
                        "Costo","Precio","Exist.","Min","Max" })
                        CeldaHdr(table).Text(h).FontSize(8).Bold().FontColor(Colors.White);

                    foreach (var i in items)
                    {
                        string bg = i.Existencia == 0 ? "#ffd6d6"
                                  : i.Existencia < i.ExistenMin ? "#fff5c0"
                                  : Colors.White;
                        foreach (var v in new[] {
                            i.Codigo, i.Descripcion ?? "", i.Departamento ?? "", i.Unidad ?? "",
                            i.CostoActual.ToString("C2"), i.Precio.ToString("C2"),
                            i.Existencia.ToString("N0"),
                            i.ExistenMin.ToString("N0"), i.ExistenMax.ToString("N0") })
                            table.Cell().Background(bg).Padding(3).Text(v).FontSize(7.5f);
                    }
                })));

        // ════════════════════════════════════════════════════════════════
        //  PATRONES DE VENTAS MULTI-AÑO
        // ════════════════════════════════════════════════════════════════
        public static byte[] GenerarReportePatronesVentas(
            List<(int Ano, int Mes, decimal Total)> datos, string anosStr)
        {
            var anos = datos.Select(d => d.Ano).Distinct().OrderBy(a => a).ToList();
            string[] nm = { "Ene","Feb","Mar","Abr","May","Jun",
                             "Jul","Ago","Sep","Oct","Nov","Dic" };

            return Pdf(c => Cabecera(c,
                "Patrones de Comportamiento en Ventas",
                $"Anos: {anosStr}  |  Generado: {DateTime.Today:dd/MM/yyyy}",
                content => content.Column(col =>
                {
                    // Tabla mensual
                    col.Item().Table(table =>
                    {
                        table.ColumnsDefinition(cd =>
                        {
                            cd.ConstantColumn(45);
                            for (int m = 0; m < 12; m++) cd.RelativeColumn();
                            cd.ConstantColumn(75);
                        });

                        CeldaHdr(table, "#641464").Text("Ano").FontSize(8).Bold().FontColor(Colors.White);
                        foreach (var m in nm)
                            CeldaHdr(table, "#641464").Text(m).FontSize(8).Bold().FontColor(Colors.White);
                        CeldaHdr(table, "#641464").Text("Total").FontSize(8).Bold().FontColor(Colors.White);

                        bool alt = false;
                        foreach (var ano in anos)
                        {
                            string bg = alt ? "#faf0ff" : Colors.White; alt = !alt;
                            table.Cell().Background(bg).Padding(3).Text(ano.ToString()).FontSize(8).Bold();
                            for (int m = 1; m <= 12; m++)
                            {
                                var val = datos.FirstOrDefault(d => d.Ano == ano && d.Mes == m).Total;
                                table.Cell().Background(bg).Padding(3)
                                    .Text(val > 0 ? val.ToString("C0") : "—").FontSize(7.5f);
                            }
                            table.Cell().Background(bg).Padding(3)
                                .Text(datos.Where(d => d.Ano == ano).Sum(d => d.Total).ToString("C0"))
                                .FontSize(8).Bold();
                        }
                    });

                    col.Item().Height(16);

                    // Analisis tendencia
                    col.Item().Background("#f5f0ff").Padding(10).Column(analisis =>
                    {
                        analisis.Item().Text("Analisis de Tendencia por Mes").FontSize(10).Bold();
                        analisis.Item().Height(6);

                        if (anos.Count < 2) return;

                        for (int m = 1; m <= 12; m++)
                        {
                            var vals = anos.Select(a =>
                                datos.FirstOrDefault(d => d.Ano == a && d.Mes == m).Total).ToList();
                            if (vals.All(v => v == 0)) continue;

                            decimal pct = vals.First() > 0
                                ? Math.Round((vals.Last() - vals.First()) / vals.First() * 100, 1) : 0;
                            string tend = pct > 5 ? "▲ CRECIMIENTO" : pct < -5 ? "▼ CAIDA" : "► ESTABLE";
                            string color = pct > 5 ? "#1e6440" : pct < -5 ? "#8c1414" : "#444444";

                            analisis.Item()
                                .Text($"{nm[m-1]}: {tend} ({pct:+0.0;-0.0}%)   " +
                                      string.Join(" → ", vals.Select(v => v.ToString("C0"))))
                                .FontSize(8).FontColor(color);
                        }
                    });
                })));
        }
    }
}
