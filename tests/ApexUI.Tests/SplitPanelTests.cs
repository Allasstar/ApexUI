namespace ApexUI.Tests;

public class SplitPanelTests
{
    private static readonly float Divider = Theme.Light.SplitterSize;

    private static SplitPanel Arranged(SplitPanel split, float width, float height = 100)
    {
        split.Measure(new Size(width, height));
        split.Arrange(new Rect(0, 0, width, height));
        return split;
    }

    [Fact]
    public void Fixed_first_pane_keeps_its_size_and_the_second_takes_the_rest()
    {
        var first  = new FixedBox(10, 10);
        var second = new FixedBox(10, 10);
        Arranged(new SplitPanel(first, second).WithSplitSize(200), 800);

        Assert.Equal(new Rect(0, 0, 200, 100), first.LayoutBounds);
        Assert.Equal(new Rect(200 + Divider, 0, 600 - Divider, 100), second.LayoutBounds);
    }

    [Fact]
    public void Fixed_second_pane_stays_put_when_the_panel_widens()
    {
        var first  = new FixedBox(10, 10);
        var second = new FixedBox(10, 10);
        var split  = Arranged(new SplitPanel(first, second).WithFixedSide(SplitSide.Second).WithSplitSize(300), 800);
        Arranged(split, 1000);

        Assert.Equal(300, second.LayoutBounds.Width);
        Assert.Equal(1000 - 300, second.LayoutBounds.X);
        Assert.Equal(1000 - 300 - Divider, first.LayoutBounds.Width);
    }

    [Fact]
    public void Vertical_split_divides_the_height()
    {
        var first  = new FixedBox(10, 10);
        var second = new FixedBox(10, 10);
        Arranged(new SplitPanel(first, second, Orientation.Vertical).WithSplitSize(40), 100, 200);

        Assert.Equal(new Rect(0, 0, 100, 40), first.LayoutBounds);
        Assert.Equal(new Rect(0, 40 + Divider, 100, 160 - Divider), second.LayoutBounds);
    }

    [Fact]
    public void The_other_pane_keeps_its_minimum_when_the_panel_is_small()
    {
        var first = new FixedBox(10, 10);
        var split = Arranged(new SplitPanel(first, new FixedBox(10, 10)).WithSplitSize(300).WithMinOtherSize(100), 250);

        Assert.Equal(250 - 100 - Divider, first.LayoutBounds.Width);
        Assert.Equal(300, split.SplitSize);
    }

    [Fact]
    public void SplitSize_is_clamped_to_its_range()
    {
        var split = new SplitPanel(new FixedBox(1, 1), new FixedBox(1, 1)).WithSplitRange(100, 400);

        split.SplitSize = 50;
        Assert.Equal(100, split.SplitSize);
        split.SplitSize = 900;
        Assert.Equal(400, split.SplitSize);
    }

    [Fact]
    public void Collapsing_hides_the_fixed_pane_and_gives_its_space_away()
    {
        var first  = new FixedBox(10, 10);
        var second = new FixedBox(10, 10);
        var split  = new SplitPanel(first, second).WithSplitSize(200);
        bool? reported = null;
        split.CollapsedChanged += c => reported = c;

        split.IsCollapsed = true;
        Arranged(split, 800);

        Assert.False(first.IsVisible);
        Assert.True(reported);
        Assert.Equal(new Rect(Divider, 0, 800 - Divider, 100), second.LayoutBounds);
        Assert.Equal(200, split.SplitSize);
    }

    [Fact]
    public void Dragging_the_divider_resizes_the_fixed_pane()
    {
        var first = new FixedBox(10, 10);
        var split = new SplitPanel(first, new FixedBox(10, 10)).WithSplitSize(200);
        var (_, platform) = TestPlatform.Show(split);
        platform.Render().Dispose();

        float x = 200 + Divider / 2;
        platform.Move(x, 50);
        platform.Press(x, 50);
        platform.Move(x + 60, 50);
        platform.Release(x + 60, 50);
        platform.Render().Dispose();

        Assert.Equal(260, split.SplitSize);
        Assert.Equal(260, first.LayoutBounds.Width);
    }

    [Fact]
    public void Dragging_a_fixed_second_pane_divider_left_widens_it()
    {
        var split = new SplitPanel(new FixedBox(10, 10), new FixedBox(10, 10))
            .WithFixedSide(SplitSide.Second).WithSplitSize(200);
        var (_, platform) = TestPlatform.Show(split);
        platform.Render().Dispose();

        float x = 800 - 200 - Divider / 2;
        platform.Move(x, 50);
        platform.Press(x, 50);
        platform.Move(x - 50, 50);
        platform.Release(x - 50, 50);

        Assert.Equal(250, split.SplitSize);
    }

    [Fact]
    public void Dragging_below_half_the_minimum_collapses()
    {
        var split = new SplitPanel(new FixedBox(10, 10), new FixedBox(10, 10)).WithSplitRange(120, 400).WithSplitSize(200);
        var (_, platform) = TestPlatform.Show(split);
        platform.Render().Dispose();

        float x = 200 + Divider / 2;
        platform.Move(x, 50);
        platform.Press(x, 50);
        platform.Move(40, 50);
        Assert.True(split.IsCollapsed);

        platform.Move(150, 50);
        platform.Release(150, 50);
        Assert.False(split.IsCollapsed);
    }

    [Fact]
    public void Double_click_on_the_divider_toggles_collapse()
    {
        var split = new SplitPanel(new FixedBox(10, 10), new FixedBox(10, 10)).WithSplitSize(200);
        var (_, platform) = TestPlatform.Show(split);
        platform.Render().Dispose();

        float x = 200 + Divider / 2;
        platform.Click(x, 50);
        platform.Click(x, 50);

        Assert.True(split.IsCollapsed);
    }

    [Fact]
    public void Divider_shows_a_resize_cursor()
    {
        var split = new SplitPanel(new FixedBox(10, 10), new FixedBox(10, 10)).WithSplitSize(200);
        var (_, platform) = TestPlatform.Show(split);
        platform.Render().Dispose();

        platform.Move(200 + Divider / 2, 50);

        Assert.Equal(CursorShape.ResizeHorizontal, platform.Cursor);
    }
}
