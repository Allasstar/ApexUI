namespace ApexUI.Core;

/// Font for measuring text during layout. Uses the running application's shared cache;
/// without an application (headless tests) it creates a temporary font that Dispose frees.
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

    public static LayoutFont Get(float sizePx, bool bold = false)
    {
        var app = Application.Current;
        if (app is not null)
            return new LayoutFont(app.Fonts.Get(app.FontFamily, sizePx, bold), owned: false);

        using var typeface = SKTypeface.FromFamilyName(FontCache.DefaultFamily, bold ? SKFontStyle.Bold : SKFontStyle.Normal);
        return new LayoutFont(new SKFont(typeface, sizePx), owned: true);
    }

    public void Dispose()
    {
        if (_owned) Font.Dispose();
    }
}
