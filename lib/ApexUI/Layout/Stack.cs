namespace ApexUI.Layout;

/// Children all occupy the same space, drawn in order (last = on top).
public class Stack : Widget
{
    public Stack(params Widget[] children)
    {
        foreach (var c in children) AddChild(c);
    }

    protected override Size MeasureCore(Size available)
    {
        float w = 0, h = 0;
        foreach (var child in Children)
        {
            child.Measure(available);
            w = Math.Max(w, child.DesiredSize.Width);
            h = Math.Max(h, child.DesiredSize.Height);
        }
        return new Size(w, h);
    }

    protected override void ArrangeCore(Rect finalRect)
    {
        foreach (var child in Children)
        {
            child.Measure(new Size(finalRect.Width, finalRect.Height));
            child.Arrange(finalRect);
        }
    }
}