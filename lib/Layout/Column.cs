namespace ApexUI.Layout;

/// Children arranged top-to-bottom with optional spacing.
public class Column : Widget
{
    public float Spacing
    {
        get;
        set { field = value; InvalidateLayout(); }
    } = 0f;

    public Column(params Widget[] children)
    {
        foreach (var c in children) AddChild(c);
    }

    public Column WithSpacing(float spacing) { Spacing = spacing; return this; }

    protected override Size MeasureCore(Size available)
    {
        float totalH = 0, maxW = 0;
        bool first = true;

        foreach (var child in Children)
        {
            if (!child.IsVisible) continue;
            if (!first) totalH += Spacing;

            var childAvail = new Size(available.Width, Math.Max(0, available.Height - totalH));
            child.Measure(childAvail);
            totalH += child.DesiredSize.Height;
            maxW    = Math.Max(maxW, child.DesiredSize.Width);
            first = false;
        }

        return new Size(maxW, totalH);
    }

    protected override void ArrangeCore(Rect finalRect)
    {
        float y = finalRect.Y;
        bool first = true;

        foreach (var child in Children)
        {
            if (!child.IsVisible) continue;
            if (!first) y += Spacing;

            child.Measure(new Size(finalRect.Width, finalRect.Bottom - y));
            child.Arrange(new Rect(finalRect.X, y, finalRect.Width, child.DesiredSize.Height));
            y += child.DesiredSize.Height;
            first = false;
        }
    }
}