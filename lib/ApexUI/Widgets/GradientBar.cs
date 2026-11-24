namespace ApexUI.Widgets;

/// One color of a gradient: Offset 0 to 1 along it.
public readonly record struct ColorStop(float Offset, SKColor Color);

/// A gradient strip with a handle per stop. Clicking the strip adds a stop in the color the gradient has there; dragging
/// a handle moves it; dragging it well off the strip, or Delete while focused, removes it, never below MinStops. A press
/// selects a stop: the host edits its color (SetStopColor), for example with a ColorPicker. Stops keep their place in
/// the list whatever their offsets, so an index names the same stop through a drag past another.
/// StopsChanged fires on every change, EditCompleted once per add, remove or finished drag.
public class GradientBar : Widget
{
    public const int MinStops = 2;

    private readonly List<ColorStop> _stops = [];
    private SKColor[] _sortedColors = [];
    private float[] _sortedOffsets = [];
    private bool _gradientDirty = true;
    private SKShader? _shader;
    private SKPaint? _paint;
    private Rect _shaderRect;
    private int _dragging = -1;
    private bool _dragMoved;
    private bool _dragOff;
    private float _grabOffset;

    public GradientBar(params ColorStop[] stops)
    {
        Focusable = true;
        Cursor = CursorShape.Hand;
        if (stops.Length >= MinStops) _stops.AddRange(stops);
        else _stops.AddRange([new ColorStop(0f, SKColors.Black), new ColorStop(1f, SKColors.White)]);
    }

    public IReadOnlyList<ColorStop> Stops => _stops;

    public int SelectedIndex
    {
        get;
        set
        {
            value = Math.Clamp(value, 0, _stops.Count - 1);
            if (field == value) return;
            field = value;
            Invalidate();
            SelectionChanged?.Invoke(value);
        }
    }

    public ColorStop SelectedStop => _stops[SelectedIndex];

    public event Action? StopsChanged;

    public event Action? EditCompleted;

    public event Action<int>? SelectionChanged;

    public GradientBar OnChange(Action action) { StopsChanged += action; return this; }

    public GradientBar OnEditCompleted(Action action) { EditCompleted += action; return this; }

    public GradientBar OnSelectionChange(Action<int> action) { SelectionChanged += action; return this; }

    /// Replaces the stops (at least MinStops; fewer are ignored) without raising StopsChanged. The selection keeps its
    /// index when it still exists.
    public void SetStops(IReadOnlyList<ColorStop> stops)
    {
        if (stops.Count < MinStops) return;
        _stops.Clear();
        for (int i = 0; i < stops.Count; i++) _stops.Add(stops[i] with { Offset = Math.Clamp(stops[i].Offset, 0f, 1f) });
        _dragging = -1;
        StopsUpdated(notify: false);
        SelectedIndex = SelectedIndex;
    }

    public void SetStopColor(int index, SKColor color)
    {
        if ((uint)index >= (uint)_stops.Count || _stops[index].Color == color) return;
        _stops[index] = _stops[index] with { Color = color };
        StopsUpdated(notify: true);
    }

    /// Moves a stop to offset (clamped to 0 to 1), as a drag does; for a position field next to the bar.
    public void SetStopOffset(int index, float offset)
    {
        offset = Math.Clamp(offset, 0f, 1f);
        if ((uint)index >= (uint)_stops.Count || _stops[index].Offset == offset) return;
        _stops[index] = _stops[index] with { Offset = offset };
        StopsUpdated(notify: true);
    }

    public bool RemoveStop(int index)
    {
        if (_stops.Count <= MinStops || (uint)index >= (uint)_stops.Count) return false;
        _stops.RemoveAt(index);
        StopsUpdated(notify: true);
        int selected = Math.Min(SelectedIndex, _stops.Count - 1);
        if (selected == SelectedIndex) SelectionChanged?.Invoke(selected);
        else SelectedIndex = selected;
        EditCompleted?.Invoke();
        return true;
    }

    /// The color the gradient has at offset: stops sorted by offset, interpolated in unpremultiplied RGBA, clamped at
    /// both ends.
    public static SKColor ColorAt(IReadOnlyList<ColorStop> stops, float offset)
    {
        ColorStop below = default, above = default;
        bool hasBelow = false, hasAbove = false;
        for (int i = 0; i < stops.Count; i++)
        {
            var stop = stops[i];
            if (stop.Offset <= offset && (!hasBelow || stop.Offset >= below.Offset))
            {
                below = stop;
                hasBelow = true;
            }

            if (stop.Offset >= offset && (!hasAbove || stop.Offset < above.Offset))
            {
                above = stop;
                hasAbove = true;
            }
        }

        if (!hasBelow) return above.Color;
        if (!hasAbove || above.Offset <= below.Offset) return below.Color;
        float t = (offset - below.Offset) / (above.Offset - below.Offset);
        return new SKColor(
            Lerp(below.Color.Red, above.Color.Red, t),
            Lerp(below.Color.Green, above.Color.Green, t),
            Lerp(below.Color.Blue, above.Color.Blue, t),
            Lerp(below.Color.Alpha, above.Color.Alpha, t));
    }

    private static byte Lerp(byte a, byte b, float t) => (byte)MathF.Round(a + (b - a) * t);

    // ── Layout and drawing ────────────────────────────────────────────────────

    protected override Size MeasureCore(Size available)
    {
        float width = float.IsInfinity(available.Width) ? Theme.ColorAreaSize : available.Width;
        return new Size(width, Theme.ColorStripHeight * 2f);
    }

    protected override float DefaultCornerRadius(Theme theme) => theme.CornerRadiusSmall;

    private float HandleRadius => Theme.ColorStripHeight * 0.75f;

    private Rect StripRect
    {
        get
        {
            var b = ContentBounds;
            float r = HandleRadius;
            float h = Theme.ColorStripHeight;
            return new Rect(b.X + r, b.CenterY - h * 0.5f, Math.Max(0f, b.Width - 2f * r), h);
        }
    }

    private float XOf(float offset)
    {
        var strip = StripRect;
        return strip.X + offset * strip.Width;
    }

    private float OffsetAt(float x)
    {
        var strip = StripRect;
        return strip.Width > 0f ? Math.Clamp((x - strip.X) / strip.Width, 0f, 1f) : 0f;
    }

    protected override void DrawCore(DrawContext ctx)
    {
        var t = ctx.Theme;
        var strip = StripRect;
        using (ctx.PushClip(strip, CornerRadius))
            ctx.FillCheckerboard(strip, strip.Height * 0.5f, t.Surface, t.SurfacePressed);

        if (_gradientDirty || _shader is null || strip != _shaderRect) BuildShader(strip);
        _paint ??= new SKPaint { IsAntialias = true };
        _paint.Shader = _shader;
        ctx.Canvas.DrawRoundRect(strip.ToSKRect(), CornerRadius, CornerRadius, _paint);
        _paint.Shader = null;
        ctx.StrokeRoundRect(strip, CornerRadius, t.Border);

        float r = HandleRadius;
        float cy = strip.CenterY;
        for (int i = 0; i < _stops.Count; i++)
        {
            if (i == SelectedIndex) continue;
            DrawHandle(ctx, i, cy, r, selected: false);
        }

        DrawHandle(ctx, SelectedIndex, cy, r, selected: true);
    }

    private void DrawHandle(DrawContext ctx, int index, float cy, float r, bool selected)
    {
        if (index == _dragging && _dragOff) return;
        var t = ctx.Theme;
        float x = XOf(_stops[index].Offset);
        ctx.FillCircle(x, cy, r, selected ? t.Primary : t.Knob);
        ctx.StrokeCircle(x, cy, r, t.Shadow, 1f);
        ctx.FillCircle(x, cy, r - (selected ? 3f : 2f), _stops[index].Color.WithAlpha(255));
    }

    protected override void OnDetached()
    {
        _shader?.Dispose();
        _shader = null;
        _paint?.Dispose();
        _paint = null;
    }

    private void BuildShader(Rect strip)
    {
        if (_gradientDirty) SortStops();
        _shader?.Dispose();
        _shader = SKShader.CreateLinearGradient(new SKPoint(strip.X, strip.Y), new SKPoint(strip.Right, strip.Y),
            _sortedColors, _sortedOffsets, SKShaderTileMode.Clamp);
        _shaderRect = strip;
        _gradientDirty = false;
    }

    private void SortStops()
    {
        int n = _stops.Count;
        if (_sortedColors.Length != n)
        {
            _sortedColors = new SKColor[n];
            _sortedOffsets = new float[n];
        }

        for (int i = 0; i < n; i++)
        {
            _sortedOffsets[i] = _stops[i].Offset;
            _sortedColors[i] = _stops[i].Color;
        }

        Array.Sort(_sortedOffsets, _sortedColors);
    }

    private void StopsUpdated(bool notify)
    {
        _gradientDirty = true;
        Invalidate();
        if (notify) StopsChanged?.Invoke();
    }

    // ── Input ─────────────────────────────────────────────────────────────────

    private int HandleAt(float x, float y)
    {
        float r = HandleRadius;
        float cy = StripRect.CenterY;
        // The selected handle is on top, so it wins where handles overlap.
        if (Near(SelectedIndex)) return SelectedIndex;
        for (int i = _stops.Count - 1; i >= 0; i--)
        {
            if (Near(i)) return i;
        }

        return -1;

        bool Near(int i)
        {
            float dx = x - XOf(_stops[i].Offset), dy = y - cy;
            return dx * dx + dy * dy <= r * r;
        }
    }

    protected override void OnPointerPressed(PointerEventArgs e)
    {
        if (e.Button != PointerButton.Left) return;
        e.Handled = true;
        int hit = HandleAt(e.X, e.Y);
        if (hit < 0)
        {
            float offset = OffsetAt(e.X);
            _stops.Add(new ColorStop(offset, ColorAt(_stops, offset)));
            StopsUpdated(notify: true);
            hit = _stops.Count - 1;
            SelectedIndex = hit;
            EditCompleted?.Invoke();
        }
        else
        {
            SelectedIndex = hit;
        }

        _dragging = hit;
        _dragMoved = false;
        _dragOff = false;
        _grabOffset = _stops[hit].Offset - OffsetAt(e.X);
        CapturePointer();
    }

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        if (_dragging < 0) return;
        e.Handled = true;
        bool off = _stops.Count > MinStops && MathF.Abs(e.Y - StripRect.CenterY) > LayoutBounds.Height * 1.5f;
        if (off != _dragOff)
        {
            _dragOff = off;
            Invalidate();
        }

        float offset = Math.Clamp(OffsetAt(e.X) + _grabOffset, 0f, 1f);
        if (offset == _stops[_dragging].Offset) return;
        _stops[_dragging] = _stops[_dragging] with { Offset = offset };
        _dragMoved = true;
        StopsUpdated(notify: true);
    }

    protected override void OnPointerReleased(PointerEventArgs e)
    {
        if (_dragging < 0 || e.Button != PointerButton.Left) return;
        e.Handled = true;
        ReleasePointerCapture();
    }

    protected override void OnPointerCaptureLost()
    {
        if (_dragging < 0) return;
        int index = _dragging;
        bool off = _dragOff, moved = _dragMoved;
        _dragging = -1;
        _dragOff = false;
        if (off && RemoveStop(index)) return;
        Invalidate();
        if (moved) EditCompleted?.Invoke();
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        if (e.Key is Key.Delete or Key.Backspace) e.Handled = RemoveStop(SelectedIndex);
    }
}
