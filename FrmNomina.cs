using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace SistemaGestion
{
    // ══════════════════════════════════════════════════════════════════════════
    //  SUBMENU NOMINA
    // ══════════════════════════════════════════════════════════════════════════
    public class FrmNomina : FormBase
    {
        public FrmNomina()
        {
            Text = "Nómina";
            Size = new Size(560, 420);
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.Sizable;
            MaximizeBox = true;
            BackColor = Color.FromArgb(240, 244, 250);

            // ── Header ────────────────────────────────────────────────────
            var hdr = new Panel { Dock = DockStyle.Top, Height = 50, BackColor = Color.FromArgb(26, 74, 138) };
            hdr.Controls.Add(new Label {
                Text = "Módulo de Nómina", ForeColor = Color.White,
                Font = new Font("Segoe UI", 12f, FontStyle.Bold),
                AutoSize = true, Location = new Point(16, 13) });

            // ── Grid 2×2 de módulos ───────────────────────────────────────
            var items = new (string ico, string titulo, string sub, Color color, Action accion)[] {
                ("◎", "Empleados",        "Catálogo y altas",      Color.FromArgb(40,  80,160), () => new FrmEmpleados().ShowDialog()),
                ("▣", "Procesar Nómina",  "Nómina semanal",        Color.FromArgb(26,  74,138), () => new FrmProcesarNomina().ShowDialog()),
                ("◫", "Préstamos",        "Préstamos internos",    Color.FromArgb(140, 80, 20), () => new FrmPrestamos().ShowDialog()),
                ("◈", "Beneficios LOTTT", "Utilidades y prestaciones", Color.FromArgb(100,40,150), () => new FrmBeneficiosLOTTT().ShowDialog()),
            };

            
            var _menuTip = AyudaService.CrearTooltip();
            var pnlGrid = new Panel { BackColor = Color.FromArgb(240, 244, 250) };
            int cols = 2, btnW = 220, btnH = 110, gap = 12, padX = 24, padY = 16;
            pnlGrid.Size = new Size(cols * btnW + gap + padX * 2, 2 * btnH + gap + padY * 2);
            pnlGrid.Location = new Point(0, 50);
            pnlGrid.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;

            for (int i = 0; i < items.Length; i++)
            {
                var (ico, titulo, sub, color, accion) = items[i];
                int col = i % cols, row = i / cols;
                var card = new Panel {
                    Size = new Size(btnW, btnH),
                    Location = new Point(padX + col * (btnW + gap), padY + row * (btnH + gap)),
                    BackColor = Color.White, Cursor = Cursors.Hand
                };
                var lIco = new Label { Text = ico, Font = new Font("Segoe UI", 20f),
                    ForeColor = color, AutoSize = false, Size = new Size(40, 40), Location = new Point(12, 12) };
                var lTit = new Label { Text = titulo, Font = new Font("Segoe UI", 10f, FontStyle.Bold),
                    ForeColor = Color.FromArgb(30, 41, 59), AutoSize = true, Location = new Point(56, 18) };
                var lSub = new Label { Text = sub, Font = new Font("Segoe UI", 8f),
                    ForeColor = Color.FromArgb(100, 116, 139), AutoSize = true, Location = new Point(56, 38) };
                var acc = accion;
                card.Controls.AddRange(new Control[] { lIco, lTit, lSub });
                card.Click  += (s, e) => acc();
                lIco.Click  += (s, e) => acc();
                lTit.Click  += (s, e) => acc();
                lSub.Click  += (s, e) => acc();
                card.MouseEnter += (s, e) => { card.BackColor = Color.FromArgb(232, 240, 250); };
                card.MouseLeave += (s, e) => { card.BackColor = Color.White; };
                card.Paint += (s, e) => {
                    e.Graphics.DrawRectangle(new Pen(Color.FromArgb(220, 230, 245), 1f),
                        0, 0, card.Width - 1, card.Height - 1);
                };
                switch (titulo) {
                case "Empleados": AyudaService.Tip(_menuTip, card, "Ver y gestionar el catálogo de empleados"); break;
                case "Procesar Nómina": AyudaService.Tip(_menuTip, card, "Calcular y cerrar la nómina semanal"); break;
                case "Préstamos": AyudaService.Tip(_menuTip, card, "Gestionar préstamos internos a empleados"); break;
                case "Beneficios LOTTT": AyudaService.Tip(_menuTip, card, "Calcular utilidades y prestaciones sociales"); break;
                }
                pnlGrid.Controls.Add(card);
            }

            // ── Footer ────────────────────────────────────────────────────
            var footer = new Panel { Dock = DockStyle.Bottom, Height = 46, BackColor = Color.FromArgb(240, 244, 250) };
            footer.Paint += (s, e) => e.Graphics.DrawLine(new Pen(Color.FromArgb(220, 230, 245)),
                0, 0, footer.Width, 0);
            var btnCerrar = new Button {
                Text = "Cerrar", Size = new Size(110, 30),
                Location = new Point(this.ClientSize.Width / 2 - 55, 8),
                Anchor = AnchorStyles.Top, BackColor = Color.FromArgb(100, 116, 139),
                ForeColor = Color.White, FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold) };
            btnCerrar.FlatAppearance.BorderSize = 0;
            btnCerrar.Click += (s, e) => Close();
            footer.Controls.Add(btnCerrar);

            Controls.AddRange(new Control[] { pnlGrid, hdr, footer });

            // Ajustar tamaño al grid
            ClientSize = new Size(cols * btnW + gap + padX * 2,
                50 + 2 * btnH + gap + padY * 2 + 46);

            VentanaPrefs.Restaurar(this);
            var _tpVP = new System.Windows.Forms.Timer { Interval = 800 };
            _tpVP.Tick      += (s, e) => { _tpVP.Stop(); VentanaPrefs.Guardar(this); };
            SizeChanged     += (s, e) => { _tpVP.Stop(); _tpVP.Start(); };
            LocationChanged += (s, e) => { _tpVP.Stop(); _tpVP.Start(); };
            FormClosed      += (s, e) => { _tpVP.Stop(); _tpVP.Dispose(); };

        }
    }  // fin FrmNomina

    // ══════════════════════════════════════════════════════════════════════════
    //  CATALOGO DE EMPLEADOS
    // ══════════════════════════════════════════════════════════════════════════
    public class FrmEmpleados : Form
    {
        private DataGridView dgv;
        private Label        lblRes;
        private CheckBox     chkSoloActivos;

        public FrmEmpleados()
        {
            Text = "Catalogo de Empleados";
            Size = new Size(1100, 560);
            StartPosition = FormStartPosition.CenterParent;
            BackColor = Color.FromArgb(245, 247, 255);

            var hdr = Hdr("Empleados", Color.FromArgb(26, 74, 138));
            var btnN = Btn("+ Nuevo",     Color.FromArgb(40, 160, 80));
            var btnE = Btn("Editar",      Color.FromArgb(60, 110, 190));
            var btnD = Btn("Dar de baja", Color.FromArgb(180, 50, 50));
            btnD.Width = 100;

            var pnlHdrRight = new FlowLayoutPanel {
                Dock = DockStyle.Right, AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false, BackColor = Color.Transparent,
                Padding = new Padding(0, 9, 8, 0)
            };
            pnlHdrRight.Controls.AddRange(new Control[] { btnN, btnE, btnD });
            hdr.Controls.Add(pnlHdrRight);

            var toolBar = new Panel { Dock = DockStyle.Top, Height = 36,
                BackColor = Color.FromArgb(228, 232, 248) };
            toolBar.Controls.Add(new Label { Text = "Filtrar:", Location = new Point(10, 10), AutoSize = true });
            chkSoloActivos = new CheckBox
            {
                Text = "Solo activos", Location = new Point(60, 8),
                AutoSize = true, Checked = true, Font = new Font("Segoe UI", 9f)
            };
            chkSoloActivos.CheckedChanged += (s, e) => Cargar();
            toolBar.Controls.Add(chkSoloActivos);

            dgv = Grid(Color.FromArgb(40, 80, 160));
            dgv.DoubleClick += (s, e) => Editar();

            var footer = new Panel { Dock = DockStyle.Bottom, Height = 38,
                BackColor = Color.FromArgb(218, 222, 248) };
            lblRes = new Label { AutoSize = true, Location = new Point(12, 10),
                Font = new Font("Segoe UI", 9f), ForeColor = Color.FromArgb(30, 50, 130) };
            var btnCerrar = BtnFooter("Cerrar", new Point(990, 6), footer);
            btnCerrar.Click += (s, e) => Close();
            footer.Controls.Add(lblRes);

            Controls.AddRange(new Control[] { dgv, toolBar, hdr, footer });

            btnN.Click += (s, e) => { using var d = new DlgEmpleado(null); if (d.ShowDialog() == DialogResult.OK) { DatabaseService.InsertarEmpleado(d.Resultado); Cargar(); } };
            btnE.Click += (s, e) => Editar();
            btnD.Click += (s, e) => DarDeBaja();

            Cargar();
        }

        private void Cargar()
        {
            var lista = DatabaseService.ObtenerEmpleados(chkSoloActivos.Checked);
            dgv.DataSource = null;
            dgv.DataSource = lista.Select(e => new
            {
                e.Id, e.Cedula, e.NombreCompleto, e.Cargo, e.TipoEmpleado,
                Ingreso    = e.FechaIngreso.ToShortDateString(),
                Antiguedad = e.AniosServicio + " anos",
                SueldoDia  = e.SueldoDiarioUSD.ToString("C2"),
                TarifaPieza= e.TarifaPiezaUSD > 0 ? e.TarifaPiezaUSD.ToString("C2") : "—",
                TarifaVenta= e.TarifaVentaUSD > 0 ? e.TarifaVentaUSD.ToString("C2") : "—",
                Estado     = e.Activo ? "Activo" : "Inactivo"
            }).ToList();

            var all = DatabaseService.ObtenerEmpleados();
            lblRes.Text = "  Empleados: " + all.Count(e => e.Activo) + " activos  |  " +
                all.Count(e => e.TipoEmpleado == "Produccion") + " produccion  |  " +
                all.Count(e => e.TipoEmpleado == "Oficina")    + " oficina  |  " +
                all.Count(e => e.TipoEmpleado == "Fijo")       + " fijo";
        }

        private void Editar()
        {
            var emp = GetSel(); if (emp == null) return;
            using var d = new DlgEmpleado(emp);
            if (d.ShowDialog() == DialogResult.OK) { DatabaseService.ActualizarEmpleado(d.Resultado); Cargar(); }
        }

        private void DarDeBaja()
        {
            var emp = GetSel(); if (emp == null) return;
            if (MessageBox.Show("Dar de baja a " + emp.NombreCompleto + "?", "Confirmar",
                MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes)
            {
                emp.Activo = !emp.Activo;
                DatabaseService.ActualizarEmpleado(emp);
                Cargar();
            }
        }

        private Empleado GetSel()
        {
            if (dgv.CurrentRow == null) return null;
            int id = (int)dgv.CurrentRow.Cells["Id"].Value;
            return DatabaseService.ObtenerEmpleados().Find(e => e.Id == id);
        }

        private Panel Hdr(string t, Color c)
        {
            var p = new Panel { Dock = DockStyle.Top, Height = 50, BackColor = c };
            p.Controls.Add(new Label { Text = t, ForeColor = Color.White,
                Font = new Font("Segoe UI", 12f, FontStyle.Bold), AutoSize = true, Location = new Point(12, 13) });
            return p;
        }
        private Button Btn(string t, Color c, Point p, int w = 90)
        {
            var b = new Button { Text = t, Size = new Size(w, 33), Location = p, BackColor = c,
                ForeColor = Color.White, FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 8f, FontStyle.Bold), Cursor = Cursors.Hand };
            b.FlatAppearance.BorderSize = 0; return b;
        }
        private Button Btn(string t, Color c)
        {
            var b = new Button { Text = t, Size = new Size(90, 32), BackColor = c,
                ForeColor = Color.White, FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 8f, FontStyle.Bold), Cursor = Cursors.Hand,
                Margin = new Padding(0, 0, 4, 0) };
            b.FlatAppearance.BorderSize = 0; return b;
        }
        private Button BtnFooter(string t, Point p, Panel footer)
        {
            var b = new Button { Text = t, Size = new Size(90, 26), Location = p,
                BackColor = Color.FromArgb(100, 100, 120), ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
            b.FlatAppearance.BorderSize = 0; footer.Controls.Add(b); return b;
        }
        private DataGridView Grid(Color h)
        {
            var g = new DataGridView
            {
                Dock = DockStyle.Fill, ReadOnly = true,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false, AllowUserToAddRows = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                BackgroundColor = Color.White, BorderStyle = BorderStyle.None,
                RowHeadersVisible = false, Font = new Font("Segoe UI", 9f),
                RowTemplate = { Height = 24 }
            };
            g.ColumnHeadersDefaultCellStyle.BackColor = h;
            g.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
            g.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 9f, FontStyle.Bold);
            g.EnableHeadersVisualStyles = false;
            g.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(238, 242, 255);
            return g;
        }
    }

    // ══════════════════════════════════════════════════════════════════════════
    //  DIALOGO EMPLEADO
    // ══════════════════════════════════════════════════════════════════════════
    public class DlgEmpleado : Form
    {
        public Empleado Resultado { get; private set; }

        private TextBox       txtCedula, txtNombre, txtApellido, txtCargo, txtTel, txtEmail;
        private ComboBox      cmbTipo;
        private DateTimePicker dtpIngreso;
        private NumericUpDown  numSueldo, numTarPieza, numTarVenta, numSalNormal;
        private CheckBox       chkActivo;
        private Panel          pnlTarPieza, pnlTarVenta;

        public DlgEmpleado(Empleado e)
        {
            bool nuevo = e == null;
            Text = nuevo ? "Nuevo Empleado" : "Editar — " + e?.NombreCompleto;
            Size = new Size(520, 560); StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.Sizable; MaximizeBox = true; MinimizeBox = false;
            BackColor = Color.FromArgb(240, 244, 250);

            var banner = new Panel { Dock = DockStyle.Top, Height = 38,
                BackColor = Color.FromArgb(26, 74, 138) };
            banner.Controls.Add(new Label { Text = Text, ForeColor = Color.White,
                Font = new Font("Segoe UI", 10f, FontStyle.Bold), AutoSize = true, Location = new Point(12, 9) });
            Controls.Add(banner);

            int y = 50;
            void L(string t) => Controls.Add(new Label { Text = t, Location = new Point(16, y + 3),
                AutoSize = true, Font = new Font("Segoe UI", 9f, FontStyle.Bold) });
            TextBox T(string v = "") { var tb = new TextBox { Text = v,
                Location = new Point(175, y), Size = new Size(315, 24) }; Controls.Add(tb); return tb; }
            NumericUpDown N(decimal v, decimal max = 99999) { var n = new NumericUpDown {
                Location = new Point(175, y), Size = new Size(200, 24),
                Maximum = max, DecimalPlaces = 2, Value = v }; Controls.Add(n); return n; }

            L("Cedula:");        txtCedula   = T(e?.Cedula   ?? ""); y += 32;
            L("Nombre:");        txtNombre   = T(e?.Nombre   ?? ""); y += 32;
            L("Apellido:");      txtApellido = T(e?.Apellido ?? ""); y += 32;
            L("Cargo:");         txtCargo    = T(e?.Cargo    ?? ""); y += 32;

            L("Tipo empleado:");
            cmbTipo = new ComboBox { Location = new Point(175, y), Size = new Size(200, 24),
                DropDownStyle = ComboBoxStyle.DropDownList };
            cmbTipo.Items.AddRange(new object[] { "Produccion", "Oficina", "Fijo" });
            cmbTipo.SelectedItem = e?.TipoEmpleado ?? "Fijo";
            if (cmbTipo.SelectedIndex < 0) cmbTipo.SelectedIndex = 2;
            Controls.Add(cmbTipo); y += 32;

            L("Fecha ingreso:");
            dtpIngreso = new DateTimePicker { Location = new Point(175, y), Size = new Size(200, 24),
                Format = DateTimePickerFormat.Short,
                Value = e?.FechaIngreso ?? DateTime.Today };
            Controls.Add(dtpIngreso); y += 32;

            L("Sueldo diario USD:");      numSueldo   = N(e?.SueldoDiarioUSD  ?? 0); y += 32;
            L("Sal. normal mensual USD:"); numSalNormal= N(e?.SalarioNormalUSD ?? 0); y += 32;

            // Tarifa pieza (solo Produccion)
            pnlTarPieza = new Panel { Location = new Point(0, y), Size = new Size(500, 32), BackColor = Color.Transparent };
            var lPieza = new Label { Text = "Tarifa por pieza USD:", Location = new Point(16, 3),
                AutoSize = true, Font = new Font("Segoe UI", 9f, FontStyle.Bold) };
            numTarPieza = new NumericUpDown { Location = new Point(175, 0), Size = new Size(200, 24),
                Maximum = 9999, DecimalPlaces = 4, Value = e?.TarifaPiezaUSD ?? 0 };
            pnlTarPieza.Controls.AddRange(new Control[] { lPieza, numTarPieza });
            Controls.Add(pnlTarPieza); y += 32;

            // Tarifa venta (solo Oficina)
            pnlTarVenta = new Panel { Location = new Point(0, y), Size = new Size(500, 32), BackColor = Color.Transparent };
            var lVenta = new Label { Text = "Tarifa por venta USD:", Location = new Point(16, 3),
                AutoSize = true, Font = new Font("Segoe UI", 9f, FontStyle.Bold) };
            numTarVenta = new NumericUpDown { Location = new Point(175, 0), Size = new Size(200, 24),
                Maximum = 9999, DecimalPlaces = 2, Value = e?.TarifaVentaUSD ?? 0 };
            pnlTarVenta.Controls.AddRange(new Control[] { lVenta, numTarVenta });
            Controls.Add(pnlTarVenta); y += 32;

            L("Telefono:");  txtTel   = T(e?.Telefono ?? ""); y += 32;
            L("Email:");     txtEmail = T(e?.Email    ?? ""); y += 28;

            chkActivo = new CheckBox { Text = "Empleado activo", Location = new Point(175, y),
                AutoSize = true, Checked = e?.Activo ?? true };
            Controls.Add(chkActivo); y += 44;

            var bOk = new Button { Text = "Guardar empleado", Location = new Point(120, y),
                Size = new Size(170, 33), BackColor = Color.FromArgb(30, 130, 70),
                ForeColor = Color.White, FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold) };
            bOk.FlatAppearance.BorderSize = 0;
            var bCan = new Button { Text = "Cancelar", DialogResult = DialogResult.Cancel,
                Location = new Point(308, y), Size = new Size(110, 33),
                BackColor = Color.FromArgb(120, 120, 130), ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
            bCan.FlatAppearance.BorderSize = 0;
            
            
            var _dlgTip = AyudaService.CrearTooltip();
            if (txtCedula != null) AyudaService.Tip(_dlgTip, txtCedula, "Número de cédula de identidad");
            if (txtNombre != null) AyudaService.Tip(_dlgTip, txtNombre, "Primer nombre del empleado");
            if (txtApellido != null) AyudaService.Tip(_dlgTip, txtApellido, "Primer apellido del empleado");
            if (txtCargo != null) AyudaService.Tip(_dlgTip, txtCargo, "Cargo o puesto de trabajo");
            if (numSueldo != null) AyudaService.Tip(_dlgTip, numSueldo, "Salario diario en USD (base para cálculo de nómina)");
            if (numTarPieza != null) AyudaService.Tip(_dlgTip, numTarPieza, "Pago por pieza de primera calidad producida (solo Producción)");
            // Recordar posición y tamaño
            VentanaPrefs.Restaurar(this);
            Controls.AddRange(new Control[] { bOk, bCan });

            // Mostrar/ocultar tarifas segun tipo
            cmbTipo.SelectedIndexChanged += (s, ev) => ActualizarVisibilidad();
            ActualizarVisibilidad();

            bOk.Click += (s, ev) => Guardar(e);
        
            VentanaPrefs.Restaurar(this);
        }

        private void ActualizarVisibilidad()
        {
            string tipo = cmbTipo.SelectedItem?.ToString() ?? "Fijo";
            pnlTarPieza.Visible = tipo == "Produccion";
            pnlTarVenta.Visible = tipo == "Oficina";
        }

        private void Guardar(Empleado original)
        {
            if (string.IsNullOrWhiteSpace(txtCedula.Text))
            { MessageBox.Show("Ingresa la cedula.", "Validacion", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
            if (string.IsNullOrWhiteSpace(txtNombre.Text) || string.IsNullOrWhiteSpace(txtApellido.Text))
            { MessageBox.Show("Ingresa nombre y apellido.", "Validacion", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }

            Resultado = original ?? new Empleado();
            Resultado.Cedula          = txtCedula.Text.Trim();
            Resultado.Nombre          = txtNombre.Text.Trim();
            Resultado.Apellido        = txtApellido.Text.Trim();
            Resultado.Cargo           = txtCargo.Text.Trim();
            Resultado.TipoEmpleado    = cmbTipo.SelectedItem?.ToString() ?? "Fijo";
            Resultado.FechaIngreso    = dtpIngreso.Value;
            Resultado.Activo          = chkActivo.Checked;
            Resultado.Telefono        = txtTel.Text.Trim();
            Resultado.Email           = txtEmail.Text.Trim();
            Resultado.SueldoDiarioUSD = numSueldo.Value;
            Resultado.SalarioNormalUSD= numSalNormal.Value;
            Resultado.TarifaPiezaUSD  = numTarPieza.Value;
            Resultado.TarifaVentaUSD  = numTarVenta.Value;
            DialogResult = DialogResult.OK;
        }
    }
}
