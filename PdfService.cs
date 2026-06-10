using System;
using System.Collections.Generic;
using System.IO;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace SistemaGestion
{
    /// <summary>
    /// Genera PDFs de facturas usando QuestPDF (licencia Community — gratuita).
    /// </summary>
    public static partial class PdfService
    {
        // ── Configuracion global de QuestPDF ──────────────────────────────
        public static void Inicializar()
        {
            QuestPDF.Settings.License = LicenseType.Community;
        }

        // ═════════════════════════════════════════════════════════════════
        //  GENERAR PDF DE FACTURA
        // ═════════════════════════════════════════════════════════════════
        public static byte[] GenerarFactura(
            Factura factura,
            List<LineaFactura> lineas,
            string nombreEmpresa = "EL HATO PASCHER C.A.",
            string rif = "J-30571871-7",
            string direccion = "Ctra. Villa de Cura - San Francisco de Asís, Local N° S/N, Sector Los Colorados - Villa de Cura - Estado Aragua",
            string telefono = "0244 386.10.13 - 386.33.04",
            string rifCliente = "",
            string dirCliente = "",
            string telCliente = "")
        {
            bool esNota = factura.EsNotaEntrega;

            var doc = Document.Create(container =>
            {
                container.Page(page =>
                {
                    if (esNota)
                    {
                        // Nota de entrega: media carta (~140×210mm)
                        page.Size(140, 210, Unit.Millimetre);
                        page.Margin(6, Unit.Millimetre);
                    }
                    else
                    {
                        // Factura fiscal: tamaño carta
                        page.Size(PageSizes.Letter);
                        page.Margin(10, Unit.Millimetre);
                    }
                    page.DefaultTextStyle(x => x.FontSize(esNota ? 8f : 9f).FontFamily("Arial"));

                    // ── ENCABEZADO ────────────────────────────────────────
                    page.Header().Column(header =>
                    {
                        if (!esNota)
                        {
                            // ── Factura fiscal: encabezado EHP completo ──
                            header.Item().AlignCenter().Text(nombreEmpresa)
                                .FontSize(16).Bold().FontColor("#CC0000");
                            header.Item().AlignCenter().Text(direccion)
                                .FontSize(7).FontColor("#333333");
                            header.Item().AlignCenter().Text("RIF: " + rif + "     Telfs: " + telefono)
                                .FontSize(8).Bold();
                            header.Item().Height(3);

                            header.Item().Border(1).Padding(3).Row(row =>
                            {
                                row.RelativeItem().Column(col =>
                                {
                                    col.Item().Text("FECHA").FontSize(7).Bold();
                                    col.Item().Text(factura.Fecha.ToString("dd / MM / yyyy")).FontSize(9);
                                });
                                row.RelativeItem().AlignCenter().Column(col =>
                                {
                                    col.Item().Text("FACTURA").FontSize(9).Bold();
                                    col.Item().Text("N° " + factura.NumeroF)
                                        .FontSize(14).Bold().FontColor("#CC0000");
                                });
                                row.RelativeItem().AlignRight().Column(col =>
                                {
                                    col.Item().Text("N° CONTROL").FontSize(7).Bold();
                                    col.Item().Text("00 — " + factura.NumeroF.PadLeft(6, '0'))
                                        .FontSize(10).Bold();
                                });
                            });

                            header.Item().Height(2);
                            header.Item().Border(1).Padding(2).Text(t =>
                            {
                                t.Span("Nombre o Razón Social: ").FontSize(7).Bold().FontColor("#555555");
                                t.Span(factura.Codigo).FontSize(9);
                            });
                            header.Item().Border(1).Padding(2).Text(t =>
                            {
                                t.Span("Domicilio Fiscal: ").FontSize(7).Bold().FontColor("#555555");
                                t.Span(dirCliente).FontSize(8);
                            });
                            header.Item().Height(1);
                            header.Item().Border(1).Padding(2).Row(row =>
                            {
                                row.RelativeItem().Text(t => { t.Span("RIF: ").FontSize(6).Bold(); t.Span(rifCliente).FontSize(8); });
                                row.RelativeItem().Text(t => { t.Span("Teléfono: ").FontSize(6).Bold(); t.Span(telCliente).FontSize(8); });
                                row.RelativeItem().Text(t => { t.Span("Cond. Pago: ").FontSize(6).Bold(); t.Span(factura.FormaDePago ?? "").FontSize(8); });
                            });
                        }
                        else
                        {
                            // ── Nota de entrega: encabezado simple ──
                            header.Item().Border(1).Padding(2).Row(row =>
                            {
                                row.RelativeItem().Column(col =>
                                {
                                    col.Item().Text("NOTA DE ENTREGA").FontSize(7).Bold();
                                    col.Item().Text("N° " + factura.NumeroF).FontSize(11).Bold();
                                });
                                row.ConstantItem(60).Column(col =>
                                {
                                    col.Item().Text("FECHA").FontSize(6).Bold();
                                    col.Item().Text(factura.Fecha.ToString("dd/MM/yyyy")).FontSize(9);
                                });
                            });
                            header.Item().Height(1);
                            header.Item().Border(1).Padding(2).Text(t =>
                            {
                                t.Span("NOMBRE: ").FontSize(7).Bold();
                                t.Span(factura.Codigo).FontSize(8);
                            });
                            header.Item().Border(1).Padding(2).Row(row =>
                            {
                                row.RelativeItem().Text(t => { t.Span("DIRECCIÓN: ").FontSize(7).Bold(); t.Span(dirCliente).FontSize(8); });
                                row.ConstantItem(50).Text(t => { t.Span("TELF: ").FontSize(7).Bold(); t.Span(telCliente).FontSize(8); });
                            });
                        }
                        header.Item().Height(3);
                    });

                    // ── CONTENIDO ─────────────────────────────────────────
                    // Variables accesibles desde todo el contenido
                    decimal desc = factura.Descuento;
                    decimal iva  = factura.IvaF;
                    decimal total = factura.TotalF;

                    page.Content().Column(content =>
                    {
                        content.Item().Height(4);

                        // Tabla de productos
                        content.Item().Table(table =>
                        {
                            // Definir columnas según tipo
                            table.ColumnsDefinition(cols =>
                            {
                                if (esNota)
                                {
                                    cols.ConstantColumn(30);   // Cant
                                    cols.RelativeColumn();     // Descripcion
                                    cols.ConstantColumn(55);   // Precio
                                    cols.ConstantColumn(55);   // Total
                                }
                                else
                                {
                                    cols.ConstantColumn(30);   // Cant
                                    cols.ConstantColumn(55);   // Ref/Codigo
                                    cols.RelativeColumn();     // Descripcion
                                    cols.ConstantColumn(65);   // Precio
                                    cols.ConstantColumn(65);   // Total
                                }
                            });

                            // Encabezado de tabla
                            static IContainer HeaderCell(IContainer c) =>
                                c.Background("#1E3A8A").Padding(3).AlignMiddle();

                            table.Header(h =>
                            {
                                h.Cell().Element(HeaderCell).Text("Cant.").FontColor("#FFFFFF").Bold().AlignCenter();
                                if (!esNota)
                                    h.Cell().Element(HeaderCell).Text("Ref.").FontColor("#FFFFFF").Bold();
                                h.Cell().Element(HeaderCell).Text("Descripción").FontColor("#FFFFFF").Bold();
                                h.Cell().Element(HeaderCell).Text("P. Unit.").FontColor("#FFFFFF").Bold().AlignRight();
                                h.Cell().Element(HeaderCell).Text("Total").FontColor("#FFFFFF").Bold().AlignRight();
                            });

                            // Filas de productos
                            int fila = 0;
                            decimal totalLineas = 0;

                            foreach (var linea in lineas)
                            {
                                fila++;
                                bool par = fila % 2 == 0;
                                string bg = par ? "#F0F4FF" : "#FFFFFF";

                                static IContainer DataCell(IContainer c, string bg) =>
                                    c.Background(bg).Padding(3).AlignMiddle();

                                decimal subtotal = linea.Cantidad * linea.PrecioUnit;
                                totalLineas += subtotal;

                                table.Cell().Element(c => DataCell(c, bg))
                                    .Text(linea.Cantidad.ToString("N0")).AlignCenter();
                                if (!esNota)
                                    table.Cell().Element(c => DataCell(c, bg))
                                        .Text(linea.CodigoItem).FontSize(8);
                                table.Cell().Element(c => DataCell(c, bg))
                                    .Text(linea.Descripcion);
                                table.Cell().Element(c => DataCell(c, bg))
                                    .Text(linea.PrecioUnit.ToString("N2")).AlignRight();
                                table.Cell().Element(c => DataCell(c, bg))
                                    .Text(subtotal.ToString("N2")).AlignRight().Bold();
                            }

                            // Fila de totales
                            decimal descMonto = totalLineas * desc / 100;
                            decimal subTras   = totalLineas - descMonto;

                            static IContainer TotalCell(IContainer c) =>
                                c.BorderTop(1).BorderColor("#CCCCCC").Padding(5).AlignMiddle();

                            uint spanEmpty = esNota ? 2u : 3u;

                            // Fila descuento (si aplica)
                            if (factura.Descuento > 0)
                            {
                                table.Cell().ColumnSpan(spanEmpty).Element(TotalCell).Text("");
                                table.Cell().Element(TotalCell)
                                    .Text("Descuento (" + factura.Descuento + "%):")
                                    .AlignRight().FontColor("#777777");
                                table.Cell().Element(TotalCell)
                                    .Text("-" + descMonto.ToString("N2"))
                                    .AlignRight().FontColor("#CC0000");
                            }

                            // Subtotal
                            table.Cell().ColumnSpan(spanEmpty).Element(TotalCell).Text("");
                            table.Cell().Element(TotalCell).Text("Subtotal:").AlignRight();
                            table.Cell().Element(TotalCell).Text(subTras.ToString("N2")).AlignRight();

                            if (!esNota)
                            {
                                // IVA solo en factura fiscal
                                table.Cell().ColumnSpan(spanEmpty).Element(TotalCell).Text("");
                                table.Cell().Element(TotalCell).Text("IVA 16%:").AlignRight();
                                table.Cell().Element(TotalCell).Text(iva.ToString("N2")).AlignRight();
                            }

                            // TOTAL
                            table.Cell().ColumnSpan(esNota ? 2u : 4u).Background("#1E3A8A").Padding(5).Text("");
                            table.Cell().Background("#1E3A8A").Padding(5)
                                .Text("TOTAL:").Bold().FontColor("#FFFFFF").AlignRight();
                            table.Cell().Background("#1E3A8A").Padding(5)
                                .Text(total.ToString("C2")).Bold().FontColor("#FFFFFF").AlignRight();
                        });

                        content.Item().Height(8);

                        if (!esNota)
                        {
                            // ── Factura fiscal: forma de pago + totales desglosados ──
                            content.Item().Row(row =>
                            {
                                row.RelativeItem().Border(1).Padding(4).Column(col =>
                                {
                                    col.Item().Text("FORMA DE PAGO").FontSize(7).Bold();
                                    col.Item().Height(4);
                                    string fp = factura.FormaDePago?.ToLower() ?? "";
                                    string Chk(string k) => fp.Contains(k) ? "[X]" : "[  ]";
                                    col.Item().Text($"{Chk("efectivo")} Efectivo   {Chk("cheque")} Cheque   {Chk("créd")} Tarj. Crédito").FontSize(8);
                                    col.Item().Text($"{Chk("transf")} Transf. Bancaria   {Chk("débit")} Tarj. Débito").FontSize(8);
                                });
                                row.ConstantItem(180).Border(1).Column(col =>
                                {
                                    decimal bi = total - iva;
                                    void Fila(string lbl, string val, bool bold = false) {
                                        col.Item().BorderBottom(1).BorderColor("#CCC").Padding(2).Row(r => {
                                            r.RelativeItem().Text(lbl).FontSize(8).AlignRight();
                                            var t = r.ConstantItem(70).Text(val).FontSize(8).AlignRight();
                                            if (bold) t.Bold();
                                        });
                                    }
                                    Fila("SUB-TOTAL", (bi + (desc > 0 ? bi * factura.Descuento / (100 - factura.Descuento) : 0)).ToString("N2"));
                                    if (desc > 0) Fila("DESCUENTO " + factura.Descuento + "%", "-" + (bi * factura.Descuento / (100 - factura.Descuento)).ToString("N2"));
                                    Fila("EXENTO", "");
                                    Fila("BASE IMPONIBLE", bi.ToString("N2"));
                                    Fila("IVA 16%", iva.ToString("N2"));
                                    Fila("TOTAL", total.ToString("N2"), true);
                                });
                            });

                            // Firmas
                            content.Item().Height(15);
                            content.Item().Row(row =>
                            {
                                row.RelativeItem().Column(col =>
                                {
                                    col.Item().Height(20);
                                    col.Item().BorderTop(1).PaddingTop(2).AlignCenter()
                                        .Text("Firma y Sello").FontSize(7);
                                });
                                row.ConstantItem(20).Text("");
                                row.RelativeItem().Column(col =>
                                {
                                    col.Item().Height(20);
                                    col.Item().BorderTop(1).PaddingTop(2).AlignCenter()
                                        .Text("Cliente Conforme").FontSize(7);
                                });
                            });
                        }
                        else
                        {
                            // ── Nota de entrega: pie simple ──
                            content.Item().Row(row =>
                            {
                                row.RelativeItem().Column(col =>
                                {
                                    col.Item().Height(12);
                                    col.Item().BorderTop(1).PaddingTop(2).AlignCenter()
                                        .Text("FIRMA").FontSize(7);
                                    col.Item().AlignCenter().Text("Esta factura va sin tachadura ni enmendaduras")
                                        .FontSize(6).FontColor("#888888");
                                });
                                row.ConstantItem(80).Border(1).Column(col =>
                                {
                                    void Fila(string lbl, string val, bool bold = false) {
                                        col.Item().BorderBottom(1).BorderColor("#CCC").Padding(1).Row(r => {
                                            r.RelativeItem().Text(lbl).FontSize(7).AlignRight();
                                            var t = r.ConstantItem(28).Text(val).FontSize(7).AlignRight();
                                            if (bold) t.Bold();
                                        });
                                    }
                                    Fila("SUB-TOTAL", (total - iva).ToString("N2"));
                                    if (desc > 0) Fila("DESCUENTO", "-" + (total * desc / 100).ToString("N2"));
                                    Fila("TOTAL", total.ToString("N2"), true);
                                });
                            });
                        }

                        // Abono y saldo pendiente
                        if (factura.Abono > 0)
                        {
                            content.Item().Height(6);
                            decimal saldo = factura.TotalF - factura.Abono;
                            content.Item().Row(row =>
                            {
                                row.RelativeItem();
                                row.ConstantItem(200).Column(col =>
                                {
                                    col.Item().Background("#E8F5E9").Padding(4).Row(r =>
                                    {
                                        r.RelativeItem().Text("Abono:").Bold().FontSize(8);
                                        r.RelativeItem().Text(factura.Abono.ToString("C2"))
                                            .AlignRight().FontSize(8).FontColor("#2E7D32");
                                    });
                                    col.Item().Background(saldo <= 0 ? "#E8F5E9" : "#FFF8E1").Padding(4).Row(r =>
                                    {
                                        r.RelativeItem().Text("Saldo:").Bold().FontSize(8);
                                        r.RelativeItem().Text(saldo.ToString("C2"))
                                            .AlignRight().FontSize(8)
                                            .FontColor(saldo <= 0 ? "#2E7D32" : "#E65100");
                                    });
                                });
                            });
                        }
                    });

                    // ── PIE DE PAGINA ─────────────────────────────────────
                    page.Footer().AlignCenter().Text(txt =>
                    {
                        txt.Span("Pagina ").FontSize(8).FontColor("#999999");
                        txt.CurrentPageNumber().FontSize(8).FontColor("#999999");
                        txt.Span(" de ").FontSize(8).FontColor("#999999");
                        txt.TotalPages().FontSize(8).FontColor("#999999");
                        txt.Span("   |   Generado: " + DateTime.Now.ToString("dd/MM/yyyy HH:mm"))
                            .FontSize(8).FontColor("#999999");
                        txt.Span("   |   " + nombreEmpresa)
                            .FontSize(8).FontColor("#999999");
                    });
                });
            });

            return doc.GeneratePdf();
        }

        // ═════════════════════════════════════════════════════════════════
        //  GENERAR REPORTE DE FACTURAS (listado)
        // ═════════════════════════════════════════════════════════════════
        public static byte[] GenerarReporteFacturas(
            List<Factura> facturas,
            string titulo = "Reporte de Facturas",
            string nombreEmpresa = "Mi Empresa S.A.")
        {
            var doc = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4.Landscape());
                    page.Margin(30);
                    page.DefaultTextStyle(x => x.FontSize(9).FontFamily("Arial"));

                    page.Header().Column(col =>
                    {
                        col.Item().Row(row =>
                        {
                            row.RelativeItem().Text(nombreEmpresa)
                                .FontSize(14).Bold().FontColor("#1E3A8A");
                            row.ConstantItem(200).Text(titulo)
                                .FontSize(11).Bold().AlignRight();
                        });
                        col.Item().Text("Generado: " + DateTime.Now.ToString("dd/MM/yyyy HH:mm"))
                            .FontSize(8).FontColor("#777777");
                        col.Item().Height(2).Background("#1E3A8A");
                        col.Item().Height(8);
                    });

                    page.Content().Table(table =>
                    {
                        table.ColumnsDefinition(cols =>
                        {
                            cols.ConstantColumn(80);   // Numero
                            cols.RelativeColumn();     // Cliente
                            cols.ConstantColumn(75);   // Fecha
                            cols.ConstantColumn(70);   // Forma pago
                            cols.ConstantColumn(80);   // Total
                            cols.ConstantColumn(70);   // Abono
                            cols.ConstantColumn(70);   // Saldo
                            cols.ConstantColumn(70);   // Estado
                        });

                        static IContainer HCell(IContainer c) =>
                            c.Background("#1E3A8A").Padding(4).AlignMiddle();

                        table.Header(h =>
                        {
                            h.Cell().Element(HCell).Text("Numero").FontColor("#FFFFFF").Bold();
                            h.Cell().Element(HCell).Text("Cliente").FontColor("#FFFFFF").Bold();
                            h.Cell().Element(HCell).Text("Fecha").FontColor("#FFFFFF").Bold();
                            h.Cell().Element(HCell).Text("F. Pago").FontColor("#FFFFFF").Bold();
                            h.Cell().Element(HCell).Text("Total").FontColor("#FFFFFF").Bold().AlignRight();
                            h.Cell().Element(HCell).Text("Abono").FontColor("#FFFFFF").Bold().AlignRight();
                            h.Cell().Element(HCell).Text("Saldo").FontColor("#FFFFFF").Bold().AlignRight();
                            h.Cell().Element(HCell).Text("Estado").FontColor("#FFFFFF").Bold().AlignCenter();
                        });

                        int i = 0;
                        decimal sumTotal = 0, sumAbono = 0;

                        foreach (var f in facturas)
                        {
                            i++;
                            bool par = i % 2 == 0;
                            string bg = par ? "#F0F4FF" : "#FFFFFF";
                            bool cancelada = f.Abono < 0;
                            decimal abono  = cancelada ? 0 : f.Abono;
                            decimal saldo  = cancelada ? 0 : f.TotalF - f.Abono;
                            string estado  = cancelada ? "Cancelada"
                                           : saldo <= 0 ? "Saldada" : "Pendiente";
                            string bgEstado = estado == "Saldada"   ? "#E8F5E9"
                                            : estado == "Cancelada" ? "#FFEBEE"
                                            : "#FFF8E1";
                            string colEstado = estado == "Saldada"   ? "#2E7D32"
                                             : estado == "Cancelada" ? "#C62828"
                                             : "#E65100";

                            if (!cancelada) { sumTotal += f.TotalF; sumAbono += abono; }

                            static IContainer DCell(IContainer c, string bg) =>
                                c.Background(bg).Padding(4).AlignMiddle();

                            table.Cell().Element(c => DCell(c, bg)).Text(f.NumeroF);
                            table.Cell().Element(c => DCell(c, bg)).Text(f.Codigo);
                            table.Cell().Element(c => DCell(c, bg))
                                .Text(f.Fecha.ToString("dd/MM/yyyy"));
                            table.Cell().Element(c => DCell(c, bg)).Text(f.FormaDePago ?? "");
                            table.Cell().Element(c => DCell(c, bg))
                                .Text(f.TotalF.ToString("C2")).AlignRight();
                            table.Cell().Element(c => DCell(c, bg))
                                .Text(abono.ToString("C2")).AlignRight();
                            table.Cell().Element(c => DCell(c, bg))
                                .Text(saldo.ToString("C2")).AlignRight()
                                .FontColor(saldo > 0 ? "#E65100" : "#2E7D32");
                            table.Cell().Background(bgEstado).Padding(4).AlignMiddle()
                                .Text(estado).FontColor(colEstado).AlignCenter().Bold();
                        }

                        // Fila de totales
                        static IContainer TCell(IContainer c) =>
                            c.Background("#1E3A8A").Padding(4).AlignMiddle();

                        table.Cell().ColumnSpan(4).Element(TCell)
                            .Text("TOTALES  (" + facturas.Count + " facturas)")
                            .FontColor("#FFFFFF").Bold().AlignRight();
                        table.Cell().Element(TCell)
                            .Text(sumTotal.ToString("C2")).FontColor("#FFFFFF").Bold().AlignRight();
                        table.Cell().Element(TCell)
                            .Text(sumAbono.ToString("C2")).FontColor("#FFFFFF").Bold().AlignRight();
                        table.Cell().Element(TCell)
                            .Text((sumTotal - sumAbono).ToString("C2"))
                            .FontColor("#FFEB3B").Bold().AlignRight();
                        table.Cell().Element(TCell).Text("");
                    });

                    page.Footer().AlignCenter().Text(txt =>
                    {
                        txt.Span("Pagina ").FontSize(8).FontColor("#999999");
                        txt.CurrentPageNumber().FontSize(8).FontColor("#999999");
                        txt.Span(" de ").FontSize(8).FontColor("#999999");
                        txt.TotalPages().FontSize(8).FontColor("#999999");
                    });
                });
            });

            return doc.GeneratePdf();
        }

        // ═════════════════════════════════════════════════════════════════
        //  HELPERS: guardar, abrir, imprimir
        // ═════════════════════════════════════════════════════════════════


        // ═════════════════════════════════════════════════════════════════
        //  REPORTE DE RENTABILIDAD POR PRODUCTO
        // ═════════════════════════════════════════════════════════════════
        public static byte[] GenerarReporteRentabilidad(
            System.Collections.Generic.List<RentabilidadProducto> datos,
            DateTime desde,
            DateTime hasta,
            string nombreEmpresa = "Mi Empresa S.A.")
        {
            var doc = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4.Landscape());
                    page.Margin(25);
                    page.DefaultTextStyle(x => x.FontSize(8).FontFamily("Arial"));

                    page.Header().Column(col =>
                    {
                        col.Item().Row(row =>
                        {
                            row.RelativeItem().Column(c =>
                            {
                                c.Item().Text(nombreEmpresa)
                                    .FontSize(13).Bold().FontColor("#1E6438");
                                c.Item().Text("Reporte de Rentabilidad por Producto")
                                    .FontSize(10).Bold();
                                c.Item().Text(
                                    "Periodo: " + desde.ToString("dd/MM/yyyy") +
                                    " al " + hasta.ToString("dd/MM/yyyy"))
                                    .FontSize(8).FontColor("#555555");
                            });
                            row.ConstantItem(200).Column(c =>
                            {
                                decimal totalIngreso  = 0;
                                decimal totalUtilidad = 0;
                                foreach (var r in datos)
                                {
                                    totalIngreso  += r.IngresoTotal;
                                    totalUtilidad += r.UtilidadBruta;
                                }
                                decimal margenGlobal = totalIngreso > 0
                                    ? Math.Round(totalUtilidad / totalIngreso * 100, 1) : 0;

                                c.Item().Background("#1E6438").Padding(6).Column(inner =>
                                {
                                    inner.Item().Row(r =>
                                    {
                                        r.RelativeItem().Text("Ingreso total:").FontColor("#CCFFDD");
                                        r.ConstantItem(90).Text(
                                            totalIngreso.ToString("C0"))
                                            .FontColor("#FFFFFF").Bold().AlignRight();
                                    });
                                    inner.Item().Row(r =>
                                    {
                                        r.RelativeItem().Text("Utilidad bruta:").FontColor("#CCFFDD");
                                        r.ConstantItem(90).Text(
                                            totalUtilidad.ToString("C0"))
                                            .FontColor("#FFFFFF").Bold().AlignRight();
                                    });
                                    inner.Item().Row(r =>
                                    {
                                        r.RelativeItem().Text("Margen global:").FontColor("#CCFFDD");
                                        r.ConstantItem(90).Text(
                                            margenGlobal + "%")
                                            .FontColor("#FFFF99").Bold().AlignRight();
                                    });
                                });
                            });
                        });
                        col.Item().Height(2).Background("#1E6438");
                        col.Item().Height(6);
                    });

                    page.Content().Table(table =>
                    {
                        table.ColumnsDefinition(cols =>
                        {
                            cols.ConstantColumn(55);   // Codigo
                            cols.RelativeColumn(2);    // Descripcion
                            cols.ConstantColumn(55);   // Depto
                            cols.ConstantColumn(55);   // Costo
                            cols.ConstantColumn(55);   // Precio
                            cols.ConstantColumn(45);   // Margen$
                            cols.ConstantColumn(45);   // Margen%
                            cols.ConstantColumn(45);   // Vendidas
                            cols.ConstantColumn(70);   // Ingresos
                            cols.ConstantColumn(70);   // Costo Total
                            cols.ConstantColumn(70);   // Utilidad
                            cols.ConstantColumn(40);   // Stock
                            cols.ConstantColumn(45);   // Rotacion
                        });

                        static IContainer HCell(IContainer c) =>
                            c.Background("#1E6438").Padding(3).AlignMiddle();

                        table.Header(h =>
                        {
                            foreach (var col in new[]
                            {
                                "Codigo","Descripcion","Depto","Costo Unit.",
                                "Precio","Margen $","Margen %","Vendidas",
                                "Ingresos","Costo Total","Utilidad","Stock","Rotacion"
                            })
                            h.Cell().Element(HCell)
                                .Text(col).FontColor("#FFFFFF").Bold().AlignCenter();
                        });

                        int fila = 0;
                        foreach (var r in datos)
                        {
                            fila++;
                            bool par   = fila % 2 == 0;
                            string bg  = par ? "#F0FFF4" : "#FFFFFF";
                            string bgU = r.UtilidadBruta < 0  ? "#FFE0E0"
                                       : r.MargenPct >= 30    ? "#E8FFE8"
                                       : r.MargenPct >= 15    ? "#FFFFF0"
                                       : bg;
                            string fgM = r.MargenPct >= 30    ? "#1A7040"
                                       : r.MargenPct >= 15    ? "#806000"
                                       : r.MargenPct > 0      ? "#A03000"
                                       : "#CC0000";

                            static IContainer DCell(IContainer c, string bg) =>
                                c.Background(bg).Padding(3).AlignMiddle();

                            table.Cell().Element(c => DCell(c, bg)).Text(r.Codigo).AlignCenter();
                            table.Cell().Element(c => DCell(c, bg)).Text(r.Descripcion);
                            table.Cell().Element(c => DCell(c, bg)).Text(r.Departamento).AlignCenter();
                            table.Cell().Element(c => DCell(c, bg)).Text(r.CostoUnitario.ToString("C2")).AlignRight();
                            table.Cell().Element(c => DCell(c, bg)).Text(r.PrecioVenta.ToString("C2")).AlignRight();
                            table.Cell().Element(c => DCell(c, bg))
                                .Text((r.PrecioVenta - r.CostoUnitario).ToString("C2"))
                                .FontColor(fgM).AlignRight();
                            table.Cell().Element(c => DCell(c, bg))
                                .Text(r.MargenPct.ToString("F1") + "%")
                                .FontColor(fgM).Bold().AlignCenter();
                            table.Cell().Element(c => DCell(c, bg))
                                .Text(r.UnidadesVendidas.ToString("N0")).AlignCenter();
                            table.Cell().Element(c => DCell(c, bg))
                                .Text(r.IngresoTotal.ToString("C0")).AlignRight();
                            table.Cell().Element(c => DCell(c, bg))
                                .Text(r.CostoTotal.ToString("C0")).AlignRight();
                            table.Cell().Element(c => DCell(c, bgU))
                                .Text(r.UtilidadBruta.ToString("C0"))
                                .Bold().FontColor(r.UtilidadBruta < 0 ? "#CC0000" : "#1A7040")
                                .AlignRight();
                            table.Cell().Element(c => DCell(c, bg))
                                .Text(r.StockActual.ToString("N0")).AlignCenter();
                            table.Cell().Element(c => DCell(c, bg))
                                .Text(r.RotacionMeses > 0
                                    ? r.RotacionMeses.ToString("F1") + "m" : "—")
                                .AlignCenter();
                        }

                        // Fila de totales
                        decimal sumIngreso  = 0, sumCostoTotal = 0, sumUtilidad = 0;
                        int     sumVendidas = 0;
                        foreach (var r in datos)
                        {
                            sumIngreso   += r.IngresoTotal;
                            sumCostoTotal+= r.CostoTotal;
                            sumUtilidad  += r.UtilidadBruta;
                            sumVendidas  += (int)r.UnidadesVendidas;
                        }
                        decimal margenFinal = sumIngreso > 0
                            ? Math.Round(sumUtilidad / sumIngreso * 100, 1) : 0;

                        static IContainer TCell(IContainer c) =>
                            c.Background("#1E6438").Padding(3).AlignMiddle();

                        table.Cell().ColumnSpan(7).Element(TCell)
                            .Text("TOTALES (" + datos.Count + " productos)")
                            .FontColor("#FFFFFF").Bold().AlignRight();
                        table.Cell().Element(TCell)
                            .Text(sumVendidas.ToString("N0"))
                            .FontColor("#FFFFFF").Bold().AlignCenter();
                        table.Cell().Element(TCell)
                            .Text(sumIngreso.ToString("C0"))
                            .FontColor("#FFFFFF").Bold().AlignRight();
                        table.Cell().Element(TCell)
                            .Text(sumCostoTotal.ToString("C0"))
                            .FontColor("#FFFFFF").Bold().AlignRight();
                        table.Cell().Element(TCell)
                            .Text(sumUtilidad.ToString("C0") + " (" + margenFinal + "%)")
                            .FontColor("#FFFF99").Bold().AlignRight();
                        table.Cell().ColumnSpan(2).Element(TCell).Text("");
                    });

                    page.Footer().AlignCenter().Text(txt =>
                    {
                        txt.Span("Pagina ").FontSize(7).FontColor("#999999");
                        txt.CurrentPageNumber().FontSize(7).FontColor("#999999");
                        txt.Span(" de ").FontSize(7).FontColor("#999999");
                        txt.TotalPages().FontSize(7).FontColor("#999999");
                        txt.Span("   |   Generado: " + DateTime.Now.ToString("dd/MM/yyyy HH:mm"))
                            .FontSize(7).FontColor("#999999");
                    });
                });
            });

            return doc.GeneratePdf();
        }


        // ═════════════════════════════════════════════════════════════════
        //  REPORTE DE CONTROL DE CALIDAD
        // ═════════════════════════════════════════════════════════════════
        public static byte[] GenerarReporteCalidad(
            System.Collections.Generic.List<InspeccionCalidad> inspecciones,
            string nombreEmpresa = "Mi Empresa S.A.")
        {
            var doc = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4.Landscape());
                    page.Margin(28);
                    page.DefaultTextStyle(x => x.FontSize(8).FontFamily("Arial"));

                    page.Header().Column(col =>
                    {
                        col.Item().Row(row =>
                        {
                            row.RelativeItem().Column(c =>
                            {
                                c.Item().Text(nombreEmpresa).FontSize(13).Bold().FontColor("#28508C");
                                c.Item().Text("Reporte de Control de Calidad").FontSize(10).Bold();
                                c.Item().Text("Generado: " + DateTime.Now.ToString("dd/MM/yyyy HH:mm"))
                                    .FontSize(8).FontColor("#666666");
                            });
                            row.ConstantItem(220).Column(c =>
                            {
                                decimal totalU    = inspecciones.Sum(i => i.CantidadTotal);
                                decimal aprobadas = inspecciones.Sum(i => i.CantidadAprobada);
                                decimal rechazadas= inspecciones.Sum(i => i.CantidadRechazada);
                                decimal pctAp = totalU > 0 ? Math.Round(aprobadas  / totalU * 100, 1) : 0;
                                decimal pctRe = totalU > 0 ? Math.Round(rechazadas / totalU * 100, 1) : 0;

                                c.Item().Background("#28508C").Padding(6).Column(inner =>
                                {
                                    inner.Item().Row(r =>
                                    {
                                        r.RelativeItem().Text("Total unidades:").FontColor("#CCDDFF");
                                        r.ConstantItem(70).Text(totalU.ToString("N0")).FontColor("#FFFFFF").Bold().AlignRight();
                                    });
                                    inner.Item().Row(r =>
                                    {
                                        r.RelativeItem().Text("% Aprobado:").FontColor("#CCDDFF");
                                        r.ConstantItem(70).Text(pctAp + "%").FontColor("#99FF99").Bold().AlignRight();
                                    });
                                    inner.Item().Row(r =>
                                    {
                                        r.RelativeItem().Text("% Rechazado:").FontColor("#CCDDFF");
                                        r.ConstantItem(70).Text(pctRe + "%").FontColor("#FF9999").Bold().AlignRight();
                                    });
                                });
                            });
                        });
                        col.Item().Height(2).Background("#28508C");
                        col.Item().Height(6);
                    });

                    page.Content().Table(table =>
                    {
                        table.ColumnsDefinition(cols =>
                        {
                            cols.ConstantColumn(60);   // Lote
                            cols.ConstantColumn(65);   // Producto
                            cols.ConstantColumn(65);   // Fecha
                            cols.ConstantColumn(65);   // Inspector
                            cols.ConstantColumn(45);   // Total
                            cols.ConstantColumn(50);   // Aprobadas
                            cols.ConstantColumn(50);   // Rechazadas
                            cols.ConstantColumn(45);   // %Ap
                            cols.ConstantColumn(35);   // Dim
                            cols.ConstantColumn(35);   // Ap
                            cols.ConstantColumn(35);   // Func
                            cols.ConstantColumn(35);   // Etiq
                            cols.ConstantColumn(35);   // Prom
                            cols.ConstantColumn(65);   // Resultado
                            cols.RelativeColumn();     // Observaciones
                        });

                        static IContainer HCell(IContainer c) =>
                            c.Background("#28508C").Padding(3).AlignMiddle();

                        table.Header(h =>
                        {
                            foreach (var col in new[]
                            {
                                "Lote","Producto","Fecha","Inspector",
                                "Total","Aprobadas","Rechazadas","% Ap.",
                                "Dim","Ap","Func","Etiq","Prom","Resultado","Observaciones"
                            })
                            h.Cell().Element(HCell).Text(col).FontColor("#FFFFFF").Bold().AlignCenter();
                        });

                        int fila = 0;
                        foreach (var i in inspecciones)
                        {
                            fila++;
                            string bg  = fila % 2 == 0 ? "#EEF2FF" : "#FFFFFF";
                            string bgR = i.Resultado == "Aprobado"  ? "#E8FFE8"
                                       : i.Resultado == "Rechazado" ? "#FFE8E8" : "#FFF8DC";
                            string fgR = i.Resultado == "Aprobado"  ? "#1A7040"
                                       : i.Resultado == "Rechazado" ? "#AA2020" : "#806000";

                            static IContainer DCell(IContainer c, string bg) =>
                                c.Background(bg).Padding(3).AlignMiddle();

                            table.Cell().Element(c => DCell(c, bg)).Text(i.Lote).AlignCenter();
                            table.Cell().Element(c => DCell(c, bg)).Text(i.ProductoCod).AlignCenter();
                            table.Cell().Element(c => DCell(c, bg)).Text(i.FechaInspeccion.ToString("dd/MM/yy")).AlignCenter();
                            table.Cell().Element(c => DCell(c, bg)).Text(i.Inspector);
                            table.Cell().Element(c => DCell(c, bg)).Text(i.CantidadTotal.ToString("N0")).AlignRight();
                            table.Cell().Element(c => DCell(c, bg)).Text(i.CantidadAprobada.ToString("N0")).AlignRight().FontColor("#1A7040");
                            table.Cell().Element(c => DCell(c, bg)).Text(i.CantidadRechazada.ToString("N0")).AlignRight().FontColor("#AA2020");
                            table.Cell().Element(c => DCell(c, bg)).Text(i.PctAprobado + "%").AlignCenter().Bold();
                            table.Cell().Element(c => DCell(c, bg)).Text(i.PuntajeDimensiones + "/5").AlignCenter();
                            table.Cell().Element(c => DCell(c, bg)).Text(i.PuntajeApariencia  + "/5").AlignCenter();
                            table.Cell().Element(c => DCell(c, bg)).Text(i.PuntajeFuncional   + "/5").AlignCenter();
                            table.Cell().Element(c => DCell(c, bg)).Text(i.PuntajeEtiquetado  + "/5").AlignCenter();
                            table.Cell().Element(c => DCell(c, bg)).Text(i.PuntajePromedio.ToString("F1")).AlignCenter().Bold();
                            table.Cell().Background(bgR).Padding(3).AlignMiddle()
                                .Text(i.Resultado).FontColor(fgR).Bold().AlignCenter();
                            table.Cell().Element(c => DCell(c, bg)).Text(i.Observaciones);
                        }
                    });

                    page.Footer().AlignCenter().Text(txt =>
                    {
                        txt.Span("Pagina ").FontSize(7).FontColor("#999999");
                        txt.CurrentPageNumber().FontSize(7).FontColor("#999999");
                        txt.Span(" de ").FontSize(7).FontColor("#999999");
                        txt.TotalPages().FontSize(7).FontColor("#999999");
                    });
                });
            });

            return doc.GeneratePdf();
        }


        // ═════════════════════════════════════════════════════════════════
        //  REPORTE DE MANTENIMIENTO DE MOLDES
        // ═════════════════════════════════════════════════════════════════
        public static byte[] GenerarReporteMantenimiento(
            System.Collections.Generic.List<ProgramaMant> programas,
            System.Collections.Generic.List<RegistroMantenimiento> historial,
            string nombreEmpresa = "Mi Empresa S.A.")
        {
            var doc = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(30);
                    page.DefaultTextStyle(x => x.FontSize(9).FontFamily("Arial"));

                    page.Header().Column(col =>
                    {
                        col.Item().Row(row =>
                        {
                            row.RelativeItem().Column(c =>
                            {
                                c.Item().Text(nombreEmpresa).FontSize(14).Bold().FontColor("#14506E");
                                c.Item().Text("Programa de Mantenimiento — Moldes").FontSize(11).Bold();
                                c.Item().Text("Generado: " + DateTime.Now.ToString("dd/MM/yyyy HH:mm"))
                                    .FontSize(8).FontColor("#666666");
                            });
                        });
                        col.Item().Height(2).Background("#14506E");
                        col.Item().Height(8);
                    });

                    page.Content().Column(body =>
                    {
                        // ── Seccion 1: Programa activo ────────────────────
                        body.Item().Text("Estado del Programa").FontSize(11).Bold().FontColor("#14506E");
                        body.Item().Height(6);

                        body.Item().Table(table =>
                        {
                            table.ColumnsDefinition(cols =>
                            {
                                cols.ConstantColumn(70);
                                cols.RelativeColumn();
                                cols.ConstantColumn(70);
                                cols.ConstantColumn(60);
                                cols.ConstantColumn(70);
                                cols.ConstantColumn(70);
                                cols.ConstantColumn(55);
                                cols.ConstantColumn(55);
                            });

                            static IContainer HCell(IContainer c) =>
                                c.Background("#14506E").Padding(4).AlignMiddle();

                            table.Header(h =>
                            {
                                foreach (var t in new[] { "Molde","Descripcion","Tipo","Frec.dias","Ult.Mant","Prox.Mant","Dias Rest.","Estado" })
                                    h.Cell().Element(HCell).Text(t).FontColor("#FFFFFF").Bold().AlignCenter();
                            });

                            int fi = 0;
                            foreach (var p in programas.OrderBy(p => p.DiasRestantes))
                            {
                                fi++;
                                string bg  = fi % 2 == 0 ? "#EEF4FA" : "#FFFFFF";
                                string bgE = p.Estado == "VENCIDO" ? "#FFE0E0"
                                           : p.Estado == "URGENTE" ? "#FFF0D8"
                                           : p.Estado == "PROXIMO" ? "#FFFCE0" : "#E8FFE8";
                                string fgE = p.Estado == "VENCIDO" ? "#AA1111"
                                           : p.Estado == "URGENTE" ? "#AA5500"
                                           : p.Estado == "PROXIMO" ? "#888800" : "#115511";

                                static IContainer DC(IContainer c, string bg) =>
                                    c.Background(bg).Padding(4).AlignMiddle();

                                table.Cell().Element(c => DC(c, bg)).Text(p.CodigoMolde).AlignCenter();
                                table.Cell().Element(c => DC(c, bg)).Text(p.DescMolde);
                                table.Cell().Element(c => DC(c, bg)).Text(p.TipoMant).AlignCenter();
                                table.Cell().Element(c => DC(c, bg)).Text(p.FrecuenciaDias.ToString()).AlignCenter();
                                table.Cell().Element(c => DC(c, bg)).Text(p.UltimoMant.ToString("dd/MM/yy")).AlignCenter();
                                table.Cell().Element(c => DC(c, bg)).Text(p.ProximoMant.ToString("dd/MM/yy")).AlignCenter();
                                table.Cell().Element(c => DC(c, bg)).Text(p.DiasRestantes.ToString()).AlignCenter()
                                    .FontColor(p.DiasRestantes < 0 ? "#CC0000" : "#333333");
                                table.Cell().Background(bgE).Padding(4).AlignMiddle()
                                    .Text(p.Estado).FontColor(fgE).Bold().AlignCenter();
                            }
                        });

                        body.Item().Height(20);

                        // ── Seccion 2: Historial reciente ─────────────────
                        body.Item().Text("Historial reciente (ultimos 20)").FontSize(11).Bold().FontColor("#14506E");
                        body.Item().Height(6);

                        body.Item().Table(table =>
                        {
                            table.ColumnsDefinition(cols =>
                            {
                                cols.ConstantColumn(65);
                                cols.ConstantColumn(65);
                                cols.ConstantColumn(65);
                                cols.RelativeColumn();
                                cols.ConstantColumn(55);
                                cols.ConstantColumn(40);
                                cols.ConstantColumn(60);
                            });

                            static IContainer HCell(IContainer c) =>
                                c.Background("#14506E").Padding(4).AlignMiddle();

                            table.Header(h =>
                            {
                                foreach (var t in new[] { "Molde","Fecha","Tipo","Descripcion","Costo","Horas","Resultado" })
                                    h.Cell().Element(HCell).Text(t).FontColor("#FFFFFF").Bold().AlignCenter();
                            });

                            int fi = 0;
                            foreach (var r in historial.Take(20))
                            {
                                fi++;
                                string bg  = fi % 2 == 0 ? "#EEF4FA" : "#FFFFFF";
                                string bgR = r.Resultado == "Completado" ? "#E8FFE8"
                                           : r.Resultado == "Cancelado"  ? "#FFE8E8" : bg;

                                static IContainer DC(IContainer c, string bg) =>
                                    c.Background(bg).Padding(4).AlignMiddle();

                                table.Cell().Element(c => DC(c, bg)).Text(r.CodigoMolde).AlignCenter();
                                table.Cell().Element(c => DC(c, bg)).Text(r.FechaRealizada.ToString("dd/MM/yy")).AlignCenter();
                                table.Cell().Element(c => DC(c, bg)).Text(r.TipoMant).AlignCenter();
                                table.Cell().Element(c => DC(c, bg)).Text(r.Descripcion);
                                table.Cell().Element(c => DC(c, bg)).Text(r.CostoMant.ToString("C0")).AlignRight();
                                table.Cell().Element(c => DC(c, bg)).Text(r.DuracionHoras + "h").AlignCenter();
                                table.Cell().Background(bgR).Padding(4).AlignMiddle()
                                    .Text(r.Resultado).AlignCenter()
                                    .FontColor(r.Resultado == "Completado" ? "#115511" : "#881111");
                            }
                        });
                    });

                    page.Footer().AlignCenter().Text(txt =>
                    {
                        txt.Span("Pagina ").FontSize(7).FontColor("#999999");
                        txt.CurrentPageNumber().FontSize(7).FontColor("#999999");
                        txt.Span(" de ").FontSize(7).FontColor("#999999");
                        txt.TotalPages().FontSize(7).FontColor("#999999");
                        txt.Span("   |   " + nombreEmpresa + "   |   " +
                            DateTime.Now.ToString("dd/MM/yyyy"))
                            .FontSize(7).FontColor("#999999");
                    });
                });
            });

            return doc.GeneratePdf();
        }


        // ═════════════════════════════════════════════════════════════════
        //  NOMINA — PLANILLA SEMANAL
        // ═════════════════════════════════════════════════════════════════
        public static byte[] GenerarPlanillaNomina(
            System.Collections.Generic.List<DetalleNomina> detalles,
            PeriodoNomina periodo,
            string empresa = "Mi Empresa S.A.")
        {
            var doc = Document.Create(c =>
            {
                c.Page(page =>
                {
                    page.Size(PageSizes.A4.Landscape()); page.Margin(25);
                    page.DefaultTextStyle(x => x.FontSize(8).FontFamily("Arial"));

                    page.Header().Column(col =>
                    {
                        col.Item().Row(row =>
                        {
                            row.RelativeItem().Column(c2 =>
                            {
                                c2.Item().Text(empresa).FontSize(13).Bold().FontColor("#1A5C2A");
                                c2.Item().Text("Nomina Semanal — " + periodo.DescPeriodo).FontSize(10).Bold();
                                c2.Item().Text("Tasa BCV: Bs " + periodo.TasaBCV.ToString("N2") + "/USD").FontSize(8).FontColor("#555");
                            });
                            row.ConstantItem(180).Column(c2 =>
                            {
                                decimal totalDev = 0, totalNet = 0;
                                foreach (var d in detalles) { totalDev += d.TotalDevengado; totalNet += d.NetoUSD; }
                                c2.Item().Background("#1A5C2A").Padding(6).Column(inner =>
                                {
                                    inner.Item().Row(r =>
                                    {
                                        r.RelativeItem().Text("Total devengado:").FontColor("#CCFFCC");
                                        r.ConstantItem(70).Text("$" + totalDev.ToString("N2")).FontColor("#FFF").Bold().AlignRight();
                                    });
                                    inner.Item().Row(r =>
                                    {
                                        r.RelativeItem().Text("Total neto USD:").FontColor("#CCFFCC");
                                        r.ConstantItem(70).Text("$" + totalNet.ToString("N2")).FontColor("#FFFF88").Bold().AlignRight();
                                    });
                                    inner.Item().Row(r =>
                                    {
                                        r.RelativeItem().Text("Total neto Bs:").FontColor("#CCFFCC");
                                        r.ConstantItem(70).Text("Bs " + (totalNet * periodo.TasaBCV).ToString("N0")).FontColor("#FFF").Bold().AlignRight();
                                    });
                                });
                            });
                        });
                        col.Item().Height(2).Background("#1A5C2A");
                        col.Item().Height(6);
                    });

                    page.Content().Table(table =>
                    {
                        table.ColumnsDefinition(cols =>
                        {
                            cols.RelativeColumn(2); // Empleado
                            cols.ConstantColumn(55); // Tipo
                            cols.ConstantColumn(35); // Dias
                            cols.ConstantColumn(45); // Piezas/Ventas
                            cols.ConstantColumn(65); // Dev.Sueldo
                            cols.ConstantColumn(60); // Dev.Prod
                            cols.ConstantColumn(65); // Total Dev
                            cols.ConstantColumn(50); // IVSS
                            cols.ConstantColumn(45); // RPVH
                            cols.ConstantColumn(50); // Faltas
                            cols.ConstantColumn(50); // Prestamo
                            cols.ConstantColumn(65); // Neto USD
                            cols.ConstantColumn(70); // Neto Bs
                        });

                        static IContainer HCell(IContainer c) =>
                            c.Background("#1A5C2A").Padding(3).AlignMiddle();

                        table.Header(h =>
                        {
                            foreach (var t in new[] { "Empleado","Tipo","Dias","Prod/Vta",
                                "Dev.Sueldo","Dev.Prod","Tot.Dev","IVSS 4%","RPVH 1%",
                                "Faltas","Prestamo","NETO USD","NETO Bs" })
                                h.Cell().Element(HCell).Text(t).FontColor("#FFF").Bold().AlignCenter();
                        });

                        int fi = 0;
                        foreach (var d in detalles)
                        {
                            fi++;
                            string bg = fi % 2 == 0 ? "#EEF8EE" : "#FFFFFF";
                            string bgN= "#E8FFE8";

                            static IContainer DC(IContainer c, string bg) =>
                                c.Background(bg).Padding(3).AlignMiddle();

                            decimal prodVta = d.TipoEmpleado == "Produccion" ? d.PiezasProducidas : d.VentasCerradas;

                            table.Cell().Element(c => DC(c, bg)).Text(d.NombreEmpleado);
                            table.Cell().Element(c => DC(c, bg)).Text(d.TipoEmpleado).AlignCenter();
                            table.Cell().Element(c => DC(c, bg)).Text(d.DiasTrabajados.ToString("N1")).AlignCenter();
                            table.Cell().Element(c => DC(c, bg)).Text(prodVta.ToString("N0")).AlignCenter();
                            table.Cell().Element(c => DC(c, bg)).Text("$"+d.DevengadoSueldo.ToString("N2")).AlignRight();
                            table.Cell().Element(c => DC(c, bg)).Text("$"+(d.DevengadoPiezas+d.DevengadoVentas).ToString("N2")).AlignRight();
                            table.Cell().Element(c => DC(c, bg)).Text("$"+d.TotalDevengado.ToString("N2")).Bold().AlignRight();
                            table.Cell().Element(c => DC(c, bg)).Text("$"+d.DeduccionIVSS.ToString("N2")).FontColor("#AA2020").AlignRight();
                            table.Cell().Element(c => DC(c, bg)).Text("$"+d.DeduccionRPVH.ToString("N2")).FontColor("#AA2020").AlignRight();
                            table.Cell().Element(c => DC(c, bg)).Text("$"+d.DescuentoFaltas.ToString("N2")).AlignRight();
                            table.Cell().Element(c => DC(c, bg)).Text("$"+d.CuotaPrestamo.ToString("N2")).AlignRight();
                            table.Cell().Background(bgN).Padding(3).AlignMiddle().Text("$"+d.NetoUSD.ToString("N2")).Bold().FontColor("#115511").AlignRight();
                            table.Cell().Background(bgN).Padding(3).AlignMiddle().Text("Bs "+d.NetoBs.ToString("N0")).FontColor("#115511").AlignRight();
                        }

                        // Fila de totales
                        static IContainer TCell(IContainer c) =>
                            c.Background("#1A5C2A").Padding(3).AlignMiddle();

                        decimal sumDev = detalles.Sum(d => d.TotalDevengado);
                        decimal sumNet = detalles.Sum(d => d.NetoUSD);
                        decimal tasa   = detalles.Count > 0 ? detalles[0].TasaBCV : 1;

                        table.Cell().ColumnSpan(6).Element(TCell).Text("TOTALES (" + detalles.Count + " empleados)").FontColor("#FFF").Bold().AlignRight();
                        table.Cell().Element(TCell).Text("$"+sumDev.ToString("N2")).FontColor("#FFF").Bold().AlignRight();
                        table.Cell().ColumnSpan(4).Element(TCell).Text("");
                        table.Cell().Element(TCell).Text("$"+sumNet.ToString("N2")).FontColor("#FFFF88").Bold().AlignRight();
                        table.Cell().Element(TCell).Text("Bs "+(sumNet*tasa).ToString("N0")).FontColor("#FFFF88").Bold().AlignRight();
                    });

                    page.Footer().AlignCenter().Text(txt =>
                    {
                        txt.Span("Pag. ").FontSize(7).FontColor("#999");
                        txt.CurrentPageNumber().FontSize(7).FontColor("#999");
                        txt.Span(" de ").FontSize(7).FontColor("#999");
                        txt.TotalPages().FontSize(7).FontColor("#999");
                        txt.Span("   |   Generado: " + DateTime.Now.ToString("dd/MM/yyyy HH:mm")).FontSize(7).FontColor("#999");
                    });
                });
            });
            return doc.GeneratePdf();
        }

        // ═════════════════════════════════════════════════════════════════
        //  NOMINA — PLANILLA IVSS (resumen mensual)
        // ═════════════════════════════════════════════════════════════════
        public static byte[] GenerarPlanillaIVSS(
            System.Collections.Generic.List<DetalleNomina> detalles,
            PeriodoNomina periodo,
            string empresa = "Mi Empresa S.A.")
        {
            var doc = Document.Create(c =>
            {
                c.Page(page =>
                {
                    page.Size(PageSizes.A4); page.Margin(30);
                    page.DefaultTextStyle(x => x.FontSize(9).FontFamily("Arial"));

                    page.Header().Column(col =>
                    {
                        col.Item().Text(empresa).FontSize(14).Bold().FontColor("#1A3A7C");
                        col.Item().Text("Planilla IVSS / RPVH — Periodo: " + periodo.DescPeriodo).FontSize(11).Bold();
                        col.Item().Text("Tasa BCV: Bs " + periodo.TasaBCV.ToString("N2") + "/USD  |  Fecha: " +
                            DateTime.Today.ToString("dd/MM/yyyy")).FontSize(8).FontColor("#555");
                        col.Item().Height(2).Background("#1A3A7C");
                        col.Item().Height(8);
                    });

                    page.Content().Table(table =>
                    {
                        table.ColumnsDefinition(cols =>
                        {
                            cols.RelativeColumn(2);
                            cols.ConstantColumn(80);
                            cols.ConstantColumn(80);
                            cols.ConstantColumn(70);
                            cols.ConstantColumn(70);
                            cols.ConstantColumn(70);
                        });

                        static IContainer HCell(IContainer c) =>
                            c.Background("#1A3A7C").Padding(4).AlignMiddle();

                        table.Header(h =>
                        {
                            foreach (var t in new[] { "Empleado","Salario USD","Salario Bs","IVSS Trab 4%","RPVH Trab 1%","Total Bs" })
                                h.Cell().Element(HCell).Text(t).FontColor("#FFF").Bold().AlignCenter();
                        });

                        int fi = 0;
                        decimal sumIVSS = 0, sumRPVH = 0;
                        foreach (var d in detalles)
                        {
                            fi++;
                            string bg = fi % 2 == 0 ? "#EEF2FF" : "#FFF";
                            static IContainer DC(IContainer c, string bg) =>
                                c.Background(bg).Padding(4).AlignMiddle();

                            decimal salBs  = d.TotalDevengado * d.TasaBCV;
                            decimal ivss   = d.DeduccionIVSS * d.TasaBCV;
                            decimal rpvh   = d.DeduccionRPVH * d.TasaBCV;
                            sumIVSS += ivss; sumRPVH += rpvh;

                            table.Cell().Element(c => DC(c, bg)).Text(d.NombreEmpleado);
                            table.Cell().Element(c => DC(c, bg)).Text("$"+d.TotalDevengado.ToString("N2")).AlignRight();
                            table.Cell().Element(c => DC(c, bg)).Text("Bs "+salBs.ToString("N0")).AlignRight();
                            table.Cell().Element(c => DC(c, bg)).Text("Bs "+ivss.ToString("N2")).AlignRight().FontColor("#AA2020");
                            table.Cell().Element(c => DC(c, bg)).Text("Bs "+rpvh.ToString("N2")).AlignRight().FontColor("#AA2020");
                            table.Cell().Element(c => DC(c, bg)).Text("Bs "+(ivss+rpvh).ToString("N2")).Bold().AlignRight();
                        }

                        static IContainer TCell(IContainer c) =>
                            c.Background("#1A3A7C").Padding(4).AlignMiddle();

                        table.Cell().ColumnSpan(3).Element(TCell).Text("TOTALES A ENTERAR AL IVSS").FontColor("#FFF").Bold().AlignRight();
                        table.Cell().Element(TCell).Text("Bs "+sumIVSS.ToString("N2")).FontColor("#FFFF88").Bold().AlignRight();
                        table.Cell().Element(TCell).Text("Bs "+sumRPVH.ToString("N2")).FontColor("#FFFF88").Bold().AlignRight();
                        table.Cell().Element(TCell).Text("Bs "+(sumIVSS+sumRPVH).ToString("N2")).FontColor("#FFFF88").Bold().AlignRight();
                    });
                });
            });
            return doc.GeneratePdf();
        }

        // ═════════════════════════════════════════════════════════════════
        //  NOMINA — REPORTE BENEFICIOS LOTTT
        // ═════════════════════════════════════════════════════════════════
        public static byte[] GenerarReporteBeneficios(
            System.Collections.Generic.List<BeneficioAnual> beneficios,
            int anio,
            string empresa = "Mi Empresa S.A.")
        {
            var doc = Document.Create(c =>
            {
                c.Page(page =>
                {
                    page.Size(PageSizes.A4); page.Margin(28);
                    page.DefaultTextStyle(x => x.FontSize(9).FontFamily("Arial"));

                    page.Header().Column(col =>
                    {
                        col.Item().Text(empresa).FontSize(14).Bold().FontColor("#5A1A8C");
                        col.Item().Text("Beneficios Anuales LOTTT — Ano " + anio).FontSize(11).Bold();
                        col.Item().Height(2).Background("#5A1A8C");
                        col.Item().Height(8);
                    });

                    page.Content().Table(table =>
                    {
                        table.ColumnsDefinition(cols =>
                        {
                            cols.RelativeColumn(2);
                            cols.ConstantColumn(90);
                            cols.ConstantColumn(45);
                            cols.ConstantColumn(75);
                            cols.ConstantColumn(75);
                            cols.ConstantColumn(70);
                            cols.ConstantColumn(65);
                        });

                        static IContainer HCell(IContainer c) =>
                            c.Background("#5A1A8C").Padding(4).AlignMiddle();

                        table.Header(h =>
                        {
                            foreach (var t in new[] { "Empleado","Tipo Beneficio","Dias","Sal.Diario USD","Monto USD","Monto Bs","Estado" })
                                h.Cell().Element(HCell).Text(t).FontColor("#FFF").Bold().AlignCenter();
                        });

                        int fi = 0;
                        foreach (var b in beneficios)
                        {
                            fi++;
                            string bg = fi % 2 == 0 ? "#F5F0FF" : "#FFF";
                            string bgE = b.Estado == "Pagado" ? "#E8FFE8" : "#FFF8E0";
                            string fgE = b.Estado == "Pagado" ? "#115511" : "#885500";

                            static IContainer DC(IContainer c, string bg) =>
                                c.Background(bg).Padding(4).AlignMiddle();

                            table.Cell().Element(c => DC(c, bg)).Text(b.NombreEmpleado);
                            table.Cell().Element(c => DC(c, bg)).Text(b.TipoBeneficio).AlignCenter();
                            table.Cell().Element(c => DC(c, bg)).Text(b.DiasCorresponden.ToString("N0")).AlignCenter();
                            table.Cell().Element(c => DC(c, bg)).Text("$"+b.SalarioDiarioUSD.ToString("N2")).AlignRight();
                            table.Cell().Element(c => DC(c, bg)).Text("$"+b.MontoUSD.ToString("N2")).Bold().AlignRight();
                            table.Cell().Element(c => DC(c, bg)).Text("Bs "+b.MontoBs.ToString("N0")).AlignRight();
                            table.Cell().Background(bgE).Padding(4).AlignMiddle().Text(b.Estado).FontColor(fgE).Bold().AlignCenter();
                        }

                        static IContainer TCell(IContainer c) =>
                            c.Background("#5A1A8C").Padding(4).AlignMiddle();

                        decimal total = beneficios.Sum(b => b.MontoUSD);
                        decimal pend  = beneficios.Where(b => b.Estado == "Pendiente").Sum(b => b.MontoUSD);

                        table.Cell().ColumnSpan(4).Element(TCell).Text("TOTALES").FontColor("#FFF").Bold().AlignRight();
                        table.Cell().Element(TCell).Text("$"+total.ToString("N2")).FontColor("#FFFF88").Bold().AlignRight();
                        table.Cell().Element(TCell).Text("Pend: $"+pend.ToString("N2")).FontColor("#FFCC88").Bold().AlignRight();
                        table.Cell().Element(TCell).Text("");
                    });
                });
            });
            return doc.GeneratePdf();
        }


        // ═════════════════════════════════════════════════════════════════
        //  NOMINA — PLANILLA DE APORTES PATRONALES
        // ═════════════════════════════════════════════════════════════════
        public static byte[] GenerarPlanillaAportesPatronales(
            System.Collections.Generic.List<AportePatronal> aportes,
            PeriodoNomina periodo,
            ResumenAportesPatronales resumen,
            string empresa = "Mi Empresa S.A.")
        {
            var doc = Document.Create(c =>
            {
                c.Page(page =>
                {
                    page.Size(PageSizes.A4.Landscape()); page.Margin(25);
                    page.DefaultTextStyle(x => x.FontSize(8).FontFamily("Arial"));

                    page.Header().Column(col =>
                    {
                        col.Item().Row(row =>
                        {
                            row.RelativeItem().Column(c2 =>
                            {
                                c2.Item().Text(empresa).FontSize(13).Bold().FontColor("#5A1A8C");
                                c2.Item().Text("Planilla de Aportes Patronales — " + periodo.DescPeriodo)
                                    .FontSize(10).Bold();
                                c2.Item().Text(
                                    "Tasa BCV: Bs " + periodo.TasaBCV.ToString("N2") + "/USD  |  " +
                                    "Fecha: " + System.DateTime.Today.ToString("dd/MM/yyyy"))
                                    .FontSize(8).FontColor("#555");
                            });
                            row.ConstantItem(230).Column(c2 =>
                            {
                                c2.Item().Background("#5A1A8C").Padding(6).Column(inner =>
                                {
                                    inner.Item().Row(r =>
                                    {
                                        r.RelativeItem().Text("IVSS Patron:").FontColor("#DDD0FF");
                                        r.ConstantItem(90).Text("Bs " + resumen.IVSSPatronBs.ToString("N2"))
                                            .FontColor("#FFF").Bold().AlignRight();
                                    });
                                    inner.Item().Row(r =>
                                    {
                                        r.RelativeItem().Text("RPVH/FAOV Patron:").FontColor("#DDD0FF");
                                        r.ConstantItem(90).Text("Bs " + resumen.RPVHPatronBs.ToString("N2"))
                                            .FontColor("#FFF").Bold().AlignRight();
                                    });
                                    inner.Item().Row(r =>
                                    {
                                        r.RelativeItem().Text("INCES (2%):").FontColor("#DDD0FF");
                                        r.ConstantItem(90).Text("Bs " + resumen.INCESBs.ToString("N2"))
                                            .FontColor("#FFF").Bold().AlignRight();
                                    });
                                    inner.Item().Row(r =>
                                    {
                                        r.RelativeItem().Text("Bono CESTA:").FontColor("#DDD0FF");
                                        r.ConstantItem(90).Text("Bs " + resumen.CestaBs.ToString("N2"))
                                            .FontColor("#FFFF88").Bold().AlignRight();
                                    });
                                    inner.Item().Height(1).Background("#FFFFFF");
                                    inner.Item().Row(r =>
                                    {
                                        r.RelativeItem().Text("TOTAL APORTES:").FontColor("#DDD0FF").Bold();
                                        r.ConstantItem(90).Text("Bs " + resumen.TotalAportesBs.ToString("N2"))
                                            .FontColor("#FFFF44").Bold().AlignRight();
                                    });
                                    inner.Item().Row(r =>
                                    {
                                        r.RelativeItem().Text("Costo total nomina:").FontColor("#DDD0FF");
                                        r.ConstantItem(90).Text("Bs " + resumen.CostoTotalBs.ToString("N2"))
                                            .FontColor("#FFFFFF").Bold().AlignRight();
                                    });
                                });
                            });
                        });
                        col.Item().Height(2).Background("#5A1A8C");
                        col.Item().Height(6);
                    });

                    page.Content().Column(body =>
                    {
                        // Nota legal
                        body.Item().Background("#F5F0FF").Padding(6).Column(nota =>
                        {
                            nota.Item().Text(
                                "Obligaciones legales del empleador (Venezuela):  " +
                                "IVSS Patron: 9%-11% segun nivel de riesgo (art. 105 LSS)  |  " +
                                "RPVH/FAOV Patron: 2% salario normal (art. 172 LOSSS)  |  " +
                                "INCES: 2% nomina total (art. 10 LINCES)  |  " +
                                "Bono CESTA: 0.5 UT x dia laboral (LCA, exento ISLR)")
                                .FontSize(7.5f).FontColor("#5A1A8C");
                        });
                        body.Item().Height(8);

                        body.Item().Table(table =>
                        {
                            table.ColumnsDefinition(cols =>
                            {
                                cols.RelativeColumn(2);  // Empleado
                                cols.ConstantColumn(75); // Sal Bs
                                cols.ConstantColumn(45); // % IVSS
                                cols.ConstantColumn(80); // IVSS Patron
                                cols.ConstantColumn(80); // RPVH Patron
                                cols.ConstantColumn(70); // INCES
                                cols.ConstantColumn(45); // Dias
                                cols.ConstantColumn(75); // ValUT
                                cols.ConstantColumn(75); // CESTA
                                cols.ConstantColumn(85); // Total Bs
                                cols.ConstantColumn(75); // Total USD
                            });

                            static IContainer HCell(IContainer c) =>
                                c.Background("#5A1A8C").Padding(3).AlignMiddle();

                            table.Header(h =>
                            {
                                foreach (var t in new[] {
                                    "Empleado","Sal.Normal Bs","% IVSS",
                                    "IVSS Patron","RPVH/FAOV","INCES 2%",
                                    "Dias","Valor UT","CESTA","TOTAL Bs","TOTAL USD" })
                                    h.Cell().Element(HCell).Text(t).FontColor("#FFF").Bold().AlignCenter();
                            });

                            int fi = 0;
                            foreach (var a in aportes)
                            {
                                fi++;
                                string bg  = fi % 2 == 0 ? "#F5F0FF" : "#FFF";
                                string bgT = "#EEE5FF";

                                static IContainer DC(IContainer c, string bg) =>
                                    c.Background(bg).Padding(3).AlignMiddle();

                                table.Cell().Element(c => DC(c, bg)).Text(a.NombreEmpleado);
                                table.Cell().Element(c => DC(c, bg)).Text("Bs "+a.SalarioNormalBs.ToString("N2")).AlignRight();
                                table.Cell().Element(c => DC(c, bg)).Text(a.PorcentajeIVSS.ToString("N1")+"%").AlignCenter();
                                table.Cell().Element(c => DC(c, bg)).Text("Bs "+a.IVSSPatronBs.ToString("N2")).AlignRight().FontColor("#7700AA");
                                table.Cell().Element(c => DC(c, bg)).Text("Bs "+a.RPVHPatronBs.ToString("N2")).AlignRight().FontColor("#7700AA");
                                table.Cell().Element(c => DC(c, bg)).Text("Bs "+a.INCESBs.ToString("N2")).AlignRight().FontColor("#7700AA");
                                table.Cell().Element(c => DC(c, bg)).Text(a.DiasLaborales.ToString("N1")).AlignCenter();
                                table.Cell().Element(c => DC(c, bg)).Text("Bs "+a.ValorUTBs.ToString("N2")).AlignRight();
                                table.Cell().Element(c => DC(c, bg)).Text("Bs "+a.CestaBs.ToString("N2")).AlignRight().FontColor("#0050AA");
                                table.Cell().Background(bgT).Padding(3).AlignMiddle()
                                    .Text("Bs "+a.TotalAportesBs.ToString("N2")).Bold().FontColor("#5A1A8C").AlignRight();
                                table.Cell().Background(bgT).Padding(3).AlignMiddle()
                                    .Text(a.TotalAportesUSD.ToString("C2")).Bold().FontColor("#0050AA").AlignRight();
                            }

                            // Fila de totales
                            static IContainer TCell(IContainer c) =>
                                c.Background("#5A1A8C").Padding(3).AlignMiddle();

                            table.Cell().Element(TCell).Text("TOTALES ("+aportes.Count+" empleados)").FontColor("#FFF").Bold();
                            table.Cell().Element(TCell).Text("Bs "+resumen.NominaTotalBs.ToString("N2")).FontColor("#DDD0FF").AlignRight();
                            table.Cell().Element(TCell).Text("");
                            table.Cell().Element(TCell).Text("Bs "+resumen.IVSSPatronBs.ToString("N2")).FontColor("#FFFF88").Bold().AlignRight();
                            table.Cell().Element(TCell).Text("Bs "+resumen.RPVHPatronBs.ToString("N2")).FontColor("#FFFF88").Bold().AlignRight();
                            table.Cell().Element(TCell).Text("Bs "+resumen.INCESBs.ToString("N2")).FontColor("#FFFF88").Bold().AlignRight();
                            table.Cell().Element(TCell).Text("");
                            table.Cell().Element(TCell).Text("");
                            table.Cell().Element(TCell).Text("Bs "+resumen.CestaBs.ToString("N2")).FontColor("#AADDFF").Bold().AlignRight();
                            table.Cell().Element(TCell).Text("Bs "+resumen.TotalAportesBs.ToString("N2")).FontColor("#FFFF44").Bold().AlignRight();
                            table.Cell().Element(TCell).Text("$"+(resumen.NominaTotal).ToString("N2")).FontColor("#FFFF44").Bold().AlignRight();
                        });
                    });

                    page.Footer().AlignCenter().Text(txt =>
                    {
                        txt.Span("Pag. ").FontSize(7).FontColor("#999");
                        txt.CurrentPageNumber().FontSize(7).FontColor("#999");
                        txt.Span(" de ").FontSize(7).FontColor("#999");
                        txt.TotalPages().FontSize(7).FontColor("#999");
                        txt.Span("   |   Generado: " + System.DateTime.Now.ToString("dd/MM/yyyy HH:mm"))
                            .FontSize(7).FontColor("#999");
                    });
                });
            });
            return doc.GeneratePdf();
        }


        // ═════════════════════════════════════════════════════════════════
        //  FLUJO DE CAJA
        // ═════════════════════════════════════════════════════════════════
        public static byte[] GenerarFlujoCaja(
            System.Collections.Generic.List<MovimientoCaja> movimientos,
            ResumenFlujoCaja resumen,
            string empresa = "Mi Empresa S.A.")
        {
            var doc = Document.Create(c =>
            {
                c.Page(page =>
                {
                    page.Size(PageSizes.A4); page.Margin(28);
                    page.DefaultTextStyle(x => x.FontSize(9).FontFamily("Arial"));

                    page.Header().Column(col =>
                    {
                        col.Item().Row(row =>
                        {
                            row.RelativeItem().Column(c2 =>
                            {
                                c2.Item().Text(empresa).FontSize(13).Bold().FontColor("#143C7C");
                                c2.Item().Text("Flujo de Caja Proyectado — 90 dias").FontSize(10).Bold();
                                c2.Item().Text("Generado: " + System.DateTime.Today.ToString("dd/MM/yyyy") +
                                    "  |  Saldo inicial: $" + resumen.SaldoInicial.ToString("N2"))
                                    .FontSize(8).FontColor("#555");
                            });
                            row.ConstantItem(200).Column(c2 =>
                            {
                                c2.Item().Background("#143C7C").Padding(5).Column(inner =>
                                {
                                    inner.Item().Row(r =>
                                    {
                                        r.RelativeItem().Text("Saldo a 30d:").FontColor("#BBDDFF");
                                        r.ConstantItem(80).Text("$"+resumen.SaldoProyectado30.ToString("N0"))
                                            .FontColor(resumen.SaldoProyectado30>=0?"#88FF88":"#FF8888")
                                            .Bold().AlignRight();
                                    });
                                    inner.Item().Row(r =>
                                    {
                                        r.RelativeItem().Text("Saldo a 60d:").FontColor("#BBDDFF");
                                        r.ConstantItem(80).Text("$"+resumen.SaldoProyectado60.ToString("N0"))
                                            .FontColor(resumen.SaldoProyectado60>=0?"#88FF88":"#FF8888")
                                            .Bold().AlignRight();
                                    });
                                    inner.Item().Row(r =>
                                    {
                                        r.RelativeItem().Text("Saldo a 90d:").FontColor("#BBDDFF");
                                        r.ConstantItem(80).Text("$"+resumen.SaldoProyectado90.ToString("N0"))
                                            .FontColor(resumen.SaldoProyectado90>=0?"#FFFF44":"#FF8888")
                                            .Bold().AlignRight();
                                    });
                                });
                            });
                        });
                        col.Item().Height(2).Background("#143C7C");
                        col.Item().Height(6);
                    });

                    page.Content().Column(body =>
                    {
                        // Resumen por tramo
                        body.Item().Table(tbl =>
                        {
                            tbl.ColumnsDefinition(cols =>
                            {
                                cols.RelativeColumn();
                                cols.ConstantColumn(80);
                                cols.ConstantColumn(80);
                                cols.ConstantColumn(80);
                                cols.ConstantColumn(80);
                            });
                            static IContainer HCell(IContainer c) =>
                                c.Background("#143C7C").Padding(4).AlignMiddle();
                            tbl.Header(h =>
                            {
                                h.Cell().Element(HCell).Text("Tramo").FontColor("#FFF").Bold();
                                h.Cell().Element(HCell).Text("Ingresos").FontColor("#FFF").Bold().AlignRight();
                                h.Cell().Element(HCell).Text("Egresos").FontColor("#FFF").Bold().AlignRight();
                                h.Cell().Element(HCell).Text("Neto").FontColor("#FFF").Bold().AlignRight();
                                h.Cell().Element(HCell).Text("Saldo acum.").FontColor("#FFF").Bold().AlignRight();
                            });
                            var tramos = new[]
                            {
                                ("0 - 30 dias",  resumen.IngreSos30, resumen.Egresos30, resumen.NetoAcum30, resumen.SaldoProyectado30),
                                ("31 - 60 dias", resumen.IngreSos60, resumen.Egresos60, resumen.NetoAcum60, resumen.SaldoProyectado60),
                                ("61 - 90 dias", resumen.IngreSos90, resumen.Egresos90, resumen.NetoAcum90, resumen.SaldoProyectado90),
                            };
                            int fi = 0;
                            foreach (var (etq, ing, egr, net, sal) in tramos)
                            {
                                fi++;
                                string bg  = fi%2==0?"#EEF4FF":"#FFF";
                                string fgN = net>=0?"#1A7040":"#CC0000";
                                string fgS = sal>=0?"#1A7040":"#CC0000";
                                static IContainer DC(IContainer c,string bg)=>c.Background(bg).Padding(4).AlignMiddle();
                                tbl.Cell().Element(c=>DC(c,bg)).Text(etq).Bold();
                                tbl.Cell().Element(c=>DC(c,bg)).Text("$"+ing.ToString("N2")).AlignRight().FontColor("#1A7040");
                                tbl.Cell().Element(c=>DC(c,bg)).Text("$"+egr.ToString("N2")).AlignRight().FontColor("#CC0000");
                                tbl.Cell().Element(c=>DC(c,bg)).Text("$"+net.ToString("N2")).AlignRight().Bold().FontColor(fgN);
                                tbl.Cell().Element(c=>DC(c,bg)).Text("$"+sal.ToString("N2")).AlignRight().Bold().FontColor(fgS);
                            }
                        });

                        body.Item().Height(14);

                        // Detalle de movimientos
                        body.Item().Text("Detalle de movimientos").FontSize(10).Bold().FontColor("#143C7C");
                        body.Item().Height(4);
                        body.Item().Table(tbl =>
                        {
                            tbl.ColumnsDefinition(cols =>
                            {
                                cols.ConstantColumn(65);
                                cols.ConstantColumn(40);
                                cols.ConstantColumn(60);
                                cols.ConstantColumn(70);
                                cols.RelativeColumn();
                                cols.ConstantColumn(80);
                            });
                            static IContainer HCell(IContainer c) =>
                                c.Background("#143C7C").Padding(3).AlignMiddle();
                            tbl.Header(h =>
                            {
                                foreach (var t in new[]{"Fecha","Dias","Tipo","Categoria","Concepto","Monto USD"})
                                    h.Cell().Element(HCell).Text(t).FontColor("#FFF").Bold().AlignCenter();
                            });
                            var hoy2 = System.DateTime.Today;
                            int fi = 0;
                            foreach (var m in movimientos.OrderBy(m=>m.Fecha).Take(80))
                            {
                                fi++;
                                string bg  = m.Tipo=="Ingreso"?"#F0FFF4":"#FFF5F5";
                                string fgM = m.Tipo=="Ingreso"?"#1A7040":"#CC0000";
                                static IContainer DC(IContainer c,string bg)=>c.Background(bg).Padding(3).AlignMiddle();
                                tbl.Cell().Element(c=>DC(c,bg)).Text(m.Fecha.ToString("dd/MM/yy")).AlignCenter();
                                tbl.Cell().Element(c=>DC(c,bg)).Text(((int)(m.Fecha-hoy2).TotalDays)+"d").AlignCenter();
                                tbl.Cell().Element(c=>DC(c,bg)).Text(m.Tipo).AlignCenter().FontColor(fgM).Bold();
                                tbl.Cell().Element(c=>DC(c,bg)).Text(m.Categoria).AlignCenter();
                                tbl.Cell().Element(c=>DC(c,bg)).Text(m.Concepto);
                                tbl.Cell().Element(c=>DC(c,bg)).Text("$"+m.MontoUSD.ToString("N2")).AlignRight().Bold().FontColor(fgM);
                            }
                        });
                    });

                    page.Footer().AlignCenter().Text(txt =>
                    {
                        txt.Span("Proyeccion basada en datos del sistema. No constituye garantia de flujo real. Pag. ")
                            .FontSize(7).FontColor("#999");
                        txt.CurrentPageNumber().FontSize(7).FontColor("#999");
                        txt.Span(" de ").FontSize(7).FontColor("#999");
                        txt.TotalPages().FontSize(7).FontColor("#999");
                    });
                });
            });
            return doc.GeneratePdf();
        }

        /// <summary>Guarda el PDF en disco y lo abre con el visor predeterminado.</summary>
        public static void GuardarYAbrir(byte[] pdf, string nombreSugerido)
        {
            var dlg = new System.Windows.Forms.SaveFileDialog
            {
                Title      = "Guardar PDF",
                Filter     = "PDF (*.pdf)|*.pdf",
                FileName   = nombreSugerido,
                DefaultExt = "pdf"
            };

            if (dlg.ShowDialog() != System.Windows.Forms.DialogResult.OK) return;

            File.WriteAllBytes(dlg.FileName, pdf);

            // Abrir con el visor PDF del sistema
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName        = dlg.FileName,
                UseShellExecute = true
            });

            DatabaseService.RegistrarAuditoria("PDF_EXPORTADO", "Facturas",
                "Archivo: " + System.IO.Path.GetFileName(dlg.FileName));
        }

        /// <summary>Imprime el PDF directamente via PrintDocument (shell print).</summary>
        public static void Imprimir(byte[] pdf, string nombreTmp = "factura_tmp.pdf")
        {
            string ruta = Path.Combine(Path.GetTempPath(), nombreTmp);
            File.WriteAllBytes(ruta, pdf);

            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName        = ruta,
                Verb            = "print",
                UseShellExecute = true,
                CreateNoWindow  = true
            });

            DatabaseService.RegistrarAuditoria("PDF_IMPRESO", "Facturas", nombreTmp);
        }

        /// <summary>
        /// Muestra un diálogo Guardar y devuelve la ruta elegida, o string vacío si se canceló.
        /// Usado por servicios PDF que escriben directamente al disco (ej. PdfService_ISLR).
        /// </summary>
        public static string GuardarRuta(string nombreSugerido)
        {
            var dlg = new System.Windows.Forms.SaveFileDialog
            {
                Title      = "Guardar PDF",
                Filter     = "PDF (*.pdf)|*.pdf",
                FileName   = nombreSugerido,
                DefaultExt = "pdf"
            };
            return dlg.ShowDialog() == System.Windows.Forms.DialogResult.OK
                ? dlg.FileName
                : string.Empty;
        }

        /// <summary>
        /// Abre un archivo PDF (u otro) con el visor predeterminado del sistema.
        /// </summary>
        public static void Abrir(string ruta)
        {
            if (string.IsNullOrEmpty(ruta) || !File.Exists(ruta)) return;
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName        = ruta,
                UseShellExecute = true
            });
        }
    }
}
