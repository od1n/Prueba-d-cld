// ════════════════════════════════════════════════════════════════════════
// ARCHIVO: TunelService.cs  →  raíz del proyecto
//
// Estado y control del túnel Cloudflare (servicio Windows "cloudflared")
// desde la app de escritorio. Solo tiene sentido en el equipo donde está
// instalado el servicio (OLIMPO); en otros equipos Instalado = false y la
// app no muestra el indicador.
//
//   • ConsultarAsync()  — estado del servicio + prueba del dominio público
//   • LevantarAsync()   — inicia (o reinicia) el servicio. Primero intenta
//                         sin elevar (deploy\configurar-tunel-olimpo.ps1 da
//                         ese permiso a los usuarios con sesión); si no se
//                         puede, o si el servicio está deshabilitado, pide
//                         permiso de Administrador (ventana de Windows).
//
// No usa paquetes extra: consulta el servicio con powershell.exe
// (Get-Service devuelve los estados en inglés, sin depender del idioma).
// ════════════════════════════════════════════════════════════════════════

#nullable enable
using System;
using System.Diagnostics;
using System.Net.Http;
using System.Threading.Tasks;

namespace SistemaGestion
{
    public enum NivelTunel { NoInstalado, Arriba, Degradado, Caido }

    public sealed class EstadoTunel
    {
        public bool       Instalado    { get; set; }
        public string     Servicio     { get; set; } = "";   // Running / Stopped / ...
        public string     Inicio       { get; set; } = "";   // Automatic / Disabled / ...
        public int        CodigoPublico{ get; set; }         // 200, 502, 530, 0 = sin respuesta
        public NivelTunel Nivel        { get; set; }
        public string     Detalle      { get; set; } = "";
    }

    public static class TunelService
    {
        public const string NombreServicio = "cloudflared";
        public const string UrlPublica     = "https://api-sg.hato-pascher.com/index.html";

        private static readonly HttpClient _http = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };

        public static async Task<EstadoTunel> ConsultarAsync()
        {
            var e = new EstadoTunel();
            var (ok, salida) = await EjecutarPowerShellAsync(
                "$s = Get-Service -Name '" + NombreServicio + "' -ErrorAction SilentlyContinue; " +
                "if ($s) { $s.Status.ToString() + '|' + $s.StartType.ToString() } else { 'NO' }");
            salida = (salida ?? "").Trim();
            if (!ok || salida == "NO" || !salida.Contains('|'))
            {
                e.Instalado = false;
                e.Nivel     = NivelTunel.NoInstalado;
                e.Detalle   = "El servicio cloudflared no está instalado en este equipo.";
                return e;
            }
            e.Instalado = true;
            var partes  = salida.Split('|');
            e.Servicio  = partes[0].Trim();
            e.Inicio    = partes.Length > 1 ? partes[1].Trim() : "";

            e.CodigoPublico = await ProbarPublicoAsync();

            bool corriendo = e.Servicio == "Running";
            if (corriendo && e.CodigoPublico == 200)
            {
                e.Nivel = NivelTunel.Arriba;
                e.Detalle = "Túnel conectado: el sistema responde por api-sg.hato-pascher.com.";
            }
            else if (corriendo)
            {
                e.Nivel = NivelTunel.Degradado;
                e.Detalle = e.CodigoPublico switch
                {
                    502 => "El túnel está conectado pero la API no responde (código 502). Revisa que la API esté corriendo.",
                    530 => "El servicio corre pero Cloudflare todavía no lo ve conectado (código 530). Espera un minuto o reinícialo.",
                    0   => "El servicio corre, pero no hubo respuesta del dominio público (¿sin Internet?).",
                    _   => "El servicio corre, pero el dominio público respondió código " + e.CodigoPublico + "."
                };
            }
            else
            {
                e.Nivel = NivelTunel.Caido;
                e.Detalle = "El servicio cloudflared está '" + e.Servicio + "'" +
                            (e.Inicio == "Disabled" ? " y DESHABILITADO" : "") +
                            ": el sistema no es accesible desde fuera de la oficina.";
            }
            return e;
        }

        /// <summary>
        /// Inicia el servicio (o lo reinicia si ya corre). Devuelve true si
        /// quedó en ejecución. Puede mostrar la ventana de permisos de Windows.
        /// </summary>
        public static async Task<bool> LevantarAsync(bool reiniciar, bool deshabilitado)
        {
            if (!deshabilitado)
            {
                string cmd = reiniciar
                    ? "Restart-Service -Name '" + NombreServicio + "' -Force -ErrorAction Stop"
                    : "Start-Service -Name '" + NombreServicio + "' -ErrorAction Stop";
                var (ok, _) = await EjecutarPowerShellAsync(cmd + "; 'OK'");
                if (ok && await EsperarEnEjecucionAsync()) return true;
            }

            // Sin permiso o deshabilitado: pedir elevación (UAC)
            string elevado =
                "sc.exe config " + NombreServicio + " start= delayed-auto | Out-Null; " +
                (reiniciar ? "Restart-Service -Name '" + NombreServicio + "' -Force"
                           : "Start-Service -Name '" + NombreServicio + "'");
            try
            {
                var psi = new ProcessStartInfo("powershell.exe",
                    "-NoProfile -NonInteractive -ExecutionPolicy Bypass -WindowStyle Hidden -Command \"" + elevado + "\"")
                {
                    UseShellExecute = true,
                    Verb            = "runas",
                    WindowStyle     = ProcessWindowStyle.Hidden
                };
                using var p = Process.Start(psi);
                if (p != null) await Task.Run(() => p.WaitForExit(60000));
            }
            catch (System.ComponentModel.Win32Exception)
            {
                return false;   // el usuario canceló la ventana de permisos
            }
            return await EsperarEnEjecucionAsync();
        }

        // ── Auxiliares ──────────────────────────────────────────────────────
        private static async Task<bool> EsperarEnEjecucionAsync()
        {
            for (int i = 0; i < 10; i++)
            {
                var (ok, s) = await EjecutarPowerShellAsync(
                    "(Get-Service -Name '" + NombreServicio + "').Status.ToString()");
                if (ok && (s ?? "").Trim() == "Running") return true;
                await Task.Delay(1000);
            }
            return false;
        }

        private static async Task<int> ProbarPublicoAsync()
        {
            try
            {
                using var r = await _http.GetAsync(UrlPublica + "?t=" + DateTime.Now.Ticks,
                                                   HttpCompletionOption.ResponseHeadersRead);
                return (int)r.StatusCode;
            }
            catch { return 0; }
        }

        private static async Task<(bool ok, string? salida)> EjecutarPowerShellAsync(string comando)
        {
            try
            {
                var psi = new ProcessStartInfo("powershell.exe",
                    "-NoProfile -NonInteractive -ExecutionPolicy Bypass -Command \"" + comando + "\"")
                {
                    UseShellExecute        = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError  = true,
                    CreateNoWindow         = true
                };
                using var p = Process.Start(psi);
                if (p == null) return (false, null);
                var tOut = p.StandardOutput.ReadToEndAsync();
                var tErr = p.StandardError.ReadToEndAsync();
                await Task.Run(() => p.WaitForExit(20000));
                string salida = await tOut;
                await tErr;
                return (p.HasExited && p.ExitCode == 0, salida);
            }
            catch { return (false, null); }
        }
    }
}
