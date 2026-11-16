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

    // Takes no length when unconstrained (e.g. in a menu that sizes to its items); stretching fills the slot.
    protected override Size MeasureCore(Size available)
        => Orientation == SeparatorOrientation.Horizontal
            ? new Size(float.IsInfinity(available.Width) ? 0f : available.Width, 1f)
            : new Size(1f, float.IsInfinity(available.Height) ? 0f : available.Height);

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
