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
        private static readonly string DIG_TOKEN =
            Environment.GetEnvironmentVariable("SG_INTERNAL_TOKEN") ?? "";

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
                    if (!string.IsNullOrEmpty(DIG_TOKEN))
                        cli.DefaultRequestHeaders.Add("X-Internal-Token", DIG_TOKEN);
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
                    if (!string.IsNullOrEmpty(DIG_TOKEN))
                        cli.DefaultRequestHeaders.Add("X-Internal-Token", DIG_TOKEN);
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
                    if (!string.IsNullOrEmpty(DIG_TOKEN))
                        cli.DefaultRequestHeaders.Add("X-Internal-Token", DIG_TOKEN);
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

            // ── Registro manual de producción (sin digitalizador) ─────────

            app.MapPost("/api/produccion/registro", (HttpContext ctx, RegistroProduccionRequest req) =>
            {
                if (req.Moldes == null || req.Moldes.Count == 0)
                    return Results.BadRequest(new { error = "Debe incluir al menos un molde." });

                string fecha = string.IsNullOrWhiteSpace(req.Fecha)
                    ? DateTime.Today.ToString("yyyy-MM-dd") : req.Fecha;
                string usuario = ctx.User?.FindFirst("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name")?.Value ?? "portal";

                using var c = Db.Conn();
                using var tx = c.BeginTransaction();
                try
                {
                    int insertados = 0;
                    foreach (var m in req.Moldes)
                    {
                        if (string.IsNullOrWhiteSpace(m.CodigoMolde)) continue;
                        var cmd = c.CreateCommand(); cmd.Transaction = tx;
                        cmd.CommandText = @"INSERT INTO RegistroProduccionDia
                            (Fecha,CodigoMolde,Material,Operario,Turno,
                             CantPrimeraCalidad,CantSegundaCalidad,CantDefectuosas,Observaciones)
                            VALUES($f,$mol,$mat,$op,$turno,$p1,$p2,$def,$obs)";
                        cmd.Parameters.AddWithValue("$f", fecha);
                        cmd.Parameters.AddWithValue("$mol", m.CodigoMolde);
                        cmd.Parameters.AddWithValue("$mat", req.Material ?? "Virgen");
                        cmd.Parameters.AddWithValue("$op", req.Operario ?? usuario);
                        cmd.Parameters.AddWithValue("$turno", req.Turno ?? "");
                        cmd.Parameters.AddWithValue("$p1", m.Primera);
                        cmd.Parameters.AddWithValue("$p2", m.Segunda);
                        cmd.Parameters.AddWithValue("$def", m.Defectuosas);
                        cmd.Parameters.AddWithValue("$obs", req.Observaciones ?? "");
                        cmd.ExecuteNonQuery();
                        insertados++;
                    }

                    // Descontar insumos del kardex si se proporcionan materiales
                    if (req.Materiales != null)
                    {
                        foreach (var mat in req.Materiales)
                        {
                            if (string.IsNullOrWhiteSpace(mat.CodigoInsumo) || mat.Kg <= 0) continue;
                            // Update stock
                            var upd = c.CreateCommand(); upd.Transaction = tx;
                            upd.CommandText = @"UPDATE ProductosProveedor SET StockActual=MAX(0,StockActual-$kg)
                                WHERE CodigoInterno=$cod";
                            upd.Parameters.AddWithValue("$kg", (double)mat.Kg);
                            upd.Parameters.AddWithValue("$cod", mat.CodigoInsumo);
                            upd.ExecuteNonQuery();

                            // Kardex movement
                            var mv = c.CreateCommand(); mv.Transaction = tx;
                            mv.CommandText = @"INSERT INTO MovimientosMateriaPrima
                                (IdInsumo,FechaHora,TipoMovimiento,Concepto,Cantidad,Usuario)
                                SELECT Id,datetime('now','localtime'),'SALIDA',$conc,$kg,$usr
                                FROM ProductosProveedor WHERE CodigoInterno=$cod";
                            mv.Parameters.AddWithValue("$cod", mat.CodigoInsumo);
                            mv.Parameters.AddWithValue("$conc", "Producción " + fecha);
                            mv.Parameters.AddWithValue("$kg", (double)mat.Kg);
                            mv.Parameters.AddWithValue("$usr", usuario);
                            mv.ExecuteNonQuery();
                        }
                    }

                    tx.Commit();
                    return Results.Ok(new { ok = true, insertados, fecha });
                }
                catch (Exception ex)
                {
                    tx.Rollback();
                    return Results.Problem(ex.Message);
                }
            }).RequireAuthorization();

            // ── Listado de registros recientes ────────────────────────────

            app.MapGet("/api/produccion/registros", (string? desde, string? hasta) =>
            {
                string d = desde ?? DateTime.Today.AddDays(-7).ToString("yyyy-MM-dd");
                string h = hasta ?? DateTime.Today.ToString("yyyy-MM-dd");
                using var c = Db.Conn();
                var cmd = c.CreateCommand();
                cmd.CommandText = @"SELECT Id,Fecha,CodigoMolde,Material,Operario,Turno,
                    CantPrimeraCalidad,CantSegundaCalidad,CantDefectuosas,Observaciones
                    FROM RegistroProduccionDia WHERE Fecha BETWEEN $d AND $h
                    ORDER BY Fecha DESC, Id DESC LIMIT 100";
                cmd.Parameters.AddWithValue("$d", d);
                cmd.Parameters.AddWithValue("$h", h);
                var lista = new List<object>();
                using var r = cmd.ExecuteReader();
                while (r.Read())
                    lista.Add(new {
                        id=Db.I(r,0), fecha=Db.S(r,1), molde=Db.S(r,2), material=Db.S(r,3),
                        operario=Db.S(r,4), turno=Db.S(r,5),
                        primera=Db.I2(r,6), segunda=Db.I2(r,7), defectuosas=Db.I2(r,8),
                        obs=Db.S(r,9)
                    });
                return Results.Ok(lista);
            }).RequireAuthorization();

            // ── Planilla imprimible en blanco ─────────────────────────────

            app.MapGet("/api/produccion/planilla", async (HttpContext ctx) =>
            {
                // Load mold catalog for headers
                using var c = Db.Conn();
                var cmd = c.CreateCommand();
                cmd.CommandText = "SELECT CodigoMold, Descripcion FROM TbMoldes ORDER BY CodigoMold";
                var moldes = new List<(string cod, string desc)>();
                using var r = cmd.ExecuteReader();
                while (r.Read()) moldes.Add((Db.S(r,0), Db.S(r,1)));

                var hoy = DateTime.Today.ToString("dd/MM/yyyy");
                // Count molds to calculate available space
                var nMoldes = moldes.Count;
                // Max blank rows: ensure total fits on one page (max ~20 data rows)
                var blankRows = Math.Max(0, 10 - nMoldes);

                var html = "<!DOCTYPE html><html lang='es'><head><meta charset='UTF-8'>" +
                "<title>Planilla de Producción</title><style>" +
                "@page{size:letter landscape;margin:15mm 8mm 6mm 8mm}" +
                "*{box-sizing:border-box;margin:0;padding:0;font-family:Arial,sans-serif;font-size:8pt}" +
                "body{color:#111;width:100%;max-width:100%}" +
                ".hdr{text-align:center;margin-bottom:2mm}" +
                ".hdr h1{font-size:12pt;font-weight:900;margin-bottom:0}" +
                ".hdr .sub{font-size:7.5pt;color:#555}" +
                ".info{display:flex;gap:3mm;margin-bottom:2mm;font-size:8pt}" +
                ".info div{border-bottom:1px solid #333;flex:1;padding:0.5mm 0}" +
                ".info label{font-weight:700;font-size:7pt}" +
                "table{width:100%;border-collapse:collapse;margin-bottom:2mm}" +
                "th,td{border:1px solid #333;padding:1mm 2mm;text-align:center}" +
                "th{background:#eee;font-size:6.5pt;text-transform:uppercase}" +
                "td{height:5.5mm;font-size:7.5pt}" +
                ".noprint{margin-bottom:3mm;padding:2mm 8mm;background:#333;color:#fff;border:none;border-radius:3px;cursor:pointer;font-size:10pt}" +
                "@media print{.noprint{display:none!important}html,body{width:100%;height:100%;}}" +
                "</style></head><body>" +
                "<button class='noprint' onclick='window.print()'>Imprimir</button>" +
                "<div class='hdr'>" +
                "<h1>EL HATO PASCHER C.A.</h1>" +
                "<div class='sub'>PLANILLA DE CONTROL DE PRODUCCIÓN DIARIA</div>" +
                "</div>" +
                "<div class='info'>" +
                "<div><label>FECHA:</label> ___/___/______</div>" +
                "<div><label>OPERARIO:</label> _________________________</div>" +
                "<div><label>TURNO:</label> __________</div>" +
                "<div><label>MATERIAL:</label> ☐ Virgen ☐ Reciclado</div>" +
                "</div>" +
                "<table><thead><tr><th style='width:28mm'>MOLDE</th><th>1ª CALIDAD</th><th>2ª CALIDAD</th><th>RECICLADO</th><th style='color:red'>DEFECTUOSAS</th><th>TOTAL</th></tr></thead><tbody>";

                // One row per mold from catalog
                foreach (var (cod, desc) in moldes)
                    html += $"<tr><td style='text-align:left;font-size:7pt'>{System.Web.HttpUtility.HtmlEncode(cod)} - {System.Web.HttpUtility.HtmlEncode(desc)}</td><td></td><td></td><td></td><td></td><td></td></tr>";

                // Blank rows (dynamic based on mold count)
                for (int i = 0; i < blankRows; i++)
                    html += "<tr><td></td><td></td><td></td><td></td><td></td><td></td></tr>";

                html += "<tr style='font-weight:700;background:#f5f5f5'><td>TOTALES</td><td></td><td></td><td></td><td></td><td></td></tr>" +
                "</tbody></table>" +
                "<table><thead><tr><th style='width:28mm'>MATERIAL / INSUMO</th><th>KG UTILIZADOS</th><th>LOTE</th><th>OBSERVACIONES</th></tr></thead><tbody>";

                for (int i = 0; i < 4; i++)
                    html += "<tr><td></td><td></td><td></td><td></td></tr>";

                html += "</tbody></table>" +
                "<div class='info' style='margin-top:3mm'>" +
                "<div><label>OBSERVACIONES:</label> ___________________________________________</div>" +
                "</div>" +
                "<div class='info' style='margin-top:8mm'>" +
                "<div style='text-align:center;border-top:1px solid #333;border-bottom:none;padding-top:1mm'><label>FIRMA OPERARIO</label></div>" +
                "<div style='text-align:center;border-top:1px solid #333;border-bottom:none;padding-top:1mm'><label>FIRMA SUPERVISOR</label></div>" +
                "</div>" +
                "</body></html>";

                ctx.Response.ContentType = "text/html; charset=utf-8";
                await ctx.Response.WriteAsync(html);
            }).RequireAuthorization();
        }
    }

    public record MoldeRegistro(string CodigoMolde, int Primera, int Segunda, int Defectuosas);
    public record MaterialRegistro(string CodigoInsumo, decimal Kg);
    public record RegistroProduccionRequest(
        string? Fecha,
        string? Material,
        string? Operario,
        string? Turno,
        string? Observaciones,
        List<MoldeRegistro>? Moldes,
        List<MaterialRegistro>? Materiales);
}
