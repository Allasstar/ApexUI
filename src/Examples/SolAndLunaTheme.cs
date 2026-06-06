// Custom theme registered as a partial extension of ThemeLibrary.
// namespace must be ApexUI.Core to extend the partial class in lib.
namespace ApexUI.Core;

public static partial class ThemeLibrary
{
    public const string SolAndLuna = "Sol & Luna";

    static partial void RegisterCustom()
        => Register(SolAndLuna, Sol, Luna);

    // Sol — warm sunlit day: golden surfaces, vivid sun-orange primary.
    private static readonly Theme Sol = new()
    {
        Background     = new(255, 253, 240),
        Surface        = new(255, 244, 204),
        SurfaceHover   = new(255, 228, 144),
        SurfacePressed = new(255, 208, 96),
        OnSurface      = new(61,  32,  0),
        OnSurfaceMuted = new(138, 92,  24),
        Primary        = new(242, 140, 0),
        PrimaryHover   = new(212, 114, 0),
        OnPrimary      = new(255, 255, 255),
        Success        = new(94,  158, 42),
        Warning        = new(232, 104, 0),
        Danger         = new(212, 32,  32),
        Border         = new(232, 192, 96),
    };

    // Luna — moonlit night: deep navy, cool silver text, soft blue primary.
    private static readonly Theme Luna = new()
    {
        Background     = new(8,   12,  24),
        Surface        = new(16,  24,  40),
        SurfaceHover   = new(28,  40,  64),
        SurfacePressed = new(40,  56,  88),
        OnSurface      = new(232, 239, 248),
        OnSurfaceMuted = new(112, 144, 176),
        Primary        = new(144, 192, 240),
        PrimaryHover   = new(168, 208, 255),
        OnPrimary      = new(8,   12,  24),
        Success        = new(64,  184, 144),
        Warning        = new(208, 160, 64),
        Danger         = new(224, 80,  112),
        Border         = new(42,  58,  88),
    };
}
