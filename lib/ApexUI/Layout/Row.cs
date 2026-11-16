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

    public Row Add(Widget child) { AddChild(child); return this; }

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
        // Fast path: no Spacers — original single-pass preserving remaining-width semantics.
        bool hasSpacers = false;
        foreach (var child in Children)
            if (child.IsVisible && child is Spacer) { hasSpacers = true; break; }

        if (!hasSpacers)
        {
            float x = finalRect.X;
            bool first = true;
            foreach (var child in Children)
            {
                if (!child.IsVisible) continue;
                if (!first) x += Spacing;
                child.Measure(new Size(Math.Max(0f, finalRect.Right - x), finalRect.Height));
                child.Arrange(new Rect(x, finalRect.Y, child.DesiredSize.Width, finalRect.Height));
                x += child.DesiredSize.Width;
                first = false;
            }
            return;
        }

        // Two-pass Spacer layout: measure fixed children first, then distribute remainder.
        int visible = 0, spacerCount = 0;
        float fixedW = 0f;
        foreach (var child in Children)
        {
            if (!child.IsVisible) continue;
            visible++;
            if (child is Spacer) { spacerCount++; continue; }
            child.Measure(new Size(finalRect.Width, finalRect.Height));
            fixedW += child.DesiredSize.Width;
        }
        float usedSpacing = visible > 1 ? (visible - 1) * Spacing : 0f;
        float spacerW = Math.Max(0f, finalRect.Width - fixedW - usedSpacing) / spacerCount;

        float x2 = finalRect.X;
        bool first2 = true;
        foreach (var child in Children)
        {
            if (!child.IsVisible) continue;
            if (!first2) x2 += Spacing;
            float w = child is Spacer ? spacerW : child.DesiredSize.Width;
            child.Arrange(new Rect(x2, finalRect.Y, w, finalRect.Height));
            x2 += w;
            first2 = false;
        }
    }
}