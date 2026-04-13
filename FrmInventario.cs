using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace SistemaGestion
{
    public class FrmInventario : Form
    {
        private DataGridView dgv;
        private Label lblResumen;
        private TextBox txtBuscar;

        public FrmInventario()
        {
            Text = "Inventario"; Size = new Size(1050, 580);
            StartPosition = FormStartPosition.CenterParent;
            BackColor = Color.FromArgb(245, 248, 245);

            var hdr = new Panel { Dock = DockStyle.Top, Height = 50, BackColor = Color.FromArgb(30, 100, 60) };
            hdr.Controls.Add(new Label { Text = "Inventario de Productos", ForeColor = Color.White,
                Font = new Font("Segoe UI", 12f, FontStyle.Bold), AutoSize = true, Location = new Point(12, 13) });
            var btnNuevo  = Btn("+ Nuevo",   Color.FromArgb(20, 140, 70),  new Point(600, 8));
            var btnEditar = Btn("Editar",    Color.FromArgb(60, 100, 180), new Point(698, 8));
            var btnElim   = Btn("Eliminar",  Color.FromArgb(180, 60, 60),  new Point(795, 8));
            hdr.Controls.AddRange(new Control[] { btnNuevo, btnEditar, btnElim });

            // Barra de búsqueda
            var panelSearch = new Panel { Dock = DockStyle.Top, Height = 38, BackColor = Color.FromArgb(230, 245, 235) };
            panelSearch.Controls.Add(new Label { Text = "Buscar:", Location = new Point(10, 10), AutoSize = true, Font = new Font("Segoe UI", 9f) });
            txtBuscar = new TextBox { Location = new Point(65, 7), Size = new Size(280, 24) };
            txtBuscar.TextChanged += (s, e) => Filtrar();
            panelSearch.Controls.Add(txtBuscar);
            panelSearch.Controls.Add(new Label { Text = "* Rojo = bajo mínimo  |  Amarillo = sin existencia", Location = new Point(380, 10), AutoSize = true, ForeColor = Color.FromArgb(120, 60, 0), Font = new Font("Segoe UI", 8.5f, FontStyle.Italic) });

            dgv = CrearGrid(Color.FromArgb(30, 100, 60));
            dgv.DataBindingComplete += (s, e) => ColorearStock();

            var footer = new Panel { Dock = DockStyle.Bottom, Height = 40, BackColor = Color.FromArgb(215, 235, 220) };
            lblResumen = new Label { AutoSize = true, Location = new Point(12, 10), Font = new Font("Segoe UI", 9.5f, FontStyle.Bold), ForeColor = Color.FromArgb(20, 80, 40) };
            var btnCerrar = BtnFooter("Cerrar", footer, new Point(930, 6));
            btnCerrar.Click += (s, e) => Close();
            footer.Controls.AddRange(new Control[] { lblResumen, btnCerrar });

            Controls.AddRange(new Control[] { dgv, panelSearch, hdr, footer });

            btnNuevo.Click  += (s, e) => { using var d = new DlgInventario(null); if (d.ShowDialog() == DialogResult.OK) { DatabaseService.InsertarInventario(d.Resultado); CargarDatos(); } };
            btnEditar.Click += (s, e) => Editar();
            btnElim.Click   += (s, e) => Eliminar();

            CargarDatos();
        }

        private void CargarDatos()
        {
            var lista = DatabaseService.ObtenerInventario();
            dgv.DataSource = null;
            dgv.DataSource = lista.Select(i => new {
                i.Codigo, i.Descripcion, i.Departamento, i.Unidad,
                CostoActual = i.CostoActual.ToString("C2"),
                Precio      = i.Precio.ToString("C2"),
                Existencia  = i.Existencia,
                Min         = i.ExistenMin,
                Max         = i.ExistenMax,
                Proveedor   = i.CodProveedor,
                Actualizado = i.UltimaActualz.ToShortDateString()
            }).ToList();
            var l = DatabaseService.ObtenerInventario();
            lblResumen.Text = $"  Productos: {l.Count}   |   Bajo mínimo: {l.Count(x => x.Existencia < x.ExistenMin)}   |   Sin existencia: {l.Count(x => x.Existencia == 0)}";
        }

        private void Filtrar()
        {
            var q = txtBuscar.Text.ToLower();
            var lista = DatabaseService.ObtenerInventario()
                .Where(i => i.Codigo.ToLower().Contains(q) || (i.Descripcion ?? "").ToLower().Contains(q)).ToList();
            dgv.DataSource = null;
            dgv.DataSource = lista.Select(i => new { i.Codigo, i.Descripcion, i.Departamento, i.Unidad,
                CostoActual = i.CostoActual.ToString("C2"), Precio = i.Precio.ToString("C2"),
                i.Existencia, Min = i.ExistenMin, Max = i.ExistenMax, Proveedor = i.CodProveedor,
                Actualizado = i.UltimaActualz.ToShortDateString() }).ToList();
        }

        private void ColorearStock()
        {
            foreach (DataGridViewRow row in dgv.Rows)
            {
                if (row.Cells["Existencia"].Value == null) continue;
                decimal ex  = Convert.ToDecimal(row.Cells["Existencia"].Value);
                decimal min = Convert.ToDecimal(row.Cells["Min"].Value);
                if (ex == 0)      row.DefaultCellStyle.BackColor = Color.FromArgb(255, 210, 210);
                else if (ex < min) row.DefaultCellStyle.BackColor = Color.FromArgb(255, 245, 190);
            }
        }

        private void Editar()
        {
            if (dgv.CurrentRow == null) return;
            var cod = dgv.CurrentRow.Cells["Codigo"].Value?.ToString();
            var item = DatabaseService.ObtenerInventario().Find(x => x.Codigo == cod);
            if (item == null) return;
            using var d = new DlgInventario(item);
            if (d.ShowDialog() == DialogResult.OK) { DatabaseService.ActualizarInventario(d.Resultado); CargarDatos(); }
        }

        private void Eliminar()
        {
            if (dgv.CurrentRow == null) return;
            var cod = dgv.CurrentRow.Cells["Codigo"].Value?.ToString();
            if (MessageBox.Show($"Eliminar producto {cod}?", "Confirmar", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes)
            { DatabaseService.EliminarInventario(cod); CargarDatos(); }
        }

        private DataGridView CrearGrid(Color headerColor)
        {
            var dgv = new DataGridView { Dock = DockStyle.Fill, ReadOnly = true,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect, MultiSelect = false,
                AllowUserToAddRows = false, AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                BackgroundColor = Color.White, BorderStyle = BorderStyle.None, RowHeadersVisible = false };
            dgv.ColumnHeadersDefaultCellStyle.BackColor = headerColor;
            dgv.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
            dgv.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 9f, FontStyle.Bold);
            dgv.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(240, 250, 243);
            dgv.EnableHeadersVisualStyles = false;
            return dgv;
        }

        private Button Btn(string t, Color c, Point p) { var b = new Button { Text = t, Size = new Size(90, 33), Location = p, BackColor = c, ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 8.5f, FontStyle.Bold), Cursor = Cursors.Hand }; b.FlatAppearance.BorderSize = 0; return b; }
        private Button BtnFooter(string t, Panel par, Point p) { var b = new Button { Text = t, Size = new Size(90, 27), Location = p, BackColor = Color.FromArgb(100, 100, 120), ForeColor = Color.White, FlatStyle = FlatStyle.Flat }; b.FlatAppearance.BorderSize = 0; par.Controls.Add(b); return b; }
    }

    public class DlgInventario : Form
    {
        public Inventario Resultado { get; private set; }
        private TextBox txtCodigo, txtDesc, txtDepto, txtUnidad, txtEnser, txtProv;
        private NumericUpDown numCostoAct, numCostoAnt, numCostoPro, numPrecio, numExist, numMin, numMax;

        public DlgInventario(Inventario i)
        {
            Text = i == null ? "Nuevo Producto" : "Editar Producto";
            Size = new Size(500, 560); StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog; MaximizeBox = MinimizeBox = false;
            BackColor = Color.FromArgb(245, 250, 245);

            // 2 columnas
            int y = 15; int lx = 15; int fx = 165; int w = 145;

            L("Codigo:", lx, y);          txtCodigo  = T(fx, y, w);              y += 35;
            L("Descripcion:", lx, y);     txtDesc    = T(fx, y, w*2);            y += 35;
            L("Departamento:", lx, y);    txtDepto   = T(fx, y, w);              y += 35;
            L("Unidad:", lx, y);          txtUnidad  = T(fx, y, w);              y += 35;
            L("Enser:", lx, y);           txtEnser   = T(fx, y, w);              y += 35;
            L("Costo Actual:", lx, y);    numCostoAct = N(fx, y, w);             y += 35;
            L("Costo Anterior:", lx, y);  numCostoAnt = N(fx, y, w);             y += 35;
            L("Costo Promedio:", lx, y);  numCostoPro = N(fx, y, w);             y += 35;
            L("Precio Venta:", lx, y);    numPrecio   = N(fx, y, w);             y += 35;
            L("Existencia:", lx, y);      numExist    = N(fx, y, w);
            L("Min:", 340, y);            numMin      = N(385, y, 80);           y += 35;
            L("Exist.Max:", lx, y);       numMax      = N(fx, y, w);             y += 35;
            L("Cod.Proveedor:", lx, y);   txtProv    = T(fx, y, w);              y += 45;

            if (i != null)
            {
                txtCodigo.Text = i.Codigo; txtDesc.Text = i.Descripcion;
                txtDepto.Text = i.Departamento; txtUnidad.Text = i.Unidad; txtEnser.Text = i.Enser;
                numCostoAct.Value = i.CostoActual; numCostoAnt.Value = i.CostoAnt; numCostoPro.Value = i.CostoPro;
                numPrecio.Value = i.Precio; numExist.Value = i.Existencia;
                numMin.Value = i.ExistenMin; numMax.Value = i.ExistenMax; txtProv.Text = i.CodProveedor;
                txtCodigo.ReadOnly = true;
            }

            var bOk  = BBtn("Guardar", Color.FromArgb(30, 130, 70),  new Point(100, y));
            var bCan = BBtn("Cancelar", Color.FromArgb(160, 50, 50), new Point(270, y));
            bCan.DialogResult = DialogResult.Cancel;
            bOk.Click += (s, e) =>
            {
                if (string.IsNullOrWhiteSpace(txtCodigo.Text)) { MessageBox.Show("Ingrese el codigo.", "Validacion", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
                Resultado = i ?? new Inventario();
                Resultado.Codigo = txtCodigo.Text.Trim(); Resultado.Descripcion = txtDesc.Text.Trim();
                Resultado.Departamento = txtDepto.Text.Trim(); Resultado.Unidad = txtUnidad.Text.Trim();
                Resultado.Enser = txtEnser.Text.Trim(); Resultado.CostoActual = numCostoAct.Value;
                Resultado.CostoAnt = numCostoAnt.Value; Resultado.CostoPro = numCostoPro.Value;
                Resultado.Precio = numPrecio.Value; Resultado.Existencia = numExist.Value;
                Resultado.ExistenMin = numMin.Value; Resultado.ExistenMax = numMax.Value;
                Resultado.CodProveedor = txtProv.Text.Trim(); Resultado.UltimaActualz = DateTime.Today;
                DialogResult = DialogResult.OK;
            };
            Controls.AddRange(new Control[] { bOk, bCan });
        }

        private void L(string t, int x, int y) => Controls.Add(new Label { Text = t, Location = new Point(x, y+3), AutoSize = true, Font = new Font("Segoe UI", 9f, FontStyle.Bold) });
        private TextBox T(int x, int y, int w) { var tb = new TextBox { Location = new Point(x, y), Size = new Size(w, 24) }; Controls.Add(tb); return tb; }
        private NumericUpDown N(int x, int y, int w) { var n = new NumericUpDown { Location = new Point(x, y), Size = new Size(w, 24), Maximum = 9999999, DecimalPlaces = 2 }; Controls.Add(n); return n; }
        private Button BBtn(string t, Color c, Point p) { var b = new Button { Text = t, Location = p, Size = new Size(120, 32), BackColor = c, ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 9f, FontStyle.Bold) }; b.FlatAppearance.BorderSize = 0; Controls.Add(b); return b; }
    }
}
