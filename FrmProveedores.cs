using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace SistemaGestion
{
    public class FrmProveedores : FormBase
    {
        private DataGridView  dgv;
        private Label         lblInfo;
        private TextBox       txtBuscar;
        private string        _sortCol = "Codigo";
        private bool          _sortAsc = true;
        private List<Proveedor> _todos = new();

        public FrmProveedores()
        {
            Text          = "Proveedores";
            Size          = new Size(1000, 540);
            StartPosition = FormStartPosition.CenterParent;
            BackColor     = Color.FromArgb(240, 244, 250);

            var hdr = new Panel { Dock = DockStyle.Top, Height = 50,
                BackColor = Color.FromArgb(26, 74, 138) };
            hdr.Controls.Add(new Label { Text = "Proveedores", ForeColor = Color.White,
                Font = new Font("Segoe UI", 12f, FontStyle.Bold),
                AutoSize = true, Location = new Point(12, 13) });
            var btnN = Btn("+ Nuevo",  Color.FromArgb(40, 160, 80));
            var btnE = Btn("Editar",   Color.FromArgb(60, 100, 180));
            var btnD = Btn("Eliminar", Color.FromArgb(180, 60, 60));
            btnN.Visible = Sesion.EsAdmin;
            btnD.Visible = Sesion.EsAdmin;

            
            var _btnAyuda = new Button {
                Text = "? Ayuda", Size = new Size(80, 32),
                BackColor = Color.FromArgb(80, 50, 150), ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
                Cursor = Cursors.Hand, Margin = new Padding(0, 0, 4, 0)
            };
            _btnAyuda.FlatAppearance.BorderSize = 0;
            _btnAyuda.Click += (s, e) => AyudaService.Mostrar(this, "FrmProveedores");
            var pnlHdrRight = new FlowLayoutPanel {
                Dock = DockStyle.Right, AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false, BackColor = Color.Transparent,
                Padding = new Padding(0, 9, 8, 0)
            };
            pnlHdrRight.Controls.AddRange(new Control[] { _btnAyuda, btnN, btnE, btnD });
            hdr.Controls.Add(pnlHdrRight);
            // ── ToolTips ─────────────────────────────────────────────────
            var _tip = AyudaService.CrearTooltip();
            AyudaService.Tip(_tip, _btnAyuda, "Abrir la guía de ayuda de este módulo");
            AyudaService.Tip(_tip, btnN, "Agregar un nuevo proveedor al directorio (solo Admin)");
            AyudaService.Tip(_tip, btnE, "Modificar los datos del proveedor seleccionado");
            AyudaService.Tip(_tip, btnD, "Eliminar el proveedor seleccionado (solo Admin)");

            var barBuscar = new Panel { Dock = DockStyle.Top, Height = 36,
                BackColor = Color.FromArgb(232, 240, 250) };
            barBuscar.Controls.Add(new Label { Text = "Buscar:",
                Location = new Point(10, 9), AutoSize = true });
            txtBuscar = new TextBox { Location = new Point(65, 6), Size = new Size(260, 24) };
            txtBuscar.TextChanged += (s, e) => Filtrar();
            barBuscar.Controls.Add(txtBuscar);

            dgv = Grid(Color.FromArgb(26, 74, 138));
            dgv.Name = "dgv";
            dgv.ColumnHeaderMouseClick += DgvColumnHeaderMouseClick;
            dgv.CellDoubleClick        += (s, e) => Editar();

            var footer = new Panel { Dock = DockStyle.Bottom, Height = 40,
                BackColor = Color.FromArgb(240, 244, 250) };
            lblInfo = new Label { AutoSize = true, Location = new Point(12, 10),
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                ForeColor = Color.FromArgb(100, 60, 10) };
            var btnC = new Button { Text = "Cerrar", Size = new Size(90, 27),
                Location = new Point(880, 6), BackColor = Color.FromArgb(100, 100, 120),
                ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
            btnC.FlatAppearance.BorderSize = 0;
            btnC.Click += (s, e) => Close();
            footer.Controls.AddRange(new Control[] { lblInfo, btnC });

            Controls.AddRange(new Control[] { dgv, barBuscar, hdr, footer });

            btnN.Click += (s, e) => Nuevo();
            btnE.Click += (s, e) => Editar();
            btnD.Click += (s, e) => Eliminar();

            VentanaPrefs.Restaurar(this);
            var _tp = new System.Windows.Forms.Timer { Interval = 800 };
            _tp.Tick        += (s, e) => { _tp.Stop(); VentanaPrefs.Guardar(this); };
            SizeChanged     += (s, e) => { _tp.Stop(); _tp.Start(); };
            LocationChanged += (s, e) => { _tp.Stop(); _tp.Start(); };
            FormClosed      += (s, e) => { _tp.Stop(); _tp.Dispose(); };

            CargarDatos();
        }

        private void Nuevo()
        {
            string cod = GenerarCodigo();
            using var d = new DlgProveedor(null, cod);
            if (d.ShowDialog() == DialogResult.OK)
            {
                DatabaseService.InsertarProveedor(d.Resultado);
                CargarDatos();
            }
        }

        private void Editar()
        {
            var p = GetSel();
            if (p == null) return;
            using var d = new DlgProveedor(p, null);
            if (d.ShowDialog() == DialogResult.OK)
            {
                DatabaseService.ActualizarProveedor(d.Resultado);
                CargarDatos();
            }
        }

        private void Eliminar()
        {
            var p = GetSel();
            if (p == null) return;
            if (MessageBox.Show($"Eliminar proveedor {p.Codigo} — {p.Descripcion}?",
                "Confirmar", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes)
            {
                DatabaseService.EliminarProveedor(p.Codigo);
                CargarDatos();
            }
        }

        private string GenerarCodigo()
        {
            var nums = DatabaseService.ObtenerProveedores()
                .Select(p => { int.TryParse(p.Codigo, out int n); return n; })
                .Where(n => n > 0).ToList();
            return (nums.Any() ? nums.Max() + 1 : 1).ToString();
        }

        private void CargarDatos()
        {
            _todos = DatabaseService.ObtenerProveedores();
            Filtrar();
            lblInfo.Text = $"  Proveedores: {_todos.Count}";
        }

        private void Filtrar()
        {
            var q = txtBuscar.Text;
            // Búsqueda flexible: tolera ceros a la izquierda y separadores en CI/RIF
            var lista = string.IsNullOrWhiteSpace(q) ? _todos
                : _todos.Where(p => BusquedaHelper.ContieneFlexibleEnAlguno(q,
                        p.Codigo, p.Descripcion, p.CiRif, p.Nit,
                        p.Telefono1, p.Representante)).ToList();

            lista = _sortCol switch {
                "Nombre" => _sortAsc
                    ? lista.OrderByNatural(p => p.Descripcion).ToList()
                    : lista.OrderByDescendingNatural(p => p.Descripcion).ToList(),
                "CiRif"  => _sortAsc
                    ? lista.OrderByNatural(p => p.CiRif).ToList()
                    : lista.OrderByDescendingNatural(p => p.CiRif).ToList(),
                _ => _sortAsc
                    ? lista.OrderByNatural(p => p.Codigo).ToList()
                    : lista.OrderByDescendingNatural(p => p.Codigo).ToList()
            };

            dgv.DataSource = null;
            dgv.DataSource = lista.Select(p => new {
                p.Codigo, Nombre = p.Descripcion, p.Representante,
                p.Telefono1, p.CiRif, p.Nit,
                DiasCredito = p.DiasCredito,
                Retencion   = $"{p.PctRetencion}%",
                p.Tipo
            }).ToList();
            VentanaPrefs.AplicarAnchos(this, dgv);

            foreach (DataGridViewColumn col in dgv.Columns)
                col.HeaderCell.SortGlyphDirection = SortOrder.None;
            if (dgv.Columns.Contains(_sortCol))
                dgv.Columns[_sortCol].HeaderCell.SortGlyphDirection =
                    _sortAsc ? SortOrder.Ascending : SortOrder.Descending;
        }

        private void DgvColumnHeaderMouseClick(object sender,
            DataGridViewCellMouseEventArgs e)
        {
            string col = dgv.Columns[e.ColumnIndex].Name;
            if (col != "Codigo" && col != "Nombre" && col != "CiRif") return;
            if (_sortCol == col) _sortAsc = !_sortAsc;
            else { _sortCol = col; _sortAsc = true; }
            Filtrar();
        }

        private Proveedor GetSel()
        {
            if (dgv.CurrentRow == null) return null;
            var cod = dgv.CurrentRow.Cells["Codigo"].Value?.ToString();
            return _todos.Find(x => x.Codigo == cod);
        }

        private DataGridView Grid(Color h)
        {
            var g = new DataGridView {
                Dock = DockStyle.Fill, ReadOnly = true,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false, AllowUserToAddRows = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None,
                BackgroundColor = Color.White, BorderStyle = BorderStyle.None,
                RowHeadersVisible = false
            };
            g.ColumnHeadersDefaultCellStyle.BackColor = h;
            g.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
            g.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 9f, FontStyle.Bold);
            g.EnableHeadersVisualStyles = false;
            g.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(255, 245, 230);
            return g;
        }
        private Button Btn(string t, Color c, Point p)
        {
            var b = new Button { Text = t, Size = new Size(90, 33), Location = p,
                BackColor = c, ForeColor = Color.White, FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 8.5f, FontStyle.Bold), Cursor = Cursors.Hand };
            b.FlatAppearance.BorderSize = 0; return b;
        }
        private Button Btn(string t, Color c)
        {
            var b = new Button { Text = t, Size = new Size(90, 32),
                BackColor = c, ForeColor = Color.White, FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 8.5f, FontStyle.Bold), Cursor = Cursors.Hand,
                Margin = new Padding(0, 0, 4, 0) };
            b.FlatAppearance.BorderSize = 0; return b;
        }
    }

    // ════════════════════════════════════════════════════════════════════════
    //  DIALOGO PROVEEDOR
    // ════════════════════════════════════════════════════════════════════════
    public class DlgProveedor : FormBase
    {
        public Proveedor Resultado { get; private set; }

        private TextBox        txtCod, txtDesc, txtRep, txtDir;
        private TextBox        txtT1, txtT2, txtF1, txtF2, txtCiRif, txtNit, txtObs;
        private NumericUpDown  numDias, numPct;
        private ComboBox       cmbTipo;
        private DateTimePicker dtpFecha;

        private readonly Proveedor _original;
        // Permite letras, números, espacios y la puntuación común de razones
        // sociales: punto, coma, guion, &, /, apóstrofo y paréntesis (p. ej. "C.A.").
        private static readonly Regex _rxNombre =
            new Regex(@"^[a-zA-ZáéíóúÁÉÍÓÚüÜñÑ0-9\s\.\,\-\&\/'()]+$");

        public DlgProveedor(Proveedor p, string codigoSugerido)
        {
            _original = p;
            Text      = p == null ? "Nuevo Proveedor" : "Editar Proveedor";
            Size      = new Size(480, 600);
            StartPosition   = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.Sizable;
            MaximizeBox = true; MinimizeBox = false;
            BackColor   = Color.FromArgb(240, 244, 250);

            int y = 12;
            L("Codigo:",        y); txtCod   = T(y); y += 33;
            L("Nombre:",        y); txtDesc  = T(y); y += 33;
            L("Representante:", y); txtRep   = T(y); y += 33;
            L("Direccion:",     y); txtDir   = T(y); y += 33;
            L("Telefono 1:",    y); txtT1    = T(y); y += 33;
            L("Telefono 2:",    y); txtT2    = T(y); y += 33;
            L("Fax 1:",         y); txtF1    = T(y); y += 33;
            L("Fax 2:",         y); txtF2    = T(y); y += 33;
            L("CI/RIF:",        y); txtCiRif = T(y); y += 33;
            L("NIT:",           y); txtNit   = T(y); y += 33;
            L("Dias Credito:",  y); numDias  = Num(y, 365); y += 33;
            L("% Retencion:",   y); numPct   = Num(y, 100); y += 33;
            L("Tipo:",          y);
            cmbTipo = new ComboBox { Location = new Point(155, y), Size = new Size(270, 24),
                DropDownStyle = ComboBoxStyle.DropDownList };
            cmbTipo.Items.AddRange(new object[] {
                "Nacional", "Internacional", "Servicio", "Mixto" });
            cmbTipo.SelectedIndex = 0; Controls.Add(cmbTipo); y += 33;
            L("Fecha Inicio:", y);
            dtpFecha = new DateTimePicker { Location = new Point(155, y),
                Size = new Size(270, 24), Format = DateTimePickerFormat.Short };
            Controls.Add(dtpFecha); y += 33;
            L("Observaciones:", y);
            txtObs = new TextBox { Location = new Point(155, y),
                Size = new Size(270, 45), Multiline = true };
            Controls.Add(txtObs); y += 55;

            if (p != null)
            {
                txtCod.Text   = p.Codigo; txtCod.ReadOnly = true;
                txtDesc.Text  = p.Descripcion; txtRep.Text  = p.Representante;
                txtDir.Text   = p.Direccion;   txtT1.Text   = p.Telefono1;
                txtT2.Text    = p.Telefono2;   txtF1.Text   = p.Fax1;
                txtF2.Text    = p.Fax2;        txtCiRif.Text = p.CiRif;
                txtNit.Text   = p.Nit;         numDias.Value = p.DiasCredito;
                numPct.Value  = p.PctRetencion;
                if (cmbTipo.Items.Contains(p.Tipo)) cmbTipo.SelectedItem = p.Tipo;
                dtpFecha.Value = p.FechaInicio;
                txtObs.Text    = p.Observaciones;
            }
            else if (codigoSugerido != null)
            {
                txtCod.Text = codigoSugerido;
                txtCod.ReadOnly = true;
            }

            var bOk  = BB("Guardar",   Color.FromArgb(26, 74, 138), new Point(90,  y));
            var bCan = BB("Cancelar",  Color.FromArgb(160, 50, 50), new Point(260, y));
            bCan.DialogResult = DialogResult.Cancel;
            bOk.Click += BtnOk_Click;
            
            
            var _dlgTip = AyudaService.CrearTooltip();
            if (txtCod != null) AyudaService.Tip(_dlgTip, txtCod, "Código único del proveedor (ej: PETRO)");
            if (txtDesc != null) AyudaService.Tip(_dlgTip, txtDesc, "Nombre o razón social del proveedor");
            if (txtCiRif != null) AyudaService.Tip(_dlgTip, txtCiRif, "RIF o CI del proveedor sin guiones");
            // Recordar posición y tamaño
            VentanaPrefs.Restaurar(this);
            Controls.AddRange(new Control[] { bOk, bCan });

            // Tab con seleccion
            foreach (var ctrl in new TextBox[] {
                txtCod, txtDesc, txtRep, txtDir, txtT1, txtT2,
                txtF1, txtF2, txtCiRif, txtNit, txtObs })
                ctrl.Enter += (s, e) => ((TextBox)s).SelectAll();
            numDias.Enter += (s, e) => numDias.Select(0, numDias.Text.Length);
            numPct.Enter  += (s, e) => numPct.Select(0, numPct.Text.Length);
        
            VentanaPrefs.Restaurar(this);
        }

        private void BtnOk_Click(object sender, EventArgs e)
        {
            string nombre = txtDesc.Text.Trim();
            if (string.IsNullOrWhiteSpace(nombre))
            { Warn("El nombre es obligatorio."); return; }
            if (!_rxNombre.IsMatch(nombre))
            { Warn("El nombre contiene caracteres no permitidos. Se aceptan letras, números, espacios y . , - & / ' ( )."); return; }

            string ciRif = txtCiRif.Text.Trim();
            if (!string.IsNullOrWhiteSpace(ciRif))
            {
                var dup = DatabaseService.ObtenerProveedores()
                    .FirstOrDefault(p => p.CiRif == ciRif &&
                                         p.Codigo != txtCod.Text.Trim());
                if (dup != null)
                { Warn($"El CI/RIF '{ciRif}' ya está en el proveedor {dup.Codigo} — {dup.Descripcion}."); return; }
            }

            Resultado = _original ?? new Proveedor();
            Resultado.Codigo       = txtCod.Text.Trim();
            Resultado.Descripcion  = nombre;
            Resultado.Representante = txtRep.Text.Trim();
            Resultado.Direccion    = txtDir.Text.Trim();
            Resultado.Telefono1    = txtT1.Text.Trim();
            Resultado.Telefono2    = txtT2.Text.Trim();
            Resultado.Fax1         = txtF1.Text.Trim();
            Resultado.Fax2         = txtF2.Text.Trim();
            Resultado.CiRif        = ciRif;
            Resultado.Nit          = txtNit.Text.Trim();
            Resultado.DiasCredito  = (int)numDias.Value;
            Resultado.PctRetencion = numPct.Value;
            Resultado.Tipo         = cmbTipo.SelectedItem?.ToString();
            Resultado.FechaInicio  = dtpFecha.Value;
            Resultado.Observaciones = txtObs.Text.Trim();
            DialogResult = DialogResult.OK;
        }

        private void Warn(string msg) =>
            MessageBox.Show(msg, "Validacion", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        private void L(string t, int y) =>
            Controls.Add(new Label { Text = t, Location = new Point(12, y + 3),
                AutoSize = true, Font = new Font("Segoe UI", 9f, FontStyle.Bold) });
        private TextBox T(int y)
        {
            var tb = new TextBox { Location = new Point(155, y), Size = new Size(270, 24) };
            Controls.Add(tb); return tb;
        }
        private NumericUpDown Num(int y, decimal max)
        {
            var n = new NumericUpDown { Location = new Point(155, y), Size = new Size(270, 24),
                Maximum = max, DecimalPlaces = 2 };
            Controls.Add(n); return n;
        }
        private Button BB(string t, Color c, Point p)
        {
            var b = new Button { Text = t, Location = p, Size = new Size(130, 32),
                BackColor = c, ForeColor = Color.White, FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold) };
            b.FlatAppearance.BorderSize = 0; Controls.Add(b); return b;
        }
    }
}
