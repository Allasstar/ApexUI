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
        PointerEvent? clicked = null;
        box.OnClick = e => clicked = e;
        var (host, platform) = TestPlatform.Show(box);
        host.UiScale = 2f;

        platform.Click(150, 50);

        Assert.Equal(new PointerEvent(75, 25, PointerButton.Left, false), clicked);
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
        var box = new FixedBox(100, 100);
        KeyEvent? released = null;
        box.OnKeyUp = e => released = e;
        var (_, platform) = TestPlatform.Show(box);

        platform.Click(10, 10);
        platform.Listener.KeyUp(Key.S, KeyModifiers.Control | KeyModifiers.Shift);

        Assert.Equal(new KeyEvent(Key.S, KeyModifiers.Control | KeyModifiers.Shift, IsDown: false), released);
        Assert.True(released!.Value.Ctrl && released.Value.Shift && !released.Value.Alt);
    }

    [Fact]
    public void Open_overlay_belongs_to_the_host_and_receives_clicks_first()
    {
        var root    = new FixedBox(800, 600);
        var content = new FixedBox(100, 100);
        var overlay = new Overlay { Content = content, ContentHAlign = HAlign.Center, ContentVAlign = VAlign.Center };
        int rootClicks = 0, contentClicks = 0;
        root.OnClick    = _ => rootClicks++;
        content.OnClick = _ => contentClicks++;
        var (host, platform) = TestPlatform.Show(root);

        overlay.Open(host);
        platform.Render().Dispose();
        platform.Click(400, 300);

        Assert.Same(host, content.Host);
        Assert.Equal((0, 1), (rootClicks, contentClicks));

        overlay.Close();
        platform.Click(400, 300);

        Assert.Null(content.Host);
        Assert.Equal((1, 1), (rootClicks, contentClicks));
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
        var (_, platform) = TestPlatform.Show(new Gallery.Examples.TabsExample(), 900, 700);

        platform.Render().Dispose();
        platform.Listener.Close();
    }
}
