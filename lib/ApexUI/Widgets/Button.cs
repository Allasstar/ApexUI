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

    public event Action? Pressed;

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
        Pressed   += onPressed;
        Focusable = true;
    }

    protected override Thickness DefaultPadding(Theme theme) => theme.ButtonPadding;
    protected override float DefaultCornerRadius(Theme theme) => theme.CornerRadiusControl;

    public Button WithText(string text)    { Text = text; return this; }
    public Button WithVariant(ButtonVariant v) { Variant = v; return this; }
    public Button OnPress(Action action)   { Pressed += action; return this; }

    protected override void OnClick(PointerEventArgs e)
    {
        e.Handled = true;
        Pressed?.Invoke();
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        if (!Activation.IsActivationKey(e)) return;
        e.Handled = true;
        Pressed?.Invoke();
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

        float radius = CornerRadius;
        if (bg != SKColor.Empty)
            ctx.FillRoundRect(LayoutBounds, radius, bg);

        if (Variant != ButtonVariant.Primary)
            ctx.StrokeRoundRect(LayoutBounds, radius, ctx.Theme.Border);
    }
}