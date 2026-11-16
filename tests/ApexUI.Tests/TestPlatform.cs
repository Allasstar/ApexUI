namespace ApexUI.Tests;

/// Headless IPlatform: Run returns at once, and tests send events through Listener.
internal sealed class TestPlatform(int width = 800, int height = 600) : IPlatform
{
    private string? _clipboard;

    public IPlatformListener Listener { get; private set; } = null!;
    public CursorShape Cursor { get; private set; }
    public int FramesRequested { get; private set; }

    public static (UiHost Host, TestPlatform Platform) Show(Widget root, int width = 800, int height = 600)
    {
        var platform = new TestPlatform(width, height);
        var host     = new UiHost(platform);
        host.Run(root);
        return (host, platform);
    }

    public void Run(IPlatformListener listener)
    {
        Listener = listener;
        listener.Resize(width, height);
    }

    public void Click(float x, float y, PointerButton button = PointerButton.Left)
    {
        Listener.PointerMove(x, y);
        Listener.PointerDown(x, y, button);
        Listener.PointerUp(x, y, button);
    }

    public void Render()
    {
        using var surface = SKSurface.Create(new SKImageInfo(width, height));
        Listener.Render(surface.Canvas, 1f / 60f);
    }

    public void RequestFrame() => FramesRequested++;
    public string? GetClipboardText() => _clipboard;
    public void SetClipboardText(string text) => _clipboard = text;
    public void SetCursor(CursorShape cursor) => Cursor = cursor;
    public IDisposable StartTimer(TimeSpan interval, Action tick) => throw new NotSupportedException();
    public Task<string?> OpenFileAsync(FileDialogOptions options) => Task.FromResult<string?>(null);
    public Task<string?> SaveFileAsync(FileDialogOptions options) => Task.FromResult<string?>(null);
}
