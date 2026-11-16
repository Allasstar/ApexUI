namespace ApexUI.Core;

/// <summary>2-component size used in measure pass.</summary>
public readonly record struct Size(float Width, float Height)
{
    public static readonly Size Zero    = new(0, 0);
    public static readonly Size Infinite = new(float.PositiveInfinity, float.PositiveInfinity);

    public Size Constrain(Size constraint)
        => new(Math.Min(Width,  constraint.Width),
            Math.Min(Height, constraint.Height));
}