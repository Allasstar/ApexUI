namespace ApexUI.Gallery.Examples;

/// Demonstrates Viewport: a pan and zoom camera that draws its own content (the grid) and hosts
/// widgets that are laid out once and then only redrawn, hit-tested and sent pointer positions
/// through the view transform.
public class ViewportExample : Widget
{
    private static readonly Rect ContentArea = new(0f, 0f, 260f, 300f);

    public ViewportExample()
    {
        var zoomLabel = new Label { Text = "Zoom 100%" };
        var clicks    = new Label { Text = "Button clicks: 0" };
        int count     = 0;

        var button = new Button("Click me, zoomed");
        button.Pressed += () => clicks.Text = $"Button clicks: {++count}";

        var content = new Canvas()
            .Add(MakeTile("A", new SKColor(0x33, 0x6B, 0xE6)), 0, 0, 120, 120)
            .Add(MakeTile("B", new SKColor(0x4C, 0xAF, 0x50)), 140, 0, 120, 120)
            .Add(MakeTile("C", new SKColor(0xF4, 0x43, 0x36)), 0, 140, 120, 120)
            .Add(button, 140, 180)
            .Add(new Label { Text = "Wheel zooms around the pointer, drag pans.", Role = TextRole.Small }, 0, 280);

        var view = new GridView(content)
        {
            Height           = 400f,
            BackgroundSource = t => t.Surface,
            CornerRadius     = 8f,
            PanButtons       = PointerButtons.Left | PointerButtons.Middle | PointerButtons.Right,
        }.WithZoomRange(0.25f, 8f);
        view.ViewChanged += () => zoomLabel.Text = $"Zoom {view.Zoom * 100f:0}%";
        view.Fit(ContentArea, 16f);

        AddChild(new Column(
            new Label { Text = "Viewport", Bold = true, Role = TextRole.Heading },
            new Label
            {
                Text     = "The grid is drawn by the viewport in content space with hairlines, so it stays one pixel wide at any zoom. The tiles are widgets: zooming and panning change only the view transform, so they redraw without a layout pass, and clicks still reach the right widget. Fit keeps refitting on resize until you pan or zoom.",
                WordWrap = true,
            },
            new Row(
                new Button("Zoom out", () => view.ZoomAtCenter(1f / 1.25f)),
                new Button("Zoom in",  () => view.ZoomAtCenter(1.25f)),
                new Button("Fit",      () => view.Fit(ContentArea, 16f)),
                zoomLabel,
                clicks
            ).WithSpacing(12f),
            view
        ) { Spacing = 12f, Padding = new Thickness(24f) });
    }

    private static Widget MakeTile(string text, SKColor color)
        => new Label
        {
            Text       = text,
            FontSize   = 40f,
            Bold       = true,
            Color      = SKColors.White,
            Background = color,
            CornerRadius = 8f,
            Padding    = new Thickness(44f, 32f),
        };

    private sealed class GridView(Widget content) : Viewport(content)
    {
        private const float Step   = 20f;
        private const float Extent = 1000f;

        protected override void DrawContent(DrawContext ctx)
        {
            var color = ctx.Theme.BorderSubtle;
            for (float v = -Extent; v <= Extent; v += Step)
            {
                ctx.DrawLine(v, -Extent, v, Extent, color, 0f, SKStrokeCap.Butt);
                ctx.DrawLine(-Extent, v, Extent, v, color, 0f, SKStrokeCap.Butt);
            }
        }
    }
}
