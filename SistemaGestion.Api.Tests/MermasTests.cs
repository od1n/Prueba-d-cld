using Xunit;

namespace SistemaGestion.Api.Tests;

/// <summary>
/// Tests unitarios para la lógica de mermas de producción.
/// Cubre LineaMerma (PctMerma, Nivel) y ResumenMerma (PctMermaTotal).
/// Réplica fiel de DB_Mermas.cs — sin BD ni WinForms.
/// </summary>
public class MermasTests
{
    // ══════════════════════════════════════════════════════════════════════
    // RÉPLICAS (mismas fórmulas que DB_Mermas.cs)
    // ══════════════════════════════════════════════════════════════════════

    private sealed class LineaMerma
    {
        public decimal ConsumoTeorico { get; init; }
        public decimal ConsumoReal    { get; init; }

        public decimal Merma    => ConsumoTeorico - ConsumoReal;
        public decimal PctMerma => ConsumoTeorico > 0
            ? Math.Max(0m, Merma / ConsumoTeorico) : 0m;
        public string Nivel     => PctMerma > 0.15m ? "CRITICO"
                                 : PctMerma > 0.10m ? "ALTO"
                                 : PctMerma > 0.05m ? "MEDIO"
                                 : "NORMAL";
    }

    private sealed class ResumenMerma
    {
        public List<LineaMerma> Lineas { get; init; } = new();

        public decimal MermaTotal    => Lineas.Sum(l => l.Merma);
        public decimal TeoricoTotal  => Lineas.Sum(l => l.ConsumoTeorico);
        public decimal PctMermaTotal => TeoricoTotal > 0
            ? Math.Max(0m, MermaTotal / TeoricoTotal) : 0m;
    }

    // ══════════════════════════════════════════════════════════════════════
    // LINEAMERMA — MERMA Y PORCENTAJE
    // ══════════════════════════════════════════════════════════════════════

    [Fact(DisplayName = "Merma = ConsumoTeorico - ConsumoReal (positiva cuando hay pérdida)")]
    public void Merma_Positiva()
    {
        var l = new LineaMerma { ConsumoTeorico = 100m, ConsumoReal = 85m };
        Assert.Equal(15m, l.Merma);
    }

    [Fact(DisplayName = "Merma = 0 cuando consumo real iguala al teórico")]
    public void Merma_Cero_CuandoIguales()
    {
        var l = new LineaMerma { ConsumoTeorico = 100m, ConsumoReal = 100m };
        Assert.Equal(0m, l.Merma);
    }

    [Fact(DisplayName = "PctMerma = 0 cuando ConsumoReal > ConsumoTeorico (Max con 0)")]
    public void PctMerma_NoNegativo_CuandoRealSuperaTeorico()
    {
        var l = new LineaMerma { ConsumoTeorico = 100m, ConsumoReal = 110m };
        Assert.True(l.Merma < 0, "Merma debe ser negativa en este caso");
        Assert.Equal(0m, l.PctMerma);  // Max(0, negative) = 0
    }

    [Fact(DisplayName = "PctMerma = 0 cuando ConsumoTeorico = 0 (sin división por cero)")]
    public void PctMerma_NoExcepcion_ConsumoTeoricosCero()
    {
        var l = new LineaMerma { ConsumoTeorico = 0m, ConsumoReal = 5m };
        Assert.Equal(0m, l.PctMerma);
    }

    [Fact(DisplayName = "PctMerma = 15% cuando se pierden 15 de 100 unidades")]
    public void PctMerma_15PorCiento()
    {
        var l = new LineaMerma { ConsumoTeorico = 100m, ConsumoReal = 85m };
        Assert.Equal(0.15m, l.PctMerma);
    }

    [Fact(DisplayName = "PctMerma = 100% cuando todo el material se pierde")]
    public void PctMerma_100PorCiento()
    {
        var l = new LineaMerma { ConsumoTeorico = 50m, ConsumoReal = 0m };
        Assert.Equal(1m, l.PctMerma);
    }

    // ══════════════════════════════════════════════════════════════════════
    // LINEAMERMA — NIVEL DE CRITICIDAD
    // ══════════════════════════════════════════════════════════════════════

    [Fact(DisplayName = "Nivel = NORMAL cuando PctMerma ≤ 5%")]
    public void Nivel_Normal_MenorIgual5Pct()
    {
        var l = new LineaMerma { ConsumoTeorico = 100m, ConsumoReal = 95m }; // 5%
        Assert.Equal("NORMAL", l.Nivel);
    }

    [Fact(DisplayName = "Nivel = NORMAL cuando PctMerma = 0%")]
    public void Nivel_Normal_SinMerma()
    {
        var l = new LineaMerma { ConsumoTeorico = 100m, ConsumoReal = 100m };
        Assert.Equal("NORMAL", l.Nivel);
    }

    [Fact(DisplayName = "Nivel = MEDIO cuando PctMerma > 5% y ≤ 10%")]
    public void Nivel_Medio_Entre5y10Pct()
    {
        // 7% de merma
        var l = new LineaMerma { ConsumoTeorico = 100m, ConsumoReal = 93m };
        Assert.Equal("MEDIO", l.Nivel);
    }

    [Fact(DisplayName = "Nivel = ALTO cuando PctMerma > 10% y ≤ 15%")]
    public void Nivel_Alto_Entre10y15Pct()
    {
        // 12% de merma
        var l = new LineaMerma { ConsumoTeorico = 100m, ConsumoReal = 88m };
        Assert.Equal("ALTO", l.Nivel);
    }

    [Fact(DisplayName = "Nivel = CRITICO cuando PctMerma > 15%")]
    public void Nivel_Critico_Mayor15Pct()
    {
        // 20% de merma
        var l = new LineaMerma { ConsumoTeorico = 100m, ConsumoReal = 80m };
        Assert.Equal("CRITICO", l.Nivel);
    }

    [Theory(DisplayName = "Nivel en los límites exactos de cada tramo")]
    [InlineData(100, 100,  "NORMAL")]   //  0% → NORMAL
    [InlineData(100,  95,  "NORMAL")]   //  5% → NORMAL (no es estrictamente mayor)
    [InlineData(100,  94,  "MEDIO")]    //  6% → MEDIO
    [InlineData(100,  90,  "MEDIO")]    // 10% → MEDIO (no es estrictamente mayor)
    [InlineData(100,  89,  "ALTO")]     // 11% → ALTO
    [InlineData(100,  85,  "ALTO")]     // 15% → ALTO (no es estrictamente mayor)
    [InlineData(100,  84,  "CRITICO")]  // 16% → CRITICO
    [InlineData(100,   0,  "CRITICO")]  // 100% → CRITICO
    public void Nivel_EnLimitesDeTramos(int teorico, int real, string nivelEsperado)
    {
        var l = new LineaMerma
        {
            ConsumoTeorico = (decimal)teorico,
            ConsumoReal    = (decimal)real
        };
        Assert.Equal(nivelEsperado, l.Nivel);
    }

    // ══════════════════════════════════════════════════════════════════════
    // RESUMENMERMA
    // ══════════════════════════════════════════════════════════════════════

    [Fact(DisplayName = "PctMermaTotal = 0 cuando no hay líneas")]
    public void PctMermaTotal_SinLineas_EsCero()
    {
        var r = new ResumenMerma { Lineas = new List<LineaMerma>() };
        Assert.Equal(0m, r.PctMermaTotal);
    }

    [Fact(DisplayName = "PctMermaTotal = 0 cuando TeoricoTotal = 0 (sin división por cero)")]
    public void PctMermaTotal_TeoricoTotalCero_EsCero()
    {
        var r = new ResumenMerma
        {
            Lineas = new List<LineaMerma>
            {
                new() { ConsumoTeorico = 0m, ConsumoReal = 0m }
            }
        };
        Assert.Equal(0m, r.PctMermaTotal);
    }

    [Fact(DisplayName = "MermaTotal = suma de Merma de todas las líneas")]
    public void MermaTotal_SumaLineas()
    {
        var r = new ResumenMerma
        {
            Lineas = new List<LineaMerma>
            {
                new() { ConsumoTeorico = 100m, ConsumoReal = 90m },  // merma 10
                new() { ConsumoTeorico =  50m, ConsumoReal = 40m },  // merma 10
                new() { ConsumoTeorico =  80m, ConsumoReal = 70m }   // merma 10
            }
        };
        Assert.Equal(30m, r.MermaTotal);
        Assert.Equal(230m, r.TeoricoTotal);
    }

    [Fact(DisplayName = "PctMermaTotal = MermaTotal / TeoricoTotal en múltiples líneas")]
    public void PctMermaTotal_MultipleLineas()
    {
        // 3 líneas: total teórico = 200, total merma = 20 → 10%
        var r = new ResumenMerma
        {
            Lineas = new List<LineaMerma>
            {
                new() { ConsumoTeorico = 100m, ConsumoReal = 90m },   // 10
                new() { ConsumoTeorico = 100m, ConsumoReal = 90m }    // 10
            }
        };
        Assert.Equal(0.10m, r.PctMermaTotal);
    }

    [Fact(DisplayName = "PctMermaTotal no puede ser negativo aunque alguna línea tenga ConsumoReal > Teórico")]
    public void PctMermaTotal_NoNegativo()
    {
        // Una línea con merma positiva y otra con "merma" negativa (sobreconsumo)
        var r = new ResumenMerma
        {
            Lineas = new List<LineaMerma>
            {
                new() { ConsumoTeorico = 100m, ConsumoReal = 50m },   // merma +50
                new() { ConsumoTeorico = 100m, ConsumoReal = 200m }   // merma -100
            }
        };
        // MermaTotal = 50 + (-100) = -50 → PctMermaTotal = Max(0, -50/200) = 0
        Assert.True(r.PctMermaTotal >= 0m);
        Assert.Equal(0m, r.PctMermaTotal);
    }

    [Fact(DisplayName = "PctMermaTotal = 100% cuando no se recupera nada del material")]
    public void PctMermaTotal_100PorCiento()
    {
        var r = new ResumenMerma
        {
            Lineas = new List<LineaMerma>
            {
                new() { ConsumoTeorico = 100m, ConsumoReal = 0m },
                new() { ConsumoTeorico =  50m, ConsumoReal = 0m }
            }
        };
        Assert.Equal(1m, r.PctMermaTotal);
    }
}
