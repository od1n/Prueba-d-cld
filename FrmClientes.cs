using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace SistemaGestion
{
    public class FrmClientes : FormBase
    {
        private DataGridView dgv;
        private TextBox      txtBuscar;
        private Label        lblInfo;

        // Ordenamiento por columna
        private string _sortCol = "Codigo";
        private bool   _sortAsc = true;
        private List<Cliente> _todos = new();

        public FrmClientes()
        {
            Text          = "Clientes";
            Size          = new Size(1000, 560);
            StartPosition = FormStartPosition.CenterParent;
            BackColor     = Color.FromArgb(240, 244, 250);

            var hdr = new Panel { Dock = DockStyle.Top, Height = 50,
                BackColor = Color.FromArgb(26, 74, 138) };
            hdr.Controls.Add(new Label { Text = "Clientes", ForeColor = Color.White,
                Font = new Font("Segoe UI", 12f, FontStyle.Bold),
                AutoSize = true, Location = new Point(12, 13) });
            var btnNuevo  = Btn("+ Nuevo",  Color.FromArgb(40, 160, 80));
            var btnEditar = Btn("Editar",   Color.FromArgb(60, 120, 200));
            var btnElim   = Btn("Eliminar", Color.FromArgb(26, 74, 138));
            
            
            var _btnAyuda = new Button {
                Text = "? Ayuda", Size = new Size(80, 32),
                BackColor = Color.FromArgb(80, 50, 150), ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
                Cursor = Cursors.Hand, Margin = new Padding(0, 0, 4, 0)
            };
            _btnAyuda.FlatAppearance.BorderSize = 0;
            _btnAyuda.Click += (s, e) => AyudaService.Mostrar(this, "FrmClientes");
            var _pnlHdr = new FlowLayoutPanel {
                Dock = DockStyle.Right, AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false, BackColor = Color.Transparent,
                Padding = new Padding(0, 9, 8, 0)
            };
            _pnlHdr.Controls.AddRange(new Control[] { _btnAyuda, btnNuevo, btnEditar, btnElim });
            hdr.Controls.Add(_pnlHdr);
            // ── ToolTips ─────────────────────────────────────────────────
            var _tip = AyudaService.CrearTooltip();
            AyudaService.Tip(_tip, _btnAyuda, "Abrir la guía de ayuda de este módulo");
            AyudaService.Tip(_tip, btnNuevo, "Agregar un nuevo cliente al directorio");
            AyudaService.Tip(_tip, btnEditar, "Modificar los datos del cliente seleccionado");
            AyudaService.Tip(_tip, btnElim, "Eliminar el cliente seleccionado del sistema");

            var panelSearch = new Panel { Dock = DockStyle.Top, Height = 36,
                BackColor = Color.FromArgb(232, 240, 250) };
            panelSearch.Controls.Add(new Label { Text = "Buscar:",
                Location = new Point(10, 9), AutoSize = true });
            txtBuscar = new TextBox { Location = new Point(65, 6), Size = new Size(260, 24) };
            txtBuscar.TextChanged += (s, e) => Filtrar();
            panelSearch.Controls.Add(txtBuscar);

            dgv = Grid(Color.FromArgb(26, 74, 138));
            dgv.Name = "dgv";
            dgv.ColumnHeaderMouseClick += DgvColumnHeaderMouseClick;
            dgv.CellDoubleClick        += (s, e) => Editar();

            var footer = new Panel { Dock = DockStyle.Bottom, Height = 40,
                BackColor = Color.FromArgb(220, 228, 250) };
            lblInfo = new Label { AutoSize = true, Location = new Point(12, 10),
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                ForeColor = Color.FromArgb(30, 60, 130) };
            var btnCerrar = BtnF("Cerrar", footer, new Point(880, 6));
            btnCerrar.Click += (s, e) => Close();
            footer.Controls.AddRange(new Control[] { lblInfo, btnCerrar });

            Controls.AddRange(new Control[] { dgv, panelSearch, hdr, footer });

            btnNuevo.Click  += (s, e) => Nuevo();
            btnEditar.Click += (s, e) => Editar();
            btnElim.Click   += (s, e) => Eliminar();

            VentanaPrefs.Restaurar(this);
            var _tpVP = new System.Windows.Forms.Timer { Interval = 800 };
            _tpVP.Tick      += (s, e) => { _tpVP.Stop(); VentanaPrefs.Guardar(this); };
            SizeChanged     += (s, e) => { _tpVP.Stop(); _tpVP.Start(); };
            LocationChanged += (s, e) => { _tpVP.Stop(); _tpVP.Start(); };
            FormClosed      += (s, e) => { _tpVP.Stop(); _tpVP.Dispose(); };


            CargarDatos();
        }

        // ── CRUD ──────────────────────────────────────────────────────────────
        private void Nuevo()
        {
            string codigoSugerido = GenerarCodigo();
            using var d = new DlgCliente(null, codigoSugerido);
            if (d.ShowDialog() == DialogResult.OK)
            {
                DatabaseService.InsertarCliente(d.Resultado);
                CargarDatos();
            }
        }

        private void Editar()
        {
            var cl = GetSelected();
            if (cl == null) return;
            using var d = new DlgCliente(cl, null);
            if (d.ShowDialog() == DialogResult.OK)
            {
                DatabaseService.ActualizarCliente(d.Resultado);
                CargarDatos();
            }
        }

        private void Eliminar()
        {
            var cl = GetSelected();
            if (cl == null) return;
            if (MessageBox.Show($"Eliminar cliente {cl.Codigo} — {cl.Descripcion}?",
                "Confirmar", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes)
            {
                DatabaseService.EliminarCliente(cl.Codigo);
                CargarDatos();
            }
        }

        // ── Codigo automatico ─────────────────────────────────────────────────
        private string GenerarCodigo()
        {
            var codigos = DatabaseService.ObtenerClientes()
                .Select(c => { int.TryParse(c.Codigo, out int n); return n; })
                .Where(n => n > 0).ToList();
            int siguiente = 500;
            if (codigos.Any())
                siguiente = Math.Max(500, codigos.Max() + 1);
            return siguiente.ToString();
        }

        // ── Carga y filtrado ──────────────────────────────────────────────────
        private void CargarDatos()
        {
            _todos = DatabaseService.ObtenerClientes();
            Filtrar();
            lblInfo.Text = $"  Clientes: {_todos.Count}   |   " +
                           $"Credito total: {_todos.Sum(c => c.Credito):C2}";
        }

        private void Filtrar()
        {
            var q = txtBuscar.Text;
            // Búsqueda flexible: tolera ceros a la izquierda y separadores.
            // "03" encuentra al cliente con código "3"; "J-12345" encuentra
            // un CI/RIF guardado como "J12345".
            var lista = string.IsNullOrWhiteSpace(q) ? _todos
                : _todos.Where(c => BusquedaHelper.ContieneFlexibleEnAlguno(q,
                        c.Codigo, c.Descripcion, c.Ciudad, c.CiRif,
                        c.Telefono, c.Telefono2, c.Nit)).ToList();

            lista = Ordenar(lista);
            BindGrid(lista);
        }

        private List<Cliente> Ordenar(List<Cliente> lista) => _sortCol switch
        {
            "Nombre"  => _sortAsc ? lista.OrderByNatural(c => c.Descripcion).ToList()
                                  : lista.OrderByDescendingNatural(c => c.Descripcion).ToList(),
            "Credito" => _sortAsc ? lista.OrderBy(c => c.Credito).ToList()
                                  : lista.OrderByDescending(c => c.Credito).ToList(),
            "AFavor"  => _sortAsc ? lista.OrderBy(c => c.AFavor).ToList()
                                  : lista.OrderByDescending(c => c.AFavor).ToList(),
            "CiRif"   => _sortAsc ? lista.OrderByNatural(c => c.CiRif).ToList()
                                  : lista.OrderByDescendingNatural(c => c.CiRif).ToList(),
            "Ciudad"  => _sortAsc ? lista.OrderByNatural(c => c.Ciudad).ToList()
                                  : lista.OrderByDescendingNatural(c => c.Ciudad).ToList(),
            // Default: orden NATURAL por código (3 antes de 10, "03" como 3)
            _         => _sortAsc ? lista.OrderByNatural(c => c.Codigo).ToList()
                                  : lista.OrderByDescendingNatural(c => c.Codigo).ToList()
        };

        private void BindGrid(List<Cliente> lista)
        {
            dgv.DataSource = null;
            dgv.DataSource = lista.Select(c => new {
                c.Codigo,
                Nombre    = c.Descripcion,
                c.Ciudad, c.Estado, c.CiRif, c.Telefono,
                Credito   = c.Credito.ToString("C2"),
                c.TipoCliente,
                AFavor    = c.AFavor.ToString("C2")
            }).ToList();
            VentanaPrefs.AplicarAnchos(this, dgv);

            // Mostrar flecha en cabecera
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
            // Columnas ordenables (incluye CI/RIF y Ciudad además de las clásicas)
            var ordenables = new HashSet<string> {
                "Codigo", "Nombre", "Credito", "AFavor", "CiRif", "Ciudad"
            };
            if (!ordenables.Contains(col)) return;

            if (_sortCol == col) _sortAsc = !_sortAsc;
            else { _sortCol = col; _sortAsc = true; }
            Filtrar();
        }

        private Cliente GetSelected()
        {
            if (dgv.CurrentRow == null) return null;
            var cod = dgv.CurrentRow.Cells["Codigo"].Value?.ToString();
            return _todos.Find(x => x.Codigo == cod);
        }

        // ── Helpers UI ────────────────────────────────────────────────────────
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
            g.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(238, 243, 255);
            return g;
        }
        private Button Btn(string t, Color c, int w = 90)
        {
            var b = new Button { Text = t, Size = new Size(w, 32),
                BackColor = c, ForeColor = Color.White, FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 8.5f, FontStyle.Bold), Cursor = Cursors.Hand,
                Margin = new Padding(0, 0, 4, 0) };
            b.FlatAppearance.BorderSize = 0; return b;
        }
        private Button BtnF(string t, Panel par, Point p)
        {
            var b = new Button { Text = t, Size = new Size(90, 27), Location = p,
                BackColor = Color.FromArgb(100, 100, 120), ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat };
            b.FlatAppearance.BorderSize = 0; par.Controls.Add(b); return b;
        }
    }

    // ════════════════════════════════════════════════════════════════════════
    //  DIALOGO CLIENTE
    // ════════════════════════════════════════════════════════════════════════
    public class DlgCliente : FormBase
    {
        public Cliente Resultado { get; private set; }

        private TextBox        txtCod, txtDesc, txtDir, txtCiud, txtEdo;
        private TextBox        txtCiRif, txtNit, txtFax, txtTel, txtTel2, txtObs;
        private NumericUpDown  numCred, numDesc, numAFav;
        private ComboBox       cmbTipo;
        private DateTimePicker dtpFecha;

        private readonly Cliente _original;

        // Regex: solo letras, numeros y espacios
        private static readonly Regex _rxNombre =
            new Regex(@"^[a-zA-ZáéíóúÁÉÍÓÚüÜñÑ0-9\s]+$");

        public DlgCliente(Cliente cl, string codigoSugerido)
        {
            _original = cl;
            Text      = cl == null ? "Nuevo Cliente" : "Editar Cliente";
            Size      = new Size(480, 600);
            StartPosition   = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.Sizable;
            MaximizeBox = true; MinimizeBox = false;
            BackColor   = Color.FromArgb(240, 244, 250);

            int y = 12;
            Lbl("Codigo:",        y); txtCod   = Txt(y, true);  y += 34;
            Lbl("Nombre:",        y); txtDesc  = Txt(y, false); y += 34;
            Lbl("Direccion:",     y); txtDir   = Txt(y, false); y += 34;
            Lbl("Ciudad:",        y); txtCiud  = Txt(y, false); y += 34;
            Lbl("Estado:",        y); txtEdo   = Txt(y, false); y += 34;
            Lbl("CI/RIF:",        y); txtCiRif = Txt(y, false); y += 34;
            Lbl("NIT:",           y); txtNit   = Txt(y, false); y += 34;
            Lbl("Fax:",           y); txtFax   = Txt(y, false); y += 34;
            Lbl("Telefono:",      y); txtTel   = Txt(y, false); y += 34;
            Lbl("Telefono2:",     y); txtTel2  = Txt(y, false); y += 34;
            Lbl("Credito:",       y); numCred  = Num(y, 9999999); y += 34;
            Lbl("% Descuento:",   y); numDesc  = Num(y, 100);     y += 34;
            Lbl("A Favor:",       y); numAFav  = Num(y, 9999999); y += 34;
            Lbl("Tipo Cliente:",  y);
            cmbTipo = new ComboBox { Location = new Point(155, y), Size = new Size(270, 24),
                DropDownStyle = ComboBoxStyle.DropDownList };
            cmbTipo.Items.AddRange(new object[] {
                "Regular", "VIP", "Mayorista", "Minorista", "Gobierno" });
            cmbTipo.SelectedIndex = 0;
            Controls.Add(cmbTipo); y += 34;
            Lbl("Fecha:", y);
            dtpFecha = new DateTimePicker { Location = new Point(155, y),
                Size = new Size(270, 24), Format = DateTimePickerFormat.Short };
            Controls.Add(dtpFecha); y += 34;
            Lbl("Observaciones:", y);
            txtObs = new TextBox { Location = new Point(155, y),
                Size = new Size(270, 45), Multiline = true };
            Controls.Add(txtObs); y += 55;

            // Rellenar si es edicion
            if (cl != null)
            {
                txtCod.Text   = cl.Codigo;   txtCod.ReadOnly = true;
                txtDesc.Text  = cl.Descripcion; txtDir.Text  = cl.Direccion;
                txtCiud.Text  = cl.Ciudad;   txtEdo.Text     = cl.Estado;
                txtCiRif.Text = cl.CiRif;    txtNit.Text     = cl.Nit;
                txtFax.Text   = cl.Fax;      txtTel.Text     = cl.Telefono;
                txtTel2.Text  = cl.Telefono2;
                numCred.Value = cl.Credito;  numDesc.Value   = cl.Descuento;
                numAFav.Value = cl.AFavor;
                if (cmbTipo.Items.Contains(cl.TipoCliente))
                    cmbTipo.SelectedItem = cl.TipoCliente;
                dtpFecha.Value = cl.Fecha;
                txtObs.Text    = cl.Observaciones;
            }
            else if (codigoSugerido != null)
            {
                txtCod.Text = codigoSugerido;
                txtCod.ReadOnly = true; // generado automaticamente
            }

            var bOk  = BBtn("Guardar",   Color.FromArgb(40, 100, 180), new Point(90,  y));
            var bCan = BBtn("Cancelar",  Color.FromArgb(160, 50, 50),  new Point(260, y));
            bCan.DialogResult = DialogResult.Cancel;
            bOk.Click += BtnOk_Click;
            
            
            var _dlgTip = AyudaService.CrearTooltip();
            if (txtCod != null) AyudaService.Tip(_dlgTip, txtCod, "Código único del cliente (ej: CLI-001)");
            if (txtDesc != null) AyudaService.Tip(_dlgTip, txtDesc, "Nombre completo o razón social");
            if (txtCiRif != null) AyudaService.Tip(_dlgTip, txtCiRif, "RIF o Cédula sin guiones (ej: J123456789)");
            if (txtTel != null) AyudaService.Tip(_dlgTip, txtTel, "Teléfono principal de contacto");
            if (numCred != null) AyudaService.Tip(_dlgTip, numCred, "Límite de crédito en USD (0 = sin crédito)");
            // Recordar posición y tamaño
            VentanaPrefs.Restaurar(this);
            Controls.AddRange(new Control[] { bOk, bCan });

            // Tab con seleccion completa del campo
            ConfigurarTabConSeleccion();
        
            VentanaPrefs.Restaurar(this);
        }

        private void BtnOk_Click(object sender, EventArgs e)
        {
            // Validar nombre
            string nombre = txtDesc.Text.Trim();
            if (string.IsNullOrWhiteSpace(nombre))
            {
                Warn("El nombre es obligatorio."); return;
            }
            if (!_rxNombre.IsMatch(nombre))
            {
                Warn("El nombre solo puede contener letras, números y espacios."); return;
            }

            // Validar CI/RIF unico si no esta vacio
            string ciRif = txtCiRif.Text.Trim();
            if (!string.IsNullOrWhiteSpace(ciRif))
            {
                var duplicado = DatabaseService.ObtenerClientes()
                    .FirstOrDefault(c => c.CiRif == ciRif &&
                                         c.Codigo != txtCod.Text.Trim());
                if (duplicado != null)
                {
                    Warn($"El CI/RIF '{ciRif}' ya está registrado en el cliente {duplicado.Codigo} — {duplicado.Descripcion}.");
                    return;
                }
            }

            Resultado = _original ?? new Cliente();
            Resultado.Codigo        = txtCod.Text.Trim();
            Resultado.Descripcion   = nombre;
            Resultado.Direccion     = txtDir.Text.Trim();
            Resultado.Ciudad        = txtCiud.Text.Trim();
            Resultado.Estado        = txtEdo.Text.Trim();
            Resultado.CiRif         = ciRif;
            Resultado.Nit           = txtNit.Text.Trim();
            Resultado.Fax           = txtFax.Text.Trim();
            Resultado.Telefono      = txtTel.Text.Trim();
            Resultado.Telefono2     = txtTel2.Text.Trim();
            Resultado.Credito       = numCred.Value;
            Resultado.Descuento     = numDesc.Value;
            Resultado.AFavor        = numAFav.Value;
            Resultado.TipoCliente   = cmbTipo.SelectedItem?.ToString();
            Resultado.Fecha         = dtpFecha.Value;
            Resultado.Observaciones = txtObs.Text.Trim();
            DialogResult = DialogResult.OK;
        }

        // Tab: al entrar al campo selecciona todo el contenido
        private void ConfigurarTabConSeleccion()
        {
            var campos = new Control[] {
                txtCod, txtDesc, txtDir, txtCiud, txtEdo, txtCiRif,
                txtNit, txtFax, txtTel, txtTel2, txtObs
            };
            foreach (var c in campos)
            {
                if (c is TextBox tb)
                    tb.Enter += (s, e) => tb.SelectAll();
            }
            numCred.Enter  += (s, e) => numCred.Select(0, numCred.Text.Length);
            numDesc.Enter  += (s, e) => numDesc.Select(0, numDesc.Text.Length);
            numAFav.Enter  += (s, e) => numAFav.Select(0, numAFav.Text.Length);
        }

        private void Warn(string msg) =>
            MessageBox.Show(msg, "Validacion", MessageBoxButtons.OK, MessageBoxIcon.Warning);

        private void Lbl(string t, int y) =>
            Controls.Add(new Label { Text = t, Location = new Point(12, y + 3),
                AutoSize = true, Font = new Font("Segoe UI", 9f, FontStyle.Bold) });

        private TextBox Txt(int y, bool readOnly)
        {
            var tb = new TextBox { Location = new Point(155, y), Size = new Size(270, 24),
                ReadOnly = readOnly };
            Controls.Add(tb); return tb;
        }
        private NumericUpDown Num(int y, decimal max)
        {
            var n = new NumericUpDown { Location = new Point(155, y), Size = new Size(270, 24),
                Maximum = max, DecimalPlaces = 2 };
            Controls.Add(n); return n;
        }
        private Button BBtn(string t, Color c, Point p)
        {
            var b = new Button { Text = t, Location = p, Size = new Size(130, 32),
                BackColor = c, ForeColor = Color.White, FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold) };
            b.FlatAppearance.BorderSize = 0; Controls.Add(b); return b;
        }
    }
}
