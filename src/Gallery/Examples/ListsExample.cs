namespace ApexUI.Gallery.Examples;

/// ListView with selection, keyboard navigation and drag reordering; rows rename in place with EditableLabel.
/// TreeView with expand/collapse, multi-select and drag into folders.
/// ReorderList: items with their own fields, an eye, a fold chevron and a remove button, dragged by the grip; a header click folds.
/// FoldList: rows that fold open onto their fields, several at once, with Expand all, Collapse all and a row cap.
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
                tree.Status,
                new Separator { Margin = new Thickness(0, 4f) },
                new Label { Text = "ReorderList", Bold = true, Role = TextRole.Heading },
                new Label { Text = "Drag an item by its header (or Alt+Up/Down on a focused header). The eye switches it off, × removes it. Add brings more.", WordWrap = true },
                BuildSteps(),
                new Separator { Margin = new Thickness(0, 4f) },
                new Label { Text = "FoldList", Bold = true, Role = TextRole.Heading },
                new Label { Text = "Click a row to open it; any number can stay open. Expand all and Collapse all work on every row. Past six rows the rest wait behind Show all. The status shows the row under the pointer; Highlight draws a row as hovered from code.", WordWrap = true },
                BuildFolds()
            ).WithSpacing(12f),
            new Thickness(24f)));
    }

    private sealed class Step(string name, float amount)
    {
        public string Name { get; } = name;
        public float Amount { get; set; } = amount;
        public bool On { get; set; } = true;
        public bool Open { get; set; } = true;
    }

    private static Widget BuildSteps()
    {
        var steps = new List<Step> { new("Brightness", 10f), new("Contrast", 25f), new("Saturation", 50f) };
        var status = new Label { ColorSource = t => t.OnSurfaceMuted };
        var list = new ReorderList { Width = 280f, HAlign = HAlign.Left };

        void Describe()
        {
            var parts = new List<string>();
            foreach (Step step in steps) parts.Add(step.On ? $"{step.Name} {step.Amount:0}" : $"({step.Name})");
            status.Text = steps.Count == 0 ? "No steps" : string.Join(" → ", parts);
        }

        void Rebuild()
        {
            list.ClearItems();
            foreach (Step step in steps)
            {
                var amount = new NumberField("Amount", step.Amount).WithRange(0f, 100f).WithStep(0.5f)
                    .OnChange(v => { step.Amount = v; Describe(); });
                list.AddItem(step.Name, amount, step.On, step.Open);
            }

            Describe();
        }

        list.OnReorder((from, to) =>
            {
                Step moved = steps[from];
                steps.RemoveAt(from);
                steps.Insert(to, moved);
                Rebuild();
            })
            .OnToggle((index, on) => { steps[index].On = on; Describe(); })
            .OnRemove(index => { steps.RemoveAt(index); Rebuild(); })
            .OnOpenChange((index, open) => steps[index].Open = open);
        Rebuild();

        var add = new MenuButton("Add") { HAlign = HAlign.Left };
        foreach (string name in new[] { "Brightness", "Contrast", "Saturation", "Hue" })
            add.AddItem(name, () => { steps.Add(new Step(name, 0f)); Rebuild(); });
        return new Column(list, add, status).WithSpacing(8f);
    }

    private static Widget BuildFolds()
    {
        const int count = 8;
        string[] kinds = ["Sharp", "Rounded", "Mirrored"];
        var status = new Label { Text = "Pointer over no row", ColorSource = t => t.OnSurfaceMuted };
        var list = new FoldList($"Points ({count})") { Width = 280f, HAlign = HAlign.Left, MaxVisibleRows = 6 };
        for (int i = 0; i < count; i++)
        {
            int index = i;
            string kind = kinds[i % kinds.Length];
            var field = new NumberField("Radius", 4f + i).WithRange(0f, 100f).WithStep(0.5f);
            field.OnChange(v => list.SetRowText(index, kind, Summary(kind, v)));
            list.AddRow($"Point {i + 1}", field, kind, Summary(kind, field.Value));
        }

        list.HoverChanged += index => status.Text = index < 0 ? "Pointer over no row" : $"Pointer over Point {index + 1}";
        var highlight = new NumberField("Highlight (0 = none)", 0f) { Width = 280f, HAlign = HAlign.Left }.WithRange(0f, count).WithStep(0.2f).WithFormat("0");
        highlight.OnChange(v => list.HighlightedRow = (int)MathF.Round(v) - 1);
        return new Column(list, status, highlight).WithSpacing(8f);

        static string Summary(string kind, float value) => kind == "Sharp" ? "–" : $"{value:0.##} px";
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
