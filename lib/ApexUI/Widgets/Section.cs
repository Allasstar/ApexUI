namespace ApexUI.Widgets;

/// A titled group whose body can be collapsed by clicking the header (or Space/Enter on it).
/// Sections stacked in a Column read as an inspector: each header is a tinted band with a disclosure chevron
/// on its left, and an open section marks its header with an accent strip.
public class Section : Widget
{
    private sealed class HeaderBar : Widget
    {
        private readonly Section _owner;
        private readonly Label   _title;

        public HeaderBar(Section owner, string title)
        {
            _owner = owner;
            _title = new Label
            {
                Text             = title,
                Bold             = true,
                IsHitTestVisible = false,
                VAlign           = VAlign.Center,
            };
            AddChild(_title);
            Focusable = true;
        }

        public string Title
        {
            get => _title.Text;
            set => _title.Text = value;
        }

        protected override Thickness DefaultPadding(Theme theme) => theme.SectionHeaderPadding;

        private float ChevronRoom => Theme.IconSize + Theme.SpacingXs;

        protected override Size MeasureCore(Size available)
        {
            _title.Measure(new Size(Math.Max(0f, available.Width - ChevronRoom), available.Height));
            return new Size(_title.DesiredSize.Width + ChevronRoom, Math.Max(_title.DesiredSize.Height, Theme.IconSize));
        }

        protected override void ArrangeCore(Rect r)
            => _title.Arrange(new Rect(r.X + ChevronRoom, r.Y, Math.Max(0f, r.Width - ChevronRoom), r.Height));

        protected override void OnClick(PointerEventArgs e)
        {
            e.Handled = true;
            _owner.IsExpanded = !_owner.IsExpanded;
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            if (!Activation.IsActivationKey(e)) return;
            e.Handled = true;
            _owner.IsExpanded = !_owner.IsExpanded;
        }

        // A tinted band with a disclosure chevron on the left, so every header reads as a clickable bar;
        // an open section also gets an accent strip and a line closing the band off from its body.
        protected override void DrawCore(DrawContext ctx)
        {
            var   t        = ctx.Theme;
            var   b        = LayoutBounds;
            bool  expanded = _owner.IsExpanded;
            ctx.FillRect(b, t.OnSurface.WithAlpha(IsPressed ? 0.12f : IsHovered ? 0.08f : 0.04f));
            ctx.DrawLine(b.X, b.Y + 0.5f, b.Right, b.Y + 0.5f, t.BorderSubtle, 1f, SKStrokeCap.Butt);
            if (expanded)
            {
                ctx.DrawLine(b.X, b.Bottom - 0.5f, b.Right, b.Bottom - 0.5f, t.BorderSubtle, 1f, SKStrokeCap.Butt);
                ctx.FillRect(new Rect(b.X, b.Y, AccentWidth, b.Height), t.Primary);
            }

            var   c     = ContentBounds;
            float cx    = c.X + t.IconSize * 0.5f;
            float cy    = c.CenterY;
            float arm   = t.IconSize * 0.22f;
            var   color = IsHovered || expanded ? t.OnSurface : t.OnSurfaceMuted;
            if (expanded)
            {
                ctx.DrawChevron(cx, cy, arm, pointUp: false, color, 1.75f);
                return;
            }

            ctx.DrawLine(cx - arm * 0.5f, cy - arm, cx + arm * 0.5f, cy, color, 1.75f);
            ctx.DrawLine(cx + arm * 0.5f, cy, cx - arm * 0.5f, cy + arm, color, 1.75f);
        }
    }

    private const float AccentWidth = 2f;

    private readonly HeaderBar _header;

    public Section(string title, Widget content, bool expanded = true)
    {
        _header    = new HeaderBar(this, title);
        Content    = content;
        IsExpanded = expanded;
        content.IsVisible = expanded;
        AddChild(_header);
        AddChild(content);
    }

    public Widget Header => _header;

    public Widget Content { get; }

    public string Title
    {
        get => _header.Title;
        set => _header.Title = value;
    }

    public bool IsExpanded
    {
        get;
        set
        {
            if (field == value) return;
            field = value;
            Content.IsVisible = value;
            _header.Invalidate();
            ExpandedChanged?.Invoke(value);
        }
    }

    public event Action<bool>? ExpandedChanged;

    public Section WithExpanded(bool expanded)       { IsExpanded = expanded; return this; }
    public Section OnExpandedChange(Action<bool> a)  { ExpandedChanged += a; return this; }

    protected override Size MeasureCore(Size available)
    {
        _header.Measure(available);
        float height = _header.DesiredSize.Height;
        float width  = _header.DesiredSize.Width;
        if (Content.IsVisible)
        {
            Content.Measure(new Size(available.Width, Math.Max(0f, available.Height - height)));
            height += Content.DesiredSize.Height;
            width   = Math.Max(width, Content.DesiredSize.Width);
        }
        return new Size(width, height);
    }

    protected override void ArrangeCore(Rect r)
    {
        float headerH = _header.DesiredSize.Height;
        _header.Arrange(new Rect(r.X, r.Y, r.Width, headerH));
        if (Content.IsVisible)
            Content.Arrange(new Rect(r.X, r.Y + headerH, r.Width, Math.Max(0f, r.Height - headerH)));
    }

}
