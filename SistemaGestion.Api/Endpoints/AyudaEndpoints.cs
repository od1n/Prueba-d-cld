using System.Text;
using System.Text.Json;

namespace SistemaGestion.Api.Endpoints
{
    /// <summary>
    /// Endpoint de asistente de ayuda:
    ///   POST /api/ayuda/chat  → proxy autenticado hacia Anthropic Claude Opus
    ///
    /// Requiere variable de entorno: ANTHROPIC_API_KEY
    /// Rate limit: "ayuda-opus" (definido en Program.cs)
    /// </summary>
    public static class AyudaEndpoints
    {
        private const string ANTHROPIC_URL     = "https://api.anthropic.com/v1/messages";
        private const string ANTHROPIC_VERSION = "2023-06-01";
        private const string MODEL             = "claude-opus-4-6";
        private const int    MAX_TOKENS        = 1024;
        private const int    MAX_MENSAJES      = 40;
        private const int    MAX_CHARS_MENSAJE = 4000;

        public static void MapAyudaEndpoints(this WebApplication app)
        {
            app.MapPost("/api/ayuda/chat", async (AyudaChatRequest req, IHttpClientFactory http) =>
            {
                // ── Validar configuración ──────────────────────────────────
                var apiKey = Environment.GetEnvironmentVariable("ANTHROPIC_API_KEY");
                if (string.IsNullOrEmpty(apiKey))
                    return Results.Json(
                        new { error = "Asistente no configurado. Contacta al administrador." },
                        statusCode: 503);

                // ── Validar entrada ────────────────────────────────────────
                if (req.Mensajes == null || req.Mensajes.Count == 0)
                    return Results.BadRequest(new { error = "Se requiere al menos un mensaje." });

                if (req.Mensajes.Count > MAX_MENSAJES)
                    return Results.BadRequest(new { error = "Historial demasiado largo. Inicia una nueva conversación." });

                foreach (var m in req.Mensajes)
                {
                    if (m.Role != "user" && m.Role != "assistant")
                        return Results.BadRequest(new { error = "Rol de mensaje inválido." });
                    if (string.IsNullOrWhiteSpace(m.Content) || m.Content.Length > MAX_CHARS_MENSAJE)
                        return Results.BadRequest(new { error = "Contenido de mensaje inválido." });
                }

                // System prompt hardcodeado server-side — el cliente NO lo controla.
                // Esto previene prompt injection desde el frontend.
                const string sistemaPrompt =
                    "Eres un asistente experto en el sistema SistemaGestion, un ERP para " +
                    "una fábrica de plásticos en Venezuela. Ayudas a los usuarios con " +
                    "facturación, inventario, nómina, producción, egresos y reportes. " +
                    "Responde siempre en español neutro, de forma clara y concisa. " +
                    "No ejecutes código ni accedas a sistemas externos.";

                // ── Llamada a Anthropic ────────────────────────────────────
                try
                {
                    var cli = http.CreateClient("anthropic");

                    var payload = new
                    {
                        model      = MODEL,
                        max_tokens = MAX_TOKENS,
                        system     = sistemaPrompt,
                        messages   = req.Mensajes
                                        .Select(m => new { role = m.Role, content = m.Content })
                                        .ToList()
                    };

                    var jsonBody = JsonSerializer.Serialize(payload,
                        new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });

                    using var requestMsg = new HttpRequestMessage(HttpMethod.Post, ANTHROPIC_URL);
                    requestMsg.Headers.Add("x-api-key", apiKey);
                    requestMsg.Headers.Add("anthropic-version", ANTHROPIC_VERSION);
                    requestMsg.Content = new StringContent(jsonBody, Encoding.UTF8, "application/json");

                    var resp = await cli.SendAsync(requestMsg);
                    var body = await resp.Content.ReadAsStringAsync();

                    if (!resp.IsSuccessStatusCode)
                    {
                        Console.WriteLine($"[AYUDA] Anthropic error {(int)resp.StatusCode}: {body[..Math.Min(200, body.Length)]}");
                        return Results.Json(new { error = "Error al consultar el asistente. Intenta nuevamente." }, statusCode: 503);
                    }

                    // Extraer texto de la respuesta Anthropic
                    using var doc  = JsonDocument.Parse(body);
                    var root       = doc.RootElement;
                    var texto      = root.GetProperty("content")[0].GetProperty("text").GetString() ?? "";
                    var inputTok   = root.TryGetProperty("usage", out var uso)
                                     && uso.TryGetProperty("input_tokens",  out var it) ? it.GetInt32() : 0;
                    var outputTok  = uso.TryGetProperty("output_tokens", out var ot) ? ot.GetInt32() : 0;

                    Console.WriteLine($"[AYUDA] tokens in={inputTok} out={outputTok}");

                    return Results.Ok(new { texto });
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[AYUDA ERROR] {ex}");
                    return Results.Json(new { error = "Asistente temporalmente no disponible." }, statusCode: 503);
                }
            }).RequireAuthorization().RequireRateLimiting("ayuda-opus");
        }
    }

    public record AyudaChatRequest(
        List<ChatMessage> Mensajes
    );

    public record ChatMessage(string Role, string Content);
}
