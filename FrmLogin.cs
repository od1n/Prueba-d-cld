using System;
using System.Drawing;
using System.Windows.Forms;

namespace SistemaGestion
{
    public class FrmLogin : Form
    {
        private TextBox txtUsuario;
        private TextBox txtPassword;
        private Label   lblError;
        private Button  btnEntrar;
        private int     _intentosFallidos = 0;
        private const int MAX_INTENTOS = 5;

        public FrmLogin()
        {
            Text            = "Iniciar Sesión — Sistema de Gestión";
            Size            = new Size(420, 480);
            StartPosition   = FormStartPosition.CenterScreen;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox     = false;
            MinimizeBox     = false;
            BackColor       = Color.FromArgb(245, 247, 252);

            // ── Banner superior ───────────────────────────────────────────
            var banner = new Panel
            {
                Dock      = DockStyle.Top,
                Height    = 110,
                BackColor = Color.FromArgb(30, 70, 140)
            };
            banner.Controls.Add(new Label
            {
                Text      = "SISTEMA DE GESTIÓN",
                Font      = new Font("Segoe UI", 16f, FontStyle.Bold),
                ForeColor = Color.White,
                AutoSize  = false,
                TextAlign = ContentAlignment.MiddleCenter,
                Dock      = DockStyle.Fill
            });

            // ── Formulario centrado ───────────────────────────────────────
            var lblTitulo = new Label
            {
                Text      = "Ingresa tus credenciales",
                Font      = new Font("Segoe UI", 10f),
                ForeColor = Color.FromArgb(80, 80, 100),
                AutoSize  = false,
                TextAlign = ContentAlignment.MiddleCenter,
                Location  = new Point(40, 128),
                Size      = new Size(330, 24)
            };

            // ── Usuario ───────────────────────────────────────────────────
            var lblU = new Label
            {
                Text     = "Usuario",
                Font     = new Font("Segoe UI", 9f, FontStyle.Bold),
                Location = new Point(50, 168),
                AutoSize = true,
                ForeColor= Color.FromArgb(50, 50, 80)
            };
            txtUsuario = new TextBox
            {
                Location    = new Point(50, 188),
                Size        = new Size(310, 32),
                Font        = new Font("Segoe UI", 11f),
                BorderStyle = BorderStyle.FixedSingle,
                MaxLength   = 50
            };
            txtUsuario.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Enter) txtPassword.Focus();
            };

            // ── Contraseña ────────────────────────────────────────────────
            var lblP = new Label
            {
                Text     = "Contraseña",
                Font     = new Font("Segoe UI", 9f, FontStyle.Bold),
                Location = new Point(50, 234),
                AutoSize = true,
                ForeColor= Color.FromArgb(50, 50, 80)
            };
            txtPassword = new TextBox
            {
                Location      = new Point(50, 254),
                Size          = new Size(310, 32),
                Font          = new Font("Segoe UI", 11f),
                BorderStyle   = BorderStyle.FixedSingle,
                PasswordChar  = '●',
                MaxLength     = 100
            };
            txtPassword.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Enter) Intentar();
            };

            // Mostrar/ocultar contraseña
            var chkMostrar = new CheckBox
            {
                Text     = "Mostrar contraseña",
                Location = new Point(50, 292),
                AutoSize = true,
                Font     = new Font("Segoe UI", 8.5f),
                ForeColor= Color.FromArgb(100, 100, 120)
            };
            chkMostrar.CheckedChanged += (s, e) =>
                txtPassword.PasswordChar = chkMostrar.Checked ? '\0' : '●';

            // ── Mensaje de error ──────────────────────────────────────────
            lblError = new Label
            {
                Text      = "",
                Font      = new Font("Segoe UI", 9f, FontStyle.Bold),
                ForeColor = Color.FromArgb(180, 40, 40),
                Location  = new Point(50, 318),
                Size      = new Size(310, 40),
                TextAlign = ContentAlignment.MiddleCenter
            };

            // ── Botón entrar ──────────────────────────────────────────────
            btnEntrar = new Button
            {
                Text      = "Entrar",
                Location  = new Point(50, 364),
                Size      = new Size(310, 44),
                Font      = new Font("Segoe UI", 11f, FontStyle.Bold),
                BackColor = Color.FromArgb(30, 70, 140),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor    = Cursors.Hand
            };
            btnEntrar.FlatAppearance.BorderSize = 0;
            btnEntrar.Click += (s, e) => Intentar();

            // Hover effect
            btnEntrar.MouseEnter += (s, e) => btnEntrar.BackColor = Color.FromArgb(50, 100, 180);
            btnEntrar.MouseLeave += (s, e) => btnEntrar.BackColor = Color.FromArgb(30, 70, 140);

            // ── Footer de versión ─────────────────────────────────────────
            var lblVersion = new Label
            {
                Text      = "v1.0  —  usuario por defecto: admin / admin123",
                Font      = new Font("Segoe UI", 7.5f),
                ForeColor = Color.FromArgb(160, 160, 175),
                AutoSize  = false,
                TextAlign = ContentAlignment.MiddleCenter,
                Location  = new Point(40, 422),
                Size      = new Size(330, 20)
            };

            Controls.AddRange(new Control[]
            {
                banner, lblTitulo, lblU, txtUsuario,
                lblP, txtPassword, chkMostrar,
                lblError, btnEntrar, lblVersion
            });

            // Foco inicial
            Shown += (s, e) => txtUsuario.Focus();
        }

        // ── Lógica de autenticación ───────────────────────────────────────
        private void Intentar()
        {
            lblError.Text = "";

            if (string.IsNullOrWhiteSpace(txtUsuario.Text))
            {
                MostrarError("Ingresa tu nombre de usuario.");
                txtUsuario.Focus();
                return;
            }
            if (string.IsNullOrWhiteSpace(txtPassword.Text))
            {
                MostrarError("Ingresa tu contraseña.");
                txtPassword.Focus();
                return;
            }

            btnEntrar.Enabled = false;
            btnEntrar.Text    = "Verificando...";

            var usuario = DatabaseService.ValidarLogin(
                txtUsuario.Text.Trim().ToLower(),
                txtPassword.Text);

            if (usuario != null)
            {
                // Login exitoso — cargar sesión
                Sesion.UsuarioId      = usuario.Id;
                Sesion.NombreUsuario  = usuario.NombreUsuario;
                Sesion.NombreCompleto = usuario.NombreCompleto;
                Sesion.Rol            = usuario.Rol;

                DatabaseService.RegistrarAuditoria("LOGIN", "Usuarios",
                    $"Acceso exitoso — Rol: {usuario.Rol}");

                DialogResult = DialogResult.OK;
                Close();
            }
            else
            {
                _intentosFallidos++;
                DatabaseService.RegistrarAuditoria("LOGIN_FALLIDO", "Usuarios",
                    $"Intento fallido para usuario: {txtUsuario.Text.Trim()}");

                if (_intentosFallidos >= MAX_INTENTOS)
                {
                    MostrarError($"Demasiados intentos fallidos.\nEl sistema se cerrará.");
                    System.Threading.Tasks.Task.Delay(2000)
                        .ContinueWith(_ => Application.Exit());
                    return;
                }

                MostrarError(
                    $"Usuario o contraseña incorrectos.\n" +
                    $"Intentos restantes: {MAX_INTENTOS - _intentosFallidos}");

                txtPassword.Clear();
                txtPassword.Focus();

                btnEntrar.Enabled = true;
                btnEntrar.Text    = "Entrar";
            }
        }

        private void MostrarError(string msg)
        {
            lblError.Text     = msg;
            btnEntrar.Enabled = true;
            btnEntrar.Text    = "Entrar";
        }
    }
}
