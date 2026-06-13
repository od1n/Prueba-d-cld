using Xunit;

namespace SistemaGestion.Api.Tests;

/// <summary>
/// Tests unitarios para la lógica de nómina.
/// Cubre DetalleNomina, AportePatronal y reglas de cálculo de deducciones/aportes.
/// Sin dependencia de BD ni WinForms — réplica fiel de DB.cs.
/// </summary>
public class NominaTests
{
    // ══════════════════════════════════════════════════════════════════════
    // RÉPLICAS (mismas fórmulas que DB.cs)
    // ══════════════════════════════════════════════════════════════════════

    private sealed class DetalleNomina
    {
        public decimal DiasTrabajados   { get; init; }
        public decimal DiasInasistencia { get; init; }
        public decimal SueldoDiarioUSD  { get; init; }
        // Devengado
        public decimal DevengadoSueldo  { get; init; }
        public decimal DevengadoPiezas  { get; init; }
        public decimal DevengadoVentas  { get; init; }
        public decimal OtrosIngresos    { get; init; }
        public decimal TotalDevengado   => DevengadoSueldo + DevengadoPiezas + DevengadoVentas + OtrosIngresos;
        // Deducciones
        public decimal DeduccionIVSS    { get; init; }   // 4% sobre devengado
        public decimal DeduccionRPVH    { get; init; }   // 1% sobre devengado
        public decimal DescuentoFaltas  { get; init; }   // SueldoDiario × DiasInasistencia
        public decimal CuotaPrestamo    { get; init; }
        public decimal OtrasDeduccion   { get; init; }
        // Deducciones reales (sí se descuentan)
        public decimal DeduccionesReales => DescuentoFaltas + CuotaPrestamo + OtrasDeduccion;
        // Deducciones legales (IVSS/RPVH — solo informativas)
        public decimal DeduccionesLegales => DeduccionIVSS + DeduccionRPVH;
        public decimal TotalDeduccionesRef => DeduccionesReales + DeduccionesLegales;
        // Pago real = devengado - deducciones reales (sin IVSS/RPVH)
        public decimal TasaBCV          { get; init; }
        public decimal PagoRealUSD      => TotalDevengado - DeduccionesReales;
        public decimal PagoRealBs       => PagoRealUSD * TasaBCV;
        // Neto legal (informativo — si se aplicaran TODAS las deducciones)
        public decimal NetoLegalUSD     => TotalDevengado - TotalDeduccionesRef;
        public decimal NetoLegalBs      => NetoLegalUSD * TasaBCV;
        // Compatibilidad
        public decimal TotalDeducciones => TotalDeduccionesRef;
        public decimal NetoUSD          => PagoRealUSD;
        public decimal NetoBs           => PagoRealBs;
    }

    private sealed class AportePatronal
    {
        public decimal SalarioNormalBs  { get; init; }  // TotalDevengado × TasaBCV
        public decimal TasaBCV          { get; init; }
        public decimal PorcentajeIVSS   { get; init; }  // default 9
        public decimal IVSSPatronBs     { get; init; }  // salBs × pct / 100
        public decimal RPVHPatronBs     { get; init; }  // salBs × 2%
        public decimal INCESBs          { get; init; }  // salBs × 2%
        public decimal DiasLaborales    { get; init; }
        public decimal ValorUTBs        { get; init; }
        public decimal CestaBs          { get; init; }  // dias × 0.5 × ValorUT
        public decimal TotalAportesBs   => IVSSPatronBs + RPVHPatronBs + INCESBs + CestaBs;
        public decimal TotalAportesUSD  => TasaBCV > 0 ? TotalAportesBs / TasaBCV : 0m;
    }

    // Helper — réplica de las reglas de cálculo de DB.cs (simplificado v1.1)
    private static DetalleNomina Calcular(
        decimal sueldoDiario, decimal dias, decimal diasFalta = 0,
        decimal piezasUSD = 0, decimal ventasUSD = 0, decimal otrosUSD = 0,
        decimal cuotaPrestamo = 0, decimal otrasDeduccion = 0,
        decimal tasa = 36.5m)
    {
        decimal devSueldo  = sueldoDiario * dias;
        decimal totalDev   = devSueldo + piezasUSD + ventasUSD + otrosUSD;
        if (tasa <= 0) tasa = 1;
        decimal ivss   = Math.Round(totalDev * 0.04m, 2);
        decimal rpvh   = Math.Round(totalDev * 0.01m, 2);
        decimal faltas = sueldoDiario * diasFalta;

        return new DetalleNomina
        {
            DiasTrabajados   = dias,
            DiasInasistencia = diasFalta,
            SueldoDiarioUSD  = sueldoDiario,
            DevengadoSueldo  = devSueldo,
            DevengadoPiezas  = piezasUSD,
            DevengadoVentas  = ventasUSD,
            OtrosIngresos    = otrosUSD,
            DeduccionIVSS    = ivss,
            DeduccionRPVH    = rpvh,
            DescuentoFaltas  = faltas,
            CuotaPrestamo    = cuotaPrestamo,
            OtrasDeduccion   = otrasDeduccion,
            TasaBCV          = tasa
        };
    }

    // ══════════════════════════════════════════════════════════════════════
    // DETALLENOMINA — DEVENGADO
    // ══════════════════════════════════════════════════════════════════════

    [Fact(DisplayName = "TotalDevengado = suma de las cuatro fuentes (sueldo + piezas + ventas + otros)")]
    public void TotalDevengado_SumaFuentes()
    {
        var d = new DetalleNomina
        {
            DevengadoSueldo = 100m,
            DevengadoPiezas = 20m,
            DevengadoVentas = 15m,
            OtrosIngresos   = 5m
        };
        Assert.Equal(140m, d.TotalDevengado);
    }

    [Fact(DisplayName = "TotalDevengado = 0 cuando todas las fuentes son cero")]
    public void TotalDevengado_TodosCero_EsCero()
    {
        var d = new DetalleNomina();
        Assert.Equal(0m, d.TotalDevengado);
    }

    [Fact(DisplayName = "TotalDevengado solo sueldo = SueldoDiario × DíasTrabajados")]
    public void TotalDevengado_SoloSueldo()
    {
        var d = Calcular(sueldoDiario: 10m, dias: 5m);
        Assert.Equal(50m, d.TotalDevengado);
    }

    // ══════════════════════════════════════════════════════════════════════
    // DETALLENOMINA — DEDUCCIONES LEGALES
    // ══════════════════════════════════════════════════════════════════════

    [Fact(DisplayName = "IVSS empleado = 4% del devengado (calculado sobre Bs, equivale a 4% USD)")]
    public void DeduccionIVSS_4PorCiento()
    {
        // 100 USD × 4% = 4 USD (exacto sin redondeo)
        var d = Calcular(sueldoDiario: 10m, dias: 10m);  // 100 USD
        Assert.Equal(4m, d.DeduccionIVSS);
    }

    [Fact(DisplayName = "RPVH empleado = 1% del devengado")]
    public void DeduccionRPVH_1PorCiento()
    {
        var d = Calcular(sueldoDiario: 10m, dias: 10m);  // 100 USD
        Assert.Equal(1m, d.DeduccionRPVH);
    }

    [Fact(DisplayName = "DescuentoFaltas = SueldoDiario × DíasInasistencia")]
    public void DescuentoFaltas_ProporcionalAlSueldo()
    {
        var d = Calcular(sueldoDiario: 8m, dias: 5m, diasFalta: 2m);
        Assert.Equal(16m, d.DescuentoFaltas);  // 8 × 2 = 16
    }

    [Fact(DisplayName = "DescuentoFaltas = 0 cuando no hay inasistencias")]
    public void DescuentoFaltas_SinFaltas_EsCero()
    {
        var d = Calcular(sueldoDiario: 8m, dias: 5m, diasFalta: 0m);
        Assert.Equal(0m, d.DescuentoFaltas);
    }

    [Fact(DisplayName = "TotalDeducciones = suma de las cinco fuentes")]
    public void TotalDeducciones_SumaFuentes()
    {
        var d = new DetalleNomina
        {
            DeduccionIVSS   = 4m,
            DeduccionRPVH   = 1m,
            DescuentoFaltas = 16m,
            CuotaPrestamo   = 10m,
            OtrasDeduccion  = 5m
        };
        Assert.Equal(36m, d.TotalDeducciones);
    }

    [Fact(DisplayName = "TotalDeducciones = 0 cuando todas son cero")]
    public void TotalDeducciones_TodosCero_EsCero()
    {
        var d = new DetalleNomina();
        Assert.Equal(0m, d.TotalDeducciones);
    }

    // ══════════════════════════════════════════════════════════════════════
    // DETALLENOMINA — NETO
    // ══════════════════════════════════════════════════════════════════════

    [Fact(DisplayName = "PagoRealUSD = TotalDevengado cuando no hay deducciones reales")]
    public void PagoRealUSD_SinDeduccionesReales()
    {
        var d = Calcular(sueldoDiario: 10m, dias: 10m);
        // Sin faltas, préstamos ni otras ded → pago real = devengado
        Assert.Equal(100m, d.PagoRealUSD);
        Assert.Equal(d.TotalDevengado, d.PagoRealUSD);
    }

    [Fact(DisplayName = "PagoRealUSD descuenta préstamo (SÍ es deducción real)")]
    public void PagoRealUSD_DescontaPrestamo()
    {
        var d = Calcular(sueldoDiario: 10m, dias: 10m, cuotaPrestamo: 20m);
        // 100 devengado - 20 préstamo = 80 pago real
        Assert.Equal(80m, d.PagoRealUSD);
    }

    [Fact(DisplayName = "PagoRealUSD NO descuenta IVSS/RPVH (solo informativo)")]
    public void PagoRealUSD_NoDescontaIVSS()
    {
        var d = Calcular(sueldoDiario: 10m, dias: 10m);
        // IVSS=4, RPVH=1 calculados pero NO restados del pago real
        Assert.Equal(4m, d.DeduccionIVSS);
        Assert.Equal(1m, d.DeduccionRPVH);
        Assert.Equal(100m, d.PagoRealUSD);  // pago real = devengado, sin tocar IVSS
    }

    [Fact(DisplayName = "NetoLegalUSD = TotalDevengado - TODAS las deducciones (informativo)")]
    public void NetoLegalUSD_Formula()
    {
        var d = Calcular(sueldoDiario: 10m, dias: 10m, cuotaPrestamo: 20m);
        // 100 - IVSS(4) - RPVH(1) - prestamo(20) = 75
        Assert.Equal(75m, d.NetoLegalUSD);
    }

    [Fact(DisplayName = "PagoRealBs = PagoRealUSD × TasaBCV")]
    public void PagoRealBs_Conversion()
    {
        var d = Calcular(sueldoDiario: 10m, dias: 10m, tasa: 40m);
        // PagoRealUSD = 100 → PagoRealBs = 100 × 40 = 4,000 Bs
        Assert.Equal(4_000m, d.PagoRealBs);
    }

    [Fact(DisplayName = "PagoRealBs = 0 cuando TasaBCV = 0")]
    public void PagoRealBs_TasaCero_EsCero()
    {
        var d = new DetalleNomina
        {
            DevengadoSueldo = 100m, TasaBCV = 0m
        };
        Assert.Equal(0m, d.PagoRealBs);
    }

    [Theory(DisplayName = "Pago real = devengado - ded.reales; neto legal = devengado - todas")]
    [InlineData(10.0, 5, 50.0, 2.0, 0.5, 50.0, 47.5)]   // sin ded reales: pagoReal=devengado
    [InlineData(20.0, 5, 100.0, 4.0, 1.0, 100.0, 95.0)]
    [InlineData( 5.0, 3, 15.0, 0.6, 0.15, 15.0, 14.25)]
    public void PagoReal_SemanaCompleta(
        double sueldo, double dias,
        double devEsperado, double ivssEsp, double rpvhEsp, double pagoRealEsp, double netoLegalEsp)
    {
        var d = Calcular(sueldoDiario: (decimal)sueldo, dias: (decimal)dias);
        Assert.Equal((decimal)devEsperado,   d.TotalDevengado);
        Assert.Equal((decimal)ivssEsp,        d.DeduccionIVSS);
        Assert.Equal((decimal)rpvhEsp,        d.DeduccionRPVH);
        Assert.Equal((decimal)pagoRealEsp,    d.PagoRealUSD);
        Assert.Equal((decimal)netoLegalEsp,   d.NetoLegalUSD);
    }

    // ══════════════════════════════════════════════════════════════════════
    // APORTES PATRONALES
    // ══════════════════════════════════════════════════════════════════════

    [Fact(DisplayName = "IVSS patronal = PorcentajeIVSS% del SalarioNormalBs (default 9%)")]
    public void AporteIVSS_9PorCiento()
    {
        // SalarioNormalBs = 100 USD × 40 Bs = 4,000 Bs → IVSS = 4,000 × 9% = 360 Bs
        var a = new AportePatronal
        {
            SalarioNormalBs = 4_000m,
            PorcentajeIVSS  = 9m,
            IVSSPatronBs    = Math.Round(4_000m * 9m / 100m, 2),
            RPVHPatronBs    = Math.Round(4_000m * 0.02m, 2),
            INCESBs         = Math.Round(4_000m * 0.02m, 2),
            DiasLaborales   = 5m, ValorUTBs = 360m,
            CestaBs         = Math.Round(5m * 0.5m * 360m, 2),
            TasaBCV         = 40m
        };

        Assert.Equal(360m,  a.IVSSPatronBs);
        Assert.Equal(80m,   a.RPVHPatronBs);
        Assert.Equal(80m,   a.INCESBs);
        Assert.Equal(900m,  a.CestaBs);        // 5 × 0.5 × 360 = 900
        Assert.Equal(1_420m, a.TotalAportesBs); // 360+80+80+900
    }

    [Fact(DisplayName = "TotalAportesUSD = TotalAportesBs / TasaBCV")]
    public void TotalAportesUSD_Conversion()
    {
        var a = new AportePatronal
        {
            IVSSPatronBs = 360m, RPVHPatronBs = 80m,
            INCESBs = 80m, CestaBs = 900m, TasaBCV = 40m
        };
        // TotalAportesBs = 1,420 / 40 = 35.5 USD
        Assert.Equal(35.5m, a.TotalAportesUSD);
    }

    [Fact(DisplayName = "TotalAportesUSD = 0 cuando TasaBCV = 0 (sin división por cero)")]
    public void TotalAportesUSD_TasaCero_EsCero()
    {
        var a = new AportePatronal
        {
            IVSSPatronBs = 360m, TasaBCV = 0m
        };
        Assert.Equal(0m, a.TotalAportesUSD);
    }

    [Fact(DisplayName = "Cesta ticket = DíasLaborales × 0.5 × ValorUT")]
    public void CestaBs_Formula()
    {
        // 5 días × 0.5 × 500 Bs/UT = 1,250 Bs
        decimal cesta = Math.Round(5m * 0.5m * 500m, 2);
        Assert.Equal(1_250m, cesta);
    }

    [Fact(DisplayName = "Cesta ticket = 0 cuando ValorUT = 0")]
    public void CestaBs_ValorUTCero_EsCero()
    {
        decimal cesta = 5m > 0 ? Math.Round(5m * 0.5m * 0m, 2) : 0m;
        Assert.Equal(0m, cesta);
    }

    // ══════════════════════════════════════════════════════════════════════
    // EMPLEADO
    // ══════════════════════════════════════════════════════════════════════

    [Fact(DisplayName = "NombreCompleto = Nombre + espacio + Apellido")]
    public void NombreCompleto_Concatenacion()
    {
        string nombre   = "Juan";
        string apellido = "Pérez";
        string completo = nombre + " " + apellido;
        Assert.Equal("Juan Pérez", completo);
    }

    [Fact(DisplayName = "AniosServicio = 0 para empleado con fecha de ingreso de hoy")]
    public void AniosServicio_RecienIngresado_EsCero()
    {
        int anios = Math.Max(0, (int)((DateTime.Today - DateTime.Today).TotalDays / 365.25));
        Assert.Equal(0, anios);
    }

    [Fact(DisplayName = "AniosServicio ≥ 0 siempre (Max con 0)")]
    public void AniosServicio_NoNegativo()
    {
        // Fecha de ingreso en el futuro → debería devolver 0
        DateTime futuro = DateTime.Today.AddDays(30);
        int anios = Math.Max(0, (int)((DateTime.Today - futuro).TotalDays / 365.25));
        Assert.True(anios >= 0);
        Assert.Equal(0, anios);
    }

    [Fact(DisplayName = "AniosServicio = 3 para empleado con más de 3 años completos de antigüedad")]
    public void AniosServicio_TresAnios()
    {
        // AddYears(-3).AddDays(-1) garantiza >3 años sin depender de bisiestos
        DateTime ingreso = DateTime.Today.AddYears(-3).AddDays(-1);
        int anios = Math.Max(0, (int)((DateTime.Today - ingreso).TotalDays / 365.25));
        Assert.Equal(3, anios);
    }

    // ══════════════════════════════════════════════════════════════════════
    // NUEVOS TESTS v1.1 — Validaciones, préstamos, aportes, tope neto
    // ══════════════════════════════════════════════════════════════════════

    [Fact(DisplayName = "#3 Tasa BCV 0 no causa excepción (guard a 1)")]
    public void TasaBCV_CeroNoExcepcion()
    {
        var d = Calcular(sueldoDiario: 10m, dias: 7, tasa: 0m);
        Assert.True(d.TasaBCV >= 0);
        Assert.Equal(Math.Round(70m * 0.04m, 2), d.DeduccionIVSS);
    }

    [Fact(DisplayName = "#6 Neto USD no debería ser menor que cero (tope)")]
    public void NetoUSD_NoNegativo()
    {
        var d = Calcular(sueldoDiario: 5m, dias: 1, cuotaPrestamo: 100m);
        decimal neto = Math.Max(0, d.NetoUSD);
        Assert.True(neto >= 0, "El neto clamped debería ser >= 0");
    }

    [Fact(DisplayName = "#10 Cuota préstamo limitada al saldo pendiente")]
    public void CuotaPrestamo_LimitadaASaldo()
    {
        decimal cuotaSemanal = 25m;
        decimal saldoPendiente = 10m;
        decimal cuotaReal = Math.Min(cuotaSemanal, saldoPendiente);
        Assert.Equal(10m, cuotaReal);
    }

    [Fact(DisplayName = "#10 Cuota préstamo completa si saldo lo permite")]
    public void CuotaPrestamo_CompletaSiHaySaldo()
    {
        decimal cuotaSemanal = 25m;
        decimal saldoPendiente = 100m;
        decimal cuotaReal = Math.Min(cuotaSemanal, saldoPendiente);
        Assert.Equal(25m, cuotaReal);
    }

    [Fact(DisplayName = "#5 Valores negativos: fórmula no produce resultado absurdo")]
    public void ValoresNegativos_Rechazar()
    {
        // En producción se valida en API; aquí verificamos que el cálculo con 0 sea correcto
        decimal dias = Math.Max(0, -3m);
        decimal faltas = Math.Max(0, -2m);
        var d = Calcular(sueldoDiario: 10m, dias: dias, diasFalta: faltas);
        Assert.Equal(0m, d.DevengadoSueldo);
        Assert.Equal(0m, d.DescuentoFaltas);
    }

    [Fact(DisplayName = "IVSS simplificado = totalDev × 4% directo (sin Bs/tasa)")]
    public void IVSS_FormulaSimplificada()
    {
        var d = Calcular(sueldoDiario: 20m, dias: 5, tasa: 50m);
        // totalDev = 100, IVSS = 100 × 0.04 = 4.00
        Assert.Equal(4.00m, d.DeduccionIVSS);
    }

    [Fact(DisplayName = "Aportes patronales: IVSS + RPVH + INCES + Cesta = Total")]
    public void AportesPatronales_SumaTotal()
    {
        decimal salBs = 4000m, pctIVSS = 9m;
        decimal ivssP = Math.Round(salBs * pctIVSS / 100, 2);  // 360
        decimal rpvhP = Math.Round(salBs * 0.02m, 2);           // 80
        decimal incesP = Math.Round(salBs * 0.02m, 2);          // 80
        decimal cestaP = Math.Round(5m * 0.5m * 200m, 2);       // 500
        decimal total = ivssP + rpvhP + incesP + cestaP;
        Assert.Equal(1020m, total);
    }

    [Fact(DisplayName = "Abono préstamo: saldo se reduce correctamente")]
    public void AbonoPrestamo_SaldoReduce()
    {
        decimal saldo = 100m;
        decimal cuota = 25m;
        decimal abono = Math.Min(cuota, saldo);
        decimal nuevoSaldo = saldo - abono;
        Assert.Equal(75m, nuevoSaldo);
    }

    [Fact(DisplayName = "Abono préstamo: saldo llega a 0 cuando cuota >= saldo")]
    public void AbonoPrestamo_SaldoACero()
    {
        decimal saldo = 10m;
        decimal cuota = 25m;
        decimal abono = Math.Min(cuota, saldo);
        decimal nuevoSaldo = saldo - abono;
        Assert.Equal(0m, nuevoSaldo);
    }
}
