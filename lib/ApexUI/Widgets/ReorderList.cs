namespace ApexUI.Widgets;

/// A short stack of items of any height (a layer's effects, filters, steps), each a header over an optional body.
/// The header holds a drag grip, an eye that switches the item on and off, the title and a remove button.
/// Dragging a header shows a drop line and, on release, raises Reordered(from, to) with the item's final index;
/// Alt+Up and Alt+Down on a focused header do the same by one place. The eye flips the item's IsOn and raises
/// Toggled; the remove button raises Removed. As with ListView, the list never moves or removes items itself:
/// the app changes its model and rebuilds the items.
///
///   var list = new ReorderList()
///       .OnReorder((from, to) => { Move(from, to); Rebuild(); })
///       .OnToggle((index, on) => model[index].On = on)
///       .OnRemove(index => { model.RemoveAt(index); Rebuild(); });
///   list.AddItem("Outline", outlineFields);
public class ReorderList : Widget
{
    private const float DragThreshold = 4f;
    private const float DropLineWidth = 2f;
    private const string EyeOpenIcon = "M2 12s3.6-7 10-7 10 7 10 7-3.6 7-10 7S2 12 2 12zM12 9a3 3 0 1 0 0 6 3 3 0 0 0 0-6z";
    private const string EyeClosedIcon = "M3 3l18 18M10.6 5.1A10 10 0 0 1 12 5c6.4 0 10 7 10 7a17 17 0 0 1-3.2 4M6.6 6.6C3.7 8.4 2 12 2 12s3.6 7 10 7c1.9 0 3.6-.6 5-1.5";
    private const string RemoveIcon = "M6 6l12 12M18 6L6 18";

    private readonly Column _items = new() { SpacingSource = t => t.SpacingXs };
    private int _pressIndex = -1;
    private float _pressY;
    private bool _dragging;
    private int _dropSlot = -1;

    public ReorderList()
    {
        AddChild(_items);
    }

    public int Count => _items.Children.Count;

    public bool IsDragging => _dragging;

    /// (from, to): the dragged item's index before and after the move.
    public event Action<int, int>? Reordered;

    /// (index, on): the item's state after its eye was clicked.
    public event Action<int, bool>? Toggled;

    public event Action<int>? Removed;

    public ReorderList OnReorder(Action<int, int> action) { Reordered += action; return this; }
    public ReorderList OnToggle(Action<int, bool> action) { Toggled += action; return this; }
    public ReorderList OnRemove(Action<int> action) { Removed += action; return this; }

    /// Adds an item at the end and returns its index. The body, if any, is shown under the header.
    public int AddItem(string title, Widget? body = null, bool isOn = true)
    {
        _items.Add(new Item(this, title, body, isOn));
        return Count - 1;
    }

    public void ClearItems()
    {
        if (_dragging) ReleasePointerCapture();
        _pressIndex = -1;
        _items.RemoveAllChildren();
    }

    public string TitleOf(int index) => ItemAt(index).Title;

    public Widget? BodyOf(int index) => ItemAt(index).Body;

    public bool IsOn(int index) => ItemAt(index).IsOn;

    /// Sets the eye from code; raises nothing.
    public void SetOn(int index, bool on) => ItemAt(index).IsOn = on;

    // ── Input ─────────────────────────────────────────────────────────────────

    // Only a header starts a drag: the body's own fields keep their presses.
    protected override void OnPointerPressed(PointerEventArgs e)
    {
        base.OnPointerPressed(e);
        if (e.Handled || e.Button != PointerButton.Left) return;

        int index = HeaderIndexAt(e.X, e.Y);
        if (index < 0) return;
        _pressIndex = index;
        _pressY = e.Y;
    }

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        base.OnPointerMoved(e);
        if (e.Handled || _pressIndex < 0) return;

        if (!_dragging && MathF.Abs(e.Y - _pressY) >= DragThreshold && CapturePointer())
        {
            _dragging = true;
            Invalidate();
        }

        if (!_dragging) return;
        e.Handled = true;
        SetDropSlot(DropSlotAt(e.Y));
    }

    protected override void OnPointerReleased(PointerEventArgs e)
    {
        base.OnPointerReleased(e);
        if (_dragging)
        {
            e.Handled = true;
            int from = _pressIndex;
            int slot = _dropSlot;
            EndDrag();
            // Slots are gaps between items; an item dropped into either gap beside it stays where it is.
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

    // ── Drawing ───────────────────────────────────────────────────────────────

    protected override void DrawCore(DrawContext ctx)
    {
        if (_dragging && _pressIndex >= 0 && _pressIndex < Count)
            ctx.FillRoundRect(_items.Children[_pressIndex].LayoutBounds, ctx.Theme.CornerRadiusSmall, ctx.Theme.Selection);
    }

    protected override void DrawOverChildren(DrawContext ctx)
    {
        if (!_dragging || _dropSlot < 0 || Count == 0) return;

        float half = _items.Spacing * 0.5f;
        float y = _dropSlot < Count ? _items.Children[_dropSlot].LayoutBounds.Y - half : _items.Children[Count - 1].LayoutBounds.Bottom + half;
        Rect bounds = _items.LayoutBounds;
        ctx.DrawLine(bounds.X, y, bounds.Right, y, ctx.Theme.Primary, DropLineWidth, SKStrokeCap.Round);
    }

    // ── Private ───────────────────────────────────────────────────────────────

    private Item ItemAt(int index) => (Item)_items.Children[index];

    private int IndexOf(Item item)
    {
        for (int i = 0; i < Count; i++)
        {
            if (ReferenceEquals(_items.Children[i], item)) return i;
        }

        return -1;
    }

    private int HeaderIndexAt(float x, float y)
    {
        for (int i = 0; i < Count; i++)
        {
            if (ItemAt(i).HeaderBounds.Contains(x, y)) return i;
        }

        return -1;
    }

    private int DropSlotAt(float y)
    {
        int slot = 0;
        while (slot < Count && y > _items.Children[slot].LayoutBounds.CenterY) slot++;
        return slot;
    }

    private void SetDropSlot(int slot)
    {
        if (_dropSlot == slot) return;
        _dropSlot = slot;
        Invalidate();
    }

    private void EndDrag()
    {
        _dragging = false;
        _dropSlot = -1;
        Invalidate();
    }

    private void Toggle(Item item)
    {
        item.IsOn = !item.IsOn;
        int index = IndexOf(item);
        if (index >= 0) Toggled?.Invoke(index, item.IsOn);
    }

    private void Remove(Item item)
    {
        int index = IndexOf(item);
        if (index >= 0) Removed?.Invoke(index);
    }

    private void MoveBy(Item item, int step)
    {
        int from = IndexOf(item);
        int to = from + step;
        if (from >= 0 && to >= 0 && to < Count) Reordered?.Invoke(from, to);
    }

    private sealed class Item : Column
    {
        private readonly Header _header;

        public Item(ReorderList owner, string title, Widget? body, bool isOn)
        {
            _header = new Header(owner, this, title);
            Body = body;
            IsOn = isOn;
            SpacingSource = t => t.SpacingXs;
            Add(_header);
            if (body is not null) Add(body);
        }

        public string Title => _header.Title;

        public Widget? Body { get; }

        public Rect HeaderBounds => _header.LayoutBounds;

        public bool IsOn
        {
            get;
            set
            {
                if (field == value) return;
                field = value;
                _header.ShowOn(value);
            }
        } = true;

        protected override Thickness DefaultPadding(Theme theme) => new(theme.SpacingXs);
        protected override float DefaultCornerRadius(Theme theme) => theme.CornerRadiusSmall;

        protected override void DrawCore(DrawContext ctx) => ctx.StrokeRoundRect(LayoutBounds, CornerRadius, ctx.Theme.BorderSubtle);
    }

    private sealed class Header : Row
    {
        private const float GripDots = 3f;

        private readonly ReorderList _owner;
        private readonly Item _item;
        private readonly Icon _eye;
        private readonly Label _title;

        public Header(ReorderList owner, Item item, string title)
        {
            _owner = owner;
            _item = item;
            _eye = new Icon(EyeOpenIcon, IconStyle.Stroke);
            _title = new Label { Text = title, Grow = 1f, VAlign = VAlign.Center, IsHitTestVisible = false };
            var eye = new IconButton(_eye, () => _owner.Toggle(_item)) { Cursor = CursorShape.Arrow, VAlign = VAlign.Center };
            var remove = new IconButton(new Icon(RemoveIcon, IconStyle.Stroke), () => _owner.Remove(_item)) { Cursor = CursorShape.Arrow, VAlign = VAlign.Center };
            SpacingSource = t => t.SpacingXs;
            Focusable = true;
            Cursor = CursorShape.Move;
            Add(eye);
            Add(_title);
            Add(remove);
        }

        public string Title => _title.Text;

        public void ShowOn(bool on)
        {
            _eye.PathData = on ? EyeOpenIcon : EyeClosedIcon;
            _title.ColorSource = on ? null : t => t.OnSurfaceMuted;
        }

        // The grip's dots sit in the left padding.
        protected override Thickness DefaultPadding(Theme theme) => new(theme.IconSize * 0.75f, 0f, 0f, 0f);
        protected override float DefaultCornerRadius(Theme theme) => theme.CornerRadiusSmall;

        protected override void OnKeyDown(KeyEventArgs e)
        {
            if (e.Modifiers != KeyModifiers.Alt || e.Key is not (Key.Up or Key.Down)) return;
            e.Handled = true;
            _owner.MoveBy(_item, e.Key == Key.Up ? -1 : 1);
        }

        // Two columns of dots at the left mark where the item is dragged from.
        protected override void DrawCore(DrawContext ctx)
        {
            Theme t = ctx.Theme;
            if (IsHovered && !_owner.IsDragging) ctx.FillRoundRect(LayoutBounds, CornerRadius, t.SurfaceHover);
            Rect b = LayoutBounds;
            float step = t.IconSize / (GripDots + 1f);
            float radius = step * 0.25f;
            float x0 = b.X + (ContentBounds.X - b.X - step * 1.5f) * 0.5f;
            for (int row = -1; row <= 1; row++)
            {
                float y = b.CenterY + row * step * 1.5f;
                ctx.FillCircle(x0, y, radius, t.OnSurfaceMuted);
                ctx.FillCircle(x0 + step * 1.5f, y, radius, t.OnSurfaceMuted);
            }
        }
    }
}
