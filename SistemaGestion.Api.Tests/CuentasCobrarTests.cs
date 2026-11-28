using Microsoft.Data.Sqlite;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;

namespace SistemaGestion.Api.Tests;

/// <summary>
/// Tests de integración end-to-end para el flujo de Facturas (Contado / Crédito),
/// Cuentas por Cobrar (CxC) y Abonos (parcial, total, FIFO, comprobantes y reconciliación).
/// </summary>
[Collection("ApiIntegration")]
public class CuentasCobrarTests
{
    private readonly ApiFactory _factory;

    public CuentasCobrarTests(ApiFactory factory) => _factory = factory;

    private async Task<HttpClient> ClienteAutenticado()
    {
        var client = _factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
        var loginRes = await client.PostAsJsonAsync("/api/login",
            new { usuario = "admin", contrasena = "admin123" });
        loginRes.EnsureSuccessStatusCode();
        if (loginRes.Headers.TryGetValues("Set-Cookie", out var setCookies))
        {
            foreach (var c in setCookies)
            {
                if (c.StartsWith("sg_token=", StringComparison.OrdinalIgnoreCase))
                {
                    client.DefaultRequestHeaders.Add("Cookie", c.Split(';')[0]);
                    break;
                }
            }
        }
        return client;
    }

    [Fact(DisplayName = "Factura de Contado → queda pagada al 100% y NO crea CxC")]
    public async Task FacturaContado_NoCreaCxC()
    {
        var client = await ClienteAutenticado();
        var numF = "NE-CONT-01";
        var res = await client.PostAsJsonAsync("/api/facturas", new
        {
            numeroF       = numF,
            fecha         = DateTime.Today.ToString("yyyy-MM-dd"),
            codigoCliente = "CLI-001",
            formaDePago   = "Contado",
            pctIva        = 0,
            descuento     = 0,
            diasCredito   = 0,
            observaciones = "Prueba contado",
            items         = new[] { new { codigo = "PROD-CXC-001", cantidad = 2m, precioUnit = 50m } }
        });
        Assert.Equal(HttpStatusCode.Created, res.StatusCode);
        var body = await res.Content.ReadFromJsonAsync<JsonElement>();
        Assert.False(body.GetProperty("cxcCreada").GetBoolean());

        var detRes = await client.GetAsync($"/api/facturas/{numF}");
        Assert.Equal(HttpStatusCode.OK, detRes.StatusCode);
        var det = await detRes.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(det.GetProperty("factura").GetProperty("pagada").GetBoolean());
        Assert.Equal(0m, det.GetProperty("factura").GetProperty("saldo").GetDecimal());
    }

    [Fact(DisplayName = "Factura a Crédito → crea CxC; abono parcial + total vía /api/facturas/{num}/pago actualiza Factura, CxC y Abonos")]
    public async Task FacturaCredito_AbonoParcialYTotal_SincronizaTodo()
    {
        var client = await ClienteAutenticado();
        var numF = "NE-CRED-01";
        var hoy = DateTime.Today.ToString("yyyy-MM-dd");

        // 1. Crear factura a crédito 15 días por $200
        var crear = await client.PostAsJsonAsync("/api/facturas", new
        {
            numeroF       = numF,
            fecha         = hoy,
            codigoCliente = "CLI-001",
            formaDePago   = "Credito 15",
            pctIva        = 0,
            descuento     = 0,
            diasCredito   = 15,
            observaciones = "Prueba credito",
            items         = new[] { new { codigo = "PROD-CXC-001", cantidad = 4m, precioUnit = 50m } }
        });
        Assert.Equal(HttpStatusCode.Created, crear.StatusCode);
        var crearBody = await crear.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(crearBody.GetProperty("cxcCreada").GetBoolean());
        int idCxC = crearBody.GetProperty("idCxC").GetInt32();
        Assert.True(idCxC > 0);

        // Verificar fechaVence correcta (hoy + 15 días, NO vencida)
        var detIni = await (await client.GetAsync($"/api/facturas/{numF}")).Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(DateTime.Today.AddDays(15).ToString("yyyy-MM-dd"), detIni.GetProperty("factura").GetProperty("fechaVence").GetString());

        // 2. Abono parcial de $80 vía JSON
        var pago1 = await client.PostAsJsonAsync($"/api/facturas/{numF}/pago", new
        {
            monto = 80m, metodoPago = "Zelle", referencia = "ZEL-001"
        });
        Assert.Equal(HttpStatusCode.OK, pago1.StatusCode);
        var p1Body = await pago1.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(p1Body.GetProperty("cxcActualizada").GetBoolean());
        Assert.False(p1Body.GetProperty("pagada").GetBoolean());
        Assert.Equal(80m, p1Body.GetProperty("abono").GetDecimal());
        Assert.Equal(120m, p1Body.GetProperty("saldo").GetDecimal());
        Assert.True(p1Body.GetProperty("idAbono").GetInt32() > 0);

        // Verificar CxC actualizada a Pagado=80, Saldo=120, Estado=Vigente
        var cxcList1 = await (await client.GetAsync("/api/cxc?cliente=CLI-001")).Content.ReadFromJsonAsync<JsonElement>();
        var cxcRow1 = cxcList1.EnumerateArray().First(x => x.GetProperty("id").GetInt32() == idCxC);
        Assert.Equal(80m, cxcRow1.GetProperty("pagado").GetDecimal());
        Assert.Equal(120m, cxcRow1.GetProperty("saldo").GetDecimal());
        Assert.Equal("Vigente", cxcRow1.GetProperty("estado").GetString());

        // 3. Abono total restante de $120 vía multipart
        using var form = new MultipartFormDataContent();
        form.Add(new StringContent("120"), "monto");
        form.Add(new StringContent("PagoMovil"), "metodoPago");
        form.Add(new StringContent("PM-999"), "referencia");
        var pago2 = await client.PostAsync($"/api/facturas/{numF}/pago", form);
        Assert.Equal(HttpStatusCode.OK, pago2.StatusCode);
        var p2Body = await pago2.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(p2Body.GetProperty("pagada").GetBoolean());
        Assert.True(p2Body.GetProperty("cxcActualizada").GetBoolean());
        Assert.Equal(0m, p2Body.GetProperty("saldo").GetDecimal());

        // Verificar CxC quedó Saldada con Pagado=200, Saldo=0
        var cxcList2 = await (await client.GetAsync("/api/cxc?cliente=CLI-001")).Content.ReadFromJsonAsync<JsonElement>();
        var cxcRow2 = cxcList2.EnumerateArray().First(x => x.GetProperty("id").GetInt32() == idCxC);
        Assert.Equal(200m, cxcRow2.GetProperty("pagado").GetDecimal());
        Assert.Equal(0m, cxcRow2.GetProperty("saldo").GetDecimal());
        Assert.Equal("Saldada", cxcRow2.GetProperty("estado").GetString());

        // Verificar ambos abonos aparecen en GET /api/abonos
        var abonos = await (await client.GetAsync("/api/abonos?cliente=CLI-001")).Content.ReadFromJsonAsync<JsonElement>();
        var abonosFact = abonos.EnumerateArray().Where(a => a.GetProperty("numeroF").GetString() == numF).ToList();
        Assert.Equal(2, abonosFact.Count);
    }

    [Fact(DisplayName = "POST /api/cxc/{id}/pago actualiza CxC, Facturas.Abono y crea registro en Abonos")]
    public async Task PagoDirectoCxC_SincronizaFacturaYAbono()
    {
        var client = await ClienteAutenticado();
        var numF = "NE-CXC-02";

        var crear = await client.PostAsJsonAsync("/api/facturas", new
        {
            numeroF       = numF,
            fecha         = DateTime.Today.AddDays(-20).ToString("yyyy-MM-dd"),
            codigoCliente = "CLI-001",
            formaDePago   = "Credito 15",
            pctIva        = 0,
            descuento     = 0,
            diasCredito   = 15,
            observaciones = "Prueba vencida",
            items         = new[] { new { codigo = "PROD-CXC-001", cantidad = 3m, precioUnit = 100m } }
        });
        Assert.Equal(HttpStatusCode.Created, crear.StatusCode);
        int idCxC = (await crear.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("idCxC").GetInt32();

        var pago = await client.PostAsJsonAsync($"/api/cxc/{idCxC}/pago", new
        {
            monto = 300m, metodoPago = "Efectivo", referencia = "EFE-1"
        });
        Assert.Equal(HttpStatusCode.OK, pago.StatusCode);
        var pBody = await pago.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(pBody.GetProperty("saldada").GetBoolean());
        Assert.True(pBody.GetProperty("facturaActualizada").GetBoolean());

        // Factura debe quedar pagada
        var det = await (await client.GetAsync($"/api/facturas/{numF}")).Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(det.GetProperty("factura").GetProperty("pagada").GetBoolean());
        Assert.Equal(300m, det.GetProperty("factura").GetProperty("abono").GetDecimal());
    }

    [Fact(DisplayName = "POST /api/abonos en modo automático FIFO rebaja CxC más antigua primero")]
    public async Task AbonoFifo_RebajaCxCMasAntiguaPrimero()
    {
        var client = await ClienteAutenticado();

        await client.PostAsJsonAsync("/api/clientes", new
        {
            codigo = "CLI-FIFO", descripcion = "Cliente FIFO", ciRif = "J-99999999-1"
        });

        var f1 = await client.PostAsJsonAsync("/api/facturas", new
        {
            numeroF = "NE-FIFO-1", fecha = DateTime.Today.AddDays(-30).ToString("yyyy-MM-dd"),
            codigoCliente = "CLI-FIFO", formaDePago = "Credito 15", pctIva = 0, descuento = 0, diasCredito = 15,
            items = new[] { new { codigo = "PROD-CXC-001", cantidad = 1m, precioUnit = 100m } }
        });
        int cxc1 = (await f1.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("idCxC").GetInt32();

        var f2 = await client.PostAsJsonAsync("/api/facturas", new
        {
            numeroF = "NE-FIFO-2", fecha = DateTime.Today.AddDays(-10).ToString("yyyy-MM-dd"),
            codigoCliente = "CLI-FIFO", formaDePago = "Credito 15", pctIva = 0, descuento = 0, diasCredito = 15,
            items = new[] { new { codigo = "PROD-CXC-001", cantidad = 1m, precioUnit = 150m } }
        });
        int cxc2 = (await f2.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("idCxC").GetInt32();

        // Abono general de $160 (sin idCuentaCobrar → FIFO: liquida $100 de NE-FIFO-1 y abona $60 a NE-FIFO-2)
        using var fd = new MultipartFormDataContent();
        fd.Add(new StringContent("CLI-FIFO"), "cliente");
        fd.Add(new StringContent(DateTime.Today.ToString("yyyy-MM-dd")), "fecha");
        fd.Add(new StringContent("160"), "monto");
        fd.Add(new StringContent("Zelle"), "metodoPago");
        fd.Add(new StringContent("Confirmado"), "estado");
        var abRes = await client.PostAsync("/api/abonos", fd);
        Assert.Equal(HttpStatusCode.OK, abRes.StatusCode);

        var cxcAll = await (await client.GetAsync("/api/cxc?cliente=CLI-FIFO")).Content.ReadFromJsonAsync<JsonElement>();
        var row1 = cxcAll.EnumerateArray().First(x => x.GetProperty("id").GetInt32() == cxc1);
        var row2 = cxcAll.EnumerateArray().First(x => x.GetProperty("id").GetInt32() == cxc2);
        Assert.Equal("Saldada", row1.GetProperty("estado").GetString());
        Assert.Equal(0m, row1.GetProperty("saldo").GetDecimal());
        Assert.Equal(60m, row2.GetProperty("pagado").GetDecimal());
        Assert.Equal(90m, row2.GetProperty("saldo").GetDecimal());
    }

    [Fact(DisplayName = "POST /api/facturas/{num}/comprobantes en factura a crédito rebaja Factura y CxC")]
    public async Task ComprobanteFacturaCredito_RebajaCxC()
    {
        var client = await ClienteAutenticado();
        var numF = "NE-COMP-01";
        var crear = await client.PostAsJsonAsync("/api/facturas", new
        {
            numeroF = numF, fecha = DateTime.Today.ToString("yyyy-MM-dd"),
            codigoCliente = "CLI-001", formaDePago = "Credito 30", pctIva = 0, descuento = 0, diasCredito = 30,
            items = new[] { new { codigo = "PROD-CXC-001", cantidad = 2m, precioUnit = 100m } }
        });
        int idCxC = (await crear.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("idCxC").GetInt32();

        using var fd = new MultipartFormDataContent();
        fd.Add(new StringContent("Zelle"), "metodoPago");
        fd.Add(new StringContent("75"), "monto");
        fd.Add(new StringContent("ZEL-COMP"), "referencia");
        fd.Add(new StringContent("CLI-001"), "cliente");
        var compRes = await client.PostAsync($"/api/facturas/{numF}/comprobantes", fd);
        Assert.Equal(HttpStatusCode.OK, compRes.StatusCode);
        var compBody = await compRes.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(compBody.GetProperty("abonoAplicadoACxC").GetBoolean());

        var cxcAll = await (await client.GetAsync("/api/cxc?cliente=CLI-001")).Content.ReadFromJsonAsync<JsonElement>();
        var row = cxcAll.EnumerateArray().First(x => x.GetProperty("id").GetInt32() == idCxC);
        Assert.Equal(75m, row.GetProperty("pagado").GetDecimal());
        Assert.Equal(125m, row.GetProperty("saldo").GetDecimal());
    }

    [Fact(DisplayName = "POST /api/cxc/sincronizar corrige CxC históricas desincronizadas con Facturas")]
    public async Task SincronizarCxC_CorrigeDesincronizacionHistorica()
    {
        var client = await ClienteAutenticado();
        var numF = "NE-DESYNC-1";

        using (var conn = new SqliteConnection($"Data Source={_factory.DbPath};"))
        {
            conn.Open();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                INSERT INTO Facturas (NumeroF, Fecha, Codigo, FormaDePago, Abono, TotalF, DiasCreditoF, Anulada)
                VALUES ($n, '2026-08-01', 'CLI-001', 'Credito 15', 500, 500, 15, 0);
                INSERT INTO CuentasCobrar (Cliente, Concepto, Monto, Pagado, FechaEmision, FechaVence, Estado)
                VALUES ('CLI-001', 'Factura ' || $n, 500, 0, '2026-08-01', '2026-08-16', 'Vencida');";
            cmd.Parameters.AddWithValue("$n", numF);
            cmd.ExecuteNonQuery();
        }

        var syncRes = await client.PostAsync("/api/cxc/sincronizar", null);
        Assert.Equal(HttpStatusCode.OK, syncRes.StatusCode);
        var syncBody = await syncRes.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(syncBody.GetProperty("cxcActualizadas").GetInt32() >= 1);

        var cxcAll = await (await client.GetAsync("/api/cxc?cliente=CLI-001")).Content.ReadFromJsonAsync<JsonElement>();
        var row = cxcAll.EnumerateArray().First(x => x.GetProperty("concepto").GetString() == $"Factura {numF}");
        Assert.Equal("Saldada", row.GetProperty("estado").GetString());
        Assert.Equal(500m, row.GetProperty("pagado").GetDecimal());
        Assert.Equal(0m, row.GetProperty("saldo").GetDecimal());
    }
}
