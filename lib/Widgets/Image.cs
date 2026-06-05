namespace ApexUI.Widgets;

public enum ImageStretch { None, Fill, Uniform, UniformToFill }

public class Image : Widget, IDisposable
{
    private SKImage?        _raster;
    private SKPicture?      _vector;
    private Svg.Skia.SKSvg? _svgLoader;
    private float           _naturalW, _naturalH;
    private bool            _disposed;

    public ImageStretch Stretch
    {
        get;
        set { field = value; Invalidate(); }
    } = ImageStretch.Uniform;

    private Image() { }

    /// Load a raster (PNG/JPG/…) or vector (.svg) file by extension.
    public static Image FromFile(string path)
    {
        var img = new Image();
        if (Path.GetExtension(path).Equals(".svg", StringComparison.OrdinalIgnoreCase))
            img.LoadSvgFile(path);
        else
            img.LoadRasterFile(path);
        return img;
    }

    /// Wrap an existing bitmap (Image takes ownership of the backing SKImage, not the bitmap).
    public static Image FromBitmap(SKBitmap bitmap)
    {
        var img = new Image();
        img._raster   = SKImage.FromBitmap(bitmap);
        img._naturalW = bitmap.Width;
        img._naturalH = bitmap.Height;
        return img;
    }

    /// Parse an inline SVG XML string into a vector image.
    public static Image FromSvgString(string svgXml)
    {
        var img = new Image();
        var svg = new Svg.Skia.SKSvg();
        using var ms = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(svgXml));
        svg.Load(ms);
        img._svgLoader = svg;
        img._vector    = svg.Picture;
        if (img._vector is not null)
        {
            img._naturalW = img._vector.CullRect.Width;
            img._naturalH = img._vector.CullRect.Height;
        }
        return img;
    }

    // ── Fluent ───────────────────────────────────────────────────────────────

    public Image WithStretch(ImageStretch s)    { Stretch = s; return this; }
    public Image WithSize(float w, float h)     { Width = w; Height = h; return this; }

    // ── Layout ───────────────────────────────────────────────────────────────

    protected override Size MeasureCore(Size available)
    {
        if (_naturalW <= 0 || _naturalH <= 0) return Size.Zero;

        // If only one dimension is explicit, preserve aspect ratio for the other.
        if (!float.IsNaN(Width) && float.IsNaN(Height))
            return new Size(Width, Width * (_naturalH / _naturalW));
        if (float.IsNaN(Width) && !float.IsNaN(Height))
            return new Size(Height * (_naturalW / _naturalH), Height);

        return new Size(
            float.IsNaN(Width)  ? _naturalW : Width,
            float.IsNaN(Height) ? _naturalH : Height);
    }

    // ── Drawing ──────────────────────────────────────────────────────────────

    protected override void DrawCore(DrawContext ctx)
    {
        if (LayoutBounds.Width <= 0 || LayoutBounds.Height <= 0) return;
        if (_naturalW <= 0 || _naturalH <= 0) return;

        var dst = StretchRect(_naturalW, _naturalH, LayoutBounds, Stretch);

        if (_raster is not null)
            ctx.DrawImage(_raster, dst);
        else if (_vector is not null)
            ctx.DrawPicture(_vector, dst, _naturalW, _naturalH);
    }

    // ── Stretch helpers ──────────────────────────────────────────────────────

    private static Rect StretchRect(float natW, float natH, Rect bounds, ImageStretch stretch)
        => stretch switch
        {
            ImageStretch.Fill => bounds,
            ImageStretch.None => new Rect(
                bounds.X + (bounds.Width  - natW) * 0.5f,
                bounds.Y + (bounds.Height - natH) * 0.5f,
                natW, natH),
            _ => Uniform(natW, natH, bounds, fill: stretch == ImageStretch.UniformToFill)
        };

    private static Rect Uniform(float natW, float natH, Rect bounds, bool fill)
    {
        float scaleX = bounds.Width  / natW;
        float scaleY = bounds.Height / natH;
        float scale  = fill ? Math.Max(scaleX, scaleY) : Math.Min(scaleX, scaleY);
        float w = natW * scale;
        float h = natH * scale;
        return new Rect(
            bounds.X + (bounds.Width  - w) * 0.5f,
            bounds.Y + (bounds.Height - h) * 0.5f,
            w, h);
    }

    // ── Private loaders ──────────────────────────────────────────────────────

    private void LoadRasterFile(string path)
    {
        using var data = SKData.Create(path);
        _raster   = SKImage.FromEncodedData(data);
        _naturalW = _raster?.Width  ?? 0;
        _naturalH = _raster?.Height ?? 0;
    }

    private void LoadSvgFile(string path)
    {
        var svg = new Svg.Skia.SKSvg();
        svg.Load(path);
        _svgLoader = svg;
        _vector    = svg.Picture;
        if (_vector is not null)
        {
            _naturalW = _vector.CullRect.Width;
            _naturalH = _vector.CullRect.Height;
        }
    }

    // ── Disposal ─────────────────────────────────────────────────────────────

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _raster?.Dispose();
        _vector?.Dispose();
        _svgLoader?.Dispose();
    }
}