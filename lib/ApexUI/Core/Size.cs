namespace ApexUI.Core;

/// Width and height in logical units, used by the measure pass.
public readonly record struct Size(float Width, float Height)
{
    public static readonly Size Zero    = new(0, 0);
    public static readonly Size Infinite = new(float.PositiveInfinity, float.PositiveInfinity);
}
