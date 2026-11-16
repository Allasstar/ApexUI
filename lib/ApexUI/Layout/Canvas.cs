namespace ApexUI.Layout;

/// Children positioned at explicit X/Y coordinates relative to the Canvas origin.
/// Size of each child is its natural DesiredSize unless explicit W/H is provided.
public class Canvas : Widget
{
    private sealed class Placement(float x, float y, float width, float height)
    {
        public float X = x, Y = y;
        public readonly float Width = width, Height = height;
    }

    /// Add a child at (x, y); child sizes itself naturally.
    public Canvas Add(Widget child, float x, float y)
        => AddAt(child, x, y, float.NaN, float.NaN);

    /// Add a child at (x, y) with an explicit size.
    public Canvas Add(Widget child, float x, float y, float width, float height)
        => AddAt(child, x, y, width, height);

    /// Move an already-added child to a new position.
    public Canvas Move(Widget child, float x, float y)
    {
        if (child.Parent == this && GetParentData(child) is Placement p)
        {
            p.X = x;
            p.Y = y;
            InvalidateLayout();
        }
        return this;
    }

    /// Remove a child from the canvas.
    public new Canvas RemoveChild(Widget child)
    {
        base.RemoveChild(child);
        return this;
    }

    protected override Size MeasureCore(Size available)
    {
        float maxX = 0f, maxY = 0f;
        foreach (var child in Children)
        {
            if (!child.IsVisible || GetParentData(child) is not Placement p) continue;
            child.Measure(new Size(
                float.IsNaN(p.Width)  ? float.PositiveInfinity : p.Width,
                float.IsNaN(p.Height) ? float.PositiveInfinity : p.Height));
            maxX = Math.Max(maxX, p.X + SlotWidth(child, p));
            maxY = Math.Max(maxY, p.Y + SlotHeight(child, p));
        }
        return new Size(maxX, maxY);
    }

    protected override void ArrangeCore(Rect finalRect)
    {
        foreach (var child in Children)
        {
            if (!child.IsVisible || GetParentData(child) is not Placement p) continue;
            child.Arrange(new Rect(finalRect.X + p.X, finalRect.Y + p.Y, SlotWidth(child, p), SlotHeight(child, p)));
        }
    }

    private static float SlotWidth(Widget child, Placement p)  => float.IsNaN(p.Width)  ? child.DesiredSize.Width  : p.Width;
    private static float SlotHeight(Widget child, Placement p) => float.IsNaN(p.Height) ? child.DesiredSize.Height : p.Height;

    private Canvas AddAt(Widget child, float x, float y, float w, float h)
    {
        AddChild(child);
        SetParentData(child, new Placement(x, y, w, h));
        return this;
    }
}
