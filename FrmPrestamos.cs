using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace SistemaGestion
{
    public class FrmPrestamos : FormBase
    {
        private DataGridView dgv;
        private Label        lblRes;

        public FrmPrestamos()
        {
            Text = "Prestamos Internos a Empleados";
            Size = new Size(1000, 540); StartPosition = FormStartPosition.CenterParent;
            BackColor = Color.FromArgb(240, 244, 250);

            var hdr = new Panel { Dock = DockStyle.Top, Height = 50,
                BackColor = Color.FromArgb(26, 74, 138) };
            hdr.Controls.Add(new Label { Text = "Prestamos a Empleados",
                ForeColor = Color.White, Font = new Font("Segoe UI", 12f, FontStyle.Bold),
                AutoSize = true, Location = new Point(12, 13) });

            var btnN = Btn("+ Nuevo",   Color.FromArgb(40, 160, 80));
            var btnAb= Btn("Abonar",    Color.FromArgb(60, 110, 190));
            var btnC = Btn("Cancelar",  Color.FromArgb(180, 50, 50));
            
            var _btnAy = new Button {
                Text = "? Ayuda", Size = new Size(80, 32),
                BackColor = Color.FromArgb(80, 50, 150), ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
                Cursor = Cursors.Hand, Margin = new Padding(0, 0, 4, 0)
            };
            _btnAy.FlatAppearance.BorderSize = 0;
            _btnAy.Click += (s, e) => AyudaService.Mostrar(this, "FrmPrestamos");
            var pnlHdr = new FlowLayoutPanel {
                Dock = DockStyle.Right, AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false, BackColor = Color.Transparent,
                Padding = new Padding(0, 9, 8, 0)
            };
            pnlHdr.Controls.AddRange(new Control[] { _btnAy, btnN, btnAb, btnC });
            hdr.Controls.Add(pnlHdr);
            var _tipF = AyudaService.CrearTooltip();
            AyudaService.Tip(_tipF, _btnAy, "Abrir guía de ayuda de este módulo");
            AyudaService.Tip(_tipF, btnN, "Registrar un nuevo préstamo a un empleado");
            AyudaService.Tip(_tipF, btnAb, "Registrar un abono manual al préstamo seleccionado");
            AyudaService.Tip(_tipF, btnC, "Cancelar el préstamo seleccionado");

            dgv = new DataGridView
            {
                Dock = DockStyle.Fill, ReadOnly = true,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false, AllowUserToAddRows = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                BackgroundColor = Color.White, BorderStyle = BorderStyle.None,
                RowHeadersVisible = false, Font = new Font("Segoe UI", 9f),
                RowTemplate = { Height = 24 }
            };
            dgv.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(140, 80, 20);
            dgv.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
            dgv.ColumnHeadersDefaultCellStyle.Font      = new Font("Segoe UI", 9f, FontStyle.Bold);
            dgv.EnableHeadersVisualStyles = false;
            dgv.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(255, 245, 235);
            dgv.DataBindingComplete += (s, e) => ColorearFilas();

            var footer = new Panel { Dock = DockStyle.Bottom, Height = 38,
                BackColor = Color.FromArgb(240, 225, 210) };
            lblRes = new Label { AutoSize = true, Location = new Point(12, 10),
                Font = new Font("Segoe UI", 9f), ForeColor = Color.FromArgb(100, 50, 10) };
            var btnCerrar = new Button { Text = "Cerrar", Size = new Size(90, 26),
                Location = new Point(890, 6), BackColor = Color.FromArgb(100, 100, 120),
                ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
            btnCerrar.FlatAppearance.BorderSize = 0;
            btnCerrar.Click += (s, e) => Close();
            footer.Controls.AddRange(new Control[] { lblRes, btnCerrar });

            Controls.AddRange(new Control[] { dgv, hdr, footer });

            btnN.Click  += (s, e) => NuevoPrestamo();
            btnAb.Click += (s, e) => AbonarManual();
            btnC.Click  += (s, e) => CancelarPrestamo();

            
            VentanaPrefs.Restaurar(this);
            var _vpT = new System.Windows.Forms.Timer { Interval = 800 };
            _vpT.Tick        += (s, e) => { _vpT.Stop(); VentanaPrefs.Guardar(this); };
            SizeChanged     += (s, e) => { _vpT.Stop(); _vpT.Start(); };
            LocationChanged += (s, e) => { _vpT.Stop(); _vpT.Start(); };
            FormClosed      += (s, e) => { _vpT.Stop(); _vpT.Dispose(); };

            Cargar();
        }

        private void Cargar()
        {
            var lista = DatabaseService.ObtenerPrestamos();
            dgv.DataSource = null;
            dgv.DataSource = lista.Select(p => new
            {
                p.Id, Empleado = p.NombreEmpleado,
                Otorgado  = p.FechaOtorgado.ToShortDateString(),
                Total     = p.MontoTotalUSD.ToString("C2"),
                CuotaSem  = p.CuotaSemanalUSD.ToString("C2"),
                Saldo     = p.SaldoPendiente.ToString("C2"),
                Semanas   = p.CuotaSemanalUSD > 0
                    ? ((int)Math.Ceiling((double)p.SaldoPendiente / (double)p.CuotaSemanalUSD)).ToString() + " sem."
                    : "—",
                p.Estado, p.Descripcion
            }).ToList();

            decimal totalSaldo = lista.Where(p => p.Estado == "Activo").Sum(p => p.SaldoPendiente);
            lblRes.Text =
                "  Prestamos activos: " + lista.Count(p => p.Estado == "Activo") +
                "   |   Saldo total pendiente: $" + totalSaldo.ToString("N2") +
                "   |   Saldados: " + lista.Count(p => p.Estado == "Saldado");
        }

        private void ColorearFilas()
        {
            foreach (DataGridViewRow row in dgv.Rows)
            {
                var est = row.Cells["Estado"].Value?.ToString();
                if (est == "Saldado")  row.DefaultCellStyle.BackColor = Color.FromArgb(225, 255, 225);
                else if (est == "Cancelado") row.DefaultCellStyle.BackColor = Color.FromArgb(245, 235, 235);
            }
        }

        private void NuevoPrestamo()
        {
            var empleados = DatabaseService.ObtenerEmpleados(soloActivos: true);
            if (empleados.Count == 0)
            { MessageBox.Show("No hay empleados activos registrados.", "Aviso", MessageBoxButtons.OK, MessageBoxIcon.Information); return; }

            using var dlg = new DlgNuevoPrestamo(empleados);
            if (dlg.ShowDialog() == DialogResult.OK)
            {
                DatabaseService.InsertarPrestamo(dlg.Resultado);
                Cargar();
            }
        }

        private void AbonarManual()
        {
            if (dgv.CurrentRow == null) return;
            int id = (int)dgv.CurrentRow.Cells["Id"].Value;
            var pr = DatabaseService.ObtenerPrestamos().Find(p => p.Id == id);
            if (pr == null || pr.Estado != "Activo") return;

            using var dlg = new DlgAbonoManual(pr);
            if (dlg.ShowDialog() == DialogResult.OK)
            {
                DatabaseService.AbonarPrestamo(id, dlg.Monto);
                Cargar();
            }
        }

        private void CancelarPrestamo()
        {
            if (dgv.CurrentRow == null) return;
            int id = (int)dgv.CurrentRow.Cells["Id"].Value;
            if (MessageBox.Show("¿Cancelar este préstamo?", "Confirmar",
                MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes)
            {
                try
                {
                    // ALTA-3 FIX: Usar DatabaseService centralizado con auditoría y control de permisos
                    DatabaseService.CancelarPrestamo(id);
                    Cargar();
                }
                catch (UnauthorizedAccessException)
                {
                    MessageBox.Show("Solo el administrador puede cancelar préstamos.",
                        "Sin permiso", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error al cancelar: {ex.Message}", "Error",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private Button Btn(string t, Color c, Point p)
        {
            var b = new Button { Text = t, Size = new Size(90, 33), Location = p, BackColor = c,
                ForeColor = Color.White, FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 8.5f, FontStyle.Bold), Cursor = Cursors.Hand };
            b.FlatAppearance.BorderSize = 0; return b;
        }
        private Button Btn(string t, Color c)
        {
            var b = new Button { Text = t, Size = new Size(90, 33), BackColor = c,
                ForeColor = Color.White, FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 8.5f, FontStyle.Bold), Cursor = Cursors.Hand };
            b.Margin = new Padding(0, 0, 4, 0);
            b.FlatAppearance.BorderSize = 0; return b;
        }

    }

    public class DlgNuevoPrestamo : Form
    {
        public PrestamoEmpleado Resultado { get; private set; }

        public DlgNuevoPrestamo(System.Collections.Generic.List<Empleado> empleados)
        {
            Text = "Nuevo Prestamo Interno";
            Size = new Size(420, 340); StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.Sizable; MaximizeBox = true; MinimizeBox = false;
            BackColor = Color.FromArgb(240, 244, 250);

            var banner = new Panel { Dock = DockStyle.Top, Height = 38, BackColor = Color.FromArgb(26, 74, 138) };
            banner.Controls.Add(new Label { Text = Text, ForeColor = Color.White,
                Font = new Font("Segoe UI", 10f, FontStyle.Bold), AutoSize = true, Location = new Point(12, 9) });
            Controls.Add(banner);

            int y = 52;
            void L(string t) => Controls.Add(new Label { Text = t, Location = new Point(16, y + 3),
                AutoSize = true, Font = new Font("Segoe UI", 9f, FontStyle.Bold) });

            L("Empleado:");
            var cmbEmp = new ComboBox { Location = new Point(160, y), Size = new Size(230, 26),
                DropDownStyle = ComboBoxStyle.DropDownList };
            foreach (var e in empleados)
                cmbEmp.Items.Add(e.NombreCompleto + " (" + e.Cedula + ")");
            cmbEmp.SelectedIndex = 0;
            Controls.Add(cmbEmp); y += 36;

            L("Monto total USD:");
            var numMonto = new NumericUpDown { Location = new Point(160, y), Size = new Size(230, 26),
                Maximum = 999999, DecimalPlaces = 2 };
            Controls.Add(numMonto); y += 36;

            L("Cuota semanal USD:");
            var numCuota = new NumericUpDown { Location = new Point(160, y), Size = new Size(230, 26),
                Maximum = 99999, DecimalPlaces = 2 };
            Controls.Add(numCuota); y += 36;

            L("Descripcion:");
            var txtDesc = new TextBox { Location = new Point(160, y), Size = new Size(230, 24) };
            Controls.Add(txtDesc); y += 44;

            var bOk = new Button { Text = "Registrar prestamo", Location = new Point(60, y), Size = new Size(170, 33),
                BackColor = Color.FromArgb(30, 130, 70), ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 9f, FontStyle.Bold) };
            bOk.FlatAppearance.BorderSize = 0;
            var bCan = new Button { Text = "Cancelar", DialogResult = DialogResult.Cancel,
                Location = new Point(248, y), Size = new Size(110, 33),
                BackColor = Color.FromArgb(120, 120, 130), ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
            bCan.FlatAppearance.BorderSize = 0;

            bOk.Click += (s, e) =>
            {
                if (numMonto.Value <= 0)
                { MessageBox.Show("Ingresa el monto.", "Validacion", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
                var emp = empleados[cmbEmp.SelectedIndex];
                Resultado = new PrestamoEmpleado
                {
                    IdEmpleado = emp.Id, NombreEmpleado = emp.NombreCompleto,
                    FechaOtorgado = DateTime.Today, MontoTotalUSD = numMonto.Value,
                    CuotaSemanalUSD = numCuota.Value, SaldoPendiente = numMonto.Value,
                    Estado = "Activo", Descripcion = txtDesc.Text.Trim()
                };
                DialogResult = DialogResult.OK;
            };
            
            
            var _dlgTip = AyudaService.CrearTooltip();
            if (numMonto != null) AyudaService.Tip(_dlgTip, numMonto, "Monto total del préstamo en USD");
            if (numCuota != null) AyudaService.Tip(_dlgTip, numCuota, "Cuota semanal que se descontará de la nómina");
            if (txtDesc != null) AyudaService.Tip(_dlgTip, txtDesc, "Motivo o descripción del préstamo");
            // Recordar posición y tamaño
            VentanaPrefs.Restaurar(this);
            Controls.AddRange(new Control[] { bOk, bCan });
        
            VentanaPrefs.Restaurar(this);
        }
    }

    public class DlgAbonoManual : Form
    {
        public decimal Monto { get; private set; }

        public DlgAbonoManual(PrestamoEmpleado pr)
        {
            Text = "Abono a prestamo — " + pr.NombreEmpleado;
            Size = new Size(360, 210); StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.Sizable; MaximizeBox = true; MinimizeBox = false;
            BackColor = Color.FromArgb(240, 244, 250);

            var banner = new Panel { Dock = DockStyle.Top, Height = 38, BackColor = Color.FromArgb(26, 74, 138) };
            banner.Controls.Add(new Label { Text = "Abono a prestamo", ForeColor = Color.White,
                Font = new Font("Segoe UI", 10f, FontStyle.Bold), AutoSize = true, Location = new Point(12, 9) });
            Controls.Add(banner);

            Controls.Add(new Label { Text = "Saldo pendiente: $" + pr.SaldoPendiente.ToString("N2"),
                Location = new Point(20, 55), AutoSize = true, Font = new Font("Segoe UI", 9f), ForeColor = Color.Gray });

            Controls.Add(new Label { Text = "Monto a abonar:",
                Location = new Point(20, 90), AutoSize = true, Font = new Font("Segoe UI", 9f, FontStyle.Bold) });
            var num = new NumericUpDown { Location = new Point(170, 87), Size = new Size(150, 26),
                Maximum = pr.SaldoPendiente, DecimalPlaces = 2, Value = pr.CuotaSemanalUSD };
            Controls.Add(num);

            var bOk = new Button { Text = "Registrar abono", Location = new Point(50, 135),
                Size = new Size(150, 32), BackColor = Color.FromArgb(30, 130, 70),
                ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 9f, FontStyle.Bold) };
            bOk.FlatAppearance.BorderSize = 0;
            bOk.Click += (s, e) => { Monto = num.Value; DialogResult = DialogResult.OK; };
            var bCan = new Button { Text = "Cancelar", DialogResult = DialogResult.Cancel,
                Location = new Point(215, 135), Size = new Size(100, 32),
                BackColor = Color.FromArgb(120, 120, 130), ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
            bCan.FlatAppearance.BorderSize = 0;
            
            
            var _dlgTip = AyudaService.CrearTooltip();
            if (num != null) AyudaService.Tip(_dlgTip, num, "Monto a abonar en USD (no puede superar el saldo pendiente)");
            // Recordar posición y tamaño
            VentanaPrefs.Restaurar(this);
            Controls.AddRange(new Control[] { bOk, bCan });
        }
    }
}
