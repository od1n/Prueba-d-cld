using System;
using System.Windows.Forms;

namespace SistemaGestion
{
    static class Program
    {
        [STAThread]
        static void Main()
        {
            // Try/catch global: cualquier excepcion del arranque (antes de
            // Application.Run) muestra MessageBox con stack completo + escribe
            // a un log en %AppData%\SistemaGestion\arranque_error.log
            try
            {
                MainInterno();
            }
            catch (Exception ex)
            {
                string log = "";
                try
                {
                    string ruta = System.IO.Path.Combine(
                        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                        "SistemaGestion", "arranque_error.log");
                    System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(ruta)!);
                    log = "[" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "] " +
                          ex.GetType().FullName + ": " + ex.Message + "\n" +
                          (ex.InnerException != null ? "INNER: " + ex.InnerException.Message + "\n" : "") +
                          ex.StackTrace + "\n\n";
                    System.IO.File.AppendAllText(ruta, log);
                    log = "Detalle guardado en:\n" + ruta + "\n\n";
                }
                catch { }

                MessageBox.Show(
                    "ERROR FATAL DURANTE EL ARRANQUE\n\n" +
                    log +
                    ex.GetType().Name + ":\n" + ex.Message +
                    (ex.InnerException != null ? "\n\nInner: " + ex.InnerException.Message : "") +
                    "\n\nStack:\n" + ex.StackTrace,
                    "SistemaGestion — Error fatal",
                    MessageBoxButtons.OK, MessageBoxIcon.Stop);
            }
        }

        static void MainInterno()
        {
            // ── DPI awareness — antes de EnableVisualStyles ─────────────────
            Application.SetHighDpiMode(HighDpiMode.PerMonitorV2);
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            // ── 0. Inicializar QuestPDF (licencia Community gratuita) ─────────
            PdfService.Inicializar();

            // ── 1. Seguridad de archivos (mover BD, permisos NTFS, clave AES)
            SecurityService.Inicializar();

            // ── 2. Capturar excepciones no controladas ─────────────────────
            Application.ThreadException += (s, e) =>
            {
                try { DatabaseService.RegistrarAuditoria("ERROR", "Sistema", e.Exception.Message); } catch { }
                MessageBox.Show(
                    $"Error inesperado:\n{e.Exception.Message}\n\nStack:\n{e.Exception.StackTrace}",
                    "Error del sistema", MessageBoxButtons.OK, MessageBoxIcon.Error);
            };
            AppDomain.CurrentDomain.UnhandledException += (s, e) =>
            {
                if (e.ExceptionObject is Exception ex)
                    MessageBox.Show(
                        "Excepción no controlada del dominio:\n\n" + ex.Message + "\n\n" + ex.StackTrace,
                        "Error fatal", MessageBoxButtons.OK, MessageBoxIcon.Stop);
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
            catch (Exception exAlert)
            {
                // ALTA-6 FIX: No silenciar completamente — registrar en auditoría
                DatabaseService.RegistrarAuditoria("ERROR_ALERTAS", "Sistema",
                    $"Fallo al cargar alertas al inicio: {exAlert.Message}");
            }

            // ── 5b. Chequeo diario de tasa BCV/paralelo ────────────────────
            //   Una vez al dia, al primer login, verifica si la tasa del dia
            //   ya esta cargada. Si no esta y el usuario no la pospuso, abre
            //   el dialogo de actualizacion automatica via TasaCambioService.
            try
            {
                var tasaHoy = DatabaseService.ObtenerTasaPorFecha(DateTime.Today);
                bool yaTengoHoy = tasaHoy != null && tasaHoy.Fecha.Date == DateTime.Today;
                string pospuestaHasta = DatabaseService.ObtenerConfig("TasaPospuestaHasta", "");
                bool pospuestaActiva = DateTime.TryParse(pospuestaHasta, out var pd) && pd > DateTime.Today;

                if (!yaTengoHoy && !pospuestaActiva)
                {
                    var anterior = DatabaseService.ObtenerTasaHoy();
                    using var frmTasa = new FrmActualizarTasa(anterior);
                    frmTasa.ShowDialog();
                }
            }
            catch (Exception exTasa)
            {
                DatabaseService.RegistrarAuditoria("ERROR_TASA_AUTO", "Sistema",
                    $"Fallo chequeo diario de tasa: {exTasa.Message}");
            }

            // ── 6. Iniciar servicio de respaldo ────────────────────────────
            BackupService.Iniciar();
            Application.ApplicationExit += (s, e) => BackupService.Detener();

            Application.Run(new MenuPrincipalV2());
        }
    }
}
