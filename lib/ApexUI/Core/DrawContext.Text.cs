namespace ApexUI.Core;

public sealed partial class DrawContext
{
    // ── Text ──────────────────────────────────────────────────────────────────

    public float MeasureText(string text, float sizePx, bool bold = false)
        => GetFont(sizePx, bold).MeasureText(text);

    /// Measures a slice of text without allocating a substring.
    public float MeasureText(ReadOnlySpan<char> text, float sizePx, bool bold = false)
        => GetFont(sizePx, bold).MeasureText(text);

    /// Draw text vertically centered within bounds.
    /// Horizontal anchor: Left → bounds.X, Center → bounds center, Right → bounds.Right.
    public void DrawText(string text, Rect bounds, SKColor color,
        float sizePx, bool bold = false, SKTextAlign align = SKTextAlign.Left)
    {
        if (string.IsNullOrEmpty(text)) return;
        var   font = GetFont(sizePx, bold);
        var   m    = font.Metrics;
        float y    = bounds.Y + (bounds.Height - (m.Descent - m.Ascent)) * 0.5f - m.Ascent;
        float x    = align switch
        {
            SKTextAlign.Center => bounds.X + bounds.Width * 0.5f,
            SKTextAlign.Right  => bounds.Right,
            _                  => bounds.X,
        };
        Canvas.DrawText(text, x, y, align, font, TextPaint(color));
    }
}
