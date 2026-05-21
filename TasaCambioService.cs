// ════════════════════════════════════════════════════════════════════════
// ARCHIVO: TasaCambioService.cs  →  raíz del proyecto
//
// Consulta automatica de tasas de cambio Venezuela:
//   - BCV (oficial)        — fuente primaria: ve.dolarapi.com/v1/dolares/oficial
//                            fallback: scraping HTML de bcv.org.ve
//   - Paralelo / EnParalelo — fuente: ve.dolarapi.com/v1/dolares/paralelo
//                             fallback: monitor de dolarapi.com (criptodolar)
//
// Diseno:
//   - HttpClient estatico reutilizable (vida del proceso, evita socket exhaustion)
//   - Timeout corto (8s) para no bloquear el arranque si la red falla
//   - Todos los metodos async, sin interaccion UI
//   - Devuelve un TasaResultado con BCV, Paralelo y origen de cada dato
// ════════════════════════════════════════════════════════════════════════

#nullable enable
using System;
using System.Net.Http;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace SistemaGestion
{
    public sealed class TasaResultado
    {
        public decimal BCV          { get; set; }
        public decimal Paralelo     { get; set; }
        public string  OrigenBCV    { get; set; } = "";
        public string  OrigenPar    { get; set; } = "";
        public DateTime FechaBCV    { get; set; }
        public DateTime FechaPar    { get; set; }
        public bool    HayBCV       => BCV > 0;
        public bool    HayParalelo  => Paralelo > 0;
        public string? Error        { get; set; }
    }

    public static class TasaCambioService
    {
        private static readonly HttpClient _http = CrearHttp();

        private static HttpClient CrearHttp()
        {
            var c = new HttpClient
            {
                Timeout = TimeSpan.FromSeconds(8)
            };
            c.DefaultRequestHeaders.Add("User-Agent",
                "SistemaGestion/1.0 (+https://github.com)");
            c.DefaultRequestHeaders.Add("Accept", "application/json,text/html");
            return c;
        }

        // ── API publica ──────────────────────────────────────────────────────

        /// <summary>
        /// Intenta registrar la tasa de hoy de forma silenciosa (sin UI).
        /// Solo actúa si no existe tasa para hoy. No lanza excepciones.
        /// Retorna true si guardó una tasa nueva, false en cualquier otro caso.
        /// </summary>
        public static async Task<bool> ActualizarSilenciosoAsync(string usuario = "auto")
        {
            try
            {
                // Si ya hay tasa de hoy, no hacer nada
                var existente = DatabaseService.ObtenerTasaHoy();
                if (existente != null && existente.Fecha.Date == DateTime.Today)
                    return false;

                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
                var r = await ObtenerTasasActualesAsync(cts.Token);

                if (!r.HayBCV) return false;   // sin BCV no guardamos

                var t = new TasaCambio
                {
                    Fecha         = DateTime.Today,
                    TasaBCV       = r.BCV,
                    TasaParalelo  = r.HayParalelo ? r.Paralelo : 0m,
                    Usuario       = usuario,
                    Origen        = "AUTO_INICIO",
                    FechaRegistro = DateTime.Now
                };
                DatabaseService.GuardarTasa(t);
                return true;
            }
            catch
            {
                return false;   // sin red, sin permisos, etc. — ignorar silenciosamente
            }
        }

        /// <summary>
        /// Consulta ambas tasas (BCV oficial + paralelo) en paralelo.
        /// No lanza excepciones: si una fuente falla, la otra puede tener exito.
        /// </summary>
        public static async Task<TasaResultado> ObtenerTasasActualesAsync(
            CancellationToken ct = default)
        {
            var resultado = new TasaResultado();
            var tBcv = ObtenerBcvAsync(ct);
            var tPar = ObtenerParaleloAsync(ct);

            try { await Task.WhenAll(tBcv, tPar); } catch { /* tratamos cada uno abajo */ }

            if (tBcv.Status == TaskStatus.RanToCompletion)
            {
                var (val, origen, fecha) = tBcv.Result;
                resultado.BCV = val; resultado.OrigenBCV = origen; resultado.FechaBCV = fecha;
            }
            if (tPar.Status == TaskStatus.RanToCompletion)
            {
                var (val, origen, fecha) = tPar.Result;
                resultado.Paralelo = val; resultado.OrigenPar = origen; resultado.FechaPar = fecha;
            }

            if (!resultado.HayBCV && !resultado.HayParalelo)
                resultado.Error = "No se pudo obtener ninguna tasa. " +
                                  "Verifica conexion a internet y reintenta.";
            return resultado;
        }

        // ── BCV oficial ──────────────────────────────────────────────────────
        private static async Task<(decimal, string, DateTime)> ObtenerBcvAsync(CancellationToken ct)
        {
            // Fuente primaria: dolarapi.com (REST JSON estable)
            try
            {
                var json = await _http.GetStringAsync(
                    "https://ve.dolarapi.com/v1/dolares/oficial", ct);
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;
                decimal venta = LeerDecimal(root, "promedio")
                              ?? LeerDecimal(root, "venta")
                              ?? LeerDecimal(root, "compra") ?? 0m;
                if (venta > 0)
                {
                    var fecha = LeerFecha(root, "fechaActualizacion") ?? DateTime.Now;
                    return (venta, "dolarapi.com", fecha);
                }
            }
            catch { /* fallback */ }

            // Fallback: scraping bcv.org.ve (la pagina renderea la tasa USD inline)
            try
            {
                var html = await _http.GetStringAsync(
                    "https://www.bcv.org.ve/", ct);
                // El bloque del USD esta en un div con id="dolar" -> strong -> texto "36,84000000"
                var m = Regex.Match(html,
                    @"id=""dolar""[\s\S]*?<strong>\s*([\d.,]+)\s*</strong>",
                    RegexOptions.IgnoreCase);
                if (m.Success)
                {
                    var raw = m.Groups[1].Value.Trim()
                        .Replace(".", "").Replace(",", ".");
                    if (decimal.TryParse(raw, System.Globalization.NumberStyles.Any,
                                          System.Globalization.CultureInfo.InvariantCulture,
                                          out var val) && val > 0)
                        return (val, "bcv.org.ve", DateTime.Today);
                }
            }
            catch { /* sin BCV */ }

            return (0m, "", DateTime.MinValue);
        }

        // ── Paralelo / EnParalelo ────────────────────────────────────────────
        private static async Task<(decimal, string, DateTime)> ObtenerParaleloAsync(CancellationToken ct)
        {
            // Fuente primaria: dolarapi.com paralelo
            try
            {
                var json = await _http.GetStringAsync(
                    "https://ve.dolarapi.com/v1/dolares/paralelo", ct);
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;
                decimal venta = LeerDecimal(root, "promedio")
                              ?? LeerDecimal(root, "venta")
                              ?? LeerDecimal(root, "compra") ?? 0m;
                if (venta > 0)
                {
                    var fecha = LeerFecha(root, "fechaActualizacion") ?? DateTime.Now;
                    return (venta, "dolarapi.com (paralelo)", fecha);
                }
            }
            catch { /* fallback */ }

            // Fallback: monitor cripto (binance promedio) tambien expuesto por dolarapi
            try
            {
                var json = await _http.GetStringAsync(
                    "https://ve.dolarapi.com/v1/dolares", ct);
                using var doc = JsonDocument.Parse(json);
                foreach (var el in doc.RootElement.EnumerateArray())
                {
                    var nombre = LeerString(el, "nombre")?.ToLower() ?? "";
                    if (nombre.Contains("paralelo") || nombre.Contains("binance"))
                    {
                        decimal venta = LeerDecimal(el, "promedio")
                                      ?? LeerDecimal(el, "venta") ?? 0m;
                        if (venta > 0)
                        {
                            var fecha = LeerFecha(el, "fechaActualizacion") ?? DateTime.Now;
                            return (venta, "dolarapi.com (" + nombre + ")", fecha);
                        }
                    }
                }
            }
            catch { /* sin paralelo */ }

            return (0m, "", DateTime.MinValue);
        }

        // ── Helpers JSON ─────────────────────────────────────────────────────
        private static decimal? LeerDecimal(JsonElement obj, string prop)
        {
            if (!obj.TryGetProperty(prop, out var p)) return null;
            if (p.ValueKind == JsonValueKind.Number)
                return p.TryGetDecimal(out var d) ? d : null;
            if (p.ValueKind == JsonValueKind.String &&
                decimal.TryParse(p.GetString(), System.Globalization.NumberStyles.Any,
                                 System.Globalization.CultureInfo.InvariantCulture, out var ds))
                return ds;
            return null;
        }
        private static string? LeerString(JsonElement obj, string prop) =>
            obj.TryGetProperty(prop, out var p) && p.ValueKind == JsonValueKind.String
                ? p.GetString() : null;
        private static DateTime? LeerFecha(JsonElement obj, string prop)
        {
            var s = LeerString(obj, prop);
            return DateTime.TryParse(s, out var d) ? d : null;
        }
    }
}
