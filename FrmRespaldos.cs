using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace SistemaGestion
{
    public class FrmRespaldos : FormBase
    {
        private DataGridView dgv;
        private Label        lblInfo;
        private Label        lblRuta;

        public FrmRespaldos()
        {
            Text          = "Gestion de Respaldos";
            Size          = new Size(960, 520);
            StartPosition = FormStartPosition.CenterParent;
            BackColor     = Color.FromArgb(240, 244, 250);

            // ── Header ────────────────────────────────────────────────────
            var hdr = new Panel { Dock = DockStyle.Top, Height = 50, BackColor = Color.FromArgb(26, 74, 138) };
            hdr.Controls.Add(new Label
            {
                Text      = "Respaldos de Base de Datos",
                ForeColor = Color.White,
                Font      = new Font("Segoe UI", 12f, FontStyle.Bold),
                AutoSize  = true,
                Location  = new Point(12, 13)
            });

            var btnRespaldo   = Btn("+ Respaldar ahora", Color.FromArgb(30, 140, 70));
            var btnRestaurar  = Btn("Restaurar",          Color.FromArgb(180, 60, 60), 100);
            var btnVerificar  = Btn("Verificar",          Color.FromArgb(120, 60, 180));
            var btnAbrir      = Btn("Abrir carpeta",      Color.FromArgb(26, 74, 138));
            
            
            var _btnAyuda = new Button {
                Text = "? Ayuda", Size = new Size(80, 32),
                BackColor = Color.FromArgb(80, 50, 150), ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
                Cursor = Cursors.Hand, Margin = new Padding(0, 0, 4, 0)
            };
            _btnAyuda.FlatAppearance.BorderSize = 0;
            _btnAyuda.Click += (s, e) => AyudaService.Mostrar(this, "FrmRespaldos");
            var _pnlHdr = new FlowLayoutPanel {
                Dock = DockStyle.Right, AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false, BackColor = Color.Transparent,
                Padding = new Padding(0, 9, 8, 0)
            };
            _pnlHdr.Controls.AddRange(new Control[] { _btnAyuda, btnRespaldo, btnRestaurar, btnVerificar, btnAbrir });
            hdr.Controls.Add(_pnlHdr);
            // ── ToolTips ─────────────────────────────────────────────────
            var _tip = AyudaService.CrearTooltip();
            AyudaService.Tip(_tip, _btnAyuda, "Abrir la guía de ayuda de este módulo");
            AyudaService.Tip(_tip, btnRespaldo, "Crear una copia de seguridad de la base de datos ahora mismo");
            AyudaService.Tip(_tip, btnVerificar, "Verificar la integridad de todos los archivos de respaldo");
            AyudaService.Tip(_tip, btnAbrir, "Abrir la carpeta de respaldos en el Explorador de Windows");

            // ── Sub-header con ruta ───────────────────────────────────────
            var panelRuta = new Panel
            {
                Dock      = DockStyle.Top,
                Height    = 30,
                BackColor = Color.FromArgb(232, 240, 250)
            };
            lblRuta = new Label
            {
                AutoSize  = false,
                Dock      = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                Font      = new Font("Segoe UI", 8.5f, FontStyle.Italic),
                ForeColor = Color.FromArgb(60, 100, 60),
                Padding   = new Padding(10, 0, 0, 0),
                Text      = "Carpeta: " + BackupService.CarpetaRespaldos
            };
            panelRuta.Controls.Add(lblRuta);

            // ── Grid ──────────────────────────────────────────────────────
            dgv = new DataGridView
            {
                Dock                = DockStyle.Fill,
                ReadOnly            = true,
                SelectionMode       = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect         = false,
                AllowUserToAddRows  = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                BackgroundColor     = Color.White,
                BorderStyle         = BorderStyle.None,
                RowHeadersVisible   = false,
                Font                = new Font("Segoe UI", 9f),
                RowTemplate         = { Height = 26 }
            };
            dgv.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(26, 74, 138);
            dgv.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
            dgv.ColumnHeadersDefaultCellStyle.Font      = new Font("Segoe UI", 9f, FontStyle.Bold);
            dgv.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(245, 248, 255);
            dgv.EnableHeadersVisualStyles = false;
            dgv.DataBindingComplete += (s, e) => ColorearFilas();

            // ── Footer ────────────────────────────────────────────────────
            var footer = new Panel
            {
                Dock      = DockStyle.Bottom,
                Height    = 44,
                BackColor = Color.FromArgb(240, 244, 250)
            };
            lblInfo = new Label
            {
                AutoSize  = true,
                Location  = new Point(12, 12),
                Font      = new Font("Segoe UI", 9f),
                ForeColor = Color.FromArgb(20, 80, 40)
            };
            var btnCerrar = new Button
            {
                Text      = "Cerrar",
                Size      = new Size(90, 27),
                Location  = new Point(840, 8),
                BackColor = Color.FromArgb(100, 100, 120),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            btnCerrar.FlatAppearance.BorderSize = 0;
            btnCerrar.Click += (s, e) => Close();
            footer.Controls.AddRange(new Control[] { lblInfo, btnCerrar });

            Controls.AddRange(new Control[] { dgv, panelRuta, hdr, footer });

            // ── Eventos ───────────────────────────────────────────────────
            btnRespaldo.Click   += (s, e) => HacerRespaldoManual();
            btnRestaurar.Click  += (s, e) => RestaurarRespaldo();
            btnVerificar.Click  += (s, e) => VerificarIntegridad();
            btnAbrir.Click     += (s, e) =>
            {
                Directory.CreateDirectory(BackupService.CarpetaRespaldos);
                System.Diagnostics.Process.Start("explorer.exe", BackupService.CarpetaRespaldos);
            };

            
            VentanaPrefs.Restaurar(this);
            var _tp = new System.Windows.Forms.Timer { Interval = 800 };
            _tp.Tick        += (s, e) => { _tp.Stop(); VentanaPrefs.Guardar(this); };
            SizeChanged     += (s, e) => { _tp.Stop(); _tp.Start(); };
            LocationChanged += (s, e) => { _tp.Stop(); _tp.Start(); };
            FormClosed      += (s, e) => { _tp.Stop(); _tp.Dispose(); };

CargarDatos();
        }

        // ── Respaldo manual ───────────────────────────────────────────────
        private void HacerRespaldoManual()
        {
            var resultado = BackupService.HacerRespaldo(manual: true);

            if (resultado.Exitoso)
            {
                MessageBox.Show(
                    "Respaldo creado exitosamente.\n\n" +
                    "Archivo: " + resultado.Archivo + "\n" +
                    "Tamanio: " + BackupService.FormatBytes(resultado.Tamano) + "\n" +
                    "Hora:    " + resultado.FechaHora.ToString("dd/MM/yyyy HH:mm:ss"),
                    "Respaldo exitoso",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
            }
            else
            {
                MessageBox.Show(
                    "No se pudo crear el respaldo:\n" + resultado.Mensaje,
                    "Error en respaldo",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }

            CargarDatos();
        }

        // ── Restaurar respaldo ────────────────────────────────────────────
        private void RestaurarRespaldo()
        {
            if (dgv.CurrentRow == null)
            {
                MessageBox.Show(this, "Selecciona un respaldo de la lista.",
                    "Restaurar", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var nombre = dgv.CurrentRow.Cells["Archivo"].Value?.ToString();
            if (string.IsNullOrEmpty(nombre)) return;

            // Buscar el archivo .enc correspondiente
            string encPath = Path.Combine(BackupService.CarpetaRespaldos,
                nombre.Replace(".db", ".db.enc"));
            if (!File.Exists(encPath))
            {
                MessageBox.Show(this, "No se encontró el archivo:\n" + encPath,
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            var confirm = MessageBox.Show(this,
                "¿Restaurar la base de datos desde este respaldo?\n\n" +
                "Archivo: " + nombre + "\n\n" +
                "ADVERTENCIA: Esto reemplazará TODA la base de datos actual.\n" +
                "Se creará un respaldo de seguridad antes de restaurar.",
                "Confirmar restauración",
                MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
            if (confirm != DialogResult.Yes) return;

            try
            {
                string rutaBD = SecurityService.RutaBD;

                // 1. Respaldar BD actual por seguridad
                string backupActual = rutaBD + ".pre-restauracion";
                if (File.Exists(rutaBD))
                    File.Copy(rutaBD, backupActual, overwrite: true);

                // 2. Descifrar a archivo temporal
                string tempDescifrado = rutaBD + ".restaurando";
                try
                {
                    SecurityService.DescifrarRespaldo(encPath, tempDescifrado);
                }
                catch (Exception exDec)
                {
                    MessageBox.Show(this,
                        "Error al descifrar el respaldo:\n" + exDec.Message,
                        "Error de descifrado", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    if (File.Exists(tempDescifrado)) File.Delete(tempDescifrado);
                    return;
                }

                // 3. Verificar que es SQLite válido
                var header = new byte[16];
                using (var fs = File.OpenRead(tempDescifrado))
                    fs.Read(header, 0, 16);
                if (System.Text.Encoding.ASCII.GetString(header, 0, 13) != "SQLite format")
                {
                    MessageBox.Show(this,
                        "El archivo descifrado no es una base de datos SQLite válida.",
                        "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    File.Delete(tempDescifrado);
                    return;
                }

                // 4. Forzar cierre de conexiones SQLite (liberar archivo)
                GC.Collect();
                GC.WaitForPendingFinalizers();
                Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();

                // 5. Reemplazar BD
                // Eliminar WAL/SHM si existen
                foreach (var ext in new[] { "-wal", "-shm" })
                    if (File.Exists(rutaBD + ext)) File.Delete(rutaBD + ext);

                File.Copy(tempDescifrado, rutaBD, overwrite: true);
                File.Delete(tempDescifrado);

                // 6. Reinicializar BD
                DatabaseService.Inicializar();

                MessageBox.Show(this,
                    "Base de datos restaurada exitosamente desde:\n" + nombre + "\n\n" +
                    "El respaldo de seguridad está en:\n" + backupActual + "\n\n" +
                    "Se recomienda reiniciar la aplicación.",
                    "Restauración exitosa",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this,
                    "Error al restaurar:\n" + ex.Message,
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // ── Verificar integridad ──────────────────────────────────────────
        private void VerificarIntegridad()
        {
            var lista = BackupService.ListarRespaldos();
            if (lista.Count == 0)
            {
                MessageBox.Show(
                    "No hay respaldos para verificar.",
                    "Verificacion",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
                return;
            }

            int ok   = 0;
            int fail = 0;

            foreach (var r in lista)
            {
                // El archivo real en disco tiene extension .db.enc
                string encPath = r.RutaCompleta;
                if (!encPath.EndsWith(".enc"))
                    encPath = encPath + ".enc";

                if (File.Exists(encPath))
                {
                    if (SecurityService.VerificarHash(encPath))
                        ok++;
                    else
                        fail++;
                }
            }

            if (fail == 0)
            {
                MessageBox.Show(
                    "Todos los respaldos son integros (" + ok + " verificados).",
                    "Verificacion de integridad",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
            }
            else
            {
                MessageBox.Show(
                    "ADVERTENCIA: " + fail + " respaldo(s) pueden estar danados o alterados.\n" +
                    ok + " respaldo(s) son correctos.",
                    "Verificacion de integridad",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
            }
        }

        // ── Cargar datos en grid ──────────────────────────────────────────
        private void CargarDatos()
        {
            var lista = BackupService.ListarRespaldos();

            dgv.DataSource = null;
            dgv.DataSource = lista.ConvertAll(r => new
            {
                Archivo = r.Nombre,
                Fecha   = r.FechaHora.ToString("dd/MM/yyyy"),
                Hora    = r.FechaHora.ToString("HH:mm:ss"),
                Tamanio = BackupService.FormatBytes(r.Tamano),
                Tipo    = r.EsManual ? "Manual" : "Automatico",
                Dias    = (DateTime.Today - r.FechaHora.Date).Days == 0
                          ? "Hoy"
                          : "Hace " + (DateTime.Today - r.FechaHora.Date).Days + " dia(s)"
            });

            long totalBytes = 0;
            lista.ForEach(r => totalBytes += r.Tamano);

            lblInfo.Text =
                "  Respaldos: " + lista.Count +
                "   |   Espacio usado: " + BackupService.FormatBytes(totalBytes) +
                "   |   Retencion: 30 dias" +
                "   |   Proximo: " + ProximoRespaldo();
        }

        private void ColorearFilas()
        {
            foreach (DataGridViewRow row in dgv.Rows)
            {
                if (row.Cells["Dias"].Value?.ToString() == "Hoy")
                    row.DefaultCellStyle.BackColor = Color.FromArgb(220, 255, 225);

                if (row.Cells["Tipo"].Value?.ToString() == "Manual")
                    row.DefaultCellStyle.ForeColor = Color.FromArgb(30, 80, 160);
            }
        }

        private string ProximoRespaldo()
        {
            var lista = BackupService.ListarRespaldos();
            if (lista.Count == 0) return "Al reiniciar";
            return lista[0].FechaHora.Date == DateTime.Today
                ? "Manana"
                : "Al reiniciar el sistema";
        }

        // ── Helper ────────────────────────────────────────────────────────
        private Button Btn(string texto, Color color, int ancho = 90)
        {
            var b = new Button
            {
                Text      = texto,
                Size      = new Size(ancho, 32),
                BackColor = color,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font      = new Font("Segoe UI", 8.5f, FontStyle.Bold),
                Cursor    = Cursors.Hand,
                Margin    = new Padding(0, 0, 4, 0)
            };
            b.FlatAppearance.BorderSize = 0;
            return b;
        }
    }
}
