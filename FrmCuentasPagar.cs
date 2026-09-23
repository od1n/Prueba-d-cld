using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Windows.Forms;

namespace SistemaGestion
{
    // =========================================================================
    // FORMULARIO PRINCIPAL
    // =========================================================================
    public class FrmCuentasPagar : FormBase
    {
        private DataGridView dgv;
        private ComboBox cmbFiltro;
        private Button btnNueva, btnPagar, btnEditar, btnEliminar, btnPdf, btnVerPagos;
        private Panel pnlKpis;
        private List<CuentaPagar> _lista = new();

        public FrmCuentasPagar()
        {
            InitializeComponent();
            Cargar();
        }

        private void InitializeComponent()
        {
            this.Text = "Cuentas por Pagar";
            this.Size = new Size(1000, 620);
            this.StartPosition = FormStartPosition.CenterParent;
            this.MinimumSize = new Size(800, 500);
            this.BackColor = Color.FromArgb(245, 247, 250);
            this.Font = new Font("Segoe UI", 9f);

            // Header
            var pnlHeader = new Panel { Dock = DockStyle.Top, Height = 50, BackColor = Color.FromArgb(100, 30, 100) };
            pnlHeader.Controls.Add(new Label
            {
                Text = "  Cuentas por Pagar", ForeColor = Color.White,
                Font = new Font("Segoe UI", 12f, FontStyle.Bold), AutoSize = true, Location = new Point(8, 14)
            });

            // Toolbar
            var pnlToolbar = new Panel { Dock = DockStyle.Top, Height = 44, BackColor = Color.FromArgb(235, 237, 240) };

            pnlToolbar.Controls.Add(new Label { Text = "Filtro:", AutoSize = true, Location = new Point(8, 14), ForeColor = Color.FromArgb(44,62,80) });
            cmbFiltro = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 120, Location = new Point(48, 10) };
            cmbFiltro.Items.AddRange(new object[] { "Todas", "Pendiente", "Parcial", "Vencida", "Pagada" });
            cmbFiltro.SelectedIndex = 0;
            cmbFiltro.SelectedIndexChanged += (s, e) => Cargar();
            pnlToolbar.Controls.Add(cmbFiltro);

            btnNueva   = CrearBtn("+ Nueva",    Color.FromArgb(39,174,96),  new Point(184, 8));
            btnEditar  = CrearBtn("Editar",     Color.FromArgb(41,128,185), new Point(274, 8));
            btnPagar   = CrearBtn("Registrar Pago", Color.FromArgb(100,30,100), new Point(349, 8));
            btnPagar.Width = 120;
            btnVerPagos= CrearBtn("Ver Pagos",  Color.FromArgb(52,73,94),   new Point(479, 8));
            btnEliminar= CrearBtn("Eliminar",   Color.FromArgb(192,57,43),  new Point(559, 8));
            btnPdf     = CrearBtn("PDF",        Color.FromArgb(192,57,43),  new Point(649, 8));

            btnNueva.Click    += BtnNueva_Click;
            btnEditar.Click   += BtnEditar_Click;
            btnPagar.Click    += BtnPagar_Click;
            btnVerPagos.Click += BtnVerPagos_Click;
            btnEliminar.Click += BtnEliminar_Click;
            btnPdf.Click      += BtnPdf_Click;

            pnlToolbar.Controls.AddRange(new Control[] { btnNueva, btnEditar, btnPagar, btnVerPagos, btnEliminar, btnPdf });

            // KPIs
            pnlKpis = new Panel { Dock = DockStyle.Top, Height = 68, BackColor = Color.FromArgb(245,247,250) };
            pnlKpis.Paint += PnlKpis_Paint;

            // Grid
            dgv = new DataGridView
            {
                Dock = DockStyle.Fill, ReadOnly = true, AllowUserToAddRows = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect, MultiSelect = false,
                BackgroundColor = Color.White, BorderStyle = BorderStyle.None,
                RowHeadersVisible = false, AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                Font = new Font("Segoe UI", 9f)
            };
            dgv.EnableHeadersVisualStyles = false;
            dgv.ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
            {
                BackColor = Color.FromArgb(100,30,100), ForeColor = Color.White,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold)
            };
            dgv.ColumnHeadersHeight = 30;
            dgv.RowTemplate.Height = 26;
            dgv.DefaultCellStyle = new DataGridViewCellStyle
            {
                SelectionBackColor = Color.FromArgb(41,128,185), SelectionForeColor = Color.White
            };
            dgv.CellDoubleClick += (s, e) => BtnEditar_Click(s, e);

            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "colProveedor",  HeaderText = "Proveedor",      FillWeight = 22 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "colConcepto",   HeaderText = "Concepto",       FillWeight = 22 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "colNumFact",    HeaderText = "N° Compra",      FillWeight = 10 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "colEmision",    HeaderText = "Emision",        FillWeight = 9 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "colVence",      HeaderText = "Vence",          FillWeight = 9 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "colMonto",      HeaderText = "Monto (USD)",    FillWeight = 10,
                DefaultCellStyle = new DataGridViewCellStyle { Alignment = DataGridViewContentAlignment.MiddleRight } });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "colPagado",     HeaderText = "Pagado (USD)",   FillWeight = 10,
                DefaultCellStyle = new DataGridViewCellStyle { Alignment = DataGridViewContentAlignment.MiddleRight } });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "colSaldo",      HeaderText = "Saldo (USD)",    FillWeight = 10,
                DefaultCellStyle = new DataGridViewCellStyle { Alignment = DataGridViewContentAlignment.MiddleRight } });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "colEstado",     HeaderText = "Estado",         FillWeight = 8 });

            // Footer
            var pnlFooter = new Panel { Dock = DockStyle.Bottom, Height = 36, BackColor = Color.FromArgb(235,237,240) };
            var btnCerrar = new Button
            {
                Text = "Cerrar", Size = new Size(90, 26), Location = new Point(890, 5),
                FlatStyle = FlatStyle.Flat, BackColor = Color.FromArgb(44,62,80),
                ForeColor = Color.White, Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
                FlatAppearance = { BorderSize = 0 }
            };
            btnCerrar.Click += (s, e) => Close();
            pnlFooter.Controls.Add(btnCerrar);

            this.Controls.Add(dgv);
            this.Controls.Add(pnlKpis);
            this.Controls.Add(pnlToolbar);
            this.Controls.Add(pnlHeader);
            this.Controls.Add(pnlFooter);
        }

        // ── Cargar ─────────────────────────────────────────────────────────

        private void Cargar()
        {
            string filtro = cmbFiltro.SelectedIndex > 0 ? cmbFiltro.SelectedItem.ToString() : null;

            // Actualizar estados vencidos antes de cargar
            _lista = DatabaseService.ObtenerCuentasPagar(filtro);
            foreach (var c in _lista.Where(x => x.Estado == "Pendiente" && x.FechaVence < DateTime.Today))
            {
                c.Estado = "Vencida";
                DatabaseService.ActualizarCuentaPagar(c);
            }
            _lista = DatabaseService.ObtenerCuentasPagar(filtro);

            dgv.Rows.Clear();
            foreach (var c in _lista)
            {
                int row = dgv.Rows.Add();
                dgv.Rows[row].Cells["colProveedor"].Value = c.Proveedor;
                dgv.Rows[row].Cells["colConcepto"].Value  = c.Concepto;
                dgv.Rows[row].Cells["colNumFact"].Value   = c.NumFactura;
                dgv.Rows[row].Cells["colEmision"].Value   = c.FechaEmision.ToString("dd/MM/yyyy");
                dgv.Rows[row].Cells["colVence"].Value     = c.FechaVence.ToString("dd/MM/yyyy");
                dgv.Rows[row].Cells["colMonto"].Value     = $"$ {c.Monto:N2}";
                dgv.Rows[row].Cells["colPagado"].Value    = $"$ {c.Pagado:N2}";
                dgv.Rows[row].Cells["colSaldo"].Value     = $"$ {c.Saldo:N2}";
                dgv.Rows[row].Cells["colEstado"].Value    = c.Estado;
                dgv.Rows[row].Tag = c;

                dgv.Rows[row].DefaultCellStyle.BackColor =
                    c.Estado == "Vencida"  ? Color.FromArgb(255,210,210) :
                    c.Estado == "Pagada"   ? Color.FromArgb(240,255,240) :
                    c.Estado == "Parcial"  ? Color.FromArgb(255,250,210) :
                                             Color.White;
            }
            pnlKpis.Invalidate();
        }

        private CuentaPagar Seleccionada() =>
            dgv.SelectedRows.Count > 0 ? dgv.SelectedRows[0].Tag as CuentaPagar : null;

        // ── Acciones ───────────────────────────────────────────────────────

        private void BtnNueva_Click(object sender, EventArgs e)
        {
            using var dlg = new DlgCuentaPagar(null);
            if (dlg.ShowDialog(this) == DialogResult.OK)
            {
                DatabaseService.InsertarCuentaPagar(dlg.Resultado);
                Cargar();
            }
        }

        private void BtnEditar_Click(object sender, EventArgs e)
        {
            var c = Seleccionada(); if (c == null) return;
            using var dlg = new DlgCuentaPagar(c);
            if (dlg.ShowDialog(this) == DialogResult.OK)
            {
                DatabaseService.ActualizarCuentaPagar(dlg.Resultado);
                Cargar();
            }
        }

        private void BtnPagar_Click(object sender, EventArgs e)
        {
            var c = Seleccionada(); if (c == null) return;
            if (c.Estado == "Pagada")
            {
                MessageBox.Show("Esta cuenta ya esta completamente pagada.", "Aviso",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            using var dlg = new DlgPagoCxP(c);
            if (dlg.ShowDialog(this) == DialogResult.OK)
            {
                DatabaseService.RegistrarPagoCxP(c.Id, dlg.Resultado);
                Cargar();
            }
        }

        private void BtnVerPagos_Click(object sender, EventArgs e)
        {
            var c = Seleccionada(); if (c == null) return;
            var pagos = DatabaseService.ObtenerPagosCxP(c.Id);
            using var dlg = new FrmVerPagosCxP(c, pagos);
            dlg.ShowDialog(this);
        }

        private void BtnEliminar_Click(object sender, EventArgs e)
        {
            var c = Seleccionada(); if (c == null) return;
            if (MessageBox.Show($"Eliminar cuenta '{c.Concepto}' de {c.Proveedor}?\nSe eliminaran todos sus pagos registrados.",
                "Confirmar", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes)
            {
                DatabaseService.EliminarCuentaPagar(c.Id);
                Cargar();
            }
        }

        private void BtnPdf_Click(object sender, EventArgs e)
        {
            if (_lista.Count == 0)
            {
                MessageBox.Show("No hay datos para exportar.", "Sin datos",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            try
            {
                var bytes = PdfService.GenerarReporteCxP(_lista);
                using var dlg = new SaveFileDialog
                {
                    Filter = "PDF|*.pdf",
                    FileName = $"CuentasPagar_{DateTime.Now:yyyyMMdd}.pdf"
                };
                if (dlg.ShowDialog() == DialogResult.OK)
                {
                    System.IO.File.WriteAllBytes(dlg.FileName, bytes);
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                    { FileName = dlg.FileName, UseShellExecute = true });
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al generar PDF: " + ex.Message, "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // ── KPIs ───────────────────────────────────────────────────────────

        private void PnlKpis_Paint(object sender, PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            var panel = sender as Panel;
            int kpiW = (panel.Width - 20) / 4;

            var todas    = DatabaseService.ObtenerCuentasPagar();
            decimal total    = todas.Where(x => x.Estado != "Pagada").Sum(x => x.Saldo);
            decimal vencidas = todas.Where(x => x.Estado == "Vencida").Sum(x => x.Saldo);
            int     nVenc    = todas.Count(x => x.Estado == "Vencida");
            int     nPend    = todas.Count(x => x.Estado == "Pendiente" || x.Estado == "Parcial");

            var kpis = new (string Label, string Valor, Color Color)[]
            {
                ("Saldo Total Pendiente", $"$ {total:N2}",    Color.FromArgb(100,30,100)),
                ("Saldo Vencido",         $"$ {vencidas:N2}", vencidas > 0 ? Color.FromArgb(192,57,43) : Color.FromArgb(39,174,96)),
                ("Cuentas Vencidas",      nVenc.ToString(),   nVenc > 0 ? Color.FromArgb(192,57,43) : Color.FromArgb(39,174,96)),
                ("Cuentas Pendientes",    nPend.ToString(),   Color.FromArgb(41,128,185)),
            };

            var fontTit = new Font("Segoe UI", 7.5f);
            var fontVal = new Font("Segoe UI", 11f, FontStyle.Bold);
            var sfC = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };

            for (int i = 0; i < kpis.Length; i++)
            {
                int x = 10 + i * (kpiW + 2);
                var rect = new Rectangle(x, 6, kpiW, 56);
                using (var path = RoundedRect(rect, 6)) g.FillPath(Brushes.White, path);
                using (var br = new SolidBrush(kpis[i].Color))
                    g.FillRectangle(br, x, 6, kpiW, 3);
                using (var br = new SolidBrush(Color.FromArgb(100,120,140)))
                    g.DrawString(kpis[i].Label, fontTit, br, new RectangleF(x, 10, kpiW, 20), sfC);
                using (var br = new SolidBrush(kpis[i].Color))
                    g.DrawString(kpis[i].Valor, fontVal, br, new RectangleF(x, 26, kpiW, 34), sfC);
            }
            fontTit.Dispose(); fontVal.Dispose(); sfC.Dispose();
        }

        private Button CrearBtn(string texto, Color color, Point loc)
        {
            var btn = new Button
            {
                Text = texto, Size = new Size(85, 28), Location = loc,
                FlatStyle = FlatStyle.Flat, BackColor = color, ForeColor = Color.White,
                Font = new Font("Segoe UI", 8f, FontStyle.Bold),
                FlatAppearance = { BorderSize = 0 }, Cursor = Cursors.Hand
            };
            return btn;
        }

        private static GraphicsPath RoundedRect(Rectangle rect, int r)
        {
            var path = new GraphicsPath();
            path.AddArc(rect.X, rect.Y, r*2, r*2, 180, 90);
            path.AddArc(rect.Right-r*2, rect.Y, r*2, r*2, 270, 90);
            path.AddArc(rect.Right-r*2, rect.Bottom-r*2, r*2, r*2, 0, 90);
            path.AddArc(rect.X, rect.Bottom-r*2, r*2, r*2, 90, 90);
            path.CloseFigure();
            return path;
        }
    }

    // =========================================================================
    // DIALOGO NUEVA / EDITAR CUENTA
    // =========================================================================
    public class DlgCuentaPagar : FormBase
    {
        public CuentaPagar Resultado { get; private set; }
        private readonly CuentaPagar _original;

        private TextBox txtProveedor, txtConcepto, txtNumFactura, txtObservaciones;
        private ComboBox cmbCodProv;
        private NumericUpDown numMonto;
        private DateTimePicker dtpEmision, dtpVence;
        private Button btnGuardar, btnCancelar;

        public DlgCuentaPagar(CuentaPagar original)
        {
            _original = original;
            InitializeComponent();
            if (_original != null) 
            VentanaPrefs.Restaurar(this);
            var _vpT = new System.Windows.Forms.Timer { Interval = 800 };
            _vpT.Tick        += (s, e) => { _vpT.Stop(); VentanaPrefs.Guardar(this); };
            SizeChanged     += (s, e) => { _vpT.Stop(); _vpT.Start(); };
            LocationChanged += (s, e) => { _vpT.Stop(); _vpT.Start(); };
            FormClosed      += (s, e) => { _vpT.Stop(); _vpT.Dispose(); };

            
            var _btnHlp = new Button {
                Text = "? Ayuda", Size = new Size(80, 28),
                BackColor = Color.FromArgb(80, 50, 150), ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            _btnHlp.FlatAppearance.BorderSize = 0;
            _btnHlp.Click += (s, e) => AyudaService.Mostrar(this, "FrmCuentasPagar");
            // Solo precargar datos cuando se edita una cuenta existente; en "Nueva"
            // _original es null y CargarDatos() haría NullReferenceException.
            if (_original != null) CargarDatos();
            CargarProveedores();
        }

        private void InitializeComponent()
        {
            this.Text = _original == null ? "Nueva Cuenta por Pagar" : "Editar Cuenta por Pagar";
            // ClientSize (área útil, sin barra de título ni bordes) para que nada
            // quede cortado; ventana redimensionable con un tamaño mínimo.
            this.ClientSize = new Size(460, 400);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.Sizable;
            this.MinimumSize = new Size(476, 439);
            this.MaximizeBox = false; this.MinimizeBox = false;
            this.BackColor = Color.FromArgb(245,247,250);
            this.Font = new Font("Segoe UI", 9.5f);

            var pnlHdr = new Panel { Dock = DockStyle.Top, Height = 44, BackColor = Color.FromArgb(100,30,100) };
            pnlHdr.Controls.Add(new Label { Text = this.Text, ForeColor = Color.White,
                Font = new Font("Segoe UI", 11f, FontStyle.Bold), Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter });

            var pnl = new Panel { Location = new Point(20, 56), Size = new Size(410, 280) };

            int y = 0;
            Label L(string t) { var l = new Label { Text = t, Location = new Point(0, y+3), AutoSize = true, ForeColor = Color.FromArgb(44,62,80) }; return l; }

            pnl.Controls.Add(L("Proveedor:")); 
            cmbCodProv = new ComboBox { Location = new Point(130, y), Size = new Size(280, 24), DropDownStyle = ComboBoxStyle.DropDownList };
            cmbCodProv.SelectedIndexChanged += (s, e) => {
                if (cmbCodProv.SelectedItem is Proveedor p) txtProveedor.Text = p.Descripcion;
            };
            pnl.Controls.Add(cmbCodProv); y += 34;

            pnl.Controls.Add(L("Nombre:")); 
            txtProveedor = new TextBox { Location = new Point(130, y), Size = new Size(280, 24) };
            pnl.Controls.Add(txtProveedor); y += 34;

            pnl.Controls.Add(L("Concepto:")); 
            txtConcepto = new TextBox { Location = new Point(130, y), Size = new Size(280, 24) };
            pnl.Controls.Add(txtConcepto); y += 34;

            pnl.Controls.Add(L("N° Compra:")); 
            txtNumFactura = new TextBox { Location = new Point(130, y), Size = new Size(280, 24) };
            pnl.Controls.Add(txtNumFactura); y += 34;

            pnl.Controls.Add(L("Monto (USD):")); 
            numMonto = new NumericUpDown { Location = new Point(130, y), Size = new Size(280, 24),
                Minimum = 0, Maximum = 9999999, DecimalPlaces = 2, ThousandsSeparator = true };
            pnl.Controls.Add(numMonto); y += 34;

            pnl.Controls.Add(L("Fecha Emision:")); 
            dtpEmision = new DateTimePicker { Location = new Point(130, y), Size = new Size(280, 24), Format = DateTimePickerFormat.Short };
            pnl.Controls.Add(dtpEmision); y += 34;

            pnl.Controls.Add(L("Fecha Vence:")); 
            dtpVence = new DateTimePicker { Location = new Point(130, y), Size = new Size(280, 24), Format = DateTimePickerFormat.Short,
                Value = DateTime.Today.AddDays(30) };
            pnl.Controls.Add(dtpVence); y += 34;

            pnl.Controls.Add(L("Observaciones:")); 
            txtObservaciones = new TextBox { Location = new Point(130, y), Size = new Size(280, 24) };
            pnl.Controls.Add(txtObservaciones);

            btnCancelar = new Button { Text = "Cancelar", Size = new Size(95, 32), Location = new Point(245, 356),
                Anchor = AnchorStyles.Bottom | AnchorStyles.Right,
                FlatStyle = FlatStyle.Flat, BackColor = Color.FromArgb(149,165,166), ForeColor = Color.White,
                FlatAppearance = { BorderSize = 0 }, DialogResult = DialogResult.Cancel };
            btnGuardar = new Button { Text = "Guardar", Size = new Size(95, 32), Location = new Point(350, 356),
                Anchor = AnchorStyles.Bottom | AnchorStyles.Right,
                FlatStyle = FlatStyle.Flat, BackColor = Color.FromArgb(39,174,96), ForeColor = Color.White,
                FlatAppearance = { BorderSize = 0 } };
            btnGuardar.Click += BtnGuardar_Click;

            this.Controls.AddRange(new Control[] { pnlHdr, pnl, btnCancelar, btnGuardar });
            this.AcceptButton = btnGuardar; this.CancelButton = btnCancelar;
        }

        private void CargarProveedores()
        {
            var provs = DatabaseService.ObtenerProveedores();
            cmbCodProv.Items.Clear();
            foreach (var p in provs) cmbCodProv.Items.Add(p);
            cmbCodProv.DisplayMember = "Descripcion";
            if (_original != null)
            {
                var match = provs.FirstOrDefault(p => p.Codigo == _original.CodigoProveedor);
                if (match != null) cmbCodProv.SelectedItem = match;
            }
        }

        private void CargarDatos()
        {
            txtProveedor.Text  = _original.Proveedor;
            txtConcepto.Text   = _original.Concepto;
            txtNumFactura.Text = _original.NumFactura;
            numMonto.Value     = _original.Monto;
            dtpEmision.Value   = _original.FechaEmision;
            dtpVence.Value     = _original.FechaVence;
            txtObservaciones.Text = _original.Observaciones;
        }

        private void BtnGuardar_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtConcepto.Text))
            {
                MessageBox.Show("Ingrese el concepto.", "Validacion", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            if (numMonto.Value <= 0)
            {
                MessageBox.Show("El monto debe ser mayor a cero.", "Validacion", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var prov = cmbCodProv.SelectedItem as Proveedor;
            Resultado = new CuentaPagar
            {
                Id              = _original?.Id ?? 0,
                CodigoProveedor = prov?.Codigo ?? "",
                Proveedor       = txtProveedor.Text.Trim(),
                Concepto        = txtConcepto.Text.Trim(),
                NumFactura      = txtNumFactura.Text.Trim(),
                Monto           = numMonto.Value,
                Pagado          = _original?.Pagado ?? 0,
                FechaEmision    = dtpEmision.Value,
                FechaVence      = dtpVence.Value,
                Estado          = _original?.Estado ?? "Pendiente",
                Observaciones   = txtObservaciones.Text.Trim()
            };
            this.DialogResult = DialogResult.OK;
        }
    }

    // =========================================================================
    // DIALOGO REGISTRAR PAGO
    // =========================================================================
    public class DlgPagoCxP : FormBase
    {
        public PagoCxP Resultado { get; private set; }
        private NumericUpDown numMonto;
        private ComboBox cmbFormaPago;
        private TextBox txtReferencia, txtObservaciones;
        private DateTimePicker dtpFecha;

        public DlgPagoCxP(CuentaPagar cuenta)
        {
            this.Text = $"Registrar Pago — {cuenta.Proveedor}";
            // ClientSize (área útil) para que los botones no queden cortados por la
            // barra de título; ventana redimensionable con tamaño mínimo.
            this.ClientSize = new Size(395, 272);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.Sizable;
            this.MinimumSize = new Size(411, 311);
            this.MaximizeBox = false; this.MinimizeBox = false;
            this.BackColor = Color.FromArgb(245,247,250);
            this.Font = new Font("Segoe UI", 9.5f);

            var pnlHdr = new Panel { Dock = DockStyle.Top, Height = 44, BackColor = Color.FromArgb(100,30,100) };
            pnlHdr.Controls.Add(new Label { Text = $"Saldo: $ {cuenta.Saldo:N2}", ForeColor = Color.White,
                Font = new Font("Segoe UI", 11f, FontStyle.Bold), Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter });

            var pnl = new Panel { Location = new Point(20, 56), Size = new Size(355, 200) };
            int y = 0;

            pnl.Controls.Add(new Label { Text = "Fecha:", Location = new Point(0, y+3), AutoSize = true });
            dtpFecha = new DateTimePicker { Location = new Point(130, y), Size = new Size(225, 24), Format = DateTimePickerFormat.Short };
            pnl.Controls.Add(dtpFecha); y += 34;

            pnl.Controls.Add(new Label { Text = "Monto (USD):", Location = new Point(0, y+3), AutoSize = true });
            numMonto = new NumericUpDown { Location = new Point(130, y), Size = new Size(225, 24),
                Minimum = 0.01m, Maximum = cuenta.Saldo, Value = cuenta.Saldo, DecimalPlaces = 2 };
            pnl.Controls.Add(numMonto); y += 34;

            pnl.Controls.Add(new Label { Text = "Forma Pago:", Location = new Point(0, y+3), AutoSize = true });
            cmbFormaPago = new ComboBox { Location = new Point(130, y), Size = new Size(225, 24), DropDownStyle = ComboBoxStyle.DropDownList };
            cmbFormaPago.Items.AddRange(new object[] { "Efectivo", "Transferencia", "Cheque", "Deposito", "Otro" });
            cmbFormaPago.SelectedIndex = 0;
            pnl.Controls.Add(cmbFormaPago); y += 34;

            pnl.Controls.Add(new Label { Text = "Referencia:", Location = new Point(0, y+3), AutoSize = true });
            txtReferencia = new TextBox { Location = new Point(130, y), Size = new Size(225, 24) };
            pnl.Controls.Add(txtReferencia); y += 34;

            pnl.Controls.Add(new Label { Text = "Observaciones:", Location = new Point(0, y+3), AutoSize = true });
            txtObservaciones = new TextBox { Location = new Point(130, y), Size = new Size(225, 24) };
            pnl.Controls.Add(txtObservaciones);

            var btnCancelar = new Button { Text = "Cancelar", Size = new Size(90, 30), Location = new Point(180, 230),
                Anchor = AnchorStyles.Bottom | AnchorStyles.Right,
                FlatStyle = FlatStyle.Flat, BackColor = Color.FromArgb(149,165,166), ForeColor = Color.White,
                FlatAppearance = { BorderSize = 0 }, DialogResult = DialogResult.Cancel };
            var btnGuardar = new Button { Text = "Registrar", Size = new Size(90, 30), Location = new Point(280, 230),
                Anchor = AnchorStyles.Bottom | AnchorStyles.Right,
                FlatStyle = FlatStyle.Flat, BackColor = Color.FromArgb(39,174,96), ForeColor = Color.White,
                FlatAppearance = { BorderSize = 0 } };
            btnGuardar.Click += (s, e) =>
            {
                Resultado = new PagoCxP
                {
                    IdCuenta     = cuenta.Id,
                    Fecha        = dtpFecha.Value,
                    Monto        = numMonto.Value,
                    FormaPago    = cmbFormaPago.SelectedItem?.ToString() ?? "",
                    Referencia   = txtReferencia.Text.Trim(),
                    Observaciones= txtObservaciones.Text.Trim()
                };
                this.DialogResult = DialogResult.OK;
            };

            this.Controls.AddRange(new Control[] { pnlHdr, pnl, btnCancelar, btnGuardar });
            this.AcceptButton = btnGuardar; this.CancelButton = btnCancelar;
        }
    }

    // =========================================================================
    // VISOR DE PAGOS
    // =========================================================================
    public class FrmVerPagosCxP : FormBase
    {
        public FrmVerPagosCxP(CuentaPagar cuenta, List<PagoCxP> pagos)
        {
            this.Text = $"Pagos — {cuenta.Concepto}";
            this.Size = new Size(620, 380);
            this.StartPosition = FormStartPosition.CenterParent;
            this.BackColor = Color.FromArgb(245,247,250);
            this.Font = new Font("Segoe UI", 9f);

            var pnlHdr = new Panel { Dock = DockStyle.Top, Height = 44, BackColor = Color.FromArgb(100,30,100) };
            pnlHdr.Controls.Add(new Label
            {
                Text = $"  {cuenta.Proveedor} — Monto: $ {cuenta.Monto:N2}  |  Pagado: $ {cuenta.Pagado:N2}  |  Saldo: $ {cuenta.Saldo:N2}",
                ForeColor = Color.White, Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                AutoSize = true, Location = new Point(8, 13)
            });

            var dgv = new DataGridView
            {
                Dock = DockStyle.Fill, ReadOnly = true, AllowUserToAddRows = false,
                BackgroundColor = Color.White, BorderStyle = BorderStyle.None,
                RowHeadersVisible = false, AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                Font = new Font("Segoe UI", 9f)
            };
            dgv.EnableHeadersVisualStyles = false;
            dgv.ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
            {
                BackColor = Color.FromArgb(100,30,100), ForeColor = Color.White,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold)
            };
            dgv.ColumnHeadersHeight = 28;
            dgv.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Fecha",       FillWeight = 15 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Monto (USD)", FillWeight = 15,
                DefaultCellStyle = new DataGridViewCellStyle { Alignment = DataGridViewContentAlignment.MiddleRight } });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Forma Pago",  FillWeight = 15 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Referencia",  FillWeight = 20 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Observaciones",FillWeight = 35 });

            foreach (var p in pagos)
                dgv.Rows.Add(p.Fecha.ToString("dd/MM/yyyy"), $"$ {p.Monto:N2}",
                    p.FormaPago, p.Referencia, p.Observaciones);

            var pnlFooter = new Panel { Dock = DockStyle.Bottom, Height = 36, BackColor = Color.FromArgb(235,237,240) };
            var btnCerrar = new Button { Text = "Cerrar", Size = new Size(90, 26), Location = new Point(510, 5),
                FlatStyle = FlatStyle.Flat, BackColor = Color.FromArgb(44,62,80), ForeColor = Color.White,
                FlatAppearance = { BorderSize = 0 }, DialogResult = DialogResult.OK };
            pnlFooter.Controls.Add(btnCerrar);

            this.Controls.Add(dgv);
            this.Controls.Add(pnlHdr);
            this.Controls.Add(pnlFooter);
        }
    }
}
