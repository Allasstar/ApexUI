namespace ApexUI.Widgets;

public class Toggle : Widget
{
    private const float TrackW   = 44f;
    private const float TrackH   = 24f;
    private const float ThumbR   = 10f;
    private const float LabelGap = 8f;

    public bool IsChecked
    {
        get;
        set { field = value; Invalidate(); }
    }

    public string Label
    {
        get;
        set { field = value; InvalidateLayout(); }
    } = "";

    public Action<bool>? OnChanged;

    public Toggle(bool isChecked = false, string label = "", Action<bool>? onChanged = null)
    {
        IsChecked = isChecked;
        Label     = label;
        OnChanged = onChanged;
        OnClick   = _ => Flip();
    }

    // ── Fluent ───────────────────────────────────────────────────────────────

    public Toggle WithLabel(string label)        { Label = label; return this; }
    public Toggle WithChecked(bool value)        { IsChecked = value; return this; }
    public Toggle OnChange(Action<bool> action)  { OnChanged = action; return this; }

    public Toggle Bind(Bindable<bool> source)
    {
        IsChecked  = source.Value;
        OnChanged += v => source.Value = v;
        source.Changed += v => { IsChecked = v; Invalidate(); };
        return this;
    }

    // ── Layout ───────────────────────────────────────────────────────────────

    protected override Size MeasureCore(Size available)
    {
        float w = TrackW;
        if (!string.IsNullOrEmpty(Label))
        {
            string family = Application.Current?.FontFamily ?? "Segoe UI";
            using var font = new SKFont(SKTypeface.FromFamilyName(family, SKFontStyle.Normal), 14f);
            w += LabelGap + font.MeasureText(Label) + 2f;
        }
        return new Size(w, TrackH);
    }

    // ── Drawing ──────────────────────────────────────────────────────────────

    protected override void DrawCore(DrawContext ctx)
    {
        float ty = LayoutBounds.Y + (LayoutBounds.Height - TrackH) * 0.5f;

        var trackColor = IsEnabled
            ? (IsChecked ? ctx.Theme.Primary : ctx.Theme.Border)
            : ctx.Theme.SurfaceHover;
        ctx.FillRoundRect(new Rect(LayoutBounds.X, ty, TrackW, TrackH), TrackH * 0.5f, trackColor);

        float thumbX = IsChecked
            ? LayoutBounds.X + TrackW - ThumbR - 2f
            : LayoutBounds.X + ThumbR + 2f;
        ctx.FillCircle(thumbX, ty + TrackH * 0.5f, ThumbR, SKColors.White);

        // Optional label
        if (!string.IsNullOrEmpty(Label))
        {
            using var font  = ctx.MakeTextFont(14f);
            using var paint = ctx.MakeTextPaint(IsEnabled ? ctx.Theme.OnSurface : ctx.Theme.OnSurfaceMuted);
            var m = font.Metrics;
            float lx = LayoutBounds.X + TrackW + LabelGap;
            float ly = LayoutBounds.Y + (LayoutBounds.Height - (m.Descent - m.Ascent)) * 0.5f - m.Ascent;
            ctx.Canvas.DrawText(Label, lx, ly, SKTextAlign.Left, font, paint);
        }
    }

    // ── Private ──────────────────────────────────────────────────────────────

    private void Flip()
    {
        IsChecked = !IsChecked;
        OnChanged?.Invoke(IsChecked);
    }
}