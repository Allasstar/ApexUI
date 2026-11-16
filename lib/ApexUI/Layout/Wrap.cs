namespace ApexUI.Layout;

/// Children arranged left-to-right, wrapping to the next line when they overflow.
public class Wrap : Widget
{
    public float HorizontalSpacing { get; set => SetLayoutProperty(ref field, value); }
    public float VerticalSpacing   { get; set => SetLayoutProperty(ref field, value); }

    public Wrap(params Widget[] children)
    {
        foreach (var c in children) AddChild(c);
    }

    public Wrap WithSpacing(float horizontal, float vertical)
    {
        HorizontalSpacing = horizontal;
        VerticalSpacing   = vertical;
        return this;
    }

    protected override Size MeasureCore(Size available)
    {
        float maxWidth = float.IsPositiveInfinity(available.Width) ? float.MaxValue : available.Width;
        float width = 0f, height = 0f;
        int next = 0;
        bool first = true;
        while (NextLine(ref next, maxWidth, out _, out float lineWidth, out float lineHeight))
        {
            if (!first) height += VerticalSpacing;
            first  = false;
            width  = Math.Max(width, lineWidth);
            height += lineHeight;
        }
        return new Size(width, height);
    }

    protected override void ArrangeCore(Rect finalRect)
    {
        float y = finalRect.Y;
        int next = 0;
        while (NextLine(ref next, finalRect.Width, out int start, out _, out float lineHeight))
        {
            float x = finalRect.X;
            for (int i = start; i < next; i++)
            {
                var child = Children[i];
                if (!child.IsVisible) continue;
                child.Arrange(new Rect(x, y, child.DesiredSize.Width, lineHeight));
                x += child.DesiredSize.Width + HorizontalSpacing;
            }
            y += lineHeight + VerticalSpacing;
        }
    }

    // Measures the children of the line that starts at index and moves index past them.
    // Returns false when no visible child is left.
    private bool NextLine(ref int index, float maxWidth, out int start, out float lineWidth, out float lineHeight)
    {
        var children = Children;
        while (index < children.Count && !children[index].IsVisible) index++;
        start      = index;
        lineWidth  = 0f;
        lineHeight = 0f;
        if (index >= children.Count) return false;

        bool first = true;
        for (; index < children.Count; index++)
        {
            var child = children[index];
            if (!child.IsVisible) continue;
            child.Measure(new Size(maxWidth, float.PositiveInfinity));
            float w = child.DesiredSize.Width;
            if (!first && lineWidth + HorizontalSpacing + w > maxWidth) break;
            if (!first) lineWidth += HorizontalSpacing;
            first      = false;
            lineWidth += w;
            lineHeight = Math.Max(lineHeight, child.DesiredSize.Height);
        }
        return true;
    }
}
