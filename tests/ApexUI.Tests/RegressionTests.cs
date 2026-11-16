namespace ApexUI.Tests;

public class ThemeTests
{
    // Colors that read the same on light and dark backgrounds.
    private static readonly string[] SharedColors = [nameof(Theme.Knob)];

    [Fact]
    public void Dark_theme_sets_every_color_instead_of_inheriting_light_values()
    {
        var colorProperties = typeof(Theme).GetProperties()
            .Where(p => p.PropertyType == typeof(SKColor) && p.CanWrite && !SharedColors.Contains(p.Name));

        foreach (var property in colorProperties)
            Assert.NotEqual(property.GetValue(Theme.Light), property.GetValue(Theme.Dark));
    }
}

public class TextInputTests
{
    [Fact]
    public void Setting_shorter_value_while_caret_is_at_end_still_draws()
    {
        var (input, platform) = FocusedInput();
        platform.Type("0.123");

        input.Value = "0.12";

        platform.Render().Dispose();
    }

    [Fact]
    public void Setting_shorter_value_with_selection_still_draws()
    {
        var (input, platform) = FocusedInput();
        platform.Type("hello");
        platform.Key(Key.A, KeyModifiers.Control);

        input.Value = "hi";

        platform.Render().Dispose();
    }

    [Fact]
    public void Backspace_at_the_end_leaves_the_caret_at_the_new_end()
    {
        var (input, platform) = FocusedInput();
        platform.Type("ab");

        platform.Key(Key.Backspace);
        platform.Type("c");

        Assert.Equal("ac", input.Value);
    }

    private static (TextInput, TestPlatform) FocusedInput()
    {
        var input = new TextInput { Width = 200, HAlign = HAlign.Left, VAlign = VAlign.Top };
        var (_, platform) = TestPlatform.Show(input);
        platform.Click(10, 10);
        return (input, platform);
    }
}

public class GridTests
{
    [Fact]
    public void Star_columns_size_to_content_when_width_is_unconstrained()
    {
        var grid = new Grid()
            .DefineColumns(GridLength.Star(), GridLength.Star())
            .WithSpacing(10, 0)
            .Add(new FixedBox(30, 10), 0, 0)
            .Add(new FixedBox(50, 20), 1, 0);

        grid.Measure(new Size(float.PositiveInfinity, float.PositiveInfinity));

        Assert.Equal(new Size(90, 20), grid.DesiredSize);
    }

    [Fact]
    public void Star_columns_share_finite_width()
    {
        var left  = new FixedBox(30, 10);
        var right = new FixedBox(50, 10);
        var grid  = new Grid()
            .DefineColumns(GridLength.Star(), GridLength.Star(3))
            .Add(left, 0, 0)
            .Add(right, 1, 0);

        grid.Measure(new Size(400, 100));
        grid.Arrange(new Rect(0, 0, 400, 100));

        Assert.Equal(100, left.LayoutBounds.Width);
        Assert.Equal(300, right.LayoutBounds.Width);
    }

    [Fact]
    public void RemoveAllChildren_forgets_cells()
    {
        var grid = new Grid().Add(new FixedBox(30, 10));

        grid.RemoveAllChildren();
        grid.Measure(new Size(100, 100));

        Assert.Equal(Size.Zero, grid.DesiredSize);
    }

    [Fact]
    public void Child_moved_to_another_parent_leaves_the_grid()
    {
        var box  = new FixedBox(30, 10);
        var grid = new Grid().Add(box);

        new Column().Add(box);
        grid.Measure(new Size(100, 100));

        Assert.Empty(grid.Children);
        Assert.Equal(Size.Zero, grid.DesiredSize);
    }
}

public class WidgetTreeTests
{
    [Fact]
    public void Adding_a_child_to_a_new_parent_removes_it_from_the_old_one()
    {
        var box = new FixedBox(10, 10);
        var oldParent = new Column(box);
        var newParent = new Row();

        newParent.Add(box);

        Assert.Empty(oldParent.Children);
        Assert.Same(newParent, box.Parent);
        Assert.Single(newParent.Children);
    }

    [Fact]
    public void Canvas_forgets_position_of_a_moved_child()
    {
        var box    = new FixedBox(10, 10);
        var canvas = new Layout.Canvas().Add(box, 50, 50);

        new Column().Add(box);
        canvas.Measure(new Size(200, 200));

        Assert.Equal(Size.Zero, canvas.DesiredSize);
    }
}

public class SliderTests
{
    [Fact]
    public void Lowering_max_clamps_value()
    {
        var slider = new Slider().WithValue(0.8f);

        slider.Max = 0.5f;

        Assert.Equal(0.5f, slider.Value);
    }

    [Fact]
    public void Raising_min_above_value_clamps_value()
    {
        var slider = new Slider().WithMax(100).WithValue(5);

        slider.Min = 10;

        Assert.Equal(10, slider.Value);
    }

    [Fact]
    public void Setting_min_above_current_max_first_does_not_throw()
    {
        var slider = new Slider { Min = 10, Max = 20 };

        Assert.Equal(10, slider.Value);
    }
}
