namespace ApexUI.Widgets;

/// A segmented control: a row (or column) of options showing text or an icon, exactly one selected.
/// Selecting an option from the user or from code raises SelectionChanged once per change.
public class ToggleGroup<T> : LinearPanel
{
    private sealed class Segment : Widget
    {
        private readonly Action _onSelect;

        public Segment(Widget content, bool iconOnly, Action onSelect)
        {
            _onSelect = onSelect;
            IconOnly  = iconOnly;
            Focusable = true;
            content.HAlign           = HAlign.Center;
            content.VAlign           = VAlign.Center;
            content.IsHitTestVisible = false;
            AddChild(content);
        }

        public bool IconOnly { get; }

        public bool IsSelected { get; set => SetRenderProperty(ref field, value); }

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
        Segment? segment = null;
        var label = new Label
        {
            Text        = text,
            ColorSource = t => segment!.IsSelected ? t.OnPrimary : segment.IsEnabled ? t.OnSurface : t.OnSurfaceMuted,
        };
        segment = Add(value, label, iconOnly: false);
        AddChild(segment);
        return this;
    }

    /// tooltip: shown on hover, since an icon alone may not say what the option does.
    public ToggleGroup<T> AddOption(T value, Icon icon, string? tooltip = null)
    {
        Segment? segment = null;
        icon.ColorSource = t => segment!.IsSelected ? t.OnPrimary : segment.IsEnabled ? t.OnSurface : t.OnSurfaceMuted;
        segment = Add(value, icon, iconOnly: true);
        AddChild(tooltip is null ? segment : new Tooltip(segment, tooltip));
        return this;
    }

    public ToggleGroup<T> WithSelected(T value)         { SelectedValue = value; return this; }
    public ToggleGroup<T> OnChange(Action<T?> action)   { SelectionChanged += action; return this; }

    public ToggleGroup<T> Bind(Bindable<T> source)
    {
        SelectedValue = source.Value;
        SelectionChanged += v => { if (v is not null) source.Value = v; };
        Observe(source, v => SelectedValue = v);
        return this;
    }

    private Segment Add(T value, Widget content, bool iconOnly)
    {
        var segment = new Segment(content, iconOnly, () => SelectedValue = value);
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
