using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Windows.Forms;

namespace SistemaGestion
{
    public class FrmFlujoCaja : Form
    {
        // ── Controles ─────────────────────────────────────────────────────
        private NumericUpDown  numSaldo;
        private DataGridView   dgv;
        private Panel          pnlGrafica;
        private Label          lblRes30, lblRes60, lblRes90;
        private CheckBox       chkIngresos, chkEgresos, chkNomina;
        private ComboBox       cmbFiltro;

        // ── Datos ─────────────────────────────────────────────────────────
        private List<MovimientoCaja>  _todos   = new();
        private List<MovimientoCaja>  _filtros = new();
        private ResumenFlujoCaja      _resumen;

        // ── Movimientos manuales adicionales ──────────────────────────────
        private List<MovimientoCaja> _manuales = new();

        public FrmFlujoCaja()
        {
            Text          = "Flujo de Caja — Proyeccion 30 / 60 / 90 dias";
            Size          = new Size(1280, 720);
            MinimumSize   = new Size(1000, 580);
            StartPosition = FormStartPosition.CenterParent;
            BackColor     = Color.FromArgb(245, 248, 252);

            BuildUI();
            Calcular();
        }

        // ══════════════════════════════════════════════════════════════════
        //  UI
        // ══════════════════════════════════════════════════════════════════
        private void BuildUI()
        {
            // ── Header ────────────────────────────────────────────────────
            var hdr = new Panel { Dock = DockStyle.Top, Height = 50,
                BackColor = Color.FromArgb(20, 70, 130) };
            hdr.Controls.Add(new Label
            {
                Text = "Flujo de Caja — Proyeccion 30 / 60 / 90 dias",
                ForeColor = Color.White,
                Font      = new Font("Segoe UI", 12f, FontStyle.Bold),
                AutoSize  = true, Location = new Point(14, 13)
            });
            var btnPdf = Btn("Exportar PDF", Color.FromArgb(180, 60, 20), new Point(1060, 9), 110);
            btnPdf.Click += (s, e) => ExportarPdf();
            var btnCerrar = Btn("Cerrar", Color.FromArgb(100, 100, 120), new Point(1180, 9));
            btnCerrar.Click += (s, e) => Close();
            hdr.Controls.AddRange(new Control[] { btnPdf, btnCerrar });

            // ── Barra de parametros ───────────────────────────────────────
            var pnlParam = new Panel { Dock = DockStyle.Top, Height = 48,
                BackColor = Color.FromArgb(225, 235, 252) };

            pnlParam.Controls.Add(new Label
            {
                Text = "Saldo inicial (efectivo hoy USD):",
                Location = new Point(10, 15), AutoSize = true,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold)
            });
            numSaldo = new NumericUpDown
            {
                Location = new Point(230, 12), Size = new Size(130, 26),
                Maximum  = 99999999, DecimalPlaces = 2, Value = 0
            };
            pnlParam.Controls.Add(numSaldo);

            var btnCalc = new Button
            {
                Text = "Recalcular", Location = new Point(375, 11), Size = new Size(100, 28),
                BackColor = Color.FromArgb(20, 70, 130), ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 9f, FontStyle.Bold)
            };
            btnCalc.FlatAppearance.BorderSize = 0;
            btnCalc.Click += (s, e) => Calcular();
            pnlParam.Controls.Add(btnCalc);

            // Filtros
            pnlParam.Controls.Add(new Label
                { Text = "Mostrar:", Location = new Point(500, 15), AutoSize = true,
                  Font = new Font("Segoe UI", 9f) });
            chkIngresos = new CheckBox { Text = "Ingresos", Location = new Point(558, 13),
                AutoSize = true, Checked = true, Font = new Font("Segoe UI", 9f) };
            chkEgresos  = new CheckBox { Text = "Egresos",  Location = new Point(640, 13),
                AutoSize = true, Checked = true, Font = new Font("Segoe UI", 9f) };
            chkNomina   = new CheckBox { Text = "Nomina",   Location = new Point(718, 13),
                AutoSize = true, Checked = true, Font = new Font("Segoe UI", 9f) };
            pnlParam.Controls.AddRange(new Control[] { chkIngresos, chkEgresos, chkNomina });

            pnlParam.Controls.Add(new Label
                { Text = "Tramo:", Location = new Point(800, 15), AutoSize = true,
                  Font = new Font("Segoe UI", 9f) });
            cmbFiltro = new ComboBox { Location = new Point(848, 11), Size = new Size(110, 26),
                DropDownStyle = ComboBoxStyle.DropDownList };
            cmbFiltro.Items.AddRange(new object[] { "Todos (90d)", "30 dias", "60 dias", "90 dias" });
            cmbFiltro.SelectedIndex = 0;
            cmbFiltro.SelectedIndexChanged += (s, e) => AplicarFiltros();
            pnlParam.Controls.Add(cmbFiltro);

            var btnManual = Btn("+ Manual", Color.FromArgb(80, 130, 40), new Point(975, 11), 90);
            btnManual.Click += (s, e) => AgregarManual();
            pnlParam.Controls.Add(btnManual);

            foreach (var chk in new[] { chkIngresos, chkEgresos, chkNomina })
                chk.CheckedChanged += (s, e) => AplicarFiltros();

            // ── KPIs de 3 tramos ──────────────────────────────────────────
            var pnlKpis = new Panel { Dock = DockStyle.Top, Height = 90,
                BackColor = Color.FromArgb(235, 240, 252) };
            lblRes30 = KpiTriple(pnlKpis, "0 — 30 dias",  Color.FromArgb(20, 100, 60),  0);
            lblRes60 = KpiTriple(pnlKpis, "31 — 60 dias", Color.FromArgb(20, 70, 130),  1);
            lblRes90 = KpiTriple(pnlKpis, "61 — 90 dias", Color.FromArgb(100, 50, 160), 2);

            // ── Layout principal ──────────────────────────────────────────
            var pnlMain = new Panel { Dock = DockStyle.Fill };

            // Grafica a la derecha
            pnlGrafica = new Panel { Dock = DockStyle.Right, Width = 360,
                BackColor = Color.White };
            pnlGrafica.Paint += PaintGrafica;
            var sep = new Panel { Dock = DockStyle.Right, Width = 3,
                BackColor = Color.FromArgb(200, 210, 230) };

            // Grid principal
            dgv = new DataGridView
            {
                Dock = DockStyle.Fill, ReadOnly = true,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false, AllowUserToAddRows = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                BackgroundColor = Color.White, BorderStyle = BorderStyle.None,
                RowHeadersVisible = false, Font = new Font("Segoe UI", 9f),
                RowTemplate = { Height = 24 }
            };
            dgv.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(20, 70, 130);
            dgv.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
            dgv.ColumnHeadersDefaultCellStyle.Font      = new Font("Segoe UI", 9f, FontStyle.Bold);
            dgv.EnableHeadersVisualStyles = false;
            dgv.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(238, 244, 255);
            dgv.DataBindingComplete += (s, e) => ColorearFilas();

            pnlMain.Controls.Add(dgv);
            pnlMain.Controls.Add(sep);
            pnlMain.Controls.Add(pnlGrafica);

            // ── Footer ────────────────────────────────────────────────────
            var footer = new Panel { Dock = DockStyle.Bottom, Height = 32,
                BackColor = Color.FromArgb(218, 228, 248) };
            footer.Controls.Add(new Label
            {
                Text = "  Los montos proyectados se basan en fechas de vencimiento de facturas, compras," +
                       " cuentas por cobrar y nomina del ultimo periodo. Ajusta el saldo inicial manualmente.",
                AutoSize = true, Location = new Point(10, 8),
                Font = new Font("Segoe UI", 8f, FontStyle.Italic),
                ForeColor = Color.FromArgb(60, 80, 130)
            });

            Controls.AddRange(new Control[] { pnlMain, footer, pnlKpis, pnlParam, hdr });
        }

        // ══════════════════════════════════════════════════════════════════
        //  CALCULAR
        // ══════════════════════════════════════════════════════════════════
        private void Calcular()
        {
            var (movimientos, resumen) = DatabaseService.ObtenerFlujoCaja(numSaldo.Value);

            // Agregar manuales
            _todos   = movimientos.Concat(_manuales).OrderBy(m => m.Fecha).ToList();
            _resumen = RecalcularResumen(_todos, numSaldo.Value);

            // Recalcular resumen incluyendo manuales
            AplicarFiltros();
            ActualizarKpis();
        }

        private ResumenFlujoCaja RecalcularResumen(List<MovimientoCaja> mov, decimal saldoIni)
        {
            var hoy  = DateTime.Today;
            var r    = new ResumenFlujoCaja { SaldoInicial = saldoIni };
            var lim30 = hoy.AddDays(30);
            var lim60 = hoy.AddDays(60);

            foreach (var m in mov)
            {
                bool en30 = m.Fecha <= lim30;
                bool en60 = m.Fecha > lim30 && m.Fecha <= lim60;
                bool en90 = m.Fecha > lim60;

                if (m.Tipo == "Ingreso")
                {
                    if (en30) r.IngreSos30 += m.MontoUSD;
                    else if (en60) r.IngreSos60 += m.MontoUSD;
                    else if (en90) r.IngreSos90 += m.MontoUSD;
                }
                else
                {
                    if (en30) r.Egresos30 += m.MontoUSD;
                    else if (en60) r.Egresos60 += m.MontoUSD;
                    else if (en90) r.Egresos90 += m.MontoUSD;
                }
            }
            return r;
        }

        private void AplicarFiltros()
        {
            var hoy   = DateTime.Today;
            int tramo = cmbFiltro.SelectedIndex;

            _filtros = _todos.Where(m =>
            {
                if (!chkIngresos.Checked && m.Tipo == "Ingreso") return false;
                if (!chkEgresos.Checked  && m.Tipo == "Egreso"
                    && m.Categoria != "Nomina") return false;
                if (!chkNomina.Checked   && m.Categoria == "Nomina") return false;
                int dias = (int)(m.Fecha - hoy).TotalDays;
                return tramo switch
                {
                    1 => dias <= 30,
                    2 => dias <= 60,
                    3 => dias <= 90,
                    _ => dias <= 90
                };
            }).ToList();

            BindGrid();
            pnlGrafica.Invalidate();
        }

        private void BindGrid()
        {
            var hoy = DateTime.Today;
            dgv.DataSource = null;
            dgv.DataSource = _filtros.Select(m => new
            {
                Fecha     = m.Fecha.ToString("dd/MM/yyyy"),
                Dias      = ((int)(m.Fecha - hoy).TotalDays) + "d",
                m.Tipo,
                m.Categoria,
                m.Concepto,
                Ingreso   = m.Tipo == "Ingreso" ? m.MontoUSD.ToString("C2") : "",
                Egreso    = m.Tipo == "Egreso"  ? m.MontoUSD.ToString("C2") : "",
                Origen    = m.EsReal ? "Real" : "Proyectado"
            }).ToList();
        }

        private void ActualizarKpis()
        {
            if (_resumen == null) return;

            void SetKpi(Label lbl, decimal ingreso, decimal egreso, decimal saldo)
            {
                bool positivo = saldo >= 0;
                lbl.Text =
                    "Ingresos: $" + ingreso.ToString("N0") + "\n" +
                    "Egresos:  $" + egreso.ToString("N0") + "\n" +
                    "Neto: $" + (ingreso - egreso).ToString("N0") + "\n" +
                    "Saldo acum: $" + saldo.ToString("N0");
                lbl.ForeColor = positivo ? Color.White : Color.FromArgb(255, 200, 180);
            }

            SetKpi(lblRes30, _resumen.IngreSos30, _resumen.Egresos30, _resumen.SaldoProyectado30);
            SetKpi(lblRes60, _resumen.IngreSos60, _resumen.Egresos60, _resumen.SaldoProyectado60);
            SetKpi(lblRes90, _resumen.IngreSos90, _resumen.Egresos90, _resumen.SaldoProyectado90);
        }

        private void ColorearFilas()
        {
            foreach (DataGridViewRow row in dgv.Rows)
            {
                var tipo = row.Cells["Tipo"].Value?.ToString();
                var cat  = row.Cells["Categoria"].Value?.ToString();
                row.DefaultCellStyle.BackColor = (tipo, cat) switch
                {
                    ("Ingreso", _)       => Color.FromArgb(228, 255, 235),
                    (_, "Nomina")        => Color.FromArgb(255, 248, 225),
                    ("Egreso", "Compra") => Color.FromArgb(255, 238, 238),
                    ("Egreso", _)        => Color.FromArgb(255, 244, 235),
                    _                    => Color.White
                };
            }
        }

        // ══════════════════════════════════════════════════════════════════
        //  GRAFICA DE SALDO ACUMULADO (linea + area)
        // ══════════════════════════════════════════════════════════════════
        private void PaintGrafica(object sender, PaintEventArgs e)
        {
            var g   = e.Graphics;
            var pnl = (Panel)sender;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            // Titulo
            g.FillRectangle(new SolidBrush(Color.FromArgb(20, 70, 130)),
                0, 0, pnl.Width, 28);
            using var fntT = new Font("Segoe UI", 8.5f, FontStyle.Bold);
            g.DrawString("Saldo acumulado proyectado", fntT, Brushes.White, 10, 7);

            if (_resumen == null) return;

            // Puntos: Hoy, +30d, +60d, +90d
            var hoy = DateTime.Today;
            var puntos = new[]
            {
                ("Hoy",  _resumen.SaldoInicial),
                ("+30d", _resumen.SaldoProyectado30),
                ("+60d", _resumen.SaldoProyectado60),
                ("+90d", _resumen.SaldoProyectado90)
            };

            int padL = 70, padR = 20, padT = 40, padB = 60;
            int w    = pnl.Width  - padL - padR;
            int h    = (pnl.Height / 2) - padT - padB;

            decimal maxVal = puntos.Max(p => Math.Abs(p.Item2));
            if (maxVal == 0) maxVal = 1;
            decimal minVal = puntos.Min(p => p.Item2);
            decimal range  = Math.Max(maxVal, Math.Abs(minVal)) * 1.1m;

            float CentroY() => padT + h / 2f;
            float ToY(decimal v) => CentroY() - (float)(h / 2.0 * (double)v / (double)range);
            float ToX(int i)     => padL + (float)w * i / 3;

            using var fntS = new Font("Segoe UI", 7.5f);
            using var brGr = new SolidBrush(Color.FromArgb(130, 140, 165));

            // Linea de cero
            using var penCero = new Pen(Color.FromArgb(180, 185, 210), 1f) { DashStyle = DashStyle.Dash };
            g.DrawLine(penCero, padL, (int)CentroY(), padL + w, (int)CentroY());
            g.DrawString("$0", fntS, brGr, 2, CentroY() - 8);

            // Grid horizontal (positivo/negativo)
            foreach (var factor in new[] { 0.5m, 1.0m })
            {
                float yP = ToY(range * factor);
                float yN = ToY(-range * factor);
                using var penG = new Pen(Color.FromArgb(225, 228, 242), 1f);
                g.DrawLine(penG, padL, (int)yP, padL + w, (int)yP);
                g.DrawLine(penG, padL, (int)yN, padL + w, (int)yN);
                decimal val = range * factor;
                string lbl  = val >= 1000 ? "$" + (val / 1000).ToString("N0") + "k" : "$" + val.ToString("N0");
                g.DrawString(lbl,  fntS, brGr, 2, yP - 8);
                g.DrawString("-" + lbl.Substring(1), fntS, brGr, 2, yN - 8);
            }

            // Area bajo la curva (relleno verde/rojo segun signo)
            var ptsArea = new PointF[puntos.Length + 2];
            ptsArea[0] = new PointF(ToX(0), CentroY());
            for (int i = 0; i < puntos.Length; i++)
                ptsArea[i + 1] = new PointF(ToX(i), ToY(puntos[i].Item2));
            ptsArea[puntos.Length + 1] = new PointF(ToX(3), CentroY());

            decimal saldoFinal = puntos[3].Item2;
            Color areaColor    = saldoFinal >= 0
                ? Color.FromArgb(50, 30, 170, 90)
                : Color.FromArgb(50, 200, 60, 60);
            g.FillPolygon(new SolidBrush(areaColor), ptsArea);

            // Linea principal
            var lineaPts = new PointF[puntos.Length];
            for (int i = 0; i < puntos.Length; i++)
                lineaPts[i] = new PointF(ToX(i), ToY(puntos[i].Item2));

            Color lineaColor = saldoFinal >= 0
                ? Color.FromArgb(20, 140, 70)
                : Color.FromArgb(180, 40, 40);
            using var penLine = new Pen(lineaColor, 2.5f);
            g.DrawLines(penLine, lineaPts);

            // Puntos y etiquetas
            for (int i = 0; i < puntos.Length; i++)
            {
                float x = ToX(i);
                float y = ToY(puntos[i].Item2);

                bool positivo = puntos[i].Item2 >= 0;
                Color ptColor = positivo ? Color.FromArgb(20, 140, 70) : Color.FromArgb(180, 40, 40);

                g.FillEllipse(new SolidBrush(ptColor), x - 5, y - 5, 10, 10);

                string valStr = puntos[i].Item2 >= 1000 || puntos[i].Item2 <= -1000
                    ? "$" + (puntos[i].Item2 / 1000).ToString("N1") + "k"
                    : "$" + puntos[i].Item2.ToString("N0");
                g.DrawString(valStr, fntS, new SolidBrush(ptColor), x - 20, y - 18);

                g.DrawString(puntos[i].Item1, fntS, brGr,
                    new RectangleF(x - 18, padT + h + 6, 36, 14),
                    new StringFormat { Alignment = StringAlignment.Center });
            }

            // ── Segunda grafica: barras de ingreso/egreso por tramo ───────
            int g2Y = pnl.Height / 2 + 10;
            g.FillRectangle(new SolidBrush(Color.FromArgb(20, 70, 130)),
                0, g2Y - 2, pnl.Width, 26);
            g.DrawString("Ingresos vs egresos por tramo", fntT, Brushes.White, 10, g2Y + 4);

            var tramos = new[]
            {
                ("0-30d",  _resumen.IngreSos30, _resumen.Egresos30),
                ("31-60d", _resumen.IngreSos60, _resumen.Egresos60),
                ("61-90d", _resumen.IngreSos90, _resumen.Egresos90)
            };

            decimal maxBar = Math.Max(1,
                tramos.Max(t => Math.Max(t.Item2, t.Item3)));

            int g2T    = g2Y + 28;
            int g2H    = pnl.Height - g2T - 35;
            int tramW  = (w - 20) / 3;
            int barW   = tramW / 3;

            for (int i = 0; i < tramos.Length; i++)
            {
                var (etq, ing, egr) = tramos[i];
                float baseX = padL + 10 + i * (tramW + 5);

                // Barra de ingreso
                float hIng  = Math.Max(2, (float)(g2H * (double)ing / (double)maxBar));
                g.FillRectangle(new SolidBrush(Color.FromArgb(30, 150, 75)),
                    baseX, g2T + g2H - hIng, barW - 2, hIng);

                // Barra de egreso
                float hEgr = Math.Max(2, (float)(g2H * (double)egr / (double)maxBar));
                g.FillRectangle(new SolidBrush(Color.FromArgb(200, 60, 60)),
                    baseX + barW, g2T + g2H - hEgr, barW - 2, hEgr);

                // Valores
                if (ing > 0)
                    g.DrawString(ing >= 1000 ? (ing/1000).ToString("N0")+"k" : ing.ToString("N0"),
                        fntS, Brushes.DarkGreen, baseX, g2T + g2H - hIng - 13);
                if (egr > 0)
                    g.DrawString(egr >= 1000 ? (egr/1000).ToString("N0")+"k" : egr.ToString("N0"),
                        fntS, Brushes.DarkRed, baseX + barW, g2T + g2H - hEgr - 13);

                // Etiqueta tramo
                g.DrawString(etq, fntS, brGr,
                    new RectangleF(baseX, g2T + g2H + 3, tramW, 14),
                    new StringFormat { Alignment = StringAlignment.Center });
            }

            // Leyenda
            g.FillRectangle(new SolidBrush(Color.FromArgb(30, 150, 75)),
                padL + 10, pnl.Height - 18, 10, 10);
            g.DrawString("Ingresos", fntS, brGr, padL + 24, pnl.Height - 20);
            g.FillRectangle(new SolidBrush(Color.FromArgb(200, 60, 60)),
                padL + 100, pnl.Height - 18, 10, 10);
            g.DrawString("Egresos", fntS, brGr, padL + 114, pnl.Height - 20);
        }

        // ══════════════════════════════════════════════════════════════════
        //  MOVIMIENTO MANUAL
        // ══════════════════════════════════════════════════════════════════
        private void AgregarManual()
        {
            using var dlg = new DlgMovimientoCaja();
            if (dlg.ShowDialog() != DialogResult.OK) return;
            _manuales.Add(dlg.Resultado);
            Calcular();
        }

        // ══════════════════════════════════════════════════════════════════
        //  PDF
        // ══════════════════════════════════════════════════════════════════
        private void ExportarPdf()
        {
            if (_resumen == null) return;
            try
            {
                var pdf = PdfService.GenerarFlujoCaja(_todos, _resumen);
                PdfService.GuardarYAbrir(pdf,
                    "FlujoCaja_" + DateTime.Today.ToString("yyyy-MM-dd") + ".pdf");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al generar PDF:\n" + ex.Message, "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // ══════════════════════════════════════════════════════════════════
        //  HELPERS UI
        // ══════════════════════════════════════════════════════════════════
        private Label KpiTriple(Panel parent, string titulo, Color color, int idx)
        {
            int w = (parent.Width - 12) / 3;
            var card = new Panel
            {
                Location  = new Point(6 + idx * (w + 3), 4),
                Size      = new Size(w, 82), BackColor = color
            };
            card.Controls.Add(new Label
            {
                Text = titulo, Font = new Font("Segoe UI", 8f, FontStyle.Bold),
                ForeColor = Color.FromArgb(200, 225, 255),
                Location = new Point(8, 5), Size = new Size(w - 16, 16)
            });
            var lbl = new Label
            {
                Text = "—", Font = new Font("Segoe UI", 8f),
                ForeColor = Color.White,
                Location = new Point(8, 22), Size = new Size(w - 14, 55),
                AutoSize = false
            };
            card.Controls.Add(lbl);
            parent.Controls.Add(card);
            parent.Resize += (s, e) =>
            {
                int cw = (parent.Width - 12) / 3;
                card.SetBounds(6 + idx * (cw + 3), 4, cw, 82);
                lbl.Width = cw - 14;
            };
            return lbl;
        }

        private Button Btn(string t, Color c, Point p, int w = 90)
        {
            var b = new Button { Text = t, Size = new Size(w, 32), Location = p,
                BackColor = c, ForeColor = Color.White, FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 8.5f, FontStyle.Bold), Cursor = Cursors.Hand };
            b.FlatAppearance.BorderSize = 0; return b;
        }
    }

    // ══════════════════════════════════════════════════════════════════════════
    //  DIALOGO: AGREGAR MOVIMIENTO MANUAL
    // ══════════════════════════════════════════════════════════════════════════
    public class DlgMovimientoCaja : Form
    {
        public MovimientoCaja Resultado { get; private set; }

        public DlgMovimientoCaja()
        {
            Text = "Agregar movimiento manual";
            Size = new Size(400, 300); StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog; MaximizeBox = MinimizeBox = false;
            BackColor = Color.FromArgb(245, 248, 252);

            var banner = new Panel { Dock = DockStyle.Top, Height = 38,
                BackColor = Color.FromArgb(20, 70, 130) };
            banner.Controls.Add(new Label { Text = Text, ForeColor = Color.White,
                Font = new Font("Segoe UI", 10f, FontStyle.Bold), AutoSize = true,
                Location = new Point(12, 9) });
            Controls.Add(banner);

            int y = 52;
            void L(string t) => Controls.Add(new Label { Text = t,
                Location = new Point(16, y + 3), AutoSize = true,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold) });

            L("Tipo:");
            var cmbTipo = new ComboBox { Location = new Point(155, y), Size = new Size(210, 26),
                DropDownStyle = ComboBoxStyle.DropDownList };
            cmbTipo.Items.AddRange(new object[] { "Ingreso", "Egreso" });
            cmbTipo.SelectedIndex = 0;
            Controls.Add(cmbTipo); y += 34;

            L("Fecha:");
            var dtp = new DateTimePicker { Location = new Point(155, y), Size = new Size(210, 26),
                Format = DateTimePickerFormat.Short, Value = DateTime.Today.AddDays(7) };
            Controls.Add(dtp); y += 34;

            L("Concepto:");
            var txtConcepto = new TextBox { Location = new Point(155, y), Size = new Size(210, 24) };
            Controls.Add(txtConcepto); y += 34;

            L("Monto USD:");
            var numMonto = new NumericUpDown { Location = new Point(155, y), Size = new Size(210, 26),
                Maximum = 9999999, DecimalPlaces = 2 };
            Controls.Add(numMonto); y += 44;

            var bOk = new Button { Text = "Agregar", Location = new Point(80, y),
                Size = new Size(120, 33), BackColor = Color.FromArgb(20, 70, 130),
                ForeColor = Color.White, FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold) };
            bOk.FlatAppearance.BorderSize = 0;
            var bCan = new Button { Text = "Cancelar", DialogResult = DialogResult.Cancel,
                Location = new Point(218, y), Size = new Size(100, 33),
                BackColor = Color.FromArgb(120, 120, 130), ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat };
            bCan.FlatAppearance.BorderSize = 0;

            bOk.Click += (s, e) =>
            {
                if (numMonto.Value <= 0)
                { MessageBox.Show("Ingresa un monto.", "Validacion", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
                Resultado = new MovimientoCaja
                {
                    Fecha     = dtp.Value,
                    Tipo      = cmbTipo.SelectedItem?.ToString() ?? "Egreso",
                    Categoria = "Manual",
                    Concepto  = string.IsNullOrWhiteSpace(txtConcepto.Text)
                                ? "Movimiento manual" : txtConcepto.Text.Trim(),
                    MontoUSD  = numMonto.Value,
                    EsReal    = false
                };
                DialogResult = DialogResult.OK;
            };
            Controls.AddRange(new Control[] { bOk, bCan });
        }
    }
}
