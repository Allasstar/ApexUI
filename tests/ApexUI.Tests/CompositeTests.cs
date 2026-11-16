namespace ApexUI.Tests;

public class ScrollbarTests
{
    // 200×200 viewport over 1000 units of content: the thumb is 40 long, travels 160, and scrolls up to 800.
    private static (Widget Content, TestPlatform Platform) TallScroll()
    {
        var scroll = new Scroll(new FixedBox(100, 1000));
        var (_, platform) = TestPlatform.Show(scroll, 200, 200);
        platform.Render().Dispose();
        return (scroll.Children[0], platform);
    }

    [Fact]
    public void Dragging_the_thumb_scrolls_in_proportion()
    {
        var (content, platform) = TallScroll();

        platform.Move(196, 10);
        platform.Press(196, 10);
        platform.Move(196, 90);
        platform.Release(196, 90);

        Assert.Equal(-400f, content.WindowBounds.Y, 3);
    }

    [Fact]
    public void Pressing_the_track_jumps_there()
    {
        var (content, platform) = TallScroll();

        platform.Click(196, 180);

        Assert.Equal(-800f, content.WindowBounds.Y, 3);
    }

    [Fact]
    public void Hidden_scrollbars_leave_the_whole_viewport_to_the_content()
    {
        var scroll = new Scroll(new FixedBox(1000, 1000)) { Direction = ScrollDirection.Both, ShowScrollbar = false };
        var (_, platform) = TestPlatform.Show(scroll, 200, 200);
        platform.Render().Dispose();

        platform.Move(50, 50);
        platform.Listener.Wheel(-100, -100, KeyModifiers.None);

        Assert.Equal(-800f, scroll.Children[0].WindowBounds.X, 3);
        Assert.Equal(-800f, scroll.Children[0].WindowBounds.Y, 3);
    }
}

public class MenuTests
{
    [Fact]
    public void Separator_takes_no_length_when_unconstrained()
    {
        var separator = new Separator();

        separator.Measure(Size.Infinite);

        Assert.Equal(new Size(0f, 1f), separator.DesiredSize);
    }

    [Fact]
    public void Context_menu_items_after_a_separator_are_clickable()
    {
        bool pasted = false;
        var target = new FixedBox(400, 400) { HAlign = HAlign.Left, VAlign = VAlign.Top };
        ContextMenu.Attach(target, new ContextMenu()
            .AddItem("Copy", () => { })
            .AddSeparator()
            .AddItem("Paste", () => pasted = true));
        var (host, platform) = TestPlatform.Show(target);
        platform.Render().Dispose();

        platform.Click(10, 10, PointerButton.Right);
        platform.Render().Dispose();
        // Popup padding 4 + item 31.6 + separator 5 + spacing 2×2: Paste spans y 54.6–86.2 below the press.
        platform.Click(60, 10 + 70);

        Assert.True(pasted);
        Assert.Equal(0, host.OpenOverlayCount);
    }
}
