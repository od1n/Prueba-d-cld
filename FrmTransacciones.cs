using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace SistemaGestion
{
    public class FrmTransacciones : FormBase
    {
        public FrmTransacciones()
        {
            Text = "Transacciones";
            ClientSize = new Size(880, 500);
            MinimumSize = new Size(640, 380);
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.Sizable;
            MaximizeBox = true;
            BackColor = UITheme.Fondo;
            Font = UITheme.Body;

            // ── Header oscuro ───────────────────────────────────────────────
            var hdr = new Panel
            {
                Dock = DockStyle.Top, Height = 60,
                BackColor = UITheme.AzulOscuro
            };
            hdr.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                using var fTit = new Font("Segoe UI", 14f, FontStyle.Bold);
                e.Graphics.DrawString("Transacciones", fTit, Brushes.White, 20, 12);
                using var fSub = new Font("Segoe UI", 9.5f);
                using var brSub = new SolidBrush(Color.FromArgb(168, 177, 187));
                e.Graphics.DrawString("Operaciones de venta, cobros, precios y egresos",
                    fSub, brSub, 22, 34);
            };

            // ── Footer claro unificado ──────────────────────────────────────
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
                Cursor = Cursors.Hand,
                Margin = new Padding(0, 7, 16, 7)
            };
            btnCerrar.FlatAppearance.BorderSize = 0;
            btnCerrar.FlatAppearance.MouseOverBackColor = UITheme.AzulHover;
            btnCerrar.Click += (s, e) => Close();

            var flowR = new FlowLayoutPanel
            {
                Dock = DockStyle.Right, FlowDirection = FlowDirection.RightToLeft,
                BackColor = UITheme.FondoCard, Width = 170, WrapContents = false
            };
            flowR.Controls.Add(btnCerrar);
            footer.Controls.Add(flowR);

            // ── Contenedor de tarjetas con padding ──────────────────────────
            var content = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = UITheme.Fondo,
                Padding = new Padding(28, 24, 28, 24)
            };
            var grid = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = UITheme.Fondo
            };
            content.Controls.Add(grid);

            // ── Definición de módulos ───────────────────────────────────────
            // Colores coherentes con la paleta UITheme y MenuPrincipalV2.
            var tip = AyudaService.CrearTooltip();
            var tarjetas = new List<Panel>();

            void Add(string ico, string titulo, string sub, Color color,
                     Action accion, string textoTip)
            {
                var card = UICards.CrearTarjeta(ico, titulo, sub, color,
                    accion, tieneAcceso: true, tip: tip, textoTooltip: textoTip);
                tarjetas.Add(card);
                grid.Controls.Add(card);
            }

            Add("◈", "Facturas",            "Emitir y consultar",
                UITheme.Azul,
                () => new FrmFacturas().ShowDialog(),
                "Ver, emitir y gestionar todas las facturas de venta");

            Add("◫", "Cuentas por Cobrar",  "Saldos y vencimientos",
                Color.FromArgb(0, 150, 136),       // teal
                () => new FrmCuentasCobrar().ShowDialog(),
                "Gestionar saldos pendientes de clientes");

            Add("▤", "Cambio de Precios",   "Actualizar precios y costos",
                UITheme.Naranja,                   // ámbar
                () => new FrmCambioPrecios().ShowDialog(),
                "Actualizar precios de venta y costos del inventario");

            Add("◉", "Egresos",             "Compras, gastos y MP",
                UITheme.Verde,
                () => new FrmEgresos().ShowDialog(),
                "Materia prima, servicios, mantenimiento, oficina y CxP");

            Add("◎", "Cuentas por Pagar",   "Obligaciones pendientes",
                Color.FromArgb(120, 60, 140),      // morado
                () => new FrmCuentasPagar().ShowDialog(),
                "Ver y gestionar obligaciones con proveedores");

            Add("▣", "Ingreso Producción",  "Registro diario",
                UITheme.Cyan,
                () => new FrmRegistroProduccionDia().ShowDialog(),
                "Registrar la producción diaria de cada molde");

            Add("◆", "Abonos",              "Pagos y estado de cuenta",
                Color.FromArgb(30, 120, 180),     // azul medio
                () => new FrmAbonos().ShowDialog(),
                "Registrar abonos de clientes, ver comprobantes y generar estado de cuenta");

            // Distribución responsive: columnas según ancho disponible,
            // con buen aire entre tarjetas para que respiren visualmente.
            UICards.DistribuirGrid(grid, tarjetas, minAncho: 250, alto: 124, gap: 24);

            // Orden de Add: Fill antes que Top/Bottom (z-order inverso)
            Controls.Add(content);
            Controls.Add(footer);
            Controls.Add(hdr);
        }
    }
}
