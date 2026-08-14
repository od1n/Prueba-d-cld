#nullable enable
// FrmAbonos.cs — Formulario para gestión de abonos de clientes
// Permite registrar abonos con comprobante (imagen), filtrar por cliente/fecha,
// ver comprobantes adjuntos y generar estado de cuenta.

using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace SistemaGestion
{
    public class FrmAbonos : FormBase
    {
        // ── Controles principales ──────────────────────────────────────────
        private DataGridView _grid = null!;
        private ComboBox     _cboCliente = null!;
        private DateTimePicker _dtDesde = null!, _dtHasta = null!;
        private Label        _lblTotal = null!;

        // Datos
        private List<Abono> _abonos = new();
        private List<(string cod, string nombre)> _clientes = new();

        public FrmAbonos()
        {
            Text = "Abonos de Clientes";
            ClientSize = new Size(1020, 620);
            MinimumSize = new Size(800, 500);
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.Sizable;
            MaximizeBox = true;
            BackColor = UITheme.Fondo;
            Font = UITheme.Body;

            // ── Header ─────────────────────────────────────────────────────
            var hdr = new Panel { Dock = DockStyle.Top, Height = 56, BackColor = UITheme.AzulOscuro };
            hdr.Paint += (s, e) =>
            {
                using var f = new Font("Segoe UI", 14f, FontStyle.Bold);
                e.Graphics.DrawString("Abonos de Clientes", f, Brushes.White, 20, 10);
                using var fs = new Font("Segoe UI", 9f);
                using var br = new SolidBrush(Color.FromArgb(168, 177, 187));
                e.Graphics.DrawString("Registro, comprobantes y estado de cuenta", fs, br, 22, 33);
            };

            // ── Panel de filtros ───────────────────────────────────────────
            var pnlFiltros = new Panel { Dock = DockStyle.Top, Height = 50, BackColor = UITheme.FondoCard,
                Padding = new Padding(16, 8, 16, 8) };

            pnlFiltros.Controls.Add(new Label { Text = "Cliente:", AutoSize = true,
                Location = new Point(16, 14), Font = UITheme.BodyBold });

            _cboCliente = new ComboBox { Location = new Point(80, 10), Width = 250,
                DropDownStyle = ComboBoxStyle.DropDownList, Font = UITheme.Body };
            _cboCliente.SelectedIndexChanged += (s, e) => CargarAbonos();

            pnlFiltros.Controls.Add(new Label { Text = "Desde:", AutoSize = true,
                Location = new Point(350, 14), Font = UITheme.Body });
            _dtDesde = new DateTimePicker { Location = new Point(400, 10), Width = 120,
                Format = DateTimePickerFormat.Short,
                Value = new DateTime(DateTime.Today.Year, 1, 1) };
            _dtDesde.ValueChanged += (s, e) => CargarAbonos();

            pnlFiltros.Controls.Add(new Label { Text = "Hasta:", AutoSize = true,
                Location = new Point(535, 14), Font = UITheme.Body });
            _dtHasta = new DateTimePicker { Location = new Point(585, 10), Width = 120,
                Format = DateTimePickerFormat.Short, Value = DateTime.Today };
            _dtHasta.ValueChanged += (s, e) => CargarAbonos();

            pnlFiltros.Controls.AddRange(new Control[] { _cboCliente, _dtDesde, _dtHasta });

            // ── Footer con botones ─────────────────────────────────────────
            var footer = new Panel { Dock = DockStyle.Bottom, Height = 52, BackColor = UITheme.FondoCard };
            footer.Paint += (s, e) =>
            {
                using var pen = new Pen(UITheme.Borde);
                e.Graphics.DrawLine(pen, 0, 0, footer.Width, 0);
            };

            _lblTotal = new Label { Text = "Total: $ 0,00", Font = UITheme.H3,
                ForeColor = UITheme.Azul, AutoSize = true, Location = new Point(20, 14) };

            var btnNuevo = Boton("+ Nuevo Abono", UITheme.Verde, NuevoAbono);
            btnNuevo.Location = new Point(0, 7);
            var btnEstado = Boton("Estado de Cuenta", UITheme.Azul, GenerarEstadoCuenta);
            btnEstado.Location = new Point(0, 7);
            var btnCerrar = Boton("Cerrar", Color.FromArgb(100, 110, 125), () => Close());
            btnCerrar.Location = new Point(0, 7);

            var flowR = new FlowLayoutPanel { Dock = DockStyle.Right,
                FlowDirection = FlowDirection.RightToLeft, Width = 460,
                BackColor = UITheme.FondoCard, WrapContents = false };
            flowR.Controls.AddRange(new Control[] { btnCerrar, btnEstado, btnNuevo });

            footer.Controls.Add(_lblTotal);
            footer.Controls.Add(flowR);

            // ── Grid ───────────────────────────────────────────────────────
            _grid = new DataGridView
            {
                Dock = DockStyle.Fill, ReadOnly = true, AllowUserToAddRows = false,
                AllowUserToDeleteRows = false, SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false, BackgroundColor = UITheme.Fondo,
                BorderStyle = BorderStyle.None, RowHeadersVisible = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                Font = UITheme.Body, EnableHeadersVisualStyles = false
            };
            _grid.DefaultCellStyle.SelectionBackColor = Color.FromArgb(220, 230, 245);
            _grid.DefaultCellStyle.SelectionForeColor = UITheme.Texto;
            _grid.ColumnHeadersDefaultCellStyle.BackColor = UITheme.AzulOscuro;
            _grid.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
            _grid.ColumnHeadersDefaultCellStyle.Font = UITheme.BodyBold;
            _grid.ColumnHeadersHeight = 36;
            _grid.RowTemplate.Height = 32;

            _grid.Columns.AddRange(new DataGridViewColumn[]
            {
                new DataGridViewTextBoxColumn { Name = "Fecha",     HeaderText = "Fecha",      FillWeight = 12 },
                new DataGridViewTextBoxColumn { Name = "Cliente",   HeaderText = "Cliente",    FillWeight = 18 },
                new DataGridViewTextBoxColumn { Name = "Monto",     HeaderText = "Monto ($)",  FillWeight = 12 },
                new DataGridViewTextBoxColumn { Name = "Metodo",    HeaderText = "Método",     FillWeight = 12 },
                new DataGridViewTextBoxColumn { Name = "Referencia",HeaderText = "Referencia", FillWeight = 14 },
                new DataGridViewTextBoxColumn { Name = "Cuenta",    HeaderText = "Cuenta",     FillWeight = 14 },
                new DataGridViewTextBoxColumn { Name = "Estado",    HeaderText = "Estado",     FillWeight = 10 },
                new DataGridViewTextBoxColumn { Name = "Comprobante",HeaderText = "📎",        FillWeight = 5  },
            });

            _grid.CellDoubleClick += Grid_CellDoubleClick;
            _grid.CellFormatting += Grid_CellFormatting;

            // ── Ensamblar ──────────────────────────────────────────────────
            Controls.Add(_grid);
            Controls.Add(pnlFiltros);
            Controls.Add(footer);
            Controls.Add(hdr);

            Load += (s, e) => { CargarClientes(); CargarAbonos(); };
        }

        // ── Helpers ────────────────────────────────────────────────────────

        private Button Boton(string texto, Color color, Action accion)
        {
            var btn = new Button
            {
                Text = texto, Width = 140, Height = 36,
                FlatStyle = FlatStyle.Flat, BackColor = color, ForeColor = Color.White,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold), Cursor = Cursors.Hand,
                Margin = new Padding(6, 0, 0, 0)
            };
            btn.FlatAppearance.BorderSize = 0;
            btn.Click += (s, e) => accion();
            return btn;
        }

        private void CargarClientes()
        {
            _clientes = DatabaseService.ObtenerClientes()
                .Select(c => (c.Codigo, c.Descripcion ?? c.Codigo))
                .OrderBy(c => c.Item2)
                .ToList();

            _cboCliente.Items.Clear();
            _cboCliente.Items.Add("— Todos —");
            foreach (var (cod, nombre) in _clientes)
                _cboCliente.Items.Add($"{nombre}  [{cod}]");
            _cboCliente.SelectedIndex = 0;
        }

        private void CargarAbonos()
        {
            string? clienteFiltro = null;
            if (_cboCliente.SelectedIndex > 0)
                clienteFiltro = _clientes[_cboCliente.SelectedIndex - 1].cod;

            _abonos = DatabaseService.ObtenerAbonosExtendido(
                cliente: clienteFiltro,
                desde: _dtDesde.Value.Date,
                hasta: _dtHasta.Value.Date);

            var cuentas = DatabaseService.ObtenerCuentasReceptoras(soloActivas: false);

            _grid.Rows.Clear();
            foreach (var a in _abonos)
            {
                string cuenta = cuentas.FirstOrDefault(c => c.Id == a.IdCuentaReceptora)?.Nombre ?? "—";
                string comprobante = string.IsNullOrEmpty(a.RutaComprobante) ? "" : "📎";
                _grid.Rows.Add(
                    a.Fecha.ToString("dd/MM/yyyy"),
                    a.Cliente,
                    a.Monto.ToString("N2"),
                    a.MetodoPago,
                    a.Referencia,
                    cuenta,
                    a.Estado,
                    comprobante
                );
            }

            decimal total = _abonos.Where(a => a.Estado == "Confirmado").Sum(a => a.Monto);
            _lblTotal.Text = $"Total confirmados: $ {total:N2}  ({_abonos.Count} registros)";
        }

        private void Grid_CellFormatting(object? sender, DataGridViewCellFormattingEventArgs e)
        {
            if (_grid.Columns[e.ColumnIndex].Name == "Estado" && e.Value is string estado)
            {
                e.CellStyle!.ForeColor = estado switch
                {
                    "Confirmado" => UITheme.Verde,
                    "Pendiente"  => UITheme.Naranja,
                    "Rechazado"  => UITheme.Rojo,
                    _ => UITheme.Texto
                };
                e.CellStyle.Font = UITheme.BodyBold;
            }
        }

        private void Grid_CellDoubleClick(object? sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0 || e.RowIndex >= _abonos.Count) return;
            var abono = _abonos[e.RowIndex];

            // Si hizo doble click en la columna del comprobante y hay imagen
            if (_grid.Columns[e.ColumnIndex].Name == "Comprobante"
                && !string.IsNullOrEmpty(abono.RutaComprobante))
            {
                VerComprobante(abono.RutaComprobante);
                return;
            }

            // Doble click en cualquier otra columna: ver detalle
            VerDetalleAbono(abono);
        }

        private void VerComprobante(string ruta)
        {
            if (!File.Exists(ruta))
            {
                MessageBox.Show(this, "El archivo de comprobante no se encuentra:\n" + ruta,
                    "Archivo no encontrado", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            try
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                { FileName = ruta, UseShellExecute = true });
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, "No se pudo abrir: " + ex.Message,
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void VerDetalleAbono(Abono a)
        {
            var cuentas = DatabaseService.ObtenerCuentasReceptoras(soloActivas: false);
            string cuenta = cuentas.FirstOrDefault(c => c.Id == a.IdCuentaReceptora)?.Nombre ?? "—";

            string detalle =
                $"Fecha: {a.Fecha:dd/MM/yyyy}\n" +
                $"Cliente: {a.Cliente}\n" +
                $"Monto: $ {a.Monto:N2}\n" +
                $"Método: {a.MetodoPago}\n" +
                $"Referencia: {(string.IsNullOrEmpty(a.Referencia) ? "—" : a.Referencia)}\n" +
                $"Remitente: {(string.IsNullOrEmpty(a.NombreRemitente) ? "—" : a.NombreRemitente)}\n" +
                $"Cuenta destino: {cuenta}\n" +
                $"Estado: {a.Estado}\n" +
                $"Observaciones: {(string.IsNullOrEmpty(a.Observaciones) ? "—" : a.Observaciones)}\n" +
                $"Comprobante: {(string.IsNullOrEmpty(a.RutaComprobante) ? "Sin adjunto" : "📎 Adjunto")}";

            var result = MessageBox.Show(this, detalle +
                (string.IsNullOrEmpty(a.RutaComprobante) ? "" : "\n\n¿Desea ver el comprobante?"),
                $"Abono #{a.NumAbono}",
                string.IsNullOrEmpty(a.RutaComprobante) ? MessageBoxButtons.OK : MessageBoxButtons.YesNo,
                MessageBoxIcon.Information);

            if (result == DialogResult.Yes && !string.IsNullOrEmpty(a.RutaComprobante))
                VerComprobante(a.RutaComprobante);
        }

        // ── Nuevo Abono ────────────────────────────────────────────────────

        private void NuevoAbono()
        {
            using var dlg = new DlgNuevoAbono(_clientes,
                DatabaseService.ObtenerCuentasReceptoras());
            if (dlg.ShowDialog(this) == DialogResult.OK && dlg.AbonoCreado != null)
            {
                DatabaseService.InsertarAbonoExtendido(dlg.AbonoCreado);
                CargarAbonos();
            }
        }

        // ── Estado de Cuenta ───────────────────────────────────────────────

        private void GenerarEstadoCuenta()
        {
            if (_cboCliente.SelectedIndex <= 0)
            {
                MessageBox.Show(this, "Selecciona un cliente para generar el estado de cuenta.",
                    "Cliente requerido", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            var (cod, nombre) = _clientes[_cboCliente.SelectedIndex - 1];
            var (saldoFavor, abonos, totalAbonos) = DatabaseService.ObtenerEstadoCuenta(cod);

            string reporte =
                $"══════════════════════════════════════════════\n" +
                $"  ESTADO DE CUENTA — {nombre}\n" +
                $"  Código: {cod}  |  Fecha: {DateTime.Today:dd/MM/yyyy}\n" +
                $"══════════════════════════════════════════════\n\n";

            var confirmados = abonos.Where(a => a.Estado == "Confirmado").ToList();
            if (confirmados.Count == 0)
            {
                reporte += "  Sin abonos registrados en el periodo.\n";
            }
            else
            {
                reporte += $"  {"N°",-4} {"Fecha",-12} {"Método",-12} {"Referencia",-14} {"Monto",12}\n";
                reporte += $"  {"—",-4} {"—",-12} {"—",-12} {"—",-14} {"—",12}\n";
                int n = 1;
                foreach (var a in confirmados.OrderBy(a => a.Fecha))
                {
                    reporte += $"  {n,-4} {a.Fecha:dd/MM/yy,-12} {a.MetodoPago,-12} " +
                               $"{(string.IsNullOrEmpty(a.Referencia) ? "—" : a.Referencia),-14} " +
                               $"{"$ " + a.Monto.ToString("N2"),12}\n";
                    n++;
                }
            }

            reporte += $"\n  Total abonos: $ {totalAbonos:N2}\n";
            reporte += $"  Saldo a favor: $ {saldoFavor:N2}\n";
            reporte += $"\n══════════════════════════════════════════════\n";
            reporte += $"  EL HATO PASCHER C.A. — RIF J-30571871-7\n";

            // Mostrar en un form sencillo con opción de copiar
            using var frm = new Form
            {
                Text = $"Estado de Cuenta — {nombre}",
                ClientSize = new Size(640, 500),
                StartPosition = FormStartPosition.CenterParent,
                BackColor = UITheme.Fondo, Font = UITheme.Body
            };
            var txt = new TextBox
            {
                Dock = DockStyle.Fill, Multiline = true, ReadOnly = true,
                ScrollBars = ScrollBars.Vertical, Font = new Font("Consolas", 10f),
                Text = reporte, BackColor = Color.White
            };
            var btnCopiar = new Button
            {
                Text = "Copiar al portapapeles", Dock = DockStyle.Bottom, Height = 40,
                FlatStyle = FlatStyle.Flat, BackColor = UITheme.Azul, ForeColor = Color.White,
                Font = UITheme.BodyBold
            };
            btnCopiar.FlatAppearance.BorderSize = 0;
            btnCopiar.Click += (s, e) =>
            {
                Clipboard.SetText(reporte);
                MessageBox.Show(frm, "Copiado al portapapeles.", "Listo",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            };
            frm.Controls.Add(txt);
            frm.Controls.Add(btnCopiar);
            frm.ShowDialog(this);
        }
    }

    // ════════════════════════════════════════════════════════════════════════
    //  Diálogo para crear un nuevo abono
    // ════════════════════════════════════════════════════════════════════════

    public class DlgNuevoAbono : Form
    {
        private ComboBox   _cboCliente = null!, _cboMetodo = null!, _cboCuenta = null!;
        private TextBox    _txtMonto = null!, _txtReferencia = null!,
                           _txtRemitente = null!, _txtObs = null!, _txtComprobante = null!;
        private DateTimePicker _dtFecha = null!;
        private PictureBox    _picPreview = null!;

        private readonly List<(string cod, string nombre)> _clientes;
        private readonly List<CuentaReceptora> _cuentas;

        public Abono? AbonoCreado { get; private set; }

        private static readonly string[] MetodosPago =
            { "Efectivo", "Zelle", "TransfNac", "PagoMovil" };

        public DlgNuevoAbono(
            List<(string cod, string nombre)> clientes,
            List<CuentaReceptora> cuentas)
        {
            _clientes = clientes;
            _cuentas = cuentas;

            Text = "Nuevo Abono";
            ClientSize = new Size(520, 560);
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false; MinimizeBox = false;
            BackColor = UITheme.Fondo;
            Font = UITheme.Body;

            var pnl = new Panel { Dock = DockStyle.Fill, AutoScroll = true,
                Padding = new Padding(24, 16, 24, 16) };

            int y = 16;
            int xLbl = 0, xCtrl = 140, wCtrl = 330;

            // Cliente
            pnl.Controls.Add(new Label { Text = "Cliente *", Font = UITheme.BodyBold,
                Location = new Point(xLbl, y + 3), AutoSize = true });
            _cboCliente = new ComboBox { Location = new Point(xCtrl, y), Width = wCtrl,
                DropDownStyle = ComboBoxStyle.DropDownList };
            foreach (var (cod, nombre) in _clientes)
                _cboCliente.Items.Add($"{nombre}  [{cod}]");
            if (_cboCliente.Items.Count > 0) _cboCliente.SelectedIndex = 0;
            pnl.Controls.Add(_cboCliente);
            y += 38;

            // Fecha
            pnl.Controls.Add(new Label { Text = "Fecha", Font = UITheme.BodyBold,
                Location = new Point(xLbl, y + 3), AutoSize = true });
            _dtFecha = new DateTimePicker { Location = new Point(xCtrl, y), Width = 150,
                Format = DateTimePickerFormat.Short, Value = DateTime.Today };
            pnl.Controls.Add(_dtFecha);
            y += 38;

            // Monto
            pnl.Controls.Add(new Label { Text = "Monto ($) *", Font = UITheme.BodyBold,
                Location = new Point(xLbl, y + 3), AutoSize = true });
            _txtMonto = new TextBox { Location = new Point(xCtrl, y), Width = 150 };
            pnl.Controls.Add(_txtMonto);
            y += 38;

            // Método de pago
            pnl.Controls.Add(new Label { Text = "Método *", Font = UITheme.BodyBold,
                Location = new Point(xLbl, y + 3), AutoSize = true });
            _cboMetodo = new ComboBox { Location = new Point(xCtrl, y), Width = 200,
                DropDownStyle = ComboBoxStyle.DropDownList };
            _cboMetodo.Items.AddRange(MetodosPago);
            _cboMetodo.SelectedIndex = 0;
            _cboMetodo.SelectedIndexChanged += (s, e) => MetodoChanged();
            pnl.Controls.Add(_cboMetodo);
            y += 38;

            // Cuenta destino
            pnl.Controls.Add(new Label { Text = "Cuenta destino", Font = UITheme.BodyBold,
                Location = new Point(xLbl, y + 3), AutoSize = true });
            _cboCuenta = new ComboBox { Location = new Point(xCtrl, y), Width = wCtrl,
                DropDownStyle = ComboBoxStyle.DropDownList };
            _cboCuenta.Items.Add("— No especificada —");
            foreach (var ct in _cuentas)
                _cboCuenta.Items.Add($"{ct.Nombre} ({ct.Titular})");
            _cboCuenta.SelectedIndex = 0;
            pnl.Controls.Add(_cboCuenta);
            y += 38;

            // Referencia
            pnl.Controls.Add(new Label { Text = "Referencia", Font = UITheme.Body,
                Location = new Point(xLbl, y + 3), AutoSize = true });
            _txtReferencia = new TextBox { Location = new Point(xCtrl, y), Width = wCtrl };
            pnl.Controls.Add(_txtReferencia);
            y += 38;

            // Remitente
            pnl.Controls.Add(new Label { Text = "Remitente", Font = UITheme.Body,
                Location = new Point(xLbl, y + 3), AutoSize = true,
                ForeColor = UITheme.TextoTenue });
            _txtRemitente = new TextBox { Location = new Point(xCtrl, y), Width = wCtrl };
            pnl.Controls.Add(_txtRemitente);
            y += 38;

            // Comprobante (imagen)
            pnl.Controls.Add(new Label { Text = "Comprobante", Font = UITheme.BodyBold,
                Location = new Point(xLbl, y + 3), AutoSize = true });
            _txtComprobante = new TextBox { Location = new Point(xCtrl, y), Width = wCtrl - 80,
                ReadOnly = true };
            var btnAdj = new Button { Text = "Adjuntar", Location = new Point(xCtrl + wCtrl - 74, y),
                Width = 74, Height = 28, FlatStyle = FlatStyle.Flat,
                BackColor = UITheme.Azul, ForeColor = Color.White,
                Font = UITheme.Pequeno, Cursor = Cursors.Hand };
            btnAdj.FlatAppearance.BorderSize = 0;
            btnAdj.Click += (s, e) => AdjuntarComprobante();
            pnl.Controls.AddRange(new Control[] { _txtComprobante, btnAdj });
            y += 34;

            // Preview de imagen
            _picPreview = new PictureBox
            {
                Location = new Point(xCtrl, y), Size = new Size(200, 120),
                SizeMode = PictureBoxSizeMode.Zoom, BorderStyle = BorderStyle.FixedSingle,
                BackColor = Color.White, Visible = false
            };
            pnl.Controls.Add(_picPreview);
            y += 126;

            // Observaciones
            pnl.Controls.Add(new Label { Text = "Observaciones", Font = UITheme.Body,
                Location = new Point(xLbl, y + 3), AutoSize = true });
            _txtObs = new TextBox { Location = new Point(xCtrl, y), Width = wCtrl, Height = 50,
                Multiline = true };
            pnl.Controls.Add(_txtObs);

            // ── Footer ─────────────────────────────────────────────────────
            var footer = new Panel { Dock = DockStyle.Bottom, Height = 52,
                BackColor = UITheme.FondoCard };
            var btnGuardar = new Button
            {
                Text = "Guardar", Width = 130, Height = 38,
                FlatStyle = FlatStyle.Flat, BackColor = UITheme.Verde, ForeColor = Color.White,
                Font = UITheme.Boton, Cursor = Cursors.Hand,
                Location = new Point(ClientSize.Width - 290, 7)
            };
            btnGuardar.FlatAppearance.BorderSize = 0;
            btnGuardar.Click += (s, e) => Guardar();

            var btnCancel = new Button
            {
                Text = "Cancelar", Width = 130, Height = 38,
                FlatStyle = FlatStyle.Flat, BackColor = Color.FromArgb(100, 110, 125),
                ForeColor = Color.White, Font = UITheme.Boton, Cursor = Cursors.Hand,
                Location = new Point(ClientSize.Width - 150, 7)
            };
            btnCancel.FlatAppearance.BorderSize = 0;
            btnCancel.Click += (s, e) => { DialogResult = DialogResult.Cancel; Close(); };

            footer.Controls.AddRange(new Control[] { btnGuardar, btnCancel });

            Controls.Add(pnl);
            Controls.Add(footer);
        }

        private void MetodoChanged()
        {
            bool esEfectivo = _cboMetodo.SelectedItem?.ToString() == "Efectivo";
            // Si es efectivo, la cuenta destino no aplica
            _cboCuenta.Enabled = !esEfectivo;
            if (esEfectivo) _cboCuenta.SelectedIndex = 0;
        }

        private void AdjuntarComprobante()
        {
            using var ofd = new OpenFileDialog
            {
                Title = "Selecciona el comprobante (foto del pago)",
                Filter = "Imágenes|*.jpg;*.jpeg;*.png;*.bmp;*.webp|PDF|*.pdf|Todos|*.*"
            };
            if (ofd.ShowDialog(this) != DialogResult.OK) return;
            try
            {
                var carpeta = Path.Combine(SecurityService.CarpetaDatos, "comprobantes", "abonos",
                    DateTime.Today.ToString("yyyy-MM"));
                Directory.CreateDirectory(carpeta);
                string ext = Path.GetExtension(ofd.FileName);
                string destino = Path.Combine(carpeta,
                    "abono_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ext);
                File.Copy(ofd.FileName, destino, true);
                _txtComprobante.Text = destino;

                // Mostrar preview si es imagen
                try
                {
                    string extLower = ext.ToLower();
                    if (extLower is ".jpg" or ".jpeg" or ".png" or ".bmp" or ".webp")
                    {
                        _picPreview.Image?.Dispose();
                        _picPreview.Image = Image.FromFile(destino);
                        _picPreview.Visible = true;
                    }
                    else
                    {
                        _picPreview.Visible = false;
                    }
                }
                catch { _picPreview.Visible = false; }
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, "No se pudo adjuntar: " + ex.Message,
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void Guardar()
        {
            // Validaciones
            if (_cboCliente.SelectedIndex < 0)
            {
                MessageBox.Show(this, "Selecciona un cliente.", "Validación",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (!decimal.TryParse(_txtMonto.Text.Replace(",", "."),
                System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.InvariantCulture, out decimal monto) || monto <= 0)
            {
                MessageBox.Show(this, "Ingresa un monto válido mayor a cero.", "Validación",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                _txtMonto.Focus();
                return;
            }

            string metodo = _cboMetodo.SelectedItem?.ToString() ?? "Efectivo";

            // Si no es efectivo, pedir comprobante
            if (metodo != "Efectivo" && string.IsNullOrEmpty(_txtComprobante.Text))
            {
                var resp = MessageBox.Show(this,
                    "No has adjuntado un comprobante. Para pagos no en efectivo se recomienda adjuntar la imagen.\n\n¿Deseas continuar sin comprobante?",
                    "Comprobante faltante", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                if (resp == DialogResult.No) return;
            }

            var (cod, _) = _clientes[_cboCliente.SelectedIndex];

            int idCuenta = 0;
            if (_cboCuenta.SelectedIndex > 0)
                idCuenta = _cuentas[_cboCuenta.SelectedIndex - 1].Id;

            AbonoCreado = new Abono
            {
                NumeroF           = "",  // abono libre, no ligado a factura específica
                Fecha             = _dtFecha.Value.Date,
                Monto             = monto,
                Cliente           = cod,
                MetodoPago        = metodo,
                Referencia        = _txtReferencia.Text.Trim(),
                NombreRemitente   = _txtRemitente.Text.Trim(),
                RutaComprobante   = _txtComprobante.Text.Trim(),
                Observaciones     = _txtObs.Text.Trim(),
                IdCuentaReceptora = idCuenta,
                Estado            = "Confirmado"
            };

            DialogResult = DialogResult.OK;
            Close();
        }
    }
}
