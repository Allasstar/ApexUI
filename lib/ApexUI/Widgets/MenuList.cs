namespace ApexUI.Widgets;

// ── Shared popup-list building blocks used by Dropdown<T> and ContextMenu ─────

/// Scrollable popup panel: Surface background + border + Column of item widgets.
/// Sizes to content height, capped at maxHeight. Width auto-fits content, floored at minWidth.
internal sealed class MenuList : Widget
{
    private readonly Column _column;
    private readonly Scroll _scroll;

    public MenuList(float minWidth = 160f, float maxHeight = 280f)
    {
        BackgroundSource = t => t.Surface;
        CornerRadius     = 8f;
        Padding          = new Thickness(4f);
        MinWidth         = minWidth;
        MaxHeight        = maxHeight;

        _column = new Column { Spacing = 2f };
        // Scroll keeps the list scrollable when items exceed maxHeight.
        _scroll = new Scroll(_column).HideScrollbar();
        AddChild(_scroll);
    }

    public void Add(Widget item)  => _column.Add(item);
    public void Clear()           => _column.RemoveAllChildren();

    // Infinite height makes the Scroll report the items' natural height; MaxHeight caps it.
    protected override Size MeasureCore(Size available)
    {
        _scroll.Measure(new Size(available.Width, float.PositiveInfinity));
        return _scroll.DesiredSize;
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
    private readonly Func<bool>? _isChecked;
    private readonly Action _onActivate;

    private const float PadV    =  6f;
    private const float PadH    = 12f;
    private const float CheckW  = 20f;

    public MenuItemWidget(string text, Action onActivate, bool enabled = true, Func<bool>? isChecked = null)
    {
        _isChecked  = isChecked;
        _onActivate = onActivate;

        // Extra left padding reserved for the check indicator when one is used.
        float padLeft = isChecked is not null ? PadH + CheckW : PadH;
        Padding      = new Thickness(padLeft, PadV, PadH, PadV);
        CornerRadius = 6f;
        IsEnabled    = enabled;
        AddChild(new Label
        {
            Text             = text,
            IsHitTestVisible = false,
            ColorSource      = enabled ? t => t.OnSurface : t => t.OnSurfaceMuted,
        });
    }

    protected override void OnClick(PointerEventArgs e)
    {
        e.Handled = true;
        _onActivate();
    }

    protected override void DrawCore(DrawContext ctx)
    {
        var t  = ctx.Theme;
        var bg = IsPressed ? t.SurfacePressed
               : IsHovered ? t.SurfaceHover
               : SKColor.Empty;

        if (bg != SKColor.Empty)
            ctx.FillRoundRect(LayoutBounds, CornerRadius, bg);

        if (_isChecked is not null && _isChecked())
            ctx.FillCircle(LayoutBounds.X + PadH + CheckW * 0.5f, LayoutBounds.CenterY, 4f, t.Primary);
    }
}

/// Compact non-interactive section header inside a menu list.
internal sealed class MenuHeaderWidget : Widget
{
    public MenuHeaderWidget(string text)
    {
        Padding = new Thickness(12f, 4f, 12f, 2f);
        IsHitTestVisible = false;
        AddChild(new Label { Text = text, FontSize = 11f, Bold = true, IsHitTestVisible = false });
    }
}
