namespace ApexUI.Core;

/// The theme pairs every ThemeRegistry starts with. Reuse them as a base for your own:
///
///   BuiltInThemes.ForestDark with { Primary = new(0, 200, 120) }
public static class BuiltInThemes
{
    public const string Default  = "Default";
    public const string Contrast = "Contrast";
    public const string Forest   = "Forest";
    public const string Desert   = "Desert";
    public const string Space    = "Space";

    internal static void RegisterAll(ThemeRegistry registry)
    {
        registry.Register(Default,  Theme.Light,   Theme.Dark);
        registry.Register(Contrast, ContrastLight, ContrastDark);
        registry.Register(Forest,   ForestLight,   ForestDark);
        registry.Register(Desert,   DesertLight,   DesertDark);
        registry.Register(Space,    SpaceLight,    SpaceDark);
    }

    // Contrast — maximum readability, saturated accent on pure white/black.
    public static Theme ContrastLight { get; } = new()
    {
        Background     = new(255, 255, 255),
        Surface        = new(240, 240, 240),
        SurfaceHover   = new(220, 220, 220),
        SurfacePressed = new(192, 192, 192),
        OnSurface      = new(0,   0,   0),
        OnSurfaceMuted = new(64,  64,  64),
        Primary        = new(0,   85,  204),
        PrimaryHover   = new(0,   62,  160),
        OnPrimary      = new(255, 255, 255),
        Success        = new(0,   119, 0),
        Warning        = new(187, 85,  0),
        Danger         = new(204, 0,   0),
        Border         = new(112, 112, 112),
    };
    public static Theme ContrastDark { get; } = new()
    {
        Background     = new(0,   0,   0),
        Surface        = new(20,  20,  20),
        SurfaceHover   = new(42,  42,  42),
        SurfacePressed = new(64,  64,  64),
        OnSurface      = new(255, 255, 255),
        OnSurfaceMuted = new(187, 187, 187),
        Primary        = new(255, 255, 68),
        PrimaryHover   = new(255, 255, 0),
        OnPrimary      = new(0,   0,   0),
        Success        = new(68,  255, 68),
        Warning        = new(255, 136, 0),
        Danger         = new(255, 68,  68),
        Border         = new(136, 136, 136),
    };

    // Forest — warm cream + deep greens; dark is deep-woods night.
    public static Theme ForestLight { get; } = new()
    {
        Background     = new(240, 238, 221),
        Surface        = new(228, 216, 192),
        SurfaceHover   = new(212, 200, 172),
        SurfacePressed = new(194, 184, 152),
        OnSurface      = new(28,  46,  28),
        OnSurfaceMuted = new(74,  102, 64),
        Primary        = new(45,  122, 58),
        PrimaryHover   = new(30,  94,  40),
        OnPrimary      = new(255, 255, 255),
        Success        = new(58,  144, 64),
        Warning        = new(176, 104, 32),
        Danger         = new(168, 48,  48),
        Border         = new(154, 170, 120),
    };
    public static Theme ForestDark { get; } = new()
    {
        Background     = new(12,  22,  12),
        Surface        = new(24,  34,  24),
        SurfaceHover   = new(36,  48,  36),
        SurfacePressed = new(48,  64,  48),
        OnSurface      = new(200, 224, 192),
        OnSurfaceMuted = new(106, 138, 96),
        Primary        = new(82,  192, 96),
        PrimaryHover   = new(104, 212, 118),
        OnPrimary      = new(12,  22,  12),
        Success        = new(82,  192, 96),
        Warning        = new(212, 130, 10),
        Danger         = new(224, 96,  96),
        Border         = new(48,  72,  48),
    };

    // Desert — sandy warmth + terracotta; dark is canyon at night.
    public static Theme DesertLight { get; } = new()
    {
        Background     = new(251, 242, 220),
        Surface        = new(242, 232, 200),
        SurfaceHover   = new(230, 218, 180),
        SurfacePressed = new(216, 204, 158),
        OnSurface      = new(58,  28,  0),
        OnSurfaceMuted = new(122, 80,  48),
        Primary        = new(192, 106, 24),
        PrimaryHover   = new(158, 84,  16),
        OnPrimary      = new(255, 255, 255),
        Success        = new(90,  138, 46),
        Warning        = new(204, 120, 0),
        Danger         = new(188, 44,  44),
        Border         = new(200, 176, 112),
    };
    public static Theme DesertDark { get; } = new()
    {
        Background     = new(28,  16,  0),
        Surface        = new(42,  26,  0),
        SurfaceHover   = new(60,  38,  0),
        SurfacePressed = new(80,  52,  0),
        OnSurface      = new(240, 216, 152),
        OnSurfaceMuted = new(160, 120, 64),
        Primary        = new(232, 138, 40),
        PrimaryHover   = new(240, 160, 64),
        OnPrimary      = new(28,  16,  0),
        Success        = new(104, 160, 48),
        Warning        = new(224, 160, 32),
        Danger         = new(224, 80,  80),
        Border         = new(96,  64,  32),
    };

    // Space — soft lavender surfaces; dark is deep cosmic with neon accents.
    public static Theme SpaceLight { get; } = new()
    {
        Background     = new(240, 238, 248),
        Surface        = new(228, 224, 244),
        SurfaceHover   = new(212, 206, 236),
        SurfacePressed = new(192, 184, 224),
        OnSurface      = new(26,  16,  64),
        OnSurfaceMuted = new(90,  80,  128),
        Primary        = new(96,  48,  204),
        PrimaryHover   = new(74,  32,  168),
        OnPrimary      = new(255, 255, 255),
        Success        = new(24,  144, 168),
        Warning        = new(192, 80,  144),
        Danger         = new(224, 32,  80),
        Border         = new(144, 144, 204),
    };
    public static Theme SpaceDark { get; } = new()
    {
        Background     = new(5,   2,   16),
        Surface        = new(14,  10,  40),
        SurfaceHover   = new(26,  20,  64),
        SurfacePressed = new(38,  32,  92),
        OnSurface      = new(232, 224, 255),
        OnSurfaceMuted = new(120, 112, 168),
        Primary        = new(144, 64,  255),
        PrimaryHover   = new(170, 85,  255),
        OnPrimary      = new(255, 255, 255),
        Success        = new(32,  208, 232),
        Warning        = new(192, 80,  255),
        Danger         = new(255, 32,  96),
        Border         = new(56,  48,  160),
    };
}
