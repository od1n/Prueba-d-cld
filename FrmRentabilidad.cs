using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Windows.Forms;

namespace SistemaGestion
{
    public class FrmRentabilidad : Form
    {
        // ── Filtros ───────────────────────────────────────────────────────
        private DateTimePicker dtpDesde, dtpHasta;
        private ComboBox       cmbDepto, cmbOrden;
        private TextBox        txtBuscar;
        private CheckBox       chkSoloConVentas;

        // ── Grid principal ────────────────────────────────────────────────
        private DataGridView dgv;

        // ── KPIs ──────────────────────────────────────────────────────────
        private Label lblTotalIngreso, lblTotalUtilidad, lblMargenProm;
        private Label lblTopProducto, lblMejorMargen, lblSinMovimiento;

        // ── Grafica de barras horizontales (top 10) ───────────────────────
        private Panel pnlGrafica;

        // ── Data ──────────────────────────────────────────────────────────
        private List<RentabilidadProducto> _datos = new();
        private List<RentabilidadProducto> _filtrados = new();

        public FrmRentabilidad()
        {
            Text          = "Reporte de Rentabilidad por Producto";
            Size          = new Size(1280, 760);
            MinimumSize   = new Size(1000, 600);
            StartPosition = FormStartPosition.CenterParent;
            BackColor     = Color.FromArgb(245, 247, 252);

            BuildUI();
            CargarDatos();
        }

        // ══════════════════════════════════════════════════════════════════
        //  CONSTRUCCION DE LA UI
        // ══════════════════════════════════════════════════════════════════
        private void BuildUI()
        {
            // ── Header ────────────────────────────────────────────────────
            var hdr = new Panel
            {
                Dock      = DockStyle.Top,
                Height    = 50,
                BackColor = Color.FromArgb(30, 100, 60)
            };
            hdr.Controls.Add(new Label
            {
                Text      = "Rentabilidad por Producto",
                Font      = new Font("Segoe UI", 13f, FontStyle.Bold),
                ForeColor = Color.White,
                AutoSize  = true,
                Location  = new Point(14, 13)
            });

            var btnPdf = new Button
            {
                Text      = "Exportar PDF",
                Size      = new Size(110, 30),
                Location  = new Point(1020, 10),
                BackColor = Color.FromArgb(180, 60, 30),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font      = new Font("Segoe UI", 8.5f, FontStyle.Bold),
                Cursor    = Cursors.Hand
            };
            btnPdf.FlatAppearance.BorderSize = 0;
            btnPdf.Click += (s, e) => ExportarPdf();

            var btnCerrar = new Button
            {
                Text      = "Cerrar",
                Size      = new Size(80, 30),
                Location  = new Point(1140, 10),
                BackColor = Color.FromArgb(100, 100, 120),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font      = new Font("Segoe UI", 8.5f, FontStyle.Bold)
            };
            btnCerrar.FlatAppearance.BorderSize = 0;
            btnCerrar.Click += (s, e) => Close();
            hdr.Controls.AddRange(new Control[] { btnPdf, btnCerrar });

            // ── Panel de filtros ──────────────────────────────────────────
            var pnlFiltros = new Panel
            {
                Dock      = DockStyle.Top,
                Height    = 48,
                BackColor = Color.FromArgb(228, 242, 232),
                Padding   = new Padding(8, 8, 8, 6)
            };

            int fx = 10;
            pnlFiltros.Controls.Add(Lbl("Desde:", fx, 14)); fx += 50;
            dtpDesde = new DateTimePicker
            {
                Location = new Point(fx, 10), Size = new Size(110, 26),
                Format   = DateTimePickerFormat.Short,
                Value    = DateTime.Today.AddMonths(-3)
            };
            pnlFiltros.Controls.Add(dtpDesde); fx += 120;

            pnlFiltros.Controls.Add(Lbl("Hasta:", fx, 14)); fx += 48;
            dtpHasta = new DateTimePicker
            {
                Location = new Point(fx, 10), Size = new Size(110, 26),
                Format   = DateTimePickerFormat.Short,
                Value    = DateTime.Today
            };
            pnlFiltros.Controls.Add(dtpHasta); fx += 120;

            pnlFiltros.Controls.Add(Lbl("Depto:", fx, 14)); fx += 48;
            cmbDepto = new ComboBox
            {
                Location      = new Point(fx, 10), Size = new Size(130, 26),
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            cmbDepto.Items.Add("Todos");
            cmbDepto.SelectedIndex = 0;
            pnlFiltros.Controls.Add(cmbDepto); fx += 140;

            pnlFiltros.Controls.Add(Lbl("Orden:", fx, 14)); fx += 48;
            cmbOrden = new ComboBox
            {
                Location      = new Point(fx, 10), Size = new Size(160, 26),
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            cmbOrden.Items.AddRange(new object[]
            {
                "Utilidad bruta (desc)",
                "Margen % (desc)",
                "Ingresos (desc)",
                "Unidades vendidas (desc)",
                "Descripcion (asc)"
            });
            cmbOrden.SelectedIndex = 0;
            pnlFiltros.Controls.Add(cmbOrden); fx += 170;

            pnlFiltros.Controls.Add(Lbl("Buscar:", fx, 14)); fx += 55;
            txtBuscar = new TextBox { Location = new Point(fx, 10), Size = new Size(160, 26) };
            pnlFiltros.Controls.Add(txtBuscar); fx += 170;

            chkSoloConVentas = new CheckBox
            {
                Text     = "Solo con ventas",
                Location = new Point(fx, 13),
                AutoSize = true,
                Font     = new Font("Segoe UI", 9f)
            };
            pnlFiltros.Controls.Add(chkSoloConVentas); fx += 130;

            var btnFiltrar = new Button
            {
                Text      = "Filtrar",
                Location  = new Point(fx, 9), Size = new Size(80, 28),
                BackColor = Color.FromArgb(30, 100, 60),
                ForeColor = Color.White, FlatStyle = FlatStyle.Flat,
                Font      = new Font("Segoe UI", 9f, FontStyle.Bold)
            };
            btnFiltrar.FlatAppearance.BorderSize = 0;
            btnFiltrar.Click += (s, e) => CargarDatos();
            pnlFiltros.Controls.Add(btnFiltrar);

            txtBuscar.TextChanged += (s, e) => AplicarFiltros();
            cmbOrden.SelectedIndexChanged += (s, e) => AplicarFiltros();
            cmbDepto.SelectedIndexChanged += (s, e) => AplicarFiltros();
            chkSoloConVentas.CheckedChanged += (s, e) => AplicarFiltros();

            // ── Panel KPIs ────────────────────────────────────────────────
            var pnlKpis = new Panel
            {
                Dock      = DockStyle.Top,
                Height    = 70,
                BackColor = Color.FromArgb(240, 248, 242)
            };

            int kx = 10;
            lblTotalIngreso  = KpiCard(pnlKpis, "Ingresos totales",  Color.FromArgb(30, 100, 60),  kx); kx += 196;
            lblTotalUtilidad = KpiCard(pnlKpis, "Utilidad bruta",    Color.FromArgb(22, 55, 120),  kx); kx += 196;
            lblMargenProm    = KpiCard(pnlKpis, "Margen promedio",   Color.FromArgb(100, 60, 160), kx); kx += 196;
            lblTopProducto   = KpiCard(pnlKpis, "Top producto",      Color.FromArgb(160, 100, 20), kx); kx += 196;
            lblMejorMargen   = KpiCard(pnlKpis, "Mejor margen",      Color.FromArgb(20, 110, 130), kx); kx += 196;
            lblSinMovimiento = KpiCard(pnlKpis, "Sin movimiento",    Color.FromArgb(160, 50, 50),  kx);

            // ── Contenedor principal fill ─────────────────────────────────
            var pnlMain = new Panel { Dock = DockStyle.Fill, BackColor = Color.FromArgb(225, 230, 240) };

            // Panel grafica a la derecha (ancho fijo 340)
            pnlGrafica = new Panel
            {
                Dock      = DockStyle.Right,
                Width     = 340,
                BackColor = Color.White
            };
            pnlGrafica.Paint += PaintGrafica;

            // Separador visual
            var sep = new Panel { Dock = DockStyle.Right, Width = 4, BackColor = Color.FromArgb(210, 215, 228) };

            // Grid ocupa el resto
            dgv = new DataGridView
            {
                Dock                = DockStyle.Fill,
                ReadOnly            = true,
                SelectionMode       = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect         = false,
                AllowUserToAddRows  = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                BackgroundColor     = Color.White,
                BorderStyle         = BorderStyle.None,
                RowHeadersVisible   = false,
                Font                = new Font("Segoe UI", 8.5f),
                RowTemplate         = { Height = 22 }
            };
            dgv.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(30, 100, 60);
            dgv.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
            dgv.ColumnHeadersDefaultCellStyle.Font      = new Font("Segoe UI", 8.5f, FontStyle.Bold);
            dgv.EnableHeadersVisualStyles   = false;
            dgv.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(238, 248, 242);
            dgv.DataBindingComplete += (s, e) => ColorearFilas();
            dgv.SelectionChanged    += (s, e) => pnlGrafica.Invalidate();

            pnlMain.Controls.Add(dgv);
            pnlMain.Controls.Add(sep);
            pnlMain.Controls.Add(pnlGrafica);

            // ── Footer ────────────────────────────────────────────────────
            var footer = new Panel
            {
                Dock      = DockStyle.Bottom,
                Height    = 30,
                BackColor = Color.FromArgb(215, 235, 220)
            };
            var lblFooter = new Label
            {
                AutoSize  = true, Location = new Point(12, 8),
                Font      = new Font("Segoe UI", 8f),
                ForeColor = Color.FromArgb(60, 100, 70)
            };
            footer.Controls.Add(lblFooter);
            dgv.DataBindingComplete += (s, e) =>
                lblFooter.Text = "  Productos: " + _filtrados.Count +
                    "   |   Click en una fila para ver detalle en la grafica";

            Controls.AddRange(new Control[] { pnlMain, footer, pnlKpis, pnlFiltros, hdr });
        }

        // ══════════════════════════════════════════════════════════════════
        //  CARGA Y FILTRADO
        // ══════════════════════════════════════════════════════════════════
        private void CargarDatos()
        {
            _datos = DatabaseService.ObtenerRentabilidad(dtpDesde.Value, dtpHasta.Value);

            // Poblar combo de departamentos
            var deptos = _datos.Select(d => d.Departamento)
                               .Where(d => !string.IsNullOrWhiteSpace(d))
                               .Distinct().OrderBy(d => d).ToList();
            cmbDepto.Items.Clear();
            cmbDepto.Items.Add("Todos");
            foreach (var dep in deptos) cmbDepto.Items.Add(dep);
            if (cmbDepto.SelectedIndex < 0) cmbDepto.SelectedIndex = 0;

            AplicarFiltros();
        }

        private void AplicarFiltros()
        {
            string buscar = txtBuscar.Text.ToLower();
            string depto  = cmbDepto.SelectedItem?.ToString() ?? "Todos";

            _filtrados = _datos
                .Where(r =>
                    (depto == "Todos" || r.Departamento == depto) &&
                    (!chkSoloConVentas.Checked || r.UnidadesVendidas > 0) &&
                    (string.IsNullOrEmpty(buscar) ||
                     r.Codigo.ToLower().Contains(buscar) ||
                     r.Descripcion.ToLower().Contains(buscar)))
                .ToList();

            // Ordenar
            _filtrados = (cmbOrden.SelectedIndex) switch
            {
                1 => _filtrados.OrderByDescending(r => r.MargenPct).ToList(),
                2 => _filtrados.OrderByDescending(r => r.IngresoTotal).ToList(),
                3 => _filtrados.OrderByDescending(r => r.UnidadesVendidas).ToList(),
                4 => _filtrados.OrderBy(r => r.Descripcion).ToList(),
                _ => _filtrados.OrderByDescending(r => r.UtilidadBruta).ToList()
            };

            BindGrid();
            ActualizarKpis();
            pnlGrafica.Invalidate();
        }

        private void BindGrid()
        {
            dgv.DataSource = null;
            dgv.DataSource = _filtrados.Select(r => new
            {
                r.Codigo,
                Descripcion    = r.Descripcion.Length > 28
                                 ? r.Descripcion.Substring(0, 28) : r.Descripcion,
                r.Departamento,
                Costo          = r.CostoUnitario.ToString("C2"),
                Precio         = r.PrecioVenta.ToString("C2"),
                Margen_U       = (r.PrecioVenta - r.CostoUnitario).ToString("C2"),
                Margen_Pct     = r.MargenPct.ToString("F1") + "%",
                Vendidas       = r.UnidadesVendidas.ToString("N0"),
                Ingresos       = r.IngresoTotal.ToString("C2"),
                Costo_Total    = r.CostoTotal.ToString("C2"),
                Utilidad       = r.UtilidadBruta.ToString("C2"),
                Stock          = r.StockActual.ToString("N0"),
                ValInventario  = r.ValorInventario.ToString("C2"),
                Rotacion       = r.RotacionMeses > 0
                                 ? r.RotacionMeses.ToString("F1") + " m" : "—"
            }).ToList();
        }

        private void ActualizarKpis()
        {
            if (_filtrados.Count == 0)
            {
                lblTotalIngreso.Text  = "$0";
                lblTotalUtilidad.Text = "$0";
                lblMargenProm.Text    = "0%";
                lblTopProducto.Text   = "—";
                lblMejorMargen.Text   = "—";
                lblSinMovimiento.Text = "—";
                return;
            }

            decimal totalIngreso  = _filtrados.Sum(r => r.IngresoTotal);
            decimal totalUtilidad = _filtrados.Sum(r => r.UtilidadBruta);
            decimal margenProm    = totalIngreso > 0
                ? Math.Round(totalUtilidad / totalIngreso * 100, 1) : 0;

            var top     = _filtrados.OrderByDescending(r => r.UtilidadBruta).First();
            var mejor   = _filtrados.Where(r => r.IngresoTotal > 0)
                                    .OrderByDescending(r => r.MargenPct).FirstOrDefault();
            int sinMov  = _filtrados.Count(r => r.UnidadesVendidas == 0);

            lblTotalIngreso.Text  = FormatMiles(totalIngreso);
            lblTotalUtilidad.Text = FormatMiles(totalUtilidad);
            lblMargenProm.Text    = margenProm + "%";
            lblTopProducto.Text   = top.Codigo;
            lblMejorMargen.Text   = mejor != null
                ? mejor.Codigo + " " + mejor.MargenPct + "%" : "—";
            lblSinMovimiento.Text = sinMov.ToString();
        }

        private static string FormatMiles(decimal v)
        {
            if (v >= 1_000_000) return "$" + (v / 1_000_000).ToString("F1") + "M";
            if (v >= 1_000)     return "$" + (v / 1_000).ToString("F0") + "k";
            return v.ToString("C0");
        }

        // ══════════════════════════════════════════════════════════════════
        //  COLOREAR FILAS
        // ══════════════════════════════════════════════════════════════════
        private void ColorearFilas()
        {
            foreach (DataGridViewRow row in dgv.Rows)
            {
                if (row.Index >= _filtrados.Count) continue;
                var r = _filtrados[row.Index];

                // Color por margen
                if (r.UnidadesVendidas == 0)
                    row.DefaultCellStyle.ForeColor = Color.FromArgb(160, 160, 170);
                else if (r.MargenPct >= 30)
                    row.Cells["Margen_Pct"].Style.ForeColor = Color.FromArgb(20, 130, 60);
                else if (r.MargenPct >= 10)
                    row.Cells["Margen_Pct"].Style.ForeColor = Color.FromArgb(160, 120, 20);
                else
                    row.Cells["Margen_Pct"].Style.ForeColor = Color.FromArgb(180, 40, 40);

                // Color utilidad negativa
                if (r.UtilidadBruta < 0)
                {
                    row.Cells["Utilidad"].Style.BackColor  = Color.FromArgb(255, 235, 235);
                    row.Cells["Utilidad"].Style.ForeColor  = Color.FromArgb(180, 30, 30);
                }
            }
        }

        // ══════════════════════════════════════════════════════════════════
        //  GRAFICA DE BARRAS HORIZONTALES (top 10 utilidad)
        // ══════════════════════════════════════════════════════════════════
        private void PaintGrafica(object sender, PaintEventArgs e)
        {
            var g   = e.Graphics;
            var pnl = (Panel)sender;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            if (_filtrados.Count == 0)
            {
                g.DrawString("Sin datos", new Font("Segoe UI", 10f),
                    Brushes.Gray, 20, 20);
                return;
            }

            // Identificar fila seleccionada
            string codSeleccionado = null;
            if (dgv.CurrentRow != null && dgv.CurrentRow.Index < _filtrados.Count)
                codSeleccionado = _filtrados[dgv.CurrentRow.Index].Codigo;

            // Titulo
            using var fntTit = new Font("Segoe UI", 9f, FontStyle.Bold);
            g.FillRectangle(new SolidBrush(Color.FromArgb(30, 100, 60)),
                0, 0, pnl.Width, 28);
            g.DrawString("Top 10 — Utilidad bruta", fntTit, Brushes.White, 10, 7);

            int padL = 10, padR = 70, padT = 36, padB = 20;
            int w    = pnl.Width  - padL - padR;
            int h    = pnl.Height - padT - padB;

            // Top 10 por utilidad
            var top10 = _filtrados
                .OrderByDescending(r => r.UtilidadBruta)
                .Take(10)
                .ToList();

            if (top10.Count == 0) return;

            decimal maxVal = Math.Max(
                Math.Abs(top10.Max(r => r.UtilidadBruta)),
                Math.Abs(top10.Min(r => r.UtilidadBruta)));
            if (maxVal == 0) maxVal = 1;

            float rowH  = (float)h / top10.Count;
            using var fntS = new Font("Segoe UI", 7.5f);
            using var fntB = new Font("Segoe UI", 7.5f, FontStyle.Bold);

            // Linea de cero
            float xCero = padL + (float)(w * 0.5);

            for (int i = 0; i < top10.Count; i++)
            {
                var r      = top10[i];
                float y    = padT + i * rowH + rowH * 0.1f;
                float barH = rowH * 0.75f;

                bool seleccionado = (r.Codigo == codSeleccionado);

                // Fondo fila seleccionada
                if (seleccionado)
                    g.FillRectangle(new SolidBrush(Color.FromArgb(230, 245, 235)),
                        padL, padT + i * rowH, w + padR - 5, rowH);

                // Determinar color por margen
                Color barColor;
                if (r.UtilidadBruta < 0)      barColor = Color.FromArgb(200, 60, 60);
                else if (r.MargenPct >= 30)    barColor = Color.FromArgb(25, 130, 65);
                else if (r.MargenPct >= 15)    barColor = Color.FromArgb(100, 160, 60);
                else                           barColor = Color.FromArgb(220, 160, 30);

                float barW = (float)(w * 0.5 * Math.Abs((double)r.UtilidadBruta) / (double)maxVal);
                float barX = r.UtilidadBruta >= 0 ? xCero : xCero - barW;

                using var brBar = new SolidBrush(barColor);
                if (barW > 0)
                    g.FillRectangle(brBar, barX, y, barW, barH);

                // Borde si seleccionado
                if (seleccionado)
                    g.DrawRectangle(new Pen(barColor, 1.5f), barX, y, barW, barH);

                // Nombre producto (truncado)
                string nombre = (r.Descripcion ?? r.Codigo);
                if (nombre.Length > 14) nombre = nombre.Substring(0, 14);
                g.DrawString(nombre, seleccionado ? fntB : fntS,
                    seleccionado ? Brushes.DarkGreen : Brushes.DimGray,
                    new RectangleF(padL, y, xCero - padL - 3, barH),
                    new StringFormat { Alignment = StringAlignment.Far,
                                       LineAlignment = StringAlignment.Center });

                // Valor al lado de la barra
                string valStr = FormatMiles(r.UtilidadBruta) + " (" + r.MargenPct.ToString("F0") + "%)";
                float valX    = r.UtilidadBruta >= 0 ? barX + barW + 3 : barX - 3;
                using var brVal = new SolidBrush(barColor);
                g.DrawString(valStr, fntS, brVal, valX, y + 1);
            }

            // Linea de cero vertical
            using var penCero = new Pen(Color.FromArgb(150, 150, 160), 1f);
            g.DrawLine(penCero, xCero, padT, xCero, padT + h);

            // Leyenda
            int ly = pnl.Height - 18;
            DrawLegRect(g, padL,      ly, Color.FromArgb(25, 130, 65),  "Margen >= 30%", fntS);
            DrawLegRect(g, padL + 110,ly, Color.FromArgb(100, 160, 60), "Margen >= 15%", fntS);
            DrawLegRect(g, padL + 220,ly, Color.FromArgb(220, 160, 30), "Margen < 15%",  fntS);
            DrawLegRect(g, padL + 320,ly, Color.FromArgb(200, 60, 60),  "Perdida",        fntS);
        }

        private void DrawLegRect(Graphics g, int x, int y, Color c, string txt, Font f)
        {
            g.FillRectangle(new SolidBrush(c), x, y, 10, 10);
            g.DrawString(txt, f, Brushes.DimGray, x + 13, y - 1);
        }

        // ══════════════════════════════════════════════════════════════════
        //  EXPORTAR PDF
        // ══════════════════════════════════════════════════════════════════
        private void ExportarPdf()
        {
            if (_filtrados.Count == 0)
            {
                MessageBox.Show("No hay datos para exportar.", "Aviso",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            try
            {
                var pdf = PdfService.GenerarReporteRentabilidad(_filtrados,
                    dtpDesde.Value, dtpHasta.Value);
                PdfService.GuardarYAbrir(pdf,
                    "Rentabilidad_" + DateTime.Today.ToString("yyyy-MM-dd") + ".pdf");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al generar PDF:\n" + ex.Message,
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // ══════════════════════════════════════════════════════════════════
        //  HELPERS UI
        // ══════════════════════════════════════════════════════════════════
        private Label KpiCard(Panel parent, string titulo, Color color, int x)
        {
            var card = new Panel
            {
                Location  = new Point(x, 5),
                Size      = new Size(190, 60),
                BackColor = color
            };
            card.Controls.Add(new Label
            {
                Text      = titulo,
                Font      = new Font("Segoe UI", 7.5f),
                ForeColor = Color.FromArgb(200, 230, 210),
                AutoSize  = false,
                Size      = new Size(180, 16),
                Location  = new Point(8, 6),
                TextAlign = ContentAlignment.TopLeft
            });
            var lbl = new Label
            {
                Text      = "—",
                Font      = new Font("Segoe UI", 14f, FontStyle.Bold),
                ForeColor = Color.White,
                AutoSize  = false,
                Size      = new Size(180, 32),
                Location  = new Point(8, 22),
                TextAlign = ContentAlignment.MiddleLeft
            };
            card.Controls.Add(lbl);
            parent.Controls.Add(card);
            return lbl;
        }

        private Label Lbl(string t, int x, int y) =>
            new Label { Text = t, Location = new Point(x, y), AutoSize = true,
                        Font = new Font("Segoe UI", 9f) };
    }
}
