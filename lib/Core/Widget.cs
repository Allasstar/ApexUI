// src/Core/Widget.cs
//
// THE most important file in the framework.
// Every button, label, panel, and custom widget inherits from this.
//
// Design goals:
//  - Measure → Arrange → Draw, the same 3-pass model WPF/Flutter/HTML use
//  - Properties call Invalidate() automatically (via C# 14 `field` keyword)
//  - Zero allocations in the render loop (Span<Widget>, no boxing)
//  - Parent owns children — widgets don't need to know their own position

namespace ApexUI.Core;

public abstract class Widget
{
    // ── Identity ──────────────────────────────────────────────────────────────

    public string? Id { get; init; }

    // ── Tree ─────────────────────────────────────────────────────────────────

    public Widget? Parent { get; private set; }

    private readonly List<Widget> _children = [];
    public IReadOnlyList<Widget> Children => _children;

    protected void AddChild(Widget child)
    {
        child.Parent = this;
        _children.Add(child);
        InvalidateLayout();
    }

    protected void RemoveChild(Widget child)
    {
        if (_children.Remove(child))
        {
            child.Parent = null;
            InvalidateLayout();
        }
    }

    // ── Layout properties (C# 14 `field` keyword) ─────────────────────────
    // `field` is the implicit backing field — no separate `private float _width`

    public float Width
    {
        get;
        set { field = value; InvalidateLayout(); }
    } = float.NaN; // NaN = "auto" (size to content)

    public float Height
    {
        get;
        set { field = value; InvalidateLayout(); }
    } = float.NaN;

    public float MinWidth  { get; set { field = value; InvalidateLayout(); } } = 0f;
    public float MinHeight { get; set { field = value; InvalidateLayout(); } } = 0f;
    public float MaxWidth  { get; set { field = value; InvalidateLayout(); } } = float.PositiveInfinity;
    public float MaxHeight { get; set { field = value; InvalidateLayout(); } } = float.PositiveInfinity;

    public Thickness Margin  { get; set { field = value; InvalidateLayout(); } } = Thickness.Zero;
    public Thickness Padding { get; set { field = value; InvalidateLayout(); } } = Thickness.Zero;

    public HAlign HAlign { get; set { field = value; InvalidateLayout(); } } = HAlign.Stretch;
    public VAlign VAlign { get; set { field = value; InvalidateLayout(); } } = VAlign.Stretch;

    // ── Visual properties ─────────────────────────────────────────────────

    public bool IsVisible
    {
        get;
        set { field = value; Invalidate(); }
    } = true;

    public float Opacity
    {
        get;
        set { field = Math.Clamp(value, 0f, 1f); Invalidate(); }
    } = 1f;

    public SkiaSharp.SKColor Background
    {
        get;
        set { field = value; Invalidate(); }
    } = SkiaSharp.SKColor.Empty;

    public float CornerRadius
    {
        get;
        set { field = value; Invalidate(); }
    } = 0f;

    // ── Interaction ────────────────────────────────────────────────────────

    public bool IsEnabled        { get; set { field = value; Invalidate(); } } = true;
    public bool IsHitTestVisible { get; set; } = true;
    public bool IsHovered        { get; internal set; }
    public bool IsPressed        { get; internal set; }

    public Action<PointerEvent>? OnPointerDown;
    public Action<PointerEvent>? OnPointerUp;
    public Action<PointerEvent>? OnPointerMove;
    public Action<PointerEvent>? OnPointerEnter;
    public Action<PointerEvent>? OnPointerExit;
    public Action<PointerEvent>? OnClick;
    public Action<KeyEvent>?     OnKeyDown;
    public Action<KeyEvent>?     OnKeyUp;

    // ── Layout state (set by parent during Arrange) ───────────────────────

    /// Bounds in parent-local coordinates, set by parent's ArrangeCore.
    public Rect LayoutBounds { get; internal set; }

    /// Desired size, set during Measure pass.
    public Size DesiredSize  { get; private set; }

    // ── Dirty flags ────────────────────────────────────────────────────────

    private bool _layoutDirty  = true;
    private bool _visualDirty  = true;

    /// Mark this widget and its ancestors as needing a full layout + redraw.
    public void InvalidateLayout()
    {
        _layoutDirty = true;
        _visualDirty = true;
        Parent?.InvalidateLayout();
    }

    /// Mark only visual as dirty (color change, text change, etc.)
    public void Invalidate()
    {
        _visualDirty = true;
        // Bubble up so the root knows to redraw
        Parent?.Invalidate();
    }

    internal bool IsLayoutDirty  => _layoutDirty;
    internal bool IsVisualDirty  => _visualDirty;

    internal void ClearDirty()
    {
        _layoutDirty = false;
        _visualDirty = false;
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
        ClearDirty();
    }

    // Override in subclasses to measure content.
    protected virtual Size MeasureCore(Size available) => Size.Zero;

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
        {
            using var alphaPaint = new SkiaSharp.SKPaint { Color = SkiaSharp.SKColors.Black.WithAlpha((byte)(Opacity * 255)) };
            ctx.Canvas.SaveLayer(alphaPaint);
        }

        // Background
        if (Background != SkiaSharp.SKColor.Empty)
        {
            using var paint = new SkiaSharp.SKPaint { Color = Background };
            if (CornerRadius > 0)
                ctx.Canvas.DrawRoundRect(LayoutBounds.ToSKRect(), CornerRadius, CornerRadius, paint);
            else
                ctx.Canvas.DrawRect(LayoutBounds.ToSKRect(), paint);
        }

        // Let subclass draw its content
        DrawCore(ctx);

        // Draw children on top
        foreach (var child in _children)
            child.Draw(ctx);

        if (Opacity < 1f) ctx.Canvas.Restore();
        ctx.Canvas.Restore();
    }

    // Override to draw widget-specific content.
    protected virtual void DrawCore(DrawContext ctx) { }

    // ── Hit-testing ────────────────────────────────────────────────────────

    /// Returns the deepest widget at (x, y), or null if nothing hit.
    public Widget? HitTest(float x, float y)
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
