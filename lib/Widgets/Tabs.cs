namespace ApexUI.Widgets;

public enum TabPosition { Top, Bottom, Left, Right }

/// Tabbed container with a scrollable tab bar and a content panel.
/// Add tabs with AddTab(title, content). Tab bar scrolls via mouse wheel or drag.
public class Tabs : Widget
{
    // ── Inner types ───────────────────────────────────────────────────────────

    // Horizontal or vertical flow of tab buttons (supports dynamic Add).
    private sealed class TabBar : Widget
    {
        private readonly bool _horizontal;

        public TabBar(bool horizontal) => _horizontal = horizontal;
        public void Add(TabButton btn) => AddChild(btn);

        protected override Size MeasureCore(Size available)
        {
            float total = 0f, cross = 0f;
            foreach (var child in Children)
            {
                if (!child.IsVisible) continue;
                child.Measure(_horizontal
                    ? new Size(float.PositiveInfinity, available.Height)
                    : new Size(available.Width, float.PositiveInfinity));
                if (_horizontal) { total += child.DesiredSize.Width;  cross = Math.Max(cross, child.DesiredSize.Height); }
                else             { total += child.DesiredSize.Height; cross = Math.Max(cross, child.DesiredSize.Width);  }
            }
            return _horizontal ? new Size(total, cross) : new Size(cross, total);
        }

        protected override void ArrangeCore(Rect r)
        {
            float pos = _horizontal ? r.X : r.Y;
            foreach (var child in Children)
            {
                if (!child.IsVisible) continue;
                child.Measure(_horizontal
                    ? new Size(float.PositiveInfinity, r.Height)
                    : new Size(r.Width, float.PositiveInfinity));
                var b = _horizontal
                    ? new Rect(pos, r.Y, child.DesiredSize.Width, r.Height)
                    : new Rect(r.X, pos, r.Width, child.DesiredSize.Height);
                child.Arrange(b);
                pos += _horizontal ? child.DesiredSize.Width : child.DesiredSize.Height;
            }
        }
    }

    private sealed class TabButton : Widget
    {
        private const float PadH = 16f;
        private const float PadV = 10f;

        private readonly Label _label;

        public bool IsActive
        {
            get;
            set { field = value; Invalidate(); }
        }

        public TabPosition Position { get; }

        public TabButton(string title, TabPosition position, Action onSelect)
        {
            Position = position;
            _label   = new Label { Text = title, IsHitTestVisible = false };
            AddChild(_label);
            OnClick = e =>
            {
                if (e.Button != PointerButton.Left) return;
                onSelect();
            };
        }

        protected override Size MeasureCore(Size available)
        {
            _label.Measure(new Size(
                Math.Max(0f, available.Width  - PadH * 2),
                Math.Max(0f, available.Height - PadV * 2)));
            return new Size(
                _label.DesiredSize.Width  + PadH * 2,
                _label.DesiredSize.Height + PadV * 2);
        }

        protected override void ArrangeCore(Rect r)
        {
            _label.Measure(new Size(r.Width - PadH * 2, r.Height - PadV * 2));
            _label.Arrange(new Rect(r.X + PadH, r.Y + PadV, r.Width - PadH * 2, r.Height - PadV * 2));
        }

        protected override void DrawCore(DrawContext ctx)
        {
            var t = ctx.Theme;

            var bg = IsActive  ? t.Background
                   : IsPressed ? t.SurfacePressed
                   : IsHovered ? t.SurfaceHover
                   : SKColor.Empty;

            if (bg != SKColor.Empty)
            {
                using var p = ctx.MakePaint(bg);
                ctx.Canvas.DrawRect(LayoutBounds.ToSKRect(), p);
            }

            // Active indicator line on the content-side edge
            if (IsActive)
            {
                const float T = 2f;
                var b = LayoutBounds;
                SKRect ind = Position switch
                {
                    TabPosition.Top    => new SKRect(b.X, b.Bottom - T, b.Right, b.Bottom),
                    TabPosition.Bottom => new SKRect(b.X, b.Y,          b.Right, b.Y + T),
                    TabPosition.Left   => new SKRect(b.Right - T, b.Y,  b.Right, b.Bottom),
                    TabPosition.Right  => new SKRect(b.X, b.Y,          b.X + T, b.Bottom),
                    _ => SKRect.Empty,
                };
                using var p = ctx.MakePaint(t.Primary);
                ctx.Canvas.DrawRect(ind, p);
            }

            _label.Color = IsActive ? t.Primary
                         : IsHovered ? t.OnSurface
                         : t.OnSurfaceMuted;
        }
    }

    private readonly record struct TabEntry(string Title, Widget Content);

    // ── Tabs state ────────────────────────────────────────────────────────────

    private const float BarThick = 40f;  // tab bar height (Top/Bottom) or width (Left/Right)

    private readonly List<TabEntry>   _tabs    = [];
    private readonly List<TabButton>  _buttons = [];
    private readonly Scroll           _barScroll;
    private readonly TabBar           _bar;
    private int _selectedIndex;

    public TabPosition Position { get; }
    public Action<int>? OnTabChanged;

    public int SelectedIndex
    {
        get => _selectedIndex;
        set => SelectTab(Math.Clamp(value, 0, Math.Max(0, _tabs.Count - 1)));
    }

    public Tabs(TabPosition position = TabPosition.Top)
    {
        Position = position;
        bool horiz = position is TabPosition.Top or TabPosition.Bottom;

        _bar = new TabBar(horiz);
        _barScroll = new Scroll(_bar)
        {
            Direction     = horiz ? ScrollDirection.Horizontal : ScrollDirection.Vertical,
            ShowScrollbar = false,
        };
        AddChild(_barScroll);
    }

    // ── Public API ────────────────────────────────────────────────────────────

    public Tabs AddTab(string title, Widget content)
    {
        int index = _tabs.Count;
        var btn = new TabButton(title, Position, () => SelectTab(index))
        {
            IsActive = index == 0
        };
        _buttons.Add(btn);
        _bar.Add(btn);
        _tabs.Add(new TabEntry(title, content));
        content.IsVisible = index == 0;
        AddChild(content);
        InvalidateLayout();
        return this;
    }

    // ── Layout ────────────────────────────────────────────────────────────────

    protected override Size MeasureCore(Size available)
    {
        var (barRect, contentRect) = SplitRect(new Rect(0, 0, available.Width, available.Height));
        _barScroll.Measure(new Size(barRect.Width, barRect.Height));
        foreach (var tab in _tabs)
            tab.Content.Measure(new Size(contentRect.Width, contentRect.Height));
        return new Size(available.Width, available.Height);
    }

    protected override void ArrangeCore(Rect r)
    {
        var (barRect, contentRect) = SplitRect(r);
        _barScroll.Measure(new Size(barRect.Width, barRect.Height));
        _barScroll.Arrange(barRect);
        foreach (var tab in _tabs)
        {
            tab.Content.Measure(new Size(contentRect.Width, contentRect.Height));
            tab.Content.Arrange(contentRect);
        }
    }

    // ── Drawing ───────────────────────────────────────────────────────────────

    protected override void DrawCore(DrawContext ctx)
    {
        var (barRect, _) = SplitRect(LayoutBounds);
        var lb = LayoutBounds;
        var t  = ctx.Theme;

        // Tab bar background
        using (var p = ctx.MakePaint(t.Surface))
            ctx.Canvas.DrawRect(barRect.ToSKRect(), p);

        // Separator between bar and content
        using var sep = ctx.MakePaint(t.Border);
        SKRect line = Position switch
        {
            TabPosition.Top    => new SKRect(lb.X, barRect.Bottom - 1, lb.Right, barRect.Bottom),
            TabPosition.Bottom => new SKRect(lb.X, barRect.Y,          lb.Right, barRect.Y + 1),
            TabPosition.Left   => new SKRect(barRect.Right - 1, lb.Y,  barRect.Right, lb.Bottom),
            TabPosition.Right  => new SKRect(barRect.X, lb.Y,          barRect.X + 1, lb.Bottom),
            _ => SKRect.Empty,
        };
        ctx.Canvas.DrawRect(line, sep);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private (Rect bar, Rect content) SplitRect(Rect r) => Position switch
    {
        TabPosition.Top    => (new Rect(r.X, r.Y,              r.Width,         BarThick),
                               new Rect(r.X, r.Y + BarThick,   r.Width,         r.Height - BarThick)),
        TabPosition.Bottom => (new Rect(r.X, r.Bottom - BarThick, r.Width,      BarThick),
                               new Rect(r.X, r.Y,              r.Width,         r.Height - BarThick)),
        TabPosition.Left   => (new Rect(r.X, r.Y,              BarThick,        r.Height),
                               new Rect(r.X + BarThick, r.Y,   r.Width - BarThick, r.Height)),
        TabPosition.Right  => (new Rect(r.Right - BarThick, r.Y, BarThick,     r.Height),
                               new Rect(r.X, r.Y,              r.Width - BarThick, r.Height)),
        _                  => (new Rect(r.X, r.Y,              r.Width,         BarThick),
                               new Rect(r.X, r.Y + BarThick,   r.Width,         r.Height - BarThick)),
    };

    private void SelectTab(int index)
    {
        if (index < 0 || index >= _tabs.Count) return;
        _selectedIndex = index;
        for (int i = 0; i < _tabs.Count; i++)
        {
            _tabs[i].Content.IsVisible = i == index;
            _buttons[i].IsActive       = i == index;
        }
        OnTabChanged?.Invoke(index);
        InvalidateLayout();
    }
}
