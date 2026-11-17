namespace ApexUI.Gallery.Examples;

/// ListView with selection, keyboard navigation and drag reordering; rows rename in place with EditableLabel.
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

        AddChild(new PaddingBox(
            new Column(
                new Label { Text = "ListView", Bold = true, Role = TextRole.Heading },
                new Label { Text = "Click or use the arrow keys to select. Drag a row to move it. Double-click a name to rename it.", WordWrap = true },
                list,
                status,
                new Separator { Margin = new Thickness(0, 4f) },
                new Label { Text = "2,000 rows, only the visible ones are built", Bold = true },
                bigList
            ).WithSpacing(12f),
            new Thickness(24f)));
    }
}
