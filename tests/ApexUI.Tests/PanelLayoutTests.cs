namespace ApexUI.Tests;

public class StackLayoutTests
{
    [Fact]
    public void Stack_is_as_large_as_its_largest_child_plus_padding()
    {
        var stack = new Stack(new FixedBox(40, 10), new FixedBox(20, 30)) { Padding = new Thickness(4) };

        stack.Measure(new Size(500, 500));

        Assert.Equal(new Size(48, 38), stack.DesiredSize);
    }

    [Fact]
    public void Stack_children_share_the_content_box_and_keep_their_own_alignment()
    {
        var corner = new FixedBox(40, 10) { HAlign = HAlign.Left, VAlign = VAlign.Top };
        var fill   = new FixedBox(20, 30);
        var stack  = new Stack(corner, fill) { Padding = new Thickness(5) };

        stack.Measure(new Size(100, 100));
        stack.Arrange(new Rect(0, 0, 100, 100));

        Assert.Equal(new Rect(5, 5, 40, 10), corner.LayoutBounds);
        Assert.Equal(new Rect(5, 5, 90, 90), fill.LayoutBounds);
    }

    [Fact]
    public void Hidden_children_do_not_size_the_stack()
    {
        var stack = new Stack(new FixedBox(40, 10), new FixedBox(90, 90) { IsVisible = false });

        stack.Measure(new Size(500, 500));

        Assert.Equal(new Size(40, 10), stack.DesiredSize);
    }
}

public class LinearPanelLayoutTests
{
    [Fact]
    public void Hidden_children_take_neither_space_nor_spacing()
    {
        var first  = new FixedBox(40, 10);
        var hidden = new FixedBox(30, 10) { IsVisible = false };
        var last   = new FixedBox(20, 10);
        var row    = new Row(first, hidden, last).WithSpacing(5);

        row.Measure(new Size(500, 100));
        row.Arrange(new Rect(0, 0, 500, 100));

        Assert.Equal(65, row.DesiredSize.Width);
        Assert.Equal(45, last.LayoutBounds.X);
    }

    [Fact]
    public void Children_align_across_the_line_inside_their_slot()
    {
        var centered = new FixedBox(20, 10) { HAlign = HAlign.Center };
        var right    = new FixedBox(20, 10) { HAlign = HAlign.Right };
        var column   = new Column(centered, right);

        column.Measure(new Size(100, 100));
        column.Arrange(new Rect(0, 0, 100, 100));

        Assert.Equal(new Rect(40, 0, 20, 10), centered.LayoutBounds);
        Assert.Equal(new Rect(80, 10, 20, 10), right.LayoutBounds);
    }
}

public class WrapLayoutTests
{
    private static (Wrap Wrap, FixedBox[] Boxes) FourBoxes()
    {
        FixedBox[] boxes = [new(40, 10), new(40, 10), new(40, 20), new(40, 10)];
        var wrap = new Wrap(boxes).WithSpacing(5, 3);
        return (wrap, boxes);
    }

    [Fact]
    public void Children_that_overflow_the_width_move_to_the_next_line()
    {
        var (wrap, boxes) = FourBoxes();

        wrap.Measure(new Size(100, 500));
        wrap.Arrange(new Rect(0, 0, 100, 500));

        // Two 40 px boxes and a 5 px gap fit in 100; a third would need 130.
        Assert.Equal(new Rect(0, 0, 40, 10), boxes[0].LayoutBounds);
        Assert.Equal(new Rect(45, 0, 40, 10), boxes[1].LayoutBounds);
        Assert.Equal(new Rect(0, 13, 40, 20), boxes[2].LayoutBounds);
        Assert.Equal(new Rect(45, 13, 40, 20), boxes[3].LayoutBounds);
    }

    [Fact]
    public void Wrap_measures_its_widest_line_and_lines_plus_spacing()
    {
        var (wrap, _) = FourBoxes();

        wrap.Measure(new Size(100, 500));

        Assert.Equal(new Size(85, 33), wrap.DesiredSize);
    }

    [Fact]
    public void Unconstrained_width_keeps_everything_on_one_line()
    {
        var (wrap, _) = FourBoxes();

        wrap.Measure(Size.Infinite);

        Assert.Equal(new Size(175, 20), wrap.DesiredSize);
    }

    [Fact]
    public void A_child_wider_than_the_wrap_gets_a_line_of_its_own()
    {
        var small = new FixedBox(40, 10);
        var wide  = new FixedBox(150, 10);
        var wrap  = new Wrap(small, wide).WithSpacing(5, 0);

        wrap.Measure(new Size(100, 500));
        wrap.Arrange(new Rect(0, 0, 100, 500));

        Assert.Equal(new Rect(0, 10, 150, 10), wide.LayoutBounds);
    }
}

public class CanvasLayoutTests
{
    [Fact]
    public void Explicit_size_sets_the_slot_and_the_canvas_extent()
    {
        var box    = new FixedBox(5, 5);
        var canvas = new Canvas().Add(box, 10, 20, 50, 60);

        canvas.Measure(Size.Infinite);
        canvas.Arrange(new Rect(100, 100, 300, 300));

        Assert.Equal(new Size(60, 80), canvas.DesiredSize);
        Assert.Equal(new Rect(110, 120, 50, 60), box.LayoutBounds);
    }
}

public class GridLayoutTests
{
    [Fact]
    public void Fixed_and_star_rows_split_a_finite_height()
    {
        var top    = new FixedBox(5, 5);
        var bottom = new FixedBox(5, 5);
        var grid = new Grid()
            .DefineColumns(GridLength.Fixed(50), GridLength.Star())
            .DefineRows(GridLength.Fixed(20), GridLength.Star(), GridLength.Star(2))
            .Add(top, 0, 1)
            .Add(bottom, 1, 2);

        grid.Measure(new Size(200, 110));
        grid.Arrange(new Rect(0, 0, 200, 110));

        Assert.Equal(new Rect(0, 20, 50, 30), top.LayoutBounds);
        Assert.Equal(new Rect(50, 50, 150, 60), bottom.LayoutBounds);
    }

    [Fact]
    public void Auto_tracks_fit_their_largest_cell_and_rows_are_inferred_from_cells()
    {
        var a = new FixedBox(30, 10);
        var b = new FixedBox(20, 25);
        var c = new FixedBox(10, 5);
        var grid = new Grid()
            .DefineColumns(GridLength.Auto, GridLength.Auto)
            .WithSpacing(4, 2)
            .Add(a, 0, 0)
            .Add(b, 1, 0)
            .Add(c, 0, 1);

        grid.Measure(Size.Infinite);
        grid.Arrange(new Rect(0, 0, grid.DesiredSize.Width, grid.DesiredSize.Height));

        Assert.Equal(new Size(54, 32), grid.DesiredSize);
        Assert.Equal(new Rect(34, 0, 20, 25), b.LayoutBounds);
        Assert.Equal(new Rect(0, 27, 30, 5), c.LayoutBounds);
    }

    [Fact]
    public void Spanning_cells_do_not_widen_auto_columns()
    {
        var narrow = new FixedBox(20, 10);
        var wide   = new FixedBox(200, 10);
        var grid = new Grid()
            .DefineColumns(GridLength.Auto, GridLength.Auto)
            .Add(narrow, 0, 0)
            .Add(wide, 0, 1, colSpan: 2);

        grid.Measure(Size.Infinite);

        Assert.Equal(20, grid.DesiredSize.Width);
    }

    [Fact]
    public void A_cell_outside_the_columns_gets_an_empty_slot_at_the_origin()
    {
        var stray = new FixedBox(10, 10);
        var grid  = new Grid().WithColumns(2).Add(stray, 5, 0);

        grid.Measure(new Size(100, 100));
        grid.Arrange(new Rect(10, 10, 100, 100));

        Assert.Equal(new Rect(10, 10, 0, 0), stray.LayoutBounds);
    }
}

public class SizeAndAlignmentTests
{
    [Fact]
    public void Right_and_bottom_alignment_place_the_child_at_the_far_corner()
    {
        var box = new FixedBox(20, 10) { HAlign = HAlign.Right, VAlign = VAlign.Bottom };

        box.Measure(new Size(100, 50));
        box.Arrange(new Rect(0, 0, 100, 50));

        Assert.Equal(new Rect(80, 40, 20, 10), box.LayoutBounds);
    }

    [Fact]
    public void Alignment_applies_inside_the_margin()
    {
        var box = new FixedBox(20, 10)
        {
            HAlign = HAlign.Right, VAlign = VAlign.Bottom, Margin = new Thickness(5, 4),
        };

        box.Measure(new Size(100, 50));
        box.Arrange(new Rect(0, 0, 100, 50));

        Assert.Equal(new Rect(75, 36, 20, 10), box.LayoutBounds);
    }

    [Fact]
    public void Min_and_max_clamp_the_desired_size()
    {
        var box = new FixedBox(10, 10)
        {
            MinWidth = 30, MaxHeight = 5, HAlign = HAlign.Left, VAlign = VAlign.Top,
        };

        box.Measure(new Size(100, 100));
        box.Arrange(new Rect(0, 0, 100, 100));

        Assert.Equal(new Size(30, 5), box.DesiredSize);
        Assert.Equal(new Rect(0, 0, 30, 5), box.LayoutBounds);
    }

    [Fact]
    public void Explicit_size_replaces_the_content_size()
    {
        var box = new FixedBox(10, 10) { Width = 70, Height = 15, HAlign = HAlign.Left, VAlign = VAlign.Top };

        box.Measure(new Size(100, 100));
        box.Arrange(new Rect(0, 0, 100, 100));

        Assert.Equal(new Rect(0, 0, 70, 15), box.LayoutBounds);
    }

    [Fact]
    public void An_aligned_child_never_exceeds_its_slot()
    {
        var box = new FixedBox(300, 300) { HAlign = HAlign.Center, VAlign = VAlign.Center };

        box.Measure(new Size(100, 50));
        box.Arrange(new Rect(0, 0, 100, 50));

        Assert.Equal(new Rect(0, 0, 100, 50), box.LayoutBounds);
    }

    [Fact]
    public void Aspect_ratio_derives_height_from_width()
    {
        var box = new FixedBox(10, 10)
        {
            Width = 100, AspectRatio = 2, HAlign = HAlign.Left, VAlign = VAlign.Top,
        };

        box.Measure(new Size(300, 300));
        box.Arrange(new Rect(0, 0, 300, 300));

        Assert.Equal(new Size(100, 50), box.DesiredSize);
        Assert.Equal(new Rect(0, 0, 100, 50), box.LayoutBounds);
    }

    [Fact]
    public void Aspect_ratio_can_derive_width_from_height()
    {
        var box = new FixedBox(10, 10)
        {
            Height = 40, AspectRatio = 2, AspectRatioMode = AspectRatioMode.HeightControlsWidth,
            HAlign = HAlign.Left, VAlign = VAlign.Top,
        };

        box.Measure(new Size(300, 300));

        Assert.Equal(new Size(80, 40), box.DesiredSize);
    }
}
