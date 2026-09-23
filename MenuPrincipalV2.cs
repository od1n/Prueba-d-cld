// ════════════════════════════════════════════════════════════════════════
// ARCHIVO: MenuPrincipalV2.cs  →  raíz del proyecto
//
// Rediseno del MenuPrincipal alineado con el estilo C+B del Dashboard V2:
//   - Header oscuro denso (32px) con logo + saludo + acciones
//   - Strip de info (64px) con BCV/Paralelo + sello frescura + alertas resumidas
//   - Centro: grid de modulos con tarjetas modernizadas (paleta clara,
//     barra lateral de color por modulo, hover sutil)
//   - Bottom bar con accesos secundarios (Reportes, Tasa, Sistema, Salir)
//
// Mantiene toda la funcionalidad del MenuPrincipal original:
//   - Permisos por rol (Admin / Ventas / Almacen / Contabilidad)
//   - Atajo F3 buscador, perfil, ayuda, cerrar sesion
//   - Carga clientes de prueba si BD esta vacia
//   - Backup al cerrar sesion
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
    public class MenuPrincipalV2 : FormBase
    {
        // Paleta clara unificada con FrmDashboardV2
        private static readonly Color CFondo    = Color.FromArgb(223, 230, 241); // = UINeo.Ground
        private static readonly Color CSurf     = Color.White;
        private static readonly Color CBorde    = Color.FromArgb(215, 222, 230);
        private static readonly Color CTexto    = Color.FromArgb(30,  34,  51);
        private static readonly Color CTextoTen = Color.FromArgb(122, 131, 149);
        private static readonly Color CAzul     = Color.FromArgb(26,  74, 138);
        private static readonly Color CAzulOsc  = Color.FromArgb(15,  45,  85);
        private static readonly Color CAzulSec  = Color.FromArgb(45, 108, 192);
        private static readonly Color CCyan     = Color.FromArgb(0,  180, 216);
        private static readonly Color COk       = Color.FromArgb(15, 110,  86);
        private static readonly Color CWarn     = Color.FromArgb(210, 153,  34);
        private static readonly Color CDang     = Color.FromArgb(198,  40,  40);
        private static readonly Color CMorado   = Color.FromArgb(83,  74, 183);

        // Controles persistidos
        private Label _lblBcv = null!, _lblPara = null!, _lblBcvSello = null!;
        private Button _btnToggleNE = null!;
        private Panel  _pnlBannerNE = null!;
        private Label _lblAlertas = null!;
        private Panel _pnlGrid   = null!;

        // ── Toast de alertas críticas (overlay flotante) ────────────────────
        private Panel _pnlToast      = null!;
        private bool  _toastMostrado = false;

        // ── Panel de chat IA lateral ────────────────────────────────────────
        private Panel       _pnlChat       = null!;
        private RichTextBox _rtfChat       = null!;
        private TextBox     _txtChatInput  = null!;
        private Button      _btnToggleChat = null!;
        private bool        _chatVisible   = false;
        private readonly List<(string rol, string texto)> _historialChat = new();

        public MenuPrincipalV2()
        {
            Text          = "Sistema de Gestión";
            Size          = new Size(1100, 720);
            MinimumSize   = new Size(900, 600);
            StartPosition = FormStartPosition.CenterScreen;
            FormBorderStyle = FormBorderStyle.Sizable;
            BackColor     = CFondo;

            ConstruirUI();
            VentanaPrefs.Restaurar(this);

            var timerPrefs = new System.Windows.Forms.Timer { Interval = 800 };
            timerPrefs.Tick     += (s, e) => { timerPrefs.Stop(); VentanaPrefs.Guardar(this); };
            SizeChanged         += (s, e) => { timerPrefs.Stop(); timerPrefs.Start(); };
            LocationChanged     += (s, e) => { timerPrefs.Stop(); timerPrefs.Start(); };
            FormClosed          += (s, e) => { timerPrefs.Stop(); timerPrefs.Dispose(); };

            KeyPreview = true;
            KeyDown += (s, ev) => { if (ev.KeyCode == Keys.F3) new FrmBuscador().ShowDialog(); };

            Load += async (s, e) =>
            {
                RefrescarInfo();
                // Intentar actualizar tasa BCV en silencio si no hay una para hoy.
                // Si tiene éxito refresca los labels; si falla no hace nada.
                bool guardada = await TasaCambioService.ActualizarSilenciosoAsync(
                    Sesion.NombreUsuario ?? "auto");
                if (guardada) RefrescarInfo();
            };
        }

        // ── Construccion UI ─────────────────────────────────────────────────
        private void ConstruirUI()
        {
            _toastMostrado = false;   // resetear al reconstruir (nuevo login)

            var root = new TableLayoutPanel
            {
                Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 5,
                BackColor = CFondo
            };
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 50f));   // navbar
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 28f));   // banner NE (oculto por default)
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 96f));   // strip info
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));   // grid
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 64f));   // bottom

            root.Controls.Add(ConstruirNavbar(),    0, 0);
            root.Controls.Add(ConstruirBannerNE(),  0, 1);
            root.Controls.Add(ConstruirStripInfo(), 0, 2);
            root.Controls.Add(ConstruirContenido(), 0, 3);
            root.Controls.Add(ConstruirBottomBar(), 0, 4);
            Controls.Add(root);

            // Toast flotante de alertas críticas (overlay sobre el TableLayout)
            _pnlToast = ConstruirToastAlertas();
            Controls.Add(_pnlToast);
            _pnlToast.BringToFront();
            SizeChanged += (s, e) => { if (_pnlToast?.Visible == true) PosicionarToast(); };

            // Sincronizar el estado inicial del banner
            ActualizarBannerNE();
        }

        // Banner permanente cuando MostrarNotasEntrega = true.
        // Imposible olvidarlo encendido — está en cada pantalla, ámbar fuerte.
        private Panel ConstruirBannerNE()
        {
            _pnlBannerNE = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(210, 130, 30),  // ámbar advertencia
                Visible = Sesion.MostrarNotasEntrega
            };
            _pnlBannerNE.Paint += (s, e) =>
            {
                var g = e.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;
                using var f = new Font("Segoe UI", 9.5f, FontStyle.Bold);
                g.DrawString(
                    "🔓  VISTA DE NOTAS DE ENTREGA ACTIVA — los reportes están INCLUYENDO documentos sin valor fiscal.  " +
                    "Se desactiva sola al cerrar sesión.",
                    f, Brushes.White, 12, 6);
            };
            return _pnlBannerNE;
        }

        private Panel ConstruirNavbar()
        {
            var p = new Panel { Dock = DockStyle.Fill, BackColor = UINeo.Ground };
            p.Paint += (s, e) =>
            {
                using var pen = new Pen(Color.FromArgb(120, UINeo.ShadowDark), 1f);
                e.Graphics.DrawLine(pen, 0, p.Height - 1, p.Width, p.Height - 1);
            };

            var lblLogo = new Label
            {
                Text = "  ⚙  SistemaGestion",
                Font = new Font("Segoe UI", 11f, FontStyle.Bold),
                ForeColor = CAzul, BackColor = Color.Transparent,
                AutoSize = false, TextAlign = ContentAlignment.MiddleLeft,
                Dock = DockStyle.Left, Width = 220
            };

            // Boton Cerrar sesion - Dock.Right primero (queda mas a la derecha)
            var btnLogout = new Button
            {
                Text = "Cerrar sesión",
                Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
                Cursor = Cursors.Hand,
                Width = 130, Dock = DockStyle.Right
            };
            UINeo.BotonAccento(btnLogout, UITheme.Rojo, Color.FromArgb(150, 30, 30), 8);
            btnLogout.Click += BtnCerrarSesion_Click;

            // Label de usuario - Dock.Right despues (queda a la izq del boton)
            var lblUser = new Label
            {
                Text = (Sesion.NombreCompleto ?? Sesion.NombreUsuario ?? "") +
                       "  ·  " + (Sesion.Rol ?? "—") + "  ",
                Font = new Font("Segoe UI", 9f),
                ForeColor = UINeo.InkSoft, BackColor = Color.Transparent,
                AutoSize = false, TextAlign = ContentAlignment.MiddleRight,
                Padding = new Padding(0, 0, 12, 0),
                Width = 240, Dock = DockStyle.Right
            };

            // Botones de acciones - centro
            var pnlAcc = new FlowLayoutPanel
            {
                Dock = DockStyle.Right, AutoSize = false,
                FlowDirection = FlowDirection.LeftToRight,
                BackColor = UINeo.Ground, Padding = new Padding(0, 8, 8, 8),
                WrapContents = false, Width = 360
            };
            pnlAcc.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                UINeo.SombrasHijos(e.Graphics, pnlAcc, 9);
            };
            pnlAcc.Layout += (s, e) => pnlAcc.Invalidate();
            pnlAcc.Controls.Add(BotonNav("F3 Buscar",   () => new FrmBuscador().ShowDialog()));
            pnlAcc.Controls.Add(BotonNav("Mi perfil",   () => new FrmPerfil().ShowDialog()));
            pnlAcc.Controls.Add(BotonNav("?  Ayuda",    () => AyudaService.Mostrar(this, "MenuPrincipal")));

            // ── Toggle de visibilidad de Notas de Entrega (solo Admin) ────
            // Cuando está OFF (default), reportes y dashboard ignoran las NE.
            // Cuando está ON, se incluyen — pero el botón cambia a rojo y
            // aparece un banner permanente debajo del navbar para que sea
            // imposible olvidar que está activo.
            if (Sesion.EsAdmin)
            {
                _btnToggleNE = BotonNav("Vista NE: OFF", null);
                _btnToggleNE.Width = 110;
                _btnToggleNE.Click += (s, e) => ToggleVistaNE();
                pnlAcc.Controls.Add(_btnToggleNE);
                pnlAcc.Width = 480;  // espacio para el botón extra
                ActualizarBotonNE();
            }

            // En WinForms, con multiples Dock.Right, el ULTIMO agregado queda
            // mas a la derecha. Orden deseado: [Logo] ... [pnlAcc] [user] [logout]
            p.Controls.Add(lblLogo);   // Dock.Left
            p.Controls.Add(pnlAcc);    // Dock.Right (primero -> mas a la izquierda del grupo derecho)
            p.Controls.Add(lblUser);   // Dock.Right (intermedio)
            p.Controls.Add(btnLogout); // Dock.Right (ultimo -> mas a la derecha) ✓
            return p;
        }

        // accion es nullable: pasa null si vas a registrar el handler aparte
        // (caso del botón Vista NE: ON/OFF que se engancha externamente).
        private Button BotonNav(string texto, Action? accion)
        {
            var b = new Button
            {
                Text = texto,
                Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
                Cursor = Cursors.Hand,
                Width = 100, Height = 30,
                Margin = new Padding(4, 0, 4, 0),
                TextAlign = ContentAlignment.MiddleCenter
            };
            UINeo.BotonSuave(b, UINeo.Ink, 9);
            if (accion != null) b.Click += (s, e) => accion();
            return b;
        }

        // ── Toggle de visibilidad de Notas de Entrega ────────────────────
        // El cambio se audita y aplica solo a la sesión actual. Al cerrar
        // sesión vuelve a OFF automáticamente.
        private void ToggleVistaNE()
        {
            if (!Sesion.EsAdmin) return;
            bool nuevoValor = !Sesion.MostrarNotasEntrega;

            string mensaje = nuevoValor
                ? "¿Activar la visibilidad de Notas de Entrega?\n\n" +
                  "MIENTRAS ESTÉ ACTIVA, los reportes, el dashboard, el P&L y " +
                  "todos los agregados INCLUIRÁN las notas de entrega (que " +
                  "normalmente quedan fuera por no tener valor fiscal).\n\n" +
                  "Esta acción se registra en auditoría y se desactiva " +
                  "automáticamente al cerrar sesión."
                : "¿Desactivar la visibilidad de Notas de Entrega?\n\n" +
                  "Los reportes vuelven a excluirlas como es normal.";

            if (MessageBox.Show(this, mensaje,
                "Visibilidad de Notas de Entrega",
                MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes)
                return;

            Sesion.SetMostrarNotasEntrega(nuevoValor);
            ActualizarBotonNE();
            ActualizarBannerNE();
        }

        private void ActualizarBotonNE()
        {
            if (_btnToggleNE == null) return;
            if (Sesion.MostrarNotasEntrega)
            {
                _btnToggleNE.Text = "🔓 Vista NE: ON";
                _btnToggleNE.BackColor = Color.FromArgb(180, 100, 30);
                _btnToggleNE.ForeColor = Color.White;
                _btnToggleNE.FlatAppearance.MouseOverBackColor = Color.FromArgb(210, 130, 50);
            }
            else
            {
                _btnToggleNE.Text = "Vista NE: OFF";
                _btnToggleNE.BackColor = UINeo.Surface;
                _btnToggleNE.ForeColor = UINeo.Ink;
                _btnToggleNE.FlatAppearance.MouseOverBackColor = UINeo.SurfaceHi;
            }
        }

        private void ActualizarBannerNE()
        {
            if (_pnlBannerNE == null) return;
            _pnlBannerNE.Visible = Sesion.MostrarNotasEntrega;
            _pnlBannerNE.Invalidate();
        }

        private Panel ConstruirStripInfo()
        {
            var p = new Panel { Dock = DockStyle.Fill, BackColor = UINeo.Ground };

            var fila = new TableLayoutPanel
            {
                Dock = DockStyle.Fill, RowCount = 1, ColumnCount = 4,
                BackColor = UINeo.Ground, Padding = new Padding(10, 8, 10, 8)
            };
            // Sombra de relieve neumórfico bajo cada sub-tarjeta del strip
            fila.Paint += (s, e) =>
            {
                var g = e.Graphics; g.SmoothingMode = SmoothingMode.AntiAlias;
                foreach (Control c in fila.Controls)
                    if (c.Visible && (c is Panel || (c.Tag as string) == "neo"))
                        UINeo.SombraRelieve(g, c.Bounds, 14);
            };
            fila.Layout += (s, e) => fila.Invalidate();
            // Convierte un panel en sub-tarjeta elevada (esquinas redondeadas)
            void Relieve(Control c, int radio = 14)
            {
                c.Margin = new Padding(8, 8, 8, 8);
                void redib() { if (c.Width > 0 && c.Height > 0) {
                    using var pt = RoundRect(new Rectangle(0, 0, c.Width, c.Height), radio);
                    c.Region = new Region(pt); } }
                c.Resize += (s2, e2) => redib();
                redib();
            }
            fila.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 320f));   // saludo + fecha
            fila.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 300f));   // BCV/Paralelo
            fila.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));    // alertas
            fila.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 180f));   // boton actualizar tasa

            // Saludo + fecha
            var pnlSaludo = new Panel { Dock = DockStyle.Fill, BackColor = UINeo.Surface,
                Padding = new Padding(14, 6, 6, 6) };
            var lblSaludo = new Label
            {
                Text = SaludoSegunHora() + ", " + (PrimerNombre() ?? "Admin"),
                Font = new Font("Segoe UI", 12f, FontStyle.Bold),
                ForeColor = CTexto, AutoSize = false, Dock = DockStyle.Top, Height = 26
            };
            var lblFecha = new Label
            {
                Text = DateTime.Now.ToString("dddd, d 'de' MMMM 'de' yyyy",
                    new System.Globalization.CultureInfo("es-ES")),
                Font = new Font("Segoe UI", 9f),
                ForeColor = CTextoTen, AutoSize = false, Dock = DockStyle.Top, Height = 18
            };
            pnlSaludo.Controls.Add(lblFecha); pnlSaludo.Controls.Add(lblSaludo);
            fila.Controls.Add(pnlSaludo, 0, 0);
            Relieve(pnlSaludo);

            // KPI BCV/Paralelo (mismo widget que el dashboard)
            var pnlBcv = KpiBcvParalelo();
            fila.Controls.Add(pnlBcv, 1, 0);
            Relieve(pnlBcv);

            // Alertas resumidas
            var pnlA = new Panel { Dock = DockStyle.Fill, BackColor = UINeo.Surface,
                Padding = new Padding(10, 6, 6, 6) };
            var bar = new Panel { Dock = DockStyle.Left, Width = 3, BackColor = CWarn };
            var lblTit = new Label { Text = "ALERTAS",
                Font = new Font("Segoe UI", 7.5f, FontStyle.Bold),
                ForeColor = CTextoTen, AutoSize = false, Dock = DockStyle.Top, Height = 14,
                Padding = new Padding(8, 0, 0, 0) };
            _lblAlertas = new Label
            {
                Text = "Sin alertas", Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                ForeColor = COk, AutoSize = false, Dock = DockStyle.Fill,
                Padding = new Padding(8, 2, 0, 0), Cursor = Cursors.Hand
            };
            _lblAlertas.Click += (s, e) =>
            {
                try { new FrmAlertas(AlertasService.ObtenerAlertas()).ShowDialog(); }
                catch (Exception ex) { MessageBox.Show(ex.Message); }
            };
            pnlA.Controls.Add(_lblAlertas); pnlA.Controls.Add(lblTit); pnlA.Controls.Add(bar);
            fila.Controls.Add(pnlA, 2, 0);
            Relieve(pnlA);

            // Boton "Actualizar tasa" — altura fija, anclado horizontal y centrado vertical
            var btnTasa = new Button
            {
                Text = "↻  Actualizar tasa",
                Anchor = AnchorStyles.Left | AnchorStyles.Right,
                Height = 38,
                FlatStyle = FlatStyle.Flat, BackColor = CAzul, ForeColor = Color.White,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold), Cursor = Cursors.Hand,
                Margin = new Padding(6, 29, 12, 29),  // strip 96 - botón 38 = 58 / 2 = 29 cada lado
                TextAlign = ContentAlignment.MiddleCenter
            };
            UINeo.BotonAccento(btnTasa, CAzul, CAzulSec);
            btnTasa.Click += (s, e) =>
            {
                try
                {
                    using var f = new FrmActualizarTasa(DatabaseService.ObtenerTasaHoy());
                    if (f.ShowDialog() == DialogResult.OK) RefrescarInfo();
                }
                catch (Exception ex)
                {
                    MessageBox.Show(this, "Error: " + ex.Message,
                        "Tasa", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            };
            fila.Controls.Add(btnTasa, 3, 0);

            p.Controls.Add(fila);
            return p;
        }

        private Panel KpiBcvParalelo()
        {
            var p = new Panel { Dock = DockStyle.Fill, BackColor = UINeo.Surface,
                Padding = new Padding(8, 4, 4, 4) };
            var bar = new Panel { Dock = DockStyle.Left, Width = 3, BackColor = COk };

            var fila = new TableLayoutPanel
            {
                Dock = DockStyle.Fill, RowCount = 2, ColumnCount = 4,
                BackColor = UINeo.Surface
            };
            // Columnas internas mas anchas para que "BCV" y "PAR" no se partan
            fila.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 50f));    // label BCV
            fila.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50f));     // valor BCV
            fila.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 50f));    // label PAR
            fila.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50f));     // valor PAR
            fila.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
            fila.RowStyles.Add(new RowStyle(SizeType.Absolute, 18f));

            fila.Controls.Add(new Label { Text = "BCV",
                Font = new Font("Segoe UI", 7.5f, FontStyle.Bold), ForeColor = CTextoTen,
                Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(8, 0, 0, 0) }, 0, 0);
            _lblBcv = new Label { Text = "—",
                Font = new Font("Segoe UI", 11f, FontStyle.Bold), ForeColor = CTexto,
                Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft };
            fila.Controls.Add(_lblBcv, 1, 0);

            fila.Controls.Add(new Label { Text = "PAR",
                Font = new Font("Segoe UI", 7.5f, FontStyle.Bold), ForeColor = CTextoTen,
                Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft }, 2, 0);
            _lblPara = new Label { Text = "—",
                Font = new Font("Segoe UI", 11f, FontStyle.Bold), ForeColor = CMorado,
                Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft };
            fila.Controls.Add(_lblPara, 3, 0);

            _lblBcvSello = new Label { Text = "sin tasa",
                Font = new Font("Segoe UI", 7.5f, FontStyle.Italic), ForeColor = CWarn,
                Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(8, 0, 0, 0) };
            fila.SetColumnSpan(_lblBcvSello, 4);
            fila.Controls.Add(_lblBcvSello, 0, 1);

            p.Controls.Add(fila); p.Controls.Add(bar);
            return p;
        }

        // ── Centro: grid de modulos + panel chat lateral ────────────────────
        private Panel ConstruirContenido()
        {
            // Envoltorio con split horizontal: grid (Fill) + chat (Right, ancho 0 inicial)
            var wrapper = new Panel { Dock = DockStyle.Fill, BackColor = UINeo.Ground };

            _pnlChat = ConstruirPanelChat();
            _pnlChat.Dock  = DockStyle.Right;
            _pnlChat.Width = 0;
            _pnlChat.Visible = false;

            // Fondo MEDIO-oscuro de la zona: gris azulado claro pero
            // notablemente distinto a los washes pastel de las tarjetas.
            Color fondoZona = UINeo.Ground;
            var p = new Panel { Dock = DockStyle.Fill, BackColor = fondoZona,
                Padding = new Padding(20, 16, 20, 4) };

            _pnlGrid = new Panel { Dock = DockStyle.Fill, BackColor = fondoZona };

            // Pintar sombras bajo cada tarjeta antes de que las tarjetas
            // se pinten encima. Da efecto de elevacion sin tocar las tarjetas.
            _pnlGrid.Paint += (s, e) =>
            {
                var g = e.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;
                foreach (Control c in _pnlGrid.Controls)
                {
                    if (c is Panel card && card.Visible)
                    {
                        UINeo.SombraRelieve(g, new Rectangle(card.Left, card.Top, card.Width, card.Height), 14);
                    }
                }
            };

            var modulos = new List<(string ico, string titulo, string sub, string perm,
                                    Action accion, Color color)>
            {
                ("▣", "Dashboard",     "Resumen y KPIs del negocio",        "Reportes",
                 () => new FrmDashboardV2().ShowDialog(),                CAzul),
                ("◈", "Transacciones", "Facturas, CxC, precios",            "Transacciones",
                 () => new FrmTransacciones().ShowDialog(),              CMorado),
                ("◧", "Archivos",      "Inventario, clientes y más",        "Archivos",
                 () => new FrmArchivos().ShowDialog(),                   Color.FromArgb(176, 100, 20)),
                ("⊞", "Producción",    "Registro, kardex, moldes",          "IngresoProduccion",
                 () => new FrmRegistroProduccionDia().ShowDialog(),      COk),
                // FrmCompras viejo deprecado: las compras de MP ahora se hacen
                // vía Transacciones → Egresos (categoría Materia Prima).
                ("◎", "Nómina",        "Empleados y períodos",              "Nomina",
                 () => new FrmNomina().ShowDialog(),                     Color.FromArgb(30, 90, 100)),
                ("◉", "Cámaras",       "Vigilancia EZVIZ en vivo",          "Camaras",
                 () => new FrmCamaras().ShowDialog(),                    Color.FromArgb(150, 60, 60)),
            };

            int cols = 3;
            int gap  = 16;
            var tarjetas = new List<Panel>();

            foreach (var (ico, titulo, sub, perm, accion, color) in modulos)
                tarjetas.Add(TarjetaModulo(ico, titulo, sub, perm, accion, color));

            foreach (var t in tarjetas) _pnlGrid.Controls.Add(t);

            void Reposicionar()
            {
                int n = tarjetas.Count;
                int totalAncho = _pnlGrid.ClientSize.Width - 4;
                int totalAlto  = _pnlGrid.ClientSize.Height - 4;
                int w = (totalAncho - (cols - 1) * gap) / cols;
                // Altura FIJA — las tarjetas no se estiran al alto disponible
                // (antes salian cajas blancas gigantes con mucho aire vacio).
                int h = 140;
                int filas = (n + cols - 1) / cols;
                int alturaUsada = filas * h + (filas - 1) * gap;
                // Centrar verticalmente el bloque de tarjetas
                int offY = Math.Max(0, (totalAlto - alturaUsada) / 2);
                for (int i = 0; i < n; i++)
                {
                    int col = i % cols, row = i / cols;
                    tarjetas[i].SetBounds(col * (w + gap), offY + row * (h + gap), w, h);
                }
            }
            _pnlGrid.SizeChanged += (s, e) => Reposicionar();
            _pnlGrid.HandleCreated += (s, e) => Reposicionar();

            p.Controls.Add(_pnlGrid);

            // Fill primero, Right después (orden correcto WinForms)
            wrapper.Controls.Add(p);
            wrapper.Controls.Add(_pnlChat);
            return wrapper;
        }

        // ── Panel de chat IA ─────────────────────────────────────────────────
        private Panel ConstruirPanelChat()
        {
            var pnl = new Panel { BackColor = Color.FromArgb(30, 40, 60) };

            // Encabezado
            var hdr = new Panel { Dock = DockStyle.Top, Height = 40,
                BackColor = CAzulOsc };
            var lblTit = new Label
            {
                Text = "🤖  Asistente IA",
                Font = new Font("Segoe UI", 10f, FontStyle.Bold),
                ForeColor = Color.White, Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(12, 0, 0, 0)
            };
            var btnCerrar = new Label
            {
                Text = "✕", Font = new Font("Segoe UI", 10f, FontStyle.Bold),
                ForeColor = Color.FromArgb(168, 177, 187),
                Dock = DockStyle.Right, Width = 36,
                TextAlign = ContentAlignment.MiddleCenter,
                Cursor = Cursors.Hand
            };
            btnCerrar.Click += (s, e) => ToggleChat();
            hdr.Controls.Add(lblTit);
            hdr.Controls.Add(btnCerrar);

            // Área de chat (historial)
            _rtfChat = new RichTextBox
            {
                Dock = DockStyle.Fill, ReadOnly = true,
                BackColor = Color.FromArgb(30, 40, 60),
                ForeColor = Color.FromArgb(220, 225, 235),
                BorderStyle = BorderStyle.None,
                Font = new Font("Segoe UI", 9.5f),
                ScrollBars = RichTextBoxScrollBars.Vertical,
                Padding = new Padding(10, 8, 10, 4)
            };

            // Panel inferior: input + botón
            var pnlInput = new Panel
            {
                Dock = DockStyle.Bottom, Height = 80,
                BackColor = Color.FromArgb(22, 30, 46),
                Padding = new Padding(8, 6, 8, 8)
            };
            pnlInput.Paint += (s, e) =>
            {
                using var pen = new Pen(Color.FromArgb(60, 80, 120), 1f);
                e.Graphics.DrawLine(pen, 0, 0, pnlInput.Width, 0);
            };

            _txtChatInput = new TextBox
            {
                Multiline = false,
                Font = new Font("Segoe UI", 9.5f),
                BackColor = Color.FromArgb(44, 56, 80),
                ForeColor = Color.FromArgb(220, 225, 235),
                BorderStyle = BorderStyle.FixedSingle,
                Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top,
                PlaceholderText = "Escribe tu pregunta…"
            };
            var btnEnviar = new Button
            {
                Text = "Enviar",
                FlatStyle = FlatStyle.Flat,
                BackColor = CAzul, ForeColor = Color.White,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                Cursor = Cursors.Hand,
                Anchor = AnchorStyles.Right | AnchorStyles.Top
            };
            btnEnviar.FlatAppearance.BorderSize = 0;
            btnEnviar.FlatAppearance.MouseOverBackColor = CAzulSec;

            pnlInput.Layout += (s, e) =>
            {
                int pad = 8, btnW = 70, gap = 6, h = 30;
                int inputW = pnlInput.Width - pad * 2 - btnW - gap;
                _txtChatInput.SetBounds(pad, 12, Math.Max(60, inputW), h);
                btnEnviar.SetBounds(pad + inputW + gap, 11, btnW, h + 2);
            };

            async void Enviar(object? s, EventArgs e) => await EnviarMensajeIAAsync();
            btnEnviar.Click += Enviar;
            _txtChatInput.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Enter && !e.Shift)
                {
                    e.SuppressKeyPress = true;
                    _ = EnviarMensajeIAAsync();
                }
            };

            pnlInput.Controls.Add(_txtChatInput);
            pnlInput.Controls.Add(btnEnviar);

            // Fill primero, luego Top y Bottom
            pnl.Controls.Add(_rtfChat);
            pnl.Controls.Add(pnlInput);
            pnl.Controls.Add(hdr);

            // Mensaje de bienvenida
            ChatAppend("sistema",
                "Hola. Soy tu asistente IA. Puedes preguntarme sobre los módulos " +
                "del sistema, interpretar alertas, consultar tasas o pedir " +
                "recomendaciones sobre los datos del negocio.");
            return pnl;
        }

        private void ToggleChat()
        {
            _chatVisible = !_chatVisible;
            _pnlChat.Visible = _chatVisible;
            _pnlChat.Width   = _chatVisible ? 320 : 0;
            if (_btnToggleChat != null)
            {
                _btnToggleChat.Text      = _chatVisible ? "✕  Cerrar chat" : "🤖  Chat IA";
                _btnToggleChat.BackColor = _chatVisible
                    ? Color.FromArgb(60, 52, 145)
                    : UITheme.Azul;
            }
            if (_chatVisible) _txtChatInput?.Focus();
        }

        private void ChatAppend(string rol, string texto)
        {
            // Colores según quien habla
            bool esSistema = rol == "sistema";
            Color cRol  = esSistema ? Color.FromArgb(100, 160, 230) : Color.FromArgb(130, 210, 160);
            Color cText = esSistema ? Color.FromArgb(200, 210, 230) : Color.FromArgb(230, 240, 230);
            string prefijo = esSistema ? "🤖  Asistente" : "👤  Tú";

            _rtfChat.SelectionStart = _rtfChat.TextLength;
            _rtfChat.SelectionColor = cRol;
            _rtfChat.SelectionFont  = new Font("Segoe UI", 8.5f, FontStyle.Bold);
            _rtfChat.AppendText("\r\n" + prefijo + "\r\n");

            _rtfChat.SelectionColor = cText;
            _rtfChat.SelectionFont  = new Font("Segoe UI", 9.5f);
            _rtfChat.AppendText(texto + "\r\n");

            _rtfChat.SelectionStart = _rtfChat.TextLength;
            _rtfChat.ScrollToCaret();
        }

        private async System.Threading.Tasks.Task EnviarMensajeIAAsync()
        {
            string pregunta = _txtChatInput.Text.Trim();
            if (string.IsNullOrEmpty(pregunta)) return;

            _txtChatInput.Clear();
            _historialChat.Add(("usuario", pregunta));
            ChatAppend("usuario", pregunta);
            ChatAppend("sistema", "⏳  Pensando…");

            // Índice del último mensaje para poder actualizarlo
            int posMarcador = _rtfChat.TextLength;

            try
            {
                bool disponible = await OllamaService.IsAvailableAsync();
                if (!disponible)
                {
                    RemplazarUltimoMensaje(
                        "⚠  Ollama no está disponible en http://localhost:11434.\n" +
                        "Asegúrate de que el servidor esté corriendo.");
                    return;
                }

                string modelo  = DatabaseService.ObtenerConfig("OllamaModelo", "gemma4:31b-cloud");
                string empresa = DatabaseService.ObtenerConfig("NombreEmpresa", "la empresa");

                // Contexto del sistema (tasa + alertas)
                string ctxTasa = "";
                try
                {
                    var t = DatabaseService.ObtenerTasaHoy();
                    if (t != null)
                        ctxTasa = $"Tasa BCV hoy: {t.TasaBCV:N2} Bs/USD. " +
                                  $"Tasa paralelo: {(t.TasaParalelo > 0 ? t.TasaParalelo.ToString("N2") : "no registrada")} Bs/USD. ";
                }
                catch { }

                string ctxAlertas = "";
                try
                {
                    var al = AlertasService.ObtenerAlertas();
                    if (al.TieneAlertas)
                        ctxAlertas = $"Alertas activas: {al.StockBajo.Count} stock bajo, " +
                                     $"{al.CxCVencidas.Count} CxC vencidas. ";
                }
                catch { }

                // Historial reciente (max 6 turnos = 3 intercambios)
                var histReciente = _historialChat.TakeLast(6).ToList();
                var sb = new System.Text.StringBuilder();
                foreach (var (rol, txt) in histReciente)
                    sb.AppendLine(rol == "usuario" ? $"Usuario: {txt}" : $"Asistente: {txt}");

                string prompt =
                    $"Eres un asistente de negocios para la empresa venezolana \"{empresa}\", " +
                    $"integrado en su sistema de gestión (SistemaGestion). " +
                    $"Contexto actual: {ctxTasa}{ctxAlertas}" +
                    $"El sistema tiene módulos de: Facturas, Inventario, CxC, Compras, " +
                    $"Nómina, Producción, ISLR, Reportes y más.\n\n" +
                    $"Conversación:\n{sb}\n" +
                    $"Responde en español, de forma concisa y práctica. Máximo 150 palabras.";

                string respuesta = await OllamaService.GenerateAsync(modelo, prompt, 350);
                _historialChat.Add(("sistema", respuesta));
                RemplazarUltimoMensaje(respuesta);
            }
            catch (Exception ex)
            {
                RemplazarUltimoMensaje($"⚠  Error: {ex.Message}");
            }
        }

        private void RemplazarUltimoMensaje(string nuevoTexto)
        {
            // Buscar el "⏳  Pensando…" y reemplazarlo
            string actual = _rtfChat.Text;
            int idx = actual.LastIndexOf("⏳  Pensando…", StringComparison.Ordinal);
            if (idx >= 0)
            {
                _rtfChat.Select(idx, "⏳  Pensando…".Length);
                _rtfChat.SelectedText = nuevoTexto;
            }
            else
            {
                ChatAppend("sistema", nuevoTexto);
            }
            _rtfChat.SelectionStart = _rtfChat.TextLength;
            _rtfChat.ScrollToCaret();
        }

        private Panel TarjetaModulo(string icono, string titulo, string subtitulo,
            string permiso, Action accion, Color colorAcento)
        {
            // Cámaras es una excepción: el "permiso" es per-usuario (PermisosCamara),
            // no rol-basado (PermisosRol). Si el usuario no es admin, hay que
            // consultar la tabla para saber si tiene alguna cámara asignada.
            bool tieneAcceso;
            if (permiso == "Camaras")
            {
                if (Sesion.EsAdmin) tieneAcceso = true;
                else
                {
                    try
                    {
                        tieneAcceso = DatabaseService
                            .ContarCamarasVisibles(Sesion.UsuarioId, false) > 0;
                    }
                    catch { tieneAcceso = false; }
                }
            }
            else
            {
                tieneAcceso = Sesion.EsAdmin || Sesion.TienePermiso(permiso);
            }

            // Wash MUY claro del color del modulo: mezcla colorAcento al ~10%
            // sobre blanco. Mucho mas amable que el color sólido y permite
            // que el texto sea legible.
            Color washCard = tieneAcceso ? UINeo.Surface : Color.FromArgb(224, 228, 235);
            // Borde mas saturado para que perimetro se note bien.
            Color bordeCard = tieneAcceso ? Color.FromArgb(250, 252, 255) : Color.FromArgb(214, 218, 226);

            var card = new Panel
            {
                BackColor = washCard,
                Cursor = tieneAcceso ? Cursors.Hand : Cursors.Default
            };
            // Region clipped al RoundRect para esquinas perfectas.
            card.Resize += (s, e) =>
            {
                using var path = RoundRect(new Rectangle(0, 0, card.Width, card.Height), 14);
                card.Region = new Region(path);
            };
            card.Paint += (s, e) =>
            {
                var g = e.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;
                using var pen = new Pen(bordeCard, 1.2f);
                using var path = RoundRect(new Rectangle(0, 0, card.Width - 1, card.Height - 1), 14);
                g.DrawPath(pen, path);
            };

            // Barra lateral de color (codificacion por modulo)
            var bar = new Panel { Dock = DockStyle.Left, Width = 4,
                BackColor = tieneAcceso ? colorAcento : CTextoTen };

            // Icono — circulo de fondo solido del color del modulo (mas saturado)
            var pnlIco = new Panel
            {
                Size = new Size(52, 52),
                Location = new Point(20, 22),
                BackColor = washCard
            };
            // Fondo del icono: blanco solido para que el glifo destaque
            // contra el wash de la tarjeta.
            Color fondoIco = tieneAcceso
                ? Mezclar(colorAcento, UINeo.Surface, 0.80f)   // chip con tinte del color del modulo
                : Color.FromArgb(226, 230, 236);
            pnlIco.Paint += (s, e) =>
            {
                var g = e.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;
                using var br = new SolidBrush(fondoIco);
                g.FillEllipse(br, 0, 0, pnlIco.Width - 1, pnlIco.Height - 1);
            };
            var lblIco = new Label
            {
                Text = icono,
                Font = new Font("Segoe UI", 22f, FontStyle.Bold),
                ForeColor = tieneAcceso ? colorAcento : CTextoTen,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter,
                BackColor = Color.Transparent
            };
            pnlIco.Controls.Add(lblIco);

            // Titulo
            var lblT = new Label
            {
                Text = titulo,
                Font = new Font("Segoe UI", 13f, FontStyle.Bold),
                ForeColor = tieneAcceso ? CTexto : CTextoTen,
                AutoSize = false, Location = new Point(86, 26),
                Size = new Size(260, 24),
                BackColor = washCard
            };

            // Subtitulo
            var lblS = new Label
            {
                Text = subtitulo,
                Font = new Font("Segoe UI", 9.5f),
                ForeColor = CTextoTen,
                AutoSize = false, Location = new Point(86, 52),
                Size = new Size(280, 20),
                BackColor = washCard
            };

            // Footer de la tarjeta: ancla al fondo
            var pnlFooter = new Panel
            {
                Dock = DockStyle.Bottom, Height = 36,
                BackColor = washCard
            };
            if (!tieneAcceso)
            {
                pnlFooter.Controls.Add(new Label
                {
                    Text = "  Sin acceso  ",
                    Font = new Font("Segoe UI", 7.5f, FontStyle.Bold),
                    ForeColor = CTextoTen,
                    BackColor = Color.FromArgb(238, 240, 245),
                    AutoSize = true, Location = new Point(20, 10)
                });
            }
            else
            {
                pnlFooter.Controls.Add(new Label
                {
                    Text = "Abrir  →",
                    Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                    ForeColor = colorAcento,
                    AutoSize = true, Location = new Point(20, 10),
                    BackColor = washCard
                });
            }

            card.Controls.Add(pnlFooter);
            card.Controls.Add(lblS);
            card.Controls.Add(lblT);
            card.Controls.Add(pnlIco);
            card.Controls.Add(bar);

            // Hover + click recursivo (los paneles anidados tambien necesitan
            // el cambio de color para que el hover se vea uniforme)
            if (tieneAcceso)
            {
                // Hover: wash MAS saturado del color del modulo
                // (perceptiblemente mas intenso que el reposo).
                Color colorHover = UINeo.SurfaceHi;
                void Pintar(Control c, Color bg)
                {
                    // bar (lateral) y fondo del icono mantienen su propio color
                    if (c == bar) return;
                    if (c == pnlIco) return;
                    if (c.BackColor == Color.Transparent) { /* nada */ }
                    else if (c is Label || c is Panel) c.BackColor = bg;
                    foreach (Control h in c.Controls) Pintar(h, bg);
                }
                void EnterHandler(object? s, EventArgs e) => Pintar(card, colorHover);
                void LeaveHandler(object? s, EventArgs e) => Pintar(card, washCard);
                void EngancharRec(Control c, MouseEventHandler? me, EventHandler enter, EventHandler leave, EventHandler clk)
                {
                    c.MouseEnter += enter;
                    c.MouseLeave += leave;
                    c.Click      += clk;
                    foreach (Control h in c.Controls) EngancharRec(h, me, enter, leave, clk);
                }
                EventHandler clkH = (s, e) => { try { accion(); } catch (Exception ex) { MessageBox.Show(this, ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error); } };
                EngancharRec(card, null, EnterHandler, LeaveHandler, clkH);
            }

            return card;
        }

        /// <summary>
        /// Mezcla dos colores. ratio = peso del SEGUNDO color (0..1).
        /// Mezclar(azul, blanco, 0.92) = blanco con 8% de azul = wash muy claro.
        /// </summary>
        private static Color Mezclar(Color c1, Color c2, float ratio)
        {
            ratio = Math.Max(0f, Math.Min(1f, ratio));
            float a = 1f - ratio;
            return Color.FromArgb(
                (int)(c1.R * a + c2.R * ratio),
                (int)(c1.G * a + c2.G * ratio),
                (int)(c1.B * a + c2.B * ratio));
        }

        private static GraphicsPath RoundRect(Rectangle r, int rad)
        {
            var p = new GraphicsPath();
            p.AddArc(r.X, r.Y, rad * 2, rad * 2, 180, 90);
            p.AddArc(r.Right - rad * 2, r.Y, rad * 2, rad * 2, 270, 90);
            p.AddArc(r.Right - rad * 2, r.Bottom - rad * 2, rad * 2, rad * 2, 0, 90);
            p.AddArc(r.X, r.Bottom - rad * 2, rad * 2, rad * 2, 90, 90);
            p.CloseFigure();
            return p;
        }

        // ── Bottom bar — CLARO con buen contraste (estilo unificado) ────────
        // Antes era carbón profundo y los botones se confundían con el fondo.
        // Ahora fondo claro, botones azules sólidos para acciones de navegación
        // y rojo para Salir. Línea superior tenue como separador.
        private Panel ConstruirBottomBar()
        {
            var p = new Panel { Dock = DockStyle.Fill, BackColor = UINeo.Ground };

            var fila = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill, FlowDirection = FlowDirection.LeftToRight,
                BackColor = UINeo.Ground, Padding = new Padding(20, 12, 20, 12),
                AutoSize = false, WrapContents = false
            };
            fila.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                UINeo.SombrasHijos(e.Graphics, fila, 12);
            };
            fila.Layout += (s, e) => fila.Invalidate();

            fila.Controls.Add(BotonInferior("▤  Reportes",       () => new FrmReportes().ShowDialog()));
            fila.Controls.Add(BotonInferior("$  Tasa de cambio", () => new FrmTasaCambio().ShowDialog()));
            fila.Controls.Add(BotonInferior("⚙  Sistema",        () => new FrmSistema().ShowDialog()));

            // Botón Chat IA
            _btnToggleChat = new Button
            {
                Text = "🤖  Chat IA",
                Width = 130, Height = 38,
                Font = new Font("Segoe UI", 10f, FontStyle.Bold),
                Cursor = Cursors.Hand,
                Margin = new Padding(0, 4, 8, 4)
            };
            UINeo.BotonAccento(_btnToggleChat, CMorado, Color.FromArgb(60, 52, 145));
            _btnToggleChat.Click += (s, e) => ToggleChat();
            fila.Controls.Add(_btnToggleChat);

            // Spacer
            fila.Controls.Add(new Panel { Width = 30, Height = 1, BackColor = UINeo.Ground });

            // Salir — rojo sólido para destacar acción destructiva
            var btnSalir = new Button
            {
                Text = "✕  Salir",
                Width = 130, Height = 38,
                Font = new Font("Segoe UI", 10f, FontStyle.Bold),
                Cursor = Cursors.Hand,
                Margin = new Padding(8, 4, 0, 4)
            };
            UINeo.BotonAccento(btnSalir, UITheme.Rojo, Color.FromArgb(150, 30, 30));
            btnSalir.Click += BtnSalir_Click;
            fila.Controls.Add(btnSalir);

            p.Controls.Add(fila);
            return p;
        }

        private Button BotonInferior(string texto, Action accion)
        {
            // Botón primario azul sólido — buen contraste sobre fondo claro
            var b = new Button
            {
                Text = texto, Width = 160, Height = 38,
                Font = new Font("Segoe UI", 10f, FontStyle.Bold),
                Cursor = Cursors.Hand,
                Margin = new Padding(0, 4, 10, 4)
            };
            UINeo.BotonSuave(b, UINeo.Ink, 12);
            b.Click += (s, e) => { try { accion(); } catch (Exception ex) { MessageBox.Show(this, ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error); } };
            return b;
        }

        // ── Toast flotante de alertas críticas ─────────────────────────────
        private Panel ConstruirToastAlertas()
        {
            var p = new Panel
            {
                Size      = new Size(340, 84),
                BackColor = Color.FromArgb(170, 28, 28),
                Visible   = false,
                Padding   = new Padding(0)
            };
            p.Paint += (s, e) =>
            {
                var g = e.Graphics;
                // Banda izquierda roja viva
                using var br  = new SolidBrush(Color.FromArgb(220, 60, 60));
                g.FillRectangle(br, 0, 0, 4, p.Height);
                // Borde sutil
                using var pen = new Pen(Color.FromArgb(210, 55, 55), 1f);
                g.DrawRectangle(pen, 0, 0, p.Width - 1, p.Height - 1);
            };

            var lblIco = new Label
            {
                Text = "⚠",
                Font = new Font("Segoe UI", 20f),
                ForeColor = Color.FromArgb(255, 220, 100),
                AutoSize = false, Dock = DockStyle.Left, Width = 52,
                TextAlign = ContentAlignment.MiddleCenter,
                BackColor = Color.Transparent
            };

            var btnX = new Label
            {
                Text = "✕",
                Font = new Font("Segoe UI", 9f),
                ForeColor = Color.FromArgb(210, 160, 160),
                Dock = DockStyle.Right, Width = 28,
                TextAlign = ContentAlignment.TopCenter,
                Padding = new Padding(0, 6, 4, 0),
                Cursor = Cursors.Hand
            };
            btnX.Click += (s, e) => OcultarToast();

            var lblTexto = new Label
            {
                Text = "",
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                ForeColor = Color.White,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(8, 6, 4, 6),
                BackColor = Color.Transparent
            };

            // Fill primero, luego Left y Right
            p.Controls.Add(lblTexto);
            p.Controls.Add(lblIco);
            p.Controls.Add(btnX);
            p.Tag = lblTexto;   // para actualizarlo en MostrarToast
            return p;
        }

        private void PosicionarToast()
        {
            if (_pnlToast == null) return;
            _pnlToast.Location = new Point(
                ClientSize.Width  - _pnlToast.Width - 16,
                155   // debajo del navbar (42) + bannerNE (28) + stripInfo (80) + 5px margen
            );
        }

        private void MostrarToast(AlertasData alertas)
        {
            if (_toastMostrado || _pnlToast == null) return;
            _toastMostrado = true;

            if (_pnlToast.Tag is Label lbl)
            {
                var partes = new List<string>();
                if (alertas.StockBajo.Count   > 0) partes.Add($"{alertas.StockBajo.Count} stock bajo");
                if (alertas.CxCVencidas.Count > 0) partes.Add($"{alertas.CxCVencidas.Count} CxC vencida(s)");
                if (alertas.MantVencido.Count > 0) partes.Add($"{alertas.MantVencido.Count} mant. vencido(s)");
                lbl.Text = "Alertas críticas:\n" + string.Join("  ·  ", partes);
            }

            PosicionarToast();
            _pnlToast.Visible = true;
            _pnlToast.BringToFront();

            // Auto-dismiss después de 8 segundos
            var timer = new System.Windows.Forms.Timer { Interval = 8000 };
            timer.Tick += (s, e) => { timer.Stop(); timer.Dispose(); OcultarToast(); };
            timer.Start();
        }

        private void OcultarToast()
        {
            if (_pnlToast != null) _pnlToast.Visible = false;
        }

        // ── Refresco de info dinamica ───────────────────────────────────────
        private void RefrescarInfo()
        {
            try
            {
                var t = DatabaseService.ObtenerTasaHoy();
                if (t != null)
                {
                    _lblBcv.Text  = t.TasaBCV.ToString("N2");
                    _lblPara.Text = t.TasaParalelo > 0 ? t.TasaParalelo.ToString("N2") : "—";
                    int dias = (DateTime.Today - t.Fecha.Date).Days;
                    if (dias == 0)      { _lblBcvSello.Text = "● actualizada hoy";      _lblBcvSello.ForeColor = COk; }
                    else if (dias <= 2) { _lblBcvSello.Text = "● " + dias + " día(s) atrás"; _lblBcvSello.ForeColor = CWarn; }
                    else                { _lblBcvSello.Text = "● " + dias + " días atrás";   _lblBcvSello.ForeColor = CDang; }
                }
                else
                {
                    _lblBcv.Text = "—"; _lblPara.Text = "—";
                    _lblBcvSello.Text = "sin tasa registrada";
                    _lblBcvSello.ForeColor = CDang;
                }
            }
            catch { }

            try
            {
                var alertas = AlertasService.ObtenerAlertas();

                // Toast la primera vez que hay alertas no silenciadas en la sesión
                if (!_toastMostrado && alertas.TieneAlertasNoSilenciadas)
                    MostrarToast(alertas);

                if (alertas.TieneAlertas)
                {
                    var partes = new List<string>();
                    if (alertas.StockBajo.Count   > 0) partes.Add(alertas.StockBajo.Count + " stock");
                    if (alertas.CxCVencidas.Count > 0) partes.Add(alertas.CxCVencidas.Count + " CxC");
                    if (alertas.MantVencido.Count  > 0) partes.Add(alertas.MantVencido.Count + " mant");
                    _lblAlertas.Text = "● " + string.Join(" · ", partes) + "  ›";
                    _lblAlertas.ForeColor = alertas.CxCVencidas.Count > 0 ? CDang : CWarn;
                }
                else
                {
                    _lblAlertas.Text = "● Sin alertas";
                    _lblAlertas.ForeColor = COk;
                }
            }
            catch { }

            // Clientes de prueba (legacy, se mantiene del original)
            try
            {
                string cs = $"Data Source={SecurityService.RutaBD}";
                if (CargarClientesHelper.ContarClientesExistentes(cs) == 0)
                {
                    CargarClientesHelper.InsertarClientesDePrueba(cs);
                }
            }
            catch { }
        }

        // ── Helpers ─────────────────────────────────────────────────────────
        private static string SaludoSegunHora()
        {
            int h = DateTime.Now.Hour;
            return h < 12 ? "Buenos días" : (h < 19 ? "Buenas tardes" : "Buenas noches");
        }
        private static string? PrimerNombre()
        {
            var n = Sesion.NombreCompleto ?? Sesion.NombreUsuario;
            return n?.Split(' ').FirstOrDefault();
        }

        // ── Handlers ────────────────────────────────────────────────────────
        private void BtnCerrarSesion_Click(object? sender, EventArgs e)
        {
            if (MessageBox.Show("¿Cerrar sesión?", "Confirmar",
                MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;
            try { BackupService.HacerRespaldo(manual: false); } catch { }
            try { DatabaseService.RegistrarAuditoria("LOGOUT", "Usuarios"); } catch { }
            Sesion.Cerrar();
            Hide();
            using var login = new FrmLogin();
            if (login.ShowDialog() == DialogResult.OK)
            {
                Controls.Clear();
                ConstruirUI();
                RefrescarInfo();
                Show();
            }
            else Application.Exit();
        }

        private void BtnSalir_Click(object? sender, EventArgs e)
        {
            if (MessageBox.Show("¿Desea salir del sistema?", "Confirmar salida",
                MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                try { DatabaseService.RegistrarAuditoria("SALIDA", "Sistema"); } catch { }
                Application.Exit();
            }
        }
    }
}
