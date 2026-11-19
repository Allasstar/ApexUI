using System.Runtime.InteropServices;

namespace ApexUI.Core;

/// Text blobs for the string text helpers of DrawContext, so text drawn again on the next frame reuses its
/// glyphs: drawing a string straight to the canvas creates native objects and their wrappers on every call.
/// A blob not drawn during a whole frame is dropped when the next one begins.
internal sealed class TextBlobCache : IDisposable
{
    private readonly Dictionary<(string Text, SKFont Font), Entry> _entries = new();
    private int _frame;

    public int Count => _entries.Count;

    /// The blob for text in font (null when the text has no glyphs) and its advance width.
    public SKTextBlob? Get(string text, SKFont font, out float width)
    {
        ref Entry entry = ref CollectionsMarshal.GetValueRefOrAddDefault(_entries, (text, font), out bool exists);
        if (!exists)
        {
            entry.Blob  = SKTextBlob.Create(text, font);
            entry.Width = font.MeasureText(text);
        }
        entry.LastFrame = _frame;
        width = entry.Width;
        return entry.Blob;
    }

    public void BeginFrame()
    {
        _frame++;
        foreach (var (key, entry) in _entries)
        {
            if (entry.LastFrame >= _frame - 1) continue;
            entry.Blob?.Dispose();
            _entries.Remove(key);
        }
    }

    public void Dispose()
    {
        foreach (var entry in _entries.Values) entry.Blob?.Dispose();
        _entries.Clear();
    }

    private struct Entry
    {
        public SKTextBlob? Blob;
        public float       Width;
        public int         LastFrame;
    }
}
