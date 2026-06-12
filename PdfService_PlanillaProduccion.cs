// ════════════════════════════════════════════════════════════════════════
// ARCHIVO: PdfService_PlanillaProduccion.cs
// Genera la planilla imprimible de producción diaria usando QuestPDF.
// Orientación A4 horizontal — para imprimir y rellenar a mano en línea.
// ════════════════════════════════════════════════════════════════════════

using System;
using System.Collections.Generic;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace SistemaGestion
{
    public static partial class PdfService
    {
        /// <summary>
        /// Genera una planilla de producción diaria imprimible.
        /// </summary>
        /// <param name="fecha">Fecha del día.</param>
        /// <param name="moldes">Moldes activos en la línea ese día.</param>
        /// <param name="vueltas">Ciclos estimados (0 = no se muestra la columna).</param>
        /// <param name="tipoMaterial">«Virgen» o «Reciclado».</param>
        public static byte[] GenerarPlanillaProduccion(
            DateTime      fecha,
            List<TbMolde> moldes,
            int           vueltas      = 0,
            string        tipoMaterial = "Virgen")
        {
            bool esVirgen     = tipoMaterial != "Reciclado";
            bool mostrarVuelt = vueltas > 0;

            // Número total de columnas (para la lógica de headers y celdas)
            // Siempre: # + Molde + [Vueltas] + (1ª+2ª | Reciclado) + Defect. + Total + Obs.
            int nCols = 2                               // # + Molde
                      + (mostrarVuelt ? 1 : 0)          // Vueltas
                      + (esVirgen ? 2 : 1)              // 1ª+2ª ó Reciclado
                      + 3;                              // Defect. + Total + Obs.

            var doc = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4.Landscape());
                    page.MarginHorizontal(28);
                    page.MarginVertical(22);
                    page.DefaultTextStyle(x => x.FontSize(9).FontFamily("Arial"));

                    // ── ENCABEZADO ────────────────────────────────────────
                    page.Header().Column(hCol =>
                    {
                        hCol.Item().Row(row =>
                        {
                            row.RelativeItem(3).Column(col =>
                            {
                                col.Item().Text("PLANILLA DE PRODUCCIÓN DIARIA")
                                    .FontSize(15).Bold().FontColor("#1A4A8A");
                                col.Item().Text($"Fecha: {fecha:dd/MM/yyyy}   |   Material: {tipoMaterial}" +
                                    (mostrarVuelt ? $"   |   Vueltas estimadas: {vueltas}" : ""))
                                    .FontSize(10).Bold();
                            });

                            row.RelativeItem(2).Column(col =>
                            {
                                string Linea(string lbl) => lbl + ": ___________________________";
                                col.Item().AlignRight().Text(Linea("Operario")).FontSize(9);
                                col.Item().Height(2);
                                col.Item().AlignRight().Text(Linea("Supervisor")).FontSize(9);
                                col.Item().Height(2);
                                col.Item().AlignRight().Text(Linea("Turno")).FontSize(9);
                            });
                        });
                        hCol.Item().BorderBottom(1).BorderColor("#1A4A8A").PaddingBottom(5);
                    });

                    // ── CUERPO ────────────────────────────────────────────
                    page.Content().PaddingVertical(8).Column(body =>
                    {
                        // ─ Tabla de moldes ────────────────────────────────
                        body.Item().Text("REGISTRO POR MOLDE")
                            .FontSize(10).Bold().FontColor("#1A4A8A");
                        body.Item().Height(3);

                        body.Item().Table(tbl =>
                        {
                            tbl.ColumnsDefinition(cols =>
                            {
                                cols.ConstantColumn(26);            // #
                                cols.RelativeColumn(3f);            // Molde
                                if (mostrarVuelt)
                                    cols.RelativeColumn(1.4f);      // Vueltas
                                if (esVirgen)
                                {
                                    cols.RelativeColumn(1.4f);      // Primera
                                    cols.RelativeColumn(1.4f);      // Segunda
                                }
                                else
                                {
                                    cols.RelativeColumn(1.6f);      // Reciclado
                                }
                                cols.RelativeColumn(1.4f);          // Defect.
                                cols.RelativeColumn(1.4f);          // Total
                                cols.RelativeColumn(2.8f);          // Obs.
                            });

                            // ─ Encabezado de la tabla
                            tbl.Header(h =>
                            {
                                void HC(string t) =>
                                    h.Cell().Background("#1A4A8A").Padding(4)
                                     .AlignCenter().Text(t).Bold().FontColor("#FFFFFF");

                                HC("#");
                                HC("Molde / Descripción");
                                if (mostrarVuelt) HC("Vueltas");
                                if (esVirgen) { HC("1ª Cal."); HC("2ª Cal."); }
                                else            HC("Reciclado");
                                HC("Defect.");
                                HC("Total\nBuenas");
                                HC("Observaciones");
                            });

                            // ─ Filas de datos
                            string[] grises = { "#FFFFFF", "#F0F4FF" };
                            for (int i = 0; i < moldes.Count; i++)
                            {
                                var    m  = moldes[i];
                                string bg = grises[i % 2];

                                // Celda con texto
                                void DC(string t, bool negrita = false)
                                {
                                    var c = tbl.Cell().Background(bg)
                                               .BorderBottom(0.3f).BorderColor("#CCCCCC")
                                               .Padding(3).AlignCenter();
                                    if (negrita) c.Text(t).Bold();
                                    else         c.Text(t);
                                }

                                // Celda vacía para rellenar a mano
                                void BC() =>
                                    tbl.Cell().Background(bg)
                                       .BorderBottom(0.3f).BorderColor("#CCCCCC")
                                       .MinHeight(24).Text("");

                                DC((i + 1).ToString());
                                DC($"{m.CodigoMold}\n{m.Descripcion}", negrita: true);
                                if (mostrarVuelt) BC();
                                if (esVirgen) { BC(); BC(); }
                                else            BC();
                                BC(); // Defect.
                                BC(); // Total
                                BC(); // Obs.
                            }

                            // ─ 4 filas vacías extras para productos adicionales
                            for (int x = 0; x < 4; x++)
                            {
                                string xbg = grises[(moldes.Count + x) % 2];
                                void XC() =>
                                    tbl.Cell().Background(xbg)
                                       .BorderBottom(0.3f).BorderColor("#CCCCCC")
                                       .MinHeight(24).Text("");
                                XC(); // #
                                XC(); // Molde
                                if (mostrarVuelt) XC();
                                if (esVirgen) { XC(); XC(); }
                                else            XC();
                                XC(); // Defect.
                                XC(); // Total
                                XC(); // Obs.
                            }

                            // ─ Fila de totales
                            void TC(string t = "")
                            {
                                tbl.Cell().Background("#E0E8F8")
                                   .BorderTop(0.8f).BorderColor("#1A4A8A")
                                   .Padding(3).AlignCenter().Text(t).Bold();
                            }

                            TC("TOTAL");
                            TC();
                            if (mostrarVuelt) TC();
                            if (esVirgen) { TC(); TC(); }
                            else            TC();
                            TC();
                            TC();
                            TC();
                        });

                        body.Item().Height(10);

                        // ─ Tabla de materiales ────────────────────────────
                        body.Item().Text("MATERIAL UTILIZADO  (1 saco = 25 kg)")
                            .FontSize(10).Bold().FontColor("#643C14");
                        body.Item().Height(3);

                        body.Item().Table(tbl =>
                        {
                            tbl.ColumnsDefinition(cols =>
                            {
                                cols.RelativeColumn(3);   // Insumo
                                cols.RelativeColumn(1.5f); // Sacos
                                cols.RelativeColumn(1.5f); // Kg
                                cols.RelativeColumn(4);   // Lote / Obs.
                            });

                            tbl.Header(h =>
                            {
                                void MH(string t) =>
                                    h.Cell().Background("#643C14").Padding(4)
                                     .AlignCenter().Text(t).Bold().FontColor("#FFFFFF");
                                MH("Insumo / Resina");
                                MH("Sacos");
                                MH("Kg  (× 25)");
                                MH("Lote / Observaciones");
                            });

                            string[] insumos = { "Soplado (6200B)", "Tobo (2706)", "Otro" };
                            string[] matBg   = { "#FFFFFF", "#FFF6EE", "#FFFFFF" };

                            for (int i = 0; i < insumos.Length; i++)
                            {
                                string bg = matBg[i];
                                tbl.Cell().Background(bg).BorderBottom(0.3f)
                                   .BorderColor("#CCCCCC").Padding(4)
                                   .Text(insumos[i]).Bold();
                                tbl.Cell().Background(bg).BorderBottom(0.3f)
                                   .BorderColor("#CCCCCC").MinHeight(24).Text("");
                                tbl.Cell().Background(bg).BorderBottom(0.3f)
                                   .BorderColor("#CCCCCC").MinHeight(24).Text("");
                                tbl.Cell().Background(bg).BorderBottom(0.3f)
                                   .BorderColor("#CCCCCC").MinHeight(24).Text("");
                            }
                        });

                        body.Item().Height(10);

                        // ─ Observaciones generales ────────────────────────
                        body.Item().Text("OBSERVACIONES GENERALES")
                            .FontSize(10).Bold();
                        body.Item().Height(3);
                        body.Item().Border(0.5f).BorderColor("#AAAAAA")
                            .MinHeight(44).Padding(4).Text("");
                    });

                    // ── PIE ───────────────────────────────────────────────
                    page.Footer().BorderTop(0.5f).BorderColor("#CCCCCC").PaddingTop(4).Row(row =>
                    {
                        row.RelativeItem().Text(
                            $"Impresa: {DateTime.Now:dd/MM/yyyy HH:mm}")
                            .FontSize(7.5f).FontColor("#888888");
                        row.RelativeItem().AlignCenter().Text("Sistema de Gestión — Producción")
                            .FontSize(7.5f).FontColor("#888888");
                        row.RelativeItem().AlignRight()
                            .Text("Firma y sello: ____________________")
                            .FontSize(7.5f).FontColor("#888888");
                    });
                });
            });

            return doc.GeneratePdf();
        }
    }
}
