// ════════════════════════════════════════════════════════════════════════
// ARCHIVO: FrmGastoEditor.cs  →  raíz del proyecto
//
// Diálogo modal para registrar o editar un Gasto. Maneja:
//   - Selección de categoría (dropdown desde CategoriasGasto)
//   - Multi-moneda: USD directo / Bs a tasa BCV / Bs a tasa Paralelo /
//     Bs a tasa personalizada. Cálculo automático de MontoUSD y MontoBs.
//   - Estado Pendiente/Pagado con fecha de vencimiento
//   - Adjuntar comprobante (foto o PDF) — copiado a %AppData%\SistemaGestion\comprobantes
// ════════════════════════════════════════════════════════════════════════

#nullable enable
using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace SistemaGestion
{
    public class FrmGastoEditor : FormBase
    {
        private Egreso _egreso;
        private bool _esNuevo;

        // Tasas vigentes (precargadas para auto-llenado)
        private decimal _tasaBCV     = 0m;
        private decimal _tasaParalelo = 0m;

        // Controles
        private DateTimePicker _dtpFecha = null!;
        private ComboBox       _cmbCategoria = null!;
        private TextBox        _txtConcepto = null!;
        private TextBox        _txtProveedor = null!;
        private NumericUpDown  _numMonto = null!;
        private ComboBox       _cmbMoneda = null!;
        private ComboBox       _cmbTipoTasa = null!;
        private NumericUpDown  _numTasa = null!;
        private Label          _lblConvUsd = null!, _lblConvBs = null!;
        private CheckBox       _chkPendiente = null!;
        private CheckBox       _chkPrecioPendiente = null!;
        private NumericUpDown  _numPagado = null!;
        private DateTimePicker _dtpVence = null!;
        private TextBox        _txtComprobante = null!;
        private Button         _btnAdjuntar = null!, _btnVerComprobante = null!;
        private TextBox        _txtObs = null!;
        private Label          _lblError = null!;
        private ValidacionInline _v = null!;

        // ── Materia Prima (visibles solo si la categoría es MP) ─────────────
        private Panel          _pnlMP = null!;
        private ComboBox       _cmbInsumo = null!;
        private NumericUpDown  _numCantidad = null!;
        private NumericUpDown  _numPrecioUnit = null!;
        // Multilínea: una factura puede traer varios productos.
        private DataGridView   _gridLineas = null!;
        private Button         _btnAddLinea = null!, _btnDelLinea = null!;
        private readonly System.Collections.Generic.List<DetalleEgreso> _lineas = new();
        private Label          _lblMontoCalc = null!;

        // Layout raíz (para insertar lblError + pnlBtn en filas correctas)
        private TableLayoutPanel _rootForm = null!;

        public Egreso? EgresoGuardado { get; private set; }

        public FrmGastoEditor(Egreso? existente = null)
        {
            _egreso = existente ?? new Egreso {
                Fecha = DateTime.Today,
                MonedaOriginal = "USD",
                TipoTasa = "USD_DIRECTO",
                Estado = "Pagado"
            };
            _esNuevo = existente == null || existente.Id == 0;

            // Pre-cargar tasas vigentes
            try
            {
                var t = DatabaseService.ObtenerTasaPorFecha(_egreso.Fecha);
                if (t != null)
                {
                    _tasaBCV      = t.TasaBCV;
                    _tasaParalelo = t.TasaParalelo;
                }
            }
            catch { }

            ConstruirUI();
        }

        private void ConstruirUI()
        {
            Text          = _esNuevo ? "Registrar egreso" : "Editar egreso #" + _egreso.Id;
            // Altura reducida — el contenido interior tiene AutoScroll
            // (en pantallas pequeñas, el ClientSize de 760 cortaba los botones).
            ClientSize    = new Size(720, 640);
            MinimumSize   = new Size(700, 520);
            StartPosition = FormStartPosition.CenterParent;
            // Sizable: el usuario puede agrandar la ventana si necesita más espacio.
            FormBorderStyle = FormBorderStyle.Sizable;
            MaximizeBox = true; MinimizeBox = false;
            BackColor = UITheme.Fondo;

            // ── Layout raíz: TableLayoutPanel garantiza orden correcto ──
            // (con Controls.Add y Dock.Top/Bottom/Fill el orden de inserción
            //  causaba que el banner quedara TAPADO por el panel Fill.
            //  TableLayoutPanel evita ese problema.)
            var root = new TableLayoutPanel
            {
                Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 4,
                BackColor = UITheme.Fondo
            };
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 56f));   // banner
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));   // contenido scroll
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 28f));   // mensaje error
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 50f));   // botones

            // Banner
            var banner = new Panel { Dock = DockStyle.Fill,
                BackColor = UITheme.AzulOscuro, Tag = "no-theme" };
            banner.Controls.Add(new Label
            {
                Text = "  " + Text,
                Font = new Font("Segoe UI", 14f, FontStyle.Bold),
                ForeColor = Color.White, BackColor = UITheme.AzulOscuro,
                Dock = DockStyle.Fill, AutoSize = false,
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(20, 0, 0, 0)
            });
            root.Controls.Add(banner, 0, 0);

            // Panel scroll exterior con padding lateral.
            // Padding del Panel afecta al DisplayRectangle del hijo Dock.Fill,
            // así que los controles con Location(0, y) en pnl interno respetan
            // el margen lateral del exterior.
            var pnlExt = new Panel { Dock = DockStyle.Fill, AutoScroll = true,
                BackColor = UITheme.Fondo,
                Padding = new Padding(28, 18, 28, 20) };
            // El panel interno crece con su contenido (AutoSize) — así el
            // AutoScroll del panel exterior muestra la barra y se puede
            // desplazar para ver todos los campos. Con Dock.Fill se ajustaba
            // al área visible y cortaba los campos de abajo (sin scroll).
            var pnl = new Panel { Dock = DockStyle.Top, AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink, BackColor = UITheme.Fondo };
            pnlExt.Controls.Add(pnl);
            root.Controls.Add(pnlExt, 0, 1);

            Controls.Add(root);
            // Guardamos referencia para que las funciones Controls.Add que vienen
            // después de la sección "// ── Sección moneda ───" (lblError, pnlBtn)
            // sepan dónde insertarse vía variables _rootForm y row index.
            _rootForm = root;

            int y = 0;

            // Fecha + Categoría
            pnl.Controls.Add(new Label { Text = "Fecha", Font = UITheme.BodyBold,
                Location = new Point(0, y), AutoSize = true, ForeColor = UITheme.AzulOscuro });
            pnl.Controls.Add(new Label { Text = "Categoría", Font = UITheme.BodyBold,
                Location = new Point(220, y), AutoSize = true, ForeColor = UITheme.AzulOscuro });
            y += 22;
            _dtpFecha = new DateTimePicker { Location = new Point(0, y),
                Size = new Size(200, 28), Format = DateTimePickerFormat.Short,
                Value = _egreso.Fecha };
            _cmbCategoria = new ComboBox { Location = new Point(220, y),
                Size = new Size(420, 28), DropDownStyle = ComboBoxStyle.DropDownList,
                Font = UITheme.Body };
            CargarCategorias();
            pnl.Controls.AddRange(new Control[] { _dtpFecha, _cmbCategoria });
            y += 42;

            // Concepto
            pnl.Controls.Add(new Label { Text = "Concepto / descripción", Font = UITheme.BodyBold,
                Location = new Point(0, y), AutoSize = true, ForeColor = UITheme.AzulOscuro });
            y += 22;
            _txtConcepto = new TextBox { Location = new Point(0, y), Size = new Size(640, 28),
                Font = UITheme.Body, Text = _egreso.Concepto, MaxLength = 200 };
            pnl.Controls.Add(_txtConcepto);
            y += 42;

            // Proveedor
            pnl.Controls.Add(new Label { Text = "Proveedor / pagado a", Font = UITheme.BodyBold,
                Location = new Point(0, y), AutoSize = true, ForeColor = UITheme.AzulOscuro });
            y += 22;
            _txtProveedor = new TextBox { Location = new Point(0, y), Size = new Size(640, 28),
                Font = UITheme.Body, Text = _egreso.Proveedor, MaxLength = 100 };
            // Autocompletar desde el directorio de Proveedores (si ya existe, se enlaza
            // al guardar; si es nuevo, se pregunta si registrarlo).
            try
            {
                var acProv = new AutoCompleteStringCollection();
                acProv.AddRange(DatabaseService.ObtenerProveedores()
                    .Select(p => p.Descripcion).Where(d => !string.IsNullOrWhiteSpace(d)).ToArray());
                _txtProveedor.AutoCompleteMode   = AutoCompleteMode.SuggestAppend;
                _txtProveedor.AutoCompleteSource = AutoCompleteSource.CustomSource;
                _txtProveedor.AutoCompleteCustomSource = acProv;
            }
            catch { /* si falla el autocompletar, el campo sigue siendo texto libre */ }
            pnl.Controls.Add(_txtProveedor);
            y += 50;

            // ── Panel Materia Prima (visible solo si categoría EsMateriaPrima) ──
            // Multilínea: se agrega una línea por producto (una factura puede traer
            // varios). Cada línea de MP entra al Kardex por separado al guardar.
            _pnlMP = new Panel
            {
                Location = new Point(0, y), Size = new Size(640, 230),
                BackColor = Color.FromArgb(248, 252, 255),
                BorderStyle = BorderStyle.FixedSingle,
                Visible = false
            };
            int my = 8;
            _pnlMP.Controls.Add(new Label
            {
                Text = "  Materia Prima — cada línea actualiza stock + kardex automáticamente",
                Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
                ForeColor = UITheme.Verde, AutoSize = false,
                Location = new Point(0, my), Size = new Size(640, 16)
            });
            my += 18;
            _pnlMP.Controls.Add(new Label
            {
                Text = "Insumo del catálogo", Font = UITheme.BodyBold,
                Location = new Point(10, my), AutoSize = true, ForeColor = UITheme.AzulOscuro
            });
            _pnlMP.Controls.Add(new Label
            {
                Text = "Cantidad", Font = UITheme.BodyBold,
                Location = new Point(360, my), AutoSize = true, ForeColor = UITheme.AzulOscuro
            });
            _pnlMP.Controls.Add(new Label
            {
                Text = "Precio unit. (USD)", Font = UITheme.BodyBold,
                Location = new Point(455, my), AutoSize = true, ForeColor = UITheme.AzulOscuro
            });
            my += 20;
            _cmbInsumo = new ComboBox
            {
                Location = new Point(10, my), Size = new Size(340, 28),
                DropDownStyle = ComboBoxStyle.DropDownList, Font = UITheme.Body
            };
            _numCantidad = new NumericUpDown
            {
                Location = new Point(360, my), Size = new Size(90, 28),
                Font = new Font("Segoe UI", 11f),
                Maximum = 1_000_000m, DecimalPlaces = 2
            };
            _numPrecioUnit = new NumericUpDown
            {
                Location = new Point(455, my), Size = new Size(100, 28),
                Font = new Font("Segoe UI", 11f),
                Maximum = 1_000_000m, DecimalPlaces = 4
            };
            _btnAddLinea = new Button
            {
                Text = "➕", Location = new Point(560, my), Size = new Size(70, 28),
                FlatStyle = FlatStyle.Flat, BackColor = UITheme.Verde, ForeColor = Color.White,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold), Cursor = Cursors.Hand
            };
            _btnAddLinea.FlatAppearance.BorderSize = 0;
            _btnAddLinea.Click += (s, e) => AgregarLinea();
            _pnlMP.Controls.AddRange(new Control[] { _cmbInsumo, _numCantidad, _numPrecioUnit, _btnAddLinea });
            my += 34;

            _gridLineas = new DataGridView
            {
                Location = new Point(10, my), Size = new Size(620, 120),
                ReadOnly = true, AllowUserToAddRows = false, AllowUserToResizeRows = false,
                RowHeadersVisible = false, BackgroundColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle, MultiSelect = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill, Font = UITheme.Body
            };
            _gridLineas.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Insumo",   FillWeight = 50 });
            _gridLineas.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Cantidad", FillWeight = 15,
                DefaultCellStyle = new DataGridViewCellStyle { Alignment = DataGridViewContentAlignment.MiddleRight } });
            _gridLineas.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "P. Unit.", FillWeight = 17,
                DefaultCellStyle = new DataGridViewCellStyle { Alignment = DataGridViewContentAlignment.MiddleRight } });
            _gridLineas.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Subtotal", FillWeight = 18,
                DefaultCellStyle = new DataGridViewCellStyle { Alignment = DataGridViewContentAlignment.MiddleRight } });
            _pnlMP.Controls.Add(_gridLineas);
            my += 126;

            _btnDelLinea = new Button
            {
                Text = "Quitar línea seleccionada", Location = new Point(10, my), Size = new Size(200, 26),
                FlatStyle = FlatStyle.Flat, BackColor = Color.FromArgb(149,165,166), ForeColor = Color.White,
                Font = new Font("Segoe UI", 8.5f), Cursor = Cursors.Hand
            };
            _btnDelLinea.FlatAppearance.BorderSize = 0;
            _btnDelLinea.Click += (s, e) => QuitarLineaSeleccionada();
            _lblMontoCalc = new Label
            {
                Text = "Agrega una línea por producto de la factura.",
                Font = new Font("Segoe UI", 8.5f, FontStyle.Italic),
                ForeColor = UITheme.TextoTenue, AutoSize = false,
                Location = new Point(220, my + 4), Size = new Size(410, 18)
            };
            _pnlMP.Controls.AddRange(new Control[] { _btnDelLinea, _lblMontoCalc });

            CargarInsumos();
            // Cargar líneas existentes (OCR o edición de un egreso ya guardado).
            if (_egreso.Detalles != null && _egreso.Detalles.Count > 0)
                _lineas.AddRange(_egreso.Detalles);
            else if (!_esNuevo && _egreso.Id > 0)
                _lineas.AddRange(DatabaseService.ObtenerDetallesEgreso(_egreso.Id));
            RefrescarGridLineas();
            pnl.Controls.Add(_pnlMP);
            y += 240;

            // El panel MP se muestra/oculta según categoría
            _cmbCategoria.SelectedIndexChanged += (s, e) => OnCategoriaChanged();

            // ── Sección moneda ─────────────────────────────────────────────
            var lblSec = new Label
            {
                Text = "  Pago — moneda y tasa",
                Font = new Font("Segoe UI", 10f, FontStyle.Bold),
                ForeColor = Color.White, BackColor = UITheme.AzulOscuro,
                Location = new Point(0, y), Size = new Size(640, 28),
                TextAlign = ContentAlignment.MiddleLeft
            };
            pnl.Controls.Add(lblSec);
            y += 38;

            pnl.Controls.Add(new Label { Text = "Tipo de pago", Font = UITheme.BodyBold,
                Location = new Point(0, y), AutoSize = true, ForeColor = UITheme.AzulOscuro });
            pnl.Controls.Add(new Label { Text = "Moneda original", Font = UITheme.BodyBold,
                Location = new Point(330, y), AutoSize = true, ForeColor = UITheme.AzulOscuro });
            y += 22;
            _cmbTipoTasa = new ComboBox { Location = new Point(0, y),
                Size = new Size(310, 28), DropDownStyle = ComboBoxStyle.DropDownList,
                Font = UITheme.Body };
            _cmbTipoTasa.Items.AddRange(new object[] {
                "USD_DIRECTO   →   Pagué en USD efectivo",
                "BCV           →   Pagué en Bs a tasa oficial BCV",
                "PARALELO      →   Pagué en Bs a tasa paralelo",
                "PERSONALIZADA →   Pagué en Bs a tasa específica"
            });
            _cmbTipoTasa.SelectedIndex = _egreso.TipoTasa switch
            {
                "BCV" => 1, "PARALELO" => 2, "PERSONALIZADA" => 3, _ => 0
            };
            _cmbTipoTasa.SelectedIndexChanged += (s, e) => OnTipoTasaChanged();
            _cmbMoneda = new ComboBox { Location = new Point(330, y),
                Size = new Size(310, 28), DropDownStyle = ComboBoxStyle.DropDownList,
                Font = UITheme.Body };
            _cmbMoneda.Items.AddRange(new object[] { "USD — Dólares", "BS — Bolívares" });
            _cmbMoneda.SelectedIndex = _egreso.MonedaOriginal == "BS" ? 1 : 0;
            _cmbMoneda.SelectedIndexChanged += (s, e) => RecalcularConv();
            pnl.Controls.AddRange(new Control[] { _cmbTipoTasa, _cmbMoneda });
            y += 42;

            pnl.Controls.Add(new Label { Text = "Monto", Font = UITheme.BodyBold,
                Location = new Point(0, y), AutoSize = true, ForeColor = UITheme.AzulOscuro });
            pnl.Controls.Add(new Label { Text = "Tasa Bs/USD aplicada", Font = UITheme.BodyBold,
                Location = new Point(220, y), AutoSize = true, ForeColor = UITheme.AzulOscuro });
            y += 22;
            _numMonto = new NumericUpDown { Location = new Point(0, y), Size = new Size(200, 28),
                Font = new Font("Segoe UI", 11f), Maximum = 99_999_999m, DecimalPlaces = 2,
                ThousandsSeparator = true, Value = _egreso.MontoOriginal };
            _numMonto.ValueChanged += (s, e) => RecalcularConv();
            _numTasa = new NumericUpDown { Location = new Point(220, y), Size = new Size(200, 28),
                Font = new Font("Segoe UI", 11f), Maximum = 1_000_000m, DecimalPlaces = 4,
                Value = _egreso.TasaUsada > 0 ? _egreso.TasaUsada : 0m };
            _numTasa.ValueChanged += (s, e) => RecalcularConv();
            pnl.Controls.AddRange(new Control[] { _numMonto, _numTasa });
            y += 38;

            _lblConvUsd = new Label { Location = new Point(0, y), AutoSize = false,
                Size = new Size(310, 24), Font = new Font("Segoe UI", 9.5f, FontStyle.Italic),
                ForeColor = UITheme.Verde };
            _lblConvBs  = new Label { Location = new Point(330, y), AutoSize = false,
                Size = new Size(310, 24), Font = new Font("Segoe UI", 9.5f, FontStyle.Italic),
                ForeColor = Color.FromArgb(83, 74, 183) };
            pnl.Controls.AddRange(new Control[] { _lblConvUsd, _lblConvBs });
            y += 40;

            // ── Precio pendiente (nota de entrega sin montos) ────────────────
            // La mercancía se registra ahora (cantidad + insumo) y el precio lo
            // completa luego el admin; dispara una alerta de "precio pendiente".
            _chkPrecioPendiente = new CheckBox
            {
                Text = "Precio pendiente (nota de entrega sin montos) — el admin lo completa luego",
                Font = UITheme.Body, AutoSize = true,
                Location = new Point(0, y), ForeColor = Color.FromArgb(150, 90, 0),
                Checked = _egreso.PrecioPendiente
            };
            pnl.Controls.Add(_chkPrecioPendiente);
            y += 26;

            // ── Pago / crédito ──────────────────────────────────────────────
            // Si NO se paga completo, el saldo genera una Cuenta por Pagar.
            _chkPendiente = new CheckBox
            {
                Text = "Pago a crédito (parcial o sin pagar) — genera Cuenta por Pagar",
                Font = UITheme.Body, AutoSize = true,
                Location = new Point(0, y), ForeColor = UITheme.AzulOscuro,
                Checked = _egreso.Estado == "Pendiente"
            };
            pnl.Controls.Add(_chkPendiente);
            y += 26;
            pnl.Controls.Add(new Label { Text = "Monto abonado (USD)", Font = UITheme.BodyBold,
                Location = new Point(0, y), AutoSize = true, ForeColor = UITheme.AzulOscuro });
            pnl.Controls.Add(new Label { Text = "Vence el", Font = UITheme.BodyBold,
                Location = new Point(360, y), AutoSize = true, ForeColor = UITheme.AzulOscuro });
            y += 22;
            _numPagado = new NumericUpDown { Location = new Point(0, y), Size = new Size(200, 28),
                Font = new Font("Segoe UI", 11f), Maximum = 99_999_999m, DecimalPlaces = 2,
                ThousandsSeparator = true,
                Value = _egreso.PagadoUSD > 0 ? _egreso.PagadoUSD : 0m,
                Enabled = _chkPendiente.Checked };
            _dtpVence = new DateTimePicker { Location = new Point(360, y),
                Size = new Size(200, 28), Format = DateTimePickerFormat.Short,
                Value = _egreso.FechaVencimiento ?? DateTime.Today.AddDays(30),
                Enabled = _chkPendiente.Checked };
            _chkPendiente.CheckedChanged += (s, e) =>
            {
                _numPagado.Enabled = _chkPendiente.Checked;
                _dtpVence.Enabled  = _chkPendiente.Checked;
            };
            pnl.Controls.AddRange(new Control[] { _numPagado, _dtpVence });
            y += 50;

            // Con "precio pendiente" los montos aún no se conocen: se deshabilitan
            // los campos de monto/tasa/pago para evitar capturar valores inválidos.
            void AplicarPrecioPendiente()
            {
                bool pp = _chkPrecioPendiente.Checked;
                _numMonto.Enabled     = !pp;
                _numTasa.Enabled      = !pp;
                _numPrecioUnit.Enabled= !pp;
                _chkPendiente.Enabled = !pp;
                _numPagado.Enabled    = !pp && _chkPendiente.Checked;
                _dtpVence.Enabled     = !pp && _chkPendiente.Checked;
                if (pp) { _numMonto.Value = 0m; _numPrecioUnit.Value = 0m; }
                if (_gridLineas != null) RefrescarGridLineas();
            }
            _chkPrecioPendiente.CheckedChanged += (s, e) => AplicarPrecioPendiente();
            AplicarPrecioPendiente();

            // ── Comprobante ─────────────────────────────────────────────────
            pnl.Controls.Add(new Label { Text = "Comprobante (foto/PDF, opcional)", Font = UITheme.BodyBold,
                Location = new Point(0, y), AutoSize = true, ForeColor = UITheme.AzulOscuro });
            y += 22;
            _txtComprobante = new TextBox { Location = new Point(0, y),
                Size = new Size(420, 28), Font = UITheme.Body,
                Text = _egreso.RutaComprobante, ReadOnly = true };
            _btnAdjuntar = new Button { Text = "📎 Adjuntar",
                Location = new Point(430, y), Size = new Size(100, 28),
                FlatStyle = FlatStyle.Flat, BackColor = UITheme.AzulHover,
                ForeColor = Color.White, Font = UITheme.Body, Cursor = Cursors.Hand };
            _btnAdjuntar.FlatAppearance.BorderSize = 0;
            _btnAdjuntar.Click += (s, e) => AdjuntarComprobante();
            _btnVerComprobante = new Button { Text = "Ver",
                Location = new Point(540, y), Size = new Size(100, 28),
                FlatStyle = FlatStyle.Flat, BackColor = Color.FromArgb(15, 110, 86),
                ForeColor = Color.White, Font = UITheme.Body, Cursor = Cursors.Hand };
            _btnVerComprobante.FlatAppearance.BorderSize = 0;
            _btnVerComprobante.Click += (s, e) => VerComprobante();
            pnl.Controls.AddRange(new Control[] { _txtComprobante, _btnAdjuntar, _btnVerComprobante });
            y += 50;

            // Observaciones
            pnl.Controls.Add(new Label { Text = "Observaciones", Font = UITheme.BodyBold,
                Location = new Point(0, y), AutoSize = true, ForeColor = UITheme.AzulOscuro });
            y += 22;
            _txtObs = new TextBox { Location = new Point(0, y), Size = new Size(640, 60),
                Font = UITheme.Body, Multiline = true, Text = _egreso.Observaciones };
            pnl.Controls.Add(_txtObs);

            // Mensaje error — fila 2 del root (entre contenido y botonera)
            _lblError = new Label { Dock = DockStyle.Fill,
                ForeColor = UITheme.Rojo, Font = UITheme.BodyBold,
                BackColor = Color.FromArgb(253, 235, 235), TextAlign = ContentAlignment.MiddleCenter,
                Visible = false };
            _rootForm.Controls.Add(_lblError, 0, 2);

            // Botonera — fila 3 del root.
            // Wrapper con fondo blanco + línea superior + FlowLayoutPanel Dock.Right
            // que mantiene los botones SIEMPRE pegados al lado derecho sin importar
            // cómo se redimensione la ventana (Anchor con Location absoluta fallaba).
            var pnlBtn = new Panel { Dock = DockStyle.Fill,
                BackColor = UITheme.FondoCard };
            pnlBtn.Paint += (s, e) =>
            {
                using var pen = new Pen(UITheme.Borde, 1f);
                e.Graphics.DrawLine(pen, 0, 0, pnlBtn.Width, 0);
            };

            // Cancelar (ghost gris)
            var btnCancelar = new Button { Text = "Cancelar",
                FlatStyle = FlatStyle.Flat,
                BackColor = UITheme.FondoCard,
                ForeColor = UITheme.TextoTenue,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                Cursor = Cursors.Hand,
                Width = 110, Height = 36,
                Margin = new Padding(0, 0, 8, 0) };
            btnCancelar.FlatAppearance.BorderColor = UITheme.Borde;
            btnCancelar.FlatAppearance.MouseOverBackColor = Color.FromArgb(238, 240, 245);
            btnCancelar.Click += (s, e) => { DialogResult = DialogResult.Cancel; Close(); };

            // Guardar (primario azul)
            var btnGuardar = new Button { Text = "Guardar egreso",
                FlatStyle = FlatStyle.Flat,
                BackColor = UITheme.Azul, ForeColor = Color.White,
                Font = new Font("Segoe UI", 10f, FontStyle.Bold),
                Cursor = Cursors.Hand,
                Width = 160, Height = 36,
                Margin = new Padding(0, 0, 16, 0) };
            btnGuardar.FlatAppearance.BorderSize = 0;
            btnGuardar.FlatAppearance.MouseOverBackColor = UITheme.AzulHover;
            btnGuardar.Click += (s, e) => Guardar();

            // FlowLayoutPanel Dock.Right que apila los botones derecha → izquierda
            // El primero agregado queda más a la derecha (botón principal)
            var flowBtn = new FlowLayoutPanel
            {
                Dock = DockStyle.Right, AutoSize = true,
                FlowDirection = FlowDirection.RightToLeft,
                BackColor = UITheme.FondoCard,
                WrapContents = false,
                Padding = new Padding(0, 7, 0, 7)
            };
            flowBtn.Controls.Add(btnGuardar);
            flowBtn.Controls.Add(btnCancelar);
            pnlBtn.Controls.Add(flowBtn);
            _rootForm.Controls.Add(pnlBtn, 0, 3);

            // Default button: Enter dispara Guardar
            this.AcceptButton = btnGuardar;
            this.CancelButton = btnCancelar;

            _v = new ValidacionInline(this);
            FormClosed += (s, e) => _v?.Dispose();

            OnTipoTasaChanged();
            OnCategoriaChanged();
        }

        private void CargarCategorias()
        {
            var cats = DatabaseService.ObtenerCategoriasGasto();
            foreach (var c in cats)
                _cmbCategoria.Items.Add(c);
            _cmbCategoria.DisplayMember = "Nombre";
            if (_egreso.IdCategoria > 0)
            {
                var sel = cats.FirstOrDefault(c => c.Id == _egreso.IdCategoria);
                if (sel != null) _cmbCategoria.SelectedItem = sel;
            }
            else if (cats.Count > 0) _cmbCategoria.SelectedIndex = 0;
        }

        private void CargarInsumos()
        {
            try
            {
                var insumos = DatabaseService.ObtenerProductosProveedor(soloActivos: true);
                _cmbInsumo.Items.Clear();
                foreach (var i in insumos)
                    _cmbInsumo.Items.Add(i);
                _cmbInsumo.DisplayMember = "Descripcion";
                if (_egreso.IdInsumo.HasValue)
                {
                    var sel = insumos.FirstOrDefault(i => i.Id == _egreso.IdInsumo.Value);
                    if (sel != null) _cmbInsumo.SelectedItem = sel;
                }
            }
            catch { }
        }

        private void OnCategoriaChanged()
        {
            var cat = _cmbCategoria.SelectedItem as CategoriaGasto;
            bool esMP = cat != null && cat.EsMateriaPrima;
            _pnlMP.Visible = esMP;
            // Si pasó a MP y no hay insumo seleccionado pero hay catálogo, el
            // usuario debe escoger uno antes de guardar. Si pasó de MP a otra
            // categoría, limpiamos los datos de MP del egreso.
            if (!esMP)
            {
                _egreso.IdInsumo = null;
                _egreso.Cantidad = 0;
            }
        }

        // ── Líneas de materia prima (multilínea) ────────────────────────────
        private void AgregarLinea()
        {
            if (_cmbInsumo.SelectedItem is not ProductoProveedor pp)
            {
                MostrarError("Selecciona un insumo del catálogo para la línea.");
                return;
            }
            if (_numCantidad.Value <= 0)
            {
                MostrarError("La cantidad de la línea debe ser mayor que cero.");
                return;
            }
            _lineas.Add(new DetalleEgreso
            {
                IdArticulo     = pp.Id,
                CodigoInterno  = pp.CodigoInterno ?? "",
                Descripcion    = pp.Descripcion ?? "",
                TipoInsumo     = pp.TipoInsumo ?? "Materia prima",
                EsMateriaPrima = true,
                Cantidad       = _numCantidad.Value,
                PrecioUnitario = _chkPrecioPendiente.Checked ? 0m : _numPrecioUnit.Value
            });
            _numCantidad.Value = 0;
            _numPrecioUnit.Value = 0;
            RefrescarGridLineas();
        }

        private void QuitarLineaSeleccionada()
        {
            if (_gridLineas.CurrentRow == null) return;
            int i = _gridLineas.CurrentRow.Index;
            if (i >= 0 && i < _lineas.Count)
            {
                _lineas.RemoveAt(i);
                RefrescarGridLineas();
            }
        }

        private void RefrescarGridLineas()
        {
            // _chkPrecioPendiente puede no existir todavía (esta función corre al
            // construir el panel MP, antes de crear la casilla de precio pendiente).
            bool pp = _chkPrecioPendiente != null && _chkPrecioPendiente.Checked;

            _gridLineas.Rows.Clear();
            foreach (var l in _lineas)
                _gridLineas.Rows.Add(l.Descripcion,
                    l.Cantidad.ToString("N2"),
                    pp ? "—" : l.PrecioUnitario.ToString("N4"),
                    pp ? "—" : ("$ " + l.Subtotal.ToString("N2")));

            decimal total = _lineas.Sum(l => l.Subtotal);
            if (_lineas.Count == 0)
                _lblMontoCalc.Text = "Agrega una línea por producto de la factura.";
            else if (pp)
                _lblMontoCalc.Text = _lineas.Count + " línea(s) · precio pendiente";
            else
            {
                _lblMontoCalc.Text = _lineas.Count + " línea(s) · Total $ " + total.ToString("N2");
                if (_numMonto != null) _numMonto.Value = Math.Min(_numMonto.Maximum, total);
            }
        }

        private void OnTipoTasaChanged()
        {
            string tt = TipoTasaSeleccionado();
            // Auto-llenar tasa según selección
            switch (tt)
            {
                case "USD_DIRECTO":
                    _numTasa.Value   = 0;
                    _numTasa.Enabled = false;
                    _cmbMoneda.SelectedIndex = 0;       // USD forzado
                    _cmbMoneda.Enabled = false;
                    break;
                case "BCV":
                    _numTasa.Value   = Math.Min(_numTasa.Maximum, _tasaBCV);
                    _numTasa.Enabled = false;
                    _cmbMoneda.SelectedIndex = 1;       // BS por defecto
                    _cmbMoneda.Enabled = true;
                    break;
                case "PARALELO":
                    _numTasa.Value   = Math.Min(_numTasa.Maximum, _tasaParalelo);
                    _numTasa.Enabled = false;
                    _cmbMoneda.SelectedIndex = 1;
                    _cmbMoneda.Enabled = true;
                    break;
                case "PERSONALIZADA":
                    _numTasa.Enabled = true;            // editable
                    if (_numTasa.Value <= 0) _numTasa.Value = Math.Min(_numTasa.Maximum, _tasaBCV);
                    _cmbMoneda.SelectedIndex = 1;
                    _cmbMoneda.Enabled = true;
                    break;
            }
            RecalcularConv();
        }

        private string TipoTasaSeleccionado()
        {
            int i = _cmbTipoTasa.SelectedIndex;
            return i switch { 1 => "BCV", 2 => "PARALELO", 3 => "PERSONALIZADA", _ => "USD_DIRECTO" };
        }

        private void RecalcularConv()
        {
            try
            {
                var g = new Egreso
                {
                    MonedaOriginal = _cmbMoneda.SelectedIndex == 1 ? "BS" : "USD",
                    TipoTasa       = TipoTasaSeleccionado(),
                    TasaUsada      = _numTasa.Value,
                    MontoOriginal  = _numMonto.Value
                };
                DatabaseService.CalcularMontos(g);
                _lblConvUsd.Text = "= USD " + g.MontoUSD.ToString("N2");
                _lblConvBs.Text  = g.MontoBs > 0 ? "= Bs " + g.MontoBs.ToString("N2") : "";
            }
            catch (Exception ex)
            {
                _lblConvUsd.Text = "(" + ex.Message + ")";
                _lblConvBs.Text  = "";
            }
        }

        private void AdjuntarComprobante()
        {
            using var ofd = new OpenFileDialog
            {
                Title = "Selecciona el comprobante (foto o PDF)",
                Filter = "Imágenes y PDF|*.jpg;*.jpeg;*.png;*.bmp;*.pdf|Todos|*.*"
            };
            if (ofd.ShowDialog(this) != DialogResult.OK) return;
            try
            {
                var carpeta = Path.Combine(SecurityService.CarpetaDatos, "comprobantes");
                Directory.CreateDirectory(carpeta);
                string ext = Path.GetExtension(ofd.FileName);
                string destino = Path.Combine(carpeta,
                    "gasto_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ext);
                File.Copy(ofd.FileName, destino, true);
                _txtComprobante.Text = destino;
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, "No se pudo adjuntar: " + ex.Message,
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void VerComprobante()
        {
            string ruta = _txtComprobante.Text;
            if (string.IsNullOrEmpty(ruta) || !File.Exists(ruta))
            {
                MessageBox.Show(this, "No hay comprobante adjunto.", "Comprobante",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            try
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = ruta, UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, "No se pudo abrir: " + ex.Message, "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void Guardar()
        {
            _v.Limpiar();
            _v.Requerido(_txtConcepto, "Concepto requerido")
              .Custom(_cmbCategoria, _cmbCategoria.SelectedItem != null, "Selecciona una categoría");
            if (!_v.EsValido) return;

            bool precioPendiente = _chkPrecioPendiente.Checked;
            var cat = _cmbCategoria.SelectedItem as CategoriaGasto;
            bool esMP = cat != null && cat.EsMateriaPrima;

            // Validación adicional para Materia Prima (multilínea)
            if (esMP)
            {
                // Si el usuario dejó datos en la fila de captura sin pulsar "➕",
                // los tomamos como una línea más (conveniencia).
                if (_cmbInsumo.SelectedItem is ProductoProveedor ppPend && _numCantidad.Value > 0)
                    AgregarLinea();

                if (_lineas.Count == 0)
                {
                    MostrarError("Agrega al menos una línea de insumo (insumo + cantidad, y precio si lo hay).");
                    _cmbInsumo.Focus(); return;
                }
            }
            // El monto se exige solo para egresos que NO son materia prima
            // (en MP el monto sale de la suma de las líneas).
            else if (!precioPendiente && _numMonto.Value <= 0)
            {
                MostrarError("El monto debe ser mayor que cero.");
                _numMonto.Focus(); return;
            }

            if (!esMP && precioPendiente)
            {
                // Precio pendiente sin materia prima: solo tiene sentido documental.
                if (MessageBox.Show(this,
                        "Marcaste 'precio pendiente' pero la categoría no es materia prima.\n" +
                        "Se registrará el egreso sin monto y quedará como pendiente de precio.\n\n¿Continuar?",
                        "Precio pendiente", MessageBoxButtons.YesNo, MessageBoxIcon.Question)
                    != DialogResult.Yes) return;
            }
            try
            {
                _egreso.Fecha          = _dtpFecha.Value.Date;
                _egreso.IdCategoria    = cat!.Id;
                _egreso.Concepto       = _txtConcepto.Text.Trim();
                // ── Proveedor: enlazar al directorio o registrar si es nuevo ──────
                string nomProv = _txtProveedor.Text.Trim();
                _egreso.Proveedor       = nomProv;
                _egreso.CodigoProveedor = "";
                if (!string.IsNullOrEmpty(nomProv))
                {
                    // Match tolerante: ignora mayúsculas, acentos, puntuación y
                    // sufijos jurídicos (C.A, S.A, SRL). Así "INVERSIONES VALENKZO, C.A"
                    // empareja con "inversiones valenkzo" del directorio.
                    var existente = DatabaseService.BuscarProveedorPorNombre(nomProv);
                    if (existente != null)
                    {
                        _egreso.CodigoProveedor = existente.Codigo;
                        _egreso.Proveedor       = existente.Descripcion;   // normaliza el nombre
                    }
                    else
                    {
                        var resp = MessageBox.Show(this,
                            "El proveedor \"" + nomProv + "\" no está registrado en el directorio.\n\n" +
                            "¿Deseas registrarlo ahora? (podrás completar sus datos luego en Proveedores)",
                            "Proveedor nuevo", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question);
                        if (resp == DialogResult.Cancel) return;   // volver al formulario
                        if (resp == DialogResult.Yes)
                        {
                            var nuevo = new Proveedor
                            {
                                Codigo = DatabaseService.ProximoCodigoProveedor(),
                                Descripcion = nomProv, FechaInicio = DateTime.Today,
                                Representante = "", Direccion = "", Telefono1 = "", Telefono2 = "",
                                Fax1 = "", Fax2 = "", CiRif = "", Nit = "", Tipo = "", Observaciones = ""
                            };
                            DatabaseService.InsertarProveedor(nuevo);
                            _egreso.CodigoProveedor = nuevo.Codigo;
                        }
                        // Si respondió No: se guarda el nombre como texto libre, sin enlazar.
                    }
                }
                _egreso.MonedaOriginal = _cmbMoneda.SelectedIndex == 1 ? "BS" : "USD";
                _egreso.TipoTasa       = TipoTasaSeleccionado();
                _egreso.TasaUsada      = _numTasa.Value;
                _egreso.MontoOriginal  = _numMonto.Value;
                _egreso.PrecioPendiente = precioPendiente;
                // Pago: a crédito → se abona lo indicado (USD) y el saldo genera CxP;
                // completo → GuardarEgreso iguala PagadoUSD al total. El estado y la
                // Cuenta por Pagar se derivan en GuardarEgreso a partir de PagadoUSD.
                if (precioPendiente)
                {
                    // Sin montos aún; GuardarEgreso deja el egreso en 'Pendiente' de precio.
                    _egreso.MontoOriginal    = 0m;
                    _egreso.PagadoUSD        = 0m;
                    _egreso.Estado           = "Pendiente";
                    _egreso.FechaVencimiento = _dtpVence.Value.Date;
                    _egreso.FechaPago        = null;
                }
                else if (_chkPendiente.Checked)
                {
                    _egreso.PagadoUSD        = _numPagado.Value;
                    _egreso.Estado           = "Pendiente";
                    _egreso.FechaVencimiento = _dtpVence.Value.Date;
                    _egreso.FechaPago        = _numPagado.Value > 0 ? DateTime.Today : (DateTime?)null;
                }
                else
                {
                    _egreso.PagadoUSD        = 0m;   // GuardarEgreso lo iguala al total
                    _egreso.Estado           = "Pagado";
                    _egreso.FechaVencimiento = null;
                    _egreso.FechaPago        = DateTime.Today;
                }
                _egreso.RutaComprobante = _txtComprobante.Text.Trim();
                _egreso.Observaciones  = _txtObs.Text.Trim();
                // MP: capturar las líneas (una factura puede traer varios productos).
                if (cat.EsMateriaPrima)
                {
                    // Si es precio pendiente, forzamos precio 0 en las líneas.
                    if (precioPendiente)
                        foreach (var l in _lineas) l.PrecioUnitario = 0m;
                    _egreso.Detalles = new System.Collections.Generic.List<DetalleEgreso>(_lineas);
                }
                else
                {
                    _egreso.Detalles = new System.Collections.Generic.List<DetalleEgreso>();
                    _egreso.IdInsumo = null;
                    _egreso.Cantidad = 0m;
                }

                int id = DatabaseService.GuardarEgreso(_egreso);
                _egreso.Id = id;
                EgresoGuardado = _egreso;
                DialogResult = DialogResult.OK;
                Close();
            }
            catch (UnauthorizedAccessException ex)
            {
                MessageBox.Show(this, ex.Message, "Permiso denegado",
                    MessageBoxButtons.OK, MessageBoxIcon.Stop);
            }
            catch (Exception ex)
            {
                MostrarError(ex.Message);
            }
        }

        private void MostrarError(string m) { _lblError.Text = "  " + m; _lblError.Visible = true; }
    }
}
