namespace ApexUI.Core;

public sealed partial class DrawContext
{
    // ── Clipping ──────────────────────────────────────────────────────────────

    /// Saves canvas state, clips to a rect, and returns a scope that restores on dispose.
    /// Use with `using`: using (ctx.PushClip(r)) { ... }
    public ClipScope PushClip(Rect r)
    {
        Canvas.Save();
        Canvas.ClipRect(r.ToSKRect(), antialias: true);
        return new ClipScope(Canvas);
    }

    /// Saves canvas state, clips to a rounded rect, and returns a scope that restores on dispose.
    public ClipScope PushClip(Rect r, float radius)
    {
        Canvas.Save();
        Canvas.ClipRoundRect(new SKRoundRect(r.ToSKRect(), radius), antialias: true);
        return new ClipScope(Canvas);
    }
}

/// Returned by DrawContext.PushClip — restores the canvas save when disposed.
public readonly struct ClipScope(SKCanvas canvas) : IDisposable
{
    public void Dispose() => canvas.Restore();
}
