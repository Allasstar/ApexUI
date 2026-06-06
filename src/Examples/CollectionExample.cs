namespace ApexUI.App.Examples;

/// Demonstrates ObservableList<T>, VirtualList<T>, and live-bound Dropdown<T>.
public class CollectionExample : Widget
{
    public CollectionExample()
    {
        AddChild(new PaddingBox(
            new Column(

                // ── VirtualList — large static dataset ────────────────────────
                new Label { Text = "VirtualList — 2 000 rows", Bold = true, FontSize = 18f },

                MakeCard(new Column(
                    new Label { Text = "Only rows in the viewport are measured and drawn. Scroll to verify." },
                    BuildLargeList(),
                    new Label { Text = "2 000 items · ~10 rows rendered at a time", FontSize = 12f }
                ).WithSpacing(8f)),

                new Separator { Margin = new Thickness(0, 4f) },

                // ── ObservableList → Dropdown live binding ─────────────────────
                new Label { Text = "ObservableList — Live Dropdown", Bold = true, FontSize = 18f },

                MakeCard(BuildLiveDropdown()),

                new Separator { Margin = new Thickness(0, 4f) },

                // ── ObservableList → VirtualList live binding ──────────────────
                new Label { Text = "ObservableList — Live VirtualList", Bold = true, FontSize = 18f },

                MakeCard(BuildLiveList())

            ).WithSpacing(16f),
            new Thickness(24f)
        ));
    }

    // ── Large static list ─────────────────────────────────────────────────────

    private static Widget BuildLargeList()
    {
        var palette = new SKColor[]
        {
            SKColor.FromHex("#336BE6"), SKColor.FromHex("#1D9E75"),
            SKColor.FromHex("#E2874B"), SKColor.FromHex("#9C27B0"),
            SKColor.FromHex("#F44336"), SKColor.FromHex("#607D8B"),
        };

        var items = new List<(int Index, string Name, SKColor Color)>(2000);
        for (int i = 0; i < 2000; i++)
            items.Add((i, $"Item #{i + 1:D4}", palette[i % palette.Length]));

        return new VirtualList<(int Index, string Name, SKColor Color)> { Height = 300f }
            .WithItems(items)
            .WithRowHeight(38f)
            .WithTemplate(MakeLargeRow);
    }

    private static Widget MakeLargeRow((int Index, string Name, SKColor Color) item)
    {
        var badge = new PaddingBox(
            new Label
            {
                Text     = $"{item.Index + 1}",
                FontSize = 11f,
                Color    = SKColors.White,
                HAlign   = HAlign.Center,
                VAlign   = VAlign.Center,
            }, 4f)
        {
            Background   = item.Color,
            CornerRadius = 4f,
            Width        = 44f,
        };

        return new PaddingBox(
            new Row(badge, new Label { Text = item.Name, VAlign = VAlign.Center }).WithSpacing(10f),
            new Thickness(8f, 4f));
    }

    // ── Live dropdown ─────────────────────────────────────────────────────────

    private static Widget BuildLiveDropdown()
    {
        var fruits     = new ObservableList<string>(["Apple", "Banana", "Cherry", "Mango", "Peach"]);
        var selected   = new Bindable<string>("Apple");
        var countLbl   = new Label { Text = Count(fruits.Count) };
        var selLbl     = new Label { Text = Selected(selected.Value) };
        selected.Changed += v => selLbl.Text = Selected(v);

        int newIdx = 1;

        var dropdown = new Dropdown<string>()
            .WithItems(fruits)
            .WithPlaceholder("Pick a fruit…")
            .Bind(selected);

        return new Column(
            new Label { Text = "Dropdown bound to ObservableList — items update live:" },
            new Row(dropdown, new Spacer(), countLbl).WithSpacing(8f),
            selLbl,
            new Row(
                new Button("+ Add item").OnPress(() =>
                {
                    fruits.Add($"Fruit #{newIdx++}");
                    countLbl.Text = Count(fruits.Count);
                }),
                new Button("- Remove last").WithVariant(ButtonVariant.Secondary).OnPress(() =>
                {
                    if (fruits.Count > 0)
                    {
                        fruits.RemoveAt(fruits.Count - 1);
                        countLbl.Text = Count(fruits.Count);
                    }
                })
            ).WithSpacing(8f)
        ).WithSpacing(10f);
    }

    // ── Live virtual list ─────────────────────────────────────────────────────

    private static Widget BuildLiveList()
    {
        var items    = new ObservableList<string>(["First item", "Second item", "Third item"]);
        var input    = new TextInput("New item text… (Enter to add)");
        var countLbl = new Label { Text = Count(items.Count) };
        items.Changed += () => countLbl.Text = Count(items.Count);

        void AddItem()
        {
            if (string.IsNullOrWhiteSpace(input.Value)) return;
            items.Add(input.Value.Trim());
            input.WithValue("");
        }

        var list = new VirtualList<string> { Height = 220f }
            .WithItems(items)
            .WithRowHeight(36f)
            .WithTemplate(MakeLiveRow);

        return new Column(
            new Label { Text = "VirtualList reacts to ObservableList.Changed — add/remove and view updates immediately:" },
            input.WithSubmit(_ => AddItem()),
            new Row(
                new Button("Add").OnPress(AddItem),
                new Button("Remove last").WithVariant(ButtonVariant.Secondary).OnPress(() =>
                {
                    if (items.Count > 0) items.RemoveAt(items.Count - 1);
                }),
                new Button("Clear all").WithVariant(ButtonVariant.Ghost).OnPress(() => items.Clear()),
                new Spacer(),
                countLbl
            ).WithSpacing(8f),
            list
        ).WithSpacing(10f);
    }

    private static Widget MakeLiveRow(string item, int idx)
    {
        var bg = (Func<Theme, SKColor>)(idx % 2 == 0 ? t => t.Background : t => t.SurfaceHover);
        return new PaddingBox(
            new Row(
                new Label { Text = item, VAlign = VAlign.Center },
                new Spacer(),
                new Label { Text = $"#{idx + 1}", FontSize = 11f, VAlign = VAlign.Center }
            ).WithSpacing(8f),
            new Thickness(12f, 4f))
        {
            BackgroundSource = bg,
        };
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static string Count(int n)       => $"{n} item{(n == 1 ? "" : "s")}";
    private static string Selected(string? v) => $"Selected: {v ?? "—"}";

    private static Widget MakeCard(Widget content)
        => new PaddingBox(content, 16f)
        {
            BackgroundSource = t => t.Surface,
            CornerRadius     = 8f,
        };
}
