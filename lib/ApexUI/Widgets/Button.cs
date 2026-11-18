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

    /// As tall as an IconButton, with less side padding: for text buttons in a toolbar row of icon buttons,
    /// which then keeps its height whether or not the text button is shown.
    public bool IsCompact
    {
        get;
        set => SetLayoutProperty(ref field, value);
    }

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
            ColorSource      = t => !IsEnabledInTree ? t.OnSurfaceMuted : Variant == ButtonVariant.Primary ? t.OnPrimary : t.OnSurface,
        };
        AddChild(_label);
        Pressed   += onPressed;
        Focusable = true;
    }

    protected override Thickness DefaultPadding(Theme theme)
    {
        if (!IsCompact) return theme.ButtonPadding;
        float iconHeight = theme.IconSize + theme.IconButtonPadding.Vertical;
        float vertical = Math.Max(0f, (iconHeight - theme.FontSizeBody * theme.LineHeight) / 2f);
        return new Thickness(theme.SpacingSm, vertical);
    }
    protected override float DefaultCornerRadius(Theme theme) => theme.CornerRadiusControl;

    public Button WithText(string text)    { Text = text; return this; }
    public Button WithVariant(ButtonVariant v) { Variant = v; return this; }
    public Button AsCompact()              { IsCompact = true; return this; }
    public Button OnPress(Action action)   { Pressed += action; return this; }

    /// Runs command when pressed and follows its enabled state while on screen.
    public Button WithCommand(Command command)
    {
        Pressed += () => command.Execute();
        IsEnabled = command.IsEnabled;
        Observe(command.Enabled, enabled => IsEnabled = enabled);
        return this;
    }

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
        var bg = !IsEnabledInTree
            ? (Variant == ButtonVariant.Ghost ? SKColor.Empty : ctx.Theme.Surface)
            : Variant switch
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

        if (Variant != ButtonVariant.Primary || !IsEnabledInTree)
            ctx.StrokeRoundRect(LayoutBounds, radius, ctx.Theme.Border);
    }
}