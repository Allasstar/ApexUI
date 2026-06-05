namespace ApexUI.Core;

// Shape-drawing helpers on DrawContext.
// Add your own shapes without touching the library:
//
//   // anywhere in src/ — same namespace, same assembly
//   namespace ApexUI.Core;
//   public sealed partial class DrawContext
//   {
//       public void DrawStar(float cx, float cy, float size, SKColor color) { ... }
//   }

public sealed partial class DrawContext
{
    // ── Rectangles ────────────────────────────────────────────────────────────

    public void FillRect(Rect r, SKColor color)
    {
        using var p = MakePaint(color);
        Canvas.DrawRect(r.ToSKRect(), p);
    }

    public void FillRoundRect(Rect r, float radius, SKColor color)
    {
        using var p = MakePaint(color);
        Canvas.DrawRoundRect(r.ToSKRect(), radius, radius, p);
    }

    public void StrokeRoundRect(Rect r, float radius, SKColor color, float thickness = 1f)
    {
        // Inset by half stroke width so the outer edge of the stroke aligns with the
        // fill boundary — prevents double-AA fringing at corners.
        float h = thickness * 0.5f;
        using var p = MakePaint(color);
        p.IsStroke    = true;
        p.StrokeWidth = thickness;
        Canvas.DrawRoundRect(
            new SKRect(r.X + h, r.Y + h, r.Right - h, r.Bottom - h),
            Math.Max(0f, radius - h), Math.Max(0f, radius - h), p);
    }

    // ── Circles & ovals ───────────────────────────────────────────────────────

    public void FillCircle(float cx, float cy, float radius, SKColor color)
    {
        using var p = MakePaint(color);
        Canvas.DrawCircle(cx, cy, radius, p);
    }

    public void StrokeCircle(float cx, float cy, float radius, SKColor color, float thickness = 1f)
    {
        using var p = MakePaint(color);
        p.IsStroke    = true;
        p.StrokeWidth = thickness;
        Canvas.DrawCircle(cx, cy, radius - thickness * 0.5f, p);
    }

    public void FillOval(Rect r, SKColor color)
    {
        using var p = MakePaint(color);
        Canvas.DrawOval(r.ToSKRect(), p);
    }

    // ── Lines ─────────────────────────────────────────────────────────────────

    public void DrawLine(float x0, float y0, float x1, float y1, SKColor color,
        float thickness = 1f, SKStrokeCap cap = SKStrokeCap.Round)
    {
        using var p = MakePaint(color);
        p.IsStroke    = true;
        p.StrokeWidth = thickness;
        p.StrokeCap   = cap;
        Canvas.DrawLine(x0, y0, x1, y1, p);
    }

    // ── Triangles ─────────────────────────────────────────────────────────────

    public void FillTriangle(float x0, float y0, float x1, float y1, float x2, float y2, SKColor color)
    {
        using var path = new SKPath();
        path.MoveTo(x0, y0); path.LineTo(x1, y1); path.LineTo(x2, y2); path.Close();
        using var p = MakePaint(color);
        Canvas.DrawPath(path, p);
    }

    public void StrokeTriangle(float x0, float y0, float x1, float y1, float x2, float y2,
        SKColor color, float thickness = 1f, SKStrokeJoin join = SKStrokeJoin.Round)
    {
        using var path = new SKPath();
        path.MoveTo(x0, y0); path.LineTo(x1, y1); path.LineTo(x2, y2); path.Close();
        using var p = MakePaint(color);
        p.IsStroke    = true;
        p.StrokeWidth = thickness;
        p.StrokeJoin  = join;
        Canvas.DrawPath(path, p);
    }

    // ── Shadow ────────────────────────────────────────────────────────────────

    /// Soft blurred drop-shadow behind r. Typically offset r by (0, 4) before calling.
    public void DrawShadow(Rect r, float cornerRadius, float blur, SKColor color)
    {
        using var sp = new SKPaint
        {
            Color       = color,
            MaskFilter  = SKMaskFilter.CreateBlur(SKBlurStyle.Normal, blur),
            IsAntialias = true,
        };
        Canvas.DrawRoundRect(r.ToSKRect(), cornerRadius, cornerRadius, sp);
    }

    // ── Common indicators ─────────────────────────────────────────────────────

    /// Open V or ^ shape — chevron pointing down (pointUp=false) or up (pointUp=true).
    public void DrawChevron(float cx, float cy, float size, bool pointUp, SKColor color, float thickness = 1.5f)
    {
        float sign = pointUp ? -1f : 1f;
        using var path = new SKPath();
        path.MoveTo(cx - size, cy - sign * size * 0.5f);
        path.LineTo(cx,        cy + sign * size * 0.5f);
        path.LineTo(cx + size, cy - sign * size * 0.5f);
        using var p = MakePaint(color);
        p.IsStroke    = true;
        p.StrokeWidth = thickness;
        p.StrokeCap   = SKStrokeCap.Round;
        p.StrokeJoin  = SKStrokeJoin.Round;
        Canvas.DrawPath(path, p);
    }

    /// ✓ checkmark centered at (cx, cy), scaled by size.
    public void DrawCheckmark(float cx, float cy, float size, SKColor color, float thickness = 2f)
    {
        using var path = new SKPath();
        path.MoveTo(cx - size * 0.45f, cy);
        path.LineTo(cx - size * 0.05f, cy + size * 0.4f);
        path.LineTo(cx + size * 0.5f,  cy - size * 0.35f);
        using var p = MakePaint(color);
        p.IsStroke    = true;
        p.StrokeWidth = thickness;
        p.StrokeCap   = SKStrokeCap.Round;
        p.StrokeJoin  = SKStrokeJoin.Round;
        Canvas.DrawPath(path, p);
    }

    /// × cross centered at (cx, cy), scaled by size.
    public void DrawCross(float cx, float cy, float size, SKColor color, float thickness = 2f)
    {
        using var p = MakePaint(color);
        p.IsStroke    = true;
        p.StrokeWidth = thickness;
        p.StrokeCap   = SKStrokeCap.Round;
        Canvas.DrawLine(cx - size, cy - size, cx + size, cy + size, p);
        Canvas.DrawLine(cx + size, cy - size, cx - size, cy + size, p);
    }
}
