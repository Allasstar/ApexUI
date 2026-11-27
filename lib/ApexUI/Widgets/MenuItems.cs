namespace ApexUI.Widgets;

/// The entries of a popup menu, shared by ContextMenu and MenuButton. Builds the MenuList they show;
/// close runs before an action item's action, so the action may open another overlay.
internal sealed class MenuItems
{
    private readonly List<MenuEntry> _entries = [];

    /// Bumped on every change, so an owner can rebuild a panel it built earlier.
    public int Version { get; private set; }

    /// True when any entry computes its enabled state dynamically when the menu opens.
    public bool HasDynamicItems { get; private set; }

    public void AddItem(string label, Action action, bool enabled)
        => Add(new MenuEntry(label, action, enabled, null));

    public void AddItem(string label, Action action, Func<bool> isEnabled)
    {
        HasDynamicItems = true;
        Add(new MenuEntry(label, action, true, isEnabled));
    }

    public void AddCheckItem(string label, Bindable<bool> binding, bool closeOnClick)
        => Add(new MenuEntry(label, () => binding.Value = !binding.Value, true, null) { Binding = binding, CloseOnClick = closeOnClick });

    public void AddSeparator() => Add(new MenuEntry("", static () => { }, true, null) { IsSeparator = true });

    public void AddHeader(string text) => Add(new MenuEntry(text, static () => { }, false, null) { IsHeader = true });

    public void Clear()
    {
        _entries.Clear();
        HasDynamicItems = false;
        Version++;
    }

    public MenuList Build(Action close, float minWidth)
    {
        var panel = new MenuList(minWidth: minWidth, maxHeight: float.PositiveInfinity);
        foreach (MenuEntry entry in _entries) panel.Add(BuildEntry(entry, close));
        return panel;
    }

    private void Add(MenuEntry entry)
    {
        _entries.Add(entry);
        Version++;
    }

    private static Widget BuildEntry(MenuEntry entry, Action close)
    {
        if (entry.IsSeparator) return new MenuSeparator();
        if (entry.IsHeader) return new MenuHeaderWidget(entry.Label);

        if (entry.Binding is { } binding)
        {
            bool closeOnClick = entry.CloseOnClick;
            MenuItemWidget? item = null;
            item = new MenuItemWidget(entry.Label, isChecked: () => binding.Value, onActivate: () =>
            {
                binding.Value = !binding.Value;
                item!.Invalidate();
                if (closeOnClick) close();
            });
            return item;
        }

        Action action = entry.Action;
        bool enabled = entry.IsEnabled is not null ? entry.IsEnabled() : entry.Enabled;
        return new MenuItemWidget(entry.Label, enabled: enabled, onActivate: () =>
        {
            close();
            if (enabled) action();
        });
    }

    private sealed class MenuEntry(string label, Action action, bool enabled, Func<bool>? isEnabled)
    {
        public string Label { get; } = label;
        public Action Action { get; } = action;
        public bool Enabled { get; } = enabled;
        public Func<bool>? IsEnabled { get; } = isEnabled;
        public bool IsSeparator { get; init; }
        public bool IsHeader { get; init; }
        public Bindable<bool>? Binding { get; init; }
        public bool CloseOnClick { get; init; }
    }
}
