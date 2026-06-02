// src/Core/Rect.cs
//
// Lightweight value type for layout bounds.
// Used in every measure/arrange pass — must be a struct (zero allocation).

namespace ApexUI.Core;

/// <summary>
/// Axis-aligned rectangle used for layout and hit-testing.
/// All coordinates are in logical pixels (DPI-independent).
/// </summary>
public readonly record struct Rect(float X, float Y, float Width, float Height)
{
    public static readonly Rect Zero = new(0, 0, 0, 0);

    public float Right  => X + Width;
    public float Bottom => Y + Height;
    public float CenterX => X + Width  * 0.5f;
    public float CenterY => Y + Height * 0.5f;

    public bool Contains(float px, float py)
        => px >= X && px <= Right && py >= Y && py <= Bottom;

    public bool Intersects(Rect other)
        => X < other.Right && Right > other.X
        && Y < other.Bottom && Bottom > other.Y;

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