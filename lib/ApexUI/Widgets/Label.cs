namespace ApexUI.Widgets;

public class Label : Widget
{
    public string Text
    {
        get;
        set { field = value; InvalidateLayout(); }
    } = "";

    public float FontSize
    {
        get;
        set { field = value; InvalidateLayout(); }
    } = float.NaN; // NaN = use Theme default

    public bool Bold
    {
        get;
        set { field = value; InvalidateLayout(); }
    }

    public bool WordWrap
    {
        get;
        set { field = value; InvalidateLayout(); }
    }

    public SKColor? Color { get; set { field = value; Invalidate(); } }

    public Label WithText(string text)    { Text = text;  return this; }
    public Label WithSize(float size)     { FontSize = size; return this; }
    public Label WithColor(SKColor color) { Color = color; return this; }
    public Label AsBold()                 { Bold = true;  return this; }
    public Label WithWordWrap()           { WordWrap = true; return this; }

    // Text measurement results, reused until text, font or wrap width change.
    private readonly List<string> _lines = [];
    private TextKey _linesKey;
    private float   _linesWidth = float.NaN;
    private TextKey _widthKey;
    private float   _width;

    private readonly record struct TextKey(string? Text, string? Family, float FontSize, bool Bold);

    protected override Size MeasureCore(Size available)
    {
        // Must match the size DrawCore uses, or the measured box and the drawn text disagree.
        float fontSize = float.IsNaN(FontSize) ? (Application.Current?.Theme ?? Theme.Light).FontSizeBase : FontSize;
        float lineH    = fontSize * 1.4f;

        if (!WordWrap || float.IsInfinity(available.Width))
            return new Size(SingleLineWidth(fontSize) + 2, lineH);

        return new Size(available.Width, Lines(fontSize, available.Width).Count * lineH);
    }

    protected override void DrawCore(DrawContext ctx)
    {
        float fontSize = float.IsNaN(FontSize) ? ctx.Theme.FontSizeBase : FontSize;
        var color = Color ?? ctx.Theme.OnSurface;

        if (!WordWrap)
        {
            ctx.DrawText(Text, LayoutBounds, color, fontSize, Bold);
            return;
        }

        var   lines = Lines(fontSize, LayoutBounds.Width);
        float lineH = fontSize * 1.4f;
        for (int i = 0; i < lines.Count; i++)
        {
            var lineRect = new Rect(LayoutBounds.X, LayoutBounds.Y + i * lineH, LayoutBounds.Width, lineH);
            ctx.DrawText(lines[i], lineRect, color, fontSize, Bold);
        }
    }

    private TextKey CurrentKey(float fontSize)
        => new(Text, Application.Current?.FontFamily ?? FontCache.DefaultFamily, fontSize, Bold);

    private float SingleLineWidth(float fontSize)
    {
        var key = CurrentKey(fontSize);
        if (key == _widthKey) return _width;

        using var font = LayoutFont.Get(fontSize, Bold);
        _width    = font.Font.MeasureText(Text);
        _widthKey = key;
        return _width;
    }

    private List<string> Lines(float fontSize, float maxWidth)
    {
        var key = CurrentKey(fontSize);
        if (key == _linesKey && maxWidth == _linesWidth) return _lines;

        using var font = LayoutFont.Get(fontSize, Bold);
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
