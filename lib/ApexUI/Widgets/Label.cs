namespace ApexUI.Widgets;

public class Label : Widget
{
    public string Text
    {
        get;
        set => SetLayoutProperty(ref field, value);
    } = "";

    public float FontSize
    {
        get;
        set => SetLayoutProperty(ref field, value);
    } = float.NaN; // NaN = the theme's size for Role

    /// Which of the theme's font sizes the label uses while FontSize is not set.
    public TextRole Role
    {
        get;
        set => SetLayoutProperty(ref field, value);
    }

    public bool Bold
    {
        get;
        set => SetLayoutProperty(ref field, value);
    }

    public bool WordWrap
    {
        get;
        set => SetLayoutProperty(ref field, value);
    }

    public SKColor? Color { get; set => SetRenderProperty(ref field, value); }

    /// When set, overrides Color at draw time using the current theme. It may read other widgets' state
    /// (a button's hover, a tab's selection); whatever changes that state must invalidate.
    public Func<Theme, SKColor>? ColorSource { get; set => SetRenderProperty(ref field, value); }

    public Label WithText(string text)    { Text = text;  return this; }
    public Label WithSize(float size)     { FontSize = size; return this; }
    public Label WithRole(TextRole role)  { Role = role; return this; }
    public Label WithColor(SKColor color) { Color = color; return this; }
    public Label AsBold()                 { Bold = true;  return this; }
    public Label WithWordWrap()           { WordWrap = true; return this; }

    // Text measurement results, reused until text, font or wrap width change.
    private readonly List<string> _lines = [];
    private TextKey _linesKey;
    private float   _linesWidth = float.NaN;
    private TextKey _widthKey;
    private float   _width;

    // One blob per drawn line, rebuilt when text, font or wrap width change; null for an empty line.
    private readonly List<SKTextBlob?> _blobs = [];
    private TextKey _blobsKey;
    private float   _blobsWidth;
    private bool    _hasBlobs;

    private readonly record struct TextKey(string? Text, string? Family, float FontSize, bool Bold);

    protected override Size MeasureCore(Size available)
    {
        // Must match the size DrawCore uses, or the measured box and the drawn text disagree.
        var   theme    = Theme;
        float fontSize = ResolvedFontSize(theme);
        float lineH    = fontSize * theme.LineHeight;

        if (!WordWrap || float.IsInfinity(available.Width))
            return new Size(SingleLineWidth(fontSize) + 2, lineH);

        return new Size(available.Width, Lines(fontSize, available.Width).Count * lineH);
    }

    protected override void DrawCore(DrawContext ctx)
    {
        float fontSize = ResolvedFontSize(ctx.Theme);
        var color = ColorSource?.Invoke(ctx.Theme) ?? Color ?? ctx.Theme.OnSurface;

        var blobs   = Blobs(ctx, fontSize);
        var content = ContentBounds;
        if (!WordWrap)
        {
            ctx.DrawText(blobs[0], content, color, fontSize, Bold);
            return;
        }

        float lineH = fontSize * ctx.Theme.LineHeight;
        for (int i = 0; i < blobs.Count; i++)
        {
            var lineRect = new Rect(content.X, content.Y + i * lineH, content.Width, lineH);
            ctx.DrawText(blobs[i], lineRect, color, fontSize, Bold);
        }
    }

    protected override void OnDetached() => ReleaseBlobs();

    private float ResolvedFontSize(Theme theme) => float.IsNaN(FontSize) ? theme.FontSize(Role) : FontSize;

    private List<SKTextBlob?> Blobs(DrawContext ctx, float fontSize)
    {
        var   key   = new TextKey(Text, ctx.FontFamily, fontSize, Bold);
        float width = WordWrap ? ContentBounds.Width : -1f;
        if (_hasBlobs && key == _blobsKey && width == _blobsWidth) return _blobs;

        ReleaseBlobs();
        var font = ctx.GetFont(fontSize, Bold);
        if (WordWrap)
        {
            foreach (var line in Lines(fontSize, width))
                _blobs.Add(line.Length == 0 ? null : SKTextBlob.Create(line, font));
        }
        else
        {
            _blobs.Add(Text.Length == 0 ? null : SKTextBlob.Create(Text, font));
        }
        _blobsKey   = key;
        _blobsWidth = width;
        _hasBlobs   = true;
        return _blobs;
    }

    private void ReleaseBlobs()
    {
        foreach (var blob in _blobs) blob?.Dispose();
        _blobs.Clear();
        _hasBlobs = false;
    }

    private TextKey CurrentKey(float fontSize)
        => new(Text, Theme.FontFamily, fontSize, Bold);

    private float SingleLineWidth(float fontSize)
    {
        var key = CurrentKey(fontSize);
        if (key == _widthKey) return _width;

        using var font = LayoutFont.Get(Host, fontSize, Bold);
        _width    = font.Font.MeasureText(Text);
        _widthKey = key;
        return _width;
    }

    private List<string> Lines(float fontSize, float maxWidth)
    {
        var key = CurrentKey(fontSize);
        if (key == _linesKey && maxWidth == _linesWidth) return _lines;

        using var font = LayoutFont.Get(Host, fontSize, Bold);
        ComputeLines(Text, font.Font, maxWidth, _lines);
        _linesKey   = key;
        _linesWidth = maxWidth;
        return _lines;
    }

    private static void ComputeLines(string text, SKFont font, float maxWidth, List<string> result)
    {
        result.Clear();
        foreach (var paragraph in text.Split('\n'))
        {
            if (string.IsNullOrEmpty(paragraph)) { result.Add(""); continue; }
            var sb = new System.Text.StringBuilder();
            foreach (var word in paragraph.Split(' '))
            {
                if (sb.Length == 0)
                {
                    sb.Append(word);
                }
                else
                {
                    string candidate = sb + " " + word;
                    if (font.MeasureText(candidate) <= maxWidth)
                        sb.Append(' ').Append(word);
                    else
                    {
                        result.Add(sb.ToString());
                        sb.Clear().Append(word);
                    }
                }
            }
            if (sb.Length > 0) result.Add(sb.ToString());
        }
    }
}
