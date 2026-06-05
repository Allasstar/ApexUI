// src/Core/DrawContext.cs
//
// Passed into every DrawCore() call.
// Carries the canvas + theme so widgets don't need global state.

using SkiaSharp;

namespace ApexUI.Core;

public sealed partial class DrawContext(SKCanvas canvas, Theme theme, float dpiScale, string fontFamily = "Segoe UI")
{
    public SKCanvas Canvas     { get; } = canvas;
    public Theme    Theme      { get; } = theme;
    public float    DpiScale   { get; } = dpiScale;
    public string   FontFamily { get; } = fontFamily;

    public SKPaint MakePaint(SKColor color, bool antialias = true) => new()
    {
        Color       = color,
        IsAntialias = antialias,
    };

    public SKFont MakeTextFont(float sizePx, bool bold = false) => new(
        bold ? SKTypeface.FromFamilyName(FontFamily, SKFontStyle.Bold)
             : SKTypeface.FromFamilyName(FontFamily, SKFontStyle.Normal),
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
