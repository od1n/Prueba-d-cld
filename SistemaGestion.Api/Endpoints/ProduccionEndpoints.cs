using Microsoft.Data.Sqlite;
using SistemaGestion.Api.Auth;

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
                        @"SELECT m.CodigoMold, m.Descripcion, COALESCE(mp.CodPrimera,'') AS CodProd
                          FROM TbMoldes m
                          LEFT JOIN MoldeProductos mp ON mp.CodigoMolde = m.CodigoMold
                          ORDER BY CASE WHEN COALESCE(mp.CodPrimera,'')='' THEN 1 ELSE 0 END, mp.CodPrimera, m.CodigoMold";
                    var lista = new List<object>();
                    using var r = cmd.ExecuteReader();
                    while (r.Read())
                        lista.Add(new { codigo = Db.S(r, 0), descripcion = Db.S(r, 1), codigoProducto = Db.S(r, 2) });
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
            }).AddEndpointFilter(RequireRole.Of(TokenService.PuedeEscribirProduccion)).RequireAuthorization().DisableAntiforgery().RequireRateLimiting("ia-extraccion");

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
            }).AddEndpointFilter(RequireRole.Of(TokenService.PuedeEscribirProduccion)).RequireAuthorization();

            // ── Registro manual de producción (canónico: escribe igual que el digitalizador/escritorio) ──
            //   Un registro por día (Fecha UNICA); detalle en DetalleProduccionMolde con
            //   CodigoProducto = producto base del molde (para que el bono de producción case);
            //   materiales en MaterialProduccionDia; obreros en ParticipantesProduccionDia.
            app.MapPost("/api/produccion/registro", (HttpContext ctx, RegistroProduccionRequest req) =>
            {
                if (req.Moldes == null || req.Moldes.Count == 0)
                    return Results.BadRequest(new { error = "Debe incluir al menos un molde." });

                string fecha    = string.IsNullOrWhiteSpace(req.Fecha) ? DateTime.Today.ToString("yyyy-MM-dd") : req.Fecha;
                string material = string.IsNullOrWhiteSpace(req.Material) ? "Virgen" : req.Material;

                using var c = Db.Conn();
                using var tx = c.BeginTransaction();
                try
                {
                    // 1. get-or-create RegistroProduccionDia (Fecha es UNICA)
                    int regId;
                    var q = c.CreateCommand(); q.Transaction = tx;
                    q.CommandText = "SELECT Id FROM RegistroProduccionDia WHERE Fecha=$f";
                    q.Parameters.AddWithValue("$f", fecha);
                    var found = q.ExecuteScalar();
                    if (found != null && found != DBNull.Value)
                    {
                        regId = (int)(long)found;
                        var up = c.CreateCommand(); up.Transaction = tx;
                        up.CommandText = "UPDATE RegistroProduccionDia SET TipoMaterial=$tm, Observacion=$ob WHERE Id=$id";
                        up.Parameters.AddWithValue("$tm", material);
                        up.Parameters.AddWithValue("$ob", req.Observaciones ?? "");
                        up.Parameters.AddWithValue("$id", regId);
                        up.ExecuteNonQuery();
                        foreach (var tbl in new[] { "DetalleProduccionMolde", "MaterialProduccionDia", "ParticipantesProduccionDia" })
                        {
                            var d = c.CreateCommand(); d.Transaction = tx;
                            d.CommandText = $"DELETE FROM {tbl} WHERE IdRegistro=$id";
                            d.Parameters.AddWithValue("$id", regId);
                            d.ExecuteNonQuery();
                        }
                    }
                    else
                    {
                        var ins = c.CreateCommand(); ins.Transaction = tx;
                        ins.CommandText = "INSERT INTO RegistroProduccionDia (Fecha,TipoMaterial,Observacion) VALUES ($f,$tm,$ob)";
                        ins.Parameters.AddWithValue("$f", fecha);
                        ins.Parameters.AddWithValue("$tm", material);
                        ins.Parameters.AddWithValue("$ob", req.Observaciones ?? "");
                        ins.ExecuteNonQuery();
                        var rid = c.CreateCommand(); rid.Transaction = tx;
                        rid.CommandText = "SELECT last_insert_rowid()";
                        regId = (int)(long)rid.ExecuteScalar();
                    }

                    // 2. Detalle por molde (CodigoProducto = producto base del molde)
                    int moldes = 0;
                    var producidos = new Dictionary<string, double>();   // codProducto -> unidades (para el kardex de producto terminado)
                    foreach (var m in req.Moldes)
                    {
                        if (string.IsNullOrWhiteSpace(m.CodigoMolde)) continue;
                        string codProd = "", cpMolde = "", csMolde = "", crMolde = "";
                        var mp = c.CreateCommand(); mp.Transaction = tx;
                        mp.CommandText = "SELECT COALESCE(CodPrimera,''), COALESCE(CodSegunda,''), COALESCE(CodReciclado,'') FROM MoldeProductos WHERE CodigoMolde=$cm";
                        mp.Parameters.AddWithValue("$cm", m.CodigoMolde);
                        using (var rr = mp.ExecuteReader())
                            if (rr.Read())
                            {
                                cpMolde = rr.GetString(0); csMolde = rr.GetString(1); crMolde = rr.GetString(2);
                                codProd = material == "Reciclado" ? (string.IsNullOrEmpty(crMolde) ? cpMolde : crMolde) : cpMolde;
                            }
                        double p1 = m.Primera, p2 = m.Segunda, def = m.Defectuosas;
                        double rec         = material == "Reciclado" ? p1 : 0;
                        double cantPrimera = material == "Reciclado" ? 0  : p1;
                        double total       = p1 + p2;
                        // acumular los productos terminados que entran al inventario (segunda = producto distinto)
                        if (material == "Reciclado")
                        {
                            if (!string.IsNullOrEmpty(crMolde) && rec > 0)
                                producidos[crMolde] = producidos.GetValueOrDefault(crMolde) + rec;
                        }
                        else
                        {
                            if (!string.IsNullOrEmpty(cpMolde) && cantPrimera > 0)
                                producidos[cpMolde] = producidos.GetValueOrDefault(cpMolde) + cantPrimera;
                            if (!string.IsNullOrEmpty(csMolde) && p2 > 0)
                                producidos[csMolde] = producidos.GetValueOrDefault(csMolde) + p2;
                        }
                        var di = c.CreateCommand(); di.Transaction = tx;
                        di.CommandText = @"INSERT INTO DetalleProduccionMolde
                            (IdRegistro,CodigoMolde,CodigoProducto,Cantidad,CantPrimera,CantSegunda,CantReciclado,CantDefectuosas)
                            VALUES ($ir,$cm,$cp,$ca,$c1,$c2,$cr,$cd)";
                        di.Parameters.AddWithValue("$ir", regId);
                        di.Parameters.AddWithValue("$cm", m.CodigoMolde);
                        di.Parameters.AddWithValue("$cp", codProd);
                        di.Parameters.AddWithValue("$ca", total);
                        di.Parameters.AddWithValue("$c1", cantPrimera);
                        di.Parameters.AddWithValue("$c2", (double)p2);
                        di.Parameters.AddWithValue("$cr", rec);
                        di.Parameters.AddWithValue("$cd", (double)def);
                        di.ExecuteNonQuery();
                        moldes++;
                    }

                    // 3. Materiales consumidos
                    if (req.Materiales != null)
                        foreach (var mat in req.Materiales)
                        {
                            if (string.IsNullOrWhiteSpace(mat.CodigoInsumo) || mat.Kg <= 0) continue;
                            var mi = c.CreateCommand(); mi.Transaction = tx;
                            mi.CommandText = "INSERT INTO MaterialProduccionDia (IdRegistro,CodigoInsumo,Cantidad) VALUES ($ir,$ci,$kg)";
                            mi.Parameters.AddWithValue("$ir", regId);
                            mi.Parameters.AddWithValue("$ci", mat.CodigoInsumo);
                            mi.Parameters.AddWithValue("$kg", (double)mat.Kg);
                            mi.ExecuteNonQuery();
                        }

                    // 4. Participantes (obreros marcados)
                    if (req.Participantes != null)
                        foreach (var idEmp in req.Participantes.Distinct())
                        {
                            if (idEmp <= 0) continue;
                            var pi = c.CreateCommand(); pi.Transaction = tx;
                            pi.CommandText = "INSERT INTO ParticipantesProduccionDia (IdRegistro,IdEmpleado) VALUES ($ir,$ie)";
                            pi.Parameters.AddWithValue("$ir", regId);
                            pi.Parameters.AddWithValue("$ie", idEmp);
                            pi.ExecuteNonQuery();
                        }

                    // 5. Kardex valorizado e idempotente (mismo criterio que digitalizador y escritorio)
                    string refDia = "PROD-" + fecha;
                    ReversarMovimientosDia(c, tx, refDia);

                    // 5a. Consumir insumos a costo promedio ponderado (salida de materia prima)
                    decimal costoMateriales = 0m;
                    if (req.Materiales != null)
                        foreach (var mat in req.Materiales)
                        {
                            if (string.IsNullOrWhiteSpace(mat.CodigoInsumo) || mat.Kg <= 0) continue;
                            var pp = c.CreateCommand(); pp.Transaction = tx;
                            pp.CommandText = "SELECT COALESCE(StockActual,0), COALESCE(NULLIF(CostoPromedio,0), PrecioPromedio, 0) FROM ProductosProveedor WHERE CodigoInterno=$ci";
                            pp.Parameters.AddWithValue("$ci", mat.CodigoInsumo);
                            decimal sa = 0m, cprom = 0m; bool existe = false;
                            using (var rr = pp.ExecuteReader())
                                if (rr.Read()) { sa = Db.D(rr, 0); cprom = Db.D(rr, 1); existe = true; }
                            if (!existe) continue;   // insumo no esta en ProductosProveedor: se registro el consumo, pero no hay kardex de MP
                            decimal kg = mat.Kg;
                            decimal ns = sa - kg;
                            costoMateriales += kg * cprom;
                            RegistrarMovProd(c, tx, mat.CodigoInsumo, "SALIDA", kg, sa, ns, cprom,
                                "MATERIA_PRIMA", refDia, fecha, $"Produccion {fecha} ({material})");
                            var uu = c.CreateCommand(); uu.Transaction = tx;
                            uu.CommandText = "UPDATE ProductosProveedor SET StockActual=$ns WHERE CodigoInterno=$ci";
                            uu.Parameters.AddWithValue("$ns", (double)ns);
                            uu.Parameters.AddWithValue("$ci", mat.CodigoInsumo);
                            uu.ExecuteNonQuery();
                        }

                    // 5b. Mano de obra del dia = suma de los bonos de los obreros que cobran (participantes, o tarifados si no hay)
                    var lc = c.CreateCommand(); lc.Transaction = tx;
                    lc.CommandText = @"
                        SELECT COALESCE(SUM(d.Cantidad * t.TarifaBs),0)
                        FROM DetalleProduccionMolde d
                        JOIN RegistroProduccionDia r ON r.Id = d.IdRegistro
                        JOIN TarifasBonoProduccion t
                          ON t.CodigoProducto = d.CodigoProducto
                         AND r.Fecha >= t.FechaDesde
                         AND (t.FechaHasta IS NULL OR r.Fecha <= t.FechaHasta)
                        WHERE r.Id = $id
                          AND ( NOT EXISTS (SELECT 1 FROM ParticipantesProduccionDia pp WHERE pp.IdRegistro=r.Id)
                                OR EXISTS (SELECT 1 FROM ParticipantesProduccionDia p2 WHERE p2.IdRegistro=r.Id AND p2.IdEmpleado=t.IdEmpleado) )";
                    lc.Parameters.AddWithValue("$id", regId);
                    var oMano = lc.ExecuteScalar();
                    decimal manoObra = (oMano == null || oMano == DBNull.Value) ? 0m : Convert.ToDecimal(oMano);

                    // 5c. Costo de fabricacion por unidad y entrada de productos terminados a Inventario
                    decimal unidades = 0m;
                    foreach (var kv in producidos) unidades += (decimal)kv.Value;
                    decimal costoFabUnit = unidades > 0 ? Math.Round((costoMateriales + manoObra) / unidades, 4) : 0m;
                    foreach (var kv in producidos)
                    {
                        string cod = kv.Key; decimal cant = (decimal)kv.Value;
                        if (string.IsNullOrEmpty(cod) || cant <= 0) continue;
                        var ce = c.CreateCommand(); ce.Transaction = tx;
                        ce.CommandText = "SELECT COALESCE(Existencia,0) FROM Inventario WHERE Codigo=$cod";
                        ce.Parameters.AddWithValue("$cod", cod);
                        var oex = ce.ExecuteScalar();
                        if (oex == null || oex == DBNull.Value) continue;   // producto no esta en Inventario
                        decimal sa2 = Convert.ToDecimal(oex);
                        decimal ns2 = sa2 + cant;
                        RegistrarMovProd(c, tx, cod, "ENTRADA", cant, sa2, ns2, costoFabUnit,
                            "PRODUCTO", refDia, fecha, $"Produccion {fecha} ({material})");
                        var uu = c.CreateCommand(); uu.Transaction = tx;
                        uu.CommandText = "UPDATE Inventario SET Existencia=$ns, CostoActual=$cu, UltimaActualz=$f WHERE Codigo=$cod";
                        uu.Parameters.AddWithValue("$ns", (double)ns2);
                        uu.Parameters.AddWithValue("$cu", (double)costoFabUnit);
                        uu.Parameters.AddWithValue("$f", fecha);
                        uu.Parameters.AddWithValue("$cod", cod);
                        uu.ExecuteNonQuery();
                    }

                    tx.Commit();
                    return Results.Ok(new { ok = true, insertados = moldes, fecha, costoUnitario = costoFabUnit, manoObra });
                }
                catch (Exception ex)
                {
                    tx.Rollback();
                    return Results.Json(new { ok = false, error = ex.Message }, statusCode: 500);
                }
            }).AddEndpointFilter(RequireRole.Of(TokenService.PuedeEscribirProduccion)).RequireAuthorization();

            // ── Obreros de producción activos (para marcar participantes) ──
            app.MapGet("/api/produccion/obreros", () =>
            {
                using var c = Db.Conn();
                var cmd = c.CreateCommand();
                cmd.CommandText = @"SELECT Id, Nombre, Apellido FROM Empleados
                    WHERE Activo=1 AND TipoEmpleado='Produccion' ORDER BY Apellido, Nombre";
                var lista = new List<object>();
                using var r = cmd.ExecuteReader();
                while (r.Read())
                    lista.Add(new { id = Db.I2(r, 0), nombre = (Db.S(r, 1) + " " + Db.S(r, 2)).Trim() });
                return Results.Ok(lista);
            }).RequireAuthorization();

            // ── Último registro (para preseleccionar material, obreros y moldes) ──
            app.MapGet("/api/produccion/ultimo", () =>
            {
                using var c = Db.Conn();
                var q = c.CreateCommand();
                q.CommandText = "SELECT Id, COALESCE(TipoMaterial,'Virgen') FROM RegistroProduccionDia ORDER BY Fecha DESC, Id DESC LIMIT 1";
                int regId = 0; string material = "Virgen";
                using (var r = q.ExecuteReader())
                    if (r.Read()) { regId = Db.I2(r, 0); material = Db.S(r, 1); }
                var participantes = new List<int>();
                var moldes = new List<string>();
                if (regId > 0)
                {
                    var pc = c.CreateCommand();
                    pc.CommandText = "SELECT IdEmpleado FROM ParticipantesProduccionDia WHERE IdRegistro=$id";
                    pc.Parameters.AddWithValue("$id", regId);
                    using (var r = pc.ExecuteReader()) while (r.Read()) participantes.Add(Db.I2(r, 0));
                    var mc = c.CreateCommand();
                    mc.CommandText = "SELECT DISTINCT CodigoMolde FROM DetalleProduccionMolde WHERE IdRegistro=$id";
                    mc.Parameters.AddWithValue("$id", regId);
                    using (var r = mc.ExecuteReader()) while (r.Read()) moldes.Add(Db.S(r, 0));
                }
                return Results.Ok(new { material, participantes, moldes });
            }).RequireAuthorization();

            // ── Listado de registros recientes ────────────────────────────

            app.MapGet("/api/produccion/registros", (string? desde, string? hasta, string? q) =>
            {
                string d = desde ?? DateTime.Today.AddDays(-7).ToString("yyyy-MM-dd");
                string h = hasta ?? DateTime.Today.ToString("yyyy-MM-dd");
                using var c = Db.Conn();
                var cmd = c.CreateCommand();
                string where = "Fecha BETWEEN $d AND $h";
                if (!string.IsNullOrWhiteSpace(q))
                    where += " AND (Operario LIKE $q OR CodigoMolde LIKE $q)";
                cmd.CommandText = $@"SELECT Id,Fecha,CodigoMolde,Material,Operario,Turno,
                    CantPrimeraCalidad,CantSegundaCalidad,CantDefectuosas,Observaciones
                    FROM RegistroProduccionDia WHERE {where}
                    ORDER BY Fecha DESC, Id DESC LIMIT 100";
                cmd.Parameters.AddWithValue("$d", d);
                cmd.Parameters.AddWithValue("$h", h);
                if (!string.IsNullOrWhiteSpace(q))
                    cmd.Parameters.AddWithValue("$q", $"%{q}%");
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

            // ── Proxy: extraer factura de venta (OCR) ─────────────────────
            // Usa el digitalizador Python con tipo="factura_venta"
            app.MapPost("/api/facturas/extraer-ocr", async (IFormFile imagen, IHttpClientFactory http) =>
            {
                const long MAX_BYTES = 20 * 1024 * 1024;
                if (imagen.Length > MAX_BYTES)
                    return Results.BadRequest(new { error = "Imagen demasiado grande (máx 20 MB)." });

                try
                {
                    var cli = http.CreateClient("digitalizador");
                    if (!string.IsNullOrEmpty(DIG_TOKEN))
                        cli.DefaultRequestHeaders.Add("X-Internal-Token", DIG_TOKEN);

                    using var ms = new MemoryStream();
                    await imagen.CopyToAsync(ms);

                    using var multipart = new MultipartFormDataContent();
                    var imgPart = new ByteArrayContent(ms.ToArray());
                    imgPart.Headers.ContentType =
                        System.Net.Http.Headers.MediaTypeHeaderValue.Parse(
                            imagen.ContentType ?? "image/jpeg");
                    multipart.Add(imgPart, "imagen", imagen.FileName ?? "foto.jpg");
                    multipart.Add(new StringContent("factura_venta"), "tipo");

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
            }).AddEndpointFilter(RequireRole.Of(TokenService.PuedeEscribirFacturas)).RequireAuthorization().DisableAntiforgery().RequireRateLimiting("ia-extraccion");

            // ── Planilla imprimible en blanco ─────────────────────────────

            app.MapGet("/api/produccion/planilla", async (HttpContext ctx) =>
            {
                // 12 filas vacías — el operario escribe los moldes en uso ese día
                const int filasMolde = 12;

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
                "td{height:7mm;font-size:7.5pt}" +
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
                "<table><thead><tr><th style='width:5mm'>#</th><th style='width:28mm'>MOLDE</th><th>1ª CALIDAD</th><th>2ª CALIDAD</th><th>RECICLADO</th><th style='color:red'>DEFECTUOSAS</th><th>TOTAL</th></tr></thead><tbody>";

                // 12 filas vacías — el operario escribe los moldes en producción ese día
                for (int i = 1; i <= filasMolde; i++)
                    html += $"<tr><td style='color:#aaa'>{i}</td><td></td><td></td><td></td><td></td><td></td><td></td></tr>";

                html += "<tr style='font-weight:700;background:#f5f5f5'><td></td><td>TOTALES</td><td></td><td></td><td></td><td></td><td></td></tr>" +
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

        // ── Kardex: revertir los movimientos de un dia de produccion (idempotencia) ──
        private static void ReversarMovimientosDia(SqliteConnection c, SqliteTransaction tx, string refDia)
        {
            var sel = c.CreateCommand(); sel.Transaction = tx;
            sel.CommandText = "SELECT CodigoProducto, TipoMovimiento, Cantidad, TipoKardex FROM MovimientosInventario WHERE Referencia=$r";
            sel.Parameters.AddWithValue("$r", refDia);
            var movs = new List<(string cod, string tipoMov, double cant, string tipoK)>();
            using (var rr = sel.ExecuteReader())
                while (rr.Read())
                    movs.Add((rr.IsDBNull(0) ? "" : rr.GetString(0), rr.IsDBNull(1) ? "" : rr.GetString(1),
                             rr.IsDBNull(2) ? 0 : rr.GetDouble(2), rr.IsDBNull(3) ? "" : rr.GetString(3)));
            foreach (var mv in movs)
            {
                if (mv.tipoK == "MATERIA_PRIMA")
                {
                    double signo = mv.tipoMov == "SALIDA" ? +1 : -1;   // deshacer el efecto sobre el stock
                    var u = c.CreateCommand(); u.Transaction = tx;
                    u.CommandText = "UPDATE ProductosProveedor SET StockActual = COALESCE(StockActual,0) + $d WHERE CodigoInterno=$cod";
                    u.Parameters.AddWithValue("$d", signo * mv.cant);
                    u.Parameters.AddWithValue("$cod", mv.cod);
                    u.ExecuteNonQuery();
                }
                else
                {
                    double signo = mv.tipoMov == "ENTRADA" ? -1 : +1;   // deshacer el efecto sobre el stock
                    var u = c.CreateCommand(); u.Transaction = tx;
                    u.CommandText = "UPDATE Inventario SET Existencia = COALESCE(Existencia,0) + $d WHERE Codigo=$cod";
                    u.Parameters.AddWithValue("$d", signo * mv.cant);
                    u.Parameters.AddWithValue("$cod", mv.cod);
                    u.ExecuteNonQuery();
                }
            }
            var del = c.CreateCommand(); del.Transaction = tx;
            del.CommandText = "DELETE FROM MovimientosInventario WHERE Referencia=$r";
            del.Parameters.AddWithValue("$r", refDia);
            del.ExecuteNonQuery();
        }

        // ── Kardex: insertar un movimiento valorizado ──
        private static void RegistrarMovProd(SqliteConnection c, SqliteTransaction tx,
            string cod, string tipoMov, decimal cant, decimal saldoAnt, decimal saldoNue, decimal costoUnit,
            string tipoKardex, string referencia, string fecha, string concepto)
        {
            var ci = c.CreateCommand(); ci.Transaction = tx;
            ci.CommandText = @"INSERT INTO MovimientosInventario
                (FechaHora,CodigoProducto,TipoMovimiento,Concepto,Cantidad,SaldoAnterior,NuevoSaldo,Referencia,Usuario,TipoKardex,CostoUnitario)
                VALUES ($fh,$cp,$tm,$co,$ca,$sa,$ns,$re,$us,$tk,$cu)";
            ci.Parameters.AddWithValue("$fh", fecha + " " + DateTime.Now.ToString("HH:mm:ss"));
            ci.Parameters.AddWithValue("$cp", cod);
            ci.Parameters.AddWithValue("$tm", tipoMov);
            ci.Parameters.AddWithValue("$co", concepto);
            ci.Parameters.AddWithValue("$ca", (double)cant);
            ci.Parameters.AddWithValue("$sa", (double)saldoAnt);
            ci.Parameters.AddWithValue("$ns", (double)saldoNue);
            ci.Parameters.AddWithValue("$re", referencia);
            ci.Parameters.AddWithValue("$us", "Portal");
            ci.Parameters.AddWithValue("$tk", tipoKardex);
            ci.Parameters.AddWithValue("$cu", (double)costoUnit);
            ci.ExecuteNonQuery();
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
        List<MaterialRegistro>? Materiales,
        List<int>? Participantes);
}
