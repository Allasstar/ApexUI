namespace ApexUI.Widgets;

/// Scrollable list that creates and lays out only the rows near the visible viewport.
/// Rows are real children, cached by index and laid out at their unscrolled positions;
/// scrolling moves them with ChildTransform, and runs layout only when it reaches rows
/// that are not built yet. Rows far out of view are removed (and so detached).
/// Row height must be uniform. Use Height (or a parent that constrains height) to make
/// the list scrollable; if Height is NaN and available height is infinite it sizes to content.
public class VirtualList<T> : Widget
{
    public float RowHeight
    {
        get;
        set => SetLayoutProperty(ref field, value);
    } = 36f;

    private IReadOnlyList<T>?  _source;
    private Func<T, int, Widget>? _template;
    private IDisposable?       _sourceSubscription;

    private float _scrollOff;
    private readonly Dictionary<int, Widget> _cache    = [];
    private readonly List<int>               _evictBuf = [];  // reused to avoid per-pass alloc

    // Rows built by the last layout; scrolling within them needs no layout.
    private int _builtFirst;
    private int _builtLast = -1;

    private bool  _sbDragging;
    private float _sbDragStartY;
    private float _sbDragStartOff;

    private float SbWidth  => Theme.ScrollbarSize;
    private float MinThumb => Theme.ScrollbarMinThumb;
    private const int   Overscan = 4;   // rows built beyond the visible range
    private const int   EvictBuf = 15;  // rows kept beyond the visible range

    // Handled only when it actually scrolled, so an outer Scroll gets the wheel at the ends.
    protected override void OnWheel(WheelEventArgs e)
    {
        if (ScrollTo(_scrollOff - e.DeltaY * RowHeight * 3f)) e.Handled = true;
    }

    protected override void OnPointerCaptureLost() => _sbDragging = false;

    protected override void OnPointerPressed(PointerEventArgs e)
    {
        if (e.Button != PointerButton.Left) return;
        var b = LayoutBounds;
        float totalH = TotalHeight;
        if (totalH <= b.Height) return;

        float tX = b.Right - SbWidth;
        if (e.X < tX) return;

        float maxOff = totalH - b.Height;
        float tH     = Math.Max(MinThumb, b.Height * b.Height / totalH);
        float tY     = b.Y + (_scrollOff / maxOff) * (b.Height - tH);

        e.Handled = true;
        if (e.Y >= tY && e.Y <= tY + tH)
        {
            _sbDragging    = true;
            _sbDragStartY  = e.Y;
            _sbDragStartOff = _scrollOff;
            CapturePointer();
        }
        else
        {
            float ratio = Math.Clamp((e.Y - b.Y - tH * 0.5f) / (b.Height - tH), 0f, 1f);
            ScrollTo(ratio * maxOff);
        }
    }

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        if (!_sbDragging) return;
        e.Handled = true;
        var b = LayoutBounds;
        float totalH = TotalHeight;
        float maxOff = Math.Max(0f, totalH - b.Height);
        float tH     = Math.Max(MinThumb, b.Height * b.Height / totalH);

        float delta = e.Y - _sbDragStartY;
        ScrollTo(_sbDragStartOff + delta * maxOff / (b.Height - tH));
    }

    // ── Fluent API ────────────────────────────────────────────────────────────

    public VirtualList<T> WithItems(ObservableList<T> source)
    {
        _sourceSubscription?.Dispose();
        _sourceSubscription = Observe(source, Refresh);
        _source = source;
        Refresh();
        return this;
    }

    public VirtualList<T> WithItems(IReadOnlyList<T> source)
    {
        _sourceSubscription?.Dispose();
        _sourceSubscription = null;
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
        float w = available.Width;
        float h = float.IsInfinity(available.Height) ? TotalHeight : available.Height;
        return new Size(w, h);
    }

    protected override void ArrangeCore(Rect b)
    {
        int count = _source?.Count ?? 0;
        if (_template is null || count == 0)
        {
            ClearRows();
            ApplyScroll();
            return;
        }

        _scrollOff = Math.Clamp(_scrollOff, 0f, MaxScrollOffset(b.Height));
        float cW = TotalHeight > b.Height ? b.Width - SbWidth : b.Width;
        var (first, last) = VisibleRows(b.Height);

        _evictBuf.Clear();
        foreach (var k in _cache.Keys)
            if (k < first - EvictBuf || k > last + EvictBuf) _evictBuf.Add(k);
        foreach (var k in _evictBuf)
        {
            RemoveChild(_cache[k]);
            _cache.Remove(k);
        }

        _builtFirst = Math.Max(0, first - Overscan);
        _builtLast  = Math.Min(count - 1, last + Overscan);
        for (int i = _builtFirst; i <= _builtLast; i++)
        {
            if (_cache.ContainsKey(i)) continue;
            var row = _template(_source![i], i);
            _cache[i] = row;
            AddChild(row);
        }

        // Cached rows outside the built range are arranged too; unchanged ones return at once.
        foreach (var (index, row) in _cache)
        {
            row.Measure(new Size(cW, RowHeight));
            row.Arrange(new Rect(b.X, b.Y + index * RowHeight, cW, RowHeight));
        }
        ApplyScroll();
    }

    // ── Draw ──────────────────────────────────────────────────────────────────

    // Rows are children, drawn by Widget.Draw after this, inside this widget's clip.
    // They are never wider than the content column, so they do not cover the scrollbar.
    protected override void DrawCore(DrawContext ctx)
    {
        var   b      = LayoutBounds;
        float totalH = TotalHeight;

        if (totalH > b.Height)
        {
            float maxOff = totalH - b.Height;
            float tX     = b.Right - SbWidth;
            float tH     = Math.Max(MinThumb, b.Height * b.Height / totalH);
            float tY     = b.Y + (_scrollOff / maxOff) * (b.Height - tH);

            var t = ctx.Theme;
            ctx.FillRoundRect(new Rect(tX, b.Y, SbWidth, b.Height), SbWidth * 0.5f, t.ScrollbarTrack);
            ctx.FillRoundRect(new Rect(tX, tY, SbWidth, tH), SbWidth * 0.5f,
                _sbDragging ? t.ScrollbarThumbActive : t.ScrollbarThumb);
        }
    }

    // ── Private ───────────────────────────────────────────────────────────────

    private float TotalHeight => (_source?.Count ?? 0) * RowHeight;

    private float MaxScrollOffset(float viewportHeight) => Math.Max(0f, TotalHeight - viewportHeight);

    private (int First, int Last) VisibleRows(float viewportHeight)
    {
        int count = _source?.Count ?? 0;
        int first = Math.Max(0, (int)(_scrollOff / RowHeight));
        int last  = Math.Min(count - 1, (int)((_scrollOff + viewportHeight) / RowHeight));
        return (first, last);
    }

    // Returns whether the offset changed.
    private bool ScrollTo(float offset)
    {
        float viewportHeight = LayoutBounds.Height;
        float next = Math.Clamp(offset, 0f, MaxScrollOffset(viewportHeight));
        if (next == _scrollOff) return false;
        _scrollOff = next;
        ApplyScroll();

        var (first, last) = VisibleRows(viewportHeight);
        if (first < _builtFirst || last > _builtLast) InvalidateLayout();
        return true;
    }

    private void ApplyScroll() => ChildTransform = Transform2D.Translation(0f, -_scrollOff);

    private void Refresh()
    {
        ClearRows();
        _scrollOff = 0f;
        InvalidateLayout();
    }

    private void ClearRows()
    {
        _builtFirst = 0;
        _builtLast  = -1;
        if (_cache.Count == 0) return;
        RemoveAllChildren();
        _cache.Clear();
    }
}
