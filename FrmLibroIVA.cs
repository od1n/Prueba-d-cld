using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace SistemaGestion
{
    /// <summary>
    /// Libro de Ventas y Compras IVA — formato SENIAT Venezuela.
    /// Se abre desde FrmReportes via TabLibroIVA().
    /// </summary>
    public class FrmLibroIVA : FormBase
    {
        // Controles
        private TabControl tabLibros;
        private ComboBox cmbMes, cmbAnio;
        private Button btnConsultar, btnPdfVentas, btnPdfCompras;
        private DataGridView dgvVentas, dgvCompras;

        // Totales labels — Ventas
        private Label lblVBaseImp, lblVIVA, lblVTotal;
        // Totales labels — Compras
        private Label lblCBaseImp, lblCIVA, lblCTotal;

        // Datos
        private List<FilaLibroIVA> _ventas  = new();
        private List<FilaLibroIVA> _compras = new();

        public FrmLibroIVA()
        {
            InitializeComponent();
            // Seleccionar mes actual
            cmbMes.SelectedIndex  = DateTime.Today.Month - 1;
            cmbAnio.SelectedIndex = cmbAnio.Items.IndexOf(DateTime.Today.Year.ToString());
            Consultar();
        }

        private void InitializeComponent()
        {
            this.Text = "Libro de Ventas y Compras IVA — SENIAT";
            this.Size = new Size(980, 640);
            this.StartPosition = FormStartPosition.CenterParent;
            this.MinimumSize = new Size(800, 500);
            this.BackColor = Color.FromArgb(245, 247, 250);
            this.Font = new Font("Segoe UI", 9f);

            // --- Header ---
            var pnlHeader = new Panel
            {
                Dock = DockStyle.Top,
                Height = 50,
                BackColor = Color.FromArgb(140, 20, 20)
            };
            pnlHeader.Controls.Add(new Label
            {
                Text = "  Libro de Ventas y Compras IVA",
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 12f, FontStyle.Bold),
                AutoSize = true,
                Location = new Point(8, 14)
            });

            // --- Toolbar ---
            var pnlToolbar = new Panel
            {
                Dock = DockStyle.Top,
                Height = 44,
                BackColor = Color.FromArgb(235, 237, 240),
                Padding = new Padding(8, 6, 8, 6)
            };

            pnlToolbar.Controls.Add(new Label
            {
                Text = "Mes:",
                AutoSize = true,
                Location = new Point(8, 14),
                ForeColor = Color.FromArgb(44, 62, 80)
            });

            cmbMes = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Width = 110,
                Location = new Point(40, 10)
            };
            string[] meses = { "Enero","Febrero","Marzo","Abril","Mayo","Junio",
                                "Julio","Agosto","Septiembre","Octubre","Noviembre","Diciembre" };
            cmbMes.Items.AddRange(meses);
            pnlToolbar.Controls.Add(cmbMes);

            pnlToolbar.Controls.Add(new Label
            {
                Text = "Año:",
                AutoSize = true,
                Location = new Point(162, 14),
                ForeColor = Color.FromArgb(44, 62, 80)
            });

            cmbAnio = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Width = 80,
                Location = new Point(190, 10)
            };
            for (int y = DateTime.Today.Year; y >= DateTime.Today.Year - 5; y--)
                cmbAnio.Items.Add(y.ToString());
            pnlToolbar.Controls.Add(cmbAnio);

            btnConsultar = new Button
            {
                Text = "Consultar",
                Size = new Size(90, 28),
                Location = new Point(284, 8),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(41, 128, 185),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
                FlatAppearance = { BorderSize = 0 },
                Cursor = Cursors.Hand
            };
            btnConsultar.Click += (s, e) => Consultar();
            pnlToolbar.Controls.Add(btnConsultar);

            btnPdfVentas = new Button
            {
                Text = "PDF Ventas",
                Size = new Size(95, 28),
                Location = new Point(388, 8),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(192, 57, 43),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
                FlatAppearance = { BorderSize = 0 },
                Cursor = Cursors.Hand
            };
            btnPdfVentas.Click += BtnPdfVentas_Click;
            pnlToolbar.Controls.Add(btnPdfVentas);

            btnPdfCompras = new Button
            {
                Text = "PDF Compras",
                Size = new Size(100, 28),
                Location = new Point(494, 8),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(142, 68, 173),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
                FlatAppearance = { BorderSize = 0 },
                Cursor = Cursors.Hand
            };
            btnPdfCompras.Click += BtnPdfCompras_Click;
            pnlToolbar.Controls.Add(btnPdfCompras);

            // --- TabControl ---
            tabLibros = new TabControl
            {
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 9.5f)
            };

            // Tab Ventas
            var tabVentas = new TabPage("Libro de Ventas") { BackColor = Color.FromArgb(245, 247, 250) };
            var pnlVentas = new Panel { Dock = DockStyle.Fill };
            dgvVentas = CrearGrid(Color.FromArgb(140, 20, 20));
            ConfigurarColumnasVentas();
            var pnlTotVentas = CrearPanelTotales(
                out lblVBaseImp, out lblVIVA, out lblVTotal,
                Color.FromArgb(140, 20, 20));
            pnlVentas.Controls.Add(dgvVentas);
            pnlVentas.Controls.Add(pnlTotVentas);
            tabVentas.Controls.Add(pnlVentas);

            // Tab Compras
            var tabCompras = new TabPage("Libro de Compras") { BackColor = Color.FromArgb(245, 247, 250) };
            var pnlCompras = new Panel { Dock = DockStyle.Fill };
            dgvCompras = CrearGrid(Color.FromArgb(100, 50, 160));
            ConfigurarColumnasCompras();
            var pnlTotCompras = CrearPanelTotales(
                out lblCBaseImp, out lblCIVA, out lblCTotal,
                Color.FromArgb(100, 50, 160));
            pnlCompras.Controls.Add(dgvCompras);
            pnlCompras.Controls.Add(pnlTotCompras);
            tabCompras.Controls.Add(pnlCompras);

            tabLibros.TabPages.Add(tabVentas);
            tabLibros.TabPages.Add(tabCompras);

            // --- Footer ---
            var pnlFooter = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 36,
                BackColor = Color.FromArgb(235, 237, 240)
            };
            var btnCerrar = new Button
            {
                Text = "Cerrar",
                Size = new Size(90, 26),
                Location = new Point(870, 5),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(44, 62, 80),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
                FlatAppearance = { BorderSize = 0 },
                DialogResult = DialogResult.OK
            };
            btnCerrar.Click += (s, e) => Close();
            pnlFooter.Controls.Add(btnCerrar);

            this.Controls.Add(tabLibros);
            this.Controls.Add(pnlToolbar);
            this.Controls.Add(pnlHeader);
            this.Controls.Add(pnlFooter);
        }

        // -------------------------------------------------------------------------
        // COLUMNAS
        // -------------------------------------------------------------------------

        private void ConfigurarColumnasVentas()
        {
            dgvVentas.Columns.Clear();
            dgvVentas.Columns.Add(new DataGridViewTextBoxColumn { Name = "colNum",      HeaderText = "N°",            FillWeight = 5 });
            dgvVentas.Columns.Add(new DataGridViewTextBoxColumn { Name = "colFecha",    HeaderText = "Fecha",         FillWeight = 10 });
            dgvVentas.Columns.Add(new DataGridViewTextBoxColumn { Name = "colNumDoc",   HeaderText = "N° Factura",    FillWeight = 12 });
            dgvVentas.Columns.Add(new DataGridViewTextBoxColumn { Name = "colCliente",  HeaderText = "Cliente",       FillWeight = 25 });
            dgvVentas.Columns.Add(new DataGridViewTextBoxColumn { Name = "colFormaPago",HeaderText = "Forma Pago",    FillWeight = 10 });
            dgvVentas.Columns.Add(new DataGridViewTextBoxColumn { Name = "colBaseImp",  HeaderText = "Base Imponible",FillWeight = 13,
                DefaultCellStyle = new DataGridViewCellStyle { Alignment = DataGridViewContentAlignment.MiddleRight, Format = "N2" }});
            dgvVentas.Columns.Add(new DataGridViewTextBoxColumn { Name = "colIVA",      HeaderText = "IVA (USD)",     FillWeight = 13,
                DefaultCellStyle = new DataGridViewCellStyle { Alignment = DataGridViewContentAlignment.MiddleRight, Format = "N2" }});
            dgvVentas.Columns.Add(new DataGridViewTextBoxColumn { Name = "colTotal",    HeaderText = "Total (USD)",   FillWeight = 12,
                DefaultCellStyle = new DataGridViewCellStyle { Alignment = DataGridViewContentAlignment.MiddleRight, Format = "N2" }});
        }

        private void ConfigurarColumnasCompras()
        {
            dgvCompras.Columns.Clear();
            dgvCompras.Columns.Add(new DataGridViewTextBoxColumn { Name = "colNum",      HeaderText = "N°",            FillWeight = 5 });
            dgvCompras.Columns.Add(new DataGridViewTextBoxColumn { Name = "colFecha",    HeaderText = "Fecha",         FillWeight = 10 });
            dgvCompras.Columns.Add(new DataGridViewTextBoxColumn { Name = "colNumDoc",   HeaderText = "N° Compra",     FillWeight = 12 });
            dgvCompras.Columns.Add(new DataGridViewTextBoxColumn { Name = "colProveedor",HeaderText = "Proveedor",     FillWeight = 25 });
            dgvCompras.Columns.Add(new DataGridViewTextBoxColumn { Name = "colFormaPago",HeaderText = "Forma Pago",    FillWeight = 10 });
            dgvCompras.Columns.Add(new DataGridViewTextBoxColumn { Name = "colBaseImp",  HeaderText = "Base Imponible",FillWeight = 13,
                DefaultCellStyle = new DataGridViewCellStyle { Alignment = DataGridViewContentAlignment.MiddleRight, Format = "N2" }});
            dgvCompras.Columns.Add(new DataGridViewTextBoxColumn { Name = "colIVA",      HeaderText = "IVA (USD)",     FillWeight = 13,
                DefaultCellStyle = new DataGridViewCellStyle { Alignment = DataGridViewContentAlignment.MiddleRight, Format = "N2" }});
            dgvCompras.Columns.Add(new DataGridViewTextBoxColumn { Name = "colTotal",    HeaderText = "Total (USD)",   FillWeight = 12,
                DefaultCellStyle = new DataGridViewCellStyle { Alignment = DataGridViewContentAlignment.MiddleRight, Format = "N2" }});
        }

        // -------------------------------------------------------------------------
        // CONSULTAR
        // -------------------------------------------------------------------------

        private void Consultar()
        {
            if (cmbMes.SelectedIndex < 0 || cmbAnio.SelectedIndex < 0) return;

            int mes  = cmbMes.SelectedIndex + 1;
            int anio = int.Parse(cmbAnio.SelectedItem.ToString());

            _ventas  = DatabaseService.ObtenerLibroVentas(mes, anio);
            _compras = DatabaseService.ObtenerLibroCompras(mes, anio);

            CargarGridVentas();
            CargarGridCompras();
        }

        private void CargarGridVentas()
        {
            dgvVentas.Rows.Clear();
            int n = 1;
            foreach (var f in _ventas)
            {
                int row = dgvVentas.Rows.Add();
                dgvVentas.Rows[row].Cells["colNum"].Value       = n++;
                dgvVentas.Rows[row].Cells["colFecha"].Value     = f.Fecha.ToString("dd/MM/yyyy");
                dgvVentas.Rows[row].Cells["colNumDoc"].Value    = f.NumeroDoc;
                dgvVentas.Rows[row].Cells["colCliente"].Value   = f.Nombre;
                dgvVentas.Rows[row].Cells["colFormaPago"].Value = f.FormaPago;
                dgvVentas.Rows[row].Cells["colBaseImp"].Value   = f.BaseImponible;
                dgvVentas.Rows[row].Cells["colIVA"].Value       = f.MontoIVA;
                dgvVentas.Rows[row].Cells["colTotal"].Value     = f.Total;
            }
            ActualizarTotales(_ventas, lblVBaseImp, lblVIVA, lblVTotal);
        }

        private void CargarGridCompras()
        {
            dgvCompras.Rows.Clear();
            int n = 1;
            foreach (var c in _compras)
            {
                int row = dgvCompras.Rows.Add();
                dgvCompras.Rows[row].Cells["colNum"].Value        = n++;
                dgvCompras.Rows[row].Cells["colFecha"].Value      = c.Fecha.ToString("dd/MM/yyyy");
                dgvCompras.Rows[row].Cells["colNumDoc"].Value     = c.NumeroDoc;
                dgvCompras.Rows[row].Cells["colProveedor"].Value  = c.Nombre;
                dgvCompras.Rows[row].Cells["colFormaPago"].Value  = c.FormaPago;
                dgvCompras.Rows[row].Cells["colBaseImp"].Value    = c.BaseImponible;
                dgvCompras.Rows[row].Cells["colIVA"].Value        = c.MontoIVA;
                dgvCompras.Rows[row].Cells["colTotal"].Value      = c.Total;
            }
            ActualizarTotales(_compras, lblCBaseImp, lblCIVA, lblCTotal);
        }

        private void ActualizarTotales(List<FilaLibroIVA> lista,
            Label lblBase, Label lblIva, Label lblTot)
        {
            lblBase.Text = $"$ {lista.Sum(x => x.BaseImponible):N2}";
            lblIva.Text  = $"$ {lista.Sum(x => x.MontoIVA):N2}";
            lblTot.Text  = $"$ {lista.Sum(x => x.Total):N2}";
        }

        // -------------------------------------------------------------------------
        // PDF
        // -------------------------------------------------------------------------

        private void BtnPdfVentas_Click(object sender, EventArgs e)
        {
            if (_ventas.Count == 0)
            {
                MessageBox.Show("No hay datos de ventas para el periodo seleccionado.",
                    "Sin datos", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            ExportarPdf(esVentas: true);
        }

        private void BtnPdfCompras_Click(object sender, EventArgs e)
        {
            if (_compras.Count == 0)
            {
                MessageBox.Show("No hay datos de compras para el periodo seleccionado.",
                    "Sin datos", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            ExportarPdf(esVentas: false);
        }

        private void ExportarPdf(bool esVentas)
        {
            int mes  = cmbMes.SelectedIndex + 1;
            int anio = int.Parse(cmbAnio.SelectedItem.ToString());
            string nombreMes = cmbMes.SelectedItem.ToString();

            try
            {
                var lista = esVentas ? _ventas : _compras;
                var bytes = PdfService.GenerarLibroIVA(lista, esVentas, mes, anio, nombreMes);

                string tipo = esVentas ? "Ventas" : "Compras";
                using var dlg = new SaveFileDialog
                {
                    Filter   = "PDF|*.pdf",
                    FileName = $"LibroIVA_{tipo}_{nombreMes}{anio}.pdf"
                };
                if (dlg.ShowDialog() == DialogResult.OK)
                {
                    System.IO.File.WriteAllBytes(dlg.FileName, bytes);
                    MessageBox.Show("PDF generado correctamente.", "Exito",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = dlg.FileName, UseShellExecute = true
                    });
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al generar PDF: " + ex.Message, "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // -------------------------------------------------------------------------
        // HELPERS UI
        // -------------------------------------------------------------------------

        private DataGridView CrearGrid(Color colorHeader)
        {
            var dgv = new DataGridView
            {
                Dock = DockStyle.Fill,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                ReadOnly = true,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false,
                BackgroundColor = Color.White,
                BorderStyle = BorderStyle.None,
                RowHeadersVisible = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                Font = new Font("Segoe UI", 9f)
            };
            dgv.EnableHeadersVisualStyles = false;
            dgv.ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
            {
                BackColor = colorHeader,
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                Alignment = DataGridViewContentAlignment.MiddleLeft
            };
            dgv.ColumnHeadersHeight = 30;
            dgv.RowTemplate.Height = 24;
            dgv.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(249, 249, 252);
            dgv.DefaultCellStyle = new DataGridViewCellStyle
            {
                SelectionBackColor = Color.FromArgb(41, 128, 185),
                SelectionForeColor = Color.White
            };
            dgv.GridColor = Color.FromArgb(220, 225, 230);
            return dgv;
        }

        private Panel CrearPanelTotales(
            out Label lblBase, out Label lblIva, out Label lblTot,
            Color color)
        {
            var pnl = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 36,
                BackColor = color
            };

            var lBase = new Label { Text = "Base Imponible:", ForeColor = Color.FromArgb(200,220,255), AutoSize = true, Location = new Point(10, 10), Font = new Font("Segoe UI", 8.5f) };
            var vBase = new Label { Text = "$ 0.00", ForeColor = Color.White, AutoSize = true, Location = new Point(120, 10), Font = new Font("Segoe UI", 8.5f, FontStyle.Bold) };

            var lIva = new Label { Text = "IVA:", ForeColor = Color.FromArgb(200,220,255), AutoSize = true, Location = new Point(270, 10), Font = new Font("Segoe UI", 8.5f) };
            var vIva = new Label { Text = "$ 0.00", ForeColor = Color.White, AutoSize = true, Location = new Point(300, 10), Font = new Font("Segoe UI", 8.5f, FontStyle.Bold) };

            var lTot = new Label { Text = "TOTAL:", ForeColor = Color.FromArgb(200,220,255), AutoSize = true, Location = new Point(450, 10), Font = new Font("Segoe UI", 8.5f) };
            var vTot = new Label { Text = "$ 0.00", ForeColor = Color.White, AutoSize = true, Location = new Point(500, 10), Font = new Font("Segoe UI", 10f, FontStyle.Bold) };

            pnl.Controls.AddRange(new Control[] { lBase, vBase, lIva, vIva, lTot, vTot });

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
            _btnHlp.Click += (s, e) => AyudaService.Mostrar(this, "FrmLibroIVA");
            Controls.AddRange(new Control[] { lBase, vBase, lIva, vIva, lTot, vTot });

            lblBase = vBase;
            lblIva  = vIva;
            lblTot  = vTot;
            return pnl;
        }
    }
}
