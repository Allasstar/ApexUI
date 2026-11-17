namespace ApexUI.Layout;

public enum Orientation { Horizontal, Vertical }

/// Children in a line along Orientation, separated by Spacing. Each child gets its desired
/// length along the line and the panel's full thickness across it (the child's own alignment
/// applies inside). Leftover length is shared among children with Grow > 0, in proportion.
/// Row and Column are the horizontal and vertical forms.
public abstract class LinearPanel : Widget
{
    protected LinearPanel(Orientation orientation) => Orientation = orientation;

    public Orientation Orientation { get; }

    /// Gap between neighbouring children. Until set, SpacingSource, else DefaultSpacing (0 unless a subclass takes it from the theme).
    public float Spacing
    {
        get => !float.IsNaN(_spacing) ? _spacing : SpacingSource?.Invoke(Theme) ?? DefaultSpacing(Theme);
        set => SetLayoutProperty(ref _spacing, value);
    }
    private float _spacing = float.NaN;

    /// Takes the gap from the current theme, e.g. t => t.SpacingSm, so it follows theme changes.
    public Func<Theme, float>? SpacingSource { get; set => SetLayoutProperty(ref field, value); }

    protected virtual float DefaultSpacing(Theme theme) => 0f;

    private bool Horizontal => Orientation == Orientation.Horizontal;

    protected override Size MeasureCore(Size available)
    {
        float availMain  = Horizontal ? available.Width  : available.Height;
        float availCross = Horizontal ? available.Height : available.Width;
        (float main, float cross, _) = MeasureChildren(availMain, availCross);
        return Oriented(main, cross);
    }

    protected override void ArrangeCore(Rect r)
    {
        float finalMain  = Horizontal ? r.Width  : r.Height;
        float finalCross = Horizontal ? r.Height : r.Width;

        // Re-measured at the final thickness: a wrapping label is taller in a narrower column.
        (float used, _, float totalGrow) = MeasureChildren(finalMain, finalCross);

        float extra = totalGrow > 0f && !float.IsInfinity(finalMain) ? Math.Max(0f, finalMain - used) : 0f;
        float pos = Horizontal ? r.X : r.Y;
        bool first = true;
        foreach (var child in Children)
        {
            if (!child.IsVisible) continue;
            if (!first) pos += Spacing;
            first = false;

            float length = MainOf(child.DesiredSize);
            if (child.Grow > 0f) length += extra * child.Grow / totalGrow;
            child.Arrange(Horizontal
                ? new Rect(pos, r.Y, length, r.Height)
                : new Rect(r.X, pos, r.Width, length));
            pos += length;
        }
    }

    // Growing children are measured last, with what the others leave: a child that fills the length it
    // is offered (a text field) would otherwise starve the children after it.
    private (float Main, float Cross, float TotalGrow) MeasureChildren(float availMain, float availCross)
    {
        float main = 0f, cross = 0f, totalGrow = 0f;
        int visible = 0;
        foreach (var child in Children)
        {
            if (child.IsVisible) visible++;
        }

        if (visible > 1) main = Spacing * (visible - 1);
        for (int pass = 0; pass < 2; pass++)
        {
            foreach (var child in Children)
            {
                if (!child.IsVisible || (child.Grow > 0f) != (pass == 1)) continue;
                child.Measure(Oriented(Math.Max(0f, availMain - main), availCross));
                main += MainOf(child.DesiredSize);
                cross = Math.Max(cross, CrossOf(child.DesiredSize));
                totalGrow += child.Grow;
            }
        }

        return (main, cross, totalGrow);
    }

    private Size  Oriented(float main, float cross) => Horizontal ? new Size(main, cross) : new Size(cross, main);
    private float MainOf(Size s)  => Horizontal ? s.Width  : s.Height;
    private float CrossOf(Size s) => Horizontal ? s.Height : s.Width;
}
