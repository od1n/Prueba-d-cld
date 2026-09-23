using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace SistemaGestion
{
    public class FrmLogin : FormBase
    {
        // Login usa su propio diseño neumórfico (con tokens de Tema); el
        // aplicador global no debe reescribirlo.
        protected override bool TemaAutomatico => false;

        private TextBox txtUsuario = null!;
        private TextBox txtPassword = null!;
        private Label   lblError = null!;
        private Button  btnEntrar = null!;
        private CheckBox chkMostrar = null!;
        private Panel   _card = null!;
        private Panel   _pnlIcono = null!;
        private ValidacionInline _v = null!;

        private int _intentosFallidosSesion = 0;

        public FrmLogin()
        {
            ConstruirUI();
        }

        // ── Construcción de la UI (neumórfica) ───────────────────────────────
        private void ConstruirUI()
        {
            Text            = "Iniciar Sesión — Sistema de Gestión";
            ClientSize      = new Size(440, 580);
            StartPosition   = FormStartPosition.CenterScreen;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox     = false;
            MinimizeBox     = false;
            BackColor       = Tema.Fondo;
            DoubleBuffered  = true;

            // ── Tarjeta central elevada ────────────────────────────────────
            _card = new Panel { Size = new Size(360, 486), BackColor = Tema.Fondo };
            _card.Location = new Point((ClientSize.Width - _card.Width) / 2, 44);
            Tema.Redondear(_card, 24);
            // Relieve de la tarjeta (lo pinta el formulario, detrás de ella)
            this.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                UINeo.SombraRelieve(e.Graphics, _card.Bounds, 24);
            };

            // Ícono superior (círculo hundido con candado)
            _pnlIcono = new Panel { Size = new Size(72, 72),
                Location = new Point((_card.Width - 72) / 2, 26), BackColor = Tema.Fondo };
            Tema.Redondear(_pnlIcono, 36);
            _pnlIcono.Paint += (s, e) =>
            {
                var g = e.Graphics; g.SmoothingMode = SmoothingMode.AntiAlias;
                using var f = new Font("Segoe UI Emoji", 22f);
                var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
                using var br = new SolidBrush(Tema.Primario);
                g.DrawString("🔒", f, br, new RectangleF(0, 0, _pnlIcono.Width, _pnlIcono.Height), sf);
            };
            _card.Controls.Add(_pnlIcono);

            // Título + subtítulo
            var lblBienvenido = new Label
            {
                Text = "Bienvenido", Font = new Font("Segoe UI", 19f, FontStyle.Bold),
                ForeColor = Tema.Tinta, AutoSize = false, TextAlign = ContentAlignment.MiddleCenter,
                Bounds = new Rectangle(0, 108, _card.Width, 34)
            };
            var lblSub = new Label
            {
                Text = "Ingresa tus credenciales para continuar",
                Font = new Font("Segoe UI", 9.5f), ForeColor = Tema.TintaSuave,
                AutoSize = false, TextAlign = ContentAlignment.MiddleCenter,
                Bounds = new Rectangle(0, 144, _card.Width, 20)
            };
            _card.Controls.Add(lblBienvenido);
            _card.Controls.Add(lblSub);

            // Usuario
            _card.Controls.Add(Etiqueta("Usuario", 34, 182));
            var wellU = PozoInput(34, 204, 292, 46, out txtUsuario);
            txtUsuario.MaxLength = 50;
            txtUsuario.KeyDown += (s, e) =>
            { if (e.KeyCode == Keys.Enter) { txtPassword.Focus(); e.SuppressKeyPress = true; } };
            _card.Controls.Add(wellU);

            // Contraseña
            _card.Controls.Add(Etiqueta("Contraseña", 34, 262));
            var wellP = PozoInput(34, 284, 292, 46, out txtPassword);
            txtPassword.PasswordChar = '●';
            txtPassword.MaxLength = 100;
            txtPassword.KeyDown += (s, e) =>
            { if (e.KeyCode == Keys.Enter) { Intentar(); e.SuppressKeyPress = true; } };
            _card.Controls.Add(wellP);

            // Mostrar contraseña
            chkMostrar = new CheckBox
            {
                Text = "Mostrar contraseña", AutoSize = true, Font = Tema.Pequeno,
                ForeColor = Tema.TintaSuave, BackColor = Tema.Fondo,
                Location = new Point(34, 338)
            };
            chkMostrar.CheckedChanged += (s, e) =>
                txtPassword.PasswordChar = chkMostrar.Checked ? '\0' : '●';
            _card.Controls.Add(chkMostrar);

            // Botón Entrar (relieve suave, texto azul — como el mockup)
            btnEntrar = new Button
            {
                Text = "Entrar", Bounds = new Rectangle(34, 372, 292, 50),
                FlatStyle = FlatStyle.Flat, BackColor = Tema.Fondo, ForeColor = Tema.Primario,
                Font = new Font("Segoe UI", 11.5f, FontStyle.Bold), Cursor = Cursors.Hand
            };
            btnEntrar.FlatAppearance.BorderSize = 0;
            btnEntrar.FlatAppearance.MouseOverBackColor = Tema.SuperficieAlt;
            Tema.Redondear(btnEntrar, 14);
            btnEntrar.Click += (s, e) => Intentar();
            _card.Controls.Add(btnEntrar);

            // Relieve del ícono y del botón (los pinta la tarjeta, detrás de ellos)
            _card.Paint += (s, e) =>
            {
                var g = e.Graphics; g.SmoothingMode = SmoothingMode.AntiAlias;
                UINeo.SombraRelieve(g, _pnlIcono.Bounds, 36);
                UINeo.SombraRelieve(g, btnEntrar.Bounds, 14);
            };

            // Mensaje de error
            lblError = new Label
            {
                Text = "", Bounds = new Rectangle(34, 432, 292, 40),
                Font = Tema.BodyBold, ForeColor = Tema.Peligro,
                AutoSize = false, TextAlign = ContentAlignment.MiddleCenter, Visible = false
            };
            _card.Controls.Add(lblError);

            this.Controls.Add(_card);
            this.AcceptButton = btnEntrar;

            _v = new ValidacionInline(this);
            this.FormClosed += (s, e) => _v?.Dispose();
            this.Shown      += (s, e) => txtUsuario.Focus();
        }

        // Etiqueta de campo
        private Label Etiqueta(string texto, int x, int y) => new Label
        {
            Text = texto, Font = Tema.BodyBold, ForeColor = Tema.Tinta,
            AutoSize = false, Bounds = new Rectangle(x, y, 292, 18), BackColor = Tema.Fondo
        };

        // Campo de texto dentro de un "pozo" hundido neumórfico
        private Panel PozoInput(int x, int y, int w, int h, out TextBox caja)
        {
            var pozo = new Panel { Bounds = new Rectangle(x, y, w, h), BackColor = Tema.Fondo };
            pozo.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                UINeo.PozoHundido(e.Graphics, new Rectangle(0, 0, pozo.Width - 1, pozo.Height - 1), 12);
            };
            caja = new TextBox
            {
                BorderStyle = BorderStyle.None, BackColor = Tema.Superficie, ForeColor = Tema.Tinta,
                Font = new Font("Segoe UI", 12f),
                Bounds = new Rectangle(16, (h - 22) / 2, w - 32, 22)
            };
            pozo.Controls.Add(caja);
            return pozo;
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
