using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace SistemaGestion
{
    public class FrmCompras : FormBase
    {
        private DataGridView dgv;
        private Label        lblResumen;
        private string       _sortCol = "CompraN";
        private bool         _sortAsc = false; // mas reciente primero por defecto

        public FrmCompras()
        {
            Text          = "Compras";
            Size          = new Size(950, 540);
            StartPosition = FormStartPosition.CenterParent;
            BackColor     = Color.FromArgb(248, 245, 255);

            var hdr = new Panel { Dock = DockStyle.Top, Height = 50,
                BackColor = Color.FromArgb(100, 50, 160) };
            hdr.Controls.Add(new Label { Text = "Registro de Compras",
                ForeColor = Color.White, Font = new Font("Segoe UI", 12f, FontStyle.Bold),
                AutoSize = true, Location = new Point(12, 13) });
            var btnN = Btn("+ Nueva",  Color.FromArgb(40, 160, 80));
            var btnE = Btn("Editar",   Color.FromArgb(60, 100, 180));
            var btnD = Btn("Eliminar", Color.FromArgb(180, 60, 60));
            
            var _tpF2 = AyudaService.CrearTooltip();
            var _btnA2 = new Button {
                Text = "? Ayuda", Size = new Size(80, 32),
                BackColor = Color.FromArgb(80, 50, 150), ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
                Cursor = Cursors.Hand, Margin = new Padding(0, 0, 4, 0)
            };
            _btnA2.FlatAppearance.BorderSize = 0;
            _btnA2.Click += (s, e) => AyudaService.Mostrar(this, "FrmCompras");
            AyudaService.Tip(_tpF2, _btnA2, "Abrir guía de ayuda");
            AyudaService.Tip(_tpF2, btnN, "Registrar una nueva compra a proveedor");
            AyudaService.Tip(_tpF2, btnE, "Ver y modificar la compra seleccionada");
            AyudaService.Tip(_tpF2, btnD, "Eliminar la compra seleccionada");
            var _pnlHF = new FlowLayoutPanel {
                Dock = DockStyle.Right, AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false, BackColor = Color.Transparent,
                Padding = new Padding(0, 9, 8, 0)
            };
            _pnlHF.Controls.AddRange(new Control[] { _btnA2, btnN, btnE, btnD });
            hdr.Controls.Add(_pnlHF);

            dgv = new DataGridView {
                Name = "dgv",
                Dock = DockStyle.Fill, ReadOnly = true,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false, AllowUserToAddRows = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None,
                BackgroundColor = Color.White, BorderStyle = BorderStyle.None,
                RowHeadersVisible = false
            };
            dgv.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(100, 50, 160);
            dgv.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
            dgv.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 9f, FontStyle.Bold);
            dgv.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(245, 240, 255);
            dgv.EnableHeadersVisualStyles = false;
            dgv.ColumnHeaderMouseClick += DgvColumnHeaderMouseClick;
            dgv.CellDoubleClick += (s, e) => {
                var cp = GetSel();
                if (cp == null) return;
                using var d = new DlgCompra(cp);
                if (d.ShowDialog() == DialogResult.OK)
                { DatabaseService.ActualizarCompra(d.Resultado); CargarDatos(); }
            };

            var footer = new Panel { Dock = DockStyle.Bottom, Height = 40,
                BackColor = Color.FromArgb(232, 225, 248) };
            lblResumen = new Label { AutoSize = true, Location = new Point(12, 10),
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                ForeColor = Color.FromArgb(70, 30, 120) };
            var btnC = new Button { Text = "Cerrar", Size = new Size(90, 27),
                Location = new Point(830, 6), BackColor = Color.FromArgb(100, 100, 120),
                ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
            btnC.FlatAppearance.BorderSize = 0;
            btnC.Click += (s, e) => Close();
            footer.Controls.AddRange(new Control[] { lblResumen, btnC });

            Controls.AddRange(new Control[] { dgv, hdr, footer });

            btnN.Click += (s, e) => {
                using var d = new DlgCompra(null);
                if (d.ShowDialog() == DialogResult.OK)
                { DatabaseService.InsertarCompra(d.Resultado); CargarDatos(); }
            };
            btnE.Click += (s, e) => {
                var cp = GetSel(); if (cp == null) return;
                using var d = new DlgCompra(cp);
                if (d.ShowDialog() == DialogResult.OK)
                { DatabaseService.ActualizarCompra(d.Resultado); CargarDatos(); }
            };
            btnD.Click += (s, e) => {
                var cp = GetSel(); if (cp == null) return;
                if (MessageBox.Show($"Eliminar compra {cp.CompraN}?", "Confirmar",
                    MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes)
                { DatabaseService.EliminarCompra(cp.CompraN); CargarDatos(); }
            };

            VentanaPrefs.Restaurar(this);
            var _vpT2 = new System.Windows.Forms.Timer { Interval = 800 };
            _vpT2.Tick       += (s, e) => { _vpT2.Stop(); VentanaPrefs.Guardar(this); };
            SizeChanged     += (s, e) => { _vpT2.Stop(); _vpT2.Start(); };
            LocationChanged += (s, e) => { _vpT2.Stop(); _vpT2.Start(); };
            FormClosed      += (s, e) => { _vpT2.Stop(); _vpT2.Dispose(); };

            CargarDatos();
        }

        private void CargarDatos()
        {
            var lista = DatabaseService.ObtenerCompras();

            lista = _sortCol switch {
                "Fecha"     => _sortAsc ? lista.OrderBy(c => c.FechaC).ToList()
                                        : lista.OrderByDescending(c => c.FechaC).ToList(),
                "Proveedor" => _sortAsc ? lista.OrderBy(c => c.CodigoP).ToList()
                                        : lista.OrderByDescending(c => c.CodigoP).ToList(),
                "Total"     => _sortAsc ? lista.OrderBy(c => c.TotalC).ToList()
                                        : lista.OrderByDescending(c => c.TotalC).ToList(),
                _           => _sortAsc ? lista.OrderBy(c => c.CompraN).ToList()
                                        : lista.OrderByDescending(c => c.CompraN).ToList()
            };

            dgv.DataSource = null;
            dgv.DataSource = lista.Select(cp => new {
                cp.CompraN,
                Fecha     = cp.FechaC.ToShortDateString(),
                Proveedor = cp.CodigoP,
                cp.FormaDePago,
                Descuento = $"{cp.DescuentoC}%",
                IVA       = cp.IvaC.ToString("C2"),
                Total     = cp.TotalC.ToString("C2"),
                cp.DiasDeCred
            }).ToList();
            VentanaPrefs.AplicarAnchos(this, dgv);

            foreach (DataGridViewColumn col in dgv.Columns)
                col.HeaderCell.SortGlyphDirection = SortOrder.None;
            if (dgv.Columns.Contains(_sortCol))
                dgv.Columns[_sortCol].HeaderCell.SortGlyphDirection =
                    _sortAsc ? SortOrder.Ascending : SortOrder.Descending;

            lblResumen.Text =
                $"  Compras: {lista.Count}   |   " +
                $"Total comprado: {lista.Sum(c => c.TotalC):C2}   |   " +
                $"Abonado: {lista.Sum(c => c.AbonoC):C2}";
        }

        private void DgvColumnHeaderMouseClick(object sender,
            DataGridViewCellMouseEventArgs e)
        {
            string col = dgv.Columns[e.ColumnIndex].Name;
            var permitidas = new[] { "CompraN", "Fecha", "Proveedor", "Total" };
            if (!permitidas.Contains(col)) return;
            if (_sortCol == col) _sortAsc = !_sortAsc;
            else { _sortCol = col; _sortAsc = true; }
            CargarDatos();
        }

        private Compra GetSel()
        {
            if (dgv.CurrentRow == null) return null;
            var num = dgv.CurrentRow.Cells["CompraN"].Value?.ToString();
            return DatabaseService.ObtenerCompras().Find(x => x.CompraN == num);
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
            var b = new Button { Text = t, Size = new Size(90, 33),
                BackColor = c, ForeColor = Color.White, FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 8.5f, FontStyle.Bold), Cursor = Cursors.Hand };
            b.Margin = new Padding(0, 0, 4, 0);
            b.FlatAppearance.BorderSize = 0; return b;
        }

    }

    // ════════════════════════════════════════════════════════════════════════
    //  DIALOGO COMPRA
    // ════════════════════════════════════════════════════════════════════════
    public class DlgCompra : FormBase
    {
        public Compra Resultado { get; private set; }

        private TextBox        txtNum, txtFPago, txtTransp, txtObs;
        private ComboBox       cmbProveedor;
        private ComboBox       cmbIva;          // 0% o valor configurable
        private NumericUpDown  numDesc, numAbono, numTotal;
        private NumericUpDown  numDias;
        private DateTimePicker dtpFecha;

        // Grid de detalles (productos)
        private DataGridView   dgvDetalles;

        private readonly Compra _original;

        // IVA configurable — leer de PreferenciasUsuario o usar 16 por defecto
        private static decimal _tasaIva = 16m;

        public DlgCompra(Compra cp)
        {
            _original   = cp;

            Text = cp == null ? "Nueva Compra" : "Editar Compra";
            Size = new Size(700, 620);
            StartPosition   = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.Sizable;
            MaximizeBox = true; MinimizeBox = false;
            BackColor   = Color.FromArgb(240, 244, 250);

            BuildUI(cp);
        
            // Recordar posición y tamaño
            VentanaPrefs.Restaurar(this);
        }

        private void BuildUI(Compra cp)
        {
            // ── Panel izquierdo: datos de la compra ───────────────────────
            var pnlDatos = new Panel { Location = new Point(0, 0),
                Size = new Size(340, 570), BackColor = Color.FromArgb(248, 245, 255) };

            int y = 12;
            L(pnlDatos, "No. Compra:", y);
            txtNum = Txt(pnlDatos, y); y += 33;

            L(pnlDatos, "Proveedor:", y);
            cmbProveedor = new ComboBox { Location = new Point(130, y),
                Size = new Size(195, 24), DropDownStyle = ComboBoxStyle.DropDownList };
            cmbProveedor.Items.Add("-- Sin proveedor --");
            DatabaseService.ObtenerProveedores()
                .ForEach(p => cmbProveedor.Items.Add(p.Codigo + " — " + p.Descripcion));
            cmbProveedor.SelectedIndex = 0;
            pnlDatos.Controls.Add(cmbProveedor); y += 33;

            L(pnlDatos, "Fecha:", y);
            dtpFecha = new DateTimePicker { Location = new Point(130, y),
                Size = new Size(195, 24), Format = DateTimePickerFormat.Short };
            pnlDatos.Controls.Add(dtpFecha); y += 33;

            L(pnlDatos, "Forma de Pago:", y); txtFPago = Txt(pnlDatos, y); y += 33;
            L(pnlDatos, "Descuento (%):", y); numDesc  = Num(pnlDatos, y, 100); y += 33;
            L(pnlDatos, "Abono:", y);          numAbono = Num(pnlDatos, y, 9999999); y += 33;

            L(pnlDatos, "IVA:", y);
            cmbIva = new ComboBox { Location = new Point(130, y), Size = new Size(195, 24),
                DropDownStyle = ComboBoxStyle.DropDownList };
            cmbIva.Items.Add("0%");
            cmbIva.Items.Add($"{_tasaIva}%");
            cmbIva.SelectedIndex = 0;
            pnlDatos.Controls.Add(cmbIva); y += 33;

            L(pnlDatos, "Dias Credito:", y); numDias  = Num(pnlDatos, y, 365);
            numDias.DecimalPlaces = 0; y += 33;
            L(pnlDatos, "Total:", y);         numTotal = Num(pnlDatos, y, 99999999); y += 33;
            L(pnlDatos, "Transporte:", y);    txtTransp = Txt(pnlDatos, y); y += 33;
            L(pnlDatos, "Observaciones:", y);
            txtObs = new TextBox { Location = new Point(130, y), Size = new Size(195, 50),
                Multiline = true };
            pnlDatos.Controls.Add(txtObs);

            // ── Panel derecho: detalle de productos ───────────────────────
            var pnlDet = new Panel { Location = new Point(345, 0),
                Size = new Size(340, 570), BackColor = Color.White };

            var hdrDet = new Panel { Dock = DockStyle.Top, Height = 30,
                BackColor = Color.FromArgb(100, 50, 160) };
            hdrDet.Controls.Add(new Label { Text = "Productos de la compra",
                ForeColor = Color.White, Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                AutoSize = true, Location = new Point(6, 6) });
            var btnAgrDet = new Button { Text = "+ Agregar", Size = new Size(80, 24),
                Location = new Point(250, 3), BackColor = Color.FromArgb(40, 160, 80),
                ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
            btnAgrDet.FlatAppearance.BorderSize = 0;
            btnAgrDet.Click += (s, e) => AgregarFilaDetalle();
            var btnQuitDet = new Button { Text = "Quitar", Size = new Size(60, 24),
                Location = new Point(186, 3), BackColor = Color.FromArgb(180, 50, 50),
                ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
            btnQuitDet.FlatAppearance.BorderSize = 0;
            btnQuitDet.Click += (s, e) => QuitarFilaDetalle();
            hdrDet.Controls.AddRange(new Control[] { btnAgrDet, btnQuitDet });

            dgvDetalles = new DataGridView {
                Name = "dgvDetalles",
                Dock = DockStyle.Fill, AllowUserToAddRows = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                BackgroundColor = Color.White, BorderStyle = BorderStyle.None,
                RowHeadersVisible = false, Font = new Font("Segoe UI", 8.5f),
                EditMode = DataGridViewEditMode.EditOnEnter
            };


            dgvDetalles.Name = "dgvDetalles";
            dgvDetalles.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(100, 50, 160);
            dgvDetalles.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
            dgvDetalles.EnableHeadersVisualStyles = false;

            var colProd = new DataGridViewComboBoxColumn {
                Name = "Producto", HeaderText = "Producto (Insumo)",
                DisplayStyle = DataGridViewComboBoxDisplayStyle.DropDownButton, Width = 200
            };
            colProd.Items.Add("");
            DatabaseService.ObtenerProductosProveedor(soloActivos: true)
                .ForEach(i => colProd.Items.Add(i.CodigoInterno + " — " + i.Descripcion));

            dgvDetalles.Columns.Add(colProd);
            dgvDetalles.Columns.Add(new DataGridViewTextBoxColumn {
                Name = "Cantidad", HeaderText = "Cantidad", Width = 80 });
            dgvDetalles.Columns.Add(new DataGridViewTextBoxColumn {
                Name = "PrecioC", HeaderText = "Precio C/U", Width = 80 });

            dgvDetalles.CurrentCellDirtyStateChanged += (s, e) => {
                if (dgvDetalles.IsCurrentCellDirty)
                    dgvDetalles.CommitEdit(DataGridViewDataErrorContexts.Commit);
            };

            pnlDet.Controls.Add(dgvDetalles);
            pnlDet.Controls.Add(hdrDet);

            // ── Botones ───────────────────────────────────────────────────
            var bOk = new Button { Text = "Guardar", Location = new Point(120, 575),
                Size = new Size(130, 32), BackColor = Color.FromArgb(100, 50, 160),
                ForeColor = Color.White, FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold) };
            bOk.FlatAppearance.BorderSize = 0;
            bOk.Click += BtnOk_Click;

            var bCan = new Button { Text = "Cancelar", DialogResult = DialogResult.Cancel,
                Location = new Point(265, 575), Size = new Size(130, 32),
                BackColor = Color.FromArgb(160, 50, 50), ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat };
            bCan.FlatAppearance.BorderSize = 0;

            Controls.AddRange(new Control[] { pnlDatos, pnlDet, bOk, bCan });

            // Generar numero automatico
            if (cp == null)
            {
                txtNum.Text = GenerarNumero();
                txtNum.ReadOnly = true;
            }
            else
            {
                txtNum.Text = cp.CompraN; txtNum.ReadOnly = true;
                // Seleccionar proveedor
                for (int i = 0; i < cmbProveedor.Items.Count; i++)
                    if (cmbProveedor.Items[i].ToString().StartsWith(cp.CodigoP + " —"))
                    { cmbProveedor.SelectedIndex = i; break; }
                dtpFecha.Value   = cp.FechaC;
                txtFPago.Text    = cp.FormaDePago;
                numDesc.Value    = cp.DescuentoC;
                numAbono.Value   = cp.AbonoC;
                numDias.Value    = cp.DiasDeCred;
                numTotal.Value   = cp.TotalC;
                txtTransp.Text   = cp.Transporte;
                txtObs.Text      = cp.Observaciones;

                // Seleccionar IVA
                decimal pctIva = cp.TotalC > 0
                    ? Math.Round(cp.IvaC / cp.TotalC * 100, 0) : 0;
                cmbIva.SelectedIndex = pctIva == 0 ? 0 : 1;
            }

            // Tab con seleccion
            foreach (var tb in new TextBox[] { txtNum, txtFPago, txtTransp, txtObs })
                tb.Enter += (s, e) => ((TextBox)s).SelectAll();
            numDesc.Enter  += (s, e) => numDesc.Select(0, numDesc.Text.Length);
            numAbono.Enter += (s, e) => numAbono.Select(0, numAbono.Text.Length);
            numTotal.Enter += (s, e) => numTotal.Select(0, numTotal.Text.Length);
            numDias.Enter  += (s, e) => numDias.Select(0, numDias.Text.Length);
        }

        private string GenerarNumero()
        {
            var nums = DatabaseService.ObtenerCompras()
                .Select(c => { int.TryParse(c.CompraN.Replace("C", ""), out int n); return n; })
                .Where(n => n > 0).ToList();
            int sig = nums.Any() ? nums.Max() + 1 : 1;
            return "C" + sig.ToString("D5");
        }

        private void AgregarFilaDetalle()
        {
            dgvDetalles.Rows.Add("", "1", "0");
        }

        private void QuitarFilaDetalle()
        {
            if (dgvDetalles.CurrentRow != null && !dgvDetalles.CurrentRow.IsNewRow)
                dgvDetalles.Rows.RemoveAt(dgvDetalles.CurrentRow.Index);
        }

        private void BtnOk_Click(object sender, EventArgs e)
        {
            // Validar proveedor
            if (cmbProveedor.SelectedIndex <= 0)
            {
                Warn("Selecciona un proveedor."); return;
            }

            // Validar al menos un detalle
            bool tieneDetalle = dgvDetalles.Rows.Cast<DataGridViewRow>()
                .Any(r => !r.IsNewRow &&
                     !string.IsNullOrWhiteSpace(r.Cells["Producto"].Value?.ToString()));
            if (!tieneDetalle)
            {
                Warn("Agrega al menos un producto en el detalle."); return;
            }

            // Validar IVA no negativo (siempre cumple con combo)
            decimal iva = cmbIva.SelectedIndex == 0 ? 0m : _tasaIva;

            string codProv = cmbProveedor.SelectedItem.ToString().Split('—')[0].Trim();

            Resultado = _original ?? new Compra();
            Resultado.CompraN     = txtNum.Text.Trim();
            Resultado.CodigoP     = codProv;
            Resultado.FechaC      = dtpFecha.Value;
            Resultado.FormaDePago = txtFPago.Text.Trim();
            Resultado.DescuentoC  = numDesc.Value;
            Resultado.AbonoC      = numAbono.Value;
            Resultado.IvaC        = Math.Round(numTotal.Value * iva / 100, 2);
            Resultado.DiasDeCred  = (int)numDias.Value;
            Resultado.TotalC      = numTotal.Value;
            Resultado.Transporte  = txtTransp.Text.Trim();
            Resultado.Observaciones = txtObs.Text.Trim();
            DialogResult = DialogResult.OK;
        }

        private void Warn(string msg) =>
            MessageBox.Show(msg, "Validacion", MessageBoxButtons.OK, MessageBoxIcon.Warning);

        private void L(Control parent, string t, int y) =>
            parent.Controls.Add(new Label { Text = t, Location = new Point(12, y + 3),
                AutoSize = true, Font = new Font("Segoe UI", 9f, FontStyle.Bold) });
        private TextBox Txt(Control parent, int y)
        {
            var tb = new TextBox { Location = new Point(130, y), Size = new Size(195, 24) };
            parent.Controls.Add(tb); return tb;
        }
        private NumericUpDown Num(Control parent, int y, decimal max)
        {
            var n = new NumericUpDown { Location = new Point(130, y), Size = new Size(195, 24),
                Maximum = max, DecimalPlaces = 2 };
            parent.Controls.Add(n); return n;
        }
    }
}
