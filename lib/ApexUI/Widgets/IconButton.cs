namespace ApexUI.Widgets;

/// A borderless button showing only an Icon (toolbars, list rows). Wrap it in a Tooltip to name it.
public class IconButton : Widget
{
    public event Action? Pressed;

    public IconButton(Icon icon, Action? onPressed = null)
    {
        Icon             = icon;
        icon.ColorSource = t => IsEnabled ? t.OnSurface : t.OnSurfaceMuted;
        icon.HAlign      = HAlign.Center;
        icon.VAlign      = VAlign.Center;
        AddChild(icon);
        Pressed   += onPressed;
        Focusable = true;
    }

    public IconButton(string pathData, Action? onPressed = null) : this(new Icon(pathData), onPressed) { }

    public Icon Icon { get; }

    public IconButton OnPress(Action action) { Pressed += action; return this; }

    /// Runs command when pressed and follows its enabled state while on screen.
    public IconButton WithCommand(Command command)
    {
        Pressed += () => command.Execute();
        IsEnabled = command.IsEnabled;
        Observe(command.Enabled, enabled => IsEnabled = enabled);
        return this;
    }

    protected override Thickness DefaultPadding(Theme theme) => theme.IconButtonPadding;
    protected override float DefaultCornerRadius(Theme theme) => theme.CornerRadiusControl;

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
        if (!IsEnabled) return;
        if (IsPressed)      ctx.FillRoundRect(LayoutBounds, CornerRadius, ctx.Theme.SurfacePressed);
        else if (IsHovered) ctx.FillRoundRect(LayoutBounds, CornerRadius, ctx.Theme.SurfaceHover);
    }
}
