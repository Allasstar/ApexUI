namespace ApexUI.Widgets;

/// A foldable list: a title with Expand all and Collapse all over rows, each a chevron, a title, an optional chip
/// and a short summary that fold open onto a body widget. Any number of rows can be open at once. With MaxVisibleRows
/// set, longer lists show that many rows and a "Show all" link. Like ReorderList, it keeps no model: the app adds
/// the rows, updates their texts with SetRowText and rebuilds when its items change.
///
/// Opening or closing by the user (a header click, Space or Enter, Expand all, Collapse all) raises RowToggled;
/// SetOpen from code is silent. HoverChanged gives the row under the pointer, or -1 once it leaves the list.
/// HighlightedRow draws a row as hovered from code, for a link from elsewhere such as a canvas, and raises nothing.
///
///   var list = new FoldList("Points (3)") { MaxVisibleRows = 6 };
///   list.AddRow("Point 1", fields, chip: "Rounded", summary: "8 px");
///   list.RowToggled += (index, open) => Select(index, open);
public class FoldList : Widget
{
    private const string ExpandAllText = "Expand all";
    private const string CollapseAllText = "Collapse all";

    private readonly Label _title;
    private readonly Column _rows = new() { SpacingSource = t => t.SpacingXs };
    private readonly Button _more;
    private int _hovered = -1;
    private int _highlighted = -1;

    public FoldList(string title = "")
    {
        _title = new Label { Text = title, Bold = true, Grow = 1f, VAlign = VAlign.Center };
        var expand = new Button(ExpandAllText, ExpandAll) { Variant = ButtonVariant.Ghost, IsCompact = true };
        var collapse = new Button(CollapseAllText, CollapseAll) { Variant = ButtonVariant.Ghost, IsCompact = true };
        _more = new Button("", () => ShowAll = !ShowAll) { Variant = ButtonVariant.Ghost, IsCompact = true, HAlign = HAlign.Left, IsVisible = false };
        var header = new Row(_title, expand, collapse) { SpacingSource = t => t.SpacingXs };
        AddChild(new Column(header, _rows, _more) { SpacingSource = t => t.SpacingXs });
    }

    public string Title
    {
        get => _title.Text;
        set => _title.Text = value;
    }

    public int Count => _rows.Children.Count;

    /// Rows shown before "Show all"; 0 shows every row.
    public int MaxVisibleRows
    {
        get;
        set
        {
            value = Math.Max(0, value);
            if (field == value) return;
            field = value;
            ApplyCap();
        }
    }

    /// Whether the rows past MaxVisibleRows are shown too.
    public bool ShowAll
    {
        get;
        set
        {
            if (field == value) return;
            field = value;
            ApplyCap();
        }
    }

    /// A row drawn as hovered whatever the pointer does, or -1. Set from code; raises nothing. An index out of range
    /// highlights no row, and ClearRows resets it.
    public int HighlightedRow
    {
        get => _highlighted;
        set
        {
            if (value < 0 || value >= Count) value = -1;
            if (_highlighted == value) return;
            InvalidateHeader(_highlighted);
            _highlighted = value;
            InvalidateHeader(value);
        }
    }

    /// (index, open): a row the user opened or closed, once per row for Expand all and Collapse all.
    public event Action<int, bool>? RowToggled;

    /// The row under the pointer, or -1 when it is over none.
    public event Action<int>? HoverChanged;

    public FoldList OnToggle(Action<int, bool> action) { RowToggled += action; return this; }
    public FoldList OnHover(Action<int> action) { HoverChanged += action; return this; }

    /// Adds a row at the end and returns its index.
    public int AddRow(string title, Widget body, string chip = "", string summary = "", bool open = false)
    {
        _rows.Add(new FoldRow(this, Count, title, body, chip, summary, open));
        ApplyCap();
        return Count - 1;
    }

    public void ClearRows()
    {
        _rows.RemoveAllChildren();
        _highlighted = -1;
        SetHovered(-1);
        ApplyCap();
    }

    public bool IsOpen(int index) => RowAt(index).IsOpen;

    /// Opens or closes a row from code; raises nothing.
    public void SetOpen(int index, bool open) => RowAt(index).IsOpen = open;

    public string TitleOf(int index) => RowAt(index).Header.Title;

    public Widget BodyOf(int index) => RowAt(index).Body;

    /// Changes a row's chip and summary without rebuilding it; an empty text hides the chip or summary.
    public void SetRowText(int index, string chip, string summary) => RowAt(index).Header.SetTexts(chip, summary);

    /// True when the cap leaves the row out.
    public bool IsHiddenByCap(int index) => !RowAt(index).IsVisible;

    /// Shows every row when the cap hides this one, so the row can be seen.
    public void Reveal(int index)
    {
        if (IsHiddenByCap(index)) ShowAll = true;
    }

    public void ExpandAll() => SetAll(true);

    public void CollapseAll() => SetAll(false);

    private FoldRow RowAt(int index) => (FoldRow)_rows.Children[index];

    private void InvalidateHeader(int index)
    {
        if (index >= 0) RowAt(index).Header.Invalidate();
    }

    private void SetAll(bool open)
    {
        for (int i = 0; i < Count; i++)
        {
            FoldRow row = RowAt(i);
            if (row.IsOpen == open) continue;
            row.IsOpen = open;
            RowToggled?.Invoke(i, open);
        }
    }

    private void Toggle(FoldRow row)
    {
        row.IsOpen = !row.IsOpen;
        RowToggled?.Invoke(row.Index, row.IsOpen);
    }

    private void SetHovered(int index)
    {
        if (_hovered == index) return;
        _hovered = index;
        HoverChanged?.Invoke(index);
    }

    // Resolved from the pointer position, so moving from one row to the next reports once and never passes -1.
    protected override void OnPointerMoved(PointerEventArgs e)
    {
        base.OnPointerMoved(e);
        int hovered = -1;
        for (int i = 0; i < Count && hovered < 0; i++)
        {
            FoldRow row = RowAt(i);
            if (row.IsVisible && row.LayoutBounds.Contains(e.X, e.Y)) hovered = i;
        }

        SetHovered(hovered);
    }

    protected override void OnPointerExited(PointerEventArgs e)
    {
        base.OnPointerExited(e);
        SetHovered(-1);
    }

    private void ApplyCap()
    {
        int cap = MaxVisibleRows;
        bool capped = cap > 0 && Count > cap;
        for (int i = 0; i < Count; i++) RowAt(i).IsVisible = !capped || ShowAll || i < cap;
        _more.IsVisible = capped;
        if (capped) _more.Text = ShowAll ? "Show fewer" : $"Show all ({Count})";
        InvalidateLayout();
    }

    private sealed class FoldRow : Column
    {
        // Rows are only added at the end and cleared all at once, so an index never changes.
        public FoldRow(FoldList owner, int index, string title, Widget body, string chip, string summary, bool open)
        {
            Index = index;
            Header = new RowHeader(owner, this, title, chip, summary);
            Body = body;
            SpacingSource = t => t.SpacingXs;
            Add(Header);
            Add(body);
            IsOpen = open;
        }

        public int Index { get; }

        public RowHeader Header { get; }

        public Widget Body { get; }

        public bool IsOpen
        {
            get;
            set
            {
                field = value;
                Body.IsVisible = value;
                Header.Invalidate();
            }
        }

        protected override Thickness DefaultPadding(Theme theme) => new(theme.SpacingXs);
        protected override float DefaultCornerRadius(Theme theme) => theme.CornerRadiusSmall;

        protected override void DrawCore(DrawContext ctx) => ctx.StrokeRoundRect(LayoutBounds, CornerRadius, ctx.Theme.BorderSubtle);
    }

    private sealed class RowHeader : Row
    {
        private readonly FoldList _owner;
        private readonly FoldRow _row;
        private readonly Label _title;
        private readonly Label _chip;
        private readonly Label _summary;

        public RowHeader(FoldList owner, FoldRow row, string title, string chip, string summary)
        {
            _owner = owner;
            _row = row;
            _title = new Label { Text = title, Grow = 1f, VAlign = VAlign.Center, IsHitTestVisible = false };
            _chip = new Chip { Role = TextRole.Small, VAlign = VAlign.Center, IsHitTestVisible = false, ColorSource = t => t.OnSurfaceMuted, BackgroundSource = t => t.SurfaceHover };
            _summary = new Label { Role = TextRole.Small, VAlign = VAlign.Center, IsHitTestVisible = false, ColorSource = t => t.OnSurfaceMuted };
            SpacingSource = t => t.SpacingSm;
            Focusable = true;
            Add(_title);
            Add(_chip);
            Add(_summary);
            SetTexts(chip, summary);
        }

        public string Title => _title.Text;

        public void SetTexts(string chip, string summary)
        {
            _chip.Text = chip;
            _chip.IsVisible = chip.Length > 0;
            _summary.Text = summary;
            _summary.IsVisible = summary.Length > 0;
        }

        // The chevron sits in the left padding.
        protected override Thickness DefaultPadding(Theme theme) => new(theme.IconSize + theme.SpacingXs, theme.SpacingXs / 2f, theme.SpacingXs, theme.SpacingXs / 2f);
        protected override float DefaultCornerRadius(Theme theme) => theme.CornerRadiusSmall;

        protected override void OnClick(PointerEventArgs e)
        {
            e.Handled = true;
            _owner.Toggle(_row);
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            if (!Activation.IsActivationKey(e)) return;
            e.Handled = true;
            _owner.Toggle(_row);
        }

        protected override void DrawCore(DrawContext ctx)
        {
            Theme t = ctx.Theme;
            if (IsHovered || _owner._highlighted == _row.Index) ctx.FillRoundRect(LayoutBounds, CornerRadius, t.SurfaceHover);
            float half = t.IconSize * 0.5f;
            ctx.DrawChevron(LayoutBounds.X + t.SpacingXs + half, LayoutBounds.CenterY, half * 0.5f, _row.IsOpen, t.OnSurfaceMuted);
        }
    }

    private sealed class Chip : Label
    {
        protected override Thickness DefaultPadding(Theme theme) => new(theme.SpacingSm, 0f);
        protected override float DefaultCornerRadius(Theme theme) => theme.CornerRadiusSmall;
    }
}
