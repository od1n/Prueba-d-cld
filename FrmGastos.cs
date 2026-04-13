using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace SistemaGestion
{
    public class FrmGastos : Form
    {
        private DataGridView dgv;
        private Label lblTotal;

        public FrmGastos()
        {
            Text = "Gastos";
            Size = new Size(800, 520);
            StartPosition = FormStartPosition.CenterParent;
            BackColor = Color.FromArgb(245, 250, 245);

            var toolbar = new Panel { Dock = DockStyle.Top, Height = 50, BackColor = Color.FromArgb(40, 140, 80) };
            toolbar.Controls.Add(new Label { Text = "Gestion de Gastos", ForeColor = Color.White,
                Font = new Font("Segoe UI", 12f, FontStyle.Bold), AutoSize = true, Location = new Point(12, 13) });

            var btnNuevo  = BtnTool("+ Nuevo",   Color.FromArgb(30, 110, 60),  new Point(530, 8));
            var btnEditar = BtnTool("Editar",    Color.FromArgb(180, 140, 20), new Point(625, 8));
            var btnElim   = BtnTool("Eliminar",  Color.FromArgb(180, 60, 60),  new Point(718, 8));
            toolbar.Controls.AddRange(new Control[] { btnNuevo, btnEditar, btnElim });

            dgv = new DataGridView
            {
                Dock = DockStyle.Fill, ReadOnly = true,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false, AllowUserToAddRows = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                BackgroundColor = Color.White, BorderStyle = BorderStyle.None,
                RowHeadersVisible = false, Font = new Font("Segoe UI", 9f)
            };
            dgv.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 9f, FontStyle.Bold);
            dgv.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(40, 140, 80);
            dgv.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
            dgv.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(235, 250, 240);
            dgv.EnableHeadersVisualStyles = false;

            var footer = new Panel { Dock = DockStyle.Bottom, Height = 45, BackColor = Color.FromArgb(215, 235, 220) };
            lblTotal = new Label { AutoSize = true, Location = new Point(12, 12),
                Font = new Font("Segoe UI", 10f, FontStyle.Bold), ForeColor = Color.FromArgb(30, 90, 50) };
            var btnCerrar = new Button { Text = "Cerrar", Size = new Size(90, 28), Location = new Point(680, 8),
                BackColor = Color.FromArgb(100, 100, 120), ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
            btnCerrar.FlatAppearance.BorderSize = 0;
            btnCerrar.Click += (s, e) => Close();
            footer.Controls.AddRange(new Control[] { lblTotal, btnCerrar });

            Controls.AddRange(new Control[] { dgv, toolbar, footer });

            btnNuevo.Click += (s, e) =>
            {
                using (var d = new DlgGasto(null))
                {
                    if (d.ShowDialog() == DialogResult.OK)
                    {
                        DatabaseService.InsertarGasto(d.Resultado);
                        CargarDatos();
                    }
                }
            };
            btnEditar.Click += (s, e) =>
            {
                if (dgv.CurrentRow == null) return;
                int id = (int)dgv.CurrentRow.Cells["Id"].Value;
                var lista = DatabaseService.ObtenerGastos();
                var g = lista.Find(x => x.Id == id);
                if (g == null) return;
                using (var d = new DlgGasto(g))
                {
                    if (d.ShowDialog() == DialogResult.OK)
                    {
                        DatabaseService.ActualizarGasto(d.Resultado);
                        CargarDatos();
                    }
                }
            };
            btnElim.Click += (s, e) =>
            {
                if (dgv.CurrentRow == null) return;
                int id = (int)dgv.CurrentRow.Cells["Id"].Value;
                if (MessageBox.Show($"Eliminar gasto #{id}?", "Confirmar",
                    MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes)
                {
                    DatabaseService.EliminarGasto(id);
                    CargarDatos();
                }
            };

            CargarDatos();
        }

        private Button BtnTool(string t, Color c, Point p)
        {
            var btn = new Button { Text = t, Size = new Size(85, 33), Location = p, BackColor = c,
                ForeColor = Color.White, FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 8f, FontStyle.Bold), Cursor = Cursors.Hand };
            btn.FlatAppearance.BorderSize = 0;
            return btn;
        }

        private void CargarDatos()
        {
            var lista = DatabaseService.ObtenerGastos();
            dgv.DataSource = null;
            dgv.DataSource = lista.Select(g => new
            {
                g.Id, g.Categoria, g.Descripcion,
                Monto = g.Monto.ToString("C2"),
                Fecha = g.Fecha.ToShortDateString(),
                g.Proveedor, g.Estado
            }).ToList();

            decimal total     = lista.Sum(g => g.Monto);
            decimal pagados   = lista.Where(g => g.Estado == "Pagado").Sum(g => g.Monto);
            decimal pendiente = lista.Where(g => g.Estado == "Pendiente").Sum(g => g.Monto);
            lblTotal.Text = $"  Total: {total:C2}   |   Pagados: {pagados:C2}   |   Pendientes: {pendiente:C2}";
        }
    }

    // ── Dialogo Gasto ─────────────────────────────────────────────────────────
    public class DlgGasto : Form
    {
        public Gasto Resultado { get; private set; }
        private ComboBox cmbCategoria, cmbEstado;
        private TextBox txtDescripcion, txtProveedor;
        private NumericUpDown numMonto;
        private DateTimePicker dtpFecha;

        public DlgGasto(Gasto g)
        {
            Text = g == null ? "Nuevo Gasto" : "Editar Gasto";
            Size = new Size(400, 360);
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = MinimizeBox = false;
            BackColor = Color.FromArgb(245, 250, 245);

            int y = 20;
            AddLbl("Categoria:", y);
            cmbCategoria = new ComboBox { Location = new Point(150, y), Size = new Size(200, 25), DropDownStyle = ComboBoxStyle.DropDownList };
            cmbCategoria.Items.AddRange(new object[] { "Servicios", "Nomina", "Suministros", "Renta", "Mantenimiento", "Otros" });
            cmbCategoria.SelectedIndex = 0;

            y += 40; AddLbl("Descripcion:", y);
            txtDescripcion = new TextBox { Location = new Point(150, y), Size = new Size(200, 25) };

            y += 40; AddLbl("Monto ($):", y);
            numMonto = new NumericUpDown { Location = new Point(150, y), Size = new Size(200, 25), Maximum = 9999999, DecimalPlaces = 2 };

            y += 40; AddLbl("Fecha:", y);
            dtpFecha = new DateTimePicker { Location = new Point(150, y), Size = new Size(200, 25), Format = DateTimePickerFormat.Short };

            y += 40; AddLbl("Proveedor:", y);
            txtProveedor = new TextBox { Location = new Point(150, y), Size = new Size(200, 25) };

            y += 40; AddLbl("Estado:", y);
            cmbEstado = new ComboBox { Location = new Point(150, y), Size = new Size(200, 25), DropDownStyle = ComboBoxStyle.DropDownList };
            cmbEstado.Items.AddRange(new object[] { "Pendiente", "Pagado" });
            cmbEstado.SelectedIndex = 0;

            if (g != null)
            {
                cmbCategoria.SelectedItem = g.Categoria;
                txtDescripcion.Text       = g.Descripcion;
                numMonto.Value            = g.Monto;
                dtpFecha.Value            = g.Fecha;
                txtProveedor.Text         = g.Proveedor;
                cmbEstado.SelectedItem    = g.Estado;
            }

            y += 50;
            var bOk = new Button { Text = "Guardar", Location = new Point(80, y), Size = new Size(100, 32),
                BackColor = Color.FromArgb(40, 140, 80), ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
            bOk.FlatAppearance.BorderSize = 0;
            var bCan = new Button { Text = "Cancelar", DialogResult = DialogResult.Cancel,
                Location = new Point(200, y), Size = new Size(100, 32),
                BackColor = Color.FromArgb(150, 60, 60), ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
            bCan.FlatAppearance.BorderSize = 0;

            bOk.Click += (s, e) =>
            {
                if (string.IsNullOrWhiteSpace(txtDescripcion.Text))
                {
                    MessageBox.Show("Ingrese descripcion.", "Validacion", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                Resultado = g ?? new Gasto();
                Resultado.Categoria   = cmbCategoria.SelectedItem.ToString();
                Resultado.Descripcion = txtDescripcion.Text.Trim();
                Resultado.Monto       = numMonto.Value;
                Resultado.Fecha       = dtpFecha.Value;
                Resultado.Proveedor   = txtProveedor.Text.Trim();
                Resultado.Estado      = cmbEstado.SelectedItem.ToString();
                DialogResult = DialogResult.OK;
            };

            Controls.AddRange(new Control[] { cmbCategoria, txtDescripcion, numMonto, dtpFecha, txtProveedor, cmbEstado, bOk, bCan });
        }

        private void AddLbl(string text, int y) =>
            Controls.Add(new Label { Text = text, Location = new Point(20, y + 3), AutoSize = true, Font = new Font("Segoe UI", 9f, FontStyle.Bold) });
    }
}
