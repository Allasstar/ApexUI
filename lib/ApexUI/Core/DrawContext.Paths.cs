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

    /// Strokes the path as dashes `dash` long with `gap` between them, both in the current canvas units
    /// (inside a Viewport, divide by Zoom to keep them a fixed size on screen). thickness 0 draws hairline dashes.
    public void StrokeDashedPath(SKPath path, SKColor color, float thickness, float dash, float gap)
        => Canvas.DrawPath(path, DashedPaint(color, thickness, dash, gap));
}
