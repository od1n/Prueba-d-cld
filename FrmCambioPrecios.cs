using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace SistemaGestion
{
    public class FrmCambioPrecios : Form
    {
        private DataGridView dgv;
        private Label lblResumen;

        public FrmCambioPrecios()
        {
            Text = "Cambio de Precios y Costos";
            Size = new Size(1000, 560);
            StartPosition = FormStartPosition.CenterParent;
            BackColor = Color.FromArgb(255, 249, 240);

            var hdr = new Panel { Dock = DockStyle.Top, Height = 50, BackColor = Color.FromArgb(180, 110, 30) };
            hdr.Controls.Add(new Label { Text = "Cambio de Precios y Costos", ForeColor = Color.White,
                Font = new Font("Segoe UI", 12f, FontStyle.Bold), AutoSize = true, Location = new Point(12, 13) });

            var btnNuevo   = Btn("+ Producto",  Color.FromArgb(40, 160, 80),  new Point(570, 8));
            var btnEditar  = Btn("Editar",      Color.FromArgb(60, 100, 180), new Point(670, 8));
            var btnAplicar = Btn("Aplicar",     Color.FromArgb(140, 80, 20),  new Point(770, 8));
            var btnElim    = Btn("Eliminar",    Color.FromArgb(180, 60, 60),  new Point(870, 8));
            hdr.Controls.AddRange(new Control[] { btnNuevo, btnEditar, btnAplicar, btnElim });

            // Panel filtros
            var panelFiltro = new Panel { Dock = DockStyle.Top, Height = 40, BackColor = Color.FromArgb(245, 235, 215) };
            panelFiltro.Controls.Add(new Label { Text = "Filtrar categoria:", Location = new Point(10, 12), AutoSize = true, Font = new Font("Segoe UI", 9f) });
            var cmbFiltro = new ComboBox { Location = new Point(130, 8), Size = new Size(160, 25), DropDownStyle = ComboBoxStyle.DropDownList };
            cmbFiltro.Items.AddRange(new object[] { "Todas", "Alimentos", "Electronicos", "Ropa", "Servicios", "Materia Prima", "Otros" });
            cmbFiltro.SelectedIndex = 0;
            panelFiltro.Controls.Add(cmbFiltro);
            panelFiltro.Controls.Add(new Label { Text = "% Ajuste global:", Location = new Point(320, 12), AutoSize = true, Font = new Font("Segoe UI", 9f) });
            var numPct = new NumericUpDown { Location = new Point(430, 8), Size = new Size(80, 25), Minimum = -99, Maximum = 999, DecimalPlaces = 1 };
            panelFiltro.Controls.Add(numPct);
            var btnAjuste = new Button { Text = "Aplicar %", Location = new Point(520, 7), Size = new Size(90, 27),
                BackColor = Color.FromArgb(180, 110, 30), ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 8.5f, FontStyle.Bold) };
            btnAjuste.FlatAppearance.BorderSize = 0;
            panelFiltro.Controls.Add(btnAjuste);

            dgv = new DataGridView { Dock = DockStyle.Fill, ReadOnly = true,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false, AllowUserToAddRows = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                BackgroundColor = Color.White, BorderStyle = BorderStyle.None,
                RowHeadersVisible = false, Font = new Font("Segoe UI", 9f) };
            dgv.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(180, 110, 30);
            dgv.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
            dgv.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 9f, FontStyle.Bold);
            dgv.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(255, 245, 230);
            dgv.EnableHeadersVisualStyles = false;
            dgv.DataBindingComplete += (s, e) => ColorearFilas();

            var footer = new Panel { Dock = DockStyle.Bottom, Height = 45, BackColor = Color.FromArgb(240, 228, 210) };
            lblResumen = new Label { AutoSize = true, Location = new Point(12, 12),
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold), ForeColor = Color.FromArgb(100, 60, 10) };
            var btnCerrar = new Button { Text = "Cerrar", Size = new Size(90, 28), Location = new Point(880, 8),
                BackColor = Color.FromArgb(100, 100, 120), ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
            btnCerrar.FlatAppearance.BorderSize = 0;
            btnCerrar.Click += (s, e) => Close();
            footer.Controls.AddRange(new Control[] { lblResumen, btnCerrar });

            Controls.AddRange(new Control[] { dgv, panelFiltro, hdr, footer });

            btnNuevo.Click  += (s, e) => { using var d = new DlgProducto(null); if (d.ShowDialog() == DialogResult.OK) { DatabaseService.InsertarProducto(d.Resultado); CargarDatos(); } };
            btnEditar.Click += (s, e) =>
            {
                if (dgv.CurrentRow == null) return;
                int id = (int)dgv.CurrentRow.Cells["Id"].Value;
                var p = DatabaseService.ObtenerProductos().Find(x => x.Id == id);
                if (p == null) return;
                using var d = new DlgProducto(p);
                if (d.ShowDialog() == DialogResult.OK) { DatabaseService.ActualizarProducto(d.Resultado); CargarDatos(); }
            };
            btnAplicar.Click += (s, e) => AplicarNuevosPrecios();
            btnElim.Click += (s, e) =>
            {
                if (dgv.CurrentRow == null) return;
                int id = (int)dgv.CurrentRow.Cells["Id"].Value;
                if (MessageBox.Show($"Eliminar producto #{id}?", "Confirmar", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes)
                { DatabaseService.EliminarProducto(id); CargarDatos(); }
            };
            btnAjuste.Click += (s, e) => AplicarPorcentaje(numPct.Value);
            cmbFiltro.SelectedIndexChanged += (s, e) => CargarDatos(cmbFiltro.SelectedItem.ToString());

            CargarDatos();
        }

        private Button Btn(string t, Color c, Point p) { var b = new Button { Text = t, Size = new Size(90, 33), Location = p, BackColor = c, ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 8f, FontStyle.Bold), Cursor = Cursors.Hand }; b.FlatAppearance.BorderSize = 0; return b; }

        private void CargarDatos(string filtro = "Todas")
        {
            var lista = DatabaseService.ObtenerProductos();
            if (filtro != "Todas") lista = lista.Where(x => x.Categoria == filtro).ToList();

            dgv.DataSource = null;
            dgv.DataSource = lista.Select(p => new {
                p.Id, p.Codigo, p.Nombre, p.Categoria,
                CostoActual  = p.CostoActual.ToString("C2"),
                PrecioActual = p.PrecioActual.ToString("C2"),
                CostoNuevo   = p.CostoNuevo > 0 ? p.CostoNuevo.ToString("C2") : "-",
                PrecioNuevo  = p.PrecioNuevo > 0 ? p.PrecioNuevo.ToString("C2") : "-",
                Margen       = p.PrecioActual > 0 ? $"{((p.PrecioActual - p.CostoActual) / p.PrecioActual * 100):F1}%" : "0%",
                Actualizado  = p.UltimaActualizacion.ToShortDateString()
            }).ToList();

            lblResumen.Text = $"  Productos: {lista.Count}   |   Con precio nuevo: {lista.Count(x => x.PrecioNuevo > 0)}   |   Pendientes aplicar: {lista.Count(x => x.PrecioNuevo > 0 && x.PrecioNuevo != x.PrecioActual)}";
        }

        private void ColorearFilas()
        {
            foreach (DataGridViewRow row in dgv.Rows)
            {
                var pnStr = row.Cells["PrecioNuevo"].Value?.ToString();
                if (pnStr != null && pnStr != "-")
                    row.DefaultCellStyle.BackColor = Color.FromArgb(255, 245, 200);
            }
        }

        private void AplicarNuevosPrecios()
        {
            var lista = DatabaseService.ObtenerProductos().Where(x => x.PrecioNuevo > 0 && x.PrecioNuevo != x.PrecioActual).ToList();
            if (lista.Count == 0) { MessageBox.Show("No hay precios nuevos pendientes de aplicar.", "Info", MessageBoxButtons.OK, MessageBoxIcon.Information); return; }
            if (MessageBox.Show($"Aplicar nuevos precios a {lista.Count} productos?", "Confirmar", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                foreach (var p in lista)
                {
                    p.CostoActual = p.CostoNuevo; p.PrecioActual = p.PrecioNuevo;
                    p.CostoNuevo = 0; p.PrecioNuevo = 0;
                    p.UltimaActualizacion = DateTime.Today;
                    DatabaseService.ActualizarProducto(p);
                }
                CargarDatos();
                MessageBox.Show($"Precios actualizados en {lista.Count} productos.", "Exito", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void AplicarPorcentaje(decimal pct)
        {
            var lista = DatabaseService.ObtenerProductos();
            if (lista.Count == 0) return;
            if (MessageBox.Show($"Aplicar {pct:+0.0;-0.0}% a precios de {lista.Count} productos?", "Confirmar", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                foreach (var p in lista)
                {
                    p.PrecioNuevo = Math.Round(p.PrecioActual * (1 + pct / 100), 2);
                    p.CostoNuevo  = p.CostoNuevo == 0 ? p.CostoActual : p.CostoNuevo;
                    DatabaseService.ActualizarProducto(p);
                }
                CargarDatos();
            }
        }
    }

    public class DlgProducto : Form
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
            FormBorderStyle = FormBorderStyle.FixedDialog; MaximizeBox = MinimizeBox = false;
            BackColor = Color.FromArgb(255, 249, 240);

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
                BackColor = Color.FromArgb(180, 110, 30), ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
            bOk.FlatAppearance.BorderSize = 0;
            var bCan = new Button { Text = "Cancelar", DialogResult = DialogResult.Cancel, Location = new Point(210, y), Size = new Size(110, 34),
                BackColor = Color.FromArgb(150, 60, 60), ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
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
            Controls.AddRange(new Control[] { bOk, bCan });
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
