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
                Show(host, e.WindowX, e.WindowY);
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
            menu.Show(host, e.WindowX, e.WindowY);
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
        var panel = new MenuList(minWidth: 180f, maxHeight: float.PositiveInfinity);

        foreach (var entry in _entries)
        {
            if (entry.IsSeparator)
            {
                panel.Add(new MenuSeparator());
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
