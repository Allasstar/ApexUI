namespace ApexUI.Gallery.Examples;

/// Demonstrates ButtonGroup (a toolbar of grouped controls) and KeyHint (a status bar of key hints that
/// shows only the held modifier's hints, highlighted, through UiHost.ModifiersChanged). The Larger hints switch
/// sets the KeyHintFontSize, KeyHintText and KeyHintBorder tokens on the current theme.
public class ToolbarExample : Widget
{
    private const string Pointer = "M5 3l14 8-6 2-3 6z";
    private const string Hand    = "M7 11V6a2 2 0 0 1 4 0v5 M11 10V4a2 2 0 0 1 4 0v6 M15 10V6a2 2 0 0 1 4 0v8a7 7 0 0 1-7 7h-1a7 7 0 0 1-6-4l-2-4a2 2 0 0 1 3-2l2 3";
    private const string Square  = "M4 4h16v16H4z";
    private const string Circle  = "M12 3a9 9 0 1 0 0 18a9 9 0 1 0 0-18z";
    private const string Undo    = "M9 14L4 9l5-5 M4 9h11a5 5 0 0 1 0 10h-3";
    private const string Redo    = "M15 14l5-5-5-5 M20 9H9a5 5 0 0 0 0 10h3";
    private const string Minus   = "M6 12h12";
    private const string Plus    = "M6 12h12 M12 6v12";

    private static readonly (string Key, string Text, KeyHintKind Kind)[] Hints =
    [
        ("LMB", "Select", KeyHintKind.Mouse),
        ("Drag", "Marquee", KeyHintKind.Mouse),
        ("Shift", "Add", KeyHintKind.Key),
        ("Ctrl", "Deepest", KeyHintKind.Key),
        ("Alt", "Measure", KeyHintKind.Key),
    ];

    private readonly KeyHint[] _hints = new KeyHint[Hints.Length];
    private UiHost? _host;

    public ToolbarExample()
    {
        IconButton pointer = ToolButton(Pointer), hand = ToolButton(Hand), square = ToolButton(Square), circle = ToolButton(Circle);
        IconButton[] tools = [pointer, hand, square, circle];
        foreach (var tool in tools)
        {
            tool.OnPress(() =>
            {
                foreach (var other in tools) other.IsSelected = other == tool;
            });
        }
        pointer.IsSelected = true;

        int zoom = 100;
        var zoomLabel = new Label { Text = "100%", Role = TextRole.Small, MinWidth = 40f };
        void Zoom(int step)
        {
            zoom = Math.Clamp(zoom + step, 10, 400);
            zoomLabel.Text = $"{zoom}%";
        }

        var file = new MenuButton("File")
            .AddItem("Open…", () => { })
            .AddItem("Save", () => { })
            .AddSeparator()
            .AddItem("Export…", () => { });

        var toolbar = new Row(
            new ButtonGroup(pointer, hand),
            new ButtonGroup(square, circle),
            new Spacer(),
            new ButtonGroup(new IconButton(new Icon(Undo, IconStyle.Stroke)), new IconButton(new Icon(Redo, IconStyle.Stroke))),
            new ButtonGroup(
                new IconButton(new Icon(Minus, IconStyle.Stroke), () => Zoom(-10)),
                zoomLabel,
                new IconButton(new Icon(Plus, IconStyle.Stroke), () => Zoom(10))).WithChildrenCentered(),
            new ButtonGroup(file))
        {
            SpacingSource    = t => t.SpacingSm,
            Padding          = new Thickness(8f, 6f),
            BackgroundSource = t => t.Surface,
            CornerRadius     = 8f,
        };

        var statusBar = new Row { SpacingSource = t => t.SpacingMd, Padding = new Thickness(8f, 4f), BackgroundSource = t => t.Surface, CornerRadius = 8f };
        for (int i = 0; i < Hints.Length; i++)
        {
            _hints[i] = new KeyHint(Hints[i].Key, Hints[i].Text, Hints[i].Kind);
            statusBar.Add(_hints[i]);
        }

        var vertical = new ButtonGroup(Orientation.Vertical, ToolButton(Square), ToolButton(Circle)) { HAlign = HAlign.Left };

        AddChild(new Column(
            new Label { Text = "ButtonGroup: related controls on one rounded background", Bold = true },
            toolbar,
            new Label { Text = "Vertical ButtonGroup", Bold = true },
            vertical,
            new Label { Text = "KeyHint: hold Shift, Ctrl or Alt to see only that key's hint, highlighted", Bold = true },
            statusBar,
            new Toggle(false, "Larger hints: KeyHintFontSize 1.25 x small, KeyHintText OnSurface, KeyHintBorder OnSurfaceMuted").OnChange(UseLargerHints),
            new Row(
                new KeyHint("Enter", "Finish"),
                new KeyHint("Esc", "Cancel"),
                new KeyHint("RMB", "Cancel", KeyHintKind.Mouse),
                new KeyHint("Shift", "Highlighted") { IsHighlighted = true }).WithSpacing(16f)
        ) { Padding = new Thickness(16f), SpacingSource = t => t.SpacingMd });
    }

    protected override void OnAttached(UiHost host)
    {
        _host = host;
        host.ModifiersChanged += ShowHeld;
        ShowHeld(host.Modifiers);
    }

    protected override void OnDetached()
    {
        if (_host is not null) _host.ModifiersChanged -= ShowHeld;
        _host = null;
    }

    // Changing the theme preset afterwards brings back the preset's own tokens.
    private void UseLargerHints(bool on)
    {
        if (_host is null) return;
        Theme theme = _host.Theme;
        _host.Theme = on
            ? theme with { KeyHintFontSize = theme.FontSizeSmall * 1.25f, KeyHintText = theme.OnSurface, KeyHintBorder = theme.OnSurfaceMuted }
            : theme with { KeyHintFontSize = 0f, KeyHintText = SKColor.Empty, KeyHintBorder = SKColor.Empty };
    }

    private void ShowHeld(KeyModifiers held)
    {
        for (int i = 0; i < _hints.Length; i++)
        {
            KeyModifiers key = Hints[i].Key switch
            {
                "Shift" => KeyModifiers.Shift,
                "Ctrl"  => KeyModifiers.Control,
                "Alt"   => KeyModifiers.Alt,
                _       => KeyModifiers.None,
            };
            bool holding = (held & (KeyModifiers.Shift | KeyModifiers.Control | KeyModifiers.Alt)) != 0;
            _hints[i].IsVisible     = !holding || (key & held) != 0;
            _hints[i].IsHighlighted = holding && (key & held) != 0;
        }
    }

    private static IconButton ToolButton(string path) => new(new Icon(path, IconStyle.Stroke));
}
