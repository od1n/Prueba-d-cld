using System;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace SistemaGestion
{
    // ══════════════════════════════════════════════════════════════════════
    //  FrmSistema — Configuración del sistema
    //  Los campos de config leen y guardan en DatabaseService (tabla Config).
    // ══════════════════════════════════════════════════════════════════════
    public class FrmSistema : FormBase
    {
        // ── Campos de configuración ────────────────────────────────────────
        private TextBox _txtEmpresa  = null!;
        private TextBox _txtRIF      = null!;
        private TextBox _txtOllama   = null!;
        private Label   _lblOllamaStatus = null!;

        public FrmSistema()
        {
            Text          = "Configuración del Sistema";
            Size          = new Size(560, 620);
            MinimumSize   = new Size(480, 540);
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = MinimizeBox = false;
            BackColor   = Color.FromArgb(245, 245, 250);

            ConstruirUI();
            CargarConfig();
        }

        private void ConstruirUI()
        {
            // ── Header ─────────────────────────────────────────────────────
            var hdr = new Panel { Dock = DockStyle.Top, Height = 50,
                BackColor = Color.FromArgb(26, 74, 138) };
            hdr.Controls.Add(new Label
            {
                Text = "⚙  Configuración del Sistema",
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 11f, FontStyle.Bold),
                AutoSize = true, Location = new Point(14, 13)
            });
            Controls.Add(hdr);

            int y = 68, xLbl = 28, xCtrl = 200, w = 270;

            // ── Sección Empresa ────────────────────────────────────────────
            Seccion("EMPRESA", ref y);
            Campo("Nombre de la empresa:", ref y, out _txtEmpresa, w, xLbl, xCtrl);
            Campo("RIF / NIT:", ref y, out _txtRIF, w, xLbl, xCtrl);

            Separador(ref y);

            // ── Sección Inteligencia Artificial ────────────────────────────
            Seccion("INTELIGENCIA ARTIFICIAL (Ollama)", ref y);

            Lbl("Modelo Ollama:", y, xLbl);
            _txtOllama = new TextBox
            {
                Location = new Point(xCtrl, y - 1), Size = new Size(160, 25),
                Font = new Font("Segoe UI", 9.5f)
            };
            Controls.Add(_txtOllama);

            var btnProbar = new Button
            {
                Text = "Probar conexión",
                Location = new Point(xCtrl + 168, y - 3),
                Size = new Size(120, 28),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(26, 74, 138), ForeColor = Color.White,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold), Cursor = Cursors.Hand
            };
            btnProbar.FlatAppearance.BorderSize = 0;
            btnProbar.FlatAppearance.MouseOverBackColor = Color.FromArgb(45, 108, 192);
            btnProbar.Click += async (s, e) => await ProbarOllamaAsync(btnProbar);
            Controls.Add(btnProbar);
            y += 30;

            _lblOllamaStatus = new Label
            {
                Text = "http://localhost:11434",
                Location = new Point(xCtrl, y),
                AutoSize = true,
                Font = new Font("Segoe UI", 8.5f, FontStyle.Italic),
                ForeColor = Color.FromArgb(122, 131, 149)
            };
            Controls.Add(_lblOllamaStatus);
            y += 24;

            var lblHint = new Label
            {
                Text = "Modelos populares: llama3, mistral, phi3, gemma2",
                Location = new Point(xCtrl, y),
                AutoSize = true,
                Font = new Font("Segoe UI", 8f, FontStyle.Italic),
                ForeColor = Color.FromArgb(150, 160, 180)
            };
            Controls.Add(lblHint);
            y += 30;

            Separador(ref y);

            // ── Sección Sistema ────────────────────────────────────────────
            Seccion("INFORMACIÓN DEL SISTEMA", ref y);

            var lblVer = new Label
            {
                Location = new Point(xLbl, y), AutoSize = true,
                Font = new Font("Segoe UI", 9f, FontStyle.Italic),
                ForeColor = Color.FromArgb(80, 100, 80)
            };
            try
            {
                int ver = DatabaseService.ObtenerVersionEsquema();
                bool alDia = ver == DatabaseService.SCHEMA_VERSION;
                lblVer.Text = $"Esquema BD: v{ver}  (código: v{DatabaseService.SCHEMA_VERSION})" +
                              (alDia ? " — Al día ✓" : " — PENDIENTE MIGRACIÓN");
                lblVer.ForeColor = alDia ? Color.FromArgb(30, 100, 40) : Color.FromArgb(160, 50, 30);
            }
            catch { lblVer.Text = "No se pudo leer versión de esquema"; }
            Controls.Add(lblVer);
            y += 28;

            // ── Botones de acceso ──────────────────────────────────────────
            BotonAcceso("▤  Ver respaldos",        Color.FromArgb(20, 100, 50),
                        () => new FrmRespaldos().ShowDialog(),          ref y, xLbl);
            BotonAcceso("👤  Mi perfil y preferencias", Color.FromArgb(55, 70, 110),
                        () => new FrmPerfil().ShowDialog(),             ref y, xLbl);

            if (Sesion.EsAdmin)
            {
                BotonAcceso("📋  Log de auditoría",  Color.FromArgb(40, 55, 90),
                            () => new FrmLogAuditoria().ShowDialog(),   ref y, xLbl);
                BotonAcceso("👥  Gestión de usuarios", Color.FromArgb(30, 80, 160),
                            () => new FrmUsuarios().ShowDialog(),       ref y, xLbl);
                BotonAcceso("📷  Gestión de cámaras", Color.FromArgb(150, 60, 60),
                            () => new FrmCamarasAdmin().ShowDialog(),   ref y, xLbl);
            }

            y += 8;

            // ── Botones Guardar / Cerrar ───────────────────────────────────
            var btnGuardar = new Button
            {
                Text = "💾  Guardar cambios",
                Location = new Point(xLbl, y), Size = new Size(180, 36),
                BackColor = Color.FromArgb(26, 74, 138), ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 10f, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnGuardar.FlatAppearance.BorderSize = 0;
            btnGuardar.FlatAppearance.MouseOverBackColor = Color.FromArgb(45, 108, 192);
            btnGuardar.Click += GuardarConfig;
            Controls.Add(btnGuardar);

            var btnCerrar = new Button
            {
                Text = "Cerrar",
                Location = new Point(xLbl + 190, y), Size = new Size(110, 36),
                BackColor = Color.FromArgb(100, 100, 120), ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 10f)
            };
            btnCerrar.FlatAppearance.BorderSize = 0;
            btnCerrar.Click += (s, e) => Close();
            Controls.Add(btnCerrar);

            // Ajustar alto del form al contenido
            this.ClientSize = new Size(ClientSize.Width, y + 80);
        }

        // ── Helpers de construcción ────────────────────────────────────────
        private void Seccion(string titulo, ref int y)
        {
            var lbl = new Label
            {
                Text = titulo,
                Location = new Point(28, y),
                AutoSize = true,
                Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
                ForeColor = Color.FromArgb(26, 74, 138)
            };
            Controls.Add(lbl);
            var line = new Panel
            {
                Location = new Point(28, y + 16), Size = new Size(480, 1),
                BackColor = Color.FromArgb(215, 222, 230)
            };
            Controls.Add(line);
            y += 26;
        }

        private void Separador(ref int y)
        {
            y += 8;
            Controls.Add(new Panel
            {
                Location = new Point(28, y), Size = new Size(480, 1),
                BackColor = Color.FromArgb(225, 228, 235)
            });
            y += 14;
        }

        private void Lbl(string texto, int y, int x) => Controls.Add(new Label
        {
            Text = texto, Location = new Point(x, y + 3), AutoSize = true,
            Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
            ForeColor = Color.FromArgb(50, 60, 80)
        });

        private void Campo(string etiqueta, ref int y, out TextBox tb,
                           int ancho, int xLbl, int xCtrl)
        {
            Lbl(etiqueta, y, xLbl);
            tb = new TextBox
            {
                Location = new Point(xCtrl, y - 1), Size = new Size(ancho, 25),
                Font = new Font("Segoe UI", 9.5f)
            };
            Controls.Add(tb);
            y += 36;
        }

        private void BotonAcceso(string texto, Color color, Action accion, ref int y, int x)
        {
            var b = new Button
            {
                Text = texto, Location = new Point(x, y), Size = new Size(220, 32),
                BackColor = color, ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                Cursor = Cursors.Hand, TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(8, 0, 0, 0)
            };
            b.FlatAppearance.BorderSize = 0;
            b.Click += (s, e) => { try { accion(); } catch (Exception ex) {
                MessageBox.Show(this, ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error); }};
            Controls.Add(b);
            y += 40;
        }

        // ── Lógica ─────────────────────────────────────────────────────────
        private void CargarConfig()
        {
            try
            {
                _txtEmpresa.Text = DatabaseService.ObtenerConfig("NombreEmpresa", "Mi Empresa");
                _txtRIF.Text     = DatabaseService.ObtenerConfig("RIF", "");
                _txtOllama.Text  = DatabaseService.ObtenerConfig("OllamaModelo", "llama3");
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, "Error al cargar configuración: " + ex.Message,
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void GuardarConfig(object? sender, EventArgs e)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(_txtEmpresa.Text))
                {
                    MessageBox.Show(this, "El nombre de la empresa no puede estar vacío.",
                        "Validación", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    _txtEmpresa.Focus();
                    return;
                }
                if (string.IsNullOrWhiteSpace(_txtOllama.Text))
                    _txtOllama.Text = "llama3";

                DatabaseService.GuardarConfig("NombreEmpresa", _txtEmpresa.Text.Trim());
                DatabaseService.GuardarConfig("RIF",           _txtRIF.Text.Trim());
                DatabaseService.GuardarConfig("OllamaModelo",  _txtOllama.Text.Trim());

                DatabaseService.RegistrarAuditoria("CONFIG_GUARDADA", "Sistema",
                    $"Empresa={_txtEmpresa.Text.Trim()}");

                MessageBox.Show(this, "Configuración guardada correctamente.",
                    "Éxito", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, "Error al guardar: " + ex.Message,
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async Task ProbarOllamaAsync(Button btn)
        {
            btn.Enabled = false;
            btn.Text    = "Probando…";
            _lblOllamaStatus.Text      = "Conectando a http://localhost:11434…";
            _lblOllamaStatus.ForeColor = Color.FromArgb(122, 131, 149);

            try
            {
                bool disponible = await OllamaService.IsAvailableAsync();
                if (disponible)
                {
                    string modelo = _txtOllama.Text.Trim();

                    // Verificar que el modelo específico existe haciendo un generate mínimo
                    try
                    {
                        await OllamaService.GenerateAsync(modelo, "di solo: ok", 5);
                        _lblOllamaStatus.Text      = $"✓  Ollama activo — modelo \"{modelo}\" disponible";
                        _lblOllamaStatus.ForeColor = Color.FromArgb(15, 110, 86);
                    }
                    catch
                    {
                        _lblOllamaStatus.Text =
                            $"⚠  Ollama activo, pero el modelo \"{modelo}\" no está descargado. " +
                            $"Ejecuta: ollama pull {modelo}";
                        _lblOllamaStatus.ForeColor = Color.FromArgb(198, 40, 40);
                    }
                }
                else
                {
                    _lblOllamaStatus.Text      = "✗  Sin respuesta en http://localhost:11434 — verifica que Ollama esté corriendo";
                    _lblOllamaStatus.ForeColor = Color.FromArgb(198, 40, 40);
                }
            }
            catch (Exception ex)
            {
                _lblOllamaStatus.Text      = $"✗  Error: {ex.Message}";
                _lblOllamaStatus.ForeColor = Color.FromArgb(198, 40, 40);
            }
            finally
            {
                btn.Enabled = true;
                btn.Text    = "Probar conexión";
            }
        }
    }
}
