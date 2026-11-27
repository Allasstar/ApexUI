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
    private const float MinWidth = 180f;

    private readonly MenuItems _items = new();
    private readonly Overlay _overlay;
    private int _builtVersion = -1;

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

    public bool IsOpen => _overlay.IsVisible;

    // ── Fluent builder ────────────────────────────────────────────────────────

    public ContextMenu AddItem(string label, Action action, bool enabled = true)
    {
        _items.AddItem(label, action, enabled);
        return this;
    }

    public ContextMenu AddItem(string label, Action action, Func<bool> isEnabled)
    {
        _items.AddItem(label, action, isEnabled);
        return this;
    }

    public ContextMenu AddCheckItem(string label, Bindable<bool> binding, bool closeOnClick = false)
    {
        _items.AddCheckItem(label, binding, closeOnClick);
        return this;
    }

    public ContextMenu AddSeparator()
    {
        _items.AddSeparator();
        return this;
    }

    public ContextMenu AddHeader(string text)
    {
        _items.AddHeader(text);
        return this;
    }

    public ContextMenu Clear()
    {
        _items.Clear();
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
        if (_builtVersion != _items.Version || _items.HasDynamicItems)
        {
            _overlay.Content = _items.Build(Close, MinWidth);
            _builtVersion = _items.Version;
        }

        _overlay.ContentHAlign = HAlign.Left;
        _overlay.ContentVAlign = VAlign.Top;
        _overlay.PositionX     = x;
        _overlay.PositionY     = y;
        _overlay.Anchor        = null;
        _overlay.Open(host);
    }

    public void Close() => _overlay.Close();
}
