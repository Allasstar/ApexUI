namespace ApexUI.Widgets;

// ── Shared popup-list building blocks used by Dropdown<T> and ContextMenu ─────

/// Popup panel shared by Dropdown and ContextMenu: Surface background + border + a column of items.
/// Sizes to content height, capped at maxHeight (scrollable beyond it). Width auto-fits content, floored at minWidth.
internal sealed class MenuList : Widget
{
    private readonly Column _column;
    private readonly Scroll _scroll;

    public MenuList(float minWidth = 160f, float maxHeight = 280f)
    {
        BackgroundSource = t => t.Surface;
        MinWidth         = minWidth;
        MaxHeight        = maxHeight;

        _column = new Column { Spacing = 2f };
        // Scroll keeps the list scrollable when items exceed maxHeight.
        _scroll = new Scroll(_column).HideScrollbar();
        AddChild(_scroll);
    }

    public void Add(Widget item)  => _column.Add(item);
    public void Clear()           => _column.RemoveAllChildren();

    protected override Thickness DefaultPadding(Theme theme) => theme.PopupPadding;
    protected override float DefaultCornerRadius(Theme theme) => theme.CornerRadiusPopup;

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
        => ctx.StrokeRoundRect(LayoutBounds, CornerRadius, ctx.Theme.BorderSubtle);
}

/// Shared clickable row for Dropdown and ContextMenu.
/// When isChecked is provided, 20 px on the left is reserved for a ✓ indicator.
internal sealed class MenuItemWidget : Widget
{
    private readonly Func<bool>? _isChecked;
    private readonly Action _onActivate;

    private const float CheckW  = 20f;

    public MenuItemWidget(string text, Action onActivate, bool enabled = true, Func<bool>? isChecked = null)
    {
        _isChecked  = isChecked;
        _onActivate = onActivate;

        IsEnabled    = enabled;
        AddChild(new Label
        {
            Text             = text,
            IsHitTestVisible = false,
            ColorSource      = enabled ? t => t.OnSurface : t => t.OnSurfaceMuted,
        });
    }

    // Extra left padding holds the check indicator when one is used.
    protected override Thickness DefaultPadding(Theme theme)
    {
        var p = theme.MenuItemPadding;
        return _isChecked is null ? p : p with { Left = p.Left + CheckW };
    }

    protected override float DefaultCornerRadius(Theme theme) => theme.CornerRadiusControl;

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
            ctx.FillCircle(LayoutBounds.X + t.MenuItemPadding.Left + CheckW * 0.5f, LayoutBounds.CenterY, 4f, t.Primary);
    }
}

/// Divider between groups of menu items, inset from the panel's edges.
internal sealed class MenuSeparator : Separator
{
    protected override Thickness DefaultPadding(Theme theme) => new(theme.SpacingXs, theme.SpacingXs * 0.5f);
}

/// Compact non-interactive section header inside a menu list.
internal sealed class MenuHeaderWidget : Widget
{
    public MenuHeaderWidget(string text)
    {
        IsHitTestVisible = false;
        AddChild(new Label { Text = text, Role = TextRole.Small, Bold = true, IsHitTestVisible = false });
    }

    protected override Thickness DefaultPadding(Theme theme)
    {
        var p = theme.MenuItemPadding;
        return new Thickness(p.Left, theme.SpacingXs, p.Right, theme.SpacingXs * 0.5f);
    }
}
