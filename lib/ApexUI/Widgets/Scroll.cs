namespace ApexUI.Widgets;

public enum ScrollDirection { Vertical, Horizontal, Both }

/// Scrollable single-child container.
/// Content is clipped to viewport; optional scrollbar thumb drawn on the edge.
/// Wheel events bubble up from any child to this widget.
/// Content is laid out once at its unscrolled position and moved by ChildTransform,
/// so scrolling redraws without a layout pass.
public class Scroll : Widget
{
    private float BarSize  => Theme.ScrollbarSize;
    private float MinThumb => Theme.ScrollbarMinThumb;
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
            ScrollDirection.Horizontal => MoveX((-e.DeltaX - e.DeltaY) * WheelSpd), // both axes for horizontal-only
            ScrollDirection.Vertical   => MoveY(-e.DeltaY * WheelSpd),
            _                          => MoveX(-e.DeltaX * WheelSpd) | MoveY(-e.DeltaY * WheelSpd),
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

    // ── Layout ────────────────────────────────────────────────────────────────

    protected override Size MeasureCore(Size available)
    {
        bool hasV = Direction is ScrollDirection.Vertical   or ScrollDirection.Both;
        bool hasH = Direction is ScrollDirection.Horizontal or ScrollDirection.Both;

        float vpW = hasV && ShowScrollbar ? Math.Max(0f, available.Width  - BarSize) : available.Width;
        float vpH = hasH && ShowScrollbar ? Math.Max(0f, available.Height - BarSize) : available.Height;

        _content.Measure(new Size(
            hasH ? float.PositiveInfinity : vpW,
            hasV ? float.PositiveInfinity : vpH));

        // When available is infinite (e.g. inside an Overlay), shrink to content size.
        // When available is finite (normal layout), fill the given space.
        float reportW = float.IsInfinity(available.Width)
            ? _content.DesiredSize.Width  + (hasV && ShowScrollbar ? BarSize : 0f)
            : available.Width;
        float reportH = float.IsInfinity(available.Height)
            ? _content.DesiredSize.Height + (hasH && ShowScrollbar ? BarSize : 0f)
            : available.Height;
        return new Size(reportW, reportH);
    }

    protected override void ArrangeCore(Rect r)
    {
        bool hasV = Direction is ScrollDirection.Vertical   or ScrollDirection.Both;
        bool hasH = Direction is ScrollDirection.Horizontal or ScrollDirection.Both;

        float vpW = hasV && ShowScrollbar ? r.Width  - BarSize : r.Width;
        float vpH = hasH && ShowScrollbar ? r.Height - BarSize : r.Height;

        float cw = hasH ? _content.DesiredSize.Width  : vpW;
        float ch = hasV ? _content.DesiredSize.Height : vpH;

        _scrollX = Math.Clamp(_scrollX, 0f, Math.Max(0f, cw - vpW));
        _scrollY = Math.Clamp(_scrollY, 0f, Math.Max(0f, ch - vpH));

        _content.Arrange(new Rect(r.X, r.Y, cw, ch));
        ApplyScroll();
    }

    private void ApplyScroll() => ChildTransform = Transform2D.Translation(-_scrollX, -_scrollY);

    // ── Drawing ───────────────────────────────────────────────────────────────

    protected override void DrawCore(DrawContext ctx)
    {
        if (!ShowScrollbar) return;

        bool hasV = Direction is ScrollDirection.Vertical   or ScrollDirection.Both;
        bool hasH = Direction is ScrollDirection.Horizontal or ScrollDirection.Both;
        var lb = LayoutBounds;
        var t  = ctx.Theme;

        float vpW = hasV ? lb.Width  - BarSize : lb.Width;
        float vpH = hasH ? lb.Height - BarSize : lb.Height;
        float maxSX = Math.Max(0f, _content.DesiredSize.Width  - vpW);
        float maxSY = Math.Max(0f, _content.DesiredSize.Height - vpH);

        if (hasV && maxSY > 0f)
        {
            var track  = new Rect(lb.Right - BarSize, lb.Y, BarSize, vpH);
            float th   = Math.Max(MinThumb, vpH * vpH / _content.DesiredSize.Height);
            float range = vpH - th;
            float ty   = range > 0f ? lb.Y + _scrollY / maxSY * range : lb.Y;
            ctx.FillRoundRect(track, BarSize * 0.5f, t.ScrollbarTrack);
            ctx.FillRoundRect(new Rect(lb.Right - BarSize, ty, BarSize, th),
                BarSize * 0.5f,
                _draggingV ? t.ScrollbarThumbActive : t.ScrollbarThumb);
        }

        if (hasH && maxSX > 0f)
        {
            var track  = new Rect(lb.X, lb.Bottom - BarSize, vpW, BarSize);
            float tw   = Math.Max(MinThumb, vpW * vpW / _content.DesiredSize.Width);
            float range = vpW - tw;
            float tx   = range > 0f ? lb.X + _scrollX / maxSX * range : lb.X;
            ctx.FillRoundRect(track, BarSize * 0.5f, t.ScrollbarTrack);
            ctx.FillRoundRect(new Rect(tx, lb.Bottom - BarSize, tw, BarSize),
                BarSize * 0.5f,
                _draggingH ? t.ScrollbarThumbActive : t.ScrollbarThumb);
        }
    }

    // ── Scrollbar dragging ────────────────────────────────────────────────────

    // Returns whether the press landed on a scrollbar. A thumb drag captures the pointer,
    // because the press may have landed on content that would otherwise keep the moves.
    private bool BeginDrag(float x, float y)
    {
        if (!ShowScrollbar) return false;
        var lb = LayoutBounds;
        bool hasV = Direction is ScrollDirection.Vertical   or ScrollDirection.Both;
        bool hasH = Direction is ScrollDirection.Horizontal or ScrollDirection.Both;

        float vpW = hasV ? lb.Width  - BarSize : lb.Width;
        float vpH = hasH ? lb.Height - BarSize : lb.Height;
        float maxSX = Math.Max(0f, _content.DesiredSize.Width  - vpW);
        float maxSY = Math.Max(0f, _content.DesiredSize.Height - vpH);

        if (hasV && maxSY > 0f)
        {
            float th    = Math.Max(MinThumb, vpH * vpH / _content.DesiredSize.Height);
            float range = vpH - th;
            float ty    = range > 0f ? lb.Y + _scrollY / maxSY * range : lb.Y;
            var track   = new Rect(lb.Right - BarSize, lb.Y, BarSize, vpH);
            var thumb   = new Rect(lb.Right - BarSize, ty, BarSize, th);
            if (thumb.Contains(x, y))
            {
                _draggingV = true; _dragStart = y; _dragScrollStart = _scrollY;
                CapturePointer();
                Invalidate();
                return true;
            }
            if (track.Contains(x, y))
            {
                float ratio = Math.Clamp((y - track.Y - th * 0.5f) / Math.Max(1f, range), 0f, 1f);
                _scrollY = ratio * maxSY;
                ApplyScroll();
                return true;
            }
        }

        if (hasH && maxSX > 0f)
        {
            float tw    = Math.Max(MinThumb, vpW * vpW / _content.DesiredSize.Width);
            float range = vpW - tw;
            float tx    = range > 0f ? lb.X + _scrollX / maxSX * range : lb.X;
            var track   = new Rect(lb.X, lb.Bottom - BarSize, vpW, BarSize);
            var thumb   = new Rect(tx, lb.Bottom - BarSize, tw, BarSize);
            if (thumb.Contains(x, y))
            {
                _draggingH = true; _dragStart = x; _dragScrollStart = _scrollX;
                CapturePointer();
                Invalidate();
                return true;
            }
            if (track.Contains(x, y))
            {
                float ratio = Math.Clamp((x - track.X - tw * 0.5f) / Math.Max(1f, range), 0f, 1f);
                _scrollX = ratio * maxSX;
                ApplyScroll();
                return true;
            }
        }
        return false;
    }

    private void UpdateDrag(float x, float y)
    {
        var lb = LayoutBounds;
        bool hasV = Direction is ScrollDirection.Vertical   or ScrollDirection.Both;
        bool hasH = Direction is ScrollDirection.Horizontal or ScrollDirection.Both;

        float vpW = hasV ? lb.Width  - BarSize : lb.Width;
        float vpH = hasH ? lb.Height - BarSize : lb.Height;

        if (_draggingV)
        {
            float maxSY = Math.Max(0f, _content.DesiredSize.Height - vpH);
            float th    = Math.Max(MinThumb, vpH * vpH / Math.Max(1f, _content.DesiredSize.Height));
            float range = vpH - th;
            if (range > 0f)
            {
                _scrollY = Math.Clamp(
                    _dragScrollStart + (y - _dragStart) * maxSY / range, 0f, maxSY);
                ApplyScroll();
            }
        }

        if (_draggingH)
        {
            float maxSX = Math.Max(0f, _content.DesiredSize.Width - vpW);
            float tw    = Math.Max(MinThumb, vpW * vpW / Math.Max(1f, _content.DesiredSize.Width));
            float range = vpW - tw;
            if (range > 0f)
            {
                _scrollX = Math.Clamp(
                    _dragScrollStart + (x - _dragStart) * maxSX / range, 0f, maxSX);
                ApplyScroll();
            }
        }
    }

    private bool MoveX(float delta)
    {
        bool hasV = Direction is ScrollDirection.Vertical   or ScrollDirection.Both;
        float vpW = hasV && ShowScrollbar ? LayoutBounds.Width - BarSize : LayoutBounds.Width;
        float maxSX = Math.Max(0f, _content.DesiredSize.Width - vpW);
        float next  = Math.Clamp(_scrollX + delta, 0f, maxSX);
        if (next == _scrollX) return false;
        _scrollX = next;
        ApplyScroll();
        return true;
    }

    private bool MoveY(float delta)
    {
        bool hasH = Direction is ScrollDirection.Horizontal or ScrollDirection.Both;
        float vpH = hasH && ShowScrollbar ? LayoutBounds.Height - BarSize : LayoutBounds.Height;
        float maxSY = Math.Max(0f, _content.DesiredSize.Height - vpH);
        float next  = Math.Clamp(_scrollY + delta, 0f, maxSY);
        if (next == _scrollY) return false;
        _scrollY = next;
        ApplyScroll();
        return true;
    }
}
