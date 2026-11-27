namespace ApexUI.Widgets;

/// A reset-to-default hook for a value widget or label. Right-clicking the target opens a ContextMenu
/// with a Reset item (disabled while already at the default) and optional host items.
public sealed class ResetHook
{
    private readonly ContextMenu _menu = new();
    private bool _built;

    public ResetHook(Func<bool> isDefault, Action reset, string label = "Reset", Action<ContextMenu>? extendMenu = null)
    {
        IsDefault  = isDefault;
        Reset      = reset;
        Label      = label;
        ExtendMenu = extendMenu;
    }

    public Func<bool> IsDefault { get; }

    public Action Reset { get; }

    public string Label { get; }

    public Action<ContextMenu>? ExtendMenu { get; }

    public ContextMenu Menu
    {
        get
        {
            if (!_built)
            {
                _built = true;
                _menu.AddItem(Label, () => Reset(), () => !IsDefault());
                ExtendMenu?.Invoke(_menu);
            }

            return _menu;
        }
    }

    public void Show(UiHost host, float x, float y) => Menu.Show(host, x, y);

    public static ResetHook Attach(Widget target, Func<bool> isDefault, Action reset, string label = "Reset", Action<ContextMenu>? extendMenu = null)
    {
        var hook = new ResetHook(isDefault, reset, label, extendMenu);
        target.AttachedReset = hook;
        return hook;
    }

    public static void Attach(Widget target, ResetHook? hook) => target.AttachedReset = hook;
}
