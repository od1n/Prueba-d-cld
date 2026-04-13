using System;
using System.Collections.Generic;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace SistemaGestion
{
    public static partial class PdfService
    {
        // =====================================================================
        // PDF COMPARACION DE PERIODOS
        // =====================================================================
        public static byte[] GenerarReporteComparacion(
            ComparacionData data,
            DateTime p1Desde, DateTime p1Hasta,
            DateTime p2Desde, DateTime p2Hasta)
        {
            string labelP1 = $"{p1Desde:dd/MM/yy} – {p1Hasta:dd/MM/yy}";
            string labelP2 = $"{p2Desde:dd/MM/yy} – {p2Hasta:dd/MM/yy}";

            var doc = Document.Create(c =>
            {
                c.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(24);
                    page.DefaultTextStyle(x => x.FontFamily("Arial").FontSize(9));

                    page.Header().Column(col =>
                    {
                        col.Item().Text("REPORTE COMPARATIVO DE PERIODOS")
                            .FontSize(14).Bold().FontColor("#323280");
                        col.Item().Text($"Generado: {DateTime.Now:dd/MM/yyyy HH:mm}")
                            .FontSize(8).FontColor("#888888");
                        col.Item().Height(6);
                        col.Item().LineHorizontal(2).LineColor("#323280");
                        col.Item().Height(8);
                    });

                    page.Content().Column(col =>
                    {
                        // Encabezado periodos
                        col.Item().Table(hdr =>
                        {
                            hdr.ColumnsDefinition(cd => { cd.RelativeColumn(); cd.RelativeColumn(); cd.RelativeColumn(); });
                            hdr.Cell().Background("#323280").Padding(6).Text("Metrica").FontColor(Colors.White).Bold();
                            hdr.Cell().Background("#163770").Padding(6).Text($"Periodo 1\n{labelP1}").FontColor(Colors.White).Bold().AlignCenter();
                            hdr.Cell().Background("#a02828").Padding(6).Text($"Periodo 2\n{labelP2}").FontColor(Colors.White).Bold().AlignCenter();
                        });
                        col.Item().Height(4);

                        // Tabla de métricas
                        col.Item().Table(tabla =>
                        {
                            tabla.ColumnsDefinition(cd =>
                            {
                                cd.RelativeColumn(3);
                                cd.RelativeColumn(2);
                                cd.RelativeColumn(2);
                                cd.RelativeColumn(2);
                                cd.RelativeColumn(1);
                            });

                            tabla.Header(h =>
                            {
                                void Hdr(string t, bool right = false)
                                {
                                    var cell = h.Cell().Background("#323280").Padding(4)
                                        .Text(t).FontColor(Colors.White).Bold().FontSize(8);
                                    if (right) cell.AlignRight();
                                }
                                Hdr("Metrica"); Hdr(labelP1, true); Hdr(labelP2, true);
                                Hdr("Diferencia", true); Hdr("Var%", true);
                            });

                            void Seccion(string titulo)
                            {
                                tabla.Cell().ColumnSpan(5).Background("#e8e8f5").Padding(4)
                                    .Text(titulo).Bold().FontSize(9).FontColor("#323280");
                            }

                            void Fila(string nombre, decimal v1, decimal v2,
                                bool esDinero = true, bool menorEsMejor = false)
                            {
                                decimal dif = v1 - v2;
                                decimal pct = v2 != 0 ? dif / Math.Abs(v2) * 100 : (v1 != 0 ? 100 : 0);
                                bool positivo = menorEsMejor ? dif < 0 : dif > 0;
                                string colorPct = dif == 0 ? "#888888"
                                    : positivo ? "#27ae60" : "#c0392b";
                                string fmt(decimal v) => esDinero ? $"$ {v:N2}" : v.ToString("N0");

                                tabla.Cell().Padding(4).Text(nombre).FontSize(8.5f);
                                tabla.Cell().Padding(4).Text(fmt(v1)).FontSize(8.5f).AlignRight();
                                tabla.Cell().Padding(4).Text(fmt(v2)).FontSize(8.5f).AlignRight();
                                tabla.Cell().Padding(4).Text(fmt(dif)).FontSize(8.5f).AlignRight()
                                    .FontColor(dif >= 0 ? "#163770" : "#c0392b");
                                tabla.Cell().Padding(4).Text($"{pct:+0.0;-0.0}%")
                                    .FontSize(8.5f).AlignRight().FontColor(colorPct);
                            }

                            Seccion("VENTAS");
                            Fila("Total Facturado",   data.VentasPer1,    data.VentasPer2);
                            Fila("N° de Facturas",    data.NFacturasPer1, data.NFacturasPer2, false);
                            Fila("Ticket Promedio",   data.TicketPer1,    data.TicketPer2);
                            Fila("IVA Ventas",        data.IvaVentasPer1, data.IvaVentasPer2);

                            Seccion("COMPRAS");
                            Fila("Total Compras",     data.ComprasPer1,   data.ComprasPer2, menorEsMejor: true);
                            Fila("N° de Compras",     data.NComprasPer1,  data.NComprasPer2, false, true);
                            Fila("IVA Compras",       data.IvaComprasPer1,data.IvaComprasPer2);

                            Seccion("GASTOS");
                            Fila("Total Gastos",      data.GastosPer1,    data.GastosPer2, menorEsMejor: true);

                            Seccion("RESULTADO OPERACIONAL");
                            Fila("Utilidad Bruta",    data.UtilidadPer1,  data.UtilidadPer2);
                            Fila("Margen Bruto %",    data.MargenPer1,    data.MargenPer2, false);

                            Seccion("NOMINA");
                            Fila("Total Nomina",      data.NominaPer1,    data.NominaPer2, menorEsMejor: true);
                            Fila("Periodos Cerrados", data.NPeriodosPer1, data.NPeriodosPer2, false);
                        });
                    });

                    page.Footer().Row(row =>
                    {
                        row.RelativeItem().Text($"SistemaGestion — Comparacion {labelP1} vs {labelP2}")
                            .FontSize(7).FontColor("#aaaaaa");
                        row.ConstantItem(80).AlignRight().Text(x =>
                        {
                            x.Span("Pag. ").FontSize(7).FontColor("#aaaaaa");
                            x.CurrentPageNumber().FontSize(7).FontColor("#aaaaaa");
                            x.Span(" / ").FontSize(7).FontColor("#aaaaaa");
                            x.TotalPages().FontSize(7).FontColor("#aaaaaa");
                        });
                    });
                });
            });

            return doc.GeneratePdf();
        }

        // =====================================================================
        // PDF DASHBOARD EJECUTIVO
        // =====================================================================
        public static byte[] GenerarDashboardEjecutivo(DashboardSnapshot snap)
        {
            var doc = Document.Create(c =>
            {
                c.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(24);
                    page.DefaultTextStyle(x => x.FontFamily("Arial").FontSize(9));

                    page.Header().Column(col =>
                    {
                        col.Item().Row(row =>
                        {
                            row.RelativeItem().Column(inner =>
                            {
                                inner.Item().Text("DASHBOARD EJECUTIVO")
                                    .FontSize(16).Bold().FontColor("#163770");
                                inner.Item().Text($"Generado: {snap.FechaGeneracion:dd/MM/yyyy HH:mm}  |  Usuario: {snap.Usuario}")
                                    .FontSize(8).FontColor("#888888");
                            });
                        });
                        col.Item().Height(6);
                        col.Item().LineHorizontal(2.5f).LineColor("#163770");
                        col.Item().Height(10);
                    });

                    page.Content().Column(col =>
                    {
                        // ── KPIs ──
                        col.Item().Text("Indicadores Clave del Periodo")
                            .FontSize(11).Bold().FontColor("#163770");
                        col.Item().Height(6);

                        col.Item().Table(kpis =>
                        {
                            kpis.ColumnsDefinition(cd =>
                            {
                                cd.RelativeColumn(); cd.RelativeColumn();
                                cd.RelativeColumn(); cd.RelativeColumn();
                                cd.RelativeColumn(); cd.RelativeColumn();
                            });

                            void Kpi(string titulo, string valor, string colorHex)
                            {
                                kpis.Cell().Background(colorHex).Padding(10).Column(c =>
                                {
                                    c.Item().Text(titulo).FontSize(7.5f)
                                        .FontColor("#ccddff");
                                    c.Item().Height(4);
                                    c.Item().Text(valor).FontSize(13).Bold()
                                        .FontColor(Colors.White);
                                });
                            }

                            Kpi("Ventas del Mes",        $"$ {snap.VentasMes:N0}",    "#163770");
                            Kpi("Facturado Hoy",         $"$ {snap.VentasHoy:N0}",    "#1a6e3c");
                            Kpi("Facturas Pendientes",   snap.FacturasPend.ToString(), "#b46414");
                            Kpi("Productos Bajo Min.",   snap.StockBajo.ToString(),    "#a02828");
                            Kpi("Clientes",              snap.TotalClientes.ToString(),"#503296");
                            Kpi("Top Producto",          snap.TopProducto,             "#146e82");
                        });

                        col.Item().Height(18);

                        // ── Ventas por mes ──
                        col.Item().Text("Ventas Ultimos 6 Meses (USD)")
                            .FontSize(11).Bold().FontColor("#163770");
                        col.Item().Height(6);

                        col.Item().Table(ventas =>
                        {
                            ventas.ColumnsDefinition(cd =>
                            {
                                foreach (var _ in snap.VentasPorMes)
                                    cd.RelativeColumn();
                            });

                            decimal maxV = snap.VentasPorMes.Count > 0
                                ? snap.VentasPorMes.Max(v => v.Total) : 1;
                            if (maxV == 0) maxV = 1;

                            // Cabecera mes
                            foreach (var (mes, _) in snap.VentasPorMes)
                                ventas.Cell().Background("#163770").Padding(4)
                                    .Text(mes).FontColor(Colors.White).Bold().FontSize(8).AlignCenter();

                            // Valor
                            int idx = 0;
                            foreach (var (_, total) in snap.VentasPorMes)
                            {
                                bool esActual = idx == snap.VentasPorMes.Count - 1;
                                string bg = esActual ? "#dce8ff" : "#f5f7ff";
                                var cell = ventas.Cell().Background(bg).Padding(6)
                                    .Text($"$ {total:N0}").FontSize(9).AlignCenter()
                                    .FontColor(esActual ? "#163770" : "#444444");
                                if (esActual) cell.Bold();
                                idx++;
                            }
                        });

                        col.Item().Height(18);

                        // ── Estado de Facturas ──
                        col.Item().Text("Estado de Facturas")
                            .FontSize(11).Bold().FontColor("#163770");
                        col.Item().Height(6);

                        col.Item().Table(estados =>
                        {
                            estados.ColumnsDefinition(cd =>
                            {
                                cd.RelativeColumn(2); cd.RelativeColumn();
                                cd.RelativeColumn(2); cd.RelativeColumn();
                                cd.RelativeColumn(2); cd.RelativeColumn();
                            });

                            estados.Cell().Background("#27ae60").Padding(8)
                                .Text("Saldadas").FontColor(Colors.White).Bold().AlignCenter();
                            estados.Cell().Background("#2ecc71").Padding(8)
                                .Text(snap.Saldadas.ToString()).FontColor(Colors.White)
                                .FontSize(14).Bold().AlignCenter();

                            estados.Cell().Background("#e67e22").Padding(8)
                                .Text("Pendientes").FontColor(Colors.White).Bold().AlignCenter();
                            estados.Cell().Background("#f39c12").Padding(8)
                                .Text(snap.Pendientes.ToString()).FontColor(Colors.White)
                                .FontSize(14).Bold().AlignCenter();

                            estados.Cell().Background("#c0392b").Padding(8)
                                .Text("Canceladas").FontColor(Colors.White).Bold().AlignCenter();
                            estados.Cell().Background("#e74c3c").Padding(8)
                                .Text(snap.Canceladas.ToString()).FontColor(Colors.White)
                                .FontSize(14).Bold().AlignCenter();
                        });

                        col.Item().Height(18);

                        // ── Top Stock ──
                        col.Item().Text("Stock Actual — Top Productos")
                            .FontSize(11).Bold().FontColor("#163770");
                        col.Item().Height(6);

                        col.Item().Table(stock =>
                        {
                            stock.ColumnsDefinition(cd =>
                            {
                                cd.RelativeColumn(4);
                                cd.RelativeColumn(2);
                                cd.RelativeColumn(2);
                                cd.RelativeColumn(2);
                            });

                            stock.Header(h =>
                            {
                                h.Cell().Background("#163770").Padding(4).Text("Producto").FontColor(Colors.White).Bold().FontSize(8);
                                h.Cell().Background("#163770").Padding(4).Text("Existencia").FontColor(Colors.White).Bold().FontSize(8).AlignRight();
                                h.Cell().Background("#163770").Padding(4).Text("Minimo").FontColor(Colors.White).Bold().FontSize(8).AlignRight();
                                h.Cell().Background("#163770").Padding(4).Text("Estado").FontColor(Colors.White).Bold().FontSize(8).AlignCenter();
                            });

                            bool alt = false;
                            foreach (var (nombre, existencia, minimo) in snap.TopStock)
                            {
                                string bg = alt ? "#f5f7ff" : "#ffffff";
                                string estado = existencia == 0 ? "SIN STOCK"
                                    : existencia < minimo ? "BAJO MIN." : "OK";
                                string colorEstado = existencia == 0 ? "#c0392b"
                                    : existencia < minimo ? "#e67e22" : "#27ae60";
                                alt = !alt;

                                stock.Cell().Background(bg).Padding(4).Text(nombre).FontSize(8.5f);
                                stock.Cell().Background(bg).Padding(4).Text(existencia.ToString("N0")).FontSize(8.5f).AlignRight();
                                stock.Cell().Background(bg).Padding(4).Text(minimo.ToString("N0")).FontSize(8.5f).AlignRight();
                                stock.Cell().Background(bg).Padding(4).Text(estado).FontSize(8).AlignCenter().FontColor(colorEstado);
                            }
                        });

                        // Nota
                        col.Item().Height(14);
                        col.Item().Background("#f5f5f5").Padding(8)
                            .Text("Los datos reflejan el estado del sistema al momento de la generacion del reporte. " +
                                  "Ventas del mes incluye todas las facturas del mes en curso.")
                            .FontSize(7.5f).FontColor("#888888").Italic();
                    });

                    page.Footer().Row(row =>
                    {
                        row.RelativeItem()
                            .Text($"SistemaGestion — Dashboard Ejecutivo — {snap.FechaGeneracion:dd/MM/yyyy}")
                            .FontSize(7).FontColor("#aaaaaa");
                        row.ConstantItem(80).AlignRight().Text(x =>
                        {
                            x.Span("Pag. ").FontSize(7).FontColor("#aaaaaa");
                            x.CurrentPageNumber().FontSize(7).FontColor("#aaaaaa");
                            x.Span(" / ").FontSize(7).FontColor("#aaaaaa");
                            x.TotalPages().FontSize(7).FontColor("#aaaaaa");
                        });
                    });
                });
            });

            return doc.GeneratePdf();
        }
    }

    // ── Snapshot del dashboard para el PDF ──────────────────────────────────
    public class DashboardSnapshot
    {
        public DateTime FechaGeneracion { get; set; } = DateTime.Now;
        public string   Usuario         { get; set; } = "";
        public decimal  VentasMes       { get; set; }
        public decimal  VentasHoy       { get; set; }
        public int      FacturasPend    { get; set; }
        public int      StockBajo       { get; set; }
        public int      TotalClientes   { get; set; }
        public string   TopProducto     { get; set; } = "";
        public int      Saldadas        { get; set; }
        public int      Pendientes      { get; set; }
        public int      Canceladas      { get; set; }
        public List<(string Mes, decimal Total)>              VentasPorMes { get; set; } = new();
        public List<(string Nombre, decimal Stock, decimal Min)> TopStock  { get; set; } = new();
    }
}
