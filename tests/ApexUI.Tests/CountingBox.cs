namespace ApexUI.Tests;

/// Fixed-size test widget that counts its layout and draw passes.
internal sealed class CountingBox(float width, float height) : Widget
{
    public int Measures { get; private set; }
    public int Arranges { get; private set; }
    public int Draws    { get; private set; }

    public void Resize(float w, float h)
    {
        width  = w;
        height = h;
        InvalidateLayout();
    }

    protected override Size MeasureCore(Size available)
    {
        Measures++;
        return new Size(width, height);
    }

    protected override void ArrangeCore(Rect finalRect) => Arranges++;

    protected override void DrawCore(DrawContext ctx) => Draws++;
}

/// Test panel whose child transform the test sets, like a pan and zoom viewport.
internal sealed class TransformPanel : Widget
{
    public TransformPanel(params Widget[] children)
    {
        foreach (var c in children) AddChild(c);
    }

    public void SetTransform(Transform2D transform) => ChildTransform = transform;

    protected override void ArrangeCore(Rect finalRect)
    {
        foreach (var child in Children)
        {
            child.Measure(Size.Infinite);
            child.Arrange(new Rect(finalRect.X, finalRect.Y, child.DesiredSize.Width, child.DesiredSize.Height));
        }
    }
}
