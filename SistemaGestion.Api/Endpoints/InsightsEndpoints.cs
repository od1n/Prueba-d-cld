using System.Text;
using System.Text.Json;

namespace SistemaGestion.Api.Endpoints
{
    /// <summary>
    /// Agente de Insights + RCA — endpoints del portal.
    ///   GET  /api/insights/serie?desde=YYYY-MM-DD&hasta=YYYY-MM-DD  → Capas 1 + 2
    ///   GET  /api/insights/desglose?mes=YYYY-MM                     → Capa 3
    ///   POST /api/insights/narrar   { mes }                        → Capa 4 (narración con Ollama local)
    ///
    /// Las Capas 1-3 son SQL determinístico (DbInsights). La Capa 4 es lo ÚNICO que llama a un
    /// modelo, y solo para narrar números YA calculados — nunca calcula ni inventa cifras.
    /// Usa el MISMO Ollama local que el digitalizador y la app WinForms:
    ///   endpoint http://localhost:11434/api/generate (configurable con la variable SG_OLLAMA_URL),
    ///   modelo desde la config OllamaModelo (igual que FrmSistema / OllamaService).
    /// </summary>
    public static class InsightsEndpoints
    {
        // Mismo host que OllamaService (WinForms). Se puede sobreescribir con SG_OLLAMA_URL
        // (útil en Servo, que puede apuntar al Ollama de Olimpo si no corre uno propio).
        private static readonly string OLLAMA_URL =
            (Environment.GetEnvironmentVariable("SG_OLLAMA_URL")?.TrimEnd('/')
             ?? "http://localhost:11434") + "/api/generate";

        private const int NUM_PREDICT = 500;

        // Instrucciones de la Capa 4 — español neutro, grounding estricto (capa4_prompt_narracion.md)
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
            "7. Responde en 3-5 oraciones y SOLO la explicación, sin preámbulo ni encabezados. No repitas " +
            "todos los números del JSON, solo los que explican el cambio.";

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

            // ── Capa 4 — narración con Ollama local ────────────────────────
            app.MapPost("/api/insights/narrar", async (NarrarRequest req, IHttpClientFactory http) =>
            {
                if (req?.Mes == null || req.Mes.Length != 7 || req.Mes[4] != '-')
                    return Results.BadRequest(new { error = "Parámetro 'mes' inválido. Formato esperado: YYYY-MM." });

                DesgloseResult dg;
                try { dg = DbInsights.Desglose(req.Mes); }
                catch (Exception ex)
                {
                    Console.WriteLine($"[INSIGHTS ERROR narrar/desglose] {ex}");
                    return Results.Json(new { error = "No se pudo preparar el desglose para narrar." }, statusCode: 500);
                }

                // JSON del evento — se arma en el servidor a partir de la Capa 3 (nunca lo genera el modelo)
                var evento = new
                {
                    mes            = dg.Mes,
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

                // Ollama /api/generate no tiene rol "system": se antepone la instrucción al prompt.
                string prompt = SISTEMA_PROMPT +
                    "\n\nDatos del evento (JSON), ya calculados:\n" + eventoJson +
                    "\n\nEscribe ahora la explicación, en español neutro, siguiendo las reglas:";

                string modelo = Db.ObtenerConfig("OllamaModelo", "gemma4:31b-cloud");

                var payload = new
                {
                    model   = modelo,
                    prompt  = prompt,
                    stream  = false,
                    options = new { num_predict = NUM_PREDICT }
                };
                string jsonBody = JsonSerializer.Serialize(payload);

                try
                {
                    var cli = http.CreateClient();
                    cli.Timeout = TimeSpan.FromSeconds(120); // modelos locales pueden tardar
                    using var content = new StringContent(jsonBody, Encoding.UTF8, "application/json");
                    var resp = await cli.PostAsync(OLLAMA_URL, content);
                    var body = await resp.Content.ReadAsStringAsync();

                    if (!resp.IsSuccessStatusCode)
                    {
                        Console.WriteLine($"[INSIGHTS] Ollama error {(int)resp.StatusCode}: {body[..Math.Min(200, body.Length)]}");
                        return Results.Json(new { error = $"El modelo local devolvió un error. Verifica que el modelo '{modelo}' esté descargado en Ollama." }, statusCode: 503);
                    }

                    string texto = "";
                    using (var doc = JsonDocument.Parse(body))
                    {
                        if (doc.RootElement.TryGetProperty("response", out var r))
                            texto = r.GetString() ?? "";
                        else if (doc.RootElement.TryGetProperty("text", out var t))
                            texto = t.GetString() ?? "";
                    }
                    return Results.Ok(new { mes = dg.Mes, narracion = texto.Trim() });
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[INSIGHTS ERROR narrar] {ex}");
                    return Results.Json(new { error = $"Narración no disponible: Ollama no responde en {OLLAMA_URL}. Verifica que el servidor Ollama esté corriendo." }, statusCode: 503);
                }
            }).RequireAuthorization();
        }
    }

    public record NarrarRequest(string Mes);
}
