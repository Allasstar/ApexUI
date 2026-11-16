namespace ApexUI.Core;

/// Uniform scale followed by a translation: a point p maps to p × Scale + (OffsetX, OffsetY).
/// Used as a widget's ChildTransform, mapping its children's layout space into its own.
/// Scale must be positive.
public readonly record struct Transform2D(float OffsetX, float OffsetY, float Scale)
{
    public static readonly Transform2D Identity = new(0f, 0f, 1f);

    public static Transform2D Translation(float offsetX, float offsetY) => new(offsetX, offsetY, 1f);

    public bool IsIdentity => OffsetX == 0f && OffsetY == 0f && Scale == 1f;

    public (float X, float Y) Apply(float x, float y) => (x * Scale + OffsetX, y * Scale + OffsetY);

    public (float X, float Y) Invert(float x, float y) => ((x - OffsetX) / Scale, (y - OffsetY) / Scale);

    public Rect Apply(Rect r) => new(r.X * Scale + OffsetX, r.Y * Scale + OffsetY, r.Width * Scale, r.Height * Scale);
}
