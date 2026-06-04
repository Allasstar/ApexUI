namespace ApexUI.Widgets;

/// Pick-one-from-a-list widget. Opens a MenuList Overlay on left-click.
/// Selected item shown with a ✓ checkmark inside the list.
public class Dropdown<T> : Widget
{
    private readonly List<(T Value, string Label)> _items = [];
    private T? _selected;
    private string _placeholder = "Select...";
    private readonly Button _toggleButton;
    private readonly MenuList _menuList;
    private readonly Overlay _overlay;
    private Bindable<T>? _binding;
    private bool _suppressBinding;

    public T? SelectedValue
    {
        get => _selected;
        set
        {
            _selected = value;
            SyncButtonText();
            if (!_suppressBinding && _binding is not null)
            {
                _suppressBinding = true;
                _binding.Value = value!;
                _suppressBinding = false;
            }
            OnChanged?.Invoke(value);
        }
    }

    public string Placeholder
    {
        get => _placeholder;
        set { _placeholder = value; SyncButtonText(); }
    }

    public Action<T?>? OnChanged;

    public Dropdown()
    {
        _menuList = new MenuList(minWidth: 160f, maxHeight: 200f);

        _overlay = new Overlay { DismissOnClickOutside = true };
        _overlay.Content = _menuList;

        _toggleButton = new Button(_placeholder).WithVariant(ButtonVariant.Secondary);
        // Only left-click toggles the dropdown — right-click is reserved for context menus.
        _toggleButton.OnClick = e =>
        {
            if (e.Button != PointerButton.Left) return;
            if (_overlay.IsVisible) _overlay.Close();
            else _overlay.Open(_toggleButton, OverlayAnchor.BelowAnchor);
        };

        AddChild(_toggleButton);
    }

    // ── Fluent API ────────────────────────────────────────────────────────────

    public Dropdown<T> AddItem(T value, string label)
    {
        _items.Add((value, label));
        RebuildList();
        return this;
    }

    public Dropdown<T> WithPlaceholder(string text) { Placeholder = text; return this; }

    public Dropdown<T> WithSelected(T value) { SelectedValue = value; return this; }

    public Dropdown<T> OnChange(Action<T?> action) { OnChanged = action; return this; }

    public Dropdown<T> Bind(Bindable<T> source)
    {
        _binding = source;
        _suppressBinding = true;
        SelectedValue = source.Value;
        _suppressBinding = false;
        source.Changed += v =>
        {
            if (_suppressBinding) return;
            _suppressBinding = true;
            SelectedValue = v;
            _suppressBinding = false;
        };
        return this;
    }

    // ── Layout pass-through ───────────────────────────────────────────────────

    protected override Size MeasureCore(Size available)
    {
        _toggleButton.Measure(available);
        return _toggleButton.DesiredSize;
    }

    protected override void ArrangeCore(Rect r)
        => _toggleButton.Arrange(r);

    // ── Internal ──────────────────────────────────────────────────────────────

    private void SyncButtonText()
    {
        if (_selected is null)
        {
            _toggleButton.Text = _placeholder;
            return;
        }
        var match = _items.FirstOrDefault(i => EqualityComparer<T>.Default.Equals(i.Value, _selected));
        _toggleButton.Text = match.Label ?? _placeholder;
    }

    private void RebuildList()
    {
        _menuList.Clear();
        foreach (var (value, label) in _items)
        {
            var itemValue = value;
            MenuItemWidget? item = null;
            item = new MenuItemWidget(
                label,
                enabled: true,
                isChecked: () => EqualityComparer<T>.Default.Equals(_selected, itemValue));
            item.OnClick = e =>
            {
                if (e.Button != PointerButton.Left) return;
                SelectedValue = itemValue;
                _overlay.Close();
            };
            _menuList.Add(item);
        }
    }
}
