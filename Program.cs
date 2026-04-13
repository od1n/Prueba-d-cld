using System;
using System.Windows.Forms;

namespace SistemaGestion
{
    static class Program
    {
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            // ── 0. Inicializar QuestPDF (licencia Community gratuita) ─────────
            PdfService.Inicializar();

            // ── 1. Seguridad de archivos (mover BD, permisos NTFS, clave AES)
            SecurityService.Inicializar();

            // ── 2. Capturar excepciones no controladas ─────────────────────
            Application.ThreadException += (s, e) =>
            {
                DatabaseService.RegistrarAuditoria("ERROR", "Sistema", e.Exception.Message);
                MessageBox.Show(
                    $"Error inesperado:\n{e.Exception.Message}\n\nEl error fue registrado.",
                    "Error del sistema", MessageBoxButtons.OK, MessageBoxIcon.Error);
            };

            // ── 3. Inicializar base de datos ───────────────────────────────
            DatabaseService.Inicializar();
            DatabaseService.SeedAdminSiNoExiste();

            // ── 4. Login ───────────────────────────────────────────────────
            using var login = new FrmLogin();
            if (login.ShowDialog() != DialogResult.OK)
                return;

            // ── 5. Alertas al inicio de sesion ─────────────────────────────
            try
            {
                var alertas = AlertasService.ObtenerAlertas();
                if (alertas.TieneAlertasNoSilenciadas)
                    new FrmAlertas(alertas).ShowDialog();
            }
            catch { /* No interrumpir el arranque si falla */ }

            // ── 6. Iniciar servicio de respaldo ────────────────────────────
            BackupService.Iniciar();
            Application.ApplicationExit += (s, e) => BackupService.Detener();

            Application.Run(new MenuPrincipal());
        }
    }
}
