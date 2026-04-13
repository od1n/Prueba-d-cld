using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace SistemaGestion
{
    // ══════════════════════════════════════════════════════════════
    //  GESTIÓN DE USUARIOS  (solo Admin)
    // ══════════════════════════════════════════════════════════════
    public class FrmUsuarios : Form
    {
        private DataGridView dgv;

        public FrmUsuarios()
        {
            Text            = "Gestión de Usuarios";
            Size            = new Size(860, 500);
            StartPosition   = FormStartPosition.CenterParent;
            BackColor       = Color.FromArgb(245, 245, 252);

            var hdr = new Panel { Dock = DockStyle.Top, Height = 50, BackColor = Color.FromArgb(30, 70, 140) };
            hdr.Controls.Add(new Label
            {
                Text = "Gestión de Usuarios y Roles",
                ForeColor = Color.White, Font = new Font("Segoe UI", 12f, FontStyle.Bold),
                AutoSize = true, Location = new Point(12, 13)
            });

            var btnNuevo   = Btn("+ Nuevo",    Color.FromArgb(40, 160, 80),  new Point(580, 8));
            var btnEditar  = Btn("Editar",     Color.FromArgb(60, 120, 200), new Point(678, 8));
            var btnToggle  = Btn("Activar/Des",Color.FromArgb(180, 130, 20), new Point(776, 8));
            hdr.Controls.AddRange(new Control[] { btnNuevo, btnEditar, btnToggle });

            dgv = new DataGridView
            {
                Dock = DockStyle.Fill, ReadOnly = true,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false, AllowUserToAddRows = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                BackgroundColor = Color.White, BorderStyle = BorderStyle.None,
                RowHeadersVisible = false, Font = new Font("Segoe UI", 9f)
            };
            dgv.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(30, 70, 140);
            dgv.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
            dgv.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 9f, FontStyle.Bold);
            dgv.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(235, 240, 255);
            dgv.EnableHeadersVisualStyles = false;
            dgv.DataBindingComplete += (s, e) => ColorearFilas();

            var footer = new Panel { Dock = DockStyle.Bottom, Height = 40, BackColor = Color.FromArgb(218, 225, 248) };
            var lblInfo = new Label
            {
                AutoSize = true, Location = new Point(12, 10),
                Font = new Font("Segoe UI", 9f), ForeColor = Color.FromArgb(50, 50, 100)
            };
            var btnCerrar = new Button
            {
                Text = "Cerrar", Size = new Size(90, 26), Location = new Point(740, 7),
                BackColor = Color.FromArgb(100, 100, 120), ForeColor = Color.White, FlatStyle = FlatStyle.Flat
            };
            btnCerrar.FlatAppearance.BorderSize = 0;
            btnCerrar.Click += (s, e) => Close();
            footer.Controls.AddRange(new Control[] { lblInfo, btnCerrar });

            Controls.AddRange(new Control[] { dgv, hdr, footer });

            btnNuevo.Click  += (s, e) =>
            {
                using var d = new DlgUsuario(null);
                if (d.ShowDialog() == DialogResult.OK)
                {
                    DatabaseService.InsertarUsuario(d.Resultado);
                    DatabaseService.RegistrarAuditoria("CREAR", "Usuarios",
                        $"Nuevo usuario: {d.Resultado.NombreUsuario} Rol: {d.Resultado.Rol}");
                    CargarDatos(lblInfo);
                }
            };
            btnEditar.Click += (s, e) =>
            {
                var u = GetSel();
                if (u == null) return;
                using var d = new DlgUsuario(u);
                if (d.ShowDialog() == DialogResult.OK)
                {
                    DatabaseService.ActualizarUsuario(d.Resultado);
                    DatabaseService.RegistrarAuditoria("EDITAR", "Usuarios",
                        $"Editado: {d.Resultado.NombreUsuario} Rol: {d.Resultado.Rol}");
                    CargarDatos(lblInfo);
                }
            };
            btnToggle.Click += (s, e) =>
            {
                var u = GetSel();
                if (u == null) return;
                if (u.NombreUsuario == Sesion.NombreUsuario)
                {
                    MessageBox.Show("No puedes desactivar tu propio usuario.",
                        "Aviso", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                string accion = u.Activo ? "desactivar" : "activar";
                if (MessageBox.Show($"¿{accion} usuario {u.NombreCompleto}?", "Confirmar",
                    MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                {
                    u.Activo = !u.Activo;
                    DatabaseService.ActualizarUsuario(u);
                    DatabaseService.RegistrarAuditoria(accion.ToUpper(), "Usuarios",
                        $"Usuario: {u.NombreUsuario}");
                    CargarDatos(lblInfo);
                }
            };

            CargarDatos(lblInfo);
        }

        private void CargarDatos(Label lbl)
        {
            var lista = DatabaseService.ObtenerUsuarios();
            dgv.DataSource = null;
            dgv.DataSource = lista.Select(u => new
            {
                u.Id,
                Usuario       = u.NombreUsuario,
                NombreCompleto= u.NombreCompleto,
                u.Rol,
                Estado        = u.Activo ? "Activo" : "Inactivo",
                Creado        = u.FechaCreacion.ToShortDateString(),
                UltimoAcceso  = u.UltimoAcceso?.ToString("dd/MM/yyyy HH:mm") ?? "—"
            }).ToList();
            lbl.Text = $"  Usuarios: {lista.Count}   |   Activos: {lista.Count(u => u.Activo)}   |   " +
                       $"Admins: {lista.Count(u => u.Rol == "Admin")}";
        }

        private void ColorearFilas()
        {
            foreach (DataGridViewRow row in dgv.Rows)
            {
                if (row.Cells["Estado"].Value?.ToString() == "Inactivo")
                    row.DefaultCellStyle.ForeColor = Color.FromArgb(160, 160, 160);

                var rol = row.Cells["Rol"].Value?.ToString();
                if (rol == "Admin")
                    row.DefaultCellStyle.BackColor = Color.FromArgb(235, 240, 255);
            }
        }

        private Usuario GetSel()
        {
            if (dgv.CurrentRow == null) return null;
            int id = (int)dgv.CurrentRow.Cells["Id"].Value;
            return DatabaseService.ObtenerUsuarios().Find(x => x.Id == id);
        }

        private Button Btn(string t, Color c, Point p)
        {
            var b = new Button
            {
                Text = t, Size = new Size(90, 33), Location = p, BackColor = c,
                ForeColor = Color.White, FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 8.5f, FontStyle.Bold), Cursor = Cursors.Hand
            };
            b.FlatAppearance.BorderSize = 0;
            return b;
        }
    }

    // ── Diálogo Nuevo / Editar Usuario ───────────────────────────────────────
    public class DlgUsuario : Form
    {
        public Usuario Resultado { get; private set; }

        private TextBox       txtNombreUsuario, txtNombreCompleto;
        private TextBox       txtPassword, txtConfirmar;
        private ComboBox      cmbRol;
        private CheckBox      chkActivo;
        private Label         lblPermisosDesc;

        public DlgUsuario(Usuario u)
        {
            bool esNuevo = u == null;
            Text            = esNuevo ? "Nuevo Usuario" : "Editar Usuario";
            Size            = new Size(460, 500);
            StartPosition   = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox     = MinimizeBox = false;
            BackColor       = Color.FromArgb(245, 245, 252);

            // Banner de rol
            var banner = new Panel { Dock = DockStyle.Top, Height = 44, BackColor = Color.FromArgb(30, 70, 140) };
            banner.Controls.Add(new Label
            {
                Text = esNuevo ? "Crear nuevo usuario" : $"Editando: {u.NombreCompleto}",
                ForeColor = Color.White, Font = new Font("Segoe UI", 10f, FontStyle.Bold),
                AutoSize = true, Location = new Point(14, 12)
            });

            int y = 58;
            L("Usuario:", y);
            txtNombreUsuario = T(y, esNuevo ? "" : u.NombreUsuario);
            if (!esNuevo) txtNombreUsuario.ReadOnly = true;
            y += 38;

            L("Nombre completo:", y);
            txtNombreCompleto = T(y, esNuevo ? "" : u.NombreCompleto); y += 38;

            L("Rol:", y);
            cmbRol = new ComboBox
            {
                Location = new Point(170, y), Size = new Size(250, 24),
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("Segoe UI", 9f)
            };
            cmbRol.Items.AddRange(new object[] { "Admin", "Ventas", "Almacen", "Contabilidad" });
            cmbRol.SelectedItem = esNuevo ? "Ventas" : u.Rol;
            cmbRol.SelectedIndexChanged += (s, e) => ActualizarDescripcionRol();
            Controls.Add(cmbRol); y += 38;

            // Descripción de permisos del rol
            lblPermisosDesc = new Label
            {
                Location  = new Point(20, y),
                Size      = new Size(410, 46),
                Font      = new Font("Segoe UI", 8.5f, FontStyle.Italic),
                ForeColor = Color.FromArgb(60, 80, 140),
                BorderStyle = BorderStyle.FixedSingle
            };
            Controls.Add(lblPermisosDesc); y += 54;

            L("Contraseña:", y);
            txtPassword = new TextBox
            {
                Location = new Point(170, y), Size = new Size(250, 24),
                PasswordChar = '●', MaxLength = 100,
                Font = new Font("Segoe UI", 9f),
                PlaceholderText = esNuevo ? "" : "(dejar vacío = no cambiar)"
            };
            Controls.Add(txtPassword); y += 38;

            L("Confirmar:", y);
            txtConfirmar = new TextBox
            {
                Location = new Point(170, y), Size = new Size(250, 24),
                PasswordChar = '●', MaxLength = 100,
                Font = new Font("Segoe UI", 9f)
            };
            Controls.Add(txtConfirmar); y += 38;

            chkActivo = new CheckBox
            {
                Text     = "Usuario activo",
                Location = new Point(170, y),
                AutoSize = true,
                Checked  = esNuevo ? true : u.Activo,
                Font     = new Font("Segoe UI", 9f)
            };
            Controls.Add(chkActivo); y += 44;

            var bOk = new Button
            {
                Text = esNuevo ? "Crear usuario" : "Guardar cambios",
                Location = new Point(60, y), Size = new Size(160, 34),
                BackColor = Color.FromArgb(30, 70, 140), ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 9f, FontStyle.Bold)
            };
            bOk.FlatAppearance.BorderSize = 0;

            var bCan = new Button
            {
                Text = "Cancelar", DialogResult = DialogResult.Cancel,
                Location = new Point(250, y), Size = new Size(130, 34),
                BackColor = Color.FromArgb(150, 60, 60), ForeColor = Color.White, FlatStyle = FlatStyle.Flat
            };
            bCan.FlatAppearance.BorderSize = 0;

            bOk.Click += (s, e) =>
            {
                if (string.IsNullOrWhiteSpace(txtNombreUsuario.Text))
                { MessageBox.Show("Ingresa el nombre de usuario.", "Validación", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }

                if (string.IsNullOrWhiteSpace(txtNombreCompleto.Text))
                { MessageBox.Show("Ingresa el nombre completo.", "Validación", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }

                bool cambiaPass = !string.IsNullOrWhiteSpace(txtPassword.Text);
                if (esNuevo && !cambiaPass)
                { MessageBox.Show("Ingresa una contraseña.", "Validación", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }

                if (cambiaPass)
                {
                    if (txtPassword.Text.Length < 6)
                    { MessageBox.Show("La contraseña debe tener al menos 6 caracteres.", "Validación", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }

                    if (txtPassword.Text != txtConfirmar.Text)
                    { MessageBox.Show("Las contraseñas no coinciden.", "Validación", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
                }

                Resultado = u ?? new Usuario();
                Resultado.NombreUsuario  = txtNombreUsuario.Text.Trim().ToLower();
                Resultado.NombreCompleto = txtNombreCompleto.Text.Trim();
                Resultado.Rol            = cmbRol.SelectedItem?.ToString() ?? "Ventas";
                Resultado.Activo         = chkActivo.Checked;
                Resultado.FechaCreacion  = esNuevo ? DateTime.Now : u.FechaCreacion;
                Resultado.PasswordHash   = cambiaPass
                    ? DatabaseService.HashPassword(txtPassword.Text)
                    : (u?.PasswordHash ?? DatabaseService.HashPassword(txtPassword.Text));

                DialogResult = DialogResult.OK;
            };

            Controls.AddRange(new Control[] { banner, bOk, bCan });
            ActualizarDescripcionRol();
        }

        private void ActualizarDescripcionRol()
        {
            var desc = cmbRol.SelectedItem?.ToString() switch
            {
                "Admin"        => "Acceso total al sistema. Puede crear y eliminar usuarios, ver todos los módulos y configuraciones.",
                "Ventas"       => "Acceso a: Facturas, Cuentas por Cobrar, Clientes y Reportes.",
                "Almacen"      => "Acceso a: Inventario, Kardex, Compras, Proveedores, Ingreso de Producción y Moldes.",
                "Contabilidad" => "Acceso a: Reportes, Facturas, Compras, Gastos y Cambio de Precios.",
                _              => ""
            };
            lblPermisosDesc.Text = "  " + desc;
        }

        private void L(string t, int y) =>
            Controls.Add(new Label
            {
                Text = t, Location = new Point(20, y + 3), AutoSize = true,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold)
            });

        private TextBox T(int y, string val)
        {
            var tb = new TextBox
            {
                Text = val, Location = new Point(170, y), Size = new Size(250, 24),
                Font = new Font("Segoe UI", 9f), MaxLength = 80
            };
            Controls.Add(tb);
            return tb;
        }
    }
}
