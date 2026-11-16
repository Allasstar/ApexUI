namespace ApexUI.Widgets;

/// Scrollable list that creates, lays out and draws only the rows near the visible viewport.
/// Rows are real children, cached by index; rows that scroll far out of view are removed
/// (and so detached), rows just outside the viewport are kept but hidden.
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
    private IDisposable?       _sourceSubscription;

    private float _scrollOff;
    private readonly Dictionary<int, Widget> _cache    = [];
    private readonly List<int>               _evictBuf = [];  // reused to avoid per-pass alloc

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
            if (next != _scrollOff) { _scrollOff = next; InvalidateLayout(); }
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
            InvalidateLayout();
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
        InvalidateLayout();
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
        int count = _source?.Count ?? 0;
        float totalH = count * RowHeight;
        float w = available.Width;
        float h = float.IsInfinity(available.Height) ? totalH : available.Height;
        return new Size(w, h);
    }

    protected override void ArrangeCore(Rect b)
    {
        int count = _source?.Count ?? 0;
        if (_template is null || count == 0)
        {
            ClearRows();
            return;
        }

        _scrollOff = Math.Clamp(_scrollOff, 0f, Math.Max(0f, count * RowHeight - b.Height));
        float cW    = count * RowHeight > b.Height ? b.Width - SbWidth : b.Width;
        int   first = Math.Max(0, (int)(_scrollOff / RowHeight));
        int   last  = Math.Min(count - 1, (int)((_scrollOff + b.Height) / RowHeight));

        _evictBuf.Clear();
        foreach (var k in _cache.Keys)
            if (k < first - EvictBuf || k > last + EvictBuf) _evictBuf.Add(k);
        foreach (var k in _evictBuf)
        {
            RemoveChild(_cache[k]);
            _cache.Remove(k);
        }

        foreach (var (index, row) in _cache)
            SetVisible(row, index >= first && index <= last);

        for (int i = first; i <= last; i++)
        {
            if (!_cache.TryGetValue(i, out var row))
            {
                row = _template(_source![i], i);
                _cache[i] = row;
                AddChild(row);
            }
            SetVisible(row, true);
            row.Measure(new Size(cW, RowHeight));
            row.Arrange(new Rect(b.X, b.Y + i * RowHeight - _scrollOff, cW, RowHeight));
        }
    }

    // ── Draw ──────────────────────────────────────────────────────────────────

    // Rows are children, drawn by Widget.Draw after this, inside this widget's clip.
    // They are never wider than the content column, so they do not cover the scrollbar.
    protected override void DrawCore(DrawContext ctx)
    {
        var   b      = LayoutBounds;
        float totalH = (_source?.Count ?? 0) * RowHeight;

        if (totalH > b.Height)
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

    // ── Private ───────────────────────────────────────────────────────────────

    private void Refresh()
    {
        ClearRows();
        _scrollOff = 0f;
        InvalidateLayout();
    }

    private void ClearRows()
    {
        if (_cache.Count == 0) return;
        RemoveAllChildren();
        _cache.Clear();
    }

    // Guarded because every change invalidates layout, and this runs during arrange.
    private static void SetVisible(Widget row, bool visible)
    {
        if (row.IsVisible != visible) row.IsVisible = visible;
    }

    private float MaxScrollOffset()
    {
        float totalH = (_source?.Count ?? 0) * RowHeight;
        return Math.Max(0f, totalH - LayoutBounds.Height);
    }
}
