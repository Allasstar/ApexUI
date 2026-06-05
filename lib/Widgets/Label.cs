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

    public SKColor? Color { get; set { field = value; Invalidate(); } }

    // Fluent builder methods for quick setup
    public Label WithText(string text)       { Text = text;  return this; }
    public Label WithSize(float size)        { FontSize = size; return this; }
    public Label WithColor(SKColor color)    { Color = color; return this; }
    public Label AsBold()                    { Bold = true;  return this; }

    protected override Size MeasureCore(Size available)
    {
        float fontSize = float.IsNaN(FontSize) ? 14f : FontSize;
        string family  = Application.Current?.FontFamily ?? "Segoe UI";
        using var font = new SKFont(
            SKTypeface.FromFamilyName(family, Bold ? SKFontStyle.Bold : SKFontStyle.Normal),
            fontSize);
        return new Size(font.MeasureText(Text) + 2, fontSize * 1.4f);
    }

    protected override void DrawCore(DrawContext ctx)
    {
        if (string.IsNullOrEmpty(Text)) return;

        float fontSize = float.IsNaN(FontSize) ? ctx.Theme.FontSizeBase : FontSize;
        var color = Color ?? ctx.Theme.OnSurface;

        using var font  = ctx.MakeTextFont(fontSize, Bold);
        using var paint = ctx.MakeTextPaint(color);

        var metrics = font.Metrics;
        float textHeight = metrics.Descent - metrics.Ascent;
        float y = LayoutBounds.Y + (LayoutBounds.Height - textHeight) * 0.5f - metrics.Ascent;

        ctx.Canvas.DrawText(Text, LayoutBounds.X, y, SKTextAlign.Left, font, paint);
    }
}