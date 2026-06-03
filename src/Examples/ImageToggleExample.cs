namespace ApexUI.App.Examples;

public class ImageToggleExample : Widget
{
    public ImageToggleExample()
    {
        var rasterImage = Image.FromBitmap(CreateGradientBitmap()).WithSize(280, 180);
        var svgImage    = Image.FromSvgString(SvgSource).WithSize(280, 180);

        var stretchStatus = new Label()
            .WithText("Stretch: Uniform")
            .WithColor(SKColor.FromHex("#888888"))
            .WithSize(13f);

        AddChild(new PaddingBox(
            new Column(
                new Label().WithText("Image & Toggle").WithSize(22).AsBold(),

                // Side-by-side image columns
                new Row(
                    new Column(
                        new Label().WithText("Raster Bitmap").WithSize(13).AsBold(),
                        rasterImage
                    ).WithSpacing(6),
                    new Column(
                        new Label().WithText("Vector SVG").WithSize(13).AsBold(),
                        svgImage
                    ).WithSpacing(6)
                ).WithSpacing(24),

                // Image controls
                new Label().WithText("Image Controls").WithSize(16).AsBold(),
                new Toggle(true, "Show images").OnChange(v =>
                {
                    rasterImage.IsVisible = v;
                    svgImage.IsVisible    = v;
                }),
                new Toggle(false, "Fill stretch (ignores aspect ratio)").OnChange(v =>
                {
                    var stretch         = v ? ImageStretch.Fill : ImageStretch.Uniform;
                    rasterImage.Stretch = stretch;
                    svgImage.Stretch    = stretch;
                    stretchStatus.WithText($"Stretch: {stretch}");
                }),
                stretchStatus,

                // Generic toggle demos
                new Label().WithText("Settings").WithSize(16).AsBold(),
                new Toggle(true,  "Notifications enabled"),
                new Toggle(false, "Dark mode"),
                new Toggle(true,  "Auto-save"),
                new Toggle(false, "High-quality rendering")
            ).WithSpacing(12),
            all: 24
        ));
    }

    private static SKBitmap CreateGradientBitmap()
    {
        const int W = 280, H = 180;
        var bmp = new SKBitmap(W, H);
        using var canvas = new SKCanvas(bmp);

        // Gradient background
        using (var p = new SKPaint
        {
            Shader = SKShader.CreateLinearGradient(
                new SKPoint(0, 0), new SKPoint(W, H),
                new[] { new SKColor(0x89, 0xB4, 0xFA), new SKColor(0xA6, 0xE3, 0xA1) },
                SKShaderTileMode.Clamp),
            IsAntialias = true
        })
            canvas.DrawRect(new SKRect(0, 0, W, H), p);

        // Decorative circles
        using (var p = new SKPaint { Color = new SKColor(0xFF, 0xFF, 0xFF, 0x50), IsAntialias = true })
        {
            canvas.DrawCircle(W * 0.28f, H * 0.40f, 45f, p);
            canvas.DrawCircle(W * 0.75f, H * 0.65f, 55f, p);
        }

        // Label
        using var font      = new SKFont(SKTypeface.Default, 16f);
        using var textPaint = new SKPaint { Color = SKColors.White, IsAntialias = true };
        canvas.DrawText("Raster",  W * 0.5f, H * 0.5f - 9f, SKTextAlign.Center, font, textPaint);
        canvas.DrawText("Bitmap",  W * 0.5f, H * 0.5f + 13f, SKTextAlign.Center, font, textPaint);

        return bmp;
    }

    private const string SvgSource = """
        <svg xmlns="http://www.w3.org/2000/svg" width="280" height="180" viewBox="0 0 280 180">
          <defs>
            <linearGradient id="g" x1="0" y1="0" x2="1" y2="1" gradientUnits="objectBoundingBox">
              <stop offset="0%"   stop-color="#F38BA8"/>
              <stop offset="100%" stop-color="#CBA6F7"/>
            </linearGradient>
          </defs>
          <rect width="280" height="180" fill="url(#g)"/>
          <circle cx="84"  cy="72"  r="45" fill="white" opacity="0.25"/>
          <circle cx="210" cy="117" r="55" fill="white" opacity="0.20"/>
          <text x="140" y="83"  text-anchor="middle" fill="white" font-size="16" font-family="Segoe UI">Vector</text>
          <text x="140" y="107" text-anchor="middle" fill="white" font-size="16" font-family="Segoe UI">SVG</text>
        </svg>
        """;
}
