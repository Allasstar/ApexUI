namespace ApexUI.Layout;

/// Children arranged left-to-right with optional spacing.
public class Row : Widget
{
    public float Spacing
    {
        get;
        set { field = value; InvalidateLayout(); }
    } = 0f;

    public Row(params Widget[] children)
    {
        foreach (var c in children) AddChild(c);
    }

    public Row WithSpacing(float spacing) { Spacing = spacing; return this; }

    protected override Size MeasureCore(Size available)
    {
        float totalW = 0, maxH = 0;
        bool first = true;

        foreach (var child in Children)
        {
            if (!child.IsVisible) continue;
            if (!first) totalW += Spacing;

            var childAvail = new Size(Math.Max(0, available.Width - totalW), available.Height);
            child.Measure(childAvail);
            totalW += child.DesiredSize.Width;
            maxH    = Math.Max(maxH, child.DesiredSize.Height);
            first = false;
        }

        return new Size(totalW, maxH);
    }

    protected override void ArrangeCore(Rect finalRect)
    {
        float x = finalRect.X;
        bool first = true;

        foreach (var child in Children)
        {
            if (!child.IsVisible) continue;
            if (!first) x += Spacing;

            child.Measure(new Size(finalRect.Right - x, finalRect.Height));
            child.Arrange(new Rect(x, finalRect.Y, child.DesiredSize.Width, finalRect.Height));
            x += child.DesiredSize.Width;
            first = false;
        }
    }
}