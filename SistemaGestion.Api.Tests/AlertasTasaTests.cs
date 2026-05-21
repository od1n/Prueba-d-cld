using Xunit;

namespace SistemaGestion.Api.Tests;

/// <summary>
/// Tests unitarios para:
///   - AlertasData: TieneAlertas, TotalAlertas y clasificación por lista
///   - TasaResultado: HayBCV, HayParalelo
///   - Lógica de fresquedad de la tasa (días desde la última actualización)
/// Sin BD ni WinForms — réplica fiel de AlertasService.cs y TasaCambioService.cs.
/// </summary>
public class AlertasTasaTests
{
    // ══════════════════════════════════════════════════════════════════════
    // RÉPLICAS
    // ══════════════════════════════════════════════════════════════════════

    /// <summary>Réplica de AlertasData (SistemaGestion) con List&lt;object&gt; para independencia de tipos.</summary>
    private sealed class AlertasData
    {
        public List<object> StockBajo   { get; init; } = new();
        public List<object> CxCVencidas { get; init; } = new();
        public List<object> MantVencido { get; init; } = new();

        public bool TieneAlertas =>
            StockBajo.Count > 0 || CxCVencidas.Count > 0 || MantVencido.Count > 0;

        public int TotalAlertas =>
            StockBajo.Count + CxCVencidas.Count + MantVencido.Count;
    }

    /// <summary>Réplica de TasaResultado (SistemaGestion).</summary>
    private sealed class TasaResultado
    {
        public decimal  BCV      { get; init; }
        public decimal  Paralelo { get; init; }
        public string?  Error    { get; init; }

        public bool HayBCV      => BCV > 0;
        public bool HayParalelo => Paralelo > 0;
    }

    /// <summary>
    /// Clasifica la fresquedad de la tasa según los días transcurridos.
    /// Réplica de la lógica de RefrescarInfo() en MenuPrincipalV2.cs.
    /// Retorna "ok" | "warn" | "danger".
    /// </summary>
    private static string FresquedadTasa(DateTime fechaTasa)
    {
        int dias = (DateTime.Today - fechaTasa.Date).Days;
        if (dias == 0)      return "ok";
        if (dias <= 2)      return "warn";
        return "danger";
    }

    // ══════════════════════════════════════════════════════════════════════
    // ALERTASDATA — TieneAlertas
    // ══════════════════════════════════════════════════════════════════════

    [Fact(DisplayName = "TieneAlertas = false cuando las tres listas están vacías")]
    public void TieneAlertas_TodasVacias_EsFalse()
    {
        var a = new AlertasData();
        Assert.False(a.TieneAlertas);
    }

    [Fact(DisplayName = "TieneAlertas = true cuando hay stock bajo")]
    public void TieneAlertas_StockBajo_EsTrue()
    {
        var a = new AlertasData
        {
            StockBajo = new List<object> { new() }
        };
        Assert.True(a.TieneAlertas);
    }

    [Fact(DisplayName = "TieneAlertas = true cuando hay CxC vencidas")]
    public void TieneAlertas_CxCVencidas_EsTrue()
    {
        var a = new AlertasData
        {
            CxCVencidas = new List<object> { new(), new() }
        };
        Assert.True(a.TieneAlertas);
    }

    [Fact(DisplayName = "TieneAlertas = true cuando hay mantenimiento vencido")]
    public void TieneAlertas_MantVencido_EsTrue()
    {
        var a = new AlertasData
        {
            MantVencido = new List<object> { new() }
        };
        Assert.True(a.TieneAlertas);
    }

    [Fact(DisplayName = "TieneAlertas = true aunque solo una de las tres listas tenga datos")]
    public void TieneAlertas_SoloUnaTieneItems_EsTrue()
    {
        // StockBajo vacío, CxC vacío, solo Mant tiene 1 item
        var a = new AlertasData
        {
            MantVencido = new List<object> { new() }
        };
        Assert.True(a.TieneAlertas);
        Assert.Empty(a.StockBajo);
        Assert.Empty(a.CxCVencidas);
    }

    // ══════════════════════════════════════════════════════════════════════
    // ALERTASDATA — TotalAlertas
    // ══════════════════════════════════════════════════════════════════════

    [Fact(DisplayName = "TotalAlertas = 0 cuando no hay ninguna alerta")]
    public void TotalAlertas_SinAlertas_EsCero()
    {
        var a = new AlertasData();
        Assert.Equal(0, a.TotalAlertas);
    }

    [Fact(DisplayName = "TotalAlertas = suma exacta de las tres listas")]
    public void TotalAlertas_SumaListas()
    {
        var a = new AlertasData
        {
            StockBajo   = new List<object> { new(), new(), new() },   // 3
            CxCVencidas = new List<object> { new(), new() },          // 2
            MantVencido = new List<object> { new() }                  // 1
        };
        Assert.Equal(6, a.TotalAlertas);
    }

    [Fact(DisplayName = "TotalAlertas = count correcto con una sola lista activa")]
    public void TotalAlertas_SolaUnaLista()
    {
        var a = new AlertasData
        {
            CxCVencidas = new List<object> { new(), new(), new(), new(), new() }  // 5
        };
        Assert.Equal(5, a.TotalAlertas);
        Assert.True(a.TieneAlertas);
    }

    [Theory(DisplayName = "TotalAlertas refleja cada combinación de conteos")]
    [InlineData(0, 0, 0, 0)]
    [InlineData(1, 0, 0, 1)]
    [InlineData(0, 3, 0, 3)]
    [InlineData(0, 0, 2, 2)]
    [InlineData(4, 5, 6, 15)]
    public void TotalAlertas_Combinaciones(int nStock, int nCxC, int nMant, int totalEsp)
    {
        var a = new AlertasData
        {
            StockBajo   = Enumerable.Range(0, nStock).Select(_ => new object()).ToList(),
            CxCVencidas = Enumerable.Range(0, nCxC).Select(_ => new object()).ToList(),
            MantVencido = Enumerable.Range(0, nMant).Select(_ => new object()).ToList()
        };
        Assert.Equal(totalEsp, a.TotalAlertas);
    }

    // ══════════════════════════════════════════════════════════════════════
    // TASARESULTADO — HayBCV / HayParalelo
    // ══════════════════════════════════════════════════════════════════════

    [Fact(DisplayName = "HayBCV = false cuando BCV = 0")]
    public void HayBCV_CuandoBCVEsCero_EsFalse()
    {
        var t = new TasaResultado { BCV = 0m };
        Assert.False(t.HayBCV);
    }

    [Fact(DisplayName = "HayBCV = true cuando BCV > 0")]
    public void HayBCV_CuandoBCVPositivo_EsTrue()
    {
        var t = new TasaResultado { BCV = 36.50m };
        Assert.True(t.HayBCV);
    }

    [Fact(DisplayName = "HayParalelo = false cuando Paralelo = 0")]
    public void HayParalelo_CuandoParaleloEsCero_EsFalse()
    {
        var t = new TasaResultado { Paralelo = 0m };
        Assert.False(t.HayParalelo);
    }

    [Fact(DisplayName = "HayParalelo = true cuando Paralelo > 0")]
    public void HayParalelo_CuandoParaleloPositivo_EsTrue()
    {
        var t = new TasaResultado { Paralelo = 40.20m };
        Assert.True(t.HayParalelo);
    }

    [Fact(DisplayName = "Puede haber BCV sin Paralelo (tasa oficial publicada, paralelo no registrado)")]
    public void HayBCV_SinParalelo()
    {
        var t = new TasaResultado { BCV = 36.50m, Paralelo = 0m };
        Assert.True(t.HayBCV);
        Assert.False(t.HayParalelo);
    }

    [Fact(DisplayName = "Sin ninguna tasa, ambas propiedades son false")]
    public void SinTasas_AmbosHayEsFalse()
    {
        var t = new TasaResultado { BCV = 0m, Paralelo = 0m };
        Assert.False(t.HayBCV);
        Assert.False(t.HayParalelo);
    }

    // ══════════════════════════════════════════════════════════════════════
    // FRESQUEDAD DE LA TASA (lógica de RefrescarInfo en MenuPrincipalV2)
    // ══════════════════════════════════════════════════════════════════════

    [Fact(DisplayName = "Tasa de hoy → fresquedad 'ok'")]
    public void FresquedadTasa_Hoy_EsOk()
    {
        Assert.Equal("ok", FresquedadTasa(DateTime.Today));
    }

    [Fact(DisplayName = "Tasa de ayer → fresquedad 'warn' (1 día atrás)")]
    public void FresquedadTasa_Ayer_EsWarn()
    {
        Assert.Equal("warn", FresquedadTasa(DateTime.Today.AddDays(-1)));
    }

    [Fact(DisplayName = "Tasa de 2 días atrás → fresquedad 'warn'")]
    public void FresquedadTasa_DosDias_EsWarn()
    {
        Assert.Equal("warn", FresquedadTasa(DateTime.Today.AddDays(-2)));
    }

    [Fact(DisplayName = "Tasa de 3 días atrás → fresquedad 'danger'")]
    public void FresquedadTasa_TresDias_EsDanger()
    {
        Assert.Equal("danger", FresquedadTasa(DateTime.Today.AddDays(-3)));
    }

    [Fact(DisplayName = "Tasa de 30 días atrás → fresquedad 'danger'")]
    public void FresquedadTasa_TreintaDias_EsDanger()
    {
        Assert.Equal("danger", FresquedadTasa(DateTime.Today.AddDays(-30)));
    }

    [Theory(DisplayName = "Fresquedad según días desde la última actualización")]
    [InlineData(  0, "ok")]
    [InlineData(  1, "warn")]
    [InlineData(  2, "warn")]
    [InlineData(  3, "danger")]
    [InlineData( 10, "danger")]
    [InlineData(365, "danger")]
    public void FresquedadTasa_Parametrico(int diasAtras, string esperado)
    {
        Assert.Equal(esperado, FresquedadTasa(DateTime.Today.AddDays(-diasAtras)));
    }
}
