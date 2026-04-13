using System;
using System.IO;
using System.Linq;
using System.Threading;

namespace SistemaGestion
{
    /// <summary>
    /// Servicio de respaldo automático.
    /// - Al arrancar: crea un respaldo si no existe uno del día de hoy.
    /// - En segundo plano: verifica cada hora y respalda si cambió el día.
    /// - Limpia automáticamente respaldos con más de 30 días de antigüedad.
    /// </summary>
    public static class BackupService
    {
        // ── Configuración ──────────────────────────────────────────────────
        // La ruta de la BD la provee SecurityService
        private const string BACKUP_DIR   = "Respaldos";
        private const int    DIAS_RETENER = 30;
        private const int    INTERVALO_MS = 3_600_000;   // 1 hora

        private static System.Threading.Timer _timer;
        private static string _ultimoRespaldo = "";

        // Carpeta en AppData del usuario para no requerir permisos de admin
        public static string CarpetaRespaldos =>
            Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "SistemaGestion", BACKUP_DIR);

        // ── Arranque ───────────────────────────────────────────────────────
        public static void Iniciar()
        {
            try
            {
                Directory.CreateDirectory(CarpetaRespaldos);
                HacerRespaldoSiNecesario();

                _timer = new System.Threading.Timer(
                    _ => HacerRespaldoSiNecesario(),
                    null, INTERVALO_MS, INTERVALO_MS);
            }
            catch (Exception ex)
            {
                DatabaseService.RegistrarAuditoria("ERROR_BACKUP", "Sistema",
                    $"No se pudo iniciar el servicio de respaldo: {ex.Message}");
            }
        }

        public static void Detener()
        {
            _timer?.Dispose();
            _timer = null;
        }

        // ── Lógica principal ───────────────────────────────────────────────
        private static void HacerRespaldoSiNecesario()
        {
            string fechaHoy = DateTime.Today.ToString("yyyy-MM-dd");
            if (_ultimoRespaldo == fechaHoy) return;

            // ¿Ya existe un archivo de hoy?
            if (Directory.GetFiles(CarpetaRespaldos, $"*_{fechaHoy}_*.enc").Length > 0)
            {
                _ultimoRespaldo = fechaHoy;
                return;
            }

            HacerRespaldo(manual: false);
        }

        /// <summary>Crea un respaldo inmediato. Devuelve el resultado.</summary>
        public static RespaldoResultado HacerRespaldo(bool manual = false)
        {
            var r = new RespaldoResultado();
            try
            {
                string origen = SecurityService.RutaBD;
                if (!File.Exists(origen))
                {
                    r.Exitoso = false;
                    r.Mensaje = $"BD no encontrada en: {origen}";
                    return r;
                }

                string sufijo  = manual ? "_manual" : "_auto";
                string ts      = DateTime.Now.ToString("yyyy-MM-dd_HHmmss");
                string nombre  = $"SistemaGestion_{ts}{sufijo}.db";
                string destino = Path.Combine(CarpetaRespaldos, nombre);

                File.Copy(origen, destino, overwrite: false);

                // Copiar WAL / SHM si existen (escrituras pendientes de SQLite)
                foreach (var ext in new[] { "-wal", "-shm" })
                    if (File.Exists(origen + ext))
                        File.Copy(origen + ext, destino + ext, overwrite: true);

                // Cifrar el respaldo con AES-256 y guardar hash de integridad
                string rutaEnc = SecurityService.CifrarRespaldo(destino);
                SecurityService.GuardarHash(rutaEnc);
                // Eliminar la copia sin cifrar (conservar solo el .enc)
                File.Delete(destino);
                foreach (var ext in new[] { "-wal", "-shm" })
                    if (File.Exists(destino + ext)) File.Delete(destino + ext);
                destino = rutaEnc;   // Apuntar al archivo cifrado

                _ultimoRespaldo = DateTime.Today.ToString("yyyy-MM-dd");

                r.Exitoso      = true;
                r.Archivo      = nombre;
                r.RutaCompleta = destino;
                r.Tamano       = File.Exists(destino) ? new FileInfo(destino).Length : 0;
                r.FechaHora    = DateTime.Now;

                DatabaseService.RegistrarAuditoria(
                    manual ? "BACKUP_MANUAL" : "BACKUP_AUTO",
                    "Sistema",
                    $"{nombre}  |  {FormatBytes(r.Tamano)}");

                LimpiarAntiguos();
            }
            catch (Exception ex)
            {
                r.Exitoso = false;
                r.Mensaje = ex.Message;
                DatabaseService.RegistrarAuditoria("ERROR_BACKUP", "Sistema", ex.Message);
            }
            return r;
        }

        // ── Limpieza ───────────────────────────────────────────────────────
        private static void LimpiarAntiguos()
        {
            try
            {
                var corte = DateTime.Today.AddDays(-DIAS_RETENER);
                var viejos = new DirectoryInfo(CarpetaRespaldos)
                    .GetFiles("*.enc")
                    .Where(f => f.Name.EndsWith(".db.enc"))
                    .Where(f => f.CreationTime < corte)
                    .ToList();

                foreach (var fi in viejos)
                {
                    fi.Delete();
                    foreach (var ext in new[] { "-wal", "-shm" })
                    {
                        var aux = new FileInfo(fi.FullName + ext);
                        if (aux.Exists) aux.Delete();
                    }
                }

                if (viejos.Count > 0)
                    DatabaseService.RegistrarAuditoria("BACKUP_LIMPIEZA", "Sistema",
                        $"Eliminados {viejos.Count} respaldo(s) > {DIAS_RETENER} días");
            }
            catch { }
        }

        // ── Consultas ──────────────────────────────────────────────────────
        public static System.Collections.Generic.List<InfoRespaldo> ListarRespaldos()
        {
            var lista = new System.Collections.Generic.List<InfoRespaldo>();
            if (!Directory.Exists(CarpetaRespaldos)) return lista;

            foreach (var fi in new DirectoryInfo(CarpetaRespaldos)
                .GetFiles("*.enc")
                .Where(f => !f.Name.EndsWith("-wal") && !f.Name.EndsWith("-shm"))
                .OrderByDescending(f => f.CreationTime))
            {
                lista.Add(new InfoRespaldo
                {
                    Nombre       = fi.Name.Replace(".db.enc", ".db"),
                    RutaCompleta = fi.FullName,
                    FechaHora    = fi.CreationTime,
                    Tamano       = fi.Length,
                    EsManual     = fi.Name.Contains("_manual")
                });
            }
            return lista;
        }

        public static string FormatBytes(long b)
        {
            if (b >= 1_048_576) return $"{b / 1_048_576.0:F1} MB";
            if (b >= 1_024)     return $"{b / 1_024.0:F1} KB";
            return $"{b} B";
        }
    }

    // ── DTOs ────────────────────────────────────────────────────────────────
    public class RespaldoResultado
    {
        public bool     Exitoso      { get; set; }
        public string   Archivo      { get; set; }
        public string   RutaCompleta { get; set; }
        public long     Tamano       { get; set; }
        public DateTime FechaHora    { get; set; }
        public string   Mensaje      { get; set; }
    }

    public class InfoRespaldo
    {
        public string   Nombre       { get; set; }
        public string   RutaCompleta { get; set; }
        public DateTime FechaHora    { get; set; }
        public long     Tamano       { get; set; }
        public bool     EsManual     { get; set; }
    }
}
