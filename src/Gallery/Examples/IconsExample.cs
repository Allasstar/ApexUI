namespace ApexUI.Gallery.Examples;

/// Demonstrates Icon, IconButton and ToggleGroup: vector icons from SVG path data, borderless
/// icon buttons with tooltips, and segmented single-choice groups with text or icons.
public class IconsExample : Widget
{
    private const string Pencil = "M4 20h4L19 9l-4-4L4 16z M13 7l4 4";
    private const string Eraser = "M8 20h12 M5 15l9-9 5 5-9 9H8z";
    private const string Hand   = "M7 11V6a2 2 0 0 1 4 0v5 M11 10V4a2 2 0 0 1 4 0v6 M15 10V6a2 2 0 0 1 4 0v8a7 7 0 0 1-7 7h-1a7 7 0 0 1-6-4l-2-4a2 2 0 0 1 3-2l2 3";
    private const string Undo   = "M9 14L4 9l5-5 M4 9h11a5 5 0 0 1 0 10h-3";
    private const string Redo   = "M15 14l5-5-5-5 M20 9H9a5 5 0 0 0 0 10h3";
    private const string Star   = "M12 2l3 7h7l-5.5 4.5L18.5 21 12 16.5 5.5 21l2-7.5L2 9h7z";

    private enum Tool { Draw, Erase, Pan }

    public IconsExample()
    {
        var status  = new Label { Text = "Pressed: nothing", Role = TextRole.Small };
        var toolLbl = new Label { Text = "Tool: Draw", Role = TextRole.Small };
        var sizeLbl = new Label { Text = "Size: Medium", Role = TextRole.Small };

        IconButton Pressable(string data, string name, bool enabled = true)
            => new(new Icon(data, IconStyle.Stroke), () => status.Text = $"Pressed: {name}") { IsEnabled = enabled };

        var tools = new ToggleGroup<Tool>()
            .AddOption(Tool.Draw,  new Icon(Pencil, IconStyle.Stroke), "Draw")
            .AddOption(Tool.Erase, new Icon(Eraser, IconStyle.Stroke), "Erase")
            .AddOption(Tool.Pan,   new Icon(Hand,   IconStyle.Stroke), "Pan")
            .WithSelected(Tool.Draw)
            .OnChange(t => toolLbl.Text = $"Tool: {t}");

        var sizes = new ToggleGroup<string>()
            .AddOption("Small", "Small")
            .AddOption("Medium", "Medium")
            .AddOption("Large", "Large")
            .WithSelected("Medium")
            .OnChange(s => sizeLbl.Text = $"Size: {s}");

        var vertical = new ToggleGroup<int>(Orientation.Vertical)
            .AddOption(1, "First")
            .AddOption(2, "Second")
            .AddOption(3, "Third")
            .WithSelected(2);
        vertical.HAlign = HAlign.Left;

        AddChild(new Column(
            new Label { Text = "Icons", Bold = true, Role = TextRole.Heading },
            new Label { Text = "Icons are SVG path data in a 24-unit box, filled or stroked, sized by the theme's IconSize.", WordWrap = true },
            new Row(
                new Icon(Star),
                new Icon(Star) { Size = 24f },
                new Icon(Star) { Size = 32f, ColorSource = t => t.Primary },
                new Icon(Pencil, IconStyle.Stroke) { Size = 32f },
                new Icon(Eraser, IconStyle.Stroke) { Size = 32f, ColorSource = t => t.Danger }
            ).WithSpacing(12f),

            new Label { Text = "Icon buttons", Bold = true, Role = TextRole.Subheading },
            new Row(
                new Tooltip(Pressable(Undo, "Undo"), "Undo"),
                new Tooltip(Pressable(Redo, "Redo"), "Redo"),
                new Tooltip(Pressable(Star, "Star (disabled)", enabled: false), "Disabled"),
                status
            ).WithSpacing(4f),

            new Label { Text = "Toggle groups", Bold = true, Role = TextRole.Subheading },
            new Row(tools, toolLbl).WithSpacing(12f),
            new Row(sizes, sizeLbl).WithSpacing(12f),
            vertical
        ) { Spacing = 12f, Padding = new Thickness(24f) });
    }
}
