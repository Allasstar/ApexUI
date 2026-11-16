namespace ApexUI.Core;

/// Everything ApexUI needs from the operating system or browser: a window with a GPU surface,
/// input, clipboard, cursor, timers and file dialogs. A backend (for example ApexUI.Desktop)
/// implements it; UiHost holds the platform-neutral part of the app.
public interface IPlatform
{
    /// Opens the window and runs the event loop until it closes, reporting events to listener.
    /// Call Resize on the listener before the first Render.
    void Run(IPlatformListener listener);

    /// Asks for a Render call as soon as possible.
    void RequestFrame();

    string? GetClipboardText();
    void SetClipboardText(string text);

    void SetCursor(CursorShape cursor);

    /// Calls tick on the UI thread every interval until the returned handle is disposed.
    IDisposable StartTimer(TimeSpan interval, Action tick);

    /// Asks the user for a file to open. Completes with null when cancelled.
    Task<string?> OpenFileAsync(FileDialogOptions options);

    /// Asks the user where to save a file. Completes with null when cancelled.
    Task<string?> SaveFileAsync(FileDialogOptions options);
}

/// Receives events from an IPlatform. Coordinates and sizes are in window pixels.
public interface IPlatformListener
{
    void Resize(float width, float height);
    void Render(SKCanvas canvas, float deltaSeconds);
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
