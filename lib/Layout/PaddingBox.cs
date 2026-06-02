namespace ApexUI.Layout;

/// Wraps a single child with padding on all sides.
public class PaddingBox : Widget
{
    public PaddingBox(Widget child, Thickness padding)
    {
        Padding = padding;
        AddChild(child);
    }

    public PaddingBox(Widget child, float all)
        : this(child, new Thickness(all)) { }

    protected override Size MeasureCore(Size available)
    {
        var child = Children[0];
        var inner = new Size(
            Math.Max(0, available.Width  - Padding.Horizontal),
            Math.Max(0, available.Height - Padding.Vertical));
        child.Measure(inner);
        return new Size(
            child.DesiredSize.Width  + Padding.Horizontal,
            child.DesiredSize.Height + Padding.Vertical);
    }

    protected override void ArrangeCore(Rect finalRect)
    {
        var inner = finalRect.Deflate(Padding);
        Children[0].Measure(new Size(inner.Width, inner.Height));
        Children[0].Arrange(inner);
    }
}