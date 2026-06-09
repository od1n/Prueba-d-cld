using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.IdentityModel.Tokens;
using SistemaGestion.Api;
using SistemaGestion.Api.Auth;
using SistemaGestion.Api.Endpoints;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

// ── HTTP + HTTPS ─────────────────────────────────────────────────────────
//   Por defecto exponemos AMBOS puertos:
//     5050 → HTTP (compatibilidad con instalaciones existentes)
//     5443 → HTTPS (cert auto-firmado generado y persistido por la app)
//   El middleware UseHttpsRedirection más abajo redirige automáticamente.
//   La cookie sg_token tiene Secure=true en HTTPS, canal seguro real.
//
//   Para usar tu propio cert PFX: variables de entorno
//     SG_HTTPS_PFX="C:\ruta\cert.pfx"
//     SG_HTTPS_PWD="password"
//
//   Si no hay PFX externo, CertService genera un cert auto-firmado en
//   %AppData%\SistemaGestion\sg-https.pfx (vida 5 años, reusado en
//   arranques posteriores). Sin dependencia de "dotnet dev-certs".
var certHttps = CertService.ObtenerCertificado();
Console.WriteLine($"[API] Cert HTTPS: {certHttps.Subject} (expira {certHttps.NotAfter:yyyy-MM-dd})");

builder.WebHost.ConfigureKestrel(opt =>
{
    opt.ListenAnyIP(5050, lo =>
    {
        // HTTP - solo HTTP/1.1 (h2c sin TLS no es estandar para navegadores)
        lo.Protocols = Microsoft.AspNetCore.Server.Kestrel.Core.HttpProtocols.Http1;
    });
    opt.ListenAnyIP(5443, lo =>
    {
        lo.UseHttps(certHttps);
        // CRITICO: PowerShell 5.1 (System.Net.WebRequest) no soporta HTTP/2.
        // Si Kestrel ofrece h2 via ALPN y el cliente solo habla h1, el
        // handshake aborta con "conexion terminada". Forzamos Http1 + Http2:
        // los navegadores modernos negocian h2, los clientes legacy h1.
        lo.Protocols = Microsoft.AspNetCore.Server.Kestrel.Core.HttpProtocols.Http1AndHttp2;
    });
});

builder.Services.AddHttpsRedirection(o =>
{
    o.HttpsPort = 5443;
    o.RedirectStatusCode = StatusCodes.Status308PermanentRedirect;
});

builder.Services.AddHsts(o =>
{
    o.Preload           = false;          // OK para LAN
    o.IncludeSubDomains = true;
    o.MaxAge            = TimeSpan.FromDays(180);
    // Por defecto ASP.NET excluye localhost/127.0.0.1/::1 del header HSTS
    // para no "lock-out" durante desarrollo. En LAN privada queremos HSTS
    // siempre, asi que limpiamos esa exclusion.
    o.ExcludedHosts.Clear();
});

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey         = TokenService.ObtenerClaveSeguridad(),
            ValidateIssuer           = true,
            ValidIssuer              = "SistemaGestion.Api",
            ValidateAudience         = true,
            ValidAudience            = "SistemaGestion.Portal",
            ValidateLifetime         = true,
            ClockSkew                = TimeSpan.FromSeconds(30)  // tolera desfaces leves
        };
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = ctx =>
            {
                // La cookie HttpOnly es la fuente principal del token.
                // El header Authorization queda como respaldo para clientes
                // de integración (no navegadores).
                if (ctx.Request.Cookies.TryGetValue("sg_token", out var t))
                    ctx.Token = t;
                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddAuthorization();

// CORS restrictivo: el portal corre en el mismo origen, así que sólo
// permitimos peticiones del propio host. Si un cliente externo de la red
// local lo necesita, se debe agregar explícitamente.
builder.Services.AddCors(o => o.AddDefaultPolicy(p =>
    p.SetIsOriginAllowed(origin =>
     {
         if (string.IsNullOrEmpty(origin)) return false;
         try
         {
             var uri = new Uri(origin);
             return uri.Host is "localhost" or "127.0.0.1"
                 || System.Net.IPAddress.TryParse(uri.Host, out var ip) && EsLan(ip);
         }
         catch { return false; }
     })
     .AllowAnyMethod()
     .AllowAnyHeader()
     .AllowCredentials()));

// Rate limiting para evitar fuerza bruta sobre /api/login (defensa adicional
// al bloqueo por usuario que ya hace AuthEndpoints).
// HttpClient para proxy hacia el digitalizador Python (:8080)
builder.Services.AddHttpClient("digitalizador", c =>
{
    c.Timeout = TimeSpan.FromSeconds(120); // extracción IA puede tardar
});

// HttpClient para proxy hacia Anthropic (asistente de ayuda)
builder.Services.AddHttpClient("anthropic", c =>
{
    c.Timeout = TimeSpan.FromSeconds(60);
    c.BaseAddress = new Uri("https://api.anthropic.com");
});

// Necesario para IFormFile en Minimal APIs (.DisableAntiforgery() en cada endpoint)
builder.Services.AddAntiforgery();

builder.Services.AddRateLimiter(options =>
{
    options.AddFixedWindowLimiter("login", o =>
    {
        o.PermitLimit = 10;                      // 10 intentos
        o.Window      = TimeSpan.FromMinutes(1); //   por minuto
        o.QueueLimit  = 0;
    });
    // Endpoint de extracción IA — costoso en créditos/GPU
    options.AddFixedWindowLimiter("ia-extraccion", o =>
    {
        o.PermitLimit = 10;                      // 10 llamadas
        o.Window      = TimeSpan.FromMinutes(1); //   por minuto
        o.QueueLimit  = 0;
    });
    // Asistente de ayuda Claude Opus — costoso en tokens
    options.AddFixedWindowLimiter("ayuda-opus", o =>
    {
        o.PermitLimit = 20;                       // 20 mensajes
        o.Window      = TimeSpan.FromMinutes(5);  //   cada 5 minutos
        o.QueueLimit  = 0;
    });
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
});

var app = builder.Build();

// ── Headers de seguridad ────────────────────────────────────────────────────
app.Use(async (ctx, next) =>
{
    var h = ctx.Response.Headers;
    h["X-Content-Type-Options"]   = "nosniff";
    h["X-Frame-Options"]          = "DENY";
    h["Referrer-Policy"]          = "strict-origin-when-cross-origin";
    h["Permissions-Policy"]       = "geolocation=(), microphone=(), camera=(self)";
    // CSP: el portal no carga scripts externos salvo Google Fonts (CSS).
    // Se permite 'unsafe-inline' temporalmente porque las páginas aún tienen
    // Todo el JS está en archivos externos — 'unsafe-inline' eliminado de script-src.
    h["Content-Security-Policy"]  =
        "default-src 'self'; " +
        "script-src 'self'; " +
        "style-src  'self' 'unsafe-inline' https://fonts.googleapis.com; " +
        "font-src   'self' https://fonts.gstatic.com; " +
        "img-src    'self' data: blob:; " +
        "media-src  'self' blob:; " +
        "connect-src 'self'; " +
        "frame-ancestors 'none'; " +
        "base-uri 'self';";
    await next();
});

// Restricción red local (IPv4 + IPv6 link-local / ULA + loopback)
app.Use(async (ctx, next) =>
{
    var ip = ctx.Connection.RemoteIpAddress;
    if (!EsLan(ip))
    {
        ctx.Response.StatusCode = StatusCodes.Status403Forbidden;
        await ctx.Response.WriteAsync("Acceso solo desde la red local.");
        return;
    }
    await next();
});

// HSTS + redirect HTTPS:
//   En entorno Test se omiten completamente.
//   En LAN el redirect se puede deshabilitar via variable de entorno
//   SG_HTTPS_REDIRECT=false para que los celulares Android puedan
//   conectarse por HTTP sin que el cert auto-firmado bloquee el handshake.
var httpsRedirect = !string.Equals(
    Environment.GetEnvironmentVariable("SG_HTTPS_REDIRECT"), "false",
    StringComparison.OrdinalIgnoreCase);

if (!app.Environment.IsEnvironment("Test") && httpsRedirect)
{
    app.UseHsts();
    try { app.UseHttpsRedirection(); }
    catch (Exception ex) { Console.WriteLine($"[API] HTTPS redirect deshabilitado: {ex.Message}"); }
}
else if (!httpsRedirect)
{
    Console.WriteLine("[API] HTTPS redirect desactivado (SG_HTTPS_REDIRECT=false).");
}

app.UseStaticFiles(new Microsoft.AspNetCore.Builder.StaticFileOptions
{
    OnPrepareResponse = ctx =>
    {
        // Cache largo para assets versionados; HTML siempre fresco.
        var path = ctx.File.PhysicalPath ?? "";
        if (path.EndsWith(".html"))
            ctx.Context.Response.Headers["Cache-Control"] = "no-store";
        else
            ctx.Context.Response.Headers["Cache-Control"] = "public, max-age=86400";
    }
});
app.UseCors();
// En entorno Test se omite el rate limiter para que los tests no se bloqueen entre sí.
if (!app.Environment.IsEnvironment("Test"))
    app.UseRateLimiter();
app.UseAuthentication();

// ── Validación de revocación (jti) ─────────────────────────────────────────
// Después de UseAuthentication el ctx.User ya tiene los claims. Si el token
// tiene jti y la BD no lo reconoce como válido (revocado o desconocido),
// devolvemos 401. Sólo aplica a usuarios identificados; los anónimos pasan
// para que los endpoints públicos sigan funcionando.
app.Use(async (ctx, next) =>
{
    if (ctx.User?.Identity?.IsAuthenticated == true)
    {
        var jti = TokenService.ObtenerJti(ctx.User);
        if (!string.IsNullOrEmpty(jti) && !SesionesActivas.EsValido(jti))
        {
            ctx.Response.Cookies.Delete("sg_token", new CookieOptions
            {
                HttpOnly = true, SameSite = SameSiteMode.Strict,
                Secure   = ctx.Request.IsHttps, Path = "/"
            });
            ctx.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await ctx.Response.WriteAsJsonAsync(new { error = "Sesión revocada." });
            return;
        }
    }
    await next();
});

app.UseAuthorization();

// Inicializar BD — misma ruta que el sistema de escritorio.
// Prioridad:
//   1) SG_DB_PATH  — ruta absoluta explícita (producción / red compartida)
//   2) SG_TEST_BD  — ruta temporal en tests automatizados
//   3) Default     — %AppData%\SistemaGestion\sistema_gestion.db
var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
var rutaBD  = Environment.GetEnvironmentVariable("SG_DB_PATH")
           ?? Environment.GetEnvironmentVariable("SG_TEST_BD")
           ?? Path.Combine(appData, "SistemaGestion", "sistema_gestion.db");
try
{
    Db.Inicializar(rutaBD);
    Console.WriteLine($"[API] BD conectada: {rutaBD}");

    // Asegurar tabla de sesiones activas (para revocación de tokens).
    Db.EnsureSesionesActivas();

    // Asegurar tabla de detalle de egresos (líneas de factura de compra).
    Db.EnsureDetallesGasto();

    // Asegurar tabla de configuración del portal (backend OCR, etc.).
    Db.EnsureConfiguracionPortal();

    // Limpieza inicial de sesiones expiradas hace > 24h.
    var limpiadas = SistemaGestion.Api.Auth.SesionesActivas.LimpiarExpiradas();
    if (limpiadas > 0)
        Console.WriteLine($"[API] {limpiadas} sesión(es) expirada(s) purgada(s).");

    // Marcar usuarios con hash SHA-256 legacy para que cambien
    // contraseña al próximo login. Idempotente.
    var n = Db.MarcarHashesLegacyParaMigracion();
    if (n > 0)
        Console.WriteLine($"[API] {n} usuario(s) con hash legacy → marcados para cambio obligatorio.");
}
catch (Exception ex)
{
    Console.WriteLine($"[API] ADVERTENCIA BD: {ex.Message}");
    Console.WriteLine("[API] La API arranca igual — la BD se conectará al primer request.");
}

app.MapAuthEndpoints();
app.MapDashboardEndpoints();
app.MapInventarioEndpoints();
app.MapKardexEndpoints();
app.MapClientesEndpoints();
app.MapFacturasEndpoints();
app.MapProduccionEndpoints();
app.MapAyudaEndpoints();
app.MapEgresosEndpoints();
app.MapComprasEndpoints();
app.MapReportesEndpoints();
app.MapNominaEndpoints();
app.MapGet("/", ctx => { ctx.Response.Redirect("/index.html"); return Task.CompletedTask; });

Console.WriteLine("[API] Portal móvil corriendo:");
Console.WriteLine("       HTTP  → http://0.0.0.0:5050  (redirige a HTTPS)");
Console.WriteLine("       HTTPS → https://0.0.0.0:5443");
app.Run();

// Helper compartido para verificación de origen LAN (IPv4 + IPv6).
static bool EsLan(System.Net.IPAddress? ip)
{
    if (ip is null) return true;
    if (System.Net.IPAddress.IsLoopback(ip)) return true;

    // Kestrel en modo dual-stack recibe IPv4 del celular como ::ffff:192.168.x.x
    // (IPv4-mapped IPv6). Hay que desempaquetarla antes de validar.
    if (ip.IsIPv4MappedToIPv6)
        return EsLan(ip.MapToIPv4());

    if (ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
    {
        var b = ip.GetAddressBytes();
        return b[0] == 10
            || (b[0] == 172 && b[1] >= 16 && b[1] <= 31)
            || (b[0] == 192 && b[1] == 168)
            || (b[0] == 169 && b[1] == 254); // APIPA / link-local
    }
    if (ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetworkV6)
    {
        return ip.IsIPv6LinkLocal
            || ip.IsIPv6SiteLocal
            || ip.GetAddressBytes()[0] == 0xfc      // ULA fc00::/7
            || ip.GetAddressBytes()[0] == 0xfd;
