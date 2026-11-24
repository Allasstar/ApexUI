namespace ApexUI.Tests;

public class ReorderListTests
{
    private static (ReorderList List, UiHost Host, TestPlatform Platform) Show(int count = 3, bool bodies = false)
    {
        var list = new ReorderList();
        for (int i = 0; i < count; i++) list.AddItem($"Item {i}", bodies ? new FixedBox(100, 30) : null);
        var (host, platform) = TestPlatform.Show(list, 300, 400);
        platform.Render().Dispose();
        return (list, host, platform);
    }

    private static Widget Item(ReorderList list, int index) => list.Children[0].Children[index];

    private static Rect ItemBounds(ReorderList list, int index) => Item(list, index).LayoutBounds;

    private static Widget Header(ReorderList list, int index) => Item(list, index).Children[0];

    private static Rect HeaderBounds(ReorderList list, int index) => Header(list, index).LayoutBounds;

    // The grip sits in the header's left padding, clear of the eye button.
    private static float GripX(ReorderList list) => HeaderBounds(list, 0).X + 3f;

    private static void Drag(TestPlatform platform, float x, float fromY, float toY)
    {
        platform.Move(x, fromY);
        platform.Press(x, fromY);
        for (float y = fromY; Math.Abs(y - toY) > 0.5f; y += Math.Sign(toY - y) * Math.Min(5f, Math.Abs(toY - y)))
            platform.Move(x, y);
        platform.Move(x, toY);
        platform.Release(x, toY);
    }

    [Fact]
    public void Items_stack_top_to_bottom_with_their_bodies_under_the_headers()
    {
        var (list, _, _) = Show(bodies: true);

        Assert.Equal(3, list.Count);
        for (int i = 1; i < 3; i++) Assert.True(ItemBounds(list, i).Y >= ItemBounds(list, i - 1).Bottom);
        Widget body = list.BodyOf(1)!;
        Assert.True(body.LayoutBounds.Y >= HeaderBounds(list, 1).Bottom);
        Assert.True(ItemBounds(list, 1).Height >= HeaderBounds(list, 1).Height + 30);
    }

    [Fact]
    public void Dragging_a_header_below_the_next_item_reports_the_final_index()
    {
        var (list, _, platform) = Show();
        var moves = new List<(int, int)>();
        list.Reordered += (from, to) => moves.Add((from, to));

        Drag(platform, GripX(list), HeaderBounds(list, 0).CenterY, ItemBounds(list, 1).Bottom + 1);

        Assert.Equal([(0, 1)], moves);
        Assert.False(list.IsDragging);
    }

    [Fact]
    public void Dragging_to_the_top_moves_an_item_first()
    {
        var (list, _, platform) = Show();
        var moves = new List<(int, int)>();
        list.Reordered += (from, to) => moves.Add((from, to));

        Drag(platform, GripX(list), HeaderBounds(list, 2).CenterY, ItemBounds(list, 0).Y + 1);

        Assert.Equal([(2, 0)], moves);
    }

    [Fact]
    public void A_drop_beside_its_own_place_or_a_short_move_reports_nothing()
    {
        var (list, _, platform) = Show();
        int moves = 0;
        list.Reordered += (_, _) => moves++;

        Drag(platform, GripX(list), HeaderBounds(list, 1).CenterY, HeaderBounds(list, 1).CenterY + 2);
        Drag(platform, GripX(list), HeaderBounds(list, 1).CenterY, ItemBounds(list, 1).CenterY + 3);

        Assert.Equal(0, moves);
    }

    [Fact]
    public void Dragging_in_a_body_does_not_start_a_drag()
    {
        var (list, _, platform) = Show(bodies: true);
        int moves = 0;
        list.Reordered += (_, _) => moves++;
        Rect body = list.BodyOf(0)!.LayoutBounds;

        Drag(platform, body.X + 5, body.CenterY, ItemBounds(list, 2).Bottom - 1);

        Assert.Equal(0, moves);
        Assert.False(list.IsDragging);
    }

    [Fact]
    public void The_eye_flips_the_item_and_reports_its_new_state()
    {
        var (list, _, platform) = Show();
        var toggles = new List<(int, bool)>();
        list.Toggled += (index, on) => toggles.Add((index, on));
        Rect eye = Header(list, 1).Children[0].LayoutBounds;

        platform.Click(eye.CenterX, eye.CenterY);
        Assert.False(list.IsOn(1));
        platform.Click(eye.CenterX, eye.CenterY);

        Assert.Equal([(1, false), (1, true)], toggles);
        Assert.True(list.IsOn(1));
    }

    [Fact]
    public void Setting_the_state_from_code_reports_nothing()
    {
        var (list, _, _) = Show();
        int toggles = 0;
        list.Toggled += (_, _) => toggles++;

        list.SetOn(0, false);

        Assert.False(list.IsOn(0));
        Assert.Equal(0, toggles);
    }

    [Fact]
    public void The_remove_button_reports_the_item_and_leaves_it_to_the_app()
    {
        var (list, _, platform) = Show();
        var removed = new List<int>();
        list.Removed += removed.Add;
        Rect remove = Header(list, 2).Children[2].LayoutBounds;

        platform.Click(remove.CenterX, remove.CenterY);

        Assert.Equal([2], removed);
        Assert.Equal(3, list.Count);
    }

    [Fact]
    public void Alt_arrows_on_a_focused_header_move_it_by_one_place_within_the_list()
    {
        var (list, _, platform) = Show();
        var moves = new List<(int, int)>();
        list.Reordered += (from, to) => moves.Add((from, to));
        Assert.True(Header(list, 0).Focus());

        platform.Key(Key.Up, KeyModifiers.Alt);
        platform.Key(Key.Down, KeyModifiers.Alt);

        Assert.Equal([(0, 1)], moves);
    }

    [Fact]
    public void Clearing_removes_every_item()
    {
        var (list, _, platform) = Show();

        list.ClearItems();
        list.AddItem("Only");
        platform.Render().Dispose();

        Assert.Equal(1, list.Count);
        Assert.Equal("Only", list.TitleOf(0));
    }
}
