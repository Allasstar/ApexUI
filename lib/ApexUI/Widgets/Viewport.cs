namespace ApexUI.Widgets;

[Flags]
public enum PointerButtons
{
    None   = 0,
    Left   = 1,
    Right  = 2,
    Middle = 4,
}

/// A pan and zoom camera over an unbounded content space.
/// Subclasses draw in content space by overriding DrawContent; child widgets are laid out once at their
/// natural size from the content origin and moved by ChildTransform, so panning and zooming only redraw.
/// A content point c appears at ViewRect origin + Pan + c × Zoom.
/// Wheel zooms around the pointer; a drag with one of PanButtons pans.
public class Viewport : Widget
{
    private const float ZoomEpsilon = 1e-6f;

    private Rect  _viewRect;
    private float _panX, _panY;
    private float _zoom = 1f;

    private Rect  _fitRect;
    private float _fitPadding;
    private bool  _fitPending;

    private bool  _panning;
    private float _dragStartX, _dragStartY, _panStartX, _panStartY;
    private CursorShape? _cursorBeforePan;

    public Viewport(Widget? content = null)
    {
        if (content is not null) AddChild(content);
    }

    /// Raised after Pan or Zoom changes, from the user or from code.
    public event Action? ViewChanged;

    public float PanX => _panX;
    public float PanY => _panY;
    public float Zoom => _zoom;

    /// The area the camera shows, in this widget's layout space (ContentBounds after the last arrange).
    public Rect ViewRect => _viewRect;

    /// Maps content space into this widget's layout space.
    public Transform2D ViewTransform => new(_viewRect.X + _panX, _viewRect.Y + _panY, _zoom);

    public bool IsPanning => _panning;

    /// Buttons that pan when dragged. Add Left while a pan tool or key is active.
    public PointerButtons PanButtons { get; set; } = PointerButtons.Middle | PointerButtons.Right;

    /// Zoom factor for one wheel notch.
    public float WheelZoomStep { get; set; } = 1.15f;

    /// Cursor shown while a drag pans; the widget's Cursor comes back when the drag ends.
    public CursorShape PanCursor { get; set; } = CursorShape.Move;

    public float MinZoom
    {
        get;
        set { field = value; Apply(_panX, _panY, _zoom); }
    } = 0.1f;

    public float MaxZoom
    {
        get;
        set { field = value; Apply(_panX, _panY, _zoom); }
    } = 10f;

    public (float X, float Y) ContentToView(float x, float y) => ViewTransform.Apply(x, y);

    public (float X, float Y) ViewToContent(float x, float y) => ViewTransform.Invert(x, y);

    public void SetView(float panX, float panY, float zoom)
    {
        _fitPending = false;
        Apply(panX, panY, zoom);
    }

    public void PanBy(float dx, float dy) => SetView(_panX + dx, _panY + dy, _zoom);

    /// Zooms by factor, keeping the content point under (viewX, viewY) in place.
    public void ZoomAt(float viewX, float viewY, float factor)
    {
        float next = ClampZoom(_zoom * factor);
        var (cx, cy) = ViewToContent(viewX, viewY);
        SetView(viewX - _viewRect.X - cx * next, viewY - _viewRect.Y - cy * next, next);
    }

    public void ZoomAtCenter(float factor) => ZoomAt(_viewRect.CenterX, _viewRect.CenterY, factor);

    /// Centers content in the view at the largest zoom that leaves padding on every side.
    /// Until the view is changed some other way, a resize fits again.
    public void Fit(Rect content, float padding)
    {
        _fitRect    = content;
        _fitPadding = padding;
        _fitPending = true;
        ApplyFit();
    }

    public Viewport WithZoomRange(float min, float max) { MinZoom = min; MaxZoom = max; return this; }
    public Viewport WithPanButtons(PointerButtons buttons) { PanButtons = buttons; return this; }

    // ── Layout ────────────────────────────────────────────────────────────────

    protected override Size MeasureCore(Size available)
    {
        foreach (var child in Children)
            child.Measure(Size.Infinite);
        return new Size(
            float.IsInfinity(available.Width)  ? 0f : available.Width,
            float.IsInfinity(available.Height) ? 0f : available.Height);
    }

    protected override void ArrangeCore(Rect finalRect)
    {
        _viewRect = finalRect;
        foreach (var child in Children)
            child.Arrange(new Rect(0f, 0f, child.DesiredSize.Width, child.DesiredSize.Height));
        if (_fitPending) ApplyFit();
        else             Apply(_panX, _panY, _zoom);
    }

    // ── Drawing ───────────────────────────────────────────────────────────────

    /// Draws in content space: the canvas already carries the view transform.
    /// Use a zero stroke width for lines that stay one device pixel wide at any zoom.
    protected virtual void DrawContent(DrawContext ctx) { }

    protected override void DrawCore(DrawContext ctx)
    {
        var canvas = ctx.Canvas;
        canvas.Save();
        canvas.Translate(_viewRect.X + _panX, _viewRect.Y + _panY);
        canvas.Scale(_zoom);
        DrawContent(ctx);
        canvas.Restore();
    }

    // ── Input ─────────────────────────────────────────────────────────────────

    protected override void OnWheel(WheelEventArgs e)
    {
        if (e.DeltaY == 0f) return;
        e.Handled = true;
        ZoomAt(e.X, e.Y, MathF.Pow(WheelZoomStep, e.DeltaY));
    }

    protected override void OnPointerPressed(PointerEventArgs e)
    {
        if (_panning || (PanButtons & ToFlag(e.Button)) == 0) return;
        if (!CapturePointer()) return;
        e.Handled        = true;
        _panning         = true;
        _dragStartX      = e.X;
        _dragStartY      = e.Y;
        _panStartX       = _panX;
        _panStartY       = _panY;
        _cursorBeforePan = Cursor;
        Cursor           = PanCursor;
    }

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        if (!_panning) return;
        e.Handled = true;
        SetView(_panStartX + e.X - _dragStartX, _panStartY + e.Y - _dragStartY, _zoom);
    }

    protected override void OnPointerCaptureLost()
    {
        if (!_panning) return;
        _panning = false;
        Cursor   = _cursorBeforePan;
        Invalidate();
    }

    private static PointerButtons ToFlag(PointerButton button) => button switch
    {
        PointerButton.Left   => PointerButtons.Left,
        PointerButton.Right  => PointerButtons.Right,
        PointerButton.Middle => PointerButtons.Middle,
        _                    => PointerButtons.None,
    };

    // ── View state ────────────────────────────────────────────────────────────

    private void ApplyFit()
    {
        float availW = _viewRect.Width  - 2f * _fitPadding;
        float availH = _viewRect.Height - 2f * _fitPadding;
        if (availW <= 0f || availH <= 0f || _fitRect.Width <= 0f || _fitRect.Height <= 0f) return;

        float zoom = ClampZoom(Math.Min(availW / _fitRect.Width, availH / _fitRect.Height));
        Apply(
            (_viewRect.Width  - _fitRect.Width  * zoom) * 0.5f - _fitRect.X * zoom,
            (_viewRect.Height - _fitRect.Height * zoom) * 0.5f - _fitRect.Y * zoom,
            zoom);
    }

    // MaxZoom wins when the range is set out of order mid-update.
    private float ClampZoom(float zoom) => Math.Min(Math.Max(zoom, MinZoom), MaxZoom);

    private void Apply(float panX, float panY, float zoom)
    {
        zoom = ClampZoom(zoom);
        bool changed = panX != _panX || panY != _panY || MathF.Abs(zoom - _zoom) > ZoomEpsilon;
        _panX = panX;
        _panY = panY;
        _zoom = zoom;
        ChildTransform = ViewTransform;
        if (!changed) return;
        Invalidate();
        ViewChanged?.Invoke();
    }
}
