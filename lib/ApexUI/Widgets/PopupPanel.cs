namespace ApexUI.Widgets;

/// The surface behind floating content (tooltips, toasts, picker popups): Surface fill, themed padding
/// and corner radius. Overlay draws the shadow around it.
internal sealed class PopupPanel : Widget
{
    private readonly Func<Theme, Thickness> _padding;
    private readonly Func<Theme, float> _radius;

    public PopupPanel(Widget content, Func<Theme, Thickness> padding, Func<Theme, float> radius)
    {
        _padding = padding;
        _radius = radius;
        BackgroundSource = t => t.Surface;
        AddChild(content);
    }

    protected override Thickness DefaultPadding(Theme theme) => _padding(theme);

    protected override float DefaultCornerRadius(Theme theme) => _radius(theme);
}
