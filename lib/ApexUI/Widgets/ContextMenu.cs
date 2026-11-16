namespace ApexUI.Widgets;

/// Right-click overlay with labeled action items, separators, headers, and toggle items.
/// Dismisses on click-outside. Attach to any widget: ContextMenu.Attach(widget, menu)
///
/// Standard behavior:
///   • Right mouse UP on target → menu appears at cursor
///   • Left-click item → action fires, menu closes
///   • Left press outside → menu closes
///   • Right press outside while open → menu moves to new cursor position
///
/// Example:
///   var darkMode = new Bindable<bool>(false);
///   var menu = new ContextMenu()
///       .AddHeader("View")
///       .AddCheckItem("Dark Mode", darkMode)
///       .AddSeparator()
///       .AddItem("Refresh", () => Refresh());
///   ContextMenu.Attach(panel, menu);
public sealed class ContextMenu
{
    private readonly List<MenuEntry> _entries = [];
    private readonly Overlay _overlay;
    private bool _built;

    public ContextMenu()
    {
        // DismissOnClickOutside is handled manually so we get right-click reposition.
        _overlay = new Overlay { DismissOnClickOutside = false };
        _overlay.PointerPressed += e =>
        {
            if (e.Source != _overlay) return;
            e.Handled = true;
            if (e.Button == PointerButton.Right && _overlay.Host is { } host)
                Show(host, e.X, e.Y);
            else
                Close();
        };
    }

    // ── Fluent builder ────────────────────────────────────────────────────────

    public ContextMenu AddItem(string label, Action action, bool enabled = true)
    {
        _entries.Add(new MenuEntry(label, action, enabled));
        return this;
    }

    public ContextMenu AddCheckItem(string label, Bindable<bool> binding, bool closeOnClick = false)
    {
        _entries.Add(new MenuEntry(label, () => binding.Value = !binding.Value, true)
            { Binding = binding, CloseOnClick = closeOnClick });
        return this;
    }

    public ContextMenu AddSeparator()
    {
        _entries.Add(new MenuEntry("", () => { }, true) { IsSeparator = true });
        return this;
    }

    public ContextMenu AddHeader(string text)
    {
        _entries.Add(new MenuEntry(text, () => { }, false) { IsHeader = true });
        return this;
    }

    // ── Attachment ────────────────────────────────────────────────────────────

    /// Shows menu on a right-button release over target or any of its descendants,
    /// including ones added later (the release bubbles up to target).
    public static void Attach(Widget target, ContextMenu menu)
    {
        target.PointerReleased += e =>
        {
            if (e.Button != PointerButton.Right || target.Host is not { } host) return;
            e.Handled = true;
            menu.Show(host, e.X, e.Y);
        };
    }

    // ── Show / Close ──────────────────────────────────────────────────────────

    /// Shows the menu at (x, y) in host's window, in logical units.
    public void Show(UiHost host, float x, float y)
    {
        if (!_built) BuildPanel();
        _overlay.ContentHAlign = HAlign.Left;
        _overlay.ContentVAlign = VAlign.Top;
        _overlay.PositionX     = x;
        _overlay.PositionY     = y;
        _overlay.Anchor        = null;
        _overlay.Open(host);
    }

    public void Close() => _overlay.Close();

    // ── Panel construction (lazy — built once on first Show) ──────────────────

    private void BuildPanel()
    {
        _built = true;
        var panel = new ContextPanel(minWidth: 180f);

        foreach (var entry in _entries)
        {
            if (entry.IsSeparator)
            {
                panel.Add(new Separator { Margin = new Thickness(4f, 2f) });
                continue;
            }
            if (entry.IsHeader)
            {
                panel.Add(new MenuHeaderWidget(entry.Label));
                continue;
            }
            if (entry.Binding is not null)
            {
                var binding      = entry.Binding;
                var closeOnClick = entry.CloseOnClick;
                MenuItemWidget? item = null;
                item = new MenuItemWidget(entry.Label, isChecked: () => binding.Value, onActivate: () =>
                {
                    binding.Value = !binding.Value;
                    item!.Invalidate();
                    if (closeOnClick) Close();
                });
                panel.Add(item);
            }
            else
            {
                var action  = entry.Action;
                var enabled = entry.Enabled;
                var item    = new MenuItemWidget(entry.Label, enabled: enabled, onActivate: () =>
                {
                    Close();
                    if (enabled) action();
                });
                panel.Add(item);
            }
        }

        _overlay.Content = panel;
    }

    // ── Panel widget ──────────────────────────────────────────────────────────

    // Flat panel: Surface background + rounded border + Column of items.
    // No Scroll or PaddingBox wrappers — padding is managed directly so
    // layout is straightforward and the background renders reliably.
    private sealed class ContextPanel : Widget
    {
        private readonly Column _column;
        private readonly float  _minWidth;
        private const float     Pad = 4f;

        public ContextPanel(float minWidth = 180f)
        {
            _minWidth        = minWidth;
            BackgroundSource = t => t.Surface;
            CornerRadius     = 8f;
            _column          = new Column { Spacing = 2f };
            AddChild(_column);
        }

        public void Add(Widget item) => _column.Add(item);

        protected override Size MeasureCore(Size available)
        {
            // Measure column at minWidth so items report a stable preferred width.
            float innerW = Math.Max(0f, _minWidth - Pad * 2f);
            _column.Measure(new Size(innerW, float.PositiveInfinity));
            float w = Math.Max(_column.DesiredSize.Width + Pad * 2f, _minWidth);
            float h = _column.DesiredSize.Height + Pad * 2f;
            return new Size(w, h);
        }

        protected override void ArrangeCore(Rect r)
        {
            float innerW = r.Width  - Pad * 2f;
            float innerH = r.Height - Pad * 2f;
            // Re-measure so DesiredSize reflects the final width before arrange.
            _column.Measure(new Size(innerW, float.PositiveInfinity));
            _column.Arrange(new Rect(r.X + Pad, r.Y + Pad, innerW, Math.Max(innerH, _column.DesiredSize.Height)));
        }

        protected override void DrawCore(DrawContext ctx)
            => ctx.StrokeRoundRect(LayoutBounds, CornerRadius, ctx.Theme.Border.WithAlpha(0.4f));
    }

    // ── Entry data ────────────────────────────────────────────────────────────

    private sealed class MenuEntry(string Label, Action Action, bool Enabled)
    {
        public string          Label        { get; } = Label;
        public Action          Action       { get; } = Action;
        public bool            Enabled      { get; } = Enabled;
        public bool            IsSeparator  { get; init; }
        public bool            IsHeader     { get; init; }
        public Bindable<bool>? Binding      { get; init; }
        public bool            CloseOnClick { get; init; }
    }
}
