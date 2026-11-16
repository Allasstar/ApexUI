namespace ApexUI.Widgets;

public enum TabPosition { Top, Bottom, Left, Right }

/// Tabbed container with a scrollable tab bar and a content panel.
/// Add tabs with AddTab(title, content). Tab bar scrolls via mouse wheel or drag.
public class Tabs : Widget
{
    // ── Inner types ───────────────────────────────────────────────────────────

    // Line of tab buttons inside a Scroll along the bar.
    private sealed class TabBar(Orientation orientation) : LinearPanel(orientation)
    {
        public void Add(TabButton btn) => AddChild(btn);
    }

    private sealed class TabButton : Widget
    {
        private readonly Action _onSelect;

        public bool IsActive
        {
            get;
            set => SetRenderProperty(ref field, value);
        }

        public TabPosition Position { get; }

        public TabButton(string title, TabPosition position, Action onSelect)
        {
            Position  = position;
            _onSelect = onSelect;
            Padding   = new Thickness(16f, 10f);
            AddChild(new Label
            {
                Text             = title,
                IsHitTestVisible = false,
                ColorSource      = t => IsActive ? t.Primary : IsHovered ? t.OnSurface : t.OnSurfaceMuted,
            });
        }

        protected override void OnClick(PointerEventArgs e)
        {
            e.Handled = true;
            _onSelect();
        }

        protected override void DrawCore(DrawContext ctx)
        {
            var t = ctx.Theme;

            var bg = IsActive  ? t.Background
                   : IsPressed ? t.SurfacePressed
                   : IsHovered ? t.SurfaceHover
                   : SKColor.Empty;

            if (bg != SKColor.Empty)
                ctx.FillRect(LayoutBounds, bg);

            if (IsActive)
            {
                const float T = 2f;
                var b = LayoutBounds;
                Rect? ind = Position switch
                {
                    TabPosition.Top    => new Rect(b.X, b.Bottom - T, b.Width,  T),
                    TabPosition.Bottom => new Rect(b.X, b.Y,          b.Width,  T),
                    TabPosition.Left   => new Rect(b.Right - T, b.Y,  T, b.Height),
                    TabPosition.Right  => new Rect(b.X, b.Y,          T, b.Height),
                    _ => (Rect?)null,
                };
                if (ind.HasValue) ctx.FillRect(ind.Value, t.Primary);
            }
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

    internal int TabCount => _tabs.Count;

    public int SelectedIndex
    {
        get => _selectedIndex;
        set => SelectTab(Math.Clamp(value, 0, Math.Max(0, _tabs.Count - 1)));
    }

    public Tabs(TabPosition position = TabPosition.Top)
    {
        Position = position;
        bool horiz = position is TabPosition.Top or TabPosition.Bottom;

        _bar = new TabBar(horiz ? Orientation.Horizontal : Orientation.Vertical);
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

        ctx.FillRect(barRect, t.Surface);

        switch (Position)
        {
            case TabPosition.Top:    ctx.DrawLine(lb.X, barRect.Bottom, lb.Right,      barRect.Bottom, t.Border, cap: SKStrokeCap.Butt); break;
            case TabPosition.Bottom: ctx.DrawLine(lb.X, barRect.Y,      lb.Right,      barRect.Y,      t.Border, cap: SKStrokeCap.Butt); break;
            case TabPosition.Left:   ctx.DrawLine(barRect.Right, lb.Y,  barRect.Right, lb.Bottom,      t.Border, cap: SKStrokeCap.Butt); break;
            case TabPosition.Right:  ctx.DrawLine(barRect.X,     lb.Y,  barRect.X,     lb.Bottom,      t.Border, cap: SKStrokeCap.Butt); break;
        }
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
