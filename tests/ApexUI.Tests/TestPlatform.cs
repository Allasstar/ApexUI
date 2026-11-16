namespace ApexUI.Tests;

/// Headless IPlatform: Run returns at once, and tests send events through Listener.
/// Sizes and pointer positions are surface pixels, as from a real backend.
/// Time stands still until Advance moves it, which also runs the timers that came due.
internal sealed class TestPlatform(int pixelWidth = 800, int pixelHeight = 600, float dpiScale = 1f) : IPlatform
{
    private readonly List<TestTimer> _timers = [];
    private string? _clipboard;

    public IPlatformListener Listener { get; private set; } = null!;
    public CursorShape Cursor { get; private set; }
    public int FramesRequested { get; private set; }
    public TimeSpan Now { get; private set; }
    public int ActiveTimers => _timers.Count(t => !t.IsStopped);

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
        Listener.Render(canvas, Now);
        return bitmap;
    }

    /// Renders one frame and returns how many frames were requested while rendering it.
    public int RenderAndCountRequests()
    {
        int before = FramesRequested;
        Render().Dispose();
        return FramesRequested - before;
    }

    public void Advance(TimeSpan time)
    {
        var end = Now + time;
        while (true)
        {
            var next = _timers.Where(t => !t.IsStopped && t.Due <= end).MinBy(t => t.Due);
            if (next is null) break;
            Now      = next.Due;
            next.Due = Now + next.Interval;
            next.Tick();
        }
        Now = end;
        _timers.RemoveAll(t => t.IsStopped);
    }

    public void RequestFrame() => FramesRequested++;
    public string? GetClipboardText() => _clipboard;
    public void SetClipboardText(string text) => _clipboard = text;
    public void SetCursor(CursorShape cursor) => Cursor = cursor;

    public IDisposable StartTimer(TimeSpan interval, Action tick)
    {
        var timer = new TestTimer(interval, tick, Now + interval);
        _timers.Add(timer);
        return timer;
    }

    public Task<string?> OpenFileAsync(FileDialogOptions options) => Task.FromResult<string?>(null);
    public Task<string?> SaveFileAsync(FileDialogOptions options) => Task.FromResult<string?>(null);

    private sealed class TestTimer(TimeSpan interval, Action tick, TimeSpan due) : IDisposable
    {
        public TimeSpan Interval  { get; } = interval;
        public Action   Tick      { get; } = tick;
        public TimeSpan Due       { get; set; } = due;
        public bool     IsStopped { get; private set; }

        public void Dispose() => IsStopped = true;
    }
}
