// ════════════════════════════════════════════════════════════════════════
// OcrDigitalizadorService.cs  →  raíz del proyecto (app de escritorio)
//
// Llama DIRECTAMENTE al digitalizador (servicio Python con Ollama/Gemma en
// :8080) por HTTP para extraer los datos de una factura / nota de entrega.
//
// La app de escritorio NO depende de la API web para esto: el digitalizador
// es un microservicio aparte (el mismo que usa el portal). Así el OCR funciona
// desde el escritorio sin abrir el navegador.
//
// Configuración por variables de entorno (ya existen en OLIMPO):
//   SG_DIG_URL         → base del digitalizador (por defecto http://localhost:8080)
//   SG_INTERNAL_TOKEN  → token interno que exige el digitalizador
// ════════════════════════════════════════════════════════════════════════

#nullable enable
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading.Tasks;

namespace SistemaGestion
{
    public class OcrLinea
    {
        public string  Descripcion    { get; set; } = "";
        public decimal Cantidad       { get; set; }
        public string  Unidad         { get; set; } = "";
        public decimal PrecioUnitario { get; set; }
        public decimal Total          { get; set; }
    }

    public class OcrResultado
    {
        public bool           Encontrado    { get; set; } = true;
        public string         Fecha         { get; set; } = "";   // DD/MM/YYYY según el modelo
        public string         NumeroFactura { get; set; } = "";
        public string         Proveedor     { get; set; } = "";
        public decimal        Subtotal      { get; set; }
        public decimal        Iva           { get; set; }
        public decimal        Total         { get; set; }
        public string         Moneda        { get; set; } = "USD";
        public List<OcrLinea> Items         { get; set; } = new();
    }

    public static class OcrDigitalizadorService
    {
        private static readonly HttpClient _http =
            new HttpClient { Timeout = TimeSpan.FromSeconds(180) };

        public static string UrlBase =>
            Environment.GetEnvironmentVariable("SG_DIG_URL")
            ?? Environment.GetEnvironmentVariable("SG_DIG_URL", EnvironmentVariableTarget.Machine)
            ?? "http://localhost:8080";

        private static string Token =>
            Environment.GetEnvironmentVariable("SG_INTERNAL_TOKEN")
            ?? Environment.GetEnvironmentVariable("SG_INTERNAL_TOKEN", EnvironmentVariableTarget.Machine)
            ?? "";

        /// <summary>True si el digitalizador responde (servicio arriba).</summary>
        public static async Task<bool> ServicioActivoAsync()
        {
            try
            {
                using var resp = await _http.GetAsync(UrlBase.TrimEnd('/') + "/api/estado");
                return resp.IsSuccessStatusCode;
            }
            catch { return false; }
        }

        /// <summary>
        /// Envía el archivo (imagen o PDF) al digitalizador y devuelve los datos
        /// extraídos. Lanza excepción con un mensaje claro si algo falla.
        /// </summary>
        public static async Task<OcrResultado> ExtraerAsync(string rutaArchivo)
        {
            if (string.IsNullOrWhiteSpace(rutaArchivo) || !File.Exists(rutaArchivo))
                throw new FileNotFoundException("No se encontró el archivo seleccionado.", rutaArchivo);

            var bytes = await File.ReadAllBytesAsync(rutaArchivo);

            using var form = new MultipartFormDataContent();
            var fileContent = new ByteArrayContent(bytes);
            string ext  = Path.GetExtension(rutaArchivo).ToLowerInvariant();
            string mime = ext switch
            {
                ".pdf"  => "application/pdf",
                ".png"  => "image/png",
                ".webp" => "image/webp",
                _        => "image/jpeg"
            };
            fileContent.Headers.ContentType = new MediaTypeHeaderValue(mime);
            form.Add(fileContent, "imagen", Path.GetFileName(rutaArchivo));
            form.Add(new StringContent("factura"), "tipo");
            form.Add(new StringContent(""),        "backend");   // usa el backend configurado

            using var req = new HttpRequestMessage(HttpMethod.Post, UrlBase.TrimEnd('/') + "/api/extraer")
            { Content = form };
            if (!string.IsNullOrEmpty(Token))
                req.Headers.Add("X-Internal-Token", Token);

            string body; bool exito; int code;
            try
            {
                using var resp = await _http.SendAsync(req);
                body  = await resp.Content.ReadAsStringAsync();
                exito = resp.IsSuccessStatusCode;
                code  = (int)resp.StatusCode;
            }
            catch (Exception ex)
            {
                throw new Exception(
                    "No se pudo contactar al digitalizador en " + UrlBase + ".\n" +
                    "Verifica que el servicio esté corriendo (run.bat) y que SG_DIG_URL apunte a la máquina correcta.\n\n" +
                    "Detalle: " + ex.Message);
            }

            if (!exito)
                throw new Exception($"El digitalizador respondió con error ({code}). {Recortar(body)}");

            using var doc = JsonDocument.Parse(body);
            var root = doc.RootElement;
            if (!root.TryGetProperty("ok", out var ok) || ok.ValueKind == JsonValueKind.False)
            {
                var msg = root.TryGetProperty("error", out var e) ? e.GetString() : "El modelo no pudo procesar el documento.";
                throw new Exception(msg ?? "Error del digitalizador.");
            }

            var datos = root.GetProperty("datos");
            return Parse(datos);
        }

        // ── Parseo del JSON del extractor (PROMPT_FACTURA) ───────────────────
        private static OcrResultado Parse(JsonElement d)
        {
            var r = new OcrResultado
            {
                Encontrado    = !(d.TryGetProperty("encontrado", out var enc) && enc.ValueKind == JsonValueKind.False),
                Fecha         = Str(d, "fecha"),
                NumeroFactura = Str(d, "numero_factura"),
                Proveedor     = Str(d, "proveedor"),
                Subtotal      = Dec(d, "subtotal"),
                Iva           = Dec(d, "iva"),
                Total         = Dec(d, "total"),
                Moneda        = NormalizarMoneda(Str(d, "moneda"))
            };

            if (d.TryGetProperty("items", out var items) && items.ValueKind == JsonValueKind.Array)
            {
                foreach (var it in items.EnumerateArray())
                {
                    r.Items.Add(new OcrLinea
                    {
                        Descripcion    = Str(it, "descripcion"),
                        Cantidad       = Dec(it, "cantidad"),
                        Unidad         = Str(it, "unidad"),
                        PrecioUnitario = Dec(it, "precio_unitario"),
                        Total          = Dec(it, "total")
                    });
                }
            }
            return r;
        }

        private static string NormalizarMoneda(string m)
        {
            m = (m ?? "").Trim().ToUpperInvariant();
            if (m is "VES" or "BS" or "BSD" or "BOLIVARES" or "BOLÍVARES") return "BS";
            return "USD";
        }

        /// <summary>Convierte la fecha DD/MM/YYYY del modelo a DateTime (o null).</summary>
        public static DateTime? ParseFecha(string? fecha)
        {
            if (string.IsNullOrWhiteSpace(fecha)) return null;
            string[] formatos = { "dd/MM/yyyy", "d/M/yyyy", "yyyy-MM-dd", "dd-MM-yyyy" };
            if (DateTime.TryParseExact(fecha.Trim(), formatos, CultureInfo.InvariantCulture,
                    DateTimeStyles.None, out var dt))
                return dt;
            if (DateTime.TryParse(fecha.Trim(), out var dt2)) return dt2;
            return null;
        }

        private static string Str(JsonElement e, string prop) =>
            e.TryGetProperty(prop, out var v) && v.ValueKind == JsonValueKind.String
                ? (v.GetString() ?? "") : "";

        private static decimal Dec(JsonElement e, string prop)
        {
            if (!e.TryGetProperty(prop, out var v)) return 0m;
            if (v.ValueKind == JsonValueKind.Number && v.TryGetDecimal(out var d)) return d;
            if (v.ValueKind == JsonValueKind.String &&
                decimal.TryParse(v.GetString(), NumberStyles.Any, CultureInfo.InvariantCulture, out var ds)) return ds;
            return 0m;
        }

        private static string Recortar(string s) =>
            string.IsNullOrEmpty(s) ? "" : (s.Length > 300 ? s.Substring(0, 300) + "…" : s);
    }
}
