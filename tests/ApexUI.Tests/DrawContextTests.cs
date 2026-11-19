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

    [Fact]
    public void DrawText_of_unchanged_strings_allocates_nothing_from_frame_to_frame()
    {
        using var bitmap = new SKBitmap(200, 40);
        using var canvas = new SKCanvas(bitmap);
        using var ctx    = new DrawContext(canvas, Theme.Light, 1f);
        var bounds = new Rect(0f, 0f, 200f, 20f);

        long bytes = TestCanvas.MinAllocatedBytes(() =>
        {
            ctx.BeginFrame(canvas, Theme.Light, 1f);
            ctx.DrawText("Width", bounds, SKColors.Black, 12f);
            ctx.DrawText("Centred", bounds, SKColors.Black, 12f, align: SKTextAlign.Center);
            ctx.DrawText("Bold", bounds, SKColors.Black, 12f, bold: true);
        });

        Assert.Equal(0, bytes);
    }

    [Fact]
    public void DrawText_keeps_glyphs_only_for_text_drawn_in_the_last_frame()
    {
        using var bitmap = new SKBitmap(200, 40);
        using var canvas = new SKCanvas(bitmap);
        using var ctx    = new DrawContext(canvas, Theme.Light, 1f);
        var bounds = new Rect(0f, 0f, 200f, 20f);

        ctx.BeginFrame(canvas, Theme.Light, 1f);
        ctx.DrawText("1", bounds, SKColors.Black, 12f);
        ctx.DrawText("Label", bounds, SKColors.Black, 12f);
        ctx.BeginFrame(canvas, Theme.Light, 1f);
        ctx.DrawText("2", bounds, SKColors.Black, 12f);
        ctx.DrawText("Label", bounds, SKColors.Black, 12f);
        ctx.BeginFrame(canvas, Theme.Light, 1f);

        Assert.Equal(2, ctx.CachedTextBlobCount);
    }

    [Fact]
    public void DrawText_right_aligned_ends_at_the_right_edge()
    {
        using var bitmap = new SKBitmap(100, 20);
        using var canvas = new SKCanvas(bitmap);
        using var ctx    = new DrawContext(canvas, Theme.Light, 1f);
        canvas.Clear(SKColors.White);

        ctx.DrawText("MMMM", new Rect(0f, 0f, 100f, 20f), SKColors.Black, 16f, align: SKTextAlign.Right);

        Assert.True(InkColumns(bitmap, 70, 100) > 0);
        Assert.Equal(0, InkColumns(bitmap, 0, 40));
    }

    private static int InkColumns(SKBitmap bitmap, int from, int to)
    {
        int count = 0;
        for (int x = from; x < to; x++)
        {
            for (int y = 0; y < bitmap.Height; y++)
            {
                if (bitmap.GetPixel(x, y).Red >= 128) continue;
                count++;
                break;
            }
        }
        return count;
    }
}
