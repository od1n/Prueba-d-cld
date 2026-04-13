using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace SistemaGestion
{
    public class FrmCuentasCobrar : Form
    {
        private DataGridView dgv;
        private Label lblResumen;

        public FrmCuentasCobrar()
        {
            Text = "Cuentas por Cobrar";
            Size = new Size(950, 560);
            StartPosition = FormStartPosition.CenterParent;
            BackColor = Color.FromArgb(244, 252, 252);

            var hdr = new Panel { Dock = DockStyle.Top, Height = 50, BackColor = Color.FromArgb(40, 140, 140) };
            hdr.Controls.Add(new Label { Text = "Cuentas por Cobrar", ForeColor = Color.White,
                Font = new Font("Segoe UI", 12f, FontStyle.Bold), AutoSize = true, Location = new Point(12, 13) });

            var btnNueva  = Btn("+ Nueva",   Color.FromArgb(40, 160, 80),  new Point(600, 8));
            var btnEditar = Btn("Editar",    Color.FromArgb(60, 100, 180), new Point(698, 8));
            var btnPago   = Btn("Reg. Pago", Color.FromArgb(180, 130, 20), new Point(795, 8));
            hdr.Controls.AddRange(new Control[] { btnNueva, btnEditar, btnPago });

            dgv = new DataGridView { Dock = DockStyle.Fill, ReadOnly = true,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false, AllowUserToAddRows = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                BackgroundColor = Color.White, BorderStyle = BorderStyle.None,
                RowHeadersVisible = false, Font = new Font("Segoe UI", 9f) };
            dgv.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(40, 140, 140);
            dgv.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
            dgv.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 9f, FontStyle.Bold);
            dgv.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(235, 250, 250);
            dgv.EnableHeadersVisualStyles = false;
            dgv.DataBindingComplete += (s, e) => ColorearFilas();

            var footer = new Panel { Dock = DockStyle.Bottom, Height = 45, BackColor = Color.FromArgb(215, 240, 240) };
            lblResumen = new Label { AutoSize = true, Location = new Point(12, 12),
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold), ForeColor = Color.FromArgb(20, 80, 80) };
            var btnCerrar = new Button { Text = "Cerrar", Size = new Size(90, 28), Location = new Point(830, 8),
                BackColor = Color.FromArgb(100, 100, 120), ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
            btnCerrar.FlatAppearance.BorderSize = 0;
            btnCerrar.Click += (s, e) => Close();
            footer.Controls.AddRange(new Control[] { lblResumen, btnCerrar });

            Controls.AddRange(new Control[] { dgv, hdr, footer });

            btnNueva.Click  += (s, e) => { using var d = new DlgCuenta(null); if (d.ShowDialog() == DialogResult.OK) { DatabaseService.InsertarCuenta(d.Resultado); CargarDatos(); } };
            btnEditar.Click += (s, e) =>
            {
                if (dgv.CurrentRow == null) return;
                int id = (int)dgv.CurrentRow.Cells["Id"].Value;
                var cc = DatabaseService.ObtenerCuentas().Find(x => x.Id == id);
                if (cc == null) return;
                using var d = new DlgCuenta(cc);
                if (d.ShowDialog() == DialogResult.OK) { DatabaseService.ActualizarCuenta(d.Resultado); CargarDatos(); }
            };
            btnPago.Click += (s, e) => RegistrarPago();

            CargarDatos();
        }

        private Button Btn(string t, Color c, Point p) { var b = new Button { Text = t, Size = new Size(90, 33), Location = p, BackColor = c, ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 8.5f, FontStyle.Bold), Cursor = Cursors.Hand }; b.FlatAppearance.BorderSize = 0; return b; }

        private void CargarDatos()
        {
            var lista = DatabaseService.ObtenerCuentas();
            // Actualizar vencidas automaticamente
            foreach (var cc in lista.Where(x => x.Estado == "Vigente" && x.FechaVence < DateTime.Today))
            { cc.Estado = "Vencida"; DatabaseService.ActualizarCuenta(cc); }

            lista = DatabaseService.ObtenerCuentas();
            dgv.DataSource = null;
            dgv.DataSource = lista.Select(cc => new {
                cc.Id, cc.Cliente, cc.Concepto,
                Monto   = cc.Monto.ToString("C2"),
                Pagado  = cc.Pagado.ToString("C2"),
                Saldo   = cc.Saldo.ToString("C2"),
                Emision = cc.FechaEmision.ToShortDateString(),
                Vence   = cc.FechaVence.ToShortDateString(),
                cc.Estado
            }).ToList();

            decimal totalMonto = lista.Sum(x => x.Monto);
            decimal totalSaldo = lista.Sum(x => x.Saldo);
            lblResumen.Text = $"  Cuentas: {lista.Count}   |   Total cargado: {totalMonto:C2}   |   Saldo pendiente: {totalSaldo:C2}   |   Vencidas: {lista.Count(x => x.Estado == "Vencida")}";
        }

        private void ColorearFilas()
        {
            foreach (DataGridViewRow row in dgv.Rows)
            {
                var e = row.Cells["Estado"].Value?.ToString();
                if (e == "Saldada")  row.DefaultCellStyle.BackColor = Color.FromArgb(225, 255, 230);
                else if (e == "Vencida") row.DefaultCellStyle.BackColor = Color.FromArgb(255, 220, 220);
                else row.DefaultCellStyle.BackColor = Color.FromArgb(255, 250, 215);
            }
        }

        private void RegistrarPago()
        {
            if (dgv.CurrentRow == null) return;
            int id = (int)dgv.CurrentRow.Cells["Id"].Value;
            var cc = DatabaseService.ObtenerCuentas().Find(x => x.Id == id);
            if (cc == null) return;
            if (cc.Estado == "Saldada") { MessageBox.Show("Esta cuenta ya esta saldada.", "Info", MessageBoxButtons.OK, MessageBoxIcon.Information); return; }

            using var dlg = new DlgPago(cc.Saldo);
            if (dlg.ShowDialog() == DialogResult.OK)
            {
                cc.Pagado += dlg.MontoPago;
                if (cc.Pagado >= cc.Monto) { cc.Pagado = cc.Monto; cc.Estado = "Saldada"; }
                DatabaseService.ActualizarCuenta(cc);
                CargarDatos();
                MessageBox.Show($"Pago de {dlg.MontoPago:C2} registrado.", "Pago Registrado", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }
    }

    public class DlgCuenta : Form
    {
        public CuentaCobrar Resultado { get; private set; }
        private TextBox txtCliente, txtConcepto;
        private NumericUpDown numMonto, numPagado;
        private DateTimePicker dtpEmision, dtpVence;
        private ComboBox cmbEstado;

        public DlgCuenta(CuentaCobrar cc)
        {
            Text = cc == null ? "Nueva Cuenta" : "Editar Cuenta";
            Size = new Size(420, 420); StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog; MaximizeBox = MinimizeBox = false;
            BackColor = Color.FromArgb(244, 252, 252);

            int y = 15;
            L("Cliente:", y);       txtCliente  = T("", y); y += 38;
            L("Concepto:", y);      txtConcepto = T("", y); y += 38;
            L("Monto Total:", y);   numMonto  = N(y, 9999999); y += 38;
            L("Monto Pagado:", y);  numPagado = N(y, 9999999); y += 38;
            L("Fecha Emision:", y); dtpEmision = D(DateTime.Today, y); y += 38;
            L("Fecha Vence:", y);   dtpVence   = D(DateTime.Today.AddDays(30), y); y += 38;
            L("Estado:", y);
            cmbEstado = new ComboBox { Location = new Point(150, y), Size = new Size(230, 25), DropDownStyle = ComboBoxStyle.DropDownList };
            cmbEstado.Items.AddRange(new object[] { "Vigente", "Vencida", "Saldada" });
            cmbEstado.SelectedIndex = 0; Controls.Add(cmbEstado); y += 50;

            if (cc != null)
            {
                txtCliente.Text = cc.Cliente; txtConcepto.Text = cc.Concepto;
                numMonto.Value = cc.Monto; numPagado.Value = cc.Pagado;
                dtpEmision.Value = cc.FechaEmision; dtpVence.Value = cc.FechaVence;
                cmbEstado.SelectedItem = cc.Estado;
            }

            var bOk = new Button { Text = "Guardar", Location = new Point(70, y), Size = new Size(110, 34),
                BackColor = Color.FromArgb(40, 140, 140), ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
            bOk.FlatAppearance.BorderSize = 0;
            var bCan = new Button { Text = "Cancelar", DialogResult = DialogResult.Cancel, Location = new Point(200, y), Size = new Size(110, 34),
                BackColor = Color.FromArgb(150, 60, 60), ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
            bCan.FlatAppearance.BorderSize = 0;
            bOk.Click += (s, e) =>
            {
                if (string.IsNullOrWhiteSpace(txtCliente.Text)) { MessageBox.Show("Ingrese cliente.", "Validacion", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
                Resultado = cc ?? new CuentaCobrar();
                Resultado.Cliente = txtCliente.Text.Trim(); Resultado.Concepto = txtConcepto.Text.Trim();
                Resultado.Monto = numMonto.Value; Resultado.Pagado = numPagado.Value;
                Resultado.FechaEmision = dtpEmision.Value; Resultado.FechaVence = dtpVence.Value;
                Resultado.Estado = cmbEstado.SelectedItem.ToString();
                DialogResult = DialogResult.OK;
            };
            Controls.AddRange(new Control[] { bOk, bCan });
        }

        private void L(string t, int y) => Controls.Add(new Label { Text = t, Location = new Point(15, y + 3), AutoSize = true, Font = new Font("Segoe UI", 9f, FontStyle.Bold) });
        private TextBox T(string v, int y) { var tb = new TextBox { Text = v, Location = new Point(150, y), Size = new Size(230, 25) }; Controls.Add(tb); return tb; }
        private NumericUpDown N(int y, decimal max) { var n = new NumericUpDown { Location = new Point(150, y), Size = new Size(230, 25), Maximum = max, DecimalPlaces = 2 }; Controls.Add(n); return n; }
        private DateTimePicker D(DateTime v, int y) { var dp = new DateTimePicker { Value = v, Location = new Point(150, y), Size = new Size(230, 25), Format = DateTimePickerFormat.Short }; Controls.Add(dp); return dp; }
    }

    public class DlgPago : Form
    {
        public decimal MontoPago { get; private set; }
        public DlgPago(decimal saldo)
        {
            Text = "Registrar Pago"; Size = new Size(320, 180);
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog; MaximizeBox = MinimizeBox = false;
            BackColor = Color.FromArgb(244, 252, 252);

            Controls.Add(new Label { Text = $"Saldo pendiente: {saldo:C2}", Location = new Point(20, 20), AutoSize = true, Font = new Font("Segoe UI", 10f, FontStyle.Bold) });
            Controls.Add(new Label { Text = "Monto a pagar:", Location = new Point(20, 55), AutoSize = true, Font = new Font("Segoe UI", 9f) });
            var num = new NumericUpDown { Location = new Point(150, 52), Size = new Size(130, 25), Maximum = saldo, Minimum = 0, Value = saldo, DecimalPlaces = 2 };
            Controls.Add(num);

            var bOk = new Button { Text = "Registrar", Location = new Point(60, 100), Size = new Size(100, 32),
                BackColor = Color.FromArgb(40, 140, 140), ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
            bOk.FlatAppearance.BorderSize = 0;
            var bCan = new Button { Text = "Cancelar", DialogResult = DialogResult.Cancel, Location = new Point(175, 100), Size = new Size(100, 32),
                BackColor = Color.FromArgb(150, 60, 60), ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
            bCan.FlatAppearance.BorderSize = 0;
            bOk.Click += (s, e) => { MontoPago = num.Value; DialogResult = DialogResult.OK; };
            Controls.AddRange(new Control[] { bOk, bCan });
        }
    }
}
