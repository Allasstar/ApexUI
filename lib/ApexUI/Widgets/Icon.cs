namespace ApexUI.Widgets;

public enum IconStyle { Fill, Stroke }

/// A vector icon drawn from SVG path data (the `d` attribute) laid out in a ViewBox × ViewBox square,
/// scaled to Size. Fill icons use the nonzero rule; stroke icons draw round-capped lines StrokeWidth
/// view-box units wide. The parsed path is released when the icon leaves the screen.
public sealed class Icon : Widget
{
    private SKPath? _path;
    private bool    _parsed;

    public Icon(string pathData, IconStyle style = IconStyle.Fill)
    {
        PathData         = pathData;
        Style            = style;
        IsHitTestVisible = false;
    }

    public string PathData
    {
        get;
        set
        {
            if (field == value) return;
            field = value;
            ReleasePath();
            Invalidate();
        }
    }

    public IconStyle Style
    {
        get;
        set => SetRenderProperty(ref field, value);
    }

    /// Side of the square the path data is drawn in (24 for most icon sets).
    public float ViewBox
    {
        get;
        set => SetRenderProperty(ref field, value);
    } = 24f;

    /// Line width of a stroke icon, in view-box units.
    public float StrokeWidth
    {
        get;
        set => SetRenderProperty(ref field, value);
    } = 2f;

    /// Width and height of the icon. Until set, the theme's IconSize.
    public float Size
    {
        get => float.IsNaN(_size) ? Theme.IconSize : _size;
        set => SetLayoutProperty(ref _size, value);
    }
    private float _size = float.NaN;

    public SKColor? Color { get; set => SetRenderProperty(ref field, value); }

    /// When set, overrides Color at draw time using the current theme. Default: OnSurface.
    public Func<Theme, SKColor>? ColorSource { get; set => SetRenderProperty(ref field, value); }

    public Icon WithSize(float size)      { Size = size; return this; }
    public Icon WithColor(SKColor color)  { Color = color; return this; }
    public Icon WithStrokeWidth(float w)  { StrokeWidth = w; return this; }

    protected override Size MeasureCore(Size available) => new(Size, Size);

    protected override void DrawCore(DrawContext ctx)
    {
        var path = Path();
        if (path is null || ViewBox <= 0f) return;

        var   t     = ctx.Theme;
        var   color = ColorSource?.Invoke(t) ?? Color ?? (IsEnabledInTree ? t.OnSurface : t.OnSurfaceMuted);
        float size  = Size;
        var   area  = ContentBounds;

        var canvas = ctx.Canvas;
        canvas.Save();
        canvas.Translate(area.CenterX - size * 0.5f, area.CenterY - size * 0.5f);
        canvas.Scale(size / ViewBox);
        if (Style == IconStyle.Stroke)
            ctx.StrokePath(path, color, StrokeWidth, SKStrokeCap.Round, SKStrokeJoin.Round);
        else
            ctx.FillPath(path, color);
        canvas.Restore();
    }

    protected override void OnDetached() => ReleasePath();

    internal bool HasParsedPath => _path is not null;

    // Parsed once on first draw; invalid data draws nothing instead of throwing.
    private SKPath? Path()
    {
        if (_parsed) return _path;
        _parsed = true;
        _path   = SKPath.ParseSvgPathData(PathData);
        return _path;
    }

    private void ReleasePath()
    {
        _path?.Dispose();
        _path   = null;
        _parsed = false;
    }
}
