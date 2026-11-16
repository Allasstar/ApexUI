namespace ApexUI.Widgets;

/// Mutually-exclusive group of radio options backed by a Bindable<T>, laid out in a line.
/// Use AddOption() to populate, then Bind() or SelectedValue to control selection.
public class RadioGroup<T> : LinearPanel
{
    // ── Inner radio button ────────────────────────────────────────────────────

    private sealed class RadioItem : Widget
    {
        private readonly Action _onSelect;

        public string ItemLabel { get; }
        public bool IsSelected  { get; set => SetRenderProperty(ref field, value); }

        public RadioItem(string label, Action onSelect)
        {
            ItemLabel    = label;
            _onSelect    = onSelect;
            Focusable    = true;
        }

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

        protected override Size MeasureCore(Size available)
        {
            var t = Theme;
            using var font = LayoutFont.Get(Host, t.FontSizeBody);
            float textW = string.IsNullOrEmpty(ItemLabel) ? 0f : font.Font.MeasureText(ItemLabel) + 2f;
            return new Size(t.RadioSize + t.SpacingSm + textW, MathF.Max(t.RadioSize + 4f, t.FontSizeBody * t.LineHeight));
        }

        protected override void DrawCore(DrawContext ctx)
        {
            var   t  = ctx.Theme;
            float r  = t.RadioSize * 0.5f;
            float cx = LayoutBounds.X + r + 2;
            float cy = LayoutBounds.CenterY;

            if ((IsHovered || IsPressed) && IsEnabled)
                ctx.FillCircle(cx, cy, r, t.PrimaryHoverGhost);

            var ringColor = IsSelected && IsEnabled ? t.Primary : t.Border;
            ctx.StrokeCircle(cx, cy, r, ringColor, 2f);

            if (IsSelected)
                ctx.FillCircle(cx, cy, r * 0.5f, IsEnabled ? t.Primary : t.Border);

            if (!string.IsNullOrEmpty(ItemLabel))
            {
                float offset = t.RadioSize + t.SpacingSm;
                ctx.DrawText(ItemLabel,
                    new Rect(LayoutBounds.X + offset, LayoutBounds.Y, LayoutBounds.Width - offset, LayoutBounds.Height),
                    IsEnabled ? t.OnSurface : t.OnSurfaceMuted, t.FontSizeBody);
            }
        }
    }

    // ── RadioGroup<T> ─────────────────────────────────────────────────────────

    private readonly List<(T Value, RadioItem Item)> _options = [];

    private T? _selected;

    public T? SelectedValue
    {
        get => _selected;
        set
        {
            _selected = value;
            foreach (var (v, item) in _options)
                item.IsSelected = EqualityComparer<T>.Default.Equals(v, value);
            SelectionChanged?.Invoke(value);
        }
    }

    public event Action<T?>? SelectionChanged;

    /// spacing: gap between options; NaN takes the theme's SpacingSm.
    public RadioGroup(Orientation orientation = Orientation.Vertical, float spacing = float.NaN) : base(orientation)
    {
        Spacing = spacing;
    }

    protected override float DefaultSpacing(Theme theme) => theme.SpacingSm;

    // ── Fluent ────────────────────────────────────────────────────────────────

    public RadioGroup<T> AddOption(T value, string label)
    {
        var item = new RadioItem(label, () => SelectedValue = value);
        _options.Add((value, item));
        AddChild(item);
        return this;
    }

    public RadioGroup<T> WithSelected(T value)    { SelectedValue = value; return this; }
    public RadioGroup<T> OnChange(Action<T?> action) { SelectionChanged += action; return this; }

    public RadioGroup<T> Bind(Bindable<T> source)
    {
        SelectedValue = source.Value;
        SelectionChanged += v => { if (v is not null) source.Value = v; };
        Observe(source, v => SelectedValue = v);
        return this;
    }
}
