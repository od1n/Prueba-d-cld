using Microsoft.Data.Sqlite;
using SistemaGestion.Api.Auth;
using System.Security.Claims;
using System.Text;
using System.Text.Json;

namespace SistemaGestion.Api.Endpoints
{
    public static class EgresosEndpoints
    {
        private static readonly string DIG_URL =
            Environment.GetEnvironmentVariable("SG_DIG_URL") ?? "http://localhost:8080";
        private static readonly string DIG_TOKEN =
            Environment.GetEnvironmentVariable("SG_INTERNAL_TOKEN") ?? "";

        public static void MapEgresosEndpoints(this WebApplication app)
        {
            // ── 1. Buscar artículos en catálogo ─────────────────────────────
            // GET /api/articulos?q=texto&tipo=Materia+prima
            app.MapGet("/api/articulos", (string? q, string? tipo) =>
            {
                var resultados = new List<ArticuloCatDto>();
                using var c   = Db.Conn();
                using var cmd = c.CreateCommand();

                var sb = new StringBuilder(@"
                    SELECT Id, CodigoInterno, Descripcion, TipoInsumo,
                           PrecioUltCompra, StockActual
                    FROM ProductosProveedor
                    WHERE Activo = 1");

                if (!string.IsNullOrWhiteSpace(q))
                {
                    sb.Append(" AND (Descripcion LIKE @q OR CodigoInterno LIKE @q)");
                    cmd.Parameters.AddWithValue("@q", $"%{q.Trim()}%");
                }
                if (!string.IsNullOrWhiteSpace(tipo))
                {
                    sb.Append(" AND TipoInsumo = @tipo");
                    cmd.Parameters.AddWithValue("@tipo", tipo.Trim());
                }
                sb.Append(" ORDER BY Descripcion LIMIT 50");
                cmd.CommandText = sb.ToString();

                using var r = cmd.ExecuteReader();
                while (r.Read())
                {
                    resultados.Add(new ArticuloCatDto(
                        Id             : r.GetInt32(0),
                        CodigoInterno  : Db.S(r, 1),
                        Nombre         : Db.S(r, 2),
                        TipoInsumo     : Db.S(r, 3),
                        PrecioUnitario : Db.D(r, 4),
                        StockActual    : Db.D(r, 5),
                        EsInventariable: Db.S(r, 3) == "Materia prima"
                    ));
                }
                return Results.Ok(resultados);
            }).RequireAuthorization();

            // ── 2a. Estado del digitalizador + configuración de backend ─────
            // GET /api/egresos/config-vision
            app.MapGet("/api/egresos/config-vision", async (IHttpClientFactory http) =>
            {
                var backendDB = Db.GetConfig("vision_backend", "ollama");

                // Verificar si el servicio Python está accesible (online)
                // y si además el modelo backend está listo (modeloListo).
                // Son dos estados distintos:
                //   online=true  + modeloListo=false → servicio activo pero Ollama/API no responde
                //   online=false                      → el proceso Python no está corriendo
                bool online      = false;
                bool modeloListo = false;
                string nombre = "", modelo = "";
                try
                {
                    var cli  = http.CreateClient("digitalizador");
                    if (!string.IsNullOrEmpty(DIG_TOKEN))
                        cli.DefaultRequestHeaders.Add("X-Internal-Token", DIG_TOKEN);
                    var resp = await cli.GetAsync($"{DIG_URL}/api/estado");
                    if (resp.IsSuccessStatusCode)
                    {
                        online = true;   // el servicio Python está corriendo
                        var body = await resp.Content.ReadAsStringAsync();
                        using var doc = JsonDocument.Parse(body);
                        var r = doc.RootElement;
                        modeloListo = r.TryGetProperty("ok",     out var ok)  && ok.GetBoolean();
                        nombre      = r.TryGetProperty("nombre", out var nom) ? nom.GetString() ?? "" : "";
                        modelo      = r.TryGetProperty("modelo", out var mod) ? mod.GetString() ?? "" : "";
                    }
                }
                catch { /* digitalizador offline */ }

                return Results.Ok(new
                {
                    backend            = backendDB,
                    digitalizadorOnline= online,
                    modeloListo,
                    nombre,
                    modelo
                });
            }).RequireAuthorization();

            // ── 2b. Cambiar backend (solo Admin) ─────────────────────────────
            // PUT /api/egresos/config-vision   body: {"backend":"ollama"|"anthropic"}
            app.MapPut("/api/egresos/config-vision",
                (ConfigVisionRequest req, HttpContext ctx) =>
            {
                if (!TokenService.EsAdmin(ctx.User))
                    return Results.Forbid();

                var permitidos = new[] { "ollama", "anthropic", "groq" };
                var valor      = (req.Backend ?? "").ToLower().Trim();
                if (!permitidos.Contains(valor))
                    return Results.BadRequest(new { error = $"Backend no válido. Opciones: {string.Join(", ", permitidos)}." });

                Db.SetConfig("vision_backend", valor);
                return Results.Ok(new { backend = valor, mensaje = $"Backend cambiado a '{valor}'." });
            }).RequireAuthorization();

            // ── 2. Extraer documento con OCR IA ─────────────────────────────
            // Proxy hacia el digitalizador Python (:8080/api/extraer?tipo=factura).
            // El backend de visión (Ollama / Groq / Anthropic) se configura en
            // digitalizador/.env — sin cambios en este archivo.
            // POST /api/egresos/extraer-documento  (multipart/form-data: imagen)
            app.MapPost("/api/egresos/extraer-documento",
                async (IFormFile imagen, IHttpClientFactory http) =>
            {
                const long MAX_BYTES = 20 * 1024 * 1024;
                if (imagen is null || imagen.Length == 0)
                    return Results.BadRequest(new { error = "Campo 'imagen' requerido." });
                if (imagen.Length > MAX_BYTES)
                    return Results.BadRequest(new { error = "Imagen demasiado grande (máx 20 MB)." });

                try
                {
                    var cli            = http.CreateClient("digitalizador");
                    if (!string.IsNullOrEmpty(DIG_TOKEN))
                        cli.DefaultRequestHeaders.Add("X-Internal-Token", DIG_TOKEN);
                    var backendActual  = Db.GetConfig("vision_backend", "ollama");

                    // Leer imagen a memoria y reenviar como multipart al Python
                    using var ms = new MemoryStream();
                    await imagen.CopyToAsync(ms);
                    ms.Position = 0;

                    using var multipart = new MultipartFormDataContent();
                    var imgPart = new ByteArrayContent(ms.ToArray());
                    imgPart.Headers.ContentType =
                        System.Net.Http.Headers.MediaTypeHeaderValue.Parse(
                            imagen.ContentType ?? "image/jpeg");
                    multipart.Add(imgPart, "imagen", imagen.FileName ?? "foto.jpg");
                    multipart.Add(new StringContent("factura"),       "tipo");
                    multipart.Add(new StringContent(backendActual),   "backend");

                    var resp    = await cli.PostAsync($"{DIG_URL}/api/extraer", multipart);
                    var bodyStr = await resp.Content.ReadAsStringAsync();

                    if (!resp.IsSuccessStatusCode)
                        return Results.Problem($"Error del digitalizador ({(int)resp.StatusCode}).", statusCode: 502);

                    // El Python devuelve {"ok": true/false, "datos": {...}} o {"ok":false,"error":"..."}
                    using var doc = JsonDocument.Parse(bodyStr);
                    var root = doc.RootElement;

                    if (!root.TryGetProperty("ok", out var okProp) || !okProp.GetBoolean())
                    {
                        var errMsg = root.TryGetProperty("error", out var ep) ? ep.GetString() : "Error desconocido";
                        return Results.Problem(errMsg, statusCode: 502);
                    }

                    // Adaptar la respuesta al formato que espera el JS del portal
                    // Python: { fecha, numero_factura, proveedor, items:[{descripcion,cantidad,unidad,precio_unitario,total}], subtotal, iva, total, moneda }
                    // Portal: { fecha, nroDocumento, proveedor, items:[{descripcion,cantidad,precioUnitario,subtotal}], subtotal, impuesto, total, moneda }
                    var datos = root.GetProperty("datos");
                    var adaptado = AdaptarRespuestaFactura(datos);
                    return Results.Ok(adaptado);
                }
                catch (Exception ex)
                {
                    return Results.Problem($"Error al contactar el digitalizador: {ex.Message}", statusCode: 502);
                }

            }).AddEndpointFilter(RequireRole.Of(TokenService.PuedeEscribirEgresos)).RequireAuthorization()
              .RequireRateLimiting("ia-extraccion")
              .DisableAntiforgery();

            // ── 3. Crear egreso con líneas de detalle ────────────────────────
            // POST /api/egresos
            app.MapPost("/api/egresos",
                async (HttpContext ctx, EgresoNuevoRequest req) =>
            {
                if (req.Detalles is null || req.Detalles.Count == 0)
                    return Results.BadRequest(new { error = "Se requiere al menos un detalle." });
                if (req.MontoUSD <= 0 && req.MontoBs <= 0)
                    return Results.BadRequest(new { error = "El monto debe ser mayor a 0." });

                var usuario = ctx.User?.FindFirstValue(ClaimTypes.Name) ?? "Portal";

                using var c  = Db.Conn();
                using var tx = c.BeginTransaction();
                try
                {
                    // Determinar tipo de tasa y montos
                    var tipoTasa    = req.TasaBCV > 0 ? "BCV" : "USD_DIRECTO";
                    var montoOrig   = req.MontoUSD > 0 ? req.MontoUSD : req.MontoBs;
                    var monedaOrig  = req.MontoUSD > 0 ? "USD" : "BS";
                    var montoUSD    = req.MontoUSD > 0
                        ? req.MontoUSD
                        : (req.TasaBCV > 0 ? Math.Round(req.MontoBs / req.TasaBCV, 4) : 0);
                    var montoBs     = req.MontoBs > 0
                        ? req.MontoBs
                        : (req.TasaBCV > 0 ? Math.Round(req.MontoUSD * req.TasaBCV, 2) : 0);

                    // Construir concepto: "Compra a Proveedor" o usar descripción
                    var concepto = !string.IsNullOrWhiteSpace(req.Proveedor)
                        ? $"Compra a {req.Proveedor}"
                        : (req.Descripcion ?? "Compra");

                    // Observaciones = nroDocumento + notas adicionales
                    var observaciones = new List<string>();
                    if (!string.IsNullOrWhiteSpace(req.NroDocumento))
                        observaciones.Add($"Doc: {req.NroDocumento}");
                    if (!string.IsNullOrWhiteSpace(req.Notas))
                        observaciones.Add(req.Notas);
                    var obs = string.Join(" | ", observaciones);

                    // Insertar cabecera Gastos
                    using var cmdG = c.CreateCommand();
                    cmdG.Transaction = tx;
                    cmdG.CommandText = @"
                        INSERT INTO Gastos
                            (Fecha, IdCategoria, Concepto, Proveedor, IdInsumo, Cantidad,
                             MonedaOriginal, TipoTasa, TasaUsada, IdTasaCambio,
                             MontoOriginal, MontoUSD, MontoBs,
                             Estado, RutaComprobante, Usuario, FechaRegistro, Observaciones)
                        VALUES
                            (@fecha, @idCat, @conc, @prov, NULL, @cant,
                             @mon, @tt, @tasa, NULL,
                             @mo, @usd, @bs,
                             'Pagado', @ruta, @usr, datetime('now','localtime'), @obs);
                        SELECT last_insert_rowid();";

                    cmdG.Parameters.AddWithValue("@fecha", req.FechaGasto ?? DateTime.Today.ToString("yyyy-MM-dd"));
                    cmdG.Parameters.AddWithValue("@idCat", (object?)req.IdCategoria ?? DBNull.Value);
                    cmdG.Parameters.AddWithValue("@conc",  concepto);
                    cmdG.Parameters.AddWithValue("@prov",  req.Proveedor  ?? "");
                    cmdG.Parameters.AddWithValue("@cant",  (double)(req.CantidadTotal > 0
                                                               ? req.CantidadTotal
                                                               : req.Detalles.Sum(d => d.Cantidad)));
                    cmdG.Parameters.AddWithValue("@mon",   monedaOrig);
                    cmdG.Parameters.AddWithValue("@tt",    tipoTasa);
                    cmdG.Parameters.AddWithValue("@tasa",  (double)req.TasaBCV);
                    cmdG.Parameters.AddWithValue("@mo",    (double)montoOrig);
                    cmdG.Parameters.AddWithValue("@usd",   (double)montoUSD);
                    cmdG.Parameters.AddWithValue("@bs",    (double)montoBs);
                    cmdG.Parameters.AddWithValue("@ruta",  req.RutaComprobante ?? "");
                    cmdG.Parameters.AddWithValue("@usr",   usuario);
                    cmdG.Parameters.AddWithValue("@obs",   obs);

                    var idGasto = Convert.ToInt32(cmdG.ExecuteScalar());

                    // Insertar líneas de detalle
                    foreach (var det in req.Detalles)
                    {
                        var esInv = det.TipoInsumo == "Materia prima" ? 1 : 0;
                        using var cmdD = c.CreateCommand();
                        cmdD.Transaction = tx;
                        cmdD.CommandText = @"
                            INSERT INTO DetallesGasto
                                (IdGasto, Descripcion, IdArticulo, CodigoInterno,
                                 TipoInsumo, EsInventariable, Cantidad, PrecioUnitario, Subtotal)
                            VALUES
                                (@idG, @desc, @idArt, @cod,
                                 @tipo, @esInv, @cant, @pu, @sub);";
                        cmdD.Parameters.AddWithValue("@idG",   idGasto);
                        cmdD.Parameters.AddWithValue("@desc",  det.Descripcion ?? "");
                        cmdD.Parameters.AddWithValue("@idArt", (object?)det.IdArticulo    ?? DBNull.Value);
                        cmdD.Parameters.AddWithValue("@cod",   (object?)det.CodigoInterno ?? DBNull.Value);
                        cmdD.Parameters.AddWithValue("@tipo",  (object?)det.TipoInsumo    ?? DBNull.Value);
                        cmdD.Parameters.AddWithValue("@esInv", esInv);
                        cmdD.Parameters.AddWithValue("@cant",  (double)det.Cantidad);
                        cmdD.Parameters.AddWithValue("@pu",    (double)det.PrecioUnitario);
                        cmdD.Parameters.AddWithValue("@sub",   (double)det.Subtotal);

                        // Materia Prima: actualizar stock + CPP en ProductosProveedor
                        if (esInv == 1 && det.IdArticulo.HasValue && det.Cantidad > 0)
                        {
                            ActualizarInventarioMP(
                                c, tx,
                                det.IdArticulo.Value,
                                det.Cantidad,
                                det.PrecioUnitario,
                                idGasto,
                                usuario
                            );
                        }
                    }

                    tx.Commit();
                    return Results.Ok(new { id = idGasto, mensaje = "Egreso registrado correctamente." });
                }
                catch (Exception ex)
                {
                    tx.Rollback();
                    return Results.Problem($"Error al guardar: {ex.Message}", statusCode: 500);
                }
            }).AddEndpointFilter(RequireRole.Of(TokenService.PuedeEscribirEgresos)).RequireAuthorization();

            // ── 4. Listar egresos ────────────────────────────────────────────
            // GET /api/egresos?desde=2025-01-01&hasta=2025-12-31&categoriaId=3
            app.MapGet("/api/egresos", (string? desde, string? hasta, int? categoriaId, string? q) =>
            {
                var lista = new List<EgresoResDto>();
                using var c   = Db.Conn();
                using var cmd = c.CreateCommand();

                var sb = new StringBuilder(@"
                    SELECT g.Id, g.Fecha, g.Concepto, g.Proveedor, g.Observaciones,
                           g.MontoUSD, g.MontoBs, g.Estado,
                           COALESCE(cg.Nombre, '') AS Categoria
                    FROM Gastos g
                    LEFT JOIN CategoriasGasto cg ON cg.Id = g.IdCategoria
                    WHERE 1=1");

                if (!string.IsNullOrWhiteSpace(desde))
                {
                    sb.Append(" AND g.Fecha >= @desde");
                    cmd.Parameters.AddWithValue("@desde", desde!.Trim());
                }
                if (!string.IsNullOrWhiteSpace(hasta))
                {
                    sb.Append(" AND g.Fecha <= @hasta");
                    cmd.Parameters.AddWithValue("@hasta", hasta!.Trim());
                }
                if (categoriaId.HasValue)
                {
                    sb.Append(" AND g.IdCategoria = @cat");
                    cmd.Parameters.AddWithValue("@cat", categoriaId.Value);
                }
                if (!string.IsNullOrWhiteSpace(q))
                {
                    sb.Append(" AND (g.Concepto LIKE @q OR g.Proveedor LIKE @q OR g.Observaciones LIKE @q)");
                    cmd.Parameters.AddWithValue("@q", $"%{q.Trim()}%");
                }
                sb.Append(" ORDER BY g.Fecha DESC, g.Id DESC LIMIT 200");
                cmd.CommandText = sb.ToString();

                using var r = cmd.ExecuteReader();
                while (r.Read())
                {
                    // Extraer NroDocumento del campo Observaciones (prefijo "Doc: ")
                    var obsRaw    = Db.S(r, 4);
                    var nroDoc    = "";
                    if (obsRaw.StartsWith("Doc: "))
                    {
                        var pipIdx = obsRaw.IndexOf(" | ");
                        nroDoc = pipIdx >= 0 ? obsRaw[5..pipIdx] : obsRaw[5..];
                    }
                    lista.Add(new EgresoResDto(
                        Id          : r.GetInt32(0),
                        Fecha       : Db.S(r, 1),
                        Concepto    : Db.S(r, 2),
                        Proveedor   : Db.S(r, 3),
                        NroDocumento: nroDoc,
                        MontoUSD    : Db.D(r, 5),
                        MontoBs     : Db.D(r, 6),
                        Estado      : Db.S(r, 7),
                        Categoria   : Db.S(r, 8)
                    ));
                }
                return Results.Ok(lista);
            }).RequireAuthorization();

            // ── 5. Detalle de un egreso ──────────────────────────────────────
            // GET /api/egresos/{id}
            app.MapGet("/api/egresos/{id:int}", (int id) =>
            {
                using var c = Db.Conn();
                EgresoDetDto? cabecera = null;

                using (var cmd = c.CreateCommand())
                {
                    cmd.CommandText = @"
                        SELECT g.Id, g.Fecha, g.Concepto, g.Proveedor, g.Observaciones,
                               g.MontoUSD, g.MontoBs, g.TasaUsada, g.Estado,
                               COALESCE(cg.Nombre, '')
                        FROM Gastos g
                        LEFT JOIN CategoriasGasto cg ON cg.Id = g.IdCategoria
                        WHERE g.Id = @id;";
                    cmd.Parameters.AddWithValue("@id", id);
                    using var r = cmd.ExecuteReader();
                    if (r.Read())
                    {
                        var obsRaw = Db.S(r, 4);
                        var nroDoc = "";
                        var notas  = obsRaw;
                        if (obsRaw.StartsWith("Doc: "))
                        {
                            var pipIdx = obsRaw.IndexOf(" | ");
                            if (pipIdx >= 0)
                            {
                                nroDoc = obsRaw[5..pipIdx];
                                notas  = obsRaw[(pipIdx + 3)..];
                            }
                            else
                            {
                                nroDoc = obsRaw[5..];
                                notas  = "";
                            }
                        }
                        cabecera = new EgresoDetDto(
                            Id           : r.GetInt32(0),
                            Fecha        : Db.S(r, 1),
                            Concepto     : Db.S(r, 2),
                            Proveedor    : Db.S(r, 3),
                            NroDocumento : nroDoc,
                            MontoUSD     : Db.D(r, 5),
                            MontoBs      : Db.D(r, 6),
                            TasaBCV      : Db.D(r, 7),
                            Estado       : Db.S(r, 8),
                            Notas        : notas,
                            Categoria    : Db.S(r, 9),
                            Detalles     : new List<DetalleGastoDto>()
                        );
                    }
                }
                if (cabecera is null)
                    return Results.NotFound(new { error = "Egreso no encontrado." });

                using (var cmd2 = c.CreateCommand())
                {
                    cmd2.CommandText = @"
                        SELECT Id, Descripcion, IdArticulo, CodigoInterno,
                               TipoInsumo, EsInventariable, Cantidad, PrecioUnitario, Subtotal
                        FROM DetallesGasto WHERE IdGasto = @id ORDER BY Id;";
                    cmd2.Parameters.AddWithValue("@id", id);
                    using var r2 = cmd2.ExecuteReader();
                    while (r2.Read())
                    {
                        cabecera.Detalles.Add(new DetalleGastoDto(
                            Id             : r2.GetInt32(0),
                            Descripcion    : Db.S(r2, 1),
                            IdArticulo     : r2.IsDBNull(2) ? null : r2.GetInt32(2),
                            CodigoInterno  : Db.S(r2, 3),
                            TipoInsumo     : Db.S(r2, 4),
                            EsInventariable: Db.I(r2, 5) == 1,
                            Cantidad       : Db.D(r2, 6),
                            PrecioUnitario : Db.D(r2, 7),
                            Subtotal       : Db.D(r2, 8)
                        ));
                    }
                }
                return Results.Ok(cabecera);
            }).RequireAuthorization();

            // ── 6. Análisis de egresos por categoría ────────────────────────
            // GET /api/egresos/analisis?desde=YYYY-MM-DD&hasta=YYYY-MM-DD
            app.MapGet("/api/egresos/analisis", (string? desde, string? hasta) =>
            {
                var resultado = new List<object>();
                using var c   = Db.Conn();
                using var cmd = c.CreateCommand();

                var sb = new StringBuilder(@"
                    SELECT COALESCE(cg.Nombre,'Sin categoría') AS Cat,
                           cg.Color,
                           COUNT(*) AS Cant,
                           COALESCE(SUM(g.MontoUSD),0) AS TotalUSD,
                           COALESCE(SUM(g.MontoBs),0)  AS TotalBs
                    FROM Gastos g
                    LEFT JOIN CategoriasGasto cg ON cg.Id = g.IdCategoria
                    WHERE 1=1");

                if (!string.IsNullOrWhiteSpace(desde))
                { sb.Append(" AND g.Fecha >= @d"); cmd.Parameters.AddWithValue("@d", desde!.Trim()); }
                if (!string.IsNullOrWhiteSpace(hasta))
                { sb.Append(" AND g.Fecha <= @h"); cmd.Parameters.AddWithValue("@h", hasta!.Trim()); }

                sb.Append(" GROUP BY cg.Id, cg.Nombre ORDER BY TotalUSD DESC");
                cmd.CommandText = sb.ToString();

                using var r = cmd.ExecuteReader();
                while (r.Read())
                    resultado.Add(new {
                        categoria = Db.S(r, 0),
                        color     = Db.S(r, 1),
                        cantidad  = r.GetInt32(2),
                        totalUSD  = Db.D(r, 3),
                        totalBs   = Db.D(r, 4)
                    });

                return Results.Ok(resultado);
            }).RequireAuthorization();

            // ── 7. Categorías de gasto ───────────────────────────────────────
            // GET /api/categorias-gasto
            app.MapGet("/api/categorias-gasto", () =>
            {
                var lista = new List<CatGastoDto>();
                using var c   = Db.Conn();
                using var cmd = c.CreateCommand();
                cmd.CommandText = @"
                    SELECT Id, Nombre, EsMateriaPrima
                    FROM CategoriasGasto
                    WHERE Activa = 1
                    ORDER BY Orden, Nombre;";
                using var r = cmd.ExecuteReader();
                while (r.Read())
                    lista.Add(new CatGastoDto(r.GetInt32(0), Db.S(r, 1), Db.I(r, 2) == 1));
                return Results.Ok(lista);
            }).RequireAuthorization();
        }

        // ── Adaptar respuesta del digitalizador Python al formato del portal ────
        // Python:  { fecha:"DD/MM/YYYY", numero_factura, proveedor, rif_proveedor,
        //            items:[{descripcion, cantidad, unidad, precio_unitario, total}],
        //            subtotal, iva, total, moneda, observaciones }
        // Portal:  { fecha:"YYYY-MM-DD", nroDocumento, proveedor,
        //            items:[{descripcion, cantidad, precioUnitario, subtotal}],
        //            subtotal, impuesto, total, moneda }
        private static object AdaptarRespuestaFactura(JsonElement datos)
        {
            // Fecha: convertir DD/MM/YYYY → YYYY-MM-DD si es posible
            var fechaRaw  = datos.TryGetProperty("fecha", out var fp) ? fp.GetString() ?? "" : "";
            var fechaISO  = "";
            if (fechaRaw.Length == 10 && fechaRaw[2] == '/')
            {
                // DD/MM/YYYY → YYYY-MM-DD
                fechaISO = $"{fechaRaw[6..]}-{fechaRaw[3..5]}-{fechaRaw[0..2]}";
            }
            else fechaISO = fechaRaw;

            var items = new List<object>();
            if (datos.TryGetProperty("items", out var itemsEl) && itemsEl.ValueKind == JsonValueKind.Array)
            {
                foreach (var it in itemsEl.EnumerateArray())
                {
                    var desc = it.TryGetProperty("descripcion", out var d) ? d.GetString() ?? "" : "";
                    var cant = it.TryGetProperty("cantidad",    out var c) ? c.GetDouble() : 1.0;
                    var pu   = it.TryGetProperty("precio_unitario", out var pu2) ? pu2.GetDouble()
                             : it.TryGetProperty("precioUnitario",  out var pu3) ? pu3.GetDouble() : 0.0;
                    var sub  = it.TryGetProperty("total",   out var t2) ? t2.GetDouble()
                             : it.TryGetProperty("subtotal", out var s2) ? s2.GetDouble()
                             : Math.Round(cant * pu, 2);
                    items.Add(new { descripcion = desc, cantidad = cant, precioUnitario = pu, subtotal = sub });
                }
            }

            // "encontrado": true/false — el modelo lo indica cuando no reconoce la imagen
            var encontrado = true;
            if (datos.TryGetProperty("encontrado", out var encProp) && encProp.ValueKind == JsonValueKind.False)
                encontrado = false;

            return new
            {
                encontrado,
                fecha        = fechaISO,
                nroDocumento = datos.TryGetProperty("numero_factura", out var nf) ? nf.GetString() ?? "" : "",
                proveedor    = datos.TryGetProperty("proveedor",      out var pr) ? pr.GetString() ?? "" : "",
                moneda       = datos.TryGetProperty("moneda",         out var mo) ? mo.GetString() ?? "USD" : "USD",
                subtotal     = datos.TryGetProperty("subtotal",       out var st) ? st.GetDouble() : 0.0,
                impuesto     = datos.TryGetProperty("iva",            out var iv) ? iv.GetDouble() : 0.0,
                total        = datos.TryGetProperty("total",          out var to) ? to.GetDouble() : 0.0,
                items
            };
        }

        // ── Actualizar stock + CPP (Materia Prima) ───────────────────────────
        private static void ActualizarInventarioMP(
            SqliteConnection c, SqliteTransaction tx,
            int idArticulo, decimal cantidad, decimal precioUnitario,
            int idGasto, string usuario)
        {
            // 1. Leer estado actual del insumo
            string codigoInterno = "";
            decimal stockAnt = 0, costoAnt = 0;
            using (var cmdL = c.CreateCommand())
            {
                cmdL.Transaction = tx;
                cmdL.CommandText = @"
                    SELECT CodigoInterno, COALESCE(StockActual, 0), COALESCE(CostoPromedio, 0)
                    FROM ProductosProveedor WHERE Id = @id;";
                cmdL.Parameters.AddWithValue("@id", idArticulo);
                using var rL = cmdL.ExecuteReader();
                if (rL.Read())
                {
                    codigoInterno = Db.S(rL, 0);
                    stockAnt      = (decimal)rL.GetDouble(1);
                    costoAnt      = (decimal)rL.GetDouble(2);
                }
            }
            if (string.IsNullOrEmpty(codigoInterno)) return;  // insumo no encontrado

            // 2. Calcular nuevo stock y nuevo CPP
            var nuevoStock = stockAnt + cantidad;
            var nuevoCosto = nuevoStock > 0
                ? Math.Round((stockAnt * costoAnt + cantidad * precioUnitario) / nuevoStock, 4)
                : precioUnitario;

            // 3. Actualizar ProductosProveedor
            using (var cmdU = c.CreateCommand())
            {
                cmdU.Transaction = tx;
                cmdU.CommandText = @"
                    UPDATE ProductosProveedor
                    SET StockActual = @ns, CostoPromedio = @ncp, PrecioUltCompra = @pu
                    WHERE Id = @id;";
                cmdU.Parameters.AddWithValue("@ns",  (double)nuevoStock);
                cmdU.Parameters.AddWithValue("@ncp", (double)nuevoCosto);
                cmdU.Parameters.AddWithValue("@pu",  (double)precioUnitario);
                cmdU.Parameters.AddWithValue("@id",  idArticulo);
                cmdU.ExecuteNonQuery();
            }

            // 4. Registrar movimiento en MovimientosInventario
            using var cmdM = c.CreateCommand();
            cmdM.Transaction = tx;
            cmdM.CommandText = @"
                INSERT INTO MovimientosInventario
                    (FechaHora, CodigoProducto, TipoMovimiento, Concepto,
                     Cantidad, SaldoAnterior, NuevoSaldo,
                     Referencia, Usuario, TipoKardex, CostoUnitario)
                VALUES
                    (datetime('now','localtime'), @cod, 'ENTRADA', @conc,
                     @cant, @sa, @ns,
                     @ref, @usr, 'MATERIA_PRIMA', @cu);";
            cmdM.Parameters.AddWithValue("@cod",  codigoInterno);
            cmdM.Parameters.AddWithValue("@conc", $"Compra — Gasto #{idGasto}");
            cmdM.Parameters.AddWithValue("@cant", (double)cantidad);
            cmdM.Parameters.AddWithValue("@sa",   (double)stockAnt);
            cmdM.Parameters.AddWithValue("@ns",   (double)nuevoStock);
            cmdM.Parameters.AddWithValue("@ref",  $"Gasto#{idGasto}");
            cmdM.Parameters.AddWithValue("@usr",  usuario);
            cmdM.Parameters.AddWithValue("@cu",   (double)precioUnitario);
            cmdM.ExecuteNonQuery();
        }
    }

    // ── DTOs ─────────────────────────────────────────────────────────────────

    record ArticuloCatDto(
        int     Id,
        string  CodigoInterno,
        string  Nombre,
        string  TipoInsumo,
        decimal PrecioUnitario,
        decimal StockActual,
        bool    EsInventariable
    );

    record CatGastoDto(int Id, string Nombre, bool EsMateriaPrima);

    record EgresoResDto(
        int     Id,
        string  Fecha,
        string  Concepto,
        string  Proveedor,
        string  NroDocumento,
        decimal MontoUSD,
        decimal MontoBs,
        string  Estado,
        string  Categoria
    );

    record EgresoDetDto(
        int                   Id,
        string                Fecha,
        string                Concepto,
        string                Proveedor,
        string                NroDocumento,
        decimal               MontoUSD,
        decimal               MontoBs,
        decimal               TasaBCV,
        string                Estado,
        string                Notas,
        string                Categoria,
        List<DetalleGastoDto> Detalles
    );

    record DetalleGastoDto(
        int     Id,
        string  Descripcion,
        int?    IdArticulo,
        string  CodigoInterno,
        string  TipoInsumo,
        bool    EsInventariable,
        decimal Cantidad,
        decimal PrecioUnitario,
        decimal Subtotal
    );

    record EgresoNuevoRequest(
        int?    IdCategoria,
        string? Descripcion,
        string? Proveedor,
        string? NroDocumento,
        string? FechaGasto,
        decimal CantidadTotal,
        decimal MontoUSD,
        decimal MontoBs,
        decimal TasaBCV,
        string? RutaComprobante,
        string? Notas,
        List<DetalleLineaRequest> Detalles
    );

    record DetalleLineaRequest(
        string?  Descripcion,
        int?     IdArticulo,
        string?  CodigoInterno,
        string?  TipoInsumo,
        decimal  Cantidad,
        decimal  PrecioUnitario,
        decimal  Subtotal
    );

    record ConfigVisionRequest(string? Backend);
}
