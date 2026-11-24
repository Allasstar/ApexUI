using System.Buffers.Binary;
using System.Runtime.InteropServices;

namespace ApexUI.Desktop;

// Clipboard reads on Windows go through Win32 rather than GLFW. GLFW reads only text, and when the clipboard holds
// none it reports an error before closing the clipboard; Silk.NET throws from that error callback, the throw skips
// GLFW's CloseClipboard, and the clipboard stays locked for every application until the process exits.
// Pictures: browsers and most editors put a "PNG" format next to the bitmap, which keeps transparency; a screenshot
// (Print Screen, Win+Shift+S) is only a device-independent bitmap, which becomes a PNG here so apps get one encoded
// format either way.
internal static partial class WindowsClipboard
{
    private const uint UnicodeTextFormat = 13; // CF_UNICODETEXT
    private const uint DibFormat = 8;          // CF_DIB; Windows makes it from CF_DIBV5 and CF_BITMAP when only those are set
    private const int FileHeaderSize = 14;
    private const uint BitFields = 3;
    private const uint AlphaBitFields = 6;
    // Another application may hold the clipboard for a moment; GLFW tries three times too.
    private const int OpenAttempts = 3;

    public static string? ReadText(nint owner)
    {
        if (!IsClipboardFormatAvailable(UnicodeTextFormat) || !Open(owner)) return null;
        try
        {
            nint handle = GetClipboardData(UnicodeTextFormat);
            if (handle == 0) return null;
            nint pointer = GlobalLock(handle);
            if (pointer == 0) return null;
            try
            {
                return Marshal.PtrToStringUni(pointer);
            }
            finally
            {
                GlobalUnlock(handle);
            }
        }
        finally
        {
            CloseClipboard();
        }
    }

    public static byte[]? ReadImage(nint owner)
    {
        uint png = RegisterClipboardFormat("PNG");
        bool hasPng = png != 0 && IsClipboardFormatAvailable(png);
        if (!hasPng && !IsClipboardFormatAvailable(DibFormat) || !Open(owner)) return null;
        try
        {
            if (hasPng && Copy(png) is { } encoded) return encoded;
            return Copy(DibFormat) is { } dib ? DibToPng(dib) : null;
        }
        finally
        {
            CloseClipboard();
        }
    }

    private static bool Open(nint owner)
    {
        for (int attempt = 0; attempt < OpenAttempts; attempt++)
        {
            if (OpenClipboard(owner)) return true;
            Thread.Sleep(1);
        }

        return false;
    }

    // A DIB is a BMP file without its 14-byte file header; with the header back, Skia decodes every bit depth.
    // Skia treats a 32-bit BMP without an alpha mask as opaque, which is what screenshots need: their unused
    // fourth byte is usually 0.
    public static byte[]? DibToPng(ReadOnlySpan<byte> dib)
    {
        if (dib.Length < 40) return null;
        uint headerSize = BinaryPrimitives.ReadUInt32LittleEndian(dib);
        ushort bitCount = BinaryPrimitives.ReadUInt16LittleEndian(dib[14..]);
        uint compression = BinaryPrimitives.ReadUInt32LittleEndian(dib[16..]);
        uint colorsUsed = BinaryPrimitives.ReadUInt32LittleEndian(dib[32..]);
        if (headerSize < 40 || headerSize > dib.Length) return null;

        // With the plain 40-byte header the channel masks follow it; larger headers hold them inside.
        long masks = headerSize == 40 ? compression switch { BitFields => 12, AlphaBitFields => 16, _ => 0 } : 0;
        long colorTable = (colorsUsed != 0 ? colorsUsed : bitCount <= 8 ? 1u << bitCount : 0) * 4L;
        long pixelOffset = FileHeaderSize + headerSize + masks + colorTable;
        if (pixelOffset > FileHeaderSize + dib.Length) return null;

        var file = new byte[FileHeaderSize + dib.Length];
        file[0] = (byte)'B';
        file[1] = (byte)'M';
        BinaryPrimitives.WriteUInt32LittleEndian(file.AsSpan(2), (uint)file.Length);
        BinaryPrimitives.WriteUInt32LittleEndian(file.AsSpan(10), (uint)pixelOffset);
        dib.CopyTo(file.AsSpan(FileHeaderSize));

        using var bitmap = SKBitmap.Decode(file);
        if (bitmap is null) return null;
        using SKData data = bitmap.Encode(SKEncodedImageFormat.Png, 100);
        return data.ToArray();
    }

    private static byte[]? Copy(uint format)
    {
        nint handle = GetClipboardData(format);
        if (handle == 0) return null;
        nint pointer = GlobalLock(handle);
        if (pointer == 0) return null;
        try
        {
            var bytes = new byte[(int)GlobalSize(handle)];
            Marshal.Copy(pointer, bytes, 0, bytes.Length);
            return bytes;
        }
        finally
        {
            GlobalUnlock(handle);
        }
    }

    [LibraryImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool OpenClipboard(nint owner);

    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool CloseClipboard();

    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool IsClipboardFormatAvailable(uint format);

    [LibraryImport("user32.dll")]
    private static partial nint GetClipboardData(uint format);

    [LibraryImport("user32.dll", EntryPoint = "RegisterClipboardFormatW", StringMarshalling = StringMarshalling.Utf16)]
    private static partial uint RegisterClipboardFormat(string name);

    [LibraryImport("kernel32.dll")]
    private static partial nint GlobalLock(nint handle);

    [LibraryImport("kernel32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool GlobalUnlock(nint handle);

    [LibraryImport("kernel32.dll")]
    private static partial nuint GlobalSize(nint handle);
}
