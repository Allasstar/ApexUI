namespace ApexUI.Widgets;

public enum ScrollDirection { Vertical, Horizontal, Both }

/// Scrollable single-child container.
/// Content is clipped to viewport; optional scrollbar thumb drawn on the edge.
/// Mouse-wheel scrolling bubbles up from any child to this widget.
public class Scroll : Widget
{
    private const float BarSize  = 8f;
    private const float MinThumb = 20f;
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
        set { field = value; InvalidateLayout(); }
    } = true;

    public ScrollDirection Direction
    {
        get;
        set { field = value; InvalidateLayout(); }
    } = ScrollDirection.Vertical;

    public Scroll(Widget content)
    {
        _content = content;
        AddChild(content);

        OnScroll = (dx, dy) =>
        {
            if (Direction == ScrollDirection.Horizontal)
                MoveX((-dx - dy) * WheelSpd);     // use both axes for horizontal-only
            else if (Direction == ScrollDirection.Vertical)
                MoveY(-dy * WheelSpd);
            else { MoveX(-dx * WheelSpd); MoveY(-dy * WheelSpd); }
        };

        OnPointerDown = e => BeginDrag(e.X, e.Y);
        OnPointerMove = e => UpdateDrag(e.X, e.Y);
        OnPointerUp   = _ => { _draggingV = false; _draggingH = false; };
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

        _content.Arrange(new Rect(r.X - _scrollX, r.Y - _scrollY, cw, ch));
    }

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
            ctx.FillRoundRect(track, BarSize * 0.5f, t.Border.WithAlpha(0.25f));
            ctx.FillRoundRect(new Rect(lb.Right - BarSize, ty, BarSize, th),
                BarSize * 0.5f,
                _draggingV ? t.OnSurface.WithAlpha(0.6f) : t.OnSurfaceMuted.WithAlpha(0.6f));
        }

        if (hasH && maxSX > 0f)
        {
            var track  = new Rect(lb.X, lb.Bottom - BarSize, vpW, BarSize);
            float tw   = Math.Max(MinThumb, vpW * vpW / _content.DesiredSize.Width);
            float range = vpW - tw;
            float tx   = range > 0f ? lb.X + _scrollX / maxSX * range : lb.X;
            ctx.FillRoundRect(track, BarSize * 0.5f, t.Border.WithAlpha(0.25f));
            ctx.FillRoundRect(new Rect(tx, lb.Bottom - BarSize, tw, BarSize),
                BarSize * 0.5f,
                _draggingH ? t.OnSurface.WithAlpha(0.6f) : t.OnSurfaceMuted.WithAlpha(0.6f));
        }
    }

    // ── Scrollbar dragging ────────────────────────────────────────────────────

    private void BeginDrag(float x, float y)
    {
        if (!ShowScrollbar) return;
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
            }
            else if (track.Contains(x, y))
            {
                float ratio = Math.Clamp((y - track.Y - th * 0.5f) / Math.Max(1f, range), 0f, 1f);
                _scrollY = ratio * maxSY;
                InvalidateLayout();
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
            }
            else if (track.Contains(x, y))
            {
                float ratio = Math.Clamp((x - track.X - tw * 0.5f) / Math.Max(1f, range), 0f, 1f);
                _scrollX = ratio * maxSX;
                InvalidateLayout();
            }
        }
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
                InvalidateLayout();
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
                InvalidateLayout();
            }
        }
    }

    private void MoveX(float delta)
    {
        bool hasV = Direction is ScrollDirection.Vertical   or ScrollDirection.Both;
        float vpW = hasV && ShowScrollbar ? LayoutBounds.Width - BarSize : LayoutBounds.Width;
        float maxSX = Math.Max(0f, _content.DesiredSize.Width - vpW);
        _scrollX = Math.Clamp(_scrollX + delta, 0f, maxSX);
        InvalidateLayout();
    }

    private void MoveY(float delta)
    {
        bool hasH = Direction is ScrollDirection.Horizontal or ScrollDirection.Both;
        float vpH = hasH && ShowScrollbar ? LayoutBounds.Height - BarSize : LayoutBounds.Height;
        float maxSY = Math.Max(0f, _content.DesiredSize.Height - vpH);
        _scrollY = Math.Clamp(_scrollY + delta, 0f, maxSY);
        InvalidateLayout();
    }
}
