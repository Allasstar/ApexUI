namespace ApexUI.Gallery.Examples;

/// Demonstrates SplitPanel and Section: a three-pane layout whose side panes keep their width
/// while the middle one grows, and collapsible sections in a scrolling inspector.
public class PanelsExample : Widget
{
    public PanelsExample()
    {
        var info = new Label { Text = "Drag a divider; double-click it to collapse or restore the pane.", Role = TextRole.Small };

        var left = Pane("Left pane: 200 wide, fixed, 120 to 400.");
        var center = Pane("Center pane: takes what the side panes leave, at least 160.");

        var inspector = new Scroll(new Column(
            new Section("Position", Fields("X", "Y")),
            new Section("Size", Fields("Width", "Height")),
            new Section("Appearance", Fields("Fill", "Stroke", "Opacity")),
            new Section("Advanced", Fields("Blend", "Mask"), expanded: false)
        ));

        var right = new SplitPanel(center, inspector)
            .WithFixedSide(SplitSide.Second)
            .WithSplitRange(160, 420)
            .WithSplitSize(240)
            .WithMinOtherSize(160);

        var split = new SplitPanel(left, right)
            .WithSplitRange(120, 400)
            .WithSplitSize(200)
            .WithMinOtherSize(320);
        split.Grow = 1f;
        split.SplitSizeChanged += s => info.Text = $"Left pane: {s:0} wide";
        split.CollapsedChanged += c => info.Text = c ? "Left pane collapsed" : "Left pane restored";

        AddChild(new Column(
            new Label { Text = "Split panels and sections", Bold = true, Role = TextRole.Heading },
            info,
            new Row(split) { Height = 420f, BackgroundSource = t => t.Surface, CornerRadius = 8f }
        ) { Spacing = 12f, Padding = new Thickness(24f) });
    }

    private static Widget Pane(string text) => new Label
    {
        Text     = text,
        WordWrap = true,
        Padding  = new Thickness(12f),
        VAlign   = VAlign.Top,
    };

    private static Widget Fields(params string[] names)
    {
        var column = new Column { Spacing = 6f, Padding = new Thickness(12f, 4f, 12f, 12f) };
        foreach (var name in names)
            column.Add(new Row(new Label { Text = name, Width = 70f }, new TextInput { Grow = 1f }).WithSpacing(8f));
        return column;
    }
}
