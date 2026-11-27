namespace ApexUI.Widgets;

public class Toggle : Widget
{
    public bool IsChecked
    {
        get;
        set { if (SetRenderProperty(ref field, value)) CheckedChanged?.Invoke(value); }
    }

    public string Label
    {
        get;
        set => SetLayoutProperty(ref field, value);
    } = "";

    public event Action<bool>? CheckedChanged;

    public Toggle(bool isChecked = false, string label = "", Action<bool>? onChanged = null)
    {
        IsChecked = isChecked;
        Label     = label;
        CheckedChanged += onChanged;
        Focusable = true;
    }

    protected override float DefaultCornerRadius(Theme theme) => theme.ToggleHeight * 0.5f;

    protected override void OnClick(PointerEventArgs e)
    {
        e.Handled = true;
        Flip();
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        if (!Activation.IsActivationKey(e)) return;
        e.Handled = true;
        Flip();
    }

    // ── Fluent ───────────────────────────────────────────────────────────────

    public Toggle WithLabel(string label)        { Label = label; return this; }
    public Toggle WithChecked(bool value)        { IsChecked = value; return this; }
    public Toggle OnChange(Action<bool> action)  { CheckedChanged += action; return this; }
    public Toggle WithReset(Func<bool> isDefault, Action reset, Action<ContextMenu>? extendMenu = null)
    {
        ResetHook.Attach(this, isDefault, reset, extendMenu: extendMenu);
        return this;
    }

    public Toggle Bind(Bindable<bool> source)
    {
        IsChecked  = source.Value;
        CheckedChanged += v => source.Value = v;
        Observe(source, v => IsChecked = v);
        return this;
    }

    // ── Layout ───────────────────────────────────────────────────────────────

    protected override Size MeasureCore(Size available)
    {
        var   t = Theme;
        float w = t.ToggleWidth;
        if (!string.IsNullOrEmpty(Label))
        {
            using var font = LayoutFont.Get(Host, t.FontSizeBody);
            w += t.SpacingSm + font.Font.MeasureText(Label) + 2f;
        }
        return new Size(w, t.ToggleHeight);
    }

    // ── Drawing ──────────────────────────────────────────────────────────────

    protected override void DrawCore(DrawContext ctx)
    {
        var   t      = ctx.Theme;
        float trackW = t.ToggleWidth;
        float trackH = t.ToggleHeight;
        float thumbR = trackH * 0.5f - 2f;
        float ty     = LayoutBounds.Y + (LayoutBounds.Height - trackH) * 0.5f;

        var trackColor = IsEnabled
            ? (IsChecked ? t.Primary : t.Border)
            : t.SurfaceHover;
        ctx.FillRoundRect(new Rect(LayoutBounds.X, ty, trackW, trackH), trackH * 0.5f, trackColor);

        float thumbX = IsChecked
            ? LayoutBounds.X + trackW - thumbR - 2f
            : LayoutBounds.X + thumbR + 2f;
        ctx.FillCircle(thumbX, ty + trackH * 0.5f, thumbR, t.Knob);

        if (!string.IsNullOrEmpty(Label))
        {
            float offset = trackW + t.SpacingSm;
            ctx.DrawText(Label,
                new Rect(LayoutBounds.X + offset, LayoutBounds.Y, LayoutBounds.Width - offset, LayoutBounds.Height),
                IsEnabled ? t.OnSurface : t.OnSurfaceMuted, t.FontSizeBody);
        }
    }

    // ── Private ──────────────────────────────────────────────────────────────

    private void Flip()
    {
        IsChecked = !IsChecked;
    }
}