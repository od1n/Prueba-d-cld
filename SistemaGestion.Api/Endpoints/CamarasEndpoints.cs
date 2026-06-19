using SistemaGestion.Api.Auth;
using System.Net;

namespace SistemaGestion.Api.Endpoints
{
    // ══════════════════════════════════════════════════════════════════════
    //  CamarasEndpoints — Cámaras RTSP visibles en el portal móvil
    //
    //  GET  /api/camaras               → lista cámaras según permisos
    //  GET  /api/camaras/{id}/snapshot  → proxy JPEG snapshot desde cámara
    //
    //  Permisos:
    //    Admin  → ve todas las cámaras activas
    //    Otros  → solo las asignadas en PermisosCamara
    // ══════════════════════════════════════════════════════════════════════
    public static class CamarasEndpoints
    {
        public static void MapCamarasEndpoints(this WebApplication app)
        {
            // ── Lista de cámaras visibles ────────────────────────────────
            app.MapGet("/api/camaras", (HttpContext ctx) =>
            {
                var idUsuario = TokenService.ObtenerIdUsuario(ctx.User);
                var esAdmin   = TokenService.EsAdmin(ctx.User);

                var camaras = Db.ObtenerCamarasVisibles(idUsuario, esAdmin);

                // Devolver sin datos sensibles (sin ClaveCifrada ni IpLocal)
                var resultado = camaras.Select(c => new
                {
                    c.Id,
                    c.Nombre,
                    c.Ubicacion,
                    c.Modelo,
                    c.Activa,
                    c.Orden,
                    // Solo admin ve IP y detalles técnicos
                    IpLocal    = esAdmin ? c.IpLocal    : null,
                    PuertoRtsp = esAdmin ? c.PuertoRtsp : (int?)null,
                    RutaStream = esAdmin ? c.RutaStream : null
                });

                return Results.Ok(resultado);
            }).RequireAuthorization();

            // ── Snapshot JPEG desde la cámara (proxy) ────────────────────
            // La API obtiene la imagen directamente de la cámara en la LAN
            // y la reenvía al cliente. El cliente nunca ve las credenciales.
            //
            // Ruta ISAPI estándar Hikvision/EZVIZ H8:
            //   GET /ISAPI/Streaming/channels/101/picture
            //   Authorization: Digest admin:<clave>
            app.MapGet("/api/camaras/{id:int}/snapshot", async (int id, HttpContext ctx) =>
            {
                var idUsuario = TokenService.ObtenerIdUsuario(ctx.User);
                var esAdmin   = TokenService.EsAdmin(ctx.User);

                // Verificar que el usuario puede ver esta cámara
                if (!esAdmin)
                {
                    var permisos = Db.ObtenerPermisosCamara(idUsuario);
                    if (!permisos.Contains(id))
                        return Results.Forbid();
                }

                var cam = Db.ObtenerCamaraPorId(id);
                if (cam is null)
                    return Results.NotFound(new { error = "Cámara no encontrada." });
                if (!cam.Activa)
                    return Results.BadRequest(new { error = "Cámara inactiva." });

                try
                {
                    var clave = cam.ObtenerClavePlano();

                    // HttpClientHandler con Digest auth (requerido por ISAPI Hikvision)
                    using var handler = new HttpClientHandler
                    {
                        Credentials = new NetworkCredential(
                            cam.UsuarioCamara ?? "admin", clave),
                        PreAuthenticate = false   // Digest requiere challenge previo
                    };
                    using var http = new HttpClient(handler)
                    {
                        Timeout = TimeSpan.FromSeconds(8)
                    };

                    // EZVIZ H8: /onvif/snapshot devuelve JPEG con Digest auth
                    // Fallbacks: ISAPI (Hikvision puro), luego Streaming
                    string[] rutas = [
                        $"http://{cam.IpLocal}/onvif/snapshot",
                        $"http://{cam.IpLocal}/ISAPI/Streaming/channels/101/picture",
                        $"http://{cam.IpLocal}/Streaming/channels/101/picture"
                    ];

                    HttpResponseMessage? resp = null;
                    foreach (var url in rutas)
                    {
                        resp = await http.GetAsync(url);
                        if (resp.IsSuccessStatusCode) break;
                    }

                    if (resp is null || !resp.IsSuccessStatusCode)
                        return Results.StatusCode(502);

                    var bytes = await resp.Content.ReadAsByteArrayAsync();
                    return Results.File(bytes, "image/jpeg");
                }
                catch (TaskCanceledException)
                {
                    return Results.StatusCode(504); // Gateway Timeout
                }
                catch (HttpRequestException ex)
                {
                    return Results.Problem(
                        detail: $"No se pudo conectar a la cámara: {ex.Message}",
                        statusCode: 502);
                }
            }).RequireAuthorization();

            // ── Conteo rápido (para badge en UI) ─────────────────────────
            app.MapGet("/api/camaras/count", (HttpContext ctx) =>
            {
                var idUsuario = TokenService.ObtenerIdUsuario(ctx.User);
                var esAdmin   = TokenService.EsAdmin(ctx.User);
                var count     = Db.ContarCamarasVisibles(idUsuario, esAdmin);
                return Results.Ok(new { count });
            }).RequireAuthorization();
        }
    }
}
