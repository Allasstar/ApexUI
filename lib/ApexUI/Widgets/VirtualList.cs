namespace ApexUI.Widgets;

/// Scrollable list that measures and draws only the rows in the visible viewport.
/// Row widgets are cached by index and evicted when they scroll far out of view.
/// Row height must be uniform. Use Height (or a parent that constrains height) to make
/// the list scrollable; if Height is NaN and available height is infinite it sizes to content.
public class VirtualList<T> : Widget
{
    public float RowHeight
    {
        get;
        set { field = value; InvalidateLayout(); }
    } = 36f;

    private IReadOnlyList<T>?  _source;
    private Func<T, int, Widget>? _template;
    private ObservableList<T>? _observedSource;

    private float _scrollOff;
    private readonly Dictionary<int, Widget> _cache    = [];
    private readonly List<int>               _evictBuf = [];  // reused to avoid per-frame alloc
    private int _prevFirst = -1, _prevLast = -1;

    private bool  _sbDragging;
    private float _sbDragStartY;
    private float _sbDragStartOff;

    private const float SbWidth = 7f;  // scrollbar track width
    private const int   EvictBuf = 15; // rows kept beyond visible range

    public VirtualList()
    {
        OnScroll = (_, dy) =>
        {
            float max  = MaxScrollOffset();
            float next = Math.Clamp(_scrollOff - dy * RowHeight * 3f, 0f, max);
            if (next != _scrollOff) { _scrollOff = next; Invalidate(); }
        };
        OnPointerDown = HandlePointerDown;
        OnPointerMove = HandlePointerMove;
        OnPointerUp   = _ => _sbDragging = false;
    }

    private void HandlePointerDown(PointerEvent e)
    {
        if (e.Button != PointerButton.Left) return;
        var b = LayoutBounds;
        int count = _source?.Count ?? 0;
        float totalH = count * RowHeight;
        if (totalH <= b.Height) return;

        float tX = b.Right - SbWidth;
        if (e.X < tX) return;

        float maxOff = totalH - b.Height;
        float tH     = Math.Max(16f, b.Height * b.Height / totalH);
        float tY     = b.Y + (_scrollOff / maxOff) * (b.Height - tH);

        if (e.Y >= tY && e.Y <= tY + tH)
        {
            _sbDragging    = true;
            _sbDragStartY  = e.Y;
            _sbDragStartOff = _scrollOff;
        }
        else
        {
            float ratio = Math.Clamp((e.Y - b.Y - tH * 0.5f) / (b.Height - tH), 0f, 1f);
            _scrollOff = ratio * maxOff;
            Invalidate();
        }
    }

    private void HandlePointerMove(PointerEvent e)
    {
        if (!_sbDragging) return;
        var b = LayoutBounds;
        int count = _source?.Count ?? 0;
        float totalH = count * RowHeight;
        float maxOff = Math.Max(0f, totalH - b.Height);
        float tH     = Math.Max(16f, b.Height * b.Height / totalH);

        float delta = e.Y - _sbDragStartY;
        _scrollOff = Math.Clamp(_sbDragStartOff + delta * maxOff / (b.Height - tH), 0f, maxOff);
        Invalidate();
    }

    // ── Fluent API ────────────────────────────────────────────────────────────

    public VirtualList<T> WithItems(ObservableList<T> source)
    {
        if (_observedSource is not null) _observedSource.Changed -= Refresh;
        _observedSource = source;
        source.Changed += Refresh;
        _source = source;
        Refresh();
        return this;
    }

    public VirtualList<T> WithItems(IReadOnlyList<T> source)
    {
        _observedSource = null;
        _source = source;
        Refresh();
        return this;
    }

    public VirtualList<T> WithTemplate(Func<T, Widget> t)
    {
        _template = (item, _) => t(item);
        Refresh();
        return this;
    }

    public VirtualList<T> WithTemplate(Func<T, int, Widget> t)
    {
        _template = t;
        Refresh();
        return this;
    }

    public VirtualList<T> WithRowHeight(float h) { RowHeight = h; return this; }

    // ── Layout ────────────────────────────────────────────────────────────────

    protected override Size MeasureCore(Size available)
    {
        int count = _source?.Count ?? 0;
        float totalH = count * RowHeight;
        float w = available.Width;
        float h = float.IsInfinity(available.Height) ? totalH : available.Height;
        return new Size(w, h);
    }

    // ── Draw ──────────────────────────────────────────────────────────────────

    protected override void DrawCore(DrawContext ctx)
    {
        if (_source is null || _template is null) return;

        var b     = LayoutBounds;
        int count = _source.Count;
        if (count == 0) return;

        float totalH  = count * RowHeight;
        bool  hasSb   = totalH > b.Height;
        float cW      = hasSb ? b.Width - SbWidth : b.Width;

        int first = Math.Max(0, (int)(_scrollOff / RowHeight));
        int last  = Math.Min(count - 1, (int)((_scrollOff + b.Height) / RowHeight));

        // Evict cache entries far outside the visible window (uses pre-allocated _evictBuf)
        if (first != _prevFirst || last != _prevLast)
        {
            _evictBuf.Clear();
            foreach (var k in _cache.Keys)
                if (k < first - EvictBuf || k > last + EvictBuf) _evictBuf.Add(k);
            foreach (var k in _evictBuf)
            {
                if (_cache.TryGetValue(k, out var old)) old.Parent = null;
                _cache.Remove(k);
            }
            _prevFirst = first;
            _prevLast  = last;
        }

        // Draw visible rows clipped to the content column
        using (ctx.PushClip(new Rect(b.X, b.Y, cW, b.Height)))
        {
            for (int i = first; i <= last; i++)
            {
                if (!_cache.TryGetValue(i, out var widget))
                {
                    widget = _template(_source[i], i);
                    // Link to VirtualList so scroll events bubble up through row children
                    widget.Parent = this;
                    _cache[i] = widget;
                }

                float rowY = b.Y + i * RowHeight - _scrollOff;
                widget.Measure(new Size(cW, RowHeight));
                widget.Arrange(new Rect(b.X, rowY, cW, RowHeight));
                widget.Draw(ctx);
            }
        }

        // Scrollbar
        if (hasSb)
        {
            float maxOff = totalH - b.Height;
            float tX     = b.Right - SbWidth;
            float tH     = Math.Max(16f, b.Height * b.Height / totalH);
            float tY     = b.Y + (_scrollOff / maxOff) * (b.Height - tH);

            ctx.FillRect(
                new Rect(tX, b.Y, SbWidth, b.Height),
                ctx.Theme.Border.WithAlpha(0.15f));
            ctx.FillRoundRect(
                new Rect(tX + 1f, tY, SbWidth - 2f, tH),
                (SbWidth - 2f) * 0.5f,
                ctx.Theme.OnSurfaceMuted.WithAlpha(0.55f));
        }
    }

    // ── Hit-test ──────────────────────────────────────────────────────────────

    public override Widget? HitTest(float x, float y)
    {
        if (!IsVisible || !IsEnabled || !IsHitTestVisible) return null;
        if (!LayoutBounds.Contains(x, y)) return null;

        for (int i = _prevLast; i >= _prevFirst && i >= 0; i--)
        {
            if (!_cache.TryGetValue(i, out var w)) continue;
            var hit = w.HitTest(x, y);
            if (hit is not null) return hit;
        }
        return this;
    }

    // ── Private ───────────────────────────────────────────────────────────────

    private void Refresh()
    {
        foreach (var w in _cache.Values) w.Parent = null;
        _cache.Clear();
        _prevFirst = _prevLast = -1;
        _scrollOff = 0f;
        InvalidateLayout();
    }

    private float MaxScrollOffset()
    {
        float totalH = (_source?.Count ?? 0) * RowHeight;
        return Math.Max(0f, totalH - LayoutBounds.Height);
    }
}
