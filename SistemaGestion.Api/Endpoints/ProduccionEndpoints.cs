using Microsoft.Data.Sqlite;

namespace SistemaGestion.Api.Endpoints
{
    /// <summary>
    /// Endpoints de producción:
    ///   GET  /api/produccion/moldes   → catálogo TbMoldes desde BD
    ///   GET  /api/produccion/insumos  → ProductosProveedor activos
    ///   GET  /api/produccion/estado   → estado del servidor Python (digitalizador)
    ///   POST /api/produccion/extraer  → proxy multipart → Python :8080/api/extraer
    ///   POST /api/produccion/confirmar → proxy JSON     → Python :8080/api/confirmar
    /// </summary>
    public static class ProduccionEndpoints
    {
        private static readonly string DIG_URL =
            Environment.GetEnvironmentVariable("SG_DIG_URL") ?? "http://localhost:8080";

        public static void MapProduccionEndpoints(this WebApplication app)
        {
            // ── Catálogos (BD directa) ────────────────────────────────────

            app.MapGet("/api/produccion/moldes", () =>
            {
                try
                {
                    using var c = Db.Conn();
                    var cmd = c.CreateCommand();
                    cmd.CommandText =
                        "SELECT CodigoMold, Descripcion FROM TbMoldes ORDER BY CodigoMold";
                    var lista = new List<object>();
                    using var r = cmd.ExecuteReader();
                    while (r.Read())
                        lista.Add(new { codigo = Db.S(r, 0), descripcion = Db.S(r, 1) });
                    return Results.Ok(lista);
                }
                catch { return Results.Ok(new List<object>()); }
            }).RequireAuthorization();

            app.MapGet("/api/produccion/insumos", () =>
            {
                try
                {
                    using var c = Db.Conn();
                    var cmd = c.CreateCommand();
                    cmd.CommandText =
                        "SELECT CodigoInterno, Descripcion FROM ProductosProveedor " +
                        "WHERE Activo=1 ORDER BY Descripcion";
                    var lista = new List<object>();
                    try
                    {
                        using var r = cmd.ExecuteReader();
                        while (r.Read())
                            lista.Add(new { codigo = Db.S(r, 0), descripcion = Db.S(r, 1) });
                    }
                    catch { /* tabla opcional */ }
                    return Results.Ok(lista);
                }
                catch { return Results.Ok(new List<object>()); }
            }).RequireAuthorization();

            // ── Estado del digitalizador Python ───────────────────────────

            app.MapGet("/api/produccion/estado", async (IHttpClientFactory http) =>
            {
                try
                {
                    var cli = http.CreateClient("digitalizador");
                    var resp = await cli.GetAsync($"{DIG_URL}/api/estado");
                    var body = await resp.Content.ReadAsStringAsync();
                    return Results.Text(body, "application/json");
                }
                catch
                {
                    return Results.Json(new { ok = false, error = "Digitalizador offline" });
                }
            }).RequireAuthorization();

            // ── Proxy: extracción IA ──────────────────────────────────────
            // Se lee el IFormFile en C# y se re-arma el multipart hacia Python.
            // Pipear req.Body directamente falla porque ASP.NET Core ya consume
            // el stream antes de que llegue al handler.

            app.MapPost("/api/produccion/extraer", async (IFormFile imagen, IFormCollection form, IHttpClientFactory http) =>
            {
                const long MAX_BYTES = 20 * 1024 * 1024; // 20 MB
                if (imagen.Length > MAX_BYTES)
                    return Results.BadRequest(new { error = "Imagen demasiado grande (máx 20 MB)." });

                try
                {
                    var cli  = http.CreateClient("digitalizador");
                    var tipo = form["tipo"].FirstOrDefault() ?? "planilla";

                    // Leer la imagen a memoria y reenviar como multipart
                    using var ms = new MemoryStream();
                    await imagen.CopyToAsync(ms);

                    using var multipart = new MultipartFormDataContent();
                    var imgPart = new ByteArrayContent(ms.ToArray());
                    imgPart.Headers.ContentType =
                        System.Net.Http.Headers.MediaTypeHeaderValue.Parse(
                            imagen.ContentType ?? "image/jpeg");
                    multipart.Add(imgPart, "imagen", imagen.FileName ?? "foto.jpg");
                    multipart.Add(new StringContent(tipo), "tipo");

                    var resp = await cli.PostAsync($"{DIG_URL}/api/extraer", multipart);
                    var body = await resp.Content.ReadAsStringAsync();
                    return Results.Text(body, "application/json", statusCode: (int)resp.StatusCode);
                }
                catch (Exception ex)
                {
                    return Results.Json(
                        new { ok = false, error = $"Digitalizador no disponible: {ex.Message}" },
                        statusCode: 503);
                }
            }).RequireAuthorization().DisableAntiforgery().RequireRateLimiting("ia-extraccion");

            // ── Proxy: confirmar / guardar ────────────────────────────────

            app.MapPost("/api/produccion/confirmar", async (HttpRequest req, IHttpClientFactory http) =>
            {
                try
                {
                    var cli  = http.CreateClient("digitalizador");
                    var json = await new StreamReader(req.Body).ReadToEndAsync();
                    using var content = new StringContent(json,
                        System.Text.Encoding.UTF8, "application/json");
                    var resp = await cli.PostAsync($"{DIG_URL}/api/confirmar", content);
                    var body = await resp.Content.ReadAsStringAsync();
                    return Results.Text(body, "application/json", statusCode: (int)resp.StatusCode);
                }
                catch (Exception ex)
                {
                    return Results.Json(
                        new { ok = false, error = $"Digitalizador no disponible: {ex.Message}" },
                        statusCode: 503);
                }
            }).RequireAuthorization();
        }
    }
}
