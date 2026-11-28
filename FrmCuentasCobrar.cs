using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace SistemaGestion
{
    public class FrmCuentasCobrar : FormBase
    {
        private DataGridView dgv;
        private Label lblResumen;
        private List<CuentaCobrar> _cuentas = new List<CuentaCobrar>();
        private string _sortCol = "Vence";   // orden inicial por vencimiento
        private bool   _sortAsc = true;

        public FrmCuentasCobrar()
        {
            Text = "Cuentas por Cobrar";
            Size = new Size(950, 560);
            StartPosition = FormStartPosition.CenterParent;
            BackColor = Color.FromArgb(240, 244, 250);

            var hdr = new Panel { Dock = DockStyle.Top, Height = 50, BackColor = Color.FromArgb(26, 74, 138) };
            hdr.Controls.Add(new Label { Text = "Cuentas por Cobrar", ForeColor = Color.White,
                Font = new Font("Segoe UI", 12f, FontStyle.Bold), AutoSize = true, Location = new Point(12, 13) });

            var btnNueva  = Btn("+ Nueva",   Color.FromArgb(40, 160, 80));
            var btnEditar = Btn("Editar",    Color.FromArgb(60, 100, 180));
            var btnPago   = Btn("Reg. Pago", Color.FromArgb(180, 130, 20));
            
            var _btnAy = new Button {
                Text = "? Ayuda", Size = new Size(80, 32),
                BackColor = Color.FromArgb(80, 50, 150), ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
                Cursor = Cursors.Hand, Margin = new Padding(0, 0, 4, 0)
            };
            _btnAy.FlatAppearance.BorderSize = 0;
            _btnAy.Click += (s, e) => AyudaService.Mostrar(this, "FrmCuentasCobrar");
            var pnlHdr = new FlowLayoutPanel {
                Dock = DockStyle.Right, AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false, BackColor = Color.Transparent,
                Padding = new Padding(0, 9, 8, 0)
            };
            pnlHdr.Controls.AddRange(new Control[] { _btnAy, btnNueva, btnEditar, btnPago });
            hdr.Controls.Add(pnlHdr);
            var _tipF = AyudaService.CrearTooltip();
            AyudaService.Tip(_tipF, _btnAy, "Abrir guía de ayuda de este módulo");
            AyudaService.Tip(_tipF, btnNueva, "Registrar una nueva cuenta por cobrar manualmente");
            AyudaService.Tip(_tipF, btnEditar, "Modificar los datos de la cuenta seleccionada");
            AyudaService.Tip(_tipF, btnPago, "Registrar un abono o pago total de la cuenta seleccionada");

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
            // Ordenar al hacer clic en el encabezado de la columna
            dgv.ColumnHeaderMouseClick += (s, e) =>
            {
                string col = dgv.Columns[e.ColumnIndex].Name;
                if (col == _sortCol) _sortAsc = !_sortAsc;
                else { _sortCol = col; _sortAsc = true; }
                Rebind();
            };
            // Doble clic en una fila: abrir la factura asociada (solo lectura)
            dgv.CellDoubleClick += (s, e) => { if (e.RowIndex >= 0) AbrirFactura(); };

            var footer = new Panel { Dock = DockStyle.Bottom, Height = 45, BackColor = Color.FromArgb(215, 240, 240) };
            lblResumen = new Label { AutoSize = true, Location = new Point(12, 12),
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold), ForeColor = Color.FromArgb(20, 80, 80) };
            var btnCerrar = new Button { Text = "Cerrar", Size = new Size(90, 28), Location = new Point(830, 8),
                BackColor = Color.FromArgb(100, 100, 120), ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
            btnCerrar.FlatAppearance.BorderSize = 0;
            btnCerrar.Click += (s, e) => Close();
            footer.Controls.AddRange(new Control[] { lblResumen, btnCerrar });

            Controls.AddRange(new Control[] { dgv, hdr, footer });

            btnNueva.Click  += (s, e) => { using var d = new DlgCuenta(null); if (d.ShowDialog() == DialogResult.OK) { DatabaseService.InsertarCuenta(d.Resultado); 
            VentanaPrefs.Restaurar(this);
            var _vpT = new System.Windows.Forms.Timer { Interval = 800 };
            _vpT.Tick        += (s, e) => { _vpT.Stop(); VentanaPrefs.Guardar(this); };
            SizeChanged     += (s, e) => { _vpT.Stop(); _vpT.Start(); };
            LocationChanged += (s, e) => { _vpT.Stop(); _vpT.Start(); };
            FormClosed      += (s, e) => { _vpT.Stop(); _vpT.Dispose(); };

            CargarDatos(); } };
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
        private Button Btn(string t, Color c)
        { var b = new Button { Text = t, Size = new Size(90, 33), BackColor = c, ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 8.5f, FontStyle.Bold), Cursor = Cursors.Hand }; b.Margin = new Padding(0, 0, 4, 0);
            b.FlatAppearance.BorderSize = 0; return b; }


        private void CargarDatos()
        {
            DatabaseService.SincronizarCxCYFacturas();
            var lista = DatabaseService.ObtenerCuentas();
            // Actualizar vencidas automaticamente
            foreach (var cc in lista.Where(x => x.Estado == "Vigente" && x.FechaVence < DateTime.Today && x.Saldo > 0.005m))
            { cc.Estado = "Vencida"; DatabaseService.ActualizarCuenta(cc); }

            _cuentas = DatabaseService.ObtenerCuentas().Where(x => x.Estado != "Anulada").ToList();
            Rebind();
        }

        // Reconstruye la grilla aplicando el orden actual (_sortCol / _sortAsc).
        private void Rebind()
        {
            var orden = new List<CuentaCobrar>(_cuentas);
            if (!string.IsNullOrEmpty(_sortCol))
            {
                Comparison<CuentaCobrar> cmp = _sortCol switch
                {
                    "Id"       => (a, b) => a.Id.CompareTo(b.Id),
                    "Cliente"  => (a, b) => string.Compare(a.Cliente,  b.Cliente,  StringComparison.OrdinalIgnoreCase),
                    "Concepto" => (a, b) => string.Compare(a.Concepto, b.Concepto, StringComparison.OrdinalIgnoreCase),
                    "Monto"    => (a, b) => a.Monto.CompareTo(b.Monto),
                    "Pagado"   => (a, b) => a.Pagado.CompareTo(b.Pagado),
                    "Saldo"    => (a, b) => a.Saldo.CompareTo(b.Saldo),
                    "Emision"  => (a, b) => a.FechaEmision.CompareTo(b.FechaEmision),
                    "Vence"    => (a, b) => a.FechaVence.CompareTo(b.FechaVence),
                    "Estado"   => (a, b) => string.Compare(a.Estado,   b.Estado,   StringComparison.OrdinalIgnoreCase),
                    _          => (a, b) => a.Id.CompareTo(b.Id)
                };
                orden.Sort(cmp);
                if (!_sortAsc) orden.Reverse();
            }

            dgv.DataSource = null;
            dgv.DataSource = orden.Select(cc => new {
                cc.Id, cc.Cliente, cc.Concepto,
                Monto   = cc.Monto.ToString("C2"),
                Pagado  = cc.Pagado.ToString("C2"),
                Saldo   = cc.Saldo.ToString("C2"),
                Emision = cc.FechaEmision.ToShortDateString(),
                Vence   = cc.FechaVence.ToShortDateString(),
                cc.Estado
            }).ToList();

            // Orden manual: evita que el DataGridView intente ordenar por su cuenta
            // (una List<> enlazada no soporta el ordenamiento automático).
            foreach (DataGridViewColumn c in dgv.Columns)
                c.SortMode = DataGridViewColumnSortMode.Programmatic;
            if (dgv.Columns.Contains(_sortCol))
                dgv.Columns[_sortCol].HeaderCell.SortGlyphDirection =
                    _sortAsc ? SortOrder.Ascending : SortOrder.Descending;

            decimal totalMonto = _cuentas.Sum(x => x.Monto);
            decimal totalSaldo = _cuentas.Sum(x => x.Saldo);
            lblResumen.Text = $"  Cuentas: {_cuentas.Count}   |   Total cargado: {totalMonto:C2}   |   Saldo pendiente: {totalSaldo:C2}   |   Vencidas: {_cuentas.Count(x => x.Estado == "Vencida")}";
        }

        // Abre la factura asociada a la cuenta seleccionada (el Concepto es "Factura <N°>").
        private void AbrirFactura()
        {
            if (dgv.CurrentRow == null) return;
            string concepto = dgv.CurrentRow.Cells["Concepto"].Value?.ToString() ?? "";
            string num = concepto.StartsWith("Factura ", StringComparison.OrdinalIgnoreCase)
                       ? concepto.Substring("Factura ".Length).Trim()
                       : "";
            if (string.IsNullOrWhiteSpace(num))
            {
                MessageBox.Show("Esta cuenta no está asociada a una factura.",
                    "Sin factura", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var f = DatabaseService.ObtenerFacturas(true, true)
                        .Find(x => string.Equals(x.NumeroF, num, StringComparison.OrdinalIgnoreCase));
            if (f == null)
            {
                MessageBox.Show($"No se encontró la factura {num}.",
                    "No encontrada", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var lineas = DatabaseService.ObtenerDetallesFacturaPorNumero(f.NumeroF);
            using var dlg = new DlgFactura(f, lineas, soloLectura: true);
            dlg.ShowDialog();
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
            if (cc.Estado == "Saldada" || cc.Estado == "Pagada" || cc.Saldo <= 0.005m)
            {
                MessageBox.Show("Esta cuenta ya está saldada.", "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            using var dlg = new DlgPago(cc.Saldo);
            if (dlg.ShowDialog(this) == DialogResult.OK && dlg.MontoPago > 0)
            {
                try
                {
                    var (aplicado, saldoRest, saldada) = DatabaseService.RegistrarPagoCxC(
                        cc.Id, dlg.MontoPago, dlg.MetodoPago, dlg.Referencia, dlg.Observaciones);
                    CargarDatos();
                    string msg = saldada
                        ? $"Pago de {aplicado:C2} registrado. La cuenta y su factura quedaron saldadas."
                        : $"Pago de {aplicado:C2} registrado. Saldo restante: {saldoRest:C2}.";
                    MessageBox.Show(this, msg, "Pago Registrado", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(this, "Error al registrar el pago:\n" + ex.Message,
                        "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }
    }

    public class DlgCuenta : FormBase
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
            FormBorderStyle = FormBorderStyle.Sizable; MaximizeBox = true; MinimizeBox = false;
            BackColor = Color.FromArgb(240, 244, 250);

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
                BackColor = Color.FromArgb(30, 130, 70), ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
            bOk.FlatAppearance.BorderSize = 0;
            var bCan = new Button { Text = "Cancelar", DialogResult = DialogResult.Cancel, Location = new Point(200, y), Size = new Size(110, 34),
                BackColor = Color.FromArgb(160, 50, 50), ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
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
            
            
            var _dlgTip = AyudaService.CrearTooltip();
            if (txtCliente != null) AyudaService.Tip(_dlgTip, txtCliente, "Nombre del cliente que debe");
            if (txtConcepto != null) AyudaService.Tip(_dlgTip, txtConcepto, "Descripción de la deuda o servicio");
            if (numMonto != null) AyudaService.Tip(_dlgTip, numMonto, "Monto total de la deuda en USD");
            if (numPagado != null) AyudaService.Tip(_dlgTip, numPagado, "Monto ya recibido como pago");
            if (dtpVence != null) AyudaService.Tip(_dlgTip, dtpVence, "Fecha límite de pago — después genera alerta de vencimiento");
            // Recordar posición y tamaño
            VentanaPrefs.Restaurar(this);
            Controls.AddRange(new Control[] { bOk, bCan });
        }

        private void L(string t, int y) => Controls.Add(new Label { Text = t, Location = new Point(15, y + 3), AutoSize = true, Font = new Font("Segoe UI", 9f, FontStyle.Bold) });
        private TextBox T(string v, int y) { var tb = new TextBox { Text = v, Location = new Point(150, y), Size = new Size(230, 25) }; Controls.Add(tb); return tb; }
        private NumericUpDown N(int y, decimal max) { var n = new NumericUpDown { Location = new Point(150, y), Size = new Size(230, 25), Maximum = max, DecimalPlaces = 2 }; Controls.Add(n); return n; }
        private DateTimePicker D(DateTime v, int y) { var dp = new DateTimePicker { Value = v, Location = new Point(150, y), Size = new Size(230, 25), Format = DateTimePickerFormat.Short }; Controls.Add(dp); return dp; }
    }

    public class DlgPago : FormBase
    {
        public decimal MontoPago     { get; private set; }
        public string  MetodoPago    { get; private set; } = "Efectivo";
        public string  Referencia    { get; private set; } = "";
        public string  Observaciones { get; private set; } = "";

        public DlgPago(decimal saldo)
        {
            Text = "Registrar Pago";
            ClientSize = new Size(390, 255);
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            BackColor = Color.FromArgb(240, 244, 250);

            int y = 18;
            Controls.Add(new Label {
                Text = $"Saldo pendiente: {saldo:C2}",
                Location = new Point(20, y), AutoSize = true,
                Font = new Font("Segoe UI", 11f, FontStyle.Bold),
                ForeColor = Color.FromArgb(26, 74, 138)
            });
            y += 38;

            Controls.Add(new Label { Text = "Monto a pagar:", Location = new Point(20, y + 3), AutoSize = true, Font = new Font("Segoe UI", 9.5f) });
            var num = new NumericUpDown {
                Location = new Point(150, y), Size = new Size(215, 26),
                Maximum = Math.Max(saldo, 0.01m), Minimum = 0.01m,
                Value = Math.Max(saldo, 0.01m), DecimalPlaces = 2,
                Font = new Font("Segoe UI", 9.5f)
            };
            Controls.Add(num);
            y += 38;

            Controls.Add(new Label { Text = "Método de pago:", Location = new Point(20, y + 3), AutoSize = true, Font = new Font("Segoe UI", 9.5f) });
            var cmbMetodo = new ComboBox {
                Location = new Point(150, y), Size = new Size(215, 26),
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("Segoe UI", 9.5f)
            };
            cmbMetodo.Items.AddRange(new object[] { "Efectivo", "Zelle", "TransfNac", "PagoMovil" });
            cmbMetodo.SelectedIndex = 0;
            Controls.Add(cmbMetodo);
            y += 38;

            Controls.Add(new Label { Text = "Referencia:", Location = new Point(20, y + 3), AutoSize = true, Font = new Font("Segoe UI", 9.5f) });
            var txtRef = new TextBox {
                Location = new Point(150, y), Size = new Size(215, 26),
                Font = new Font("Segoe UI", 9.5f)
            };
            Controls.Add(txtRef);
            y += 48;

            var bOk = new Button {
                Text = "Registrar", Location = new Point(150, y), Size = new Size(102, 34),
                BackColor = Color.FromArgb(30, 130, 70), ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            bOk.FlatAppearance.BorderSize = 0;
            var bCan = new Button {
                Text = "Cancelar", DialogResult = DialogResult.Cancel,
                Location = new Point(263, y), Size = new Size(102, 34),
                BackColor = Color.FromArgb(160, 50, 50), ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            bCan.FlatAppearance.BorderSize = 0;
            bOk.Click += (s, e) =>
            {
                if (num.Value <= 0)
                {
                    MessageBox.Show(this, "El monto debe ser mayor a cero.", "Validación", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                MontoPago  = num.Value;
                MetodoPago = cmbMetodo.SelectedItem?.ToString() ?? "Efectivo";
                Referencia = txtRef.Text.Trim();
                DialogResult = DialogResult.OK;
            };
            AcceptButton = bOk;
            CancelButton = bCan;
            Controls.AddRange(new Control[] { bOk, bCan });
        }
    }
}
