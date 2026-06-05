namespace ApexUI.Core;

/// Ordered registry of named theme pairs (light + dark variant each).
///
/// To add a custom theme without editing this file, create a partial class
/// file anywhere in your project (must use namespace ApexUI.Core):
///
///   // src/MyThemes.cs
///   namespace ApexUI.Core;
///   public static partial class ThemeLibrary
///   {
///       public const string Neon = "Neon";
///
///       static partial void RegisterCustom()
///           => Register(Neon, new Theme { ... }, new Theme { ... });
///   }
///
/// RegisterCustom() is called once after all built-in themes are registered.
/// To hide a built-in theme call Remove(name) inside RegisterCustom().
///
public static partial class ThemeLibrary
{
    // ── Built-in name constants ───────────────────────────────────────────────

    public const string Default  = "Default";
    public const string Contrast = "Contrast";
    public const string Forest   = "Forest";
    public const string Desert   = "Desert";
    public const string Space    = "Space";

    // ── Internal storage ─────────────────────────────────────────────────────

    private static readonly List<string> _order = [];
    private static readonly Dictionary<string, (Theme Light, Theme Dark)> _registry
        = new(StringComparer.OrdinalIgnoreCase);

    static ThemeLibrary()
    {
        Register(Default,  Theme.Light,   Theme.Dark);
        Register(Contrast, ContrastLight, ContrastDark);
        Register(Forest,   ForestLight,   ForestDark);
        Register(Desert,   DesertLight,   DesertDark);
        Register(Space,    SpaceLight,    SpaceDark);
        RegisterCustom();
    }

    // Extension point — implement in a partial file to register custom themes.
    static partial void RegisterCustom();

    // ── Registry API ─────────────────────────────────────────────────────────

    /// All registered theme names in registration order.
    public static IReadOnlyList<string> Names => _order;

    /// First registered theme name (used as the application default).
    public static string DefaultName => _order.Count > 0 ? _order[0] : Default;

    /// Add or replace a theme. If the name is new it is appended at the end.
    public static void Register(string name, Theme light, Theme dark)
    {
        if (!_registry.ContainsKey(name))
            _order.Add(name);
        _registry[name] = (light, dark);
    }

    /// Remove a theme by name. No-op if not found.
    public static void Remove(string name)
    {
        if (_registry.Remove(name))
            _order.Remove(name);
    }

    /// Resolve a theme by name + dark flag.
    /// Falls back to the first registered theme if name is not found.
    public static Theme Get(string name, bool dark)
    {
        if (_registry.TryGetValue(name, out var pair))
            return dark ? pair.Dark : pair.Light;

        if (_order.Count > 0 && _registry.TryGetValue(_order[0], out var first))
            return dark ? first.Dark : first.Light;

        return dark ? Theme.Dark : Theme.Light;
    }

    // ── Built-in palettes ─────────────────────────────────────────────────────

    // Contrast — maximum readability, saturated accent on pure white/black.
    private static readonly Theme ContrastLight = new()
    {
        Background     = C(0xFFFFFF), Surface        = C(0xF0F0F0),
        SurfaceHover   = C(0xDCDCDC), SurfacePressed = C(0xC0C0C0),
        OnSurface      = C(0x000000), OnSurfaceMuted = C(0x404040),
        Primary        = C(0x0055CC), PrimaryHover   = C(0x003EA0),
        OnPrimary      = C(0xFFFFFF),
        Success        = C(0x007700), Warning        = C(0xBB5500), Danger = C(0xCC0000),
        Border         = C(0x707070),
    };
    private static readonly Theme ContrastDark = new()
    {
        Background     = C(0x000000), Surface        = C(0x141414),
        SurfaceHover   = C(0x2A2A2A), SurfacePressed = C(0x404040),
        OnSurface      = C(0xFFFFFF), OnSurfaceMuted = C(0xBBBBBB),
        Primary        = C(0xFFFF44), PrimaryHover   = C(0xFFFF00),
        OnPrimary      = C(0x000000),
        Success        = C(0x44FF44), Warning        = C(0xFF8800), Danger = C(0xFF4444),
        Border         = C(0x888888),
    };

    // Forest — warm cream + deep greens; dark is deep-woods night.
    private static readonly Theme ForestLight = new()
    {
        Background     = C(0xF0EEDD), Surface        = C(0xE4D8C0),
        SurfaceHover   = C(0xD4C8AC), SurfacePressed = C(0xC2B898),
        OnSurface      = C(0x1C2E1C), OnSurfaceMuted = C(0x4A6640),
        Primary        = C(0x2D7A3A), PrimaryHover   = C(0x1E5E28),
        OnPrimary      = C(0xFFFFFF),
        Success        = C(0x3A9040), Warning        = C(0xB06820), Danger = C(0xA83030),
        Border         = C(0x9AAA78),
    };
    private static readonly Theme ForestDark = new()
    {
        Background     = C(0x0C160C), Surface        = C(0x182218),
        SurfaceHover   = C(0x243024), SurfacePressed = C(0x304030),
        OnSurface      = C(0xC8E0C0), OnSurfaceMuted = C(0x6A8A60),
        Primary        = C(0x52C060), PrimaryHover   = C(0x68D476),
        OnPrimary      = C(0x0C160C),
        Success        = C(0x52C060), Warning        = C(0xD4820A), Danger = C(0xE06060),
        Border         = C(0x304830),
    };

    // Desert — sandy warmth + terracotta; dark is canyon at night.
    private static readonly Theme DesertLight = new()
    {
        Background     = C(0xFBF2DC), Surface        = C(0xF2E8C8),
        SurfaceHover   = C(0xE6DAB4), SurfacePressed = C(0xD8CC9E),
        OnSurface      = C(0x3A1C00), OnSurfaceMuted = C(0x7A5030),
        Primary        = C(0xC06A18), PrimaryHover   = C(0x9E5410),
        OnPrimary      = C(0xFFFFFF),
        Success        = C(0x5A8A2E), Warning        = C(0xCC7800), Danger = C(0xBC2C2C),
        Border         = C(0xC8B070),
    };
    private static readonly Theme DesertDark = new()
    {
        Background     = C(0x1C1000), Surface        = C(0x2A1A00),
        SurfaceHover   = C(0x3C2600), SurfacePressed = C(0x503400),
        OnSurface      = C(0xF0D898), OnSurfaceMuted = C(0xA07840),
        Primary        = C(0xE88A28), PrimaryHover   = C(0xF0A040),
        OnPrimary      = C(0x1C1000),
        Success        = C(0x68A030), Warning        = C(0xE0A020), Danger = C(0xE05050),
        Border         = C(0x604020),
    };

    // Space — soft lavender surfaces; dark is deep cosmic with neon accents.
    private static readonly Theme SpaceLight = new()
    {
        Background     = C(0xF0EEF8), Surface        = C(0xE4E0F4),
        SurfaceHover   = C(0xD4CEEC), SurfacePressed = C(0xC0B8E0),
        OnSurface      = C(0x1A1040), OnSurfaceMuted = C(0x5A5080),
        Primary        = C(0x6030CC), PrimaryHover   = C(0x4A20A8),
        OnPrimary      = C(0xFFFFFF),
        Success        = C(0x1890A8), Warning        = C(0xC05090), Danger = C(0xE02050),
        Border         = C(0x9090CC),
    };
    private static readonly Theme SpaceDark = new()
    {
        Background     = C(0x050210), Surface        = C(0x0E0A28),
        SurfaceHover   = C(0x1A1440), SurfacePressed = C(0x26205C),
        OnSurface      = C(0xE8E0FF), OnSurfaceMuted = C(0x7870A8),
        Primary        = C(0x9040FF), PrimaryHover   = C(0xAA55FF),
        OnPrimary      = C(0xFFFFFF),
        Success        = C(0x20D0E8), Warning        = C(0xC050FF), Danger = C(0xFF2060),
        Border         = C(0x3830A0),
    };

    private static SKColor C(uint rgb)
        => new((byte)((rgb >> 16) & 0xFF), (byte)((rgb >> 8) & 0xFF), (byte)(rgb & 0xFF));
}
