namespace ApexUI.Widgets;

/// A VirtualList with one selected row, a hover highlight, keyboard navigation and optional drag reordering.
/// Pressing a row selects it (unless a widget inside the row handled the press); Up/Down, Home/End move
/// the selection while the list has focus. With AllowReorder, dragging a row shows a drop line and, on
/// release, raises Reordered(from, to) with the row's final index. The list never changes the items
/// itself: the app moves the item and refreshes the source.
public class ListView<T> : VirtualList<T>
{
    private const float DragThreshold = 4f;
    private const float DropLineWidth = 2f;

    private int _hoverIndex = -1;
    private int _pressIndex = -1;
    private float _pressY;
    private bool _dragging;
    private int _dropSlot = -1;

    public ListView()
    {
        Focusable = true;
    }

    /// -1 when nothing is selected. Setting a value outside the items selects nothing.
    public int SelectedIndex
    {
        get;
        set
        {
            int index = value >= 0 && value < Count ? value : -1;
            if (field == index) return;
            field = index;
            Invalidate();
            SelectionChanged?.Invoke(index);
        }
    } = -1;

    public bool AllowReorder { get; set; }

    public bool IsDragging => _dragging;

    public event Action<int>? SelectionChanged;

    /// (from, to): the dragged row's index before and after the move.
    public event Action<int, int>? Reordered;

    // The VirtualList setters again, typed as ListView so fluent chains keep the list's own methods.
    public new ListView<T> WithItems(ObservableList<T> source)   { base.WithItems(source); return this; }
    public new ListView<T> WithItems(IReadOnlyList<T> source)    { base.WithItems(source); return this; }
    public new ListView<T> WithTemplate(Func<T, Widget> t)      { base.WithTemplate(t); return this; }
    public new ListView<T> WithTemplate(Func<T, int, Widget> t) { base.WithTemplate(t); return this; }
    public new ListView<T> WithRowHeight(float h)               { base.WithRowHeight(h); return this; }

    public ListView<T> WithReorder(bool allow = true) { AllowReorder = allow; return this; }
    public ListView<T> OnSelectionChange(Action<int> action) { SelectionChanged += action; return this; }
    public ListView<T> OnReorder(Action<int, int> action) { Reordered += action; return this; }

    // ── Input ─────────────────────────────────────────────────────────────────

    protected override void OnPointerPressed(PointerEventArgs e)
    {
        base.OnPointerPressed(e);
        if (e.Handled || e.Button != PointerButton.Left) return;

        int index = RowIndexAt(e.Y);
        if (index < 0) return;
        e.Handled = true;
        SelectedIndex = index;
        _pressIndex = index;
        _pressY = e.Y;
    }

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        base.OnPointerMoved(e);
        if (e.Handled) return;

        if (_pressIndex >= 0 && AllowReorder && !_dragging && MathF.Abs(e.Y - _pressY) >= DragThreshold && CapturePointer())
        {
            _dragging = true;
        }

        if (_dragging)
        {
            e.Handled = true;
            SetDropSlot(DropSlotAt(e.Y));
            return;
        }

        SetHoverIndex(RowIndexAt(e.Y));
    }

    protected override void OnPointerReleased(PointerEventArgs e)
    {
        if (_dragging)
        {
            e.Handled = true;
            int from = _pressIndex;
            int slot = _dropSlot;
            EndDrag();
            // Slots are gaps between rows; a row dropped into either gap beside it stays where it is.
            int to = slot > from ? slot - 1 : slot;
            if (slot >= 0 && to != from) Reordered?.Invoke(from, to);
        }

        _pressIndex = -1;
    }

    protected override void OnPointerCaptureLost()
    {
        base.OnPointerCaptureLost();
        if (_dragging) EndDrag();
        _pressIndex = -1;
    }

    protected override void OnPointerExited(PointerEventArgs e) => SetHoverIndex(-1);

    protected override void OnKeyDown(KeyEventArgs e)
    {
        if (Count == 0 || e.Modifiers != KeyModifiers.None) return;

        int index = e.Key switch
        {
            Key.Up   => SelectedIndex < 0 ? Count - 1 : Math.Max(0, SelectedIndex - 1),
            Key.Down => SelectedIndex < 0 ? 0 : Math.Min(Count - 1, SelectedIndex + 1),
            Key.Home => 0,
            Key.End  => Count - 1,
            _        => -1,
        };
        if (index < 0) return;

        e.Handled = true;
        SelectedIndex = index;
        ScrollIntoView(index);
    }

    // ── Drawing ───────────────────────────────────────────────────────────────

    // Under the rows, so row content stays readable on top of the highlight.
    protected override void DrawCore(DrawContext ctx)
    {
        var t = ctx.Theme;
        if (_hoverIndex >= 0 && _hoverIndex != SelectedIndex && !_dragging)
            ctx.FillRoundRect(RowRect(_hoverIndex), t.CornerRadiusSmall, t.SurfaceHover);
        if (SelectedIndex >= 0)
            ctx.FillRoundRect(RowRect(SelectedIndex), t.CornerRadiusSmall, t.Selection);

        base.DrawCore(ctx);
    }

    protected override void DrawOverChildren(DrawContext ctx)
    {
        if (!_dragging || _dropSlot < 0) return;

        Rect slot = RowRect(_dropSlot);
        ctx.DrawLine(slot.X, slot.Y, slot.Right, slot.Y, ctx.Theme.Primary, DropLineWidth, SKStrokeCap.Round);
    }

    // ── Private ───────────────────────────────────────────────────────────────

    private int DropSlotAt(float y)
    {
        float offset = y - LayoutBounds.Y + ScrollOffset;
        return Math.Clamp((int)MathF.Round(offset / RowHeight), 0, Count);
    }

    private void SetDropSlot(int slot)
    {
        if (_dropSlot == slot) return;
        _dropSlot = slot;
        Invalidate();
    }

    private void SetHoverIndex(int index)
    {
        if (_hoverIndex == index) return;
        _hoverIndex = index;
        Invalidate();
    }

    private void EndDrag()
    {
        _dragging = false;
        _dropSlot = -1;
        Invalidate();
    }
}
