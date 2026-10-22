#nullable enable
using System;
using System.Threading;
using System.Windows.Forms;

namespace SistemaGestion
{
    static class Program
    {
        private static Mutex? _instanciaMutex;

        [STAThread]
        static void Main()
        {
            // ── Instancia única ─────────────────────────────────────────────
            const string mutexId = "Global\\SistemaGestion_SingleInstance_EHP";
            _instanciaMutex = new Mutex(true, mutexId, out bool esNueva);
            if (!esNueva)
            {
                MessageBox.Show(
                    "SistemaGestion ya está en ejecución.\n\n" +
                    "Si no la ves, revisa la barra de tareas.",
                    "SistemaGestion",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

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

        static void AplicarMonedaDolar()
        {
            var ci = (System.Globalization.CultureInfo)
                System.Globalization.CultureInfo.CurrentCulture.Clone();
            ci.NumberFormat.CurrencySymbol          = "$";
            ci.NumberFormat.CurrencyPositivePattern = 2;   // $ n
            ci.NumberFormat.CurrencyNegativePattern = 12;  // $ -n
            System.Globalization.CultureInfo.DefaultThreadCurrentCulture = ci;
            System.Globalization.CultureInfo.CurrentCulture              = ci;
        }

        static void MainInterno()
        {
            // ── Moneda: el sistema trabaja en dólares ────────────────────────
            //   Se conserva el formato regional de Windows (separadores, fechas)
            //   pero el símbolo de moneda pasa a ser "$". Así todo ToString("C2")
            //   (reportes, PDF, grillas) muestra "$ 1.234,56" y no "1.234,56 €".
            AplicarMonedaDolar();

            // ── DPI awareness — antes de EnableVisualStyles ─────────────────
            Application.SetHighDpiMode(HighDpiMode.PerMonitorV2);
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            // ── 0. Inicializar QuestPDF (licencia Community gratuita) ─────────
            PdfService.Inicializar();

            // ── 0b. Inicializar LibVLC (binarios nativos x64 de VideoLAN) ────
            //   Necesario antes de crear cualquier VideoView. Si falta el
            //   paquete VideoLAN.LibVLC.Windows o el archivo libvlc.dll no
            //   está en bin/, esto lanza DllNotFoundException — capturamos
            //   para no romper el arranque, el módulo de cámaras se desactiva.
            try { LibVLCSharp.Shared.Core.Initialize(); }
            catch (Exception exVlc)
            {
                System.IO.File.AppendAllText(
                    System.IO.Path.Combine(
                        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                        "SistemaGestion", "arranque_error.log"),
                    "[" + DateTime.Now + "] LibVLC.Init falló: " + exVlc.Message + "\n");
            }

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
            DatabaseService.EnsureConfigLegal();

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
