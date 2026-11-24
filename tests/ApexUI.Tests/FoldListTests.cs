namespace ApexUI.Tests;

public class FoldListTests
{
    private static (FoldList List, TestPlatform Platform) Show(int count = 3, int cap = 0)
    {
        var list = new FoldList("Points") { MaxVisibleRows = cap, Width = 300 };
        for (int i = 0; i < count; i++) list.AddRow($"Row {i}", new FixedBox(100, 30), "Rounded", "8 px");
        var (_, platform) = TestPlatform.Show(new Column(list), 400, 800);
        platform.Render().Dispose();
        return (list, platform);
    }

    private static Widget Row(FoldList list, int index) => list.Children[0].Children[1].Children[index];

    private static Rect HeaderBounds(FoldList list, int index) => Row(list, index).Children[0].LayoutBounds;

    private static void ClickHeader(TestPlatform platform, FoldList list, int index)
    {
        Rect bounds = HeaderBounds(list, index);
        platform.Click(bounds.X + 40, bounds.CenterY);
        platform.Render().Dispose();
    }

    [Fact]
    public void Rows_start_closed_and_their_bodies_take_no_room()
    {
        var (list, _) = Show();

        Assert.Equal(3, list.Count);
        for (int i = 0; i < 3; i++)
        {
            Assert.False(list.IsOpen(i));
            Assert.False(list.BodyOf(i).IsVisible);
        }
    }

    [Fact]
    public void Clicking_a_header_opens_the_row_and_reports_it_and_a_second_click_closes_it()
    {
        var (list, platform) = Show();
        var reports = new List<(int, bool)>();
        list.RowToggled += (i, open) => reports.Add((i, open));

        ClickHeader(platform, list, 1);
        Assert.True(list.IsOpen(1));
        Assert.True(list.BodyOf(1).IsVisible);
        ClickHeader(platform, list, 1);

        Assert.False(list.IsOpen(1));
        Assert.Equal([(1, true), (1, false)], reports);
    }

    [Fact]
    public void Several_rows_stay_open_at_once()
    {
        var (list, platform) = Show();

        ClickHeader(platform, list, 0);
        ClickHeader(platform, list, 2);

        Assert.True(list.IsOpen(0));
        Assert.False(list.IsOpen(1));
        Assert.True(list.IsOpen(2));
    }

    [Fact]
    public void An_open_row_is_taller_than_a_closed_one_by_its_body()
    {
        var (list, platform) = Show();
        float closed = Row(list, 0).LayoutBounds.Height;

        ClickHeader(platform, list, 0);

        Assert.True(Row(list, 0).LayoutBounds.Height >= closed + 30);
    }

    [Fact]
    public void Space_on_a_focused_header_toggles_the_row()
    {
        var (list, platform) = Show();
        Rect bounds = HeaderBounds(list, 0);
        platform.Click(bounds.X + 40, bounds.CenterY);
        platform.Render().Dispose();

        platform.Key(Key.Space);

        Assert.False(list.IsOpen(0));
    }

    [Fact]
    public void Expand_all_and_Collapse_all_report_each_row_that_changed()
    {
        var (list, platform) = Show();
        list.SetOpen(1, true);
        var reports = new List<(int, bool)>();
        list.RowToggled += (i, open) => reports.Add((i, open));

        list.ExpandAll();
        Assert.Equal([(0, true), (2, true)], reports);
        reports.Clear();
        list.CollapseAll();

        Assert.Equal([(0, false), (1, false), (2, false)], reports);
        Assert.False(list.IsOpen(1));
        platform.Render().Dispose();
    }

    [Fact]
    public void SetOpen_from_code_raises_nothing()
    {
        var (list, _) = Show();
        int reports = 0;
        list.RowToggled += (_, _) => reports++;

        list.SetOpen(0, true);

        Assert.True(list.IsOpen(0));
        Assert.Equal(0, reports);
    }

    [Fact]
    public void The_header_buttons_expand_and_collapse_every_row()
    {
        var (list, platform) = Show();
        Rect expand = list.Children[0].Children[0].Children[1].LayoutBounds;
        Rect collapse = list.Children[0].Children[0].Children[2].LayoutBounds;

        platform.Click(expand.CenterX, expand.CenterY);
        for (int i = 0; i < 3; i++) Assert.True(list.IsOpen(i));
        platform.Render().Dispose();
        platform.Click(collapse.CenterX, collapse.CenterY);

        for (int i = 0; i < 3; i++) Assert.False(list.IsOpen(i));
    }

    [Fact]
    public void A_cap_hides_the_rows_past_it_until_Show_all()
    {
        var (list, platform) = Show(count: 8, cap: 3);

        Assert.False(list.IsHiddenByCap(2));
        Assert.True(list.IsHiddenByCap(3));
        Assert.True(list.IsHiddenByCap(7));

        list.ShowAll = true;
        platform.Render().Dispose();
        for (int i = 0; i < 8; i++) Assert.False(list.IsHiddenByCap(i));
    }

    [Fact]
    public void A_list_within_the_cap_shows_no_Show_all_link()
    {
        var (list, _) = Show(count: 3, cap: 3);

        Widget link = list.Children[0].Children[2];
        Assert.False(link.IsVisible);
    }

    [Fact]
    public void The_Show_all_link_toggles_the_cap_and_reads_Show_fewer()
    {
        var (list, platform) = Show(count: 8, cap: 3);
        Widget link = list.Children[0].Children[2];
        Assert.True(link.IsVisible);

        platform.Click(link.LayoutBounds.CenterX, link.LayoutBounds.CenterY);
        Assert.True(list.ShowAll);
        platform.Render().Dispose();
        platform.Click(link.LayoutBounds.CenterX, link.LayoutBounds.CenterY);

        Assert.False(list.ShowAll);
        Assert.True(list.IsHiddenByCap(5));
    }

    [Fact]
    public void Reveal_shows_all_rows_only_when_the_row_is_hidden()
    {
        var (list, _) = Show(count: 8, cap: 3);

        list.Reveal(1);
        Assert.False(list.ShowAll);
        list.Reveal(6);

        Assert.True(list.ShowAll);
    }

    [Fact]
    public void Hover_reports_the_row_under_the_pointer_and_minus_one_when_it_leaves()
    {
        var (list, platform) = Show();
        var reports = new List<int>();
        list.HoverChanged += reports.Add;

        Rect row0 = HeaderBounds(list, 0), row1 = HeaderBounds(list, 1);
        platform.Move(row0.X + 40, row0.CenterY);
        platform.Move(row1.X + 40, row1.CenterY);
        platform.Move(390, 790);

        Assert.Equal([0, 1, -1], reports);
    }

    [Fact]
    public void Changing_a_rows_text_updates_its_chip_and_summary_and_empty_text_hides_them()
    {
        var (list, platform) = Show();
        Widget header = Row(list, 0).Children[0];
        var chip = (Label)header.Children[1];
        var summary = (Label)header.Children[2];

        list.SetRowText(0, "Sharp", "");
        platform.Render().Dispose();

        Assert.Equal("Sharp", chip.Text);
        Assert.True(chip.IsVisible);
        Assert.False(summary.IsVisible);
    }

    [Fact]
    public void Clearing_the_rows_empties_the_list_and_ends_the_hover()
    {
        var (list, platform) = Show();
        var reports = new List<int>();
        list.HoverChanged += reports.Add;
        Rect row0 = HeaderBounds(list, 0);
        platform.Move(row0.X + 40, row0.CenterY);

        list.ClearRows();

        Assert.Equal(0, list.Count);
        Assert.Equal([0, -1], reports);
    }

    [Fact]
    public void A_highlighted_row_draws_as_hovered_and_raises_nothing()
    {
        var (list, platform) = Show();
        var reports = new List<int>();
        list.HoverChanged += reports.Add;
        Rect header = HeaderBounds(list, 1);
        int x = (int)header.CenterX, y = (int)header.Y + 1;
        SKColor plain;
        using (SKBitmap before = platform.Render()) plain = before.GetPixel(x, y);

        list.HighlightedRow = 1;

        using SKBitmap after = platform.Render();
        Assert.Equal(1, list.HighlightedRow);
        Assert.NotEqual(plain, after.GetPixel(x, y));
        Assert.Empty(reports);
    }

    [Fact]
    public void A_highlight_out_of_range_is_none_and_clearing_the_rows_drops_it()
    {
        var (list, _) = Show();

        list.HighlightedRow = 7;
        Assert.Equal(-1, list.HighlightedRow);

        list.HighlightedRow = 2;
        list.ClearRows();
        Assert.Equal(-1, list.HighlightedRow);
    }
}
