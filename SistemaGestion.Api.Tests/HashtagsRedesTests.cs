using Xunit;

namespace SistemaGestion.Api.Tests;

/// <summary>
/// Texto final por red: Instagram admite como máximo 5 hashtags por publicación
/// (contando los que están dentro del texto). Facebook no tiene límite.
/// </summary>
public class HashtagsRedesTests
{
    private const string Cuerpo = "El cuero de calidad exige precisión absoluta.\n\nOrgullo hecho en Venezuela desde 1983.";
    private const string Generales = "#EHPSaddles #saddletree #arboldemontura #polietileno #hechoenvenezuela #talabarteria " +
                                     "#saddlemaking #leathercraft #HDPE #equestrianequipment #artesaniaytecnica";
    private const string Base = "#EHPSaddles #hechoenvenezuela";

    [Fact]
    public void Instagram_UsaLosElegidosParaElMercado_YNoMasDeCinco()
    {
        var r = HashtagsRedes.ParaInstagram(Cuerpo, "#EHPSaddles #Medellin #Antioquia #CaballoDePaso #Talabarteria", Generales, Base);
        Assert.Equal(new[] { "#EHPSaddles", "#Medellin", "#Antioquia", "#CaballoDePaso", "#Talabarteria" }, r.Hashtags);
        Assert.EndsWith("\n\n#EHPSaddles #Medellin #Antioquia #CaballoDePaso #Talabarteria", r.Caption);
        Assert.Equal(5, HashtagsRedes.Extraer(r.Caption).Count);
    }

    [Fact]
    public void Instagram_SinElegidos_TomaCincoDeLaListaGeneral_MarcaPrimero()
    {
        var r = HashtagsRedes.ParaInstagram(Cuerpo, "", "#saddletree #HDPE #EHPSaddles #talabarteria #leathercraft #hechoenvenezuela #saddlemaking", Base);
        Assert.Equal(5, r.Hashtags.Count);
        Assert.Equal("#EHPSaddles", r.Hashtags[0]);
        Assert.Equal("#hechoenvenezuela", r.Hashtags[1]);
        Assert.Single(r.Avisos);
    }

    [Fact]
    public void Instagram_CuentaLosHashtagsDelTexto()
    {
        var cuerpo = "Fustes #HDPE para #talabarteria.";
        var r = HashtagsRedes.ParaInstagram(cuerpo, "#EHPSaddles #Medellin #Antioquia #CaballoDePaso #HDPE", Generales, Base);
        Assert.Equal(5, HashtagsRedes.Extraer(r.Caption).Count);
        Assert.Equal(new[] { "#HDPE", "#talabarteria", "#EHPSaddles", "#Medellin", "#Antioquia" }, r.Hashtags);
    }

    [Fact]
    public void Instagram_TextoConMasDeCinco_QuitaLosSobrantes()
    {
        var cuerpo = "Hola #a1 #a2 #a3 #a4 #a5 #a6 #a7 fin";
        var r = HashtagsRedes.ParaInstagram(cuerpo, "#EHPSaddles", "", Base);
        Assert.Equal(5, HashtagsRedes.Extraer(r.Caption).Count);
        Assert.DoesNotContain("#a6", r.Caption);
        Assert.DoesNotContain("#EHPSaddles", r.Caption);
        Assert.NotEmpty(r.Avisos);
    }

    [Fact]
    public void Instagram_ElegidosDeMas_SoloUsaCinco()
    {
        var r = HashtagsRedes.ParaInstagram(Cuerpo, "#a #b #c #d #e #f #g", "", Base);
        Assert.Equal(5, r.Hashtags.Count);
        Assert.Contains(r.Avisos, a => a.Contains("primeros 5"));
    }

    [Fact]
    public void Instagram_NoRepiteSinDistinguirMayusculas()
    {
        var r = HashtagsRedes.ParaInstagram("Texto #ehpsaddles", "#EHPSaddles #WaggaWagga", "", Base);
        Assert.Equal(new[] { "#ehpsaddles", "#WaggaWagga" }, r.Hashtags);
    }

    [Fact]
    public void Facebook_LlevaTodosLosGenerales_MasLosDeInstagramQueFalten()
    {
        var r = HashtagsRedes.ParaFacebook(Cuerpo, "#EHPSaddles #Medellin", Generales);
        Assert.Equal(12, r.Hashtags.Count);
        Assert.Contains("#Medellin", r.Hashtags);
    }

    [Fact]
    public void Normaliza_YAceptaListasSinNumeral()
    {
        Assert.Equal("#WaggaWagga", HashtagsRedes.Normalizar("Wagga Wagga"));
        Assert.Equal(new[] { "#EHPSaddles", "#Riverina" }, HashtagsRedes.Extraer("EHPSaddles, Riverina", true));
        Assert.Equal("#a #b", HashtagsRedes.Unir(new[] { "a", "#A", "b", " " }));
        Assert.Empty(HashtagsRedes.Extraer("Lenguaje C# y página#2"));
        Assert.Empty(HashtagsRedes.Extraer("Texto normal sin hashtags"));
    }
}
