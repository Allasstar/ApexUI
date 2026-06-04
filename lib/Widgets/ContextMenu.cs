namespace ApexUI.Widgets;

/// Right-click Overlay with a list of labeled actions. Dismisses on click-outside.
/// Attach to any widget: ContextMenu.Attach(widget, menu)
///
/// Example:
///   var menu = new ContextMenu()
///       .AddItem("Copy",  () => Copy())
///       .AddItem("Paste", () => Paste())
///       .AddSeparator()
///       .AddItem("Delete", () => Delete(), enabled: false);
///   ContextMenu.Attach(myWidget, menu);
public sealed class ContextMenu
{
    private readonly List<MenuEntry> _entries = [];
    private readonly Overlay _overlay;
    private bool _built;

    public ContextMenu()
    {
        _overlay = new Overlay { DismissOnClickOutside = true };
    }

    // ── Fluent builder ────────────────────────────────────────────────────────

    public ContextMenu AddItem(string label, Action action, bool enabled = true)
    {
        _entries.Add(new MenuEntry(label, action, enabled, IsSeparator: false, IsHeader: false));
        return this;
    }

    public ContextMenu AddSeparator()
    {
        _entries.Add(new MenuEntry("", () => { }, true, IsSeparator: true, IsHeader: false));
        return this;
    }

    public ContextMenu AddHeader(string text)
    {
        _entries.Add(new MenuEntry(text, () => { }, false, IsSeparator: false, IsHeader: true));
        return this;
    }

    // ── Attachment ────────────────────────────────────────────────────────────

    /// Hook into target (and all its current descendants) to show the menu on right-click.
    /// Attach after the widget tree under target is fully built.
    public static void Attach(Widget target, ContextMenu menu)
        => AttachRecursive(target, menu);

    private static void AttachRecursive(Widget w, ContextMenu menu)
    {
        var prev = w.OnPointerDown;
        w.OnPointerDown = e =>
        {
            prev?.Invoke(e);
            if (e.Button == PointerButton.Right)
                menu.Show(e.X, e.Y);
        };
        foreach (var child in w.Children)
            AttachRecursive(child, menu);
    }

    // ── Show / Close ──────────────────────────────────────────────────────────

    public void Show(float x, float y)
    {
        if (!_built) BuildPanel();
        _overlay.PositionX    = x;
        _overlay.PositionY    = y;
        _overlay.ContentHAlign = HAlign.Left;
        _overlay.ContentVAlign = VAlign.Top;
        _overlay.Anchor        = null;
        _overlay.Open();
    }

    public void Close() => _overlay.Close();

    // ── Panel construction ────────────────────────────────────────────────────

    private void BuildPanel()
    {
        _built = true;

        var items = _entries.Select(e => BuildEntryWidget(e)).ToArray();

        _overlay.Content = new PaddingBox(new Column(items).WithSpacing(2f), new Thickness(4f))
        {
            BackgroundSource = t => t.Surface,
            CornerRadius     = 8f,
            MinWidth         = 160f,
        };
    }

    private Widget BuildEntryWidget(MenuEntry entry)
    {
        if (entry.IsSeparator)
            return new Separator { Margin = new Thickness(4f, 2f) };

        if (entry.IsHeader)
        {
            var headerLabel = new Label
            {
                Text = entry.Label,
                FontSize = 11f,
                Bold = true,
                IsHitTestVisible = false,
            };
            return new PaddingBox(headerLabel, new Thickness(12f, 4f, 12f, 2f))
            {
                IsHitTestVisible = false,
            };
        }

        var label = entry.Label;
        var action = entry.Action;
        var enabled = entry.Enabled;

        return new ContextMenuItem(label, enabled, () =>
        {
            _overlay.Close();
            if (enabled) action();
        });
    }

    // ── Private types ─────────────────────────────────────────────────────────

    private record MenuEntry(
        string Label, Action Action, bool Enabled,
        bool IsSeparator, bool IsHeader);

    private sealed class ContextMenuItem : Widget
    {
        private readonly Label _label;
        private readonly bool _enabled;

        public ContextMenuItem(string text, bool enabled, Action onClick)
        {
            _enabled = enabled;
            _label = new Label { Text = text, IsHitTestVisible = false };
            Padding = new Thickness(12f, 6f);
            CornerRadius = 6f;
            IsEnabled = enabled;
            OnClick = _ => onClick();
        }

        protected override Size MeasureCore(Size available)
        {
            _label.Measure(new Size(available.Width - Padding.Horizontal,
                available.Height - Padding.Vertical));
            return new Size(
                _label.DesiredSize.Width + Padding.Horizontal,
                _label.DesiredSize.Height + Padding.Vertical);
        }

        protected override void ArrangeCore(Rect r)
            => _label.Arrange(r.Deflate(Padding));

        protected override void DrawCore(DrawContext ctx)
        {
            var t = ctx.Theme;
            var bg = IsPressed ? t.SurfacePressed
                : IsHovered   ? t.SurfaceHover
                : SKColor.Empty;

            if (bg != SKColor.Empty)
            {
                using var p = ctx.MakePaint(bg);
                ctx.Canvas.DrawRoundRect(LayoutBounds.ToSKRect(), CornerRadius, CornerRadius, p);
            }

            _label.Color = _enabled ? t.OnSurface : t.OnSurfaceMuted;
        }
    }
}
