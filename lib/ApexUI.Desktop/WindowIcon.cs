using Silk.NET.Core;
using Silk.NET.Windowing;

namespace ApexUI.Desktop;

internal static class WindowIcon
{
    // SVG is rendered at several sizes so the OS can pick the best one.
    private static readonly int[] SvgSizes = [16, 32, 48];

    public static void Apply(IWindow window, string path)
    {
        if (!Path.IsPathRooted(path))
            path = Path.Combine(AppContext.BaseDirectory, path);

        if (!File.Exists(path))
        {
            Console.Error.WriteLine($"[ApexUI] Icon not found: {path}");
            return;
        }

        bool isSvg = Path.GetExtension(path).Equals(".svg", StringComparison.OrdinalIgnoreCase);
        var images = isSvg ? FromSvg(path) : FromRaster(path);
        if (images is not null)
            window.SetWindowIcon(images);
    }

    private static RawImage[]? FromSvg(string path)
    {
        using var svg = new Svg.Skia.SKSvg();
        if (svg.Load(path) is null) return null;
        var picture = svg.Picture!;

        var images = new RawImage[SvgSizes.Length];
        for (int i = 0; i < SvgSizes.Length; i++)
            images[i] = Render(picture, SvgSizes[i]);
        return images;
    }

    // ICO files hold several sizes, one per frame; other raster formats have a single frame.
    private static RawImage[]? FromRaster(string path)
    {
        using var codec = SKCodec.Create(path);
        if (codec is null) return null;

        int count  = Math.Max(1, codec.FrameCount);
        var images = new RawImage[count];
        var info   = codec.Info.WithColorType(SKColorType.Rgba8888).WithAlphaType(SKAlphaType.Unpremul);
        for (int i = 0; i < count; i++)
        {
            using var bmp = new SKBitmap(info);
            codec.GetPixels(info, bmp.GetPixels(), new SKCodecOptions(i));
            images[i] = new RawImage(bmp.Width, bmp.Height, new Memory<byte>(bmp.Bytes));
        }
        return images;
    }

    private static RawImage Render(SKPicture picture, int size)
    {
        float scale = size / Math.Max(picture.CullRect.Width, picture.CullRect.Height);
        var info = new SKImageInfo(size, size, SKColorType.Rgba8888, SKAlphaType.Unpremul);
        using var bmp    = new SKBitmap(info);
        using var canvas = new SKCanvas(bmp);
        canvas.Clear(SKColors.Transparent);
        canvas.Scale(scale, scale);
        canvas.DrawPicture(picture);
        return new RawImage(size, size, new Memory<byte>(bmp.Bytes));
    }
}
