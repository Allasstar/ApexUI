namespace ApexUI.Tests;

public class PaddingTests
{
    [Fact]
    public void Padding_on_any_panel_insets_its_children_and_adds_to_its_size()
    {
        var child  = new FixedBox(40, 10);
        var column = new Column(child) { Padding = new Thickness(5, 7) };

        column.Measure(new Size(200, 200));
        column.Arrange(new Rect(0, 0, 200, 200));

        Assert.Equal(new Size(50, 24), column.DesiredSize);
        Assert.Equal(new Rect(0, 0, 200, 200), column.LayoutBounds);
        Assert.Equal(new Rect(5, 7, 190, 186), column.ContentBounds);
        Assert.Equal(new Rect(5, 7, 190, 10), child.LayoutBounds);
    }

    [Fact]
    public void Explicit_width_includes_padding()
    {
        var box = new FixedBox(10, 10) { Width = 50, Padding = new Thickness(10), HAlign = HAlign.Left };

        box.Measure(new Size(200, 200));
        box.Arrange(new Rect(0, 0, 200, 200));

        Assert.Equal(50, box.LayoutBounds.Width);
        Assert.Equal(30, box.ContentBounds.Width);
    }

    [Fact]
    public void Padding_box_matches_padding_on_the_widget()
    {
        var a = new FixedBox(30, 10);
        var b = new FixedBox(30, 10);
        var box   = new PaddingBox(a, 8);
        var stack = new Stack(b) { Padding = new Thickness(8) };

        box.Measure(new Size(100, 100));
        stack.Measure(new Size(100, 100));
        box.Arrange(new Rect(0, 0, 100, 100));
        stack.Arrange(new Rect(0, 0, 100, 100));

        Assert.Equal(stack.DesiredSize, box.DesiredSize);
        Assert.Equal(b.LayoutBounds, a.LayoutBounds);
    }

    [Fact]
    public void Button_padding_surrounds_its_label()
    {
        var button = new Button("OK") { Padding = new Thickness(16, 8), HAlign = HAlign.Left, VAlign = VAlign.Top };
        var label  = button.Children[0];

        button.Measure(new Size(300, 100));
        button.Arrange(new Rect(0, 0, 300, 100));

        Assert.Equal(button.LayoutBounds.Width, label.DesiredSize.Width + 32, 3);
        Assert.Equal(button.LayoutBounds.Height, label.DesiredSize.Height + 16, 3);
    }

    // Regression: alignment used the size including the margin, so a left-aligned widget
    // with a margin was arranged wider than it asked to be.
    [Fact]
    public void Aligned_widget_with_margin_gets_its_content_size()
    {
        var box = new FixedBox(20, 10) { Margin = new Thickness(5), HAlign = HAlign.Left, VAlign = VAlign.Top };

        box.Measure(new Size(100, 100));
        box.Arrange(new Rect(0, 0, 100, 100));

        Assert.Equal(new Rect(5, 5, 20, 10), box.LayoutBounds);
    }
}

public class GrowTests
{
    [Fact]
    public void Growing_children_share_leftover_space_by_their_factors()
    {
        var a = new FixedBox(10, 10) { Grow = 1 };
        var b = new FixedBox(10, 10) { Grow = 3 };
        var row = new Row(a, new FixedBox(20, 10), b);

        row.Measure(new Size(200, 50));
        row.Arrange(new Rect(0, 0, 200, 50));

        // 200 - 40 = 160 leftover: a gets 40, b gets 120.
        Assert.Equal(new Rect(0, 0, 50, 50), a.LayoutBounds);
        Assert.Equal(new Rect(70, 0, 130, 50), b.LayoutBounds);
    }

    [Fact]
    public void Growing_child_in_a_column_fills_the_remaining_height()
    {
        var header = new FixedBox(10, 30);
        var body   = new FixedBox(10, 10) { Grow = 1 };
        var column = new Column(header, body).WithSpacing(10);

        column.Measure(new Size(100, 300));
        column.Arrange(new Rect(0, 0, 100, 300));

        Assert.Equal(new Rect(0, 40, 100, 260), body.LayoutBounds);
    }

    [Fact]
    public void Nothing_grows_when_the_children_already_fill_the_line()
    {
        var a = new FixedBox(80, 10) { Grow = 1 };
        var b = new FixedBox(80, 10);
        var row = new Row(a, b);

        row.Measure(new Size(100, 20));
        row.Arrange(new Rect(0, 0, 100, 20));

        Assert.Equal(80, a.LayoutBounds.Width);
    }

    [Fact]
    public void Horizontal_radio_group_lays_options_out_in_a_row()
    {
        var group = new RadioGroup<int>(Orientation.Horizontal, spacing: 10)
            .AddOption(1, "One")
            .AddOption(2, "Two");

        group.Measure(new Size(400, 100));
        group.Arrange(new Rect(0, 0, 400, 100));

        var first  = group.Children[0].LayoutBounds;
        var second = group.Children[1].LayoutBounds;
        Assert.Equal(first.Y, second.Y);
        Assert.Equal(first.Right + 10, second.X, 3);
    }
}

public class LayoutCacheTests
{
    [Fact]
    public void Measuring_again_with_the_same_constraint_does_no_work()
    {
        var box = new CountingBox(10, 10);

        box.Measure(new Size(100, 100));
        box.Measure(new Size(100, 100));
        Assert.Equal(1, box.Measures);

        box.Measure(new Size(50, 100));
        Assert.Equal(2, box.Measures);

        box.InvalidateLayout();
        box.Measure(new Size(50, 100));
        Assert.Equal(3, box.Measures);
    }

    [Fact]
    public void A_change_lays_out_only_its_own_branch()
    {
        var changed = new CountingBox(10, 10);
        var sibling = new CountingBox(10, 10);
        var root = new Column(new Row(changed), new Row(sibling));
        var (_, platform) = TestPlatform.Show(root);
        platform.Render().Dispose();
        int siblingMeasures = sibling.Measures, siblingArranges = sibling.Arranges;

        changed.Resize(20, 10);
        platform.Render().Dispose();

        Assert.Equal(20, changed.LayoutBounds.Width);
        Assert.Equal(siblingMeasures, sibling.Measures);
        Assert.Equal(siblingArranges, sibling.Arranges);
    }

    [Fact]
    public void A_sibling_that_moves_is_arranged_again()
    {
        var first  = new CountingBox(10, 10);
        var second = new CountingBox(10, 10);
        var root   = new Column(first, second);
        var (_, platform) = TestPlatform.Show(root);
        platform.Render().Dispose();

        first.Resize(10, 30);
        platform.Render().Dispose();

        Assert.Equal(30, second.LayoutBounds.Y);
    }

    [Fact]
    public void Changes_inside_a_hidden_branch_are_laid_out_when_it_is_shown()
    {
        var label  = new Label { Text = "a" };
        var hidden = new Column(new Column(label)) { IsVisible = false };
        var root   = new Column(hidden);
        var (_, platform) = TestPlatform.Show(root);
        platform.Render().Dispose();

        label.Text = "a much longer text than before";
        platform.Render().Dispose();
        hidden.IsVisible = true;
        platform.Render().Dispose();

        Assert.False(root.IsLayoutDirty);
        Assert.True(label.LayoutBounds.Width > 100);
    }

    [Fact]
    public void A_second_change_under_an_already_dirty_branch_is_still_laid_out()
    {
        var a = new CountingBox(10, 10);
        var b = new CountingBox(10, 10);
        var root = new Column(new Column(a, b));
        var (_, platform) = TestPlatform.Show(root);
        platform.Render().Dispose();

        a.Resize(10, 20);
        b.Resize(10, 40);
        platform.Render().Dispose();

        Assert.Equal(20, b.LayoutBounds.Y);
        Assert.Equal(40, b.LayoutBounds.Height);
        Assert.False(root.IsLayoutDirty);
    }

    [Fact]
    public void Changing_the_theme_measures_text_again()
    {
        var label = new Label { Text = "Hello", HAlign = HAlign.Left, VAlign = VAlign.Top };
        var (host, platform) = TestPlatform.Show(new Column(label));
        platform.Render().Dispose();
        float before = label.LayoutBounds.Height;

        host.Theme = new Theme { FontSizeBase = Theme.Light.FontSizeBase * 2 };
        platform.Render().Dispose();

        Assert.Equal(before * 2, label.LayoutBounds.Height, 3);
    }
}

public class PanelChildDataTests
{
    [Fact]
    public void A_child_moved_from_a_grid_to_a_canvas_is_placed_by_the_canvas()
    {
        var child  = new FixedBox(10, 10);
        var grid   = new Grid().Add(child, 0, 0);
        var canvas = new Canvas().Add(child, 30, 40);

        grid.Measure(new Size(100, 100));
        grid.Arrange(new Rect(0, 0, 100, 100));
        canvas.Measure(new Size(100, 100));
        canvas.Arrange(new Rect(0, 0, 100, 100));

        Assert.Empty(grid.Children);
        Assert.Equal(new Rect(30, 40, 10, 10), child.LayoutBounds);
    }

    [Fact]
    public void Canvas_move_repositions_a_child()
    {
        var child  = new FixedBox(10, 10);
        var canvas = new Canvas().Add(child, 0, 0);
        canvas.Measure(new Size(100, 100));
        canvas.Arrange(new Rect(0, 0, 100, 100));

        canvas.Move(child, 25, 35);
        canvas.Measure(new Size(100, 100));
        canvas.Arrange(new Rect(0, 0, 100, 100));

        Assert.Equal(new Rect(25, 35, 10, 10), child.LayoutBounds);
        Assert.Equal(new Size(35, 45), canvas.DesiredSize);
    }

    [Fact]
    public void Grid_spans_and_star_columns()
    {
        var label = new FixedBox(30, 10);
        var wide  = new FixedBox(10, 20);
        var grid = new Grid()
            .DefineColumns(GridLength.Auto, GridLength.Star(), GridLength.Star(3))
            .WithSpacing(10, 5)
            .Add(label, 0, 0)
            .Add(wide, 0, 1, colSpan: 3);

        grid.Measure(new Size(250, 100));
        grid.Arrange(new Rect(0, 0, 250, 100));

        // 250 - 30 auto - 20 spacing = 200 for the stars: 50 and 150.
        Assert.Equal(new Rect(0, 15, 250, 20), wide.LayoutBounds);
        Assert.Equal(new Rect(0, 0, 30, 10), label.LayoutBounds);
    }
}

public class LayoutAllocationTests
{
    private static long LayoutBytes(Widget panel) => TestCanvas.MinAllocatedBytes(() =>
    {
        panel.InvalidateLayout();
        panel.Measure(new Size(300, 300));
        panel.Arrange(new Rect(0, 0, 300, 300));
    });

    [Fact]
    public void Grid_layout_allocates_nothing()
    {
        var grid = new Grid().DefineColumns(GridLength.Auto, GridLength.Star()).WithSpacing(4, 4);
        for (int r = 0; r < 5; r++)
            grid.Add(new FixedBox(20, 10), 0, r).Add(new FixedBox(40, 10) { Grow = 1 }, 1, r);

        Assert.Equal(0, LayoutBytes(grid));
    }

    [Fact]
    public void Wrap_layout_allocates_nothing()
    {
        var boxes = new Widget[20];
        for (int i = 0; i < boxes.Length; i++) boxes[i] = new FixedBox(40, 10);
        var wrap = new Wrap(boxes).WithSpacing(4, 4);

        Assert.Equal(0, LayoutBytes(wrap));
    }

    [Fact]
    public void Row_layout_allocates_nothing()
    {
        var row = new Row(new FixedBox(10, 10), new Spacer(), new FixedBox(10, 10) { Grow = 2 });

        Assert.Equal(0, LayoutBytes(row));
    }
}

public class ChildTransformTests
{
    private static (Scroll Scroll, CountingBox[] Items, TestPlatform Platform) ScrolledColumn(int count = 50)
    {
        var items  = new CountingBox[count];
        var column = new Column();
        for (int i = 0; i < count; i++) column.Add(items[i] = new CountingBox(100, 20));
        var scroll = new Scroll(column);
        var (_, platform) = TestPlatform.Show(scroll, 200, 200);
        platform.Render().Dispose();
        return (scroll, items, platform);
    }

    [Fact]
    public void Scrolling_redraws_without_layout()
    {
        var (scroll, items, platform) = ScrolledColumn();
        int arranges = items[10].Arranges;
        var bounds   = items[10].LayoutBounds;
        int frames   = platform.FramesRequested;

        platform.Move(50, 50);
        platform.Listener.Wheel(0, -1, KeyModifiers.None);

        Assert.False(scroll.IsLayoutDirty);
        Assert.True(platform.FramesRequested > frames);
        Assert.Equal(bounds, items[10].LayoutBounds);
        Assert.Equal(bounds.Y - 40, items[10].WindowBounds.Y);
        platform.Render().Dispose();
        Assert.Equal(arranges, items[10].Arranges);
    }

    [Fact]
    public void Clicks_reach_the_item_under_the_pointer_after_scrolling()
    {
        var (_, items, platform) = ScrolledColumn();
        int clicked = -1;
        for (int i = 0; i < items.Length; i++)
        {
            int index = i;
            items[i].Click += _ => clicked = index;
        }

        platform.Move(50, 50);
        platform.Listener.Wheel(0, -2, KeyModifiers.None); // 80 units down
        platform.Click(50, 50);

        Assert.Equal(6, clicked); // content y = 130
    }

    [Fact]
    public void Pointer_positions_are_in_the_receivers_layout_space()
    {
        var (scroll, items, platform) = ScrolledColumn();
        float itemY = 0, itemWindowY = 0, scrollY = 0;
        items[6].PointerPressed += e => { itemY = e.Y; itemWindowY = e.WindowY; };
        scroll.PointerPressed   += e => scrollY = e.Y;

        platform.Move(50, 50);
        platform.Listener.Wheel(0, -2, KeyModifiers.None);
        platform.Click(50, 50);

        Assert.Equal(130, itemY);
        Assert.Equal(50, itemWindowY);
        Assert.Equal(50, scrollY);
    }

    [Fact]
    public void Hover_follows_content_that_scrolls_under_a_still_pointer()
    {
        var (_, items, platform) = ScrolledColumn();
        platform.Move(50, 50);
        Assert.True(items[2].IsHovered);

        platform.Listener.Wheel(0, -1, KeyModifiers.None); // 40 units down

        Assert.False(items[2].IsHovered);
        Assert.True(items[4].IsHovered);
    }

    [Fact]
    public void Only_children_inside_the_visible_area_are_drawn()
    {
        var (_, items, platform) = ScrolledColumn();
        int drawn = 0;
        foreach (var item in items) drawn += item.Draws;

        Assert.InRange(drawn, 10, 11);
    }

    [Fact]
    public void Scale_applies_to_hit_testing_and_window_bounds()
    {
        var child = new FixedBox(10, 10);
        var panel = new TransformPanel(child);
        panel.Measure(new Size(200, 200));
        panel.Arrange(new Rect(0, 0, 200, 200));

        panel.SetTransform(new Transform2D(50, 20, 2));

        Assert.Equal(new Rect(50, 20, 20, 20), child.WindowBounds);
        Assert.Same(child, panel.HitTest(65, 35));
        Assert.Same(panel, panel.HitTest(45, 35));
        Assert.Equal((7.5f, 7.5f), child.PointFromWindow(65, 35));
    }

    [Fact]
    public void Anchored_overlay_follows_its_anchor_when_the_content_scrolls()
    {
        var (_, items, platform) = ScrolledColumn();
        // Not hit-testable, so the wheel reaches the scroll under it.
        var overlay = new Overlay { Content = new FixedBox(30, 30), IsHitTestVisible = false };
        overlay.Open(items[5]);
        platform.Render().Dispose();

        platform.Move(50, 50);
        platform.Listener.Wheel(0, -1, KeyModifiers.None);
        platform.Render().Dispose();

        Assert.Equal(items[5].WindowBounds.Bottom, overlay.Content!.LayoutBounds.Y);
    }
}

public class VirtualListScrollTests
{
    private static (VirtualList<int> List, Func<int> Built, TestPlatform Platform) Show()
    {
        int built = 0;
        var list = new VirtualList<int>()
            .WithItems(new ObservableList<int>(Enumerable.Range(0, 1000)))
            .WithTemplate(i => { built++; return new FixedBox(10, 20); })
            .WithRowHeight(20);
        var (_, platform) = TestPlatform.Show(list, 300, 200);
        platform.Render().Dispose();
        return (list, () => built, platform);
    }

    [Fact]
    public void Scrolling_a_little_redraws_without_layout()
    {
        var (list, built, platform) = Show();
        int before = built();

        platform.Move(50, 50);
        platform.Listener.Wheel(0, -1f / 3f, KeyModifiers.None); // one row

        Assert.False(list.IsLayoutDirty);
        Assert.Equal(before, built());
    }

    [Fact]
    public void Scrolling_past_the_built_rows_builds_more_and_clicks_reach_them()
    {
        var (list, built, platform) = Show();
        int clicked = -1;
        list.PointerPressed += e => { if (e.Source != list) clicked = (int)(e.Source.LayoutBounds.Y / 20); };
        int before = built();

        platform.Move(50, 50);
        platform.Listener.Wheel(0, -10, KeyModifiers.None); // 30 rows
        platform.Render().Dispose();
        platform.Click(50, 50);

        Assert.True(built() > before);
        Assert.Equal(32, clicked);
    }
}
