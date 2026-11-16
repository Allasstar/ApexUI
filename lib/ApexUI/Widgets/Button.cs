namespace ApexUI.Widgets;

public enum ButtonVariant { Primary, Secondary, Ghost }

public class Button : Widget
{
    public string Text
    {
        get => _label.Text;
        set => _label.Text = value;
    }

    public ButtonVariant Variant
    {
        get;
        set => SetRenderProperty(ref field, value);
    } = ButtonVariant.Primary;

    public Action? OnPressed;

    private readonly Label _label;

    public Button(string text = "", Action? onPressed = null)
    {
        _label = new Label
        {
            Text             = text,
            HAlign           = HAlign.Center,
            VAlign           = VAlign.Center,
            IsHitTestVisible = false,
            ColorSource      = t => Variant == ButtonVariant.Primary ? t.OnPrimary : t.OnSurface,
        };
        AddChild(_label);
        OnPressed = onPressed;
        Padding = new Thickness(16f, 8f);
        CornerRadius = 8f;

        // Wire up click
        OnClick = _ => OnPressed?.Invoke();
    }

    public Button WithText(string text)    { Text = text; return this; }
    public Button WithVariant(ButtonVariant v) { Variant = v; return this; }
    public Button OnPress(Action action)   { OnPressed = action; return this; }

    protected override Size MeasureCore(Size available)
    {
        _label.Measure(new Size(available.Width - Padding.Horizontal,
            available.Height - Padding.Vertical));
        return new Size(
            _label.DesiredSize.Width  + Padding.Horizontal,
            _label.DesiredSize.Height + Padding.Vertical);
    }

    protected override void ArrangeCore(Rect finalRect)
        => _label.Arrange(finalRect.Deflate(Padding));

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

        if (bg != SKColor.Empty)
            ctx.FillRoundRect(LayoutBounds, CornerRadius, bg);

        if (Variant != ButtonVariant.Primary)
            ctx.StrokeRoundRect(LayoutBounds, CornerRadius, ctx.Theme.Border);
    }
}