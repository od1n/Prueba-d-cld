using System;
using System.Drawing;
using System.Windows.Forms;

namespace SistemaGestion
{
    public class MenuPrincipal : Form
    {
        // Declarar los botones como campos de clase
        private Button btnDashboard;
        private Button btnArchivos;
        private Button btnTransacciones;
        private Button btnCompras;
        private Button btnNomina;
        private Button btnSistema;
        private Button btnReportes;
        private Button btnSalir;

        public MenuPrincipal()
        {
            InitializeComponent();
            this.Load += MenuPrincipal_Load;
        }

        private void InitializeComponent()
        {
            this.Text = "Menu_Principal";
            this.KeyPreview = true;
            this.Size = new Size(620, 560);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.BackColor = Color.FromArgb(236, 236, 236);
            this.Font = new Font("Segoe UI", 9f);

            // ── Barra de sesión ───────────────────────────────────────────
            var barSesion = new Panel
            {
                Dock      = DockStyle.Top,
                Height    = 38,
                BackColor = Color.FromArgb(30, 70, 140)
            };

            var lblSesion = new Label
            {
                Text      = $"  Usuario: {Sesion.NombreCompleto}   |   Rol: {Sesion.Rol}",
                Font      = new Font("Segoe UI", 9f),
                ForeColor = Color.White,
                AutoSize  = true,
                Location  = new Point(8, 10)
            };

            var btnPerfil = new Button
            {
                Text      = "Mi Perfil",
                Size      = new Size(80, 24),
                Location  = new Point(290, 7),
                BackColor = Color.FromArgb(55, 70, 110),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font      = new Font("Segoe UI", 8f, FontStyle.Bold),
                Cursor    = Cursors.Hand
            };
            btnPerfil.FlatAppearance.BorderSize = 0;
            btnPerfil.Click += (s, e) => new FrmPerfil().ShowDialog();
            barSesion.Controls.Add(btnPerfil);

            var btnBuscar = new Button
            {
                Text      = "Buscar (F3)",
                Size      = new Size(100, 24),
                Location  = new Point(378, 7),
                BackColor = Color.FromArgb(50, 100, 170),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font      = new Font("Segoe UI", 8f, FontStyle.Bold),
                Cursor    = Cursors.Hand
            };
            btnBuscar.FlatAppearance.BorderSize = 0;
            btnBuscar.Click += (s, e) => new FrmBuscador().ShowDialog();
            barSesion.Controls.Add(btnBuscar);

            var btnCerrarSesion = new Button
            {
                Text      = "Cerrar sesion",
                Size      = new Size(110, 24),
                Location  = new Point(484, 7),
                BackColor = Color.FromArgb(180, 50, 50),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font      = new Font("Segoe UI", 8f, FontStyle.Bold),
                Cursor    = Cursors.Hand
            };
            btnCerrarSesion.FlatAppearance.BorderSize = 0;
            btnCerrarSesion.Click += (s, e) =>
            {
                if (MessageBox.Show("Cerrar sesion?", "Confirmar",
                    MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                {
                    DatabaseService.RegistrarAuditoria("LOGOUT", "Usuarios");
                    Sesion.Cerrar();
                    Hide();
                    using var login = new FrmLogin();
                    if (login.ShowDialog() == DialogResult.OK)
                        Show();
                    else
                        Application.Exit();
                }
            };

            // Boton de auditoria solo para Admin
            if (Sesion.EsAdmin)
            {
                var btnAudit = new Button
                {
                    Text      = "Auditoria",
                    Size      = new Size(80, 24),
                    Location  = new Point(290, 7),
                    BackColor = Color.FromArgb(40, 55, 90),
                    ForeColor = Color.White,
                    FlatStyle = FlatStyle.Flat,
                    Font      = new Font("Segoe UI", 8f, FontStyle.Bold),
                    Cursor    = Cursors.Hand
                };
                btnAudit.FlatAppearance.BorderSize = 0;
                btnAudit.Click += (s, e) => new FrmLogAuditoria().ShowDialog();
                barSesion.Controls.Add(btnAudit);
            }

            if (Sesion.EsAdmin)
            {
                var btnUsuarios = new Button
                {
                    Text      = "Usuarios",
                    Size      = new Size(80, 24),
                    Location  = new Point(398, 7),
                    BackColor = Color.FromArgb(60, 120, 200),
                    ForeColor = Color.White,
                    FlatStyle = FlatStyle.Flat,
                    Font      = new Font("Segoe UI", 8f, FontStyle.Bold),
                    Cursor    = Cursors.Hand
                };
                btnUsuarios.FlatAppearance.BorderSize = 0;
                btnUsuarios.Click += (s, e) => new FrmUsuarios().ShowDialog();
                barSesion.Controls.Add(btnUsuarios);
            }

            barSesion.Controls.AddRange(new Control[] { lblSesion, btnCerrarSesion });

            // ── Título ────────────────────────────────────────────────────
            var lblTitulo = new Label
            {
                Text      = "SISTEMA DE GESTION",
                Font      = new Font("Segoe UI", 12f, FontStyle.Bold),
                ForeColor = Color.FromArgb(50, 50, 120),
                AutoSize  = false,
                TextAlign = ContentAlignment.MiddleCenter,
                Dock      = DockStyle.Top,
                Height    = 40,
                BackColor = Color.FromArgb(220, 220, 240)
            };

            // ── Botones principales ───────────────────────────────────────
            btnDashboard     = CrearBoton("Dashboard",            Color.FromArgb(22, 55, 120),  new Point(60,  60));
            btnArchivos      = CrearBoton("Archivos del sistema", Color.FromArgb(180, 140, 20), new Point(270, 60));
            btnTransacciones = CrearBoton("Transacciones",        Color.FromArgb(60, 100, 180), new Point(60,  220));
            btnCompras       = CrearBoton("Compras",              Color.FromArgb(100, 50, 160), new Point(270, 220));
            btnNomina        = CrearBoton("Nomina",               Color.FromArgb(30, 120, 60),  new Point(480, 60));
            btnNomina.Size   = new Size(100, 350);

            // Aplicar restricciones por rol
            AplicarPermiso(btnDashboard,     "Reportes");
            AplicarPermiso(btnArchivos,      "Archivos");
            AplicarPermiso(btnTransacciones, "Transacciones");
            AplicarPermiso(btnCompras,       "Compras");

            // ── Panel inferior ────────────────────────────────────────────
            var panelBottom = new Panel
            {
                Dock      = DockStyle.Bottom,
                Height    = 75,
                BackColor = Color.FromArgb(210, 210, 210)
            };

            btnReportes = new Button
            {
                Text      = "Reportes",
                Size      = new Size(130, 45),
                Location  = new Point(10, 15),
                BackColor = Color.FromArgb(140, 20, 20),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font      = new Font("Segoe UI", 9f, FontStyle.Bold),
                Cursor    = Cursors.Hand
            };
            btnReportes.FlatAppearance.BorderSize = 0;

            btnSistema = new Button
            {
                Text      = "Sistema",
                Size      = new Size(130, 45),
                Location  = new Point(150, 15),
                BackColor = Color.FromArgb(70, 130, 180),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font      = new Font("Segoe UI", 9f, FontStyle.Bold),
                Cursor    = Cursors.Hand
            };
            btnSistema.FlatAppearance.BorderSize = 0;
            if (!Sesion.EsAdmin) btnSistema.Enabled = false;

            btnSalir = new Button
            {
                Text      = "Salir",
                Size      = new Size(130, 45),
                Location  = new Point(310, 15),
                BackColor = Color.FromArgb(180, 60, 60),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font      = new Font("Segoe UI", 9f, FontStyle.Bold),
                Cursor    = Cursors.Hand
            };
            btnSalir.FlatAppearance.BorderSize = 0;

            panelBottom.Controls.AddRange(new Control[] { btnReportes, btnSistema, btnSalir });

            // ── Hovers ───────────────────────────────────────────────────
            AplicarHover(btnDashboard,     Color.FromArgb(50, 90, 160),   Color.FromArgb(22, 55, 120));
            AplicarHover(btnArchivos,      Color.FromArgb(200, 160, 40),  Color.FromArgb(180, 140, 20));
            AplicarHover(btnTransacciones, Color.FromArgb(80, 120, 200),  Color.FromArgb(60, 100, 180));
            AplicarHover(btnCompras,       Color.FromArgb(130, 80, 190),  Color.FromArgb(100, 50, 160));

            this.Controls.AddRange(new Control[]
            {
                lblTitulo, barSesion,
                btnDashboard, btnArchivos,
                btnTransacciones, btnCompras,
                btnNomina,
                panelBottom
            });
        }

        private void MenuPrincipal_Load(object sender, EventArgs e)
        {
            // Cargar clientes de prueba si la BD esta vacia
            try
            {
                string connString = "Data Source=SistemaGestion.db";
                int clientesCount = CargarClientesHelper.ContarClientesExistentes(connString);

                if (clientesCount == 0)
                {
                    CargarClientesHelper.InsertarClientesDePrueba(connString);
                    MessageBox.Show("10 clientes de prueba cargados exitosamente",
                        "Base de datos inicializada", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Nota: {ex.Message}");
            }

            // Atajo F3 para abrir el buscador desde cualquier parte
            KeyDown += (s, ev) => { if (ev.KeyCode == Keys.F3) new FrmBuscador().ShowDialog(); };

            btnNomina.Click        += (s, ev) => new FrmNomina().ShowDialog();
            btnDashboard.Click     += (s, ev) => { if (VerificarPermiso("Reportes"))      new FrmDashboard().ShowDialog(); };
            btnArchivos.Click      += (s, ev) => { if (VerificarPermiso("Archivos"))      new FrmArchivos().ShowDialog(); };
            btnTransacciones.Click += (s, ev) => { if (VerificarPermiso("Transacciones")) new FrmTransacciones().ShowDialog(); };
            btnCompras.Click       += (s, ev) => { if (VerificarPermiso("Compras"))       new FrmCompras().ShowDialog(); };
            btnSistema.Click       += (s, ev) => new FrmSistema().ShowDialog();
            btnReportes.Click      += (s, ev) => new FrmReportes().ShowDialog();
            btnSalir.Click         += (s, ev) =>
            {
                if (MessageBox.Show("Desea salir del sistema?", "Confirmar salida",
                    MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                {
                    DatabaseService.RegistrarAuditoria("SALIDA", "Sistema");
                    Application.Exit();
                }
            };
        }

        // ── Helpers de permisos ───────────────────────────────────────────
        private void AplicarPermiso(Button btn, string modulo)
        {
            if (!Sesion.TienePermiso(modulo))
            {
                btn.BackColor = Color.FromArgb(160, 160, 165);
                btn.Cursor    = Cursors.No;
                btn.Text      = btn.Text + "\n(sin acceso)";
            }
        }

        private bool VerificarPermiso(string modulo)
        {
            if (Sesion.TienePermiso(modulo)) return true;
            MessageBox.Show(
                $"Tu rol ({Sesion.Rol}) no tiene permiso para acceder a este modulo.",
                "Acceso denegado", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return false;
        }

        private Button CrearBoton(string texto, Color color, Point location)
        {
            var btn = new Button
            {
                Text      = texto,
                Size      = new Size(200, 150),
                Location  = location,
                BackColor = color,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font      = new Font("Segoe UI", 10f, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleCenter,
                Cursor    = Cursors.Hand
            };
            btn.FlatAppearance.BorderSize = 0;
            return btn;
        }

        private void AplicarHover(Button btn, Color colorHover, Color colorNormal)
        {
            btn.MouseEnter += (s, e) =>
            {
                if (Sesion.TienePermiso(btn.Tag?.ToString() ?? ""))
                    btn.BackColor = colorHover;
            };
            btn.MouseLeave += (s, e) =>
            {
                if (Sesion.TienePermiso(btn.Tag?.ToString() ?? ""))
                    btn.BackColor = colorNormal;
            };
        }
    }
}
