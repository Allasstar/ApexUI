using System.Diagnostics;
using Silk.NET.Input;
using Silk.NET.Maths;
using Silk.NET.OpenGL;
using Silk.NET.Windowing;
using Silk.NET.Windowing.Glfw;
using SilkKey = Silk.NET.Input.Key;

namespace ApexUI.Desktop;

/// Desktop backend: a GLFW window (via Silk.NET) with an OpenGL-backed Skia surface.
/// The window size is given in logical units; on a scaled display the window grows to match,
/// and it is resized again when it moves to a display with another scale.
/// The loop sleeps until an input event, a due timer or a frame request, so an idle window uses no CPU.
///
///   new UiHost(new DesktopPlatform("My App", 900, 700).WithIcon("res/icon.svg")).Run(root);
public sealed class DesktopPlatform : IPlatform
{
    private readonly IWindow   _window;
    private readonly Stopwatch _clock = Stopwatch.StartNew();
    private readonly List<DesktopTimer> _timers = [];
    // Wakes the event wait when the next timer is due; GLFW has no timers of its own.
    private readonly Timer _wakeTimer;
    private readonly LoopSynchronizationContext _context;

    private IPlatformListener? _listener;
    private GL?                _gl;
    private IInputContext?     _input;
    private IMouse?            _mouse;
    private IKeyboard?         _keyboard;
    private GRContext?         _grContext;
    private GRBackendRenderTarget? _renderTarget;
    private SKSurface?         _surface;
    private ContentScale?      _contentScale;

    // Framebuffer pixels per window coordinate: 1 on Windows and X11, 2 on Retina macOS.
    private float _pixelRatio = 1f;
    // Framebuffer pixels per logical unit, as last reported to the listener.
    private float _dpiScale = 1f;
    private Vector2D<int> _surfaceSize;

    private string?     _iconPath;
    private CursorShape _cursor = CursorShape.Arrow;
    // The first frame is drawn once the window is sized, before anything asks for one.
    private bool        _frameRequested = true;
    private TimeSpan    _lastResizeFrame;
    // Logical units; converted to window coordinates with the current scale whenever it changes.
    private (float Width, float Height) _minimumSize;
    private bool        _sizeLimitsPending;

    public DesktopPlatform(string title = "ApexUI App", int width = 900, int height = 600)
    {
        var options = WindowOptions.Default with
        {
            Title                      = title,
            Size                       = new Vector2D<int>(width, height),
            PreferredDepthBufferBits   = 0,
            PreferredStencilBufferBits = 8,  // Skia needs stencil
            Samples                    = 4,  // 4x MSAA — driver clamps if unsupported
            IsVisible                  = false, // shown once the surface is set up
        };
        _window = Window.Create(options);

        _window.Load    += OnLoad;
        _window.Render  += OnRender;
        _window.Resize            += _ => OnWindowChanged();
        _window.FramebufferResize += _ => OnWindowChanged();
        _window.Move              += _ => OnWindowChanged();
        _window.StateChanged      += _ => OnWindowChanged();
        _window.FocusChanged      += _ => OnWindowChanged();
        _window.Closing += OnClosing;

        _wakeTimer = new Timer(_ => _window.ContinueEvents());
        _context   = new LoopSynchronizationContext(Wake);
    }

    /// Window icon from a raster or SVG file (by extension). Call before Run.
    public DesktopPlatform WithIcon(string path)
    {
        _iconPath = path;
        return this;
    }

    public void Run(IPlatformListener listener)
    {
        _listener = listener;
        var previousContext = SynchronizationContext.Current;
        SynchronizationContext.SetSynchronizationContext(_context);
        RequestScaleToMonitor();
        _window.Initialize();
        while (!_window.IsClosing)
        {
            // Wait for events only when there is nothing to draw or run; otherwise just collect them.
            _window.IsEventDriven = !_frameRequested && !_context.HasWork;
            _window.DoEvents();
            if (_window.IsClosing) break;
            _window.DoUpdate();
            _context.RunPending();
            RunDueTimers();
            ApplySizeLimits();
            if (_frameRequested)
            {
                _frameRequested = false;
                _window.DoRender();
            }
        }
        _window.DoEvents();
        _window.Reset();

        // Wait for a wake callback in flight, so none reaches the window after it is gone.
        using (var stopped = new ManualResetEvent(false))
            if (_wakeTimer.Dispose(stopped)) stopped.WaitOne();
        _window.Dispose();
        SynchronizationContext.SetSynchronizationContext(previousContext);
    }

    // GLFW sizes the window for the display's scale on creation and, on Windows, resizes it to the rectangle
    // WM_DPICHANGED suggests when it moves to a display with another scale. Resizing it here instead loops
    // while the window straddles two displays: the resize moves most of it back onto the other display.
    // Silk.NET 2.23 has no name for GLFW_SCALE_TO_MONITOR and sets its own hints without resetting the rest.
    private void RequestScaleToMonitor()
    {
        const int scaleToMonitor = 0x0002200C;
        GlfwWindowing.GetExistingApi(_window)?.WindowHint((Silk.NET.GLFW.WindowHintBool)scaleToMonitor, true);
    }

    public void RequestFrame()
    {
        if (_frameRequested) return;
        _frameRequested = true;
        if (_window.IsInitialized) _window.ContinueEvents();
    }

    public TimeSpan Now => _clock.Elapsed;

    public float ResizeFrameRate { get; set; } = 60f;

    /// Redraws per second while the user drags a window edge; 0 redraws only when the drag ends.
    public DesktopPlatform WithResizeFrameRate(float framesPerSecond)
    {
        ResizeFrameRate = Math.Max(0f, framesPerSecond);
        return this;
    }

    public void SetTitle(string title) => _window.Title = title;

    public string? GetClipboardText() => _keyboard?.ClipboardText;

    public void SetClipboardText(string text)
    {
        if (_keyboard is not null) _keyboard.ClipboardText = text;
    }

    public void SetCursor(CursorShape cursor)
    {
        _cursor = cursor;
        ApplyCursor();
    }

    public IDisposable StartTimer(TimeSpan interval, Action tick)
    {
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(interval, TimeSpan.Zero);
        var timer = new DesktopTimer(interval, tick, _clock.Elapsed + interval);
        _timers.Add(timer);
        ScheduleWake();
        return timer;
    }

    public Task<string?> OpenFileAsync(FileDialogOptions options) => FileDialogs.OpenAsync(options, OwnerHandle());

    public Task<string?> SaveFileAsync(FileDialogOptions options) => FileDialogs.SaveAsync(options, OwnerHandle());

    public Task<string?> PickFolderAsync(string title) => FileDialogs.PickFolderAsync(title, OwnerHandle());

    public void SetMinimumSize(float width, float height)
    {
        _minimumSize = (Math.Max(0f, width), Math.Max(0f, height));
        _sizeLimitsPending = true;
        if (_window.IsInitialized) _window.ContinueEvents();
    }

    // Applied from the main loop only: on Windows a smaller window is resized at once, and that resize must not
    // reach the listener in the middle of a frame (a layout pass may be what asked for the limit).
    // GLFW's limits are in its screen coordinates, which Silk's window size is not on a scaled Windows display, so
    // the ratio comes from GLFW itself. Windows ignores a minimum unless both sides have one, hence 1 for "none".
    private unsafe void ApplySizeLimits()
    {
        if (!_sizeLimitsPending || _window.Native?.Glfw is not { } native || GlfwWindowing.GetExistingApi(_window) is not { } glfw) return;
        var handle = (Silk.NET.GLFW.WindowHandle*)native;
        glfw.GetWindowSize(handle, out int windowWidth, out _);
        glfw.GetFramebufferSize(handle, out int framebufferWidth, out _);
        if (windowWidth <= 0 || framebufferWidth <= 0) return; // minimized: try again later
        _sizeLimitsPending = false;

        const int dontCare = -1;
        (float w, float h) = _minimumSize;
        if (w <= 0f && h <= 0f)
        {
            glfw.SetWindowSizeLimits(handle, dontCare, dontCare, dontCare, dontCare);
            return;
        }

        float toWindow = _dpiScale * windowWidth / framebufferWidth;
        glfw.SetWindowSizeLimits(handle, Math.Max(1, (int)MathF.Ceiling(w * toWindow)), Math.Max(1, (int)MathF.Ceiling(h * toWindow)), dontCare, dontCare);
    }

    // The dialog belongs to the window (stays on top of it and blocks its input) where the OS supports that.
    private nint OwnerHandle() => _window.Native?.Win32?.Hwnd ?? 0;

    private void Wake()
    {
        if (_window.IsInitialized) _window.ContinueEvents();
    }

    // ── Window lifecycle ──────────────────────────────────────────────────────

    private void OnLoad()
    {
        _gl        = _window.CreateOpenGL();
        _grContext = GRContext.CreateGl(GRGlInterface.Create());
        _contentScale = ContentScale.For(_window);

        if (_iconPath is not null)
            WindowIcon.Apply(_window, _iconPath);

        _input = _window.CreateInput();
        foreach (var mouse in _input.Mice)
        {
            mouse.MouseMove += OnMouseMove;
            mouse.MouseDown += OnMouseDown;
            mouse.MouseUp   += OnMouseUp;
            mouse.Scroll    += OnScroll;
        }
        foreach (var keyboard in _input.Keyboards)
        {
            keyboard.KeyDown += OnKeyDown;
            keyboard.KeyUp   += OnKeyUp;
            keyboard.KeyChar += OnKeyChar;
        }
        _mouse    = _input.Mice.Count > 0 ? _input.Mice[0] : null;
        _keyboard = _input.Keyboards.Count > 0 ? _input.Keyboards[0] : null;
        ApplyCursor();

        SyncSize();
        _window.IsVisible = true;
    }

    // A restored, uncovered or re-focused window may show stale content, and the scale may have changed.
    private void OnWindowChanged()
    {
        if (SyncSize() && TryRenderDuringResize()) return;
        RequestFrame();
    }

    // While the user drags a window edge, Windows and macOS run their own modal resize loop and DoEvents does
    // not return until the drag ends, so the main loop cannot draw. The size callbacks still arrive inside that
    // loop, so the frame is drawn right here, at most ResizeFrameRate times per second. A skipped size is drawn
    // by the next callback or by the main loop once the drag ends.
    private bool TryRenderDuringResize()
    {
        if (ResizeFrameRate <= 0f) return false;

        var now = _clock.Elapsed;
        if (now - _lastResizeFrame < TimeSpan.FromSeconds(1.0 / ResizeFrameRate)) return false;

        _lastResizeFrame = now;
        _frameRequested  = false;
        _window.DoRender();
        return true;
    }

    // Brings the surface and the listener in line with the window size and the display's scale.
    // Called on every size, framebuffer, position and frame change, because a scale change
    // (moving to another display, changing display settings) has no event of its own here.
    // Returns whether the surface changed size or scale.
    private bool SyncSize()
    {
        if (_gl is null) return false; // events can arrive before Load
        var size = _window.Size;
        var fb   = _window.FramebufferSize;
        if (size.X <= 0 || size.Y <= 0 || fb.X <= 0 || fb.Y <= 0) return false; // minimized

        _pixelRatio = fb.X / (float)size.X;
        float dpiScale = _contentScale?.Read() ?? _pixelRatio;

        if (fb == _surfaceSize && dpiScale == _dpiScale) return false;
        if (dpiScale != _dpiScale && _minimumSize != default) _sizeLimitsPending = true;
        _surfaceSize = fb;
        _dpiScale    = dpiScale;
        SetupSurface(fb.X, fb.Y);
        _listener!.Resize(fb.X, fb.Y, dpiScale);
        return true;
    }

    private void SetupSurface(int w, int h)
    {
        _surface?.Dispose();
        _renderTarget?.Dispose();

        _gl!.GetInteger((GetPName)0x8CA6, out int framebuffer); // GL_FRAMEBUFFER_BINDING
        _gl!.GetInteger((GetPName)0x80A9, out int samples);     // GL_SAMPLES — actual MSAA granted by driver

        var fbInfo = new GRGlFramebufferInfo((uint)framebuffer, 0x8058); // GL_RGBA8
        _renderTarget = new GRBackendRenderTarget(w, h, samples, 8, fbInfo);
        _surface = SKSurface.Create(_grContext!, _renderTarget, GRSurfaceOrigin.BottomLeft, SKColorType.Rgba8888);
    }

    private void OnRender(double delta)
    {
        SyncSize();
        if (_surface is null) return;
        var canvas = _surface.Canvas;
        _listener!.Render(canvas, _clock.Elapsed);
        canvas.Flush();
    }

    private void OnClosing()
    {
        _listener!.Close();
        _timers.Clear();
        _surface?.Dispose();
        _renderTarget?.Dispose();
        _grContext?.Dispose();
        _input?.Dispose();
        _gl?.Dispose();
    }

    private void RunDueTimers()
    {
        var now = _clock.Elapsed;
        // Backwards so a tick that starts a new timer (appended) does not shift unvisited entries.
        for (int i = _timers.Count - 1; i >= 0; i--)
        {
            var timer = _timers[i];
            if (timer.IsStopped) { _timers.RemoveAt(i); continue; }
            if (now < timer.Due) continue;
            timer.Due = now + timer.Interval;
            timer.Tick();
        }
        ScheduleWake();
    }

    private void ScheduleWake()
    {
        var next = TimeSpan.MaxValue;
        foreach (var timer in _timers)
            if (!timer.IsStopped && timer.Due < next) next = timer.Due;

        if (next == TimeSpan.MaxValue)
            _wakeTimer.Change(Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);
        else
            _wakeTimer.Change(Max(next - _clock.Elapsed, TimeSpan.Zero), Timeout.InfiniteTimeSpan);
    }

    private static TimeSpan Max(TimeSpan a, TimeSpan b) => a > b ? a : b;

    private void ApplyCursor()
    {
        if (_mouse is null) return;
        _mouse.Cursor.Type           = CursorType.Standard;
        _mouse.Cursor.StandardCursor = _cursor switch
        {
            CursorShape.Text             => StandardCursor.IBeam,
            CursorShape.Hand             => StandardCursor.Hand,
            CursorShape.Crosshair        => StandardCursor.Crosshair,
            CursorShape.ResizeHorizontal => StandardCursor.HResize,
            CursorShape.ResizeVertical   => StandardCursor.VResize,
            CursorShape.ResizeNwse       => StandardCursor.NwseResize,
            CursorShape.ResizeNesw       => StandardCursor.NeswResize,
            CursorShape.Move             => StandardCursor.ResizeAll,
            CursorShape.NotAllowed       => StandardCursor.NotAllowed,
            CursorShape.Wait             => StandardCursor.Wait,
            _                            => StandardCursor.Arrow,
        };
    }

    // ── Input ─────────────────────────────────────────────────────────────────

    private void OnMouseMove(IMouse mouse, System.Numerics.Vector2 pos)
        => _listener!.PointerMove(pos.X * _pixelRatio, pos.Y * _pixelRatio, Modifiers());

    private void OnMouseDown(IMouse mouse, MouseButton button)
        => _listener!.PointerDown(mouse.Position.X * _pixelRatio, mouse.Position.Y * _pixelRatio, MapButton(button), Modifiers());

    private void OnMouseUp(IMouse mouse, MouseButton button)
        => _listener!.PointerUp(mouse.Position.X * _pixelRatio, mouse.Position.Y * _pixelRatio, MapButton(button), Modifiers());

    private void OnScroll(IMouse mouse, ScrollWheel wheel)
        => _listener!.Wheel(wheel.X, wheel.Y, Modifiers());

    private void OnKeyDown(IKeyboard keyboard, SilkKey key, int scancode)
        => _listener!.KeyDown(KeyMap.From(key), Modifiers());

    private void OnKeyUp(IKeyboard keyboard, SilkKey key, int scancode)
        => _listener!.KeyUp(KeyMap.From(key), Modifiers());

    private void OnKeyChar(IKeyboard keyboard, char c)
    {
        if (!char.IsControl(c))
            _listener!.TextEntered(c.ToString());
    }

    private KeyModifiers Modifiers()
    {
        var kb = _keyboard;
        if (kb is null) return KeyModifiers.None;
        var m = KeyModifiers.None;
        if (kb.IsKeyPressed(SilkKey.ShiftLeft)   || kb.IsKeyPressed(SilkKey.ShiftRight))   m |= KeyModifiers.Shift;
        if (kb.IsKeyPressed(SilkKey.ControlLeft) || kb.IsKeyPressed(SilkKey.ControlRight)) m |= KeyModifiers.Control;
        if (kb.IsKeyPressed(SilkKey.AltLeft)     || kb.IsKeyPressed(SilkKey.AltRight))     m |= KeyModifiers.Alt;
        if (kb.IsKeyPressed(SilkKey.SuperLeft)   || kb.IsKeyPressed(SilkKey.SuperRight))   m |= KeyModifiers.Super;
        return m;
    }

    private static PointerButton MapButton(MouseButton b) => b switch
    {
        MouseButton.Left   => PointerButton.Left,
        MouseButton.Right  => PointerButton.Right,
        MouseButton.Middle => PointerButton.Middle,
        _                  => PointerButton.None,
    };

    private sealed class DesktopTimer(TimeSpan interval, Action tick, TimeSpan due) : IDisposable
    {
        public TimeSpan Interval  { get; } = interval;
        public Action   Tick      { get; } = tick;
        public TimeSpan Due       { get; set; } = due;
        public bool     IsStopped { get; private set; }

        public void Dispose() => IsStopped = true;
    }
}
