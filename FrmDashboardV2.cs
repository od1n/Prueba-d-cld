// ════════════════════════════════════════════════════════════════════════
// ARCHIVO: FrmDashboardV2.cs  →  raíz del proyecto
//
// Rediseño Dashboard híbrido C+B según mockup aprobado:
//   - Header denso (KPIs en línea, BCV + paralelo + timestamp)
//   - Panel izquierdo fijo de alertas (con barra lateral por severidad)
//   - Centro: gráfica rotativa (1 de 4) con botón 📌 para fijar
//   - Inferior: tabla de últimas facturas
//   - Paleta clara corporativa, codificación de color por categoría en KPIs
//
// Persistencia preferencia rotación:
//   "DashGraficaPin"   → indice de gráfica fijada (-1 = rotativo)
//   "DashGraficaIdx"   → indice actual (0..3)
//   "DashGraficaFecha" → ultima fecha en que rotó
// ════════════════════════════════════════════════════════════════════════

#nullable enable
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Windows.Forms;

namespace SistemaGestion
{
    public class FrmDashboardV2 : FormBase
    {
        // ── Paleta (claro, alineado a UITheme + propuestas mockup) ──────────
        private static readonly Color CFondo     = Color.FromArgb(244, 246, 250);
        private static readonly Color CSurf      = Color.White;
        private static readonly Color CBorde     = Color.FromArgb(215, 222, 230);
        private static readonly Color CTexto     = Color.FromArgb(30,  34,  51);
        private static readonly Color CTextoTen  = Color.FromArgb(122, 131, 149);
        private static readonly Color CAzul      = Color.FromArgb(26,  74, 138);
        private static readonly Color CAzulOsc   = Color.FromArgb(15,  45,  85);
        private static readonly Color CCyan      = Color.FromArgb(0,  180, 216);
        private static readonly Color COk        = Color.FromArgb(15, 110,  86);
        private static readonly Color CWarn      = Color.FromArgb(210, 153,  34);
        private static readonly Color CDang      = Color.FromArgb(198,  40,  40);
        private static readonly Color CMorado    = Color.FromArgb(83,  74, 183);
        private static readonly Color CHoverBg   = Color.FromArgb(31,  58,  95);

        // ── Catálogo de gráficas rotativas ──────────────────────────────────
        private static readonly string[] _nombresGraficas = {
            "Ventas diarias del mes",
            "Ventas mensuales (último año)",
            "Top 10 productos vendidos",
            "Estado de facturas / CxC"
        };
        private int  _idxGrafica = 0;
        private bool _fijada     = false;

        // ── Datos cargados ──────────────────────────────────────────────────
        private DashboardData? _data;

        // ── Controles persistidos ───────────────────────────────────────────
        private GraficaPanel _pnlGrafica = null!;
        private Label       _lblTituloGrafica = null!;
        private Label       _lblDots = null!;
        private Button      _btnPin = null!;
        private Label       _lblBcvVal = null!, _lblParaVal = null!, _lblBcvSello = null!;
        private FlowLayoutPanel _pnlAlertas = null!;
        private DataGridView _dgvFacturas = null!;
        private Label       _lblVentasHoy = null!, _lblVentasMes = null!,
                            _lblPend = null!, _lblStock = null!, _lblCxC = null!;

        // ── Panel de análisis IA (colapsable) ───────────────────────────────
        private Panel  _pnlIA       = null!;
        private Label  _lblIATexto  = null!;
        private Button _btnAnalizarIA = null!;
        private bool   _iaVisible   = false;

        public FrmDashboardV2()
        {
            Text          = "Dashboard — Resumen del Negocio";
            Size          = new Size(1320, 800);
            MinimumSize   = new Size(900, 550);
            StartPosition = FormStartPosition.CenterParent;
            BackColor     = CFondo;
            // Tipografia base mas grande (de 9.5 a 10pt) para legibilidad
            this.Font = new Font("Segoe UI", 10f);
            ConstruirUI();
            CargarPreferenciaRotacion();
            Load += async (s, e) => await CargarDatosAsync();
        }

        // ── Construcción UI ─────────────────────────────────────────────────
        private void ConstruirUI()
        {
            var root = new TableLayoutPanel
            {
                Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 4,
                BackColor = CFondo
            };
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 42f));   // navbar (igual menu)
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 76f));   // strip KPIs
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));   // contenido
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 50f));   // bottom (cerrar)

            root.Controls.Add(ConstruirNavbar(),  0, 0);
            root.Controls.Add(ConstruirStripKPIs(), 0, 1);
            root.Controls.Add(ConstruirContenido(), 0, 2);
            root.Controls.Add(ConstruirBottomBar(), 0, 3);
            Controls.Add(root);
        }

        // Header consistente con MenuPrincipalV2 (logo + acciones + user + cerrar)
        private Panel ConstruirNavbar()
        {
            var p = new Panel { Dock = DockStyle.Fill, BackColor = CAzulOsc };

            var lblLogo = new Label
            {
                Text = "  ⚙  SistemaGestion",
                Font = new Font("Segoe UI", 11f, FontStyle.Bold),
                ForeColor = Color.White, BackColor = CAzulOsc,
                AutoSize = false, TextAlign = ContentAlignment.MiddleLeft,
                Dock = DockStyle.Left, Width = 220
            };

            var btnVolver = new Button
            {
                Text = "← Volver al menú",
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(120, 30, 30),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                Cursor = Cursors.Hand,
                Width = 150, Dock = DockStyle.Right
            };
            btnVolver.FlatAppearance.BorderSize = 0;
            btnVolver.FlatAppearance.MouseOverBackColor = Color.FromArgb(160, 40, 40);
            btnVolver.Click += (s, e) => Close();

            var lblUser = new Label
            {
                Text = (Sesion.NombreCompleto ?? Sesion.NombreUsuario ?? "") +
                       "  ·  " + (Sesion.Rol ?? "—") + "  ",
                Font = new Font("Segoe UI", 9.5f),
                ForeColor = Color.FromArgb(168, 177, 187), BackColor = CAzulOsc,
                AutoSize = false, TextAlign = ContentAlignment.MiddleRight,
                Padding = new Padding(0, 0, 12, 0),
                Width = 260, Dock = DockStyle.Right
            };

            // Orden Add: el ULTIMO con Dock.Right queda mas a la derecha
            p.Controls.Add(lblLogo);
            p.Controls.Add(lblUser);    // intermedio
            p.Controls.Add(btnVolver);  // rightmost
            return p;
        }

        private Panel ConstruirStripKPIs()
        {
            var p = new Panel { Dock = DockStyle.Fill, BackColor = CSurf };
            var fila = new TableLayoutPanel
            {
                Dock = DockStyle.Fill, RowCount = 1, ColumnCount = 9,
                BackColor = CSurf, Padding = new Padding(8, 6, 8, 6)
            };
            // KPIs proporcionales + botones fijos (responsive a cualquier tamaño)
            fila.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 14f));   // Ventas Hoy
            fila.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 13f));   // Mes
            fila.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,  9f));   // Pend
            fila.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,  9f));   // Stk
            fila.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 13f));   // CxC
            fila.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 26f));   // BCV/Par
            fila.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 16f));   // spacer
            fila.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 120f)); // btn IA
            fila.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 130f)); // btn Actualizar

            fila.Controls.Add(KpiCell("VENTAS HOY",  out _lblVentasHoy, "$ 0,00",  CAzul),    0, 0);
            fila.Controls.Add(KpiCell("MES",         out _lblVentasMes, "$ 0,00",  CAzul),    1, 0);
            fila.Controls.Add(KpiCell("PEND",        out _lblPend,      "0",       CWarn),    2, 0);
            fila.Controls.Add(KpiCell("STK",         out _lblStock,     "0",       CDang),    3, 0);
            fila.Controls.Add(KpiCell("CxC",         out _lblCxC,       "$ 0,00",  CAzul),    4, 0);
            fila.Controls.Add(KpiBcvParalelo(),                                              5, 0);
            fila.Controls.Add(new Panel { BackColor = CSurf },                                6, 0);
            fila.Controls.Add(BotonAnalizarIA(),                                              7, 0);
            fila.Controls.Add(BotonActualizar(),                                              8, 0);

            p.Controls.Add(fila);
            // borde inferior
            p.Paint += (s, e) => e.Graphics.DrawLine(new Pen(CBorde),
                0, p.Height - 1, p.Width, p.Height - 1);
            return p;
        }

        private Panel KpiCell(string etiqueta, out Label lblValor, string valorIni, Color barra)
        {
            var p = new Panel { Dock = DockStyle.Fill, BackColor = CSurf,
                Padding = new Padding(10, 4, 6, 4) };
            var bar = new Panel { Dock = DockStyle.Left, Width = 3, BackColor = barra };
            var lblEt = new Label { Text = etiqueta, Font = new Font("Segoe UI", 8f, FontStyle.Bold),
                ForeColor = CTextoTen, AutoSize = false, Dock = DockStyle.Top, Height = 16,
                Padding = new Padding(8, 0, 0, 0) };
            // Valor mas grande (12pt) para legibilidad
            var lblVal = new Label { Text = valorIni, Font = new Font("Segoe UI", 13f, FontStyle.Bold),
                ForeColor = CTexto, AutoSize = false, Dock = DockStyle.Fill,
                Padding = new Padding(8, 2, 0, 0) };
            lblValor = lblVal;
            p.Controls.Add(lblVal); p.Controls.Add(lblEt); p.Controls.Add(bar);
            return p;
        }

        private Panel KpiBcvParalelo()
        {
            var p = new Panel { Dock = DockStyle.Fill, BackColor = CSurf,
                Padding = new Padding(10, 2, 6, 2) };
            var bar = new Panel { Dock = DockStyle.Left, Width = 3, BackColor = COk };

            // Fila de valores: altura fija = 1 línea, imposible que wrappee
            var rowVals = new Panel { Dock = DockStyle.Fill, BackColor = CSurf };

            var lblB = new Label { Text = "B", Font = new Font("Segoe UI", 8f, FontStyle.Bold),
                ForeColor = CTextoTen, Dock = DockStyle.Left, Width = 20, AutoSize = false,
                TextAlign = ContentAlignment.MiddleCenter };
            _lblBcvVal = new Label { Text = "—", Font = new Font("Segoe UI", 13f, FontStyle.Bold),
                ForeColor = CTexto, Dock = DockStyle.Left, Width = 70, AutoSize = false,
                TextAlign = ContentAlignment.MiddleLeft };
            var lblP = new Label { Text = "P", Font = new Font("Segoe UI", 8f, FontStyle.Bold),
                ForeColor = CTextoTen, Dock = DockStyle.Left, Width = 20, AutoSize = false,
                TextAlign = ContentAlignment.MiddleCenter };
            _lblParaVal = new Label { Text = "—", Font = new Font("Segoe UI", 13f, FontStyle.Bold),
                ForeColor = CMorado, Dock = DockStyle.Fill, AutoSize = false,
                TextAlign = ContentAlignment.MiddleLeft };

            // Dock order: Fill primero, luego Left en orden inverso
            rowVals.Controls.Add(_lblParaVal);  // Fill → toma lo que sobra
            rowVals.Controls.Add(lblP);          // Left
            rowVals.Controls.Add(_lblBcvVal);    // Left
            rowVals.Controls.Add(lblB);          // Left

            // Sello de frescura (fila inferior fija)
            _lblBcvSello = new Label { Text = "sin tasa hoy",
                Font = new Font("Segoe UI", 8f, FontStyle.Italic), ForeColor = CWarn,
                Dock = DockStyle.Bottom, Height = 16, AutoSize = false,
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(6, 0, 0, 0) };

            // Orden: Fill (rowVals) primero, Bottom (sello) después
            p.Controls.Add(rowVals);
            p.Controls.Add(_lblBcvSello);
            p.Controls.Add(bar);
            return p;
        }

        private Button BotonActualizar()
        {
            // Anclado horizontal con altura fija; texto completo legible.
            var b = new Button {
                Text = "↻  Actualizar",
                Anchor = AnchorStyles.Left | AnchorStyles.Right,
                Height = 38, Width = 150,
                FlatStyle = FlatStyle.Flat, BackColor = CAzul, ForeColor = Color.White,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold), Cursor = Cursors.Hand,
                Margin = new Padding(4, 13, 12, 13),
                TextAlign = ContentAlignment.MiddleCenter
            };
            b.FlatAppearance.BorderSize = 0;
            b.FlatAppearance.MouseOverBackColor = CHoverBg;
            b.Click += async (s, e) => await CargarDatosAsync();
            return b;
        }

        private Panel ConstruirContenido()
        {
            // Panel contenedor: IA colapsable arriba + contenido principal abajo
            var wrapper = new Panel { Dock = DockStyle.Fill, BackColor = CFondo };

            _pnlIA = ConstruirPanelIA();
            _pnlIA.Dock    = DockStyle.Top;
            _pnlIA.Height  = 0;
            _pnlIA.Visible = false;

            var t = new TableLayoutPanel
            {
                Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 2,
                BackColor = CFondo, Padding = new Padding(8)
            };
            t.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 240f));
            t.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,  100f));
            t.RowStyles.Add(new RowStyle(SizeType.Percent, 60f));
            t.RowStyles.Add(new RowStyle(SizeType.Percent, 40f));

            t.Controls.Add(ConstruirPanelAlertas(), 0, 0);
            t.SetRowSpan(t.GetControlFromPosition(0, 0)!, 2);

            t.Controls.Add(ConstruirPanelGrafica(), 1, 0);
            t.Controls.Add(ConstruirPanelFacturas(), 1, 1);

            // Orden crítico en WinForms: Fill primero, luego Top
            wrapper.Controls.Add(t);
            wrapper.Controls.Add(_pnlIA);
            return wrapper;
        }

        // ── Botón Analizar IA ───────────────────────────────────────────────
        private Button BotonAnalizarIA()
        {
            _btnAnalizarIA = new Button
            {
                Text      = "🤖 Analizar",
                Anchor    = AnchorStyles.Left | AnchorStyles.Right,
                Height    = 38,
                FlatStyle = FlatStyle.Flat,
                BackColor = CMorado,
                ForeColor = Color.White,
                Font      = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                Cursor    = Cursors.Hand,
                Margin    = new Padding(4, 13, 4, 13),
                TextAlign = ContentAlignment.MiddleCenter
            };
            _btnAnalizarIA.FlatAppearance.BorderSize = 0;
            _btnAnalizarIA.FlatAppearance.MouseOverBackColor = Color.FromArgb(60, 52, 145);
            _btnAnalizarIA.Click += async (s, e) => await AnalizarConIAAsync();
            return _btnAnalizarIA;
        }

        // ── Panel IA colapsable ─────────────────────────────────────────────
        private Panel ConstruirPanelIA()
        {
            var p = new Panel
            {
                BackColor = Color.FromArgb(240, 244, 255),
                Padding   = new Padding(14, 8, 14, 8)
            };
            // Línea inferior separadora
            p.Paint += (s, e) =>
            {
                using var pen = new Pen(Color.FromArgb(190, 210, 245), 1f);
                e.Graphics.DrawLine(pen, 0, p.Height - 1, p.Width, p.Height - 1);
            };

            // Barra de título
            var header = new Panel
            {
                Dock = DockStyle.Top, Height = 22,
                BackColor = Color.Transparent
            };
            var lblTit = new Label
            {
                Text      = "🤖  Análisis IA — basado en los KPIs actuales",
                Font      = new Font("Segoe UI", 9f, FontStyle.Bold),
                ForeColor = CAzul,
                Dock      = DockStyle.Left,
                AutoSize  = true,
                TextAlign = ContentAlignment.MiddleLeft
            };
            var lblCerrar = new Label
            {
                Text      = "✕",
                Font      = new Font("Segoe UI", 9f, FontStyle.Bold),
                ForeColor = CTextoTen,
                Cursor    = Cursors.Hand,
                Dock      = DockStyle.Right,
                Width     = 24,
                TextAlign = ContentAlignment.MiddleCenter
            };
            lblCerrar.Click += (s, e) => OcultarPanelIA();
            header.Controls.Add(lblTit);
            header.Controls.Add(lblCerrar);

            // Texto de respuesta
            _lblIATexto = new Label
            {
                Text      = "",
                Font      = new Font("Segoe UI", 9.5f),
                ForeColor = CTexto,
                Dock      = DockStyle.Fill,
                AutoSize  = false,
                TextAlign = ContentAlignment.TopLeft,
                Padding   = new Padding(2, 4, 2, 0)
            };

            // Orden: Fill primero, luego Top
            p.Controls.Add(_lblIATexto);
            p.Controls.Add(header);
            return p;
        }

        // ── Análisis asíncrono con Ollama ───────────────────────────────────
        private async System.Threading.Tasks.Task AnalizarConIAAsync()
        {
            if (_data is null)
            {
                MessageBox.Show(this,
                    "Primero actualiza los datos del dashboard (botón ↻ Actualizar).",
                    "Sin datos", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            MostrarPanelIA("⏳  Consultando Ollama, un momento…");
            _btnAnalizarIA.Enabled = false;

            try
            {
                bool disponible = await OllamaService.IsAvailableAsync();
                if (!disponible)
                {
                    _lblIATexto.Text =
                        "⚠  Ollama no está disponible en http://localhost:11434.\n\n" +
                        "Asegúrate de que el servidor Ollama esté corriendo y de que " +
                        "tengas al menos un modelo descargado (ej: ollama pull llama3).";
                    return;
                }

                string modelo  = DatabaseService.ObtenerConfig("OllamaModelo", "llama3");
                string empresa = DatabaseService.ObtenerConfig("NombreEmpresa", "la empresa");

                // Construir contexto de alertas
                string alertasTxt = _data.Alertas.Count == 0
                    ? "ninguna"
                    : string.Join(", ", _data.Alertas.Select(a =>
                        $"{a.Categoria} ({a.Texto})"));

                var prompt =
                    $"Eres un asistente de negocios para una empresa venezolana llamada \"{empresa}\". " +
                    $"Analiza los siguientes indicadores clave (KPIs) del día de hoy y proporciona " +
                    $"un análisis conciso en español con recomendaciones prácticas.\n\n" +
                    $"KPIs actuales:\n" +
                    $"- Ventas del día: $ {_data.VentasHoy:N2}\n" +
                    $"- Ventas del mes: $ {_data.VentasMes:N2}\n" +
                    $"- Facturas pendientes de cobro: {_data.FacturasPendientes}\n" +
                    $"- Productos con stock bajo: {_data.StockBajo}\n" +
                    $"- Cuentas por cobrar vencidas: $ {_data.CxCSaldo:N2}\n" +
                    $"- Tasa BCV: {(_data.TasaBCV > 0 ? _data.TasaBCV.ToString("N2") + " Bs/USD" : "no disponible")}\n" +
                    $"- Tasa paralelo: {(_data.TasaParalelo > 0 ? _data.TasaParalelo.ToString("N2") + " Bs/USD" : "no disponible")}\n" +
                    $"- Alertas activas: {alertasTxt}\n\n" +
                    $"Proporciona:\n" +
                    $"1. Diagnóstico breve del estado actual (2-3 oraciones)\n" +
                    $"2. Las 2-3 acciones más urgentes para hoy\n" +
                    $"3. Un punto positivo si lo hay\n\n" +
                    $"Sé directo, práctico y usa el contexto venezolano (dólares como moneda de referencia). " +
                    $"Máximo 180 palabras.";

                string respuesta = await OllamaService.GenerateAsync(modelo, prompt, 400);
                _lblIATexto.Text = respuesta;

                // Si la respuesta es larga, ampliar el panel
                if (respuesta.Length > 300 && _pnlIA.Height < 170)
                {
                    _pnlIA.Height = 170;
                }
            }
            catch (Exception ex)
            {
                _lblIATexto.Text =
                    $"⚠  Error al contactar Ollama: {ex.Message}\n\n" +
                    $"Verifica que el servicio esté activo y que el modelo " +
                    $"\"{DatabaseService.ObtenerConfig("OllamaModelo", "llama3")}\" esté descargado.";
            }
            finally
            {
                _btnAnalizarIA.Enabled = true;
            }
        }

        private void MostrarPanelIA(string textoInicial)
        {
            _lblIATexto.Text = textoInicial;
            if (!_iaVisible)
            {
                _iaVisible    = true;
                _pnlIA.Height = 130;
                _pnlIA.Visible = true;
            }
        }

        private void OcultarPanelIA()
        {
            _iaVisible     = false;
            _pnlIA.Visible = false;
            _pnlIA.Height  = 0;
        }

        // ── Panel: alertas ──────────────────────────────────────────────────
        private Panel ConstruirPanelAlertas()
        {
            var p = TarjetaConTitulo("Alertas activas", out var content);
            _pnlAlertas = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown,
                WrapContents = false, AutoScroll = true,
                BackColor = CSurf, Padding = new Padding(0)
            };
            content.Controls.Add(_pnlAlertas);
            return p;
        }

        // ── Panel: grafica rotativa ─────────────────────────────────────────
        private Panel ConstruirPanelGrafica()
        {
            var p = new Panel { Dock = DockStyle.Fill, Margin = new Padding(8, 0, 0, 8) };

            // Header con titulo + dots + pin
            var header = new Panel { Dock = DockStyle.Top, Height = 36, BackColor = CSurf };
            _lblTituloGrafica = new Label
            {
                Text = _nombresGraficas[_idxGrafica],
                Font = new Font("Segoe UI", 11f, FontStyle.Bold),
                ForeColor = CTexto, Dock = DockStyle.Fill,
                AutoSize = false, TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(12, 0, 0, 0)
            };
            _lblDots = new Label
            {
                Text = "● ○ ○ ○",
                Font = new Font("Segoe UI", 10f),
                ForeColor = CAzul, Dock = DockStyle.Left, Width = 90,
                AutoSize = false, TextAlign = ContentAlignment.MiddleCenter
            };
            _btnPin = new Button
            {
                Text = "📌 Fijar", Dock = DockStyle.Right, Width = 90,
                FlatStyle = FlatStyle.Flat, BackColor = CSurf, ForeColor = CAzul,
                Font = new Font("Segoe UI", 9f), Cursor = Cursors.Hand,
                Margin = new Padding(4)
            };
            _btnPin.FlatAppearance.BorderColor = CBorde;
            _btnPin.Click += (s, e) => ToggleFijar();

            var btnNext = new Button
            {
                Text = "›", Dock = DockStyle.Right, Width = 36,
                FlatStyle = FlatStyle.Flat, BackColor = CSurf, ForeColor = CTexto,
                Font = new Font("Segoe UI", 12f, FontStyle.Bold), Cursor = Cursors.Hand
            };
            btnNext.FlatAppearance.BorderColor = CBorde;
            btnNext.Click += (s, e) => CambiarGrafica(+1);

            var btnPrev = new Button
            {
                Text = "‹", Dock = DockStyle.Right, Width = 36,
                FlatStyle = FlatStyle.Flat, BackColor = CSurf, ForeColor = CTexto,
                Font = new Font("Segoe UI", 12f, FontStyle.Bold), Cursor = Cursors.Hand
            };
            btnPrev.FlatAppearance.BorderColor = CBorde;
            btnPrev.Click += (s, e) => CambiarGrafica(-1);

            header.Controls.Add(_lblTituloGrafica);
            header.Controls.Add(_lblDots);
            header.Controls.Add(_btnPin);
            header.Controls.Add(btnNext);
            header.Controls.Add(btnPrev);

            // Subclase Panel con DoubleBuffer real + estilos de pintado
            // que evitan acumulacion de trazos al redimensionar/cambiar.
            _pnlGrafica = new GraficaPanel { Dock = DockStyle.Fill, BackColor = CSurf };
            _pnlGrafica.Paint += PintarGrafica;
            // Forzar invalidacion COMPLETA al resize, no parcial
            _pnlGrafica.Resize += (s, e) => _pnlGrafica.Invalidate();

            var contenedor = new Panel { Dock = DockStyle.Fill,
                BackColor = CSurf, Padding = new Padding(12) };
            contenedor.Controls.Add(_pnlGrafica);
            contenedor.Controls.Add(header);

            // Borde
            var wrap = new Panel { Dock = DockStyle.Fill, BackColor = CBorde,
                Padding = new Padding(1) };
            wrap.Controls.Add(contenedor);
            p.Controls.Add(wrap);
            return p;
        }

        // ── Panel: ultimas facturas ─────────────────────────────────────────
        private Panel ConstruirPanelFacturas()
        {
            var p = TarjetaConTitulo("Últimas facturas", out var content);
            p.Margin = new Padding(8, 0, 0, 0);
            _dgvFacturas = new DataGridView
            {
                Dock = DockStyle.Fill, ReadOnly = true, AllowUserToAddRows = false,
                AllowUserToResizeRows = false, RowHeadersVisible = false,
                BackgroundColor = CSurf, BorderStyle = BorderStyle.None,
                EnableHeadersVisualStyles = false, Font = new Font("Segoe UI", 10f),
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect, MultiSelect = false,
                AlternatingRowsDefaultCellStyle = { BackColor = Color.FromArgb(250, 251, 253) }
            };
            _dgvFacturas.ColumnHeadersDefaultCellStyle.BackColor = CAzulOsc;
            _dgvFacturas.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
            _dgvFacturas.ColumnHeadersDefaultCellStyle.Font =
                new Font("Segoe UI", 9.5f, FontStyle.Bold);
            _dgvFacturas.ColumnHeadersHeight = 32;
            _dgvFacturas.RowTemplate.Height = 30;
            content.Controls.Add(_dgvFacturas);
            return p;
        }

        // ── Tarjeta con titulo (helper) ─────────────────────────────────────
        // ── Bottom bar CLARO con buen contraste (estilo unificado) ─────────
        private Panel ConstruirBottomBar()
        {
            var p = new Panel { Dock = DockStyle.Fill, BackColor = UITheme.FondoCard };
            p.Paint += (s, e) =>
            {
                using var pen = new Pen(UITheme.Borde, 1f);
                e.Graphics.DrawLine(pen, 0, 0, p.Width, 0);
            };

            var lblHint = new Label
            {
                Text = "  Tip: ↻ Actualizar refresca todos los datos · 📌 Fija/libera la gráfica",
                Font = new Font("Segoe UI", 9.5f),
                ForeColor = UITheme.TextoTenue,
                BackColor = UITheme.FondoCard,
                AutoSize = false, Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(20, 0, 0, 0)
            };

            var btnCerrar = new Button
            {
                Text = "Cerrar",
                FlatStyle = FlatStyle.Flat,
                BackColor = UITheme.Azul,
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 10f, FontStyle.Bold),
                Cursor = Cursors.Hand,
                Width = 130, Height = 36,
                Margin = new Padding(0, 7, 16, 7)
            };
            btnCerrar.FlatAppearance.BorderSize = 0;
            btnCerrar.FlatAppearance.MouseOverBackColor = UITheme.AzulHover;
            btnCerrar.Click += (s, e) => Close();

            var flowR = new FlowLayoutPanel
            {
                Dock = DockStyle.Right,
                FlowDirection = FlowDirection.RightToLeft,
                BackColor = UITheme.FondoCard,
                Width = 170,
                WrapContents = false,
                Padding = new Padding(0)
            };
            flowR.Controls.Add(btnCerrar);

            // Orden importante: Dock=Fill primero, Dock=Right al final
            p.Controls.Add(lblHint);
            p.Controls.Add(flowR);
            return p;
        }

        private Panel TarjetaConTitulo(string titulo, out Panel content)
        {
            var wrap = new Panel { Dock = DockStyle.Fill, BackColor = CBorde,
                Padding = new Padding(1) };
            var inner = new Panel { Dock = DockStyle.Fill, BackColor = CSurf };
            var head = new Panel { Dock = DockStyle.Top, Height = 28,
                BackColor = Color.FromArgb(244, 246, 250) };
            head.Controls.Add(new Label
            {
                Text = "  " + titulo,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                ForeColor = CTexto, Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft
            });
            content = new Panel { Dock = DockStyle.Fill, BackColor = CSurf };
            inner.Controls.Add(content); inner.Controls.Add(head);
            wrap.Controls.Add(inner);
            return wrap;
        }

        // ── Carga de datos ──────────────────────────────────────────────────
        private async System.Threading.Tasks.Task CargarDatosAsync()
        {
            try
            {
                this.UseWaitCursor = true;
                _data = await System.Threading.Tasks.Task.Run(() => DashboardData.Cargar());
                AplicarDatos();
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, "Error al cargar dashboard: " + ex.Message,
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally { this.UseWaitCursor = false; }
        }

        private void AplicarDatos()
        {
            if (_data is null) return;

            _lblVentasHoy.Text = "$ " + _data.VentasHoy.ToString("N2");
            _lblVentasMes.Text = "$ " + _data.VentasMes.ToString("N2");
            _lblPend.Text      = _data.FacturasPendientes.ToString();
            _lblStock.Text     = _data.StockBajo.ToString();
            _lblCxC.Text       = "$ " + _data.CxCSaldo.ToString("N2");
            _lblBcvVal.Text    = _data.TasaBCV > 0 ? _data.TasaBCV.ToString("N2") : "—";
            _lblParaVal.Text   = _data.TasaParalelo > 0 ? _data.TasaParalelo.ToString("N2") : "—";

            // Sello de frescura BCV
            if (_data.FechaTasa == DateTime.MinValue)
            {
                _lblBcvSello.Text = "sin tasa registrada";
                _lblBcvSello.ForeColor = CDang;
            }
            else
            {
                int dias = (DateTime.Today - _data.FechaTasa.Date).Days;
                if (dias == 0)      { _lblBcvSello.Text = "● actualizada hoy";      _lblBcvSello.ForeColor = COk; }
                else if (dias <= 2) { _lblBcvSello.Text = $"● {dias} día(s) atrás"; _lblBcvSello.ForeColor = CWarn; }
                else                { _lblBcvSello.Text = $"● {dias} días atrás";   _lblBcvSello.ForeColor = CDang; }
            }

            // Alertas
            _pnlAlertas.Controls.Clear();
            foreach (var a in _data.Alertas)
                _pnlAlertas.Controls.Add(BloqueAlerta(a));
            if (_data.Alertas.Count == 0)
                _pnlAlertas.Controls.Add(new Label
                {
                    Text = "Sin alertas activas", AutoSize = false,
                    Width = 230, Height = 60, Font = new Font("Segoe UI", 9.5f, FontStyle.Italic),
                    ForeColor = COk, TextAlign = ContentAlignment.MiddleCenter
                });

            // Facturas
            _dgvFacturas.DataSource = _data.UltimasFacturas
                .Select(f => new {
                    Numero  = f.Numero,
                    Cliente = f.Cliente,
                    Monto   = f.Monto.ToString("C2"),
                    Estado  = f.Estado
                }).ToList();

            _pnlGrafica.Invalidate();
        }

        private Panel BloqueAlerta(AlertaItem a)
        {
            var color = a.Severidad switch {
                "alta" => CDang, "media" => CWarn, _ => CTextoTen };
            var p = new Panel
            {
                Width = _pnlAlertas.ClientSize.Width - 20, Height = 60,
                Margin = new Padding(8, 4, 8, 4),
                BackColor = CSurf
            };
            var bar = new Panel { Dock = DockStyle.Left, Width = 3, BackColor = color };
            var lbl1 = new Label
            {
                Text = a.Categoria, Font = new Font("Segoe UI", 7.5f, FontStyle.Bold),
                ForeColor = color, AutoSize = false, Dock = DockStyle.Top, Height = 14,
                Padding = new Padding(8, 0, 0, 0)
            };
            var lbl2 = new Label
            {
                Text = a.Texto, Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                ForeColor = CTexto, AutoSize = false, Dock = DockStyle.Top, Height = 18,
                Padding = new Padding(8, 0, 0, 0)
            };
            var lbl3 = new Label
            {
                Text = a.Detalle, Font = new Font("Segoe UI", 8f),
                ForeColor = CTextoTen, AutoSize = false, Dock = DockStyle.Top, Height = 14,
                Padding = new Padding(8, 0, 0, 0)
            };
            p.Controls.Add(lbl3); p.Controls.Add(lbl2); p.Controls.Add(lbl1); p.Controls.Add(bar);
            return p;
        }

        // ── Rotación / fijar ────────────────────────────────────────────────
        private void CargarPreferenciaRotacion()
        {
            int pin = int.TryParse(DatabaseService.ObtenerConfig("DashGraficaPin", "-1"), out var p) ? p : -1;
            int idx = int.TryParse(DatabaseService.ObtenerConfig("DashGraficaIdx", "0"), out var i) ? i : 0;
            string fechaStr = DatabaseService.ObtenerConfig("DashGraficaFecha", "");
            DateTime ultima = DateTime.TryParse(fechaStr, out var f) ? f.Date : DateTime.MinValue;

            if (pin >= 0 && pin < _nombresGraficas.Length)
            {
                _idxGrafica = pin; _fijada = true;
            }
            else
            {
                _fijada = false;
                if (ultima.Date != DateTime.Today)
                {
                    _idxGrafica = (idx + 1) % _nombresGraficas.Length;
                    DatabaseService.GuardarConfig("DashGraficaIdx", _idxGrafica.ToString());
                    DatabaseService.GuardarConfig("DashGraficaFecha", DateTime.Today.ToString("yyyy-MM-dd"));
                }
                else _idxGrafica = idx;
            }
            ActualizarHeaderGrafica();
        }

        private void ToggleFijar()
        {
            _fijada = !_fijada;
            DatabaseService.GuardarConfig("DashGraficaPin", _fijada ? _idxGrafica.ToString() : "-1");
            ActualizarHeaderGrafica();
        }

        private void CambiarGrafica(int delta)
        {
            _idxGrafica = (_idxGrafica + delta + _nombresGraficas.Length) % _nombresGraficas.Length;
            if (_fijada)
                DatabaseService.GuardarConfig("DashGraficaPin", _idxGrafica.ToString());
            else
                DatabaseService.GuardarConfig("DashGraficaIdx", _idxGrafica.ToString());
            ActualizarHeaderGrafica();
            _pnlGrafica.Invalidate();
        }

        private void ActualizarHeaderGrafica()
        {
            _lblTituloGrafica.Text = _nombresGraficas[_idxGrafica];
            // Dots: ● activa, ○ inactivas
            var dots = "";
            for (int i = 0; i < _nombresGraficas.Length; i++)
                dots += (i == _idxGrafica ? "●" : "○") + " ";
            _lblDots.Text = dots.Trim();
            _btnPin.Text = _fijada ? "📌 Fijada" : "📌 Fijar";
            _btnPin.BackColor = _fijada ? Color.FromArgb(232, 240, 250) : CSurf;
        }

        // ── Pintado de gráficas ─────────────────────────────────────────────
        private void PintarGrafica(object? sender, PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.Clear(CSurf);
            if (_data is null)
            {
                using var br = new SolidBrush(CTextoTen);
                g.DrawString("Cargando…", new Font("Segoe UI", 10f, FontStyle.Italic),
                    br, _pnlGrafica.Width / 2 - 30, _pnlGrafica.Height / 2);
                return;
            }
            switch (_idxGrafica)
            {
                case 0: PintarLinea(g, _data.VentasDiariasMes, "$ ");        break;
                case 1: PintarBarras(g, _data.VentasMensuales);              break;
                case 2: PintarBarrasH(g, _data.TopProductos);                break;
                case 3: PintarDonut(g, _data.EstadoFacturas);                break;
            }
        }

        private void PintarLinea(Graphics g, List<(string lbl, decimal val)> datos, string prefijo)
        {
            if (datos.Count == 0) { DibujarVacio(g); return; }
            int W = _pnlGrafica.Width, H = _pnlGrafica.Height;
            int padL = Math.Max(40, W * 7 / 100);
            int padR = Math.Max(15, W * 3 / 100);
            int padT = 20, padB = Math.Max(24, H * 10 / 100);
            float maxV = (float)Math.Max(1m, datos.Max(d => d.val));
            int n = datos.Count;
            using var penLine = new Pen(CAzul, 2);
            using var penGrid = new Pen(Color.FromArgb(240, 242, 246), 1);
            using var brArea  = new SolidBrush(Color.FromArgb(30, CAzul));
            using var brLbl   = new SolidBrush(CTextoTen);
            using var brVal   = new SolidBrush(CTexto);
            var fontLbl = new Font("Segoe UI", 7.5f);
            for (int i = 0; i <= 4; i++)
            {
                int y = padT + (H - padT - padB) * i / 4;
                g.DrawLine(penGrid, padL, y, W - padR, y);
                float v = maxV * (4 - i) / 4f;
                g.DrawString(prefijo + v.ToString("N0"), fontLbl, brLbl, 4, y - 8);
            }
            // path area + linea
            var pts = new PointF[n];
            for (int i = 0; i < n; i++)
            {
                float x = padL + (W - padL - padR) * i / Math.Max(1f, n - 1);
                float y = padT + (H - padT - padB) * (1 - (float)datos[i].val / maxV);
                pts[i] = new PointF(x, y);
            }
            // area
            var area = new GraphicsPath();
            area.AddLine(pts[0].X, H - padB, pts[0].X, pts[0].Y);
            area.AddLines(pts);
            area.AddLine(pts[n - 1].X, pts[n - 1].Y, pts[n - 1].X, H - padB);
            area.CloseFigure();
            g.FillPath(brArea, area);
            g.DrawLines(penLine, pts);
            // ultimo punto destacado
            using var brPt = new SolidBrush(CAzul);
            g.FillEllipse(brPt, pts[n - 1].X - 4, pts[n - 1].Y - 4, 8, 8);
            g.DrawString(prefijo + ((float)datos[n - 1].val).ToString("N0"),
                new Font("Segoe UI", 9f, FontStyle.Bold), brVal,
                pts[n - 1].X - 30, pts[n - 1].Y - 22);
            // labels eje x (cada 5)
            for (int i = 0; i < n; i += Math.Max(1, n / 6))
                g.DrawString(datos[i].lbl, fontLbl, brLbl, pts[i].X - 8, H - padB + 4);
        }

        private void PintarBarras(Graphics g, List<(string lbl, decimal val)> datos)
        {
            if (datos.Count == 0) { DibujarVacio(g); return; }
            int W = _pnlGrafica.Width, H = _pnlGrafica.Height;
            int padL = Math.Max(40, W * 7 / 100);
            int padR = Math.Max(15, W * 3 / 100);
            int padT = 20, padB = Math.Max(24, H * 10 / 100);
            float maxV = (float)Math.Max(1m, datos.Max(d => d.val));
            int n = datos.Count;
            float bw = (W - padL - padR) / (float)(n * 1.5f);
            using var brBar = new SolidBrush(CAzul);
            using var brBarSoft = new SolidBrush(Color.FromArgb(150, CAzul));
            using var brLbl = new SolidBrush(CTextoTen);
            using var penGrid = new Pen(Color.FromArgb(240, 242, 246), 1);
            var fontLbl = new Font("Segoe UI", 7.5f);
            for (int i = 0; i <= 4; i++)
            {
                int y = padT + (H - padT - padB) * i / 4;
                g.DrawLine(penGrid, padL, y, W - padR, y);
                float v = maxV * (4 - i) / 4f;
                g.DrawString("$ " + v.ToString("N0"), fontLbl, brLbl, 4, y - 8);
            }
            for (int i = 0; i < n; i++)
            {
                float x = padL + (W - padL - padR) * (i + 0.25f) / n;
                float h = (H - padT - padB) * (float)datos[i].val / maxV;
                g.FillRectangle(i == n - 1 ? brBar : brBarSoft, x, H - padB - h, bw, h);
                g.DrawString(datos[i].lbl, fontLbl, brLbl, x, H - padB + 4);
            }
        }

        private void PintarBarrasH(Graphics g, List<(string lbl, decimal val)> datos)
        {
            if (datos.Count == 0) { DibujarVacio(g); return; }
            int W = _pnlGrafica.Width, H = _pnlGrafica.Height;
            int padL = Math.Max(60, Math.Min(180, W * 20 / 100));
            int padR = Math.Max(40, Math.Min(100, W * 10 / 100));
            int padT = 12, padB = 12;
            float maxV = (float)Math.Max(1m, datos.Max(d => d.val));
            int n = Math.Min(10, datos.Count);
            float rh = (H - padT - padB) / (float)n - 4;
            using var brBar = new SolidBrush(CAzul);
            using var brLbl = new SolidBrush(CTexto);
            using var brVal = new SolidBrush(CTextoTen);
            var fontLbl = new Font("Segoe UI", 8.5f);
            using var sfTrunc = new StringFormat { Trimming = StringTrimming.EllipsisCharacter,
                FormatFlags = StringFormatFlags.NoWrap, LineAlignment = StringAlignment.Center };
            // Pase 1: dibujar todas las barras
            var posiciones = new (float y, float w)[n];
            for (int i = 0; i < n; i++)
            {
                float y = padT + i * (rh + 4);
                float w = (W - padL - padR) * (float)datos[i].val / maxV;
                posiciones[i] = (y, w);
                g.FillRectangle(brBar, padL, y, Math.Max(w, 2), rh);
            }
            // Pase 2: fondo limpio + clip duro para que texto NUNCA se monte sobre las barras
            using var brBg = new SolidBrush(CSurf);
            for (int i = 0; i < n; i++)
            {
                var (y, w) = posiciones[i];
                // Borrar zona de etiqueta (fondo blanco sobre la barra)
                g.FillRectangle(brBg, 0, y, padL - 2, rh);
                // Clip region: texto solo puede renderizar en la zona de etiqueta
                var clipGuardado = g.Clip.Clone();
                g.SetClip(new RectangleF(0, y - 1, padL - 2, rh + 2),
                    System.Drawing.Drawing2D.CombineMode.Intersect);
                g.DrawString(datos[i].lbl, fontLbl, brLbl, 4, y + (rh - 14) / 2);
                g.Clip = clipGuardado;
                clipGuardado.Dispose();
                // Valor a la derecha de la barra
                g.DrawString(((float)datos[i].val).ToString("N0"), fontLbl, brVal,
                    padL + w + 4, y + (rh - 12) / 2);
            }
        }

        private void PintarDonut(Graphics g, List<(string lbl, decimal val, Color col)> datos)
        {
            if (datos.Count == 0 || datos.Sum(d => d.val) <= 0) { DibujarVacio(g); return; }
            int W = _pnlGrafica.Width, H = _pnlGrafica.Height;
            float total = (float)datos.Sum(d => d.val);
            // Reservar espacio para leyenda a la derecha
            int legendW = Math.Max(140, W * 22 / 100);
            int availW = W - legendW;
            int size = Math.Min(availW, H) - 40;
            if (size < 80) size = 80;
            int cx = (availW - size) / 2;
            int cy = (H - size) / 2;
            float ang = -90;
            for (int i = 0; i < datos.Count; i++)
            {
                float sw = (float)datos[i].val / total * 360f;
                using var br = new SolidBrush(datos[i].col);
                g.FillPie(br, cx, cy, size, size, ang, sw);
                ang += sw;
            }
            using var brHole = new SolidBrush(CSurf);
            g.FillEllipse(brHole, cx + size / 4, cy + size / 4, size / 2, size / 2);
            // Texto central centrado correctamente
            using var brTotal = new SolidBrush(CTexto);
            using var sfCenter = new StringFormat { Alignment = StringAlignment.Center,
                LineAlignment = StringAlignment.Center };
            g.DrawString(total.ToString("N0"),
                new Font("Segoe UI", Math.Max(12f, size / 10f), FontStyle.Bold), brTotal,
                new RectangleF(cx + size / 4, cy + size / 4, size / 2, size / 2), sfCenter);
            // Leyenda en la zona reservada
            int lx = availW + 10, ly = cy + 10;
            var fontLbl = new Font("Segoe UI", 9f);
            using var brTxt = new SolidBrush(CTexto);
            int lineH = Math.Max(20, (H - 40) / Math.Max(1, datos.Count));
            if (lineH > 28) lineH = 28;
            for (int i = 0; i < datos.Count; i++)
            {
                using var br = new SolidBrush(datos[i].col);
                g.FillRectangle(br, lx, ly + i * lineH, 12, 12);
                g.DrawString(datos[i].lbl + ": " + ((float)datos[i].val).ToString("N0"),
                    fontLbl, brTxt, lx + 18, ly + i * lineH - 2);
            }
        }

        private void DibujarVacio(Graphics g)
        {
            using var br = new SolidBrush(CTextoTen);
            g.DrawString("Sin datos para mostrar",
                new Font("Segoe UI", 10f, FontStyle.Italic), br,
                _pnlGrafica.Width / 2 - 80, _pnlGrafica.Height / 2 - 6);
        }
    }

    /// <summary>
    /// Panel con DoubleBuffer real + estilos de pintado que evitan
    /// acumulacion al redimensionar (era el bug del eje X duplicado).
    /// </summary>
    internal sealed class GraficaPanel : Panel
    {
        public GraficaPanel()
        {
            SetStyle(ControlStyles.OptimizedDoubleBuffer
                   | ControlStyles.AllPaintingInWmPaint
                   | ControlStyles.UserPaint
                   | ControlStyles.ResizeRedraw, true);
            UpdateStyles();
        }
    }

    // ════════════════════════════════════════════════════════════════════════
    //  Datos para el Dashboard
    // ════════════════════════════════════════════════════════════════════════
    public class AlertaItem
    {
        public string Categoria { get; set; } = "";
        public string Texto     { get; set; } = "";
        public string Detalle   { get; set; } = "";
        public string Severidad { get; set; } = "media"; // alta / media / baja
    }

    public class FacturaResumen
    {
        public string  Numero  { get; set; } = "";
        public string  Cliente { get; set; } = "";
        public decimal Monto   { get; set; }
        public string  Estado  { get; set; } = "";
    }

    public class DashboardData
    {
        public decimal VentasHoy, VentasMes, CxCSaldo;
        public int     FacturasPendientes, StockBajo;
        public decimal TasaBCV, TasaParalelo;
        public DateTime FechaTasa;
        public List<AlertaItem> Alertas = new();
        public List<FacturaResumen> UltimasFacturas = new();
        public List<(string lbl, decimal val)> VentasDiariasMes = new();
        public List<(string lbl, decimal val)> VentasMensuales = new();
        public List<(string lbl, decimal val)> TopProductos = new();
        public List<(string lbl, decimal val, Color col)> EstadoFacturas = new();

        public static DashboardData Cargar()
        {
            var d = new DashboardData();
            try
            {
                var hoy = DateTime.Today;
                // Tasa actual
                var t = DatabaseService.ObtenerTasaHoy();
                if (t != null) { d.TasaBCV = t.TasaBCV; d.TasaParalelo = t.TasaParalelo; d.FechaTasa = t.Fecha; }

                // Resumen global (1 query)
                var rsm = DatabaseService.ObtenerResumenGlobal();
                d.FacturasPendientes = rsm.Facturas;     // adaptamos: total facturado lo sacamos de query separada
                d.StockBajo = rsm.BajoMin;

                // Ventas hoy / mes / CxC con queries especificas (pequenas)
                var fActHoy = DatabaseService.ObtenerFacturasPorPeriodo(hoy, hoy);
                d.VentasHoy = fActHoy.Sum(f => f.TotalF);

                var inicioMes = new DateTime(hoy.Year, hoy.Month, 1);
                var fMes = DatabaseService.ObtenerFacturasPorPeriodo(inicioMes, hoy);
                d.VentasMes = fMes.Sum(f => f.TotalF);
                d.FacturasPendientes = fMes.Count(f => (f.TotalF - f.Abono) > 0);

                var cxc = DatabaseService.ObtenerCuentasFiltrado(null, true);
                d.CxCSaldo = cxc.Sum(c => c.Saldo);

                // Ventas diarias del mes
                d.VentasDiariasMes = fMes
                    .GroupBy(f => f.Fecha.Day)
                    .Select(g => (g.Key.ToString(), g.Sum(f => f.TotalF)))
                    .OrderBy(x => int.Parse(x.Item1))
                    .ToList();

                // Ventas mensuales (12 meses)
                int[] anos = { hoy.Year, hoy.Year - 1 };
                var mensuales = DatabaseService.ObtenerVentasMensualesPorAnos(anos)
                    .OrderBy(x => x.Ano).ThenBy(x => x.Mes)
                    .TakeLast(12).ToList();
                string[] mn = { "Ene","Feb","Mar","Abr","May","Jun","Jul","Ago","Sep","Oct","Nov","Dic" };
                d.VentasMensuales = mensuales.Select(x => (mn[x.Mes - 1], x.Total)).ToList();

                // Top productos del periodo (este año)
                var lineas = DatabaseService.ObtenerLineasVentaPorPeriodo(
                    new DateTime(hoy.Year, 1, 1), hoy);
                d.TopProductos = lineas
                    .GroupBy(l => l.Descripcion ?? l.Codigo)
                    .Select(g => (g.Key, g.Sum(x => x.Cantidad)))
                    .OrderByDescending(x => x.Item2)
                    .Take(10).ToList();

                // Estado facturas (pagadas/pendientes/vencidas)
                int pagadas = fMes.Count(f => f.Abono >= f.TotalF);
                int pend    = fMes.Count(f => f.Abono < f.TotalF);
                int venc    = cxc.Count;
                d.EstadoFacturas = new List<(string, decimal, Color)>
                {
                    ("Pagadas",   pagadas, Color.FromArgb(15,110,86)),
                    ("Pendientes",pend,    Color.FromArgb(210,153,34)),
                    ("Vencidas",  venc,    Color.FromArgb(198,40,40))
                };

                // Alertas (top 5)
                d.Alertas = new List<AlertaItem>();
                if (d.StockBajo > 0)
                    d.Alertas.Add(new AlertaItem {
                        Categoria = "STOCK BAJO",
                        Texto = d.StockBajo + " productos bajo mínimo",
                        Detalle = "Revisa el módulo de inventario",
                        Severidad = "alta"
                    });
                if (cxc.Count > 0)
                    d.Alertas.Add(new AlertaItem {
                        Categoria = "CxC VENCIDAS",
                        Texto = cxc.Count + " cuentas vencidas",
                        Detalle = "Total: $" + d.CxCSaldo.ToString("N2"),
                        Severidad = "media"
                    });
                if (d.FechaTasa == DateTime.MinValue || (DateTime.Today - d.FechaTasa.Date).Days > 2)
                    d.Alertas.Add(new AlertaItem {
                        Categoria = "TASA BCV",
                        Texto = "Tasa desactualizada",
                        Detalle = d.FechaTasa == DateTime.MinValue
                                  ? "Sin tasa registrada"
                                  : "Última: " + d.FechaTasa.ToString("dd/MM"),
                        Severidad = "media"
                    });

                // Ultimas facturas
                d.UltimasFacturas = fMes
                    .OrderByDescending(f => f.Fecha)
                    .Take(8)
                    .Select(f => new FacturaResumen {
                        Numero  = f.NumeroF,
                        Cliente = f.Codigo ?? "—",
                        Monto   = f.TotalF,
                        Estado  = f.Abono >= f.TotalF ? "● Pagada" :
                                  f.Abono > 0 ? "● Parcial" : "● Pendiente"
                    }).ToList();
            }
            catch (Exception ex)
            {
                Console.WriteLine("[Dashboard] WARN " + ex.Message);
            }
            return d;
        }
    }
}
