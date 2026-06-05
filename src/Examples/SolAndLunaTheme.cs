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
        Background     = C(0xFFFDF0),
        Surface        = C(0xFFF4CC),
        SurfaceHover   = C(0xFFE490),
        SurfacePressed = C(0xFFD060),
        OnSurface      = C(0x3D2000),
        OnSurfaceMuted = C(0x8A5C18),
        Primary        = C(0xF28C00),
        PrimaryHover   = C(0xD47200),
        OnPrimary      = C(0xFFFFFF),
        Success        = C(0x5E9E2A),
        Warning        = C(0xE86800),
        Danger         = C(0xD42020),
        Border         = C(0xE8C060),
    };

    // Luna — moonlit night: deep navy, cool silver text, soft blue primary.
    private static readonly Theme Luna = new()
    {
        Background     = C(0x080C18),
        Surface        = C(0x101828),
        SurfaceHover   = C(0x1C2840),
        SurfacePressed = C(0x283858),
        OnSurface      = C(0xE8EFF8),
        OnSurfaceMuted = C(0x7090B0),
        Primary        = C(0x90C0F0),
        PrimaryHover   = C(0xA8D0FF),
        OnPrimary      = C(0x080C18),
        Success        = C(0x40B890),
        Warning        = C(0xD0A040),
        Danger         = C(0xE05070),
        Border         = C(0x2A3A58),
    };
}
