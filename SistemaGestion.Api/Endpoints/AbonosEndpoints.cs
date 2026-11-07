// AbonosEndpoints.cs — API REST para abonos, cuentas receptoras y estado de cuenta
// GET  /api/abonos?cliente=&desde=&hasta=
// POST /api/abonos  (multipart: campos + imagen comprobante)
// PUT  /api/abonos/{id}/estado  { estado: "Confirmado"|"Rechazado" }
// GET  /api/abonos/cuentas
// GET  /api/abonos/estado-cuenta/{codigoCliente}
using SistemaGestion.Api.Auth;

namespace SistemaGestion.Api.Endpoints
{
    public static class AbonosEndpoints
    {
        public static void MapAbonosEndpoints(this WebApplication app)
        {
            // ── GET /api/abonos ─────────────────────────────────────────────
            app.MapGet("/api/abonos", (HttpContext ctx,
                string? cliente, string? desde, string? hasta) =>
            {
                if (!TokenService.PuedeVerCxC(ctx.User)) return Results.Forbid();
                using var c = Db.Conn();
                var cmd = c.CreateCommand();
                var where = new List<string>();
                if (!string.IsNullOrWhiteSpace(cliente))
                {
                    where.Add("a.Cliente = $cl");
                    cmd.Parameters.AddWithValue("$cl", cliente);
                }
                if (!string.IsNullOrWhiteSpace(desde))
                {
                    where.Add("a.Fecha >= $desde");
                    cmd.Parameters.AddWithValue("$desde", desde);
                }
                if (!string.IsNullOrWhiteSpace(hasta))
                {
                    where.Add("a.Fecha <= $hasta");
                    cmd.Parameters.AddWithValue("$hasta", hasta);
                }
                string filtro = where.Count > 0 ? " WHERE " + string.Join(" AND ", where) : "";
                cmd.CommandText = $@"
                    SELECT a.NumAbono, a.NumeroF, a.Fecha, a.Monto, a.Cliente,
                           COALESCE(a.MetodoPago,'Efectivo'),
                           COALESCE(a.Referencia,''),
                           COALESCE(a.NombreRemitente,''),
                           COALESCE(a.RutaComprobante,''),
                           COALESCE(a.Observaciones,''),
                           COALESCE(a.IdCuentaReceptora,0),
                           COALESCE(a.Estado,'Confirmado'),
                           COALESCE(cl.Descripcion, a.Cliente)
                    FROM Abonos a
                    LEFT JOIN Clientes cl ON cl.Codigo = a.Cliente
                    {filtro}
                    ORDER BY a.Fecha DESC
                    LIMIT 500";

                var list = new List<object>();
                using var r = cmd.ExecuteReader();
                while (r.Read())
                {
                    list.Add(new
                    {
                        id              = r.GetInt32(0),
                        numeroF         = Db.S(r, 1),
                        fecha           = Db.S(r, 2),
                        monto           = Db.D(r, 3),
                        cliente         = Db.S(r, 4),
                        metodoPago      = Db.S(r, 5),
                        referencia      = Db.S(r, 6),
                        remitente       = Db.S(r, 7),
                        rutaComprobante = Db.S(r, 8),
                        observaciones   = Db.S(r, 9),
                        idCuenta        = r.GetInt32(10),
                        estado          = Db.S(r, 11),
                        clienteNombre   = Db.S(r, 12)
                    });
                }
                return Results.Ok(list);
            }).RequireAuthorization();

            // ── POST /api/abonos (multipart) ────────────────────────────────
            app.MapPost("/api/abonos", async (HttpContext ctx) =>
            {
                if (!TokenService.PuedeVerCxC(ctx.User)) return Results.Forbid();

                var form = await ctx.Request.ReadFormAsync();
                string cliente      = form["cliente"].ToString();
                string fecha        = form["fecha"].ToString();
                string montoStr     = form["monto"].ToString();
                string metodo       = form["metodoPago"].ToString();
                string referencia   = form["referencia"].ToString();
                string remitente    = form["remitente"].ToString();
                string observaciones= form["observaciones"].ToString();
                string cuentaStr    = form["idCuenta"].ToString();
                string estado       = form["estado"].ToString();

                if (string.IsNullOrWhiteSpace(cliente) || string.IsNullOrWhiteSpace(montoStr))
                    return Results.BadRequest(new { error = "cliente y monto son requeridos." });

                if (!decimal.TryParse(montoStr, System.Globalization.NumberStyles.Any,
                    System.Globalization.CultureInfo.InvariantCulture, out decimal monto) || monto <= 0)
                    return Results.BadRequest(new { error = "Monto inválido." });

                int.TryParse(cuentaStr, out int idCuenta);

                // Guardar comprobante si viene imagen
                string rutaComprobante = "";
                var archivo = form.Files.GetFile("comprobante");
                if (archivo != null && archivo.Length > 0)
                {
                    const long MAX_BYTES = 15 * 1024 * 1024;
                    if (archivo.Length > MAX_BYTES)
                        return Results.BadRequest(new { error = "Imagen demasiado grande (máx 15 MB)." });

                    var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                    var carpeta = Path.Combine(appData, "SistemaGestion", "comprobantes", "abonos",
                        DateTime.Today.ToString("yyyy-MM"));
                    Directory.CreateDirectory(carpeta);
                    string ext = Path.GetExtension(archivo.FileName);
                    string destino = Path.Combine(carpeta,
                        "abono_" + DateTime.Now.ToString("yyyyMMdd_HHmmss_fff") + ext);
                    using var fs = new FileStream(destino, FileMode.Create);
                    await archivo.CopyToAsync(fs);
                    rutaComprobante = Path.GetFullPath(destino);
                }

                using var c = Db.Conn();
                var cmd = c.CreateCommand();
                cmd.CommandText = @"
                    INSERT INTO Abonos
                        (NumeroF, Fecha, Monto, Cliente, MetodoPago, Referencia,
                         NombreRemitente, RutaComprobante, Observaciones,
                         IdCuentaReceptora, Estado)
                    VALUES
                        (NULL, $fe, $mo, $cl, $mp, $ref,   -- NULL: abono sin factura (con '' falla la clave foránea a Facturas)
                         $nr, $rc, $obs, $icr, $est)";
                cmd.Parameters.AddWithValue("$fe",  fecha);
                cmd.Parameters.AddWithValue("$mo",  (double)monto);
                cmd.Parameters.AddWithValue("$cl",  cliente);
                cmd.Parameters.AddWithValue("$mp",  string.IsNullOrWhiteSpace(metodo) ? "Efectivo" : metodo);
                cmd.Parameters.AddWithValue("$ref", referencia);
                cmd.Parameters.AddWithValue("$nr",  remitente);
                cmd.Parameters.AddWithValue("$rc",  rutaComprobante);
                cmd.Parameters.AddWithValue("$obs", observaciones);
                cmd.Parameters.AddWithValue("$icr", idCuenta);
                cmd.Parameters.AddWithValue("$est", string.IsNullOrWhiteSpace(estado) ? "Confirmado" : estado);
                cmd.ExecuteNonQuery();

                // Obtener el id insertado
                cmd.CommandText = "SELECT last_insert_rowid()";
                var newId = Convert.ToInt32(cmd.ExecuteScalar());

                return Results.Ok(new { id = newId, mensaje = "Abono registrado." });
            }).RequireAuthorization()
              .DisableAntiforgery();

            // ── PUT /api/abonos/{id}/estado ──────────────────────────────────
            app.MapPut("/api/abonos/{id}/estado", (HttpContext ctx, int id) =>
            {
                if (!TokenService.PuedeVerCxC(ctx.User)) return Results.Forbid();

                var body = ctx.Request.ReadFromJsonAsync<EstadoRequest>().Result;
                if (body == null || string.IsNullOrWhiteSpace(body.Estado))
                    return Results.BadRequest(new { error = "estado requerido." });

                var validos = new[] { "Confirmado", "Pendiente", "Rechazado" };
                if (!validos.Contains(body.Estado))
                    return Results.BadRequest(new { error = $"Estado inválido. Valores: {string.Join(", ", validos)}" });

                using var c = Db.Conn();
                var cmd = c.CreateCommand();
                cmd.CommandText = "UPDATE Abonos SET Estado = $est WHERE NumAbono = $id";
                cmd.Parameters.AddWithValue("$est", body.Estado);
                cmd.Parameters.AddWithValue("$id", id);
                int affected = cmd.ExecuteNonQuery();
                if (affected == 0)
                    return Results.NotFound(new { error = "Abono no encontrado." });
                return Results.Ok(new { mensaje = "Estado actualizado." });
            }).RequireAuthorization();

            // ── GET /api/abonos/cuentas ─────────────────────────────────────
            app.MapGet("/api/abonos/cuentas", (HttpContext ctx) =>
            {
                if (!TokenService.PuedeVerCxC(ctx.User)) return Results.Forbid();
                using var c = Db.Conn();
                var cmd = c.CreateCommand();
                cmd.CommandText = @"
                    SELECT Id, Nombre, Banco, Tipo, Titular, Documento, Activa
                    FROM CuentasReceptoras ORDER BY Nombre";
                var list = new List<object>();
                using var r = cmd.ExecuteReader();
                while (r.Read())
                    list.Add(new
                    {
                        id        = r.GetInt32(0),
                        nombre    = Db.S(r, 1),
                        banco     = Db.S(r, 2),
                        tipo      = Db.S(r, 3),
                        titular   = Db.S(r, 4),
                        documento = Db.S(r, 5),
                        activa    = r.GetInt32(6) == 1
                    });
                return Results.Ok(list);
            }).RequireAuthorization();

            // ── GET /api/abonos/estado-cuenta/{codigoCliente} ───────────────
            app.MapGet("/api/abonos/estado-cuenta/{codigoCliente}", (HttpContext ctx, string codigoCliente) =>
            {
                if (!TokenService.PuedeVerCxC(ctx.User)) return Results.Forbid();
                using var c = Db.Conn();

                // Datos del cliente
                var cmdCl = c.CreateCommand();
                cmdCl.CommandText = "SELECT Descripcion, CiRif, COALESCE(AFavor,0) FROM Clientes WHERE Codigo=$cod";
                cmdCl.Parameters.AddWithValue("$cod", codigoCliente);
                using var rcl = cmdCl.ExecuteReader();
                if (!rcl.Read())
                    return Results.NotFound(new { error = "Cliente no encontrado." });
                string nombre = Db.S(rcl, 0);
                string ciRif  = Db.S(rcl, 1);
                decimal saldoFavor = Db.D(rcl, 2);
                rcl.Close();

                // Abonos del cliente
                var cmdA = c.CreateCommand();
                cmdA.CommandText = @"
                    SELECT NumAbono, Fecha, Monto,
                           COALESCE(MetodoPago,'Efectivo'),
                           COALESCE(Referencia,''),
                           COALESCE(Estado,'Confirmado')
                    FROM Abonos WHERE Cliente=$cod
                    ORDER BY Fecha";
                cmdA.Parameters.AddWithValue("$cod", codigoCliente);
                var abonos = new List<object>();
                decimal totalConfirmado = 0;
                using var ra = cmdA.ExecuteReader();
                while (ra.Read())
                {
                    string est = Db.S(ra, 5);
                    decimal m  = Db.D(ra, 2);
                    if (est == "Confirmado") totalConfirmado += m;
                    abonos.Add(new
                    {
                        id         = ra.GetInt32(0),
                        fecha      = Db.S(ra, 1),
                        monto      = m,
                        metodo     = Db.S(ra, 3),
                        referencia = Db.S(ra, 4),
                        estado     = est
                    });
                }

                // Cuentas por cobrar pendientes
                var cmdF = c.CreateCommand();
                cmdF.CommandText = @"
                    SELECT Concepto, Monto, Pagado, FechaEmision, FechaVence, Estado
                    FROM CuentasCobrar
                    WHERE (Cliente = $cod OR Cliente = $nom) AND Estado != 'Saldada'
                    ORDER BY FechaVence";
                cmdF.Parameters.AddWithValue("$cod", codigoCliente);
                cmdF.Parameters.AddWithValue("$nom", nombre);
                var facturas = new List<object>();
                decimal totalDeuda = 0;
                using var rf = cmdF.ExecuteReader();
                while (rf.Read())
                {
                    decimal monto2 = Db.D(rf, 1);
                    decimal pagado = Db.D(rf, 2);
                    totalDeuda += (monto2 - pagado);
                    facturas.Add(new
                    {
                        concepto   = Db.S(rf, 0),
                        total      = monto2,
                        pagado     = pagado,
                        saldo      = monto2 - pagado,
                        emision    = Db.S(rf, 3),
                        vence      = Db.S(rf, 4),
                        estado     = Db.S(rf, 5)
                    });
                }

                return Results.Ok(new
                {
                    cliente = new { codigo = codigoCliente, nombre, ciRif },
                    saldoFavor,
                    totalAbonos = totalConfirmado,
                    totalDeuda,
                    abonos,
                    facturasPendientes = facturas
                });
            }).RequireAuthorization();

            // ── GET /api/abonos/{id}/comprobante ────────────────────────────
            // Sirve la imagen del comprobante como archivo
            app.MapGet("/api/abonos/{id}/comprobante", (HttpContext ctx, int id) =>
            {
                if (!TokenService.PuedeVerCxC(ctx.User)) return Results.Forbid();
                using var c = Db.Conn();
                var cmd = c.CreateCommand();
                cmd.CommandText = "SELECT COALESCE(RutaComprobante,'') FROM Abonos WHERE NumAbono=$id";
                cmd.Parameters.AddWithValue("$id", id);
                var ruta = cmd.ExecuteScalar()?.ToString() ?? "";
                if (string.IsNullOrEmpty(ruta) || !File.Exists(ruta))
                    return Results.NotFound(new { error = "Comprobante no encontrado." });

                string mime = Path.GetExtension(ruta).ToLower() switch
                {
                    ".jpg" or ".jpeg" => "image/jpeg",
                    ".png"           => "image/png",
                    ".webp"          => "image/webp",
                    ".pdf"           => "application/pdf",
                    ".bmp"           => "image/bmp",
                    _                => "application/octet-stream"
                };
                return Results.File(ruta, mime);
            }).RequireAuthorization();

            // ── POST /api/facturas/{numeroF}/comprobantes ──────────────────
            // Crea un ComprobanteFactura + Abono vinculado (multipart)
            app.MapPost("/api/facturas/{numeroF}/comprobantes", async (HttpContext ctx, string numeroF) =>
            {
                if (!TokenService.PuedeVerCxC(ctx.User)) return Results.Forbid();

                var form = await ctx.Request.ReadFormAsync();
                string metodoPago  = form["metodoPago"].FirstOrDefault() ?? "Efectivo";
                string montoStr    = form["monto"].FirstOrDefault() ?? "0";
                string referencia  = form["referencia"].FirstOrDefault() ?? "";
                string cuentaIdStr = form["idCuentaReceptora"].FirstOrDefault() ?? "0";
                string obs         = form["observaciones"].FirstOrDefault() ?? "";
                string cliente     = form["cliente"].FirstOrDefault() ?? "";

                if (!decimal.TryParse(montoStr, System.Globalization.NumberStyles.Any,
                    System.Globalization.CultureInfo.InvariantCulture, out decimal monto) || monto <= 0)
                    return Results.BadRequest(new { error = "Monto inválido." });

                int.TryParse(cuentaIdStr, out int idCuenta);

                // Guardar imagen si viene
                string rutaImagen = "";
                var archivo = form.Files.GetFile("imagen");
                if (archivo != null && archivo.Length > 0)
                {
                    if (archivo.Length > 15 * 1024 * 1024)
                        return Results.BadRequest(new { error = "Imagen muy grande (máx 15 MB)." });

                    string carpeta = Path.Combine(
                        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                        "SistemaGestion", "comprobantes", "facturas",
                        DateTime.Today.ToString("yyyy-MM"));
                    Directory.CreateDirectory(carpeta);

                    string ext = Path.GetExtension(archivo.FileName);
                    string destino = Path.Combine(carpeta,
                        $"{numeroF}_{metodoPago}_{DateTime.Now:HHmmss}{ext}");
                    using (var fs = new FileStream(destino, FileMode.Create))
                        await archivo.CopyToAsync(fs);
                    rutaImagen = destino;
                }

                // Insertar ComprobanteFactura
                using var c = Db.Conn();
                var cmd = c.CreateCommand();
                cmd.CommandText = @"
                    INSERT INTO ComprobantesFactura
                        (NumeroF, MetodoPago, Monto, Referencia, RutaImagen,
                         IdCuentaReceptora, Fecha, Observaciones)
                    VALUES ($nf, $mp, $mo, $ref, $ri, $icr, $fe, $obs)";
                cmd.Parameters.AddWithValue("$nf",  numeroF);
                cmd.Parameters.AddWithValue("$mp",  metodoPago);
                cmd.Parameters.AddWithValue("$mo",  (double)monto);
                cmd.Parameters.AddWithValue("$ref", referencia);
                cmd.Parameters.AddWithValue("$ri",  rutaImagen);
                cmd.Parameters.AddWithValue("$icr", idCuenta);
                cmd.Parameters.AddWithValue("$fe",  DateTime.Today.ToString("yyyy-MM-dd"));
                cmd.Parameters.AddWithValue("$obs", obs);
                cmd.ExecuteNonQuery();

                // Insertar Abono vinculado
                var cmdA = c.CreateCommand();
                cmdA.CommandText = @"
                    INSERT INTO Abonos
                        (NumeroF, Fecha, Monto, Cliente, MetodoPago, Referencia,
                         NombreRemitente, RutaComprobante, Observaciones,
                         IdCuentaReceptora, Estado)
                    VALUES ($nf, $fe, $mo, $cl, $mp, $ref, '', $rc, $obs, $icr, 'Confirmado')";
                cmdA.Parameters.AddWithValue("$nf",  numeroF);
                cmdA.Parameters.AddWithValue("$fe",  DateTime.Today.ToString("yyyy-MM-dd"));
                cmdA.Parameters.AddWithValue("$mo",  (double)monto);
                cmdA.Parameters.AddWithValue("$cl",  cliente);
                cmdA.Parameters.AddWithValue("$mp",  metodoPago);
                cmdA.Parameters.AddWithValue("$ref", referencia);
                cmdA.Parameters.AddWithValue("$rc",  rutaImagen);
                cmdA.Parameters.AddWithValue("$obs", $"Comprobante factura {numeroF}");
                cmdA.Parameters.AddWithValue("$icr", idCuenta);
                cmdA.ExecuteNonQuery();

                return Results.Ok(new { ok = true, metodo = metodoPago, monto });
            }).RequireAuthorization().DisableAntiforgery();

            // ── GET /api/facturas/{numeroF}/comprobantes ───────────────────
            app.MapGet("/api/facturas/{numeroF}/comprobantes", (HttpContext ctx, string numeroF) =>
            {
                if (!TokenService.PuedeVerCxC(ctx.User)) return Results.Forbid();
                using var c = Db.Conn();
                var cmd = c.CreateCommand();
                cmd.CommandText = @"
                    SELECT cf.Id, cf.MetodoPago, cf.Monto, cf.Referencia,
                           cf.IdCuentaReceptora, cf.Fecha, cf.Observaciones,
                           COALESCE(cr.Nombre,'') as CuentaNombre
                    FROM ComprobantesFactura cf
                    LEFT JOIN CuentasReceptoras cr ON cr.Id = cf.IdCuentaReceptora
                    WHERE cf.NumeroF = $nf
                    ORDER BY cf.Fecha";
                cmd.Parameters.AddWithValue("$nf", numeroF);
                var list = new List<object>();
                using var r = cmd.ExecuteReader();
                while (r.Read())
                    list.Add(new
                    {
                        id        = r.GetInt32(0),
                        metodo    = Db.S(r, 1),
                        monto     = Db.D(r, 2),
                        referencia= Db.S(r, 3),
                        idCuenta  = r.GetInt32(4),
                        fecha     = Db.S(r, 5),
                        obs       = Db.S(r, 6),
                        cuenta    = Db.S(r, 7)
                    });
                return Results.Ok(list);
            }).RequireAuthorization();
        }
    }

    // DTO mínimo
    public record EstadoRequest(string Estado);
}
