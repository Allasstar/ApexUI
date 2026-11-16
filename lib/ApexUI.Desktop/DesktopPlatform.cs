using System.Diagnostics;
using Silk.NET.Input;
using Silk.NET.Maths;
using Silk.NET.OpenGL;
using Silk.NET.Windowing;
using SilkKey = Silk.NET.Input.Key;

namespace ApexUI.Desktop;

/// Desktop backend: a GLFW window (via Silk.NET) with an OpenGL-backed Skia surface.
/// The window size is given in logical units; on a scaled display the window grows to match,
/// and it is resized again when it moves to a display with another scale.
///
///   new UiHost(new DesktopPlatform("My App", 900, 700).WithIcon("res/icon.svg")).Run(root);
public sealed class DesktopPlatform : IPlatform
{
    private readonly IWindow   _window;
    private readonly Stopwatch _clock = Stopwatch.StartNew();
    private readonly List<DesktopTimer> _timers = [];

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
    // Window coordinates per logical unit that the current window size was chosen for.
    private float _windowUnitsPerLogical = 1f;
    private Vector2D<int> _surfaceSize;

    private string?     _iconPath;
    private CursorShape _cursor = CursorShape.Arrow;

    public DesktopPlatform(string title = "ApexUI App", int width = 900, int height = 600)
    {
        var options = WindowOptions.Default with
        {
            Title                      = title,
            Size                       = new Vector2D<int>(width, height),
            PreferredDepthBufferBits   = 0,
            PreferredStencilBufferBits = 8,  // Skia needs stencil
            Samples                    = 4,  // 4x MSAA — driver clamps if unsupported
            IsVisible                  = false, // shown once sized for the display's scale
        };
        _window = Window.Create(options);

        _window.Load    += OnLoad;
        _window.Update  += _ => RunDueTimers();
        _window.Render  += OnRender;
        _window.Resize            += _ => SyncSize();
        _window.FramebufferResize += _ => SyncSize();
        _window.Move              += _ => SyncSize();
        _window.Closing += OnClosing;
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
        _window.Run();
        _window.Dispose();
    }

    public void RequestFrame()
    {
        if (_window.IsInitialized) _window.ContinueEvents();
    }

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
        return timer;
    }

    public Task<string?> OpenFileAsync(FileDialogOptions options)
        => Task.FromException<string?>(new NotSupportedException("File dialogs are not implemented on desktop yet."));

    public Task<string?> SaveFileAsync(FileDialogOptions options)
        => Task.FromException<string?>(new NotSupportedException("File dialogs are not implemented on desktop yet."));

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

    // Brings the window size, the surface and the listener in line with the display's scale.
    // Called on every size, framebuffer, position and frame change, because a scale change
    // (moving to another display, changing display settings) has no event of its own here.
    private void SyncSize()
    {
        if (_gl is null) return; // events can arrive before Load
        var size = _window.Size;
        var fb   = _window.FramebufferSize;
        if (size.X <= 0 || size.Y <= 0 || fb.X <= 0 || fb.Y <= 0) return; // minimized

        _pixelRatio = fb.X / (float)size.X;
        float dpiScale = _contentScale?.Read() ?? _pixelRatio;

        float units = dpiScale / _pixelRatio;
        if (MathF.Abs(units - _windowUnitsPerLogical) > 0.01f)
        {
            float k = units / _windowUnitsPerLogical;
            _windowUnitsPerLogical = units;
            _window.Size = new Vector2D<int>((int)MathF.Round(size.X * k), (int)MathF.Round(size.Y * k));
            return; // the resize comes back here with the new size
        }

        if (fb == _surfaceSize && dpiScale == _dpiScale) return;
        _surfaceSize = fb;
        _dpiScale    = dpiScale;
        SetupSurface(fb.X, fb.Y);
        _listener!.Resize(fb.X, fb.Y, dpiScale);
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
        _listener!.Render(canvas, (float)delta);
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
    }

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
        => _listener!.PointerMove(pos.X * _pixelRatio, pos.Y * _pixelRatio);

    private void OnMouseDown(IMouse mouse, MouseButton button)
        => _listener!.PointerDown(mouse.Position.X * _pixelRatio, mouse.Position.Y * _pixelRatio, MapButton(button));

    private void OnMouseUp(IMouse mouse, MouseButton button)
        => _listener!.PointerUp(mouse.Position.X * _pixelRatio, mouse.Position.Y * _pixelRatio, MapButton(button));

    private void OnScroll(IMouse mouse, ScrollWheel wheel)
        => _listener!.Wheel(wheel.X, wheel.Y);

    private void OnKeyDown(IKeyboard keyboard, SilkKey key, int scancode)
        => _listener!.KeyDown(KeyMap.From(key), Modifiers(keyboard));

    private void OnKeyUp(IKeyboard keyboard, SilkKey key, int scancode)
        => _listener!.KeyUp(KeyMap.From(key), Modifiers(keyboard));

    private void OnKeyChar(IKeyboard keyboard, char c)
    {
        if (!char.IsControl(c))
            _listener!.TextEntered(c.ToString());
    }

    private static KeyModifiers Modifiers(IKeyboard kb)
    {
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
