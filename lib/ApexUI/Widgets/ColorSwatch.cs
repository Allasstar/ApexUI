namespace ApexUI.Widgets;

/// A small square showing a color (over a checkerboard when it is transparent). Clicking it, or Space/Enter
/// while focused, opens a ColorPicker below it; picking changes Color live. With IsReadOnly it only shows
/// the color. ColorChanged fires on every change, EditCompleted once per finished edit in the picker. Picker gives the
/// popup's picker (for example to add swatch rows); Opened and Closed bracket each opening.
public class ColorSwatch : Widget
{
    private readonly Overlay _popup;
    private ColorPicker? _picker;
    private SKColor _color;

    public ColorSwatch(SKColor color)
    {
        _color = color;
        Focusable = true;
        Cursor = CursorShape.Hand;
        _popup = new Overlay { DismissOnClickOutside = true };
        _popup.Closed += () => Closed?.Invoke();
    }

    public SKColor Color
    {
        get => _color;
        set
        {
            if (_color == value) return;
            _color = value;
            if (_picker is not null) _picker.Color = value;
            Invalidate();
            ColorChanged?.Invoke(value);
        }
    }

    public bool ShowAlpha { get; set; } = true;

    public bool IsReadOnly
    {
        get;
        set
        {
            field = value;
            Focusable = !value;
            Cursor = value ? null : CursorShape.Hand;
        }
    }

    public bool IsOpen => _popup.IsVisible;

    public event Action<SKColor>? ColorChanged;

    public event Action<SKColor>? EditCompleted;

    public event Action? Opened;

    public event Action? Closed;

    /// The picker the popup shows, created on first use.
    public ColorPicker Picker
    {
        get
        {
            if (_picker is not null) return _picker;
            _picker = new ColorPicker(_color);
            _picker.ColorChanged += c => Color = c;
            _picker.EditCompleted += c => EditCompleted?.Invoke(c);
            _popup.Content = new PopupPanel(_picker, t => new Thickness(t.SpacingSm), t => t.CornerRadiusPopup);
            return _picker;
        }
    }

    public ColorSwatch OnChange(Action<SKColor> action) { ColorChanged += action; return this; }
    public ColorSwatch OnEditCompleted(Action<SKColor> action) { EditCompleted += action; return this; }
    public ColorSwatch WithAlpha(bool show) { ShowAlpha = show; return this; }
    public ColorSwatch AsReadOnly() { IsReadOnly = true; return this; }

    public void Open()
    {
        if (IsReadOnly || Host is null || IsOpen) return;
        Picker.Color = _color;
        Picker.ShowAlpha = ShowAlpha;
        _popup.Open(this);
        Opened?.Invoke();
    }

    public void Close() => _popup.Close();

    protected override void OnClick(PointerEventArgs e)
    {
        if (IsReadOnly) return;
        e.Handled = true;
        if (IsOpen) Close();
        else Open();
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        if (IsReadOnly || !Activation.IsActivationKey(e)) return;
        e.Handled = true;
        Open();
    }

    protected override void OnDetached() => _popup.Detach();

    protected override Size MeasureCore(Size available) => new(Theme.SwatchSize, Theme.SwatchSize);

    protected override float DefaultCornerRadius(Theme theme) => theme.CornerRadiusSmall;

    protected override void DrawCore(DrawContext ctx)
    {
        var t = ctx.Theme;
        var b = LayoutBounds;
        using (ctx.PushClip(b, CornerRadius))
        {
            if (_color.Alpha < 255) ctx.FillCheckerboard(b, b.Height * 0.5f, t.Surface, t.SurfacePressed);
            ctx.FillRect(b, _color);
        }

        ctx.StrokeRoundRect(b, CornerRadius, IsHovered && !IsReadOnly ? t.OnSurfaceMuted : t.Border);
    }
}
