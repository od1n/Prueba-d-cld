using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace SistemaGestion
{
    // ══════════════════════════════════════════════════════════════
    //  MODELO AUXILIAR
    // ══════════════════════════════════════════════════════════════
    public class LineaVentaReporte
    {
        public string   NumFactura   { get; set; }
        public DateTime Fecha        { get; set; }
        public string   Codigo       { get; set; }
        public string   Descripcion  { get; set; }
        public decimal  Cantidad     { get; set; }
        public decimal  PrecioUnit   { get; set; }
        public decimal  Total        { get; set; }
        // Tasa del día de la factura (0 si no hay registrada)
        public decimal  TasaBCV      { get; set; }
        public decimal  TasaParalelo { get; set; }
        // Totales en Bs calculados
        public decimal  TotalBCV      => TasaBCV      > 0 ? Total * TasaBCV      : 0;
        public decimal  TotalParalelo => TasaParalelo > 0 ? Total * TasaParalelo : 0;
    }
    // ══════════════════════════════════════════════════════════════
    //  FrmArchivos — rediseñado con tarjetas wash de color
    // ══════════════════════════════════════════════════════════════
    public class FrmArchivos : FormBase
    {
        public FrmArchivos()
        {
            Text            = "Archivos del Sistema";
            ClientSize      = new Size(960, 600);
            MinimumSize     = new Size(700, 460);
            StartPosition   = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.Sizable;
            MaximizeBox     = true;
            BackColor       = UITheme.Fondo;
            Font            = UITheme.Body;

            // ── Header oscuro consistente ─────────────────────────────────
            var hdr = new Panel
            {
                Dock = DockStyle.Top, Height = 60,
                BackColor = UITheme.AzulOscuro
            };
            hdr.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                using var fTit = new Font("Segoe UI", 14f, FontStyle.Bold);
                e.Graphics.DrawString("Archivos del Sistema", fTit, Brushes.White, 20, 12);
                using var fSub = new Font("Segoe UI", 9.5f);
                using var brSub = new SolidBrush(Color.FromArgb(168, 177, 187));
                e.Graphics.DrawString("Catálogos maestros, inventario, accesos y configuración",
                    fSub, brSub, 22, 34);
            };

            // ── Footer claro unificado ────────────────────────────────────
            var footer = new Panel
            {
                Dock = DockStyle.Bottom, Height = 52,
                BackColor = UITheme.FondoCard
            };
            footer.Paint += (s, e) =>
            {
                using var pen = new Pen(UITheme.Borde, 1f);
                e.Graphics.DrawLine(pen, 0, 0, footer.Width, 0);
            };
            var btnCerrar = new Button
            {
                Text = "Cerrar", Width = 130, Height = 36,
                FlatStyle = FlatStyle.Flat,
                BackColor = UITheme.Azul, ForeColor = Color.White,
                Font = new Font("Segoe UI", 10f, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnCerrar.FlatAppearance.BorderSize = 0;
            btnCerrar.FlatAppearance.MouseOverBackColor = UITheme.AzulHover;
            btnCerrar.Click += (s, e) => Close();
            var flowR = new FlowLayoutPanel
            {
                Dock = DockStyle.Right, FlowDirection = FlowDirection.RightToLeft,
                BackColor = UITheme.FondoCard, Width = 170, WrapContents = false,
                Padding = new Padding(0, 7, 16, 7)
            };
            flowR.Controls.Add(btnCerrar);
            footer.Controls.Add(flowR);

            // ── Definición de módulos ─────────────────────────────────────
            // (titulo, subtitulo, color, accion, soloAdmin, tooltip)
            var modulos = new System.Collections.Generic.List<(string ico, string titulo, string sub,
                Color color, Action accion, bool soloAdmin, string tip)>
            {
                ("◧", "Inventario",      "Productos terminados",
                    UITheme.Verde,                    () => new FrmInventario().ShowDialog(),         false,
                    "Catálogo de productos terminados: códigos, precios y stock"),

                ("◈", "Clientes",        "Directorio de clientes",
                    UITheme.Azul,                     () => new FrmClientes().ShowDialog(),           false,
                    "Directorio de clientes para facturación"),

                ("◫", "Proveedores",     "Directorio de proveedores",
                    UITheme.Naranja,                  () => new FrmProveedores().ShowDialog(),        false,
                    "Directorio de proveedores de materias primas y servicios"),

                ("▣", "Moldes",          "Catálogo de moldes",
                    UITheme.Cyan,                     () => new FrmMoldes().ShowDialog(),             false,
                    "Catálogo de moldes de producción y sus productos"),

                ("⊞", "Mant. Moldes",    "Mantenimiento preventivo",
                    Color.FromArgb(20, 80, 120),      () => new FrmMantenimientoMoldes().ShowDialog(), false,
                    "Registro de mantenimientos y reparaciones de moldes"),

                ("▤", "Kardex",          "Movimientos inventario",
                    Color.FromArgb(0, 150, 136),      () => new FrmKardex().ShowDialog(),             false,
                    "Movimientos de inventario de productos e insumos"),

                ("◎", "Insumos Proveed.","Materias primas",
                    Color.FromArgb(45, 90, 175),      () => new FrmProductosProveedor().ShowDialog(), false,
                    "Control de materias primas y stock de insumos"),

                ("◈", "Control Calidad", "Inspecciones",
                    Color.FromArgb(120, 60, 140),     () => new FrmControlCalidad().ShowDialog(),     false,
                    "Registro de inspecciones y control de calidad"),

                ("▣", "Respaldos",       "Copias de seguridad",
                    Color.FromArgb(60, 100, 140),     () => new FrmRespaldos().ShowDialog(),          true,
                    "Copias de seguridad de la base de datos (solo Admin)"),

                ("◎", "Usuarios",        "Gestión de accesos",
                    UITheme.Rojo,                     () => new FrmUsuarios().ShowDialog(),           true,
                    "Gestión de usuarios y permisos del sistema (solo Admin)"),
            };

            // ── Área scrollable con cards ─────────────────────────────────
            var arcTip = AyudaService.CrearTooltip();
            var pnlScroll = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = UITheme.Fondo,
                AutoScroll = true,
                Padding = new Padding(28, 24, 28, 24)
            };

            var tarjetas = new System.Collections.Generic.List<Panel>();
            foreach (var (ico, titulo, sub, color, accion, soloAdmin, textoTip) in modulos)
            {
                if (soloAdmin && !Sesion.EsAdmin) continue;
                var card = UICards.CrearTarjeta(ico, titulo, sub, color, accion,
                    tieneAcceso: true, tip: arcTip, textoTooltip: textoTip);
                tarjetas.Add(card);
                pnlScroll.Controls.Add(card);
            }

            // Distribución responsive scrollable con buen aire entre tarjetas
            UICards.DistribuirGridScrollable(pnlScroll, tarjetas,
                minAncho: 240, alto: 124, gap: 24, padX: 8, padY: 8);

            // Orden de Add: Fill primero, después Top/Bottom
            Controls.Add(pnlScroll);
            Controls.Add(footer);
            Controls.Add(hdr);
            // VentanaPrefs.Restaurar lo invoca FormBase.OnLoad automáticamente
        }
    }
}
