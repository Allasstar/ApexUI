namespace ApexUI.Tests;

public class BubblingAndCaptureTests
{
    [Fact]
    public void Handled_press_stops_at_the_widget_that_handled_it()
    {
        var child = new FixedBox(50, 50) { HAlign = HAlign.Left, VAlign = VAlign.Top };
        var panel = new TestPanel(child);
        int panelPresses = 0;
        bool handle = true;
        child.PointerPressed += e => e.Handled = handle;
        panel.PointerPressed += _ => panelPresses++;
        var (_, platform) = TestPlatform.Show(panel);

        platform.Click(10, 10);
        handle = false;
        platform.Click(10, 10);

        Assert.Equal(1, panelPresses);
    }

    [Fact]
    public void Every_subscriber_on_a_widget_runs_even_after_one_handles_the_event()
    {
        var box = new FixedBox(50, 50);
        int second = 0;
        box.PointerPressed += e => e.Handled = true;
        box.PointerPressed += _ => second++;
        var (_, platform) = TestPlatform.Show(box);

        platform.Click(10, 10);

        Assert.Equal(1, second);
    }

    [Fact]
    public void Bubbling_events_keep_the_original_source()
    {
        var child = new FixedBox(50, 50) { HAlign = HAlign.Left, VAlign = VAlign.Top };
        var panel = new TestPanel(child);
        Widget? source = null;
        panel.PointerReleased += e => source = e.Source;
        var (_, platform) = TestPlatform.Show(panel);

        platform.Click(10, 10);

        Assert.Same(child, source);
    }

    [Fact]
    public void Releasing_capture_mid_drag_sends_moves_back_to_the_widget_under_the_pointer()
    {
        var box   = new FixedBox(50, 50) { HAlign = HAlign.Left, VAlign = VAlign.Top };
        var other = new FixedBox(50, 50) { HAlign = HAlign.Left, VAlign = VAlign.Top, Margin = new Thickness(100, 0, 0, 0) };
        int boxMoves = 0, otherMoves = 0, lost = 0;
        box.PointerMoved       += _ => boxMoves++;
        other.PointerMoved     += _ => otherMoves++;
        box.PointerCaptureLost += () => lost++;
        var (host, platform) = TestPlatform.Show(new TestPanel(box, other));

        platform.Press(10, 10);
        platform.Move(120, 10);
        box.ReleasePointerCapture();
        platform.Move(125, 10);

        Assert.Null(host.PointerCapture);
        Assert.Equal(1, lost);
        Assert.Equal(1, boxMoves);
        Assert.Equal(1, otherMoves);
    }

    [Fact]
    public void A_widget_can_capture_without_a_press()
    {
        var box = new FixedBox(50, 50) { HAlign = HAlign.Left, VAlign = VAlign.Top };
        var moves = new List<float>();
        box.PointerMoved += e => moves.Add(e.X);
        var (host, platform) = TestPlatform.Show(new TestPanel(box));

        Assert.True(box.CapturePointer());
        platform.Move(400, 400);

        Assert.Same(box, host.PointerCapture);
        Assert.Equal([400f], moves);
    }

    [Fact]
    public void Detached_widgets_cannot_take_capture_or_focus()
    {
        var box = new FixedBox(50, 50) { Focusable = true };

        Assert.False(box.CapturePointer());
        Assert.False(box.Focus());
    }

    [Fact]
    public void Disabled_widgets_pass_clicks_to_their_parent()
    {
        var button = new Button("Off") { IsEnabled = false, HAlign = HAlign.Left, VAlign = VAlign.Top };
        var panel  = new TestPanel(button);
        int buttonClicks = 0;
        Widget? source = null;
        button.Click += _ => buttonClicks++;
        panel.Click  += e => source = e.Source;
        var (_, platform) = TestPlatform.Show(panel);

        platform.Click(5, 5);

        Assert.Equal(0, buttonClicks);
        Assert.Same(panel, source);
    }
}

public class FocusTraversalTests
{
    [Fact]
    public void Tab_skips_hidden_and_disabled_widgets_and_their_descendants()
    {
        var first    = new Button("First");
        var hidden   = new Button("Hidden") { IsVisible = false };
        var disabled = new Button("Disabled") { IsEnabled = false };
        var inner    = new Button("Inner");
        var last     = new Button("Last");
        var (host, platform) = TestPlatform.Show(
            new Column(first, hidden, disabled, new Column(inner) { IsEnabled = false }, last));

        platform.Key(Key.Tab);
        Assert.Same(first, host.FocusedWidget);
        platform.Key(Key.Tab);
        Assert.Same(last, host.FocusedWidget);
    }

    [Fact]
    public void IsEnabledInTree_follows_the_nearest_disabled_ancestor()
    {
        var input = new TextInput();
        var inner = new Column(input);
        var outer = new Column(inner);

        Assert.True(input.IsEnabledInTree);
        outer.IsEnabled = false;
        Assert.False(input.IsEnabledInTree);
        Assert.True(input.IsEnabled);
        outer.IsEnabled = true;
        inner.IsEnabled = false;
        Assert.False(input.IsEnabledInTree);
        Assert.True(outer.IsEnabledInTree);
    }

    [Fact]
    public void Focus_moves_raise_lost_then_got_focus()
    {
        var a = new Button("A");
        var b = new Button("B");
        var log = new List<string>();
        a.GotFocus  += () => log.Add("a+");
        a.LostFocus += () => log.Add("a-");
        b.GotFocus  += () => log.Add("b+");
        b.LostFocus += () => log.Add("b-");
        var (host, _) = TestPlatform.Show(new Column(a, b));

        Assert.True(host.MoveFocus(forward: true));
        Assert.True(host.MoveFocus(forward: true));
        host.ClearFocus();

        Assert.Equal(["a+", "a-", "b+", "b-"], log);
        Assert.Null(host.FocusedWidget);
    }

    [Fact]
    public void Shift_tab_from_nothing_focuses_the_last_widget()
    {
        var a = new Button("A");
        var b = new Button("B");
        var (host, platform) = TestPlatform.Show(new Column(a, b));

        platform.Key(Key.Tab, KeyModifiers.Shift);

        Assert.Same(b, host.FocusedWidget);
    }

    [Fact]
    public void Clicking_inside_a_focusable_panel_focuses_the_panel()
    {
        var panel = new TestPanel(new FixedBox(200, 50)) { Focusable = true, HAlign = HAlign.Left, VAlign = VAlign.Top };
        var (host, platform) = TestPlatform.Show(new TestPanel(panel));

        platform.Click(10, 10);

        Assert.Same(panel, host.FocusedWidget);
    }
}
