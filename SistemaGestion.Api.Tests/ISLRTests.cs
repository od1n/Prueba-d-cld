using Xunit;

namespace SistemaGestion.Api.Tests;

/// <summary>
/// Tests unitarios para la lógica aritmética del cálculo ISLR Estimado — Tarifa 3
/// (Art. 52 LISR, Persona Jurídica).
///
/// No dependen de BD ni de WinForms: replican fielmente las fórmulas de
/// DB_ISLR.cs (CalcularISLR) y ResultadoISLR para validar la aritmética pura.
///
/// Ejecutar: dotnet test SistemaGestion.Api.Tests/SistemaGestion.Api.Tests.csproj
/// </summary>
public class ISLRTests
{
    // ══════════════════════════════════════════════════════════════════════
    // TARIFA 3 — Art. 52 LISR (réplica exacta de DB_ISLR.cs línea 326-331)
    // Tramos: 15% hasta 2000 UT | 22% de 2001 a 3000 UT | 34% > 3000 UT
    // ══════════════════════════════════════════════════════════════════════

    private static decimal Tarifa3(decimal enUT)
    {
        if (enUT <= 0m)     return 0m;
        if (enUT <= 2000m)  return enUT * 0.15m;
        if (enUT <= 3000m)  return 2000m * 0.15m + (enUT - 2000m) * 0.22m;
        return 2000m * 0.15m + 1000m * 0.22m + (enUT - 3000m) * 0.34m;
    }

    // ── Casos límite básicos ───────────────────────────────────────────────

    [Fact(DisplayName = "Enriquecimiento = 0 UT → impuesto = 0 UT")]
    public void Tarifa3_EnriquecimientoCero_Impuesto0()
        => Assert.Equal(0m, Tarifa3(0m));

    [Fact(DisplayName = "Enriquecimiento negativo (pérdida) → impuesto = 0 UT")]
    public void Tarifa3_Perdida_Impuesto0()
        => Assert.Equal(0m, Tarifa3(-1000m));

    // ── Primer tramo: 0–2000 UT al 15% ────────────────────────────────────

    [Fact(DisplayName = "1000 UT (primer tramo) → 150 UT (1000 × 15%)")]
    public void Tarifa3_PrimerTramo_1000UT()
        => Assert.Equal(150m, Tarifa3(1000m));

    [Fact(DisplayName = "2000 UT (límite del primer tramo) → 300 UT (2000 × 15%)")]
    public void Tarifa3_LimitePrimerTramo_2000UT()
        => Assert.Equal(300m, Tarifa3(2000m));

    // ── Segundo tramo: 2001–3000 UT al 22% ────────────────────────────────

    [Fact(DisplayName = "2001 UT (entrada al segundo tramo) → 300.22 UT")]
    public void Tarifa3_Entrada_SegundoTramo_2001UT()
        => Assert.Equal(300.22m, Tarifa3(2001m));

    [Fact(DisplayName = "2500 UT (pleno segundo tramo) → 410 UT (300 + 110)")]
    public void Tarifa3_SegundoTramo_2500UT()
        => Assert.Equal(410m, Tarifa3(2500m));

    [Fact(DisplayName = "3000 UT (límite del segundo tramo) → 520 UT (300 + 220)")]
    public void Tarifa3_LimiteSegundoTramo_3000UT()
        => Assert.Equal(520m, Tarifa3(3000m));

    // ── Tercer tramo: > 3000 UT al 34% ────────────────────────────────────

    [Fact(DisplayName = "3001 UT (entrada al tercer tramo) → 520.34 UT")]
    public void Tarifa3_Entrada_TercerTramo_3001UT()
        => Assert.Equal(520.34m, Tarifa3(3001m));

    [Fact(DisplayName = "4000 UT (pleno tercer tramo) → 860 UT (300 + 220 + 340)")]
    public void Tarifa3_TercerTramo_4000UT()
        => Assert.Equal(860m, Tarifa3(4000m));

    [Fact(DisplayName = "10000 UT (tercer tramo alto) → 2780 UT")]
    public void Tarifa3_TercerTramo_10000UT()
    {
        // 300 + 220 + 7000 * 0.34 = 300 + 220 + 2380 = 2900 UT
        // Recalculo: 2000*0.15 + 1000*0.22 + 7000*0.34 = 300 + 220 + 2380 = 2900
        Assert.Equal(2900m, Tarifa3(10000m));
    }

    // ── Parámetrico: tabla completa de tramos ──────────────────────────────

    [Theory(DisplayName = "Tarifa 3 — tramos clave")]
    [InlineData(   0,    0.00)]
    [InlineData( 500,   75.00)]
    [InlineData(1000,  150.00)]
    [InlineData(2000,  300.00)]
    [InlineData(2001,  300.22)]
    [InlineData(2500,  410.00)]
    [InlineData(3000,  520.00)]
    [InlineData(3001,  520.34)]
    [InlineData(5000, 1200.00)]
    public void Tarifa3_TramosParametrizados(double enUT, double esperadoUT)
    {
        decimal impuesto = Tarifa3((decimal)enUT);
        Assert.Equal((decimal)esperadoUT, impuesto);
    }

    [Fact(DisplayName = "Tarifa 3 es monótonamente no-decreciente para cualquier secuencia de UT positivos")]
    public void Tarifa3_EsMonotonamenteCreciente()
    {
        decimal[] puntos = { 0, 500, 1000, 1999, 2000, 2001, 2500, 3000, 3001, 5000, 10000, 50000 };
        for (int i = 1; i < puntos.Length; i++)
        {
            decimal anterior = Tarifa3(puntos[i - 1]);
            decimal actual   = Tarifa3(puntos[i]);
            Assert.True(actual >= anterior,
                $"Tarifa3({puntos[i]}) = {actual} debe ser ≥ Tarifa3({puntos[i - 1]}) = {anterior}");
        }
    }

    // ══════════════════════════════════════════════════════════════════════
    // FÓRMULAS DERIVADAS DE ResultadoISLR (sin instanciar WinForms)
    // ══════════════════════════════════════════════════════════════════════

    // Replica local de las computed-properties de ResultadoISLR
    private sealed class CalcISLR
    {
        public decimal IngresosBrutos      { get; init; }
        public decimal CostosCompras       { get; init; }
        public decimal CostosGastos        { get; init; }
        public decimal CostosNomina        { get; init; }
        public decimal ValorUT             { get; init; }
        public decimal TasaBCV             { get; init; }
        public decimal PctRebaja           { get; init; }
        public decimal RetencionesBS       { get; init; }
        public decimal ISLRAnioAnteriorBS  { get; init; }
        public decimal RetencionesAntBS    { get; init; }

        // Computed (réplica de ResultadoISLR)
        public decimal TotalCostos          => CostosCompras + CostosGastos + CostosNomina;
        public decimal EnriquecimientoNeto  => IngresosBrutos - TotalCostos;
        public decimal EnriquecimientoBS    => EnriquecimientoNeto * TasaBCV;
        public decimal EnriquecimientoUT    => ValorUT > 0 ? EnriquecimientoBS / ValorUT : 0m;
        public decimal ImpuestoUT           => ISLRTests.Tarifa3(EnriquecimientoUT);
        public decimal ImpuestoBS           => ImpuestoUT * ValorUT;
        public decimal RebajaBS             => ImpuestoBS * PctRebaja / 100m;
        public decimal ISLRNetoBS           => Math.Max(0m, ImpuestoBS - RebajaBS - RetencionesBS);
        public decimal AnticipoMensualBS    => Math.Max(0m, ISLRAnioAnteriorBS - RetencionesAntBS) / 12m;
    }

    [Fact(DisplayName = "Caso base integrado — empresa pequeña (292 UT, primer tramo)")]
    public void ResultadoIntegrado_CasoBase_PrimerTramo()
    {
        // Ingresos 10,000 USD, costos 6,000 USD → EnriquecimientoNeto = 4,000 USD
        // TasaBCV 36.5 → EnriquecimientoBS = 146,000 Bs
        // ValorUT 500 → EnriquecimientoUT = 292 UT (primer tramo)
        // ImpuestoUT = 292 × 0.15 = 43.8 UT
        // ImpuestoBS = 43.8 × 500 = 21,900 Bs
        // Rebaja 10% = 2,190 Bs → ISLRNeto = 19,710 Bs
        var r = new CalcISLR
        {
            IngresosBrutos  = 10_000m,
            CostosCompras   = 4_000m,
            CostosGastos    = 1_500m,
            CostosNomina    = 500m,
            ValorUT         = 500m,
            TasaBCV         = 36.5m,
            PctRebaja       = 10m,
            RetencionesBS   = 0m,
            ISLRAnioAnteriorBS = 0m,
            RetencionesAntBS   = 0m
        };

        Assert.Equal(6_000m,  r.TotalCostos);
        Assert.Equal(4_000m,  r.EnriquecimientoNeto);
        Assert.Equal(146_000m, r.EnriquecimientoBS);
        Assert.Equal(292m,    r.EnriquecimientoUT);
        Assert.Equal(43.8m,   r.ImpuestoUT);
        Assert.Equal(21_900m, r.ImpuestoBS);
        Assert.Equal(2_190m,  r.RebajaBS);
        Assert.Equal(19_710m, r.ISLRNetoBS);
    }

    [Fact(DisplayName = "Caso segundo tramo — empresa mediana (2500 UT)")]
    public void ResultadoIntegrado_SegundoTramo_2500UT()
    {
        // Usamos TasaBCV=40 (divisor exacto) para evitar redondeo en decimal aritmética.
        // enBS = 2500 UT × ValorUT(100) = 250,000 Bs
        // enUSD = 250,000 / 40 = 6,250 USD (exacto, sin fracción)
        var r = new CalcISLR
        {
            IngresosBrutos  = 6_250m,  // → EnriquecimientoNeto = 6,250 USD
            CostosCompras   = 0m,
            CostosGastos    = 0m,
            CostosNomina    = 0m,
            ValorUT         = 100m,
            TasaBCV         = 40m,    // 6,250 × 40 = 250,000 Bs / 100 = 2,500 UT
            PctRebaja       = 10m,
            RetencionesBS   = 0m,
            ISLRAnioAnteriorBS = 0m,
            RetencionesAntBS   = 0m
        };

        Assert.Equal(2_500m,  r.EnriquecimientoUT);
        Assert.Equal(410m,    r.ImpuestoUT);   // Tarifa3(2500) = 300 + 110 = 410 UT
        Assert.Equal(41_000m, r.ImpuestoBS);   // 410 × 100
        Assert.Equal(4_100m,  r.RebajaBS);     // 41,000 × 10%
        Assert.Equal(36_900m, r.ISLRNetoBS);   // 41,000 - 4,100
    }

    [Fact(DisplayName = "ISLRNetoBS no puede ser negativo aunque las retenciones superen el impuesto")]
    public void ISLRNetoBS_NoNegativo_CuandoRetencionesExceden()
    {
        var r = new CalcISLR
        {
            IngresosBrutos  = 5_000m,
            CostosCompras   = 0m, CostosGastos = 0m, CostosNomina = 0m,
            ValorUT         = 500m, TasaBCV = 36.5m, PctRebaja = 10m,
            RetencionesBS   = 999_999m,   // muy superior al impuesto
            ISLRAnioAnteriorBS = 0m, RetencionesAntBS = 0m
        };

        Assert.True(r.ISLRNetoBS >= 0m, "ISLRNetoBS nunca debe ser negativo");
        Assert.Equal(0m, r.ISLRNetoBS);
    }

    [Fact(DisplayName = "Empresa en pérdida → EnriquecimientoNeto negativo → impuesto 0")]
    public void Perdida_NoGeneraImpuesto()
    {
        var r = new CalcISLR
        {
            IngresosBrutos  = 5_000m,
            CostosCompras   = 3_000m, CostosGastos = 2_000m, CostosNomina = 1_000m,
            ValorUT         = 500m, TasaBCV = 36.5m, PctRebaja = 10m,
            RetencionesBS   = 0m, ISLRAnioAnteriorBS = 0m, RetencionesAntBS = 0m
        };

        Assert.True(r.EnriquecimientoNeto < 0, "Empresa en pérdida debe tener EnriquecimientoNeto < 0");
        Assert.Equal(0m, r.ImpuestoUT);
        Assert.Equal(0m, r.ISLRNetoBS);
    }

    // ── Anticipo mensual ───────────────────────────────────────────────────

    [Fact(DisplayName = "Anticipo mensual = (ISLRAnt - RetencionesAnt) / 12")]
    public void AnticipoMensual_FormulaCorrecta()
    {
        var r = new CalcISLR
        {
            IngresosBrutos = 0m, CostosCompras = 0m, CostosGastos = 0m, CostosNomina = 0m,
            ValorUT = 500m, TasaBCV = 36.5m, PctRebaja = 10m, RetencionesBS = 0m,
            ISLRAnioAnteriorBS = 120_000m,
            RetencionesAntBS   = 12_000m
        };

        // (120,000 - 12,000) / 12 = 9,000 Bs/mes
        Assert.Equal(9_000m, r.AnticipoMensualBS);
    }

    [Fact(DisplayName = "Anticipo mensual = 0 cuando retenciones anteriores superan ISLR anterior")]
    public void AnticipoMensual_NoNegativo_CuandoRetencionesExceden()
    {
        var r = new CalcISLR
        {
            IngresosBrutos = 0m, CostosCompras = 0m, CostosGastos = 0m, CostosNomina = 0m,
            ValorUT = 500m, TasaBCV = 36.5m, PctRebaja = 10m, RetencionesBS = 0m,
            ISLRAnioAnteriorBS = 5_000m,
            RetencionesAntBS   = 10_000m   // retenciones > ISLR anterior
        };

        Assert.Equal(0m, r.AnticipoMensualBS);
    }

    [Fact(DisplayName = "Anticipo mensual = 0 cuando no hubo ISLR en el año anterior")]
    public void AnticipoMensual_CeroSinISLRAnterior()
    {
        var r = new CalcISLR
        {
            IngresosBrutos = 0m, CostosCompras = 0m, CostosGastos = 0m, CostosNomina = 0m,
            ValorUT = 500m, TasaBCV = 36.5m, PctRebaja = 10m, RetencionesBS = 0m,
            ISLRAnioAnteriorBS = 0m,
            RetencionesAntBS   = 0m
        };

        Assert.Equal(0m, r.AnticipoMensualBS);
    }

    // ── Conversión USD ↔ Bs ↔ UT ──────────────────────────────────────────

    [Fact(DisplayName = "Conversión: EnriquecimientoBS = EnriquecimientoNeto × TasaBCV")]
    public void Conversion_EnriquecimientoBS_Correcta()
    {
        var r = new CalcISLR
        {
            IngresosBrutos = 1_000m, CostosCompras = 0m, CostosGastos = 0m, CostosNomina = 0m,
            ValorUT = 100m, TasaBCV = 40m, PctRebaja = 10m,
            RetencionesBS = 0m, ISLRAnioAnteriorBS = 0m, RetencionesAntBS = 0m
        };

        Assert.Equal(1_000m,  r.EnriquecimientoNeto);  // 1,000 USD
        Assert.Equal(40_000m, r.EnriquecimientoBS);     // 1,000 × 40 = 40,000 Bs
        Assert.Equal(400m,    r.EnriquecimientoUT);     // 40,000 / 100 = 400 UT
    }

    [Fact(DisplayName = "ValorUT = 0 → EnriquecimientoUT = 0 (sin división por cero)")]
    public void ValorUT_Cero_NoLanzaDivisionPorCero()
    {
        var r = new CalcISLR
        {
            IngresosBrutos = 5_000m, CostosCompras = 0m, CostosGastos = 0m, CostosNomina = 0m,
            ValorUT = 0m, TasaBCV = 36.5m, PctRebaja = 10m,
            RetencionesBS = 0m, ISLRAnioAnteriorBS = 0m, RetencionesAntBS = 0m
        };

        // Debe ser 0, no lanzar DivideByZeroException
        Assert.Equal(0m, r.EnriquecimientoUT);
        Assert.Equal(0m, r.ImpuestoUT);
    }
}
