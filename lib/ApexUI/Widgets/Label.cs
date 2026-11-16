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

    protected override Size MeasureCore(Size available)
    {
        float fontSize = float.IsNaN(FontSize) ? 14f : FontSize;
        string family  = Application.Current?.FontFamily ?? "Segoe UI";
        using var typeface = SKTypeface.FromFamilyName(family, Bold ? SKFontStyle.Bold : SKFontStyle.Normal);
        using var font     = new SKFont(typeface, fontSize);

        if (!WordWrap || float.IsInfinity(available.Width))
            return new Size(font.MeasureText(Text) + 2, fontSize * 1.4f);

        var lines = ComputeLines(Text, font, available.Width);
        return new Size(available.Width, lines.Count * fontSize * 1.4f);
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

        string family = Application.Current?.FontFamily ?? "Segoe UI";
        using var typeface = SKTypeface.FromFamilyName(family, Bold ? SKFontStyle.Bold : SKFontStyle.Normal);
        using var font     = new SKFont(typeface, fontSize);

        var   lines = ComputeLines(Text, font, LayoutBounds.Width);
        float lineH = fontSize * 1.4f;
        for (int i = 0; i < lines.Count; i++)
        {
            var lineRect = new Rect(LayoutBounds.X, LayoutBounds.Y + i * lineH, LayoutBounds.Width, lineH);
            ctx.DrawText(lines[i], lineRect, color, fontSize, Bold);
        }
    }

    private static List<string> ComputeLines(string text, SKFont font, float maxWidth)
    {
        var result = new List<string>();
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
        return result;
    }
}
