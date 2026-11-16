// Passed into every DrawCore() call.
// Carries the canvas + theme so widgets don't need global state.
// Owns reusable paints, a path and (unless one is supplied) a font cache,
// so the drawing helpers create no Skia objects per call.

namespace ApexUI.Core;

public sealed partial class DrawContext : IDisposable
{
    private readonly FontCache _fonts;
    private readonly bool      _ownsFonts;

    private readonly SKPaint _fill   = new() { IsAntialias = true };
    private readonly SKPaint _stroke = new() { IsAntialias = true, IsStroke = true };
    private readonly SKPaint _text   = new() { IsAntialias = true };
    private readonly SKPaint _shadow = new() { IsAntialias = true };
    private readonly SKPaint _layer  = new();
    private readonly SKPath  _path   = new();
    private readonly SKRoundRect _roundRect = new();
    private readonly Dictionary<float, SKMaskFilter> _blurs = new();

    /// pixelScale: canvas pixels per logical unit, already applied to the canvas transform.
    /// fonts: shared cache to use; when null the context creates and disposes its own.
    public DrawContext(SKCanvas canvas, Theme theme, float pixelScale, FontCache? fonts = null)
    {
        Canvas     = canvas;
        Theme      = theme;
        PixelScale = pixelScale;
        _fonts     = fonts ?? new FontCache();
        _ownsFonts = fonts is null;
    }

    public SKCanvas Canvas     { get; private set; }
    public Theme    Theme      { get; private set; }
    /// Canvas pixels per logical unit (display scaling × UI scale). Drawing is already scaled;
    /// use it only to snap to device pixels, e.g. for crisp hairlines.
    public float    PixelScale { get; private set; }
    /// The theme's font family; GetFont and the text helpers use it.
    public string   FontFamily => Theme.FontFamily;

    // Lets the application reuse one context (and its paints) for every frame.
    internal void BeginFrame(SKCanvas canvas, Theme theme, float pixelScale)
    {
        Canvas     = canvas;
        Theme      = theme;
        PixelScale = pixelScale;
    }

    /// Cached font in the current family, sized in logical units. Owned by the context: do not dispose.
    public SKFont GetFont(float sizePx, bool bold = false)
        => _fonts.Get(FontFamily, sizePx, bold);

    /// New paint for custom drawing. The caller owns and disposes it.
    public SKPaint MakePaint(SKColor color, bool antialias = true) => new()
    {
        Color       = color,
        IsAntialias = antialias,
    };

    /// New font for custom drawing. The caller owns and disposes it; prefer GetFont in draw code.
    public SKFont MakeTextFont(float sizePx, bool bold = false)
    {
        // The font keeps its own reference to the typeface, so the wrapper can go now.
        using var typeface = SKTypeface.FromFamilyName(FontFamily, bold ? SKFontStyle.Bold : SKFontStyle.Normal);
        return new SKFont(typeface, sizePx);
    }

    /// New text paint for custom drawing. The caller owns and disposes it.
    public SKPaint MakeTextPaint(SKColor color) => new()
    {
        Color       = color,
        IsAntialias = true,
    };

    // ── Reusable resources for the built-in helpers ──────────────────────────
    // Each call reconfigures the shared object, so a returned paint is only valid until the next call.

    internal SKPaint FillPaint(SKColor color)
    {
        _fill.Color = color;
        return _fill;
    }

    internal SKPaint StrokePaint(SKColor color, float width,
        SKStrokeCap cap = SKStrokeCap.Butt, SKStrokeJoin join = SKStrokeJoin.Miter)
    {
        _stroke.Color       = color;
        _stroke.StrokeWidth = width;
        _stroke.StrokeCap   = cap;
        _stroke.StrokeJoin  = join;
        return _stroke;
    }

    internal SKPaint TextPaint(SKColor color)
    {
        _text.Color = color;
        return _text;
    }

    internal SKPaint ShadowPaint(SKColor color, float blur)
    {
        if (!_blurs.TryGetValue(blur, out var filter))
        {
            filter = SKMaskFilter.CreateBlur(SKBlurStyle.Normal, blur);
            _blurs[blur] = filter;
        }
        _shadow.Color      = color;
        _shadow.MaskFilter = filter;
        return _shadow;
    }

    // SaveLayer copies the paint, so one instance can serve nested layers.
    internal SKPaint LayerPaint(float opacity)
    {
        _layer.Color = SKColors.Black.WithAlpha((byte)(opacity * 255));
        return _layer;
    }

    internal SKPath EmptyPath()
    {
        _path.Reset();
        return _path;
    }

    public void Dispose()
    {
        _fill.Dispose();
        _stroke.Dispose();
        _text.Dispose();
        _shadow.Dispose();
        _layer.Dispose();
        _path.Dispose();
        _roundRect.Dispose();
        foreach (var filter in _blurs.Values) filter.Dispose();
        _blurs.Clear();
        if (_ownsFonts) _fonts.Dispose();
    }
}
