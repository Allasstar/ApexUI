namespace ApexUI.Core;

/// Axis-aligned rectangle in logical units, used for layout and hit-testing.
public readonly record struct Rect(float X, float Y, float Width, float Height)
{
    public static readonly Rect Zero = new(0, 0, 0, 0);

    public float Right  => X + Width;
    public float Bottom => Y + Height;
    public float CenterX => X + Width  * 0.5f;
    public float CenterY => Y + Height * 0.5f;

    /// Half-open: the left and top edges are inside, the right and bottom edges are not,
    /// so neighbours that share an edge never both contain a point on it.
    public bool Contains(float px, float py)
        => px >= X && px < Right && py >= Y && py < Bottom;

    public Rect Deflate(Thickness t)
        => new(X + t.Left, Y + t.Top,
               Width  - t.Left - t.Right,
               Height - t.Top  - t.Bottom);

    public Rect Translate(float dx, float dy)
        => new(X + dx, Y + dy, Width, Height);

    public Rect WithSize(float w, float h)
        => new(X, Y, w, h);

    /// Convert to SkiaSharp rect for drawing.
    public SkiaSharp.SKRect ToSKRect()
        => new(X, Y, Right, Bottom);

    public override string ToString()
        => $"({X},{Y} {Width}×{Height})";
}
