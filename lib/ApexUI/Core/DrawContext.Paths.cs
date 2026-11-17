namespace ApexUI.Core;

public sealed partial class DrawContext
{
    // ── Paths ─────────────────────────────────────────────────────────────────

    public void FillPath(SKPath path, SKColor color)
        => Canvas.DrawPath(path, FillPaint(color));

    /// thickness 0 draws a hairline: one device pixel wide at any scale.
    public void StrokePath(SKPath path, SKColor color, float thickness = 1f,
        SKStrokeCap cap = SKStrokeCap.Butt, SKStrokeJoin join = SKStrokeJoin.Miter)
        => Canvas.DrawPath(path, StrokePaint(color, thickness, cap, join));
}
