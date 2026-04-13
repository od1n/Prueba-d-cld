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
        public static byte[] GenerarLibroIVA(
            List<FilaLibroIVA> lista,
            bool esVentas,
            int mes, int anio,
            string nombreMes)
        {
            string tipoLibro  = esVentas ? "LIBRO DE VENTAS" : "LIBRO DE COMPRAS";
            string colorHex   = esVentas ? "#8c1414" : "#641450";
            string colNombre  = esVentas ? "Cliente" : "Proveedor";

            decimal totalBase = lista.Sum(x => x.BaseImponible);
            decimal totalIVA  = lista.Sum(x => x.MontoIVA);
            decimal totalGral = lista.Sum(x => x.Total);

            var doc = Document.Create(c =>
            {
                c.Page(page =>
                {
                    page.Size(PageSizes.A4.Landscape());
                    page.Margin(22);
                    page.DefaultTextStyle(x => x.FontFamily("Arial").FontSize(8));

                    // ---- HEADER ----
                    page.Header().Column(col =>
                    {
                        col.Item().Row(row =>
                        {
                            row.RelativeItem().Column(inner =>
                            {
                                inner.Item()
                                    .Text(tipoLibro)
                                    .FontSize(14).Bold().FontColor(colorHex);
                                inner.Item()
                                    .Text($"Periodo: {nombreMes} {anio}  —  Generado: {DateTime.Now:dd/MM/yyyy HH:mm}")
                                    .FontSize(8).FontColor("#666666");
                                inner.Item().Height(2);
                                inner.Item()
                                    .Text("Republica Bolivariana de Venezuela — SENIAT")
                                    .FontSize(7.5f).FontColor("#888888").Italic();
                            });
                        });
                        col.Item().Height(6);
                        col.Item().LineHorizontal(2f).LineColor(colorHex);
                        col.Item().Height(6);
                    });

                    // ---- CONTENIDO ----
                    page.Content().Column(col =>
                    {
                        col.Item().Table(table =>
                        {
                            table.ColumnsDefinition(cd =>
                            {
                                cd.ConstantColumn(22);   // N°
                                cd.ConstantColumn(62);   // Fecha
                                cd.ConstantColumn(80);   // N° Doc
                                cd.RelativeColumn(3);    // Nombre
                                cd.ConstantColumn(70);   // Forma Pago
                                cd.ConstantColumn(90);   // Base Imponible
                                cd.ConstantColumn(80);   // IVA
                                cd.ConstantColumn(90);   // Total
                            });

                            // Header tabla
                            table.Header(h =>
                            {
                                void Hdr(string txt, bool right = false)
                                {
                                    var t = h.Cell().Background(colorHex).Padding(4)
                                        .Text(txt).FontColor(Colors.White).Bold().FontSize(8);
                                    if (right) t.AlignRight();
                                }

                                Hdr("N°");
                                Hdr("Fecha");
                                Hdr("N° Documento");
                                Hdr(colNombre);
                                Hdr("Forma Pago");
                                Hdr("Base Imponible", true);
                                Hdr("IVA (USD)", true);
                                Hdr("Total (USD)", true);
                            });

                            // Filas
                            bool alt = false;
                            int n = 1;
                            foreach (var f in lista)
                            {
                                string bg = alt ? "#f5f0f8" : "#ffffff";
                                if (esVentas) bg = alt ? "#fdf0f0" : "#ffffff";
                                alt = !alt;

                                void Cell(string txt, bool right = false, bool bold = false)
                                {
                                    var cell = table.Cell().Background(bg).Padding(3).Text(txt).FontSize(8);
                                    if (right) cell.AlignRight();
                                    if (bold) cell.Bold();
                                }

                                Cell(n++.ToString());
                                Cell(f.Fecha.ToString("dd/MM/yyyy"));
                                Cell(f.NumeroDoc);
                                Cell(f.Nombre);
                                Cell(f.FormaPago);
                                Cell($"{f.BaseImponible:N2}", right: true);
                                Cell($"{f.MontoIVA:N2}", right: true);
                                Cell($"{f.Total:N2}", right: true);
                            }

                            // Fila totales
                            string bgTot = esVentas ? "#f5d0d0" : "#e8d0f0";
                            table.Cell().ColumnSpan(5).Background(bgTot).Padding(4)
                                .Text("TOTALES DEL PERIODO").Bold().FontSize(9).AlignRight();
                            table.Cell().Background(bgTot).Padding(4)
                                .Text($"{totalBase:N2}").Bold().FontSize(9).AlignRight();
                            table.Cell().Background(bgTot).Padding(4)
                                .Text($"{totalIVA:N2}").Bold().FontSize(9).AlignRight();
                            table.Cell().Background(bgTot).Padding(4)
                                .Text($"{totalGral:N2}").Bold().FontSize(9).AlignRight();
                        });

                        // ---- RESUMEN ----
                        col.Item().Height(14);
                        col.Item().Table(resumen =>
                        {
                            resumen.ColumnsDefinition(cd =>
                            {
                                cd.RelativeColumn(2);
                                cd.RelativeColumn(1);
                                cd.RelativeColumn(2);
                                cd.RelativeColumn(1);
                                cd.RelativeColumn(2);
                                cd.RelativeColumn(1);
                            });

                            resumen.Header(h =>
                            {
                                h.Cell().ColumnSpan(6).Background(colorHex).Padding(5)
                                    .Text("RESUMEN DEL PERIODO").FontColor(Colors.White).Bold().FontSize(9).AlignCenter();
                            });

                            void FilaResumen(string lbl, string val)
                            {
                                resumen.Cell().Background("#f5f5f5").Padding(5)
                                    .Text(lbl).FontSize(8).FontColor("#555555");
                                resumen.Cell().Background("#ffffff").Padding(5)
                                    .Text(val).FontSize(9).Bold().AlignRight();
                            }

                            FilaResumen("N° de Operaciones:", lista.Count.ToString());
                            FilaResumen("Base Imponible Total:", $"$ {totalBase:N2}");
                            FilaResumen("IVA Total:", $"$ {totalIVA:N2}");
                            FilaResumen("Total General:", $"$ {totalGral:N2}");
                            FilaResumen("Mes / Anio:", $"{nombreMes} {anio}");
                            FilaResumen("Tasa IVA Promedio:",
                                totalBase > 0 ? $"{(totalIVA / totalBase * 100):N2} %" : "N/A");
                        });

                        // ---- FIRMA ----
                        col.Item().Height(28);
                        col.Item().Row(row =>
                        {
                            row.RelativeItem().Column(f =>
                            {
                                f.Item().LineHorizontal(0.5f).LineColor("#aaaaaa");
                                f.Item().Height(4);
                                f.Item().Text("Firma del Responsable").FontSize(7.5f).FontColor("#888888").AlignCenter();
                            });
                            row.ConstantItem(40);
                            row.RelativeItem().Column(f =>
                            {
                                f.Item().LineHorizontal(0.5f).LineColor("#aaaaaa");
                                f.Item().Height(4);
                                f.Item().Text("Sello de la Empresa").FontSize(7.5f).FontColor("#888888").AlignCenter();
                            });
                            row.ConstantItem(40);
                            row.RelativeItem().Column(f =>
                            {
                                f.Item().LineHorizontal(0.5f).LineColor("#aaaaaa");
                                f.Item().Height(4);
                                f.Item().Text("Fecha").FontSize(7.5f).FontColor("#888888").AlignCenter();
                            });
                        });
                    });

                    // ---- FOOTER ----
                    page.Footer().Row(row =>
                    {
                        row.RelativeItem()
                            .Text($"SistemaGestion — Libro IVA {(esVentas ? "Ventas" : "Compras")} {nombreMes}/{anio}")
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
