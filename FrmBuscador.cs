using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Windows.Forms;

namespace SistemaGestion
{
    public class FrmBuscador : Form
    {
        private TextBox  txtBuscar;
        private Panel    pnlResultados;
        private Label    lblEstado;
        private System.Windows.Forms.Timer _debounce;
        private List<DatabaseService.ResultadoBusqueda> _resultados = new();

        // Colores por categoria
        private static readonly Dictionary<string, Color> _colores = new()
        {
            ["Clientes"]    = Color.FromArgb(40, 80, 160),
            ["Proveedores"] = Color.FromArgb(160, 90, 20),
            ["Inventario"]  = Color.FromArgb(30, 120, 60),
            ["Facturas"]    = Color.FromArgb(60, 100, 180),
            ["Compras"]     = Color.FromArgb(100, 50, 160),
            ["Produccion"]  = Color.FromArgb(60, 120, 40),
        };

        public FrmBuscador()
        {
            Text            = "Buscador Global";
            Size            = new Size(720, 560);
            MinimumSize     = new Size(540, 400);
            StartPosition   = FormStartPosition.CenterParent;
            BackColor       = Color.FromArgb(245, 247, 252);
            KeyPreview      = true;
            KeyDown         += (s, e) => { if (e.KeyCode == Keys.Escape) Close(); };

            // ── Header con campo de búsqueda ──────────────────────────────
            var hdr = new Panel
            {
                Dock = DockStyle.Top, Height = 70,
                BackColor = Color.FromArgb(22, 55, 120)
            };

            hdr.Controls.Add(new Label
            {
                Text = "Buscar en todo el sistema",
                Font = new Font("Segoe UI", 10f, FontStyle.Bold),
                ForeColor = Color.FromArgb(180, 210, 255),
                AutoSize = true, Location = new Point(16, 10)
            });

            txtBuscar = new TextBox
            {
                Location    = new Point(16, 34),
                Size        = new Size(660, 28),
                Font        = new Font("Segoe UI", 12f),
                BorderStyle = BorderStyle.FixedSingle,
                PlaceholderText = "Escribe aqui para buscar clientes, facturas, productos, ordenes..."
            };
            txtBuscar.TextChanged += (s, e) => IniciarDebounce();
            txtBuscar.KeyDown     += (s, e) =>
            {
                if (e.KeyCode == Keys.Down && pnlResultados.Controls.Count > 0)
                    pnlResultados.Controls[0].Focus();
                if (e.KeyCode == Keys.Escape)
                    Close();
            };
            hdr.Controls.Add(txtBuscar);

            // ── Estado / contador ─────────────────────────────────────────
            var pnlInfo = new Panel
            {
                Dock = DockStyle.Top, Height = 28,
                BackColor = Color.FromArgb(235, 240, 252)
            };
            lblEstado = new Label
            {
                Text = "  Escribe al menos 2 caracteres para buscar...",
                Font = new Font("Segoe UI", 8.5f, FontStyle.Italic),
                ForeColor = Color.FromArgb(100, 110, 140),
                Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft
            };
            pnlInfo.Controls.Add(lblEstado);

            // ── Panel de resultados (scrollable) ──────────────────────────
            pnlResultados = new Panel
            {
                Dock       = DockStyle.Fill,
                AutoScroll = true,
                BackColor  = Color.FromArgb(245, 247, 252),
                Padding    = new Padding(8, 4, 8, 8)
            };

            // ── Footer ────────────────────────────────────────────────────
            var footer = new Panel
            {
                Dock = DockStyle.Bottom, Height = 32,
                BackColor = Color.FromArgb(225, 230, 245)
            };
            footer.Controls.Add(new Label
            {
                Text = "  ESC para cerrar  |  Enter para abrir el modulo del resultado seleccionado",
                Font = new Font("Segoe UI", 8f, FontStyle.Italic),
                ForeColor = Color.FromArgb(100, 110, 140),
                Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft
            });

            Controls.AddRange(new Control[] { pnlResultados, pnlInfo, hdr, footer });

            // Timer de debounce: espera 300ms tras dejar de escribir
            _debounce = new System.Windows.Forms.Timer { Interval = 300 };
            _debounce.Tick += (s, e) =>
            {
                _debounce.Stop();
                EjecutarBusqueda();
            };

            Shown += (s, e) => txtBuscar.Focus();
        }

        // ── Debounce ──────────────────────────────────────────────────────
        private void IniciarDebounce()
        {
            _debounce.Stop();
            _debounce.Start();
        }

        // ── Búsqueda ──────────────────────────────────────────────────────
        private void EjecutarBusqueda()
        {
            string q = txtBuscar.Text.Trim();

            if (q.Length < 2)
            {
                pnlResultados.Controls.Clear();
                lblEstado.Text = "  Escribe al menos 2 caracteres para buscar...";
                return;
            }

            lblEstado.Text = "  Buscando...";
            _resultados = DatabaseService.BuscarGlobal(q);

            RenderizarResultados();
        }

        // ── Render de resultados ──────────────────────────────────────────
        private void RenderizarResultados()
        {
            pnlResultados.SuspendLayout();
            pnlResultados.Controls.Clear();

            if (_resultados.Count == 0)
            {
                lblEstado.Text = "  Sin resultados para \"" + txtBuscar.Text.Trim() + "\"";
                pnlResultados.ResumeLayout();
                return;
            }

            lblEstado.Text = "  " + _resultados.Count + " resultado(s) en " +
                _resultados.Select(r => r.Modulo).Distinct().Count() + " modulo(s)";

            int y = 4;
            string ultimaCategoria = null;

            // Agrupar por categoria
            var grupos = _resultados
                .GroupBy(r => r.Categoria)
                .OrderBy(g => g.Key);

            foreach (var grupo in grupos)
            {
                // Encabezado de grupo
                if (ultimaCategoria != grupo.Key)
                {
                    ultimaCategoria = grupo.Key;
                    Color groupColor = _colores.GetValueOrDefault(grupo.Key, Color.FromArgb(80, 80, 100));

                    var lblGrupo = new Label
                    {
                        Text      = grupo.Key.ToUpper(),
                        Location  = new Point(0, y),
                        Size      = new Size(pnlResultados.Width - 20, 22),
                        Font      = new Font("Segoe UI", 7.5f, FontStyle.Bold),
                        ForeColor = groupColor,
                        BackColor = Color.FromArgb(235, 238, 250),
                        TextAlign = ContentAlignment.MiddleLeft,
                        Padding   = new Padding(8, 0, 0, 0),
                        Anchor    = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top
                    };
                    pnlResultados.Controls.Add(lblGrupo);
                    y += 26;
                }

                // Tarjetas de resultado
                foreach (var res in grupo)
                {
                    var card = CrearTarjeta(res, y);
                    pnlResultados.Controls.Add(card);
                    y += card.Height + 4;
                }

                y += 6; // espaciado entre grupos
            }

            pnlResultados.ResumeLayout();
        }

        // ── Tarjeta de resultado ──────────────────────────────────────────
        private Panel CrearTarjeta(DatabaseService.ResultadoBusqueda res, int y)
        {
            Color accent = _colores.GetValueOrDefault(res.Categoria, Color.FromArgb(80, 80, 100));

            var card = new Panel
            {
                Location  = new Point(0, y),
                Size      = new Size(pnlResultados.Width - 20, 52),
                BackColor = Color.White,
                Cursor    = Cursors.Hand,
                Anchor    = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top,
                Tag       = res
            };

            // Borde izquierdo de color
            card.Paint += (s, e) =>
            {
                e.Graphics.FillRectangle(new SolidBrush(accent), 0, 0, 4, card.Height);
                using var pen = new Pen(Color.FromArgb(220, 225, 235), 1f);
                e.Graphics.DrawRectangle(pen, 0, 0, card.Width - 1, card.Height - 1);
            };

            // Badge del módulo
            var badge = new Label
            {
                Text      = res.Modulo,
                Location  = new Point(10, 8),
                AutoSize  = false,
                Size      = new Size(Math.Min(res.Modulo.Length * 7 + 14, 140), 16),
                Font      = new Font("Segoe UI", 7f, FontStyle.Bold),
                ForeColor = accent,
                TextAlign = ContentAlignment.MiddleCenter,
                BackColor = Color.FromArgb(20,
                    accent.R > 200 ? accent.R - 40 : accent.R + 40,
                    accent.G > 200 ? accent.G - 40 : accent.G + 40,
                    accent.B > 200 ? accent.B - 40 : accent.B + 40)
            };
            // Simplify badge background
            badge.BackColor = Color.FromArgb(230, 238, 255);

            // Título
            var lblTitulo = new Label
            {
                Text      = res.Titulo,
                Location  = new Point(10, 26),
                Size      = new Size(card.Width - 80, 18),
                Font      = new Font("Segoe UI", 9f, FontStyle.Bold),
                ForeColor = Color.FromArgb(30, 35, 60),
                AutoEllipsis = true,
                Anchor    = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top
            };

            // Subtítulo
            var lblSub = new Label
            {
                Text      = res.Subtitulo,
                Location  = new Point(10, 34),
                Size      = new Size(card.Width - 80, 14),
                Font      = new Font("Segoe UI", 8f),
                ForeColor = Color.FromArgb(110, 115, 140),
                AutoEllipsis = true,
                Anchor    = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top
            };

            // Flecha
            var lblFlecha = new Label
            {
                Text      = "›",
                Location  = new Point(card.Width - 28, 14),
                Size      = new Size(20, 24),
                Font      = new Font("Segoe UI", 14f),
                ForeColor = Color.FromArgb(180, 185, 210),
                TextAlign = ContentAlignment.MiddleCenter,
                Anchor    = AnchorStyles.Right | AnchorStyles.Top
            };

            card.Controls.AddRange(new Control[] { badge, lblTitulo, lblSub, lblFlecha });

            // Hover effect
            void SetHover(bool on)
            {
                card.BackColor = on ? Color.FromArgb(242, 246, 255) : Color.White;
                lblFlecha.ForeColor = on ? accent : Color.FromArgb(180, 185, 210);
                card.Invalidate();
            }

            void ClickAction(object s, EventArgs e)
            {
                AbrirModulo(res);
            }

            card.MouseEnter += (s, e) => SetHover(true);
            card.MouseLeave += (s, e) => SetHover(false);
            card.Click      += ClickAction;
            badge.Click     += ClickAction;
            lblTitulo.Click += ClickAction;
            lblSub.Click    += ClickAction;

            // Navegación con teclado
            card.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Enter) AbrirModulo(res);
                if (e.KeyCode == Keys.Escape) Close();
                if (e.KeyCode == Keys.Up) txtBuscar.Focus();
            };

            // Hover en controles hijo
            foreach (Control ctrl in card.Controls)
            {
                ctrl.MouseEnter += (s, e) => SetHover(true);
                ctrl.MouseLeave += (s, e) => SetHover(false);
            }

            return card;
        }

        // ── Abrir módulo correspondiente ──────────────────────────────────
        private void AbrirModulo(DatabaseService.ResultadoBusqueda res)
        {
            Form frm = res.Modulo switch
            {
                "Clientes"              => new FrmClientes(),
                "Proveedores"           => new FrmProveedores(),
                "Inventario"            => new FrmInventario(),
                "Facturas"              => new FrmFacturas(),
                "Compras"               => new FrmCompras(),
                "Ordenes de Produccion" => new FrmOrdenesProduccion(),
                "Moldes"                => new FrmMoldes(),
                "Kardex"                => new FrmKardex(),
                _                       => null
            };

            if (frm != null)
            {
                DatabaseService.RegistrarAuditoria("BUSQUEDA", res.Modulo,
                    "Busqueda: '" + txtBuscar.Text.Trim() + "' -> " + res.Codigo);
                frm.ShowDialog(this);
            }
        }
    }
}
