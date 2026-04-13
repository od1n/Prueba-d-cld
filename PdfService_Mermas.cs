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
        public static byte[] GenerarReporteMermas(
            List<ResumenMerma> mermas,
            DateTime desde, DateTime hasta)
        {
            decimal totalMerma   = mermas.Sum(m => m.MermaTotal);
            decimal totalTeorico = mermas.Sum(m => m.TeoricoTotal);
            decimal pctGlobal    = totalTeorico > 0 ? totalMerma / totalTeorico : 0;
            int     nCriticas    = mermas.Count(m => m.PctMermaTotal > 0.15m);

            var doc = Document.Create(c =>
            {
                c.Page(page =>
                {
                    page.Size(PageSizes.A4.Landscape());
                    page.Margin(22);
                    page.DefaultTextStyle(x => x.FontFamily("Arial").FontSize(8));

                    // HEADER
                    page.Header().Column(col =>
                    {
                        col.Item().Row(row =>
                        {
                            row.RelativeItem().Column(inner =>
                            {
                                inner.Item().Text("REPORTE DE MERMAS Y DESPERDICIOS")
                                    .FontSize(14).Bold().FontColor("#64320a");
                                inner.Item().Text(
                                    $"Periodo: {desde:dd/MM/yyyy} al {hasta:dd/MM/yyyy}  —  " +
                                    $"Generado: {DateTime.Now:dd/MM/yyyy HH:mm}")
                                    .FontSize(8).FontColor("#666666");
                            });
                        });
                        col.Item().Height(6);
                        col.Item().LineHorizontal(2f).LineColor("#64320a");
                        col.Item().Height(6);
                    });

                    page.Content().Column(col =>
                    {
                        // RESUMEN GLOBAL
                        col.Item().Table(resumen =>
                        {
                            resumen.ColumnsDefinition(cd =>
                            {
                                cd.RelativeColumn(); cd.RelativeColumn();
                                cd.RelativeColumn(); cd.RelativeColumn();
                            });
                            resumen.Header(h =>
                            {
                                h.Cell().ColumnSpan(4).Background("#64320a").Padding(5)
                                    .Text("RESUMEN GLOBAL").FontColor(Colors.White).Bold()
                                    .FontSize(10).AlignCenter();
                            });

                            void Kpi(string lbl, string val, string bg = "#ffffff")
                            {
                                resumen.Cell().Background(bg).Padding(6)
                                    .Text(lbl).FontSize(7.5f).FontColor("#666666");
                                resumen.Cell().Background(bg).Padding(6)
                                    .Text(val).FontSize(10).Bold().AlignRight();
                            }

                            Kpi("Ordenes Analizadas:", mermas.Count.ToString(), "#f0f4ff");
                            Kpi("Ordenes Criticas (>15%):", nCriticas.ToString(),
                                nCriticas > 0 ? "#fff0f0" : "#f0fff0");
                            Kpi("Merma Total:", totalMerma.ToString("N2"), "#fff8f0");
                            Kpi("% Merma Global:",
                                pctGlobal.ToString("P2"),
                                pctGlobal > 0.10m ? "#fff0f0" :
                                pctGlobal > 0.05m ? "#fffbf0" : "#f0fff0");
                        });

                        col.Item().Height(12);

                        // TABLA POR ORDEN
                        col.Item().Text("Detalle por Orden de Produccion")
                            .FontSize(10).Bold().FontColor("#64320a");
                        col.Item().Height(4);

                        foreach (var op in mermas.OrderByDescending(m => m.PctMermaTotal))
                        {
                            string headerColor = op.PctMermaTotal > 0.15m ? "#c0392b"
                                               : op.PctMermaTotal > 0.05m ? "#e67e22"
                                               : "#27ae60";

                            col.Item().Table(tabla =>
                            {
                                tabla.ColumnsDefinition(cd =>
                                {
                                    cd.ConstantColumn(80);  // N° Orden
                                    cd.RelativeColumn(3);   // Producto
                                    cd.ConstantColumn(70);  // Fecha
                                    cd.ConstantColumn(55);  // Plan
                                    cd.ConstantColumn(55);  // Real
                                    cd.ConstantColumn(75);  // Teorico
                                    cd.ConstantColumn(75);  // Real mat
                                    cd.ConstantColumn(65);  // Merma
                                    cd.ConstantColumn(55);  // %
                                    cd.ConstantColumn(55);  // Nivel
                                });

                                // Header orden
                                tabla.Header(h =>
                                {
                                    void Hdr(string t, bool right = false)
                                    {
                                        var cell = h.Cell().Background(headerColor).Padding(4)
                                            .Text(t).FontColor(Colors.White).Bold().FontSize(7.5f);
                                        if (right) cell.AlignRight();
                                    }
                                    Hdr("N° Orden"); Hdr("Producto"); Hdr("Cierre");
                                    Hdr("Cant Plan", true); Hdr("Cant Real", true);
                                    Hdr("Teo. MP", true); Hdr("Real MP", true);
                                    Hdr("Merma", true); Hdr("% Merma", true); Hdr("Nivel");
                                });

                                // Fila resumen de la orden
                                string bgOp = op.PctMermaTotal > 0.15m ? "#ffeaea"
                                            : op.PctMermaTotal > 0.05m ? "#fff8ee"
                                            : "#eeffee";

                                void Cell(string t, bool right = false, bool bold = false)
                                {
                                    var cell = tabla.Cell().Background(bgOp).Padding(3)
                                        .Text(t).FontSize(8);
                                    if (right) cell.AlignRight();
                                    if (bold) cell.Bold();
                                }

                                Cell(op.NumOrden, bold: true);
                                Cell($"{op.ProductoCod} — {op.ProductoDesc}");
                                Cell(op.FechaFin.ToString("dd/MM/yy"));
                                Cell(op.CantidadPlan.ToString("N1"), right: true);
                                Cell(op.CantidadReal.ToString("N1"), right: true);
                                Cell(op.TeoricoTotal.ToString("N2"), right: true);
                                Cell((op.TeoricoTotal - op.MermaTotal).ToString("N2"), right: true);
                                Cell(op.MermaTotal.ToString("N2"), right: true, bold: true);
                                Cell(op.PctMermaTotal.ToString("P1"), right: true, bold: true);
                                Cell(op.PctMermaTotal > 0.15m ? "CRITICO"
                                   : op.PctMermaTotal > 0.05m ? "ALTO" : "NORMAL");

                                // Filas de materias primas
                                foreach (var l in op.Lineas.OrderByDescending(x => x.PctMerma))
                                {
                                    string bgL = l.Nivel == "CRITICO" ? "#fff5f5"
                                               : l.Nivel == "ALTO"    ? "#fffbf0"
                                               : "#ffffff";

                                    tabla.Cell().Background(bgL).Padding(3)
                                        .Text("  " + l.MateriaCod).FontSize(7.5f).FontColor("#888888");
                                    tabla.Cell().Background(bgL).Padding(3)
                                        .Text("  └ " + l.MateriaDesc).FontSize(7.5f).FontColor("#888888");
                                    tabla.Cell().Background(bgL).Padding(3).Text("").FontSize(7);
                                    tabla.Cell().Background(bgL).Padding(3).Text("").FontSize(7);
                                    tabla.Cell().Background(bgL).Padding(3).Text("").FontSize(7);
                                    tabla.Cell().Background(bgL).Padding(3)
                                        .Text(l.ConsumoTeorico.ToString("N2")).FontSize(7.5f).AlignRight();
                                    tabla.Cell().Background(bgL).Padding(3)
                                        .Text(l.ConsumoReal.ToString("N2")).FontSize(7.5f).AlignRight();
                                    tabla.Cell().Background(bgL).Padding(3)
                                        .Text(l.Merma.ToString("N2")).FontSize(7.5f).AlignRight();
                                    tabla.Cell().Background(bgL).Padding(3)
                                        .Text(l.PctMerma.ToString("P1")).FontSize(7.5f).AlignRight();
                                    tabla.Cell().Background(bgL).Padding(3)
                                        .Text(l.Nivel).FontSize(7.5f)
                                        .FontColor(l.Nivel == "CRITICO" ? "#c0392b"
                                                 : l.Nivel == "ALTO"    ? "#e67e22" : "#27ae60");
                                }
                            });

                            col.Item().Height(6);
                        }
                    });

                    // FOOTER
                    page.Footer().Row(row =>
                    {
                        row.RelativeItem().Text($"SistemaGestion — Reporte de Mermas {desde:dd/MM/yyyy} al {hasta:dd/MM/yyyy}")
                            .FontSize(7).FontColor("#aaaaaa");
                        row.ConstantItem(80).AlignRight().Text(x =>
                        {
                            x.Span("Pagina ").FontSize(7).FontColor("#aaaaaa");
                            x.CurrentPageNumber().FontSize(7).FontColor("#aaaaaa");
                            x.Span(" de ").FontSize(7).FontColor("#aaaaaa");
                            x.TotalPages().FontSize(7).FontColor("#aaaaaa");
                        });
                    });
                });
            });

            return doc.GeneratePdf();
        }
    }
}
