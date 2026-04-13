using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace SistemaGestion
{
    // ══════════════════════════════════════════════════════════════════════════
    //  KARDEX - HISTORIAL DE MOVIMIENTOS DE INVENTARIO
    // ══════════════════════════════════════════════════════════════════════════
    public class FrmKardex : Form
    {
        private DataGridView dgv;
        private Label lblResumen;
        private ComboBox cmbProducto;
        private DateTimePicker dtpDesde, dtpHasta;
        private Panel panelFiltros;

        public FrmKardex()
        {
            Text = "Kardex - Movimientos de Inventario";
            Size = new Size(1100, 620);
            StartPosition = FormStartPosition.CenterParent;
            BackColor = Color.FromArgb(245, 250, 245);

            // ── Header ───────────────────────────────────────────────────────
            var hdr = new Panel { Dock = DockStyle.Top, Height = 50, BackColor = Color.FromArgb(20, 90, 50) };
            hdr.Controls.Add(new Label
            {
                Text = "Kardex  —  Movimientos de Inventario",
                ForeColor = Color.White, Font = new Font("Segoe UI", 12f, FontStyle.Bold),
                AutoSize = true, Location = new Point(12, 13)
            });

            var btnEntrada   = Btn("+ Entrada",   Color.FromArgb(30, 140, 70),  new Point(640, 8));
            var btnSalida    = Btn("- Salida",    Color.FromArgb(180, 60, 60),  new Point(738, 8));
            var btnAjuste    = Btn("Ajuste",      Color.FromArgb(60, 100, 180), new Point(836, 8));
            var btnExportar  = Btn("Exportar",    Color.FromArgb(100, 100, 120),new Point(934, 8));
            hdr.Controls.AddRange(new Control[] { btnEntrada, btnSalida, btnAjuste, btnExportar });

            // ── Panel de filtros ─────────────────────────────────────────────
            panelFiltros = new Panel { Dock = DockStyle.Top, Height = 42, BackColor = Color.FromArgb(225, 245, 230) };

            panelFiltros.Controls.Add(Lbl("Producto:", 10, 12));
            cmbProducto = new ComboBox
            {
                Location = new Point(80, 9), Size = new Size(250, 24),
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            cmbProducto.Items.Add("-- Todos --");
            foreach (var inv in DatabaseService.ObtenerInventario())
                cmbProducto.Items.Add($"{inv.Codigo} - {inv.Descripcion}");
            cmbProducto.SelectedIndex = 0;
            panelFiltros.Controls.Add(cmbProducto);

            panelFiltros.Controls.Add(Lbl("Desde:", 345, 12));
            dtpDesde = new DateTimePicker
            {
                Location = new Point(395, 9), Size = new Size(130, 24),
                Format = DateTimePickerFormat.Short,
                Value = DateTime.Today.AddMonths(-1)
            };
            panelFiltros.Controls.Add(dtpDesde);

            panelFiltros.Controls.Add(Lbl("Hasta:", 540, 12));
            dtpHasta = new DateTimePicker
            {
                Location = new Point(590, 9), Size = new Size(130, 24),
                Format = DateTimePickerFormat.Short,
                Value = DateTime.Today
            };
            panelFiltros.Controls.Add(dtpHasta);

            var btnFiltrar = new Button
            {
                Text = "Filtrar", Location = new Point(740, 8), Size = new Size(90, 26),
                BackColor = Color.FromArgb(20, 90, 50), ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 9f, FontStyle.Bold)
            };
            btnFiltrar.FlatAppearance.BorderSize = 0;
            btnFiltrar.Click += (s, e) => CargarDatos();
            panelFiltros.Controls.Add(btnFiltrar);

            var btnLimpiar = new Button
            {
                Text = "Limpiar", Location = new Point(840, 8), Size = new Size(80, 26),
                BackColor = Color.FromArgb(100, 100, 120), ForeColor = Color.White, FlatStyle = FlatStyle.Flat
            };
            btnLimpiar.FlatAppearance.BorderSize = 0;
            btnLimpiar.Click += (s, e) =>
            {
                cmbProducto.SelectedIndex = 0;
                dtpDesde.Value = DateTime.Today.AddMonths(-1);
                dtpHasta.Value = DateTime.Today;
                CargarDatos();
            };
            panelFiltros.Controls.Add(btnLimpiar);

            // ── Grid ─────────────────────────────────────────────────────────
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
            dgv.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(20, 90, 50);
            dgv.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
            dgv.ColumnHeadersDefaultCellStyle.Font      = new Font("Segoe UI", 9f, FontStyle.Bold);
            dgv.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(235, 250, 240);
            dgv.EnableHeadersVisualStyles = false;
            dgv.DataBindingComplete += (s, e) => ColorearFilas();

            // ── Footer ───────────────────────────────────────────────────────
            var footer = new Panel { Dock = DockStyle.Bottom, Height = 45, BackColor = Color.FromArgb(210, 235, 218) };
            lblResumen = new Label
            {
                AutoSize = true, Location = new Point(12, 12),
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                ForeColor = Color.FromArgb(15, 70, 35)
            };
            var btnCerrar = new Button
            {
                Text = "Cerrar", Size = new Size(90, 28), Location = new Point(985, 8),
                BackColor = Color.FromArgb(100, 100, 120), ForeColor = Color.White, FlatStyle = FlatStyle.Flat
            };
            btnCerrar.FlatAppearance.BorderSize = 0;
            btnCerrar.Click += (s, e) => Close();
            footer.Controls.AddRange(new Control[] { lblResumen, btnCerrar });

            Controls.AddRange(new Control[] { dgv, panelFiltros, hdr, footer });

            // ── Eventos ──────────────────────────────────────────────────────
            btnEntrada.Click  += (s, e) => AbrirMovimiento("ENTRADA");
            btnSalida.Click   += (s, e) => AbrirMovimiento("SALIDA");
            btnAjuste.Click   += (s, e) => AbrirAjuste();
            btnExportar.Click += (s, e) => ExportarCSV();

            CargarDatos();
        }

        // ── Carga y filtrado ─────────────────────────────────────────────────
        private void CargarDatos()
        {
            string codFiltro = null;
            if (cmbProducto.SelectedIndex > 0)
            {
                var sel = cmbProducto.SelectedItem.ToString();
                codFiltro = sel.Split('-')[0].Trim();
            }

            var lista = DatabaseService.ObtenerKardex(codFiltro, dtpDesde.Value, dtpHasta.Value);

            dgv.DataSource = null;
            dgv.DataSource = lista.Select(m => new
            {
                m.IdMovimiento,
                FechaHora      = m.FechaHora.ToString("dd/MM/yyyy HH:mm"),
                m.CodigoProducto,
                Tipo           = m.TipoMovimiento,
                m.Concepto,
                Cantidad       = m.Cantidad.ToString("N2"),
                SaldoAnterior  = m.SaldoAnterior.ToString("N2"),
                NuevoSaldo     = m.NuevoSaldo.ToString("N2"),
                m.Referencia,
                m.Usuario
            }).ToList();

            // Resumen
            decimal totalEntradas = lista.Where(m => m.TipoMovimiento == "ENTRADA").Sum(m => m.Cantidad);
            decimal totalSalidas  = lista.Where(m => m.TipoMovimiento == "SALIDA").Sum(m => m.Cantidad);
            lblResumen.Text = $"  Registros: {lista.Count}   |   " +
                              $"Total ENTRADAS: {totalEntradas:N2}   |   " +
                              $"Total SALIDAS: {totalSalidas:N2}   |   " +
                              $"Neto: {(totalEntradas - totalSalidas):N2}";
        }

        private void ColorearFilas()
        {
            foreach (DataGridViewRow row in dgv.Rows)
            {
                var tipo = row.Cells["Tipo"].Value?.ToString();
                if (tipo == "ENTRADA")
                    row.DefaultCellStyle.ForeColor = Color.FromArgb(20, 120, 50);
                else if (tipo == "SALIDA")
                    row.DefaultCellStyle.ForeColor = Color.FromArgb(170, 40, 40);

                // Resaltar ajustes
                var concepto = row.Cells["Concepto"].Value?.ToString() ?? "";
                if (concepto.StartsWith("AJUSTE"))
                    row.DefaultCellStyle.BackColor = Color.FromArgb(255, 250, 210);
            }
        }

        // ── Registro de movimiento manual ────────────────────────────────────
        private void AbrirMovimiento(string tipo)
        {
            using var dlg = new DlgMovimiento(tipo);
            if (dlg.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    DatabaseService.RegistrarMovimiento(
                        dlg.CodigoProducto,
                        tipo,
                        dlg.Cantidad,
                        dlg.Concepto,
                        dlg.Referencia,
                        "Admin");

                    CargarDatos();
                    // Actualizar ComboBox de productos
                    ActualizarComboProductos();
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, "Error al registrar movimiento",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void AbrirAjuste()
        {
            using var dlg = new DlgAjuste();
            if (dlg.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    DatabaseService.RegistrarCorreccion(
                        dlg.CodigoProducto,
                        dlg.NuevoStock,
                        dlg.Motivo,
                        "Admin");

                    CargarDatos();
                    ActualizarComboProductos();
                    MessageBox.Show("Ajuste registrado correctamente.", "Exito",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, "Error en ajuste",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void ActualizarComboProductos()
        {
            var selIndex = cmbProducto.SelectedIndex;
            cmbProducto.Items.Clear();
            cmbProducto.Items.Add("-- Todos --");
            foreach (var inv in DatabaseService.ObtenerInventario())
                cmbProducto.Items.Add($"{inv.Codigo} - {inv.Descripcion}");
            cmbProducto.SelectedIndex = Math.Min(selIndex, cmbProducto.Items.Count - 1);
        }

        private void ExportarCSV()
        {
            var dlg = new SaveFileDialog
            {
                Title = "Exportar Kardex",
                Filter = "CSV (*.csv)|*.csv",
                FileName = $"Kardex_{DateTime.Today:yyyyMMdd}.csv"
            };
            if (dlg.ShowDialog() != DialogResult.OK) return;

            try
            {
                var lista = DatabaseService.ObtenerKardex();
                var lines = new System.Collections.Generic.List<string>
                {
                    "ID,FechaHora,Producto,Tipo,Concepto,Cantidad,SaldoAnterior,NuevoSaldo,Referencia,Usuario"
                };
                foreach (var m in lista)
                    lines.Add($"{m.IdMovimiento},{m.FechaHora:dd/MM/yyyy HH:mm},{m.CodigoProducto}," +
                              $"{m.TipoMovimiento},{m.Concepto},{m.Cantidad},{m.SaldoAnterior}," +
                              $"{m.NuevoSaldo},{m.Referencia},{m.Usuario}");

                System.IO.File.WriteAllLines(dlg.FileName, lines, System.Text.Encoding.UTF8);
                MessageBox.Show($"Exportado correctamente:\n{dlg.FileName}", "Exportar",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error al exportar", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // ── Helpers UI ───────────────────────────────────────────────────────
        private Button Btn(string t, Color c, Point p)
        {
            var b = new Button
            {
                Text = t, Size = new Size(90, 33), Location = p, BackColor = c,
                ForeColor = Color.White, FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 8.5f, FontStyle.Bold), Cursor = Cursors.Hand
            };
            b.FlatAppearance.BorderSize = 0;
            return b;
        }

        private Label Lbl(string t, int x, int y) =>
            new Label { Text = t, Location = new Point(x, y), AutoSize = true, Font = new Font("Segoe UI", 9f) };
    }

    // ══════════════════════════════════════════════════════════════════════════
    //  DIÁLOGO ENTRADA / SALIDA
    // ══════════════════════════════════════════════════════════════════════════
    public class DlgMovimiento : Form
    {
        public string  CodigoProducto { get; private set; }
        public decimal Cantidad       { get; private set; }
        public string  Concepto       { get; private set; }
        public string  Referencia     { get; private set; }

        private ComboBox cmbProducto;
        private NumericUpDown numCantidad;
        private TextBox txtConcepto, txtReferencia;
        private Label lblStockActual;

        public DlgMovimiento(string tipo)
        {
            bool esEntrada = tipo == "ENTRADA";
            Text = esEntrada ? "Registrar Entrada" : "Registrar Salida";
            Size = new Size(440, 340);
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = MinimizeBox = false;
            BackColor = esEntrada ? Color.FromArgb(240, 255, 244) : Color.FromArgb(255, 242, 242);

            Color colorAccent = esEntrada ? Color.FromArgb(30, 140, 70) : Color.FromArgb(180, 50, 50);

            // Banner
            var banner = new Panel { Dock = DockStyle.Top, Height = 40, BackColor = colorAccent };
            banner.Controls.Add(new Label
            {
                Text = esEntrada ? "▲  ENTRADA de Stock" : "▼  SALIDA de Stock",
                ForeColor = Color.White, Font = new Font("Segoe UI", 11f, FontStyle.Bold),
                AutoSize = true, Location = new Point(12, 10)
            });

            int y = 55;
            L("Producto:", y);
            cmbProducto = new ComboBox
            {
                Location = new Point(150, y), Size = new Size(255, 24),
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            foreach (var inv in DatabaseService.ObtenerInventario())
                cmbProducto.Items.Add($"{inv.Codigo} — {inv.Descripcion}");
            if (cmbProducto.Items.Count > 0) cmbProducto.SelectedIndex = 0;
            cmbProducto.SelectedIndexChanged += (s, e) => MostrarStock();
            Controls.Add(cmbProducto); y += 34;

            L("Stock actual:", y);
            lblStockActual = new Label
            {
                Location = new Point(150, y), AutoSize = true,
                Font = new Font("Segoe UI", 10f, FontStyle.Bold),
                ForeColor = Color.FromArgb(40, 100, 40)
            };
            Controls.Add(lblStockActual); y += 34;

            L("Cantidad:", y);
            numCantidad = new NumericUpDown
            {
                Location = new Point(150, y), Size = new Size(255, 24),
                Minimum = 1, Maximum = 999999, Value = 1, DecimalPlaces = 2
            };
            Controls.Add(numCantidad); y += 34;

            L("Concepto:", y);
            txtConcepto = new TextBox { Location = new Point(150, y), Size = new Size(255, 24) };
            Controls.Add(txtConcepto); y += 34;

            L("Referencia:", y);
            txtReferencia = new TextBox
            {
                Location = new Point(150, y), Size = new Size(255, 24),
                PlaceholderText = "Nro. Factura, Orden, etc."
            };
            Controls.Add(txtReferencia); y += 45;

            var bOk = new Button
            {
                Text = esEntrada ? "Registrar Entrada" : "Registrar Salida",
                Location = new Point(60, y), Size = new Size(160, 34),
                BackColor = colorAccent, ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 9f, FontStyle.Bold)
            };
            bOk.FlatAppearance.BorderSize = 0;

            var bCan = new Button
            {
                Text = "Cancelar", DialogResult = DialogResult.Cancel,
                Location = new Point(240, y), Size = new Size(120, 34),
                BackColor = Color.FromArgb(120, 120, 130), ForeColor = Color.White, FlatStyle = FlatStyle.Flat
            };
            bCan.FlatAppearance.BorderSize = 0;

            bOk.Click += (s, e) =>
            {
                if (cmbProducto.SelectedIndex < 0)
                {
                    MessageBox.Show("Seleccione un producto.", "Validacion",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning); return;
                }
                if (string.IsNullOrWhiteSpace(txtConcepto.Text))
                {
                    MessageBox.Show("Ingrese el concepto del movimiento.", "Validacion",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning); return;
                }
                CodigoProducto = cmbProducto.SelectedItem.ToString().Split('—')[0].Trim();
                Cantidad       = numCantidad.Value;
                Concepto       = txtConcepto.Text.Trim();
                Referencia     = txtReferencia.Text.Trim();
                DialogResult   = DialogResult.OK;
            };

            Controls.AddRange(new Control[] { banner, bOk, bCan });
            MostrarStock();
        }

        private void MostrarStock()
        {
            if (cmbProducto.SelectedIndex < 0) return;
            var cod = cmbProducto.SelectedItem.ToString().Split('—')[0].Trim();
            var item = DatabaseService.ObtenerInventario().Find(x => x.Codigo == cod);
            if (item == null) return;
            lblStockActual.Text = $"{item.Existencia:N2} {item.Unidad}";
            lblStockActual.ForeColor = item.Existencia <= item.ExistenMin
                ? Color.FromArgb(180, 50, 50)
                : Color.FromArgb(30, 120, 50);
        }

        private void L(string t, int y) =>
            Controls.Add(new Label
            {
                Text = t, Location = new Point(15, y + 3), AutoSize = true,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold)
            });
    }

    // ══════════════════════════════════════════════════════════════════════════
    //  DIÁLOGO AJUSTE / CORRECCIÓN
    // ══════════════════════════════════════════════════════════════════════════
    public class DlgAjuste : Form
    {
        public string  CodigoProducto { get; private set; }
        public decimal NuevoStock     { get; private set; }
        public string  Motivo         { get; private set; }

        public DlgAjuste()
        {
            Text = "Ajuste de Inventario";
            Size = new Size(420, 300);
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = MinimizeBox = false;
            BackColor = Color.FromArgb(248, 248, 240);

            var banner = new Panel { Dock = DockStyle.Top, Height = 40, BackColor = Color.FromArgb(60, 100, 180) };
            banner.Controls.Add(new Label
            {
                Text = "⚖  Ajuste / Corrección de Stock",
                ForeColor = Color.White, Font = new Font("Segoe UI", 11f, FontStyle.Bold),
                AutoSize = true, Location = new Point(12, 10)
            });

            int y = 55;
            L("Producto:", y);
            var cmbProducto = new ComboBox
            {
                Location = new Point(160, y), Size = new Size(230, 24),
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            foreach (var inv in DatabaseService.ObtenerInventario())
                cmbProducto.Items.Add($"{inv.Codigo} — {inv.Descripcion}");
            if (cmbProducto.Items.Count > 0) cmbProducto.SelectedIndex = 0;
            Controls.Add(cmbProducto); y += 34;

            L("Stock real actual:", y);
            var lblStock = new Label { Location = new Point(160, y), AutoSize = true, Font = new Font("Segoe UI", 9f, FontStyle.Italic), ForeColor = Color.Gray };
            Controls.Add(lblStock);
            cmbProducto.SelectedIndexChanged += (s, e) =>
            {
                var cod = cmbProducto.SelectedItem.ToString().Split('—')[0].Trim();
                var item = DatabaseService.ObtenerInventario().Find(x => x.Codigo == cod);
                lblStock.Text = item != null ? $"(Sistema: {item.Existencia:N2})" : "";
            };
            if (cmbProducto.Items.Count > 0) lblStock.Text = "(cargando...)"; y += 30;

            L("Nuevo stock real:", y);
            var numNuevo = new NumericUpDown
            {
                Location = new Point(160, y), Size = new Size(230, 24),
                Maximum = 9999999, DecimalPlaces = 2
            };
            Controls.Add(numNuevo); y += 34;

            L("Motivo:", y);
            var txtMotivo = new TextBox { Location = new Point(160, y), Size = new Size(230, 24) };
            Controls.Add(txtMotivo); y += 45;

            var bOk = new Button
            {
                Text = "Aplicar Ajuste", Location = new Point(60, y), Size = new Size(140, 33),
                BackColor = Color.FromArgb(60, 100, 180), ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 9f, FontStyle.Bold)
            };
            bOk.FlatAppearance.BorderSize = 0;
            var bCan = new Button
            {
                Text = "Cancelar", DialogResult = DialogResult.Cancel,
                Location = new Point(220, y), Size = new Size(110, 33),
                BackColor = Color.FromArgb(120, 120, 130), ForeColor = Color.White, FlatStyle = FlatStyle.Flat
            };
            bCan.FlatAppearance.BorderSize = 0;

            bOk.Click += (s, e) =>
            {
                if (cmbProducto.SelectedIndex < 0) return;
                if (string.IsNullOrWhiteSpace(txtMotivo.Text))
                {
                    MessageBox.Show("Ingrese el motivo del ajuste.", "Validacion",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning); return;
                }
                CodigoProducto = cmbProducto.SelectedItem.ToString().Split('—')[0].Trim();
                NuevoStock     = numNuevo.Value;
                Motivo         = txtMotivo.Text.Trim();
                DialogResult   = DialogResult.OK;
            };

            Controls.AddRange(new Control[] { banner, bOk, bCan });
        }

        private void L(string t, int y) =>
            Controls.Add(new Label
            {
                Text = t, Location = new Point(15, y + 3), AutoSize = true,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold)
            });
    }
}
