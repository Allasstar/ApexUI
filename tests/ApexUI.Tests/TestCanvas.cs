namespace ApexUI.Tests;

/// Lays out and draws a widget into an off-screen raster surface.
internal static class TestCanvas
{
    public static void Draw(Widget widget, int width, int height, Theme? theme = null)
    {
        using var surface = SKSurface.Create(new SKImageInfo(width, height));
        using var ctx     = new DrawContext(surface.Canvas, theme ?? Theme.Light, 1f);
        widget.Measure(new Size(width, height));
        widget.Arrange(new Rect(0, 0, width, height));
        widget.Draw(ctx);
    }

    /// Lays out and draws a widget into a new bitmap the caller disposes, for checking pixels.
    public static SKBitmap Render(Widget widget, int width, int height, Theme? theme = null)
    {
        var bitmap = new SKBitmap(width, height);
        using var canvas = new SKCanvas(bitmap);
        using var ctx    = new DrawContext(canvas, theme ?? Theme.Light, 1f);
        widget.Measure(new Size(width, height));
        widget.Arrange(new Rect(0, 0, width, height));
        widget.Draw(ctx);
        return bitmap;
    }

    /// Fewest bytes allocated by any of several runs of action, after warming it up.
    /// Taking the minimum filters out one-off runtime allocations (tiering, first-call setup).
    public static long MinAllocatedBytes(Action action)
    {
        for (int i = 0; i < 50; i++) action();

        long min = long.MaxValue;
        for (int i = 0; i < 5; i++)
        {
            long before = GC.GetAllocatedBytesForCurrentThread();
            action();
            min = Math.Min(min, GC.GetAllocatedBytesForCurrentThread() - before);
        }
        return min;
    }
}
