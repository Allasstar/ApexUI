namespace ApexUI.Widgets;

public enum ScrollDirection { Vertical, Horizontal, Both }

/// Scrollable single-child container.
/// Content is clipped to viewport; optional scrollbar thumb drawn on the edge.
/// Wheel events bubble up from any child to this widget.
/// Content is laid out once at its unscrolled position and moved by ChildTransform,
/// so scrolling redraws without a layout pass.
public class Scroll : Widget
{
    private const float WheelSpd = 40f;

    private float _scrollX;
    private float _scrollY;
    private bool  _draggingV;
    private bool  _draggingH;
    private float _dragStart;
    private float _dragScrollStart;
    private readonly Widget _content;

    public bool ShowScrollbar
    {
        get;
        set => SetLayoutProperty(ref field, value);
    } = true;

    public ScrollDirection Direction
    {
        get;
        set => SetLayoutProperty(ref field, value);
    } = ScrollDirection.Vertical;

    public Scroll(Widget content)
    {
        _content = content;
        AddChild(content);
    }

    // Handled only when it actually scrolled, so a nested Scroll at its end passes the wheel outward.
    protected override void OnWheel(WheelEventArgs e)
    {
        bool moved = Direction switch
        {
            ScrollDirection.Horizontal => ScrollBy(vertical: false, (-e.DeltaX - e.DeltaY) * WheelSpd), // both axes for horizontal-only
            ScrollDirection.Vertical   => ScrollBy(vertical: true, -e.DeltaY * WheelSpd),
            _                          => ScrollBy(vertical: false, -e.DeltaX * WheelSpd) | ScrollBy(vertical: true, -e.DeltaY * WheelSpd),
        };
        if (moved) e.Handled = true;
    }

    protected override void OnPointerPressed(PointerEventArgs e)
    {
        if (e.Button == PointerButton.Left && BeginDrag(e.X, e.Y))
            e.Handled = true;
    }

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        if (!_draggingV && !_draggingH) return;
        e.Handled = true;
        UpdateDrag(e.X, e.Y);
    }

    protected override void OnPointerCaptureLost()
    {
        if (!_draggingV && !_draggingH) return;
        _draggingV = _draggingH = false;
        Invalidate();
    }

    public Scroll WithDirection(ScrollDirection dir) { Direction = dir; return this; }
    public Scroll HideScrollbar()                    { ShowScrollbar = false; return this; }

    // ── Geometry ──────────────────────────────────────────────────────────────

    private bool HasV => Direction is ScrollDirection.Vertical   or ScrollDirection.Both;
    private bool HasH => Direction is ScrollDirection.Horizontal or ScrollDirection.Both;

    // The area left for content once the scrollbars take their edge.
    private Size Viewport(float width, float height)
    {
        float bar = ShowScrollbar ? Theme.ScrollbarSize : 0f;
        return new Size(
            HasV ? Math.Max(0f, width  - bar) : width,
            HasH ? Math.Max(0f, height - bar) : height);
    }

    // One scrollbar, from the current layout and offset. Range is how far the thumb can travel.
    private readonly record struct Bar(Rect Track, Rect Thumb, float MaxScroll, float Range);

    private Bar BarFor(bool vertical)
    {
        var   lb      = LayoutBounds;
        var   vp      = Viewport(lb.Width, lb.Height);
        float size    = Theme.ScrollbarSize;
        float view    = vertical ? vp.Height : vp.Width;
        float content = vertical ? _content.DesiredSize.Height : _content.DesiredSize.Width;
        float max     = Math.Max(0f, content - view);
        // Thumb length shows the visible share of the content.
        float thumb   = Math.Max(Theme.ScrollbarMinThumb, view * view / Math.Max(1f, content));
        float range   = Math.Max(0f, view - thumb);
        float offset  = max > 0f ? (vertical ? _scrollY : _scrollX) / max * range : 0f;

        return vertical
            ? new Bar(new Rect(lb.Right - size, lb.Y, size, view), new Rect(lb.Right - size, lb.Y + offset, size, thumb), max, range)
            : new Bar(new Rect(lb.X, lb.Bottom - size, view, size), new Rect(lb.X + offset, lb.Bottom - size, thumb, size), max, range);
    }

    // ── Layout ────────────────────────────────────────────────────────────────

    protected override Size MeasureCore(Size available)
    {
        var vp = Viewport(available.Width, available.Height);
        _content.Measure(new Size(
            HasH ? float.PositiveInfinity : vp.Width,
            HasV ? float.PositiveInfinity : vp.Height));

        // When available is infinite (e.g. inside an Overlay), shrink to content size.
        // When available is finite (normal layout), fill the given space.
        float bars = ShowScrollbar ? Theme.ScrollbarSize : 0f;
        float reportW = float.IsInfinity(available.Width)
            ? _content.DesiredSize.Width  + (HasV ? bars : 0f)
            : available.Width;
        float reportH = float.IsInfinity(available.Height)
            ? _content.DesiredSize.Height + (HasH ? bars : 0f)
            : available.Height;
        return new Size(reportW, reportH);
    }

    protected override void ArrangeCore(Rect r)
    {
        var   vp = Viewport(r.Width, r.Height);
        float cw = HasH ? _content.DesiredSize.Width  : vp.Width;
        float ch = HasV ? _content.DesiredSize.Height : vp.Height;

        _scrollX = Math.Clamp(_scrollX, 0f, Math.Max(0f, cw - vp.Width));
        _scrollY = Math.Clamp(_scrollY, 0f, Math.Max(0f, ch - vp.Height));

        _content.Arrange(new Rect(r.X, r.Y, cw, ch));
        ApplyScroll();
    }

    private void ApplyScroll() => ChildTransform = Transform2D.Translation(-_scrollX, -_scrollY);

    // ── Drawing ───────────────────────────────────────────────────────────────

    protected override void DrawCore(DrawContext ctx)
    {
        if (!ShowScrollbar) return;
        if (HasV) DrawBar(ctx, BarFor(vertical: true),  _draggingV);
        if (HasH) DrawBar(ctx, BarFor(vertical: false), _draggingH);
    }

    private static void DrawBar(DrawContext ctx, Bar bar, bool dragging)
    {
        if (bar.MaxScroll <= 0f) return;
        var   t      = ctx.Theme;
        float radius = t.ScrollbarSize * 0.5f;
        ctx.FillRoundRect(bar.Track, radius, t.ScrollbarTrack);
        ctx.FillRoundRect(bar.Thumb, radius, dragging ? t.ScrollbarThumbActive : t.ScrollbarThumb);
    }

    // ── Scrollbar dragging ────────────────────────────────────────────────────

    // Returns whether the press landed on a scrollbar. A thumb drag captures the pointer,
    // because the press may have landed on content that would otherwise keep the moves.
    private bool BeginDrag(float x, float y)
    {
        if (!ShowScrollbar) return false;
        return (HasV && PressBar(vertical: true, x, y)) || (HasH && PressBar(vertical: false, x, y));
    }

    // A press on the thumb starts a drag; on the track it jumps so the thumb centers on the pointer.
    private bool PressBar(bool vertical, float x, float y)
    {
        var bar = BarFor(vertical);
        if (bar.MaxScroll <= 0f) return false;

        float along = vertical ? y : x;
        if (bar.Thumb.Contains(x, y))
        {
            if (vertical) _draggingV = true; else _draggingH = true;
            _dragStart       = along;
            _dragScrollStart = vertical ? _scrollY : _scrollX;
            CapturePointer();
            Invalidate();
            return true;
        }
        if (!bar.Track.Contains(x, y)) return false;

        float trackStart = vertical ? bar.Track.Y : bar.Track.X;
        float thumbLen   = vertical ? bar.Thumb.Height : bar.Thumb.Width;
        float ratio      = Math.Clamp((along - trackStart - thumbLen * 0.5f) / Math.Max(1f, bar.Range), 0f, 1f);
        SetScroll(vertical, ratio * bar.MaxScroll);
        return true;
    }

    private void UpdateDrag(float x, float y)
    {
        bool vertical = _draggingV;
        var  bar      = BarFor(vertical);
        if (bar.Range <= 0f) return;
        float along = vertical ? y : x;
        SetScroll(vertical, _dragScrollStart + (along - _dragStart) * bar.MaxScroll / bar.Range);
    }

    // ── Offsets ───────────────────────────────────────────────────────────────

    private bool ScrollBy(bool vertical, float delta)
        => SetScroll(vertical, (vertical ? _scrollY : _scrollX) + delta);

    // Clamps to the content and returns whether the offset changed.
    private bool SetScroll(bool vertical, float value)
    {
        var   lb   = LayoutBounds;
        var   vp   = Viewport(lb.Width, lb.Height);
        float max  = vertical
            ? Math.Max(0f, _content.DesiredSize.Height - vp.Height)
            : Math.Max(0f, _content.DesiredSize.Width  - vp.Width);
        float next = Math.Clamp(value, 0f, max);

        ref float current = ref vertical ? ref _scrollY : ref _scrollX;
        if (next == current) return false;
        current = next;
        ApplyScroll();
        return true;
    }
}
