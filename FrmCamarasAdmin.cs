// ════════════════════════════════════════════════════════════════════════
// ARCHIVO: FrmCamarasAdmin.cs  →  raíz del proyecto
//
// Pantalla de administración de cámaras (solo Admin).
//   - Tab "Cámaras": grid con CRUD (Nueva / Editar / Eliminar / Refrescar).
//   - Tab "Permisos": elegir un usuario y marcar las cámaras que verá.
//     Admin no aparece en la lista (ya ve todas implícitamente).
// ════════════════════════════════════════════════════════════════════════

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace SistemaGestion
{
    public class FrmCamarasAdmin : FormBase
    {
        // Tab cámaras
        private DataGridView _dgvCamaras;
        // Tab permisos
        private ComboBox       _cmbUsuario;
        private CheckedListBox _clbCamaras;
        private Label          _lblResumenPermisos;

        public FrmCamarasAdmin()
        {
            Text            = "Administración de cámaras";
            Size            = new Size(900, 600);
            MinimumSize     = new Size(700, 480);
            StartPosition   = FormStartPosition.CenterScreen;
            FormBorderStyle = FormBorderStyle.Sizable;

            var tabs = new TabControl
            {
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 9.5f)
            };
            tabs.TabPages.Add(TabCamaras());
            tabs.TabPages.Add(TabPermisos());
            Controls.Add(tabs);

            CargarCamaras();
            CargarUsuarios();
        }

        // ── TAB 1: Cámaras ───────────────────────────────────────────────────
        private TabPage TabCamaras()
        {
            var tab = new TabPage("Cámaras") { Padding = new Padding(8) };

            var bar = new Panel { Dock = DockStyle.Top, Height = 44 };
            var btnNueva = CrearBoton("Nueva", Color.FromArgb(60, 130, 200), (s, e) => Nueva());
            var btnEdit  = CrearBoton("Editar", Color.FromArgb(80, 100, 160),(s, e) => Editar());
            var btnDel   = CrearBoton("Eliminar", Color.FromArgb(180, 60, 60),(s, e) => Eliminar());
            var btnRef   = CrearBoton("Refrescar", Color.FromArgb(100, 100, 110),(s, e) => CargarCamaras());

            // Orden visual: agregar de derecha a izquierda con Dock=Left
            bar.Controls.Add(btnRef);
            bar.Controls.Add(btnDel);
            bar.Controls.Add(btnEdit);
            bar.Controls.Add(btnNueva);

            _dgvCamaras = new DataGridView
            {
                Dock                       = DockStyle.Fill,
                AutoGenerateColumns        = false,
                AllowUserToAddRows         = false,
                AllowUserToDeleteRows      = false,
                AllowUserToResizeRows      = false,
                MultiSelect                = false,
                SelectionMode              = DataGridViewSelectionMode.FullRowSelect,
                ReadOnly                   = true,
                RowHeadersVisible          = false,
                BorderStyle                = BorderStyle.FixedSingle,
                BackgroundColor            = Color.White,
                ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
                {
                    Font      = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                    BackColor = Color.FromArgb(240, 242, 246)
                }
            };
            _dgvCamaras.Columns.AddRange(new DataGridViewColumn[]
            {
                new DataGridViewTextBoxColumn { HeaderText = "Id",         DataPropertyName = "Id",         Width = 50 },
                new DataGridViewTextBoxColumn { HeaderText = "Nombre",     DataPropertyName = "Nombre",     Width = 180 },
                new DataGridViewTextBoxColumn { HeaderText = "Ubicación",  DataPropertyName = "Ubicacion",  Width = 150 },
                new DataGridViewTextBoxColumn { HeaderText = "IP",         DataPropertyName = "IpLocal",    Width = 110 },
                new DataGridViewTextBoxColumn { HeaderText = "Puerto",     DataPropertyName = "PuertoRtsp", Width = 60  },
                new DataGridViewTextBoxColumn { HeaderText = "Modelo",     DataPropertyName = "Modelo",     Width = 90  },
                new DataGridViewTextBoxColumn { HeaderText = "Ruta",       DataPropertyName = "RutaStream", Width = 130 },
                new DataGridViewCheckBoxColumn{ HeaderText = "Activa",     DataPropertyName = "Activa",     Width = 60  }
            });
            _dgvCamaras.CellDoubleClick += (s, e) => { if (e.RowIndex >= 0) Editar(); };

            tab.Controls.Add(_dgvCamaras);
            tab.Controls.Add(bar);
            return tab;
        }

        private Button CrearBoton(string text, Color bg, EventHandler onClick)
        {
            var b = new Button
            {
                Text      = text,
                Dock      = DockStyle.Left,
                Width     = 110,
                Margin    = new Padding(0, 0, 6, 0),
                BackColor = bg,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font      = new Font("Segoe UI", 9.5f, FontStyle.Bold)
            };
            b.FlatAppearance.BorderSize = 0;
            b.Click += onClick;
            return b;
        }

        private void CargarCamaras()
        {
            try
            {
                var lista = DatabaseService.ObtenerTodasCamaras(incluirInactivas: true);
                _dgvCamaras.DataSource = lista;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error cargando cámaras: " + ex.Message,
                    "Cámaras", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private Camara FilaSeleccionada()
        {
            if (_dgvCamaras.SelectedRows.Count == 0) return null;
            return _dgvCamaras.SelectedRows[0].DataBoundItem as Camara;
        }

        private void Nueva()
        {
            using var dlg = new DlgCamara();
            if (dlg.ShowDialog(this) == DialogResult.OK && dlg.Resultado != null)
            {
                try
                {
                    DatabaseService.InsertarCamara(dlg.Resultado);
                    CargarCamaras();
                    CargarPermisosUsuario();   // refresca el checked list de la tab 2
                }
                catch (Exception ex)
                {
                    MessageBox.Show("No se pudo guardar: " + ex.Message,
                        "Cámaras", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void Editar()
        {
            var cam = FilaSeleccionada();
            if (cam == null)
            {
                MessageBox.Show("Selecciona una cámara primero.", "Cámaras",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            // Recargar desde BD para tener la ClaveCifrada actual exacta
            var fresca = DatabaseService.ObtenerCamaraPorId(cam.Id);
            if (fresca == null) { CargarCamaras(); return; }

            using var dlg = new DlgCamara(fresca);
            if (dlg.ShowDialog(this) == DialogResult.OK && dlg.Resultado != null)
            {
                try
                {
                    DatabaseService.ActualizarCamara(dlg.Resultado);
                    CargarCamaras();
                    CargarPermisosUsuario();
                }
                catch (Exception ex)
                {
                    MessageBox.Show("No se pudo actualizar: " + ex.Message,
                        "Cámaras", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void Eliminar()
        {
            var cam = FilaSeleccionada();
            if (cam == null) return;

            var resp = MessageBox.Show(
                $"¿Eliminar la cámara '{cam.Nombre}'?\n\n" +
                "Esto también borra los permisos asignados a usuarios.\n" +
                "La operación NO se puede deshacer.",
                "Confirmar eliminación",
                MessageBoxButtons.YesNo, MessageBoxIcon.Warning);

            if (resp != DialogResult.Yes) return;

            try
            {
                DatabaseService.EliminarCamara(cam.Id);
                CargarCamaras();
                CargarPermisosUsuario();
            }
            catch (Exception ex)
            {
                MessageBox.Show("No se pudo eliminar: " + ex.Message,
                    "Cámaras", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // ── TAB 2: Permisos ──────────────────────────────────────────────────
        private TabPage TabPermisos()
        {
            var tab = new TabPage("Permisos") { Padding = new Padding(12) };

            var lblHint = new Label
            {
                Text     = "Marca las cámaras que cada usuario podrá ver en el visor.\n" +
                           "Los administradores ven todas las cámaras automáticamente.",
                Dock     = DockStyle.Top,
                Height   = 38,
                ForeColor = Color.FromArgb(80, 80, 90)
            };

            var pnlTop = new Panel { Dock = DockStyle.Top, Height = 44 };
            var lblUsr = new Label
            {
                Text     = "Usuario:",
                Location = new Point(0, 12),
                AutoSize = true,
                Font     = new Font("Segoe UI", 9.5f, FontStyle.Bold)
            };
            _cmbUsuario = new ComboBox
            {
                Location      = new Point(70, 8),
                Width         = 320,
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            _cmbUsuario.SelectedIndexChanged += (s, e) => CargarPermisosUsuario();

            var btnGuardar = new Button
            {
                Text      = "Guardar permisos",
                Location  = new Point(410, 7),
                Size      = new Size(160, 30),
                BackColor = Color.FromArgb(60, 130, 200),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font      = new Font("Segoe UI", 9.5f, FontStyle.Bold)
            };
            btnGuardar.FlatAppearance.BorderSize = 0;
            btnGuardar.Click += BtnGuardarPermisos_Click;

            pnlTop.Controls.Add(lblUsr);
            pnlTop.Controls.Add(_cmbUsuario);
            pnlTop.Controls.Add(btnGuardar);

            _clbCamaras = new CheckedListBox
            {
                Dock          = DockStyle.Fill,
                CheckOnClick  = true,
                BorderStyle   = BorderStyle.FixedSingle,
                Font          = new Font("Segoe UI", 9.5f),
                IntegralHeight= false
            };

            _lblResumenPermisos = new Label
            {
                Dock      = DockStyle.Bottom,
                Height    = 24,
                TextAlign = ContentAlignment.MiddleLeft,
                ForeColor = Color.FromArgb(80, 80, 90),
                Padding   = new Padding(2, 4, 2, 4)
            };

            tab.Controls.Add(_clbCamaras);
            tab.Controls.Add(_lblResumenPermisos);
            tab.Controls.Add(pnlTop);
            tab.Controls.Add(lblHint);
            return tab;
        }

        private List<Usuario> _usuariosNoAdmin;

        private void CargarUsuarios()
        {
            try
            {
                var todos = DatabaseService.ObtenerUsuarios();
                _usuariosNoAdmin = todos
                    .Where(u => !string.Equals(u.Rol, "Admin", StringComparison.OrdinalIgnoreCase)
                             && u.Activo)
                    .OrderBy(u => u.NombreUsuario)
                    .ToList();

                _cmbUsuario.Items.Clear();
                foreach (var u in _usuariosNoAdmin)
                    _cmbUsuario.Items.Add(u.NombreUsuario + "  —  " + u.NombreCompleto);

                if (_cmbUsuario.Items.Count > 0) _cmbUsuario.SelectedIndex = 0;
                else _clbCamaras.Items.Clear();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error cargando usuarios: " + ex.Message,
                    "Permisos", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void CargarPermisosUsuario()
        {
            _clbCamaras.Items.Clear();
            if (_cmbUsuario.SelectedIndex < 0) return;
            var usr = _usuariosNoAdmin[_cmbUsuario.SelectedIndex];

            try
            {
                var todas       = DatabaseService.ObtenerTodasCamaras(incluirInactivas: true);
                var asignadas   = DatabaseService.ObtenerPermisosCamara(usr.Id);
                var setAsignado = new HashSet<int>(asignadas);

                foreach (var c in todas)
                {
                    string label = c.Nombre +
                                   (string.IsNullOrEmpty(c.Ubicacion) ? "" : " — " + c.Ubicacion) +
                                   (c.Activa ? "" : "  [Inactiva]");
                    _clbCamaras.Items.Add(new CamItem(c, label),
                                          setAsignado.Contains(c.Id));
                }
                ActualizarResumen();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error cargando permisos: " + ex.Message,
                    "Permisos", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            _clbCamaras.ItemCheck -= ItemCheckActualiza;
            _clbCamaras.ItemCheck += ItemCheckActualiza;
        }

        private void ItemCheckActualiza(object sender, ItemCheckEventArgs e)
        {
            // Actualizar el resumen DESPUÉS de aplicar el cambio:
            BeginInvoke(new Action(ActualizarResumen));
        }

        private void ActualizarResumen()
        {
            int total      = _clbCamaras.Items.Count;
            int marcadas   = _clbCamaras.CheckedItems.Count;
            _lblResumenPermisos.Text = $"{marcadas} de {total} cámaras seleccionadas.";
        }

        private void BtnGuardarPermisos_Click(object sender, EventArgs e)
        {
            if (_cmbUsuario.SelectedIndex < 0) return;
            var usr = _usuariosNoAdmin[_cmbUsuario.SelectedIndex];

            var ids = new List<int>();
            foreach (CamItem item in _clbCamaras.CheckedItems)
                ids.Add(item.Camara.Id);

            try
            {
                DatabaseService.GuardarPermisosCamara(usr.Id, ids);
                MessageBox.Show($"Permisos guardados ({ids.Count} cámaras) " +
                                $"para {usr.NombreUsuario}.",
                    "Permisos", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show("No se pudieron guardar los permisos: " + ex.Message,
                    "Permisos", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // Wrapper para mostrar texto custom en el CheckedListBox
        private class CamItem
        {
            public Camara Camara { get; }
            private readonly string _label;
            public CamItem(Camara c, string label) { Camara = c; _label = label; }
            public override string ToString() => _label;
        }
    }
}
