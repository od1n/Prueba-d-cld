using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace SistemaGestion
{
    public static class OllamaService
    {
        // Timeout generoso: los modelos locales pueden tardar varios segundos en responder.
        private static readonly HttpClient _http = new HttpClient { Timeout = TimeSpan.FromSeconds(120) };

        /// <summary>
        /// Envía un prompt a un servidor Ollama local y devuelve el texto generado.
        /// Usa stream:false para recibir una única respuesta JSON completa.
        /// Endpoint: http://localhost:11434/api/generate
        /// </summary>
        public static async Task<string> GenerateAsync(string model, string prompt, int maxTokens = 512)
        {
            if (string.IsNullOrEmpty(model)) throw new ArgumentNullException(nameof(model));
            if (prompt == null) prompt = string.Empty;

            var uri = "http://localhost:11434/api/generate";
            var payload = new
            {
                model,
                prompt,
                stream = false,          // respuesta JSON única (no NDJSON streaming)
                options = new { num_predict = maxTokens }
            };

            var json = JsonSerializer.Serialize(payload);
            using var content = new StringContent(json, Encoding.UTF8, "application/json");

            using var resp = await _http.PostAsync(uri, content).ConfigureAwait(false);
            var text = await resp.Content.ReadAsStringAsync().ConfigureAwait(false);

            if (!resp.IsSuccessStatusCode)
                throw new Exception($"Ollama devolvió {(int)resp.StatusCode}: {text}");

            // La API de Ollama devuelve: {"model":"...","response":"texto generado","done":true,...}
            try
            {
                using var doc = JsonDocument.Parse(text);
                if (doc.RootElement.ValueKind == JsonValueKind.Object)
                {
                    // stream:false → campo "response" con el texto completo
                    if (doc.RootElement.TryGetProperty("response", out var r))
                        return r.GetString()?.Trim() ?? text;
                    // fallback por si cambia la API
                    if (doc.RootElement.TryGetProperty("text", out var t))
                        return t.GetString()?.Trim() ?? text;
                }
            }
            catch
            {
                // Si no es JSON válido devolvemos el texto crudo (poco probable con stream:false)
            }

            return text.Trim();
        }

        /// <summary>
        /// Comprueba si el servidor Ollama local responde (no garantiza API específica).
        /// </summary>
        public static async Task<bool> IsAvailableAsync()
        {
            try
            {
                using var resp = await _http.GetAsync("http://localhost:11434/").ConfigureAwait(false);
                return resp.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }
    }
}
