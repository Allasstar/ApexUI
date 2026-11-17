namespace ApexUI.Tests;

public class ToggleGroupTests
{
    private const string Square = "M4 4H20V20H4Z";

    private static ToggleGroup<string> Letters() => new ToggleGroup<string>()
        .AddOption("a", "Alpha")
        .AddOption("b", "Beta")
        .AddOption("c", new Icon(Square), "Gamma");

    [Fact]
    public void Options_are_laid_out_in_a_row()
    {
        var group = Letters();
        group.Measure(Size.Infinite);
        group.Arrange(new Rect(0, 0, group.DesiredSize.Width, group.DesiredSize.Height));

        float previousRight = float.NegativeInfinity;
        foreach (var child in group.Children)
        {
            Assert.True(child.LayoutBounds.X >= previousRight);
            previousRight = child.LayoutBounds.Right;
        }
        Assert.Equal(3, group.Children.Count);
    }

    [Fact]
    public void Clicking_an_option_selects_it_once()
    {
        var group   = Letters().WithSelected("a");
        var changes = new List<string?>();
        group.SelectionChanged += changes.Add;
        var (_, platform) = TestPlatform.Show(new Row(group));
        platform.Render().Dispose();

        var beta = group.Children[1].WindowBounds;
        platform.Click(beta.CenterX, beta.CenterY);
        platform.Click(beta.CenterX, beta.CenterY);

        Assert.Equal("b", group.SelectedValue);
        Assert.Equal(["b"], changes);
    }

    [Fact]
    public void Icon_option_selects_through_its_tooltip()
    {
        var group = Letters().WithSelected("a");
        var (_, platform) = TestPlatform.Show(new Row(group));
        platform.Render().Dispose();

        var gamma = group.Children[2].WindowBounds;
        platform.Click(gamma.CenterX, gamma.CenterY);

        Assert.Equal("c", group.SelectedValue);
    }

    [Fact]
    public void Setting_the_same_value_raises_nothing()
    {
        var group = Letters().WithSelected("b");
        int raised = 0;
        group.SelectionChanged += _ => raised++;

        group.SelectedValue = "b";

        Assert.Equal(0, raised);
    }

    [Fact]
    public void Bind_follows_the_source_both_ways()
    {
        var source = new Bindable<string>("a");
        var group  = Letters().Bind(source);
        var (_, platform) = TestPlatform.Show(new Row(group));
        platform.Render().Dispose();

        source.Value = "c";
        Assert.Equal("c", group.SelectedValue);

        var alpha = group.Children[0].WindowBounds;
        platform.Click(alpha.CenterX, alpha.CenterY);
        Assert.Equal("a", source.Value);
    }
}
