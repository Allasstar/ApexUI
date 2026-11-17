namespace ApexUI.Tests;

public class SectionTests
{
    [Fact]
    public void Collapsed_section_measures_only_its_header()
    {
        var content  = new FixedBox(50, 300);
        var expanded = new Section("Grid", new FixedBox(50, 300));
        var folded   = new Section("Grid", content, expanded: false);
        expanded.Measure(new Size(200, float.PositiveInfinity));
        folded.Measure(new Size(200, float.PositiveInfinity));

        Assert.False(content.IsVisible);
        Assert.Equal(300, expanded.DesiredSize.Height - folded.DesiredSize.Height, 3);
    }

    [Fact]
    public void Content_sits_below_the_header_at_full_width()
    {
        var content = new FixedBox(50, 30);
        var section = new Section("Grid", content);
        section.Measure(new Size(200, 400));
        section.Arrange(new Rect(0, 0, 200, 400));

        Assert.Equal(200, content.LayoutBounds.Width);
        Assert.True(content.LayoutBounds.Y > 0);
    }

    [Fact]
    public void Clicking_the_header_toggles_and_reports_it()
    {
        var content = new FixedBox(50, 30);
        var section = new Section("Grid", content);
        var reports = new List<bool>();
        section.ExpandedChanged += reports.Add;
        var (_, platform) = TestPlatform.Show(new Column(section));
        platform.Render().Dispose();

        platform.Click(20, 10);
        Assert.False(section.IsExpanded);
        Assert.False(content.IsVisible);

        platform.Click(20, 10);
        Assert.True(section.IsExpanded);
        Assert.Equal([false, true], reports);
    }

    [Fact]
    public void Enter_on_the_focused_header_toggles()
    {
        var section = new Section("Grid", new FixedBox(50, 30));
        var (host, platform) = TestPlatform.Show(new Column(section));
        platform.Render().Dispose();

        host.MoveFocus(forward: true);
        platform.Key(Key.Enter);

        Assert.False(section.IsExpanded);
    }
}
