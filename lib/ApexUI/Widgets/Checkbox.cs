namespace ApexUI.Widgets;

public class Checkbox : Widget
{
    public bool IsChecked
    {
        get;
        set => SetRenderProperty(ref field, value);
    }

    public string Label
    {
        get;
        set => SetLayoutProperty(ref field, value);
    } = "";

    public Action<bool>? OnChanged;

    public Checkbox(bool isChecked = false, string label = "", Action<bool>? onChanged = null)
    {
        IsChecked = isChecked;
        Label     = label;
        OnChanged = onChanged;
        Focusable = true;
    }

    protected override void OnClick(PointerEventArgs e)
    {
        e.Handled = true;
        Toggle();
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        if (!Activation.IsActivationKey(e)) return;
        e.Handled = true;
        Toggle();
    }

    // ── Fluent ───────────────────────────────────────────────────────────────

    public Checkbox WithLabel(string label)       { Label     = label; return this; }
    public Checkbox WithChecked(bool value)       { IsChecked = value; return this; }
    public Checkbox OnChange(Action<bool> action) { OnChanged = action; return this; }

    public Checkbox Bind(Bindable<bool> source)
    {
        IsChecked       = source.Value;
        OnChanged      += v => source.Value = v;
        Observe(source, v => IsChecked = v);
        return this;
    }

    // ── Layout ───────────────────────────────────────────────────────────────

    protected override Size MeasureCore(Size available)
    {
        var   t = Theme;
        float w = t.CheckboxSize;
        if (!string.IsNullOrEmpty(Label))
        {
            using var font = LayoutFont.Get(Host, t.FontSizeBody);
            w += t.SpacingSm + font.Font.MeasureText(Label) + 2f;
        }
        return new Size(w, t.CheckboxSize);
    }

    // ── Drawing ──────────────────────────────────────────────────────────────

    protected override void DrawCore(DrawContext ctx)
    {
        var   t       = ctx.Theme;
        float size    = t.CheckboxSize;
        float cy      = LayoutBounds.CenterY;
        float cx      = LayoutBounds.X + size * 0.5f;
        var   boxRect = new Rect(LayoutBounds.X, cy - size * 0.5f, size, size);
        float r       = t.CornerRadiusSmall;

        if (IsChecked && IsEnabled)
        {
            ctx.FillRoundRect(boxRect, r, t.Primary);
            ctx.DrawCheckmark(cx, cy, size * 0.35f, t.OnPrimary);
        }
        else
        {
            ctx.FillRoundRect(boxRect, r, t.Surface);
            ctx.StrokeRoundRect(boxRect, r, IsEnabled ? t.Border : t.SurfaceHover, 1.5f);
            if (IsChecked) // disabled + checked
                ctx.DrawCheckmark(cx, cy, size * 0.35f, t.Border);
        }

        if ((IsHovered || IsPressed) && IsEnabled)
            ctx.FillRoundRect(boxRect, r, t.PrimaryHoverGhost);

        if (!string.IsNullOrEmpty(Label))
        {
            float offset = size + t.SpacingSm;
            ctx.DrawText(Label,
                new Rect(LayoutBounds.X + offset, LayoutBounds.Y, LayoutBounds.Width - offset, LayoutBounds.Height),
                IsEnabled ? t.OnSurface : t.OnSurfaceMuted, t.FontSizeBody);
        }
    }

    // ── Private ───────────────────────────────────────────────────────────────

    private void Toggle()
    {
        IsChecked = !IsChecked;
        OnChanged?.Invoke(IsChecked);
    }
}
