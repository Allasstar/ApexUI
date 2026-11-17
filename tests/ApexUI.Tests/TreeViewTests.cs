namespace ApexUI.Tests;

public class TreeViewTests
{
    private const float Row = 20;

    private sealed class Node(string name, params Node[] children)
    {
        public string Name { get; } = name;
        public List<Node> Children { get; } = [.. children];
        public override string ToString() => Name;
    }

    private sealed class NameComparer : IEqualityComparer<Node>
    {
        public bool Equals(Node? x, Node? y) => x?.Name == y?.Name;
        public int GetHashCode(Node obj) => obj.Name.GetHashCode();
    }

    // A(a1, a2), B, C(c1)
    private static List<Node> Sample() =>
    [
        new Node("A", new Node("a1"), new Node("a2")),
        new Node("B"),
        new Node("C", new Node("c1")),
    ];

    private static (TreeView<Node> Tree, List<Node> Roots, UiHost Host, TestPlatform Platform) Show(
        bool expanded = true, bool reverse = false, IEqualityComparer<Node>? comparer = null)
    {
        List<Node> roots = Sample();
        var tree = new TreeView<Node>(n => n.Children, comparer)
            .WithExpandByDefault(expanded)
            .WithReverseChildren(reverse)
            .WithRoots(roots)
            .WithTemplate(n => new Label { Text = n.Name })
            .WithRowHeight(Row)
            .WithDrag();
        var (host, platform) = TestPlatform.Show(tree, 300, 200);
        platform.Render().Dispose();
        return (tree, roots, host, platform);
    }

    private static float RowCenter(int index) => index * Row + Row / 2;

    private static string Names(IReadOnlyList<Node> nodes) => string.Join(",", nodes.Select(n => n.Name));

    private static void Drag(TestPlatform platform, float fromY, float toX, float toY)
    {
        const float x = 100;
        platform.Move(x, fromY);
        platform.Press(x, fromY);
        for (float y = fromY; Math.Abs(y - toY) > 0.5f; y += Math.Sign(toY - y) * Math.Min(5f, Math.Abs(toY - y)))
            platform.Move(x, y);
        platform.Move(toX, toY);
        platform.Release(toX, toY);
    }

    [Fact]
    public void Collapsed_items_hide_their_children()
    {
        var (tree, _, _, _) = Show(expanded: false);

        Assert.Equal("A,B,C", Names(tree.VisibleItems));
    }

    [Fact]
    public void Expanded_items_list_their_children_below_them_in_order()
    {
        var (tree, _, _, _) = Show();

        Assert.Equal("A,a1,a2,B,C,c1", Names(tree.VisibleItems));
    }

    [Fact]
    public void Reversed_children_list_the_last_item_first_at_every_level()
    {
        var (tree, _, _, _) = Show(reverse: true);

        Assert.Equal("C,c1,B,A,a2,a1", Names(tree.VisibleItems));
    }

    [Fact]
    public void Pressing_the_arrow_toggles_the_item_without_selecting_it()
    {
        var (tree, roots, _, platform) = Show();

        platform.Click(8, RowCenter(0));

        Assert.False(tree.IsExpanded(roots[0]));
        Assert.Equal("A,B,C,c1", Names(tree.VisibleItems));
        Assert.Equal(0, tree.SelectionCount);
    }

    [Fact]
    public void Click_selects_one_row_ctrl_toggles_and_shift_selects_a_range()
    {
        var (tree, roots, _, platform) = Show();
        int changes = 0;
        tree.SelectionChanged += () => changes++;
        var selection = new List<Node>();

        platform.Click(100, RowCenter(1));
        platform.Click(100, RowCenter(3), modifiers: KeyGesture.PrimaryModifier);
        tree.GetSelection(selection);
        Assert.Equal("a1,B", Names(selection));

        platform.Click(100, RowCenter(5), modifiers: KeyModifiers.Shift);
        tree.GetSelection(selection);
        Assert.Equal("B,C,c1", Names(selection));

        platform.Click(100, RowCenter(1), modifiers: KeyGesture.PrimaryModifier);
        platform.Click(100, RowCenter(1), modifiers: KeyGesture.PrimaryModifier);
        tree.GetSelection(selection);
        Assert.Equal("B,C,c1", Names(selection));
        Assert.Equal(5, changes);
        Assert.True(tree.IsSelected(roots[1]));
    }

    [Fact]
    public void Clicking_one_row_of_a_multiple_selection_narrows_it_on_release()
    {
        var (tree, _, _, platform) = Show();
        var selection = new List<Node>();
        platform.Click(100, RowCenter(1));
        platform.Click(100, RowCenter(3), modifiers: KeyModifiers.Shift);

        platform.Press(100, RowCenter(2));
        tree.GetSelection(selection);
        Assert.Equal("a1,a2,B", Names(selection));

        platform.Release(100, RowCenter(2));
        tree.GetSelection(selection);
        Assert.Equal("a2", Names(selection));
    }

    [Fact]
    public void Left_collapses_or_goes_to_the_parent_and_right_expands_or_goes_to_the_first_child()
    {
        var (tree, roots, _, platform) = Show();
        var selection = new List<Node>();
        platform.Click(100, RowCenter(2));

        platform.Key(Key.Left);
        tree.GetSelection(selection);
        Assert.Equal("A", Names(selection));

        platform.Key(Key.Left);
        Assert.False(tree.IsExpanded(roots[0]));

        platform.Key(Key.Right);
        Assert.True(tree.IsExpanded(roots[0]));

        platform.Key(Key.Right);
        tree.GetSelection(selection);
        Assert.Equal("a1", Names(selection));
    }

    [Theory]
    [InlineData(5f, null, 1)]     // left: a root, above B
    [InlineData(37f, "A", 2)]     // one level in: the last child of A
    public void The_pointer_x_picks_the_depth_of_a_drop_between_rows(float x, string? parent, int index)
    {
        var (tree, _, _, platform) = Show();
        var drops = new List<(string Items, TreeDrop<Node> Target)>();
        tree.Dropped += (items, target) => drops.Add((Names(items), target));

        Drag(platform, RowCenter(4), x, 3 * Row + 1);

        var (items, target) = Assert.Single(drops);
        Assert.Equal("C", items);
        Assert.Equal(parent, target.Parent?.Name);
        Assert.Equal(index, target.Index);
        Assert.False(target.Into);
        Assert.False(tree.IsDragging);
    }

    [Fact]
    public void Dropping_on_the_middle_of_a_row_puts_the_items_inside_it_at_the_visual_top()
    {
        var (tree, _, _, platform) = Show(reverse: true);
        var drops = new List<TreeDrop<Node>>();
        tree.Dropped += (_, target) => drops.Add(target);

        // Reversed: C, c1, B, A, a2, a1. Drag B onto A.
        Drag(platform, RowCenter(2), 100, RowCenter(3));

        TreeDrop<Node> drop = Assert.Single(drops);
        Assert.Equal("A", drop.Parent?.Name);
        Assert.Equal(2, drop.Index);
        Assert.True(drop.Into);
    }

    [Fact]
    public void A_refused_drop_into_a_row_falls_back_to_the_gap()
    {
        var (tree, _, _, platform) = Show();
        tree.CanDrop = (_, target) => !target.Into;
        var drops = new List<TreeDrop<Node>>();
        tree.Dropped += (_, target) => drops.Add(target);

        Drag(platform, RowCenter(3), 5, RowCenter(4) - 1);

        TreeDrop<Node> drop = Assert.Single(drops);
        Assert.False(drop.Into);
        Assert.Null(drop.Parent);
    }

    [Fact]
    public void Refused_drops_raise_nothing()
    {
        var (tree, _, _, platform) = Show();
        tree.CanDrop = (_, _) => false;
        int drops = 0;
        tree.Dropped += (_, _) => drops++;

        Drag(platform, RowCenter(4), 5, 1);

        Assert.Equal(0, drops);
    }

    [Fact]
    public void An_item_cannot_be_dropped_into_itself_or_its_descendants()
    {
        var (tree, _, _, platform) = Show();
        int drops = 0;
        tree.Dropped += (_, _) => drops++;

        Drag(platform, RowCenter(0), 100, RowCenter(1));
        Drag(platform, RowCenter(0), 40, 2 * Row + 1);

        Assert.Equal(0, drops);
    }

    [Fact]
    public void A_selected_child_of_a_dragged_item_moves_with_it_and_is_not_listed()
    {
        var (tree, _, _, platform) = Show();
        var drops = new List<string>();
        tree.Dropped += (items, _) => drops.Add(Names(items));
        platform.Click(100, RowCenter(0));
        platform.Click(100, RowCenter(1), modifiers: KeyGesture.PrimaryModifier);
        platform.Click(100, RowCenter(3), modifiers: KeyGesture.PrimaryModifier);

        Drag(platform, RowCenter(0), 5, 6 * Row - 1);

        Assert.Equal(["A,B"], drops);
    }

    [Fact]
    public void Refresh_without_changes_keeps_the_rows()
    {
        List<Node> roots = Sample();
        int built = 0;
        var tree = new TreeView<Node>(n => n.Children)
            .WithExpandByDefault()
            .WithRoots(roots)
            .WithTemplate(n => { built++; return new Label { Text = n.Name }; })
            .WithRowHeight(Row);
        var (_, platform) = TestPlatform.Show(tree, 300, 200);
        platform.Render().Dispose();
        int first = built;

        tree.Refresh();
        platform.Render().Dispose();
        Assert.Equal(first, built);

        roots[1].Children.Add(new Node("b1"));
        tree.Refresh();
        platform.Render().Dispose();
        Assert.Equal("A,a1,a2,B,b1,C,c1", Names(tree.VisibleItems));
        Assert.True(built > first);
    }

    [Fact]
    public void A_comparer_keeps_selection_and_expansion_for_equal_copies()
    {
        var (tree, roots, _, platform) = Show(comparer: new NameComparer());
        platform.Click(100, RowCenter(3));
        tree.SetExpanded(roots[0], false);

        List<Node> copies = Sample();
        roots.Clear();
        roots.AddRange(copies);
        tree.Refresh();

        Assert.True(tree.IsSelected(copies[1]));
        Assert.False(tree.IsExpanded(copies[0]));
        Assert.Equal("A,B,C,c1", Names(tree.VisibleItems));
    }

    [Fact]
    public void Set_selection_reports_only_real_changes()
    {
        var (tree, roots, _, _) = Show();
        int changes = 0;
        tree.SelectionChanged += () => changes++;

        tree.SetSelection([roots[0], roots[1]]);
        tree.SetSelection([roots[1], roots[0]]);
        tree.ClearSelection();
        tree.ClearSelection();

        Assert.Equal(2, changes);
    }
}
