namespace ApexUI.Widgets;

/// A titled group whose body can be collapsed by clicking the header (or Space/Enter on it).
/// Sections stacked in a Column read as an inspector: each draws a quiet line along its top edge.
public class Section : Widget
{
    private sealed class Header : Widget
    {
        private readonly Section _owner;
        private readonly Label   _title;

        public Header(Section owner, string title)
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

        private float ChevronRoom => Theme.IconSize + Theme.SpacingSm;

        protected override Size MeasureCore(Size available)
        {
            _title.Measure(new Size(Math.Max(0f, available.Width - ChevronRoom), available.Height));
            return new Size(_title.DesiredSize.Width + ChevronRoom, Math.Max(_title.DesiredSize.Height, Theme.IconSize));
        }

        protected override void ArrangeCore(Rect r)
            => _title.Arrange(new Rect(r.X, r.Y, Math.Max(0f, r.Width - ChevronRoom), r.Height));

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

        protected override void DrawCore(DrawContext ctx)
        {
            var t = ctx.Theme;
            if (IsHovered) ctx.FillRect(LayoutBounds, t.SurfaceHover);
            var   c    = ContentBounds;
            float half = t.IconSize * 0.5f;
            // Points up while expanded (click folds it away), down while collapsed.
            ctx.DrawChevron(c.Right - half, c.CenterY, half * 0.5f, _owner.IsExpanded, t.OnSurfaceMuted);
        }
    }

    private readonly Header _header;

    public Section(string title, Widget content, bool expanded = true)
    {
        _header    = new Header(this, title);
        Content    = content;
        IsExpanded = expanded;
        content.IsVisible = expanded;
        AddChild(_header);
        AddChild(content);
    }

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

    protected override void DrawCore(DrawContext ctx)
    {
        var b = LayoutBounds;
        ctx.DrawLine(b.X, b.Y + 0.5f, b.Right, b.Y + 0.5f, ctx.Theme.BorderSubtle, 1f, SKStrokeCap.Butt);
    }
}
