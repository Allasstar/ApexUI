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
