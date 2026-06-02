// src/Core/DrawContext.cs
//
// Passed into every DrawCore() call.
// Carries the canvas + theme so widgets don't need global state.

using SkiaSharp;

namespace ApexUI.Core;

public sealed class DrawContext(SKCanvas canvas, Theme theme, float dpiScale)
{
    public SKCanvas Canvas   { get; } = canvas;
    public Theme    Theme    { get; } = theme;
    public float    DpiScale { get; } = dpiScale;

    // Convenience: create a paint from the theme for common cases
    public SKPaint MakePaint(SKColor color, bool antialias = true) => new()
    {
        Color     = color,
        IsAntialias = antialias,
    };

    public SKFont MakeTextFont(float sizePx, bool bold = false) => new(
        bold ? SKTypeface.FromFamilyName(Theme.FontFamily, SKFontStyle.Bold)
             : SKTypeface.FromFamilyName(Theme.FontFamily, SKFontStyle.Normal),
        sizePx * DpiScale);

    public SKPaint MakeTextPaint(SKColor color) => new()
    {
        Color       = color,
        IsAntialias = true,
    };
}

// ── Input events ──────────────────────────────────────────────────────────────

public record struct PointerEvent(
    float  X,
    float  Y,
    PointerButton Button,
    bool   IsDown
);

public record struct KeyEvent(
    string Key,
    bool   IsDown,
    bool   Ctrl,
    bool   Shift,
    bool   Alt
);

public enum PointerButton { None, Left, Right, Middle }
