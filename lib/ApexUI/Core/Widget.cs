// src/Core/Widget.cs
//
// THE most important file in the framework.
// Every button, label, panel, and custom widget inherits from this.
//
// Design goals:
//  - Measure → Arrange → Draw, the same 3-pass model WPF/Flutter/HTML use
//  - Properties call Invalidate() automatically (via C# 14 `field` keyword)
//  - Zero allocations in the render loop (struct child enumeration, no boxing)
//  - Parent owns children — widgets don't need to know their own position

namespace ApexUI.Core;

public abstract partial class Widget
{
    // ── Identity ──────────────────────────────────────────────────────────────

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
        InvalidateLayout();
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
            child.Parent = null;
            _children.RemoveAt(i);
            OnChildRemoved(child);
            child.DetachFromHost();
        }
        InvalidateLayout();
    }

    private bool Unlink(Widget child)
    {
        if (!_children.Remove(child)) return false;
        child.Parent = null;
        OnChildRemoved(child);
        InvalidateLayout();
        return true;
    }

    /// Called after a child left this widget, however it was removed.
    /// Override to drop per-child bookkeeping such as grid cells or positions.
    protected virtual void OnChildRemoved(Widget child) { }

    // ── Layout properties (C# 14 `field` keyword) ─────────────────────────
    // `field` is the implicit backing field — no separate `private float _width`

    public float Width
    {
        get;
        set => SetLayoutProperty(ref field, value);
    } = float.NaN; // NaN = "auto" (size to content)

    public float Height
    {
        get;
        set => SetLayoutProperty(ref field, value);
    } = float.NaN;

    public float MinWidth  { get; set => SetLayoutProperty(ref field, value); } = 0f;
    public float MinHeight { get; set => SetLayoutProperty(ref field, value); } = 0f;
    public float MaxWidth  { get; set => SetLayoutProperty(ref field, value); } = float.PositiveInfinity;
    public float MaxHeight { get; set => SetLayoutProperty(ref field, value); } = float.PositiveInfinity;

    // NaN = no constraint. Mode controls which axis drives the other.
    public float            AspectRatio     { get; set => SetLayoutProperty(ref field, value); } = float.NaN;
    public AspectRatioMode  AspectRatioMode { get; set => SetLayoutProperty(ref field, value); } = AspectRatioMode.WidthControlsHeight;

    public Thickness Margin  { get; set => SetLayoutProperty(ref field, value); } = Thickness.Zero;
    public Thickness Padding { get; set => SetLayoutProperty(ref field, value); } = Thickness.Zero;

    public HAlign HAlign { get; set => SetLayoutProperty(ref field, value); } = HAlign.Stretch;
    public VAlign VAlign { get; set => SetLayoutProperty(ref field, value); } = VAlign.Stretch;

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

    public float CornerRadius
    {
        get;
        set => SetRenderProperty(ref field, value);
    } = 0f;

    // ── Interaction ────────────────────────────────────────────────────────

    public bool IsEnabled        { get; set => SetRenderProperty(ref field, value); } = true;
    public bool IsHitTestVisible { get; set; } = true;

    // ── Layout state (set by parent during Arrange) ───────────────────────

    /// Bounds in parent-local coordinates, set by parent's ArrangeCore.
    public Rect LayoutBounds { get; internal set; }

    /// Desired size, set during Measure pass.
    public Size DesiredSize  { get; private set; }

    // ── Invalidation ──────────────────────────────────────────────────────

    private bool _layoutDirty = true;

    /// Marks this widget and its ancestors for measure and arrange, and asks the host for a frame.
    public void InvalidateLayout()
    {
        for (var w = this; w is not null; w = w.Parent)
            w._layoutDirty = true;
        Host?.RequestFrame();
    }

    /// Asks the host to redraw. The whole window is redrawn, so nothing is tracked per widget.
    public void Invalidate() => Host?.RequestFrame();

    internal bool IsLayoutDirty => _layoutDirty;

    /// Sets a property that changes the widget's size or position; does nothing when the value is unchanged.
    /// Returns whether it changed.
    protected bool SetLayoutProperty<T>(ref T field, T value)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        field = value;
        InvalidateLayout();
        return true;
    }

    /// Sets a property that changes only how the widget looks; does nothing when the value is unchanged.
    /// Returns whether it changed.
    protected bool SetRenderProperty<T>(ref T field, T value)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        field = value;
        Invalidate();
        return true;
    }

    // ── Layout pass ──────────────────────────────────────────────────────
    //
    // Two-pass system identical to WPF/Avalonia/Flutter:
    //   1. Measure(availableSize)  → widget reports DesiredSize
    //   2. Arrange(finalRect)      → parent tells widget its actual bounds

    public void Measure(Size available)
    {
        if (!IsVisible)
        {
            DesiredSize = Size.Zero;
            return;
        }

        // Subtract margin from available space before measuring content
        var contentAvailable = new Size(
            Math.Max(0, available.Width  - Margin.Horizontal),
            Math.Max(0, available.Height - Margin.Vertical));

        // Let subclass measure its content
        var desired = MeasureCore(contentAvailable);

        // Apply explicit Width/Height overrides
        float w = float.IsNaN(Width)  ? desired.Width  : Width;
        float h = float.IsNaN(Height) ? desired.Height : Height;

        // Aspect ratio: compute dependent dimension.
        // When Stretch + finite available, use the available size so auto rows/cols in Grid size correctly.
        if (!float.IsNaN(AspectRatio) && AspectRatio > 0f)
        {
            if (AspectRatioMode == AspectRatioMode.WidthControlsHeight)
            {
                float aw = HAlign == HAlign.Stretch && !float.IsInfinity(contentAvailable.Width)
                    ? contentAvailable.Width : w;
                h = aw / AspectRatio;
            }
            else
            {
                float ah = VAlign == VAlign.Stretch && !float.IsInfinity(contentAvailable.Height)
                    ? contentAvailable.Height : h;
                w = ah * AspectRatio;
            }
        }

        // Clamp to Min/Max
        w = Math.Clamp(w, MinWidth,  MaxWidth);
        h = Math.Clamp(h, MinHeight, MaxHeight);

        // Add margin back to reported size
        DesiredSize = new Size(w + Margin.Horizontal, h + Margin.Vertical);
    }

    public void Arrange(Rect finalRect)
    {
        if (!IsVisible) return;

        // Remove margin — the content rect is inside the margin
        var contentRect = finalRect.Deflate(Margin);

        // Apply alignment within the content rect
        var arranged = ApplyAlignment(contentRect, DesiredSize);

        LayoutBounds = arranged;
        ArrangeCore(arranged);
        _layoutDirty = false;
    }

    // Override in subclasses to measure content.
    // Default: measures all children as overlapping (Stack semantics), returns max bounds.
    protected virtual Size MeasureCore(Size available)
    {
        float w = 0f, h = 0f;
        foreach (var child in _children)
        {
            if (!child.IsVisible) continue;
            child.Measure(available);
            w = Math.Max(w, child.DesiredSize.Width);
            h = Math.Max(h, child.DesiredSize.Height);
        }
        return new Size(w, h);
    }

    // Override in subclasses to position children.
    protected virtual void ArrangeCore(Rect finalRect)
    {
        // Default: measure + arrange each child filling the full rect
        foreach (var child in _children)
        {
            child.Measure(new Size(finalRect.Width, finalRect.Height));
            child.Arrange(finalRect);
        }
    }

    // ── Draw pass ─────────────────────────────────────────────────────────

    public void Draw(DrawContext ctx)
    {
        if (!IsVisible || Opacity <= 0f) return;

        ctx.Canvas.Save();

        // Clip to bounds
        ctx.Canvas.ClipRect(LayoutBounds.ToSKRect());

        // Opacity layer
        if (Opacity < 1f)
            ctx.Canvas.SaveLayer(ctx.LayerPaint(Opacity));

        // Background
        var bg = BackgroundSource?.Invoke(ctx.Theme) ?? Background;
        if (bg != SkiaSharp.SKColor.Empty)
        {
            if (CornerRadius > 0)
                ctx.FillRoundRect(LayoutBounds, CornerRadius, bg);
            else
                ctx.FillRect(LayoutBounds, bg);
        }

        DrawCore(ctx);

        // Draw children on top
        foreach (var child in _children)
            child.Draw(ctx);

        if (IsFocused && Host is { IsFocusVisible: true })
            DrawFocusRing(ctx);

        if (Opacity < 1f) ctx.Canvas.Restore();
        ctx.Canvas.Restore();
    }

    // Override to draw widget-specific content.
    protected virtual void DrawCore(DrawContext ctx) { }

    // ── Hit-testing ────────────────────────────────────────────────────────

    /// Returns the deepest widget at (x, y), or null if nothing hit.
    public virtual Widget? HitTest(float x, float y)
    {
        if (!IsVisible || !IsEnabled || !IsHitTestVisible) return null;
        if (!LayoutBounds.Contains(x, y)) return null;

        // Test children back-to-front (last drawn = top)
        for (int i = _children.Count - 1; i >= 0; i--)
        {
            var hit = _children[i].HitTest(x, y);
            if (hit is not null) return hit;
        }

        return this;
    }

    // ── Alignment helper ──────────────────────────────────────────────────

    private Rect ApplyAlignment(Rect available, Size desired)
    {
        float x = available.X;
        float y = available.Y;
        float w = HAlign == HAlign.Stretch ? available.Width  : Math.Min(desired.Width,  available.Width);
        float h = VAlign == VAlign.Stretch ? available.Height : Math.Min(desired.Height, available.Height);

        if (!float.IsNaN(AspectRatio) && AspectRatio > 0f)
        {
            if (AspectRatioMode == AspectRatioMode.WidthControlsHeight)
                h = w / AspectRatio;
            else
                w = h * AspectRatio;
        }

        x += HAlign switch
        {
            HAlign.Center => (available.Width  - w) * 0.5f,
            HAlign.Right  => available.Width   - w,
            _ => 0
        };

        y += VAlign switch
        {
            VAlign.Center => (available.Height - h) * 0.5f,
            VAlign.Bottom => available.Height  - h,
            _ => 0
        };

        return new Rect(x, y, w, h);
    }
}

// ── Enums ─────────────────────────────────────────────────────────────────────

public enum HAlign { Left, Center, Right, Stretch }
public enum VAlign { Top,  Center, Bottom, Stretch }
public enum AspectRatioMode { WidthControlsHeight, HeightControlsWidth }
