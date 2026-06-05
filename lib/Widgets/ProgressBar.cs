namespace ApexUI.Widgets;

public enum ProgressBarVariant { Primary, Success, Warning, Danger }

public class ProgressBar : Widget
{
    private const float TrackH = 8f;

    public float Value
    {
        get;
        set { field = Math.Clamp(value, 0f, 1f); Invalidate(); }
    } = 0f;

    public ProgressBarVariant Variant
    {
        get;
        set { field = value; Invalidate(); }
    } = ProgressBarVariant.Primary;

    public ProgressBar()
    {
        MinWidth = 80f;
        IsHitTestVisible = false;
    }

    public ProgressBar WithValue(float value)             { Value   = value; return this; }
    public ProgressBar WithVariant(ProgressBarVariant v)  { Variant = v;     return this; }

    public ProgressBar Bind(Bindable<float> source)
    {
        Value = source.Value;
        source.Changed += v => Value = v;
        return this;
    }

    protected override Size MeasureCore(Size available)
        => new(float.IsNaN(Width) ? Math.Max(MinWidth, available.Width) : Width, TrackH);

    protected override void DrawCore(DrawContext ctx)
    {
        if (LayoutBounds.Width <= 0) return;

        float rx = LayoutBounds.Height * 0.5f;
        ctx.FillRoundRect(LayoutBounds, rx, ctx.Theme.Surface);
        ctx.StrokeRoundRect(LayoutBounds, rx, ctx.Theme.Border);

        if (Value > 0f)
        {
            var fillColor = Variant switch
            {
                ProgressBarVariant.Success => ctx.Theme.Success,
                ProgressBarVariant.Warning => ctx.Theme.Warning,
                ProgressBarVariant.Danger  => ctx.Theme.Danger,
                _                          => ctx.Theme.Primary,
            };
            ctx.FillRoundRect(new Rect(LayoutBounds.X, LayoutBounds.Y, LayoutBounds.Width * Value, LayoutBounds.Height), rx, fillColor);
        }
    }
}
