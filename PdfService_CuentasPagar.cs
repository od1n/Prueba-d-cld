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
        public static byte[] GenerarReporteCxP(List<CuentaPagar> lista)
        {
            decimal totalMonto   = lista.Sum(x => x.Monto);
            decimal totalPagado  = lista.Sum(x => x.Pagado);
            decimal totalSaldo   = lista.Sum(x => x.Saldo);
            int     nVencidas    = lista.Count(x => x.Estado == "Vencida");

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
                                inner.Item().Text("REPORTE DE CUENTAS POR PAGAR")
                                    .FontSize(14).Bold().FontColor("#64001e");
                                inner.Item().Text($"Generado: {DateTime.Now:dd/MM/yyyy HH:mm}")
                                    .FontSize(8).FontColor("#666666");
                            });
                        });
                        col.Item().Height(6);
                        col.Item().LineHorizontal(2f).LineColor("#64001e");
                        col.Item().Height(6);
                    });

                    page.Content().Column(col =>
                    {
                        // RESUMEN
                        col.Item().Table(resumen =>
                        {
                            resumen.ColumnsDefinition(cd =>
                            {
                                cd.RelativeColumn(); cd.RelativeColumn();
                                cd.RelativeColumn(); cd.RelativeColumn();
                            });
                            resumen.Header(h =>
                                h.Cell().ColumnSpan(4).Background("#64001e").Padding(5)
                                    .Text("RESUMEN").FontColor(Colors.White).Bold().FontSize(10).AlignCenter());

                            void Kpi(string l, string v, string bg = "#ffffff")
                            {
                                resumen.Cell().Background(bg).Padding(6)
                                    .Text(l).FontSize(7.5f).FontColor("#666666");
                                resumen.Cell().Background(bg).Padding(6)
                                    .Text(v).FontSize(10).Bold().AlignRight();
                            }
                            Kpi("Total Facturas:", $"$ {totalMonto:N2}", "#f5f0ff");
                            Kpi("Total Pagado:", $"$ {totalPagado:N2}", "#f0fff0");
                            Kpi("Saldo Pendiente:", $"$ {totalSaldo:N2}", "#fff0f0");
                            Kpi("Cuentas Vencidas:", nVencidas.ToString(),
                                nVencidas > 0 ? "#fff0f0" : "#f0fff0");
                        });

                        col.Item().Height(12);

                        // TABLA DETALLE
                        col.Item().Table(tabla =>
                        {
                            tabla.ColumnsDefinition(cd =>
                            {
                                cd.RelativeColumn(3);  // Proveedor
                                cd.RelativeColumn(3);  // Concepto
                                cd.ConstantColumn(70); // N° Compra
                                cd.ConstantColumn(65); // Emision
                                cd.ConstantColumn(65); // Vence
                                cd.ConstantColumn(80); // Monto
                                cd.ConstantColumn(80); // Pagado
                                cd.ConstantColumn(80); // Saldo
                                cd.ConstantColumn(60); // Estado
                            });

                            tabla.Header(h =>
                            {
                                void Hdr(string t, bool right = false)
                                {
                                    var cell = h.Cell().Background("#64001e").Padding(4)
                                        .Text(t).FontColor(Colors.White).Bold().FontSize(8);
                                    if (right) cell.AlignRight();
                                }
                                Hdr("Proveedor"); Hdr("Concepto"); Hdr("N° Compra");
                                Hdr("Emision"); Hdr("Vence");
                                Hdr("Monto", true); Hdr("Pagado", true);
                                Hdr("Saldo", true); Hdr("Estado");
                            });

                            bool alt = false;
                            foreach (var item in lista.OrderBy(x => x.FechaVence))
                            {
                                string bg = item.Estado == "Vencida"  ? "#ffeaea"
                                          : item.Estado == "Pagada"   ? "#efffef"
                                          : item.Estado == "Parcial"  ? "#fffbee"
                                          : (alt ? "#f9f9f9" : "#ffffff");
                                alt = !alt;

                                void Cell(string t, bool right = false)
                                {
                                    var cell = tabla.Cell().Background(bg).Padding(3).Text(t).FontSize(8);
                                    if (right) cell.AlignRight();
                                }

                                Cell(item.Proveedor);
                                Cell(item.Concepto);
                                Cell(item.NumFactura);
                                Cell(item.FechaEmision.ToString("dd/MM/yy"));
                                Cell(item.FechaVence.ToString("dd/MM/yy"));
                                Cell($"$ {item.Monto:N2}", right: true);
                                Cell($"$ {item.Pagado:N2}", right: true);
                                Cell($"$ {item.Saldo:N2}", right: true);
                                Cell(item.Estado);
                            }

                            // Totales
                            tabla.Cell().ColumnSpan(5).Background("#e8d0f0").Padding(4)
                                .Text("TOTALES").Bold().FontSize(9).AlignRight();
                            tabla.Cell().Background("#e8d0f0").Padding(4)
                                .Text($"$ {totalMonto:N2}").Bold().FontSize(9).AlignRight();
                            tabla.Cell().Background("#e8d0f0").Padding(4)
                                .Text($"$ {totalPagado:N2}").Bold().FontSize(9).AlignRight();
                            tabla.Cell().Background("#e8d0f0").Padding(4)
                                .Text($"$ {totalSaldo:N2}").Bold().FontSize(9).AlignRight();
                            tabla.Cell().Background("#e8d0f0").Padding(4).Text("").FontSize(9);
                        });
                    });

                    page.Footer().Row(row =>
                    {
                        row.RelativeItem().Text($"SistemaGestion — Cuentas por Pagar — {DateTime.Now:dd/MM/yyyy}")
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
