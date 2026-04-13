using System;
using System.Drawing;
using System.Windows.Forms;

namespace SistemaGestion
{
    // ══════════════════════════════════════════════════════════════
    //  SUBMENÚ TRANSACCIONES
    // ══════════════════════════════════════════════════════════════
    public class FrmTransacciones : Form
    {
        public FrmTransacciones()
        {
            Text = "Transacciones";
            Size = new Size(620, 680);   // altura aumentada para 3 filas
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            BackColor = Color.FromArgb(236, 236, 236);
            Font = new Font("Segoe UI", 9f);

            var lblTitulo = new Label
            {
                Text = "TRANSACCIONES",
                Font = new Font("Segoe UI", 12f, FontStyle.Bold),
                ForeColor = Color.FromArgb(50, 50, 120),
                AutoSize = false,
                TextAlign = ContentAlignment.MiddleCenter,
                Dock = DockStyle.Top,
                Height = 45,
                BackColor = Color.FromArgb(220, 220, 240)
            };

            // Fila 1
            var btnFacturas   = CrearBoton("Facturas",                Color.FromArgb(60, 100, 180),  new Point(60,  70));
            var btnProduccion = CrearBoton("Ordenes de\nProduccion",  Color.FromArgb(60, 120, 40),   new Point(310, 70));

            // Fila 2
            var btnCuentas    = CrearBoton("Cuentas por\nCobrar",     Color.FromArgb(40, 140, 140),  new Point(60,  240));
            var btnPrecios    = CrearBoton("Cambio de\nPrecios y Costos", Color.FromArgb(180, 110, 30), new Point(310, 240));

            // Fila 3 — Cuentas por Pagar centrado
            var btnCxP        = CrearBoton("Cuentas por\nPagar",      Color.FromArgb(100, 30, 100),  new Point(185, 410));

            var panelBottom = new Panel { Dock = DockStyle.Bottom, Height = 70, BackColor = Color.FromArgb(210, 210, 210) };
            var btnSalir = new Button
            {
                Text = "Cerrar",
                Size = new Size(120, 40),
                Location = new Point(240, 15),
                BackColor = Color.FromArgb(180, 60, 60),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnSalir.FlatAppearance.BorderSize = 0;
            btnSalir.Click += (s, e) => Close();
            panelBottom.Controls.Add(btnSalir);

            btnFacturas.Click   += (s, e) => new FrmFacturas().ShowDialog();
            btnProduccion.Click += (s, e) => new FrmOrdenesProduccion().ShowDialog();
            btnCuentas.Click    += (s, e) => new FrmCuentasCobrar().ShowDialog();
            btnPrecios.Click    += (s, e) => new FrmCambioPrecios().ShowDialog();
            btnCxP.Click        += (s, e) => new FrmCuentasPagar().ShowDialog();

            AplicarHover(btnFacturas,   Color.FromArgb(80,  120, 200), Color.FromArgb(60,  100, 180));
            AplicarHover(btnProduccion, Color.FromArgb(80,  150,  60), Color.FromArgb(60,  120,  40));
            AplicarHover(btnCuentas,    Color.FromArgb(60,  160, 160), Color.FromArgb(40,  140, 140));
            AplicarHover(btnPrecios,    Color.FromArgb(200, 130,  50), Color.FromArgb(180, 110,  30));
            AplicarHover(btnCxP,        Color.FromArgb(130,  50, 130), Color.FromArgb(100,  30, 100));

            Controls.AddRange(new Control[] {
                lblTitulo,
                btnFacturas, btnProduccion,
                btnCuentas,  btnPrecios,
                btnCxP,
                panelBottom
            });
        }

        private Button CrearBoton(string texto, Color color, Point location)
        {
            var btn = new Button
            {
                Text = texto,
                Size = new Size(200, 150),
                Location = location,
                BackColor = color,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 10f, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleCenter,
                Cursor = Cursors.Hand
            };
            btn.FlatAppearance.BorderSize = 0;
            return btn;
        }

        private void AplicarHover(Button btn, Color colorHover, Color colorNormal)
        {
            btn.MouseEnter += (s, e) => btn.BackColor = colorHover;
            btn.MouseLeave += (s, e) => btn.BackColor = colorNormal;
        }
    }
}
