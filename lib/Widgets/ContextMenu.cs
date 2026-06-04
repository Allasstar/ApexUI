namespace ApexUI.Widgets;

/// Right-click Overlay with labeled action items, separators, headers, and toggle items.
/// Dismisses on click-outside. Attach to any widget: ContextMenu.Attach(widget, menu)
///
/// Standard behavior:
///   • Right mouse UP on target → menu appears at cursor
///   • Left-click item → action fires, menu closes
///   • Left-click outside → menu closes
///   • Right-click outside while open → menu moves to new cursor position
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
        // DismissOnClickOutside is handled manually below so we get right-click reposition.
        _overlay = new Overlay { DismissOnClickOutside = false };

        // Backdrop left-click → close; backdrop right-click → move to new position.
        // This fires only when the deepest hit is the Overlay itself (not a menu item).
        _overlay.OnClick = e =>
        {
            if (e.Button == PointerButton.Right)
                Show(e.X, e.Y);   // reposition without closing
            else
                Close();           // left-click (or middle) → dismiss
        };
    }

    // ── Fluent builder ────────────────────────────────────────────────────────

    /// Standard action item — closes the menu and calls action on left-click.
    public ContextMenu AddItem(string label, Action action, bool enabled = true)
    {
        _entries.Add(new MenuEntry(label, action, enabled));
        return this;
    }

    /// Toggle item — shows ● when binding is true; left-click flips the value.
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
    /// Uses OnPointerUp (right mouse button release) — matches standard OS context menu timing.
    /// Call after the widget tree under target is fully built.
    public static void Attach(Widget target, ContextMenu menu)
        => AttachRecursive(target, menu);

    private static void AttachRecursive(Widget w, ContextMenu menu)
    {
        // OnPointerUp fires on _pressedWidget regardless of current cursor position.
        // Right-button was pressed AND released on this widget → show menu at release position.
        var prev = w.OnPointerUp;
        w.OnPointerUp = e =>
        {
            prev?.Invoke(e);
            if (e.Button == PointerButton.Right)
                menu.Show(e.X, e.Y);
        };
        foreach (var child in w.Children)
            AttachRecursive(child, menu);
    }

    // ── Show / Close ──────────────────────────────────────────────────────────

    /// Show (or reposition) the menu at the given logical screen coordinates.
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
                var binding      = entry.Binding;
                var closeOnClick = entry.CloseOnClick;
                MenuItemWidget? item = null;
                item = new MenuItemWidget(entry.Label, isChecked: () => binding.Value);
                item.OnClick = e =>
                {
                    if (e.Button != PointerButton.Left) return;
                    binding.Value = !binding.Value;
                    item!.Invalidate();
                    if (closeOnClick) Close();
                };
                list.Add(item);
            }
            else
            {
                var action  = entry.Action;
                var enabled = entry.Enabled;
                var item    = new MenuItemWidget(entry.Label, enabled);
                item.OnClick = e =>
                {
                    if (e.Button != PointerButton.Left) return;
                    Close();
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
        public string          Label        { get; } = Label;
        public Action          Action       { get; } = Action;
        public bool            Enabled      { get; } = Enabled;
        public bool            IsSeparator  { get; init; }
        public bool            IsHeader     { get; init; }
        public Bindable<bool>? Binding      { get; init; }
        public bool            CloseOnClick { get; init; }
    }
}
