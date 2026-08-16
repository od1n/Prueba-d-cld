using Microsoft.AspNetCore.RateLimiting;
using System.Net;
using System.Net.Mail;

// WhatsAppEndpoints.cs — Endpoints para el bot de WhatsApp (n8n + Evolution API)
// Auth: X-Internal-Token (mismo que digitalizador), sin JWT.
// Tráfico llega via Cloudflare Tunnel → cloudflared → localhost (EsLan pasa).
//
// GET  /api/wa/catalogo?q=texto          → catálogo de productos (búsqueda)
// GET  /api/wa/producto/{codigo}         → detalle de un producto
// POST /api/wa/validar-pin              → valida PIN de cliente, devuelve info
// GET  /api/wa/estado-cuenta?pin=&tel=  → estado de cuenta del cliente
// GET  /api/wa/abonos?pin=&tel=         → historial de abonos del cliente
// GET  /api/wa/pedido?nombre=&tel=&items=&wa= → registra pedido y envía email

namespace SistemaGestion.Api.Endpoints
{
    public static class WhatsAppEndpoints
    {
        private static readonly string TOKEN =
            Environment.GetEnvironmentVariable("SG_INTERNAL_TOKEN") ?? "";

        /// <summary>Valida el header X-Internal-Token. Devuelve true si es válido.</summary>
        private static bool TokenValido(HttpContext ctx)
        {
            if (string.IsNullOrEmpty(TOKEN)) return false;
            var header = ctx.Request.Headers["X-Internal-Token"].FirstOrDefault();
            return !string.IsNullOrEmpty(header)
                && string.Equals(header, TOKEN, StringComparison.Ordinal);
        }

        public static void MapWhatsAppEndpoints(this WebApplication app)
        {
            // ── 1. Catálogo de productos ────────────────────────────────────
            // GET /api/wa/catalogo?q=tubo
            // Devuelve hasta 30 productos activos con precio > 0.
            app.MapGet("/api/wa/catalogo", (HttpContext ctx, string? q) =>
            {
                if (!TokenValido(ctx))
                    return Results.Json(new { error = "No autorizado." }, statusCode: 401);

                using var c   = Db.Conn();
                using var cmd = c.CreateCommand();

                if (!string.IsNullOrWhiteSpace(q))
                {
                    cmd.CommandText = @"
                        SELECT Codigo, Descripcion, Unidad, Precio
                        FROM Inventario
                        WHERE Activo = 1 AND Precio > 0
                          AND (Codigo LIKE $q OR Descripcion LIKE $q)
                        ORDER BY Codigo
                        LIMIT 30";
                    cmd.Parameters.AddWithValue("$q", $"%{q.Trim()}%");
                }
                else
                {
                    cmd.CommandText = @"
                        SELECT Codigo, Descripcion, Unidad, Precio
                        FROM Inventario
                        WHERE Activo = 1 AND Precio > 0
                        ORDER BY Codigo
                        LIMIT 30";
                }

                var lista = new List<object>();
                using var r = cmd.ExecuteReader();
                while (r.Read())
                {
                    lista.Add(new
                    {
                        codigo      = Db.S(r, 0),
                        descripcion = Db.S(r, 1),
                        unidad      = Db.S(r, 2),
                        precio      = Db.D(r, 3)
                    });
                }
                return Results.Ok(new { total = lista.Count, productos = lista });
            });

            // ── 2. Detalle de producto ──────────────────────────────────────
            // GET /api/wa/producto/TUB001
            app.MapGet("/api/wa/producto/{codigo}", (HttpContext ctx, string codigo) =>
            {
                if (!TokenValido(ctx))
                    return Results.Json(new { error = "No autorizado." }, statusCode: 401);

                using var c   = Db.Conn();
                using var cmd = c.CreateCommand();
                cmd.CommandText = @"
                    SELECT Codigo, Descripcion, Departamento, Unidad, Precio
                    FROM Inventario
                    WHERE Codigo = $cod AND Activo = 1";
                cmd.Parameters.AddWithValue("$cod", codigo);
                using var r = cmd.ExecuteReader();
                if (!r.Read())
                    return Results.NotFound(new { error = "Producto no encontrado." });

                return Results.Ok(new
                {
                    codigo       = Db.S(r, 0),
                    descripcion  = Db.S(r, 1),
                    departamento = Db.S(r, 2),
                    unidad       = Db.S(r, 3),
                    precio       = Db.D(r, 4)
                });
            });

            // ── 3. Validar PIN de cliente ───────────────────────────────────
            // POST /api/wa/validar-pin   body: {"pin":"1234", "telefono":"0414..."}
            // Devuelve datos básicos del cliente si el PIN es correcto.
            app.MapPost("/api/wa/validar-pin", (HttpContext ctx, PinRequest req) =>
            {
                if (!TokenValido(ctx))
                    return Results.Json(new { error = "No autorizado." }, statusCode: 401);

                if (string.IsNullOrWhiteSpace(req.Pin))
                    return Results.BadRequest(new { error = "PIN requerido." });

                var codigoCliente = BuscarClientePorPin(req.Pin, req.Telefono);
                if (codigoCliente == null)
                    return Results.Json(new { valido = false, error = "PIN incorrecto o no asignado." },
                        statusCode: 200);

                // Obtener datos básicos del cliente
                using var c   = Db.Conn();
                using var cmd = c.CreateCommand();
                cmd.CommandText = "SELECT Descripcion, CiRif FROM Clientes WHERE Codigo = $cod";
                cmd.Parameters.AddWithValue("$cod", codigoCliente);
                using var r = cmd.ExecuteReader();
                if (!r.Read())
                    return Results.Json(new { valido = false, error = "Cliente no encontrado." },
                        statusCode: 200);

                return Results.Ok(new
                {
                    valido  = true,
                    codigo  = codigoCliente,
                    nombre  = Db.S(r, 0),
                    ciRif   = Db.S(r, 1)
                });
            }).RequireRateLimiting("wa-pin");

            // ── 4. Estado de cuenta ─────────────────────────────────────────
            // GET /api/wa/estado-cuenta?pin=1234&tel=0414...
            app.MapGet("/api/wa/estado-cuenta", (HttpContext ctx, string pin, string? tel) =>
            {
                if (!TokenValido(ctx))
                    return Results.Json(new { error = "No autorizado." }, statusCode: 401);

                if (string.IsNullOrWhiteSpace(pin))
                    return Results.BadRequest(new { error = "PIN requerido." });

                var codigoCliente = BuscarClientePorPin(pin, tel);
                if (codigoCliente == null)
                    return Results.Json(new { error = "PIN incorrecto." }, statusCode: 403);

                using var c = Db.Conn();

                // Datos del cliente
                string nombre = "", ciRif = "";
                decimal saldoFavor = 0;
                using (var cmd = c.CreateCommand())
                {
                    cmd.CommandText = "SELECT Descripcion, CiRif, COALESCE(AFavor,0) FROM Clientes WHERE Codigo = $cod";
                    cmd.Parameters.AddWithValue("$cod", codigoCliente);
                    using var r = cmd.ExecuteReader();
                    if (r.Read())
                    {
                        nombre     = Db.S(r, 0);
                        ciRif      = Db.S(r, 1);
                        saldoFavor = Db.D(r, 2);
                    }
                }

                // Cuentas por cobrar pendientes
                var facturas   = new List<object>();
                decimal totalDeuda = 0;
                var hoy = DateTime.Today.ToString("yyyy-MM-dd");
                using (var cmd = c.CreateCommand())
                {
                    cmd.CommandText = @"
                        SELECT Concepto, Monto, Pagado, FechaEmision, FechaVence, Estado
                        FROM CuentasCobrar
                        WHERE (Cliente = $cod OR Cliente = $nom) AND Estado != 'Saldada'
                        ORDER BY FechaVence";
                    cmd.Parameters.AddWithValue("$cod", codigoCliente);
                    cmd.Parameters.AddWithValue("$nom", nombre);
                    using var r = cmd.ExecuteReader();
                    while (r.Read())
                    {
                        decimal monto  = Db.D(r, 1);
                        decimal pagado = Db.D(r, 2);
                        decimal saldo  = monto - pagado;
                        string  estado = Db.S(r, 5);
                        string  fv     = Db.S(r, 4);
                        bool vencida   = estado != "Pagada" && string.Compare(fv, hoy) < 0 && saldo > 0;
                        totalDeuda += saldo;
                        facturas.Add(new
                        {
                            concepto = Db.S(r, 0),
                            total    = monto,
                            pagado,
                            saldo,
                            vence    = fv,
                            estado,
                            vencida
                        });
                    }
                }

                // Total abonos confirmados
                decimal totalAbonos = 0;
                using (var cmd = c.CreateCommand())
                {
                    cmd.CommandText = @"
                        SELECT COALESCE(SUM(Monto), 0)
                        FROM Abonos
                        WHERE Cliente = $cod AND COALESCE(Estado,'Confirmado') = 'Confirmado'";
                    cmd.Parameters.AddWithValue("$cod", codigoCliente);
                    var val = cmd.ExecuteScalar();
                    if (val != null && val != DBNull.Value)
                        totalAbonos = (decimal)Convert.ToDouble(val);
                }

                return Results.Ok(new
                {
                    cliente = new { codigo = codigoCliente, nombre, ciRif },
                    saldoFavor,
                    totalAbonos,
                    totalDeuda,
                    facturasPendientes = facturas
                });
            });

            // ── 5. Historial de abonos ──────────────────────────────────────
            // GET /api/wa/abonos?pin=1234&tel=0414...&ultimos=10
            app.MapGet("/api/wa/abonos", (HttpContext ctx, string pin, string? tel, int? ultimos) =>
            {
                if (!TokenValido(ctx))
                    return Results.Json(new { error = "No autorizado." }, statusCode: 401);

                if (string.IsNullOrWhiteSpace(pin))
                    return Results.BadRequest(new { error = "PIN requerido." });

                var codigoCliente = BuscarClientePorPin(pin, tel);
                if (codigoCliente == null)
                    return Results.Json(new { error = "PIN incorrecto." }, statusCode: 403);

                int limite = Math.Clamp(ultimos ?? 20, 1, 100);

                using var c   = Db.Conn();
                using var cmd = c.CreateCommand();
                cmd.CommandText = @"
                    SELECT NumAbono, Fecha, Monto,
                           COALESCE(MetodoPago, 'Efectivo'),
                           COALESCE(Referencia, ''),
                           COALESCE(Estado, 'Confirmado')
                    FROM Abonos
                    WHERE Cliente = $cod
                    ORDER BY Fecha DESC
                    LIMIT $lim";
                cmd.Parameters.AddWithValue("$cod", codigoCliente);
                cmd.Parameters.AddWithValue("$lim", limite);

                var lista = new List<object>();
                using var r = cmd.ExecuteReader();
                while (r.Read())
                {
                    lista.Add(new
                    {
                        id         = r.GetInt32(0),
                        fecha      = Db.S(r, 1),
                        monto      = Db.D(r, 2),
                        metodo     = Db.S(r, 3),
                        referencia = Db.S(r, 4),
                        estado     = Db.S(r, 5)
                    });
                }
                return Results.Ok(new { total = lista.Count, abonos = lista });
            });

            // ── 6. Registrar pedido (envía email) ──────────────────────────────
            // GET /api/wa/pedido?nombre=Juan&tel=0414-123&items=2xTUB001,5xSLA002&wa=58414...
            app.MapGet("/api/wa/pedido", (HttpContext ctx, string nombre, string tel, string items, string? wa) =>
            {
                if (!TokenValido(ctx))
                    return Results.Json(new { error = "No autorizado." }, statusCode: 401);

                if (string.IsNullOrWhiteSpace(nombre) || string.IsNullOrWhiteSpace(items))
                    return Results.BadRequest(new { error = "Nombre y productos son requeridos." });

                var smtpUser = Environment.GetEnvironmentVariable("SG_SMTP_EMAIL") ?? "";
                var smtpPass = Environment.GetEnvironmentVariable("SG_SMTP_PASS") ?? "";
                var destino  = Environment.GetEnvironmentVariable("SG_PEDIDO_EMAIL") ?? "hatopascher@gmail.com";

                // Formatear cuerpo del email
                var ahora  = DateTime.Now.ToString("dd/MM/yyyy HH:mm");
                var cuerpo = $"""
                    NUEVO PEDIDO VÍA WHATSAPP
                    ========================
                    Fecha: {ahora}

                    Cliente: {nombre}
                    Teléfono: {tel}
                    WhatsApp: {wa ?? "N/A"}

                    Productos solicitados:
                    {items.Replace(",", "\n")}

                    ---
                    Pedido recibido automáticamente desde el bot de WhatsApp.
                    """;

                if (string.IsNullOrEmpty(smtpUser) || string.IsNullOrEmpty(smtpPass))
                {
                    // Sin SMTP configurado: loguear y retornar OK
                    Console.WriteLine($"[PEDIDO] Sin SMTP. Email pendiente:\n{cuerpo}");
                    return Results.Ok(new
                    {
                        ok      = true,
                        enviado = false,
                        mensaje = "Pedido registrado. Email pendiente (SMTP no configurado).",
                        nombre, tel, items
                    });
                }

                try
                {
                    using var msg = new MailMessage(smtpUser, destino)
                    {
                        Subject = $"Pedido WhatsApp — {nombre} ({ahora})",
                        Body    = cuerpo
                    };
                    using var smtp = new SmtpClient("smtp.gmail.com", 587)
                    {
                        Credentials           = new NetworkCredential(smtpUser, smtpPass),
                        EnableSsl              = true,
                        Timeout                = 10000
                    };
                    smtp.Send(msg);

                    return Results.Ok(new
                    {
                        ok      = true,
                        enviado = true,
                        mensaje = "Pedido enviado por email.",
                        nombre, tel, items
                    });
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[PEDIDO] Error SMTP: {ex.Message}");
                    return Results.Ok(new
                    {
                        ok      = true,
                        enviado = false,
                        mensaje = "Pedido registrado pero no se pudo enviar el email.",
                        nombre, tel, items
                    });
                }
            });
        }

        // ── Buscar cliente por PIN (replica lógica de DB_Abonos.ValidarPinBot) ──
        private static string? BuscarClientePorPin(string pin, string? telefono)
        {
            using var c   = Db.Conn();
            using var cmd = c.CreateCommand();
            cmd.CommandText = @"
                SELECT Codigo, Descripcion, Telefono, COALESCE(Telefono2,'')
                FROM Clientes
                WHERE PinBot = $pin AND PinBot != ''";
            cmd.Parameters.AddWithValue("$pin", pin);

            var candidatos = new List<(string cod, string nombre, string tel1, string tel2)>();
            using var r = cmd.ExecuteReader();
            while (r.Read())
                candidatos.Add((Db.S(r, 0), Db.S(r, 1), Db.S(r, 2), Db.S(r, 3)));

            if (candidatos.Count == 0) return null;
            if (candidatos.Count == 1) return candidatos[0].cod;

            // Desambiguar por teléfono si hay múltiples clientes con el mismo PIN
            if (!string.IsNullOrEmpty(telefono))
            {
                var limpio = telefono.Replace("+", "").Replace(" ", "").Replace("-", "");
                var match = candidatos.FirstOrDefault(c =>
                    c.tel1.Replace("+", "").Replace(" ", "").Replace("-", "").Contains(limpio) ||
                    c.tel2.Replace("+", "").Replace(" ", "").Replace("-", "").Contains(limpio) ||
                    c.nombre.ToUpper().Contains(telefono.ToUpper()));
                if (match.cod != null) return match.cod;
            }

            // Si no se puede desambiguar, devolver el primero
            return candidatos[0].cod;
        }
    }

    // ── DTOs ─────────────────────────────────────────────────────────────────
    record PinRequest(string Pin, string? Telefono);
}
