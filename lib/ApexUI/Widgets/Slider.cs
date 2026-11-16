namespace ApexUI.Widgets;

public class Slider : Widget
{
    private bool _dragging;

    public float Min  { get; set { if (SetRenderProperty(ref field, value)) ClampToRange(); } } = 0f;
    public float Max  { get; set { if (SetRenderProperty(ref field, value)) ClampToRange(); } } = 1f;

    /// 0 = continuous (no snapping); any other positive value snaps to Min + n × Step.
    public float Step { get; set { if (SetRenderProperty(ref field, value)) ClampToRange(); } } = 0f;

    public float Value
    {
        get;
        set
        {
            float snapped = Snap(value);
            if (field == snapped) return;
            field = snapped;
            Invalidate();
            ValueChanged?.Invoke(snapped);
        }
    }

    public event Action<float>? ValueChanged;

    public Slider()
    {
        MinWidth  = 80f;
        Focusable = true;
    }

    protected override float DefaultCornerRadius(Theme theme) => theme.SliderThumbSize * 0.5f;

    protected override void OnPointerPressed(PointerEventArgs e)
    {
        if (e.Button != PointerButton.Left) return;
        e.Handled = true;
        _dragging = true;
        UpdateFromX(e.X);
    }

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        if (!_dragging) return;
        e.Handled = true;
        UpdateFromX(e.X);
    }

    protected override void OnPointerCaptureLost() => _dragging = false;

    // Arrows move by Step (or 1% of the range when continuous), Page keys by ten steps, Home/End to the ends.
    protected override void OnKeyDown(KeyEventArgs e)
    {
        float step = Step > 0f ? Step : (Max - Min) / 100f;
        switch (e.Key)
        {
            case Key.Left or Key.Down: Value -= step;       break;
            case Key.Right or Key.Up:  Value += step;       break;
            case Key.PageDown:         Value -= step * 10f; break;
            case Key.PageUp:           Value += step * 10f; break;
            case Key.Home:             Value  = Min;        break;
            case Key.End:              Value  = Max;        break;
            default: return;
        }
        e.Handled = true;
    }

    // ── Fluent ───────────────────────────────────────────────────────────────

    public Slider WithMin(float min)        { Min   = min;   return this; }
    public Slider WithMax(float max)        { Max   = max;   return this; }
    public Slider WithStep(float step)      { Step  = step;  return this; }
    public Slider WithValue(float value)    { Value = value; return this; }
    public Slider OnChange(Action<float> a) { ValueChanged += a; return this; }

    // ── Binding ───────────────────────────────────────────────────────────────

    public Slider Bind(Bindable<float> source)
    {
        Value = source.Value;
        ValueChanged += v => source.Value = v;
        Observe(source, v => Value = v);
        return this;
    }

    public Slider BindInt(Bindable<int> source)
    {
        Value = source.Value;
        ValueChanged += v => source.Value = (int)MathF.Round(v);
        Observe(source, v => Value = v);
        return this;
    }

    // ── Layout ───────────────────────────────────────────────────────────────

    protected override Size MeasureCore(Size available)
        => new(float.IsNaN(Width) ? Math.Max(MinWidth, available.Width) : Width, Theme.SliderThumbSize);

    // ── Drawing ──────────────────────────────────────────────────────────────

    protected override void DrawCore(DrawContext ctx)
    {
        if (LayoutBounds.Width <= 0) return;

        var   theme  = ctx.Theme;
        float thumbR = theme.SliderThumbSize * 0.5f;
        float trackH = theme.SliderTrackHeight;
        float range  = Max - Min;
        float t      = range > 0f ? (Value - Min) / range : 0f;
        float cx0    = LayoutBounds.X + thumbR;
        float cx1    = LayoutBounds.Right - thumbR;
        float thumbX = cx0 + t * (cx1 - cx0);
        float trackY = LayoutBounds.CenterY - trackH * 0.5f;

        ctx.FillRoundRect(new Rect(cx0, trackY, cx1 - cx0, trackH), trackH * 0.5f, theme.Border);

        if (thumbX > cx0)
            ctx.FillRoundRect(new Rect(cx0, trackY, thumbX - cx0, trackH), trackH * 0.5f, theme.Primary);

        var thumbColor = (IsPressed || IsHovered) ? theme.PrimaryHover : theme.Primary;
        ctx.FillCircle(thumbX, LayoutBounds.CenterY, thumbR, thumbColor);
        ctx.StrokeCircle(thumbX, LayoutBounds.CenterY, thumbR - 1f, theme.Knob, 2f);
    }

    // ── Private ──────────────────────────────────────────────────────────────

    private void UpdateFromX(float x)
    {
        float thumbR = Theme.SliderThumbSize * 0.5f;
        float cx0    = LayoutBounds.X + thumbR;
        float trackW = LayoutBounds.Width - thumbR * 2f;
        if (trackW <= 0f) return;
        Value = Min + Math.Clamp((x - cx0) / trackW, 0f, 1f) * (Max - Min);
    }

    // Min may briefly exceed Max while both are being set; clamp once the range is valid again.
    private void ClampToRange()
    {
        if (Min <= Max) Value = Value;
    }

    // Steps count from Min, and a step past Max falls back one, so the result stays in range
    // even when the range is not a whole number of steps.
    private float Snap(float raw)
    {
        float clamped = Math.Clamp(raw, Min, Max);
        if (Step <= 0f) return clamped;
        float snapped = Min + MathF.Round((clamped - Min) / Step) * Step;
        return snapped > Max ? snapped - Step : snapped;
    }
}
