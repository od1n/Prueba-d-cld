using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace SistemaGestion
{
    public class FrmRespaldos : Form
    {
        private DataGridView dgv;
        private Label        lblInfo;
        private Label        lblRuta;

        public FrmRespaldos()
        {
            Text          = "Gestion de Respaldos";
            Size          = new Size(960, 520);
            StartPosition = FormStartPosition.CenterParent;
            BackColor     = Color.FromArgb(244, 248, 244);

            // ── Header ────────────────────────────────────────────────────
            var hdr = new Panel { Dock = DockStyle.Top, Height = 50, BackColor = Color.FromArgb(20, 100, 50) };
            hdr.Controls.Add(new Label
            {
                Text      = "Respaldos de Base de Datos",
                ForeColor = Color.White,
                Font      = new Font("Segoe UI", 12f, FontStyle.Bold),
                AutoSize  = true,
                Location  = new Point(12, 13)
            });

            var btnRespaldo  = Btn("+ Respaldar ahora", Color.FromArgb(30, 140, 70),   new Point(460, 8), 160);
            var btnVerificar = Btn("Verificar",          Color.FromArgb(120, 60, 180),  new Point(630, 8), 100);
            var btnAbrir     = Btn("Abrir carpeta",      Color.FromArgb(60, 100, 180),  new Point(740, 8), 110);
            hdr.Controls.AddRange(new Control[] { btnRespaldo, btnVerificar, btnAbrir });

            // ── Sub-header con ruta ───────────────────────────────────────
            var panelRuta = new Panel
            {
                Dock      = DockStyle.Top,
                Height    = 30,
                BackColor = Color.FromArgb(228, 242, 230)
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
            dgv.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(20, 100, 50);
            dgv.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
            dgv.ColumnHeadersDefaultCellStyle.Font      = new Font("Segoe UI", 9f, FontStyle.Bold);
            dgv.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(235, 248, 238);
            dgv.EnableHeadersVisualStyles = false;
            dgv.DataBindingComplete += (s, e) => ColorearFilas();

            // ── Footer ────────────────────────────────────────────────────
            var footer = new Panel
            {
                Dock      = DockStyle.Bottom,
                Height    = 44,
                BackColor = Color.FromArgb(215, 235, 218)
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
            btnRespaldo.Click  += (s, e) => HacerRespaldoManual();
            btnVerificar.Click += (s, e) => VerificarIntegridad();
            btnAbrir.Click     += (s, e) =>
            {
                Directory.CreateDirectory(BackupService.CarpetaRespaldos);
                System.Diagnostics.Process.Start("explorer.exe", BackupService.CarpetaRespaldos);
            };

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
        private Button Btn(string texto, Color color, Point location, int ancho = 90)
        {
            var b = new Button
            {
                Text      = texto,
                Size      = new Size(ancho, 33),
                Location  = location,
                BackColor = color,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font      = new Font("Segoe UI", 8.5f, FontStyle.Bold),
                Cursor    = Cursors.Hand
            };
            b.FlatAppearance.BorderSize = 0;
            return b;
        }
    }
}
