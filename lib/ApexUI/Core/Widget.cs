// Every button, label, panel and custom widget inherits from Widget.
// Layout is two passes, Measure then Arrange (Widget.Layout.cs), followed by Draw.
// LayoutBounds are absolute in the widget's layout space; a parent's ChildTransform
// maps its children's space into its own (scrolling, zoom).

namespace ApexUI.Core;

public abstract partial class Widget
{
    public string? Id { get; init; }

    // ── Tree ─────────────────────────────────────────────────────────────────

    public Widget? Parent { get; internal set; }

    private readonly List<Widget> _children = [];
    public ChildList Children => new(_children);

    /// Adds child as the last child. A child that already has a parent is moved here;
    /// moving within the same window keeps it attached, so it is not detached and re-attached.
    protected void AddChild(Widget child)
    {
        child.Parent?.Unlink(child);
        child.Parent = this;
        _children.Add(child);
        if (child.Host != Host)
        {
            child.DetachFromHost();
            if (Host is not null) child.AttachToHost(Host);
        }
        InvalidateLayoutUnlessArranging();
    }

    /// Removes child; if it was on screen it is detached (OnDetached runs on it and its descendants).
    protected void RemoveChild(Widget child)
    {
        if (Unlink(child))
            child.DetachFromHost();
    }

    public void RemoveAllChildren()
    {
        for (int i = _children.Count - 1; i >= 0; i--)
        {
            var child = _children[i];
            _children.RemoveAt(i);
            ReleaseChild(child);
            child.DetachFromHost();
        }
        InvalidateLayoutUnlessArranging();
    }

    private bool Unlink(Widget child)
    {
        if (!_children.Remove(child)) return false;
        ReleaseChild(child);
        InvalidateLayoutUnlessArranging();
        return true;
    }

    private void ReleaseChild(Widget child)
    {
        child.Parent      = null;
        child._parentData = null;
        OnChildRemoved(child);
    }

    /// Called after a child left this widget, however it was removed.
    protected virtual void OnChildRemoved(Widget child) { }

    // ── Visual properties ─────────────────────────────────────────────────

    public bool IsVisible
    {
        get;
        set => SetLayoutProperty(ref field, value);
    } = true;

    public float Opacity
    {
        get;
        set => SetRenderProperty(ref field, Math.Clamp(value, 0f, 1f));
    } = 1f;

    public SkiaSharp.SKColor Background
    {
        get;
        set => SetRenderProperty(ref field, value);
    } = SkiaSharp.SKColor.Empty;

    /// When set, overrides Background at draw time using the current theme.
    public Func<Theme, SkiaSharp.SKColor>? BackgroundSource
    {
        get;
        set => SetRenderProperty(ref field, value);
    }

    /// Rounds the background, the focus ring and widget-drawn outlines. Until set, the widget's
    /// themed default (DefaultCornerRadius).
    public float CornerRadius
    {
        get => float.IsNaN(_cornerRadius) ? DefaultCornerRadius(Theme) : _cornerRadius;
        set => SetRenderProperty(ref _cornerRadius, value);
    }
    private float _cornerRadius = float.NaN;

    /// CornerRadius used until it is set. Override to take it from the theme.
    protected virtual float DefaultCornerRadius(Theme theme) => 0f;

    /// The host's theme, or Theme.Light while the widget is not on screen.
    /// Read it in MeasureCore, ArrangeCore and DrawCore: a theme change re-measures and redraws everything.
    protected Theme Theme => Host?.Theme ?? Theme.Light;

    // ── Interaction ────────────────────────────────────────────────────────

    public bool IsEnabled        { get; set => SetRenderProperty(ref field, value); } = true;
    public bool IsHitTestVisible { get; set; } = true;

    // ── Child transform ───────────────────────────────────────────────────

    /// Maps the children's layout space into this widget's: children are laid out as usual,
    /// then drawn, hit-tested and sent pointer positions through this transform.
    /// Changing it only redraws, so scrolling and zooming need no layout pass.
    public Transform2D ChildTransform
    {
        get;
        protected set => SetRenderProperty(ref field, value);
    } = Transform2D.Identity;

    /// LayoutBounds in logical window coordinates, through every ancestor's ChildTransform.
    /// Use it to place overlays next to a widget that may be scrolled or zoomed.
    public Rect WindowBounds
    {
        get
        {
            var r = LayoutBounds;
            for (var p = Parent; p is not null; p = p.Parent)
                r = p.ChildTransform.Apply(r);
            return r;
        }
    }

    /// Maps a point in logical window coordinates into this widget's layout space.
    public (float X, float Y) PointFromWindow(float x, float y)
    {
        if (Parent is null) return (x, y);
        var (px, py) = Parent.PointFromWindow(x, y);
        return Parent.ChildTransform.Invert(px, py);
    }

    // ── Draw pass ─────────────────────────────────────────────────────────

    public void Draw(DrawContext ctx)
    {
        if (!IsVisible || Opacity <= 0f) return;

        var canvas = ctx.Canvas;
        var bounds = LayoutBounds.ToSKRect();
        // Everything a widget draws is clipped to its bounds, so one outside the clip draws nothing.
        if (canvas.QuickReject(bounds)) return;

        canvas.Save();
        canvas.ClipRect(bounds);

        if (Opacity < 1f)
            canvas.SaveLayer(ctx.LayerPaint(Opacity));

        var bg = BackgroundSource?.Invoke(ctx.Theme) ?? Background;
        if (bg != SkiaSharp.SKColor.Empty)
        {
            float radius = CornerRadius;
            if (radius > 0)
                ctx.FillRoundRect(LayoutBounds, radius, bg);
            else
                ctx.FillRect(LayoutBounds, bg);
        }

        DrawCore(ctx);

        if (_children.Count > 0)
        {
            var t = ChildTransform;
            bool transformed = !t.IsIdentity;
            if (transformed)
            {
                canvas.Save();
                canvas.Translate(t.OffsetX, t.OffsetY);
                canvas.Scale(t.Scale);
            }
            foreach (var child in _children)
                child.Draw(ctx);
            if (transformed) canvas.Restore();
        }

        DrawOverChildren(ctx);

        if (IsFocused && Host is { IsFocusVisible: true })
            DrawFocusRing(ctx);

        if (Opacity < 1f) canvas.Restore();
        canvas.Restore();
    }

    // Override to draw widget-specific content.
    protected virtual void DrawCore(DrawContext ctx) { }

    /// Draws above the children (drop markers, selection outlines), in this widget's layout space.
    protected virtual void DrawOverChildren(DrawContext ctx) { }

    // ── Hit-testing ────────────────────────────────────────────────────────

    /// Returns the deepest widget at (x, y), or null if nothing hit. (x, y) is in this widget's layout space.
    public virtual Widget? HitTest(float x, float y)
    {
        if (!IsVisible || !IsEnabled || !IsHitTestVisible) return null;
        if (!LayoutBounds.Contains(x, y)) return null;

        if (_children.Count > 0)
        {
            var (cx, cy) = ChildTransform.Invert(x, y);
            // Back to front: the last drawn child is on top.
            for (int i = _children.Count - 1; i >= 0; i--)
            {
                var hit = _children[i].HitTest(cx, cy);
                if (hit is not null) return hit;
            }
        }

        return this;
    }
}
