namespace ApexUI.Tests;

public class ListViewTests
{
    private const float Row = 20;

    private static (ListView<string> List, ObservableList<string> Items, UiHost Host, TestPlatform Platform) Show(int count = 5)
    {
        var items = new ObservableList<string>();
        for (int i = 0; i < count; i++) items.Add($"Item {i}");
        var list = new ListView<string>()
            .WithReorder()
            .WithItems(items)
            .WithTemplate(s => new Label { Text = s })
            .WithRowHeight(Row);
        var (host, platform) = TestPlatform.Show(list, 300, 200);
        platform.Render().Dispose();
        return (list, items, host, platform);
    }

    private static float RowCenter(int index) => index * Row + Row / 2;

    private static void Drag(TestPlatform platform, float fromY, float toY)
    {
        platform.Move(50, fromY);
        platform.Press(50, fromY);
        for (float y = fromY; Math.Abs(y - toY) > 0.5f; y += Math.Sign(toY - y) * Math.Min(5f, Math.Abs(toY - y)))
            platform.Move(50, y);
        platform.Move(50, toY);
        platform.Release(50, toY);
    }

    [Fact]
    public void Pressing_a_row_selects_it_and_reports_it_once()
    {
        var (list, _, _, platform) = Show();
        var reports = new List<int>();
        list.SelectionChanged += reports.Add;

        platform.Click(50, RowCenter(2));
        platform.Click(50, RowCenter(2));

        Assert.Equal(2, list.SelectedIndex);
        Assert.Equal([2], reports);
    }

    [Fact]
    public void Pressing_below_the_last_row_keeps_the_selection()
    {
        var (list, _, _, platform) = Show();
        list.SelectedIndex = 1;

        platform.Click(50, RowCenter(8));

        Assert.Equal(1, list.SelectedIndex);
    }

    [Fact]
    public void Arrow_keys_move_the_selection_within_the_items()
    {
        var (list, _, host, platform) = Show();
        platform.Click(50, RowCenter(0));
        Assert.Same(list, host.FocusedWidget);

        platform.Key(Key.Up);
        Assert.Equal(0, list.SelectedIndex);
        platform.Key(Key.Down);
        platform.Key(Key.Down);
        Assert.Equal(2, list.SelectedIndex);
        platform.Key(Key.End);
        Assert.Equal(4, list.SelectedIndex);
        platform.Key(Key.Down);
        Assert.Equal(4, list.SelectedIndex);
    }

    [Fact]
    public void Selecting_an_index_outside_the_items_selects_nothing()
    {
        var (list, _, _, _) = Show();
        list.SelectedIndex = 2;

        list.SelectedIndex = 9;

        Assert.Equal(-1, list.SelectedIndex);
    }

    [Theory]
    [InlineData(0, 3, 0, 2)]   // down into the gap above row 3
    [InlineData(0, 5, 0, 4)]   // past the end
    [InlineData(4, 0, 4, 0)]   // up to the top
    [InlineData(3, 1, 3, 1)]
    public void Dragging_a_row_reports_its_new_index(int from, int slot, int expectedFrom, int expectedTo)
    {
        var (list, _, _, platform) = Show();
        var moves = new List<(int, int)>();
        list.Reordered += (f, t) => moves.Add((f, t));

        Drag(platform, RowCenter(from), slot * Row + 1);

        Assert.Equal([(expectedFrom, expectedTo)], moves);
        Assert.False(list.IsDragging);
    }

    [Fact]
    public void Dropping_a_row_beside_itself_reports_nothing()
    {
        var (list, _, _, platform) = Show();
        int moves = 0;
        list.Reordered += (_, _) => moves++;

        Drag(platform, RowCenter(2), 2 * Row + 1);
        Drag(platform, RowCenter(2), 3 * Row + 1);

        Assert.Equal(0, moves);
    }

    [Fact]
    public void A_small_wobble_is_a_click_not_a_drag()
    {
        var (list, _, _, platform) = Show();
        int moves = 0;
        list.Reordered += (_, _) => moves++;

        Drag(platform, RowCenter(1), RowCenter(1) + 2);

        Assert.Equal(0, moves);
        Assert.Equal(1, list.SelectedIndex);
    }

    [Fact]
    public void Without_AllowReorder_dragging_only_selects()
    {
        var (list, _, _, platform) = Show();
        list.AllowReorder = false;
        int moves = 0;
        list.Reordered += (_, _) => moves++;

        Drag(platform, RowCenter(0), RowCenter(3));

        Assert.Equal(0, moves);
        Assert.False(list.IsDragging);
    }

    [Fact]
    public void A_press_handled_inside_a_row_does_not_select_it()
    {
        var items = new ObservableList<string>(["a", "b"]);
        var list = new ListView<string>()
            .WithItems(items)
            .WithTemplate(s => new Button(s))
            .WithRowHeight(40);
        var (_, platform) = TestPlatform.Show(list, 300, 200);
        platform.Render().Dispose();
        foreach (var row in list.Children) row.PointerPressed += e => e.Handled = true;

        platform.Click(20, 20);

        Assert.Equal(-1, list.SelectedIndex);
    }

    [Fact]
    public void Changing_the_items_keeps_the_scroll_position()
    {
        var (list, items, _, platform) = Show(100);
        platform.Move(50, 50);
        platform.Listener.Wheel(0, -2, KeyModifiers.None);
        platform.Render().Dispose();
        float scrolledRowY = list.Children.First(c => c is Label { Text: "Item 10" }).WindowBounds.Y;

        items.Set(0, "Renamed");
        platform.Render().Dispose();

        Assert.Equal(scrolledRowY, list.Children.First(c => c is Label { Text: "Item 10" }).WindowBounds.Y);
    }
}
