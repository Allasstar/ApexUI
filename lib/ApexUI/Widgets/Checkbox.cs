namespace ApexUI.Widgets;

public class Checkbox : Widget
{
    private const float BoxSize = 18f;
    private const float Gap     = 8f;

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
        float w = BoxSize;
        if (!string.IsNullOrEmpty(Label))
        {
            using var font = LayoutFont.Get(Host, 14f);
            w += Gap + font.Font.MeasureText(Label) + 2f;
        }
        return new Size(w, BoxSize);
    }

    // ── Drawing ──────────────────────────────────────────────────────────────

    protected override void DrawCore(DrawContext ctx)
    {
        float cy      = LayoutBounds.CenterY;
        float cx      = LayoutBounds.X + BoxSize * 0.5f;
        var   boxRect = new Rect(LayoutBounds.X, cy - BoxSize * 0.5f, BoxSize, BoxSize);
        float r       = ctx.Theme.CornerRadiusSm;
        
        if (IsChecked && IsEnabled)
        {
            ctx.FillRoundRect(boxRect, r, ctx.Theme.Primary);
            ctx.DrawCheckmark(cx, cy, BoxSize * 0.35f, ctx.Theme.OnPrimary);
        }
        else
        {
            ctx.FillRoundRect(boxRect, r, ctx.Theme.Surface);
            ctx.StrokeRoundRect(boxRect, r, IsEnabled ? ctx.Theme.Border : ctx.Theme.SurfaceHover, 1.5f);
            if (IsChecked) // disabled + checked
                ctx.DrawCheckmark(cx, cy, BoxSize * 0.35f, ctx.Theme.Border);
        }
        
        if ((IsHovered || IsPressed) && IsEnabled)
        {
            ctx.FillRoundRect(boxRect, r, ctx.Theme.PrimaryHoverGhost);
        }

        if (!string.IsNullOrEmpty(Label))
        {
            float tx = LayoutBounds.X + BoxSize + Gap;
            ctx.DrawText(Label,
                new Rect(tx, LayoutBounds.Y, LayoutBounds.Width - BoxSize - Gap, LayoutBounds.Height),
                IsEnabled ? ctx.Theme.OnSurface : ctx.Theme.OnSurfaceMuted, 14f);
        }
    }

    // ── Private ───────────────────────────────────────────────────────────────

    private void Toggle()
    {
        IsChecked = !IsChecked;
        OnChanged?.Invoke(IsChecked);
    }
}
