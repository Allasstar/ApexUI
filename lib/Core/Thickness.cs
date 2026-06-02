namespace ApexUI.Core;

/// <summary>Spacing on four sides (margin, padding, border).</summary>
public readonly record struct Thickness(float Left, float Top, float Right, float Bottom)
{
    public static readonly Thickness Zero = new(0, 0, 0, 0);

    public Thickness(float uniform) : this(uniform, uniform, uniform, uniform) { }
    public Thickness(float horizontal, float vertical)
        : this(horizontal, vertical, horizontal, vertical) { }

    public float Horizontal => Left + Right;
    public float Vertical   => Top  + Bottom;
}