// ════════════════════════════════════════════════════════════════════════
// ARCHIVO: FrmTarifasBonoProduccion.cs  →  Forms\  (reemplaza version anterior)
// Tabs: Bono Produccion | Bono Ventas
// Solo accesible para administradores
// ════════════════════════════════════════════════════════════════════════

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace SistemaGestion
{
    public class FrmTarifasBonoProduccion : FormBase
    {
        public FrmTarifasBonoProduccion()
        {
            Text          = "Tarifas de Bono";
            Size          = new Size(940, 620);
            MinimumSize   = new Size(800, 500);
            StartPosition = FormStartPosition.CenterParent;
            BackColor     = Color.FromArgb(245, 247, 252);

            var hdr = new Panel { Dock = DockStyle.Top, Height = 50,
                BackColor = Color.FromArgb(60, 40, 100) };
            hdr.Controls.Add(new Label {
                Text = "Configuracion de Tarifas de Bono  (solo Admin)",
                Font = new Font("Segoe UI", 12f, FontStyle.Bold),
                ForeColor = Color.White, AutoSize = true, Location = new Point(14, 13) });

            var tabs = new TabControl { Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 10f) };
            tabs.TabPages.Add(CrearTab(
                "Bono Produccion",
                Color.FromArgb(100, 30, 100),
                "Bs por unidad producida — aplica a empleados tipo Produccion",
                esVentas: false));
            tabs.TabPages.Add(CrearTab(
                "Bono Ventas",
                Color.FromArgb(30, 80, 160),
                "Bs por unidad vendida — aplica a empleados tipo Oficina/Ventas",
                esVentas: true));

            var footer = new Panel { Dock = DockStyle.Bottom, Height = 40,
                BackColor = Color.FromArgb(220, 215, 235) };
            var btnCerrar = new Button { Text = "Cerrar", Size = new Size(90, 26),
                Location = new Point(820, 7), BackColor = Color.FromArgb(100,100,120),
                ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
            btnCerrar.FlatAppearance.BorderSize = 0;
            btnCerrar.Click += (s, e) => Close();
            footer.Controls.Add(btnCerrar);

            Controls.AddRange(new Control[] { tabs, footer, hdr });
        }

        private TabPage CrearTab(string titulo, Color color,
            string descripcion, bool esVentas)
        {
            var tab = new TabPage(titulo);

            // Variables locales capturadas por las lambdas
            List<TarifaBono> datos = new();
            DataGridView dgv = null;
            ComboBox cmbFiltro = null;
            CheckBox chkVig = null;


            string sortCol = "Empleado";
            bool   sortAsc = true;
            void Cargar()
            {
                int idEmp = cmbFiltro.SelectedIndex <= 0 ? 0
                    : int.Parse(cmbFiltro.SelectedItem.ToString().Split('—')[0].Trim());

                datos = esVentas
                    ? DatabaseService.ObtenerTarifasBonoVentas(chkVig.Checked)
                    : DatabaseService.ObtenerTarifasBono(chkVig.Checked);

                if (idEmp > 0)
                    datos = datos.Where(t => t.IdEmpleado == idEmp).ToList();

                // Ordenar
                datos = sortCol switch {
                    "Producto" => sortAsc
                        ? datos.OrderBy(t => t.DescProducto).ToList()
                        : datos.OrderByDescending(t => t.DescProducto).ToList(),
                    "Tarifa" => sortAsc
                        ? datos.OrderBy(t => t.TarifaBs).ToList()
                        : datos.OrderByDescending(t => t.TarifaBs).ToList(),
                    "Desde" => sortAsc
                        ? datos.OrderBy(t => t.FechaDesde).ToList()
                        : datos.OrderByDescending(t => t.FechaDesde).ToList(),
                    "Estado" => sortAsc
                        ? datos.OrderBy(t => t.Vigente).ToList()
                        : datos.OrderByDescending(t => t.Vigente).ToList(),
                    _ => sortAsc
                        ? datos.OrderBy(t => t.NombreEmpleado).ToList()
                        : datos.OrderByDescending(t => t.NombreEmpleado).ToList()
                };

                dgv.DataSource = datos.Select(t => new {
                    t.Id,
                    Empleado  = t.NombreEmpleado,
                    Producto  = t.CodigoProducto + " — " + t.DescProducto,
                    Tarifa    = t.TarifaBs.ToString("N2") + " Bs/und",
                    Desde     = t.FechaDesde.ToString("dd/MM/yyyy"),
                    Hasta     = t.FechaHasta.HasValue
                                ? t.FechaHasta.Value.ToString("dd/MM/yyyy") : "Vigente",
                    Estado    = t.Vigente ? "Activa" : "Cerrada"
                }).ToList();
                VentanaPrefs.AplicarAnchos(this, dgv);

                if (dgv.Columns.Contains("Id"))
                    dgv.Columns["Id"].Visible = false;

                // Colorear estado
                for (int i = 0; i < dgv.Rows.Count && i < datos.Count; i++)
                {
                    if (!datos[i].Vigente)
                        dgv.Rows[i].DefaultCellStyle.ForeColor = Color.FromArgb(160,160,170);
                    else if (dgv.Columns.Contains("Estado"))
                        dgv.Rows[i].Cells["Estado"].Style.ForeColor = Color.FromArgb(20,120,50);
                }
            }

            // ── Barra de filtros ──────────────────────────────────────────
            var bar = new Panel { Dock = DockStyle.Top, Height = 48,
                BackColor = Color.FromArgb(235, 230, 248) };

            bar.Controls.Add(new Label { Text = "Empleado:", Location = new Point(8,14),
                AutoSize = true, Font = new Font("Segoe UI", 9f) });
            cmbFiltro = new ComboBox { Location = new Point(80, 10),
                Size = new Size(220, 26), DropDownStyle = ComboBoxStyle.DropDownList };
            cmbFiltro.Items.Add("Todos");

            var tipoEmpleado = esVentas ? "Oficina" : "Produccion";
            DatabaseService.ObtenerEmpleados()
                .Where(e => e.Activo)
                .ToList()
                .ForEach(e => cmbFiltro.Items.Add(e.Id + " — " + e.NombreCompleto));
            cmbFiltro.SelectedIndex = 0;
            cmbFiltro.SelectedIndexChanged += (s, e) => Cargar();
            bar.Controls.Add(cmbFiltro);

            chkVig = new CheckBox { Text = "Solo vigentes", Location = new Point(314, 13),
                AutoSize = true, Font = new Font("Segoe UI", 9f), Checked = true };
            chkVig.CheckedChanged += (s, e) => Cargar();
            bar.Controls.Add(chkVig);

            var btnNuevo  = BtnBar("+ Nueva",    color,                      new Point(440, 9));
            var btnEditar = BtnBar("Editar",      Color.FromArgb(30,80,160), new Point(536, 9));
            var btnCierra = BtnBar("Cerrar tar.", Color.FromArgb(160,100,20),new Point(632, 9));
            var btnElim   = BtnBar("Eliminar",    Color.FromArgb(180,50,50), new Point(740, 9));
            bar.Controls.AddRange(new Control[] { btnNuevo, btnEditar, btnCierra, btnElim });

            // ── Grid ──────────────────────────────────────────────────────
            // Sort state per tab

            dgv = new DataGridView {
                Name = "dgv",
                Dock = DockStyle.Fill, ReadOnly = true,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false, AllowUserToAddRows = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None,
                BackgroundColor = Color.White, BorderStyle = BorderStyle.None,
                RowHeadersVisible = false, Font = new Font("Segoe UI", 9f),
                RowTemplate = { Height = 24 }
            };
            dgv.ColumnHeadersDefaultCellStyle.BackColor = color;
            dgv.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
            dgv.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 9f, FontStyle.Bold);
            dgv.EnableHeadersVisualStyles = false;
            dgv.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(248, 245, 255);
            dgv.CellDoubleClick += (s, e) => {
                if (dgv.CurrentRow == null || dgv.CurrentRow.Index >= datos.Count) return;
                using var dlg = new DlgTarifaBono(datos[dgv.CurrentRow.Index], esVentas);
                if (dlg.ShowDialog() == DialogResult.OK)
                {
                    if (esVentas) DatabaseService.ActualizarTarifaBonoVentas(dlg.Resultado);
                    else          DatabaseService.ActualizarTarifaBono(dlg.Resultado);
                    Cargar();
                }
            };

            // ── Footer info ───────────────────────────────────────────────
            var footerTab = new Panel { Dock = DockStyle.Bottom, Height = 28,
                BackColor = Color.FromArgb(235, 230, 248) };
            var lblInfo = new Label { AutoSize = true, Location = new Point(10, 6),
                Font = new Font("Segoe UI", 8f), ForeColor = Color.FromArgb(60,40,100) };
            footerTab.Controls.Add(lblInfo);
            dgv.ColumnHeaderMouseClick += (s, e) => {
                string col = dgv.Columns[e.ColumnIndex].Name;
                var ok = new[] { "Empleado","Producto","Tarifa","Desde","Hasta","Estado" };
                if (!ok.Contains(col)) return;
                if (sortCol == col) sortAsc = !sortAsc;
                else { sortCol = col; sortAsc = true; }
                Cargar();
            };
            dgv.DataBindingComplete += (s, e) => {
                foreach (DataGridViewColumn col in dgv.Columns)
                    col.HeaderCell.SortGlyphDirection = SortOrder.None;
                if (dgv.Columns.Contains(sortCol))
                    dgv.Columns[sortCol].HeaderCell.SortGlyphDirection =
                        sortAsc ? SortOrder.Ascending : SortOrder.Descending;
                lblInfo.Text = $"  {descripcion}   |   {datos.Count} tarifas   |   " +
                    $"{datos.Count(t => t.Vigente)} vigentes";
            };

            // Eventos botones
            btnNuevo.Click += (s, e) => {
                using var dlg = new DlgTarifaBono(null, esVentas);
                if (dlg.ShowDialog() == DialogResult.OK)
                {
                    if (esVentas) DatabaseService.InsertarTarifaBonoVentas(dlg.Resultado);
                    else          DatabaseService.InsertarTarifaBono(dlg.Resultado);
                    Cargar();
                }
            };
            btnEditar.Click += (s, e) => {
                if (dgv.CurrentRow == null || dgv.CurrentRow.Index >= datos.Count) return;
                using var dlg = new DlgTarifaBono(datos[dgv.CurrentRow.Index], esVentas);
                if (dlg.ShowDialog() == DialogResult.OK)
                {
                    if (esVentas) DatabaseService.ActualizarTarifaBonoVentas(dlg.Resultado);
                    else          DatabaseService.ActualizarTarifaBono(dlg.Resultado);
                    Cargar();
                }
            };
            btnCierra.Click += (s, e) => {
                if (dgv.CurrentRow == null || dgv.CurrentRow.Index >= datos.Count) return;
                var item = datos[dgv.CurrentRow.Index];
                if (!item.Vigente) {
                    MessageBox.Show("Esta tarifa ya esta cerrada.", "Aviso",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }
                if (MessageBox.Show(
                    $"Cerrar tarifa de {item.NombreEmpleado} para {item.DescProducto}?",
                    "Confirmar", MessageBoxButtons.YesNo, MessageBoxIcon.Question)
                    == DialogResult.Yes)
                {
                    if (esVentas) DatabaseService.CerrarTarifaBonoVentas(item.Id);
                    else          DatabaseService.CerrarTarifaBono(item.Id);
                    Cargar();
                }
            };
            btnElim.Click += (s, e) => {
                if (dgv.CurrentRow == null || dgv.CurrentRow.Index >= datos.Count) return;
                var item = datos[dgv.CurrentRow.Index];
                if (MessageBox.Show(
                    $"Eliminar tarifa de {item.NombreEmpleado}?",
                    "Confirmar", MessageBoxButtons.YesNo, MessageBoxIcon.Warning)
                    == DialogResult.Yes)
                {
                    if (esVentas) DatabaseService.EliminarTarifaBonoVentas(item.Id);
                    else          DatabaseService.EliminarTarifaBono(item.Id);
                    Cargar();
                }
            };

            tab.Controls.Add(dgv);
            tab.Controls.Add(footerTab);
            tab.Controls.Add(bar);

            // Cargar al mostrar el tab
            tab.Enter += (s, e) => Cargar();

            return tab;
        }

        private Button BtnBar(string txt, Color bg, Point loc) => new Button {
            Text = txt, Location = loc, Size = new Size(90, 28),
            BackColor = bg, ForeColor = Color.White, FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 8f, FontStyle.Bold), Cursor = Cursors.Hand
        };
    }

    // ════════════════════════════════════════════════════════════════════════
    //  DIALOGO — compartido para produccion y ventas
    // ════════════════════════════════════════════════════════════════════════
    public class DlgTarifaBono : FormBase
    {
        public TarifaBono Resultado { get; private set; }

        private ComboBox       cmbEmpleado, cmbProducto;
        private NumericUpDown  nudTarifa;
        private DateTimePicker dtpDesde;
        private CheckBox       chkSinVenc;
        private DateTimePicker dtpHasta;
        private Label          lblPreview;

        private readonly TarifaBono _editar;
        private readonly bool       _esVentas;

        public DlgTarifaBono(TarifaBono editar, bool esVentas)
        {
            _editar   = editar;
            _esVentas = esVentas;
            Text      = (editar == null ? "Nueva" : "Editar") +
                        (esVentas ? " Tarifa Bono Ventas" : " Tarifa Bono Produccion");
            Size            = new Size(480, 420);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox     = false; MinimizeBox = false;
            StartPosition   = FormStartPosition.CenterParent;
            BackColor       = Color.FromArgb(248, 245, 252);

            BuildUI();
            if (editar != null) RellenarCampos();
        }

        private void BuildUI()
        {
            int y = 20;
            Add(L("Empleado:", 20, y + 3));
            cmbEmpleado = new ComboBox { Location = new Point(150, y),
                Size = new Size(300, 26), DropDownStyle = ComboBoxStyle.DropDownList };
            // Para ventas mostrar todos los activos, para produccion solo tipo Produccion
            var empleados = DatabaseService.ObtenerEmpleados().Where(e => e.Activo).ToList();
            empleados.ForEach(e => cmbEmpleado.Items.Add(e.Id + " — " + e.NombreCompleto));
            cmbEmpleado.SelectedIndexChanged += Preview;
            Add(cmbEmpleado); y += 36;

            Add(L("Producto:", 20, y + 3));
            cmbProducto = new ComboBox { Location = new Point(150, y),
                Size = new Size(300, 26), DropDownStyle = ComboBoxStyle.DropDownList };
            DatabaseService.ObtenerInventario()
                .ForEach(i => cmbProducto.Items.Add(i.Codigo + " — " + i.Descripcion));
            cmbProducto.SelectedIndexChanged += Preview;
            Add(cmbProducto); y += 36;

            Add(L("Tarifa (Bs/und):", 20, y + 3));
            nudTarifa = new NumericUpDown { Location = new Point(150, y),
                Size = new Size(140, 26), DecimalPlaces = 2,
                Minimum = 0, Maximum = 9999999, ThousandsSeparator = true };
            nudTarifa.ValueChanged += Preview;
            Add(nudTarifa); y += 36;

            Add(L("Vigente desde:", 20, y + 3));
            dtpDesde = new DateTimePicker { Location = new Point(150, y),
                Size = new Size(130, 26), Format = DateTimePickerFormat.Short,
                Value = DateTime.Today };
            Add(dtpDesde); y += 36;

            chkSinVenc = new CheckBox { Text = "Sin fecha de vencimiento",
                Location = new Point(150, y), AutoSize = true,
                Font = new Font("Segoe UI", 9f), Checked = true };
            chkSinVenc.CheckedChanged += (s, e) => dtpHasta.Enabled = !chkSinVenc.Checked;
            Add(chkSinVenc); y += 30;

            Add(L("Vence:", 20, y + 3));
            dtpHasta = new DateTimePicker { Location = new Point(150, y),
                Size = new Size(130, 26), Format = DateTimePickerFormat.Short,
                Value = DateTime.Today.AddYears(1), Enabled = false };
            Add(dtpHasta); y += 36;

            lblPreview = new Label { Location = new Point(20, y),
                Size = new Size(430, 40),
                Font = new Font("Segoe UI", 8.5f, FontStyle.Italic),
                ForeColor = Color.FromArgb(60, 80, 60),
                BackColor = Color.FromArgb(240, 252, 240),
                BorderStyle = BorderStyle.FixedSingle };
            Add(lblPreview); y += 48;

            var btnOk = new Button { Text = "Guardar", DialogResult = DialogResult.OK,
                Location = new Point(250, y), Size = new Size(90, 30),
                BackColor = Color.FromArgb(60, 40, 100), ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold) };
            btnOk.FlatAppearance.BorderSize = 0;
            btnOk.Click += BtnOk_Click;

            var btnCancelar = new Button { Text = "Cancelar",
                DialogResult = DialogResult.Cancel,
                Location = new Point(350, y), Size = new Size(90, 30),
                BackColor = Color.FromArgb(100,100,120), ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat };
            btnCancelar.FlatAppearance.BorderSize = 0;
            Add(btnOk); Add(btnCancelar);
            AcceptButton = btnOk; CancelButton = btnCancelar;
        }

        private void Preview(object sender, EventArgs e)
        {
            if (nudTarifa.Value > 0 && cmbProducto.SelectedIndex >= 0
                                    && cmbEmpleado.SelectedIndex >= 0)
            {
                string tipo = _esVentas ? "vendida" : "producida";
                lblPreview.Text =
                    $"  Por cada unidad {tipo}, el empleado recibira\n" +
                    $"  {nudTarifa.Value:N2} Bs adicionales en nomina.";
            }
            else lblPreview.Text = "";
        }

        private void RellenarCampos()
        {
            for (int i = 0; i < cmbEmpleado.Items.Count; i++)
                if (cmbEmpleado.Items[i].ToString().StartsWith(_editar.IdEmpleado + " —"))
                { cmbEmpleado.SelectedIndex = i; break; }

            for (int i = 0; i < cmbProducto.Items.Count; i++)
                if (cmbProducto.Items[i].ToString().StartsWith(_editar.CodigoProducto + " —"))
                { cmbProducto.SelectedIndex = i; break; }

            nudTarifa.Value           = _editar.TarifaBs;
            dtpDesde.Value            = _editar.FechaDesde;
            chkSinVenc.Checked        = !_editar.FechaHasta.HasValue;
            dtpHasta.Enabled          = _editar.FechaHasta.HasValue;
            if (_editar.FechaHasta.HasValue) dtpHasta.Value = _editar.FechaHasta.Value;
        }

        private void BtnOk_Click(object sender, EventArgs e)
        {
            if (cmbEmpleado.SelectedIndex < 0) {
                MessageBox.Show("Selecciona un empleado.", "Validacion",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                DialogResult = DialogResult.None; return;
            }
            if (cmbProducto.SelectedIndex < 0) {
                MessageBox.Show("Selecciona un producto.", "Validacion",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                DialogResult = DialogResult.None; return;
            }
            if (nudTarifa.Value <= 0) {
                MessageBox.Show("La tarifa debe ser mayor a 0.", "Validacion",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                DialogResult = DialogResult.None; return;
            }

            int    idEmp = int.Parse(cmbEmpleado.SelectedItem.ToString().Split('—')[0].Trim());
            string cod   = cmbProducto.SelectedItem.ToString().Split('—')[0].Trim();

            Resultado = new TarifaBono {
                Id             = _editar?.Id ?? 0,
                IdEmpleado     = idEmp,
                CodigoProducto = cod,
                TarifaBs       = nudTarifa.Value,
                FechaDesde     = dtpDesde.Value,
                FechaHasta     = chkSinVenc.Checked ? (DateTime?)null : dtpHasta.Value
            };
        }

        private void Add(Control c) => Controls.Add(c);
        private Label L(string t, int x, int y) =>
            new Label { Text = t, Location = new Point(x, y), AutoSize = true,
                Font = new Font("Segoe UI", 9f) };
    }
}
