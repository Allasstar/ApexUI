namespace ApexUI.Tests;

/// Headless IPlatform: Run returns at once, and tests send events through Listener.
/// Sizes and pointer positions are surface pixels, as from a real backend.
internal sealed class TestPlatform(int pixelWidth = 800, int pixelHeight = 600, float dpiScale = 1f) : IPlatform
{
    private string? _clipboard;

    public IPlatformListener Listener { get; private set; } = null!;
    public CursorShape Cursor { get; private set; }
    public int FramesRequested { get; private set; }

    public static (UiHost Host, TestPlatform Platform) Show(
        Widget root, int pixelWidth = 800, int pixelHeight = 600, float dpiScale = 1f)
    {
        var platform = new TestPlatform(pixelWidth, pixelHeight, dpiScale);
        var host     = new UiHost(platform);
        host.Run(root);
        return (host, platform);
    }

    public void Run(IPlatformListener listener)
    {
        Listener = listener;
        listener.Resize(pixelWidth, pixelHeight, dpiScale);
    }

    public void Click(float x, float y, PointerButton button = PointerButton.Left)
    {
        Listener.PointerMove(x, y);
        Listener.PointerDown(x, y, button);
        Listener.PointerUp(x, y, button);
    }

    /// Renders one frame into a raster bitmap of the surface size. The caller disposes it.
    public SKBitmap Render()
    {
        var bitmap = new SKBitmap(pixelWidth, pixelHeight);
        using var canvas = new SKCanvas(bitmap);
        Listener.Render(canvas, 1f / 60f);
        return bitmap;
    }

    public void RequestFrame() => FramesRequested++;
    public string? GetClipboardText() => _clipboard;
    public void SetClipboardText(string text) => _clipboard = text;
    public void SetCursor(CursorShape cursor) => Cursor = cursor;
    public IDisposable StartTimer(TimeSpan interval, Action tick) => throw new NotSupportedException();
    public Task<string?> OpenFileAsync(FileDialogOptions options) => Task.FromResult<string?>(null);
    public Task<string?> SaveFileAsync(FileDialogOptions options) => Task.FromResult<string?>(null);
}
