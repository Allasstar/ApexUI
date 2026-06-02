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
        Background        = new SKColor(0x1E, 0x1E, 0x2E),
        Surface           = new SKColor(0x31, 0x32, 0x44),
        SurfaceHover      = new SKColor(0x45, 0x47, 0x5A),
        OnSurface         = new SKColor(0xCD, 0xD6, 0xF4),
        OnSurfaceMuted    = new SKColor(0x6C, 0x70, 0x86),
        Primary           = new SKColor(0x89, 0xB4, 0xFA),
        OnPrimary         = new SKColor(0x1E, 0x1E, 0x2E),
        Border            = new SKColor(0x58, 0x5B, 0x70),
    };

    // ── Colors ────────────────────────────────────────────────────────────

    /// App/window background
    public SKColor Background     { get; init; } = new(0xFF, 0xFF, 0xFF);

    /// Card / widget surface
    public SKColor Surface        { get; init; } = new(0xF5, 0xF5, 0xF5);
    public SKColor SurfaceHover   { get; init; } = new(0xE8, 0xE8, 0xE8);
    public SKColor SurfacePressed { get; init; } = new(0xD0, 0xD0, 0xD0);

    /// Text on surface
    public SKColor OnSurface      { get; init; } = new(0x1A, 0x1A, 0x1A);
    public SKColor OnSurfaceMuted { get; init; } = new(0x88, 0x88, 0x88);

    /// Accent / interactive color
    public SKColor Primary        { get; init; } = new(0x37, 0x8A, 0xDD);
    public SKColor PrimaryHover   { get; init; } = new(0x18, 0x5F, 0xA5);
    public SKColor OnPrimary      { get; init; } = new(0xFF, 0xFF, 0xFF);

    /// Semantic
    public SKColor Success        { get; init; } = new(0x1D, 0x9E, 0x75);
    public SKColor Warning        { get; init; } = new(0xEF, 0x9F, 0x27);
    public SKColor Danger         { get; init; } = new(0xE2, 0x4B, 0x4A);

    /// Borders
    public SKColor Border         { get; init; } = new(0xCC, 0xCC, 0xCC);

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
