using System.Text;
using System.Text.Json;

namespace SistemaGestion.Api.Endpoints
{
    /// <summary>
    /// Agente de Insights + RCA — endpoints del portal.
    ///   GET  /api/insights/serie?desde=YYYY-MM-DD&hasta=YYYY-MM-DD  → Capas 1 + 2
    ///   GET  /api/insights/desglose?mes=YYYY-MM                     → Capa 3
    ///   POST /api/insights/narrar   { mes }                        → Capa 4 (narración con Claude)
    ///
    /// Las Capas 1-3 son SQL determinístico (DbInsights). La Capa 4 es lo ÚNICO que llama a
    /// Claude, y solo para narrar números YA calculados — nunca calcula ni inventa cifras.
    /// Reutiliza el mismo patrón de AyudaEndpoints (cliente "anthropic", ANTHROPIC_API_KEY).
    /// </summary>
    public static class InsightsEndpoints
    {
        private const string ANTHROPIC_URL     = "https://api.anthropic.com/v1/messages";
        private const string ANTHROPIC_VERSION = "2023-06-01";
        private const string MODEL             = "claude-opus-4-6";
        private const int    MAX_TOKENS        = 700;

        // Prompt de la Capa 4 — español neutro, grounding estricto (capa4_prompt_narracion.md)
        private const string SISTEMA_PROMPT =
            "Eres el motor de narración de un agente de insights de ventas para una empresa " +
            "manufacturera (EHP / SistemaGestion). Se te pasa un objeto JSON con los números YA " +
            "CALCULADOS de un mes marcado como anómalo: mes, valor_anterior, valor_actual, delta_abs, " +
            "delta_pct (la métrica total y su variación); top_productos (productos con venta del mes " +
            "anterior, venta del mes actual, el delta y su % de contribución al delta total); " +
            "top_clientes (clientes con sus ventas totales del mes, no delta); y categoria (desglose por " +
            "categoría, puede venir marcado como no informativo). Reglas estrictas, sin excepción: " +
            "1. Usa EXCLUSIVAMENTE los números que llegan en el JSON. No inventes cifras, fechas, nombres " +
            "ni ningún dato que no esté ahí. " +
            "2. No especules sobre POR QUÉ pasó algo (promoción, estacionalidad, evento externo) salvo que " +
            "el JSON lo incluya explícitamente. Si no hay forma de saber la causa, dilo así de claro: " +
            "\"los datos disponibles no permiten saber por qué\". " +
            "3. Cuantifica siempre — cuando descompongas el cambio en un producto o cliente, da el número " +
            "exacto y el porcentaje, no solo \"una parte importante\". " +
            "4. Si la variación se explica mayormente por un solo producto o cliente (más del 50% del delta " +
            "o del total del mes), dilo explícitamente — es información valiosa por sí sola (concentración). " +
            "5. Si la variación está repartida entre varios sin un factor dominante, dilo también, con la " +
            "misma seguridad. " +
            "6. Tono directo, en español neutro, para el dueño del negocio — sin jerga de bases de datos " +
            "(nunca \"JOIN\", \"query\", \"tabla\"). " +
            "7. 3-5 oraciones. No repitas todos los números del JSON, solo los que explican el cambio.";

        public static void MapInsightsEndpoints(this WebApplication app)
        {
            // ── Capa 1 + 2 ─────────────────────────────────────────────────
            app.MapGet("/api/insights/serie", (string? desde, string? hasta) =>
            {
                string d = desde ?? DateTime.Today.AddMonths(-36).ToString("yyyy-MM-dd");
                string h = hasta ?? DateTime.Today.ToString("yyyy-MM-dd");
                try
                {
                    return Results.Ok(DbInsights.Serie(d, h));
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[INSIGHTS ERROR serie] {ex}");
                    return Results.Json(new { error = "No se pudo calcular la serie de ventas." }, statusCode: 500);
                }
            }).RequireAuthorization();

            // ── Capa 3 ─────────────────────────────────────────────────────
            app.MapGet("/api/insights/desglose", (string? mes) =>
            {
                if (string.IsNullOrWhiteSpace(mes) || mes.Length != 7 || mes[4] != '-')
                    return Results.BadRequest(new { error = "Parámetro 'mes' inválido. Formato esperado: YYYY-MM." });
                try
                {
                    return Results.Ok(DbInsights.Desglose(mes));
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[INSIGHTS ERROR desglose] {ex}");
                    return Results.Json(new { error = "No se pudo calcular el desglose del mes." }, statusCode: 500);
                }
            }).RequireAuthorization();

            // ── Capa 4 — narración con Claude ──────────────────────────────
            app.MapPost("/api/insights/narrar", async (NarrarRequest req, IHttpClientFactory http) =>
            {
                if (req?.Mes == null || req.Mes.Length != 7 || req.Mes[4] != '-')
                    return Results.BadRequest(new { error = "Parámetro 'mes' inválido. Formato esperado: YYYY-MM." });

                var apiKey = Environment.GetEnvironmentVariable("ANTHROPIC_API_KEY");
                if (string.IsNullOrEmpty(apiKey))
                    return Results.Json(new { error = "Narración no configurada. Contacta al administrador." }, statusCode: 503);

                DesgloseResult dg;
                try { dg = DbInsights.Desglose(req.Mes); }
                catch (Exception ex)
                {
                    Console.WriteLine($"[INSIGHTS ERROR narrar/desglose] {ex}");
                    return Results.Json(new { error = "No se pudo preparar el desglose para narrar." }, statusCode: 500);
                }

                // JSON del evento — se arma en el servidor a partir de la Capa 3 (nunca lo genera Claude)
                var evento = new
                {
                    mes           = dg.Mes,
                    valor_anterior = dg.ValorAnterior,
                    valor_actual   = dg.ValorActual,
                    delta_abs      = dg.DeltaAbs,
                    delta_pct      = dg.DeltaPct,
                    top_productos  = dg.TopProductos.Select(p => new {
                        codigo = p.Codigo, descripcion = p.Descripcion,
                        venta_anterior = p.VentaAnterior, venta_actual = p.VentaActual,
                        delta = p.Delta, contrib_pct = p.ContribPct }).ToList(),
                    top_clientes   = dg.TopClientes.Select(cl => new {
                        codigo = cl.Codigo, nombre = cl.Nombre, venta_total_mes = cl.VentaTotalMes }).ToList(),
                    categoria      = dg.Categoria
                };
                string eventoJson = JsonSerializer.Serialize(evento);

                try
                {
                    var cli = http.CreateClient("anthropic");
                    var payload = new
                    {
                        model      = MODEL,
                        max_tokens = MAX_TOKENS,
                        system     = SISTEMA_PROMPT,
                        messages   = new[] { new { role = "user", content = eventoJson } }
                    };
                    var jsonBody = JsonSerializer.Serialize(payload);

                    using var requestMsg = new HttpRequestMessage(HttpMethod.Post, ANTHROPIC_URL);
                    requestMsg.Headers.Add("x-api-key", apiKey);
                    requestMsg.Headers.Add("anthropic-version", ANTHROPIC_VERSION);
                    requestMsg.Content = new StringContent(jsonBody, Encoding.UTF8, "application/json");

                    var resp = await cli.SendAsync(requestMsg);
                    var body = await resp.Content.ReadAsStringAsync();
                    if (!resp.IsSuccessStatusCode)
                    {
                        Console.WriteLine($"[INSIGHTS] Anthropic error {(int)resp.StatusCode}: {body[..Math.Min(200, body.Length)]}");
                        return Results.Json(new { error = "Error al generar la narración. Intenta nuevamente." }, statusCode: 503);
                    }

                    using var doc = JsonDocument.Parse(body);
                    var texto = doc.RootElement.GetProperty("content")[0].GetProperty("text").GetString() ?? "";
                    return Results.Ok(new { mes = dg.Mes, narracion = texto });
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[INSIGHTS ERROR narrar] {ex}");
                    return Results.Json(new { error = "Narración temporalmente no disponible." }, statusCode: 503);
                }
            }).RequireAuthorization().RequireRateLimiting("ayuda-opus");
        }
    }

    public record NarrarRequest(string Mes);
}
