namespace ApexUI.Widgets;

/// Pick-one-from-a-list widget. Opens a MenuList Overlay on left-click.
/// Selected item shown with a ✓ checkmark inside the list.
public class Dropdown<T> : Widget
{
    private readonly List<(T Value, string Label)> _items = [];
    private T? _selected;
    private string _placeholder = "Select...";
    private readonly DropdownTrigger _trigger;
    private readonly MenuList _menuList;
    private readonly Overlay _overlay;
    private Bindable<T>? _binding;
    private bool _suppressBinding;

    public T? SelectedValue
    {
        get => _selected;
        set
        {
            _selected = value;
            SyncText();
            if (!_suppressBinding && _binding is not null)
            {
                _suppressBinding = true;
                _binding.Value = value!;
                _suppressBinding = false;
            }
            OnChanged?.Invoke(value);
        }
    }

    public string Placeholder
    {
        get => _placeholder;
        set { _placeholder = value; SyncText(); }
    }

    public Action<T?>? OnChanged;

    public Dropdown()
    {
        _menuList = new MenuList(minWidth: 160f, maxHeight: 200f);

        _overlay = new Overlay { DismissOnClickOutside = true, MatchAnchorWidth = true };
        _overlay.Content = _menuList;

        _trigger = new DropdownTrigger { Text = _placeholder, IsPlaceholder = true };
        _overlay.OnDismiss = () => _trigger.IsOpen = false;

        _trigger.OnClick = e =>
        {
            if (e.Button != PointerButton.Left) return;
            if (_overlay.IsVisible)
                _overlay.Close();
            else
            {
                _overlay.Open(_trigger);
                _trigger.IsOpen = true;
            }
        };

        AddChild(_trigger);
    }

    // ── Fluent API ────────────────────────────────────────────────────────────

    public Dropdown<T> AddItem(T value, string label)
    {
        _items.Add((value, label));
        RebuildList();
        return this;
    }

    public Dropdown<T> WithPlaceholder(string text) { Placeholder = text; return this; }

    public Dropdown<T> WithSelected(T value) { SelectedValue = value; return this; }

    public Dropdown<T> OnChange(Action<T?> action) { OnChanged = action; return this; }

    public Dropdown<T> Bind(Bindable<T> source)
    {
        _binding = source;
        _suppressBinding = true;
        SelectedValue = source.Value;
        _suppressBinding = false;
        source.Changed += v =>
        {
            if (_suppressBinding) return;
            _suppressBinding = true;
            SelectedValue = v;
            _suppressBinding = false;
        };
        return this;
    }

    // ── Layout pass-through ───────────────────────────────────────────────────

    protected override Size MeasureCore(Size available)
    {
        _trigger.Measure(available);
        // Measure the popup at natural size so its minimum width (from MenuList._minWidth
        // and item content) acts as a floor for the trigger width. This guarantees both
        // are always the same width before MatchAnchorWidth even runs.
        _menuList.Measure(Size.Infinite);
        float w = Math.Max(_trigger.DesiredSize.Width, _menuList.DesiredSize.Width);
        return new Size(w, _trigger.DesiredSize.Height);
    }

    protected override void ArrangeCore(Rect r) => _trigger.Arrange(r);

    // ── Internal ──────────────────────────────────────────────────────────────

    private void SyncText()
    {
        if (_selected is null)
        {
            _trigger.Text          = _placeholder;
            _trigger.IsPlaceholder = true;
            return;
        }
        var match = _items.FirstOrDefault(i => EqualityComparer<T>.Default.Equals(i.Value, _selected));
        _trigger.Text          = match.Label ?? _placeholder;
        _trigger.IsPlaceholder = match.Label is null;
    }

    private void RebuildList()
    {
        _menuList.Clear();
        foreach (var (value, label) in _items)
        {
            var itemValue = value;
            MenuItemWidget? item;
            item = new MenuItemWidget(
                label,
                enabled: true,
                isChecked: () => EqualityComparer<T>.Default.Equals(_selected, itemValue));
            item.OnClick = e =>
            {
                if (e.Button != PointerButton.Left) return;
                SelectedValue = itemValue;
                _overlay.Close();
            };
            _menuList.Add(item);
        }
    }

    // ── Trigger widget ────────────────────────────────────────────────────────

    private sealed class DropdownTrigger : Widget
    {
        private readonly Label _label;
        private bool _isOpen;

        private const float PadH       = 10f;
        private const float ChevronW   = 28f;
        private const float MinW       = 120f;

        public DropdownTrigger()
        {
            CornerRadius = 6f;
            _label = new Label { IsHitTestVisible = false };
            AddChild(_label);
        }

        public string Text { set { _label.Text = value; InvalidateLayout(); } }

        public bool IsOpen { set { _isOpen = value; Invalidate(); } }

        public bool IsPlaceholder { get; set { field = value; Invalidate(); } }

        // ── Layout ───────────────────────────────────────────────────────────

        protected override Size MeasureCore(Size available)
        {
            _label.Measure(new Size(Math.Max(0f, available.Width - PadH - ChevronW), float.PositiveInfinity));
            float w = Math.Max(_label.DesiredSize.Width + PadH + ChevronW, MinW);
            float h = _label.DesiredSize.Height + 14f; // ~7px top+bottom padding
            return new Size(w, h);
        }

        protected override void ArrangeCore(Rect r)
        {
            var lr = new Rect(r.X + PadH, r.Y, r.Width - PadH - ChevronW, r.Height);
            _label.Measure(new Size(lr.Width, lr.Height));
            _label.Arrange(lr);
        }

        // ── Drawing ──────────────────────────────────────────────────────────

        protected override void DrawCore(DrawContext ctx)
        {
            var t  = ctx.Theme;
            var lb = LayoutBounds;

            // Background
            var bg = IsPressed ? t.SurfacePressed
                   : IsHovered ? t.SurfaceHover
                   : t.Surface;
            using (var p = ctx.MakePaint(bg))
                ctx.Canvas.DrawRoundRect(lb.ToSKRect(), CornerRadius, CornerRadius, p);

            // Border — accent + thicker when open
            using (var p = ctx.MakePaint(_isOpen ? t.Primary : t.Border))
            {
                p.IsStroke    = true;
                p.StrokeWidth = _isOpen ? 2f : 1f;
                ctx.Canvas.DrawRoundRect(lb.ToSKRect(), CornerRadius, CornerRadius, p);
            }

            // Divider between label area and chevron column
            float divX = lb.Right - ChevronW;
            using (var p = ctx.MakePaint(t.Border.WithAlpha(0.5f)))
            {
                p.IsStroke    = true;
                p.StrokeWidth = 1f;
                ctx.Canvas.DrawLine(divX, lb.Y + 6f, divX, lb.Bottom - 6f, p);
            }

            // Label color
            _label.Color = IsPlaceholder ? t.OnSurfaceMuted : t.OnSurface;

            // Chevron
            DrawChevron(ctx, lb.Right - ChevronW * 0.5f, lb.CenterY, _isOpen, t.OnSurfaceMuted);
        }

        private static void DrawChevron(DrawContext ctx, float cx, float cy, bool up, SKColor color)
        {
            float s    = 3.5f;
            float sign = up ? -1f : 1f;
            using var path = new SKPath();
            path.MoveTo(cx - s, cy - sign * s * 0.6f);
            path.LineTo(cx,     cy + sign * s * 0.6f);
            path.LineTo(cx + s, cy - sign * s * 0.6f);
            using var p = ctx.MakePaint(color);
            p.IsStroke    = true;
            p.StrokeWidth = 1.5f;
            p.StrokeCap   = SKStrokeCap.Round;
            p.StrokeJoin  = SKStrokeJoin.Round;
            ctx.Canvas.DrawPath(path, p);
        }
    }
}
