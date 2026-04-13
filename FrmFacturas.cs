using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace SistemaGestion
{
    // ══════════════════════════════════════════════════════════════════════════
    //  LISTA DE FACTURAS
    // ══════════════════════════════════════════════════════════════════════════
    public class FrmFacturas : Form
    {
        private DataGridView dgv;
        private Label        lblResumen;
        private List<Factura> _facturas = new List<Factura>();
        private string _columnaOrden = "Fecha";
        private bool _ordenAsc = false;

        public FrmFacturas()
        {
            Text          = "Facturas";
            Size          = new Size(1130, 560);
            StartPosition = FormStartPosition.CenterParent;
            BackColor     = Color.FromArgb(245, 247, 255);

            var hdr = new Panel { Dock = DockStyle.Top, Height = 50, BackColor = Color.FromArgb(60, 100, 180) };
            hdr.Controls.Add(new Label
            {
                Text = "Facturas", ForeColor = Color.White,
                Font = new Font("Segoe UI", 12f, FontStyle.Bold),
                AutoSize = true, Location = new Point(12, 13)
            });

            var btnNueva    = Btn("+ Nueva",     Color.FromArgb(40, 160, 80),  new Point(490, 8));
            var btnVer      = Btn("Ver detalle", Color.FromArgb(60, 130, 190), new Point(588, 8), 100);
            var btnPdf      = Btn("Exportar PDF",Color.FromArgb(180, 60, 30),  new Point(697, 8), 105);
            var btnImprimir = Btn("Imprimir",    Color.FromArgb(100, 60, 160), new Point(810, 8));
            var btnReporte  = Btn("Reporte PDF", Color.FromArgb(20, 110, 80),  new Point(908, 8), 100);
            var btnCancelar = Btn("Cancelar",    Color.FromArgb(180, 60, 60),  new Point(1016, 8));
            hdr.Controls.AddRange(new Control[] { btnNueva, btnVer, btnPdf, btnImprimir, btnReporte, btnCancelar });

            dgv = Grid(Color.FromArgb(60, 100, 180));
            dgv.DataBindingComplete += (s, e) => ColorearFilas();
            dgv.DoubleClick         += (s, e) => VerDetalle();
            dgv.ColumnHeaderMouseClick += (s, e) => OrdenarPorColumna(e.ColumnIndex);

            var footer = new Panel { Dock = DockStyle.Bottom, Height = 45, BackColor = Color.FromArgb(215, 220, 240) };
            lblResumen = new Label
            {
                AutoSize  = true, Location = new Point(12, 12),
                Font      = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                ForeColor = Color.FromArgb(40, 50, 120)
            };
            var btnCerrar = BtnFooter("Cerrar", footer, new Point(900, 8));
            btnCerrar.Click += (s, e) => Close();
            footer.Controls.Add(lblResumen);

            Controls.AddRange(new Control[] { dgv, hdr, footer });

            btnNueva.Click    += (s, e) => NuevaFactura();
            btnVer.Click      += (s, e) => VerDetalle();
            btnPdf.Click      += (s, e) => ExportarPdf();
            btnImprimir.Click += (s, e) => ImprimirFactura();
            btnReporte.Click  += (s, e) => ExportarReporte();
            btnCancelar.Click += (s, e) => CancelarFactura();

            CargarDatos();
        }

        private void CargarDatos()
        {
            _facturas = DatabaseService.ObtenerFacturas();

            IEnumerable<Factura> ordenadas = _facturas;
            switch (_columnaOrden)
            {
                case "NumeroF":
                    ordenadas = _ordenAsc
                        ? ordenadas
                            .OrderBy(f => long.TryParse(f.NumeroF, out _) ? 0 : 1)
                            .ThenBy(f => long.TryParse(f.NumeroF, out var n) ? n : long.MaxValue)
                            .ThenBy(f => f.NumeroF)
                        : ordenadas
                            .OrderBy(f => long.TryParse(f.NumeroF, out _) ? 0 : 1)
                            .ThenByDescending(f => long.TryParse(f.NumeroF, out var n) ? n : long.MinValue)
                            .ThenByDescending(f => f.NumeroF);
                    break;
                case "Cliente":
                    ordenadas = _ordenAsc
                        ? ordenadas
                            .OrderBy(f => long.TryParse(f.Codigo, out _) ? 0 : 1)
                            .ThenBy(f => long.TryParse(f.Codigo, out var n) ? n : long.MaxValue)
                            .ThenBy(f => f.Codigo)
                        : ordenadas
                            .OrderBy(f => long.TryParse(f.Codigo, out _) ? 0 : 1)
                            .ThenByDescending(f => long.TryParse(f.Codigo, out var n) ? n : long.MinValue)
                            .ThenByDescending(f => f.Codigo);
                    break;
                case "Fecha":
                    ordenadas = _ordenAsc
                        ? ordenadas.OrderBy(f => f.Fecha)
                        : ordenadas.OrderByDescending(f => f.Fecha);
                    break;
                case "Total":
                    ordenadas = _ordenAsc
                        ? ordenadas.OrderBy(f => f.TotalF)
                        : ordenadas.OrderByDescending(f => f.TotalF);
                    break;
            }

            dgv.DataSource = null;
            dgv.DataSource = ordenadas.Select(f => new
            {
                f.NumeroF,
                Cliente    = f.Codigo,
                Fecha      = f.Fecha.ToShortDateString(),
                FormaPago  = f.FormaDePago,
                Desc       = f.Descuento + "%",
                IVA        = f.IvaF.ToString("C2"),
                Total      = f.TotalF.ToString("C2"),
                Abono      = f.Abono < 0 ? "CANCELADA" : f.Abono.ToString("C2"),
                Saldo      = f.Abono < 0 ? "---" : (f.TotalF - f.Abono).ToString("C2"),
                Estado     = f.Abono < 0 ? "Cancelada"
                           : (f.TotalF - f.Abono) <= 0 ? "Saldada" : "Pendiente"
            }).ToList();

            foreach (DataGridViewColumn col in dgv.Columns)
                col.SortMode = DataGridViewColumnSortMode.Programmatic;
            AplicarGlyphOrden();

            decimal total   = _facturas.Where(f => f.Abono >= 0).Sum(f => f.TotalF);
            decimal cobrado = _facturas.Where(f => f.Abono >= 0).Sum(f => f.Abono);
            lblResumen.Text =
                "  Facturas: " + _facturas.Count +
                "   |   Total: " + total.ToString("C2") +
                "   |   Cobrado: " + cobrado.ToString("C2") +
                "   |   Saldo: " + (total - cobrado).ToString("C2");
        }

        private void OrdenarPorColumna(int columnIndex)
        {
            if (columnIndex < 0 || columnIndex >= dgv.Columns.Count) return;
            var nombre = dgv.Columns[columnIndex].Name;

            if (nombre != "NumeroF" && nombre != "Cliente" && nombre != "Fecha" && nombre != "Total")
                return;

            if (_columnaOrden == nombre)
                _ordenAsc = !_ordenAsc;
            else
            {
                _columnaOrden = nombre;
                _ordenAsc = true;
            }

            CargarDatos();
        }

        private void AplicarGlyphOrden()
        {
            foreach (DataGridViewColumn col in dgv.Columns)
                col.HeaderCell.SortGlyphDirection = SortOrder.None;

            if (dgv.Columns.Contains(_columnaOrden))
                dgv.Columns[_columnaOrden].HeaderCell.SortGlyphDirection =
                    _ordenAsc ? SortOrder.Ascending : SortOrder.Descending;
        }

        private void ColorearFilas()
        {
            foreach (DataGridViewRow row in dgv.Rows)
            {
                var estado = row.Cells["Estado"].Value?.ToString();
                if (estado == "Saldada")   row.DefaultCellStyle.BackColor = Color.FromArgb(225, 255, 230);
                else if (estado == "Cancelada") row.DefaultCellStyle.BackColor = Color.FromArgb(245, 230, 230);
                else                            row.DefaultCellStyle.BackColor = Color.FromArgb(255, 250, 215);
            }
        }

        // ── PDF ───────────────────────────────────────────────────────────────
        private (Factura f, List<LineaFactura> lineas) ObtenerFacturaSeleccionada()
        {
            if (dgv.CurrentRow == null) return (null, null);
            var num = dgv.CurrentRow.Cells["NumeroF"].Value?.ToString();
            var f   = DatabaseService.ObtenerFacturas().Find(x => x.NumeroF == num);
            if (f == null) return (null, null);
            int id  = ObtenerIdFactura(num);
            var lineas = DatabaseService.ObtenerDetallesFactura(id);
            return (f, lineas);
        }

        private void ExportarPdf()
        {
            var (f, lineas) = ObtenerFacturaSeleccionada();
            if (f == null)
            {
                MessageBox.Show("Selecciona una factura primero.", "Aviso",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            try
            {
                var pdf = PdfService.GenerarFactura(f, lineas);
                PdfService.GuardarYAbrir(pdf, "Factura_" + f.NumeroF + ".pdf");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al generar PDF:" + ex.Message, "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ImprimirFactura()
        {
            var (f, lineas) = ObtenerFacturaSeleccionada();
            if (f == null)
            {
                MessageBox.Show("Selecciona una factura primero.", "Aviso",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            try
            {
                var pdf = PdfService.GenerarFactura(f, lineas);
                PdfService.Imprimir(pdf, "Factura_" + f.NumeroF + ".pdf");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al imprimir:" + ex.Message, "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ExportarReporte()
        {
            try
            {
                var lista = DatabaseService.ObtenerFacturas();
                var pdf   = PdfService.GenerarReporteFacturas(lista);
                PdfService.GuardarYAbrir(pdf,
                    "Reporte_Facturas_" + DateTime.Today.ToString("yyyy-MM-dd") + ".pdf");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al generar reporte:" + ex.Message, "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void NuevaFactura()
        {
            using var dlg = new DlgFactura(null);
            if (dlg.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    DatabaseService.InsertarFactura(dlg.Resultado);

                    // Obtener el ID de la factura recien insertada
                    int idFactura = ObtenerIdFactura(dlg.Resultado.NumeroF);

                    DatabaseService.GuardarDetallesYKardex(
                        idFactura,
                        dlg.Resultado.NumeroF,
                        dlg.Lineas,
                        esNueva: true);

                    CargarDatos();
                }
                catch (Exception ex)
                {
                    // Si falla el kardex, revertir la factura tambien
                    DatabaseService.EliminarFactura(dlg.Resultado.NumeroF);
                    MessageBox.Show(
                        "Error al guardar la factura:\n" + ex.Message,
                        "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private int ObtenerIdFactura(string numeroF)
        {
            // La tabla Facturas usa NumeroF como PK de texto.
            // DetallesFactura usa IdFactura INTEGER para referencia interna.
            // Usamos el rowid de SQLite como id interno.
            using var conn = new Microsoft.Data.Sqlite.SqliteConnection(
                SecurityService.CadenaConexion);
            conn.Open();
            var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT rowid FROM Facturas WHERE NumeroF=$n";
            cmd.Parameters.AddWithValue("$n", numeroF);
            var obj = cmd.ExecuteScalar();
            return obj != null ? (int)(long)obj : 0;
        }

        private void VerDetalle()
        {
            if (dgv.CurrentRow == null) return;
            var num = dgv.CurrentRow.Cells["NumeroF"].Value?.ToString();
            var f   = DatabaseService.ObtenerFacturas().Find(x => x.NumeroF == num);
            if (f == null) return;

            int id      = ObtenerIdFactura(num);
            var lineas  = DatabaseService.ObtenerDetallesFactura(id);

            using var dlg = new DlgFactura(f, lineas, soloLectura: true);
            dlg.ShowDialog();
        }

        private void CancelarFactura()
        {
            if (dgv.CurrentRow == null) return;
            var num = dgv.CurrentRow.Cells["NumeroF"].Value?.ToString();
            var f   = DatabaseService.ObtenerFacturas().Find(x => x.NumeroF == num);
            if (f == null || f.Abono < 0) return;

            if (MessageBox.Show(
                "Cancelar factura " + f.NumeroF + "?\n\n" +
                "ATENCION: El stock NO se revertira automaticamente.\n" +
                "Si necesitas devolver el stock usa el Kardex (Entrada).",
                "Confirmar cancelacion",
                MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes)
            {
                f.Abono = -1;
                DatabaseService.ActualizarFactura(f);
                DatabaseService.RegistrarAuditoria("FACTURA_CANCEL", "Facturas", "Cancelada: " + num);
                CargarDatos();
            }
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
            g.ColumnHeadersDefaultCellStyle.Font      = new Font("Segoe UI", 9f, FontStyle.Bold);
            g.EnableHeadersVisualStyles = false;
            return g;
        }

        private Button Btn(string t, Color c, Point p, int w = 90)
        {
            var b = new Button
            {
                Text = t, Size = new Size(w, 33), Location = p, BackColor = c,
                ForeColor = Color.White, FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 8.5f, FontStyle.Bold), Cursor = Cursors.Hand
            };
            b.FlatAppearance.BorderSize = 0;
            return b;
        }

        private Button BtnFooter(string t, Panel p, Point loc)
        {
            var b = new Button
            {
                Text = t, Size = new Size(90, 27), Location = loc,
                BackColor = Color.FromArgb(100, 100, 120), ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            b.FlatAppearance.BorderSize = 0;
            p.Controls.Add(b);
            return b;
        }
    }

    // ══════════════════════════════════════════════════════════════════════════
    //  DIALOGO FACTURA  (cabecera + lineas de productos)
    // ══════════════════════════════════════════════════════════════════════════
    public class DlgFactura : Form
    {
        public Factura                       Resultado   { get; private set; }
        public List<LineaFactura> Lineas { get; private set; }
            = new List<LineaFactura>();

        private class ClienteComboItem
        {
            public string Codigo { get; set; }
            public string Descripcion { get; set; }
            public string Texto => string.IsNullOrWhiteSpace(Descripcion)
                ? Codigo
                : Codigo + " - " + Descripcion;
        }

        // Cabecera
        private TextBox       txtNumero, txtTransporte, txtObs;
        private DateTimePicker dtpFecha;
        private NumericUpDown  numDescuento, numAbono, numIva, numTotal, numDias;
        private ComboBox       cmbCliente, cmbFormaPago;
        private List<Cliente>  _clientesFactura = new List<Cliente>();

        // Grid de lineas
        private DataGridView   dgvLineas;
        private Label          lblTotalLineas;

        private readonly bool _soloLectura;

        public DlgFactura(Factura f,
                          List<LineaFactura> lineas = null,
                          bool soloLectura = false)
        {
            _soloLectura = soloLectura;

            Text            = f == null ? "Nueva Factura"
                : soloLectura ? "Detalle Factura"
                : "Editar Factura " + f.NumeroF;
            Size            = new Size(900, 680);
            StartPosition   = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox     = MinimizeBox = false;
            BackColor       = Color.FromArgb(245, 247, 255);

            // ── Banner ────────────────────────────────────────────────────
            var banner = new Panel { Dock = DockStyle.Top, Height = 40, BackColor = Color.FromArgb(60, 100, 180) };
            banner.Controls.Add(new Label
            {
                Text = Text, ForeColor = Color.White,
                Font = new Font("Segoe UI", 10f, FontStyle.Bold),
                AutoSize = true, Location = new Point(14, 10)
            });

            // ── Panel izquierdo: cabecera ─────────────────────────────────
            var panelIzq = new Panel
            {
                Location = new Point(0, 42),
                Size     = new Size(360, 370),
                BackColor= Color.FromArgb(245, 247, 255)
            };

            int y = 10;
            void Lbl(string t, int yy) => panelIzq.Controls.Add(
                new Label { Text = t, Location = new Point(10, yy + 3), AutoSize = true,
                    Font = new Font("Segoe UI", 9f, FontStyle.Bold) });
            TextBox Txt(int yy, string val = "")
            {
                var tb = new TextBox { Text = val, Location = new Point(130, yy), Size = new Size(220, 24),
                    ReadOnly = soloLectura };
                panelIzq.Controls.Add(tb);
                return tb;
            }
            NumericUpDown Num(int yy, decimal max, decimal val = 0)
            {
                var n = new NumericUpDown { Location = new Point(130, yy), Size = new Size(220, 24),
                    Maximum = max, DecimalPlaces = 2, Value = val,
                    ReadOnly = soloLectura, Enabled = !soloLectura };
                panelIzq.Controls.Add(n);
                return n;
            }

            Lbl("No. Factura:", y);  txtNumero = Txt(y); y += 32;

            Lbl("Cliente (Cod):", y);
            cmbCliente = new ComboBox
            {
                Location = new Point(130, y),
                Size = new Size(220, 24),
                DropDownStyle = soloLectura ? ComboBoxStyle.DropDownList : ComboBoxStyle.DropDown,
                Enabled = !soloLectura
            };

            _clientesFactura = DatabaseService.ObtenerClientes()
                .OrderBy(c => int.TryParse(c.Codigo, out _) ? 0 : 1)
                .ThenBy(c => int.TryParse(c.Codigo, out var n) ? n : int.MaxValue)
                .ThenBy(c => c.Codigo)
                .ToList();

            var clienteItems = _clientesFactura
                .Select(c => new ClienteComboItem
                {
                    Codigo = c.Codigo,
                    Descripcion = c.Descripcion
                })
                .ToList();

            cmbCliente.DataSource = clienteItems;
            cmbCliente.DisplayMember = "Texto";
            cmbCliente.ValueMember = "Codigo";

            if (!soloLectura)
            {
                var ac = new AutoCompleteStringCollection();
                foreach (var c in _clientesFactura)
                {
                    if (!string.IsNullOrWhiteSpace(c.Codigo)) ac.Add(c.Codigo);
                    if (!string.IsNullOrWhiteSpace(c.Descripcion)) ac.Add(c.Descripcion);
                    if (!string.IsNullOrWhiteSpace(c.Codigo) || !string.IsNullOrWhiteSpace(c.Descripcion))
                        ac.Add((c.Codigo + " - " + c.Descripcion).Trim(' ', '-'));
                }
                cmbCliente.AutoCompleteMode = AutoCompleteMode.SuggestAppend;
                cmbCliente.AutoCompleteSource = AutoCompleteSource.CustomSource;
                cmbCliente.AutoCompleteCustomSource = ac;
            }

            panelIzq.Controls.Add(cmbCliente);
            y += 32;

            Lbl("Fecha:", y);
            dtpFecha = new DateTimePicker
            {
                Location = new Point(130, y), Size = new Size(220, 24),
                Format = DateTimePickerFormat.Short, Enabled = !soloLectura
            };
            panelIzq.Controls.Add(dtpFecha); y += 32;

            Lbl("Forma Pago:", y);
            cmbFormaPago = new ComboBox
            {
                Location = new Point(130, y), Size = new Size(220, 24),
                DropDownStyle = ComboBoxStyle.DropDownList, Enabled = !soloLectura
            };
            cmbFormaPago.Items.AddRange(new object[] { "Contado", "Credito", "Cheque", "Transferencia", "Mixto" });
            cmbFormaPago.SelectedIndex = 0;
            panelIzq.Controls.Add(cmbFormaPago); y += 32;

            Lbl("Desc. global (%):", y); numDescuento = Num(y, 100);       y += 32;
            Lbl("IVA ($):", y);          numIva       = Num(y, 9999999);   y += 32;
            Lbl("Abono ($):", y);        numAbono     = Num(y, 9999999);   y += 32;
            Lbl("Total ($):", y);        numTotal     = Num(y, 9999999);   y += 32;
            Lbl("Dias credito:", y);
            numDias = new NumericUpDown
            {
                Location = new Point(130, y), Size = new Size(220, 24),
                Maximum = 365, DecimalPlaces = 0, Enabled = !soloLectura
            };
            panelIzq.Controls.Add(numDias); y += 32;
            Lbl("Transporte:", y);  txtTransporte = Txt(y); y += 32;
            Lbl("Obs:", y);
            txtObs = new TextBox
            {
                Location = new Point(130, y), Size = new Size(220, 40),
                Multiline = true, ReadOnly = soloLectura
            };
            panelIzq.Controls.Add(txtObs);

            // ── Panel derecho: lineas de productos ────────────────────────
            var panelDer = new Panel
            {
                Location  = new Point(362, 42),
                Size      = new Size(520, 370),
                BackColor = Color.FromArgb(238, 242, 255)
            };

            var lblLineas = new Label
            {
                Text     = "Productos / Servicios",
                Location = new Point(8, 8), AutoSize = true,
                Font     = new Font("Segoe UI", 9f, FontStyle.Bold),
                ForeColor= Color.FromArgb(40, 60, 140)
            };

            // Toolbar de lineas
            var btnAgregar = new Button
            {
                Text = "+ Agregar producto", Location = new Point(8, 28), Size = new Size(150, 26),
                BackColor = Color.FromArgb(40, 140, 80), ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 8f, FontStyle.Bold),
                Visible = !soloLectura
            };
            btnAgregar.FlatAppearance.BorderSize = 0;

            var btnQuitarLinea = new Button
            {
                Text = "Quitar", Location = new Point(168, 28), Size = new Size(70, 26),
                BackColor = Color.FromArgb(180, 60, 60), ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 8f, FontStyle.Bold),
                Visible = !soloLectura
            };
            btnQuitarLinea.FlatAppearance.BorderSize = 0;

            // Grid de lineas
            dgvLineas = new DataGridView
            {
                Location  = new Point(8, 62),
                Size      = new Size(504, 250),
                ReadOnly  = false,
                AllowUserToAddRows    = false,
                AllowUserToDeleteRows = false,
                AutoSizeColumnsMode   = DataGridViewAutoSizeColumnsMode.Fill,
                BackgroundColor = Color.White,
                BorderStyle     = BorderStyle.FixedSingle,
                RowHeadersVisible = false,
                Font = new Font("Segoe UI", 9f),
                RowTemplate = { Height = 24 }
            };
            dgvLineas.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(60, 100, 180);
            dgvLineas.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
            dgvLineas.ColumnHeadersDefaultCellStyle.Font      = new Font("Segoe UI", 9f, FontStyle.Bold);
            dgvLineas.EnableHeadersVisualStyles = false;

            // Columnas del grid de lineas
            dgvLineas.Columns.Add(new DataGridViewTextBoxColumn
                { Name = "Codigo", HeaderText = "Codigo", ReadOnly = true, Width = 80 });
            dgvLineas.Columns.Add(new DataGridViewTextBoxColumn
                { Name = "Descripcion", HeaderText = "Descripcion", ReadOnly = true });
            dgvLineas.Columns.Add(new DataGridViewTextBoxColumn
                { Name = "Cantidad", HeaderText = "Cant.", Width = 70 });
            dgvLineas.Columns.Add(new DataGridViewTextBoxColumn
                { Name = "PrecioUnit", HeaderText = "Precio", ReadOnly = false, Width = 80 });
            dgvLineas.Columns.Add(new DataGridViewTextBoxColumn
                { Name = "Subtotal", HeaderText = "Subtotal", ReadOnly = true, Width = 90 });
            dgvLineas.Columns.Add(new DataGridViewTextBoxColumn
                { Name = "Stock", HeaderText = "Stock", ReadOnly = true, Width = 60 });

            if (soloLectura)
                foreach (DataGridViewColumn col in dgvLineas.Columns)
                    col.ReadOnly = true;

            lblTotalLineas = new Label
            {
                Location  = new Point(8, 318),
                Size      = new Size(504, 24),
                Font      = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                ForeColor = Color.FromArgb(30, 80, 150),
                TextAlign = ContentAlignment.MiddleRight
            };

            panelDer.Controls.AddRange(new Control[]
                { lblLineas, btnAgregar, btnQuitarLinea, dgvLineas, lblTotalLineas });

            // ── Botones inferiores ────────────────────────────────────────
            var panelBtns = new Panel
            {
                Dock      = DockStyle.Bottom,
                Height    = 52,
                BackColor = Color.FromArgb(220, 225, 245)
            };

            if (!soloLectura)
            {
                var bOk = new Button
                {
                    Text      = "Guardar Factura",
                    Location  = new Point(260, 10), Size = new Size(160, 34),
                    BackColor = Color.FromArgb(30, 110, 190), ForeColor = Color.White,
                    FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 10f, FontStyle.Bold)
                };
                bOk.FlatAppearance.BorderSize = 0;
                bOk.Click += (s, e) => Guardar(f);
                panelBtns.Controls.Add(bOk);
            }

            var bCerrar = new Button
            {
                Text      = soloLectura ? "Cerrar" : "Cancelar",
                DialogResult = DialogResult.Cancel,
                Location  = new Point(440, 10), Size = new Size(110, 34),
                BackColor = Color.FromArgb(120, 120, 130), ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            bCerrar.FlatAppearance.BorderSize = 0;
            panelBtns.Controls.Add(bCerrar);

            Controls.AddRange(new Control[] { banner, panelIzq, panelDer, panelBtns });

            // ── Cargar datos si es edicion/lectura ────────────────────────
            if (f != null)
            {
                txtNumero.Text             = f.NumeroF;
                if (!string.IsNullOrWhiteSpace(f.Codigo))
                    cmbCliente.SelectedValue = f.Codigo;
                dtpFecha.Value             = f.Fecha;
                if (cmbFormaPago.Items.Contains(f.FormaDePago))
                    cmbFormaPago.SelectedItem = f.FormaDePago;
                numDescuento.Value         = f.Descuento;
                numIva.Value               = f.IvaF;
                numAbono.Value             = f.Abono < 0 ? 0 : f.Abono;
                numTotal.Value             = f.TotalF;
                numDias.Value              = f.DiasCreditoF;
                txtTransporte.Text         = f.Transporte;
                txtObs.Text                = f.Observaciones;
                txtNumero.ReadOnly         = true;
            }

            // Cargar lineas existentes
            if (lineas != null)
                foreach (var l in lineas)
                    AgregarFilaLinea(l.CodigoItem, l.Descripcion, l.Cantidad, l.PrecioUnit);

            // ── Eventos de lineas ─────────────────────────────────────────
            btnAgregar.Click += (s, e) => AbrirSelectorProducto();

            btnQuitarLinea.Click += (s, e) =>
            {
                if (dgvLineas.CurrentRow != null)
                    dgvLineas.Rows.RemoveAt(dgvLineas.CurrentRow.Index);
                RecalcularTotal();
            };

            dgvLineas.CellEndEdit += (s, e) =>
            {
                var col = dgvLineas.Columns[e.ColumnIndex].Name;
                if (col == "Cantidad" || col == "PrecioUnit")
                    RecalcularFila(e.RowIndex);

                if (col == "Cantidad")
                {
                    var row = dgvLineas.Rows[e.RowIndex];
                    decimal cant = ParseDecimal(row.Cells["Cantidad"].Value?.ToString());
                    if (cant > 0)
                        BeginInvoke(new Action(AbrirSelectorProducto));
                }
            };

            dgvLineas.KeyDown += (s, e) =>
            {
                if (dgvLineas.CurrentCell == null) return;

                var colName = dgvLineas.Columns[dgvLineas.CurrentCell.ColumnIndex].Name;
                if (colName == "Cantidad" && (e.KeyCode == Keys.Enter || e.KeyCode == Keys.Tab))
                {
                    e.Handled = true;
                    e.SuppressKeyPress = true;
                    dgvLineas.EndEdit();
                }
            };

            numDescuento.ValueChanged += (s, e) => RecalcularTotal();
            numIva.ValueChanged += (s, e) => RecalcularTotal();

            RecalcularTotal();
        }

        // ── Agregar producto al grid ───────────────────────────────────────
        private void AbrirSelectorProducto()
        {
            using var dlg = new DlgSelProducto();
            if (dlg.ShowDialog() != DialogResult.OK) return;

            var inv = dlg.ProductoSeleccionado;
            if (inv == null) return;

            // Verificar si ya esta en la lista
            foreach (DataGridViewRow row in dgvLineas.Rows)
            {
                if (row.Cells["Codigo"].Value?.ToString() == inv.Codigo)
                {
                    decimal cantActual = ParseDecimal(row.Cells["Cantidad"].Value?.ToString());
                    row.Cells["Cantidad"].Value = cantActual + 1;
                    RecalcularFila(row.Index);
                    dgvLineas.CurrentCell = row.Cells["Cantidad"];
                    dgvLineas.BeginEdit(true);
                    return;
                }
            }

            int idx = AgregarFilaLinea(inv.Codigo, inv.Descripcion, 0, inv.Precio, inv.Existencia);
            RecalcularTotal();
            dgvLineas.CurrentCell = dgvLineas.Rows[idx].Cells["Cantidad"];
            dgvLineas.BeginEdit(true);
        }

        private int AgregarFilaLinea(string codigo, string desc, decimal cant,
                                       decimal precio, decimal stock = 0)
        {
            int idx = dgvLineas.Rows.Add();
            var row = dgvLineas.Rows[idx];
            row.Cells["Codigo"].Value      = codigo;
            row.Cells["Descripcion"].Value = desc;
            row.Cells["Cantidad"].Value    = cant;
            row.Cells["PrecioUnit"].Value  = precio.ToString("F2");
            row.Cells["Subtotal"].Value    = (cant * precio).ToString("F2");
            row.Cells["Stock"].Value       = stock;

            if (stock > 0 && cant > stock)
                row.DefaultCellStyle.BackColor = Color.FromArgb(255, 220, 220);

            return idx;
        }

        private void RecalcularFila(int rowIndex)
        {
            if (rowIndex < 0 || rowIndex >= dgvLineas.Rows.Count) return;
            var row = dgvLineas.Rows[rowIndex];

            decimal cant = ParseDecimal(row.Cells["Cantidad"].Value?.ToString());
            decimal precio = ParseDecimal(row.Cells["PrecioUnit"].Value?.ToString());
            decimal stock = ParseDecimal(row.Cells["Stock"].Value?.ToString());

            row.Cells["PrecioUnit"].Value = precio.ToString("F2");
            row.Cells["Subtotal"].Value = (cant * precio).ToString("F2");

            row.DefaultCellStyle.BackColor = (stock > 0 && cant > stock)
                ? Color.FromArgb(255, 220, 220)
                : Color.White;

            RecalcularTotal();
        }

        private void RecalcularTotal()
        {
            decimal total = 0;
            foreach (DataGridViewRow row in dgvLineas.Rows)
                total += ParseDecimal(row.Cells["Subtotal"].Value?.ToString());

            lblTotalLineas.Text = "Subtotal lineas: " + total.ToString("C2");

            decimal desc = numDescuento?.Value ?? 0;
            decimal iva = numIva?.Value ?? 0;
            decimal totalConDesc = total * (1 - desc / 100m) + iva;
            if (numTotal != null)
                numTotal.Value = Math.Min(totalConDesc, numTotal.Maximum);
        }

        private void Guardar(Factura fExistente)
        {
            if (string.IsNullOrWhiteSpace(txtNumero.Text))
            {
                MessageBox.Show("Indica el numero de factura.", "Validacion",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string codigoCliente = ObtenerCodigoClienteSeleccionado();
            if (string.IsNullOrWhiteSpace(codigoCliente))
            {
                MessageBox.Show("Selecciona un cliente valido.", "Validacion",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (dgvLineas.Rows.Count == 0)
            {
                MessageBox.Show("Agrega al menos un producto.", "Validacion",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var lineas = new List<LineaFactura>();
            foreach (DataGridViewRow row in dgvLineas.Rows)
            {
                var cod = row.Cells["Codigo"].Value?.ToString();
                if (string.IsNullOrWhiteSpace(cod)) continue;

                decimal cant = ParseDecimal(row.Cells["Cantidad"].Value?.ToString());
                decimal precio = ParseDecimal(row.Cells["PrecioUnit"].Value?.ToString());
                decimal stock = ParseDecimal(row.Cells["Stock"].Value?.ToString());

                if (cant <= 0)
                {
                    MessageBox.Show("La cantidad debe ser mayor que cero para todos los productos.",
                        "Validacion", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                if (precio <= 0)
                {
                    MessageBox.Show("El precio unitario debe ser mayor que cero para todos los productos.",
                        "Validacion", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                if (stock > 0 && cant > stock)
                {
                    MessageBox.Show("Stock insuficiente para: " + cod, "Validacion",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                lineas.Add(new LineaFactura
                {
                    CodigoItem = cod,
                    Descripcion = row.Cells["Descripcion"].Value?.ToString() ?? "",
                    Cantidad = cant,
                    PrecioUnit = precio,
                    Descuento = 0
                });
            }

            if (lineas.Count == 0)
            {
                MessageBox.Show("No hay lineas validas para guardar.", "Validacion",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            Resultado = new Factura
            {
                NumeroF = txtNumero.Text.Trim(),
                Codigo = codigoCliente,
                Fecha = dtpFecha.Value.Date,
                Descuento = numDescuento.Value,
                Transporte = txtTransporte.Text?.Trim() ?? "",
                Observaciones = txtObs.Text?.Trim() ?? "",
                FormaDePago = cmbFormaPago.SelectedItem?.ToString() ?? "Contado",
                Abono = numAbono.Value,
                IvaF = numIva.Value,
                DiasCreditoF = (int)numDias.Value,
                TotalF = numTotal.Value
            };

            Lineas = lineas;
            DialogResult = DialogResult.OK;
            Close();
        }

        private decimal ParseDecimal(string txt)
        {
            if (string.IsNullOrWhiteSpace(txt)) return 0m;

            txt = txt.Trim();
            if (decimal.TryParse(txt, out var v1)) return v1;
            if (decimal.TryParse(txt, System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.InvariantCulture, out var v2)) return v2;

            txt = txt.Replace("$", "").Replace("€", "").Replace("Bs", "").Trim();
            if (decimal.TryParse(txt, out var v3)) return v3;
            if (decimal.TryParse(txt, System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.InvariantCulture, out var v4)) return v4;

            return 0m;
        }

        private string ObtenerCodigoClienteSeleccionado()
        {
            if (cmbCliente.SelectedValue != null)
            {
                var code = cmbCliente.SelectedValue.ToString();
                if (!string.IsNullOrWhiteSpace(code)) return code;
            }

            var texto = (cmbCliente.Text ?? "").Trim();
            if (string.IsNullOrWhiteSpace(texto)) return "";

            var exactCode = _clientesFactura.FirstOrDefault(c =>
                string.Equals(c.Codigo, texto, StringComparison.OrdinalIgnoreCase));
            if (exactCode != null) return exactCode.Codigo;

            var exactDesc = _clientesFactura.FirstOrDefault(c =>
                string.Equals(c.Descripcion ?? "", texto, StringComparison.OrdinalIgnoreCase));
            if (exactDesc != null) return exactDesc.Codigo;

            var prefix = _clientesFactura.FirstOrDefault(c =>
                string.Equals((c.Codigo + " - " + (c.Descripcion ?? "")).Trim(),
                    texto, StringComparison.OrdinalIgnoreCase));
            if (prefix != null) return prefix.Codigo;

            return "";
        }
    }

    // ══════════════════════════════════════════════════════════════════════════
    //  DIALOGO SELECCIONAR PRODUCTO
    // ══════════════════════════════════════════════════════════════════════════
    public class DlgSelProducto : Form
    {
        public Inventario ProductoSeleccionado { get; private set; }

        private TextBox txtBuscar;
        private DataGridView dgv;
        private List<Inventario> _inventario = new List<Inventario>();

        public DlgSelProducto()
        {
            Text = "Seleccionar producto";
            Size = new Size(760, 500);
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = MinimizeBox = false;
            BackColor = Color.FromArgb(245, 247, 255);

            var lbl = new Label
            {
                Text = "Buscar (codigo o descripcion):",
                AutoSize = true,
                Location = new Point(12, 16),
                Font = new Font("Segoe UI", 9f, FontStyle.Bold)
            };

            txtBuscar = new TextBox { Location = new Point(220, 12), Size = new Size(520, 24) };

            dgv = new DataGridView
            {
                Location = new Point(12, 46),
                Size = new Size(728, 366),
                ReadOnly = true,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                RowHeadersVisible = false,
                BackgroundColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle,
                Font = new Font("Segoe UI", 9f)
            };

            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "Codigo", HeaderText = "Codigo", Width = 110 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "Descripcion", HeaderText = "Descripcion" });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "Precio", HeaderText = "Precio", Width = 110 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "Stock", HeaderText = "Stock", Width = 90 });

            var btnSel = new Button
            {
                Text = "Seleccionar",
                Location = new Point(470, 420),
                Size = new Size(130, 32),
                BackColor = Color.FromArgb(40, 120, 200),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            btnSel.FlatAppearance.BorderSize = 0;

            var btnCancel = new Button
            {
                Text = "Cancelar",
                Location = new Point(610, 420),
                Size = new Size(130, 32),
                DialogResult = DialogResult.Cancel,
                BackColor = Color.FromArgb(120, 120, 130),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            btnCancel.FlatAppearance.BorderSize = 0;

            Controls.AddRange(new Control[] { lbl, txtBuscar, dgv, btnSel, btnCancel });

            _inventario = DatabaseService.ObtenerInventario()
                .OrderBy(i => int.TryParse(i.Codigo, out _) ? 0 : 1)
                .ThenBy(i => int.TryParse(i.Codigo, out var n) ? n : int.MaxValue)
                .ThenBy(i => i.Codigo)
                .ToList();

            var ac = new AutoCompleteStringCollection();
            foreach (var p in _inventario)
            {
                if (!string.IsNullOrWhiteSpace(p.Codigo)) ac.Add(p.Codigo);
                if (!string.IsNullOrWhiteSpace(p.Descripcion)) ac.Add(p.Descripcion);
                ac.Add(((p.Codigo ?? "") + " - " + (p.Descripcion ?? "")).Trim(' ', '-'));
            }
            txtBuscar.AutoCompleteMode = AutoCompleteMode.SuggestAppend;
            txtBuscar.AutoCompleteSource = AutoCompleteSource.CustomSource;
            txtBuscar.AutoCompleteCustomSource = ac;

            txtBuscar.TextChanged += (s, e) => Filtrar();
            dgv.DoubleClick += (s, e) => Seleccionar();
            dgv.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Enter)
                {
                    e.Handled = true;
                    e.SuppressKeyPress = true;
                    Seleccionar();
                }
            };
            btnSel.Click += (s, e) => Seleccionar();

            Filtrar();
        }

        private void Filtrar()
        {
            string q = (txtBuscar.Text ?? "").Trim().ToLowerInvariant();

            var data = string.IsNullOrWhiteSpace(q)
                ? _inventario
                : _inventario.Where(i =>
                    (i.Codigo ?? "").ToLowerInvariant().Contains(q) ||
                    (i.Descripcion ?? "").ToLowerInvariant().Contains(q))
                  .ToList();

            dgv.Rows.Clear();
            foreach (var i in data)
                dgv.Rows.Add(i.Codigo, i.Descripcion, i.Precio.ToString("F2"), i.Existencia.ToString("N2"));

            if (dgv.Rows.Count > 0)
                dgv.Rows[0].Selected = true;
        }

        private void Seleccionar()
        {
            if (dgv.CurrentRow == null) return;
            var codigo = dgv.CurrentRow.Cells["Codigo"].Value?.ToString();
            if (string.IsNullOrWhiteSpace(codigo)) return;

            ProductoSeleccionado = _inventario.FirstOrDefault(i => i.Codigo == codigo);
            if (ProductoSeleccionado == null) return;

            DialogResult = DialogResult.OK;
            Close();
        }
    }
}
