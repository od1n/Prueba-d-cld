using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace SistemaGestion
{
    public static class OllamaService
    {
        private static readonly HttpClient _http = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };

        /// <summary>
        /// Envía un prompt a un servidor Ollama local y devuelve la respuesta en bruto.
        /// Endpoint asumido: http://localhost:11434/api/generate
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
                max_tokens = maxTokens
            };

            var json = JsonSerializer.Serialize(payload);
            using var content = new StringContent(json, Encoding.UTF8, "application/json");

            using var resp = await _http.PostAsync(uri, content).ConfigureAwait(false);
            var text = await resp.Content.ReadAsStringAsync().ConfigureAwait(false);

            if (!resp.IsSuccessStatusCode)
                throw new Exception($"Ollama returned {(int)resp.StatusCode}: {text}");

            // Intentar extraer un campo 'text' si la respuesta es JSON; si no, devolver la respuesta cruda.
            try
            {
                using var doc = JsonDocument.Parse(text);
                if (doc.RootElement.ValueKind == JsonValueKind.Object && doc.RootElement.TryGetProperty("text", out var t))
                {
                    return t.GetString() ?? text;
                }
            }
            catch
            {
                // ignorar parse errors y devolver el texto tal cual
            }

            return text;
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
