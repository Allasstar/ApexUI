namespace ApexUI.Layout;

/// Children positioned at explicit X/Y coordinates relative to the Canvas origin.
/// Size of each child is its natural DesiredSize unless explicit W/H is provided.
public class Canvas : Widget
{
    private record struct Placement(float X, float Y, float W, float H);
    private readonly Dictionary<Widget, Placement> _placements = new();

    /// Add a child at (x, y); child sizes itself naturally.
    public Canvas Add(Widget child, float x, float y)
        => AddAt(child, x, y, float.NaN, float.NaN);

    /// Add a child at (x, y) with an explicit size.
    public Canvas Add(Widget child, float x, float y, float width, float height)
        => AddAt(child, x, y, width, height);

    /// Move an already-added child to a new position.
    public Canvas Move(Widget child, float x, float y)
    {
        if (_placements.TryGetValue(child, out var p))
        {
            _placements[child] = p with { X = x, Y = y };
            InvalidateLayout();
        }
        return this;
    }

    /// Remove a child from the canvas.
    public new Canvas RemoveChild(Widget child)
    {
        _placements.Remove(child);
        base.RemoveChild(child);
        return this;
    }

    protected override Size MeasureCore(Size available)
    {
        float maxX = 0f, maxY = 0f;
        foreach (var child in Children)
        {
            if (!child.IsVisible || !_placements.TryGetValue(child, out var p)) continue;
            child.Measure(new Size(
                float.IsNaN(p.W) ? float.PositiveInfinity : p.W,
                float.IsNaN(p.H) ? float.PositiveInfinity : p.H));
            float rw = float.IsNaN(p.W) ? child.DesiredSize.Width  : p.W;
            float rh = float.IsNaN(p.H) ? child.DesiredSize.Height : p.H;
            maxX = Math.Max(maxX, p.X + rw);
            maxY = Math.Max(maxY, p.Y + rh);
        }
        return new Size(maxX, maxY);
    }

    protected override void ArrangeCore(Rect finalRect)
    {
        foreach (var child in Children)
        {
            if (!child.IsVisible || !_placements.TryGetValue(child, out var p)) continue;
            float rw = float.IsNaN(p.W) ? child.DesiredSize.Width  : p.W;
            float rh = float.IsNaN(p.H) ? child.DesiredSize.Height : p.H;
            child.Arrange(new Rect(finalRect.X + p.X, finalRect.Y + p.Y, rw, rh));
        }
    }

    private Canvas AddAt(Widget child, float x, float y, float w, float h)
    {
        _placements[child] = new Placement(x, y, w, h);
        AddChild(child);
        return this;
    }
}
