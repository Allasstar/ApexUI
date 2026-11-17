namespace ApexUI.Tests;

public class ViewportTests
{
    private const float Tolerance = 1e-3f;

    private static Viewport Arranged(float width = 400, float height = 300, Widget? content = null)
    {
        var view = new Viewport(content);
        view.Measure(new Size(width, height));
        view.Arrange(new Rect(0, 0, width, height));
        return view;
    }

    [Fact]
    public void Fills_the_space_it_is_given()
    {
        var view = Arranged(400, 300);

        Assert.Equal(new Rect(0, 0, 400, 300), view.ViewRect);
        Assert.Equal(new Size(400, 300), view.DesiredSize);
    }

    [Fact]
    public void Content_and_view_coordinates_round_trip()
    {
        var view = Arranged();
        view.SetView(30, -20, 2.5f);

        var (vx, vy) = view.ContentToView(12, 34);
        var (cx, cy) = view.ViewToContent(vx, vy);

        Assert.Equal(30 + 12 * 2.5f, vx, Tolerance);
        Assert.Equal(-20 + 34 * 2.5f, vy, Tolerance);
        Assert.Equal(12, cx, Tolerance);
        Assert.Equal(34, cy, Tolerance);
    }

    [Fact]
    public void ZoomAt_keeps_the_content_point_under_the_anchor()
    {
        var view = Arranged();
        view.SetView(15, 25, 1.5f);
        var before = view.ViewToContent(120, 80);

        view.ZoomAt(120, 80, 3f);
        var after = view.ViewToContent(120, 80);

        Assert.Equal(4.5f, view.Zoom, Tolerance);
        Assert.Equal(before.X, after.X, Tolerance);
        Assert.Equal(before.Y, after.Y, Tolerance);
    }

    [Fact]
    public void Zoom_is_clamped_to_the_range()
    {
        var view = Arranged().WithZoomRange(0.5f, 4f);

        view.ZoomAtCenter(100f);
        Assert.Equal(4f, view.Zoom, Tolerance);

        view.ZoomAtCenter(0.001f);
        Assert.Equal(0.5f, view.Zoom, Tolerance);

        view.MaxZoom = 0.25f;
        view.MinZoom = 0.1f;
        Assert.Equal(0.25f, view.Zoom, Tolerance);
    }

    [Fact]
    public void Fit_centers_the_content_with_padding()
    {
        var view = Arranged(400, 300);

        view.Fit(new Rect(0, 0, 100, 100), 50);

        Assert.Equal(2f, view.Zoom, Tolerance);
        var (left, top)     = view.ContentToView(0, 0);
        var (right, bottom) = view.ContentToView(100, 100);
        Assert.Equal(100, left, Tolerance);
        Assert.Equal(50, top, Tolerance);
        Assert.Equal(300, right, Tolerance);
        Assert.Equal(250, bottom, Tolerance);
    }

    [Fact]
    public void Fit_before_the_first_layout_applies_on_arrange()
    {
        var view = new Viewport();
        view.Fit(new Rect(-50, -50, 100, 100), 0);

        view.Measure(new Size(200, 200));
        view.Arrange(new Rect(0, 0, 200, 200));

        Assert.Equal(2f, view.Zoom, Tolerance);
        var (cx, cy) = view.ContentToView(0, 0);
        Assert.Equal(100, cx, Tolerance);
        Assert.Equal(100, cy, Tolerance);
    }

    [Fact]
    public void Resize_fits_again_until_the_view_is_changed()
    {
        var view = Arranged(200, 200);
        view.Fit(new Rect(0, 0, 100, 100), 0);

        view.Measure(new Size(400, 400));
        view.Arrange(new Rect(0, 0, 400, 400));
        Assert.Equal(4f, view.Zoom, Tolerance);

        view.PanBy(10, 0);
        view.Measure(new Size(800, 800));
        view.Arrange(new Rect(0, 0, 800, 800));
        Assert.Equal(4f, view.Zoom, Tolerance);
        Assert.Equal(10f, view.PanX, Tolerance);
    }

    [Fact]
    public void ViewChanged_fires_only_when_the_view_changes()
    {
        var view = Arranged();
        int changes = 0;
        view.ViewChanged += () => changes++;

        view.SetView(10, 10, 2f);
        view.SetView(10, 10, 2f);
        view.PanBy(0, 0);

        Assert.Equal(1, changes);
    }

    [Fact]
    public void Wheel_up_zooms_in_around_the_pointer()
    {
        var view = new Viewport();
        var (_, platform) = TestPlatform.Show(view, 400, 300);
        platform.Render().Dispose();
        var before = view.ViewToContent(100, 60);

        platform.Move(100, 60);
        platform.Listener.Wheel(0, 2, KeyModifiers.None);

        Assert.Equal(1.15f * 1.15f, view.Zoom, Tolerance);
        var after = view.ViewToContent(100, 60);
        Assert.Equal(before.X, after.X, Tolerance);
        Assert.Equal(before.Y, after.Y, Tolerance);
    }

    [Fact]
    public void Middle_drag_pans_and_shows_the_pan_cursor()
    {
        var view = new Viewport { Cursor = CursorShape.Crosshair };
        var (_, platform) = TestPlatform.Show(view, 400, 300);
        platform.Render().Dispose();

        platform.Move(100, 100);
        platform.Listener.PointerDown(100, 100, PointerButton.Middle, KeyModifiers.None);
        Assert.True(view.IsPanning);
        Assert.Equal(CursorShape.Move, platform.Cursor);

        platform.Move(130, 80);
        Assert.Equal(30f, view.PanX, Tolerance);
        Assert.Equal(-20f, view.PanY, Tolerance);

        platform.Listener.PointerUp(130, 80, PointerButton.Middle, KeyModifiers.None);
        Assert.False(view.IsPanning);
        Assert.Equal(CursorShape.Crosshair, platform.Cursor);
    }

    [Fact]
    public void Left_drag_pans_only_when_left_is_a_pan_button()
    {
        var view = new Viewport();
        var (_, platform) = TestPlatform.Show(view, 400, 300);
        platform.Render().Dispose();

        platform.Move(100, 100);
        platform.Press(100, 100);
        platform.Move(150, 100);
        platform.Release(150, 100);
        Assert.Equal(0f, view.PanX, Tolerance);

        view.PanButtons |= PointerButtons.Left;
        platform.Press(150, 100);
        platform.Move(200, 100);
        platform.Release(200, 100);
        Assert.Equal(50f, view.PanX, Tolerance);
    }

    [Fact]
    public void Pan_drag_keeps_going_when_it_starts_on_a_child()
    {
        var child = new FixedBox(100, 100);
        var view  = new Viewport(child);
        var (_, platform) = TestPlatform.Show(view, 400, 300);
        platform.Render().Dispose();

        platform.Move(50, 50);
        platform.Listener.PointerDown(50, 50, PointerButton.Right, KeyModifiers.None);
        platform.Move(500, 50);

        Assert.True(view.HasPointerCapture);
        Assert.Equal(450f, view.PanX, Tolerance);
    }

    [Fact]
    public void Children_are_hit_through_the_view_transform()
    {
        var child = new FixedBox(50, 50);
        var view  = new Viewport(child);
        var (_, platform) = TestPlatform.Show(view, 400, 300);
        platform.Render().Dispose();
        int clicks = 0;
        child.Click += _ => clicks++;

        view.SetView(100, 100, 2f);
        platform.Render().Dispose();
        platform.Click(40, 40);
        platform.Click(150, 150);

        Assert.Equal(1, clicks);
    }

    [Fact]
    public void DrawContent_runs_in_content_space()
    {
        var view = new PointView();
        view.Measure(new Size(100, 100));
        view.Arrange(new Rect(0, 0, 100, 100));
        view.SetView(10, 20, 4f);

        using var surface = SKSurface.Create(new SKImageInfo(100, 100));
        using var ctx     = new DrawContext(surface.Canvas, Theme.Light, 1f);
        surface.Canvas.Clear(SKColors.White);
        view.Draw(ctx);
        using var image  = surface.Snapshot();
        using var bitmap = SKBitmap.FromImage(image);

        // Content rect (5, 5)–(6, 6) maps to view (30, 40)–(34, 44).
        Assert.Equal(SKColors.Red, bitmap.GetPixel(32, 42));
        Assert.Equal(SKColors.White, bitmap.GetPixel(28, 42));
    }

    [Fact]
    public void Drawing_allocates_nothing()
    {
        var view = new PointView();
        view.Measure(new Size(100, 100));
        view.Arrange(new Rect(0, 0, 100, 100));
        view.SetView(10, 20, 4f);
        using var surface = SKSurface.Create(new SKImageInfo(100, 100));
        using var ctx     = new DrawContext(surface.Canvas, Theme.Light, 1f);

        Assert.Equal(0, TestCanvas.MinAllocatedBytes(() => view.Draw(ctx)));
    }

    private sealed class PointView : Viewport
    {
        protected override void DrawContent(DrawContext ctx) => ctx.FillRect(new Rect(5, 5, 1, 1), SKColors.Red);
    }
}
