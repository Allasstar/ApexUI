// src/Core/Theme.cs
//
// All visual defaults live here.
// Widgets read from Theme instead of hardcoding colors.
// Swap the theme → entire app re-skins.

using SkiaSharp;

namespace ApexUI.Core;

public class Theme
{
    // ── Pre-built themes ─────────────────────────────────────────────────

    public static Theme Light { get; } = new();

    public static Theme Dark { get; } = new()
    {
        Background     = new(30,  30,  46),
        Surface        = new(49,  50,  68),
        SurfaceHover   = new(69,  71,  90),
        OnSurface      = new(205, 214, 244),
        OnSurfaceMuted = new(108, 112, 134),
        Primary        = new(137, 180, 250),
        OnPrimary      = new(30,  30,  46),
        Border         = new(88,  91,  112),
    };

    // ── Colors ────────────────────────────────────────────────────────────

    /// App/window background
    public SKColor Background     { get; init; } = new(255, 255, 255);

    /// Card / widget surface
    public SKColor Surface        { get; init; } = new(245, 245, 245);
    public SKColor SurfaceHover   { get; init; } = new(232, 232, 232);
    public SKColor SurfacePressed { get; init; } = new(208, 208, 208);

    /// Text on surface
    public SKColor OnSurface      { get; init; } = new(26,  26,  26);
    public SKColor OnSurfaceMuted { get; init; } = new(136, 136, 136);

    /// Accent / interactive color
    public SKColor Primary          { get; init; } = new(55,  138, 221);
    public SKColor PrimaryHover     { get; init; } = new(24,  95,  165);
    public SKColor PrimaryHoverGhost => Primary.WithAlpha(0.12f);   // hover/ripple tint
    public SKColor OnPrimary        { get; init; } = new(255, 255, 255);

    /// Semantic
    public SKColor Success        { get; init; } = new(29,  158, 117);
    public SKColor Warning        { get; init; } = new(239, 159, 39);
    public SKColor Danger         { get; init; } = new(226, 75,  74);

    /// Borders
    public SKColor Border         { get; init; } = new(204, 204, 204);

    // ── Typography ────────────────────────────────────────────────────────

    public string FontFamily      { get; init; } = "Segoe UI";
    public float  FontSizeBase    { get; init; } = 14f;   // logical px
    public float  FontSizeSmall   { get; init; } = 12f;
    public float  FontSizeLarge   { get; init; } = 18f;
    public float  FontSizeTitle   { get; init; } = 24f;

    // ── Shape ─────────────────────────────────────────────────────────────

    public float CornerRadiusSm   { get; init; } = 4f;
    public float CornerRadiusMd   { get; init; } = 8f;
    public float CornerRadiusLg   { get; init; } = 12f;

    // ── Spacing ───────────────────────────────────────────────────────────

    public float SpacingXs        { get; init; } = 4f;
    public float SpacingSm        { get; init; } = 8f;
    public float SpacingMd        { get; init; } = 16f;
    public float SpacingLg        { get; init; } = 24f;
}
