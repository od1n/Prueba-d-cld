// FrmCambioContrasena.cs
// Diálogo para cambio de contraseña: voluntario (desde perfil) y forzado (primer login/admin)
using System;
using System.Drawing;
using System.Windows.Forms;

namespace SistemaGestion
{
    public class FrmCambioContrasena : Form
    {
        private readonly string _usuario;
        private readonly bool   _forzado;
        private TextBox txtActual, txtNueva, txtConfirm;
        private Label   lblEstado;

        public FrmCambioContrasena(string usuario, bool forzado = false)
        {
            _usuario = usuario;
            _forzado = forzado;

            Text            = forzado ? "Cambio de contraseña obligatorio" : "Cambiar contraseña";
            Size            = new Size(420, 420);
            StartPosition   = FormStartPosition.CenterParent;
            FormBorderStyle = forzado ? FormBorderStyle.FixedDialog : FormBorderStyle.Sizable;
            MinimumSize     = new Size(380, 380);
            MaximizeBox     = false;
            // Si es forzado, no permitir cerrar con X
            if (forzado) FormClosing += (s, e) =>
            {
                if (DialogResult != DialogResult.OK)
                {
                    e.Cancel = true;
                    MessageBox.Show("Debe establecer una nueva contraseña para continuar.",
                        "Requerido", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            };

            var hdr = new Panel { Dock = DockStyle.Top, Height = 48,
                BackColor = Color.FromArgb(26, 74, 138) };
            hdr.Controls.Add(new Label {
                Text = forzado ? "⚠️  Establezca una nueva contraseña" : "Cambiar contraseña",
                ForeColor = Color.White, Font = new Font("Segoe UI", 11f, FontStyle.Bold),
                AutoSize = true, Location = new Point(14, 12) });

            int y = 66;
            Label L(string t) {
                var l = new Label { Text = t, Location = new Point(40, y),
                    Font = new Font("Segoe UI", 9f, FontStyle.Bold), AutoSize = true,
                    ForeColor = Color.FromArgb(50, 50, 80) };
                Controls.Add(l); return l;
            }
            TextBox T(bool pwd = true) {
                var tb = new TextBox { Location = new Point(40, y + 20),
                    Size = new Size(325, 28), Font = new Font("Segoe UI", 10f),
                    BorderStyle = BorderStyle.FixedSingle,
                    PasswordChar = pwd ? '●' : '\0', MaxLength = 100 };
                Controls.Add(tb); y += 60; return tb;
            }

            L("Contraseña actual:");      txtActual  = T();
            L("Nueva contraseña:");       txtNueva   = T();
            L("Confirmar contraseña:");   txtConfirm = T();

            // Indicador de fortaleza
            lblEstado = new Label {
                Location = new Point(40, y), Size = new Size(325, 40),
                Font = new Font("Segoe UI", 8.5f), AutoSize = false,
                ForeColor = Color.FromArgb(60, 60, 80),
                Text = "Mínimo 8 caracteres. Use letras, números y símbolos." };
            Controls.Add(lblEstado);
            txtNueva.TextChanged += (s, e) => ActualizarFortaleza();

            y += 50;
            var btnOk = new Button {
                Text = "Guardar contraseña", Location = new Point(40, y),
                Size = new Size(160, 36), BackColor = Color.FromArgb(30, 130, 70),
                ForeColor = Color.White, FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold) };
            btnOk.FlatAppearance.BorderSize = 0;
            btnOk.Click += Guardar;

            var btnCan = new Button {
                Text = "Cancelar", Location = new Point(215, y),
                Size = new Size(150, 36), BackColor = Color.FromArgb(100, 116, 139),
                ForeColor = Color.White, FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                DialogResult = _forzado ? DialogResult.None : DialogResult.Cancel };
            btnCan.FlatAppearance.BorderSize = 0;
            if (!_forzado) btnCan.Click += (s, e) => Close();

            Controls.AddRange(new Control[] { hdr, lblEstado, btnOk, btnCan });
            VentanaPrefs.Restaurar(this);
        }

        private void ActualizarFortaleza()
        {
            var p = txtNueva.Text;
            if (p.Length == 0) { lblEstado.Text = "Mínimo 8 caracteres."; lblEstado.ForeColor = Color.Gray; return; }
            int score = 0;
            if (p.Length >= 8)  score++;
            if (p.Length >= 12) score++;
            if (System.Text.RegularExpressions.Regex.IsMatch(p, @"[A-Z]")) score++;
            if (System.Text.RegularExpressions.Regex.IsMatch(p, @"[0-9]")) score++;
            if (System.Text.RegularExpressions.Regex.IsMatch(p, @"[^A-Za-z0-9]")) score++;
            (lblEstado.Text, lblEstado.ForeColor) = score switch {
                <= 1 => ("Contraseña débil — añada letras mayúsculas, números y símbolos.", Color.FromArgb(180,40,40)),
                2    => ("Contraseña aceptable — podría ser más segura.", Color.FromArgb(160,100,0)),
                3    => ("Contraseña buena.", Color.FromArgb(40,120,40)),
                _    => ("✓ Contraseña fuerte.", Color.FromArgb(20,100,20))
            };
        }

        private void Guardar(object sender, EventArgs e)
        {
            if (txtNueva.Text != txtConfirm.Text)
            { MessageBox.Show("Las contraseñas no coinciden.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
            if (txtNueva.Text.Length < 8)
            { MessageBox.Show("La contraseña debe tener al menos 8 caracteres.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }

            var error = DatabaseService.CambiarContrasena(_usuario, txtActual.Text, txtNueva.Text);
            if (error != null)
            { MessageBox.Show(error, "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }

            MessageBox.Show("Contraseña actualizada correctamente.", "Éxito",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
            DialogResult = DialogResult.OK;
            Close();
        }
    }
}
