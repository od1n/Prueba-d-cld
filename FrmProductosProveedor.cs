// ════════════════════════════════════════════════════════════════════════
// ARCHIVO: FrmProductosProveedor.cs
// ════════════════════════════════════════════════════════════════════════

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace SistemaGestion
{
    public class FrmProductosProveedor : FormBase
    {
        private DataGridView          dgv;
        private Label                 lblResumen;
        private TextBox               txtBuscar;
        private ComboBox              cmbFiltroTipo;
        private CheckBox              chkSoloActivos;
        private string                _sortCol = "Descripcion";
        private bool                  _sortAsc = true;
        private List<ProductoProveedor> _todos = new();

        public FrmProductosProveedor()
        {
            Text          = "Productos de Proveedores (Insumos)";
            Size          = new Size(1150, 620);
            StartPosition = FormStartPosition.CenterParent;
            BackColor     = Color.FromArgb(245, 248, 255);

            // Header
            var hdr = new Panel { Dock = DockStyle.Top, Height = 50,
                BackColor = Color.FromArgb(40, 80, 160) };
            hdr.Controls.Add(new Label {
                Text = "Productos de Proveedores — Insumos y Materiales",
                ForeColor = Color.White, AutoSize = true,
                Font = new Font("Segoe UI", 12f, FontStyle.Bold),
                Location = new Point(12, 13) });
            var btnN = Btn("+ Nuevo",  Color.FromArgb(20, 140, 70),  new Point(730, 8));
            var btnE = Btn("Editar",   Color.FromArgb(60, 100, 180), new Point(828, 8));
            var btnD = Btn("Eliminar", Color.FromArgb(180, 60, 60),  new Point(926, 8));
            hdr.Controls.AddRange(new Control[] { btnN, btnE, btnD });

            // Barra de filtros
            var bar = new Panel { Dock = DockStyle.Top, Height = 42,
                BackColor = Color.FromArgb(228, 235, 255) };
            int fx = 8;
            bar.Controls.Add(L("Buscar:", fx, 12)); fx += 56;
            txtBuscar = new TextBox { Location = new Point(fx, 8), Size = new Size(200, 26) };
            txtBuscar.TextChanged += (s, e) => Filtrar();
            bar.Controls.Add(txtBuscar); fx += 210;

            bar.Controls.Add(L("Tipo:", fx, 12)); fx += 42;
            cmbFiltroTipo = new ComboBox { Location = new Point(fx, 8),
                Size = new Size(160, 26), DropDownStyle = ComboBoxStyle.DropDownList };
            cmbFiltroTipo.Items.Add("Todos los tipos");
            ProductoProveedor.Tipos.ToList().ForEach(t => cmbFiltroTipo.Items.Add(t));
            cmbFiltroTipo.SelectedIndex = 0;
            cmbFiltroTipo.SelectedIndexChanged += (s, e) => Filtrar();
            bar.Controls.Add(cmbFiltroTipo); fx += 170;

            chkSoloActivos = new CheckBox { Text = "Solo activos",
                Location = new Point(fx, 10), AutoSize = true,
                Font = new Font("Segoe UI", 9f), Checked = true };
            chkSoloActivos.CheckedChanged += (s, e) => Filtrar();
            bar.Controls.Add(chkSoloActivos);

            // Grid
            dgv = CrearGrid(Color.FromArgb(40, 80, 160));
            dgv.Name = "dgv";
            dgv.ColumnHeaderMouseClick += DgvSort;
            dgv.CellDoubleClick        += (s, e) => Editar();

            // Footer
            var footer = new Panel { Dock = DockStyle.Bottom, Height = 40,
                BackColor = Color.FromArgb(220, 228, 252) };
            lblResumen = new Label { AutoSize = true, Location = new Point(12, 10),
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                ForeColor = Color.FromArgb(30, 60, 140) };
            var btnC = BtnF("Cerrar", footer, new Point(1030, 6));
            btnC.Click += (s, e) => Close();
            footer.Controls.AddRange(new Control[] { lblResumen, btnC });

            Controls.AddRange(new Control[] { dgv, bar, hdr, footer });

            btnN.Click += (s, e) => Nuevo();
            btnE.Click += (s, e) => Editar();
            btnD.Click += (s, e) => Eliminar();

            VentanaPrefs.Restaurar(this);

            CargarDatos();
        }

        private void Nuevo()
        {
            using var d = new DlgProductoProveedor(null);
            if (d.ShowDialog() == DialogResult.OK)
            { DatabaseService.InsertarProductoProveedor(d.Resultado); CargarDatos(); }
        }

        private void Editar()
        {
            var item = GetSel(); if (item == null) return;
            using var d = new DlgProductoProveedor(item);
            if (d.ShowDialog() == DialogResult.OK)
            { DatabaseService.ActualizarProductoProveedor(d.Resultado); CargarDatos(); }
        }

        private void Eliminar()
        {
            var item = GetSel(); if (item == null) return;
            if (MessageBox.Show(
                $"Eliminar insumo '{item.Descripcion}' ({item.CodigoInterno})?",
                "Confirmar", MessageBoxButtons.YesNo, MessageBoxIcon.Warning)
                == DialogResult.Yes)
            { DatabaseService.EliminarProductoProveedor(item.Id); CargarDatos(); }
        }

        private void CargarDatos()
        {
            _todos = DatabaseService.ObtenerProductosProveedor();
            Filtrar();
        }

        private void Filtrar()
        {
            var q    = txtBuscar.Text.ToLower();
            var tipo = cmbFiltroTipo.SelectedIndex <= 0 ? null
                : cmbFiltroTipo.SelectedItem.ToString();

            var lista = _todos.Where(pp =>
                (!chkSoloActivos.Checked || pp.Activo) &&
                (tipo == null || pp.TipoInsumo == tipo) &&
                BusquedaHelper.ContieneFlexibleEnAlguno(q,
                    pp.CodigoInterno, pp.Descripcion, pp.NombreProveedor)
            ).ToList();

            lista = _sortCol switch {
                "CodigoInterno"   => _sortAsc ? lista.OrderByNatural(p => p.CodigoInterno).ToList()
                                              : lista.OrderByDescendingNatural(p => p.CodigoInterno).ToList(),
                "TipoInsumo"      => _sortAsc ? lista.OrderByNatural(p => p.TipoInsumo).ToList()
                                              : lista.OrderByDescendingNatural(p => p.TipoInsumo).ToList(),
                "Proveedor"       => _sortAsc ? lista.OrderByNatural(p => p.NombreProveedor).ToList()
                                              : lista.OrderByDescendingNatural(p => p.NombreProveedor).ToList(),
                "StockActual"     => _sortAsc ? lista.OrderBy(p => p.StockActual).ToList()
                                              : lista.OrderByDescending(p => p.StockActual).ToList(),
                "PrecioUltCompra" => _sortAsc ? lista.OrderBy(p => p.PrecioUltCompra).ToList()
                                              : lista.OrderByDescending(p => p.PrecioUltCompra).ToList(),
                _ => _sortAsc ? lista.OrderByNatural(p => p.Descripcion).ToList()
                              : lista.OrderByDescendingNatural(p => p.Descripcion).ToList()
            };

            dgv.DataSource = null;
            dgv.DataSource = lista.Select(pp => new {
                pp.CodigoInterno,
                pp.Descripcion,
                pp.TipoInsumo,
                pp.Unidad,
                Proveedor       = pp.NombreProveedor,
                PrecioUltCompra = pp.PrecioUltCompra.ToString("N2"),
                PrecioPromedio  = pp.PrecioPromedio.ToString("N2"),
                StockActual     = pp.StockActual.ToString("N2"),
                StockMinimo     = pp.StockMinimo.ToString("N2"),
                Estado          = pp.Activo ? "Activo" : "Inactivo"
            }).ToList();
            VentanaPrefs.AplicarAnchos(this, dgv);

            foreach (DataGridViewColumn col in dgv.Columns)
                col.HeaderCell.SortGlyphDirection = SortOrder.None;
            if (dgv.Columns.Contains(_sortCol))
                dgv.Columns[_sortCol].HeaderCell.SortGlyphDirection =
                    _sortAsc ? SortOrder.Ascending : SortOrder.Descending;

            // Colorear bajo mínimo
            var listaOrig = lista;
            dgv.DataBindingComplete += (s, e) => {
                for (int i = 0; i < dgv.Rows.Count && i < listaOrig.Count; i++)
                    if (listaOrig[i].BajoMinimo)
                        dgv.Rows[i].DefaultCellStyle.BackColor = Color.FromArgb(255, 240, 200);
                    else if (!listaOrig[i].Activo)
                        dgv.Rows[i].DefaultCellStyle.ForeColor = Color.FromArgb(160, 160, 170);
            };

            lblResumen.Text =
                $"  Insumos: {lista.Count}   |   " +
                $"Bajo mínimo: {lista.Count(p => p.BajoMinimo)}   |   " +
                $"Inactivos: {lista.Count(p => !p.Activo)}";
        }

        private void DgvSort(object sender, DataGridViewCellMouseEventArgs e)
        {
            string col = dgv.Columns[e.ColumnIndex].Name;
            var ok = new[] {
                "CodigoInterno","Descripcion","TipoInsumo",
                "Proveedor","StockActual","PrecioUltCompra" };
            if (!ok.Contains(col)) return;
            if (_sortCol == col) _sortAsc = !_sortAsc;
            else { _sortCol = col; _sortAsc = true; }
            Filtrar();
        }

        private ProductoProveedor GetSel()
        {
            if (dgv.CurrentRow == null) return null;
            var cod = dgv.CurrentRow.Cells["CodigoInterno"].Value?.ToString();
            return _todos.FirstOrDefault(p => p.CodigoInterno == cod);
        }

        private DataGridView CrearGrid(Color h)
        {
            var g = new DataGridView {
                Dock = DockStyle.Fill, ReadOnly = true,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false, AllowUserToAddRows = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None,
                BackgroundColor = Color.White, BorderStyle = BorderStyle.None,
                RowHeadersVisible = false, Font = new Font("Segoe UI", 9f),
                RowTemplate = { Height = 24 } };
            g.ColumnHeadersDefaultCellStyle.BackColor = h;
            g.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
            g.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 9f, FontStyle.Bold);
            g.EnableHeadersVisualStyles = false;
            g.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(238, 243, 255);
            return g;
        }

        private Button Btn(string t, Color c, Point p) {
            var b = new Button { Text=t, Size=new Size(90,33), Location=p,
                BackColor=c, ForeColor=Color.White, FlatStyle=FlatStyle.Flat,
                Font=new Font("Segoe UI",8.5f,FontStyle.Bold), Cursor=Cursors.Hand };
            b.FlatAppearance.BorderSize=0; return b; }

        private Button BtnF(string t, Panel par, Point p) {
            var b = new Button { Text=t, Size=new Size(90,27), Location=p,
                BackColor=Color.FromArgb(100,100,120), ForeColor=Color.White,
                FlatStyle=FlatStyle.Flat };
            b.FlatAppearance.BorderSize=0; par.Controls.Add(b); return b; }

        private Label L(string t, int x, int y) =>
            new Label { Text=t, Location=new Point(x,y), AutoSize=true,
                Font=new Font("Segoe UI",9f) };
    }

    // ════════════════════════════════════════════════════════════════════════
    //  DIALOGO NUEVO / EDITAR INSUMO
    // ════════════════════════════════════════════════════════════════════════
    public class DlgProductoProveedor : FormBase
    {
        public ProductoProveedor Resultado { get; private set; }

        private TextBox        txtCodigo, txtCodProv, txtDesc, txtUnidad, txtObs;
        private ComboBox       cmbTipo, cmbProveedor;
        private NumericUpDown  nudPrecioUlt, nudPrecioPromedio, nudStock, nudStockMin;
        private CheckBox       chkActivo;

        private readonly ProductoProveedor   _original;
        private static readonly Regex _rxDesc =
            new Regex(@"^[a-zA-ZáéíóúÁÉÍÓÚüÜñÑ0-9\s\-\.\,\/\(\)\""+]+$");

        public DlgProductoProveedor(ProductoProveedor pp)
        {
            _original = pp;
            Text      = pp == null ? "Nuevo Insumo / Producto Proveedor" : "Editar Insumo";
            Size      = new Size(500, 600);
            StartPosition   = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = MinimizeBox = false;
            BackColor   = Color.FromArgb(245, 248, 255);

            int y = 15, lx = 15, fx = 170, w = 295;

            L("Código interno *:", lx, y);
            txtCodigo = Txt(fx, y, w); y += 34;

            L("Cód. proveedor:", lx, y);
            txtCodProv = Txt(fx, y, w); y += 34;

            L("Descripción *:", lx, y);
            txtDesc = Txt(fx, y, w); y += 34;

            L("Tipo insumo *:", lx, y);
            cmbTipo = new ComboBox { Location = new Point(fx, y),
                Size = new Size(w, 26), DropDownStyle = ComboBoxStyle.DropDownList };
            ProductoProveedor.Tipos.ToList().ForEach(t => cmbTipo.Items.Add(t));
            cmbTipo.SelectedIndex = 0;
            Controls.Add(cmbTipo); y += 34;

            L("Unidad *:", lx, y);
            txtUnidad = Txt(fx, y, 100);
            Controls.Add(new Label { Text = "ej: kg, L, und, m",
                Location = new Point(fx + 108, y + 4), AutoSize = true,
                Font = new Font("Segoe UI", 7.5f), ForeColor = Color.Gray });
            y += 34;

            L("Proveedor principal:", lx, y);
            cmbProveedor = new ComboBox { Location = new Point(fx, y),
                Size = new Size(w, 26), DropDownStyle = ComboBoxStyle.DropDownList };
            cmbProveedor.Items.Add("-- Sin proveedor --");
            DatabaseService.ObtenerProveedores()
                .ForEach(p => cmbProveedor.Items.Add(p.Codigo + " — " + p.Descripcion));
            cmbProveedor.SelectedIndex = 0;
            Controls.Add(cmbProveedor); y += 34;

            L("Precio últ. compra:", lx, y);
            nudPrecioUlt = Num(fx, y, 140, 2); y += 34;

            L("Precio promedio:", lx, y);
            nudPrecioPromedio = Num(fx, y, 140, 2); y += 34;

            L("Stock actual:", lx, y);
            nudStock = Num(fx, y, 140, 2); y += 34;

            L("Stock mínimo:", lx, y);
            nudStockMin = Num(fx, y, 140, 2); y += 34;

            L("Activo:", lx, y);
            chkActivo = new CheckBox { Location = new Point(fx, y),
                AutoSize = true, Checked = true, Font = new Font("Segoe UI", 9f) };
            Controls.Add(chkActivo); y += 34;

            L("Observaciones:", lx, y);
            txtObs = new TextBox { Location = new Point(fx, y),
                Size = new Size(w, 50), Multiline = true };
            Controls.Add(txtObs); y += 58;

            Controls.Add(new Label {
                Text = "* Campos obligatorios",
                Location = new Point(lx, y), AutoSize = true,
                Font = new Font("Segoe UI", 8f, FontStyle.Italic),
                ForeColor = Color.FromArgb(100, 60, 0) });
            y += 24;

            var bOk  = BBtn("Guardar",  Color.FromArgb(40, 80, 160), new Point(100, y));
            var bCan = BBtn("Cancelar", Color.FromArgb(160, 50, 50), new Point(270, y));
            bCan.DialogResult = DialogResult.Cancel;
            bOk.Click += BtnOk_Click;
            Controls.AddRange(new Control[] { bOk, bCan });
            AcceptButton = bOk; CancelButton = bCan;

            // Pre-rellenar si es edición
            if (pp != null)
            {
                txtCodigo.Text  = pp.CodigoInterno; txtCodigo.ReadOnly = true;
                txtCodProv.Text = pp.CodigoProveedor ?? "";
                txtDesc.Text    = pp.Descripcion;
                if (cmbTipo.Items.Contains(pp.TipoInsumo))
                    cmbTipo.SelectedItem = pp.TipoInsumo;
                txtUnidad.Text = pp.Unidad;
                // Seleccionar proveedor
                if (!string.IsNullOrWhiteSpace(pp.IdProveedorPrinc))
                    for (int i = 0; i < cmbProveedor.Items.Count; i++)
                        if (cmbProveedor.Items[i].ToString()
                            .StartsWith(pp.IdProveedorPrinc + " —"))
                        { cmbProveedor.SelectedIndex = i; break; }
                nudPrecioUlt.Value      = pp.PrecioUltCompra;
                nudPrecioPromedio.Value = pp.PrecioPromedio;
                nudStock.Value          = pp.StockActual;
                nudStockMin.Value       = pp.StockMinimo;
                chkActivo.Checked       = pp.Activo;
                txtObs.Text             = pp.Observaciones ?? "";
            }
            else
            {
                // Código automático
                txtCodigo.Text     = DatabaseService.GenerarCodigoInsumo();
                txtCodigo.ReadOnly = true;
            }

            // Tab con selección completa
            ConfigurarTab();
        }

        private void BtnOk_Click(object sender, EventArgs e)
        {
            // Validaciones
            if (string.IsNullOrWhiteSpace(txtDesc.Text.Trim()))
            { Warn("La descripción es obligatoria."); return; }

            if (!_rxDesc.IsMatch(txtDesc.Text.Trim()))
            { Warn("La descripción contiene caracteres no permitidos."); return; }

            if (string.IsNullOrWhiteSpace(txtUnidad.Text.Trim()))
            { Warn("La unidad de medida es obligatoria (ej: kg, L, und)."); return; }

            if (nudStockMin.Value > nudStock.Value && nudStock.Value > 0)
            {
                if (MessageBox.Show(
                    "El stock mínimo es mayor al stock actual. ¿Continuar?",
                    "Advertencia", MessageBoxButtons.YesNo, MessageBoxIcon.Warning)
                    == DialogResult.No) return;
            }

            // Proveedor
            string codProv = cmbProveedor.SelectedIndex <= 0 ? null
                : cmbProveedor.SelectedItem.ToString().Split('—')[0].Trim();

            Resultado = _original ?? new ProductoProveedor();
            Resultado.CodigoInterno    = txtCodigo.Text.Trim();
            Resultado.CodigoProveedor  = txtCodProv.Text.Trim();
            Resultado.IdProveedorPrinc = codProv;
            Resultado.Descripcion      = txtDesc.Text.Trim();
            Resultado.TipoInsumo       = cmbTipo.SelectedItem?.ToString() ?? "Materia prima";
            Resultado.Unidad           = txtUnidad.Text.Trim();
            Resultado.PrecioUltCompra  = nudPrecioUlt.Value;
            Resultado.PrecioPromedio   = nudPrecioPromedio.Value;
            Resultado.StockActual      = nudStock.Value;
            Resultado.StockMinimo      = nudStockMin.Value;
            Resultado.Activo           = chkActivo.Checked;
            Resultado.Observaciones    = txtObs.Text.Trim();
            DialogResult = DialogResult.OK;
        }

        private void ConfigurarTab()
        {
            var textos = new TextBox[] { txtCodigo, txtCodProv, txtDesc, txtUnidad, txtObs };
            foreach (var tb in textos)
                tb.Enter += (s, e) => ((TextBox)s).SelectAll();

            var nums = new NumericUpDown[] {
                nudPrecioUlt, nudPrecioPromedio, nudStock, nudStockMin };
            foreach (var n in nums)
                n.Enter += (s, e) => ((NumericUpDown)s).Select(
                    0, ((NumericUpDown)s).Text.Length);
        }

        private void Warn(string msg) =>
            MessageBox.Show(msg, "Validacion", MessageBoxButtons.OK, MessageBoxIcon.Warning);

        private void L(string t, int x, int y) =>
            Controls.Add(new Label { Text=t, Location=new Point(x, y+3),
                AutoSize=true, Font=new Font("Segoe UI", 9f, FontStyle.Bold) });

        private TextBox Txt(int x, int y, int w) {
            var tb = new TextBox { Location=new Point(x,y), Size=new Size(w,26) };
            Controls.Add(tb); return tb; }

        private NumericUpDown Num(int x, int y, int w, int dec) {
            var n = new NumericUpDown { Location=new Point(x,y), Size=new Size(w,26),
                Maximum=9999999, DecimalPlaces=dec, ThousandsSeparator=true };
            Controls.Add(n); return n; }

        private Button BBtn(string t, Color c, Point p) {
            var b = new Button { Text=t, Location=p, Size=new Size(120,32),
                BackColor=c, ForeColor=Color.White, FlatStyle=FlatStyle.Flat,
                Font=new Font("Segoe UI",9f,FontStyle.Bold) };
            b.FlatAppearance.BorderSize=0; Controls.Add(b); return b; }
    }
}
