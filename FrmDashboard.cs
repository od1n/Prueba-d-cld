using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Windows.Forms;

namespace SistemaGestion
{
    // ════════════════════════════════════════════════════════════════════════
    //  FrmDashboard — rediseño v2
    //  Layout: TopBar + sidebar izquierda + área de KPIs + gráficas
    // ════════════════════════════════════════════════════════════════════════
    public class FrmDashboard : FormBase
    {
        // ── Paleta (sincronizada con MenuPrincipal) ───────────────────────
        private static bool _darkMode =>
            DatabaseService.ObtenerConfig("TemaOscuro", "0") == "1";

        private Color C1     => _darkMode ? Color.FromArgb(30,  42, 58)  : Color.FromArgb(26, 74, 138);
        private Color C2     => _darkMode ? Color.FromArgb(37,  53, 71)  : Color.FromArgb(30, 90, 160);
        private Color CFondo => _darkMode ? Color.FromArgb(20,  29, 40)  : Color.FromArgb(240,244,250);
        private Color CSurf  => _darkMode ? Color.FromArgb(30,  42, 58)  : Color.FromArgb(255,255,255);
        private Color CBorde => _darkMode ? Color.FromArgb(45,  63, 85)  : Color.FromArgb(220,230,245);
        private Color CMuted => _darkMode ? Color.FromArgb(122,149,176)  : Color.FromArgb(100,116,139);
        private Color CText  => _darkMode ? Color.FromArgb(226,234,245)  : Color.FromArgb(30, 41, 59);
        private Color CAccent=> _darkMode ? Color.FromArgb(45,  63, 85)  : Color.FromArgb(232,240,250);

        private static readonly Color COk    = Color.FromArgb(26, 110,  58);
        private static readonly Color CWarn  = Color.FromArgb(176, 119,  0);
        private static readonly Color CDang  = Color.FromArgb(192,  57, 43);

        // ── KPI labels ────────────────────────────────────────────────────
        private Label lblVentasMes, lblVentasHoy, lblFactPend, lblStockBajo,
                      lblClientes,  lblTopProd,   lblTasaHdr;

        // ── Chart panels ──────────────────────────────────────────────────
        private Panel pnlBarras, pnlDonut, pnlLinea, pnlStock, pnlAlertas;
        private DashData _data;

        public FrmDashboard()
        {
            Text          = "Dashboard — Resumen del Negocio";
            Size          = new Size(1240, 720);
            MinimumSize   = new Size(1000, 600);
            StartPosition = FormStartPosition.CenterParent;
            BackColor     = CFondo;

            BuildUI();
            Load    += (s, e) => ActualizarDatos();

            VentanaPrefs.Restaurar(this);

            // Guardar posición/tamaño con debounce 800ms
            var timerPrefs = new System.Windows.Forms.Timer { Interval = 800 };
            timerPrefs.Tick     += (s, e) => { timerPrefs.Stop(); VentanaPrefs.Guardar(this); };
            SizeChanged         += (s, e) => { timerPrefs.Stop(); timerPrefs.Start(); };
            LocationChanged     += (s, e) => { timerPrefs.Stop(); timerPrefs.Start(); };
            FormClosed          += (s, e) => { timerPrefs.Stop(); timerPrefs.Dispose(); };

            var timer = new System.Windows.Forms.Timer { Interval = 60_000 };
            timer.Tick  += (s, e) => ActualizarDatos();
            timer.Start();
            FormClosed += (s, e) => timer.Stop();
        }

        // ════════════════════════════════════════════════════════════════
        //  BUILD UI
        // ════════════════════════════════════════════════════════════════
        private void BuildUI()
        {
            // ── TopBar ────────────────────────────────────────────────────
            var topBar = new Panel { Dock = DockStyle.Top, Height = 50, BackColor = C1 };

            var lblTitulo = new Label {
                Text = "Dashboard", Font = new Font("Segoe UI", 13f, FontStyle.Bold),
                ForeColor = Color.White, AutoSize = true, Location = new Point(16, 13)
            };

            lblTasaHdr = new Label {
                Name = "lblTasaHdr", Text = "Tasa: —",
                Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
                ForeColor = Color.FromArgb(150, 210, 170),
                AutoSize = true, Location = new Point(160, 17)
            };

            var lblFecha = new Label {
                Text = DateTime.Now.ToString("dddd dd/MM/yyyy",
                    new System.Globalization.CultureInfo("es-ES")),
                Font = new Font("Segoe UI", 8.5f),
                ForeColor = Color.FromArgb(180, 200, 230),
                AutoSize = true, Location = new Point(160, 33)
            };

            // Botones de acción — FlowLayoutPanel anclado a la derecha
            var pnlBtns = new FlowLayoutPanel {
                Dock = DockStyle.Right,
                AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false, BackColor = Color.Transparent,
                Padding = new Padding(0, 11, 8, 0)
            };

            // Botón ayuda
            var btnAyudaDash = new Button {
                Text = "? Ayuda", Size = new Size(80, 28),
                BackColor = Color.FromArgb(80, 50, 150), ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
                Margin = new Padding(0, 0, 4, 0), Cursor = Cursors.Hand
            };
            btnAyudaDash.FlatAppearance.BorderSize = 0;
            btnAyudaDash.Click += (s, e) => AyudaService.Mostrar(this, "FrmDashboard");
            pnlBtns.Controls.Add(btnAyudaDash);

            var btnsTop = new (string t, Color c, Action a)[] {
                ("Pto. Equilibrio", Color.FromArgb(20,  70,130), () => new FrmPuntoEquilibrio().ShowDialog()),
                ("Flujo de Caja",   Color.FromArgb(20,  70,130), () => new FrmFlujoCaja().ShowDialog()),
                ("Rentabilidad",    Color.FromArgb(30, 100, 60), () => new FrmRentabilidad().ShowDialog()),
                ("Ver Reportes",    Color.FromArgb(100, 40,150), () => new FrmReportes().ShowDialog()),
                ("↺ Actualizar",    Color.FromArgb(40, 140, 80), () => ActualizarDatos()),
            };

            foreach (var (t, c, a) in btnsTop)
            {
                var btn = new Button {
                    Text = t, Size = new Size(110, 28),
                    BackColor = c, ForeColor = Color.White,
                    FlatStyle = FlatStyle.Flat,
                    Font = new Font("Segoe UI", 8f, FontStyle.Bold),
                    Margin = new Padding(0, 0, 4, 0)
                };
                btn.FlatAppearance.BorderSize = 0;
                var aa = a; btn.Click += (s, e) => aa();
                pnlBtns.Controls.Add(btn);
            }

            topBar.Controls.AddRange(new Control[] { lblTitulo, lblTasaHdr, lblFecha, pnlBtns });

            // ── Layout principal ──────────────────────────────────────────
            var mainLayout = new Panel { Dock = DockStyle.Fill, BackColor = CFondo };

            // ── Sidebar de navegación rápida ──────────────────────────────
            var sidebar = new Panel {
                Dock = DockStyle.Left, Width = 170,
                BackColor = C2
            };

            var navItems = new (string ico, string label, Action accion)[] {
                ("▣", "Dashboard",      null),
                ("◈", "Transacciones",  () => new FrmTransacciones().ShowDialog()),
                ("◧", "Inventario",     () => new FrmInventario().ShowDialog()),
                ("⊞", "Producción",     () => new FrmRegistroProduccionDia().ShowDialog()),
                ("◫", "Compras",        () => new FrmCompras().ShowDialog()),
                ("▤", "Reportes",       () => new FrmReportes().ShowDialog()),
                ("◎", "Nómina",         () => new FrmNomina().ShowDialog()),
            };

            int ny = 12;
            foreach (var (ico, label, accion) in navItems)
            {
                bool esDash = accion == null;
                var item = new Panel {
                    Size = new Size(154, 34), Location = new Point(8, ny),
                    BackColor = esDash ? Color.FromArgb(50, 100, 185) : Color.Transparent,
                    Cursor = esDash ? Cursors.Default : Cursors.Hand
                };
                RoundRegion(item, 7);

                var lIco = new Label {
                    Text = ico, Font = new Font("Segoe UI", 12f),
                    ForeColor = Color.White, AutoSize = true,
                    Location = new Point(10, 8)
                };
                var lLbl = new Label {
                    Text = label, Font = new Font("Segoe UI", 9f,
                        esDash ? FontStyle.Bold : FontStyle.Regular),
                    ForeColor = esDash ? Color.White : Color.FromArgb(200, 220, 255),
                    AutoSize = true, Location = new Point(34, 9)
                };

                if (!esDash)
                {
                    var aa = accion;
                    item.MouseEnter += (s, e) => { item.BackColor = Color.FromArgb(40,80,150); item.Invalidate(); };
                    item.MouseLeave += (s, e) => { item.BackColor = Color.Transparent; };
                    item.Click  += (s, e) => aa();
                    lIco.Click  += (s, e) => aa();
                    lLbl.Click  += (s, e) => aa();
                    lIco.MouseEnter += (s, e) => { item.BackColor = Color.FromArgb(40,80,150); };
                    lLbl.MouseEnter += (s, e) => { item.BackColor = Color.FromArgb(40,80,150); };
                    lIco.MouseLeave += (s, e) => { item.BackColor = Color.Transparent; };
                    lLbl.MouseLeave += (s, e) => { item.BackColor = Color.Transparent; };
                }

                item.Controls.AddRange(new Control[] { lIco, lLbl });
                sidebar.Controls.Add(item);
                ny += 38;
            }

            // ── Área de contenido ─────────────────────────────────────────
            var content = new Panel { Dock = DockStyle.Fill, BackColor = CFondo, Padding = new Padding(16) };

            // ── Barra de alertas ──────────────────────────────────────────
            pnlAlertas = new Panel {
                Dock = DockStyle.Top, Height = 38,
                BackColor = Color.FromArgb(255, 243, 205),
                Visible = false, Padding = new Padding(12, 0, 8, 0)
            };
            var lblAlerta = new Label {
                Name = "lblAlertas", Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
                ForeColor = Color.FromArgb(146, 97, 0),
                TextAlign = ContentAlignment.MiddleLeft
            };
            var btnVerAlerta = new Button {
                Text = "Ver detalle", Dock = DockStyle.Right, Width = 100,
                BackColor = Color.FromArgb(210, 140, 0), ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 8f, FontStyle.Bold),
                Margin = new Padding(0, 4, 4, 4)
            };
            btnVerAlerta.FlatAppearance.BorderSize = 0;
            btnVerAlerta.Click += (s, e) => {
                try { new FrmAlertas(AlertasService.ObtenerAlertas()).ShowDialog(this); } catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"[Dashboard] FrmAlertas: {ex.Message}"); }
            };
            pnlAlertas.Controls.AddRange(new Control[] { lblAlerta, btnVerAlerta });

            // ── Fila de KPI cards ─────────────────────────────────────────
            var pnlKpis = new Panel {
                Dock = DockStyle.Top, Height = 104,
                BackColor = CFondo, Padding = new Padding(0, 4, 0, 8)
            };

            lblVentasMes   = KpiCard(pnlKpis, "Ventas del mes",     Color.FromArgb(26, 74,138),  0);
            lblVentasHoy   = KpiCard(pnlKpis, "Facturado hoy",      Color.FromArgb(26,110, 58),  1);
            lblFactPend    = KpiCard(pnlKpis, "Facturas pendientes", Color.FromArgb(176,119,  0), 2);
            lblStockBajo   = KpiCard(pnlKpis, "Stock bajo mín.",     Color.FromArgb(192, 57, 43), 3);
            lblClientes    = KpiCard(pnlKpis, "Clientes",            Color.FromArgb(80, 50,150),  4);
            lblTopProd     = KpiCard(pnlKpis, "Top producto",        Color.FromArgb(20,110,130),  5);

            pnlKpis.Resize += (s, e) => RecalcKpis(pnlKpis);

            // ── Área de gráficas ──────────────────────────────────────────
            var pnlGraficas = new Panel { Dock = DockStyle.Fill, BackColor = CFondo, Padding = new Padding(0, 4, 0, 0) };

            pnlBarras = ChartCard("Ventas últimos 6 meses");
            pnlLinea  = ChartCard("Tendencia diaria (mes actual)");
            pnlDonut  = ChartCard("Estado de facturas");
            pnlStock  = ChartCard("Stock — top 8 productos");

            pnlBarras.Paint += PaintBarras;
            pnlLinea.Paint  += PaintLinea;
            pnlDonut.Paint  += PaintDonut;
            pnlStock.Paint  += PaintStock;

            pnlGraficas.Controls.AddRange(new Control[] { pnlBarras, pnlDonut, pnlLinea, pnlStock });
            LayoutCharts(pnlGraficas);
            pnlGraficas.Resize += (s, e) => LayoutCharts(pnlGraficas);

            content.Controls.AddRange(new Control[] { pnlGraficas, pnlKpis, pnlAlertas });

            mainLayout.Controls.AddRange(new Control[] { content, sidebar });

            Controls.AddRange(new Control[] { mainLayout, topBar });
        }

        // ════════════════════════════════════════════════════════════════
        //  KPI CARD
        // ════════════════════════════════════════════════════════════════
        private Label KpiCard(Panel parent, string titulo, Color acento, int idx)
        {
            var card = new Panel {
                BackColor = CSurf,
                BorderStyle = BorderStyle.None
            };
            card.Paint += (s, e) => {
                var g = e.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;
                using var pen = new Pen(CBorde, 1f);
                using var path = RoundRect(new Rectangle(0,0,card.Width-1,card.Height-1), 10);
                g.FillPath(new SolidBrush(CSurf), path);
                g.DrawPath(pen, path);
                // Acento izquierdo
                g.FillRectangle(new SolidBrush(acento), 0, 12, 3, card.Height - 24);
            };

            var lblT = new Label {
                Text = titulo, AutoSize = false, Height = 16,
                Font = new Font("Segoe UI", 8f), ForeColor = CMuted,
                Location = new Point(14, 12), TextAlign = ContentAlignment.TopLeft
            };
            var lblV = new Label {
                Text = "—", AutoSize = false, Height = 32,
                Font = new Font("Segoe UI", 20f, FontStyle.Regular),
                ForeColor = CText, Location = new Point(12, 28)
            };

            card.Controls.AddRange(new Control[] { lblT, lblV });
            parent.Controls.Add(card);

            parent.Resize += (s, e) => {
                int n = 6, gap = 10;
                int w = (parent.ClientSize.Width - (n - 1) * gap) / n;
                int x = idx * (w + gap);
                card.SetBounds(x, 0, w, parent.ClientSize.Height - 8);
                lblT.Width = w - 20;
                lblV.Width = w - 20;
            };

            return lblV;
        }

        private void RecalcKpis(Panel p) { p.Invalidate(); foreach (Control c in p.Controls) c.Invalidate(); }

        // ════════════════════════════════════════════════════════════════
        //  DATOS
        // ════════════════════════════════════════════════════════════════
        private void ActualizarDatos()
        {
            _data = new DashData();
            // Excluye anuladas — no son ventas reales
            var facturas   = DatabaseService.ObtenerFacturas(incluirAnuladas: false);
            var inventario = DatabaseService.ObtenerInventario();
            var clientes   = DatabaseService.ObtenerClientes();

            decimal ventasMes  = facturas.Where(f => f.Abono >= 0 && f.Fecha.Month == DateTime.Today.Month && f.Fecha.Year == DateTime.Today.Year).Sum(f => f.TotalF);
            decimal ventasHoy  = facturas.Where(f => f.Abono >= 0 && f.Fecha.Date == DateTime.Today).Sum(f => f.TotalF);
            int     pendientes = facturas.Count(f => f.Abono >= 0 && (f.TotalF - f.Abono) > 0);
            int     stockBajo  = inventario.Count(i => i.ExistenMin > 0 && i.Existencia < i.ExistenMin);

            var kardex = DatabaseService.ObtenerKardex();
            string topProd = kardex.Where(k => k.TipoMovimiento == "SALIDA")
                .GroupBy(k => k.CodigoProducto).OrderByDescending(g => g.Sum(k => k.Cantidad))
                .Select(g => g.Key).FirstOrDefault() ?? "—";

            Invoke(() => {
                lblVentasMes.Text  = "$" + ventasMes.ToString("N0");
                lblVentasHoy.Text  = "$" + ventasHoy.ToString("N0");
                lblFactPend.Text   = pendientes.ToString();
                lblStockBajo.Text  = stockBajo.ToString();
                lblClientes.Text   = clientes.Count.ToString();
                lblTopProd.Text    = topProd.Length > 10 ? topProd[..10] + "…" : topProd;

                // Tasa
                try {
                    var t = DatabaseService.ObtenerTasaHoy();
                    lblTasaHdr.Text = t != null
                        ? $"BCV: {t.TasaBCV:N2}  ·  Par: {t.TasaParalelo:N2}  ({t.Fecha:dd/MM})"
                        : "⚠ Sin tasa hoy";
                    lblTasaHdr.ForeColor = t != null
                        ? Color.FromArgb(150, 210, 170)
                        : Color.FromArgb(255, 200, 100);
                } catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"[Dashboard] {ex.Message}"); }

                // Alertas
                try {
                    var al = AlertasService.ObtenerAlertas();
                    var lbl = pnlAlertas.Controls.Find("lblAlertas", false).FirstOrDefault() as Label;
                    if (al.TieneAlertas && lbl != null) {
                        var p2 = new List<string>();
                        if (al.StockBajo.Count   > 0) p2.Add($"⚠ Stock bajo: {al.StockBajo.Count}");
                        if (al.CxCVencidas.Count > 0) p2.Add($"⚠ CxC vencidas: {al.CxCVencidas.Count}");
                        if (al.MantVencido.Count  > 0) p2.Add($"⚠ Mantenimiento: {al.MantVencido.Count}");
                        lbl.Text = string.Join("     |     ", p2);
                        bool crit = al.CxCVencidas.Count > 0 || al.StockBajo.Count > 2;
                        pnlAlertas.BackColor = crit ? Color.FromArgb(253,235,235) : Color.FromArgb(255,243,205);
                        lbl.ForeColor = crit ? Color.FromArgb(140,20,20) : Color.FromArgb(146,97,0);
                        pnlAlertas.Visible = true;
                    } else { pnlAlertas.Visible = false; }
                } catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"[Dashboard] {ex.Message}"); }
            });

            // Datos gráficas
            _data.VentasPorMes = new List<(string, decimal)>();
            for (int i = 5; i >= 0; i--) {
                var f = DateTime.Today.AddMonths(-i);
                _data.VentasPorMes.Add((f.ToString("MMM"), facturas.Where(x => x.Abono >= 0 && x.Fecha.Month == f.Month && x.Fecha.Year == f.Year).Sum(x => x.TotalF)));
            }

            int dias = DateTime.DaysInMonth(DateTime.Today.Year, DateTime.Today.Month);
            _data.VentasDiarias = new List<decimal>();
            for (int d = 1; d <= Math.Min(dias, DateTime.Today.Day); d++) {
                var dia = new DateTime(DateTime.Today.Year, DateTime.Today.Month, d);
                _data.VentasDiarias.Add(facturas.Where(f => f.Abono >= 0 && f.Fecha.Date == dia).Sum(f => f.TotalF));
            }

            _data.Saldadas   = facturas.Count(f => f.Abono >= 0 && (f.TotalF - f.Abono) <= 0);
            _data.Pendientes = facturas.Count(f => f.Abono >= 0 && (f.TotalF - f.Abono) >  0);
            _data.Canceladas = facturas.Count(f => f.Abono < 0);
            _data.TopStock   = inventario.OrderByDescending(i => i.Existencia).Take(8)
                .Select(i => (Nombre: (i.Descripcion ?? i.Codigo).Length > 14 ? (i.Descripcion ?? i.Codigo)[..14] : (i.Descripcion ?? i.Codigo), Stock: i.Existencia, Min: i.ExistenMin)).ToList();

            Invoke(() => {
                pnlBarras.Invalidate(); pnlLinea.Invalidate();
                pnlDonut.Invalidate();  pnlStock.Invalidate();
            });
        }

        // ════════════════════════════════════════════════════════════════
        //  LAYOUT CHARTS
        // ════════════════════════════════════════════════════════════════
        private void LayoutCharts(Panel p)
        {
            int gap = 10, pad = 0;
            int w2 = (p.ClientSize.Width - gap - pad * 2) / 2;
            int h2 = (p.ClientSize.Height - gap) / 2;
            pnlBarras.SetBounds(pad,        pad,     w2, h2);
            pnlDonut.SetBounds( pad + w2 + gap, pad, p.ClientSize.Width - w2 - gap - pad, h2);
            pnlLinea.SetBounds( pad,       pad + h2 + gap, w2, p.ClientSize.Height - h2 - gap);
            pnlStock.SetBounds( pad + w2 + gap, pad + h2 + gap, p.ClientSize.Width - w2 - gap - pad, p.ClientSize.Height - h2 - gap);
            foreach (var c in new[]{pnlBarras,pnlDonut,pnlLinea,pnlStock}) c.Invalidate();
        }

        // ════════════════════════════════════════════════════════════════
        //  CHART CARD BASE
        // ════════════════════════════════════════════════════════════════
        private Panel ChartCard(string titulo)
        {
            var p = new Panel { BackColor = CSurf };
            p.Paint += (s, e) => {
                var g = e.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;
                using var path = RoundRect(new Rectangle(0, 0, p.Width-1, p.Height-1), 10);
                g.FillPath(new SolidBrush(CSurf), path);
                using var pen = new Pen(CBorde, 1f);
                g.DrawPath(pen, path);
                g.FillRectangle(new SolidBrush(C1), 12, 10, p.Width - 24, 22);
                g.DrawString(titulo, new Font("Segoe UI", 8.5f, FontStyle.Bold),
                    Brushes.White, new RectangleF(16, 12, p.Width - 32, 18));
            };
            return p;
        }

        // ════════════════════════════════════════════════════════════════
        //  PINTADO — BARRAS
        // ════════════════════════════════════════════════════════════════
        private void PaintBarras(object sender, PaintEventArgs e)
        {
            if (_data?.VentasPorMes == null || _data.VentasPorMes.Count == 0) return;
            var g = e.Graphics; var pnl = (Panel)sender;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            int padL = 20, padR = 10, padT = 38, padB = 28;
            int w = pnl.ClientSize.Width - padL - padR;
            int h = pnl.ClientSize.Height - padT - padB;
            int n = _data.VentasPorMes.Count;
            if (n == 0 || h <= 0) return;
            decimal maxV = _data.VentasPorMes.Max(x => x.Total); if (maxV == 0) maxV = 1;
            float barW = (float)w / n * 0.6f, gap2 = (float)w / n;
            using var fnt = new Font("Segoe UI", 7.5f);
            for (int i = 0; i < n; i++) {
                var (mes, tot) = _data.VentasPorMes[i];
                float barH = (float)(h * (double)tot / (double)maxV);
                float x = padL + i * gap2 + gap2 * 0.2f;
                float y = padT + h - barH;
                bool actual = i == n - 1;
                using var br = new SolidBrush(actual ? C1 : Color.FromArgb(_darkMode?60:200, _darkMode?85:215, _darkMode?115:240));
                if (barH > 0) g.FillRectangle(br, x, y, barW, barH);
                g.DrawString(mes, fnt, new SolidBrush(CMuted), x, padT + h + 4);
                if (tot > 0) g.DrawString(tot.ToString("N0"), fnt, new SolidBrush(CText), x - 2, y - 14);
            }
        }

        // ════════════════════════════════════════════════════════════════
        //  PINTADO — LÍNEA
        // ════════════════════════════════════════════════════════════════
        private void PaintLinea(object sender, PaintEventArgs e)
        {
            if (_data?.VentasDiarias == null || _data.VentasDiarias.Count < 2) return;
            var g = e.Graphics; var pnl = (Panel)sender;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            int padL = 20, padR = 10, padT = 38, padB = 24;
            int w = pnl.ClientSize.Width - padL - padR;
            int h = pnl.ClientSize.Height - padT - padB;
            var data = _data.VentasDiarias;
            decimal maxV = data.Max(); if (maxV == 0) maxV = 1;
            float stepX = (float)w / (data.Count - 1);
            var pts = data.Select((v, i) => new PointF(
                padL + i * stepX,
                padT + h - (float)(h * (double)v / (double)maxV))).ToArray();
            using var penLine = new Pen(Color.FromArgb(26, 74, 138), 2f) { LineJoin = LineJoin.Round };
            g.DrawLines(penLine, pts);
            foreach (var pt in pts)
                g.FillEllipse(new SolidBrush(C1), pt.X - 3, pt.Y - 3, 6, 6);
        }

        // ════════════════════════════════════════════════════════════════
        //  PINTADO — DONUT
        // ════════════════════════════════════════════════════════════════
        private void PaintDonut(object sender, PaintEventArgs e)
        {
            if (_data == null) return;
            var g = e.Graphics; var pnl = (Panel)sender;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            int total3 = _data.Saldadas + _data.Pendientes + _data.Canceladas;
            if (total3 == 0) return;
            int cy = pnl.Height / 2 + 14, cx = pnl.Width / 3;
            int r = Math.Min(cx, cy - 8) - 12;
            var colores  = new[] { COk, CWarn, CDang };
            var valores  = new[] { _data.Saldadas, _data.Pendientes, _data.Canceladas };
            var etiquetas= new[] { $"Saldadas {_data.Saldadas}", $"Pendientes {_data.Pendientes}", $"Vencidas {_data.Canceladas}" };
            float startAngle = -90f;
            for (int i = 0; i < 3; i++) {
                if (valores[i] == 0) continue;
                float sweep = 360f * valores[i] / total3;
                g.FillPie(new SolidBrush(colores[i]), cx - r, cy - r, r * 2, r * 2, startAngle, sweep);
                startAngle += sweep;
            }
            g.FillEllipse(new SolidBrush(CSurf), cx - r/2, cy - r/2, r, r);
            using var fntC = new Font("Segoe UI", 13f, FontStyle.Bold);
            g.DrawString(total3.ToString(), fntC, new SolidBrush(CText),
                new RectangleF(cx - r/2, cy - r/2, r, r),
                new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center });

            using var fntL = new Font("Segoe UI", 8f);
            int legX = cx + r + 14, legY = cy - 40;
            for (int i = 0; i < 3; i++) {
                g.FillEllipse(new SolidBrush(colores[i]), legX, legY + i * 26 + 2, 10, 10);
                g.DrawString(etiquetas[i], fntL, new SolidBrush(CText), legX + 16, legY + i * 26);
                decimal pct = total3 > 0 ? (decimal)valores[i] / total3 * 100 : 0;
                g.DrawString(pct.ToString("F0") + "%", fntL, new SolidBrush(colores[i]), legX + 16, legY + i * 26 + 12);
            }
        }

        // ════════════════════════════════════════════════════════════════
        //  PINTADO — STOCK
        // ════════════════════════════════════════════════════════════════
        private void PaintStock(object sender, PaintEventArgs e)
        {
            if (_data?.TopStock == null || _data.TopStock.Count == 0) return;
            var g = e.Graphics; var pnl = (Panel)sender;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            int padL = 120, padR = 20, padT = 38, padB = 10;
            int w = pnl.ClientSize.Width - padL - padR;
            int h = pnl.ClientSize.Height - padT - padB;
            var items = _data.TopStock; int n = items.Count;
            decimal maxV = items.Max(x => x.Stock); if (maxV == 0) maxV = 1;
            float rowH = (float)h / n;
            using var fnt = new Font("Segoe UI", 7.5f);
            for (int i = 0; i < n; i++) {
                var (nombre, stock, min) = items[i];
                float y = padT + i * rowH + rowH * 0.1f;
                float bH = rowH * 0.7f;
                float bW = (float)(w * (double)stock / (double)maxV);
                if (i % 2 == 0) g.FillRectangle(new SolidBrush(Color.FromArgb(_darkMode?35:248,_darkMode?48:250,_darkMode?62:252)), padL, padT + i * rowH, w, rowH);
                Color c = stock == 0 ? CDang : stock < min && min > 0 ? CWarn : COk;
                if (bW > 0) g.FillRectangle(new SolidBrush(c), padL, y, bW, bH);
                if (min > 0) {
                    float xMin = padL + (float)(w * (double)min / (double)maxV);
                    using var penMin = new Pen(CDang, 1.5f) { DashStyle = DashStyle.Dash };
                    g.DrawLine(penMin, xMin, padT + i * rowH, xMin, padT + (i + 1) * rowH);
                }
                g.DrawString(nombre, fnt, new SolidBrush(CMuted),
                    new RectangleF(2, y, padL - 4, bH),
                    new StringFormat { Alignment = StringAlignment.Far, LineAlignment = StringAlignment.Center });
                g.DrawString(stock.ToString("N0"), fnt, new SolidBrush(c), padL + bW + 3, y);
            }
        }

        // ════════════════════════════════════════════════════════════════
        //  HELPERS
        // ════════════════════════════════════════════════════════════════
        private static GraphicsPath RoundRect(Rectangle r, int rad)
        {
            var p = new GraphicsPath();
            p.AddArc(r.X, r.Y, rad*2, rad*2, 180, 90);
            p.AddArc(r.Right-rad*2, r.Y, rad*2, rad*2, 270, 90);
            p.AddArc(r.Right-rad*2, r.Bottom-rad*2, rad*2, rad*2, 0, 90);
            p.AddArc(r.X, r.Bottom-rad*2, rad*2, rad*2, 90, 90);
            p.CloseFigure();
            return p;
        }

        private static void RoundRegion(Control c, int r)
        {
            c.Region = System.Drawing.Region.FromHrgn(
                CreateRoundRectRgn(0, 0, c.Width+1, c.Height+1, r, r));
        }

        [System.Runtime.InteropServices.DllImport("Gdi32.dll")]
        private static extern IntPtr CreateRoundRectRgn(int l, int t, int r, int b, int we, int he);

        // ── Exportar PDF ──────────────────────────────────────────────────
        private void ExportarPdfDashboard(object sender, EventArgs e)
        {
            if (_data == null) { MessageBox.Show("Actualiza primero.","Sin datos",MessageBoxButtons.OK,MessageBoxIcon.Information); return; }
            try {
                var snap = new DashboardSnapshot {
                    FechaGeneracion = DateTime.Now, Usuario = Sesion.NombreCompleto,
                    VentasMes     = decimal.TryParse(lblVentasMes.Text.Replace("$","").Replace(",",""), out var vm) ? vm : 0,
                    VentasHoy     = decimal.TryParse(lblVentasHoy.Text.Replace("$","").Replace(",",""), out var vh) ? vh : 0,
                    FacturasPend  = int.TryParse(lblFactPend.Text, out var fp) ? fp : 0,
                    StockBajo     = int.TryParse(lblStockBajo.Text, out var sb) ? sb : 0,
                    TotalClientes = int.TryParse(lblClientes.Text, out var tc) ? tc : 0,
                    TopProducto   = lblTopProd.Text,
                    Saldadas      = _data.Saldadas, Pendientes = _data.Pendientes, Canceladas = _data.Canceladas,
                    VentasPorMes  = _data.VentasPorMes, TopStock = _data.TopStock
                };
                var bytes = PdfService.GenerarDashboardEjecutivo(snap);
                using var dlg = new SaveFileDialog { Filter="PDF|*.pdf", FileName=$"Dashboard_{DateTime.Now:yyyyMMdd_HHmm}.pdf" };
                if (dlg.ShowDialog() == DialogResult.OK) {
                    System.IO.File.WriteAllBytes(dlg.FileName, bytes);
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo { FileName=dlg.FileName, UseShellExecute=true });
                }
            } catch (Exception ex) { MessageBox.Show("Error: "+ex.Message,"Error",MessageBoxButtons.OK,MessageBoxIcon.Error); }
        }
    }

    // ── Modelo de datos interno del Dashboard ─────────────────────────────
    class DashData
    {
        public List<(string Mes, decimal Total)>                  VentasPorMes  { get; set; }
        public List<decimal>                                       VentasDiarias { get; set; }
        public List<(string Nombre, decimal Stock, decimal Min)>  TopStock      { get; set; }
        public int Saldadas   { get; set; }
        public int Pendientes { get; set; }
        public int Canceladas { get; set; }
    }
}
