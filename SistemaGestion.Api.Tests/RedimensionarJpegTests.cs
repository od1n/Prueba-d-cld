using System.Reflection;
using SkiaSharp;
using Xunit;

namespace SistemaGestion.Api.Tests;

/// <summary>
/// ContenidoEndpoints.RedimensionarJpeg (SkiaSharp): solo PNG, JPEG y WebP;
/// lado mayor limitado; transparencias en blanco; salida JPEG.
/// </summary>
public class RedimensionarJpegTests
{
    private static byte[] Redimensionar(byte[] datos, int maxLado = 1024, int calidad = 85)
    {
        var m = typeof(SistemaGestion.Api.Endpoints.ContenidoEndpoints)
            .GetMethod("RedimensionarJpeg", BindingFlags.NonPublic | BindingFlags.Static)!;
        try { return (byte[])m.Invoke(null, new object[] { datos, maxLado, calidad })!; }
        catch (TargetInvocationException e) { throw e.InnerException!; }
    }

    private static byte[] Imagen(int w, int h, SKEncodedImageFormat formato, SKColor color)
    {
        using var bmp = new SKBitmap(new SKImageInfo(w, h, SKColorType.Rgba8888, SKAlphaType.Unpremul));
        bmp.Erase(color);
        using var img = SKImage.FromBitmap(bmp);
        using var d = img.Encode(formato, 90);
        return d.ToArray();
    }

    private static SKBitmap Leer(byte[] jpg, out SKEncodedImageFormat formato)
    {
        using var codec = SKCodec.Create(new MemoryStream(jpg));
        formato = codec.EncodedFormat;
        return SKBitmap.Decode(jpg);
    }

    [Theory]
    [InlineData(SKEncodedImageFormat.Png)]
    [InlineData(SKEncodedImageFormat.Jpeg)]
    [InlineData(SKEncodedImageFormat.Webp)]
    public void Reduce_ElLadoMayor_YDevuelveJpeg(SKEncodedImageFormat formato)
    {
        var salida = Redimensionar(Imagen(3000, 1500, formato, SKColors.Red));
        using var bmp = Leer(salida, out var f);
        Assert.Equal(SKEncodedImageFormat.Jpeg, f);
        Assert.Equal(1024, bmp.Width);
        Assert.Equal(512, bmp.Height);
    }

    [Fact]
    public void ImagenPequena_NoSeAgranda()
    {
        using var bmp = Leer(Redimensionar(Imagen(300, 200, SKEncodedImageFormat.Png, SKColors.Blue)), out _);
        Assert.Equal(300, bmp.Width);
        Assert.Equal(200, bmp.Height);
    }

    [Fact]
    public void Transparencia_QuedaEnBlanco()
    {
        using var bmp = Leer(Redimensionar(Imagen(50, 50, SKEncodedImageFormat.Png, SKColors.Transparent)), out _);
        var px = bmp.GetPixel(25, 25);
        Assert.True(px.Red > 245 && px.Green > 245 && px.Blue > 245, $"Pixel {px}");
    }

    [Fact]
    public void Rechaza_TiffYBmp()
    {
        var tiff = new byte[] { 0x49, 0x49, 0x2A, 0x00, 0x08, 0x00, 0x00, 0x00, 0x00, 0x00 };
        Assert.Throws<InvalidOperationException>(() => Redimensionar(tiff));

        // BMP de 1x1 (24 bits): Skia sabe leerlo, pero no está permitido.
        var bmp = new byte[] {
            0x42,0x4D, 58,0,0,0, 0,0,0,0, 54,0,0,0,
            40,0,0,0, 1,0,0,0, 1,0,0,0, 1,0, 24,0, 0,0,0,0, 4,0,0,0, 0,0,0,0, 0,0,0,0, 0,0,0,0, 0,0,0,0,
            0,0,255,0 };
        var ex = Assert.Throws<InvalidOperationException>(() => Redimensionar(bmp));
        Assert.Contains("no permitido", ex.Message);
    }
}
