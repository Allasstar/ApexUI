namespace ApexUI.Widgets;

/// A segmented control: a row (or column) of options showing text or an icon, exactly one selected.
/// Selecting an option from the user or from code raises SelectionChanged once per change.
public class ToggleGroup<T> : LinearPanel
{
    private sealed class Segment : Widget
    {
        private readonly Action _onSelect;

        public Segment(bool iconOnly, Action onSelect)
        {
            _onSelect = onSelect;
            IconOnly  = iconOnly;
            Focusable = true;
        }

        public bool IconOnly { get; }

        public bool IsSelected { get; set => SetRenderProperty(ref field, value); }

        public Widget With(Widget content)
        {
            content.HAlign           = HAlign.Center;
            content.VAlign           = VAlign.Center;
            content.IsHitTestVisible = false;
            AddChild(content);
            return this;
        }

        public SKColor ContentColor(Theme t)
            => IsSelected ? t.OnPrimary : IsEnabled ? t.OnSurface : t.OnSurfaceMuted;

        protected override Thickness DefaultPadding(Theme theme)
            => IconOnly ? theme.IconButtonPadding : theme.SegmentPadding;

        protected override float DefaultCornerRadius(Theme theme) => theme.CornerRadiusSmall;

        protected override void OnClick(PointerEventArgs e)
        {
            e.Handled = true;
            _onSelect();
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            if (!Activation.IsActivationKey(e)) return;
            e.Handled = true;
            _onSelect();
        }

        protected override void DrawCore(DrawContext ctx)
        {
            var t = ctx.Theme;
            if (IsSelected)                       ctx.FillRoundRect(LayoutBounds, CornerRadius, t.Primary);
            else if (IsPressed && IsEnabled)      ctx.FillRoundRect(LayoutBounds, CornerRadius, t.SurfacePressed);
            else if (IsHovered && IsEnabled)      ctx.FillRoundRect(LayoutBounds, CornerRadius, t.SurfaceHover);
        }
    }

    private readonly List<(T Value, Segment Segment)> _options = [];
    private bool _hasSelection;

    public ToggleGroup(Orientation orientation = Orientation.Horizontal) : base(orientation) { }

    public T? SelectedValue
    {
        get;
        set
        {
            if (_hasSelection && EqualityComparer<T>.Default.Equals(field, value)) return;
            field         = value;
            _hasSelection = true;
            foreach (var (v, segment) in _options)
                segment.IsSelected = EqualityComparer<T>.Default.Equals(v, value);
            SelectionChanged?.Invoke(value);
        }
    }

    public event Action<T?>? SelectionChanged;

    protected override Thickness DefaultPadding(Theme theme) => new(theme.SpacingXs * 0.5f);
    protected override float DefaultCornerRadius(Theme theme) => theme.CornerRadiusControl;
    protected override float DefaultSpacing(Theme theme) => theme.SpacingXs * 0.5f;

    public ToggleGroup<T> AddOption(T value, string text)
    {
        var segment = NewSegment(value, iconOnly: false);
        AddChild(segment.With(new Label { Text = text, ColorSource = segment.ContentColor }));
        return this;
    }

    /// tooltip: shown on hover, since an icon alone may not say what the option does.
    public ToggleGroup<T> AddOption(T value, Icon icon, string? tooltip = null)
    {
        var segment = NewSegment(value, iconOnly: true);
        icon.ColorSource = segment.ContentColor;
        segment.With(icon);
        AddChild(tooltip is null ? segment : new Tooltip(segment, tooltip));
        return this;
    }

    public ToggleGroup<T> WithSelected(T value)         { SelectedValue = value; return this; }
    public ToggleGroup<T> OnChange(Action<T?> action)   { SelectionChanged += action; return this; }
    public ToggleGroup<T> WithReset(Func<bool> isDefault, Action reset, Action<ContextMenu>? extendMenu = null)
    {
        ResetHook.Attach(this, isDefault, reset, extendMenu: extendMenu);
        return this;
    }

    public ToggleGroup<T> Bind(Bindable<T> source)
    {
        SelectedValue = source.Value;
        SelectionChanged += v => { if (v is not null) source.Value = v; };
        Observe(source, v => SelectedValue = v);
        return this;
    }

    private Segment NewSegment(T value, bool iconOnly)
    {
        var segment = new Segment(iconOnly, () => SelectedValue = value);
        segment.IsSelected = _hasSelection && EqualityComparer<T>.Default.Equals(SelectedValue, value);
        _options.Add((value, segment));
        return segment;
    }

    protected override void DrawCore(DrawContext ctx)
    {
        var t = ctx.Theme;
        ctx.FillRoundRect(LayoutBounds, CornerRadius, t.Surface);
        ctx.StrokeRoundRect(LayoutBounds, CornerRadius, t.Border);
    }
}
