using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Windows.Forms;

namespace SistemaGestion
{
    public class FrmRentabilidad : FormBase
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
        private List<RentabilidadProducto> _datos     = new();
        private List<RentabilidadProducto> _filtrados = new();

        // ── Ordenamiento por columna ──────────────────────────────────────
        private string _sortColumn    = "";
        private bool   _sortAscending = true;

        public FrmRentabilidad()
        {
            Text          = "Reporte de Rentabilidad por Producto";
            Size          = new Size(1280, 760);
            MinimumSize   = new Size(1000, 600);
            StartPosition = FormStartPosition.CenterParent;
            BackColor     = Color.FromArgb(240, 244, 250);

            BuildUI();
            
            VentanaPrefs.Restaurar(this);
            var _tipA = AyudaService.CrearTooltip();
            var _btnHelp = new Button {
                Text = "? Ayuda", Size = new Size(80, 28),
                BackColor = Color.FromArgb(80, 50, 150), ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            _btnHelp.FlatAppearance.BorderSize = 0;
            _btnHelp.Click += (s, e) => AyudaService.Mostrar(this, "FrmRentabilidad");
            AyudaService.Tip(_tipA, _btnHelp, "Abrir guía de ayuda");;
            var _tp = new System.Windows.Forms.Timer { Interval = 800 };
            _tp.Tick        += (s, e) => { _tp.Stop(); VentanaPrefs.Guardar(this); };
            SizeChanged     += (s, e) => { _tp.Stop(); _tp.Start(); };
            LocationChanged += (s, e) => { _tp.Stop(); _tp.Start(); };
            FormClosed      += (s, e) => { _tp.Stop(); _tp.Dispose(); };

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

            txtBuscar.TextChanged        += (s, e) => AplicarFiltros();
            cmbOrden.SelectedIndexChanged += (s, e) => { _sortColumn = ""; AplicarFiltros(); };
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

            pnlGrafica = new Panel
            {
                Dock      = DockStyle.Right,
                Width     = 340,
                BackColor = Color.White
            };
            pnlGrafica.Paint += PaintGrafica;

            var sep = new Panel { Dock = DockStyle.Right, Width = 4, BackColor = Color.FromArgb(210, 215, 228) };

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
            dgv.EnableHeadersVisualStyles    = false;
            dgv.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(238, 248, 242);
            dgv.DataBindingComplete += (s, e) => ColorearFilas();
            dgv.SelectionChanged    += (s, e) => pnlGrafica.Invalidate();

            // ── Ordenar al hacer click en cabecera ────────────────────────
            dgv.ColumnHeaderMouseClick += DgvColumnHeaderMouseClick;

            pnlMain.Controls.Add(dgv);
            pnlMain.Controls.Add(sep);
            pnlMain.Controls.Add(pnlGrafica);

            var footer = new Panel
            {
                Dock      = DockStyle.Bottom,
                Height    = 30,
                BackColor = Color.FromArgb(240, 244, 250)
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
                    "   |   Click en cabecera de columna para ordenar";

            Controls.AddRange(new Control[] { pnlMain, footer, pnlKpis, pnlFiltros, hdr });
        }

        // ══════════════════════════════════════════════════════════════════
        //  ORDENAMIENTO POR CLICK EN CABECERA
        // ══════════════════════════════════════════════════════════════════
        private void DgvColumnHeaderMouseClick(object sender, DataGridViewCellMouseEventArgs e)
        {
            string col = dgv.Columns[e.ColumnIndex].Name;

            if (_sortColumn == col)
                _sortAscending = !_sortAscending;   // invertir si misma columna
            else
            {
                _sortColumn    = col;
                _sortAscending = true;
            }

            // Actualizar icono en cabecera
            foreach (DataGridViewColumn c in dgv.Columns)
                c.HeaderCell.SortGlyphDirection = SortOrder.None;
            dgv.Columns[e.ColumnIndex].HeaderCell.SortGlyphDirection =
                _sortAscending ? SortOrder.Ascending : SortOrder.Descending;

            OrdenarPorColumna();
            BindGrid();
            ActualizarKpis();
            pnlGrafica.Invalidate();
        }

        private void OrdenarPorColumna()
        {
            if (string.IsNullOrEmpty(_sortColumn)) return;

            _filtrados = _sortColumn switch
            {
                "Codigo"       => Ord(_filtrados, r => r.Codigo),
                "Descripcion"  => Ord(_filtrados, r => r.Descripcion),
                "Departamento" => Ord(_filtrados, r => r.Departamento),
                "Costo"        => Ord(_filtrados, r => r.CostoUnitario),
                "Precio"       => Ord(_filtrados, r => r.PrecioVenta),
                "Margen_U"     => Ord(_filtrados, r => r.PrecioVenta - r.CostoUnitario),
                "Margen_Pct"   => Ord(_filtrados, r => r.MargenPct),
                "Vendidas"     => Ord(_filtrados, r => r.UnidadesVendidas),
                "Ingresos"     => Ord(_filtrados, r => r.IngresoTotal),
                "Costo_Total"  => Ord(_filtrados, r => r.CostoTotal),
                "Utilidad"     => Ord(_filtrados, r => r.UtilidadBruta),
                "Stock"        => Ord(_filtrados, r => r.StockActual),
                "ValInventario"=> Ord(_filtrados, r => r.ValorInventario),
                "Rotacion"     => Ord(_filtrados, r => r.RotacionMeses),
                _              => _filtrados
            };
        }

        private List<RentabilidadProducto> Ord<T>(
            List<RentabilidadProducto> lista,
            Func<RentabilidadProducto, T> key) where T : IComparable<T>
            => _sortAscending
                ? lista.OrderBy(key).ToList()
                : lista.OrderByDescending(key).ToList();

        // ══════════════════════════════════════════════════════════════════
        //  CARGA Y FILTRADO
        // ══════════════════════════════════════════════════════════════════
        private void CargarDatos()
        {
            _datos = DatabaseService.ObtenerRentabilidad(dtpDesde.Value, dtpHasta.Value);

            var deptos = _datos.Select(d => d.Departamento)
                               .Where(d => !string.IsNullOrWhiteSpace(d))
                               .Distinct().OrderBy(d => d).ToList();
            cmbDepto.Items.Clear();
            cmbDepto.Items.Add("Todos");
            foreach (var dep in deptos) cmbDepto.Items.Add(dep);
            if (cmbDepto.SelectedIndex < 0) cmbDepto.SelectedIndex = 0;

            _sortColumn = "";   // resetear orden al recargar
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
                    BusquedaHelper.ContieneFlexibleEnAlguno(buscar,
                        r.Codigo, r.Descripcion))
                .ToList();

            // Si hay orden por columna activo, usarlo; si no, usar combo
            if (!string.IsNullOrEmpty(_sortColumn))
            {
                OrdenarPorColumna();
            }
            else
            {
                _filtrados = (cmbOrden.SelectedIndex) switch
                {
                    1 => _filtrados.OrderByDescending(r => r.MargenPct).ToList(),
                    2 => _filtrados.OrderByDescending(r => r.IngresoTotal).ToList(),
                    3 => _filtrados.OrderByDescending(r => r.UnidadesVendidas).ToList(),
                    4 => _filtrados.OrderBy(r => r.Descripcion).ToList(),
                    _ => _filtrados.OrderByDescending(r => r.UtilidadBruta).ToList()
                };
            }

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
                Descripcion    = r.Descripcion,
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
            AjustarColumnas();
            VentanaPrefs.AplicarAnchos(this, dgv);
        }

        // Anchos proporcionales: la descripción más ancha y los montos alineados
        // a la derecha, para que no se corten ni se tapen los valores.
        private void AjustarColumnas()
        {
            var pesos = new Dictionary<string, float> {
                ["Codigo"] = 70, ["Descripcion"] = 200, ["Departamento"] = 70,
                ["Costo"] = 70, ["Precio"] = 70, ["Margen_U"] = 70, ["Margen_Pct"] = 62,
                ["Vendidas"] = 62, ["Ingresos"] = 90, ["Costo_Total"] = 90, ["Utilidad"] = 90,
                ["Stock"] = 58, ["ValInventario"] = 90, ["Rotacion"] = 62 };
            foreach (DataGridViewColumn c in dgv.Columns)
            {
                if (pesos.TryGetValue(c.Name, out var w)) c.FillWeight = w;
                c.MinimumWidth = 40;
                if (c.Name != "Codigo" && c.Name != "Descripcion" && c.Name != "Departamento")
                    c.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
            }
            if (dgv.Columns.Contains("Departamento"))
                dgv.Columns["Departamento"].Visible = _filtrados.Any(r => !string.IsNullOrWhiteSpace(r.Departamento));
            if (dgv.Columns.Contains("Margen_Pct")) dgv.Columns["Margen_Pct"].HeaderText = "Margen %";
            if (dgv.Columns.Contains("Margen_U"))   dgv.Columns["Margen_U"].HeaderText   = "Margen $";
            if (dgv.Columns.Contains("Costo_Total")) dgv.Columns["Costo_Total"].HeaderText = "Costo total";
            if (dgv.Columns.Contains("ValInventario")) dgv.Columns["ValInventario"].HeaderText = "Valor inventario";
            if (dgv.Columns.Contains("Rotacion"))   dgv.Columns["Rotacion"].HeaderText   = "Rotación";
            if (dgv.Columns.Contains("Descripcion")) dgv.Columns["Descripcion"].HeaderText = "Descripción";
            if (dgv.Columns.Contains("Codigo"))     dgv.Columns["Codigo"].HeaderText     = "Código";
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

            var top   = _filtrados.OrderByDescending(r => r.UtilidadBruta).First();
            var mejor = _filtrados.Where(r => r.IngresoTotal > 0)
                                  .OrderByDescending(r => r.MargenPct).FirstOrDefault();
            int sinMov = _filtrados.Count(r => r.UnidadesVendidas == 0);

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

                if (r.UnidadesVendidas == 0)
                    row.DefaultCellStyle.ForeColor = Color.FromArgb(160, 160, 170);
                else if (r.MargenPct >= 30)
                    row.Cells["Margen_Pct"].Style.ForeColor = Color.FromArgb(20, 130, 60);
                else if (r.MargenPct >= 10)
                    row.Cells["Margen_Pct"].Style.ForeColor = Color.FromArgb(160, 120, 20);
                else
                    row.Cells["Margen_Pct"].Style.ForeColor = Color.FromArgb(180, 40, 40);

                if (r.UtilidadBruta < 0)
                {
                    row.Cells["Utilidad"].Style.BackColor = Color.FromArgb(255, 235, 235);
                    row.Cells["Utilidad"].Style.ForeColor = Color.FromArgb(180, 30, 30);
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
                g.DrawString("Sin datos", new Font("Segoe UI", 10f), Brushes.Gray, 20, 20);
                return;
            }

            string codSeleccionado = null;
            if (dgv.CurrentRow != null && dgv.CurrentRow.Index < _filtrados.Count)
                codSeleccionado = _filtrados[dgv.CurrentRow.Index].Codigo;

            using var fntTit = new Font("Segoe UI", 9f, FontStyle.Bold);
            g.FillRectangle(new SolidBrush(Color.FromArgb(30, 100, 60)), 0, 0, pnl.Width, 28);
            g.DrawString("Top 10 — Utilidad bruta", fntTit, Brushes.White, 10, 7);

            int padL = 10, padR = 70, padT = 36, padB = 20;
            int w    = pnl.Width  - padL - padR;
            int h    = pnl.Height - padT - padB;

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
            float xCero = padL + (float)(w * 0.5);

            for (int i = 0; i < top10.Count; i++)
            {
                var r      = top10[i];
                float y    = padT + i * rowH + rowH * 0.1f;
                float barH = rowH * 0.75f;
                bool sel   = (r.Codigo == codSeleccionado);

                if (sel)
                    g.FillRectangle(new SolidBrush(Color.FromArgb(230, 245, 235)),
                        padL, padT + i * rowH, w + padR - 5, rowH);

                Color barColor;
                if (r.UtilidadBruta < 0)   barColor = Color.FromArgb(200, 60, 60);
                else if (r.MargenPct >= 30) barColor = Color.FromArgb(25, 130, 65);
                else if (r.MargenPct >= 15) barColor = Color.FromArgb(100, 160, 60);
                else                        barColor = Color.FromArgb(220, 160, 30);

                float barW = (float)(w * 0.5 * Math.Abs((double)r.UtilidadBruta) / (double)maxVal);
                float barX = r.UtilidadBruta >= 0 ? xCero : xCero - barW;

                using var brBar = new SolidBrush(barColor);
                if (barW > 0)
                    g.FillRectangle(brBar, barX, y, barW, barH);

                if (sel)
                    g.DrawRectangle(new Pen(barColor, 1.5f), barX, y, barW, barH);

                string nombre = (r.Descripcion ?? r.Codigo);
                if (nombre.Length > 14) nombre = nombre.Substring(0, 14);
                g.DrawString(nombre, sel ? fntB : fntS,
                    sel ? Brushes.DarkGreen : Brushes.DimGray,
                    new RectangleF(padL, y, xCero - padL - 3, barH),
                    new StringFormat { Alignment = StringAlignment.Far,
                                       LineAlignment = StringAlignment.Center });

                string valStr = FormatMiles(r.UtilidadBruta) + " (" + r.MargenPct.ToString("F0") + "%)";
                float valX    = r.UtilidadBruta >= 0 ? barX + barW + 3 : barX - 3;
                using var brVal = new SolidBrush(barColor);
                g.DrawString(valStr, fntS, brVal, valX, y + 1);
            }

            using var penCero = new Pen(Color.FromArgb(150, 150, 160), 1f);
            g.DrawLine(penCero, xCero, padT, xCero, padT + h);

            int ly = pnl.Height - 18;
            DrawLegRect(g, padL,      ly, Color.FromArgb(25, 130, 65),  "Margen >= 30%", fntS);
            DrawLegRect(g, padL + 110,ly, Color.FromArgb(100, 160, 60), "Margen >= 15%", fntS);
            DrawLegRect(g, padL + 220,ly, Color.FromArgb(220, 160, 30), "Margen < 15%",  fntS);
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
