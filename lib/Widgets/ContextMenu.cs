namespace ApexUI.Widgets;

/// Right-click Overlay with labeled action items, separators, headers, and toggle items.
/// Dismisses on click-outside. Attach to any widget: ContextMenu.Attach(widget, menu)
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
        _overlay = new Overlay { DismissOnClickOutside = true };
    }

    // ── Fluent builder ────────────────────────────────────────────────────────

    /// Standard action item — closes the menu and calls action on click.
    public ContextMenu AddItem(string label, Action action, bool enabled = true)
    {
        _entries.Add(new MenuEntry(label, action, enabled));
        return this;
    }

    /// Toggle item — shows ✓ when binding is true; click flips the value.
    /// By default the menu stays open so the user can toggle multiple items.
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

    /// Hook right-click on target and all its current descendants.
    /// Call after the widget tree under target is fully built.
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
        _overlay.ContentHAlign = HAlign.Left;
        _overlay.ContentVAlign = VAlign.Top;
        _overlay.PositionX     = x;
        _overlay.PositionY     = y;
        _overlay.Anchor        = null;
        _overlay.Open();
    }

    public void Close() => _overlay.Close();

    // ── Panel construction (lazy — called once on first Show) ─────────────────

    private void BuildPanel()
    {
        _built = true;
        var list = new MenuList(minWidth: 180f, maxHeight: 320f);

        foreach (var entry in _entries)
        {
            if (entry.IsSeparator)
            {
                list.Add(new Separator { Margin = new Thickness(4f, 2f) });
                continue;
            }

            if (entry.IsHeader)
            {
                list.Add(new MenuHeaderWidget(entry.Label));
                continue;
            }

            if (entry.Binding is not null)
            {
                // Check (toggle) item
                var binding      = entry.Binding;
                var closeOnClick = entry.CloseOnClick;
                MenuItemWidget? item = null;
                item = new MenuItemWidget(entry.Label, isChecked: () => binding.Value);
                item.OnClick = _ =>
                {
                    binding.Value = !binding.Value;
                    item!.Invalidate();          // refresh checkmark immediately
                    if (closeOnClick) _overlay.Close();
                };
                list.Add(item);
            }
            else
            {
                // Standard action item
                var action  = entry.Action;
                var enabled = entry.Enabled;
                var item    = new MenuItemWidget(entry.Label, enabled);
                item.OnClick = _ =>
                {
                    _overlay.Close();
                    if (enabled) action();
                };
                list.Add(item);
            }
        }

        _overlay.Content = list;
    }

    // ── Entry data ────────────────────────────────────────────────────────────

    private sealed class MenuEntry(string Label, Action Action, bool Enabled)
    {
        public string        Label        { get; } = Label;
        public Action        Action       { get; } = Action;
        public bool          Enabled      { get; } = Enabled;
        public bool          IsSeparator  { get; init; }
        public bool          IsHeader     { get; init; }
        public Bindable<bool>? Binding    { get; init; }
        public bool          CloseOnClick { get; init; }
    }
}
