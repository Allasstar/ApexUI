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

    /// Draws a prepared text blob left-aligned and vertically centered within bounds, like DrawText.
    /// Drawing a blob allocates nothing, unlike drawing a string; cache the blob while the text is unchanged.
    /// sizePx and bold must match the font the blob was made with (they give the line metrics).
    public void DrawText(SKTextBlob? blob, Rect bounds, SKColor color, float sizePx, bool bold = false)
    {
        if (blob is null) return;
        var   m = GetFont(sizePx, bold).Metrics;
        float y = bounds.Y + (bounds.Height - (m.Descent - m.Ascent)) * 0.5f - m.Ascent;
        Canvas.DrawText(blob, bounds.X, y, TextPaint(color));
    }
}
