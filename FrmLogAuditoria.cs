using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Windows.Forms;

namespace SistemaGestion
{
    public class FrmLogAuditoria : Form
    {
        // ── Filtros ───────────────────────────────────────────────────────
        private DateTimePicker dtpDesde, dtpHasta;
        private ComboBox       cmbUsuario, cmbAccion;
        private TextBox        txtBuscar;

        // ── Grid ──────────────────────────────────────────────────────────
        private DataGridView dgv;

        // ── KPIs ──────────────────────────────────────────────────────────
        private Label lblTotal, lblHoy, lblErrores, lblUsuarioTop;

        // ── Grafica de actividad ──────────────────────────────────────────
        private Panel pnlGrafica;

        // ── Data ──────────────────────────────────────────────────────────
        private List<(int Id, DateTime FechaHora, string Usuario,
            string Accion, string Tabla, string Detalle)> _datos = new();

        public FrmLogAuditoria()
        {
            Text          = "Visor de Log de Auditoria";
            Size          = new Size(1240, 700);
            MinimumSize   = new Size(1000, 580);
            StartPosition = FormStartPosition.CenterParent;
            BackColor     = Color.FromArgb(245, 247, 252);

            BuildUI();
            CargarDatos();
        }

        // ══════════════════════════════════════════════════════════════════
        //  UI
        // ══════════════════════════════════════════════════════════════════
        private void BuildUI()
        {
            // ── Header ────────────────────────────────────────────────────
            var hdr = new Panel { Dock = DockStyle.Top, Height = 50,
                BackColor = Color.FromArgb(40, 55, 90) };
            hdr.Controls.Add(new Label
            {
                Text = "Log de Auditoria del Sistema", ForeColor = Color.White,
                Font = new Font("Segoe UI", 13f, FontStyle.Bold),
                AutoSize = true, Location = new Point(14, 13)
            });
            var btnExportar = Btn("Exportar CSV", Color.FromArgb(30, 120, 60),
                new Point(1000, 10), 110);
            btnExportar.Click += (s, e) => ExportarCsv();
            var btnCerrar = Btn("Cerrar", Color.FromArgb(100, 100, 120),
                new Point(1120, 10), 80);
            btnCerrar.Click += (s, e) => Close();
            hdr.Controls.AddRange(new Control[] { btnExportar, btnCerrar });

            // ── Filtros ───────────────────────────────────────────────────
            var pnlF = new Panel { Dock = DockStyle.Top, Height = 46,
                BackColor = Color.FromArgb(228, 232, 248), Padding = new Padding(8, 8, 8, 6) };

            int fx = 10;
            pnlF.Controls.Add(Lbl("Desde:", fx, 14)); fx += 52;
            dtpDesde = new DateTimePicker { Location = new Point(fx, 10),
                Size = new Size(110, 26), Format = DateTimePickerFormat.Short,
                Value = DateTime.Today.AddDays(-30) };
            pnlF.Controls.Add(dtpDesde); fx += 120;

            pnlF.Controls.Add(Lbl("Hasta:", fx, 14)); fx += 50;
            dtpHasta = new DateTimePicker { Location = new Point(fx, 10),
                Size = new Size(110, 26), Format = DateTimePickerFormat.Short,
                Value = DateTime.Today };
            pnlF.Controls.Add(dtpHasta); fx += 120;

            pnlF.Controls.Add(Lbl("Usuario:", fx, 14)); fx += 62;
            cmbUsuario = new ComboBox { Location = new Point(fx, 10), Size = new Size(130, 26),
                DropDownStyle = ComboBoxStyle.DropDownList };
            pnlF.Controls.Add(cmbUsuario); fx += 140;

            pnlF.Controls.Add(Lbl("Accion:", fx, 14)); fx += 56;
            cmbAccion = new ComboBox { Location = new Point(fx, 10), Size = new Size(160, 26),
                DropDownStyle = ComboBoxStyle.DropDownList };
            pnlF.Controls.Add(cmbAccion); fx += 170;

            pnlF.Controls.Add(Lbl("Buscar:", fx, 14)); fx += 56;
            txtBuscar = new TextBox { Location = new Point(fx, 10), Size = new Size(180, 26) };
            pnlF.Controls.Add(txtBuscar); fx += 190;

            var btnFiltrar = new Button
            {
                Text = "Filtrar", Location = new Point(fx, 9), Size = new Size(80, 28),
                BackColor = Color.FromArgb(40, 55, 90), ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 9f, FontStyle.Bold)
            };
            btnFiltrar.FlatAppearance.BorderSize = 0;
            btnFiltrar.Click += (s, e) => CargarDatos();
            pnlF.Controls.Add(btnFiltrar);

            txtBuscar.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) CargarDatos(); };
            cmbUsuario.SelectedIndexChanged += (s, e) => CargarDatos();
            cmbAccion.SelectedIndexChanged  += (s, e) => CargarDatos();

            // ── KPIs ──────────────────────────────────────────────────────
            var pnlK = new Panel { Dock = DockStyle.Top, Height = 68,
                BackColor = Color.FromArgb(235, 238, 252) };
            lblTotal       = Kpi(pnlK, "Registros",       Color.FromArgb(40, 55, 90),  0);
            lblHoy         = Kpi(pnlK, "Hoy",             Color.FromArgb(30, 110, 60), 1);
            lblErrores     = Kpi(pnlK, "Errores",         Color.FromArgb(160, 40, 40), 2);
            lblUsuarioTop  = Kpi(pnlK, "Usuario mas activo", Color.FromArgb(80, 50, 150), 3);

            // ── Layout principal ──────────────────────────────────────────
            var pnlMain = new Panel { Dock = DockStyle.Fill };

            pnlGrafica = new Panel { Dock = DockStyle.Right, Width = 300,
                BackColor = Color.White };
            pnlGrafica.Paint += PaintGrafica;

            var sep = new Panel { Dock = DockStyle.Right, Width = 3,
                BackColor = Color.FromArgb(210, 215, 235) };

            dgv = new DataGridView
            {
                Dock = DockStyle.Fill, ReadOnly = true,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false, AllowUserToAddRows = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                BackgroundColor = Color.White, BorderStyle = BorderStyle.None,
                RowHeadersVisible = false, Font = new Font("Segoe UI", 8.5f),
                RowTemplate = { Height = 22 }
            };
            dgv.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(40, 55, 90);
            dgv.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
            dgv.ColumnHeadersDefaultCellStyle.Font      = new Font("Segoe UI", 8.5f, FontStyle.Bold);
            dgv.EnableHeadersVisualStyles = false;
            dgv.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(238, 241, 252);
            dgv.DataBindingComplete += (s, e) => ColorearFilas();
            dgv.SelectionChanged    += (s, e) => pnlGrafica.Invalidate();

            pnlMain.Controls.Add(dgv);
            pnlMain.Controls.Add(sep);
            pnlMain.Controls.Add(pnlGrafica);

            // ── Footer ────────────────────────────────────────────────────
            var footer = new Panel { Dock = DockStyle.Bottom, Height = 30,
                BackColor = Color.FromArgb(218, 222, 245) };
            var lblFooter = new Label
            {
                AutoSize = true, Location = new Point(12, 8),
                Font = new Font("Segoe UI", 8.5f), ForeColor = Color.FromArgb(50, 55, 100)
            };
            footer.Controls.Add(lblFooter);
            dgv.DataBindingComplete += (s, e) =>
                lblFooter.Text = "  Mostrando " + _datos.Count + " registros  "
                    + "  |  Solo Admin puede ver este log";

            Controls.AddRange(new Control[] { pnlMain, footer, pnlK, pnlF, hdr });
        }

        // ══════════════════════════════════════════════════════════════════
        //  CARGA Y FILTRADO
        // ══════════════════════════════════════════════════════════════════
        private void CargarDatos()
        {
            // Rellenar combos la primera vez
            if (cmbUsuario.Items.Count == 0)
            {
                foreach (var u in DatabaseService.ObtenerUsuariosAuditoria())
                    cmbUsuario.Items.Add(u);
                cmbUsuario.SelectedIndex = 0;
            }
            if (cmbAccion.Items.Count == 0)
            {
                foreach (var a in DatabaseService.ObtenerAccionesAuditoria())
                    cmbAccion.Items.Add(a);
                cmbAccion.SelectedIndex = 0;
            }

            string usuario = cmbUsuario.SelectedItem?.ToString();
            string accion  = cmbAccion.SelectedItem?.ToString();
            string buscar  = txtBuscar.Text.Trim();

            _datos = DatabaseService.ObtenerAuditoriaFiltrada(
                usuario == "Todos"  ? null : usuario,
                accion  == "Todas"  ? null : accion,
                string.IsNullOrEmpty(buscar) ? null : buscar,
                dtpDesde.Value.Date,
                dtpHasta.Value.Date);

            // Filtro adicional por texto en detalle/tabla
            if (!string.IsNullOrEmpty(buscar))
                _datos = _datos.Where(d =>
                    d.Usuario.ToLower().Contains(buscar.ToLower()) ||
                    d.Accion.ToLower().Contains(buscar.ToLower()) ||
                    d.Tabla.ToLower().Contains(buscar.ToLower()) ||
                    d.Detalle.ToLower().Contains(buscar.ToLower())).ToList();

            BindGrid();
            ActualizarKpis();
            pnlGrafica.Invalidate();
        }

        private void BindGrid()
        {
            dgv.DataSource = null;
            dgv.DataSource = _datos.Select(d => new
            {
                d.Id,
                FechaHora = d.FechaHora.ToString("dd/MM/yyyy HH:mm:ss"),
                d.Usuario,
                d.Accion,
                d.Tabla,
                d.Detalle
            }).ToList();
        }

        private void ActualizarKpis()
        {
            int total   = _datos.Count;
            int hoy     = _datos.Count(d => d.FechaHora.Date == DateTime.Today);
            int errores = _datos.Count(d => d.Accion.StartsWith("ERROR"));
            string top  = _datos.Count > 0
                ? _datos.GroupBy(d => d.Usuario)
                         .OrderByDescending(g => g.Count())
                         .First().Key
                : "—";

            lblTotal.Text      = total.ToString("N0");
            lblHoy.Text        = hoy.ToString();
            lblErrores.Text    = errores.ToString();
            lblUsuarioTop.Text = top.Length > 10 ? top.Substring(0, 10) : top;
        }

        private void ColorearFilas()
        {
            foreach (DataGridViewRow row in dgv.Rows)
            {
                var accion = row.Cells["Accion"].Value?.ToString() ?? "";
                if (accion.StartsWith("ERROR"))
                {
                    row.DefaultCellStyle.BackColor = Color.FromArgb(255, 228, 228);
                    row.DefaultCellStyle.ForeColor = Color.FromArgb(160, 30, 30);
                }
                else if (accion.Contains("LOGIN") || accion.Contains("LOGOUT"))
                    row.DefaultCellStyle.BackColor = Color.FromArgb(230, 245, 255);
                else if (accion.Contains("NUEVA") || accion.Contains("INSERTAR") ||
                         accion.Contains("BACKUP"))
                    row.DefaultCellStyle.BackColor = Color.FromArgb(228, 248, 232);
                else if (accion.Contains("CANCEL") || accion.Contains("ELIMINAR"))
                    row.DefaultCellStyle.BackColor = Color.FromArgb(255, 242, 228);
            }
        }

        // ══════════════════════════════════════════════════════════════════
        //  GRAFICA: ACTIVIDAD POR HORA (del periodo filtrado)
        // ══════════════════════════════════════════════════════════════════
        private void PaintGrafica(object sender, PaintEventArgs e)
        {
            var g   = e.Graphics;
            var pnl = (Panel)sender;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            // Titulo
            g.FillRectangle(new SolidBrush(Color.FromArgb(40, 55, 90)),
                0, 0, pnl.Width, 28);
            using var fntT = new Font("Segoe UI", 8.5f, FontStyle.Bold);
            g.DrawString("Actividad por hora del dia", fntT, Brushes.White, 10, 7);

            if (_datos.Count == 0)
            {
                g.DrawString("Sin datos", new Font("Segoe UI", 9f),
                    Brushes.Gray, 20, 60);
                return;
            }

            // Agrupar por hora (0-23)
            var porHora = new int[24];
            foreach (var d in _datos) porHora[d.FechaHora.Hour]++;
            int maxH = porHora.Max();
            if (maxH == 0) maxH = 1;

            int padL = 30, padR = 10, padT = 40, padB = 30;
            int w    = pnl.Width  - padL - padR;
            int h    = pnl.Height / 2 - padT - padB;
            float barW = (float)w / 24;

            using var fntS = new Font("Segoe UI", 7f);
            using var brGr = new SolidBrush(Color.FromArgb(120, 130, 155));

            // Grid horizontal
            using var penG = new Pen(Color.FromArgb(230, 232, 244), 1f) { DashStyle = DashStyle.Dash };
            for (int i = 0; i <= 4; i++)
            {
                int y2 = padT + h - (int)(h * i / 4.0);
                g.DrawLine(penG, padL, y2, padL + w, y2);
                int val = maxH * i / 4;
                g.DrawString(val.ToString(), fntS, brGr,
                    new RectangleF(0, y2 - 8, padL - 2, 16),
                    new StringFormat { Alignment = StringAlignment.Far,
                                       LineAlignment = StringAlignment.Center });
            }

            // Eje X e Y
            using var penE = new Pen(Color.FromArgb(190, 196, 220), 1f);
            g.DrawLine(penE, padL, padT, padL, padT + h);
            g.DrawLine(penE, padL, padT + h, padL + w, padT + h);

            // Barras
            for (int hora = 0; hora < 24; hora++)
            {
                float x   = padL + hora * barW;
                float bh  = porHora[hora] == 0 ? 0
                           : Math.Max(2, (float)h * porHora[hora] / maxH);
                float y2  = padT + h - bh;

                // Color: horas de trabajo (8-18) mas oscuro
                bool laboral = hora >= 8 && hora <= 18;
                Color barCol = laboral ? Color.FromArgb(60, 90, 160) : Color.FromArgb(140, 155, 195);
                using var brB = new SolidBrush(barCol);
                if (porHora[hora] > 0)
                    g.FillRectangle(brB, x + 1, y2, barW - 2, bh);

                // Etiqueta X cada 4 horas
                if (hora % 4 == 0)
                    g.DrawString(hora + "h", fntS, brGr,
                        new RectangleF(x - 6, padT + h + 3, barW + 12, 14),
                        new StringFormat { Alignment = StringAlignment.Center });
            }

            // Titulo eje
            g.DrawString("Eventos por hora", fntS, brGr, padL, padT - 14);

            // ── Segunda grafica: top acciones ─────────────────────────────
            int g2T = pnl.Height / 2 + 10;
            g.FillRectangle(new SolidBrush(Color.FromArgb(40, 55, 90)),
                0, g2T - 2, pnl.Width, 26);
            g.DrawString("Top acciones", fntT, Brushes.White, 10, g2T + 3);

            var topAcciones = _datos
                .GroupBy(d => d.Accion)
                .OrderByDescending(gr => gr.Count())
                .Take(8)
                .ToList();

            if (topAcciones.Count == 0) return;

            int maxA  = topAcciones.First().Count();
            int rowHt = Math.Max(18, (pnl.Height - g2T - 32) / Math.Max(topAcciones.Count, 1));
            int wBar  = pnl.Width - 130 - 50;

            for (int i = 0; i < topAcciones.Count; i++)
            {
                var gr   = topAcciones[i];
                int y2   = g2T + 28 + i * rowHt;
                float bw = (float)wBar * gr.Count() / maxA;

                // Barra
                Color barC = gr.Key.StartsWith("ERROR")  ? Color.FromArgb(200, 60, 60)
                           : gr.Key.Contains("LOGIN")     ? Color.FromArgb(40, 130, 180)
                           : gr.Key.Contains("NUEVA")     ? Color.FromArgb(40, 140, 70)
                           : Color.FromArgb(60, 90, 160);
                using var brA = new SolidBrush(Color.FromArgb(180, barC));
                g.FillRectangle(brA, 130, y2, bw, rowHt - 3);

                // Nombre de accion
                string nombre = gr.Key.Length > 18 ? gr.Key.Substring(0, 18) : gr.Key;
                g.DrawString(nombre, fntS, brGr,
                    new RectangleF(2, y2, 126, rowHt),
                    new StringFormat { Alignment = StringAlignment.Far,
                                       LineAlignment = StringAlignment.Center });

                // Contador
                g.DrawString(gr.Count().ToString(), fntS,
                    new SolidBrush(barC),
                    130 + bw + 3, y2 + 2);
            }
        }

        // ══════════════════════════════════════════════════════════════════
        //  EXPORTAR CSV
        // ══════════════════════════════════════════════════════════════════
        private void ExportarCsv()
        {
            if (_datos.Count == 0)
            {
                MessageBox.Show("No hay datos para exportar.", "Aviso",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var dlg = new SaveFileDialog
            {
                Title    = "Exportar log de auditoria",
                Filter   = "CSV (*.csv)|*.csv",
                FileName = "AuditoriaLog_" + DateTime.Today.ToString("yyyy-MM-dd") + ".csv"
            };
            if (dlg.ShowDialog() != DialogResult.OK) return;

            try
            {
                var lines = new System.Collections.Generic.List<string>
                    { "Id,FechaHora,Usuario,Accion,Tabla,Detalle" };

                foreach (var d in _datos)
                    lines.Add(string.Join(",",
                        d.Id,
                        d.FechaHora.ToString("yyyy-MM-dd HH:mm:ss"),
                        "\"" + d.Usuario.Replace("\"", "'") + "\"",
                        "\"" + d.Accion.Replace("\"", "'") + "\"",
                        "\"" + d.Tabla.Replace("\"", "'") + "\"",
                        "\"" + d.Detalle.Replace("\"", "'") + "\""));

                System.IO.File.WriteAllLines(dlg.FileName, lines,
                    System.Text.Encoding.UTF8);

                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                    { FileName = dlg.FileName, UseShellExecute = true });

                DatabaseService.RegistrarAuditoria("AUDITORIA_EXPORTADA", "LogAuditoria",
                    "Registros: " + _datos.Count);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al exportar:\n" + ex.Message, "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // ══════════════════════════════════════════════════════════════════
        //  HELPERS UI
        // ══════════════════════════════════════════════════════════════════
        private Label Kpi(Panel parent, string titulo, Color color, int idx)
        {
            int w = (parent.Width - 10) / 4;
            var card = new Panel
            {
                Location = new Point(5 + idx * w, 4),
                Size     = new Size(w - 4, 60),
                BackColor= color
            };
            card.Controls.Add(new Label
            {
                Text = titulo, Font = new Font("Segoe UI", 7.5f),
                ForeColor = Color.FromArgb(200, 220, 255),
                Location = new Point(8, 6), Size = new Size(w - 16, 16)
            });
            var lbl = new Label
            {
                Text = "—", Font = new Font("Segoe UI", 16f, FontStyle.Bold),
                ForeColor = Color.White,
                Location = new Point(8, 22), Size = new Size(w - 16, 30),
                TextAlign = ContentAlignment.MiddleLeft
            };
            card.Controls.Add(lbl);
            parent.Controls.Add(card);
            parent.Resize += (s, e) =>
            {
                int cw = (parent.Width - 10) / 4;
                card.SetBounds(5 + idx * cw, 4, cw - 4, 60);
                lbl.Width = cw - 16;
            };
            return lbl;
        }

        private Button Btn(string t, Color c, Point p, int w = 90)
        {
            var b = new Button
            {
                Text = t, Size = new Size(w, 30), Location = p, BackColor = c,
                ForeColor = Color.White, FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 8.5f, FontStyle.Bold), Cursor = Cursors.Hand
            };
            b.FlatAppearance.BorderSize = 0;
            return b;
        }

        private Label Lbl(string t, int x, int y) =>
            new Label { Text = t, Location = new Point(x, y), AutoSize = true,
                        Font = new Font("Segoe UI", 9f) };
    }
}
