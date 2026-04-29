using System;
using System.Drawing;
using System.Windows.Forms;

namespace SistemaGestion
{
    public class FrmLogin : FormBase
    {
        private TextBox txtUsuario = null!;
        private TextBox txtPassword = null!;
        private Label   lblError = null!;
        private Button  btnEntrar = null!;
        private CheckBox chkMostrar = null!;
        private ValidacionInline _v = null!;

        private int _intentosFallidosSesion = 0;

        public FrmLogin()
        {
            ConstruirUI();
        }

        // ── Construcción de la UI ────────────────────────────────────────────
        private void ConstruirUI()
        {
            Text            = "Iniciar Sesión — Sistema de Gestión";
            ClientSize      = new Size(440, 520);
            StartPosition   = FormStartPosition.CenterScreen;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox     = false;
            MinimizeBox     = false;
            BackColor       = UITheme.Fondo;
            DoubleBuffered  = true;   // evita parpadeo al redimensionar/DPI

            // ── Layout raíz: 3 filas (banner / contenido / mensaje) ────────
            var root = new TableLayoutPanel
            {
                Dock       = DockStyle.Fill,
                ColumnCount = 1,
                RowCount    = 3,
                BackColor  = UITheme.Fondo
            };
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 100f));   // banner
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));    // contenido
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 36f));    // mensaje

            // ── Banner ────────────────────────────────────────────────────
            var banner = new Panel
            {
                Dock      = DockStyle.Fill,
                BackColor = UITheme.AzulOscuro,
                Tag       = "no-theme"
            };
            var lblTituloBanner = new Label
            {
                Text      = "SISTEMA DE GESTIÓN",
                Font      = new Font("Segoe UI", 17f, FontStyle.Bold),
                ForeColor = Color.White,
                BackColor = UITheme.AzulOscuro,
                AutoSize  = false,
                TextAlign = ContentAlignment.MiddleCenter,
                Dock      = DockStyle.Fill
            };
            banner.Controls.Add(lblTituloBanner);
            root.Controls.Add(banner, 0, 0);

            // ── Contenido (formulario) ─────────────────────────────────────
            var contenido = new TableLayoutPanel
            {
                Dock        = DockStyle.Fill,
                ColumnCount = 1,
                RowCount    = 8,
                Padding     = new Padding(48, 24, 48, 24),
                BackColor   = UITheme.Fondo
            };
            contenido.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            // Subtitulo / lblU / txtU / lblP / txtP / chkMostrar / btnEntrar : todos AutoSize.
            for (int i = 0; i < 7; i++)
                contenido.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            // Ultima fila: spacer flexible que absorbe el espacio sobrante
            // para que el boton no se "estire" hasta el fondo del form.
            contenido.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));

            var lblTitulo = new Label
            {
                Text      = "Ingresa tus credenciales",
                Font      = UITheme.Body,
                ForeColor = UITheme.TextoTenue,
                AutoSize  = false,
                TextAlign = ContentAlignment.MiddleCenter,
                Dock      = DockStyle.Fill,
                Height    = 24,
                Margin    = new Padding(0, 0, 0, 12)
            };
            contenido.Controls.Add(lblTitulo, 0, 0);

            var lblU = new Label
            {
                Text      = "Usuario",
                Font      = UITheme.BodyBold,
                ForeColor = UITheme.AzulOscuro,
                AutoSize  = true,
                Margin    = new Padding(0, 0, 0, 4)
            };
            contenido.Controls.Add(lblU, 0, 1);

            txtUsuario = new TextBox
            {
                Font        = new Font("Segoe UI", 11f),
                BorderStyle = BorderStyle.FixedSingle,
                MaxLength   = 50,
                Dock        = DockStyle.Fill,
                Margin      = new Padding(0, 0, 0, 12)
            };
            txtUsuario.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Enter) { txtPassword.Focus(); e.SuppressKeyPress = true; }
            };
            contenido.Controls.Add(txtUsuario, 0, 2);

            var lblP = new Label
            {
                Text      = "Contraseña",
                Font      = UITheme.BodyBold,
                ForeColor = UITheme.AzulOscuro,
                AutoSize  = true,
                Margin    = new Padding(0, 0, 0, 4)
            };
            contenido.Controls.Add(lblP, 0, 3);

            txtPassword = new TextBox
            {
                Font          = new Font("Segoe UI", 11f),
                BorderStyle   = BorderStyle.FixedSingle,
                PasswordChar  = '●',
                MaxLength     = 100,
                Dock          = DockStyle.Fill,
                Margin        = new Padding(0, 0, 0, 8)
            };
            txtPassword.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Enter) { Intentar(); e.SuppressKeyPress = true; }
            };
            contenido.Controls.Add(txtPassword, 0, 4);

            chkMostrar = new CheckBox
            {
                Text      = "Mostrar contraseña",
                AutoSize  = true,
                Font      = UITheme.Body,
                ForeColor = UITheme.TextoTenue,
                Margin    = new Padding(0, 0, 0, 16)
            };
            chkMostrar.CheckedChanged += (s, e) =>
                txtPassword.PasswordChar = chkMostrar.Checked ? '\0' : '●';
            contenido.Controls.Add(chkMostrar, 0, 5);

            btnEntrar = UITheme.BotonPrimario("Entrar");
            // Anchor.Top|Left|Right en lugar de Dock.Fill: el boton crece en
            // ancho con el contenedor pero NO se estira en altura cuando
            // sobra espacio vertical. Mantiene altura fija de 44px.
            btnEntrar.Anchor   = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            btnEntrar.Height   = 44;
            btnEntrar.Margin   = new Padding(0, 0, 0, 0);
            btnEntrar.AutoSize = false;
            btnEntrar.Click += (s, e) => Intentar();
            contenido.Controls.Add(btnEntrar, 0, 6);

            root.Controls.Add(contenido, 0, 1);

            // ── Mensaje de error (fila inferior) ───────────────────────────
            lblError = new Label
            {
                Text      = "",
                Dock      = DockStyle.Fill,
                Font      = UITheme.BodyBold,
                ForeColor = UITheme.Rojo,
                BackColor = Color.FromArgb(253, 235, 235),
                AutoSize  = false,
                TextAlign = ContentAlignment.MiddleCenter,
                Visible   = false
            };
            root.Controls.Add(lblError, 0, 2);

            this.Controls.Add(root);
            this.AcceptButton = btnEntrar;

            _v = new ValidacionInline(this);
            this.FormClosed += (s, e) => _v?.Dispose();
            this.Shown      += (s, e) => txtUsuario.Focus();
        }

        // ── Lógica de autenticación ──────────────────────────────────────────
        private void Intentar()
        {
            _v.Limpiar();
            _v.Requerido(txtUsuario, "Ingresa tu nombre de usuario.")
              .MaxLongitud(txtUsuario, 50)
              .Requerido(txtPassword, "Ingresa tu contraseña.")
              .MaxLongitud(txtPassword, 100);
            if (!_v.EsValido)
            {
                (txtUsuario.Text.Trim().Length == 0 ? txtUsuario : txtPassword).Focus();
                return;
            }

            OcultarError();
            btnEntrar.Enabled = false;
            btnEntrar.Text    = "Verificando...";

            var usuario = DatabaseService.ValidarLogin(
                txtUsuario.Text.Trim().ToLower(),
                txtPassword.Text);

            if (usuario != null)
            {
                Sesion.IniciarDesde(usuario);

                DatabaseService.RegistrarAuditoria("LOGIN", "Usuarios",
                    $"Acceso exitoso — Rol: {usuario.Rol}");

                if (usuario.RequiereCambioPassword)
                {
                    var msg = usuario.NombreUsuario == "admin"
                        ? "Primer acceso detectado.\n\nDebe cambiar la contraseña antes de continuar.\n\nLa contraseña inicial 'admin123' no es segura."
                        : "Su contraseña debe ser cambiada antes de continuar.";

                    MessageBox.Show(this, msg, "Cambio de contraseña requerido",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);

                    using var frmCambio = new FrmCambioContrasena(usuario.NombreUsuario, forzado: true);
                    if (frmCambio.ShowDialog(this) != DialogResult.OK)
                    {
                        Sesion.Cerrar();
                        MostrarError("Debe cambiar la contraseña para acceder al sistema.");
                        return;
                    }
                }

                DialogResult = DialogResult.OK;
                Close();
            }
            else
            {
                _intentosFallidosSesion++;

                var usuarioAnon = txtUsuario.Text.Trim().Length > 0
                    ? txtUsuario.Text.Trim()[0] + "***"
                    : "(vacío)";

                DatabaseService.RegistrarAuditoria("LOGIN_FALLIDO", "Usuarios",
                    $"Intento fallido — usuario: {usuarioAnon}");

                if (_intentosFallidosSesion >= 5)
                {
                    MostrarError("Demasiados intentos fallidos. La cuenta ha sido bloqueada 30 minutos.");
                    System.Threading.Tasks.Task.Delay(2500)
                        .ContinueWith(_ => this.Invoke(() => Application.Exit()),
                            System.Threading.Tasks.TaskScheduler.Default);
                    return;
                }

                MostrarError("Usuario o contraseña incorrectos.");
                txtPassword.Clear();
                txtPassword.Focus();

                btnEntrar.Enabled = true;
                btnEntrar.Text    = "Entrar";
            }
        }

        private void MostrarError(string msg)
        {
            lblError.Text     = "  " + msg;
            lblError.Visible  = true;
            btnEntrar.Enabled = true;
            btnEntrar.Text    = "Entrar";
        }
        private void OcultarError() => lblError.Visible = false;
    }
}
