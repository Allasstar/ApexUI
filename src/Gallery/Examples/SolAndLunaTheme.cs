namespace ApexUI.Gallery.Examples;

/// Custom theme pair added to the host's ThemeRegistry at startup, before any UI lists the themes.
/// Luna also overrides tokens beyond the palette: knob, scrim, shadow and rounder corners.
public static class SolAndLunaTheme
{
    public const string Name = "Sol & Luna";

    public static void Register(ThemeRegistry themes)
        => themes.Register(Name, Sol, Luna);

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
        Knob           = new(232, 239, 248),
        Scrim          = new(0,   4,   16,  170),
        Shadow         = new(0,   0,   0,   120),
        CornerRadiusControl = 10f,
        CornerRadiusPopup   = 12f,
        CornerRadiusDialog  = 16f,
    };
}
