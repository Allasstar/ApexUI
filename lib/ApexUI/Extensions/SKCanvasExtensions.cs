namespace ApexUI.Extensions;

public static class SKCanvasExtensions
{
    extension(SKCanvas canvas)
    {
        // Draw a filled rounded rect with a single color
        public void FillRoundRect(SKRect rect, float rx, SKColor color)
        {
            using var paint = new SKPaint { Color = color, IsAntialias = true };
            canvas.DrawRoundRect(rect, rx, rx, paint);
        }

        // Draw a stroked rounded rect
        public void StrokeRoundRect(SKRect rect, float rx, SKColor color, float strokeWidth = 1f)
        {
            using var paint = new SKPaint
            {
                Color       = color,
                IsStroke    = true,
                StrokeWidth = strokeWidth,
                IsAntialias = true,
            };
            canvas.DrawRoundRect(rect, rx, rx, paint);
        }

        // Draw centered text inside a rect
        public void DrawTextCentered(string text, SKRect bounds, SKFont font, SKPaint paint)
        {
            float width = font.MeasureText(text);
            float x = bounds.MidX - width * 0.5f;
            float y = bounds.MidY - font.Metrics.Ascent * 0.5f;
            canvas.DrawText(text, x, y, SKTextAlign.Left, font, paint);
        }
    }
}