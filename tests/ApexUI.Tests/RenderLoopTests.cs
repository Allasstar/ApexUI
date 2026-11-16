namespace ApexUI.Tests;

public class RenderLoopTests
{
    [Fact]
    public void An_untouched_gallery_stops_asking_for_frames_on_every_tab()
    {
        var gallery = new Gallery.Examples.TabsExample();
        var tabs    = Assert.IsType<Tabs>(Assert.Single(gallery.Children));
        var (_, platform) = TestPlatform.Show(gallery, 900, 700);

        for (int i = 0; i < tabs.TabCount; i++)
        {
            tabs.SelectedIndex = i;
            platform.Render().Dispose();

            Assert.Equal(0, platform.RenderAndCountRequests());
        }
    }

    [Fact]
    public void Hovering_over_the_gallery_draws_once_per_change()
    {
        var gallery = new Gallery.Examples.TabsExample();
        var (_, platform) = TestPlatform.Show(gallery, 900, 700);
        platform.Render().Dispose();

        for (int y = 5; y < 700; y += 37)
        {
            platform.Listener.PointerMove(y, y);
            platform.Render().Dispose();

            Assert.Equal(0, platform.RenderAndCountRequests());
        }
    }

    [Fact]
    public void Changes_before_a_frame_make_one_request()
    {
        var label = new Label { Text = "a" };
        var (_, platform) = TestPlatform.Show(new Column(label));
        platform.Render().Dispose();
        int before = platform.FramesRequested;

        label.Text  = "b";
        label.Color = SKColors.Red;
        label.Bold  = true;

        Assert.Equal(before + 1, platform.FramesRequested);
    }

    [Fact]
    public void Setting_a_property_to_its_current_value_requests_nothing()
    {
        var label  = new Label { Text = "a", Color = SKColors.Red };
        var button = new Button("Go");
        var (_, platform) = TestPlatform.Show(new Column(label, button));
        platform.Render().Dispose();
        int before = platform.FramesRequested;

        label.Text      = "a";
        label.Color     = SKColors.Red;
        label.Width     = float.NaN;
        button.Text     = "Go";
        button.IsVisible = true;

        Assert.Equal(before, platform.FramesRequested);
        Assert.False(label.IsLayoutDirty);
    }

    [Fact]
    public void Animation_frames_continue_only_while_requested()
    {
        var (host, platform) = TestPlatform.Show(new FixedBox(10, 10));
        platform.Render().Dispose();
        var times = new List<TimeSpan>();
        Action<TimeSpan>? step = null;
        step = t =>
        {
            times.Add(t);
            if (times.Count < 3) host.RequestAnimationFrame(step!);
        };

        host.RequestAnimationFrame(step);
        for (int i = 0; i < 5; i++)
        {
            platform.Advance(TimeSpan.FromMilliseconds(16));
            platform.Render().Dispose();
        }

        Assert.Equal([16, 32, 48], times.Select(t => (int)t.TotalMilliseconds));
        Assert.Equal(0, platform.RenderAndCountRequests());
    }

    [Fact]
    public void A_focused_text_input_blinks_on_a_timer_and_stops_when_unfocused()
    {
        var input = new TextInput { HAlign = HAlign.Left, VAlign = VAlign.Top, Width = 200 };
        var (_, platform) = TestPlatform.Show(new Column(input, new FixedBox(100, 100)));
        platform.Render().Dispose();

        platform.Click(10, 10);
        platform.Render().Dispose();
        Assert.Equal(1, platform.ActiveTimers);
        Assert.Equal(0, platform.RenderAndCountRequests());

        int before = platform.FramesRequested;
        platform.Advance(TimeSpan.FromSeconds(0.6));
        Assert.Equal(before + 1, platform.FramesRequested);

        platform.Click(10, 80);
        platform.Advance(TimeSpan.FromSeconds(0.1));
        Assert.Equal(0, platform.ActiveTimers);
    }

    [Fact]
    public void Tooltip_opens_after_its_delay_without_frames_in_between_and_closes_on_leave()
    {
        var target  = new FixedBox(100, 40) { HAlign = HAlign.Left, VAlign = VAlign.Top };
        var tooltip = new Tooltip(target, "Tip") { HAlign = HAlign.Left, VAlign = VAlign.Top, ShowDelay = 0.5f };
        var root    = new Column(tooltip, new FixedBox(100, 100));
        var (host, platform) = TestPlatform.Show(root);
        platform.Render().Dispose();

        platform.Listener.PointerMove(10, 10);
        platform.Render().Dispose();
        int before = platform.FramesRequested;
        platform.Advance(TimeSpan.FromSeconds(0.4));
        Assert.Equal(before, platform.FramesRequested);

        platform.Advance(TimeSpan.FromSeconds(0.2));
        Assert.True(platform.FramesRequested > before);
        platform.Render().Dispose();
        Assert.Equal(1, host.OpenOverlayCount);

        platform.Listener.PointerMove(10, 80);
        Assert.Equal(0, host.OpenOverlayCount);
        Assert.Equal(0, platform.ActiveTimers);
    }

    [Fact]
    public void Overlays_are_laid_out_only_when_something_changed()
    {
        var content = new CountingBox();
        var overlay = new Overlay { Content = content, ContentHAlign = HAlign.Center, ContentVAlign = VAlign.Center };
        var (host, platform) = TestPlatform.Show(new FixedBox(10, 10));
        overlay.Open(host);
        platform.Render().Dispose();
        int arranged = content.ArrangeCount;

        platform.Render().Dispose();
        platform.Render().Dispose();
        Assert.Equal(arranged, content.ArrangeCount);

        host.UiScale = 2f;
        platform.Render().Dispose();
        Assert.Equal(arranged + 1, content.ArrangeCount);
    }

    private sealed class CountingBox : Widget
    {
        public int ArrangeCount { get; private set; }

        protected override Size MeasureCore(Size available) => new(50, 50);

        protected override void ArrangeCore(Rect finalRect) => ArrangeCount++;
    }
}
