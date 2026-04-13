using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Windows.Forms;

namespace SistemaGestion
{
    // ══════════════════════════════════════════════════════════════════════
    //  DASHBOARD — graficas dibujadas con GDI+ puro (sin dependencias)
    // ══════════════════════════════════════════════════════════════════════
    public class FrmDashboard : Form
    {
        // KPI labels
        private Label lblVentasMes, lblVentasHoy, lblFacturasPend;
        private Label lblStockBajo, lblTotalClientes, lblTopProducto;

        // Chart panels
        private Panel pnlBarras;      // ventas ultimos 6 meses
        private Panel pnlLinea;       // tendencia diaria del mes actual
        private Panel pnlDonut;       // estado de facturas
        private Panel pnlStock;       // top 8 productos por existencia

        // Data cache
        private DashData _data;

        public FrmDashboard()
        {
            Text            = "Dashboard — Resumen del Negocio";
            Size            = new Size(1200, 700);
            MinimumSize     = new Size(1000, 600);
            StartPosition   = FormStartPosition.CenterParent;
            BackColor       = Color.FromArgb(245, 247, 252);

            BuildUI();
            this.Load += (s, e) => ActualizarDatos();

            // Auto-refresh cada 60 segundos
            var timer = new System.Windows.Forms.Timer { Interval = 60_000 };
            timer.Tick += (s, e) => ActualizarDatos();
            timer.Start();
            FormClosed += (s, e) => timer.Stop();
        }

        // ── Construccion de la UI ─────────────────────────────────────────
        private void BuildUI()
        {
            // Header
            var hdr = new Panel
            {
                Dock      = DockStyle.Top,
                Height    = 50,
                BackColor = Color.FromArgb(22, 55, 120)
            };
            hdr.Controls.Add(new Label
            {
                Text      = "Dashboard",
                Font      = new Font("Segoe UI", 15f, FontStyle.Bold),
                ForeColor = Color.White,
                AutoSize  = true,
                Location  = new Point(16, 12)
            });
            var lblFecha = new Label
            {
                Text      = DateTime.Now.ToString("dddd dd/MM/yyyy  HH:mm"),
                Font      = new Font("Segoe UI", 9f),
                ForeColor = Color.FromArgb(180, 200, 240),
                AutoSize  = true,
                Location  = new Point(200, 16)
            };
            hdr.Controls.Add(lblFecha);

            var btnPuntoEquilibrio = new Button
            {
                Text = "Punto de Equilibrio",
                Size = new Size(200, 28),
                Location = new Point(550, 11),
                BackColor = Color.FromArgb(20, 70, 130),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnPuntoEquilibrio.FlatAppearance.BorderSize = 0;
            btnPuntoEquilibrio.Click += (s, e) =>
            {
                using var frm = new FrmPuntoEquilibrio();
                frm.ShowDialog(this);
            };
            hdr.Controls.Add(btnPuntoEquilibrio);

            var btnFlujo = new Button
            {
                Text      = "Flujo de Caja",
                Size      = new Size(105, 28),
                Location  = new Point(750, 11),
                BackColor = Color.FromArgb(20, 70, 130),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font      = new Font("Segoe UI", 8.5f, FontStyle.Bold),
                Cursor    = Cursors.Hand
            };
            btnFlujo.FlatAppearance.BorderSize = 0;
            btnFlujo.Click += (s, e) => new FrmFlujoCaja().ShowDialog();
            hdr.Controls.Add(btnFlujo);

            var btnRentab = new Button
            {
                Text      = "Rentabilidad",
                Size      = new Size(100, 28),
                Location  = new Point(860, 11),
                BackColor = Color.FromArgb(30, 100, 60),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font      = new Font("Segoe UI", 8.5f, FontStyle.Bold),
                Cursor    = Cursors.Hand
            };
            btnRentab.FlatAppearance.BorderSize = 0;
            btnRentab.Click += (s, e) => new FrmRentabilidad().ShowDialog();
            hdr.Controls.Add(btnRentab);

            var btnReportes = new Button
            {
                Text      = "Ver Reportes",
                Size      = new Size(100, 28),
                Location  = new Point(970, 11),
                BackColor = Color.FromArgb(120, 50, 150),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font      = new Font("Segoe UI", 8.5f, FontStyle.Bold),
                Cursor    = Cursors.Hand
            };
            btnReportes.FlatAppearance.BorderSize = 0;
            btnReportes.Click += (s, e) => new FrmReportes().ShowDialog();
            hdr.Controls.Add(btnReportes);

            var btnRefresh = new Button
            {
                Text      = "Actualizar",
                Size      = new Size(90, 28),
                Location  = new Point(1080, 11),
                BackColor = Color.FromArgb(40, 140, 80),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font      = new Font("Segoe UI", 8.5f, FontStyle.Bold),
                Cursor    = Cursors.Hand
            };
            btnRefresh.FlatAppearance.BorderSize = 0;
            btnRefresh.Click += (s, e) => ActualizarDatos();
            hdr.Controls.Add(btnRefresh);

            var btnExportPdf = new Button
            {
                Text      = "PDF",
                Size      = new Size(55, 28),
                Location  = new Point(1175, 11),
                BackColor = Color.FromArgb(192, 57, 43),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font      = new Font("Segoe UI", 8.5f, FontStyle.Bold),
                Cursor    = Cursors.Hand
            };
            btnExportPdf.FlatAppearance.BorderSize = 0;
            btnExportPdf.Click += ExportarPdfDashboard;
            hdr.Controls.Add(btnExportPdf);

            // Fila de KPIs
            var pnlKpis = new Panel
            {
                Dock      = DockStyle.Top,
                Height    = 100,
                BackColor = Color.FromArgb(235, 240, 252),
                Padding   = new Padding(10, 8, 10, 8)
            };

            lblVentasMes     = KpiCard(pnlKpis, "Ventas del mes",    Color.FromArgb(22, 55, 120),  0);
            lblVentasHoy     = KpiCard(pnlKpis, "Facturado hoy",     Color.FromArgb(22, 110, 60),  1);
            lblFacturasPend  = KpiCard(pnlKpis, "Facturas pendientes",Color.FromArgb(180, 100, 20), 2);
            lblStockBajo     = KpiCard(pnlKpis, "Productos bajo min", Color.FromArgb(160, 40, 40),  3);
            lblTotalClientes = KpiCard(pnlKpis, "Clientes",           Color.FromArgb(80, 50, 150),  4);
            lblTopProducto   = KpiCard(pnlKpis, "Top producto",       Color.FromArgb(20, 110, 130), 5);

            // Area de graficas
            var pnlGraficas = new Panel
            {
                Dock      = DockStyle.Fill,
                BackColor = Color.FromArgb(245, 247, 252),
                Padding   = new Padding(10)
            };

            // Fila superior: barras + donut
            pnlBarras = ChartPanel("Ventas ultimos 6 meses", Color.FromArgb(22, 55, 120));
            pnlBarras.SetBounds(10, 10, 580, 230);
            pnlBarras.Paint += PaintBarras;

            pnlDonut = ChartPanel("Estado de facturas", Color.FromArgb(80, 50, 150));
            pnlDonut.SetBounds(600, 10, 570, 230);
            pnlDonut.Paint += PaintDonut;

            // Fila inferior: linea + stock
            pnlLinea = ChartPanel("Tendencia diaria (mes actual)", Color.FromArgb(22, 110, 60));
            pnlLinea.SetBounds(10, 250, 580, 230);
            pnlLinea.Paint += PaintLinea;

            pnlStock = ChartPanel("Stock actual — top 8 productos", Color.FromArgb(160, 80, 20));
            pnlStock.SetBounds(600, 250, 570, 230);
            pnlStock.Paint += PaintStock;

            pnlGraficas.Controls.AddRange(new Control[]
                { pnlBarras, pnlDonut, pnlLinea, pnlStock });

            Controls.AddRange(new Control[] { pnlGraficas, pnlKpis, hdr });
        }

        // ── Exportar PDF ──────────────────────────────────────────────────
        private void ExportarPdfDashboard(object sender, EventArgs e)
        {
            if (_data == null)
            {
                MessageBox.Show("Actualiza el dashboard antes de exportar.", "Sin datos",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            try
            {
                var snap = new DashboardSnapshot
                {
                    FechaGeneracion = DateTime.Now,
                    Usuario         = Sesion.NombreCompleto,
                    VentasMes       = decimal.TryParse(lblVentasMes.Text
                        .Replace("$","").Replace(",","").Replace(".","").Trim(), out var vm) ? vm : 0,
                    VentasHoy       = decimal.TryParse(lblVentasHoy.Text
                        .Replace("$","").Replace(",","").Replace(".","").Trim(), out var vh) ? vh : 0,
                    FacturasPend    = int.TryParse(lblFacturasPend.Text, out var fp) ? fp : 0,
                    StockBajo       = int.TryParse(lblStockBajo.Text, out var sb) ? sb : 0,
                    TotalClientes   = int.TryParse(lblTotalClientes.Text, out var tc) ? tc : 0,
                    TopProducto     = lblTopProducto.Text,
                    Saldadas        = _data.Saldadas,
                    Pendientes      = _data.Pendientes,
                    Canceladas      = _data.Canceladas,
                    VentasPorMes    = _data.VentasPorMes,
                    TopStock        = _data.TopStock
                };

                var bytes = PdfService.GenerarDashboardEjecutivo(snap);
                using var dlg = new SaveFileDialog
                {
                    Filter   = "PDF|*.pdf",
                    FileName = $"Dashboard_{DateTime.Now:yyyyMMdd_HHmm}.pdf"
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

        // ── Carga de datos ────────────────────────────────────────────────
        private void ActualizarDatos()
        {
            _data = new DashData();

            var facturas  = DatabaseService.ObtenerFacturas();
            var inventario= DatabaseService.ObtenerInventario();
            var clientes  = DatabaseService.ObtenerClientes();

            // KPIs
            decimal ventasMes = facturas
                .Where(f => f.Abono >= 0 && f.Fecha.Month == DateTime.Today.Month
                                         && f.Fecha.Year  == DateTime.Today.Year)
                .Sum(f => f.TotalF);

            decimal ventasHoy = facturas
                .Where(f => f.Abono >= 0 && f.Fecha.Date == DateTime.Today)
                .Sum(f => f.TotalF);

            int pendientes = facturas
                .Count(f => f.Abono >= 0 && (f.TotalF - f.Abono) > 0);

            int stockBajo = inventario.Count(i => i.Existencia < i.ExistenMin);

            // Top producto por ventas (usando kardex)
            var kardex = DatabaseService.ObtenerKardex();
            string topProd = kardex
                .Where(k => k.TipoMovimiento == "SALIDA")
                .GroupBy(k => k.CodigoProducto)
                .OrderByDescending(g => g.Sum(k => k.Cantidad))
                .Select(g => g.Key)
                .FirstOrDefault() ?? "—";

            // Actualizar KPI labels
            Invoke(() =>
            {
                lblVentasMes.Text     = ventasMes.ToString("C0");
                lblVentasHoy.Text     = ventasHoy.ToString("C0");
                lblFacturasPend.Text  = pendientes.ToString();
                lblStockBajo.Text     = stockBajo.ToString();
                lblTotalClientes.Text = clientes.Count.ToString();
                lblTopProducto.Text   = topProd.Length > 10
                                        ? topProd.Substring(0, 10) + "..." : topProd;
            });

            // Datos para graficas
            // Barras: ventas por mes (ultimos 6 meses)
            _data.VentasPorMes = new List<(string Mes, decimal Total)>();
            for (int i = 5; i >= 0; i--)
            {
                var fecha = DateTime.Today.AddMonths(-i);
                decimal tot = facturas
                    .Where(f => f.Abono >= 0 && f.Fecha.Month == fecha.Month
                                             && f.Fecha.Year  == fecha.Year)
                    .Sum(f => f.TotalF);
                _data.VentasPorMes.Add((fecha.ToString("MMM"), tot));
            }

            // Linea: ventas diarias del mes actual
            int diasMes = DateTime.DaysInMonth(DateTime.Today.Year, DateTime.Today.Month);
            _data.VentasDiarias = new List<decimal>();
            for (int d = 1; d <= Math.Min(diasMes, DateTime.Today.Day); d++)
            {
                var dia = new DateTime(DateTime.Today.Year, DateTime.Today.Month, d);
                decimal tot = facturas
                    .Where(f => f.Abono >= 0 && f.Fecha.Date == dia)
                    .Sum(f => f.TotalF);
                _data.VentasDiarias.Add(tot);
            }

            // Donut: estados de facturas
            _data.Saldadas   = facturas.Count(f => f.Abono >= 0 && (f.TotalF - f.Abono) <= 0);
            _data.Pendientes = facturas.Count(f => f.Abono >= 0 && (f.TotalF - f.Abono) >  0);
            _data.Canceladas = facturas.Count(f => f.Abono < 0);

            // Stock: top 8 productos
            _data.TopStock = inventario
                .OrderByDescending(i => i.Existencia)
                .Take(8)
                .Select(i => (
                    Nombre: (i.Descripcion ?? i.Codigo).Length > 12
                            ? (i.Descripcion ?? i.Codigo).Substring(0, 12) : (i.Descripcion ?? i.Codigo),
                    Stock: i.Existencia,
                    Min:   i.ExistenMin))
                .ToList();

            Invoke(() =>
            {
                pnlBarras.Invalidate();
                pnlLinea.Invalidate();
                pnlDonut.Invalidate();
                pnlStock.Invalidate();
            });
        }

        // ══════════════════════════════════════════════════════════════════
        //  PINTADO DE GRAFICAS
        // ══════════════════════════════════════════════════════════════════

        // ── Grafica de barras ─────────────────────────────────────────────
        private void PaintBarras(object sender, PaintEventArgs e)
        {
            if (_data?.VentasPorMes == null || _data.VentasPorMes.Count == 0) return;

            var g   = e.Graphics;
            var pnl = (Panel)sender;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            int padL = 60, padR = 20, padT = 40, padB = 50;
            int w    = pnl.ClientSize.Width  - padL - padR;
            int h    = pnl.ClientSize.Height - padT - padB;

            decimal maxVal = _data.VentasPorMes.Max(v => v.Total);
            if (maxVal == 0) maxVal = 1;

            int n       = _data.VentasPorMes.Count;
            float barW  = (float)w / n * 0.55f;
            float gap   = (float)w / n;

            // Ejes
            using var penEje = new Pen(Color.FromArgb(200, 200, 210), 1f);
            g.DrawLine(penEje, padL, padT, padL, padT + h);
            g.DrawLine(penEje, padL, padT + h, padL + w, padT + h);

            // Grid horizontal
            using var penGrid = new Pen(Color.FromArgb(230, 232, 240), 1f) { DashStyle = DashStyle.Dash };
            using var fntSmall = new Font("Segoe UI", 7.5f);
            using var brText   = new SolidBrush(Color.FromArgb(120, 120, 140));

            for (int i = 0; i <= 4; i++)
            {
                int y = padT + h - (int)(h * i / 4.0);
                g.DrawLine(penGrid, padL, y, padL + w, y);
                decimal val = maxVal * i / 4;
                string lbl  = val >= 1000 ? (val / 1000).ToString("N0") + "k"
                                          : val.ToString("N0");
                g.DrawString(lbl, fntSmall, brText,
                    new RectangleF(0, y - 8, padL - 4, 16),
                    new StringFormat { Alignment = StringAlignment.Far, LineAlignment = StringAlignment.Center });
            }

            // Barras
            for (int i = 0; i < n; i++)
            {
                var (mes, total) = _data.VentasPorMes[i];
                float x     = padL + i * gap + (gap - barW) / 2;
                float barH  = total == 0 ? 2 : (float)(h * (double)total / (double)maxVal);
                float y     = padT + h - barH;

                bool esMesActual = (i == n - 1);
                Color c = esMesActual
                    ? Color.FromArgb(22, 55, 120)
                    : Color.FromArgb(100, 140, 200);

                using var br = new SolidBrush(c);
                g.FillRectangle(br, x, y, barW, barH);

                // Valor encima
                if (total > 0)
                {
                    string valStr = total >= 1000 ? (total / 1000).ToString("N0") + "k"
                                                  : total.ToString("N0");
                    g.DrawString(valStr, fntSmall,
                        esMesActual ? Brushes.DarkBlue : brText,
                        new RectangleF(x - 5, y - 16, barW + 10, 14),
                        new StringFormat { Alignment = StringAlignment.Center });
                }

                // Etiqueta mes
                g.DrawString(mes, fntSmall, brText,
                    new RectangleF(x - 5, padT + h + 4, barW + 10, 20),
                    new StringFormat { Alignment = StringAlignment.Center });
            }

            // Titulo
            using var fntTit = new Font("Segoe UI", 8f, FontStyle.Bold);
            g.DrawString("$", fntTit, Brushes.Gray, 4, padT + h / 2 - 8);
        }

        // ── Grafica de linea ──────────────────────────────────────────────
        private void PaintLinea(object sender, PaintEventArgs e)
        {
            if (_data?.VentasDiarias == null || _data.VentasDiarias.Count < 2) return;

            var g   = e.Graphics;
            var pnl = (Panel)sender;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            int padL = 60, padR = 20, padT = 35, padB = 35;
            int w    = pnl.ClientSize.Width  - padL - padR;
            int h    = pnl.ClientSize.Height - padT - padB;

            var vals    = _data.VentasDiarias;
            int n       = vals.Count;
            decimal max = vals.Max();
            if (max == 0) max = 1;

            // Grid
            using var penGrid = new Pen(Color.FromArgb(225, 228, 238), 1f) { DashStyle = DashStyle.Dash };
            using var fntS    = new Font("Segoe UI", 7.5f);
            using var brGray  = new SolidBrush(Color.FromArgb(130, 130, 150));

            for (int i = 0; i <= 4; i++)
            {
                int y = padT + h - (int)(h * i / 4.0);
                g.DrawLine(penGrid, padL, y, padL + w, y);
                decimal val = max * i / 4;
                string  lbl = val >= 1000 ? (val / 1000).ToString("N0") + "k" : val.ToString("N0");
                g.DrawString(lbl, fntS, brGray,
                    new RectangleF(0, y - 8, padL - 4, 16),
                    new StringFormat { Alignment = StringAlignment.Far, LineAlignment = StringAlignment.Center });
            }

            // Ejes
            using var penEje = new Pen(Color.FromArgb(190, 193, 210), 1f);
            g.DrawLine(penEje, padL, padT, padL, padT + h);
            g.DrawLine(penEje, padL, padT + h, padL + w, padT + h);

            // Area bajo la curva
            float stepX = n > 1 ? (float)w / (n - 1) : w;
            var pts = new PointF[n + 2];
            pts[0] = new PointF(padL, padT + h);
            for (int i = 0; i < n; i++)
            {
                float x = padL + i * stepX;
                float y = padT + h - (float)(h * (double)vals[i] / (double)max);
                pts[i + 1] = new PointF(x, y);
            }
            pts[n + 1] = new PointF(padL + (n - 1) * stepX, padT + h);

            using var brArea = new SolidBrush(Color.FromArgb(40, 22, 110, 60));
            g.FillPolygon(brArea, pts);

            // Linea
            using var penLine = new Pen(Color.FromArgb(22, 110, 60), 2.5f);
            var linePoints = pts.Skip(1).Take(n).ToArray();
            if (linePoints.Length >= 2)
                g.DrawLines(penLine, linePoints);

            // Puntos y etiqueta dia actual
            for (int i = 0; i < n; i++)
            {
                float x = padL + i * stepX;
                float y = padT + h - (float)(h * (double)vals[i] / (double)max);

                bool esHoy = (i == n - 1);
                float r    = esHoy ? 5 : 3;
                g.FillEllipse(esHoy ? Brushes.DarkGreen : new SolidBrush(Color.FromArgb(22, 110, 60)),
                    x - r, y - r, r * 2, r * 2);

                // Etiqueta dia (cada 5 dias)
                if (i % 5 == 0 || esHoy)
                    g.DrawString((i + 1).ToString(), fntS, brGray,
                        new RectangleF(x - 8, padT + h + 3, 16, 14),
                        new StringFormat { Alignment = StringAlignment.Center });
            }
        }

        // ── Grafica de donut ──────────────────────────────────────────────
        private void PaintDonut(object sender, PaintEventArgs e)
        {
            if (_data == null) return;

            var g   = e.Graphics;
            var pnl = (Panel)sender;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            int total3 = _data.Saldadas + _data.Pendientes + _data.Canceladas;
            if (total3 == 0) return;

            int cx = pnl.ClientSize.Width / 2 - 40;
            int cy = pnl.ClientSize.Height / 2 + 10;
            int r  = Math.Min(pnl.ClientSize.Width / 2 - 80,
                              pnl.ClientSize.Height / 2 - 30);
            r = Math.Max(r, 40);

            var colores = new[] {
                Color.FromArgb(34, 139, 80),   // Saldadas
                Color.FromArgb(230, 160, 20),   // Pendientes
                Color.FromArgb(200, 60, 60)     // Canceladas
            };
            var valores   = new[] { _data.Saldadas, _data.Pendientes, _data.Canceladas };
            var etiquetas = new[] { "Saldadas", "Pendientes", "Canceladas" };

            float angulo = -90f;
            for (int i = 0; i < 3; i++)
            {
                if (valores[i] == 0) continue;
                float sweep = 360f * valores[i] / total3;

                // Sector externo
                using var br = new SolidBrush(colores[i]);
                g.FillPie(br, cx - r, cy - r, r * 2, r * 2, angulo, sweep);

                // Sector interno (hueco del donut)
                int ri = (int)(r * 0.55f);
                using var brBlanco = new SolidBrush(pnl.BackColor);
                g.FillPie(brBlanco, cx - ri, cy - ri, ri * 2, ri * 2, angulo, sweep);

                angulo += sweep;
            }

            // Borde exterior e interior
            using var pen = new Pen(pnl.BackColor, 2);
            g.DrawEllipse(pen, cx - r, cy - r, r * 2, r * 2);

            // Texto central
            using var fntC  = new Font("Segoe UI", 11f, FontStyle.Bold);
            using var fntCl = new Font("Segoe UI", 7.5f);
            g.DrawString(total3.ToString(), fntC, Brushes.DimGray,
                new RectangleF(cx - 20, cy - 14, 40, 22),
                new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center });
            g.DrawString("total", fntCl, Brushes.Gray,
                new RectangleF(cx - 20, cy + 6, 40, 14),
                new StringFormat { Alignment = StringAlignment.Center });

            // Leyenda a la derecha
            using var fntLeg = new Font("Segoe UI", 8f);
            int legX = cx + r + 20;
            int legY = cy - 40;
            for (int i = 0; i < 3; i++)
            {
                using var brLeg = new SolidBrush(colores[i]);
                g.FillRectangle(brLeg, legX, legY + i * 28, 12, 12);
                g.DrawString(etiquetas[i] + ": " + valores[i],
                    fntLeg, Brushes.DimGray,
                    legX + 16, legY + i * 28 - 1);
                decimal pct = total3 > 0 ? (decimal)valores[i] / total3 * 100 : 0;
                g.DrawString(pct.ToString("F0") + "%",
                    fntLeg, new SolidBrush(colores[i]),
                    legX + 16, legY + i * 28 + 12);
            }
        }

        // ── Grafica de stock ──────────────────────────────────────────────
        private void PaintStock(object sender, PaintEventArgs e)
        {
            if (_data?.TopStock == null || _data.TopStock.Count == 0) return;

            var g   = e.Graphics;
            var pnl = (Panel)sender;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            int padL = 100, padR = 20, padT = 30, padB = 20;
            int w    = pnl.ClientSize.Width  - padL - padR;
            int h    = pnl.ClientSize.Height - padT - padB;

            var items    = _data.TopStock;
            int n        = items.Count;
            decimal maxV = items.Max(x => x.Stock);
            if (maxV == 0) maxV = 1;

            float rowH = (float)h / n;
            using var fntS  = new Font("Segoe UI", 7.5f);
            using var brLbl = new SolidBrush(Color.FromArgb(70, 70, 90));

            for (int i = 0; i < n; i++)
            {
                var (nombre, stock, min) = items[i];
                float y    = padT + i * rowH + rowH * 0.15f;
                float barH = rowH * 0.65f;
                float barW = (float)(w * (double)stock / (double)maxV);

                // Color segun estado
                Color c = stock == 0        ? Color.FromArgb(200, 60, 60)
                        : stock < min       ? Color.FromArgb(220, 140, 20)
                        : Color.FromArgb(34, 130, 75);

                // Fondo de la fila (alternado)
                if (i % 2 == 0)
                    g.FillRectangle(new SolidBrush(Color.FromArgb(245, 246, 252)),
                        padL, padT + i * rowH, w, rowH);

                // Barra
                using var br = new SolidBrush(c);
                if (barW > 0)
                    g.FillRectangle(br, padL, y, barW, barH);

                // Linea de minimo
                if (min > 0)
                {
                    float xMin = padL + (float)(w * (double)min / (double)maxV);
                    using var penMin = new Pen(Color.FromArgb(180, 50, 50), 1.5f)
                        { DashStyle = DashStyle.Dash };
                    g.DrawLine(penMin, xMin, padT + i * rowH, xMin, padT + (i + 1) * rowH);
                }

                // Nombre del producto
                g.DrawString(nombre, fntS, brLbl,
                    new RectangleF(2, y + 1, padL - 4, barH),
                    new StringFormat { Alignment = StringAlignment.Far, LineAlignment = StringAlignment.Center });

                // Valor al final de la barra
                g.DrawString(stock.ToString("N0"), fntS,
                    stock < min ? Brushes.Firebrick : Brushes.DarkGreen,
                    padL + barW + 3, y + 1);
            }

            // Leyenda de linea minimo
            using var penLeg = new Pen(Color.FromArgb(180, 50, 50), 1.5f) { DashStyle = DashStyle.Dash };
            g.DrawLine(penLeg, padL, pnl.ClientSize.Height - 14,
                                padL + 18, pnl.ClientSize.Height - 14);
            g.DrawString("Stock minimo", fntS, brLbl,
                padL + 22, pnl.ClientSize.Height - 20);
        }

        // ══════════════════════════════════════════════════════════════════
        //  HELPERS UI
        // ══════════════════════════════════════════════════════════════════
        private Label KpiCard(Panel parent, string titulo, Color color, int index)
        {
            int cardW = (parent.Width - 20) / 6;
            int x     = 10 + index * cardW;

            var card = new Panel
            {
                Location  = new Point(x, 8),
                Size      = new Size(cardW - 6, 80),
                BackColor = color
            };

            card.Controls.Add(new Label
            {
                Text      = titulo,
                Font      = new Font("Segoe UI", 7.5f),
                ForeColor = Color.FromArgb(200, 220, 255),
                AutoSize  = false,
                Size      = new Size(cardW - 16, 18),
                Location  = new Point(8, 8),
                TextAlign = ContentAlignment.TopLeft
            });

            var lblValor = new Label
            {
                Text      = "—",
                Font      = new Font("Segoe UI", 16f, FontStyle.Bold),
                ForeColor = Color.White,
                AutoSize  = false,
                Size      = new Size(cardW - 16, 36),
                Location  = new Point(8, 26),
                TextAlign = ContentAlignment.MiddleLeft
            };
            card.Controls.Add(lblValor);
            parent.Controls.Add(card);

            // Resize dynamically
            parent.Resize += (s, e) =>
            {
                int cw = (parent.Width - 20) / 6;
                card.SetBounds(10 + index * cw, 8, cw - 6, 80);
                lblValor.Width = cw - 16;
            };

            return lblValor;
        }

        private Panel ChartPanel(string titulo, Color colorTitulo)
        {
            var pnl = new Panel
            {
                BackColor   = Color.White,
                BorderStyle = BorderStyle.None
            };

            // Sombra ligera via Paint del contenedor
            pnl.Paint += (s, e) =>
            {
                var rect = new Rectangle(0, 0, pnl.Width - 1, pnl.Height - 1);
                using var pen = new Pen(Color.FromArgb(210, 215, 230), 1f);
                e.Graphics.DrawRectangle(pen, rect);

                // Barra de titulo
                using var brBar = new SolidBrush(colorTitulo);
                e.Graphics.FillRectangle(brBar, 0, 0, pnl.Width, 28);
                using var fnt = new Font("Segoe UI", 8.5f, FontStyle.Bold);
                e.Graphics.DrawString(titulo, fnt, Brushes.White,
                    new RectangleF(10, 6, pnl.Width - 20, 16));
            };

            // Resize: adaptar a ventana
            var parent = this;
            Resize += (s, e) =>
            {
                int gw = (parent.ClientSize.Width  - 20) / 2;
                int gh = (parent.ClientSize.Height - 200) / 2;
                if (pnl.Left < parent.ClientSize.Width / 2)
                    pnl.Width = gw - 10;
                else
                {
                    pnl.Left  = 10 + gw;
                    pnl.Width = parent.ClientSize.Width - pnl.Left - 10;
                }
                if (pnl.Top < parent.ClientSize.Height / 2)
                    pnl.Height = gh - 10;
                else
                {
                    pnl.Height = gh - 10;
                    pnl.Top    = 10 + gh;
                }
                pnl.Invalidate();
            };

            return pnl;
        }
    }

    // ── Contenedor de datos del dashboard ────────────────────────────────
    class DashData
    {
        public List<(string Mes, decimal Total)>            VentasPorMes  { get; set; }
        public List<decimal>                                VentasDiarias { get; set; }
        public List<(string Nombre, decimal Stock, decimal Min)> TopStock { get; set; }
        public int Saldadas   { get; set; }
        public int Pendientes { get; set; }
        public int Canceladas { get; set; }
    }
}
