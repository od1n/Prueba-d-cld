// ════════════════════════════════════════════════════════════════════════
// ARCHIVO: FrmRegistroProduccionDia.cs
// Cambios v25:
//   · Columna "Defect." en el grid → CantDefectuosas (no va al kardex)
//   · Campo "Vueltas" + botón "Calcular x V." → auto-rellena Primera/Reciclado
//   · Botón "Imprimir Planilla" → genera PDF imprimible con moldes activos
// ════════════════════════════════════════════════════════════════════════

using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace SistemaGestion
{
    public class FrmRegistroProduccionDia : FormBase
    {
        private DateTimePicker  dtpFecha;
        private RadioButton     rbVirgen, rbReciclado;
        private TextBox         txtObservacion;
        private Label           lblResumen;
        private DataGridView    dgvMoldes;
        private ComboBox[]      cmbMaterial  = new ComboBox[3];
        private NumericUpDown[] nudMaterial  = new NumericUpDown[3];
        // Task B: vueltas
        private NumericUpDown   _nudVueltas;

        private RegistroProduccionDia _registro;
        private List<TbMolde>         _moldes;
        private List<Inventario>      _inventario;
        private Dictionary<string, MoldeProductos> _moldeProds = new();

        public FrmRegistroProduccionDia()
        {
            Text          = "Registro de Produccion Diaria";
            Size          = new Size(1280, 720);
            MinimumSize   = new Size(960, 600);
            StartPosition = FormStartPosition.CenterParent;
            BackColor     = Color.FromArgb(240, 244, 250);

            _moldes     = DatabaseService.ObtenerMoldes();
            _inventario = DatabaseService.ObtenerInventario();
            foreach (var m in _moldes)
            {
                var mp = DatabaseService.ObtenerMoldeProductos(m.CodigoMold);
                if (mp != null) _moldeProds[m.CodigoMold] = mp;
            }

            VentanaPrefs.Restaurar(this);
            BuildUI();
            CargarFecha(DateTime.Today);

            var _tp = new System.Windows.Forms.Timer { Interval = 800 };
            _tp.Tick        += (s, e) => { _tp.Stop(); VentanaPrefs.Guardar(this); };
            SizeChanged     += (s, e) => { _tp.Stop(); _tp.Start(); };
            LocationChanged += (s, e) => { _tp.Stop(); _tp.Start(); };
            FormClosed      += (s, e) => { _tp.Stop(); _tp.Dispose(); VentanaPrefs.Guardar(this); };
        }

        private void BuildUI()
        {
            // ── Header ────────────────────────────────────────────────────
            var hdr = new Panel { Dock = DockStyle.Top, Height = 50,
                BackColor = Color.FromArgb(26, 74, 138) };
            hdr.Controls.Add(new Label {
                Text = "Registro de Produccion Diaria",
                Font = new Font("Segoe UI", 13f, FontStyle.Bold),
                ForeColor = Color.White, AutoSize = true, Location = new Point(14, 13) });

            var pnlHdrRight = new FlowLayoutPanel {
                Dock = DockStyle.Right, AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false, BackColor = Color.Transparent,
                Padding = new Padding(0, 10, 8, 0)
            };

            var _tip = AyudaService.CrearTooltip();

            // Task C: botón Imprimir Planilla
            var btnPlanilla = MkBtn("Imprimir Planilla", Color.FromArgb(80, 50, 150), 130);
            btnPlanilla.Click += (s, e) => ImprimirPlanilla();
            AyudaService.Tip(_tip, btnPlanilla,
                "Genera una planilla PDF imprimible con los moldes del día");

            var btnAyuda = new Button {
                Text = "? Ayuda", Size = new Size(90, 30),
                BackColor = Color.FromArgb(80, 50, 150), ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
                Cursor = Cursors.Hand, Margin = new Padding(0, 0, 4, 0)
            };
            btnAyuda.FlatAppearance.BorderSize = 0;
            btnAyuda.Click += (s, e) => AyudaService.Mostrar(this, "FrmRegistroProduccionDia");

            var btnCerrar = MkBtn("Cerrar", Color.FromArgb(100, 100, 120), 90);
            btnCerrar.Click += (s, e) => Close();

            pnlHdrRight.Controls.AddRange(new Control[] { btnPlanilla, btnAyuda, btnCerrar });
            hdr.Controls.Add(pnlHdrRight);

            // ── Barra superior ────────────────────────────────────────────
            var barTop = new Panel { Dock = DockStyle.Top, Height = 52,
                BackColor = Color.FromArgb(232, 240, 250) };

            barTop.Controls.Add(Lbl("Fecha:", 8, 16));
            dtpFecha = new DateTimePicker { Location = new Point(58, 12),
                Size = new Size(120, 26), Format = DateTimePickerFormat.Short,
                Value = DateTime.Today };
            dtpFecha.ValueChanged += (s, e) => CargarFecha(dtpFecha.Value);
            barTop.Controls.Add(dtpFecha);

            barTop.Controls.Add(Lbl("Material:", 192, 16));
            rbVirgen = new RadioButton { Text = "Virgen",
                Location = new Point(252, 14), AutoSize = true,
                Font = new Font("Segoe UI", 10f, FontStyle.Bold),
                ForeColor = Color.FromArgb(20, 100, 60), Checked = true };
            rbReciclado = new RadioButton { Text = "Reciclado",
                Location = new Point(330, 14), AutoSize = true,
                Font = new Font("Segoe UI", 10f, FontStyle.Bold),
                ForeColor = Color.FromArgb(140, 60, 10) };
            rbVirgen.CheckedChanged    += (s, e) => { if (rbVirgen.Checked)    ActualizarColumnas(); };
            rbReciclado.CheckedChanged += (s, e) => { if (rbReciclado.Checked) ActualizarColumnas(); };
            barTop.Controls.AddRange(new Control[] { rbVirgen, rbReciclado });

            // Task B: Vueltas
            barTop.Controls.Add(Lbl("Vueltas:", 418, 16));
            _nudVueltas = new NumericUpDown { Location = new Point(478, 14),
                Size = new Size(60, 26), Minimum = 0, Maximum = 9999, DecimalPlaces = 0 };
            barTop.Controls.Add(_nudVueltas);

            var btnCalcVueltas = new Button {
                Text = "Calc x V.", Location = new Point(544, 13),
                Size = new Size(76, 26),
                BackColor = Color.FromArgb(40, 110, 70), ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 8.5f, FontStyle.Bold), Cursor = Cursors.Hand };
            btnCalcVueltas.FlatAppearance.BorderSize = 0;
            btnCalcVueltas.Click += (s, e) => CalcularPorVueltas();
            AyudaService.Tip(_tip, btnCalcVueltas,
                "Rellena las cantidades de cada molde según las vueltas del ciclo de producción");
            barTop.Controls.Add(btnCalcVueltas);

            barTop.Controls.Add(Lbl("Obs.:", 630, 16));
            txtObservacion = new TextBox { Location = new Point(668, 13), Size = new Size(220, 26) };
            barTop.Controls.Add(txtObservacion);

            var btnGuardar = MkBtn("Guardar y Confirmar Merma",
                Color.FromArgb(26, 74, 138), 190);
            btnGuardar.Dock = DockStyle.Right;
            btnGuardar.Click += (s, e) => Guardar();
            AyudaService.Tip(_tip, btnGuardar,
                "Confirmar y guardar la producción del día — actualiza Kardex A y B");
            barTop.Controls.Add(btnGuardar);

            // ── Resumen ───────────────────────────────────────────────────
            lblResumen = new Label { Dock = DockStyle.Top, Height = 28,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                ForeColor = Color.FromArgb(20, 80, 40),
                BackColor = Color.FromArgb(240, 244, 250),
                TextAlign = System.Drawing.ContentAlignment.MiddleLeft,
                Padding   = new Padding(10, 0, 0, 0) };

            // ── Split ─────────────────────────────────────────────────────
            var split = new SplitContainer {
                Dock = DockStyle.Fill, Orientation = Orientation.Vertical,
                BackColor = Color.FromArgb(220, 230, 245) };

            Load += (s, e) => {
                int dist = (int)(split.Width * 0.73);
                if (dist > split.Panel1MinSize && dist < split.Width - split.Panel2MinSize)
                    split.SplitterDistance = dist;
            };

            // ── Izquierda: grid moldes ────────────────────────────────────
            var pnlMoldes = new Panel { Dock = DockStyle.Fill, BackColor = Color.White };

            var hdrM = new Panel { Dock = DockStyle.Top, Height = 36,
                BackColor = Color.FromArgb(26, 74, 138) };
            hdrM.Controls.Add(new Label { Text = "Produccion por Molde",
                ForeColor = Color.White, Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                AutoSize = true, Location = new Point(8, 9) });
            var bAgr  = MkBtn("+ Molde", Color.FromArgb(40, 130, 80), 80);
            var bQuit = MkBtn("Quitar",  Color.FromArgb(180, 50, 50),  70);
            AyudaService.Tip(_tip, bAgr,  "Agregar una fila para registrar un molde que produjo hoy");
            AyudaService.Tip(_tip, bQuit, "Quitar la fila de molde seleccionada");
            bAgr.Click  += (s, e) => AgregarFilaMolde();
            bQuit.Click += (s, e) => QuitarFilaMolde();

            var pnlHdrMRight = new FlowLayoutPanel {
                Dock = DockStyle.Right, AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false, BackColor = Color.Transparent,
                Padding = new Padding(0, 4, 6, 0)
            };
            pnlHdrMRight.Controls.AddRange(new Control[] { bAgr, bQuit });
            hdrM.Controls.Add(pnlHdrMRight);

            dgvMoldes = new DataGridView {
                Dock = DockStyle.Fill, AllowUserToAddRows = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                BackgroundColor = Color.White, BorderStyle = BorderStyle.None,
                RowHeadersVisible = false, Font = new Font("Segoe UI", 9f),
                EditMode = DataGridViewEditMode.EditOnEnter,
                RowTemplate = { Height = 26 }
            };
            dgvMoldes.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(26, 74, 138);
            dgvMoldes.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
            dgvMoldes.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 9f, FontStyle.Bold);
            dgvMoldes.EnableHeadersVisualStyles = false;
            dgvMoldes.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(245, 248, 255);

            var cMolde = new DataGridViewComboBoxColumn {
                Name = "Molde", HeaderText = "Molde",
                DisplayStyle = DataGridViewComboBoxDisplayStyle.DropDownButton,
                FillWeight = 32 };
            cMolde.Items.Add("");
            _moldes.ForEach(m => cMolde.Items.Add(m.CodigoMold + " — " + m.Descripcion));

            var cPrimera   = new DataGridViewTextBoxColumn { Name="Primera",   HeaderText="Primera",   FillWeight=13 };
            var cSegunda   = new DataGridViewTextBoxColumn { Name="Segunda",   HeaderText="Segunda",   FillWeight=13 };
            var cReciclado = new DataGridViewTextBoxColumn { Name="Reciclado", HeaderText="Reciclado", FillWeight=13, Visible=false };
            // Task A: columna Defectuosas
            var cDefect    = new DataGridViewTextBoxColumn {
                Name="Defect", HeaderText="Defect.", FillWeight=11,
                DefaultCellStyle = { ForeColor = Color.FromArgb(160, 50, 30) } };
            var cInfo      = new DataGridViewTextBoxColumn { Name="ProdInfo",  HeaderText="Productos", FillWeight=30, ReadOnly=true };

            dgvMoldes.Columns.AddRange(new DataGridViewColumn[] {
                cMolde, cPrimera, cSegunda, cReciclado, cDefect, cInfo });

            dgvMoldes.CurrentCellDirtyStateChanged += (s, e) => {
                if (dgvMoldes.IsCurrentCellDirty)
                    dgvMoldes.CommitEdit(DataGridViewDataErrorContexts.Commit);
            };
            dgvMoldes.CellValueChanged += (s, e) => {
                if (e.RowIndex < 0) return;
                if (dgvMoldes.Columns[e.ColumnIndex].Name == "Molde")
                {
                    string val = dgvMoldes.Rows[e.RowIndex].Cells["Molde"].Value?.ToString() ?? "";
                    if (!string.IsNullOrWhiteSpace(val))
                    {
                        string cod = val.Split('—')[0].Trim();
                        dgvMoldes.Rows[e.RowIndex].Cells["ProdInfo"].Value =
                            ObtenerInfoProductos(cod);
                    }
                }
                ActualizarResumen();
            };
            dgvMoldes.CellValidating += (s, e) => {
                string col = dgvMoldes.Columns[e.ColumnIndex].Name;
                if (col != "Primera" && col != "Segunda" &&
                    col != "Reciclado" && col != "Defect") return;
                if (!decimal.TryParse(e.FormattedValue?.ToString(), out decimal v) || v < 0)
                {
                    e.Cancel = true;
                    MessageBox.Show("Ingresa una cantidad válida (≥ 0).",
                        "Validacion", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            };
            dgvMoldes.CellEndEdit += (s, e) => ActualizarResumen();

            pnlMoldes.Controls.Add(dgvMoldes);
            pnlMoldes.Controls.Add(hdrM);

            // ── Derecha: materiales ───────────────────────────────────────
            var pnlDer = new Panel { Dock = DockStyle.Fill, BackColor = Color.White };
            var hdrMat = new Panel { Dock = DockStyle.Top, Height = 36,
                BackColor = Color.FromArgb(100, 60, 20) };
            hdrMat.Controls.Add(new Label { Text = "Material Utilizado (mín. 1)",
                ForeColor = Color.White, Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                AutoSize = true, Location = new Point(8, 9) });

            var pnlMat = new Panel { Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(255, 252, 248), Padding = new Padding(10) };

            int my = 10;
            for (int i = 0; i < 3; i++)
            {
                pnlMat.Controls.Add(new Label {
                    Text = $"Insumo {i+1}{(i==0?" *":"")}:",
                    Location = new Point(10, my + 3), AutoSize = true,
                    Font = new Font("Segoe UI", 9f,
                        i == 0 ? FontStyle.Bold : FontStyle.Regular) });

                cmbMaterial[i] = new ComboBox { Location = new Point(78, my),
                    Size = new Size(200, 26), DropDownStyle = ComboBoxStyle.DropDownList };
                cmbMaterial[i].Items.Add("-- Sin seleccionar --");
                DatabaseService.ObtenerProductosProveedor(soloActivos: true)
                    .ForEach(ins =>
                        cmbMaterial[i].Items.Add(ins.CodigoInterno + " — " + ins.Descripcion));
                cmbMaterial[i].SelectedIndex = 0;
                cmbMaterial[i].SelectedIndexChanged += (s, e) => ActualizarResumen();
                pnlMat.Controls.Add(cmbMaterial[i]);

                nudMaterial[i] = new NumericUpDown { Location = new Point(284, my),
                    Size = new Size(80, 26), DecimalPlaces = 2,
                    Minimum = 0, Maximum = 99999, ThousandsSeparator = true };
                nudMaterial[i].ValueChanged += (s, e) => ActualizarResumen();
                pnlMat.Controls.Add(nudMaterial[i]);

                pnlMat.Controls.Add(new Label { Text = "kg",
                    Location = new Point(368, my + 3), AutoSize = true });
                my += 44;
            }

            // Nota sobre sacos
            pnlMat.Controls.Add(new Label {
                Text = "1 saco = 25 kg",
                Location = new Point(10, my + 6), AutoSize = true,
                Font = new Font("Segoe UI", 8f, FontStyle.Italic),
                ForeColor = Color.FromArgb(120, 80, 40) });

            pnlDer.Controls.Add(pnlMat);
            pnlDer.Controls.Add(hdrMat);

            split.Panel1.Controls.Add(pnlMoldes);
            split.Panel2.Controls.Add(pnlDer);

            var footer = new Panel { Dock = DockStyle.Bottom, Height = 28,
                BackColor = Color.FromArgb(240, 244, 250) };
            footer.Controls.Add(new Label {
                Text = "  Tip: Configura los productos de cada molde en Tabla de Moldes → Productos Molde  |  " +
                       "Defect. = piezas rechazadas (no van al inventario)",
                AutoSize = true, Location = new Point(8, 6),
                Font = new Font("Segoe UI", 8f), ForeColor = Color.FromArgb(40, 80, 50) });

            Controls.AddRange(new Control[] { split, footer, lblResumen, barTop, hdr });
        }

        private bool EsVirgen => rbVirgen.Checked;

        // ── Task B: calcular cantidades por vueltas ───────────────────────
        private void CalcularPorVueltas()
        {
            decimal vueltas = _nudVueltas.Value;
            if (vueltas <= 0)
            {
                Warn("Ingresa al menos 1 vuelta.");
                return;
            }
            foreach (DataGridViewRow row in dgvMoldes.Rows)
            {
                if (row.IsNewRow) continue;
                if (string.IsNullOrWhiteSpace(row.Cells["Molde"].Value?.ToString())) continue;

                if (EsVirgen)
                {
                    row.Cells["Primera"].Value   = vueltas.ToString("N0");
                    row.Cells["Segunda"].Value   = "0";
                    row.Cells["Reciclado"].Value = "0";
                }
                else
                {
                    row.Cells["Primera"].Value   = "0";
                    row.Cells["Segunda"].Value   = "0";
                    row.Cells["Reciclado"].Value = vueltas.ToString("N0");
                }
            }
            ActualizarResumen();
        }

        private void ActualizarColumnas()
        {
            if (dgvMoldes.Columns.Contains("Primera"))
                dgvMoldes.Columns["Primera"].Visible   = EsVirgen;
            if (dgvMoldes.Columns.Contains("Segunda"))
                dgvMoldes.Columns["Segunda"].Visible   = EsVirgen;
            if (dgvMoldes.Columns.Contains("Reciclado"))
                dgvMoldes.Columns["Reciclado"].Visible = !EsVirgen;

            foreach (DataGridViewRow row in dgvMoldes.Rows)
            {
                if (row.IsNewRow) continue;
                string val = row.Cells["Molde"].Value?.ToString() ?? "";
                if (string.IsNullOrWhiteSpace(val)) continue;
                string cod = val.Split('—')[0].Trim();
                row.Cells["ProdInfo"].Value = ObtenerInfoProductos(cod);
            }
            ActualizarResumen();
        }

        private void CargarFecha(DateTime fecha)
        {
            _registro = DatabaseService.ObtenerRegistroProduccion(fecha)
                        ?? new RegistroProduccionDia { Fecha = fecha, TipoMaterial = "Virgen" };

            txtObservacion.Text = _registro.Observacion ?? "";
            rbVirgen.Checked    = _registro.TipoMaterial != "Reciclado";
            rbReciclado.Checked = _registro.TipoMaterial == "Reciclado";

            dgvMoldes.Rows.Clear();
            foreach (var m in _registro.Moldes)
            {
                string item = m.CodigoMolde + " — " + m.DescMolde;
                var cmbCol = (DataGridViewComboBoxColumn)dgvMoldes.Columns["Molde"];
                if (!cmbCol.Items.Contains(item)) cmbCol.Items.Add(item);
                dgvMoldes.Rows.Add(item,
                    m.CantPrimera.ToString("N2"),
                    m.CantSegunda.ToString("N2"),
                    m.CantReciclado.ToString("N2"),
                    m.CantDefectuosas.ToString("N2"),  // Task A
                    ObtenerInfoProductos(m.CodigoMolde));
            }

            for (int i = 0; i < 3; i++)
            {
                cmbMaterial[i].SelectedIndex = 0;
                nudMaterial[i].Value = 0;
            }
            for (int i = 0; i < Math.Min(3, _registro.Materiales.Count); i++)
            {
                var mat = _registro.Materiales[i];
                for (int j = 0; j < cmbMaterial[i].Items.Count; j++)
                    if (cmbMaterial[i].Items[j].ToString().StartsWith(mat.CodigoInsumo + " —"))
                    { cmbMaterial[i].SelectedIndex = j; break; }
                nudMaterial[i].Value = mat.Cantidad;
            }

            ActualizarColumnas();
        }

        private void AgregarFilaMolde()
        {
            dgvMoldes.Rows.Add("", "0", "0", "0", "0", "");  // Task A: +defect
            dgvMoldes.CurrentCell =
                dgvMoldes.Rows[dgvMoldes.Rows.Count - 1].Cells["Molde"];
        }

        private void QuitarFilaMolde()
        {
            if (dgvMoldes.CurrentRow != null && !dgvMoldes.CurrentRow.IsNewRow)
            { dgvMoldes.Rows.RemoveAt(dgvMoldes.CurrentRow.Index); ActualizarResumen(); }
        }

        private string ObtenerInfoProductos(string codMolde)
        {
            if (!_moldeProds.ContainsKey(codMolde)) return "⚠ sin configurar";
            var mp = _moldeProds[codMolde];
            return EsVirgen
                ? $"1a:{mp.CodPrimera ?? "-"}  2a:{mp.CodSegunda ?? "-"}"
                : $"Rec:{mp.CodReciclado ?? "-"}";
        }

        private decimal ObtenerPeso(string cod)
        {
            if (string.IsNullOrWhiteSpace(cod)) return 0;
            return _inventario.FirstOrDefault(i => i.Codigo == cod)?.PesoKg ?? 0;
        }

        private void ActualizarResumen()
        {
            decimal p1 = 0, p2 = 0, pr = 0, pDef = 0, pesoTeorico = 0;
            foreach (DataGridViewRow row in dgvMoldes.Rows)
            {
                if (row.IsNewRow) continue;
                string val = row.Cells["Molde"].Value?.ToString() ?? "";
                if (string.IsNullOrWhiteSpace(val)) continue;
                string cod = val.Split('—')[0].Trim();

                decimal.TryParse(row.Cells["Primera"].Value?.ToString(),   out decimal rp1);
                decimal.TryParse(row.Cells["Segunda"].Value?.ToString(),   out decimal rp2);
                decimal.TryParse(row.Cells["Reciclado"].Value?.ToString(), out decimal rpr);
                decimal.TryParse(row.Cells["Defect"].Value?.ToString(),    out decimal rdef);  // Task A
                p1 += rp1; p2 += rp2; pr += rpr; pDef += rdef;

                if (_moldeProds.ContainsKey(cod))
                {
                    var mp = _moldeProds[cod];
                    if (EsVirgen)
                    {
                        pesoTeorico += ObtenerPeso(mp.CodPrimera) * rp1;
                        pesoTeorico += ObtenerPeso(mp.CodSegunda) * rp2;
                    }
                    else
                        pesoTeorico += ObtenerPeso(mp.CodReciclado) * rpr;
                }
            }

            decimal matIng = Enumerable.Range(0, 3)
                .Where(i => cmbMaterial[i].SelectedIndex > 0)
                .Sum(i => nudMaterial[i].Value);

            decimal merma  = matIng - pesoTeorico;
            string  pctStr = matIng > 0 ? $" ({merma/matIng*100:N1}%)" : "";
            string  defStr = pDef > 0   ? $"   Defect.: {pDef:N0}" : "";   // Task A

            lblResumen.Text = EsVirgen
                ? $"  Primera: {p1:N0}   Segunda: {p2:N0}{defStr}   |   " +
                  $"Peso teórico: {pesoTeorico:N2} kg   " +
                  $"Material: {matIng:N2} kg   Merma est.: {merma:N2} kg{pctStr}"
                : $"  Reciclado: {pr:N0}{defStr}   |   " +
                  $"Peso teórico: {pesoTeorico:N2} kg   " +
                  $"Material: {matIng:N2} kg   Merma est.: {merma:N2} kg{pctStr}";

            lblResumen.ForeColor = merma < 0
                ? Color.FromArgb(180, 50, 50)
                : Color.FromArgb(20, 80, 40);
        }

        private void Guardar()
        {
            bool tieneMoldes = dgvMoldes.Rows.Cast<DataGridViewRow>()
                .Any(r => !r.IsNewRow &&
                     !string.IsNullOrWhiteSpace(r.Cells["Molde"].Value?.ToString()));
            if (!tieneMoldes)
            { Warn("Agrega al menos un molde."); return; }

            bool tieneMat = Enumerable.Range(0, 3)
                .Any(i => cmbMaterial[i].SelectedIndex > 0 && nudMaterial[i].Value > 0);
            if (!tieneMat)
            { Warn("Ingresa al menos un material con cantidad mayor a 0."); return; }

            _registro.Fecha        = dtpFecha.Value;
            _registro.TipoMaterial = EsVirgen ? "Virgen" : "Reciclado";
            _registro.Observacion  = txtObservacion.Text.Trim();
            _registro.Moldes.Clear();
            _registro.Materiales.Clear();

            foreach (DataGridViewRow row in dgvMoldes.Rows)
            {
                if (row.IsNewRow) continue;
                string val = row.Cells["Molde"].Value?.ToString() ?? "";
                if (string.IsNullOrWhiteSpace(val)) continue;
                string codMolde = val.Split('—')[0].Trim();

                if (!_moldeProds.ContainsKey(codMolde))
                {
                    Warn($"El molde '{codMolde}' no tiene productos configurados.\n" +
                         "Configúralo en Tabla de Moldes → Productos Molde.");
                    return;
                }

                decimal.TryParse(row.Cells["Primera"].Value?.ToString(),   out decimal rp1);
                decimal.TryParse(row.Cells["Segunda"].Value?.ToString(),   out decimal rp2);
                decimal.TryParse(row.Cells["Reciclado"].Value?.ToString(), out decimal rpr);
                decimal.TryParse(row.Cells["Defect"].Value?.ToString(),    out decimal rdef);  // Task A

                // Solo agrega la fila si hay producción buena (defectuosas solas no cuentan)
                if (rp1 + rp2 + rpr <= 0 && rdef <= 0) continue;

                var mp    = _moldeProds[codMolde];
                var molde = _moldes.FirstOrDefault(m => m.CodigoMold == codMolde);
                _registro.Moldes.Add(new DetalleProduccionMolde {
                    CodigoMolde     = codMolde,
                    DescMolde       = molde?.Descripcion ?? codMolde,
                    CodigoProducto  = EsVirgen ? (mp.CodPrimera ?? "") : (mp.CodReciclado ?? ""),
                    CantPrimera     = EsVirgen  ? rp1 : 0,
                    CantSegunda     = EsVirgen  ? rp2 : 0,
                    CantReciclado   = !EsVirgen ? rpr : 0,
                    CantDefectuosas = rdef        // Task A — registrado, nunca al kardex
                });
            }

            for (int i = 0; i < 3; i++)
            {
                if (cmbMaterial[i].SelectedIndex <= 0 || nudMaterial[i].Value <= 0) continue;
                string cod = cmbMaterial[i].SelectedItem.ToString().Split('—')[0].Trim();
                var inv    = _inventario.FirstOrDefault(x => x.Codigo == cod);
                _registro.Materiales.Add(new MaterialProduccionDia {
                    CodigoInsumo = cod,
                    DescInsumo   = inv?.Descripcion ?? cod,
                    Cantidad     = nudMaterial[i].Value
                });
            }

            try
            {
                var resumen = DatabaseService.GuardarRegistroProduccion(_registro);
                using var dlg = new DlgConfirmarMerma(resumen);
                if (dlg.ShowDialog() == DialogResult.OK)
                    DatabaseService.RegistrarMermaProduccion(resumen);

                MessageBox.Show(
                    $"Produccion del {_registro.Fecha:dd/MM/yyyy} guardada correctamente.",
                    "Guardado", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al guardar:\n" + ex.Message,
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // ── Task C: Imprimir planilla ─────────────────────────────────────
        private void ImprimirPlanilla()
        {
            // Recopilar moldes activos en el grid
            var moldesEnGrid = new List<string>();
            foreach (DataGridViewRow row in dgvMoldes.Rows)
            {
                if (row.IsNewRow) continue;
                string val = row.Cells["Molde"].Value?.ToString() ?? "";
                if (!string.IsNullOrWhiteSpace(val))
                    moldesEnGrid.Add(val.Split('—')[0].Trim());
            }

            using var dlg = new DlgSeleccionarMoldesPlanilla(_moldes, moldesEnGrid);
            if (dlg.ShowDialog() != DialogResult.OK) return;

            var seleccionados = dlg.MoldesSeleccionados;
            if (seleccionados.Count == 0)
            { Warn("Selecciona al menos un molde para la planilla."); return; }

            try
            {
                byte[] pdf = PdfService.GenerarPlanillaProduccion(
                    dtpFecha.Value,
                    seleccionados,
                    (int)_nudVueltas.Value,
                    EsVirgen ? "Virgen" : "Reciclado");

                string nombre = $"Planilla_Prod_{dtpFecha.Value:yyyyMMdd}.pdf";
                string ruta   = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.Desktop), nombre);
                File.WriteAllBytes(ruta, pdf);

                var r = MessageBox.Show(
                    $"Planilla guardada en:\n{ruta}\n\n¿Abrir ahora?",
                    "Planilla lista", MessageBoxButtons.YesNo, MessageBoxIcon.Information);
                if (r == DialogResult.Yes)
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(ruta)
                        { UseShellExecute = true });
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al generar la planilla:\n" + ex.Message,
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void Warn(string msg) =>
            MessageBox.Show(msg, "Validacion", MessageBoxButtons.OK, MessageBoxIcon.Warning);

        private Button MkBtn(string t, Color bg, Point loc, int w) => new Button {
            Text = t, Location = loc, Size = new Size(w, 28),
            BackColor = bg, ForeColor = Color.White, FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 8.5f, FontStyle.Bold), Cursor = Cursors.Hand
        };
        private Button MkBtn(string t, Color bg, int w) => new Button {
            Text = t, Size = new Size(w, 30),
            BackColor = bg, ForeColor = Color.White, FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 8.5f, FontStyle.Bold), Cursor = Cursors.Hand,
            Margin = new Padding(0, 0, 4, 0)
        };
        private Label Lbl(string t, int x, int y) =>
            new Label { Text = t, Location = new Point(x, y), AutoSize = true,
                Font = new Font("Segoe UI", 9f) };
    }

    // ════════════════════════════════════════════════════════════════════════
    //  Task C: DIALOGO SELECCIÓN DE MOLDES PARA LA PLANILLA
    // ════════════════════════════════════════════════════════════════════════
    public class DlgSeleccionarMoldesPlanilla : Form
    {
        public List<TbMolde> MoldesSeleccionados { get; private set; } = new();
        private readonly CheckedListBox _clb;
        private readonly List<TbMolde> _todos;

        public DlgSeleccionarMoldesPlanilla(List<TbMolde> todos, List<string> preseleccionados)
        {
            _todos = todos;
            Text            = "Seleccionar moldes para la planilla";
            MinimumSize     = new Size(440, 380);
            Size            = new Size(480, 500);
            StartPosition   = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.Sizable;   // redimensionable
            MaximizeBox     = false;
            BackColor       = Color.FromArgb(245, 248, 255);

            var hdr = new Panel { Dock = DockStyle.Top, Height = 44,
                BackColor = Color.FromArgb(26, 74, 138) };
            hdr.Controls.Add(new Label {
                Text = "Moldes a incluir en la planilla",
                ForeColor = Color.White, AutoSize = true,
                Font = new Font("Segoe UI", 10f, FontStyle.Bold),
                Location = new Point(12, 12) });

            _clb = new CheckedListBox {
                Dock        = DockStyle.Fill,
                Font        = new Font("Segoe UI", 10f),
                CheckOnClick = true,
                BorderStyle  = BorderStyle.None,
                BackColor    = Color.FromArgb(245, 248, 255),
                ItemHeight   = 26 };

            foreach (var m in todos)
            {
                bool pre = preseleccionados.Contains(m.CodigoMold);
                _clb.Items.Add($"{m.CodigoMold}  —  {m.Descripcion}", pre);
            }

            // Footer con FlowLayoutPanel — se adapta al ancho sin recortes
            var footer = new Panel { Dock = DockStyle.Bottom, Height = 56,
                BackColor = Color.FromArgb(232, 240, 250),
                Padding = new Padding(8, 10, 8, 10) };

            var flow = new FlowLayoutPanel {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents  = false,
                AutoSize      = false };

            var btnTodos = new Button {
                Text = "Todos", Size = new Size(82, 32),
                FlatStyle = FlatStyle.Flat, Margin = new Padding(0, 0, 4, 0) };
            btnTodos.Click += (s, e) => {
                for (int i = 0; i < _clb.Items.Count; i++) _clb.SetItemChecked(i, true); };

            var btnNinguno = new Button {
                Text = "Ninguno", Size = new Size(82, 32),
                FlatStyle = FlatStyle.Flat, Margin = new Padding(0, 0, 0, 0) };
            btnNinguno.Click += (s, e) => {
                for (int i = 0; i < _clb.Items.Count; i++) _clb.SetItemChecked(i, false); };

            // Spacer empuja los botones de acción a la derecha
            var spacer = new Panel { Size = new Size(1, 1), Margin = new Padding(0) };
            flow.Controls.Add(spacer);
            flow.SetFlowBreak(spacer, false);

            var btnOk = new Button {
                Text = "Generar PDF",
                Size = new Size(110, 32),
                DialogResult = DialogResult.OK,
                BackColor = Color.FromArgb(26, 74, 138), ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                Margin = new Padding(0, 0, 6, 0) };
            btnOk.FlatAppearance.BorderSize = 0;
            btnOk.Click += (s, e) => {
                MoldesSeleccionados = _clb.CheckedIndices.Cast<int>()
                    .Select(i => _todos[i]).ToList();
            };

            var btnCancelar = new Button {
                Text = "Cancelar",
                Size = new Size(90, 32),
                DialogResult = DialogResult.Cancel,
                FlatStyle = FlatStyle.Flat,
                Margin = new Padding(0) };

            flow.Controls.AddRange(new Control[] { btnTodos, btnNinguno, btnOk, btnCancelar });

            // Alinear btnOk y btnCancelar a la derecha usando Anchor del flow
            flow.Layout += (s, e) => {
                int anchoIzq = btnTodos.Width + btnNinguno.Width + 4;
                int anchoDer = btnOk.Width + btnCancelar.Width + 6;
                spacer.Width = Math.Max(1, flow.Width - anchoIzq - anchoDer - 8);
            };

            footer.Controls.Add(flow);
            AcceptButton = btnOk; CancelButton = btnCancelar;
            Controls.AddRange(new Control[] { _clb, footer, hdr });
        }
    }

    // ════════════════════════════════════════════════════════════════════════
    //  DIALOGO CONFIRMACION DE MERMA (sin cambios)
    // ════════════════════════════════════════════════════════════════════════
    public class DlgConfirmarMerma : Form
    {
        public DlgConfirmarMerma(ResumenMermaProduccion r)
        {
            Text = "Confirmar Merma";
            Size = new Size(520, 480);
            StartPosition   = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = MinimizeBox = false;
            BackColor   = Color.FromArgb(248, 245, 252);

            var hdr = new Panel { Dock = DockStyle.Top, Height = 44,
                BackColor = Color.FromArgb(100, 60, 20) };
            hdr.Controls.Add(new Label {
                Text = $"Merma — {r.Fecha:dd/MM/yyyy}  ({r.TipoMaterial})",
                ForeColor = Color.White, AutoSize = true,
                Font = new Font("Segoe UI", 10f, FontStyle.Bold),
                Location = new Point(10, 12) });

            var rtb = new RichTextBox {
                Dock = DockStyle.Fill, ReadOnly = true,
                BorderStyle = BorderStyle.None,
                BackColor   = Color.FromArgb(252, 250, 248),
                Font        = new Font("Consolas", 9.5f),
                Padding     = new Padding(8) };

            rtb.AppendText("MATERIALES CONSUMIDOS\r\n");
            rtb.AppendText(new string('─', 50) + "\r\n");
            foreach (var (ins, kg) in r.DetalleInsumos)
                rtb.AppendText($"  {ins,-30} {kg,8:N2} kg\r\n");
            rtb.AppendText($"  {"TOTAL",-30} {r.MaterialConsumido,8:N2} kg\r\n\r\n");

            rtb.AppendText("PRODUCCION (PESO TEORICO)\r\n");
            rtb.AppendText(new string('─', 50) + "\r\n");
            foreach (var (prod, cant, peso, sub) in r.DetalleProductos)
                rtb.AppendText(
                    $"  {prod,-20} {cant,6:N0} u × {peso,5:N3} kg = {sub,7:N2} kg\r\n");
            rtb.AppendText($"  {"TOTAL TEORICO",-30} {r.PesoTeorico,8:N2} kg\r\n\r\n");

            rtb.AppendText(new string('═', 50) + "\r\n");
            string color = r.MermaKg < 0 ? "⚠ " : "";
            rtb.AppendText(
                $"  {color}MERMA{"",-25} {r.MermaKg,8:N2} kg  ({r.PctMerma:N1}%)\r\n");
            if (r.MermaKg < 0)
                rtb.AppendText(
                    "\r\n  ⚠ Merma negativa: el peso teórico supera el material\r\n" +
                    "  registrado. Verifica los pesos en Inventario.\r\n");

            var footer = new Panel { Dock = DockStyle.Bottom, Height = 56,
                BackColor = Color.FromArgb(240, 235, 248) };
            bool hayMerma = r.MermaKg > 0;

            footer.Controls.Add(new Label {
                Text = hayMerma
                    ? "¿Registrar esta merma?"
                    : r.MermaKg < 0
                        ? "Verifica los pesos antes de continuar."
                        : "Sin merma que registrar.",
                Location = new Point(10, 10), AutoSize = true,
                Font = new Font("Segoe UI", 9f) });

            if (hayMerma)
            {
                var bSi = new Button {
                    Text = "Sí, registrar merma",
                    Location = new Point(100, 18), Size = new Size(155, 30),
                    DialogResult = DialogResult.OK,
                    BackColor = Color.FromArgb(100, 60, 20), ForeColor = Color.White,
                    FlatStyle = FlatStyle.Flat,
                    Font = new Font("Segoe UI", 9f, FontStyle.Bold) };
                bSi.FlatAppearance.BorderSize = 0;
                var bNo = new Button {
                    Text = "No registrar",
                    Location = new Point(265, 18), Size = new Size(120, 30),
                    DialogResult = DialogResult.Cancel,
                    BackColor = Color.FromArgb(100, 100, 120), ForeColor = Color.White,
                    FlatStyle = FlatStyle.Flat };
                bNo.FlatAppearance.BorderSize = 0;
                footer.Controls.AddRange(new Control[] { bSi, bNo });
                AcceptButton = bSi; CancelButton = bNo;
            }
            else
            {
                var bOk = new Button {
                    Text = "Aceptar", Location = new Point(190, 18),
                    Size = new Size(120, 30), DialogResult = DialogResult.Cancel,
                    BackColor = Color.FromArgb(60, 100, 60), ForeColor = Color.White,
                    FlatStyle = FlatStyle.Flat };
                bOk.FlatAppearance.BorderSize = 0;
                footer.Controls.Add(bOk); CancelButton = bOk;
            }

            Controls.AddRange(new Control[] { rtb, footer, hdr });
        }
    }
}
