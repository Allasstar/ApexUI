namespace ApexUI.Tests;

public class ImageTests
{
    [Fact]
    public void SetBitmap_replaces_the_image_and_its_natural_size()
    {
        using var small = new SKBitmap(4, 4);
        using var wide = new SKBitmap(8, 2);
        var image = Image.FromBitmap(small);
        image.Measure(new Size(100, 100));
        Assert.Equal(new Size(4, 4), image.DesiredSize);

        image.SetBitmap(wide);
        image.Measure(new Size(100, 100));

        Assert.Equal(new Size(8, 2), image.DesiredSize);
    }

    [Fact]
    public void Without_smoothing_a_scaled_image_keeps_sharp_pixels()
    {
        using var source = new SKBitmap(2, 1);
        source.SetPixel(0, 0, SKColors.Black);
        source.SetPixel(1, 0, SKColors.White);

        using SKBitmap sharp = TestCanvas.Render(Image.FromBitmap(source).WithStretch(ImageStretch.Fill).WithSmoothing(false), 20, 10);
        using SKBitmap smooth = TestCanvas.Render(Image.FromBitmap(source).WithStretch(ImageStretch.Fill), 20, 10);

        Assert.Equal(SKColors.Black, sharp.GetPixel(9, 5));
        Assert.Equal(SKColors.White, sharp.GetPixel(10, 5));
        SKColor blended = smooth.GetPixel(9, 5);
        Assert.True(blended.Red is > 0 and < 255, $"expected a blend, got {blended}");
    }
}
