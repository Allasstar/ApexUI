namespace ApexUI.Tests;

public class DrawContextTests
{
    [Fact]
    public void StrokeDashedPath_leaves_gaps_between_dashes()
    {
        using var bitmap = new SKBitmap(40, 10);
        using var canvas = new SKCanvas(bitmap);
        using var ctx    = new DrawContext(canvas, Theme.Light, 1f);
        using var line   = new SKPath();
        line.MoveTo(0f, 5f);
        line.LineTo(40f, 5f);
        canvas.Clear(SKColors.White);

        ctx.StrokeDashedPath(line, SKColors.Black, 2f, 10f, 10f);

        Assert.Equal(SKColors.Black, bitmap.GetPixel(5, 5));
        Assert.Equal(SKColors.White, bitmap.GetPixel(15, 5));
        Assert.Equal(SKColors.Black, bitmap.GetPixel(25, 5));
    }

    [Fact]
    public void StrokeDashedPath_follows_a_new_pattern_and_leaves_plain_strokes_solid()
    {
        using var bitmap = new SKBitmap(40, 10);
        using var canvas = new SKCanvas(bitmap);
        using var ctx    = new DrawContext(canvas, Theme.Light, 1f);
        using var line   = new SKPath();
        line.MoveTo(0f, 5f);
        line.LineTo(40f, 5f);
        canvas.Clear(SKColors.White);

        ctx.StrokeDashedPath(line, SKColors.Black, 2f, 10f, 10f);
        canvas.Clear(SKColors.White);
        ctx.StrokeDashedPath(line, SKColors.Black, 2f, 4f, 16f);

        Assert.Equal(SKColors.White, bitmap.GetPixel(5, 5));

        canvas.Clear(SKColors.White);
        ctx.StrokePath(line, SKColors.Black, 2f);

        Assert.Equal(SKColors.Black, bitmap.GetPixel(15, 5));
    }
}
