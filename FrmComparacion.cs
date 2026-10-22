using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Windows.Forms;

namespace SistemaGestion
{
    // =========================================================================
    //  REPORTE DE COMPARACION POR PERIODO
    // =========================================================================
    public class FrmComparacion : FormBase
    {
        private DateTimePicker dtpPer1Desde, dtpPer1Hasta;
        private DateTimePicker dtpPer2Desde, dtpPer2Hasta;
        private Button btnComparar, btnPdf;
        private Panel pnlGrafica;
        private DataGridView dgv;
        private Label lblTituloPer1, lblTituloPer2;

        private ComparacionData _data;

        public FrmComparacion()
        {
            InitializeComponent();
            // Por defecto: mes actual vs mes anterior
            var hoy = DateTime.Today;
            dtpPer1Desde.Value = new DateTime(hoy.Year, hoy.Month, 1);
            dtpPer1Hasta.Value = hoy;
            var ant = hoy.AddMonths(-1);
            dtpPer2Desde.Value = new DateTime(ant.Year, ant.Month, 1);
            dtpPer2Hasta.Value = new DateTime(ant.Year, ant.Month,
                DateTime.DaysInMonth(ant.Year, ant.Month));
            Comparar();
        }

        private void InitializeComponent()
        {
            this.Text = "Comparacion por Periodo";
            this.Size = new Size(1100, 680);
            this.StartPosition = FormStartPosition.CenterParent;
            this.MinimumSize = new Size(850, 550);
            this.BackColor = Color.FromArgb(245, 247, 250);
            this.Font = new Font("Segoe UI", 9f);

            // Header
            var pnlHeader = new Panel { Dock = DockStyle.Top, Height = 50, BackColor = Color.FromArgb(50, 50, 120) };
            pnlHeader.Controls.Add(new Label
            {
                Text = "  Comparacion de Periodos — Ventas, Compras y Gastos",
                ForeColor = Color.White, Font = new Font("Segoe UI", 12f, FontStyle.Bold),
                AutoSize = true, Location = new Point(8, 14)
            });

            // Toolbar
            var pnlToolbar = new Panel { Dock = DockStyle.Top, Height = 80, BackColor = Color.FromArgb(230, 232, 245) };

            // Periodo 1 — DTP a 115px para que el formato dd/MM/yyyy entre completo.
            var pnlP1 = new GroupBox
            {
                Text = "Periodo 1 (actual)", Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
                Location = new Point(8, 6), Size = new Size(368, 66),
                ForeColor = Color.FromArgb(22, 55, 120)
            };
            pnlP1.Controls.Add(new Label { Text = "Desde:", Location = new Point(8, 24), AutoSize = true, Font = new Font("Segoe UI", 8.5f), ForeColor = Color.FromArgb(44, 62, 80) });
            dtpPer1Desde = new DateTimePicker { Location = new Point(62, 20), Size = new Size(118, 24), Format = DateTimePickerFormat.Short, Font = new Font("Segoe UI", 8.5f) };
            pnlP1.Controls.Add(dtpPer1Desde);
            pnlP1.Controls.Add(new Label { Text = "Hasta:", Location = new Point(190, 24), AutoSize = true, Font = new Font("Segoe UI", 8.5f), ForeColor = Color.FromArgb(44, 62, 80) });
            dtpPer1Hasta = new DateTimePicker { Location = new Point(240, 20), Size = new Size(118, 24), Format = DateTimePickerFormat.Short, Font = new Font("Segoe UI", 8.5f) };
            pnlP1.Controls.Add(dtpPer1Hasta);
            pnlToolbar.Controls.Add(pnlP1);

            // Periodo 2
            var pnlP2 = new GroupBox
            {
                Text = "Periodo 2 (comparar con)", Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
                Location = new Point(386, 6), Size = new Size(368, 66),
                ForeColor = Color.FromArgb(160, 40, 40)
            };
            pnlP2.Controls.Add(new Label { Text = "Desde:", Location = new Point(8, 24), AutoSize = true, Font = new Font("Segoe UI", 8.5f), ForeColor = Color.FromArgb(44, 62, 80) });
            dtpPer2Desde = new DateTimePicker { Location = new Point(62, 20), Size = new Size(118, 24), Format = DateTimePickerFormat.Short, Font = new Font("Segoe UI", 8.5f) };
            pnlP2.Controls.Add(dtpPer2Desde);
            pnlP2.Controls.Add(new Label { Text = "Hasta:", Location = new Point(190, 24), AutoSize = true, Font = new Font("Segoe UI", 8.5f), ForeColor = Color.FromArgb(44, 62, 80) });
            dtpPer2Hasta = new DateTimePicker { Location = new Point(240, 20), Size = new Size(118, 24), Format = DateTimePickerFormat.Short, Font = new Font("Segoe UI", 8.5f) };
            pnlP2.Controls.Add(dtpPer2Hasta);
            pnlToolbar.Controls.Add(pnlP2);

            btnComparar = new Button
            {
                Text = "Comparar", Size = new Size(100, 34), Location = new Point(766, 20),
                FlatStyle = FlatStyle.Flat, BackColor = Color.FromArgb(41, 128, 185),
                ForeColor = Color.White, Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                FlatAppearance = { BorderSize = 0 }, Cursor = Cursors.Hand
            };
            btnComparar.Click += (s, e) => Comparar();
            pnlToolbar.Controls.Add(btnComparar);

            btnPdf = new Button
            {
                Text = "Exportar PDF", Size = new Size(110, 34), Location = new Point(876, 20),
                FlatStyle = FlatStyle.Flat, BackColor = Color.FromArgb(192, 57, 43),
                ForeColor = Color.White, Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                FlatAppearance = { BorderSize = 0 }, Cursor = Cursors.Hand
            };
            btnPdf.Click += BtnPdf_Click;
            pnlToolbar.Controls.Add(btnPdf);

            // Split: gráfica arriba, grid abajo
            var split = new SplitContainer
            {
                Dock = DockStyle.Fill,
                Orientation = Orientation.Horizontal,
                BackColor = Color.FromArgb(245, 247, 250),
                Panel1MinSize = 180,
                Panel2MinSize = 120
            };

            // Gráfica de barras comparativa
            pnlGrafica = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White
            };
            pnlGrafica.Paint += PnlGrafica_Paint;
            split.Panel1.Controls.Add(pnlGrafica);

            // Grid de métricas
            var pnlGridHeader = new Panel { Dock = DockStyle.Top, Height = 28, BackColor = Color.FromArgb(230, 232, 245) };
            lblTituloPer1 = new Label { Text = "Periodo 1", AutoSize = true, Location = new Point(10, 6),
                Font = new Font("Segoe UI", 8.5f, FontStyle.Bold), ForeColor = Color.FromArgb(22, 55, 120) };
            lblTituloPer2 = new Label { Text = "Periodo 2", AutoSize = true, Location = new Point(380, 6),
                Font = new Font("Segoe UI", 8.5f, FontStyle.Bold), ForeColor = Color.FromArgb(160, 40, 40) };
            pnlGridHeader.Controls.AddRange(new Control[] { lblTituloPer1, lblTituloPer2 });

            var _btnAyC = new Button {
                Text = "? Ayuda", Size = new Size(80, 28),
                BackColor = Color.FromArgb(80, 50, 150), ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            _btnAyC.FlatAppearance.BorderSize = 0;
            _btnAyC.Click += (s, e) => AyudaService.Mostrar(this, "FrmComparacion");
            var _tipC = AyudaService.CrearTooltip();
            AyudaService.Tip(_tipC, _btnAyC, "Abrir guía de ayuda");
            VentanaPrefs.Restaurar(this);
            var _tp = new System.Windows.Forms.Timer { Interval = 800 };
            _tp.Tick        += (s, e) => { _tp.Stop(); VentanaPrefs.Guardar(this); };
            SizeChanged     += (s, e) => { _tp.Stop(); _tp.Start(); };
            LocationChanged += (s, e) => { _tp.Stop(); _tp.Start(); };
            FormClosed      += (s, e) => { _tp.Stop(); _tp.Dispose(); };

            dgv = new DataGridView
            {
                Dock = DockStyle.Fill, ReadOnly = true, AllowUserToAddRows = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                BackgroundColor = Color.White, BorderStyle = BorderStyle.None,
                RowHeadersVisible = false, AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                Font = new Font("Segoe UI", 9f), RowTemplate = { Height = 26 }
            };
            dgv.EnableHeadersVisualStyles = false;
            dgv.ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
            {
                BackColor = Color.FromArgb(50, 50, 120), ForeColor = Color.White,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold)
            };
            dgv.ColumnHeadersHeight = 30;

            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "colMetrica",  HeaderText = "Metrica",    FillWeight = 28 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "colPer1",     HeaderText = "Periodo 1",  FillWeight = 18,
                DefaultCellStyle = new DataGridViewCellStyle { Alignment = DataGridViewContentAlignment.MiddleRight } });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "colPer2",     HeaderText = "Periodo 2",  FillWeight = 18,
                DefaultCellStyle = new DataGridViewCellStyle { Alignment = DataGridViewContentAlignment.MiddleRight } });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "colDif",      HeaderText = "Diferencia", FillWeight = 18,
                DefaultCellStyle = new DataGridViewCellStyle { Alignment = DataGridViewContentAlignment.MiddleRight } });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "colPct",      HeaderText = "Variacion",  FillWeight = 10,
                DefaultCellStyle = new DataGridViewCellStyle { Alignment = DataGridViewContentAlignment.MiddleRight } });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "colTendencia",HeaderText = "Tendencia",  FillWeight = 8,
                DefaultCellStyle = new DataGridViewCellStyle { Alignment = DataGridViewContentAlignment.MiddleCenter } });

            split.Panel2.Controls.Add(dgv);
            split.Panel2.Controls.Add(pnlGridHeader);

            // Footer
            var pnlFooter = new Panel { Dock = DockStyle.Bottom, Height = 34, BackColor = Color.FromArgb(230, 232, 245) };
            var btnCerrar = new Button
            {
                Text = "Cerrar", Size = new Size(90, 24), Location = new Point(940, 5),
                FlatStyle = FlatStyle.Flat, BackColor = Color.FromArgb(44, 62, 80),
                ForeColor = Color.White, FlatAppearance = { BorderSize = 0 }
            };
            btnCerrar.Click += (s, e) => Close();
            pnlFooter.Controls.Add(btnCerrar);

            this.Controls.Add(split);
            this.Controls.Add(pnlToolbar);
            this.Controls.Add(pnlHeader);
            this.Controls.Add(pnlFooter);

            this.Load += (s, e) => split.SplitterDistance = (int)(split.Height * 0.50);
        }

        // ── Comparar ────────────────────────────────────────────────────────

        private void Comparar()
        {
            try
            {
                _data = DatabaseService.ObtenerComparacion(
                    dtpPer1Desde.Value, dtpPer1Hasta.Value,
                    dtpPer2Desde.Value, dtpPer2Hasta.Value);

                lblTituloPer1.Text = $"{dtpPer1Desde.Value:dd/MM/yy} – {dtpPer1Hasta.Value:dd/MM/yy}";
                lblTituloPer2.Text = $"{dtpPer2Desde.Value:dd/MM/yy} – {dtpPer2Hasta.Value:dd/MM/yy}";
                dgv.Columns["colPer1"].HeaderText = lblTituloPer1.Text;
                dgv.Columns["colPer2"].HeaderText = lblTituloPer2.Text;
                lblTituloPer1.Text = "Periodo 1: " + lblTituloPer1.Text;
                lblTituloPer2.Text = "Periodo 2: " + lblTituloPer2.Text;
                lblTituloPer2.Left = lblTituloPer1.Right + 24;

                CargarGrid();
                pnlGrafica.Invalidate();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al comparar: " + ex.Message, "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void CargarGrid()
        {
            dgv.Rows.Clear();
            if (_data == null) return;

            void Fila(string metrica, decimal v1, decimal v2, bool esDinero = true, bool menorEsMejor = false, bool esPct = false)
            {
                if (metrica.StartsWith("──"))
                {
                    int rs = dgv.Rows.Add();
                    dgv.Rows[rs].Cells["colMetrica"].Value = metrica.Replace("──", "").Trim();
                    dgv.Rows[rs].DefaultCellStyle.BackColor = Color.FromArgb(235, 237, 245);
                    dgv.Rows[rs].DefaultCellStyle.Font = new Font("Segoe UI", 8.5f, FontStyle.Bold);
                    dgv.Rows[rs].DefaultCellStyle.ForeColor = Color.FromArgb(50, 50, 120);
                    return;
                }
                decimal dif = v1 - v2;
                decimal pct = v2 != 0 ? (dif / Math.Abs(v2)) * 100 : (v1 != 0 ? 100 : 0);
                string tendencia = dif == 0 ? "→" : (dif > 0 ? "↑" : "↓");
                bool positivo = menorEsMejor ? dif < 0 : dif > 0;

                int row = dgv.Rows.Add();
                dgv.Rows[row].Cells["colMetrica"].Value   = metrica;
                dgv.Rows[row].Cells["colPer1"].Value      = esPct ? $"{v1:N1} %" : esDinero ? $"$ {v1:N2}" : v1.ToString("N0");
                dgv.Rows[row].Cells["colPer2"].Value      = esPct ? $"{v2:N1} %" : esDinero ? $"$ {v2:N2}" : v2.ToString("N0");
                dgv.Rows[row].Cells["colDif"].Value       = esPct ? $"{dif:+0.0;-0.0} pts" : esDinero ? $"$ {dif:N2}" : dif.ToString("N0");
                dgv.Rows[row].Cells["colPct"].Value       = $"{pct:+0.0;-0.0}%";
                dgv.Rows[row].Cells["colTendencia"].Value = tendencia;

                // Color tendencia
                var colorTend = dif == 0 ? Color.Gray
                    : positivo ? Color.FromArgb(39, 174, 96)
                               : Color.FromArgb(192, 57, 43);
                dgv.Rows[row].Cells["colTendencia"].Style.ForeColor = colorTend;
                dgv.Rows[row].Cells["colTendencia"].Style.Font = new Font("Segoe UI", 11f, FontStyle.Bold);

                if (dif != 0)
                    dgv.Rows[row].Cells["colPct"].Style.ForeColor = colorTend;

                // Separadores visuales por sección
                if (metrica.StartsWith("──"))
                {
                    dgv.Rows[row].DefaultCellStyle.BackColor = Color.FromArgb(235, 237, 245);
                    dgv.Rows[row].DefaultCellStyle.Font = new Font("Segoe UI", 8.5f, FontStyle.Bold);
                    dgv.Rows[row].DefaultCellStyle.ForeColor = Color.FromArgb(50, 50, 120);
                }
            }

            Fila("── VENTAS", 0, 0, true);
            Fila("Total Facturado",        _data.VentasPer1,       _data.VentasPer2);
            Fila("Numero de Facturas",     _data.NFacturasPer1,    _data.NFacturasPer2, false);
            Fila("Ticket Promedio",        _data.TicketPer1,       _data.TicketPer2);
            Fila("IVA Ventas",             _data.IvaVentasPer1,    _data.IvaVentasPer2);

            Fila("── COMPRAS", 0, 0, true);
            Fila("Total Compras",          _data.ComprasPer1,      _data.ComprasPer2, menorEsMejor: true);
            Fila("Numero de Compras",      _data.NComprasPer1,     _data.NComprasPer2, false, menorEsMejor: true);
            Fila("IVA Compras",            _data.IvaComprasPer1,   _data.IvaComprasPer2);

            Fila("── GASTOS", 0, 0, true);
            Fila("Total Gastos",           _data.GastosPer1,       _data.GastosPer2, menorEsMejor: true);

            Fila("── RESULTADO", 0, 0, true);
            Fila("Utilidad Bruta",         _data.UtilidadPer1,     _data.UtilidadPer2);
            Fila("Margen Bruto %",         _data.MargenPer1,       _data.MargenPer2, esPct: true);

            Fila("── NOMINA", 0, 0, true);
            Fila("Total Nomina Pagada",    _data.NominaPer1,       _data.NominaPer2, menorEsMejor: true);
            Fila("Periodos Procesados",    _data.NPeriodosPer1,    _data.NPeriodosPer2, false);
        }

        // ── Gráfica de barras comparativa ───────────────────────────────────

        private void PnlGrafica_Paint(object sender, PaintEventArgs e)
        {
            if (_data == null) return;
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            var pnl = sender as Panel;
            int pw = pnl.Width, ph = pnl.Height;
            int padL = 70, padR = 20, padT = 35, padB = 40;
            int w = pw - padL - padR, h = ph - padT - padB;

            var metricas = new[]
            {
                ("Ventas",   _data.VentasPer1,   _data.VentasPer2),
                ("Compras",  _data.ComprasPer1,  _data.ComprasPer2),
                ("Gastos",   _data.GastosPer1,   _data.GastosPer2),
                ("Utilidad", _data.UtilidadPer1, _data.UtilidadPer2),
                ("Nomina",   _data.NominaPer1,   _data.NominaPer2),
            };

            decimal maxVal = metricas.SelectMany(m => new[] { m.Item2, m.Item3 }).Max();
            if (maxVal <= 0) maxVal = 1;

            int n = metricas.Length;
            float groupW = (float)w / n;
            float barW = groupW * 0.3f;
            var colorP1 = Color.FromArgb(22, 55, 120);
            var colorP2 = Color.FromArgb(192, 57, 43);

            using var fntS = new Font("Segoe UI", 7.5f);
            using var brGray = new SolidBrush(Color.FromArgb(120, 120, 140));

            // Eje Y y grid
            using var penEje = new Pen(Color.FromArgb(200, 200, 210));
            g.DrawLine(penEje, padL, padT, padL, padT + h);
            g.DrawLine(penEje, padL, padT + h, padL + w, padT + h);

            using var penGrid = new Pen(Color.FromArgb(230, 232, 240)) { DashStyle = DashStyle.Dash };
            for (int i = 1; i <= 4; i++)
            {
                int y = padT + h - (int)(h * i / 4.0);
                g.DrawLine(penGrid, padL, y, padL + w, y);
                decimal val = maxVal * i / 4;
                string lbl = val >= 1000 ? (val / 1000).ToString("N0") + "k" : val.ToString("N0");
                g.DrawString(lbl, fntS, brGray,
                    new RectangleF(0, y - 8, padL - 4, 16),
                    new StringFormat { Alignment = StringAlignment.Far, LineAlignment = StringAlignment.Center });
            }

            // Barras
            for (int i = 0; i < n; i++)
            {
                var (nombre, v1, v2) = metricas[i];
                float cx = padL + i * groupW + groupW / 2;

                // Barra P1
                float x1 = cx - barW - 2;
                float bh1 = v1 > 0 ? (float)(h * (double)v1 / (double)maxVal) : 2;
                float y1 = padT + h - bh1;
                using var br1 = new SolidBrush(colorP1);
                g.FillRectangle(br1, x1, y1, barW, bh1);
                if (v1 > 0)
                {
                    string s1 = v1 >= 1000 ? (v1 / 1000).ToString("N0") + "k" : v1.ToString("N0");
                    g.DrawString(s1, fntS, new SolidBrush(colorP1),
                        new RectangleF(x1 - 5, y1 - 14, barW + 10, 13),
                        new StringFormat { Alignment = StringAlignment.Center });
                }

                // Barra P2
                float x2 = cx + 2;
                float bh2 = v2 > 0 ? (float)(h * (double)v2 / (double)maxVal) : 2;
                float y2 = padT + h - bh2;
                using var br2 = new SolidBrush(colorP2);
                g.FillRectangle(br2, x2, y2, barW, bh2);
                if (v2 > 0)
                {
                    string s2 = v2 >= 1000 ? (v2 / 1000).ToString("N0") + "k" : v2.ToString("N0");
                    g.DrawString(s2, fntS, new SolidBrush(colorP2),
                        new RectangleF(x2 - 5, y2 - 14, barW + 10, 13),
                        new StringFormat { Alignment = StringAlignment.Center });
                }

                // Etiqueta grupo
                g.DrawString(nombre, fntS, brGray,
                    new RectangleF(cx - groupW / 2, padT + h + 4, groupW, 18),
                    new StringFormat { Alignment = StringAlignment.Center });
            }

            // Título y leyenda
            using var fntTit = new Font("Segoe UI", 9f, FontStyle.Bold);
            g.DrawString("Comparacion por Categorias (USD)", fntTit,
                new SolidBrush(Color.FromArgb(50, 50, 120)), padL, 8);

            int legX = padL + w - 200;
            g.FillRectangle(new SolidBrush(colorP1), legX, 10, 14, 10);
            g.DrawString(lblTituloPer1?.Text ?? "Periodo 1", fntS, brGray, legX + 18, 8);
            g.FillRectangle(new SolidBrush(colorP2), legX, 26, 14, 10);
            g.DrawString(lblTituloPer2?.Text ?? "Periodo 2", fntS, brGray, legX + 18, 24);
        }

        // ── PDF ─────────────────────────────────────────────────────────────

        private void BtnPdf_Click(object sender, EventArgs e)
        {
            if (_data == null) return;
            try
            {
                var bytes = PdfService.GenerarReporteComparacion(
                    _data,
                    dtpPer1Desde.Value, dtpPer1Hasta.Value,
                    dtpPer2Desde.Value, dtpPer2Hasta.Value);
                using var dlg = new SaveFileDialog
                {
                    Filter = "PDF|*.pdf",
                    FileName = $"Comparacion_{DateTime.Now:yyyyMMdd}.pdf"
                };
                if (dlg.ShowDialog() == DialogResult.OK)
                {
                    System.IO.File.WriteAllBytes(dlg.FileName, bytes);
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                    { FileName = dlg.FileName, UseShellExecute = true });
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al generar PDF: " + ex.Message, "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
