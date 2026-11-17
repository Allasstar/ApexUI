namespace ApexUI.Layout;

public enum SplitSide { First, Second }

/// Two panes side by side (Horizontal) or stacked (Vertical) with a draggable divider between them.
/// The FixedSide pane is SplitSize long and keeps that size when the panel resizes; the other pane
/// takes the rest, never less than MinOtherSize. When Collapsible, double-clicking the divider or
/// dragging it below half of MinSplitSize hides the fixed pane; the divider stays to bring it back.
public class SplitPanel : Widget
{
    private sealed class Divider : Widget
    {
        private readonly SplitPanel _owner;
        private float _dragStart;
        private float _sizeAtStart;
        private bool  _dragging;

        public Divider(SplitPanel owner)
        {
            _owner = owner;
            Cursor = owner.Horizontal ? CursorShape.ResizeHorizontal : CursorShape.ResizeVertical;
        }

        public bool IsDragging => _dragging;

        protected override void OnPointerPressed(PointerEventArgs e)
        {
            if (e.Button != PointerButton.Left) return;
            e.Handled = true;
            if (e.ClickCount == 2 && _owner.Collapsible)
            {
                _owner.IsCollapsed = !_owner.IsCollapsed;
                return;
            }
            if (!CapturePointer()) return;
            _dragging    = true;
            _dragStart   = Along(e);
            _sizeAtStart = _owner.IsCollapsed ? 0f : _owner.ActualSplitSize;
            Invalidate();
        }

        protected override void OnPointerMoved(PointerEventArgs e)
        {
            if (!_dragging) return;
            e.Handled = true;
            float delta = Along(e) - _dragStart;
            _owner.DragTo(_sizeAtStart + (_owner.FixedSide == SplitSide.First ? delta : -delta));
        }

        protected override void OnPointerCaptureLost()
        {
            if (!_dragging) return;
            _dragging = false;
            Invalidate();
        }

        private float Along(PointerEventArgs e) => _owner.Horizontal ? e.X : e.Y;

        protected override void DrawCore(DrawContext ctx)
        {
            var   t      = ctx.Theme;
            bool  active = _dragging || IsHovered;
            var   color  = active ? t.Primary : t.Border;
            float width  = active ? 2f : 1f;
            var   b      = LayoutBounds;
            if (_owner.Horizontal) ctx.DrawLine(b.CenterX, b.Y, b.CenterX, b.Bottom, color, width, SKStrokeCap.Butt);
            else                   ctx.DrawLine(b.X, b.CenterY, b.Right, b.CenterY, color, width, SKStrokeCap.Butt);
        }
    }

    private readonly Widget  _first;
    private readonly Widget  _second;
    private readonly Divider _divider;

    public SplitPanel(Widget first, Widget second, Orientation orientation = Orientation.Horizontal)
    {
        Orientation = orientation;
        _first      = first;
        _second     = second;
        _divider    = new Divider(this);
        AddChild(first);
        AddChild(second);
        AddChild(_divider);
    }

    public Orientation Orientation { get; }

    public SplitSide FixedSide
    {
        get;
        set
        {
            if (field == value) return;
            FixedPane.IsVisible = true;
            SetLayoutProperty(ref field, value);
            FixedPane.IsVisible = !IsCollapsed;
        }
    } = SplitSide.First;

    /// Length of the FixedSide pane. Set values are clamped to MinSplitSize..MaxSplitSize;
    /// a panel too small for it shrinks the pane without changing SplitSize.
    public float SplitSize
    {
        get;
        set
        {
            float next = Math.Clamp(value, MinSplitSize, Math.Max(MinSplitSize, MaxSplitSize));
            if (next == field) return;
            SetLayoutProperty(ref field, next);
            SplitSizeChanged?.Invoke(next);
        }
    } = 240f;

    public float MinSplitSize { get; set => SetLayoutProperty(ref field, value); } = 0f;
    public float MaxSplitSize { get; set => SetLayoutProperty(ref field, value); } = float.PositiveInfinity;

    /// The other pane never gets less than this, unless the panel itself is smaller.
    public float MinOtherSize { get; set => SetLayoutProperty(ref field, value); } = 0f;

    public bool Collapsible { get; set; } = true;

    public bool IsCollapsed
    {
        get;
        set
        {
            if (field == value) return;
            field = value;
            FixedPane.IsVisible = !value;
            InvalidateLayout();
            CollapsedChanged?.Invoke(value);
        }
    }

    /// The fixed pane's length after the last arrange (0 while collapsed).
    public float ActualSplitSize { get; private set; }

    public bool IsDragging => _divider.IsDragging;

    public event Action<float>? SplitSizeChanged;
    public event Action<bool>?  CollapsedChanged;

    public SplitPanel WithSplitSize(float size)  { SplitSize = size; return this; }
    public SplitPanel WithFixedSide(SplitSide s) { FixedSide = s; return this; }
    public SplitPanel WithSplitRange(float min, float max) { MinSplitSize = min; MaxSplitSize = max; SplitSize = SplitSize; return this; }
    public SplitPanel WithMinOtherSize(float min) { MinOtherSize = min; return this; }

    private bool   Horizontal => Orientation == Orientation.Horizontal;
    private Widget FixedPane  => FixedSide == SplitSide.First ? _first : _second;
    private Widget OtherPane  => FixedSide == SplitSide.First ? _second : _first;

    private void DragTo(float size)
    {
        if (Collapsible && MinSplitSize > 0f && size < MinSplitSize * 0.5f)
        {
            IsCollapsed = true;
            return;
        }
        IsCollapsed = false;
        SplitSize   = size;
    }

    // ── Layout ────────────────────────────────────────────────────────────────

    private float FixedLength(float total)
    {
        if (IsCollapsed) return 0f;
        float room = total - Theme.SplitterSize - MinOtherSize;
        return Math.Max(0f, Math.Min(SplitSize, room));
    }

    protected override Size MeasureCore(Size available)
    {
        float main    = Horizontal ? available.Width  : available.Height;
        float cross   = Horizontal ? available.Height : available.Width;
        float divider = Theme.SplitterSize;
        float fixedLen = float.IsInfinity(main) ? (IsCollapsed ? 0f : SplitSize) : FixedLength(main);
        float otherLen = float.IsInfinity(main) ? float.PositiveInfinity : Math.Max(0f, main - divider - fixedLen);

        if (!IsCollapsed) FixedPane.Measure(Oriented(fixedLen, cross));
        OtherPane.Measure(Oriented(otherLen, cross));
        _divider.Measure(Oriented(divider, cross));

        float fixedCross = IsCollapsed ? 0f : CrossOf(FixedPane.DesiredSize);
        float desiredMain = float.IsInfinity(main)
            ? fixedLen + divider + MainOf(OtherPane.DesiredSize)
            : main;
        float desiredCross = float.IsInfinity(cross)
            ? Math.Max(fixedCross, CrossOf(OtherPane.DesiredSize))
            : cross;
        return Oriented(desiredMain, desiredCross);
    }

    protected override void ArrangeCore(Rect r)
    {
        float main     = Horizontal ? r.Width : r.Height;
        float divider  = Theme.SplitterSize;
        float fixedLen = FixedLength(main);
        float otherLen = Math.Max(0f, main - divider - fixedLen);
        ActualSplitSize = fixedLen;

        float firstLen = FixedSide == SplitSide.First ? fixedLen : otherLen;
        float secondLen = FixedSide == SplitSide.First ? otherLen : fixedLen;

        if (_first.IsVisible)  _first.Arrange(Slot(r, 0f, firstLen));
        _divider.Arrange(Slot(r, firstLen, divider));
        if (_second.IsVisible) _second.Arrange(Slot(r, firstLen + divider, secondLen));
    }

    private Rect Slot(Rect r, float offset, float length) => Horizontal
        ? new Rect(r.X + offset, r.Y, length, r.Height)
        : new Rect(r.X, r.Y + offset, r.Width, length);

    private Size  Oriented(float main, float cross) => Horizontal ? new Size(main, cross) : new Size(cross, main);
    private float MainOf(Size s)  => Horizontal ? s.Width  : s.Height;
    private float CrossOf(Size s) => Horizontal ? s.Height : s.Width;
}
