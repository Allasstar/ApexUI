namespace ApexUI.Widgets;

public enum SeparatorOrientation { Horizontal, Vertical }

public class Separator : Widget
{
    public SeparatorOrientation Orientation
    {
        get;
        set { field = value; InvalidateLayout(); }
    } = SeparatorOrientation.Horizontal;

    public SKColor? Color { get; set { field = value; Invalidate(); } }

    public Separator()
    {
        IsHitTestVisible = false;
    }

    public Separator AsHorizontal() { Orientation = SeparatorOrientation.Horizontal; return this; }
    public Separator AsVertical()   { Orientation = SeparatorOrientation.Vertical;   return this; }
    public Separator WithColor(SKColor color) { Color = color; return this; }

    protected override Size MeasureCore(Size available)
        => Orientation == SeparatorOrientation.Horizontal
            ? new Size(available.Width, 1f)
            : new Size(1f, available.Height);

    protected override void DrawCore(DrawContext ctx)
    {
        var color = Color ?? ctx.Theme.Border;
        if (Orientation == SeparatorOrientation.Horizontal)
            ctx.DrawLine(LayoutBounds.X, LayoutBounds.CenterY, LayoutBounds.Right, LayoutBounds.CenterY, color);
        else
            ctx.DrawLine(LayoutBounds.CenterX, LayoutBounds.Y, LayoutBounds.CenterX, LayoutBounds.Bottom, color);
    }
}
