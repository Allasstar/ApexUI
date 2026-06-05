namespace ApexUI.Widgets;

public class Slider : Widget
{
    private const float TrackH = 4f;
    private const float ThumbR = 10f;

    private bool _dragging;

    public float Min  { get; set { field = value; Invalidate(); } } = 0f;
    public float Max  { get; set { field = value; Invalidate(); } } = 1f;

    /// 0 = continuous (no snapping); any other positive value snaps to that interval.
    public float Step { get; set { field = value; } } = 0f;

    public float Value
    {
        get;
        set
        {
            float snapped = Snap(Math.Clamp(value, Min, Max));
            if (field == snapped) return;
            field = snapped;
            Invalidate();
            OnChanged?.Invoke(snapped);
        }
    }

    public Action<float>? OnChanged;

    public Slider()
    {
        MinWidth = 80f;
        OnPointerDown = e => { _dragging = true;  UpdateFromX(e.X); };
        OnPointerMove = e => { if (_dragging) UpdateFromX(e.X); };
        OnPointerUp   = _ => _dragging = false;
    }

    // ── Fluent ───────────────────────────────────────────────────────────────

    public Slider WithMin(float min)        { Min   = min;   return this; }
    public Slider WithMax(float max)        { Max   = max;   return this; }
    public Slider WithStep(float step)      { Step  = step;  return this; }
    public Slider WithValue(float value)    { Value = value; return this; }
    public Slider OnChange(Action<float> a) { OnChanged = a; return this; }

    // ── Binding ───────────────────────────────────────────────────────────────

    public Slider Bind(Bindable<float> source)
    {
        Value = source.Value;
        OnChanged      += v => source.Value = v;
        source.Changed += v => Value = v;
        return this;
    }

    public Slider BindInt(Bindable<int> source)
    {
        Value = source.Value;
        OnChanged      += v => source.Value = (int)MathF.Round(v);
        source.Changed += v => Value = v;
        return this;
    }

    // ── Layout ───────────────────────────────────────────────────────────────

    protected override Size MeasureCore(Size available)
        => new(float.IsNaN(Width) ? Math.Max(MinWidth, available.Width) : Width, ThumbR * 2);

    // ── Drawing ──────────────────────────────────────────────────────────────

    protected override void DrawCore(DrawContext ctx)
    {
        if (LayoutBounds.Width <= 0) return;

        float range  = Max - Min;
        float t      = range > 0f ? (Value - Min) / range : 0f;
        float cx0    = LayoutBounds.X + ThumbR;
        float cx1    = LayoutBounds.Right - ThumbR;
        float trackW = cx1 - cx0;
        float thumbX = cx0 + t * trackW;
        float trackY = LayoutBounds.CenterY - TrackH * 0.5f;

        ctx.FillRoundRect(new Rect(cx0, trackY, cx1 - cx0, TrackH), TrackH * 0.5f, ctx.Theme.Border);

        if (thumbX > cx0)
            ctx.FillRoundRect(new Rect(cx0, trackY, thumbX - cx0, TrackH), TrackH * 0.5f, ctx.Theme.Primary);

        var thumbColor = (IsPressed || IsHovered) ? ctx.Theme.PrimaryHover : ctx.Theme.Primary;
        ctx.FillCircle(thumbX, LayoutBounds.CenterY, ThumbR, thumbColor);
        ctx.StrokeCircle(thumbX, LayoutBounds.CenterY, ThumbR - 1f, SKColors.White, 2f);
    }

    // ── Private ──────────────────────────────────────────────────────────────

    private void UpdateFromX(float x)
    {
        float cx0    = LayoutBounds.X + ThumbR;
        float trackW = LayoutBounds.Width - ThumbR * 2f;
        if (trackW <= 0f) return;
        Value = Min + Math.Clamp((x - cx0) / trackW, 0f, 1f) * (Max - Min);
    }

    private float Snap(float raw)
        => Step > 0f ? MathF.Round(raw / Step) * Step : raw;
}
