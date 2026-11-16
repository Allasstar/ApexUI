namespace ApexUI.Core;

/// Keeps typefaces and fonts alive for reuse, so text can be measured and drawn
/// without creating native Skia objects on every call.
/// Fonts returned by Get are owned by the cache: do not dispose them.
public sealed class FontCache : IDisposable
{
    private readonly Dictionary<(string Family, bool Bold), SKTypeface> _typefaces = new();
    private readonly Dictionary<(string Family, bool Bold, float Size), SKFont> _fonts = new();

    /// Returns the cached font for family, size (in pixels) and weight, creating it on first use.
    public SKFont Get(string family, float sizePx, bool bold = false)
    {
        if (_fonts.TryGetValue((family, bold, sizePx), out var font))
            return font;

        if (!_typefaces.TryGetValue((family, bold), out var typeface))
        {
            typeface = SKTypeface.FromFamilyName(family, bold ? SKFontStyle.Bold : SKFontStyle.Normal);
            _typefaces[(family, bold)] = typeface;
        }

        font = new SKFont(typeface, sizePx);
        _fonts[(family, bold, sizePx)] = font;
        return font;
    }

    public void Dispose()
    {
        foreach (var font in _fonts.Values) font.Dispose();
        foreach (var typeface in _typefaces.Values) typeface.Dispose();
        _fonts.Clear();
        _typefaces.Clear();
    }
}
