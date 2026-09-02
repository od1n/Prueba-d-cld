using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace SistemaGestion
{
    public class FrmMoldes : FormBase
    {
        private DataGridView dgv;
        private Label        _lblInfo;
        private string       _sortCol = "CodigoMold";
        private bool         _sortAsc = true;

        public FrmMoldes()
        {
            Text          = "Moldes";
            Size          = new Size(1050, 560);
            StartPosition = FormStartPosition.CenterParent;
            BackColor     = Color.FromArgb(240, 250, 255);

            var hdr = new Panel { Dock = DockStyle.Top, Height = 50,
                BackColor = Color.FromArgb(20, 100, 140) };
            hdr.Controls.Add(new Label { Text = "Tabla de Moldes",
                ForeColor = Color.White, Font = new Font("Segoe UI", 12f, FontStyle.Bold),
                AutoSize = true, Location = new Point(12, 13) });

            var btnN    = Btn("+ Nuevo",         Color.FromArgb(40, 160, 80),   new Point(420, 8));
            var btnProd = Btn("Productos Molde", Color.FromArgb(100, 60, 160),  new Point(518, 8), 130);
            var btnMant = Btn("Mantenimiento",   Color.FromArgb(20, 80, 120),   new Point(656, 8), 120);
            var btnE    = Btn("Editar",          Color.FromArgb(60, 100, 180),  new Point(784, 8));
            var btnD    = Btn("Eliminar",        Color.FromArgb(180, 60, 60),   new Point(882, 8));
            hdr.Controls.AddRange(new Control[] { btnN, btnProd, btnMant, btnE, btnD });

            dgv = new DataGridView {
                Name = "dgv",
                Dock = DockStyle.Fill, ReadOnly = true,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false, AllowUserToAddRows = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None,
                BackgroundColor = Color.White, BorderStyle = BorderStyle.None,
                RowHeadersVisible = false
            };
            dgv.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(20, 100, 140);
            dgv.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
            dgv.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 9f, FontStyle.Bold);
            dgv.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(235, 247, 255);
            dgv.EnableHeadersVisualStyles    = false;
            dgv.DataBindingComplete         += (s, e) => ColorearMantenimiento();
            dgv.ColumnHeaderMouseClick      += DgvColumnHeaderMouseClick;
            dgv.CellDoubleClick             += (s, e) => Editar();

            var footer = new Panel { Dock = DockStyle.Bottom, Height = 40,
                BackColor = Color.FromArgb(218, 238, 250) };
            _lblInfo = new Label { AutoSize = true, Location = new Point(12, 10),
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                ForeColor = Color.FromArgb(10, 70, 110) };
            var btnC = new Button { Text = "Cerrar", Size = new Size(90, 27),
                Location = new Point(940, 6), BackColor = Color.FromArgb(100, 100, 120),
                ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
            btnC.FlatAppearance.BorderSize = 0;
            btnC.Click += (s, e) => Close();
            footer.Controls.AddRange(new Control[] { _lblInfo, btnC });

            Controls.AddRange(new Control[] { dgv, hdr, footer });

            btnN.Click += (s, e) => {
                using var d = new DlgMolde(null);
                if (d.ShowDialog() == DialogResult.OK)
                {
                    if (DatabaseService.ObtenerMoldes().Any(
                        m => m.CodigoMold == d.Resultado.CodigoMold))
                    {
                        MessageBox.Show($"El codigo '{d.Resultado.CodigoMold}' ya existe.",
                            "Codigo duplicado", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }
                    DatabaseService.InsertarMolde(d.Resultado);
                    CargarDatos();
                }
            };
            btnProd.Click += (s, e) => ConfigurarProductosMolde();
            btnMant.Click += (s, e) => new FrmMantenimientoMoldes().ShowDialog();
            btnE.Click    += (s, e) => Editar();
            btnD.Click    += (s, e) => {
                var m = GetSel(); if (m == null) return;
                if (MessageBox.Show($"Eliminar molde {m.CodigoMold}?", "Confirmar",
                    MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes)
                { DatabaseService.EliminarMolde(m.CodigoMold); CargarDatos(); }
            };

            VentanaPrefs.Restaurar(this);
            CargarDatos();
        }

        private void Editar()
        {
            var m = GetSel(); if (m == null) return;
            using var d = new DlgMolde(m);
            if (d.ShowDialog() == DialogResult.OK)
            { DatabaseService.ActualizarMolde(d.Resultado); CargarDatos(); }
        }

        private void ConfigurarProductosMolde()
        {
            var m = GetSel();
            if (m == null)
            {
                MessageBox.Show("Selecciona un molde primero.", "Aviso",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            using var d = new DlgMoldeProductos(m.CodigoMold);
            if (d.ShowDialog() == DialogResult.OK)
            {
                DatabaseService.GuardarMoldeProductos(d.Resultado);
                MessageBox.Show(
                    $"Productos del molde {m.CodigoMold} guardados correctamente.",
                    "Guardado", MessageBoxButtons.OK, MessageBoxIcon.Information);
                CargarDatos();
            }
        }

        private void CargarDatos()
        {
            var lista = DatabaseService.ObtenerMoldes();
            var prods = DatabaseService.ObtenerTodosMoldeProductos()
                .ToDictionary(mp => mp.CodigoMolde, mp => mp);

            lista = _sortCol switch {
                "Descripcion"      => _sortAsc ? lista.OrderBy(m => m.Descripcion).ToList()
                                               : lista.OrderByDescending(m => m.Descripcion).ToList(),
                "Producto"         => _sortAsc ? lista.OrderBy(m => m.CodigoProducto).ToList()
                                               : lista.OrderByDescending(m => m.CodigoProducto).ToList(),
                "UltMant"          => _sortAsc ? lista.OrderBy(m => m.FechaLastMant).ToList()
                                               : lista.OrderByDescending(m => m.FechaLastMant).ToList(),
                "DiasDesdeUltMant" => _sortAsc
                    ? lista.OrderBy(m => (DateTime.Today-m.FechaLastMant).Days).ToList()
                    : lista.OrderByDescending(m => (DateTime.Today-m.FechaLastMant).Days).ToList(),
                _ => _sortAsc ? lista.OrderBy(m => m.CodigoMold).ToList()
                              : lista.OrderByDescending(m => m.CodigoMold).ToList()
            };

            dgv.DataSource = null;
            dgv.DataSource = lista.Select(m => {
                var mp = prods.GetValueOrDefault(m.CodigoMold);
                string prodInfo = mp != null
                    ? $"{mp.CodPrimera ?? "-"} | {mp.CodSegunda ?? "-"} | {mp.CodReciclado ?? "-"}"
                    : "⚠ Sin configurar";
                return new {
                    m.CodigoMold, m.Descripcion,
                    Productos        = prodInfo,
                    UltMant          = m.FechaLastMant.ToShortDateString(),
                    DiasDesdeUltMant = (DateTime.Today - m.FechaLastMant).Days,
                    m.DescMant, MatXUnid = m.CantMatXunid
                };
            }).ToList();
            VentanaPrefs.AplicarAnchos(this, dgv);

            foreach (DataGridViewColumn col in dgv.Columns)
                col.HeaderCell.SortGlyphDirection = SortOrder.None;
            if (dgv.Columns.Contains(_sortCol))
                dgv.Columns[_sortCol].HeaderCell.SortGlyphDirection =
                    _sortAsc ? SortOrder.Ascending : SortOrder.Descending;

            int vencidos = lista.Count(m => (DateTime.Today - m.FechaLastMant).Days > 180);
            int sinConf  = lista.Count(m => !prods.ContainsKey(m.CodigoMold));
            _lblInfo.Text = $"  Moldes: {lista.Count}   |   " +
                            $"Mant. vencido (>180 dias): {vencidos}   |   " +
                            $"Sin productos configurados: {sinConf}";
        }

        private void DgvColumnHeaderMouseClick(object sender,
            DataGridViewCellMouseEventArgs e)
        {
            string col = dgv.Columns[e.ColumnIndex].Name;
            var ok = new[] { "CodigoMold","Descripcion","Productos","UltMant","DiasDesdeUltMant" };
            if (!ok.Contains(col)) return;
            if (_sortCol == col) _sortAsc = !_sortAsc;
            else { _sortCol = col; _sortAsc = true; }
            CargarDatos();
        }

        private void ColorearMantenimiento()
        {
            foreach (DataGridViewRow row in dgv.Rows)
            {
                if (row.Cells["DiasDesdeUltMant"].Value == null) continue;
                int dias = Convert.ToInt32(row.Cells["DiasDesdeUltMant"].Value);
                if (dias > 180)     row.DefaultCellStyle.BackColor = Color.FromArgb(255, 210, 210);
                else if (dias > 90) row.DefaultCellStyle.BackColor = Color.FromArgb(255, 245, 200);

                // Resaltar moldes sin productos configurados
                if (dgv.Columns.Contains("Productos") &&
                    row.Cells["Productos"].Value?.ToString().StartsWith("⚠") == true)
                    row.Cells["Productos"].Style.ForeColor = Color.FromArgb(180, 60, 0);
            }
        }

        private TbMolde GetSel()
        {
            if (dgv.CurrentRow == null) return null;
            var cod = dgv.CurrentRow.Cells["CodigoMold"].Value?.ToString();
            return DatabaseService.ObtenerMoldes().Find(x => x.CodigoMold == cod);
        }

        private Button Btn(string t, Color c, Point p, int w = 90)
        {
            var b = new Button { Text = t, Size = new Size(w, 33), Location = p,
                BackColor = c, ForeColor = Color.White, FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 8.5f, FontStyle.Bold), Cursor = Cursors.Hand };
            b.FlatAppearance.BorderSize = 0; return b;
        }
    }

    // ════════════════════════════════════════════════════════════════════════
    //  DIALOGO: CONFIGURAR PRODUCTOS DEL MOLDE
    // ════════════════════════════════════════════════════════════════════════
    public class DlgMoldeProductos : FormBase
    {
        public MoldeProductos Resultado { get; private set; }

        private readonly string _codigoMolde;
        private ComboBox cmbPrimera, cmbSegunda, cmbReciclado;

        public DlgMoldeProductos(string codigoMolde)
        {
            _codigoMolde    = codigoMolde;
            Text            = $"Productos del Molde: {codigoMolde}";
            Size            = new Size(480, 340);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = MinimizeBox = false;
            StartPosition   = FormStartPosition.CenterParent;
            BackColor       = Color.FromArgb(245, 242, 255);

            var hdr = new Panel { Dock = DockStyle.Top, Height = 44,
                BackColor = Color.FromArgb(100, 60, 160) };
            hdr.Controls.Add(new Label {
                Text = $"Configurar productos para molde {codigoMolde}",
                ForeColor = Color.White, AutoSize = true,
                Font = new Font("Segoe UI", 10f, FontStyle.Bold),
                Location = new Point(10, 12) });

            var inv = DatabaseService.ObtenerInventario()
                .OrderBy(i => i.Codigo, System.StringComparer.Ordinal).ToList();
            int y   = 60;

            // Helper local
            ComboBox CmbProd(string label, int ypos, string color)
            {
                Controls.Add(new Label { Text = label, Location = new Point(20, ypos + 3),
                    AutoSize = true, Font = new Font("Segoe UI", 9f, FontStyle.Bold) });
                var cmb = new ComboBox { Location = new Point(180, ypos),
                    Size = new Size(270, 26), DropDownStyle = ComboBoxStyle.DropDownList };
                cmb.Items.Add("-- No aplica --");
                inv.ForEach(i => cmb.Items.Add(i.Codigo + " — " + i.Descripcion));
                cmb.SelectedIndex = 0;
                UiSearch.Enable(cmb);
                Controls.Add(cmb);
                return cmb;
            }

            Controls.Add(new Label {
                Text = "Material VIRGEN:",
                Location = new Point(20, y - 20), AutoSize = true,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                ForeColor = Color.FromArgb(20, 100, 60) });

            cmbPrimera  = CmbProd("Producto Primera (ej: 101202):",    y,      "#green"); y += 40;
            cmbSegunda  = CmbProd("Producto Segunda (ej: 101202S):",   y,      "#orange"); y += 50;

            Controls.Add(new Label {
                Text = "Material RECICLADO:",
                Location = new Point(20, y - 10), AutoSize = true,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                ForeColor = Color.FromArgb(100, 60, 20) });

            cmbReciclado = CmbProd("Producto Reciclado (ej: 101202R):", y + 10, "#brown"); y += 60;

            // Pre-rellenar si ya existe configuracion
            var actual = DatabaseService.ObtenerMoldeProductos(codigoMolde);
            if (actual != null)
            {
                Seleccionar(cmbPrimera,   actual.CodPrimera);
                Seleccionar(cmbSegunda,   actual.CodSegunda);
                Seleccionar(cmbReciclado, actual.CodReciclado);
            }

            var bOk = new Button {
                Text = "Guardar", Location = new Point(130, y + 10), Size = new Size(120, 32),
                BackColor = Color.FromArgb(100, 60, 160), ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                DialogResult = DialogResult.OK };
            bOk.FlatAppearance.BorderSize = 0;
            bOk.Click += (s, e) => {
                Resultado = new MoldeProductos {
                    CodigoMolde  = _codigoMolde,
                    CodPrimera   = ObtenerCod(cmbPrimera),
                    CodSegunda   = ObtenerCod(cmbSegunda),
                    CodReciclado = ObtenerCod(cmbReciclado)
                };
                // Validar que al menos uno este configurado
                if (Resultado.CodPrimera == null && Resultado.CodSegunda == null
                    && Resultado.CodReciclado == null)
                {
                    MessageBox.Show("Configura al menos un producto.",
                        "Validacion", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    DialogResult = DialogResult.None;
                }
            };

            var bCan = new Button {
                Text = "Cancelar", Location = new Point(265, y + 10), Size = new Size(100, 32),
                BackColor = Color.FromArgb(100,100,120), ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat, DialogResult = DialogResult.Cancel };
            bCan.FlatAppearance.BorderSize = 0;

            Controls.AddRange(new Control[] { hdr, bOk, bCan });
            AcceptButton = bOk; CancelButton = bCan;
        }

        private void Seleccionar(ComboBox cmb, string cod)
        {
            if (string.IsNullOrWhiteSpace(cod)) return;
            for (int i = 0; i < cmb.Items.Count; i++)
                if (cmb.Items[i].ToString().StartsWith(cod + " —"))
                { cmb.SelectedIndex = i; return; }
        }

        private string ObtenerCod(ComboBox cmb) => UiSearch.CodigoDe(cmb);
    }

    // ════════════════════════════════════════════════════════════════════════
    //  DIALOGO MOLDE (sin cambios estructurales)
    // ════════════════════════════════════════════════════════════════════════
    public class DlgMolde : FormBase
    {
        public TbMolde Resultado { get; private set; }

        private TextBox        txtCod, txtDesc, txtDescMant, txtObs;
        private ComboBox       cmbProducto;
        private DateTimePicker dtpFecha, dtpLastMant;
        private NumericUpDown  numCantMat;
        private Label          lblSugerencia;
        private readonly TbMolde _original;

        public DlgMolde(TbMolde m)
        {
            _original = m;
            Text      = m == null ? "Nuevo Molde" : "Editar Molde";
            Size      = new Size(480, 460);
            StartPosition   = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = MinimizeBox = false;
            BackColor   = Color.FromArgb(240, 250, 255);

            int y = 12;
            L("Codigo Molde:", y);
            txtCod = new TextBox { Location = new Point(155, y), Size = new Size(200, 24) };
            Controls.Add(txtCod);
            lblSugerencia = new Label { Location = new Point(362, y + 4),
                AutoSize = true, Font = new Font("Segoe UI", 7.5f),
                ForeColor = Color.FromArgb(80, 100, 140) };
            Controls.Add(lblSugerencia); y += 33;

            L("Producto base:", y);
            cmbProducto = new ComboBox { Location = new Point(155, y),
                Size = new Size(285, 24), DropDownStyle = ComboBoxStyle.DropDownList };
            cmbProducto.Items.Add("-- Sin producto --");
            DatabaseService.ObtenerInventario()
                .OrderBy(i => i.Codigo, System.StringComparer.Ordinal).ToList()
                .ForEach(i => cmbProducto.Items.Add(i.Codigo + " — " + i.Descripcion));
            cmbProducto.SelectedIndex = 0;
            UiSearch.Enable(cmbProducto);
            cmbProducto.SelectedIndexChanged += ActualizarSugerencia;
            cmbProducto.TextChanged          += ActualizarSugerencia;
            Controls.Add(cmbProducto); y += 33;

            L("Descripcion:",   y); txtDesc     = T(y); y += 33;
            L("Fecha:",         y); dtpFecha    = D(DateTime.Today, y); y += 33;
            L("Ult. Mant.:",    y); dtpLastMant = D(DateTime.Today, y); y += 33;
            L("Desc. Mant.:",   y); txtDescMant = T(y); y += 33;
            L("Mat x Unidad:",  y);
            numCantMat = new NumericUpDown { Location = new Point(155, y),
                Size = new Size(285, 24), Maximum = 9999, DecimalPlaces = 2 };
            Controls.Add(numCantMat); y += 33;
            L("Observaciones:", y);
            txtObs = new TextBox { Location = new Point(155, y),
                Size = new Size(285, 45), Multiline = true };
            Controls.Add(txtObs); y += 55;

            if (m != null)
            {
                txtCod.Text = m.CodigoMold; txtCod.ReadOnly = true;
                for (int i = 0; i < cmbProducto.Items.Count; i++)
                    if (cmbProducto.Items[i].ToString()
                        .StartsWith((m.CodigoProducto ?? "") + " —"))
                    { cmbProducto.SelectedIndex = i; break; }
                txtDesc.Text = m.Descripcion; dtpFecha.Value = m.Fecha;
                dtpLastMant.Value = m.FechaLastMant; txtDescMant.Text = m.DescMant;
                numCantMat.Value = m.CantMatXunid; txtObs.Text = m.Observaciones;
            }

            var bOk  = BB("Guardar",  Color.FromArgb(20, 100, 140), new Point(80,  y));
            var bCan = BB("Cancelar", Color.FromArgb(160, 50, 50),  new Point(240, y));
            bCan.DialogResult = DialogResult.Cancel;
            bOk.Click += BtnOk_Click;
            Controls.AddRange(new Control[] { bOk, bCan });

            foreach (var tb in new TextBox[] { txtCod, txtDesc, txtDescMant, txtObs })
                tb.Enter += (s, e) => ((TextBox)s).SelectAll();
            numCantMat.Enter += (s, e) => numCantMat.Select(0, numCantMat.Text.Length);

            ActualizarSugerencia(null, null);
        }

        private void ActualizarSugerencia(object sender, EventArgs e)
        {
            if (_original != null) { lblSugerencia.Text = ""; return; }
            string codProd = UiSearch.CodigoDe(cmbProducto);
            if (codProd == null) { lblSugerencia.Text = ""; return; }
            int count = DatabaseService.ObtenerMoldes()
                .Count(m => m.CodigoProducto == codProd) + 1;
            string sug = "M" + codProd + count.ToString("D3");
            lblSugerencia.Text = $"Sugerido: {sug}";
            if (string.IsNullOrWhiteSpace(txtCod.Text)) txtCod.Text = sug;
        }

        private void BtnOk_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtCod.Text))
            { Warn("El codigo del molde es obligatorio."); return; }

            string codProd = UiSearch.CodigoDe(cmbProducto) ?? "";

            Resultado = _original ?? new TbMolde();
            Resultado.CodigoMold     = txtCod.Text.Trim();
            Resultado.CodigoProducto = codProd;
            Resultado.ModeloQfabrica = codProd;
            Resultado.Descripcion    = txtDesc.Text.Trim();
            Resultado.Fecha          = dtpFecha.Value;
            Resultado.FechaLastMant  = dtpLastMant.Value;
            Resultado.DescMant       = txtDescMant.Text.Trim();
            Resultado.CantMatXunid   = numCantMat.Value;
            Resultado.Observaciones  = txtObs.Text.Trim();
            DialogResult = DialogResult.OK;
        }

        private void Warn(string msg) =>
            MessageBox.Show(msg, "Validacion", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        private void L(string t, int y) =>
            Controls.Add(new Label { Text = t, Location = new Point(12, y + 3),
                AutoSize = true, Font = new Font("Segoe UI", 9f, FontStyle.Bold) });
        private TextBox T(int y)
        {
            var tb = new TextBox { Location = new Point(155, y), Size = new Size(285, 24) };
            Controls.Add(tb); return tb;
        }
        private DateTimePicker D(DateTime v, int y)
        {
            var dp = new DateTimePicker { Value = v, Location = new Point(155, y),
                Size = new Size(285, 24), Format = DateTimePickerFormat.Short };
            Controls.Add(dp); return dp;
        }
        private Button BB(string t, Color c, Point p)
        {
            var b = new Button { Text = t, Location = p, Size = new Size(130, 32),
                BackColor = c, ForeColor = Color.White, FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold) };
            b.FlatAppearance.BorderSize = 0; Controls.Add(b); return b;
        }
    }
}
