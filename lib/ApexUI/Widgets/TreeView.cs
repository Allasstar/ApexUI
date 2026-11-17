namespace ApexUI.Widgets;

/// Where a drag dropped: insert into Parent's children (null = the roots) at Index, counted in the order
/// the children accessor returns them, before the dragged items leave their old places.
/// Into is true when the items were dropped onto the Parent row itself.
public readonly record struct TreeDrop<T>(T? Parent, int Index, bool Into);

/// A VirtualList of hierarchical items: each visible row is indented by its depth and containers show an
/// expand arrow. The tree reads children through an accessor and never changes the items itself.
///
/// Selection: click selects one row, Ctrl (Cmd on macOS) toggles a row, Shift selects the range from the
/// last clicked row. Up/Down/Home/End move the selection, Left collapses or goes to the parent, Right
/// expands or goes to the first child.
///
/// Dragging (AllowDrag) moves the selected rows. Between rows, the pointer's x picks the depth of the
/// drop; over the middle of a row the items go inside it, at the visual top of its children. CanDrop
/// refuses drops (the indicator turns Danger and nothing is raised); dropping into a dragged item is
/// always refused. Dropped(items, target) fires on release with the items top to bottom.
///
/// Items are compared with the comparer given to the constructor, so expansion and selection can survive
/// items being replaced by equal copies. Call Refresh after the items change: rows are rebuilt only when
/// the visible items or their depths changed.
public class TreeView<T> : VirtualList<T> where T : notnull
{
    private const float DragThreshold = 4f;
    private const float DropLineWidth = 2f;
    private const float ArrowSize = 4f;
    private const float ArrowLineWidth = 1.5f;
    // The middle half of a row drops into it; the outer quarters drop between rows.
    private const float IntoBand = 0.25f;

    private readonly Func<T, IReadOnlyList<T>> _childrenOf;
    private readonly IEqualityComparer<T> _comparer;
    private readonly HashSet<T> _toggled;
    private readonly HashSet<T> _selected;
    private readonly List<T> _rows = [];
    private readonly List<RowInfo> _info = [];
    private readonly List<T> _nextRows = [];
    private readonly List<RowInfo> _nextInfo = [];
    private readonly List<T> _dragged = [];
    private readonly List<int> _draggedRows = [];
    private IReadOnlyList<T> _roots = [];
    private Func<T, Widget>? _content;
    private bool _anchorSet;
    private T _anchor = default!;

    private int _hoverIndex = -1;
    private int _pressIndex = -1;
    private float _pressY;
    private bool _selectOnRelease;
    private bool _dragging;
    private Drop _drop = Drop.None;

    public TreeView(Func<T, IReadOnlyList<T>> childrenOf, IEqualityComparer<T>? comparer = null)
    {
        _childrenOf = childrenOf;
        _comparer = comparer ?? EqualityComparer<T>.Default;
        _toggled = new HashSet<T>(_comparer);
        _selected = new HashSet<T>(_comparer);
        Focusable = true;
        base.WithItems(_rows);
        base.WithTemplate(BuildRow);
    }

    /// Lists every child list last item first, the way layer stacks show the top layer first.
    public bool ReverseChildren
    {
        get;
        set
        {
            if (field == value) return;
            field = value;
            Refresh();
        }
    }

    /// When true, items start expanded and SetExpanded(item, false) collapses them.
    public bool ExpandByDefault
    {
        get;
        set
        {
            if (field == value) return;
            field = value;
            _toggled.Clear();
            Refresh();
        }
    }

    public bool AllowDrag { get; set; }

    public bool IsDragging => _dragging;

    /// Asked while dragging and before Dropped; false shows a refused indicator.
    public Func<IReadOnlyList<T>, TreeDrop<T>, bool>? CanDrop { get; set; }

    /// The visible items, top to bottom.
    public IReadOnlyList<T> VisibleItems => _rows;

    public int SelectionCount => _selected.Count;

    public event Action? SelectionChanged;

    public event Action<IReadOnlyList<T>, TreeDrop<T>>? Dropped;

    public TreeView<T> WithRoots(IReadOnlyList<T> roots)
    {
        _roots = roots;
        Refresh();
        return this;
    }

    /// The row content; the tree adds the indent and the expand arrow on its left.
    public new TreeView<T> WithTemplate(Func<T, Widget> content)
    {
        _content = content;
        base.Refresh();
        return this;
    }

    public new TreeView<T> WithRowHeight(float h) { base.WithRowHeight(h); return this; }
    public TreeView<T> WithReverseChildren(bool reverse = true) { ReverseChildren = reverse; return this; }
    public TreeView<T> WithExpandByDefault(bool expand = true) { ExpandByDefault = expand; return this; }
    public TreeView<T> WithDrag(bool allow = true) { AllowDrag = allow; return this; }
    public TreeView<T> OnSelectionChange(Action action) { SelectionChanged += action; return this; }
    public TreeView<T> OnDrop(Action<IReadOnlyList<T>, TreeDrop<T>> action) { Dropped += action; return this; }

    /// Re-reads the items. Rows are rebuilt only when the visible items or their depths changed.
    public new void Refresh()
    {
        _nextRows.Clear();
        _nextInfo.Clear();
        Flatten(_roots, -1, 0);

        bool same = _nextRows.Count == _rows.Count;
        for (int i = 0; same && i < _rows.Count; i++)
        {
            same = ReferenceEquals(_rows[i], _nextRows[i]) && _info[i].Depth == _nextInfo[i].Depth && _info[i].HasChildren == _nextInfo[i].HasChildren;
        }

        _rows.Clear();
        _rows.AddRange(_nextRows);
        _info.Clear();
        _info.AddRange(_nextInfo);
        if (_dragging) EndDrag();
        _hoverIndex = -1;
        if (same) Invalidate();
        else base.Refresh();
    }

    public bool IsExpanded(T item) => ExpandByDefault != _toggled.Contains(item);

    public void SetExpanded(T item, bool expanded)
    {
        if (IsExpanded(item) == expanded) return;
        if (!_toggled.Remove(item)) _toggled.Add(item);
        Refresh();
    }

    /// The row of a visible item, or -1.
    public int IndexOf(T item)
    {
        for (int i = 0; i < _rows.Count; i++)
        {
            if (_comparer.Equals(_rows[i], item)) return i;
        }

        return -1;
    }

    public void ScrollIntoView(T item) => ScrollIntoView(IndexOf(item));

    public bool IsSelected(T item) => _selected.Contains(item);

    public void Select(T item)
    {
        if (_selected.Count == 1 && _selected.Contains(item)) return;
        _selected.Clear();
        _selected.Add(item);
        SetAnchor(item);
        SelectionUpdated();
    }

    /// Replaces the selection. Raises SelectionChanged only when it differs.
    public void SetSelection(IReadOnlyList<T> items)
    {
        bool same = items.Count == _selected.Count;
        for (int i = 0; same && i < items.Count; i++) same = _selected.Contains(items[i]);
        if (items.Count > 0) SetAnchor(items[^1]);
        if (same) return;

        _selected.Clear();
        for (int i = 0; i < items.Count; i++) _selected.Add(items[i]);
        SelectionUpdated();
    }

    public void ClearSelection()
    {
        if (_selected.Count == 0) return;
        _selected.Clear();
        SelectionUpdated();
    }

    /// Copies the selected items into target in visible order, then the hidden ones.
    public void GetSelection(List<T> target)
    {
        target.Clear();
        for (int i = 0; i < _rows.Count; i++)
        {
            if (_selected.Contains(_rows[i])) target.Add(_rows[i]);
        }

        if (target.Count == _selected.Count) return;
        foreach (T item in _selected)
        {
            if (IndexOf(item) < 0) target.Add(item);
        }
    }

    // ── Input ─────────────────────────────────────────────────────────────────

    protected override void OnPointerPressed(PointerEventArgs e)
    {
        base.OnPointerPressed(e);
        if (e.Handled || e.Button != PointerButton.Left) return;

        int index = RowIndexAt(e.Y);
        if (index < 0) return;
        e.Handled = true;
        T item = _rows[index];

        if (_info[index].HasChildren && IsOnArrow(index, e.X))
        {
            SetExpanded(item, !IsExpanded(item));
            return;
        }

        bool toggle = (e.Modifiers & KeyGesture.PrimaryModifier) != 0;
        if (e.Shift && _anchorSet && IndexOf(_anchor) is >= 0 and var from)
        {
            SelectRange(from, index);
            return;
        }

        if (toggle)
        {
            if (!_selected.Remove(item)) _selected.Add(item);
            SetAnchor(item);
            SelectionUpdated();
            return;
        }

        // A press on a selected row keeps the selection so it can be dragged; a click without a drag narrows it.
        _selectOnRelease = _selected.Contains(item) && _selected.Count > 1;
        if (!_selectOnRelease) Select(item);
        else SetAnchor(item);
        _pressIndex = index;
        _pressY = e.Y;
    }

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        base.OnPointerMoved(e);
        if (e.Handled) return;

        if (_pressIndex >= 0 && AllowDrag && !_dragging && MathF.Abs(e.Y - _pressY) >= DragThreshold && CapturePointer())
        {
            BeginDrag();
        }

        if (_dragging)
        {
            e.Handled = true;
            AutoScroll(e.Y);
            SetDrop(DropAt(e.X, e.Y));
            return;
        }

        SetHoverIndex(RowIndexAt(e.Y));
    }

    protected override void OnPointerReleased(PointerEventArgs e)
    {
        if (_dragging)
        {
            e.Handled = true;
            Drop drop = _drop;
            T[] items = [.. _dragged];
            EndDrag();
            if (drop.Allowed) Dropped?.Invoke(items, drop.Target);
        }
        else if (_selectOnRelease && _pressIndex >= 0 && _pressIndex < _rows.Count)
        {
            Select(_rows[_pressIndex]);
        }

        _pressIndex = -1;
        _selectOnRelease = false;
    }

    protected override void OnPointerCaptureLost()
    {
        base.OnPointerCaptureLost();
        if (_dragging) EndDrag();
        _pressIndex = -1;
        _selectOnRelease = false;
    }

    protected override void OnPointerExited(PointerEventArgs e) => SetHoverIndex(-1);

    protected override void OnKeyDown(KeyEventArgs e)
    {
        if (_rows.Count == 0 || (e.Modifiers & ~KeyModifiers.Shift) != KeyModifiers.None) return;

        int lead = _anchorSet ? IndexOf(_anchor) : -1;
        int index = e.Key switch
        {
            Key.Up => lead < 0 ? _rows.Count - 1 : Math.Max(0, lead - 1),
            Key.Down => lead < 0 ? 0 : Math.Min(_rows.Count - 1, lead + 1),
            Key.Home => 0,
            Key.End => _rows.Count - 1,
            Key.Left when lead >= 0 && !e.Shift => LeftOf(lead),
            Key.Right when lead >= 0 && !e.Shift => RightOf(lead),
            _ => -2,
        };
        if (index == -2) return;

        e.Handled = true;
        if (index < 0) return;
        if (e.Shift && lead >= 0 && e.Key is (Key.Up or Key.Down or Key.Home or Key.End))
        {
            T anchor = _anchor;
            SelectRange(IndexOf(anchor), index);
            SetAnchor(_rows[index]);
        }
        else
        {
            Select(_rows[index]);
        }

        ScrollIntoView(index);
    }

    // ── Drawing ───────────────────────────────────────────────────────────────

    // Under the rows, so row content stays readable on top of the highlight.
    protected override void DrawCore(DrawContext ctx)
    {
        Theme t = ctx.Theme;
        (int first, int last) = VisibleRange();
        for (int i = first; i <= last; i++)
        {
            bool selected = _selected.Contains(_rows[i]);
            if (selected) ctx.FillRoundRect(RowRect(i), t.CornerRadiusSmall, t.Selection);
            else if (i == _hoverIndex && !_dragging) ctx.FillRoundRect(RowRect(i), t.CornerRadiusSmall, t.SurfaceHover);
            if (_info[i].HasChildren) DrawArrow(ctx, i);
        }

        base.DrawCore(ctx);
    }

    protected override void DrawOverChildren(DrawContext ctx)
    {
        if (!_dragging || _drop.Kind == DropKind.None) return;

        Theme t = ctx.Theme;
        SKColor color = _drop.Allowed ? t.Primary : t.Danger;
        if (_drop.Kind == DropKind.Into)
        {
            ctx.StrokeRoundRect(RowRect(_drop.Row), t.CornerRadiusSmall, color, DropLineWidth);
            return;
        }

        Rect slot = RowRect(_drop.Row);
        float x = slot.X + (_drop.Depth + 1) * t.TreeIndent;
        ctx.DrawLine(x, slot.Y, slot.Right, slot.Y, color, DropLineWidth, SKStrokeCap.Round);
    }

    // ── Private ───────────────────────────────────────────────────────────────

    private Widget BuildRow(T item, int index)
    {
        Widget content = _content?.Invoke(item) ?? new Spacer();
        return new TreeRow(content, index < _info.Count ? _info[index].Depth : 0);
    }

    private void Flatten(IReadOnlyList<T> items, int parentRow, int depth)
    {
        int count = items.Count;
        for (int n = 0; n < count; n++)
        {
            int i = ReverseChildren ? count - 1 - n : n;
            T item = items[i];
            IReadOnlyList<T> children = _childrenOf(item);
            int row = _nextRows.Count;
            _nextRows.Add(item);
            _nextInfo.Add(new RowInfo(depth, parentRow, i, children.Count > 0));
            if (children.Count > 0 && IsExpanded(item)) Flatten(children, row, depth + 1);
        }
    }

    private bool IsOnArrow(int index, float x)
    {
        float left = LayoutBounds.X + _info[index].Depth * Theme.TreeIndent;
        return x >= left && x < left + Theme.TreeIndent;
    }

    private void DrawArrow(DrawContext ctx, int index)
    {
        Theme t = ctx.Theme;
        Rect row = RowRect(index);
        float cx = row.X + (_info[index].Depth + 0.5f) * t.TreeIndent;
        float cy = row.CenterY;
        float s = ArrowSize;
        SKColor color = t.OnSurfaceMuted;
        if (IsExpanded(_rows[index]))
        {
            ctx.DrawLine(cx - s, cy - s * 0.5f, cx, cy + s * 0.5f, color, ArrowLineWidth, SKStrokeCap.Round);
            ctx.DrawLine(cx, cy + s * 0.5f, cx + s, cy - s * 0.5f, color, ArrowLineWidth, SKStrokeCap.Round);
        }
        else
        {
            ctx.DrawLine(cx - s * 0.5f, cy - s, cx + s * 0.5f, cy, color, ArrowLineWidth, SKStrokeCap.Round);
            ctx.DrawLine(cx + s * 0.5f, cy, cx - s * 0.5f, cy + s, color, ArrowLineWidth, SKStrokeCap.Round);
        }
    }

    private (int First, int Last) VisibleRange()
    {
        if (_rows.Count == 0) return (0, -1);
        int first = Math.Max(0, (int)(ScrollOffset / RowHeight));
        int last = Math.Min(_rows.Count - 1, (int)((ScrollOffset + LayoutBounds.Height) / RowHeight));
        return (first, last);
    }

    private void SelectRange(int from, int to)
    {
        if (from > to) (from, to) = (to, from);
        _selected.Clear();
        for (int i = from; i <= to; i++) _selected.Add(_rows[i]);
        SelectionUpdated();
    }

    private void SetAnchor(T item)
    {
        _anchor = item;
        _anchorSet = true;
    }

    private void SelectionUpdated()
    {
        Invalidate();
        SelectionChanged?.Invoke();
    }

    private int LeftOf(int row)
    {
        T item = _rows[row];
        if (_info[row].HasChildren && IsExpanded(item))
        {
            SetExpanded(item, false);
            return -1;
        }

        return _info[row].ParentRow;
    }

    private int RightOf(int row)
    {
        if (!_info[row].HasChildren) return -1;
        T item = _rows[row];
        if (IsExpanded(item)) return row + 1;
        SetExpanded(item, true);
        return -1;
    }

    private void SetHoverIndex(int index)
    {
        if (_hoverIndex == index) return;
        _hoverIndex = index;
        Invalidate();
    }

    // ── Dragging ──────────────────────────────────────────────────────────────

    // A selected row whose ancestor is also selected moves with that ancestor.
    private void BeginDrag()
    {
        _dragged.Clear();
        _draggedRows.Clear();
        for (int i = 0; i < _rows.Count; i++)
        {
            if (!_selected.Contains(_rows[i]) || HasSelectedAncestor(i)) continue;
            _dragged.Add(_rows[i]);
            _draggedRows.Add(i);
        }

        _dragging = _dragged.Count > 0;
        _selectOnRelease = false;
        if (!_dragging) ReleasePointerCapture();
    }

    private bool HasSelectedAncestor(int row)
    {
        for (int p = _info[row].ParentRow; p >= 0; p = _info[p].ParentRow)
        {
            if (_selected.Contains(_rows[p])) return true;
        }

        return false;
    }

    private void EndDrag()
    {
        _dragging = false;
        _drop = Drop.None;
        _dragged.Clear();
        _draggedRows.Clear();
        Invalidate();
    }

    private void AutoScroll(float y)
    {
        Rect b = LayoutBounds;
        (int first, int last) = VisibleRange();
        if (y < b.Y + RowHeight * 0.5f && first > 0) ScrollIntoView(first - 1);
        else if (y > b.Bottom - RowHeight * 0.5f && last < _rows.Count - 1) ScrollIntoView(last + 1);
    }

    private Drop DropAt(float x, float y)
    {
        if (_rows.Count == 0) return Check(new Drop(DropKind.Gap, 0, 0, new TreeDrop<T>(default, 0, false)));

        float offset = y - LayoutBounds.Y + ScrollOffset;
        int row = Math.Clamp((int)(offset / RowHeight), 0, _rows.Count - 1);
        float within = offset / RowHeight - row;
        if (within >= IntoBand && within <= 1f - IntoBand)
        {
            T parent = _rows[row];
            int index = ReverseChildren ? _childrenOf(parent).Count : 0;
            Drop into = Check(new Drop(DropKind.Into, row, 0, new TreeDrop<T>(parent, index, true)));
            if (into.Allowed) return into;
        }

        int slot = Math.Clamp((int)MathF.Round(offset / RowHeight), 0, _rows.Count);
        return Check(GapDrop(slot, x));
    }

    // The gap above row `slot`: the depth runs from the row below's depth up to the row above's.
    private Drop GapDrop(int slot, float x)
    {
        int below = slot < _rows.Count ? slot : -1;
        int above = slot - 1;
        int minDepth = below >= 0 ? _info[below].Depth : 0;
        int maxDepth = above >= 0 ? Math.Max(minDepth, _info[above].Depth) : minDepth;
        int depth = Math.Clamp((int)MathF.Floor((x - LayoutBounds.X) / Theme.TreeIndent) - 1, minDepth, maxDepth);

        if (below >= 0 && depth == minDepth)
        {
            RowInfo info = _info[below];
            int index = ReverseChildren ? info.IndexInParent + 1 : info.IndexInParent;
            return new Drop(DropKind.Gap, slot, depth, new TreeDrop<T>(ParentItem(info), index, false));
        }

        int row = above;
        while (_info[row].Depth > depth) row = _info[row].ParentRow;
        RowInfo target = _info[row];
        int after = ReverseChildren ? target.IndexInParent : target.IndexInParent + 1;
        return new Drop(DropKind.Gap, slot, depth, new TreeDrop<T>(ParentItem(target), after, false));
    }

    private T? ParentItem(RowInfo info) => info.ParentRow >= 0 ? _rows[info.ParentRow] : default;

    private Drop Check(Drop drop)
    {
        bool allowed = !IntoDragged(drop) && (CanDrop?.Invoke(_dragged, drop.Target) ?? true);
        return drop with { Allowed = allowed };
    }

    private bool IntoDragged(Drop drop)
    {
        int row = drop.Kind == DropKind.Into ? drop.Row : ParentRowOf(drop);
        for (; row >= 0; row = _info[row].ParentRow)
        {
            if (_draggedRows.Contains(row)) return true;
        }

        return false;
    }

    private int ParentRowOf(Drop drop)
    {
        if (drop.Target.Parent is not { } parent) return -1;
        for (int i = 0; i < _rows.Count; i++)
        {
            if (ReferenceEquals(_rows[i], parent)) return i;
        }

        return -1;
    }

    private void SetDrop(Drop drop)
    {
        if (_drop == drop) return;
        _drop = drop;
        Invalidate();
    }

    private readonly record struct RowInfo(int Depth, int ParentRow, int IndexInParent, bool HasChildren);

    private enum DropKind { None, Gap, Into }

    private readonly record struct Drop(DropKind Kind, int Row, int Depth, TreeDrop<T> Target, bool Allowed = false)
    {
        public static Drop None => default;
    }

    // Leaves room on the left for the indent and the expand arrow, which the tree draws itself.
    private sealed class TreeRow : Widget
    {
        private readonly int _depth;

        public TreeRow(Widget content, int depth)
        {
            _depth = depth;
            AddChild(content);
        }

        protected override Thickness DefaultPadding(Theme theme) => new(theme.TreeIndent * (_depth + 1), 0f, 0f, 0f);
    }
}
