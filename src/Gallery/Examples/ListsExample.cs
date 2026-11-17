namespace ApexUI.Gallery.Examples;

/// ListView with selection, keyboard navigation and drag reordering; rows rename in place with EditableLabel.
/// TreeView with expand/collapse, multi-select and drag into folders.
public class ListsExample : Widget
{
    public ListsExample()
    {
        var planets = new ObservableList<string>(["Mercury", "Venus", "Earth", "Mars", "Jupiter", "Saturn", "Uranus", "Neptune"]);
        var status = new Label { Text = "Nothing selected", ColorSource = t => t.OnSurfaceMuted };

        ListView<string>? list = null;
        list = new ListView<string>()
            .WithReorder()
            .WithItems(planets)
            .WithRowHeight(32f)
            .WithTemplate((name, index) =>
            {
                var label = new EditableLabel(name) { Grow = 1f, VAlign = VAlign.Center };
                label.TextCommitted += text => planets.Set(index, text);
                return new PaddingBox(new Row(new Label { Text = $"{index + 1}.", Width = 24f, VAlign = VAlign.Center }, label), new Thickness(8f, 0f));
            })
            .OnSelectionChange(i => status.Text = i < 0 ? "Nothing selected" : $"Selected: {planets[i]}")
            .OnReorder((from, to) =>
            {
                string moved = planets[from];
                planets.RemoveAt(from);
                planets.Insert(to, moved);
                list!.SelectedIndex = to;
            });
        list.Height = 300f;
        list.Width = 280f;
        list.HAlign = HAlign.Left;

        var big = new ObservableList<string>();
        for (int i = 1; i <= 2000; i++) big.Add($"Row {i}");
        var bigList = new ListView<string>()
            .WithItems(big)
            .WithRowHeight(28f)
            .WithTemplate(s => new PaddingBox(new Label { Text = s, VAlign = VAlign.Center }, new Thickness(8f, 0f)));
        bigList.Height = 200f;
        bigList.Width = 280f;
        bigList.HAlign = HAlign.Left;

        var tree = BuildTree();

        AddChild(new PaddingBox(
            new Column(
                new Label { Text = "ListView", Bold = true, Role = TextRole.Heading },
                new Label { Text = "Click or use the arrow keys to select. Drag a row to move it. Double-click a name to rename it.", WordWrap = true },
                list,
                status,
                new Separator { Margin = new Thickness(0, 4f) },
                new Label { Text = "2,000 rows, only the visible ones are built", Bold = true },
                bigList,
                new Separator { Margin = new Thickness(0, 4f) },
                new Label { Text = "TreeView", Bold = true, Role = TextRole.Heading },
                new Label { Text = "Click the arrows to expand. Ctrl+click or Shift+click selects several rows. Drag rows between others (move sideways to pick the level) or onto a folder. Files cannot hold anything.", WordWrap = true },
                tree.Tree,
                tree.Status
            ).WithSpacing(12f),
            new Thickness(24f)));
    }

    private sealed class Entry(string name, bool folder, params Entry[] children)
    {
        public string Name { get; } = name;
        public bool IsFolder { get; } = folder;
        public List<Entry> Children { get; } = [.. children];
    }

    private static (TreeView<Entry> Tree, Label Status) BuildTree()
    {
        var roots = new List<Entry>
        {
            new("src", true, new Entry("App.cs", false), new Entry("Ui", true, new Entry("Panel.cs", false), new Entry("Row.cs", false))),
            new("tests", true, new Entry("AppTests.cs", false)),
            new("README.md", false),
        };
        var status = new Label { Text = "Nothing selected", ColorSource = t => t.OnSurfaceMuted };
        var selection = new List<Entry>();

        TreeView<Entry>? tree = null;
        tree = new TreeView<Entry>(e => e.Children)
            .WithExpandByDefault()
            .WithRoots(roots)
            .WithRowHeight(28f)
            .WithTemplate(e => new Label { Text = e.IsFolder ? e.Name + "/" : e.Name, VAlign = VAlign.Center, Bold = e.IsFolder })
            .WithDrag()
            .OnSelectionChange(() =>
            {
                tree!.GetSelection(selection);
                status.Text = selection.Count == 0 ? "Nothing selected" : $"Selected: {string.Join(", ", selection.Select(e => e.Name))}";
            })
            .OnDrop((items, target) =>
            {
                List<Entry> into = target.Parent?.Children ?? roots;
                int index = target.Index;
                foreach (Entry item in items)
                {
                    List<Entry> from = ParentList(roots, item)!;
                    if (from == into && from.IndexOf(item) < index) index--;
                    from.Remove(item);
                }

                into.InsertRange(index, items);
                tree!.Refresh();
            });
        tree.CanDrop = (_, target) => target.Parent is null || target.Parent.IsFolder;
        tree.Height = 240f;
        tree.Width = 280f;
        tree.HAlign = HAlign.Left;
        return (tree, status);
    }

    private static List<Entry>? ParentList(List<Entry> list, Entry item)
    {
        if (list.Contains(item)) return list;
        foreach (Entry entry in list)
        {
            if (ParentList(entry.Children, item) is { } found) return found;
        }

        return null;
    }
}
