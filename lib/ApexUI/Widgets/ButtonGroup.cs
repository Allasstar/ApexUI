namespace ApexUI.Widgets;

/// A row (or column) of related controls drawn on one shared rounded background, so a toolbar reads as
/// groups instead of a loose line of buttons. Ghost buttons, IconButtons and MenuButtons fit it best:
/// their hover and selected fills sit inside the group's background.
///
/// Usage:
///   var history = new ButtonGroup(undoButton, redoButton);
///   var zoom    = new ButtonGroup(zoomOut, zoomLabel, zoomIn).WithChildrenCentered();
public class ButtonGroup : LinearPanel
{
    public ButtonGroup(params Widget[] children) : this(Orientation.Horizontal, children) { }

    public ButtonGroup(Orientation orientation, params Widget[] children) : base(orientation)
    {
        BackgroundSource = t => t.Background;
        foreach (var child in children) AddChild(child);
    }

    /// Outline around the background. Null draws none; the default is the theme's BorderSubtle.
    public Func<Theme, SKColor>? OutlineSource { get; set => SetRenderProperty(ref field, value); } = t => t.BorderSubtle;

    public ButtonGroup Add(Widget child)
    {
        AddChild(child);
        return this;
    }

    /// Centres every child across the group (vertically in a row), so a label sits level with the buttons.
    public ButtonGroup WithChildrenCentered()
    {
        foreach (var child in Children)
        {
            if (Orientation == Orientation.Horizontal) child.VAlign = VAlign.Center;
            else child.HAlign = HAlign.Center;
        }
        return this;
    }

    protected override Thickness DefaultPadding(Theme theme) => new(theme.SpacingXs * 0.5f);
    protected override float DefaultCornerRadius(Theme theme) => theme.CornerRadiusControl;
    protected override float DefaultSpacing(Theme theme) => theme.SpacingXs * 0.5f;

    protected override void DrawCore(DrawContext ctx)
    {
        if (OutlineSource?.Invoke(ctx.Theme) is { } outline && outline.Alpha > 0)
            ctx.StrokeRoundRect(LayoutBounds, CornerRadius, outline);
    }
}
