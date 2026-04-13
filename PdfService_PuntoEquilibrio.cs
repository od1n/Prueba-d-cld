using System;
using System.Collections.Generic;
using System.Linq;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace SistemaGestion
{
    /// <summary>
    /// Metodo GenerarPuntoEquilibrio — agregar como partial de PdfService en PdfService.cs
    /// </summary>
    public static partial class PdfService
    {
        public static byte[] GenerarPuntoEquilibrio(
            List<DatoPE> datos,
            ResultadoPE resultado,
            int meses)
        {
            string periodo = meses == 1 ? "ultimo mes"
                           : meses == 3 ? "ultimos 3 meses"
                           : meses == 6 ? "ultimos 6 meses"
                           : "ultimo anio";

            var doc = Document.Create(c =>
            {
                c.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(28);
                    page.DefaultTextStyle(x => x.FontFamily("Arial").FontSize(9));

                    // ---- HEADER ----
                    page.Header().Column(col =>
                    {
                        col.Item().Row(row =>
                        {
                            row.RelativeItem().Column(inner =>
                            {
                                inner.Item()
                                    .Text("ANALISIS DE PUNTO DE EQUILIBRIO")
                                    .FontSize(14).Bold().FontColor("#1a3a5c");
                                inner.Item()
                                    .Text($"Periodo: {periodo} — Generado: {DateTime.Now:dd/MM/yyyy HH:mm}")
                                    .FontSize(8).FontColor("#666666");
                            });
                        });
                        col.Item().Height(6);
                        col.Item().LineHorizontal(1.5f).LineColor("#1a3a5c");
                        col.Item().Height(8);
                    });

                    page.Content().Column(col =>
                    {
                        // ---- METRICAS CLAVE ----
                        col.Item().Text("Metricas Clave").FontSize(11).Bold().FontColor("#1a3a5c");
                        col.Item().Height(6);

                        col.Item().Table(table =>
                        {
                            table.ColumnsDefinition(c =>
                            {
                                c.RelativeColumn(3);
                                c.RelativeColumn(2);
                            });

                            // Header
                            table.Header(h =>
                            {
                                h.Cell().Background("#1a3a5c").Padding(6)
                                    .Text("Metrica").FontColor(Colors.White).Bold().FontSize(9);
                                h.Cell().Background("#1a3a5c").Padding(6)
                                    .Text("Valor").FontColor(Colors.White).Bold().FontSize(9)
                                    .AlignRight();
                            });

                            var metricas = new (string Label, string Valor, string Bg)[]
                            {
                                ("Ingresos Mensuales Promedio",   $"$ {resultado.IngresosMensuales:N2}", "#f0f9f4"),
                                ("Costos Variables (mensuales)",  $"$ {resultado.CostosVariables:N2}",  "#fff8f0"),
                                ("Costos Fijos (mensuales)",      $"$ {resultado.CostosFijos:N2}",      "#fff0f0"),
                                ("Costos Totales",                $"$ {resultado.CostosTotales:N2}",    "#fafafa"),
                                ("Margen de Contribucion",        $"{resultado.MargenContribucion:P2}", "#f5f0ff"),
                                ("PUNTO DE EQUILIBRIO",           $"$ {resultado.PuntoEquilibrio:N2}",  "#e8f4fd"),
                                ("Margen de Seguridad",           $"{resultado.MargenSeguridad:P2}",    "#f0f9f4"),
                                ("Utilidad / Perdida Operacional",$"$ {resultado.UtilidadOperacional:N2}",
                                    resultado.UtilidadOperacional >= 0 ? "#f0f9f4" : "#fff0f0"),
                            };

                            bool alt = false;
                            foreach (var (label, valor, bg) in metricas)
                            {
                                bool isPE = label.StartsWith("PUNTO");
                                var cellBg = isPE ? "#dbeeff" : (alt ? "#fafafa" : Colors.White);
                                alt = !alt;

                                var cell1 = table.Cell().Background(cellBg).Padding(6).Text(label).FontSize(isPE ? 10 : 9);
                                if (isPE) cell1.Bold();
                                var cell2 = table.Cell().Background(cellBg).Padding(6).Text(valor).FontSize(isPE ? 10 : 9).AlignRight();
                                if (isPE) cell2.Bold();
                            }
                        });

                        col.Item().Height(18);

                        // ---- DETALLE DE COSTOS FIJOS ----
                        var fijos = datos.Where(d => d.Tipo == "Fijo").OrderByDescending(d => d.MontoMensual).ToList();
                        if (fijos.Any())
                        {
                            col.Item().Text("Detalle de Costos Fijos").FontSize(11).Bold().FontColor("#1a3a5c");
                            col.Item().Height(6);
                            col.Item().Table(table =>
                            {
                                table.ColumnsDefinition(c =>
                                {
                                    c.RelativeColumn(4);
                                    c.RelativeColumn(2);
                                    c.RelativeColumn(1);
                                });
                                table.Header(h =>
                                {
                                    h.Cell().Background("#c0392b").Padding(5).Text("Concepto").FontColor(Colors.White).Bold().FontSize(9);
                                    h.Cell().Background("#c0392b").Padding(5).Text("Monto/Mes (USD)").FontColor(Colors.White).Bold().FontSize(9).AlignRight();
                                    h.Cell().Background("#c0392b").Padding(5).Text("Origen").FontColor(Colors.White).Bold().FontSize(9).AlignCenter();
                                });
                                bool alt2 = false;
                                foreach (var d in fijos)
                                {
                                    var bg = alt2 ? "#fff5f5" : "#ffffff";
                                    alt2 = !alt2;
                                    table.Cell().Background(bg).Padding(5).Text(d.Concepto).FontSize(9);
                                    table.Cell().Background(bg).Padding(5).Text($"$ {d.MontoMensual:N2}").FontSize(9).AlignRight();
                                    table.Cell().Background(bg).Padding(5).Text(d.Origen).FontSize(8).AlignCenter();
                                }
                                // Total
                                table.Cell().Background("#ffd5d5").Padding(5).Text("TOTAL COSTOS FIJOS").Bold().FontSize(9);
                                table.Cell().Background("#ffd5d5").Padding(5).Text($"$ {resultado.CostosFijos:N2}").Bold().FontSize(9).AlignRight();
                                table.Cell().Background("#ffd5d5").Padding(5).Text("").FontSize(9);
                            });
                            col.Item().Height(14);
                        }

                        // ---- DETALLE DE COSTOS VARIABLES ----
                        var variables = datos.Where(d => d.Tipo == "Variable").OrderByDescending(d => d.MontoMensual).ToList();
                        if (variables.Any())
                        {
                            col.Item().Text("Detalle de Costos Variables").FontSize(11).Bold().FontColor("#1a3a5c");
                            col.Item().Height(6);
                            col.Item().Table(table =>
                            {
                                table.ColumnsDefinition(c =>
                                {
                                    c.RelativeColumn(4);
                                    c.RelativeColumn(2);
                                    c.RelativeColumn(1);
                                });
                                table.Header(h =>
                                {
                                    h.Cell().Background("#e67e22").Padding(5).Text("Concepto").FontColor(Colors.White).Bold().FontSize(9);
                                    h.Cell().Background("#e67e22").Padding(5).Text("Monto/Mes (USD)").FontColor(Colors.White).Bold().FontSize(9).AlignRight();
                                    h.Cell().Background("#e67e22").Padding(5).Text("Origen").FontColor(Colors.White).Bold().FontSize(9).AlignCenter();
                                });
                                bool alt2 = false;
                                foreach (var d in variables)
                                {
                                    var bg = alt2 ? "#fff8f0" : "#ffffff";
                                    alt2 = !alt2;
                                    table.Cell().Background(bg).Padding(5).Text(d.Concepto).FontSize(9);
                                    table.Cell().Background(bg).Padding(5).Text($"$ {d.MontoMensual:N2}").FontSize(9).AlignRight();
                                    table.Cell().Background(bg).Padding(5).Text(d.Origen).FontSize(8).AlignCenter();
                                }
                                table.Cell().Background("#ffe5c0").Padding(5).Text("TOTAL COSTOS VARIABLES").Bold().FontSize(9);
                                table.Cell().Background("#ffe5c0").Padding(5).Text($"$ {resultado.CostosVariables:N2}").Bold().FontSize(9).AlignRight();
                                table.Cell().Background("#ffe5c0").Padding(5).Text("").FontSize(9);
                            });
                            col.Item().Height(14);
                        }

                        // ---- NOTA METODOLOGICA ----
                        col.Item().Background("#f5f5f5").Padding(10).Column(nota =>
                        {
                            nota.Item().Text("Metodologia").Bold().FontSize(9).FontColor("#444444");
                            nota.Item().Height(4);
                            nota.Item().Text(
                                "PE = Costos Fijos / Margen de Contribucion\n" +
                                "Margen de Contribucion = (Ingresos - Costos Variables) / Ingresos\n" +
                                "Margen de Seguridad = (Ventas - PE) / Ventas\n" +
                                "Los datos automaticos corresponden al periodo seleccionado.\n" +
                                "Los conceptos manuales son ajustes ingresados por el usuario."
                            ).FontSize(8).FontColor("#666666").LineHeight(1.4f);
                        });
                    });

                    // ---- FOOTER ----
                    page.Footer().Row(row =>
                    {
                        row.RelativeItem().Text($"SistemaGestion — {DateTime.Now:dd/MM/yyyy HH:mm}").FontSize(7).FontColor("#999999");
                        row.ConstantItem(60).AlignRight().Text(x =>
                        {
                            x.Span("Pagina ").FontSize(7).FontColor("#999999");
                            x.CurrentPageNumber().FontSize(7).FontColor("#999999");
                            x.Span(" de ").FontSize(7).FontColor("#999999");
                            x.TotalPages().FontSize(7).FontColor("#999999");
                        });
                    });
                });
            });

            return doc.GeneratePdf();
        }
    }
}
