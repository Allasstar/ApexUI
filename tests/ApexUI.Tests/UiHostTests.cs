namespace ApexUI.Tests;

public class UiHostTests
{
    [Fact]
    public void Widgets_in_the_root_tree_report_the_host_until_removed()
    {
        var label = new Label { Text = "x" };
        var panel = new Column(label);

        Assert.Null(label.Host);
        var (host, _) = TestPlatform.Show(panel);
        Assert.Same(host, label.Host);

        panel.RemoveAllChildren();
        Assert.Null(label.Host);
    }

    [Fact]
    public void Click_is_routed_in_logical_units()
    {
        var box = new FixedBox(100, 100);
        (float X, float Y)? clicked = null;
        box.Click += e => clicked = (e.X, e.Y);
        var (host, platform) = TestPlatform.Show(box);
        host.UiScale = 2f;

        platform.Click(150, 50);

        Assert.Equal((75f, 25f), clicked);
    }

    [Fact]
    public void Typed_text_and_keys_reach_the_clicked_text_input()
    {
        var input = new TextInput { HAlign = HAlign.Left, VAlign = VAlign.Top, Width = 200 };
        var (_, platform) = TestPlatform.Show(input);

        platform.Click(10, 10);
        platform.Listener.TextEntered("ab");
        platform.Listener.KeyDown(Key.Backspace, KeyModifiers.None);
        platform.Listener.TextEntered("c");

        Assert.Equal("ac", input.Value);
    }

    [Fact]
    public void Key_up_keeps_its_modifiers()
    {
        var box = new FixedBox(100, 100) { Focusable = true };
        (Key, KeyModifiers, bool Ctrl, bool Shift, bool Alt)? released = null;
        box.KeyUp += e => released = (e.Key, e.Modifiers, e.Ctrl, e.Shift, e.Alt);
        var (_, platform) = TestPlatform.Show(box);

        platform.Click(10, 10);
        platform.Listener.KeyUp(Key.S, KeyModifiers.Control | KeyModifiers.Shift);

        Assert.Equal((Key.S, KeyModifiers.Control | KeyModifiers.Shift, true, true, false), released);
    }

    [Fact]
    public void Open_overlay_belongs_to_the_host_and_receives_clicks_first()
    {
        var root    = new FixedBox(800, 600);
        var content = new FixedBox(100, 100);
        var overlay = new Overlay { Content = content, ContentHAlign = HAlign.Center, ContentVAlign = VAlign.Center };
        int rootClicks = 0, contentClicks = 0;
        root.Click    += _ => rootClicks++;
        content.Click += _ => contentClicks++;
        var (host, platform) = TestPlatform.Show(root);

        overlay.Open(host);
        platform.Render().Dispose();
        platform.Click(400, 300);

        Assert.Same(host, content.Host);
        Assert.Equal((0, 1), (rootClicks, contentClicks));

        overlay.Close();
        platform.Click(400, 300);

        Assert.Same(host, content.Host);
        Assert.Equal((1, 1), (rootClicks, contentClicks));

        overlay.Detach();

        Assert.Null(content.Host);
    }

    [Fact]
    public void Anchoring_an_overlay_to_a_widget_that_is_not_shown_throws()
    {
        var overlay = new Overlay { Content = new FixedBox(10, 10) };

        Assert.Throws<InvalidOperationException>(() => overlay.Open(new FixedBox(10, 10)));
    }

    [Fact]
    public void Gallery_renders_through_the_host()
    {
        var gallery = new Gallery.Examples.TabsExample(new ThemeRegistry());
        var tabs    = Assert.IsType<Tabs>(Assert.Single(gallery.Children));
        var (_, platform) = TestPlatform.Show(gallery, 900, 700);

        for (int i = 0; i < tabs.TabCount; i++)
        {
            tabs.SelectedIndex = i;
            platform.Render().Dispose();
        }
        platform.Listener.Close();
    }
}

public class ResizeFrameRateTests
{
    [Fact]
    public void Host_reads_and_writes_the_platform_rate_and_never_goes_negative()
    {
        var (host, platform) = TestPlatform.Show(new Column());
        Assert.Equal(60f, host.ResizeFrameRate);

        host.ResizeFrameRate = 24f;
        Assert.Equal(24f, platform.ResizeFrameRate);

        host.ResizeFrameRate = -5f;
        Assert.Equal(0f, platform.ResizeFrameRate);
    }

    [Fact]
    public void Binding_follows_the_source()
    {
        var (host, platform) = TestPlatform.Show(new Column());
        var rate = new Bindable<float>(30f);

        host.BindResizeFrameRate(rate);
        Assert.Equal(30f, platform.ResizeFrameRate);

        rate.Value = 0f;
        Assert.Equal(0f, platform.ResizeFrameRate);
    }
}
