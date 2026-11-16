namespace ApexUI.Core;

/// Everything ApexUI needs from the operating system or browser: a window with a GPU surface,
/// input, clipboard, cursor, timers and file dialogs. A backend (for example ApexUI.Desktop)
/// implements it; UiHost holds the platform-neutral part of the app.
public interface IPlatform
{
    /// Opens the window and runs the event loop until it closes, reporting events to listener.
    /// Call Resize on the listener before the first Render, and again whenever the size or scale changes.
    void Run(IPlatformListener listener);

    /// Asks for one Render call as soon as possible. The platform renders only when asked
    /// (and after Resize), so an idle window costs nothing; several requests before the frame make one frame.
    void RequestFrame();

    string? GetClipboardText();
    void SetClipboardText(string text);

    void SetCursor(CursorShape cursor);

    /// Calls tick on the UI thread every interval until the returned handle is disposed.
    /// A tick does not render by itself; widgets it changes invalidate as usual.
    IDisposable StartTimer(TimeSpan interval, Action tick);

    /// Asks the user for a file to open. Completes with null when cancelled.
    Task<string?> OpenFileAsync(FileDialogOptions options);

    /// Asks the user where to save a file. Completes with null when cancelled.
    Task<string?> SaveFileAsync(FileDialogOptions options);
}

/// Receives events from an IPlatform. Sizes and pointer positions are in surface pixels:
/// the pixels of the canvas passed to Render.
public interface IPlatformListener
{
    /// dpiScale: surface pixels per logical unit at UiScale 1 (1.5 on a 150% Windows display, 2 on Retina).
    void Resize(float pixelWidth, float pixelHeight, float dpiScale);
    /// frameTime: a monotonic clock reading for this frame, used to time animations.
    void Render(SKCanvas canvas, TimeSpan frameTime);
    void PointerMove(float x, float y);
    void PointerDown(float x, float y, PointerButton button);
    void PointerUp(float x, float y, PointerButton button);
    void Wheel(float deltaX, float deltaY);
    void KeyDown(Key key, KeyModifiers modifiers);
    void KeyUp(Key key, KeyModifiers modifiers);
    /// Text typed by the user, already translated by the keyboard layout. Never contains control characters.
    void TextEntered(string text);
    void Close();
}

public enum CursorShape
{
    Arrow,
    Text,
    Hand,
    Crosshair,
    ResizeHorizontal,
    ResizeVertical,
    ResizeNwse,
    ResizeNesw,
    Move,
    NotAllowed,
    Wait,
}

/// Extensions are given without the dot, for example "svg".
public sealed record FileFilter(string Name, IReadOnlyList<string> Extensions);

public sealed record FileDialogOptions(string Title, IReadOnlyList<FileFilter> Filters, string? SuggestedFileName = null);
