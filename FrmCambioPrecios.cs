using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Windows.Forms;

namespace SistemaGestion
{
    // ── Cambio de Precios sobre el INVENTARIO REAL ─────────────────────────
    //  Permite actualizar precios de venta de forma rápida y versátil:
    //   • Editar el precio nuevo de un producto directo en la celda.
    //   • Aplicar un % (subir/bajar) a: seleccionados, filtro visible, o todos.
    //   • Redondear los precios nuevos a un múltiplo.
    //   • Fijar precio = costo + % de margen.
    //  Todo queda como "precio nuevo" (previsualización) hasta pulsar
    //  "Aplicar al inventario", que persiste en la tabla Inventario.
    public class FrmCambioPrecios : FormBase
    {
        private DataGridView dgv;
        private Label lblResumen;
        private TextBox txtBuscar;
        private ComboBox cmbDepto, cmbAlcance, cmbRedondeo;
        private NumericUpDown numPct, numMargen;

        private List<Inventario> _todo = new List<Inventario>();     // inventario completo
        private List<Inventario> _vista = new List<Inventario>();    // filtrado actual (lo que se ve)
        private readonly Dictionary<string, decimal> _nuevos =       // codigo -> precio nuevo pendiente
            new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);
        private bool _cargando = false;

        public FrmCambioPrecios()
        {
            Text = "Cambio de Precios";
            Size = new Size(1040, 600);
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.Sizable;
            MaximizeBox = true;
            MinimumSize = new Size(820, 480);
            BackColor = Color.FromArgb(240, 244, 250);

            // ── Header ──────────────────────────────────────────────────────
            var hdr = new Panel { Dock = DockStyle.Top, Height = 50, BackColor = Color.FromArgb(26, 74, 138) };
            hdr.Controls.Add(new Label { Text = "Cambio de Precios", ForeColor = Color.White,
                Font = new Font("Segoe UI", 12f, FontStyle.Bold), AutoSize = true, Location = new Point(12, 13) });
            var btnAyuda = new Button { Text = "? Ayuda", Size = new Size(84, 30),
                BackColor = Color.FromArgb(80, 50, 150), ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 8.5f, FontStyle.Bold), Cursor = Cursors.Hand };
            btnAyuda.FlatAppearance.BorderSize = 0;
            btnAyuda.Click += (s, e) => AyudaService.Mostrar(this, "FrmCambioPrecios");
            var pnlHdr = new FlowLayoutPanel { Dock = DockStyle.Right, AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink, WrapContents = false,
                BackColor = Color.Transparent, Padding = new Padding(0, 10, 8, 0) };
            pnlHdr.Controls.Add(btnAyuda);
            hdr.Controls.Add(pnlHdr);

            // ── Barra de búsqueda / filtro ──────────────────────────────────
            var barBusq = new Panel { Dock = DockStyle.Top, Height = 44, BackColor = Color.FromArgb(245, 235, 215) };
            barBusq.Controls.Add(new Label { Text = "Buscar:", Location = new Point(10, 14), AutoSize = true, Font = new Font("Segoe UI", 9f) });
            txtBuscar = new TextBox { Location = new Point(62, 10), Size = new Size(240, 25) };
            txtBuscar.TextChanged += (s, e) => AplicarFiltro();
            barBusq.Controls.Add(txtBuscar);
            barBusq.Controls.Add(new Label { Text = "Departamento:", Location = new Point(320, 14), AutoSize = true, Font = new Font("Segoe UI", 9f) });
            cmbDepto = new ComboBox { Location = new Point(420, 10), Size = new Size(200, 25), DropDownStyle = ComboBoxStyle.DropDownList };
            cmbDepto.SelectedIndexChanged += (s, e) => AplicarFiltro();
            barBusq.Controls.Add(cmbDepto);
            barBusq.Controls.Add(new Label { Text = "Tip: doble clic en un producto para cambiar su precio al instante",
                Location = new Point(640, 14), AutoSize = true, ForeColor = Color.FromArgb(120, 80, 20),
                Font = new Font("Segoe UI", 9f, FontStyle.Italic) });

            // ── Barra de acciones masivas ───────────────────────────────────
            var barAcc = new Panel { Dock = DockStyle.Top, Height = 50, BackColor = Color.FromArgb(250, 243, 230) };
            barAcc.Controls.Add(new Label { Text = "Aplicar a:", Location = new Point(10, 16), AutoSize = true, Font = new Font("Segoe UI", 9f) });
            cmbAlcance = new ComboBox { Location = new Point(78, 12), Size = new Size(150, 25), DropDownStyle = ComboBoxStyle.DropDownList };
            cmbAlcance.Items.AddRange(new object[] { "Seleccionados", "Filtro visible", "Todos" });
            cmbAlcance.SelectedIndex = 1;
            barAcc.Controls.Add(cmbAlcance);

            barAcc.Controls.Add(new Label { Text = "% ajuste:", Location = new Point(240, 16), AutoSize = true, Font = new Font("Segoe UI", 9f) });
            numPct = new NumericUpDown { Location = new Point(305, 12), Size = new Size(72, 25), Minimum = -99, Maximum = 999, DecimalPlaces = 1, Increment = 1 };
            barAcc.Controls.Add(numPct);
            var btnPct = BtnAcc("Aplicar %", Color.FromArgb(180, 110, 30), new Point(385, 10));
            btnPct.Click += (s, e) => AplicarPorcentaje();
            barAcc.Controls.Add(btnPct);

            barAcc.Controls.Add(new Label { Text = "Redondear a:", Location = new Point(500, 16), AutoSize = true, Font = new Font("Segoe UI", 9f) });
            cmbRedondeo = new ComboBox { Location = new Point(588, 12), Size = new Size(110, 25), DropDownStyle = ComboBoxStyle.DropDownList };
            cmbRedondeo.Items.AddRange(new object[] { "No redondear", "0.05", "0.10", "0.25", "0.50", "1.00" });
            cmbRedondeo.SelectedIndex = 0;
            barAcc.Controls.Add(cmbRedondeo);

            barAcc.Controls.Add(new Label { Text = "Margen s/costo %:", Location = new Point(715, 16), AutoSize = true, Font = new Font("Segoe UI", 9f) });
            numMargen = new NumericUpDown { Location = new Point(835, 12), Size = new Size(72, 25), Minimum = 0, Maximum = 999, DecimalPlaces = 1, Increment = 5, Value = 30 };
            barAcc.Controls.Add(numMargen);
            var btnMargen = BtnAcc("Precio = costo+margen", Color.FromArgb(60, 120, 80), new Point(915, 10));
            btnMargen.Width = 100;
            btnMargen.Click += (s, e) => AplicarMargen();
            barAcc.Controls.Add(btnMargen);

            // ── Grid ────────────────────────────────────────────────────────
            dgv = new DataGridView { Dock = DockStyle.Fill, ReadOnly = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect, MultiSelect = true,
                AllowUserToAddRows = false, AllowUserToDeleteRows = false,
                AutoGenerateColumns = false, AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                BackgroundColor = Color.White, BorderStyle = BorderStyle.None,
                RowHeadersVisible = false, Font = new Font("Segoe UI", 9f), EditMode = DataGridViewEditMode.EditOnEnter };
            dgv.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(180, 110, 30);
            dgv.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
            dgv.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 9f, FontStyle.Bold);
            dgv.EnableHeadersVisualStyles = false;
            AddCol("Codigo", "Código", 70, true);
            AddCol("Descripcion", "Descripción", 220, true);
            AddCol("Departamento", "Depto", 80, true);
            AddCol("Unidad", "Und", 45, true);
            AddCol("Costo", "Costo", 70, true);
            AddCol("PrecioActual", "Precio actual", 85, true);
            AddCol("PrecioNuevo", "Precio nuevo", 85, false);
            AddCol("Cambio", "Cambio", 65, true);
            AddCol("Margen", "Margen", 65, true);
            dgv.CellEndEdit += OnCellEndEdit;
            dgv.CellDoubleClick += OnRowDblClick;   // doble clic = cambiar precio de ese producto al instante

            // ── Footer ──────────────────────────────────────────────────────
            var footer = new Panel { Dock = DockStyle.Bottom, Height = 48, BackColor = Color.FromArgb(240, 228, 210) };
            lblResumen = new Label { AutoSize = true, Location = new Point(12, 15),
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold), ForeColor = Color.FromArgb(100, 60, 10) };
            var btnAplicar = new Button { Text = "✔ Aplicar al inventario", Size = new Size(190, 32),
                BackColor = Color.FromArgb(30, 130, 70), ForeColor = Color.White, FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold), Anchor = AnchorStyles.Top | AnchorStyles.Right };
            btnAplicar.FlatAppearance.BorderSize = 0;
            btnAplicar.Click += (s, e) => AplicarCambios();
            var btnDescartar = new Button { Text = "Descartar", Size = new Size(90, 32),
                BackColor = Color.FromArgb(150, 90, 40), ForeColor = Color.White, FlatStyle = FlatStyle.Flat,
                Anchor = AnchorStyles.Top | AnchorStyles.Right };
            btnDescartar.FlatAppearance.BorderSize = 0;
            btnDescartar.Click += (s, e) => { _nuevos.Clear(); PoblarGrid(); ActualizarResumen(); };
            var btnCerrar = new Button { Text = "Cerrar", Size = new Size(90, 32),
                BackColor = Color.FromArgb(100, 100, 120), ForeColor = Color.White, FlatStyle = FlatStyle.Flat,
                Anchor = AnchorStyles.Top | AnchorStyles.Right };
            btnCerrar.FlatAppearance.BorderSize = 0;
            btnCerrar.Click += (s, e) => Close();
            var flowF = new FlowLayoutPanel { Dock = DockStyle.Right, FlowDirection = FlowDirection.RightToLeft,
                AutoSize = false, Width = 400, WrapContents = false, BackColor = Color.Transparent, Padding = new Padding(0, 8, 8, 0) };
            flowF.Controls.AddRange(new Control[] { btnCerrar, btnDescartar, btnAplicar });
            footer.Controls.Add(lblResumen);
            footer.Controls.Add(flowF);

            // z-order de docking: Fill primero, luego Top (el último Top queda arriba), Bottom al final.
            Controls.AddRange(new Control[] { dgv, barAcc, barBusq, hdr, footer });

            CargarInventario();
        }

        private Button BtnAcc(string t, Color c, Point p)
        {
            var b = new Button { Text = t, Size = new Size(92, 28), Location = p, BackColor = c, ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 8.5f, FontStyle.Bold), Cursor = Cursors.Hand };
            b.FlatAppearance.BorderSize = 0;
            return b;
        }

        private void AddCol(string name, string header, int fill, bool ro)
        {
            var col = new DataGridViewTextBoxColumn { Name = name, HeaderText = header, ReadOnly = ro, FillWeight = fill };
            if (!ro) col.DefaultCellStyle.BackColor = Color.FromArgb(255, 249, 224);
            if (name == "Costo" || name == "PrecioActual" || name == "PrecioNuevo" || name == "Cambio" || name == "Margen")
                col.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
            dgv.Columns.Add(col);
        }

        // ── Carga y filtrado ────────────────────────────────────────────────
        private void CargarInventario()
        {
            try { _todo = DatabaseService.ObtenerInventario() ?? new List<Inventario>(); }
            catch (Exception ex) { MessageBox.Show("No se pudo leer el inventario:\n" + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error); _todo = new List<Inventario>(); }

            var deptos = _todo.Select(i => string.IsNullOrWhiteSpace(i.Departamento) ? "(sin depto)" : i.Departamento)
                              .Distinct().OrderBy(x => x).ToList();
            cmbDepto.Items.Clear();
            cmbDepto.Items.Add("Todos");
            foreach (var d in deptos) cmbDepto.Items.Add(d);
            cmbDepto.SelectedIndex = 0;

            AplicarFiltro();
        }

        private void AplicarFiltro()
        {
            string q = (txtBuscar?.Text ?? "").Trim();
            string depto = cmbDepto?.SelectedItem?.ToString() ?? "Todos";

            IEnumerable<Inventario> res = _todo;
            if (!string.IsNullOrEmpty(q))
                res = res.Where(i => (i.Codigo ?? "").IndexOf(q, StringComparison.OrdinalIgnoreCase) >= 0
                                  || (i.Descripcion ?? "").IndexOf(q, StringComparison.OrdinalIgnoreCase) >= 0);
            if (depto != "Todos")
                res = res.Where(i => (string.IsNullOrWhiteSpace(i.Departamento) ? "(sin depto)" : i.Departamento) == depto);

            _vista = res.ToList();
            PoblarGrid();
            ActualizarResumen();
        }

        private void PoblarGrid()
        {
            _cargando = true;
            dgv.Rows.Clear();
            foreach (var i in _vista)
            {
                bool pend = _nuevos.TryGetValue(i.Codigo, out var np) && np > 0;
                decimal precioEfectivo = pend ? np : i.Precio;
                string cambio = pend && i.Precio > 0 ? ((np - i.Precio) / i.Precio * 100m).ToString("+0.0;-0.0") + "%" : "";
                string margen = precioEfectivo > 0 ? ((precioEfectivo - i.CostoActual) / precioEfectivo * 100m).ToString("F1") + "%" : "-";
                int idx = dgv.Rows.Add(
                    i.Codigo,
                    i.Descripcion,
                    string.IsNullOrWhiteSpace(i.Departamento) ? "" : i.Departamento,
                    i.Unidad,
                    i.CostoActual.ToString("N2"),
                    i.Precio.ToString("N2"),
                    pend ? np.ToString("N2") : "",
                    cambio,
                    margen);
                if (pend) dgv.Rows[idx].DefaultCellStyle.BackColor = Color.FromArgb(255, 245, 205);
            }
            _cargando = false;
        }

        private void RefrescarFila(int rowIdx)
        {
            if (rowIdx < 0 || rowIdx >= dgv.Rows.Count) return;
            var cod = dgv.Rows[rowIdx].Cells["Codigo"].Value?.ToString();
            var item = _vista.FirstOrDefault(x => string.Equals(x.Codigo, cod, StringComparison.OrdinalIgnoreCase));
            if (item == null) return;
            bool pend = _nuevos.TryGetValue(item.Codigo, out var np) && np > 0;
            decimal precioEfectivo = pend ? np : item.Precio;
            dgv.Rows[rowIdx].Cells["PrecioNuevo"].Value = pend ? np.ToString("N2") : "";
            dgv.Rows[rowIdx].Cells["Cambio"].Value = pend && item.Precio > 0 ? ((np - item.Precio) / item.Precio * 100m).ToString("+0.0;-0.0") + "%" : "";
            dgv.Rows[rowIdx].Cells["Margen"].Value = precioEfectivo > 0 ? ((precioEfectivo - item.CostoActual) / precioEfectivo * 100m).ToString("F1") + "%" : "-";
            dgv.Rows[rowIdx].DefaultCellStyle.BackColor = pend ? Color.FromArgb(255, 245, 205) : Color.White;
        }

        private void OnCellEndEdit(object sender, DataGridViewCellEventArgs e)
        {
            if (_cargando || e.RowIndex < 0) return;
            if (dgv.Columns[e.ColumnIndex].Name != "PrecioNuevo") return;
            var cod = dgv.Rows[e.RowIndex].Cells["Codigo"].Value?.ToString();
            if (string.IsNullOrEmpty(cod)) return;
            var val = dgv.Rows[e.RowIndex].Cells["PrecioNuevo"].Value?.ToString();
            if (string.IsNullOrWhiteSpace(val)) { _nuevos.Remove(cod); }
            else if (TryPrecio(val, out var d) && d > 0) { _nuevos[cod] = Math.Round(d, 2); }
            else { MessageBox.Show("Precio inválido. Escribe un número mayor a 0.", "Validación", MessageBoxButtons.OK, MessageBoxIcon.Warning); }
            RefrescarFila(e.RowIndex);
            ActualizarResumen();
        }

        // ── Acciones masivas ────────────────────────────────────────────────
        private List<Inventario> Alcance()
        {
            int a = cmbAlcance.SelectedIndex;
            if (a == 0) // seleccionados
            {
                var cods = dgv.SelectedRows.Cast<DataGridViewRow>()
                    .Select(r => r.Cells["Codigo"].Value?.ToString())
                    .Where(x => !string.IsNullOrEmpty(x))
                    .ToList();
                var set = new HashSet<string>(cods, StringComparer.OrdinalIgnoreCase);
                return _vista.Where(i => set.Contains(i.Codigo)).ToList();
            }
            if (a == 1) return _vista.ToList();  // filtro visible
            return _todo.ToList();               // todos
        }

        private void AplicarPorcentaje()
        {
            decimal pct = numPct.Value;
            if (pct == 0) { MessageBox.Show("Indica un porcentaje distinto de 0.", "Info", MessageBoxButtons.OK, MessageBoxIcon.Information); return; }
            var objetivo = Alcance();
            if (objetivo.Count == 0) { MessageBox.Show("No hay productos en el alcance elegido.", "Info", MessageBoxButtons.OK, MessageBoxIcon.Information); return; }
            if (MessageBox.Show($"Aplicar {pct.ToString("+0.0;-0.0")}% al precio de {objetivo.Count} producto(s)?\nQueda como precio nuevo pendiente (aún no toca el inventario).",
                "Confirmar", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;
            foreach (var i in objetivo)
            {
                decimal np = Redondear(i.Precio * (1 + pct / 100m));
                if (np > 0) _nuevos[i.Codigo] = Math.Round(np, 2);
            }
            PoblarGrid();
            ActualizarResumen();
        }

        private void AplicarMargen()
        {
            decimal m = numMargen.Value;
            var objetivo = Alcance();
            if (objetivo.Count == 0) { MessageBox.Show("No hay productos en el alcance elegido.", "Info", MessageBoxButtons.OK, MessageBoxIcon.Information); return; }
            var sinCosto = objetivo.Count(i => i.CostoActual <= 0);
            if (MessageBox.Show($"Fijar precio = costo + {m:F1}% de margen para {objetivo.Count} producto(s)?" +
                (sinCosto > 0 ? $"\n({sinCosto} sin costo se omiten.)" : "") + "\nQueda como precio nuevo pendiente.",
                "Confirmar", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;
            foreach (var i in objetivo)
            {
                if (i.CostoActual <= 0) continue;
                decimal np = Redondear(i.CostoActual * (1 + m / 100m));
                if (np > 0) _nuevos[i.Codigo] = Math.Round(np, 2);
            }
            PoblarGrid();
            ActualizarResumen();
        }

        private decimal Redondear(decimal valor)
        {
            string sel = cmbRedondeo.SelectedItem?.ToString() ?? "No redondear";
            if (sel == "No redondear") return Math.Round(valor, 2);
            if (decimal.TryParse(sel, NumberStyles.Any, CultureInfo.InvariantCulture, out var paso) && paso > 0)
                return Math.Round(Math.Round(valor / paso, 0, MidpointRounding.AwayFromZero) * paso, 2);
            return Math.Round(valor, 2);
        }

        private void AplicarCambios()
        {
            var pend = _todo.Where(i => _nuevos.TryGetValue(i.Codigo, out var np) && np > 0 && np != i.Precio).ToList();
            if (pend.Count == 0) { MessageBox.Show("No hay precios nuevos pendientes de aplicar.", "Info", MessageBoxButtons.OK, MessageBoxIcon.Information); return; }
            if (MessageBox.Show($"Aplicar {pend.Count} precio(s) nuevo(s) al INVENTARIO real?\nEsta acción actualiza la base de datos.",
                "Confirmar", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes) return;
            int ok = 0; string fallo = null;
            foreach (var i in pend)
            {
                try
                {
                    i.Precio = _nuevos[i.Codigo];
                    i.UltimaActualz = DateTime.Today;
                    DatabaseService.ActualizarInventario(i);
                    ok++;
                }
                catch (Exception ex) { fallo = ex.Message; }
            }
            _nuevos.Clear();
            CargarInventario();
            if (fallo != null) MessageBox.Show($"Actualizados: {ok}. Hubo un error en al menos uno:\n{fallo}", "Parcial", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            else MessageBox.Show($"{ok} precio(s) actualizado(s) en el inventario.", "Listo", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void ActualizarResumen()
        {
            int pendientes = _todo.Count(i => _nuevos.TryGetValue(i.Codigo, out var np) && np > 0 && np != i.Precio);
            lblResumen.Text = $"  Productos: {_todo.Count}   |   En vista: {_vista.Count}   |   Precios nuevos pendientes: {pendientes}";
        }

        // Doble clic en una fila: cambiar el precio de ese producto y guardarlo YA.
        private void OnRowDblClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;
            var cod = dgv.Rows[e.RowIndex].Cells["Codigo"].Value?.ToString();
            var item = _todo.FirstOrDefault(x => string.Equals(x.Codigo, cod, StringComparison.OrdinalIgnoreCase));
            if (item == null) return;
            using var d = new DlgPrecioRapido(item);
            if (d.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    item.Precio = d.PrecioNuevo;
                    item.UltimaActualz = DateTime.Today;
                    DatabaseService.ActualizarInventario(item);
                    _nuevos.Remove(item.Codigo);
                    AplicarFiltro();   // refresca manteniendo el filtro/búsqueda actual
                }
                catch (Exception ex)
                { MessageBox.Show("No se pudo guardar: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error); }
            }
        }

        private static bool TryPrecio(string s, out decimal d)
        {
            d = 0;
            if (string.IsNullOrWhiteSpace(s)) return false;
            s = s.Replace("$", "").Replace(" ", "").Trim();
            return decimal.TryParse(s, NumberStyles.Any, CultureInfo.CurrentCulture, out d)
                || decimal.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out d);
        }
    }

    // Mini-diálogo para cambiar el precio de UN producto rápido.
    public class DlgPrecioRapido : FormBase
    {
        public decimal PrecioNuevo { get; private set; }
        private readonly NumericUpDown _num;

        public DlgPrecioRapido(Inventario i)
        {
            Text = "Cambiar precio";
            Size = new Size(380, 250);
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false; MinimizeBox = false;
            BackColor = Color.FromArgb(240, 244, 250);

            int y = 16;
            Controls.Add(new Label { Text = i.Descripcion, Location = new Point(16, y), AutoSize = true,
                MaximumSize = new Size(345, 0), Font = new Font("Segoe UI", 10.5f, FontStyle.Bold) }); y += 46;
            Controls.Add(new Label { Text = $"Código: {i.Codigo}     Costo: {i.CostoActual:N2}", Location = new Point(16, y),
                AutoSize = true, ForeColor = Color.FromArgb(90, 90, 90) }); y += 28;
            Controls.Add(new Label { Text = $"Precio actual: {i.Precio:N2}", Location = new Point(16, y),
                AutoSize = true, Font = new Font("Segoe UI", 9.5f) }); y += 32;

            Controls.Add(new Label { Text = "Precio nuevo:", Location = new Point(16, y + 4), AutoSize = true,
                Font = new Font("Segoe UI", 10f, FontStyle.Bold) });
            _num = new NumericUpDown { Location = new Point(130, y), Size = new Size(150, 28), DecimalPlaces = 2,
                Maximum = 9999999, Minimum = 0, Value = i.Precio, Font = new Font("Segoe UI", 12f) };
            Controls.Add(_num); y += 48;

            var ok = new Button { Text = "Guardar", Location = new Point(60, y), Size = new Size(120, 36),
                BackColor = Color.FromArgb(30, 130, 70), ForeColor = Color.White, FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 10f, FontStyle.Bold) };
            ok.FlatAppearance.BorderSize = 0;
            ok.Click += (s, e) =>
            {
                if (_num.Value <= 0) { MessageBox.Show("El precio debe ser mayor a 0.", "Validación", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
                PrecioNuevo = _num.Value; DialogResult = DialogResult.OK;
            };
            var can = new Button { Text = "Cancelar", DialogResult = DialogResult.Cancel, Location = new Point(200, y),
                Size = new Size(120, 36), BackColor = Color.FromArgb(160, 50, 50), ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
            can.FlatAppearance.BorderSize = 0;
            Controls.AddRange(new Control[] { ok, can });
            AcceptButton = ok; CancelButton = can;

            Shown += (s, e) => { _num.Focus(); _num.Select(0, _num.Text.Length); };
        }
    }

    public class DlgProducto : FormBase
    {
        public ProductoPrecio Resultado { get; private set; }
        private TextBox txtCodigo, txtNombre;
        private ComboBox cmbCategoria;
        private NumericUpDown numCostoActual, numPrecioActual, numCostoNuevo, numPrecioNuevo;
        private Label lblMargen;

        public DlgProducto(ProductoPrecio p)
        {
            Text = p == null ? "Nuevo Producto" : "Editar Producto";
            Size = new Size(440, 440); StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.Sizable; MaximizeBox = true; MinimizeBox = false;
            BackColor = Color.FromArgb(240, 244, 250);

            int y = 15;
            L("Codigo:", y);       txtCodigo = T("", y); y += 38;
            L("Nombre:", y);       txtNombre = T("", y); y += 38;
            L("Categoria:", y);
            cmbCategoria = new ComboBox { Location = new Point(160, y), Size = new Size(230, 25), DropDownStyle = ComboBoxStyle.DropDownList };
            cmbCategoria.Items.AddRange(new object[] { "Alimentos", "Electronicos", "Ropa", "Servicios", "Materia Prima", "Otros" });
            cmbCategoria.SelectedIndex = 0; Controls.Add(cmbCategoria); y += 38;
            L("Costo Actual:", y);  numCostoActual  = N(y); y += 38;
            L("Precio Actual:", y); numPrecioActual = N(y); numPrecioActual.ValueChanged += (s, e) => ActMargen(); y += 38;
            L("Costo Nuevo:", y);   numCostoNuevo   = N(y); y += 38;
            L("Precio Nuevo:", y);  numPrecioNuevo  = N(y); y += 38;
            L("Margen actual:", y);
            lblMargen = new Label { Location = new Point(160, y), AutoSize = true, Font = new Font("Segoe UI", 10f, FontStyle.Bold), ForeColor = Color.FromArgb(40, 120, 40) };
            Controls.Add(lblMargen); y += 45;

            if (p != null)
            {
                txtCodigo.Text = p.Codigo; txtNombre.Text = p.Nombre;
                cmbCategoria.SelectedItem = p.Categoria;
                numCostoActual.Value  = p.CostoActual; numPrecioActual.Value = p.PrecioActual;
                numCostoNuevo.Value   = p.CostoNuevo;  numPrecioNuevo.Value  = p.PrecioNuevo;
            }
            ActMargen();

            var bOk = new Button { Text = "Guardar", Location = new Point(70, y), Size = new Size(110, 34),
                BackColor = Color.FromArgb(30, 130, 70), ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
            bOk.FlatAppearance.BorderSize = 0;
            var bCan = new Button { Text = "Cancelar", DialogResult = DialogResult.Cancel, Location = new Point(210, y), Size = new Size(110, 34),
                BackColor = Color.FromArgb(160, 50, 50), ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
            bCan.FlatAppearance.BorderSize = 0;
            bOk.Click += (s, e) =>
            {
                if (string.IsNullOrWhiteSpace(txtNombre.Text)) { MessageBox.Show("Ingrese el nombre.", "Validacion", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
                Resultado = p ?? new ProductoPrecio();
                Resultado.Codigo       = txtCodigo.Text.Trim();
                Resultado.Nombre       = txtNombre.Text.Trim();
                Resultado.Categoria    = cmbCategoria.SelectedItem?.ToString() ?? "Otros";
                Resultado.CostoActual  = numCostoActual.Value;
                Resultado.PrecioActual = numPrecioActual.Value;
                Resultado.CostoNuevo   = numCostoNuevo.Value;
                Resultado.PrecioNuevo  = numPrecioNuevo.Value;
                Resultado.UltimaActualizacion = DateTime.Today;
                DialogResult = DialogResult.OK;
            };


            var _dlgTip = AyudaService.CrearTooltip();
            if (txtCodigo != null) AyudaService.Tip(_dlgTip, txtCodigo, "Código único del producto");
            if (txtNombre != null) AyudaService.Tip(_dlgTip, txtNombre, "Nombre descriptivo del producto");
            if (numCostoActual != null) AyudaService.Tip(_dlgTip, numCostoActual, "Costo actual vigente en el inventario");
            if (numPrecioActual != null) AyudaService.Tip(_dlgTip, numPrecioActual, "Precio de venta actual");
            if (numCostoNuevo != null) AyudaService.Tip(_dlgTip, numCostoNuevo, "Nuevo costo propuesto (se aplica con el botón [Aplicar])");
            if (numPrecioNuevo != null) AyudaService.Tip(_dlgTip, numPrecioNuevo, "Nuevo precio propuesto (se aplica con el botón [Aplicar])");
            // Recordar posición y tamaño
            VentanaPrefs.Restaurar(this);
            Controls.AddRange(new Control[] { bOk, bCan });

            VentanaPrefs.Restaurar(this);
        }

        private void ActMargen()
        {
            if (numPrecioActual == null || numCostoActual == null || lblMargen == null) return;
            decimal margen = numPrecioActual.Value > 0
                ? (numPrecioActual.Value - numCostoActual.Value) / numPrecioActual.Value * 100 : 0;
            lblMargen.Text = $"{margen:F1}%";
            lblMargen.ForeColor = margen >= 20 ? Color.FromArgb(40, 120, 40) : margen >= 0 ? Color.FromArgb(180, 130, 0) : Color.FromArgb(180, 40, 40);
        }

        private void L(string t, int y) => Controls.Add(new Label { Text = t, Location = new Point(15, y + 3), AutoSize = true, Font = new Font("Segoe UI", 9f, FontStyle.Bold) });
        private TextBox T(string v, int y) { var tb = new TextBox { Text = v, Location = new Point(160, y), Size = new Size(230, 25) }; Controls.Add(tb); return tb; }
        private NumericUpDown N(int y) { var n = new NumericUpDown { Location = new Point(160, y), Size = new Size(230, 25), Maximum = 9999999, DecimalPlaces = 2 }; Controls.Add(n); return n; }
    }
}
