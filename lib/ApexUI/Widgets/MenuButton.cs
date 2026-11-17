namespace ApexUI.Widgets;

/// A ghost button that opens a menu of actions below it: click, Space, Enter or Down opens it,
/// a second click closes it. The face is any widget (an Icon, a Label); a small chevron on the
/// right marks it as a menu. Items work as in ContextMenu.
///
/// Example:
///   var shapes = new MenuButton(new Icon(ShapeIcon))
///       .AddItem("Rectangle", () => Pick(Shape.Rectangle))
///       .AddItem("Ellipse", () => Pick(Shape.Ellipse));
public class MenuButton : Widget
{
    private const float MinMenuWidth = 160f;
    private const float ChevronSize = 3f;
    private const float ChevronLineWidth = 1.5f;

    private readonly MenuItems _items = new();
    private readonly Overlay _overlay = new() { DismissOnClickOutside = true };
    private int _builtVersion = -1;

    public MenuButton(Widget face)
    {
        Face = face;
        face.IsHitTestVisible = false;
        face.HAlign = HAlign.Center;
        face.VAlign = VAlign.Center;
        if (face is Icon icon) icon.ColorSource ??= t => IsEnabled ? t.OnSurface : t.OnSurfaceMuted;
        AddChild(face);
        Focusable = true;
        _overlay.Closed += Invalidate;
    }

    public MenuButton(string text) : this(new Label { Text = text }) { }

    public Widget Face { get; }

    public bool IsOpen => _overlay.IsVisible;

    public MenuButton AddItem(string label, Action action, bool enabled = true)
    {
        _items.AddItem(label, action, enabled);
        return this;
    }

    public MenuButton AddCheckItem(string label, Bindable<bool> binding, bool closeOnClick = false)
    {
        _items.AddCheckItem(label, binding, closeOnClick);
        return this;
    }

    public MenuButton AddSeparator()
    {
        _items.AddSeparator();
        return this;
    }

    public MenuButton AddHeader(string text)
    {
        _items.AddHeader(text);
        return this;
    }

    public void Open()
    {
        if (IsOpen || Host is null) return;
        if (_builtVersion != _items.Version)
        {
            _overlay.Content = _items.Build(Close, MinMenuWidth);
            _builtVersion = _items.Version;
        }

        _overlay.Open(this);
        Invalidate();
    }

    public void Close()
    {
        if (IsOpen) _overlay.Close();
    }

    protected override Thickness DefaultPadding(Theme theme)
    {
        Thickness p = theme.IconButtonPadding;
        return p with { Right = p.Right + theme.SpacingXs + ChevronSize * 2f };
    }

    protected override float DefaultCornerRadius(Theme theme) => theme.CornerRadiusControl;

    // While open, a press anywhere lands on the overlay's backdrop and closes the menu.
    protected override void OnPointerPressed(PointerEventArgs e)
    {
        if (e.Button != PointerButton.Left) return;
        e.Handled = true;
        if (IsOpen) Close();
        else Open();
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        if (!Activation.IsActivationKey(e) && !(e.Key == Key.Down && e.Modifiers == KeyModifiers.None)) return;
        e.Handled = true;
        Open();
    }

    protected override void OnDetached() => _overlay.Detach();

    protected override void DrawCore(DrawContext ctx)
    {
        Theme t = ctx.Theme;
        if (IsEnabled)
        {
            if (IsPressed || IsOpen) ctx.FillRoundRect(LayoutBounds, CornerRadius, t.SurfacePressed);
            else if (IsHovered) ctx.FillRoundRect(LayoutBounds, CornerRadius, t.SurfaceHover);
        }

        Rect b = LayoutBounds;
        float cx = b.Right - t.IconButtonPadding.Right - ChevronSize;
        float cy = b.CenterY + ChevronSize * 0.25f;
        SKColor color = IsEnabled ? t.OnSurfaceMuted : t.OnSurfaceMuted.WithAlpha(0.5f);
        ctx.DrawLine(cx - ChevronSize, cy - ChevronSize * 0.5f, cx, cy + ChevronSize * 0.5f, color, ChevronLineWidth, SKStrokeCap.Round);
        ctx.DrawLine(cx, cy + ChevronSize * 0.5f, cx + ChevronSize, cy - ChevronSize * 0.5f, color, ChevronLineWidth, SKStrokeCap.Round);
    }
}
