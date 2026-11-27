namespace ApexUI.Widgets;

/// Pick-one-from-a-list widget. Opens a MenuList Overlay on left-click.
/// Selected item shown with a ✓ checkmark inside the list.
public class Dropdown<T> : Widget
{
    private readonly List<(T Value, string Label)> _items = [];
    private T? _selected;
    private string _placeholder = "Select...";
    private readonly DropdownTrigger _trigger;
    private readonly MenuList _menuList;
    private readonly Overlay _overlay;
    private Bindable<T>? _binding;
    private bool _suppressBinding;

    private IDisposable? _itemsSubscription;
    private IDisposable? _bindingSubscription;

    public T? SelectedValue
    {
        get => _selected;
        set
        {
            _selected = value;
            SyncText();
            if (!_suppressBinding && _binding is not null)
            {
                _suppressBinding = true;
                _binding.Value = value!;
                _suppressBinding = false;
            }
            SelectionChanged?.Invoke(value);
        }
    }

    public string Placeholder
    {
        get => _placeholder;
        set { _placeholder = value; SyncText(); }
    }

    public event Action<T?>? SelectionChanged;

    public Dropdown()
    {
        _menuList = new MenuList(minWidth: 160f, maxHeight: 200f);

        _overlay = new Overlay { DismissOnClickOutside = true, MatchAnchorWidth = true };
        _overlay.Content = _menuList;

        _trigger = new DropdownTrigger(ToggleOpen) { Text = _placeholder, IsPlaceholder = true };
        _overlay.Closed += () => _trigger.IsOpen = false;

        AddChild(_trigger);
    }

    // ── Fluent API ────────────────────────────────────────────────────────────

    public Dropdown<T> AddItem(T value, string label)
    {
        _items.Add((value, label));
        RebuildList();
        return this;
    }

    /// Binds the item list to an ObservableList. Rebuilds automatically on every change.
    public Dropdown<T> WithItems(ObservableList<T> source, Func<T, string> labelOf)
    {
        _itemsSubscription?.Dispose();
        void Sync()
        {
            _items.Clear();
            foreach (var item in source) _items.Add((item, labelOf(item)));
            RebuildList();
            if (_selected is not null && !source.Contains(_selected))
            { _selected = default; SyncText(); }
        }
        Sync();
        _itemsSubscription = Observe(source, Sync);
        return this;
    }

    /// Convenience overload — uses ToString() as the label.
    public Dropdown<T> WithItems(ObservableList<T> source)
        => WithItems(source, item => item?.ToString() ?? "");

    public Dropdown<T> WithPlaceholder(string text) { Placeholder = text; return this; }

    public Dropdown<T> WithSelected(T value) { SelectedValue = value; return this; }

    public Dropdown<T> OnChange(Action<T?> action) { SelectionChanged += action; return this; }
    public Dropdown<T> WithReset(Func<bool> isDefault, Action reset, Action<ContextMenu>? extendMenu = null)
    {
        ResetHook.Attach(this, isDefault, reset, extendMenu: extendMenu);
        return this;
    }

    public Dropdown<T> Bind(Bindable<T> source)
    {
        _bindingSubscription?.Dispose();
        _binding = source;
        _suppressBinding = true;
        SelectedValue = source.Value;
        _suppressBinding = false;
        _bindingSubscription = Observe(source, v =>
        {
            if (_suppressBinding) return;
            _suppressBinding = true;
            SelectedValue = v;
            _suppressBinding = false;
        });
        return this;
    }

    // ── Layout pass-through ───────────────────────────────────────────────────

    protected override Size MeasureCore(Size available)
    {
        _trigger.Measure(available);
        // Measure the popup at natural size so its minimum width (from MenuList._minWidth
        // and item content) acts as a floor for the trigger width. This guarantees both
        // are always the same width before MatchAnchorWidth even runs.
        _menuList.Measure(Size.Infinite);
        float w = Math.Max(_trigger.DesiredSize.Width, _menuList.DesiredSize.Width);
        return new Size(w, _trigger.DesiredSize.Height);
    }

    protected override void ArrangeCore(Rect r) => _trigger.Arrange(r);

    protected override void OnDetached() => _overlay.Detach();

    private void ToggleOpen()
    {
        if (_overlay.IsVisible)
            _overlay.Close();
        else
        {
            _overlay.Open(_trigger);
            _trigger.IsOpen = true;
        }
    }

    // ── Internal ──────────────────────────────────────────────────────────────

    private void SyncText()
    {
        if (_selected is null)
        {
            _trigger.Text          = _placeholder;
            _trigger.IsPlaceholder = true;
            return;
        }
        var match = _items.FirstOrDefault(i => EqualityComparer<T>.Default.Equals(i.Value, _selected));
        _trigger.Text          = match.Label ?? _placeholder;
        _trigger.IsPlaceholder = match.Label is null;
    }

    private void RebuildList()
    {
        _menuList.Clear();
        foreach (var (value, label) in _items)
        {
            var itemValue = value;
            var item = new MenuItemWidget(
                label,
                onActivate: () =>
                {
                    SelectedValue = itemValue;
                    _overlay.Close();
                },
                isChecked: () => EqualityComparer<T>.Default.Equals(_selected, itemValue));
            _menuList.Add(item);
        }
    }

    // ── Trigger widget ────────────────────────────────────────────────────────

    private sealed class DropdownTrigger : Widget
    {
        private readonly Label _label;
        private readonly Action _onToggle;
        private bool _isOpen;

        private const float ChevronW = 28f;

        public DropdownTrigger(Action onToggle)
        {
            _onToggle    = onToggle;
            MinWidth     = 120f;
            Focusable    = true;
            _label = new Label
            {
                IsHitTestVisible = false,
                ColorSource      = t => IsPlaceholder ? t.OnSurfaceMuted : t.OnSurface,
            };
            AddChild(_label);
        }

        public string Text { set => _label.Text = value; }

        public bool IsOpen { set => SetRenderProperty(ref _isOpen, value); }

        public bool IsPlaceholder { get; set => SetRenderProperty(ref field, value); }

        protected override void OnClick(PointerEventArgs e)
        {
            e.Handled = true;
            _onToggle();
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            if (!Activation.IsActivationKey(e) && !(Activation.IsOpenKey(e) && !_isOpen)) return;
            e.Handled = true;
            _onToggle();
        }

        // The chevron sits in the right padding.
        protected override Thickness DefaultPadding(Theme theme)
        {
            var p = theme.InputPadding;
            return new Thickness(p.Left, p.Top, ChevronW, p.Bottom);
        }

        protected override float DefaultCornerRadius(Theme theme) => theme.CornerRadiusControl;

        // ── Drawing ──────────────────────────────────────────────────────────

        protected override void DrawCore(DrawContext ctx)
        {
            var t  = ctx.Theme;
            var lb = LayoutBounds;

            var bg = IsPressed ? t.SurfacePressed
                   : IsHovered ? t.SurfaceHover
                   : t.Surface;
            ctx.FillRoundRect(lb, CornerRadius, bg);
            ctx.StrokeRoundRect(lb, CornerRadius, _isOpen ? t.Primary : t.Border, _isOpen ? 2f : 1f);

            float divX = lb.Right - ChevronW;
            ctx.DrawLine(divX, lb.Y + 6f, divX, lb.Bottom - 6f, t.BorderSubtle, cap: SKStrokeCap.Butt);

            ctx.DrawChevron(lb.Right - ChevronW * 0.5f, lb.CenterY, 3.5f, pointUp: _isOpen, t.OnSurfaceMuted);
        }
    }
}
