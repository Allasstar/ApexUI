namespace ApexUI.Widgets;

/// Mutually-exclusive group of radio options backed by a Bindable<T>, laid out in a line.
/// Use AddOption() to populate, then Bind() or SelectedValue to control selection.
public class RadioGroup<T> : LinearPanel
{
    // ── Inner radio button ────────────────────────────────────────────────────

    private sealed class RadioItem : Widget
    {
        private const float CircleR = 10f;
        private const float DotR    = 5f;
        private const float Gap     = 10f;

        private readonly Action _onSelect;

        public string ItemLabel { get; }
        public bool IsSelected  { get; set => SetRenderProperty(ref field, value); }

        public RadioItem(string label, Action onSelect)
        {
            ItemLabel    = label;
            _onSelect    = onSelect;
            Focusable    = true;
            CornerRadius = 4f;
        }

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
            using var font = LayoutFont.Get(Host, 14f);
            float textW = string.IsNullOrEmpty(ItemLabel) ? 0f : font.Font.MeasureText(ItemLabel) + 2f;
            return new Size(CircleR * 2 + Gap + textW, MathF.Max(CircleR * 2 + 4f, 14f * 1.4f));
        }

        protected override void DrawCore(DrawContext ctx)
        {
            float cx = LayoutBounds.X + CircleR + 2;
            float cy = LayoutBounds.CenterY;

            // Hover ripple
            if ((IsHovered || IsPressed) && IsEnabled)
                ctx.FillCircle(cx, cy, CircleR, ctx.Theme.PrimaryHoverGhost);

            // Outer ring
            var ringColor = IsSelected && IsEnabled ? ctx.Theme.Primary : ctx.Theme.Border;
            ctx.StrokeCircle(cx, cy, CircleR, ringColor, 2f);

            // Inner dot when selected
            if (IsSelected)
                ctx.FillCircle(cx, cy, DotR, IsEnabled ? ctx.Theme.Primary : ctx.Theme.Border);

            // Label text
            if (!string.IsNullOrEmpty(ItemLabel))
            {
                float tx = LayoutBounds.X + CircleR * 2 + Gap;
                ctx.DrawText(ItemLabel,
                    new Rect(tx, LayoutBounds.Y, LayoutBounds.Width - CircleR * 2 - Gap, LayoutBounds.Height),
                    IsEnabled ? ctx.Theme.OnSurface : ctx.Theme.OnSurfaceMuted, 14f);
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
            OnChanged?.Invoke(value);
        }
    }

    public Action<T?>? OnChanged;

    public RadioGroup(Orientation orientation = Orientation.Vertical, float spacing = 8f) : base(orientation)
    {
        Spacing = spacing;
    }

    // ── Fluent ────────────────────────────────────────────────────────────────

    public RadioGroup<T> AddOption(T value, string label)
    {
        var item = new RadioItem(label, () => SelectedValue = value);
        _options.Add((value, item));
        AddChild(item);
        return this;
    }

    public RadioGroup<T> WithSelected(T value)    { SelectedValue = value; return this; }
    public RadioGroup<T> OnChange(Action<T?> action) { OnChanged  = action; return this; }

    public RadioGroup<T> Bind(Bindable<T> source)
    {
        SelectedValue   = source.Value;
        OnChanged      += v => { if (v is not null) source.Value = v; };
        Observe(source, v => SelectedValue = v);
        return this;
    }
}
