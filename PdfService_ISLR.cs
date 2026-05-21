using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System;
using System.Collections.Generic;

namespace SistemaGestion
{
    public static class PdfService_ISLR
    {
        public static void Generar(ResultadoISLR r, List<RetencionISLR> retenciones, string ruta)
        {
            string empresa = DatabaseService.ObtenerConfig("NombreEmpresa", "Mi Empresa");
            string rif     = DatabaseService.ObtenerConfig("RIF", "");

            Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.Letter);
                    page.Margin(2, Unit.Centimetre);
                    page.DefaultTextStyle(x => x.FontSize(9).FontFamily("Arial"));

                    page.Header().Element(c => Header(c, empresa, rif, r.Anio));
                    page.Content().Element(c => Contenido(c, r, retenciones));
                    page.Footer().AlignCenter().Text(t =>
                    {
                        t.Span("Generado: ").FontColor(Colors.Grey.Medium);
                        t.Span(DateTime.Now.ToString("dd/MM/yyyy HH:mm")).FontColor(Colors.Grey.Medium);
                        t.Span("   —   Página ").FontColor(Colors.Grey.Medium);
                        t.CurrentPageNumber();
                        t.Span(" de ").FontColor(Colors.Grey.Medium);
                        t.TotalPages();
                    });
                });
            }).GeneratePdf(ruta);
        }

        private static void Header(IContainer c, string empresa, string rif, int anio)
        {
            c.Column(col =>
            {
                col.Item().BorderBottom(2).BorderColor("#1A4A8A").Padding(4).Row(row =>
                {
                    row.RelativeItem().Column(inner =>
                    {
                        inner.Item().Text(empresa).Bold().FontSize(13).FontColor("#1A4A8A");
                        if (!string.IsNullOrEmpty(rif))
                            inner.Item().Text($"RIF: {rif}").FontSize(8).FontColor(Colors.Grey.Medium);
                    });
                    row.ConstantItem(220).AlignRight().Column(inner =>
                    {
                        inner.Item().Text($"ISLR ESTIMADO — EJERCICIO {anio}")
                            .Bold().FontSize(11).FontColor("#1A4A8A");
                        inner.Item().Text("Tarifa 3 — Persona Jurídica (Art. 52 LISR)")
                            .FontSize(8).FontColor(Colors.Grey.Medium);
                    });
                });
                col.Item().Height(8);
            });
        }

        private static void Contenido(IContainer c, ResultadoISLR r, List<RetencionISLR> rets)
        {
            c.Column(col =>
            {
                // ── Ingresos y costos ────────────────────────────────────
                col.Item().Element(x => Seccion(x, "Ingresos y Costos del Ejercicio"));
                col.Item().Table(t =>
                {
                    t.ColumnsDefinition(d => { d.RelativeColumn(3); d.RelativeColumn(); d.RelativeColumn(); });
                    Th(t, "Concepto"); Th(t, "USD"); Th(t, "Bs");
                    TrD(t, "Ingresos brutos (ventas)", r.IngresosBrutos, r.IngresosBrutos * r.TasaBCV);
                    TrD(t, "Costos de compras",        r.CostosCompras,  r.CostosCompras  * r.TasaBCV, neg: true);
                    TrD(t, "Gastos operativos/egresos",r.CostosGastos,   r.CostosGastos   * r.TasaBCV, neg: true);
                    TrD(t, "Nómina pagada",            r.CostosNomina,   r.CostosNomina   * r.TasaBCV, neg: true);
                    TrTotal(t, "Enriquecimiento Neto", r.EnriquecimientoNeto, r.EnriquecimientoBS,
                        positivo: r.EnriquecimientoNeto >= 0);
                });
                col.Item().Height(10);

                // ── Conversion UT ────────────────────────────────────────
                col.Item().Element(x => Seccion(x, "Conversión a Unidades Tributarias"));
                col.Item().Table(t =>
                {
                    t.ColumnsDefinition(d => { d.RelativeColumn(3); d.RelativeColumn(); });
                    Th(t, "Parámetro"); Th(t, "Valor");
                    TrS(t, $"Tasa BCV ({r.Anio}-12-31 o última disponible)", $"{r.TasaBCV:N2} Bs/USD");
                    TrS(t, "Valor Unidad Tributaria",           $"{r.ValorUT:N2} Bs");
                    TrS(t, "Enriquecimiento Neto en Bs",        $"{r.EnriquecimientoBS:N2} Bs");
                    TrS(t, "Enriquecimiento Neto en UT",        $"{r.EnriquecimientoUT:N2} UT");
                });
                col.Item().Height(10);

                // ── Calculo Tarifa 3 ─────────────────────────────────────
                col.Item().Element(x => Seccion(x, "Cálculo de Impuesto — Tarifa 3 (Art. 52 LISR)"));
                col.Item().Table(t =>
                {
                    t.ColumnsDefinition(d => { d.RelativeColumn(3); d.RelativeColumn(); d.RelativeColumn(); });
                    Th(t, "Tramo"); Th(t, "UT"); Th(t, "Bs");
                    decimal enUT = r.EnriquecimientoUT;
                    if (enUT > 0)
                    {
                        if (enUT <= 2000)
                            TrD(t, $"{enUT:N2} UT × 15%", enUT*0.15m, enUT*0.15m*r.ValorUT);
                        else if (enUT <= 3000)
                        {
                            TrD(t, "2.000 UT × 15%",          300m,              300m*r.ValorUT);
                            TrD(t, $"{enUT-2000:N2} UT × 22%",(enUT-2000)*0.22m,(enUT-2000)*0.22m*r.ValorUT);
                        }
                        else
                        {
                            TrD(t, "2.000 UT × 15%",           300m,                300m*r.ValorUT);
                            TrD(t, "1.000 UT × 22%",           220m,                220m*r.ValorUT);
                            TrD(t, $"{enUT-3000:N2} UT × 34%",(enUT-3000)*0.34m,(enUT-3000)*0.34m*r.ValorUT);
                        }
                    }
                    TrTotal(t, "Impuesto Bruto", r.ImpuestoUT, r.ImpuestoBS, positivo: false);
                });
                col.Item().Height(10);

                // ── Determinacion ISLR ───────────────────────────────────
                col.Item().Element(x => Seccion(x, "Determinación del ISLR a Pagar"));
                col.Item().Table(t =>
                {
                    t.ColumnsDefinition(d => { d.RelativeColumn(3); d.RelativeColumn(); d.RelativeColumn(); });
                    Th(t, "Concepto"); Th(t, "Bs"); Th(t, "USD");
                    TrD2(t, "Impuesto bruto",                  r.ImpuestoBS,   r.ImpuestoUSD);
                    TrD2(t, $"Rebaja Art. 57 ({r.PctRebaja}%)",r.RebajaBS,    r.RebajaBS / (r.TasaBCV>0?r.TasaBCV:1), neg: true);
                    TrD2(t, "Retenciones recibidas",           r.RetencionesBS,r.RetencionesBS/(r.TasaBCV>0?r.TasaBCV:1), neg: true);
                    TrTotal2(t, "ISLR Neto a Pagar",           r.ISLRNetoBS,   r.ISLRNetoUSD,
                        positivo: r.ISLRNetoBS == 0);
                });
                col.Item().Height(10);

                // ── Anticipos ────────────────────────────────────────────
                if (r.AnticipoMensualBS > 0)
                {
                    col.Item().Element(x => Seccion(x, "Anticipos Mensuales (Art. 83 LISR)"));
                    col.Item().Table(t =>
                    {
                        t.ColumnsDefinition(d => { d.RelativeColumn(2); d.RelativeColumn(); d.RelativeColumn(); });
                        Th(t, "Mes"); Th(t, "Vencimiento"); Th(t, "Monto (Bs)");
                        var meses = new[] { "Enero","Febrero","Marzo","Abril","Mayo","Junio",
                            "Julio","Agosto","Septiembre","Octubre","Noviembre","Diciembre" };
                        for (int m = 1; m <= 12; m++)
                            TrS3(t, meses[m-1],
                                new DateTime(r.Anio, m, 15).ToString("dd/MM/yyyy"),
                                r.AnticipoMensualBS.ToString("N2") + " Bs", even: m % 2 == 0);
                    });
                    col.Item().Height(10);
                }

                // ── Retenciones recibidas ────────────────────────────────
                if (rets.Count > 0)
                {
                    col.Item().Element(x => Seccion(x, "Detalle de Retenciones Recibidas"));
                    col.Item().Table(t =>
                    {
                        t.ColumnsDefinition(d =>
                        {
                            d.ConstantColumn(70); d.RelativeColumn(2);
                            d.RelativeColumn(); d.ConstantColumn(70);
                            d.ConstantColumn(40); d.ConstantColumn(80);
                        });
                        Th(t, "Fecha"); Th(t, "Cliente"); Th(t, "Factura");
                        Th(t, "Base (Bs)"); Th(t, "% Ret."); Th(t, "Retenido (Bs)");
                        int idx = 0;
                        foreach (var ret in rets)
                        {
                            TrS3(t, ret.Fecha.ToString("dd/MM/yyyy"), ret.Cliente,
                                ret.NumFactura, even: idx++ % 2 == 0);
                            // columnas adicionales manuales
                        }
                    });
                }

                // ── Nota legal ───────────────────────────────────────────
                col.Item().Height(16);
                col.Item().Border(1).BorderColor(Colors.Grey.Lighten2).Padding(8)
                    .Text("NOTA: Este cálculo es una estimación con fines informativos. " +
                          "La declaración definitiva de ISLR debe ser elaborada y revisada " +
                          "por un contador público colegiado conforme a la LISR vigente y " +
                          "los providencias administrativas del SENIAT.")
                    .FontSize(7.5f).FontColor(Colors.Grey.Medium).Italic();
            });
        }

        // ── Helpers tabla ────────────────────────────────────────────────
        private static void Seccion(IContainer c, string titulo)
        {
            c.BorderBottom(1).BorderColor("#1A4A8A").PaddingBottom(2)
             .Text(titulo).Bold().FontSize(9.5f).FontColor("#1A4A8A");
        }

        private static void Th(TableDescriptor t, string txt)
        {
            t.Cell().Background("#1A4A8A").Padding(4)
             .Text(txt).FontColor(Colors.White).Bold().FontSize(8);
        }

        private static void TrD(TableDescriptor t, string label, decimal v1, decimal v2, bool neg = false)
        {
            t.Cell().Padding(3).Text(label).FontSize(8);
            t.Cell().Padding(3).AlignRight().Text((neg && v1 > 0 ? "- " : "") + v1.ToString("N2")).FontSize(8);
            t.Cell().Padding(3).AlignRight().Text((neg && v2 > 0 ? "- " : "") + v2.ToString("N2")).FontSize(8);
        }

        private static void TrD2(TableDescriptor t, string label, decimal v1, decimal v2, bool neg = false)
        {
            t.Cell().Padding(3).Text(label).FontSize(8);
            t.Cell().Padding(3).AlignRight().Text((neg && v1 > 0 ? "- " : "") + v1.ToString("N2")).FontSize(8);
            t.Cell().Padding(3).AlignRight().Text((neg && v2 > 0 ? "- " : "") + v2.ToString("N2")).FontSize(8);
        }

        private static void TrTotal(TableDescriptor t, string label, decimal v1, decimal v2, bool positivo)
        {
            string color = positivo ? "#0F6E56" : "#C62828";
            t.Cell().Background("#EEF2FA").Padding(4).Text(label).Bold().FontSize(8.5f);
            t.Cell().Background("#EEF2FA").Padding(4).AlignRight()
             .Text(v1.ToString("N2")).Bold().FontSize(8.5f).FontColor(color);
            t.Cell().Background("#EEF2FA").Padding(4).AlignRight()
             .Text(v2.ToString("N2")).Bold().FontSize(8.5f).FontColor(color);
        }

        private static void TrTotal2(TableDescriptor t, string label, decimal v1, decimal v2, bool positivo)
        {
            string color = positivo ? "#0F6E56" : "#C62828";
            t.Cell().Background("#EEF2FA").Padding(4).Text(label).Bold().FontSize(8.5f);
            t.Cell().Background("#EEF2FA").Padding(4).AlignRight()
             .Text(v1.ToString("N2") + " Bs").Bold().FontSize(8.5f).FontColor(color);
            t.Cell().Background("#EEF2FA").Padding(4).AlignRight()
             .Text(v2.ToString("N2") + " USD").Bold().FontSize(8.5f).FontColor(color);
        }

        private static void TrS(TableDescriptor t, string label, string val)
        {
            t.Cell().Padding(3).Text(label).FontSize(8);
            t.Cell().Padding(3).AlignRight().Text(val).FontSize(8);
        }

        private static void TrS3(TableDescriptor t, string c1, string c2, string c3, bool even = false)
        {
            string bg = even ? "#F7F9FC" : Colors.White;
            t.Cell().Background(bg).Padding(3).Text(c1).FontSize(7.5f);
            t.Cell().Background(bg).Padding(3).Text(c2).FontSize(7.5f);
            t.Cell().Background(bg).Padding(3).Text(c3).FontSize(7.5f);
        }
    }
}
