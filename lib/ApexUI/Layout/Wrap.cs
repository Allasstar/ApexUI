namespace ApexUI.Layout;

/// Children arranged left-to-right, wrapping to the next line when they overflow.
public class Wrap : Widget
{
    public float HorizontalSpacing { get; set { field = value; InvalidateLayout(); } }
    public float VerticalSpacing   { get; set { field = value; InvalidateLayout(); } }

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
        float avW = float.IsPositiveInfinity(available.Width) ? float.MaxValue : available.Width;
        var lines = BuildLines(avW);
        if (lines.Count == 0) return Size.Zero;

        float maxW = 0f;
        foreach (var (items, _) in lines)
        {
            float lw = 0f;
            foreach (var c in items) lw += c.DesiredSize.Width;
            lw += Math.Max(0, items.Count - 1) * HorizontalSpacing;
            maxW = Math.Max(maxW, lw);
        }

        float totalH = 0f;
        for (int i = 0; i < lines.Count; i++)
        {
            if (i > 0) totalH += VerticalSpacing;
            totalH += lines[i].lineHeight;
        }

        return new Size(maxW, totalH);
    }

    protected override void ArrangeCore(Rect finalRect)
    {
        var lines = BuildLines(finalRect.Width);
        float y = finalRect.Y;

        foreach (var (items, lineH) in lines)
        {
            float x = finalRect.X;
            foreach (var child in items)
            {
                child.Arrange(new Rect(x, y, child.DesiredSize.Width, lineH));
                x += child.DesiredSize.Width + HorizontalSpacing;
            }
            y += lineH + VerticalSpacing;
        }
    }

    // Groups visible children into lines based on available width.
    // Each call to child.Measure produces the DesiredSize referenced in MeasureCore/ArrangeCore.
    private List<(List<Widget> items, float lineHeight)> BuildLines(float availableW)
    {
        var lines   = new List<(List<Widget>, float)>();
        var curLine = new List<Widget>();
        float lineX = 0f, lineH = 0f;

        foreach (var child in Children)
        {
            if (!child.IsVisible) continue;
            child.Measure(new Size(availableW, float.PositiveInfinity));
            float cw = child.DesiredSize.Width;
            float ch = child.DesiredSize.Height;

            if (curLine.Count > 0 && lineX + HorizontalSpacing + cw > availableW)
            {
                lines.Add((curLine, lineH));
                curLine = new List<Widget>();
                lineX   = 0f;
                lineH   = 0f;
            }

            if (curLine.Count > 0) lineX += HorizontalSpacing;
            lineX += cw;
            lineH  = Math.Max(lineH, ch);
            curLine.Add(child);
        }

        if (curLine.Count > 0)
            lines.Add((curLine, lineH));

        return lines;
    }
}
