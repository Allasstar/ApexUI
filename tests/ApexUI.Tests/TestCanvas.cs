namespace ApexUI.Tests;

/// Lays out and draws a widget into an off-screen raster surface.
internal static class TestCanvas
{
    public static void Draw(Widget widget, int width, int height, Theme? theme = null)
    {
        using var surface = SKSurface.Create(new SKImageInfo(width, height));
        widget.Measure(new Size(width, height));
        widget.Arrange(new Rect(0, 0, width, height));
        widget.Draw(new DrawContext(surface.Canvas, theme ?? Theme.Light, 1f));
    }
}
