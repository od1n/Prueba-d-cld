using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;

namespace SistemaGestion.Api.Tests;

/// <summary>
/// Tests de integración para SistemaGestion.Api.
/// Usan WebApplicationFactory con una BD SQLite temporal — no requieren
/// intervención del usuario ni que el sistema de escritorio esté corriendo.
///
/// Ejecutar: dotnet test SistemaGestion.Api.Tests/SistemaGestion.Api.Tests.csproj
/// </summary>
public class ApiTests : IClassFixture<ApiFactory>
{
    private readonly HttpClient _http;
    private readonly ApiFactory _factory;

    public ApiTests(ApiFactory factory)
    {
        _factory = factory;
        // allowRedirect=false para detectar redirects inesperados
        _http = factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
    }

    // ═════════════════════════════════════════════════════════════════════
    // AUTH
    // ═════════════════════════════════════════════════════════════════════

    [Fact(DisplayName = "POST /api/login — credenciales vacías → 400")]
    public async Task Login_EmptyCredentials_Returns400()
    {
        var res = await _http.PostAsJsonAsync("/api/login", new { usuario = "", contrasena = "" });
        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
        var body = await res.Content.ReadAsStringAsync();
        Assert.Contains("requeridos", body, StringComparison.OrdinalIgnoreCase);
    }

    [Fact(DisplayName = "POST /api/login — contraseña incorrecta → 401")]
    public async Task Login_WrongPassword_Returns401()
    {
        var res = await _http.PostAsJsonAsync("/api/login",
            new { usuario = "admin", contrasena = "mal_password" });
        Assert.Equal(HttpStatusCode.Unauthorized, res.StatusCode);
    }

    [Fact(DisplayName = "POST /api/login — usuario inexistente → 401")]
    public async Task Login_UnknownUser_Returns401()
    {
        var res = await _http.PostAsJsonAsync("/api/login",
            new { usuario = "noexiste", contrasena = "cualquiera" });
        Assert.Equal(HttpStatusCode.Unauthorized, res.StatusCode);
    }

    [Fact(DisplayName = "POST /api/login — credenciales correctas → 200 + cookie sg_token")]
    public async Task Login_ValidCredentials_Returns200WithCookie()
    {
        var res = await _http.PostAsJsonAsync("/api/login",
            new { usuario = "admin", contrasena = "admin123" });

        Assert.Equal(HttpStatusCode.OK, res.StatusCode);

        var body = await res.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("admin", body.GetProperty("nombreUsuario").GetString());
        Assert.Equal("Admin", body.GetProperty("rol").GetString());

        // La cookie HttpOnly sg_token debe estar presente
        Assert.True(res.Headers.Contains("Set-Cookie"), "Debe emitirse la cookie sg_token");
        var cookies = res.Headers.GetValues("Set-Cookie");
        Assert.Contains(cookies, c => c.StartsWith("sg_token=", StringComparison.OrdinalIgnoreCase));
    }

    // ═════════════════════════════════════════════════════════════════════
    // ENDPOINTS PROTEGIDOS SIN TOKEN
    // ═════════════════════════════════════════════════════════════════════

    [Theory(DisplayName = "Endpoints protegidos sin token → 401")]
    [InlineData("GET",  "/api/inventario")]
    [InlineData("GET",  "/api/inventario/PROD-001")]
    [InlineData("GET",  "/api/kardex/b")]
    [InlineData("GET",  "/api/kardex/a")]
    [InlineData("GET",  "/api/clientes")]
    [InlineData("GET",  "/api/dashboard")]
    [InlineData("GET",  "/api/me")]
    public async Task ProtectedEndpoints_WithoutToken_Return401(string method, string path)
    {
        using var req = new HttpRequestMessage(new HttpMethod(method), path);
        var res = await _http.SendAsync(req);
        Assert.Equal(HttpStatusCode.Unauthorized, res.StatusCode);
    }

    // ═════════════════════════════════════════════════════════════════════
    // ENDPOINTS CON TOKEN
    // ═════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Helper: hace login y retorna un HttpClient con la cookie sg_token inyectada
    /// en DefaultRequestHeaders. WebApplicationFactory usa un transporte en memoria
    /// que no gestiona cookies automáticamente, así que extraemos el valor de
    /// Set-Cookie y lo reenvíamos en el header Cookie de las siguientes peticiones.
    /// </summary>
    private async Task<HttpClient> ClienteAutenticado()
    {
        var client   = _factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        var loginRes = await client.PostAsJsonAsync("/api/login",
            new { usuario = "admin", contrasena = "admin123" });
        loginRes.EnsureSuccessStatusCode();

        // Extraer sg_token del header Set-Cookie
        if (loginRes.Headers.TryGetValues("Set-Cookie", out var setCookies))
        {
            foreach (var c in setCookies)
            {
                if (c.StartsWith("sg_token=", StringComparison.OrdinalIgnoreCase))
                {
                    // Formato: "sg_token=VALUE; HttpOnly; SameSite=Strict; ..."
                    var tokenValue = c.Split(';')[0]; // "sg_token=VALUE"
                    client.DefaultRequestHeaders.Add("Cookie", tokenValue);
                    break;
                }
            }
        }

        return client;
    }

    [Fact(DisplayName = "GET /api/me — con token → devuelve info del usuario admin")]
    public async Task Me_WithToken_ReturnsUserInfo()
    {
        var client = await ClienteAutenticado();
        var res    = await client.GetAsync("/api/me");
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);

        var body = await res.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("admin",  body.GetProperty("nombreUsuario").GetString());
        Assert.Equal("Admin",  body.GetProperty("rol").GetString());
        Assert.True(body.GetProperty("esAdmin").GetBoolean());
    }

    [Fact(DisplayName = "GET /api/inventario — con token → lista productos")]
    public async Task Inventario_WithToken_ReturnsList()
    {
        var client = await ClienteAutenticado();
        var res    = await client.GetAsync("/api/inventario");
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);

        var items = await res.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(JsonValueKind.Array, items.ValueKind);
        Assert.True(items.GetArrayLength() >= 1, "Debe haber al menos el producto semilla");

        var first = items[0];
        Assert.Equal("PROD-001", first.GetProperty("codigo").GetString());
    }

    [Fact(DisplayName = "GET /api/inventario?q=prueba — filtra por descripción")]
    public async Task Inventario_ConFiltro_RetornaCoincidencias()
    {
        var client = await ClienteAutenticado();
        var res    = await client.GetAsync("/api/inventario?q=prueba");
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);

        var items = await res.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(items.GetArrayLength() >= 1);
    }

    [Fact(DisplayName = "GET /api/inventario?q=XYZ_NULO — filtro sin resultados → lista vacía")]
    public async Task Inventario_FiltroSinResultados_RetornaVacio()
    {
        var client = await ClienteAutenticado();
        var res    = await client.GetAsync("/api/inventario?q=XYZ_NULO_99999");
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);

        var items = await res.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(0, items.GetArrayLength());
    }

    [Fact(DisplayName = "GET /api/inventario/{codigo} — código existente → detalle del producto")]
    public async Task InventarioDetalle_CodigoExistente_RetornaProducto()
    {
        var client = await ClienteAutenticado();
        var res    = await client.GetAsync("/api/inventario/PROD-001");
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);

        var body = await res.Content.ReadFromJsonAsync<JsonElement>();
        // El endpoint devuelve { item: {...}, movimientos: [...] }
        var item = body.GetProperty("item");
        Assert.Equal("PROD-001", item.GetProperty("codigo").GetString());
        Assert.Equal(100m, item.GetProperty("stock").GetDecimal());
    }

    [Fact(DisplayName = "GET /api/inventario/{codigo} — código inexistente → 404")]
    public async Task InventarioDetalle_CodigoInexistente_Retorna404()
    {
        var client = await ClienteAutenticado();
        var res    = await client.GetAsync("/api/inventario/NO-EXISTE-999");
        Assert.Equal(HttpStatusCode.NotFound, res.StatusCode);
    }

    [Fact(DisplayName = "GET /api/kardex/b — con token → 200")]
    public async Task KardexProductos_WithToken_Returns200()
    {
        var client = await ClienteAutenticado();
        var res    = await client.GetAsync("/api/kardex/b");
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);

        var items = await res.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(JsonValueKind.Array, items.ValueKind);
    }

    [Fact(DisplayName = "GET /api/kardex/a — con token → 200")]
    public async Task KardexMateriasPrimas_WithToken_Returns200()
    {
        var client = await ClienteAutenticado();
        var res    = await client.GetAsync("/api/kardex/a");
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);

        var items = await res.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(JsonValueKind.Array, items.ValueKind);
    }

    [Fact(DisplayName = "GET /api/clientes — con token → lista clientes")]
    public async Task Clientes_WithToken_ReturnsList()
    {
        var client = await ClienteAutenticado();
        var res    = await client.GetAsync("/api/clientes");
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);

        var items = await res.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(JsonValueKind.Array, items.ValueKind);
        Assert.True(items.GetArrayLength() >= 1);
    }

    [Fact(DisplayName = "GET /api/dashboard — con token → KPIs del día")]
    public async Task Dashboard_WithToken_ReturnsKpis()
    {
        var client = await ClienteAutenticado();
        var res    = await client.GetAsync("/api/dashboard");
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);

        var body = await res.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(body.TryGetProperty("ventasHoyUsd",      out _), "Falta ventasHoyUsd");
        Assert.True(body.TryGetProperty("facturasEmitidas",  out _), "Falta facturasEmitidas");
        Assert.True(body.TryGetProperty("stockBajo",         out _), "Falta stockBajo");
        Assert.True(body.TryGetProperty("cxcVencidas",       out _), "Falta cxcVencidas");
    }

    // ═════════════════════════════════════════════════════════════════════
    // LOGOUT
    // ═════════════════════════════════════════════════════════════════════

    [Fact(DisplayName = "POST /api/logout — con token → 200 y cookie eliminada")]
    public async Task Logout_WithToken_Returns200AndClearsCookie()
    {
        var client  = await ClienteAutenticado();
        var res     = await client.PostAsync("/api/logout", null);
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);

        // Después del logout, /api/me debe devolver 401
        var meRes = await client.GetAsync("/api/me");
        Assert.Equal(HttpStatusCode.Unauthorized, meRes.StatusCode);
    }
}
