using System;
using System.Drawing;
using System.Windows.Forms;

namespace SistemaGestion
{
    // ══════════════════════════════════════════════════════════════════════════
    //  PERFIL DE USUARIO — Cambio de contraseña + Preferencias
    // ══════════════════════════════════════════════════════════════════════════
    public class FrmPerfil : Form
    {
        // ── Tabs ──────────────────────────────────────────────────────────
        private TabControl tabs;

        // ── Tab Info ──────────────────────────────────────────────────────
        private TextBox txtNombreCompleto, txtEmail, txtTelefono;

        // ── Tab Contraseña ────────────────────────────────────────────────
        private TextBox txtActual, txtNueva, txtConfirmar;
        private Label   lblFuerzaTexto;
        private Panel   pnlFuerza;

        // ── Tab Preferencias ──────────────────────────────────────────────
        private ComboBox      cmbColorAcento;
        private CheckBox      chkKpis, chkSonido, chkConfirmar, chkUltimoMod;
        private NumericUpDown numFilas;

        private PreferenciasUsuario _prefs;

        public FrmPerfil()
        {
            Text            = "Mi Perfil — " + Sesion.NombreCompleto;
            Size            = new Size(540, 520);
            StartPosition   = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox     = MinimizeBox = false;
            BackColor       = Color.FromArgb(245, 247, 252);

            _prefs = DatabaseService.ObtenerPreferencias(Sesion.NombreUsuario);

            BuildBanner();
            BuildTabs();
            BuildFooter();
        }

        // ── Banner ────────────────────────────────────────────────────────
        private void BuildBanner()
        {
            var banner = new Panel
            {
                Dock = DockStyle.Top, Height = 72,
                BackColor = Color.FromArgb(40, 55, 90)
            };

            // Avatar circular (iniciales)
            var avatar = new Panel
            {
                Location  = new Point(16, 12),
                Size      = new Size(48, 48),
                BackColor = Color.FromArgb(80, 110, 180)
            };
            avatar.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                e.Graphics.FillEllipse(
                    new SolidBrush(Color.FromArgb(80, 110, 180)),
                    0, 0, 47, 47);
                string iniciales = "";
                var partes = (Sesion.NombreCompleto ?? "U").Split(' ');
                foreach (var p in partes)
                    if (p.Length > 0) iniciales += p[0];
                iniciales = iniciales.Substring(0, Math.Min(2, iniciales.Length)).ToUpper();
                using var f = new Font("Segoe UI", 16f, FontStyle.Bold);
                e.Graphics.DrawString(iniciales, f, Brushes.White,
                    new RectangleF(0, 0, 48, 48),
                    new StringFormat
                    {
                        Alignment = StringAlignment.Center,
                        LineAlignment = StringAlignment.Center
                    });
            };

            banner.Controls.Add(avatar);
            banner.Controls.Add(new Label
            {
                Text      = Sesion.NombreCompleto,
                Font      = new Font("Segoe UI", 12f, FontStyle.Bold),
                ForeColor = Color.White,
                AutoSize  = true,
                Location  = new Point(76, 14)
            });
            banner.Controls.Add(new Label
            {
                Text      = "@" + Sesion.NombreUsuario + "  |  " + Sesion.Rol,
                Font      = new Font("Segoe UI", 9f),
                ForeColor = Color.FromArgb(180, 200, 240),
                AutoSize  = true,
                Location  = new Point(76, 40)
            });

            Controls.Add(banner);
        }

        // ── Tabs ──────────────────────────────────────────────────────────
        private void BuildTabs()
        {
            tabs = new TabControl
            {
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 9.5f)
            };
            tabs.TabPages.Add(BuildTabInfo());
            tabs.TabPages.Add(BuildTabContrasena());
            tabs.TabPages.Add(BuildTabPreferencias());
            Controls.Add(tabs);
        }

        // ── Tab 1: Informacion personal ───────────────────────────────────
        private TabPage BuildTabInfo()
        {
            var tab = new TabPage("Informacion personal");
            tab.BackColor = Color.FromArgb(245, 247, 252);

            int y = 20;
            void L(string t) { tab.Controls.Add(new Label
            {
                Text = t, Location = new Point(20, y + 3), AutoSize = true,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold)
            }); }
            TextBox T(string v) { var tb = new TextBox
            {
                Text = v, Location = new Point(180, y), Size = new Size(300, 26),
                Font = new Font("Segoe UI", 10f)
            }; tab.Controls.Add(tb); return tb; }

            // Solo lectura
            tab.Controls.Add(new Label
            {
                Text = "Usuario:", Location = new Point(20, y + 3), AutoSize = true,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold)
            });
            tab.Controls.Add(new TextBox
            {
                Text = Sesion.NombreUsuario, Location = new Point(180, y),
                Size = new Size(300, 26), ReadOnly = true,
                Font = new Font("Segoe UI", 10f),
                BackColor = Color.FromArgb(235, 237, 245)
            });
            y += 38;

            tab.Controls.Add(new Label
            {
                Text = "Rol:", Location = new Point(20, y + 3), AutoSize = true,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold)
            });
            var lblRol = new Label
            {
                Text = Sesion.Rol, Location = new Point(180, y),
                AutoSize = true, Font = new Font("Segoe UI", 10f, FontStyle.Bold),
                ForeColor = Color.FromArgb(40, 80, 160)
            };
            tab.Controls.Add(lblRol);
            y += 38;

            L("Nombre completo:");  txtNombreCompleto = T(_prefs.NombreCompleto); y += 38;
            L("Email:");            txtEmail          = T(_prefs.Email);           y += 38;
            L("Telefono:");         txtTelefono       = T(_prefs.Telefono);        y += 38;

            // Ultima sesion
            y += 10;
            tab.Controls.Add(new Label
            {
                Text = "Miembro desde: " +
                    (DatabaseService.ObtenerUsuarios()
                     .Find(u => u.NombreUsuario == Sesion.NombreUsuario)?
                     .FechaCreacion.ToString("dd/MM/yyyy") ?? "—"),
                Location = new Point(20, y), AutoSize = true,
                Font = new Font("Segoe UI", 8.5f, FontStyle.Italic),
                ForeColor = Color.FromArgb(120, 125, 145)
            });
            y += 28;

            var btnGuardar = BtnPrimario("Guardar informacion", new Point(180, y));
            btnGuardar.Click += (s, e) => GuardarInfo();
            tab.Controls.Add(btnGuardar);

            return tab;
        }

        // ── Tab 2: Cambio de contraseña ───────────────────────────────────
        private TabPage BuildTabContrasena()
        {
            var tab = new TabPage("Cambiar contrasena");
            tab.BackColor = Color.FromArgb(245, 247, 252);

            int y = 20;

            // Info box
            var info = new Panel
            {
                Location = new Point(20, y), Size = new Size(460, 46),
                BackColor = Color.FromArgb(225, 235, 255)
            };
            info.Controls.Add(new Label
            {
                Text = "La contrasena debe tener al menos 6 caracteres.\n" +
                       "Usa letras, numeros y simbolos para mayor seguridad.",
                Location = new Point(10, 6), Size = new Size(440, 34),
                Font = new Font("Segoe UI", 8.5f), ForeColor = Color.FromArgb(40, 60, 140)
            });
            tab.Controls.Add(info);
            y += 60;

            void L(string t) => tab.Controls.Add(new Label
            {
                Text = t, Location = new Point(20, y + 3), AutoSize = true,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold)
            });
            TextBox Pwd(string placeholder)
            {
                var tb = new TextBox
                {
                    Location         = new Point(180, y),
                    Size             = new Size(280, 26),
                    PasswordChar     = '●',
                    Font             = new Font("Segoe UI", 10f),
                    PlaceholderText  = placeholder
                };
                tab.Controls.Add(tb);
                return tb;
            }

            L("Contrasena actual:");   txtActual    = Pwd("Ingresa tu contrasena actual"); y += 38;
            L("Nueva contrasena:");    txtNueva     = Pwd("Minimo 6 caracteres");
            txtNueva.TextChanged += (s, e) => ActualizarIndicadorFuerza();
            y += 38;
            L("Confirmar nueva:");     txtConfirmar = Pwd("Repite la nueva contrasena");
            txtConfirmar.TextChanged += (s, e) => ActualizarIndicadorFuerza();
            y += 38;

            // Indicador de fuerza
            tab.Controls.Add(new Label
            {
                Text = "Fuerza:", Location = new Point(20, y + 3), AutoSize = true,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold)
            });

            pnlFuerza = new Panel
            {
                Location  = new Point(180, y + 6),
                Size      = new Size(280, 12),
                BackColor = Color.FromArgb(220, 222, 235)
            };
            var pnlBarra = new Panel
            {
                Location  = new Point(0, 0),
                Size      = new Size(0, 12),
                BackColor = Color.Gray
            };
            pnlFuerza.Controls.Add(pnlBarra);
            tab.Controls.Add(pnlFuerza);
            y += 28;

            lblFuerzaTexto = new Label
            {
                Text = "", Location = new Point(180, y), AutoSize = true,
                Font = new Font("Segoe UI", 8.5f, FontStyle.Italic),
                ForeColor = Color.Gray
            };
            tab.Controls.Add(lblFuerzaTexto);
            y += 30;

            var btnCambiar = BtnPrimario("Cambiar contrasena", new Point(180, y));
            btnCambiar.Click += (s, e) => CambiarContrasena();
            tab.Controls.Add(btnCambiar);

            return tab;
        }

        // ── Tab 3: Preferencias ───────────────────────────────────────────
        private TabPage BuildTabPreferencias()
        {
            var tab = new TabPage("Preferencias");
            tab.BackColor = Color.FromArgb(245, 247, 252);

            int y = 20;
            void Sec(string t)
            {
                tab.Controls.Add(new Label
                {
                    Text = t, Location = new Point(20, y), AutoSize = true,
                    Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                    ForeColor = Color.FromArgb(40, 55, 90)
                });
                y += 26;
            }

            // Color de acento
            Sec("Apariencia");
            tab.Controls.Add(new Label
            {
                Text = "Color de acento:", Location = new Point(30, y + 3),
                AutoSize = true, Font = new Font("Segoe UI", 9f)
            });
            cmbColorAcento = new ComboBox
            {
                Location = new Point(180, y), Size = new Size(200, 26),
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            cmbColorAcento.Items.AddRange(new object[]
                { "Azul", "Verde", "Morado", "Naranja", "Rojo", "Gris" });
            cmbColorAcento.SelectedItem = _prefs.ColorAcento;
            if (cmbColorAcento.SelectedIndex < 0) cmbColorAcento.SelectedIndex = 0;
            cmbColorAcento.DrawMode = DrawMode.OwnerDrawFixed;
            cmbColorAcento.DrawItem += (s, e) =>
            {
                if (e.Index < 0) return;
                e.DrawBackground();
                Color c = ColorDeAcento(cmbColorAcento.Items[e.Index].ToString());
                e.Graphics.FillRectangle(new SolidBrush(c), e.Bounds.X + 4,
                    e.Bounds.Y + 4, 16, e.Bounds.Height - 8);
                e.Graphics.DrawString(cmbColorAcento.Items[e.Index].ToString(),
                    e.Font, Brushes.Black,
                    e.Bounds.X + 26, e.Bounds.Y + 2);
            };
            tab.Controls.Add(cmbColorAcento);
            y += 38;

            // Preferencias de comportamiento
            Sec("Comportamiento");
            chkKpis = Chk(tab, "Mostrar KPIs en el dashboard",   _prefs.MostrarKpis,      y); y += 32;
            chkSonido    = Chk(tab, "Sonido en alertas de stock bajo", _prefs.SonidoAlertas,   y); y += 32;
            chkConfirmar = Chk(tab, "Confirmar antes de eliminar registros", _prefs.ConfirmarEliminar, y); y += 32;
            chkUltimoMod = Chk(tab, "Abrir ultimo modulo al iniciar",_prefs.AbrirUltimoModulo,y); y += 38;

            // Filas por pagina
            Sec("Grillas");
            tab.Controls.Add(new Label
            {
                Text = "Filas por pagina:", Location = new Point(30, y + 3),
                AutoSize = true, Font = new Font("Segoe UI", 9f)
            });
            numFilas = new NumericUpDown
            {
                Location = new Point(180, y), Size = new Size(100, 26),
                Minimum = 10, Maximum = 500, Value = _prefs.FilasPorPagina
            };
            tab.Controls.Add(numFilas);
            y += 46;

            var btnGuardar = BtnPrimario("Guardar preferencias", new Point(180, y));
            btnGuardar.Click += (s, e) => GuardarPreferencias();
            tab.Controls.Add(btnGuardar);

            return tab;
        }

        // ── Footer ────────────────────────────────────────────────────────
        private void BuildFooter()
        {
            var footer = new Panel
            {
                Dock = DockStyle.Bottom, Height = 48,
                BackColor = Color.FromArgb(225, 228, 245)
            };
            var btnCerrar = new Button
            {
                Text = "Cerrar", Size = new Size(100, 32), Location = new Point(420, 8),
                BackColor = Color.FromArgb(100, 100, 120), ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 9f, FontStyle.Bold)
            };
            btnCerrar.FlatAppearance.BorderSize = 0;
            btnCerrar.Click += (s, e) => Close();
            footer.Controls.Add(btnCerrar);
            Controls.Add(footer);
        }

        // ══════════════════════════════════════════════════════════════════
        //  LOGICA
        // ══════════════════════════════════════════════════════════════════
        private void GuardarInfo()
        {
            _prefs.NombreCompleto = txtNombreCompleto.Text.Trim();
            _prefs.Email          = txtEmail.Text.Trim();
            _prefs.Telefono       = txtTelefono.Text.Trim();
            DatabaseService.GuardarPreferencias(_prefs);

            // Actualizar nombre en sesion si cambio
            if (!string.IsNullOrWhiteSpace(_prefs.NombreCompleto))
                Sesion.NombreCompleto = _prefs.NombreCompleto;

            MessageBox.Show("Informacion guardada correctamente.",
                "Guardado", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void CambiarContrasena()
        {
            string actual    = txtActual.Text;
            string nueva     = txtNueva.Text;
            string confirmar = txtConfirmar.Text;

            if (string.IsNullOrWhiteSpace(actual))
            { Alerta("Ingresa tu contrasena actual."); return; }

            if (nueva != confirmar)
            { Alerta("La nueva contrasena y la confirmacion no coinciden."); return; }

            string error = DatabaseService.CambiarContrasena(
                Sesion.NombreUsuario, actual, nueva);

            if (error != null) { Alerta(error); return; }

            // Limpiar campos
            txtActual.Text = txtNueva.Text = txtConfirmar.Text = "";
            ActualizarIndicadorFuerza();

            MessageBox.Show(
                "Contrasena cambiada exitosamente.\n" +
                "La nueva contrasena estara activa en el proximo inicio de sesion.",
                "Contrasena cambiada",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void GuardarPreferencias()
        {
            _prefs.ColorAcento      = cmbColorAcento.SelectedItem?.ToString() ?? "Azul";
            _prefs.MostrarKpis      = chkKpis.Checked;
            _prefs.SonidoAlertas    = chkSonido.Checked;
            _prefs.ConfirmarEliminar= chkConfirmar.Checked;
            _prefs.AbrirUltimoModulo= chkUltimoMod.Checked;
            _prefs.FilasPorPagina   = (int)numFilas.Value;
            DatabaseService.GuardarPreferencias(_prefs);

            MessageBox.Show("Preferencias guardadas. Algunos cambios se aplican al reiniciar.",
                "Guardado", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        // ── Indicador de fuerza de contraseña ─────────────────────────────
        private void ActualizarIndicadorFuerza()
        {
            string pwd = txtNueva.Text;
            int fuerza = CalcularFuerza(pwd);

            var barra = pnlFuerza.Controls.Count > 0 ? pnlFuerza.Controls[0] : null;
            if (barra == null) return;

            int ancho = (int)(pnlFuerza.Width * fuerza / 4.0);
            barra.Width = ancho;
            barra.BackColor = fuerza switch
            {
                0 => Color.FromArgb(220, 222, 235),
                1 => Color.FromArgb(200, 60, 60),
                2 => Color.FromArgb(210, 140, 20),
                3 => Color.FromArgb(160, 180, 20),
                _ => Color.FromArgb(30, 150, 70)
            };

            string[] etiquetas = { "", "Debil", "Regular", "Buena", "Fuerte" };
            lblFuerzaTexto.Text = pwd.Length == 0 ? "" : etiquetas[fuerza];
            lblFuerzaTexto.ForeColor = barra.BackColor;

            // Indicar si las contrasenias coinciden
            if (txtConfirmar.Text.Length > 0)
            {
                bool coinciden = txtNueva.Text == txtConfirmar.Text;
                txtConfirmar.BackColor = coinciden
                    ? Color.FromArgb(228, 255, 232) : Color.FromArgb(255, 230, 230);
            }
            else txtConfirmar.BackColor = Color.White;
        }

        private int CalcularFuerza(string pwd)
        {
            if (pwd.Length == 0) return 0;
            int score = 0;
            if (pwd.Length >= 6)  score++;
            if (pwd.Length >= 10) score++;
            if (System.Text.RegularExpressions.Regex.IsMatch(pwd, @"\d")) score++;
            if (System.Text.RegularExpressions.Regex.IsMatch(pwd, @"[!@#$%^&*()_+\-=\[\]{}|;':,.<>?]")) score++;
            return Math.Min(4, score);
        }

        // ══════════════════════════════════════════════════════════════════
        //  HELPERS
        // ══════════════════════════════════════════════════════════════════
        private static Color ColorDeAcento(string nombre) => nombre switch
        {
            "Verde"   => Color.FromArgb(30, 130, 60),
            "Morado"  => Color.FromArgb(100, 50, 160),
            "Naranja" => Color.FromArgb(200, 100, 20),
            "Rojo"    => Color.FromArgb(180, 40, 40),
            "Gris"    => Color.FromArgb(80, 85, 100),
            _         => Color.FromArgb(40, 80, 160)   // Azul
        };

        private Button BtnPrimario(string texto, Point loc)
        {
            var b = new Button
            {
                Text = texto, Location = loc, Size = new Size(200, 34),
                BackColor = Color.FromArgb(40, 55, 90), ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold)
            };
            b.FlatAppearance.BorderSize = 0;
            return b;
        }

        private CheckBox Chk(Control parent, string texto, bool val, int y)
        {
            var cb = new CheckBox
            {
                Text = texto, Checked = val,
                Location = new Point(30, y), AutoSize = true,
                Font = new Font("Segoe UI", 9f)
            };
            parent.Controls.Add(cb);
            return cb;
        }

        private void Alerta(string msg) =>
            MessageBox.Show(msg, "Aviso", MessageBoxButtons.OK, MessageBoxIcon.Warning);
    }
}
