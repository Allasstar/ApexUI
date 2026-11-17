namespace ApexUI.Widgets;

public enum SeparatorOrientation { Horizontal, Vertical }

public class Separator : Widget
{
    public SeparatorOrientation Orientation
    {
        get;
        set => SetLayoutProperty(ref field, value);
    } = SeparatorOrientation.Horizontal;

    public SKColor? Color { get; set => SetRenderProperty(ref field, value); }

    public Separator()
    {
        IsHitTestVisible = false;
    }

    public Separator AsHorizontal() { Orientation = SeparatorOrientation.Horizontal; return this; }
    public Separator AsVertical()   { Orientation = SeparatorOrientation.Vertical;   return this; }
    public Separator WithColor(SKColor color) { Color = color; return this; }

    // Asks for no length, so it never makes its panel longer (a menu wider, a toolbar taller);
    // stretch alignment, the default, fills the slot it is given.
    protected override Size MeasureCore(Size available)
        => Orientation == SeparatorOrientation.Horizontal ? new Size(0f, 1f) : new Size(1f, 0f);

    protected override void DrawCore(DrawContext ctx)
    {
        var color = Color ?? ctx.Theme.Border;
        var b     = ContentBounds;
        if (Orientation == SeparatorOrientation.Horizontal)
            ctx.DrawLine(b.X, b.CenterY, b.Right, b.CenterY, color);
        else
            ctx.DrawLine(b.CenterX, b.Y, b.CenterX, b.Bottom, color);
    }
}
