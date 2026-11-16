namespace ApexUI.Tests;

public class DpiTests
{
    [Fact]
    public void Layout_gets_the_window_size_in_logical_units()
    {
        var root = new FixedBox(10, 10);
        var (host, platform) = TestPlatform.Show(root, 800, 600, dpiScale: 2f);
        platform.Render().Dispose();

        Assert.Equal(new Rect(0, 0, 400, 300), root.LayoutBounds);

        host.UiScale = 2f;
        platform.Render().Dispose();

        Assert.Equal(new Rect(0, 0, 200, 150), root.LayoutBounds);
    }

    [Fact]
    public void Pointer_pixels_are_divided_by_dpi_and_ui_scale()
    {
        var box = new FixedBox(100, 100);
        (float X, float Y)? clicked = null;
        box.Click += e => clicked = (e.X, e.Y);
        var (host, platform) = TestPlatform.Show(box, 800, 600, dpiScale: 2f);
        host.UiScale = 1.5f;
        platform.Render().Dispose();

        platform.Click(300, 150);

        Assert.Equal((100f, 50f), clicked);
    }

    [Fact]
    public void Drawing_is_scaled_once_by_dpi_times_ui_scale()
    {
        var box = new FixedBox(10, 10) { HAlign = HAlign.Left, VAlign = VAlign.Top, Background = SKColors.Red };
        var (host, platform) = TestPlatform.Show(box, 100, 100, dpiScale: 2f);
        host.UiScale = 1.5f;

        using var frame = platform.Render();

        Assert.Equal(SKColors.Red, frame.GetPixel(28, 28));
        Assert.NotEqual(SKColors.Red, frame.GetPixel(32, 32));
    }

    [Fact]
    public void Fonts_are_sized_in_logical_units_because_the_canvas_is_already_scaled()
    {
        using var surface = SKSurface.Create(new SKImageInfo(10, 10));
        using var ctx     = new DrawContext(surface.Canvas, Theme.Light, pixelScale: 2f);

        Assert.Equal(14f, ctx.GetFont(14f).Size);
        using var font = ctx.MakeTextFont(14f);
        Assert.Equal(14f, font.Size);
    }

    [Fact]
    public void Text_measures_the_same_at_any_display_scale()
    {
        var atOne = new Label { Text = "Measure me" };
        var atTwo = new Label { Text = "Measure me" };
        TestPlatform.Show(atOne, 800, 600, dpiScale: 1f);
        TestPlatform.Show(atTwo, 800, 600, dpiScale: 2f);

        Assert.Equal(atOne.DesiredSize, atTwo.DesiredSize);
    }
}
