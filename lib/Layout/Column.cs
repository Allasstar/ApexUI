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
        // Fast path: no Spacers — original single-pass preserving remaining-height semantics.
        bool hasSpacers = false;
        foreach (var child in Children)
            if (child.IsVisible && child is Spacer) { hasSpacers = true; break; }

        if (!hasSpacers)
        {
            float y = finalRect.Y;
            bool first = true;
            foreach (var child in Children)
            {
                if (!child.IsVisible) continue;
                if (!first) y += Spacing;
                child.Measure(new Size(finalRect.Width, Math.Max(0f, finalRect.Bottom - y)));
                child.Arrange(new Rect(finalRect.X, y, finalRect.Width, child.DesiredSize.Height));
                y += child.DesiredSize.Height;
                first = false;
            }
            return;
        }

        // Two-pass Spacer layout: measure fixed children first, then distribute remainder.
        int visible = 0, spacerCount = 0;
        float fixedH = 0f;
        foreach (var child in Children)
        {
            if (!child.IsVisible) continue;
            visible++;
            if (child is Spacer) { spacerCount++; continue; }
            child.Measure(new Size(finalRect.Width, finalRect.Height));
            fixedH += child.DesiredSize.Height;
        }
        float usedSpacing = visible > 1 ? (visible - 1) * Spacing : 0f;
        float spacerH = Math.Max(0f, finalRect.Height - fixedH - usedSpacing) / spacerCount;

        float y2 = finalRect.Y;
        bool first2 = true;
        foreach (var child in Children)
        {
            if (!child.IsVisible) continue;
            if (!first2) y2 += Spacing;
            float h = child is Spacer ? spacerH : child.DesiredSize.Height;
            child.Arrange(new Rect(finalRect.X, y2, finalRect.Width, h));
            y2 += h;
            first2 = false;
        }
    }
}