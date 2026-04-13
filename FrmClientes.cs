using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace SistemaGestion
{
    public class FrmClientes : Form
    {
        private DataGridView dgv;
        private TextBox txtBuscar;

        public FrmClientes()
        {
            Text = "Clientes"; Size = new Size(1000, 560);
            StartPosition = FormStartPosition.CenterParent;
            BackColor = Color.FromArgb(244, 248, 255);

            var hdr = new Panel { Dock = DockStyle.Top, Height = 50, BackColor = Color.FromArgb(40, 80, 160) };
            hdr.Controls.Add(new Label { Text = "Clientes", ForeColor = Color.White,
                Font = new Font("Segoe UI", 12f, FontStyle.Bold), AutoSize = true, Location = new Point(12, 13) });
            var btnNuevo  = Btn("+ Nuevo",   Color.FromArgb(40, 160, 80),  new Point(680, 8));
            var btnEditar = Btn("Editar",    Color.FromArgb(60, 120, 200), new Point(778, 8));
            var btnElim   = Btn("Eliminar",  Color.FromArgb(180, 60, 60),  new Point(876, 8));
            hdr.Controls.AddRange(new Control[] { btnNuevo, btnEditar, btnElim });

            var panelSearch = new Panel { Dock = DockStyle.Top, Height = 36, BackColor = Color.FromArgb(230, 238, 255) };
            panelSearch.Controls.Add(new Label { Text = "Buscar:", Location = new Point(10, 9), AutoSize = true });
            txtBuscar = new TextBox { Location = new Point(65, 6), Size = new Size(260, 24) };
            txtBuscar.TextChanged += (s, e) => Filtrar();
            panelSearch.Controls.Add(txtBuscar);

            dgv = Grid(Color.FromArgb(40, 80, 160));

            var footer = new Panel { Dock = DockStyle.Bottom, Height = 40, BackColor = Color.FromArgb(220, 228, 250) };
            var lblInfo = new Label { AutoSize = true, Location = new Point(12, 10), Font = new Font("Segoe UI", 9.5f, FontStyle.Bold), ForeColor = Color.FromArgb(30, 60, 130) };
            var btnCerrar = BtnF("Cerrar", footer, new Point(880, 6));
            btnCerrar.Click += (s, e) => Close();
            footer.Controls.AddRange(new Control[] { lblInfo, btnCerrar });

            Controls.AddRange(new Control[] { dgv, panelSearch, hdr, footer });

            btnNuevo.Click  += (s, e) => { using var d = new DlgCliente(null); if (d.ShowDialog() == DialogResult.OK) { DatabaseService.InsertarCliente(d.Resultado); CargarDatos(lblInfo); } };
            btnEditar.Click += (s, e) => { var cl = GetSelected(); if (cl == null) return; using var d = new DlgCliente(cl); if (d.ShowDialog() == DialogResult.OK) { DatabaseService.ActualizarCliente(d.Resultado); CargarDatos(lblInfo); } };
            btnElim.Click   += (s, e) => { var cl = GetSelected(); if (cl == null) return; if (MessageBox.Show($"Eliminar {cl.Codigo}?", "Confirmar", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes) { DatabaseService.EliminarCliente(cl.Codigo); CargarDatos(lblInfo); } };

            CargarDatos(lblInfo);
        }

        private void CargarDatos(Label lbl)
        {
            var lista = DatabaseService.ObtenerClientes();
            BindGrid(lista);
            lbl.Text = $"  Clientes: {lista.Count}   |   Credito total: {lista.Sum(c => c.Credito):C2}";
        }

        private void Filtrar()
        {
            var q = txtBuscar.Text.ToLower();
            BindGrid(DatabaseService.ObtenerClientes().Where(c =>
                c.Codigo.ToLower().Contains(q) || (c.Descripcion ?? "").ToLower().Contains(q) || (c.Ciudad ?? "").ToLower().Contains(q)).ToList());
        }

        private void BindGrid(System.Collections.Generic.List<Cliente> lista)
        {
            dgv.DataSource = null;
            dgv.DataSource = lista.Select(c => new {
                c.Codigo, Nombre = c.Descripcion, c.Ciudad, c.Estado,
                c.CiRif, c.Telefono, Credito = c.Credito.ToString("C2"),
                c.TipoCliente, AFavor = c.AFavor.ToString("C2")
            }).ToList();
        }

        private Cliente GetSelected()
        {
            if (dgv.CurrentRow == null) return null;
            var cod = dgv.CurrentRow.Cells["Codigo"].Value?.ToString();
            return DatabaseService.ObtenerClientes().Find(x => x.Codigo == cod);
        }

        private DataGridView Grid(Color h) { var g = new DataGridView { Dock = DockStyle.Fill, ReadOnly = true, SelectionMode = DataGridViewSelectionMode.FullRowSelect, MultiSelect = false, AllowUserToAddRows = false, AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill, BackgroundColor = Color.White, BorderStyle = BorderStyle.None, RowHeadersVisible = false }; g.ColumnHeadersDefaultCellStyle.BackColor = h; g.ColumnHeadersDefaultCellStyle.ForeColor = Color.White; g.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 9f, FontStyle.Bold); g.EnableHeadersVisualStyles = false; g.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(238, 243, 255); return g; }
        private Button Btn(string t, Color c, Point p) { var b = new Button { Text = t, Size = new Size(90, 33), Location = p, BackColor = c, ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 8.5f, FontStyle.Bold), Cursor = Cursors.Hand }; b.FlatAppearance.BorderSize = 0; return b; }
        private Button BtnF(string t, Panel par, Point p) { var b = new Button { Text = t, Size = new Size(90, 27), Location = p, BackColor = Color.FromArgb(100, 100, 120), ForeColor = Color.White, FlatStyle = FlatStyle.Flat }; b.FlatAppearance.BorderSize = 0; par.Controls.Add(b); return b; }
    }

    public class DlgCliente : Form
    {
        public Cliente Resultado { get; private set; }
        private TextBox txtCod, txtDesc, txtDir, txtCiud, txtEdo, txtCiRif, txtNit, txtFax, txtTel, txtTel2, txtObs;
        private NumericUpDown numCred, numDesc, numAFav;
        private ComboBox cmbTipo;
        private DateTimePicker dtpFecha;

        public DlgCliente(Cliente cl)
        {
            Text = cl == null ? "Nuevo Cliente" : "Editar Cliente";
            Size = new Size(480, 580); StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog; MaximizeBox = MinimizeBox = false;
            BackColor = Color.FromArgb(244, 248, 255);

            int y = 12;
            Lbl("Codigo:", y);      txtCod  = Txt(y); y += 34;
            Lbl("Nombre:", y);      txtDesc = Txt(y); y += 34;
            Lbl("Direccion:", y);   txtDir  = Txt(y); y += 34;
            Lbl("Ciudad:", y);      txtCiud = Txt(y); y += 34;
            Lbl("Estado:", y);      txtEdo  = Txt(y); y += 34;
            Lbl("CI/RIF:", y);      txtCiRif= Txt(y); y += 34;
            Lbl("NIT:", y);         txtNit  = Txt(y); y += 34;
            Lbl("Fax:", y);         txtFax  = Txt(y); y += 34;
            Lbl("Telefono:", y);    txtTel  = Txt(y); y += 34;
            Lbl("Telefono2:", y);   txtTel2 = Txt(y); y += 34;
            Lbl("Credito:", y);     numCred = Num(y, 9999999); y += 34;
            Lbl("% Descuento:", y); numDesc = Num(y, 100);     y += 34;
            Lbl("A Favor:", y);     numAFav = Num(y, 9999999); y += 34;
            Lbl("Tipo Cliente:", y);
            cmbTipo = new ComboBox { Location = new Point(155, y), Size = new Size(270, 24), DropDownStyle = ComboBoxStyle.DropDownList };
            cmbTipo.Items.AddRange(new object[] { "Regular", "VIP", "Mayorista", "Minorista", "Gobierno" });
            cmbTipo.SelectedIndex = 0; Controls.Add(cmbTipo); y += 34;
            Lbl("Fecha:", y);
            dtpFecha = new DateTimePicker { Location = new Point(155, y), Size = new Size(270, 24), Format = DateTimePickerFormat.Short };
            Controls.Add(dtpFecha); y += 40;
            Lbl("Observaciones:", y); txtObs = new TextBox { Location = new Point(155, y), Size = new Size(270, 45), Multiline = true }; Controls.Add(txtObs);

            if (cl != null)
            {
                txtCod.Text = cl.Codigo; txtDesc.Text = cl.Descripcion; txtDir.Text = cl.Direccion;
                txtCiud.Text = cl.Ciudad; txtEdo.Text = cl.Estado; txtCiRif.Text = cl.CiRif;
                txtNit.Text = cl.Nit; txtFax.Text = cl.Fax; txtTel.Text = cl.Telefono;
                txtTel2.Text = cl.Telefono2; numCred.Value = cl.Credito; numDesc.Value = cl.Descuento;
                numAFav.Value = cl.AFavor; cmbTipo.SelectedItem = cl.TipoCliente;
                dtpFecha.Value = cl.Fecha; txtObs.Text = cl.Observaciones;
                txtCod.ReadOnly = true;
            }

            y += 55;
            var bOk  = BBtn("Guardar",  Color.FromArgb(40, 100, 180), new Point(90,  y));
            var bCan = BBtn("Cancelar", Color.FromArgb(160, 50, 50),  new Point(260, y));
            bCan.DialogResult = DialogResult.Cancel;
            bOk.Click += (s, e) =>
            {
                if (string.IsNullOrWhiteSpace(txtCod.Text)) { MessageBox.Show("Ingrese el codigo.", "Validacion", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
                Resultado = cl ?? new Cliente();
                Resultado.Codigo = txtCod.Text.Trim(); Resultado.Descripcion = txtDesc.Text.Trim();
                Resultado.Direccion = txtDir.Text.Trim(); Resultado.Ciudad = txtCiud.Text.Trim();
                Resultado.Estado = txtEdo.Text.Trim(); Resultado.CiRif = txtCiRif.Text.Trim();
                Resultado.Nit = txtNit.Text.Trim(); Resultado.Fax = txtFax.Text.Trim();
                Resultado.Telefono = txtTel.Text.Trim(); Resultado.Telefono2 = txtTel2.Text.Trim();
                Resultado.Credito = numCred.Value; Resultado.Descuento = numDesc.Value;
                Resultado.AFavor = numAFav.Value; Resultado.TipoCliente = cmbTipo.SelectedItem?.ToString();
                Resultado.Fecha = dtpFecha.Value; Resultado.Observaciones = txtObs.Text.Trim();
                DialogResult = DialogResult.OK;
            };
            Controls.AddRange(new Control[] { bOk, bCan });
        }

        private void Lbl(string t, int y) => Controls.Add(new Label { Text = t, Location = new Point(12, y+3), AutoSize = true, Font = new Font("Segoe UI", 9f, FontStyle.Bold) });
        private TextBox Txt(int y) { var tb = new TextBox { Location = new Point(155, y), Size = new Size(270, 24) }; Controls.Add(tb); return tb; }
        private NumericUpDown Num(int y, decimal max) { var n = new NumericUpDown { Location = new Point(155, y), Size = new Size(270, 24), Maximum = max, DecimalPlaces = 2 }; Controls.Add(n); return n; }
        private Button BBtn(string t, Color c, Point p) { var b = new Button { Text = t, Location = p, Size = new Size(130, 32), BackColor = c, ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 9f, FontStyle.Bold) }; b.FlatAppearance.BorderSize = 0; Controls.Add(b); return b; }
    }
}
