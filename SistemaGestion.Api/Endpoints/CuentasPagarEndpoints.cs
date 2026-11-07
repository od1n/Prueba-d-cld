// CuentasPagarEndpoints.cs — API REST de Cuentas por Pagar (pagos a proveedores)
// GET  /api/cxp?estado=&q=           lista (estado: Abiertas|Pendiente|Parcial|Vencida|Pagada|Todas)
// GET  /api/cxp/{id}                 detalle + historial de pagos
// POST /api/cxp/{id}/pagos           registra un pago (multipart: campos + comprobante)
// GET  /api/cxp/pagos/{idPago}/comprobante
// Mismas reglas que la app de escritorio (DatabaseService.RegistrarPagoCxP):
// suma lo pagado, recalcula el estado y sincroniza el egreso ligado.
using System.Globalization;
using SistemaGestion.Api.Auth;

namespace SistemaGestion.Api.Endpoints
{
    public static class CuentasPagarEndpoints
    {
        public static void MapCuentasPagarEndpoints(this WebApplication app)
        {
            // ── GET /api/cxp ────────────────────────────────────────────────
            app.MapGet("/api/cxp", (string? estado, string? q) =>
            {
                string hoy = DateTime.Today.ToString("yyyy-MM-dd");
                using var c = Db.Conn();
                var cmd = c.CreateCommand();
                var where = new List<string>();
                switch ((estado ?? "Abiertas").Trim())
                {
                    case "Todas": break;
                    case "Pendiente": where.Add("COALESCE(Estado,'Pendiente') = 'Pendiente'"); break;
                    case "Parcial":   where.Add("Estado = 'Parcial'"); break;
                    case "Pagada":    where.Add("Estado = 'Pagada'"); break;
                    case "Vencida":
                        where.Add("COALESCE(Estado,'Pendiente') <> 'Pagada' AND substr(FechaVence,1,10) < $hoy");
                        break;
                    default:          where.Add("COALESCE(Estado,'Pendiente') <> 'Pagada'"); break;
                }
                if (!string.IsNullOrWhiteSpace(q))
                {
                    where.Add(@"(COALESCE(Proveedor,'') LIKE $q OR COALESCE(CodigoProveedor,'') LIKE $q
                                 OR COALESCE(Concepto,'') LIKE $q OR COALESCE(NumFactura,'') LIKE $q)");
                    cmd.Parameters.AddWithValue("$q", "%" + q.Trim() + "%");
                }
                cmd.Parameters.AddWithValue("$hoy", hoy);
                cmd.CommandText = @"
                    SELECT Id, COALESCE(CodigoProveedor,''), COALESCE(Proveedor,''), COALESCE(Concepto,''),
                           COALESCE(NumFactura,''), COALESCE(Monto,0), COALESCE(Pagado,0),
                           COALESCE(FechaEmision,''), COALESCE(FechaVence,''),
                           COALESCE(Estado,'Pendiente'), COALESCE(Observaciones,''), COALESCE(IdEgreso,0)
                    FROM CuentasPagar"
                    + (where.Count > 0 ? " WHERE " + string.Join(" AND ", where) : "") + @"
                    ORDER BY CASE WHEN COALESCE(Estado,'Pendiente') = 'Pagada' THEN 1 ELSE 0 END,
                             FechaVence, Id
                    LIMIT 1000";

                var list = new List<object>();
                decimal totalSaldo = 0, totalVencido = 0;
                using var r = cmd.ExecuteReader();
                while (r.Read())
                {
                    decimal monto = Db.D(r, 5), pagado = Db.D(r, 6);
                    decimal saldo = Math.Max(0, monto - pagado);
                    string est = Db.S(r, 9);
                    string vence = Db.S(r, 8);
                    bool vencida = est != "Pagada" && vence.Length >= 10
                                   && string.CompareOrdinal(vence[..10], hoy) < 0;
                    if (est != "Pagada") { totalSaldo += saldo; if (vencida) totalVencido += saldo; }
                    list.Add(new
                    {
                        id              = r.GetInt32(0),
                        codigoProveedor = Db.S(r, 1),
                        proveedor       = Db.S(r, 2),
                        concepto        = Db.S(r, 3),
                        numFactura      = Db.S(r, 4),
                        monto, pagado, saldo,
                        fechaEmision    = Corta(Db.S(r, 7)),
                        fechaVence      = Corta(vence),
                        estado          = est,
                        vencida,
                        observaciones   = Db.S(r, 10),
                        idEgreso        = r.GetInt32(11)
                    });
                }
                return Results.Ok(new { cuentas = list, totalSaldo, totalVencido });
            }).AddEndpointFilter(RequireRole.Of(TokenService.PuedeEscribirEgresos)).RequireAuthorization();

            // ── GET /api/cxp/{id} ───────────────────────────────────────────
            app.MapGet("/api/cxp/{id:int}", (int id) =>
            {
                using var c = Db.Conn();
                var cmd = c.CreateCommand();
                cmd.CommandText = @"
                    SELECT Id, COALESCE(CodigoProveedor,''), COALESCE(Proveedor,''), COALESCE(Concepto,''),
                           COALESCE(NumFactura,''), COALESCE(Monto,0), COALESCE(Pagado,0),
                           COALESCE(FechaEmision,''), COALESCE(FechaVence,''),
                           COALESCE(Estado,'Pendiente'), COALESCE(Observaciones,''), COALESCE(IdEgreso,0)
                    FROM CuentasPagar WHERE Id = $id";
                cmd.Parameters.AddWithValue("$id", id);
                object? cuenta = null;
                using (var r = cmd.ExecuteReader())
                {
                    if (r.Read())
                    {
                        decimal monto = Db.D(r, 5), pagado = Db.D(r, 6);
                        cuenta = new
                        {
                            id = r.GetInt32(0), codigoProveedor = Db.S(r, 1), proveedor = Db.S(r, 2),
                            concepto = Db.S(r, 3), numFactura = Db.S(r, 4),
                            monto, pagado, saldo = Math.Max(0, monto - pagado),
                            fechaEmision = Corta(Db.S(r, 7)), fechaVence = Corta(Db.S(r, 8)),
                            estado = Db.S(r, 9), observaciones = Db.S(r, 10), idEgreso = r.GetInt32(11)
                        };
                    }
                }
                if (cuenta == null) return Results.NotFound(new { error = "Cuenta por pagar no encontrada." });

                var pagos = new List<object>();
                var cp = c.CreateCommand();
                cp.CommandText = @"
                    SELECT Id, Fecha, COALESCE(Monto,0), COALESCE(FormaPago,''), COALESCE(Referencia,''),
                           COALESCE(Observaciones,''), COALESCE(RutaComprobante,'')
                    FROM PagosCxP WHERE IdCuenta = $id ORDER BY Fecha DESC, Id DESC";
                cp.Parameters.AddWithValue("$id", id);
                using (var r = cp.ExecuteReader())
                    while (r.Read())
                        pagos.Add(new
                        {
                            id = r.GetInt32(0), fecha = Corta(Db.S(r, 1)), monto = Db.D(r, 2),
                            formaPago = Db.S(r, 3), referencia = Db.S(r, 4), observaciones = Db.S(r, 5),
                            tieneComprobante = Db.S(r, 6) != ""
                        });
                return Results.Ok(new { cuenta, pagos });
            }).AddEndpointFilter(RequireRole.Of(TokenService.PuedeEscribirEgresos)).RequireAuthorization();

            // ── POST /api/cxp/{id}/pagos (multipart) ────────────────────────
            app.MapPost("/api/cxp/{id:int}/pagos", async (HttpContext ctx, int id) =>
            {
                var form = await ctx.Request.ReadFormAsync();
                string fechaStr   = form["fecha"].ToString().Trim();
                string montoStr   = form["monto"].ToString().Trim();
                string formaPago  = form["formaPago"].ToString().Trim();
                string referencia = form["referencia"].ToString().Trim();
                string obs        = form["observaciones"].ToString().Trim();

                if (!decimal.TryParse(montoStr, NumberStyles.Number, CultureInfo.InvariantCulture, out decimal montoPago)
                    || montoPago <= 0)
                    return Results.BadRequest(new { error = "Monto inválido. Escribe un número mayor que cero." });
                montoPago = Math.Round(montoPago, 2);
                if (!DateTime.TryParseExact(fechaStr, "yyyy-MM-dd", CultureInfo.InvariantCulture,
                        DateTimeStyles.None, out DateTime fecha))
                    fecha = DateTime.Today;
                if (fecha.Date > DateTime.Today.AddDays(1))
                    return Results.BadRequest(new { error = "La fecha del pago no puede ser futura." });
                if (string.IsNullOrWhiteSpace(formaPago)) formaPago = "Efectivo";

                // Comprobante (opcional)
                var archivo = form.Files.GetFile("comprobante");
                string rutaComprobante = "";
                if (archivo != null && archivo.Length > 0)
                {
                    if (archivo.Length > 15 * 1024 * 1024)
                        return Results.BadRequest(new { error = "El comprobante es demasiado grande (máx 15 MB)." });
                    string ext = Path.GetExtension(archivo.FileName).ToLowerInvariant();
                    if (!new[] { ".jpg", ".jpeg", ".png", ".webp", ".pdf", ".bmp", ".heic" }.Contains(ext))
                        return Results.BadRequest(new { error = "El comprobante debe ser una foto (JPG, PNG) o un PDF." });
                    var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                    var carpeta = Path.Combine(appData, "SistemaGestion", "comprobantes", "cxp",
                        DateTime.Today.ToString("yyyy-MM"));
                    Directory.CreateDirectory(carpeta);
                    rutaComprobante = Path.GetFullPath(Path.Combine(carpeta,
                        $"cxp{id}_" + DateTime.Now.ToString("yyyyMMdd_HHmmss_fff") + ext));
                    using var fs = new FileStream(rutaComprobante, FileMode.Create);
                    await archivo.CopyToAsync(fs);
                }

                string usuario = TokenService.ObtenerNombreUsuario(ctx.User);
                using var c = Db.Conn();
                using var tx = c.BeginTransaction();
                try
                {
                    decimal monto, pagadoPrevio; int idEgreso; string estadoActual, proveedor;
                    using (var q = c.CreateCommand())
                    {
                        q.Transaction = tx;
                        q.CommandText = @"SELECT COALESCE(Monto,0), COALESCE(Pagado,0), COALESCE(IdEgreso,0),
                                                 COALESCE(Estado,'Pendiente'), COALESCE(Proveedor,'')
                                          FROM CuentasPagar WHERE Id = $id";
                        q.Parameters.AddWithValue("$id", id);
                        using var r = q.ExecuteReader();
                        if (!r.Read())
                        {
                            BorrarArchivo(rutaComprobante);
                            return Results.NotFound(new { error = "Cuenta por pagar no encontrada." });
                        }
                        monto = Db.D(r, 0); pagadoPrevio = Db.D(r, 1); idEgreso = r.GetInt32(2);
                        estadoActual = Db.S(r, 3); proveedor = Db.S(r, 4);
                    }

                    decimal saldo = Math.Round(monto - pagadoPrevio, 2);
                    if (estadoActual == "Pagada" || saldo <= 0.01m)
                    {
                        BorrarArchivo(rutaComprobante);
                        return Results.BadRequest(new { error = "Esta cuenta ya está pagada por completo." });
                    }
                    if (montoPago > saldo + 0.01m)
                    {
                        BorrarArchivo(rutaComprobante);
                        return Results.BadRequest(new
                        {
                            error = $"El monto (${montoPago:N2}) es mayor que el saldo pendiente (${saldo:N2}).",
                            saldo
                        });
                    }

                    // 1) Pago
                    using (var ins = c.CreateCommand())
                    {
                        ins.Transaction = tx;
                        ins.CommandText = @"INSERT INTO PagosCxP
                            (IdCuenta, Fecha, Monto, FormaPago, Referencia, Observaciones, RutaComprobante)
                            VALUES ($ic, $fe, $mo, $fp, $re, $ob, $rc)";
                        ins.Parameters.AddWithValue("$ic", id);
                        ins.Parameters.AddWithValue("$fe", fecha.ToString("yyyy-MM-dd"));
                        ins.Parameters.AddWithValue("$mo", (double)montoPago);
                        ins.Parameters.AddWithValue("$fp", formaPago);
                        ins.Parameters.AddWithValue("$re", referencia);
                        ins.Parameters.AddWithValue("$ob", string.IsNullOrEmpty(obs) ? $"Registrado en el portal por {usuario}" : obs);
                        ins.Parameters.AddWithValue("$rc", rutaComprobante);
                        ins.ExecuteNonQuery();
                    }
                    long idPago;
                    using (var li = c.CreateCommand()) { li.Transaction = tx; li.CommandText = "SELECT last_insert_rowid()"; idPago = (long)li.ExecuteScalar()!; }

                    // 2) Saldo y estado de la cuenta
                    decimal pagado = pagadoPrevio + montoPago;
                    string nuevoEstado = pagado >= monto - 0.01m ? "Pagada" : pagado > 0 ? "Parcial" : "Pendiente";
                    using (var up = c.CreateCommand())
                    {
                        up.Transaction = tx;
                        up.CommandText = "UPDATE CuentasPagar SET Pagado = $pa, Estado = $es WHERE Id = $id";
                        up.Parameters.AddWithValue("$pa", (double)pagado);
                        up.Parameters.AddWithValue("$es", nuevoEstado);
                        up.Parameters.AddWithValue("$id", id);
                        up.ExecuteNonQuery();
                    }

                    // 3) Egreso ligado (si la cuenta nació de un egreso a crédito)
                    if (idEgreso > 0)
                    {
                        decimal montoEgr = 0; string estadoEgr = "";
                        using (var qe = c.CreateCommand())
                        {
                            qe.Transaction = tx;
                            qe.CommandText = "SELECT COALESCE(MontoUSD,0), COALESCE(Estado,'') FROM Gastos WHERE Id = $id";
                            qe.Parameters.AddWithValue("$id", idEgreso);
                            using var re = qe.ExecuteReader();
                            if (re.Read()) { montoEgr = Db.D(re, 0); estadoEgr = Db.S(re, 1); }
                        }
                        if (estadoEgr != "Anulado" && montoEgr > 0)
                        {
                            decimal pagadoEgr = Math.Min(pagado, montoEgr);
                            bool completo = pagadoEgr >= montoEgr - 0.01m;
                            using var ue = c.CreateCommand();
                            ue.Transaction = tx;
                            ue.CommandText = completo
                                ? @"UPDATE Gastos SET PagadoUSD = $pa, Estado = 'Pagado',
                                       FechaPago = COALESCE(FechaPago, $fp), FechaVencimiento = NULL
                                    WHERE Id = $id"
                                : "UPDATE Gastos SET PagadoUSD = $pa, Estado = 'Pendiente' WHERE Id = $id";
                            ue.Parameters.AddWithValue("$pa", (double)pagadoEgr);
                            if (completo) ue.Parameters.AddWithValue("$fp", fecha.ToString("yyyy-MM-dd"));
                            ue.Parameters.AddWithValue("$id", idEgreso);
                            ue.ExecuteNonQuery();
                        }
                    }

                    // 4) Auditoría
                    using (var au = c.CreateCommand())
                    {
                        au.Transaction = tx;
                        au.CommandText = @"INSERT INTO LogAuditoria (FechaHora, Usuario, Accion, Tabla, Detalle)
                                           VALUES ($fh, $us, 'CXP_PAGO', 'CuentasPagar', $de)";
                        au.Parameters.AddWithValue("$fh", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                        au.Parameters.AddWithValue("$us", string.IsNullOrEmpty(usuario) ? "API" : usuario);
                        au.Parameters.AddWithValue("$de",
                            $"Id={id} — Pago ${montoPago:N2} ({formaPago}) desde el portal — Estado: {nuevoEstado}"
                            + (idEgreso > 0 ? $" (egreso #{idEgreso} sincronizado)" : ""));
                        au.ExecuteNonQuery();
                    }

                    tx.Commit();
                    return Results.Ok(new
                    {
                        idPago, estado = nuevoEstado, pagado,
                        saldo = Math.Max(0, monto - pagado),
                        mensaje = nuevoEstado == "Pagada"
                            ? $"Pago registrado. La cuenta de {proveedor} quedó pagada por completo."
                            : $"Pago registrado. Saldo pendiente: ${Math.Max(0, monto - pagado):N2}."
                    });
                }
                catch (Exception ex)
                {
                    try { tx.Rollback(); } catch { }
                    BorrarArchivo(rutaComprobante);
                    return Results.Problem("No se pudo registrar el pago: " + ex.Message);
                }
            }).AddEndpointFilter(RequireRole.Of(TokenService.PuedeEscribirEgresos)).RequireAuthorization()
              .DisableAntiforgery();

            // ── GET /api/cxp/pagos/{idPago}/comprobante ─────────────────────
            app.MapGet("/api/cxp/pagos/{idPago:int}/comprobante", (int idPago) =>
            {
                using var c = Db.Conn();
                var cmd = c.CreateCommand();
                cmd.CommandText = "SELECT COALESCE(RutaComprobante,'') FROM PagosCxP WHERE Id = $id";
                cmd.Parameters.AddWithValue("$id", idPago);
                var ruta = cmd.ExecuteScalar()?.ToString() ?? "";
                if (string.IsNullOrEmpty(ruta) || !File.Exists(ruta))
                    return Results.NotFound(new { error = "Comprobante no encontrado." });
                string mime = Path.GetExtension(ruta).ToLowerInvariant() switch
                {
                    ".jpg" or ".jpeg" => "image/jpeg",
                    ".png"  => "image/png",
                    ".webp" => "image/webp",
                    ".pdf"  => "application/pdf",
                    ".bmp"  => "image/bmp",
                    _       => "application/octet-stream"
                };
                return Results.File(ruta, mime);
            }).AddEndpointFilter(RequireRole.Of(TokenService.PuedeEscribirEgresos)).RequireAuthorization();
        }

        private static string Corta(string f) => f.Length >= 10 ? f[..10] : f;

        private static void BorrarArchivo(string ruta)
        {
            try { if (!string.IsNullOrEmpty(ruta) && File.Exists(ruta)) File.Delete(ruta); } catch { }
        }
    }
}
