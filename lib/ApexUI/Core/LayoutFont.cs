namespace ApexUI.Core;

/// Font for measuring text during layout. Uses the host's shared cache;
/// without a host (a widget not on screen, headless tests) it creates a temporary font that Dispose frees.
/// Use with `using` and do not keep Font beyond the using scope.
internal readonly ref struct LayoutFont
{
    private readonly bool _owned;

    public SKFont Font { get; }

    private LayoutFont(SKFont font, bool owned)
    {
        Font   = font;
        _owned = owned;
    }

    public static LayoutFont Get(UiHost? host, float sizePx, bool bold = false)
    {
        if (host is not null)
            return new LayoutFont(host.Fonts.Get(host.FontFamily, sizePx, bold), owned: false);

        using var typeface = SKTypeface.FromFamilyName(FontCache.DefaultFamily, bold ? SKFontStyle.Bold : SKFontStyle.Normal);
        return new LayoutFont(new SKFont(typeface, sizePx), owned: true);
    }

    public void Dispose()
    {
        if (_owned) Font.Dispose();
    }
}
