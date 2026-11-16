namespace ApexUI.Tests;

public class LifecycleTests
{
    private sealed class Probe : Widget
    {
        public readonly List<string> Events = [];
        public UiHost? HostSeenOnDetach;

        public Probe(params Widget[] children)
        {
            foreach (var c in children) AddChild(c);
        }

        public void Add(Widget child)    => AddChild(child);
        public void Remove(Widget child) => RemoveChild(child);

        protected override void OnAttached(UiHost host) => Events.Add("attached");
        protected override void OnDetached()
        {
            HostSeenOnDetach = Host;
            Events.Add("detached");
        }
    }

    [Fact]
    public void Attach_and_detach_reach_every_descendant()
    {
        var leaf  = new Probe();
        var panel = new Probe(new Probe(leaf));
        var (host, _) = TestPlatform.Show(new Probe(panel));

        Assert.Same(host, leaf.Host);
        Assert.Equal(["attached"], leaf.Events);

        ((Probe)panel.Parent!).Remove(panel);

        Assert.Null(leaf.Host);
        Assert.Same(host, leaf.HostSeenOnDetach);
        Assert.Equal(["attached", "detached"], leaf.Events);
    }

    [Fact]
    public void Adding_a_child_to_a_shown_tree_attaches_it()
    {
        var root  = new Probe();
        var child = new Probe();
        var (host, _) = TestPlatform.Show(root);

        root.Add(child);

        Assert.Same(host, child.Host);
        Assert.Equal(["attached"], child.Events);
    }

    [Fact]
    public void Moving_a_child_within_the_same_window_does_not_detach_it()
    {
        var a     = new Probe();
        var b     = new Probe();
        var child = new Probe();
        a.Add(child);
        TestPlatform.Show(new Probe(a, b));

        b.Add(child);

        Assert.Equal(["attached"], child.Events);
        Assert.Empty(a.Children);
    }

    [Fact]
    public void Replacing_the_root_detaches_the_old_tree()
    {
        var oldRoot = new Probe();
        var (host, _) = TestPlatform.Show(oldRoot);

        host.Root = new Probe();

        Assert.Null(oldRoot.Host);
        Assert.Equal(["attached", "detached"], oldRoot.Events);
    }

    [Fact]
    public void Removed_widget_is_no_longer_reachable_from_its_bindable()
    {
        var value  = new Bindable<float>(0.2f);
        var slider = new Slider().Bind(value);
        var root   = new Probe(slider);
        TestPlatform.Show(root);

        root.Remove(slider);
        value.Value = 0.8f;

        Assert.Equal(0.2f, slider.Value);
    }

    [Fact]
    public void Re_attached_widget_catches_up_with_its_bindable()
    {
        var value  = new Bindable<float>(0.2f);
        var slider = new Slider().Bind(value);
        var root   = new Probe(slider);
        TestPlatform.Show(root);
        root.Remove(slider);
        value.Value = 0.8f;

        root.Add(slider);
        Assert.Equal(0.8f, slider.Value);

        value.Value = 0.5f;
        Assert.Equal(0.5f, slider.Value);
    }

    [Fact]
    public void Binding_works_before_the_widget_is_shown()
    {
        var value  = new Bindable<bool>(false);
        var toggle = new Toggle().Bind(value);

        value.Value = true;

        Assert.True(toggle.IsChecked);
    }

    [Fact]
    public void Detaching_an_image_disposes_it_unless_kept()
    {
        using var bitmap = new SKBitmap(4, 4);
        var disposed = Image.FromBitmap(bitmap);
        var kept     = Image.FromBitmap(bitmap).KeepOnDetach();
        var root     = new Probe(disposed, kept);
        TestPlatform.Show(root);

        root.Remove(disposed);
        root.Remove(kept);

        Assert.True(disposed.IsDisposed);
        Assert.False(kept.IsDisposed);
    }

    [Fact]
    public void Detaching_a_hovered_widget_stops_routing_events_to_it()
    {
        var child = new FixedBox(100, 100);
        int moves = 0;
        child.PointerMoved += _ => moves++;
        var root = new Probe(child);
        var (_, platform) = TestPlatform.Show(root);
        platform.Move(10, 10);
        platform.Press(10, 10);

        root.Remove(child);
        platform.Move(20, 20);

        Assert.Equal(1, moves);
    }
}

public class VirtualListTests
{
    [Fact]
    public void Only_rows_near_the_viewport_are_children_and_they_are_attached()
    {
        var items = new ObservableList<int>(Enumerable.Range(0, 1000));
        var list  = new VirtualList<int>().WithItems(items).WithTemplate(i => new Label { Text = $"Row {i}" }).WithRowHeight(20);
        var (host, platform) = TestPlatform.Show(list, 300, 200);
        platform.Render().Dispose();

        Assert.InRange(list.Children.Count, 10, 12);
        foreach (var row in list.Children)
        {
            Assert.Same(list, row.Parent);
            Assert.Same(host, row.Host);
        }
    }

    [Fact]
    public void Creating_rows_during_layout_leaves_the_tree_clean()
    {
        var list = new VirtualList<int>()
            .WithItems(new ObservableList<int>(Enumerable.Range(0, 100)))
            .WithTemplate(i => new Label { Text = $"Row {i}" });
        list.Height = 150;
        var root = new Column(new Label { Text = "Header" }, list);
        var (_, platform) = TestPlatform.Show(root, 300, 400);

        platform.Render().Dispose();

        Assert.False(root.IsLayoutDirty);
    }

    [Fact]
    public void Clicks_reach_the_row_under_the_pointer()
    {
        var clicked = -1;
        var list = new VirtualList<int>()
            .WithItems(new ObservableList<int>(Enumerable.Range(0, 100)))
            .WithTemplate(i =>
            {
                var row = new FixedBox(10, 20);
                row.Click += _ => clicked = i;
                return row;
            })
            .WithRowHeight(20);
        var (_, platform) = TestPlatform.Show(list, 300, 200);
        platform.Render().Dispose();

        platform.Click(50, 65);

        Assert.Equal(3, clicked);
    }

    [Fact]
    public void Changing_the_source_detaches_the_old_rows()
    {
        var items = new ObservableList<int>(Enumerable.Range(0, 5));
        var list  = new VirtualList<int>().WithItems(items).WithTemplate(i => new Label { Text = $"Row {i}" });
        var (_, platform) = TestPlatform.Show(list, 300, 400);
        platform.Render().Dispose();
        var firstRow = list.Children[0];

        items.Add(5);

        Assert.Null(firstRow.Host);
        platform.Render().Dispose();
        Assert.Equal(6, list.Children.Count);
    }
}
