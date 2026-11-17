namespace ApexUI.Tests;

public class LinearLayoutTests
{
    [Fact]
    public void Column_measures_sum_of_heights_plus_spacing_and_widest_child()
    {
        var column = new Column(new FixedBox(40, 10), new FixedBox(60, 20)).WithSpacing(5);

        column.Measure(new Size(500, 500));

        Assert.Equal(new Size(60, 35), column.DesiredSize);
    }

    [Fact]
    public void Row_arranges_children_left_to_right_with_spacing()
    {
        var first  = new FixedBox(40, 10);
        var second = new FixedBox(60, 20);
        var row    = new Row(first, second).WithSpacing(5);

        row.Measure(new Size(500, 100));
        row.Arrange(new Rect(10, 20, 500, 100));

        Assert.Equal(new Rect(10, 20, 40, 100), first.LayoutBounds);
        Assert.Equal(new Rect(55, 20, 60, 100), second.LayoutBounds);
    }

    [Fact]
    public void Spacer_pushes_following_child_to_the_far_edge()
    {
        var left  = new FixedBox(40, 10);
        var right = new FixedBox(60, 10);
        var row   = new Row(left, new Spacer(), right);

        row.Measure(new Size(300, 50));
        row.Arrange(new Rect(0, 0, 300, 50));

        Assert.Equal(240, right.LayoutBounds.X);
        Assert.Equal(300, right.LayoutBounds.Right);
    }

    [Fact]
    public void Margin_shrinks_arranged_bounds()
    {
        var box = new FixedBox(10, 10) { Margin = new Thickness(5, 10) };

        box.Measure(new Size(100, 100));
        box.Arrange(new Rect(0, 0, 100, 100));

        Assert.Equal(new Size(20, 30), box.DesiredSize);
        Assert.Equal(new Rect(5, 10, 90, 80), box.LayoutBounds);
    }

    [Fact]
    public void Center_alignment_places_child_in_the_middle()
    {
        var box = new FixedBox(20, 10) { HAlign = HAlign.Center, VAlign = VAlign.Center };

        box.Measure(new Size(100, 50));
        box.Arrange(new Rect(0, 0, 100, 50));

        Assert.Equal(new Rect(40, 20, 20, 10), box.LayoutBounds);
    }

    [Fact]
    public void A_growing_child_that_fills_its_space_leaves_room_for_the_children_after_it()
    {
        var fill = new TextInput { Grow = 1 };
        var tail = new FixedBox(30, 20);
        var row = new Row(new FixedBox(20, 20), fill, tail) { Spacing = 0 };

        row.Measure(new Size(200, 50));
        row.Arrange(new Rect(0, 0, 200, 50));

        Assert.Equal(150, fill.LayoutBounds.Width, 3);
        Assert.Equal(200, tail.LayoutBounds.Right, 3);
    }
}
