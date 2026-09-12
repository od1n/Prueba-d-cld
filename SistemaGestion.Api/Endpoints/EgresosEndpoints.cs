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
                bool precioPendiente = req.PrecioPendiente;
                if (req.Detalles is null || req.Detalles.Count == 0)
                    return Results.BadRequest(new { error = "Se requiere al menos un detalle." });
                if (!precioPendiente && req.MontoUSD <= 0 && req.MontoBs <= 0)
                    return Results.BadRequest(new { error = "El monto debe ser mayor a 0." });

                var usuario = ctx.User?.FindFirstValue(ClaimTypes.Name) ?? "Portal";

                using var c  = Db.Conn();
                using var tx = c.BeginTransaction();
                try
                {
                    // Determinar tipo de tasa y montos. Con precio pendiente los
                    // montos son 0 (la nota de entrega llegó sin precios).
                    string tipoTasa, monedaOrig;
                    decimal montoOrig, montoUSD, montoBs;
                    if (precioPendiente)
                    {
                        tipoTasa = "USD_DIRECTO"; monedaOrig = "USD";
                        montoOrig = 0; montoUSD = 0; montoBs = 0;
                    }
                    else
                    {
                        tipoTasa   = req.TasaBCV > 0 ? "BCV" : "USD_DIRECTO";
                        montoOrig  = req.MontoUSD > 0 ? req.MontoUSD : req.MontoBs;
                        monedaOrig = req.MontoUSD > 0 ? "USD" : "BS";
                        montoUSD   = req.MontoUSD > 0
                            ? req.MontoUSD
                            : (req.TasaBCV > 0 ? Math.Round(req.MontoBs / req.TasaBCV, 4) : 0);
                        montoBs    = req.MontoBs > 0
                            ? req.MontoBs
                            : (req.TasaBCV > 0 ? Math.Round(req.MontoUSD * req.TasaBCV, 2) : 0);
                    }

                    // Abono / estado. Por defecto se asume pago completo. Si el
                    // egreso es a crédito (ACredito) se toma el abono indicado y el
                    // saldo genera una Cuenta por Pagar. Con precio pendiente no hay
                    // pago todavía y no se crea CxP (monto desconocido).
                    decimal pagadoUSD;
                    string  estado;
                    if (precioPendiente)
                    {
                        pagadoUSD = 0; estado = "Pendiente";
                    }
                    else if (req.ACredito)
                    {
                        pagadoUSD = req.PagadoUSD < 0 ? 0 : req.PagadoUSD;
                        if (pagadoUSD >= montoUSD - 0.01m) { pagadoUSD = montoUSD; estado = "Pagado"; }
                        else estado = "Pendiente";
                    }
                    else
                    {
                        pagadoUSD = montoUSD; estado = "Pagado";
                    }

                    // Fechas
                    var fechaStr = req.FechaGasto ?? DateTime.Today.ToString("yyyy-MM-dd");
                    DateTime fechaGasto = DateTime.TryParse(fechaStr, out var fg) ? fg : DateTime.Today;
                    string? venceStr = null;
                    if (estado == "Pendiente" && !precioPendiente)
                        venceStr = (req.FechaVencimiento ?? fechaGasto.AddDays(30).ToString("yyyy-MM-dd"));
                    else if (precioPendiente)
                        venceStr = (req.FechaVencimiento ?? fechaGasto.AddDays(30).ToString("yyyy-MM-dd"));
                    string? fechaPagoStr = estado == "Pagado" ? DateTime.Today.ToString("yyyy-MM-dd")
                                          : (pagadoUSD > 0 ? DateTime.Today.ToString("yyyy-MM-dd") : null);

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
                            (Fecha, IdCategoria, Concepto, Proveedor, CodigoProveedor, IdInsumo, Cantidad,
                             MonedaOriginal, TipoTasa, TasaUsada, IdTasaCambio,
                             MontoOriginal, MontoUSD, MontoBs, PagadoUSD, PrecioPendiente,
                             Estado, FechaVencimiento, FechaPago, RutaComprobante,
                             Usuario, FechaRegistro, Observaciones)
                        VALUES
                            (@fecha, @idCat, @conc, @prov, @cprov, NULL, @cant,
                             @mon, @tt, @tasa, NULL,
                             @mo, @usd, @bs, @pag, @pp,
                             @estado, @fv, @fp, @ruta,
                             @usr, datetime('now','localtime'), @obs);
                        SELECT last_insert_rowid();";

                    cmdG.Parameters.AddWithValue("@fecha", fechaStr);
                    cmdG.Parameters.AddWithValue("@idCat", (object?)req.IdCategoria ?? DBNull.Value);
                    cmdG.Parameters.AddWithValue("@conc",  concepto);
                    cmdG.Parameters.AddWithValue("@prov",  req.Proveedor  ?? "");
                    cmdG.Parameters.AddWithValue("@cprov", req.CodigoProveedor ?? "");
                    cmdG.Parameters.AddWithValue("@cant",  (double)(req.CantidadTotal > 0
                                                               ? req.CantidadTotal
                                                               : req.Detalles.Sum(d => d.Cantidad)));
                    cmdG.Parameters.AddWithValue("@mon",   monedaOrig);
                    cmdG.Parameters.AddWithValue("@tt",    tipoTasa);
                    cmdG.Parameters.AddWithValue("@tasa",  (double)req.TasaBCV);
                    cmdG.Parameters.AddWithValue("@mo",    (double)montoOrig);
                    cmdG.Parameters.AddWithValue("@usd",   (double)montoUSD);
                    cmdG.Parameters.AddWithValue("@bs",    (double)montoBs);
                    cmdG.Parameters.AddWithValue("@pag",   (double)pagadoUSD);
                    cmdG.Parameters.AddWithValue("@pp",    precioPendiente ? 1 : 0);
                    cmdG.Parameters.AddWithValue("@estado",estado);
                    cmdG.Parameters.AddWithValue("@fv",    (object?)venceStr ?? DBNull.Value);
                    cmdG.Parameters.AddWithValue("@fp",    (object?)fechaPagoStr ?? DBNull.Value);
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
                        cmdD.ExecuteNonQuery();

                        // Materia Prima: actualizar stock + costo promedio en ProductosProveedor.
                        // Con precio pendiente entra a costo PROVISIONAL (promedio actual).
                        if (esInv == 1 && det.IdArticulo.HasValue && det.Cantidad > 0)
                        {
                            ActualizarInventarioMP(
                                c, tx,
                                det.IdArticulo.Value,
                                det.Cantidad,
                                det.PrecioUnitario,
                                idGasto,
                                usuario,
                                precioPendiente
                            );
                        }
                    }

                    // Egreso a crédito → Cuenta por Pagar por el saldo (salvo precio pendiente)
                    if (!precioPendiente)
                        SincronizarCxPDesdeEgreso(c, tx, idGasto, req.Proveedor ?? "",
                            req.CodigoProveedor ?? "", concepto, "Egreso #" + idGasto,
                            montoUSD, pagadoUSD, fechaStr, venceStr ?? fechaGasto.AddDays(30).ToString("yyyy-MM-dd"));

                    tx.Commit();
                    return Results.Ok(new {
                        id = idGasto,
                        estado,
                        precioPendiente,
                        mensaje = precioPendiente
                            ? "Egreso registrado con precio pendiente. Complételo cuando llegue la factura."
                            : "Egreso registrado correctamente."
                    });
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

            // ── 8. Egresos con precio pendiente (nota de entrega sin montos) ─
            // GET /api/egresos/precio-pendiente
            app.MapGet("/api/egresos/precio-pendiente", () =>
            {
                var lista = new List<object>();
                using var c   = Db.Conn();
                using var cmd = c.CreateCommand();
                cmd.CommandText = @"
                    SELECT g.Id, g.Fecha, g.Concepto, COALESCE(g.Proveedor,''),
                           COALESCE(g.Cantidad,0), COALESCE(cg.Nombre,'')
                    FROM Gastos g
                    LEFT JOIN CategoriasGasto cg ON cg.Id = g.IdCategoria
                    WHERE COALESCE(g.PrecioPendiente,0) = 1 AND g.Estado != 'Anulado'
                    ORDER BY g.Fecha ASC, g.Id ASC";
                using var r = cmd.ExecuteReader();
                while (r.Read())
                    lista.Add(new {
                        id        = r.GetInt32(0),
                        fecha     = Db.S(r, 1),
                        concepto  = Db.S(r, 2),
                        proveedor = Db.S(r, 3),
                        cantidad  = Db.D(r, 4),
                        categoria = Db.S(r, 5)
                    });
                return Results.Ok(lista);
            }).RequireAuthorization();

            // ── 9. Completar precio de una nota de entrega ──────────────────
            // PUT /api/egresos/{id}/completar-precio
            app.MapPut("/api/egresos/{id:int}/completar-precio",
                (int id, CompletarPrecioRequest req) =>
            {
                if (req.MontoUSD <= 0 && req.MontoBs <= 0)
                    return Results.BadRequest(new { error = "El monto debe ser mayor a 0." });

                using var c  = Db.Conn();
                using var tx = c.BeginTransaction();
                try
                {
                    // Leer el egreso y validar que tenga precio pendiente.
                    string proveedor = "", codProv = "", concepto = "", fechaStr = DateTime.Today.ToString("yyyy-MM-dd");
                    bool esPendiente = false;
                    using (var q = c.CreateCommand())
                    {
                        q.Transaction = tx;
                        q.CommandText = @"SELECT COALESCE(Proveedor,''), COALESCE(CodigoProveedor,''),
                            COALESCE(Concepto,''), Fecha, COALESCE(PrecioPendiente,0)
                            FROM Gastos WHERE Id=@id";
                        q.Parameters.AddWithValue("@id", id);
                        using var r = q.ExecuteReader();
                        if (!r.Read()) { tx.Rollback(); return Results.NotFound(new { error = "Egreso no encontrado." }); }
                        proveedor   = r.GetString(0);
                        codProv     = r.GetString(1);
                        concepto    = r.GetString(2);
                        fechaStr    = Db.S(r, 3);
                        esPendiente = r.GetInt32(4) == 1;
                    }
                    if (!esPendiente) { tx.Rollback(); return Results.BadRequest(new { error = "El egreso no tiene precio pendiente." }); }

                    DateTime fechaGasto = DateTime.TryParse(fechaStr, out var fg) ? fg : DateTime.Today;

                    // Recalcular montos.
                    var tipoTasa   = req.TasaBCV > 0 ? "BCV" : "USD_DIRECTO";
                    var monedaOrig = req.MontoUSD > 0 ? "USD" : "BS";
                    var montoOrig  = req.MontoUSD > 0 ? req.MontoUSD : req.MontoBs;
                    var montoUSD   = req.MontoUSD > 0 ? req.MontoUSD
                                     : (req.TasaBCV > 0 ? Math.Round(req.MontoBs / req.TasaBCV, 4) : 0);
                    var montoBs    = req.MontoBs > 0 ? req.MontoBs
                                     : (req.TasaBCV > 0 ? Math.Round(req.MontoUSD * req.TasaBCV, 2) : 0);

                    // Abono / estado.
                    decimal pagadoUSD; string estado;
                    if (req.ACredito)
                    {
                        pagadoUSD = req.PagadoUSD < 0 ? 0 : req.PagadoUSD;
                        if (pagadoUSD >= montoUSD - 0.01m) { pagadoUSD = montoUSD; estado = "Pagado"; }
                        else estado = "Pendiente";
                    }
                    else { pagadoUSD = montoUSD; estado = "Pagado"; }

                    string? venceStr = estado == "Pendiente"
                        ? (req.FechaVencimiento ?? fechaGasto.AddDays(30).ToString("yyyy-MM-dd"))
                        : null;
                    string? fechaPagoStr = estado == "Pagado" || pagadoUSD > 0
                        ? DateTime.Today.ToString("yyyy-MM-dd") : null;

                    // Actualizar la cabecera Gastos.
                    using (var up = c.CreateCommand())
                    {
                        up.Transaction = tx;
                        up.CommandText = @"UPDATE Gastos SET
                            MonedaOriginal=@mon, TipoTasa=@tt, TasaUsada=@tasa,
                            MontoOriginal=@mo, MontoUSD=@usd, MontoBs=@bs, PagadoUSD=@pag,
                            PrecioPendiente=0, Estado=@es, FechaVencimiento=@fv, FechaPago=@fp
                            WHERE Id=@id";
                        up.Parameters.AddWithValue("@mon", monedaOrig);
                        up.Parameters.AddWithValue("@tt", tipoTasa);
                        up.Parameters.AddWithValue("@tasa", (double)req.TasaBCV);
                        up.Parameters.AddWithValue("@mo", (double)montoOrig);
                        up.Parameters.AddWithValue("@usd", (double)montoUSD);
                        up.Parameters.AddWithValue("@bs", (double)montoBs);
                        up.Parameters.AddWithValue("@pag", (double)pagadoUSD);
                        up.Parameters.AddWithValue("@es", estado);
                        up.Parameters.AddWithValue("@fv", (object?)venceStr ?? DBNull.Value);
                        up.Parameters.AddWithValue("@fp", (object?)fechaPagoStr ?? DBNull.Value);
                        up.Parameters.AddWithValue("@id", id);
                        up.ExecuteNonQuery();
                    }

                    // Actualizar precios de las líneas y ajustar el Kardex de cada
                    // insumo de materia prima con su costo real.
                    if (req.Detalles != null)
                    {
                        foreach (var det in req.Detalles)
                        {
                            using (var ud = c.CreateCommand())
                            {
                                ud.Transaction = tx;
                                ud.CommandText = det.IdArticulo.HasValue
                                    ? "UPDATE DetallesGasto SET PrecioUnitario=@pu, Subtotal=@sub WHERE IdGasto=@id AND IdArticulo=@art"
                                    : "UPDATE DetallesGasto SET PrecioUnitario=@pu, Subtotal=@sub WHERE IdGasto=@id AND Descripcion=@desc";
                                ud.Parameters.AddWithValue("@pu", (double)det.PrecioUnitario);
                                ud.Parameters.AddWithValue("@sub", (double)det.Subtotal);
                                ud.Parameters.AddWithValue("@id", id);
                                if (det.IdArticulo.HasValue) ud.Parameters.AddWithValue("@art", det.IdArticulo.Value);
                                else ud.Parameters.AddWithValue("@desc", det.Descripcion ?? "");
                                ud.ExecuteNonQuery();
                            }

                            // Ajuste de costo en Kardex (materia prima).
                            if (det.IdArticulo.HasValue && det.Cantidad > 0 && det.PrecioUnitario > 0)
                            {
                                string codigoInterno = "";
                                using (var q = c.CreateCommand())
                                {
                                    q.Transaction = tx;
                                    q.CommandText = "SELECT COALESCE(CodigoInterno,'') FROM ProductosProveedor WHERE Id=@art";
                                    q.Parameters.AddWithValue("@art", det.IdArticulo.Value);
                                    codigoInterno = (q.ExecuteScalar() as string) ?? "";
                                }
                                if (!string.IsNullOrEmpty(codigoInterno))
                                    AjustarCostoEntradaKardex(c, tx, codigoInterno, "Gasto#" + id, det.PrecioUnitario);
                            }
                        }
                    }

                    // Cuenta por Pagar por el saldo, ahora que se conoce el monto.
                    SincronizarCxPDesdeEgreso(c, tx, id, proveedor, codProv, concepto,
                        "Egreso #" + id, montoUSD, pagadoUSD, fechaStr,
                        venceStr ?? fechaGasto.AddDays(30).ToString("yyyy-MM-dd"));

                    tx.Commit();
                    return Results.Ok(new { id, estado, montoUSD, mensaje = "Precio completado correctamente." });
                }
                catch (Exception ex)
                {
                    tx.Rollback();
                    return Results.Problem($"Error al completar precio: {ex.Message}", statusCode: 500);
                }
            }).AddEndpointFilter(RequireRole.Of(TokenService.PuedeEscribirEgresos)).RequireAuthorization();
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

        // ── Actualizar stock + costo promedio (Materia Prima) ────────────────
        // precioPendiente: la nota de entrega llegó sin precios; la mercancía entra
        // a costo PROVISIONAL = costo promedio actual, para no distorsionar el
        // promedio ponderado. Al completar el precio se ajusta (AjustarCostoEntradaKardex).
        private static void ActualizarInventarioMP(
            SqliteConnection c, SqliteTransaction tx,
            int idArticulo, decimal cantidad, decimal precioUnitario,
            int idGasto, string usuario, bool precioPendiente = false)
        {
            // 1. Leer estado actual del insumo. El promedio se toma de PrecioPromedio
            //    (columna canónica de escritorio) con respaldo a CostoPromedio.
            string codigoInterno = "";
            decimal stockAnt = 0, costoAnt = 0;
            using (var cmdL = c.CreateCommand())
            {
                cmdL.Transaction = tx;
                cmdL.CommandText = @"
                    SELECT CodigoInterno, COALESCE(StockActual, 0),
                           COALESCE(NULLIF(PrecioPromedio,0), CostoPromedio, 0)
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

            // Costo de entrada: provisional (= promedio actual) si el precio está
            // pendiente; así el promedio ponderado no cambia hasta completar el precio.
            decimal costoEntrada = precioPendiente ? costoAnt : precioUnitario;

            // 2. Calcular nuevo stock y nuevo costo promedio ponderado
            var nuevoStock = stockAnt + cantidad;
            var nuevoCosto = nuevoStock > 0
                ? Math.Round((stockAnt * costoAnt + cantidad * costoEntrada) / nuevoStock, 4)
                : costoEntrada;

            // 3. Actualizar ProductosProveedor (ambas columnas de promedio para
            //    mantener consistencia entre web y escritorio).
            using (var cmdU = c.CreateCommand())
            {
                cmdU.Transaction = tx;
                cmdU.CommandText = @"
                    UPDATE ProductosProveedor
                    SET StockActual = @ns, CostoPromedio = @ncp, PrecioPromedio = @ncp,
                        PrecioUltCompra = @pu
                    WHERE Id = @id;";
                cmdU.Parameters.AddWithValue("@ns",  (double)nuevoStock);
                cmdU.Parameters.AddWithValue("@ncp", (double)nuevoCosto);
                cmdU.Parameters.AddWithValue("@pu",  (double)costoEntrada);
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
            cmdM.Parameters.AddWithValue("@conc", precioPendiente
                ? $"Compra MP (precio pendiente) — Gasto #{idGasto}"
                : $"Compra — Gasto #{idGasto}");
            cmdM.Parameters.AddWithValue("@cant", (double)cantidad);
            cmdM.Parameters.AddWithValue("@sa",   (double)stockAnt);
            cmdM.Parameters.AddWithValue("@ns",   (double)nuevoStock);
            cmdM.Parameters.AddWithValue("@ref",  $"Gasto#{idGasto}");
            cmdM.Parameters.AddWithValue("@usr",  usuario);
            cmdM.Parameters.AddWithValue("@cu",   (double)costoEntrada);
            cmdM.ExecuteNonQuery();
        }

        // ── Sincronizar Cuenta por Pagar desde un egreso (misma lógica que escritorio) ──
        // Idempotente: una CxP por egreso, ligada por IdEgreso.
        private static void SincronizarCxPDesdeEgreso(
            SqliteConnection c, SqliteTransaction tx,
            int idEgreso, string proveedor, string codigoProveedor,
            string concepto, string numRef, decimal montoUSD, decimal pagadoUSD,
            string emisionStr, string venceStr)
        {
            if (idEgreso <= 0) return;
            decimal saldo = montoUSD - pagadoUSD;

            // ¿Ya existe una CxP ligada a este egreso?
            int idCxP = 0;
            using (var q = c.CreateCommand())
            {
                q.Transaction = tx;
                q.CommandText = "SELECT Id FROM CuentasPagar WHERE IdEgreso=@ie LIMIT 1";
                q.Parameters.AddWithValue("@ie", idEgreso);
                var o = q.ExecuteScalar();
                if (o != null && o != DBNull.Value) idCxP = Convert.ToInt32(o);
            }

            if (saldo <= 0.01m)
            {
                // Pagado completo: si existía CxP, marcarla pagada.
                if (idCxP > 0)
                {
                    using var up = c.CreateCommand();
                    up.Transaction = tx;
                    up.CommandText = "UPDATE CuentasPagar SET Monto=@mo, Pagado=@mo, Estado='Pagada' WHERE Id=@id";
                    up.Parameters.AddWithValue("@mo", (double)montoUSD);
                    up.Parameters.AddWithValue("@id", idCxP);
                    up.ExecuteNonQuery();
                }
                return;
            }

            string estado = pagadoUSD > 0 ? "Parcial" : "Pendiente";
            if (idCxP > 0)
            {
                using var up = c.CreateCommand();
                up.Transaction = tx;
                up.CommandText = @"UPDATE CuentasPagar SET
                    CodigoProveedor=@cp, Proveedor=@pr, Concepto=@co, NumFactura=@nf,
                    Monto=@mo, Pagado=@pa, FechaEmision=@fe, FechaVence=@fv, Estado=@es
                    WHERE Id=@id";
                up.Parameters.AddWithValue("@cp", codigoProveedor ?? "");
                up.Parameters.AddWithValue("@pr", proveedor ?? "");
                up.Parameters.AddWithValue("@co", concepto ?? "");
                up.Parameters.AddWithValue("@nf", numRef ?? "");
                up.Parameters.AddWithValue("@mo", (double)montoUSD);
                up.Parameters.AddWithValue("@pa", (double)pagadoUSD);
                up.Parameters.AddWithValue("@fe", emisionStr);
                up.Parameters.AddWithValue("@fv", venceStr);
                up.Parameters.AddWithValue("@es", estado);
                up.Parameters.AddWithValue("@id", idCxP);
                up.ExecuteNonQuery();
            }
            else
            {
                using var ins = c.CreateCommand();
                ins.Transaction = tx;
                ins.CommandText = @"INSERT INTO CuentasPagar
                    (CodigoProveedor,Proveedor,Concepto,NumFactura,Monto,Pagado,
                     FechaEmision,FechaVence,Estado,Observaciones,IdEgreso)
                    VALUES(@cp,@pr,@co,@nf,@mo,@pa,@fe,@fv,@es,@ob,@ie)";
                ins.Parameters.AddWithValue("@cp", codigoProveedor ?? "");
                ins.Parameters.AddWithValue("@pr", proveedor ?? "");
                ins.Parameters.AddWithValue("@co", concepto ?? "");
                ins.Parameters.AddWithValue("@nf", numRef ?? "");
                ins.Parameters.AddWithValue("@mo", (double)montoUSD);
                ins.Parameters.AddWithValue("@pa", (double)pagadoUSD);
                ins.Parameters.AddWithValue("@fe", emisionStr);
                ins.Parameters.AddWithValue("@fv", venceStr);
                ins.Parameters.AddWithValue("@es", estado);
                ins.Parameters.AddWithValue("@ob", "Generada automáticamente desde el egreso #" + idEgreso);
                ins.Parameters.AddWithValue("@ie", idEgreso);
                ins.ExecuteNonQuery();
            }
        }

        // ── Ajustar costo de una entrada MP registrada con costo provisional ────
        // (misma lógica que escritorio: corrige el movimiento y carga la diferencia
        //  de valor sobre el stock actual). Usa la referencia "Gasto#{id}".
        private static void AjustarCostoEntradaKardex(
            SqliteConnection c, SqliteTransaction tx,
            string codigoInsumo, string referencia, decimal costoRealUnit)
        {
            if (string.IsNullOrEmpty(referencia)) return;

            int idMov = 0; decimal cantidad = 0, costoProv = 0; string concepto = "";
            using (var q = c.CreateCommand())
            {
                q.Transaction = tx;
                q.CommandText = @"SELECT IdMovimiento, Cantidad, CostoUnitario, COALESCE(Concepto,'')
                    FROM MovimientosInventario
                    WHERE TipoKardex='MATERIA_PRIMA' AND TipoMovimiento='ENTRADA'
                      AND CodigoProducto=@cod AND Referencia=@re
                    ORDER BY IdMovimiento DESC LIMIT 1";
                q.Parameters.AddWithValue("@cod", codigoInsumo);
                q.Parameters.AddWithValue("@re", referencia);
                using var r = q.ExecuteReader();
                if (r.Read())
                {
                    idMov     = r.GetInt32(0);
                    cantidad  = r.IsDBNull(1) ? 0 : (decimal)r.GetDouble(1);
                    costoProv = r.IsDBNull(2) ? 0 : (decimal)r.GetDouble(2);
                    concepto  = r.IsDBNull(3) ? "" : r.GetString(3);
                }
            }
            if (idMov == 0) return;

            decimal stock = 0, avg = 0;
            using (var q = c.CreateCommand())
            {
                q.Transaction = tx;
                q.CommandText = @"SELECT COALESCE(StockActual,0),
                    COALESCE(NULLIF(PrecioPromedio,0), CostoPromedio, 0)
                    FROM ProductosProveedor WHERE CodigoInterno=@cod";
                q.Parameters.AddWithValue("@cod", codigoInsumo);
                using var r = q.ExecuteReader();
                if (r.Read()) { stock = (decimal)r.GetDouble(0); avg = (decimal)r.GetDouble(1); }
            }

            decimal delta  = cantidad * (costoRealUnit - costoProv);
            decimal avgNew = stock > 0 ? Math.Round(avg + delta / stock, 4) : costoRealUnit;
            if (avgNew < 0) avgNew = 0;

            using (var up = c.CreateCommand())
            {
                up.Transaction = tx;
                up.CommandText = "UPDATE MovimientosInventario SET CostoUnitario=@cu, Concepto=@co WHERE IdMovimiento=@id";
                up.Parameters.AddWithValue("@cu", (double)costoRealUnit);
                up.Parameters.AddWithValue("@co", concepto + " [precio completado]");
                up.Parameters.AddWithValue("@id", idMov);
                up.ExecuteNonQuery();
            }
            using (var up = c.CreateCommand())
            {
                up.Transaction = tx;
                up.CommandText = "UPDATE ProductosProveedor SET PrecioPromedio=@pp, CostoPromedio=@pp, PrecioUltCompra=@pu WHERE CodigoInterno=@cod";
                up.Parameters.AddWithValue("@pp", (double)avgNew);
                up.Parameters.AddWithValue("@pu", (double)costoRealUnit);
                up.Parameters.AddWithValue("@cod", codigoInsumo);
                up.ExecuteNonQuery();
            }
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
        List<DetalleLineaRequest> Detalles,
        // Fase 3: pago a crédito / abono parcial y nota de entrega sin precios.
        bool    ACredito        = false,
        decimal PagadoUSD       = 0,
        bool    PrecioPendiente = false,
        string? CodigoProveedor = null,
        string? FechaVencimiento = null
    );

    // Completar el precio de una nota de entrega registrada como precio pendiente.
    record CompletarPrecioRequest(
        decimal MontoUSD,
        decimal MontoBs,
        decimal TasaBCV,
        bool    ACredito         = false,
        decimal PagadoUSD        = 0,
        string? FechaVencimiento = null,
        List<CompletarDetalleRequest>? Detalles = null
    );

    record CompletarDetalleRequest(
        int?    IdArticulo,
        string? Descripcion,
        decimal Cantidad,
        decimal PrecioUnitario,
        decimal Subtotal
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
