using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace SistemaGestion
{
    // ════════════════════════════════════════════════════════════════════════
    //  FrmUsuarios — Gestión de usuarios del sistema (solo Admin)
    //  Permite crear, editar y desactivar usuarios.
    //  Soporta múltiples roles por usuario (excepto Admin, que es exclusivo).
    // ════════════════════════════════════════════════════════════════════════
    public class FrmUsuarios : FormBase
    {
        private DataGridView dgv;
        private Label        lblResumen;
        private string _sortCol = "NombreCompleto";
        private bool   _sortAsc = true;

        private static readonly Color Verde  = Color.FromArgb(20,  90,  50);
        private static readonly Color Rojo   = Color.FromArgb(180, 60,  60);
        private static readonly Color Gris   = Color.FromArgb(100, 100, 120);

        public FrmUsuarios()
        {
            Text          = "Gestión de Usuarios";
            Size          = new Size(980, 580);
            StartPosition = FormStartPosition.CenterParent;
            BackColor     = Color.FromArgb(245, 250, 245);

            // ── Header ───────────────────────────────────────────────────
            var hdr = new Panel { Dock = DockStyle.Top, Height = 48, BackColor = Verde };
            hdr.Controls.Add(new Label {
                Text = "Gestión de Usuarios del Sistema",
                ForeColor = Color.White, AutoSize = true,
                Font = new Font("Segoe UI", 12f, FontStyle.Bold),
                Location = new Point(12, 13) });

            // ── Barra de botones ─────────────────────────────────────────
            var bar = new Panel { Dock = DockStyle.Top, Height = 44,
                BackColor = Color.FromArgb(235, 248, 240) };

            var btnNuevo   = Btn("+ Nuevo",    Verde, new Point(8,   7));
            var btnEditar  = Btn("✏ Editar",   Color.FromArgb(60,100,180), new Point(110, 7));
            var btnToggle  = Btn("⏸ Activar/Desactivar", Gris, new Point(212, 7));
            btnToggle.Width = 170;

            btnNuevo.Click  += (s, e) => AbrirDialogo(null);
            btnEditar.Click += (s, e) => {
                var u = UsuarioSeleccionado();
                if (u != null) AbrirDialogo(u);
            };
            btnToggle.Click += (s, e) => ToggleActivo();

            bar.Controls.AddRange(new Control[] { btnNuevo, btnEditar, btnToggle });

            // ── Grid ─────────────────────────────────────────────────────
            dgv = new DataGridView {
                Name = "dgv",
                Dock = DockStyle.Fill, ReadOnly = true,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false, AllowUserToAddRows = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None,
                BackgroundColor = Color.White, BorderStyle = BorderStyle.None,
                RowHeadersVisible = false, Font = new Font("Segoe UI", 9f),
                RowTemplate = { Height = 26 }
            };
            dgv.ColumnHeadersDefaultCellStyle.BackColor = Verde;
            dgv.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
            dgv.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 9f, FontStyle.Bold);
            dgv.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(235, 250, 240);
            dgv.EnableHeadersVisualStyles = false;
            dgv.DataBindingComplete      += (s, e) => ColorearFilas();
            dgv.ColumnHeaderMouseClick   += DgvSort;
            dgv.DoubleClick              += (s, e) => {
                var u = UsuarioSeleccionado();
                if (u != null) AbrirDialogo(u);
            };

            // ── Footer ───────────────────────────────────────────────────
            var footer = new Panel { Dock = DockStyle.Bottom, Height = 38,
                BackColor = Color.FromArgb(210, 235, 218) };
            lblResumen = new Label { AutoSize = true, Location = new Point(10, 11),
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                ForeColor = Color.FromArgb(15, 70, 35) };
            var btnCerrar = new Button { Text = "Cerrar", Size = new Size(90, 26),
                Location = new Point(870, 6), BackColor = Gris,
                ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
            btnCerrar.FlatAppearance.BorderSize = 0;
            btnCerrar.Click += (s, e) => Close();
            footer.Controls.AddRange(new Control[] { lblResumen, btnCerrar });

            Controls.AddRange(new Control[] { dgv, footer, bar, hdr });

            VentanaPrefs.Restaurar(this);
            Cargar();
        }

        // ── Cargar datos ─────────────────────────────────────────────────
        private void Cargar()
        {
            var lista = DatabaseService.ObtenerUsuarios();

            lista = _sortCol switch {
                "NombreCompleto" => _sortAsc ? lista.OrderBy(u=>u.NombreCompleto).ToList()
                                             : lista.OrderByDescending(u=>u.NombreCompleto).ToList(),
                "NombreUsuario"  => _sortAsc ? lista.OrderBy(u=>u.NombreUsuario).ToList()
                                             : lista.OrderByDescending(u=>u.NombreUsuario).ToList(),
                "Roles"          => _sortAsc ? lista.OrderBy(u=>u.RolesDescripcion).ToList()
                                             : lista.OrderByDescending(u=>u.RolesDescripcion).ToList(),
                _ => lista
            };

            dgv.DataSource = null;
            dgv.DataSource = lista.Select(u => new {
                u.Id,
                NombreCompleto = u.NombreCompleto,
                NombreUsuario  = u.NombreUsuario,
                Roles          = u.RolesDescripcion,
                PinBot         = u.TienePinBot ? "Si" : "—",
                Estado         = u.Activo ? "Activo" : "Inactivo",
                UltimoAcceso   = u.UltimoAcceso.HasValue
                    ? u.UltimoAcceso.Value.ToString("dd/MM/yyyy HH:mm")
                    : "Nunca",
                FechaCreacion  = u.FechaCreacion.ToString("dd/MM/yyyy"),
            }).ToList();

            // Anchos
            if (dgv.Columns.Count > 0)
            {
                var anchos = new Dictionary<string, int> {
                    ["Id"]=40, ["NombreCompleto"]=200, ["NombreUsuario"]=120,
                    ["Roles"]=180, ["PinBot"]=50, ["Estado"]=70, ["UltimoAcceso"]=130,
                    ["FechaCreacion"]=100 };
                foreach (DataGridViewColumn col in dgv.Columns)
                    if (anchos.TryGetValue(col.Name, out int w)) col.Width = w;
            }
            VentanaPrefs.AplicarAnchos(this, dgv);

            // Glyphs
            foreach (DataGridViewColumn col in dgv.Columns)
                col.HeaderCell.SortGlyphDirection = SortOrder.None;
            if (dgv.Columns.Contains(_sortCol))
                dgv.Columns[_sortCol].HeaderCell.SortGlyphDirection =
                    _sortAsc ? SortOrder.Ascending : SortOrder.Descending;

            int activos = lista.Count(u => u.Activo);
            lblResumen.Text =
                $"  Usuarios: {lista.Count}   |   Activos: {activos}   |   " +
                $"Inactivos: {lista.Count - activos}";
        }

        private void ColorearFilas()
        {
            foreach (DataGridViewRow row in dgv.Rows)
            {
                if (row.Cells["Estado"].Value?.ToString() == "Inactivo")
                {
                    row.DefaultCellStyle.ForeColor   = Color.Gray;
                    row.DefaultCellStyle.BackColor   = Color.FromArgb(245, 245, 245);
                    row.DefaultCellStyle.Font        = new Font("Segoe UI", 9f, FontStyle.Italic);
                }
                if (row.Cells["Roles"].Value?.ToString() == "Admin")
                    row.DefaultCellStyle.ForeColor = Color.FromArgb(15, 70, 35);
            }
        }

        private void DgvSort(object sender, DataGridViewCellMouseEventArgs e)
        {
            string col = dgv.Columns[e.ColumnIndex].Name;
            if (!new[]{"NombreCompleto","NombreUsuario","Roles"}.Contains(col)) return;
            if (_sortCol == col) _sortAsc = !_sortAsc;
            else { _sortCol = col; _sortAsc = true; }
            Cargar();
        }

        private Usuario UsuarioSeleccionado()
        {
            if (dgv.CurrentRow == null) return null;
            int id = (int)dgv.CurrentRow.Cells["Id"].Value;
            return DatabaseService.ObtenerUsuarios().Find(u => u.Id == id);
        }

        private void AbrirDialogo(Usuario usuarioExistente)
        {
            using var dlg = new DlgUsuario(usuarioExistente);
            if (dlg.ShowDialog() != DialogResult.OK) return;

            try
            {
                if (usuarioExistente == null)
                {
                    dlg.Usuario.FechaCreacion = DateTime.Now;
                    DatabaseService.InsertarUsuario(dlg.Usuario);
                    MessageBox.Show("Usuario creado correctamente.", "Usuarios",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else
                {
                    DatabaseService.ActualizarUsuario(dlg.Usuario);
                    MessageBox.Show("Usuario actualizado correctamente.", "Usuarios",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                Cargar();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ToggleActivo()
        {
            var u = UsuarioSeleccionado();
            if (u == null) return;

            if (u.Rol == "Admin")
            {
                MessageBox.Show("El usuario Admin no puede desactivarse.", "Usuarios",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string accion = u.Activo ? "desactivar" : "activar";
            if (MessageBox.Show($"¿Desea {accion} al usuario '{u.NombreCompleto}'?",
                "Confirmar", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
                return;

            u.Activo = !u.Activo;
            try
            {
                DatabaseService.ActualizarUsuario(u);
                Cargar();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private Button Btn(string t, Color c, Point p)
        {
            var b = new Button {
                Text = t, Height = 30, Width = 100, Location = p,
                BackColor = c, ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
                Cursor = Cursors.Hand };
            b.FlatAppearance.BorderSize = 0;
            return b;
        }
    }

    // ════════════════════════════════════════════════════════════════════════
    //  DlgUsuario — Crear o editar un usuario
    //  Muestra checkboxes para roles no-Admin, o un único checkbox Admin.
    //  Admin es exclusivo: al marcarlo se deshabilitan los demás y viceversa.
    // ════════════════════════════════════════════════════════════════════════
    public class DlgUsuario : FormBase
    {
        public Usuario Usuario { get; private set; }

        private TextBox       txtNombreUsuario, txtNombreCompleto;
        private TextBox       txtPassword, txtConfirmar;
        private CheckBox      chkAdmin;
        private CheckBox      chkVentas, chkAlmacen, chkContabilidad;
        private CheckBox      chkActivo;
        private Label         lblRolesError;
        private Label         lblPinEstado;
        private Button        btnGenerarPin, btnRevocarPin;
        private bool          _esNuevo;

        public DlgUsuario(Usuario usuarioExistente)
        {
            _esNuevo = usuarioExistente == null;
            Text = _esNuevo ? "Nuevo Usuario" : "Editar Usuario";
            Size = new Size(480, 620);
            StartPosition   = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = MinimizeBox = false;
            BackColor = Color.FromArgb(248, 250, 248);

            // ── Banner ───────────────────────────────────────────────────
            var banner = new Panel { Dock = DockStyle.Top, Height = 42,
                BackColor = Color.FromArgb(20, 90, 50) };
            banner.Controls.Add(new Label {
                Text = _esNuevo ? "👤  Nuevo Usuario" : "✏  Editar Usuario",
                ForeColor = Color.White, Font = new Font("Segoe UI", 11f, FontStyle.Bold),
                AutoSize = true, Location = new Point(12, 11) });

            int y = 56;

            // ── Datos básicos ────────────────────────────────────────────
            L("Nombre completo:", y);
            txtNombreCompleto = Txt(y); y += 32;

            L("Usuario (login):", y);
            txtNombreUsuario = Txt(y);
            if (!_esNuevo) txtNombreUsuario.ReadOnly = true;
            y += 32;

            L("Contraseña:", y);
            txtPassword = Txt(y); txtPassword.PasswordChar = '●';
            if (!_esNuevo)
            {
                // En edición la contraseña es opcional; si se deja vacía no cambia
                var lblHint = new Label {
                    Text = "(dejar vacío para no cambiar)",
                    Location = new Point(300, y + 5), AutoSize = true,
                    Font = new Font("Segoe UI", 8f, FontStyle.Italic),
                    ForeColor = Color.Gray };
                Controls.Add(lblHint);
            }
            y += 32;

            L("Confirmar contraseña:", y);
            txtConfirmar = Txt(y); txtConfirmar.PasswordChar = '●';
            y += 38;

            // ── Roles ────────────────────────────────────────────────────
            var lblRolesTitle = new Label {
                Text = "Roles asignados:",
                Location = new Point(15, y), AutoSize = true,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold) };
            Controls.Add(lblRolesTitle); y += 26;

            // Caja de roles
            var panelRoles = new Panel {
                Location = new Point(15, y), Size = new Size(430, 130),
                BorderStyle = BorderStyle.FixedSingle,
                BackColor = Color.FromArgb(245, 250, 245) };

            chkAdmin        = Chk("Admin (acceso total — exclusivo)",      new Point(12, 10));
            chkVentas       = Chk("Ventas (Facturas, Clientes, CxC)",      new Point(12, 38));
            chkAlmacen      = Chk("Almacén (Inventario, Kardex, Moldes, Compras)", new Point(12, 64));
            chkContabilidad = Chk("Contabilidad (Reportes, Gastos, IVA)",  new Point(12, 90));

            // Admin exclusivo: al marcarlo deshabilita el resto
            chkAdmin.CheckedChanged += (s, e) => {
                bool esAdmin = chkAdmin.Checked;
                chkVentas.Enabled       = !esAdmin;
                chkAlmacen.Enabled      = !esAdmin;
                chkContabilidad.Enabled = !esAdmin;
                if (esAdmin)
                {
                    chkVentas.Checked = chkAlmacen.Checked = chkContabilidad.Checked = false;
                }
                ValidarRoles();
            };

            // Al marcar cualquier rol no-Admin, desmarcar Admin
            chkVentas.CheckedChanged       += (s, e) => { if(chkVentas.Checked) chkAdmin.Checked=false; ValidarRoles(); };
            chkAlmacen.CheckedChanged      += (s, e) => { if(chkAlmacen.Checked) chkAdmin.Checked=false; ValidarRoles(); };
            chkContabilidad.CheckedChanged += (s, e) => { if(chkContabilidad.Checked) chkAdmin.Checked=false; ValidarRoles(); };

            panelRoles.Controls.AddRange(new Control[] {
                chkAdmin, chkVentas, chkAlmacen, chkContabilidad });
            Controls.Add(panelRoles);
            y += 138;

            // Error de roles
            lblRolesError = new Label {
                Location = new Point(15, y), AutoSize = true,
                ForeColor = Color.FromArgb(180, 40, 40),
                Font = new Font("Segoe UI", 8.5f, FontStyle.Italic),
                Text = "" };
            Controls.Add(lblRolesError); y += 22;

            // ── PIN de Bot (solo para Admin/Ventas, solo Admin puede gestionar) ──
            var lblPinTitle = new Label {
                Text = "PIN Bot WhatsApp:",
                Location = new Point(15, y), AutoSize = true,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold) };
            Controls.Add(lblPinTitle); y += 24;

            var panelPin = new Panel {
                Location = new Point(15, y), Size = new Size(430, 42),
                BorderStyle = BorderStyle.FixedSingle,
                BackColor = Color.FromArgb(245, 250, 245) };

            lblPinEstado = new Label {
                Text = "Sin PIN asignado",
                Location = new Point(12, 12), AutoSize = true,
                Font = new Font("Segoe UI", 9f) };

            btnGenerarPin = new Button {
                Text = _esNuevo ? "Generar PIN" : "Regenerar PIN",
                Location = new Point(220, 8), Size = new Size(110, 26),
                BackColor = Color.FromArgb(60, 100, 180), ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 8f, FontStyle.Bold),
                Cursor = Cursors.Hand };
            btnGenerarPin.FlatAppearance.BorderSize = 0;
            btnGenerarPin.Click += (s, e) => GenerarPin();

            btnRevocarPin = new Button {
                Text = "Revocar",
                Location = new Point(336, 8), Size = new Size(80, 26),
                BackColor = Color.FromArgb(180, 60, 60), ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 8f, FontStyle.Bold),
                Cursor = Cursors.Hand,
                Visible = false };
            btnRevocarPin.FlatAppearance.BorderSize = 0;
            btnRevocarPin.Click += (s, e) => RevocarPin();

            panelPin.Controls.AddRange(new Control[] { lblPinEstado, btnGenerarPin, btnRevocarPin });
            Controls.Add(panelPin);

            // Solo Admin puede gestionar PINs, y solo para roles Admin/Ventas
            bool esAdminActual = Sesion.Rol == "Admin";
            panelPin.Enabled = esAdminActual;
            if (!esAdminActual)
                lblPinEstado.Text = "(solo Admin puede gestionar PINs)";

            y += 50;

            // ── Activo ───────────────────────────────────────────────────
            chkActivo = new CheckBox {
                Text = "Usuario activo",
                Location = new Point(15, y), AutoSize = true,
                Font = new Font("Segoe UI", 9.5f),
                Checked = true };
            Controls.Add(chkActivo); y += 38;

            // ── Botones ──────────────────────────────────────────────────
            var bOk = new Button {
                Text = _esNuevo ? "Crear Usuario" : "Guardar Cambios",
                Location = new Point(80, y), Size = new Size(160, 34),
                BackColor = Color.FromArgb(20, 90, 50), ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold) };
            bOk.FlatAppearance.BorderSize = 0;

            var bCan = new Button {
                Text = "Cancelar", DialogResult = DialogResult.Cancel,
                Location = new Point(260, y), Size = new Size(120, 34),
                BackColor = Color.FromArgb(120, 120, 130), ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat };
            bCan.FlatAppearance.BorderSize = 0;

            bOk.Click += (s, e) => Aceptar();
            Controls.AddRange(new Control[] { banner, bOk, bCan });

            // ── Precargar datos si es edición ────────────────────────────
            if (!_esNuevo)
            {
                txtNombreCompleto.Text = usuarioExistente.NombreCompleto;
                txtNombreUsuario.Text  = usuarioExistente.NombreUsuario;
                chkActivo.Checked      = usuarioExistente.Activo;

                // Precargar roles
                if (usuarioExistente.EsAdmin)
                {
                    chkAdmin.Checked = true;
                }
                else
                {
                    chkVentas.Checked       = usuarioExistente.TieneRol("Ventas");
                    chkAlmacen.Checked      = usuarioExistente.TieneRol("Almacen");
                    chkContabilidad.Checked = usuarioExistente.TieneRol("Contabilidad");
                }

                if (usuarioExistente.EsAdmin)
                    chkActivo.Enabled = false;

                // Estado del PIN
                if (usuarioExistente.TienePinBot)
                {
                    lblPinEstado.Text = "PIN activo";
                    lblPinEstado.ForeColor = Color.FromArgb(20, 90, 50);
                    btnRevocarPin.Visible = true;
                    btnGenerarPin.Text = "Regenerar PIN";
                }

                // PIN solo disponible para Admin/Ventas
                bool rolPermitePin = DatabaseService.PuedeAsignarPinBot(
                    usuarioExistente.Rol ?? "");
                if (!rolPermitePin && esAdminActual)
                {
                    panelPin.Enabled = false;
                    lblPinEstado.Text = "Solo roles Admin/Ventas pueden tener PIN";
                }

                Usuario = usuarioExistente;
            }
        }

        private void GenerarPin()
        {
            if (_esNuevo)
            {
                MessageBox.Show("Primero guarde el usuario. Luego podrá generar el PIN.",
                    "PIN", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            // Verificar que el rol actual permite PIN
            var roles = ObtenerRolesSeleccionados();
            string rolActual = roles.Count > 0 ? roles[0] : Usuario?.Rol ?? "";
            if (!DatabaseService.PuedeAsignarPinBot(rolActual))
            {
                MessageBox.Show("Solo usuarios con rol Admin o Ventas pueden tener PIN.",
                    "PIN", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string accion = Usuario.TienePinBot ? "regenerar" : "generar";
            if (MessageBox.Show(
                $"¿Desea {accion} el PIN de bot para '{Usuario.NombreCompleto}'?\n\n" +
                "El PIN anterior quedará invalidado.",
                "Confirmar", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
                return;

            try
            {
                string pinPlano = DatabaseService.GenerarPinBot(Usuario.Id);

                // Actualizar modelo local
                Usuario.PinBotHash = "(actualizado)"; // No guardamos el hash localmente
                Usuario.PinBotIntentos = 0;
                Usuario.PinBotBloqueadoHasta = null;

                lblPinEstado.Text = "PIN activo";
                lblPinEstado.ForeColor = Color.FromArgb(20, 90, 50);
                btnRevocarPin.Visible = true;
                btnGenerarPin.Text = "Regenerar PIN";

                MessageBox.Show(
                    $"PIN generado exitosamente:\n\n" +
                    $"    {pinPlano}\n\n" +
                    $"Comuníquelo al operador de forma segura.\n" +
                    $"Este PIN NO se puede recuperar. Si se olvida, genere uno nuevo.",
                    "PIN Generado", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al generar PIN: {ex.Message}",
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void RevocarPin()
        {
            if (_esNuevo || Usuario == null) return;

            if (MessageBox.Show(
                $"¿Desea revocar el PIN de bot de '{Usuario.NombreCompleto}'?\n\n" +
                "El operador ya no podrá usar el bot de WhatsApp.",
                "Revocar PIN", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes)
                return;

            try
            {
                DatabaseService.RevocarPinBot(Usuario.Id);
                Usuario.PinBotHash = "";
                Usuario.PinBotIntentos = 0;
                Usuario.PinBotBloqueadoHasta = null;

                lblPinEstado.Text = "Sin PIN asignado";
                lblPinEstado.ForeColor = SystemColors.ControlText;
                btnRevocarPin.Visible = false;
                btnGenerarPin.Text = "Generar PIN";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al revocar PIN: {ex.Message}",
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void Aceptar()
        {
            // ── Validaciones ─────────────────────────────────────────────
            if (string.IsNullOrWhiteSpace(txtNombreCompleto.Text))
            { Err("El nombre completo es obligatorio."); return; }

            if (string.IsNullOrWhiteSpace(txtNombreUsuario.Text))
            { Err("El nombre de usuario es obligatorio."); return; }

            var roles = ObtenerRolesSeleccionados();
            if (roles.Count == 0)
            { lblRolesError.Text = "Debe seleccionar al menos un rol."; return; }

            if (roles.Contains("Admin") && roles.Count > 1)
            { lblRolesError.Text = "Admin es exclusivo y no puede combinarse."; return; }

            // Contraseña: obligatoria para nuevos, opcional para edición
            string passPlain = txtPassword.Text;
            if (_esNuevo && string.IsNullOrWhiteSpace(passPlain))
            { Err("La contraseña es obligatoria."); return; }

            if (!string.IsNullOrWhiteSpace(passPlain))
            {
                if (passPlain.Length < 6)
                { Err("La contraseña debe tener al menos 6 caracteres."); return; }
                if (passPlain != txtConfirmar.Text)
                { Err("Las contraseñas no coinciden."); return; }
            }

            // ── Construir usuario ────────────────────────────────────────
            if (_esNuevo)
                Usuario = new Usuario { FechaCreacion = DateTime.Now };

            Usuario.NombreCompleto = txtNombreCompleto.Text.Trim();
            Usuario.NombreUsuario  = txtNombreUsuario.Text.Trim().ToLower();
            Usuario.Activo         = chkActivo.Checked;
            Usuario.RolesCSV       = string.Join(",", roles);   // actualiza Rol y Roles

            if (!string.IsNullOrWhiteSpace(passPlain))
                Usuario.PasswordHash = DatabaseService.HashPassword(passPlain);
            else if (_esNuevo)
                Usuario.PasswordHash = DatabaseService.HashPassword(passPlain);
            // En edición con pass vacío, PasswordHash ya tiene el valor guardado

            lblRolesError.Text = "";
            DialogResult = DialogResult.OK;
        }

        private List<string> ObtenerRolesSeleccionados()
        {
            var r = new List<string>();
            if (chkAdmin.Checked)        r.Add("Admin");
            if (chkVentas.Checked)       r.Add("Ventas");
            if (chkAlmacen.Checked)      r.Add("Almacen");
            if (chkContabilidad.Checked) r.Add("Contabilidad");
            return r;
        }

        private void ValidarRoles()
        {
            lblRolesError.Text = ObtenerRolesSeleccionados().Count == 0
                ? "Debe seleccionar al menos un rol."
                : "";
        }

        private void Err(string msg) =>
            MessageBox.Show(msg, "Validación", MessageBoxButtons.OK, MessageBoxIcon.Warning);

        private void L(string t, int y) =>
            Controls.Add(new Label {
                Text = t, Location = new Point(15, y + 3),
                AutoSize = true, Font = new Font("Segoe UI", 9f, FontStyle.Bold) });

        private TextBox Txt(int y)
        {
            var t = new TextBox {
                Location = new Point(185, y), Size = new Size(260, 24),
                Font = new Font("Segoe UI", 9.5f) };
            t.Enter += (s, e) => t.SelectAll();
            Controls.Add(t);
            return t;
        }

        private CheckBox Chk(string label, Point p)
        {
            var c = new CheckBox {
                Text = label, Location = p, AutoSize = true,
                Font = new Font("Segoe UI", 9f) };
            return c;
        }
    }
}
