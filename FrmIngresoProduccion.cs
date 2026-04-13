using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace SistemaGestion
{
    public class FrmIngresoProduccion : Form
    {
        private DataGridView dgv;
        private Label lblResumen;

        public FrmIngresoProduccion()
        {
            Text = "Ingreso de Produccion";
            Size = new Size(880, 540);
            StartPosition = FormStartPosition.CenterParent;
            BackColor = Color.FromArgb(248, 244, 255);

            var hdr = new Panel { Dock = DockStyle.Top, Height = 50, BackColor = Color.FromArgb(140, 80, 160) };
            hdr.Controls.Add(new Label { Text = "Ingreso de Produccion", ForeColor = Color.White,
                Font = new Font("Segoe UI", 12f, FontStyle.Bold), AutoSize = true, Location = new Point(12, 13) });

            var btnNuevo  = Btn("+ Nuevo",  Color.FromArgb(40, 160, 80),   new Point(480, 8));
            var btnCalidad = Btn("Calidad",  Color.FromArgb(40, 80, 160),   new Point(578, 8));
            var btnEditar = Btn("Editar",   Color.FromArgb(60, 100, 180),  new Point(698, 8));
            var btnElim   = Btn("Eliminar", Color.FromArgb(180, 60, 60),   new Point(795, 8));
            hdr.Controls.AddRange(new Control[] { btnNuevo, btnCalidad, btnEditar, btnElim });

            dgv = new DataGridView { Dock = DockStyle.Fill, ReadOnly = true,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false, AllowUserToAddRows = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                BackgroundColor = Color.White, BorderStyle = BorderStyle.None,
                RowHeadersVisible = false, Font = new Font("Segoe UI", 9f) };
            dgv.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(140, 80, 160);
            dgv.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
            dgv.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 9f, FontStyle.Bold);
            dgv.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(248, 240, 255);
            dgv.EnableHeadersVisualStyles = false;
            dgv.DataBindingComplete += (s, e) => ColorearFilas();

            var footer = new Panel { Dock = DockStyle.Bottom, Height = 45, BackColor = Color.FromArgb(235, 225, 245) };
            lblResumen = new Label { AutoSize = true, Location = new Point(12, 12),
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold), ForeColor = Color.FromArgb(80, 40, 100) };
            var btnCerrar = new Button { Text = "Cerrar", Size = new Size(90, 28), Location = new Point(760, 8),
                BackColor = Color.FromArgb(100, 100, 120), ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
            btnCerrar.FlatAppearance.BorderSize = 0;
            btnCerrar.Click += (s, e) => Close();
            footer.Controls.AddRange(new Control[] { lblResumen, btnCerrar });

            Controls.AddRange(new Control[] { dgv, hdr, footer });

            btnNuevo.Click  += (s, e) =>
            {
                using var d = new DlgIngreso(null);
                if (d.ShowDialog() == DialogResult.OK)
                {
                    DatabaseService.InsertarIngreso(d.Resultado);
                    // Registrar automáticamente en el Kardex
                    try
                    {
                        DatabaseService.RegistrarMovimiento(
                            d.Resultado.Producto,
                            "ENTRADA",
                            d.Resultado.Cantidad,
                            $"Produccion - {d.Resultado.Lote}",
                            $"PROD-{d.Resultado.Lote}",
                            "Sistema");
                    }
                    catch { /* producto no existe en inventario, omitir */ }
        btnCalidad.Click += (s, e) => new FrmControlCalidad().ShowDialog();
            CargarDatos();
                }
            };
            btnEditar.Click += (s, e) =>
            {
                if (dgv.CurrentRow == null) return;
                int id = (int)dgv.CurrentRow.Cells["Id"].Value;
                var ip = DatabaseService.ObtenerIngresos().Find(x => x.Id == id);
                if (ip == null) return;
                using var d = new DlgIngreso(ip);
                if (d.ShowDialog() == DialogResult.OK) { DatabaseService.ActualizarIngreso(d.Resultado); CargarDatos(); }
            };
            btnElim.Click += (s, e) =>
            {
                if (dgv.CurrentRow == null) return;
                int id = (int)dgv.CurrentRow.Cells["Id"].Value;
                if (MessageBox.Show($"Eliminar registro #{id}?", "Confirmar", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes)
                { DatabaseService.EliminarIngreso(id); CargarDatos(); }
            };

            CargarDatos();
        }

        private Button Btn(string t, Color c, Point p) { var b = new Button { Text = t, Size = new Size(90, 33), Location = p, BackColor = c, ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 8.5f, FontStyle.Bold), Cursor = Cursors.Hand }; b.FlatAppearance.BorderSize = 0; return b; }

        private void CargarDatos()
        {
            var lista = DatabaseService.ObtenerIngresos();
            dgv.DataSource = null;
            dgv.DataSource = lista.Select(ip => new {
                ip.Id, ip.Producto, ip.Lote, ip.Cantidad, ip.Unidad,
                Fecha = ip.Fecha.ToShortDateString(),
                ip.Responsable, ip.Estado
            }).ToList();

            int total = lista.Sum(ip => ip.Cantidad);
            lblResumen.Text = $"  Registros: {lista.Count}   |   Unidades totales: {total}   |   Completados: {lista.Count(x => x.Estado == "Completado")}   |   En proceso: {lista.Count(x => x.Estado == "En proceso")}";
        }

        private void ColorearFilas()
        {
            foreach (DataGridViewRow row in dgv.Rows)
            {
                var e = row.Cells["Estado"].Value?.ToString();
                if (e == "Completado") row.DefaultCellStyle.BackColor = Color.FromArgb(225, 255, 230);
                else if (e == "Rechazado") row.DefaultCellStyle.BackColor = Color.FromArgb(255, 225, 225);
                else if (e == "En proceso") row.DefaultCellStyle.BackColor = Color.FromArgb(255, 250, 215);
            }
        }
    }

    public class DlgIngreso : Form
    {
        public IngresoProduccion Resultado { get; private set; }
        private TextBox txtProducto, txtLote, txtResponsable, txtObservacion;
        private NumericUpDown numCantidad;
        private ComboBox cmbUnidad, cmbEstado;
        private DateTimePicker dtpFecha;

        public DlgIngreso(IngresoProduccion ip)
        {
            Text = ip == null ? "Nuevo Ingreso" : "Editar Ingreso";
            Size = new Size(420, 440); StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog; MaximizeBox = MinimizeBox = false;
            BackColor = Color.FromArgb(248, 244, 255);

            int y = 15;
            L("Producto:", y);     txtProducto    = T("", y); y += 38;
            L("Lote:", y);         txtLote        = T("", y); y += 38;
            L("Cantidad:", y);
            numCantidad = new NumericUpDown { Location = new Point(150, y), Size = new Size(230, 25), Maximum = 999999, Minimum = 1, Value = 1 };
            Controls.Add(numCantidad); y += 38;
            L("Unidad:", y);
            cmbUnidad = new ComboBox { Location = new Point(150, y), Size = new Size(230, 25), DropDownStyle = ComboBoxStyle.DropDownList };
            cmbUnidad.Items.AddRange(new object[] { "Piezas", "Kg", "Litros", "Cajas", "Metros", "Toneladas" });
            cmbUnidad.SelectedIndex = 0; Controls.Add(cmbUnidad); y += 38;
            L("Fecha:", y);        dtpFecha = new DateTimePicker { Location = new Point(150, y), Size = new Size(230, 25), Format = DateTimePickerFormat.Short }; Controls.Add(dtpFecha); y += 38;
            L("Responsable:", y);  txtResponsable = T("", y); y += 38;
            L("Estado:", y);
            cmbEstado = new ComboBox { Location = new Point(150, y), Size = new Size(230, 25), DropDownStyle = ComboBoxStyle.DropDownList };
            cmbEstado.Items.AddRange(new object[] { "En proceso", "Completado", "Rechazado" });
            cmbEstado.SelectedIndex = 0; Controls.Add(cmbEstado); y += 38;
            L("Observacion:", y);  txtObservacion = T("", y); y += 38;

            if (ip != null)
            {
                txtProducto.Text = ip.Producto; txtLote.Text = ip.Lote;
                numCantidad.Value = ip.Cantidad;
                cmbUnidad.SelectedItem = ip.Unidad;
                dtpFecha.Value = ip.Fecha; txtResponsable.Text = ip.Responsable;
                cmbEstado.SelectedItem = ip.Estado; txtObservacion.Text = ip.Observacion;
            }

            var bOk = new Button { Text = "Guardar", Location = new Point(70, y), Size = new Size(110, 34),
                BackColor = Color.FromArgb(140, 80, 160), ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
            bOk.FlatAppearance.BorderSize = 0;
            var bCan = new Button { Text = "Cancelar", DialogResult = DialogResult.Cancel, Location = new Point(200, y), Size = new Size(110, 34),
                BackColor = Color.FromArgb(150, 60, 60), ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
            bCan.FlatAppearance.BorderSize = 0;
            bOk.Click += (s, e) =>
            {
                if (string.IsNullOrWhiteSpace(txtProducto.Text)) { MessageBox.Show("Ingrese el producto.", "Validacion", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
                Resultado = ip ?? new IngresoProduccion();
                Resultado.Producto    = txtProducto.Text.Trim();
                Resultado.Lote        = txtLote.Text.Trim();
                Resultado.Cantidad    = (int)numCantidad.Value;
                Resultado.Unidad      = cmbUnidad.SelectedItem?.ToString() ?? "Piezas";
                Resultado.Fecha       = dtpFecha.Value;
                Resultado.Responsable = txtResponsable.Text.Trim();
                Resultado.Estado      = cmbEstado.SelectedItem.ToString();
                Resultado.Observacion = txtObservacion.Text.Trim();
                DialogResult = DialogResult.OK;
            };
            Controls.AddRange(new Control[] { bOk, bCan });
        }

        private void L(string t, int y) => Controls.Add(new Label { Text = t, Location = new Point(15, y + 3), AutoSize = true, Font = new Font("Segoe UI", 9f, FontStyle.Bold) });
        private TextBox T(string v, int y) { var tb = new TextBox { Text = v, Location = new Point(150, y), Size = new Size(230, 25) }; Controls.Add(tb); return tb; }
    }
}
