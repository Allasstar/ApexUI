namespace ApexUI.Widgets;

public enum ButtonVariant { Primary, Secondary, Ghost }

public class Button : Widget
{
    public string Text
    {
        get;
        set { field = value; InvalidateLayout(); }
    } = "";

    public ButtonVariant Variant
    {
        get;
        set { field = value; Invalidate(); }
    } = ButtonVariant.Primary;

    public Action? OnPressed;

    private readonly Label _label;

    public Button(string text = "", Action? onPressed = null)
    {
        Text = text;
        OnPressed = onPressed;
        Padding = new Thickness(16f, 8f);
        CornerRadius = 8f;

        _label = new Label { HAlign = HAlign.Center, VAlign = VAlign.Center, IsHitTestVisible = false };
        AddChild(_label);

        // Wire up click
        OnClick = _ => OnPressed?.Invoke();
    }

    public Button WithText(string text)    { Text = text; return this; }
    public Button WithVariant(ButtonVariant v) { Variant = v; return this; }
    public Button OnPress(Action action)   { OnPressed = action; return this; }

    protected override Size MeasureCore(Size available)
    {
        _label.Text = Text;
        _label.Measure(new Size(available.Width - Padding.Horizontal,
            available.Height - Padding.Vertical));
        return new Size(
            _label.DesiredSize.Width  + Padding.Horizontal,
            _label.DesiredSize.Height + Padding.Vertical);
    }

    protected override void ArrangeCore(Rect finalRect)
    {
        _label.Text = Text;
        _label.Arrange(finalRect.Deflate(Padding));
    }

    protected override void DrawCore(DrawContext ctx)
    {
        var bg = Variant switch
        {
            ButtonVariant.Primary  => IsPressed ? ctx.Theme.PrimaryHover
                : IsHovered ? ctx.Theme.PrimaryHover
                : ctx.Theme.Primary,
            ButtonVariant.Secondary => IsPressed ? ctx.Theme.SurfacePressed
                : IsHovered  ? ctx.Theme.SurfaceHover
                : ctx.Theme.Surface,
            ButtonVariant.Ghost    => IsPressed ? ctx.Theme.SurfacePressed
                : IsHovered  ? ctx.Theme.SurfaceHover
                : SKColor.Empty,
            _ => ctx.Theme.Primary
        };

        var textColor = Variant == ButtonVariant.Primary
            ? ctx.Theme.OnPrimary
            : ctx.Theme.OnSurface;

        if (bg != SKColor.Empty)
            ctx.FillRoundRect(LayoutBounds, CornerRadius, bg);

        if (Variant != ButtonVariant.Primary)
            ctx.StrokeRoundRect(LayoutBounds, CornerRadius, ctx.Theme.Border);

        // Update label color
        _label.Color = textColor;
    }
}