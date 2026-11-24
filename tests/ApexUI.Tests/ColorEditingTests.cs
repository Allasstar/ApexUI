namespace ApexUI.Tests;

public class PixelPickTests
{
    private sealed class Paint(SKColor color) : Widget
    {
        public int Presses { get; private set; }

        protected override Size MeasureCore(Size available) => new(Math.Min(available.Width, 300f), Math.Min(available.Height, 300f));

        protected override void DrawCore(DrawContext ctx) => ctx.FillRect(LayoutBounds, color);

        protected override void OnPointerPressed(PointerEventArgs e) => Presses++;
    }

    [Fact]
    public void A_press_picks_the_color_drawn_under_it_and_never_reaches_the_widgets()
    {
        var target = new Paint(new SKColor(10, 200, 30));
        var (host, platform) = TestPlatform.Show(target, 200, 100);
        platform.Render().Dispose();
        SKColor? picked = null;

        host.PickPixel(c => picked = c);
        Assert.True(host.IsPickingPixel);
        Assert.Equal(CursorShape.Crosshair, platform.Cursor);
        platform.Move(50, 50);
        platform.Press(50, 50);
        Assert.Null(picked);
        platform.Render().Dispose();
        platform.Release(50, 50);

        Assert.Equal(new SKColor(10, 200, 30), picked);
        Assert.False(host.IsPickingPixel);
        Assert.Equal(0, target.Presses);
    }

    [Fact]
    public void Escape_cancels_a_pick()
    {
        var (host, platform) = TestPlatform.Show(new Paint(SKColors.Red), 200, 100);
        platform.Render().Dispose();
        bool cancelled = false;

        host.PickPixel(_ => Assert.Fail("nothing should be picked"), () => cancelled = true);
        platform.Key(Key.Escape);

        Assert.True(cancelled);
        Assert.False(host.IsPickingPixel);
    }

    [Fact]
    public void A_platform_that_cannot_read_pixels_never_starts_a_pick()
    {
        var picker = new ColorPicker();
        var platform = new TestPlatform { CanReadPixels = false };
        var host = new UiHost(platform);
        host.Run(new Column(picker));
        platform.Render().Dispose();

        picker.StartEyedropper();

        Assert.False(host.IsPickingPixel);
        Assert.False(Find<IconButton>(picker)!.IsVisible);
    }

    [Fact]
    public void The_picker_eyedropper_sets_its_color_and_completes_the_edit()
    {
        var picker = new ColorPicker(SKColors.White);
        var (host, platform) = TestPlatform.Show(new Row(picker, new Paint(new SKColor(0, 0, 255))), 600, 400);
        platform.Render().Dispose();
        var completed = new List<SKColor>();
        picker.EditCompleted += completed.Add;

        picker.StartEyedropper();
        Assert.True(picker.IsPickingPixel);
        platform.Press(300, 100);
        platform.Render().Dispose();

        Assert.Equal(new SKColor(0, 0, 255), picker.Color);
        Assert.Equal([new SKColor(0, 0, 255)], completed);
        Assert.False(picker.IsPickingPixel);
    }

    internal static T? Find<T>(Widget root) where T : Widget
    {
        if (root is T match) return match;
        foreach (var child in root.Children)
        {
            if (Find<T>(child) is { } found) return found;
        }

        return null;
    }

    internal static void FindAll<T>(Widget root, List<T> into) where T : Widget
    {
        if (root is T match) into.Add(match);
        foreach (var child in root.Children) FindAll(child, into);
    }
}

public class ColorPickerFieldsTests
{
    [Fact]
    public void Channel_fields_follow_the_color_and_set_it()
    {
        var picker = new ColorPicker(new SKColor(1, 2, 3, 4));
        var (_, platform) = TestPlatform.Show(new Column(picker));
        platform.Render().Dispose();
        var fields = new List<NumberField>();
        PixelPickTests.FindAll(picker, fields);
        var completed = new List<SKColor>();
        picker.EditCompleted += completed.Add;

        Assert.Equal([1f, 2f, 3f, 4f], fields.ConvertAll(f => f.Value));

        fields[1].Value = 200f;

        Assert.Equal(new SKColor(1, 200, 3, 4), picker.Color);
    }

    [Fact]
    public void A_swatch_row_picks_its_color_once_and_hides_while_empty()
    {
        var colors = new ObservableList<SKColor>();
        var picker = new ColorPicker(SKColors.White).AddSwatches("Recent", colors);
        var (_, platform) = TestPlatform.Show(new Column(picker), 400, 600);
        platform.Render().Dispose();
        var completed = new List<SKColor>();
        picker.EditCompleted += completed.Add;
        var labels = new List<Label>();
        PixelPickTests.FindAll(picker, labels);
        var row = labels.Find(l => l.Text == "Recent")!.Parent!.Parent!;
        Assert.False(row.IsVisible);

        colors.Add(new SKColor(9, 8, 7));
        platform.Render().Dispose();
        Assert.True(row.IsVisible);
        var chip = row.Children[0].Children[1].Children[0].WindowBounds;
        platform.Click(chip.CenterX, chip.CenterY);
        platform.Click(chip.CenterX, chip.CenterY);

        Assert.Equal(new SKColor(9, 8, 7), picker.Color);
        Assert.Equal([new SKColor(9, 8, 7)], completed);
    }

    [Fact]
    public void A_swatch_row_filled_before_it_is_shown_is_visible()
    {
        var colors = new ObservableList<SKColor>();
        var picker = new ColorPicker().AddSwatches("Document", colors);
        colors.Add(SKColors.Red);

        TestPlatform.Show(new Column(picker)).Platform.Render().Dispose();

        var labels = new List<Label>();
        PixelPickTests.FindAll(picker, labels);
        Assert.True(labels.Find(l => l.Text == "Document")!.Parent!.Parent!.IsVisible);
    }

    [Fact]
    public void A_swatch_reports_each_opening_and_closing()
    {
        var swatch = new ColorSwatch(SKColors.Red);
        var (_, platform) = TestPlatform.Show(new Column(swatch));
        platform.Render().Dispose();
        var events = new List<string>();
        swatch.Opened += () => events.Add("open");
        swatch.Closed += () => events.Add("close");

        platform.Click(5, 5);
        platform.Render().Dispose();
        platform.Click(700, 500);

        Assert.Equal(["open", "close"], events);
        Assert.NotNull(swatch.Picker);
    }
}

public class GradientBarTests
{
    private static (GradientBar Bar, TestPlatform Platform) Show(params ColorStop[] stops)
    {
        var bar = new GradientBar(stops) { Width = 210f };
        var (_, platform) = TestPlatform.Show(new Column(bar), 400, 300);
        platform.Render().Dispose();
        return (bar, platform);
    }

    // The strip runs between the handle radii: offset 0 at x = r, offset 1 at x = Width - r.
    private static float X(GradientBar bar, float offset)
    {
        float r = Theme.Light.ColorStripHeight * 0.75f;
        var b = bar.WindowBounds;
        return b.X + r + offset * (b.Width - 2f * r);
    }

    private static float Y(GradientBar bar) => bar.WindowBounds.CenterY;

    [Fact]
    public void Color_at_interpolates_between_the_nearest_stops_in_any_order()
    {
        ColorStop[] stops = [new(1f, new SKColor(200, 0, 0)), new(0f, new SKColor(0, 0, 0, 0))];

        Assert.Equal(new SKColor(100, 0, 0, 128), GradientBar.ColorAt(stops, 0.5f));
        Assert.Equal(new SKColor(200, 0, 0), GradientBar.ColorAt(stops, 1.5f));
    }

    [Fact]
    public void Clicking_the_strip_adds_a_selected_stop_in_the_gradient_color()
    {
        var (bar, platform) = Show(new ColorStop(0f, SKColors.Black), new ColorStop(1f, SKColors.White));
        int completed = 0;
        bar.EditCompleted += () => completed++;

        platform.Click(X(bar, 0.5f), Y(bar));

        Assert.Equal(3, bar.Stops.Count);
        Assert.Equal(2, bar.SelectedIndex);
        Assert.Equal(0.5f, bar.SelectedStop.Offset, 2);
        Assert.Equal(new SKColor(128, 128, 128), bar.SelectedStop.Color);
        Assert.Equal(1, completed);
    }

    [Fact]
    public void Dragging_a_handle_moves_it_past_others_without_changing_its_index()
    {
        var (bar, platform) = Show(new ColorStop(0f, SKColors.Black), new ColorStop(1f, SKColors.White));
        int changes = 0, completed = 0;
        bar.StopsChanged += () => changes++;
        bar.EditCompleted += () => completed++;

        platform.Move(X(bar, 0f), Y(bar));
        platform.Press(X(bar, 0f), Y(bar));
        platform.Move(X(bar, 1f) + 40f, Y(bar));
        platform.Release(X(bar, 1f) + 40f, Y(bar));

        Assert.Equal(0, bar.SelectedIndex);
        Assert.Equal(1f, bar.Stops[0].Offset);
        Assert.Equal(SKColors.Black, bar.Stops[0].Color);
        Assert.True(changes > 0);
        Assert.Equal(1, completed);
    }

    [Fact]
    public void Dragging_off_or_Delete_removes_a_stop_but_never_below_two()
    {
        var (bar, platform) = Show(new ColorStop(0f, SKColors.Black), new ColorStop(0.5f, SKColors.Red), new ColorStop(1f, SKColors.White));

        platform.Move(X(bar, 0.5f), Y(bar));
        platform.Press(X(bar, 0.5f), Y(bar));
        platform.Move(X(bar, 0.5f), Y(bar) + 200f);
        platform.Release(X(bar, 0.5f), Y(bar) + 200f);
        Assert.Equal(2, bar.Stops.Count);
        Assert.DoesNotContain(bar.Stops, s => s.Color == SKColors.Red);

        platform.Click(X(bar, 1f), Y(bar));
        platform.Key(Key.Delete);
        platform.Move(X(bar, 0f), Y(bar));
        platform.Press(X(bar, 0f), Y(bar));
        platform.Move(X(bar, 0f), Y(bar) + 200f);
        platform.Release(X(bar, 0f), Y(bar) + 200f);

        Assert.Equal(2, bar.Stops.Count);
    }

    [Fact]
    public void Set_stop_color_changes_only_that_stop()
    {
        var (bar, _) = Show(new ColorStop(0f, SKColors.Black), new ColorStop(1f, SKColors.White));
        int changes = 0;
        bar.StopsChanged += () => changes++;

        bar.SetStopColor(1, SKColors.Blue);
        bar.SetStops([new ColorStop(0.2f, SKColors.Red), new ColorStop(0.8f, SKColors.Green)]);

        Assert.Equal(1, changes);
        Assert.Equal(SKColors.Green, bar.Stops[1].Color);
    }
}

public class ClickThroughOverlayTests
{
    private sealed class Target : Widget
    {
        public int Presses { get; private set; }

        protected override Size MeasureCore(Size available) => available;

        protected override void OnPointerPressed(PointerEventArgs e) => Presses++;
    }

    [Fact]
    public void A_press_outside_reaches_the_widget_below_and_closes_the_overlay()
    {
        var below = new Target();
        var (host, platform) = TestPlatform.Show(below, 400, 300);
        var overlay = new Overlay { ClickThrough = true, Content = new FixedBox(50, 50) };
        overlay.Open(host);
        platform.Render().Dispose();

        platform.Click(10, 10);
        Assert.True(overlay.IsVisible);
        Assert.Equal(0, below.Presses);

        platform.Click(300, 200);

        Assert.Equal(1, below.Presses);
        Assert.False(overlay.IsVisible);
    }

    [Fact]
    public void Keep_open_on_press_lets_chosen_presses_through_without_closing()
    {
        var below = new Target();
        var (host, platform) = TestPlatform.Show(below, 400, 300);
        var overlay = new Overlay { ClickThrough = true, Content = new FixedBox(50, 50), KeepOpenOnPress = w => w == below };
        overlay.Open(host);
        platform.Render().Dispose();

        platform.Click(300, 200);

        Assert.Equal(1, below.Presses);
        Assert.True(overlay.IsVisible);
    }
}
