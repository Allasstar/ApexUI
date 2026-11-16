namespace ApexUI.Widgets;

// ── Shared popup-list building blocks used by Dropdown<T> and ContextMenu ─────

/// Scrollable popup panel: Surface background + border + Column of item widgets.
/// Sizes to content height, capped at maxHeight. Width auto-fits content, floored at minWidth.
internal sealed class MenuList : Widget
{
    private readonly Column _column;
    private readonly float _minWidth;
    private readonly float _maxHeight;

    public MenuList(float minWidth = 160f, float maxHeight = 280f)
    {
        _minWidth  = minWidth;
        _maxHeight = maxHeight;

        BackgroundSource = t => t.Surface;
        CornerRadius     = 8f;

        _column = new Column { Spacing = 2f };
        // Scroll keeps the list scrollable when items exceed maxHeight.
        var scroll = new Scroll(_column).HideScrollbar();
        AddChild(new PaddingBox(scroll, new Thickness(4f)));
    }

    public void Add(Widget item)  => _column.Add(item);
    public void Clear()           => _column.RemoveAllChildren();

    // ── Layout ────────────────────────────────────────────────────────────────
    // Always measure content with ∞ height so Scroll reports natural content size
    // (the Scroll.MeasureCore fix already handles this). Then cap at _maxHeight.

    protected override Size MeasureCore(Size available)
    {
        Children[0].Measure(new Size(available.Width, float.PositiveInfinity));
        float w = Math.Max(Children[0].DesiredSize.Width, _minWidth);
        float h = Math.Min(Children[0].DesiredSize.Height, _maxHeight);
        return new Size(w, h);
    }

    protected override void ArrangeCore(Rect r)
    {
        Children[0].Measure(new Size(r.Width, r.Height));
        Children[0].Arrange(r);
    }

    // ── Drawing ───────────────────────────────────────────────────────────────
    // Widget.Draw() renders the Surface background before calling DrawCore.
    // We draw the border on top of it here.

    protected override void DrawCore(DrawContext ctx)
        => ctx.StrokeRoundRect(LayoutBounds, CornerRadius, ctx.Theme.Border.WithAlpha(0.4f));
}

/// Shared clickable row for Dropdown and ContextMenu.
/// When isChecked is provided, 20 px on the left is reserved for a ✓ indicator.
internal sealed class MenuItemWidget : Widget
{
    private readonly Label _label;
    private readonly bool _enabled;
    private readonly Func<bool>? _isChecked;

    private const float PadV    =  6f;
    private const float PadH    = 12f;
    private const float CheckW  = 20f;

    public MenuItemWidget(string text, bool enabled = true, Func<bool>? isChecked = null)
    {
        _enabled   = enabled;
        _isChecked = isChecked;
        _label     = new Label { Text = text, IsHitTestVisible = false };

        // Extra left padding reserved for the check indicator when one is used.
        float padLeft = isChecked is not null ? PadH + CheckW : PadH;
        Padding      = new Thickness(padLeft, PadV, PadH, PadV);
        CornerRadius = 6f;
        IsEnabled    = enabled;
        AddChild(_label);
    }

    protected override Size MeasureCore(Size available)
    {
        _label.Measure(new Size(
            Math.Max(0, available.Width  - Padding.Horizontal),
            Math.Max(0, available.Height - Padding.Vertical)));
        return new Size(
            _label.DesiredSize.Width  + Padding.Horizontal,
            _label.DesiredSize.Height + Padding.Vertical);
    }

    protected override void ArrangeCore(Rect r)
        => _label.Arrange(r.Deflate(Padding));

    protected override void DrawCore(DrawContext ctx)
    {
        var t  = ctx.Theme;
        var bg = IsPressed ? t.SurfacePressed
               : IsHovered ? t.SurfaceHover
               : SKColor.Empty;

        if (bg != SKColor.Empty)
            ctx.FillRoundRect(LayoutBounds, CornerRadius, bg);

        _label.Color = _enabled ? t.OnSurface : t.OnSurfaceMuted;

        if (_isChecked is not null && _isChecked())
            ctx.FillCircle(LayoutBounds.X + PadH + CheckW * 0.5f, LayoutBounds.CenterY, 4f, t.Primary);
    }
}

/// Compact non-interactive section header inside a menu list.
internal sealed class MenuHeaderWidget : Widget
{
    private readonly Label _label;

    public MenuHeaderWidget(string text)
    {
        _label = new Label { Text = text, FontSize = 11f, Bold = true, IsHitTestVisible = false };
        Padding = new Thickness(12f, 4f, 12f, 2f);
        IsHitTestVisible = false;
        AddChild(_label);
    }

    protected override Size MeasureCore(Size available)
    {
        _label.Measure(new Size(
            Math.Max(0, available.Width  - Padding.Horizontal),
            Math.Max(0, available.Height - Padding.Vertical)));
        return new Size(
            _label.DesiredSize.Width  + Padding.Horizontal,
            _label.DesiredSize.Height + Padding.Vertical);
    }

    protected override void ArrangeCore(Rect r) => _label.Arrange(r.Deflate(Padding));
}
