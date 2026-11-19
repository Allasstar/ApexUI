namespace ApexUI.Core;

public sealed partial class DrawContext
{
    // ── Raster images ─────────────────────────────────────────────────────────

    public void DrawImage(SKImage image, Rect dest)
    {
        var src = new SKRect(0, 0, image.Width, image.Height);
        Canvas.DrawImage(image, src, dest.ToSKRect());
    }

    public void DrawImage(SKImage image, SKRect src, Rect dest)
        => Canvas.DrawImage(image, src, dest.ToSKRect());

    /// Linear filtering: smooth when an image is scaled.
    public static readonly SKSamplingOptions LinearSampling = new(SKFilterMode.Linear, SKMipmapMode.None);

    /// Nearest neighbour: each source pixel stays a sharp rectangle when scaled.
    public static readonly SKSamplingOptions NearestSampling = new(SKFilterMode.Nearest, SKMipmapMode.None);

    public void DrawImage(SKImage image, Rect dest, SKSamplingOptions sampling)
    {
        var src = new SKRect(0, 0, image.Width, image.Height);
        Canvas.DrawImage(image, src, dest.ToSKRect(), sampling);
    }

    // ── Vector pictures (SVG) ─────────────────────────────────────────────────

    /// Draw an SKPicture scaled to fit dest, given its natural (cull-rect) dimensions.
    public void DrawPicture(SKPicture picture, Rect dest, float naturalW, float naturalH)
    {
        Canvas.Save();
        Canvas.Translate(dest.X, dest.Y);
        Canvas.Scale(dest.Width / naturalW, dest.Height / naturalH);
        Canvas.DrawPicture(picture);
        Canvas.Restore();
    }
}
