using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Windows.Forms;

namespace SistemaGestion
{
    // ════════════════════════════════════════════════════════════════════════
    //  MenuPrincipal — rediseño v2
    //  Layout: TopBar fija + grid de módulos + panel inferior de acciones
    //  Paleta: dos temas (Azul corporativo / Dark mode)
    // ════════════════════════════════════════════════════════════════════════
    public class MenuPrincipal : FormBase
    {
        // ── Tema activo ───────────────────────────────────────────────────
        private static bool _darkMode = false;

        // Colores azul corporativo
        private static readonly Color AzulPri    = Color.FromArgb(26,  74, 138);
        private static readonly Color AzulSec    = Color.FromArgb(30,  90, 160);
        private static readonly Color AzulFondo  = Color.FromArgb(240, 244, 250);
        private static readonly Color AzulSurf   = Color.FromArgb(255, 255, 255);
        private static readonly Color AzulBorde  = Color.FromArgb(220, 230, 245);
        private static readonly Color AzulMuted  = Color.FromArgb(100, 116, 139);
        private static readonly Color AzulAccent = Color.FromArgb(232, 240, 250);

        // Colores dark mode
        private static readonly Color DarkPri    = Color.FromArgb(30,  42,  58);
        private static readonly Color DarkSec    = Color.FromArgb(37,  53,  71);
        private static readonly Color DarkFondo  = Color.FromArgb(20,  29,  40);
        private static readonly Color DarkSurf   = Color.FromArgb(30,  42,  58);
        private static readonly Color DarkBorde  = Color.FromArgb(45,  63,  85);
        private static readonly Color DarkMuted  = Color.FromArgb(122, 149, 176);
        private static readonly Color DarkAccent = Color.FromArgb(45,  63,  85);
        private static readonly Color DarkText   = Color.FromArgb(226, 234, 245);

        // Semánticos (iguales en ambos temas)
        private static readonly Color Exito      = Color.FromArgb(26,  110,  58);
        private static readonly Color Alerta     = Color.FromArgb(176, 119,   0);
        private static readonly Color Peligro    = Color.FromArgb(192,  57,  43);
        private static readonly Color Info       = Color.FromArgb(26,   74, 138);

        private Color C1     => _darkMode ? DarkPri    : AzulPri;
        private Color C2     => _darkMode ? DarkSec    : AzulSec;
        private Color CFondo => _darkMode ? DarkFondo  : AzulFondo;
        private Color CSurf  => _darkMode ? DarkSurf   : AzulSurf;
        private Color CBorde => _darkMode ? DarkBorde  : AzulBorde;
        private Color CMuted => _darkMode ? DarkMuted  : AzulMuted;
        private Color CText  => _darkMode ? DarkText   : Color.FromArgb(30, 41, 59);
        private Color CAccent=> _darkMode ? DarkAccent : AzulAccent;

        // ── Controles que necesitamos referenciar ─────────────────────────
        private Panel   pnlTopBar, pnlContent, pnlAlertaBar;
        private Label   lblTasa, lblAlertaBar;
        private Panel   pnlFondo;

        public MenuPrincipal()
        {
            Text            = "Sistema de Gestión";
            Size            = new Size(860, 600);
            MinimumSize     = new Size(760, 520);
            StartPosition   = FormStartPosition.CenterScreen;
            FormBorderStyle = FormBorderStyle.Sizable;
            BackColor       = CFondo;

            BuildUI();
            this.Load += OnLoad;

            VentanaPrefs.Restaurar(this);

            // Guardar posición/tamaño con debounce 800ms
            var timerPrefs = new System.Windows.Forms.Timer { Interval = 800 };
            timerPrefs.Tick     += (s, e) => { timerPrefs.Stop(); VentanaPrefs.Guardar(this); };
            SizeChanged         += (s, e) => { timerPrefs.Stop(); timerPrefs.Start(); };
            LocationChanged     += (s, e) => { timerPrefs.Stop(); timerPrefs.Start(); };
            FormClosed          += (s, e) => { timerPrefs.Stop(); timerPrefs.Dispose(); };
        }

        // ════════════════════════════════════════════════════════════════
        //  BUILD UI
        // ════════════════════════════════════════════════════════════════
        private void BuildUI()
        {
            // ── Fondo principal ───────────────────────────────────────────
            pnlFondo = new Panel { Dock = DockStyle.Fill, BackColor = CFondo };

            // ── TopBar ────────────────────────────────────────────────────
            pnlTopBar = new Panel {
                Dock = DockStyle.Top, Height = 50,
                BackColor = C1
            };

            // Lado izquierdo — fijo
            var lblLogo = new Label {
                Text = "SistemaGestión",
                Font = new Font("Segoe UI", 11f, FontStyle.Bold),
                ForeColor = Color.White, AutoSize = true,
                Location = new Point(18, 14)
            };

            lblTasa = new Label {
                Text = "BCV — · Par —",
                Font = new Font("Segoe UI", 8.5f),
                ForeColor = Color.FromArgb(200, 220, 245),
                AutoSize = true, Location = new Point(180, 17),
                Cursor = Cursors.Hand
            };
            lblTasa.Click += (s, e) => new FrmTasaCambio().ShowDialog();

            lblAlertaBar = new Label {
                Text = "", Font = new Font("Segoe UI", 8f, FontStyle.Bold),
                ForeColor = Color.FromArgb(255, 200, 80),
                AutoSize = true, Location = new Point(340, 17),
                Visible = false, Cursor = Cursors.Hand
            };

            // Lado derecho — FlowLayoutPanel anclado a la derecha
            var pnlRight = new FlowLayoutPanel {
                Dock = DockStyle.Right,
                AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false, BackColor = Color.Transparent,
                Padding = new Padding(0, 10, 8, 0),
                Margin = new Padding(0)
            };

            var btnPerfil = TopBtn("Mi perfil");
            btnPerfil.Click += (s, e) => new FrmPerfil().ShowDialog();

            var btnBuscar = TopBtn("F3 Buscar");
            btnBuscar.Click += (s, e) => new FrmBuscador().ShowDialog();

            var btnAyudaMenu = TopBtn("? Ayuda");
            btnAyudaMenu.BackColor = Color.FromArgb(80, 50, 150);
            btnAyudaMenu.Click += (s, e) => AyudaService.Mostrar(this, "MenuPrincipal");
            KeyDown += (s, ev) => { if (ev.KeyCode == Keys.F3) new FrmBuscador().ShowDialog(); };

            var btnTema = new Button {
                Text = "◑", Size = new Size(30, 30),
                BackColor = Color.FromArgb(50, 100, 180),
                ForeColor = Color.White, FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 11f), Cursor = Cursors.Hand,
                Margin = new Padding(0, 0, 4, 0)
            };
            btnTema.FlatAppearance.BorderSize = 0;
            btnTema.Click += (s, e) => ToggleTema();

            // Avatar circular
            var avatar = new Panel {
                Size = new Size(32, 32),
                BackColor = Color.FromArgb(50, 100, 180),
                Cursor = Cursors.Hand,
                Margin = new Padding(0, 0, 0, 0)
            };
            avatar.Paint += (s, e) => {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                e.Graphics.FillEllipse(new SolidBrush(Color.FromArgb(80, 130, 200)),
                    0, 0, avatar.Width - 1, avatar.Height - 1);
                string ini = Sesion.NombreCompleto?.Length > 0
                    ? Sesion.NombreCompleto[0].ToString().ToUpper() : "A";
                e.Graphics.DrawString(ini, new Font("Segoe UI", 11f, FontStyle.Bold),
                    Brushes.White, new RectangleF(0, 2, avatar.Width, avatar.Height - 2),
                    new StringFormat { Alignment = StringAlignment.Center });
            };
            avatar.Click += (s, e) => new FrmPerfil().ShowDialog();

            var btnCerrar = new Button {
                Text = "Cerrar sesión", Size = new Size(105, 30),
                BackColor = Color.FromArgb(180, 50, 50),
                ForeColor = Color.White, FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 8f, FontStyle.Bold),
                Cursor = Cursors.Hand, Margin = new Padding(4, 0, 0, 0)
            };
            btnCerrar.FlatAppearance.BorderSize = 0;
            btnCerrar.Click += BtnCerrarSesion_Click;

            pnlRight.Controls.AddRange(new Control[] { btnPerfil, btnBuscar, btnAyudaMenu, btnTema, avatar, btnCerrar });

            pnlTopBar.Controls.AddRange(new Control[] {
                lblLogo, lblTasa, lblAlertaBar, pnlRight
            });

            // ── Barra de info del usuario ─────────────────────────────────
            var pnlUserBar = new Panel {
                Dock = DockStyle.Top, Height = 32,
                BackColor = C2
            };
            var lblRoles = new Label {
                Text = $"  {Sesion.NombreCompleto}   |   Rol: {Sesion.Rol}",
                Font = new Font("Segoe UI", 8.5f),
                ForeColor = Color.FromArgb(200, 220, 255),
                AutoSize = true, Location = new Point(8, 8)
            };
            pnlUserBar.Controls.Add(lblRoles);

            // ── Contenido principal ───────────────────────────────────────
            pnlContent = new Panel { Dock = DockStyle.Fill, BackColor = CFondo, Padding = new Padding(24) };

            // Título de sección
            var lblBienvenido = new Label {
                Text = $"Buenos días, {Sesion.NombreCompleto?.Split(' ')[0] ?? "Admin"}",
                Font = new Font("Segoe UI", 14f, FontStyle.Regular),
                ForeColor = CText, AutoSize = true,
                Location = new Point(24, 20)
            };
            pnlContent.Controls.Add(lblBienvenido);

            var lblSub = new Label {
                Text = DateTime.Now.ToString("dddd, dd MMMM yyyy",
                    new System.Globalization.CultureInfo("es-ES")),
                Font = new Font("Segoe UI", 9f), ForeColor = CMuted,
                AutoSize = true, Location = new Point(24, 48)
            };
            pnlContent.Controls.Add(lblSub);

            // Grid de módulos
            BuildModuleGrid();

            // Panel de acciones inferiores
            BuildBottomBar();

            // ── Barra de alertas (abajo del contenido) ────────────────────
            pnlAlertaBar = new Panel {
                Dock = DockStyle.Bottom, Height = 36,
                BackColor = Color.FromArgb(255, 243, 205),
                Visible = false
            };
            var lblAlerta = new Label {
                Name = "lblAlertaDetalle",
                Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
                ForeColor = Color.FromArgb(146, 97, 0),
                AutoSize = true, Location = new Point(12, 10)
            };
            var btnVerAlerta = new Button {
                Text = "Ver detalle →",
                Size = new Size(110, 26),
                Dock = DockStyle.Right,
                BackColor = Color.FromArgb(210, 140, 0),
                ForeColor = Color.White, FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 8f, FontStyle.Bold)
            };
            btnVerAlerta.FlatAppearance.BorderSize = 0;
            btnVerAlerta.Click += (s, e) => {
                var a = AlertasService.ObtenerAlertas();
                new FrmAlertas(a).ShowDialog();
            };
            pnlAlertaBar.Controls.AddRange(new Control[] { lblAlerta, btnVerAlerta });

            // ── Ensamblar ─────────────────────────────────────────────────
            Controls.AddRange(new Control[] {
                pnlContent, pnlAlertaBar, pnlUserBar, pnlTopBar
            });

            // Atajos de teclado
            KeyPreview = true;
            KeyDown += (s, ev) => {
                if (ev.KeyCode == Keys.F3) new FrmBuscador().ShowDialog();
            };
        }

        // ════════════════════════════════════════════════════════════════
        //  GRID DE MÓDULOS
        // ════════════════════════════════════════════════════════════════
        private void BuildModuleGrid()
        {
            var modulos = new List<(string icono, string titulo, string sub, string perm, Action accion, Color color)>
            {
                ("▣", "Dashboard",     "Resumen del negocio",          "Reportes",
                 () => new FrmDashboardV2().ShowDialog(),                Color.FromArgb(26, 74, 138)),
                ("◈", "Transacciones", "Facturas · CxC · Precios",     "Transacciones",
                 () => new FrmTransacciones().ShowDialog(),              Color.FromArgb(100, 50, 160)),
                ("◧", "Archivos",      "Inventario · Clientes · Más",  "Archivos",
                 () => new FrmArchivos().ShowDialog(),                   Color.FromArgb(176, 100, 20)),
                ("⊞", "Producción",    "Registro · Kardex · Moldes",   "IngresoProduccion",
                 () => new FrmRegistroProduccionDia().ShowDialog(),      Color.FromArgb(20, 110, 60)),
                ("◫", "Compras",       "Proveedores · Insumos",        "Compras",
                 () => new FrmCompras().ShowDialog(),                    Color.FromArgb(26, 74, 138)),
                ("◎", "Nómina",        "Empleados · Períodos",         "Nomina",
                 () => new FrmNomina().ShowDialog(),                     Color.FromArgb(30, 90, 100)),
            };

            
            var _menuTip = AyudaService.CrearTooltip();
            var pnlGrid = new Panel {
                Location = new Point(24, 72),
                Anchor = AnchorStyles.Top | AnchorStyles.Left |
                         AnchorStyles.Right | AnchorStyles.Bottom
            };

            int cols = 3, btnW = 200, btnH = 120, gap = 14;
            pnlGrid.Size = new Size(cols * btnW + (cols - 1) * gap,
                                    2 * btnH + gap);
            pnlContent.Controls.Add(pnlGrid);

            var list = new List<Panel>();
            for (int i = 0; i < modulos.Count; i++)
            {
                var (ico, titulo, sub, perm, accion, color) = modulos[i];
                int col = i % cols, row = i / cols;
                bool tieneAcceso = Sesion.EsAdmin || Sesion.TienePermiso(perm);

                var card = new Panel {
                    Location = new Point(col * (btnW + gap), row * (btnH + gap)),
                    Size = new Size(btnW, btnH),
                    BackColor = tieneAcceso ? CSurf : Color.FromArgb(_darkMode ? 40 : 245, _darkMode ? 50 : 245, _darkMode ? 60 : 250),
                    Cursor = tieneAcceso ? Cursors.Hand : Cursors.Default,
                    Tag = tieneAcceso
                };
                RoundBorder(card, 10);

                // Ícono
                var lblIco = new Label {
                    Text = ico, Font = new Font("Segoe UI", 22f),
                    ForeColor = tieneAcceso ? color : CMuted,
                    AutoSize = false, Size = new Size(44, 44),
                    Location = new Point(16, 14),
                    TextAlign = ContentAlignment.MiddleCenter
                };

                // Título
                var lblT = new Label {
                    Text = titulo, Font = new Font("Segoe UI", 11f, FontStyle.Bold),
                    ForeColor = tieneAcceso ? CText : CMuted,
                    AutoSize = true, Location = new Point(64, 22)
                };

                // Subtítulo
                var lblS = new Label {
                    Text = sub, Font = new Font("Segoe UI", 8f),
                    ForeColor = CMuted, AutoSize = true,
                    Location = new Point(64, 44)
                };

                // Sin acceso badge
                if (!tieneAcceso)
                {
                    var badge = new Label {
                        Text = "Sin acceso", Font = new Font("Segoe UI", 7.5f),
                        ForeColor = CMuted,
                        BackColor = _darkMode ? Color.FromArgb(50,60,70) : Color.FromArgb(235,235,240),
                        AutoSize = true, Location = new Point(16, 80),
                        Padding = new Padding(4, 2, 4, 2)
                    };
                    card.Controls.Add(badge);
                }

                card.Controls.AddRange(new Control[] { lblIco, lblT, lblS });

                // Borde acento al pasar el mouse
                if (tieneAcceso)
                {
                    card.MouseEnter += (s, e) => {
                        card.BackColor = CAccent;
                        card.Invalidate();
                    };
                    card.MouseLeave += (s, e) => {
                        card.BackColor = CSurf;
                        card.Invalidate();
                    };
                    card.Click     += (s, e) => { if (Sesion.TienePermiso(perm)) accion(); };
                    lblIco.Click   += (s, e) => { if (Sesion.TienePermiso(perm)) accion(); };
                    lblT.Click     += (s, e) => { if (Sesion.TienePermiso(perm)) accion(); };
                    lblS.Click     += (s, e) => { if (Sesion.TienePermiso(perm)) accion(); };
                }

                switch (titulo) {
                case "Dashboard": AyudaService.Tip(_menuTip, card, "Resumen ejecutivo: ventas, inventario, alertas y KPIs del negocio"); break;
                case "Transacciones": AyudaService.Tip(_menuTip, card, "Facturas, cuentas por cobrar/pagar, precios y producción"); break;
                case "Archivos": AyudaService.Tip(_menuTip, card, "Inventario, clientes, proveedores, kardex, moldes y usuarios"); break;
                case "Producción": AyudaService.Tip(_menuTip, card, "Registro diario de producción y kardex de movimientos"); break;
                case "Compras": AyudaService.Tip(_menuTip, card, "Gestión de proveedores e insumos de producción"); break;
                case "Nómina": AyudaService.Tip(_menuTip, card, "Empleados, nómina semanal, préstamos y beneficios LOTTT"); break;
                }
                pnlGrid.Controls.Add(card);
                list.Add(card);
            }

            // Responsive resize
            pnlContent.Resize += (s, e) => {
                int available = pnlContent.ClientSize.Width - 48;
                int w = Math.Max(150, (available - (cols - 1) * gap) / cols);
                for (int i = 0; i < list.Count; i++)
                {
                    int col = i % cols, row = i / cols;
                    list[i].Location = new Point(col * (w + gap), row * (btnH + gap));
                    list[i].Width = w;
                }
                pnlGrid.Width  = cols * w + (cols - 1) * gap;
                pnlGrid.Height = 2 * btnH + gap;
            };
        }

        // ════════════════════════════════════════════════════════════════
        //  PANEL INFERIOR
        // ════════════════════════════════════════════════════════════════
        private void BuildBottomBar()
        {
            var pnlBot = new Panel {
                Height = 56, BackColor = CSurf,
                Dock = DockStyle.Bottom,
                Padding = new Padding(24, 0, 24, 0)
            };
            pnlBot.Paint += (s, e) =>
                e.Graphics.DrawLine(new Pen(CBorde), 0, 0, pnlBot.Width, 0);

            var items = new (string texto, Color color, Action accion)[]
            {
                ("▤  Reportes",      Color.FromArgb(140, 20, 20), () => new FrmReportes().ShowDialog()),
                ("$  Tasa de cambio",Color.FromArgb(20, 110, 60), () => new FrmTasaCambio().ShowDialog()),
                ("⚙  Sistema",       Color.FromArgb(70, 100, 160),() => new FrmSistema().ShowDialog()),
                ("✕  Salir",         Color.FromArgb(180, 60, 60), () => BtnSalir_Click(null, EventArgs.Empty)),
            };

            int x = 24;
            foreach (var (texto, color, accion) in items)
            {
                var btn = new Button {
                    Text = texto, Size = new Size(148, 34), Location = new Point(x, 10),
                    BackColor = color, ForeColor = Color.White,
                    FlatStyle = FlatStyle.Flat,
                    Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                    Cursor = Cursors.Hand
                };
                btn.FlatAppearance.BorderSize = 0;
                var a = accion;
                btn.Click += (s, e) => a();
                pnlBot.Controls.Add(btn);
                x += 158;
            }

            Controls.Add(pnlBot);
        }

        // ════════════════════════════════════════════════════════════════
        //  LOAD
        // ════════════════════════════════════════════════════════════════
        private void OnLoad(object sender, EventArgs e)
        {
            // Tasa vigente
            try
            {
                var tasa = DatabaseService.ObtenerTasaHoy();
                if (tasa != null)
                    lblTasa.Text = $"BCV {tasa.TasaBCV:N2}  ·  Par {tasa.TasaParalelo:N2}";
            }
            catch { }

            // Alertas
            try
            {
                var alertas = AlertasService.ObtenerAlertas();
                if (alertas.TieneAlertas)
                {
                    var partes = new List<string>();
                    if (alertas.StockBajo.Count   > 0) partes.Add($"⚠ Stock bajo: {alertas.StockBajo.Count}");
                    if (alertas.CxCVencidas.Count > 0) partes.Add($"⚠ CxC vencidas: {alertas.CxCVencidas.Count}");
                    if (alertas.MantVencido.Count  > 0) partes.Add($"⚠ Mantenimiento: {alertas.MantVencido.Count}");

                    string texto = string.Join("   |   ", partes);

                    // Topbar badge
                    lblAlertaBar.Text    = $"● {alertas.TotalAlertas} alertas";
                    lblAlertaBar.Visible = true;

                    // Barra inferior
                    var lbl = pnlAlertaBar.Controls.Find("lblAlertaDetalle", false)
                                          .FirstOrDefault() as Label;
                    if (lbl != null) lbl.Text = "  " + texto;

                    bool critico = alertas.CxCVencidas.Count > 0;
                    pnlAlertaBar.BackColor = critico
                        ? Color.FromArgb(253, 235, 235)
                        : Color.FromArgb(255, 243, 205);
                    pnlAlertaBar.Visible = true;
                }
            }
            catch { }

            // Clientes de prueba (legacy)
            try
            {
                string cs = "Data Source=SistemaGestion.db";
                if (CargarClientesHelper.ContarClientesExistentes(cs) == 0)
                {
                    CargarClientesHelper.InsertarClientesDePrueba(cs);
                    MessageBox.Show("10 clientes de prueba cargados.",
                        "Inicialización", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            catch { }
        }

        // ════════════════════════════════════════════════════════════════
        //  TOGGLE TEMA
        // ════════════════════════════════════════════════════════════════
        private void ToggleTema()
        {
            _darkMode = !_darkMode;
            // Guardar preferencia
            DatabaseService.GuardarConfig("TemaOscuro", _darkMode ? "1" : "0");
            // Reconstruir UI
            Controls.Clear();
            BackColor = CFondo;
            BuildUI();
            OnLoad(this, EventArgs.Empty);
        }

        // ════════════════════════════════════════════════════════════════
        //  HELPERS
        // ════════════════════════════════════════════════════════════════
        private static void RoundBorder(Control c, int radio)
        {
            c.Paint += (s, e) => {
                var g = e.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;
                using var penB = new Pen(Color.FromArgb(_darkMode ? 45 : 220,
                    _darkMode ? 63 : 230, _darkMode ? 85 : 245), 1f);
                using var path = RoundRect(new Rectangle(0, 0, c.Width - 1, c.Height - 1), radio);
                g.DrawPath(penB, path);
            };
            c.Region = System.Drawing.Region.FromHrgn(
                CreateRoundRectRgn(0, 0, c.Width, c.Height, radio, radio));
        }

        [System.Runtime.InteropServices.DllImport("Gdi32.dll")]
        private static extern IntPtr CreateRoundRectRgn(
            int nLeftRect, int nTopRect, int nRightRect, int nBottomRect,
            int nWidthEllipse, int nHeightEllipse);

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

        private Button TopBtn(string texto)
        {
            var b = new Button {
                Text = texto, Size = new Size(90, 30),
                BackColor = Color.FromArgb(50, 100, 180),
                ForeColor = Color.White, FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 8f), Cursor = Cursors.Hand,
                Margin = new Padding(0, 0, 4, 0)
            };
            b.FlatAppearance.BorderSize = 0;
            return b;
        }

        // ════════════════════════════════════════════════════════════════
        //  HANDLERS
        // ════════════════════════════════════════════════════════════════
        private void BtnCerrarSesion_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show("¿Cerrar sesión?", "Confirmar",
                MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;
            BackupService.HacerRespaldo(manual: false);
            DatabaseService.RegistrarAuditoria("LOGOUT", "Usuarios");
            Sesion.Cerrar();
            Hide();
            using var login = new FrmLogin();
            if (login.ShowDialog() == DialogResult.OK)
            {
                Controls.Clear(); BackColor = CFondo;
                BuildUI(); OnLoad(this, EventArgs.Empty);
                Show();
            }
            else Application.Exit();
        }

        private void BtnSalir_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show("¿Desea salir del sistema?", "Confirmar salida",
                MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                DatabaseService.RegistrarAuditoria("SALIDA", "Sistema");
                Application.Exit();
            }
        }
    }
}
