using System.Buffers.Binary;
using ApexUI.Desktop;

namespace ApexUI.Tests;

public class WindowsClipboardTests
{
    // 2 x 2, bottom-up rows: the first stored row is the bottom one.
    private static readonly SKColor TopLeft = new(255, 0, 0), TopRight = new(0, 255, 0);
    private static readonly SKColor BottomLeft = new(0, 0, 255), BottomRight = new(255, 255, 255);

    [Fact]
    public void A_32_bit_screenshot_with_zero_alpha_bytes_becomes_an_opaque_png()
    {
        byte[] dib = Dib(32, compression: 0, masks: [], rows: [Row32(BottomLeft, BottomRight), Row32(TopLeft, TopRight)]);

        AssertPixels(WindowsClipboard.DibToPng(dib));
    }

    [Fact]
    public void A_24_bit_bitmap_with_padded_rows_keeps_its_colors()
    {
        byte[] dib = Dib(24, compression: 0, masks: [], rows: [Row24(BottomLeft, BottomRight), Row24(TopLeft, TopRight)]);

        AssertPixels(WindowsClipboard.DibToPng(dib));
    }

    [Fact]
    public void Bit_field_masks_after_the_header_are_skipped_to_reach_the_pixels()
    {
        uint[] masks = [0x00ff0000, 0x0000ff00, 0x000000ff];
        byte[] dib = Dib(32, compression: 3, masks, rows: [Row32(BottomLeft, BottomRight), Row32(TopLeft, TopRight)]);

        AssertPixels(WindowsClipboard.DibToPng(dib));
    }

    [Fact]
    public void A_truncated_header_is_not_an_image()
    {
        Assert.Null(WindowsClipboard.DibToPng(new byte[20]));
    }

    private static void AssertPixels(byte[]? png)
    {
        Assert.NotNull(png);
        Assert.Equal(0x89, png[0]);
        using var bitmap = SKBitmap.Decode(png);
        Assert.Equal(2, bitmap.Width);
        Assert.Equal(2, bitmap.Height);
        Assert.Equal(TopLeft, bitmap.GetPixel(0, 0));
        Assert.Equal(TopRight, bitmap.GetPixel(1, 0));
        Assert.Equal(BottomLeft, bitmap.GetPixel(0, 1));
        Assert.Equal(BottomRight, bitmap.GetPixel(1, 1));
    }

    private static byte[] Dib(ushort bitCount, uint compression, uint[] masks, byte[][] rows)
    {
        var bytes = new List<byte>();
        var header = new byte[40];
        BinaryPrimitives.WriteUInt32LittleEndian(header, 40);
        BinaryPrimitives.WriteInt32LittleEndian(header.AsSpan(4), 2);
        BinaryPrimitives.WriteInt32LittleEndian(header.AsSpan(8), 2);
        BinaryPrimitives.WriteUInt16LittleEndian(header.AsSpan(12), 1);
        BinaryPrimitives.WriteUInt16LittleEndian(header.AsSpan(14), bitCount);
        BinaryPrimitives.WriteUInt32LittleEndian(header.AsSpan(16), compression);
        bytes.AddRange(header);
        foreach (uint mask in masks)
        {
            var word = new byte[4];
            BinaryPrimitives.WriteUInt32LittleEndian(word, mask);
            bytes.AddRange(word);
        }

        foreach (byte[] row in rows) bytes.AddRange(row);
        return [.. bytes];
    }

    private static byte[] Row32(SKColor a, SKColor b) => [a.Blue, a.Green, a.Red, 0, b.Blue, b.Green, b.Red, 0];

    // 6 bytes of pixels padded to 8.
    private static byte[] Row24(SKColor a, SKColor b) => [a.Blue, a.Green, a.Red, b.Blue, b.Green, b.Red, 0, 0];
}
