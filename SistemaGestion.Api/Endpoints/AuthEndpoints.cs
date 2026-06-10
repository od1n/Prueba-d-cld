using System.Security.Cryptography;
using System.Text;
using SistemaGestion.Api.Auth;

namespace SistemaGestion.Api.Endpoints
{
    public static class AuthEndpoints
    {
        public static void MapAuthEndpoints(this WebApplication app)
        {
            app.MapPost("/api/login", (LoginRequest req, HttpContext ctx) =>
            {
                // Pre-validación + cota mínima de tiempo para reducir oráculos
                // de timing entre "usuario inexistente" / "contraseña errónea".
                var sw = System.Diagnostics.Stopwatch.StartNew();

                if (string.IsNullOrWhiteSpace(req.Usuario) || string.IsNullOrWhiteSpace(req.Contrasena))
                    return Results.BadRequest(new { error = "Usuario y contraseña requeridos." });
                if (req.Usuario.Length > 50 || req.Contrasena.Length > 200)
                    return Results.BadRequest(new { error = "Datos inválidos." });

                try
                {
                    using var c = Db.Conn();

                    // Verificar bloqueo
                    var cmdB = c.CreateCommand();
                    cmdB.CommandText = @"SELECT Id, NombreUsuario, NombreCompleto, PasswordHash, Rol,
                        Activo, IntentosHoy, BloqueadoHasta
                        FROM Usuarios WHERE NombreUsuario=$u AND Activo=1";
                    cmdB.Parameters.AddWithValue("$u", req.Usuario.Trim().ToLower());
                    using var r = cmdB.ExecuteReader();
                    if (!r.Read())
                    {
                        // Calcular un PBKDF2 dummy para igualar tiempos con
                        // el path "usuario válido + contraseña errónea".
                        VerificarPassword(req.Contrasena,
                            "v2:00000000000000000000000000000000:0000000000000000000000000000000000000000000000000000000000000000");
                        EsperarMinimo(sw, 250);
                        return Results.Unauthorized();
                    }

                    int    id       = r.GetInt32(0);
                    string nombre   = Db.S(r, 1);
                    string nomComp  = Db.S(r, 2);
                    string hash     = Db.S(r, 3);
                    string rol      = Db.S(r, 4);
                    int    intentos = Db.I(r, 6);
                    string bloqStr  = Db.S(r, 7);
                    r.Close();

                    // Verificar bloqueo temporal
                    if (!string.IsNullOrEmpty(bloqStr) &&
                        DateTime.TryParse(bloqStr, out var bloqHasta) &&
                        bloqHasta > DateTime.Now)
                    {
                        var mins = (int)(bloqHasta - DateTime.Now).TotalMinutes + 1;
                        return Results.BadRequest(new { error = $"Cuenta bloqueada. Intente en {mins} minuto(s)." });
                    }

                    // Verificar contraseña (PBKDF2 v2: o SHA-256 legacy)
                    bool ok = VerificarPassword(req.Contrasena, hash);
                    if (!ok)
                    {
                        // Incrementar intentos
                        int nuevosIntentos = intentos + 1;
                        string? nuevoBloq = nuevosIntentos >= 5
                            ? DateTime.Now.AddMinutes(30).ToString("yyyy-MM-dd HH:mm:ss")
                            : null;
                        var upd = c.CreateCommand();
                        upd.CommandText = nuevoBloq != null
                            ? "UPDATE Usuarios SET IntentosHoy=$i, BloqueadoHasta=$b WHERE Id=$id"
                            : "UPDATE Usuarios SET IntentosHoy=$i WHERE Id=$id";
                        upd.Parameters.AddWithValue("$i", nuevosIntentos);
                        if (nuevoBloq != null) upd.Parameters.AddWithValue("$b", nuevoBloq);
                        upd.Parameters.AddWithValue("$id", id);
                        upd.ExecuteNonQuery();
                        RegistrarAuditoria(c, "LOGIN_FALLIDO_API", "Usuarios",
                            $"Intento fallido portal móvil: {nombre[0]}***");
                        return Results.Unauthorized();
                    }

                    // Login exitoso — resetear intentos
                    var reset = c.CreateCommand();
                    reset.CommandText = "UPDATE Usuarios SET IntentosHoy=0, BloqueadoHasta=NULL, UltimoAcceso=$ua WHERE Id=$id";
                    reset.Parameters.AddWithValue("$ua", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                    reset.Parameters.AddWithValue("$id", id);
                    reset.ExecuteNonQuery();

                    // Migrar hash legacy SHA-256 → PBKDF2 (v2:) automáticamente
                    if (!hash.StartsWith("v2:"))
                    {
                        try
                        {
                            var salt = RandomNumberGenerator.GetBytes(16);
                            using var pbkdf2 = new Rfc2898DeriveBytes(req.Contrasena, salt, 600_000, HashAlgorithmName.SHA256);
                            var nuevoHash = $"v2:{Convert.ToHexString(salt).ToLower()}:{Convert.ToHexString(pbkdf2.GetBytes(32)).ToLower()}";
                            var updHash = c.CreateCommand();
                            updHash.CommandText = "UPDATE Usuarios SET PasswordHash=$h WHERE Id=$id";
                            updHash.Parameters.AddWithValue("$h", nuevoHash);
                            updHash.Parameters.AddWithValue("$id", id);
                            updHash.ExecuteNonQuery();
                            Console.WriteLine($"[AUTH] Hash migrado a PBKDF2 para usuario: {nombre}");
                        }
                        catch (Exception exH) { Console.WriteLine($"[AUTH] WARN migración hash: {exH.Message}"); }
                    }

                    RegistrarAuditoria(c, "LOGIN_MOVIL", "Usuarios", $"Login portal móvil: {nombre} ({rol})");

                    var (token, jti) = TokenService.GenerarToken(id, nombre, nomComp, rol);
                    var ip = ctx.Connection.RemoteIpAddress?.ToString();
                    SesionesActivas.Registrar(id, nombre, jti, ip, DateTime.UtcNow.AddHours(8));

                    ctx.Response.Cookies.Append("sg_token", token, new CookieOptions
                    {
                        HttpOnly  = true,
                        SameSite  = SameSiteMode.Strict,
                        Secure    = ctx.Request.IsHttps,        // se activa solo donde aplica
                        Expires   = DateTimeOffset.UtcNow.AddHours(8),
                        Path      = "/",
                        IsEssential = true
                    });

                    EsperarMinimo(sw, 200);
                    // ⚠ Ya NO devolvemos el token en el cuerpo: vivirá únicamente
                    // en la cookie HttpOnly para que XSS no pueda exfiltrarlo.
                    return Results.Ok(new { nombreUsuario = nombre, nombreCompleto = nomComp, rol });
                }
                catch (Exception ex)
                {
                    // No exponemos detalles del error al cliente — sólo a la auditoría.
                    try { using var c2 = Db.Conn();
                        RegistrarAuditoria(c2, "LOGIN_ERROR_API", "Usuarios", ex.GetType().Name); }
                    catch { }
                    return Results.Problem("Error de autenticación.", statusCode: 500);
                }
            }).RequireRateLimiting("login");

            // Helper: garantiza un mínimo de tiempo de respuesta para mitigar
            // ataques de enumeración por timing.
            static void EsperarMinimo(System.Diagnostics.Stopwatch sw, int ms)
            {
                var resta = ms - (int)sw.ElapsedMilliseconds;
                if (resta > 0) System.Threading.Thread.Sleep(resta);
            }

            app.MapPost("/api/logout", (HttpContext ctx) =>
            {
                // Revocar el jti del token actual antes de borrar la cookie.
                var jti = TokenService.ObtenerJti(ctx.User);
                if (!string.IsNullOrEmpty(jti)) SesionesActivas.Revocar(jti);

                ctx.Response.Cookies.Delete("sg_token", new CookieOptions
                {
                    HttpOnly = true, SameSite = SameSiteMode.Strict,
                    Secure   = ctx.Request.IsHttps, Path = "/"
                });
                return Results.Ok(new { mensaje = "Sesión cerrada." });
            }).RequireAuthorization();

            app.MapGet("/api/me", (HttpContext ctx) =>
            {
                var u = ctx.User;
                return Results.Ok(new
                {
                    nombreUsuario        = TokenService.ObtenerNombreUsuario(u),
                    rol                  = TokenService.ObtenerRol(u),
                    esAdmin              = TokenService.EsAdmin(u),
                    puedeEscribirKardex  = TokenService.PuedeEscribirKardex(u),
                    puedeAjustarKardex   = TokenService.PuedeAjustarKardex(u),
                    puedeVerCxC          = TokenService.PuedeVerCxC(u)
                });
            }).RequireAuthorization();
        }

        // ── Verificar contraseña PBKDF2 (v2:SALT:HASH) o SHA-256 legacy ──
        private static bool VerificarPassword(string password, string hashAlmacenado)
        {
            if (hashAlmacenado.StartsWith("v2:"))
            {
                var partes = hashAlmacenado.Split(':');
                if (partes.Length != 3) return false;
                var salt     = Convert.FromHexString(partes[1]);
                var hashEsp  = Convert.FromHexString(partes[2]);
                using var pbkdf2 = new Rfc2898DeriveBytes(password, salt, 600_000, HashAlgorithmName.SHA256);
                var hashCalc = pbkdf2.GetBytes(32);
                return CryptographicOperations.FixedTimeEquals(hashCalc, hashEsp);
            }
            else
            {
                // Legacy SHA-256
                using var sha = SHA256.Create();
                var bytes = Encoding.UTF8.GetBytes(password + "SG_SALT_2024");
                var hash  = Convert.ToHexString(sha.ComputeHash(bytes)).ToLower();
                // Comparación constant-time para evitar timing attacks
                return CryptographicOperations.FixedTimeEquals(
                    Encoding.UTF8.GetBytes(hash),
                    Encoding.UTF8.GetBytes(hashAlmacenado));
            }
        }

        internal static void RegistrarAuditoria(Microsoft.Data.Sqlite.SqliteConnection c,
            string accion, string tabla, string detalle)
        {
            try
            {
                var cmd = c.CreateCommand();
                cmd.CommandText = @"INSERT INTO LogAuditoria (FechaHora,Usuario,Accion,Tabla,Detalle)
                    VALUES ($fh,'API',$ac,$ta,$de)";
                cmd.Parameters.AddWithValue("$fh", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                cmd.Parameters.AddWithValue("$ac", accion);
                cmd.Parameters.AddWithValue("$ta", tabla);
                cmd.Parameters.AddWithValue("$de", detalle);
                cmd.ExecuteNonQuery();
            }
            catch { }
        }
    }

    public record LoginRequest(string Usuario, string Contrasena);
}
