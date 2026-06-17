using SistemaGestion.Api.Auth;

namespace SistemaGestion.Api.Endpoints
{
    // ══════════════════════════════════════════════════════════════════════
    //  ConfigEndpoints — Parámetros legales y fiscales del sistema
    //  GET  /api/config/legal   → devuelve todos los parámetros legales
    //  PUT  /api/config/legal   → actualiza parámetros (solo Admin)
    // ══════════════════════════════════════════════════════════════════════
    public static class ConfigEndpoints
    {
        // Claves reconocidas (orden de presentación en la UI)
        private static readonly string[] ClavesLegales =
        [
            "IVA_Porcentaje",
            "IVSS_Empleado",
            "RPVH_Empleado",
            "IVSS_Patronal",
            "RPVH_Patronal",
            "INCES_Patronal",
            "UT_Bs",
            "SalarioMinimo_Bs",
            "DiasCredito_Default"
        ];

        public static void MapConfigEndpoints(this WebApplication app)
        {
            // GET /api/config/legal — cualquier usuario autenticado puede leer
            app.MapGet("/api/config/legal", () =>
            {
                var result = new Dictionary<string, string>();
                foreach (var clave in ClavesLegales)
                    result[clave] = Db.ObtenerConfig(clave, "");
                return Results.Ok(result);
            }).RequireAuthorization();

            // PUT /api/config/legal — solo Admin
            app.MapPut("/api/config/legal",
                (Dictionary<string, string> cambios, HttpContext ctx) =>
            {
                if (!TokenService.EsAdmin(ctx.User))
                    return Results.Forbid();

                if (cambios is null || cambios.Count == 0)
                    return Results.BadRequest(new { error = "No se recibieron parámetros." });

                var guardadas = new List<string>();
                foreach (var (clave, valor) in cambios)
                {
                    // Solo actualizar claves conocidas (whitelist)
                    if (!Array.Exists(ClavesLegales, k => k == clave))
                        continue;
                    Db.GuardarConfig(clave, valor ?? "");
                    guardadas.Add(clave);
                }

                return Results.Ok(new
                {
                    mensaje   = $"{guardadas.Count} parámetro(s) actualizado(s).",
                    guardadas
                });
            }).RequireAuthorization();
        }
    }
}
