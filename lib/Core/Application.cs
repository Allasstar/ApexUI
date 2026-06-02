// src/Core/Application.cs
//
// The entry point for every app built on ApexUI.
// Creates the OS window, runs the event loop, and drives Measure→Arrange→Draw
// every frame.  App developers never touch Silk.NET or SkiaSharp directly.

using Silk.NET.Input;
using Silk.NET.Maths;
using Silk.NET.Windowing;
using Silk.NET.OpenGL;
using SkiaSharp;
using ApexUI.Core;

namespace ApexUI.Core;

public sealed class Application
{
    private readonly IWindow _window;
    private GRContext?       _grContext;
    private GRBackendRenderTarget? _renderTarget;
    private SKSurface?       _surface;
    private GL?              _gl;

    private Widget?    _root;
    private Widget?    _hoveredWidget;
    private Widget?    _focusedWidget;
    private Widget?    _pressedWidget;

    public Theme Theme { get; set; } = Theme.Light;
    public float DpiScale { get; private set; } = 1f;

    public Application(string title = "ApexUI App", int width = 900, int height = 600)
    {
        var options = WindowOptions.Default with
        {
            Title             = title,
            Size              = new Vector2D<int>(width, height),
            PreferredDepthBufferBits   = 0,
            PreferredStencilBufferBits = 8,  // Skia needs stencil
        };
        _window = Window.Create(options);

        _window.Load    += OnLoad;
        _window.Render  += OnRender;
        _window.Resize  += OnResize;
        _window.Closing += OnClosing;
    }

    // ── Public API ────────────────────────────────────────────────────────────

    /// Set the root widget tree and start the event loop.
    public void Run(Widget root)
    {
        _root = root;
        _window.Run();
    }

    // ── Window lifecycle ──────────────────────────────────────────────────────

    private void OnLoad()
    {
        _gl = _window.CreateOpenGL();

        // Build Skia GPU context on top of OpenGL
        var glInterface = GRGlInterface.Create();
        _grContext = GRContext.CreateGl(glInterface);

        SetupSurface(_window.Size.X, _window.Size.Y);

        // Wire up input
        var input = _window.CreateInput();
        foreach (var mouse in input.Mice)
        {
            mouse.MouseMove   += OnMouseMove;
            mouse.MouseDown   += OnMouseDown;
            mouse.MouseUp     += OnMouseUp;
            mouse.Scroll      += OnScroll;
        }
        foreach (var keyboard in input.Keyboards)
        {
            keyboard.KeyDown  += OnKeyDown;
            keyboard.KeyUp    += OnKeyUp;
            keyboard.KeyChar  += OnKeyChar;
        }
    }

    private void SetupSurface(int w, int h)
    {
        _renderTarget?.Dispose();
        _surface?.Dispose();

        // Query the current OpenGL framebuffer
        _gl!.GetInteger((GetPName)0x8CA6, out int framebuffer); // GL_FRAMEBUFFER_BINDING

        var fbInfo = new GRGlFramebufferInfo((uint)framebuffer, 0x8058); // GL_RGBA8
        _renderTarget = new GRBackendRenderTarget(w, h, 0, 8, fbInfo);
        _surface = SKSurface.Create(_grContext!, _renderTarget,
                                    GRSurfaceOrigin.BottomLeft,
                                    SKColorType.Rgba8888);
    }

    private void OnResize(Vector2D<int> size)
    {
        SetupSurface(size.X, size.Y);
        LayoutRoot();
    }

    private void OnClosing()
    {
        _surface?.Dispose();
        _renderTarget?.Dispose();
        _grContext?.Dispose();
    }

    // ── Render loop ───────────────────────────────────────────────────────────

    private void OnRender(double delta)
    {
        if (_root is null || _surface is null) return;

        var canvas = _surface.Canvas;
        canvas.Clear(Theme.Background);

        // Layout if dirty
        if (_root.IsLayoutDirty)
            LayoutRoot();

        // Tick animated widgets
        TickWidgets(_root, (float)delta);

        // Draw
        var ctx = new DrawContext(canvas, Theme, DpiScale);
        _root.Draw(ctx);

        canvas.Flush();
    }

    private void LayoutRoot()
    {
        if (_root is null) return;
        var size = _window.Size;
        var available = new Size(size.X / DpiScale, size.Y / DpiScale);
        _root.Measure(available);
        _root.Arrange(new Rect(0, 0, available.Width, available.Height));
    }

    private void TickWidgets(Widget w, float delta)
    {
        if (w is Widgets.TextInput input) input.Tick(delta);
        foreach (var child in w.Children) TickWidgets(child, delta);
    }

    // ── Input ─────────────────────────────────────────────────────────────────

    private void OnMouseMove(IMouse mouse, System.Numerics.Vector2 pos)
    {
        float x = pos.X / DpiScale, y = pos.Y / DpiScale;
        var hit = _root?.HitTest(x, y);

        // Hover state
        if (hit != _hoveredWidget)
        {
            if (_hoveredWidget is not null)
            {
                _hoveredWidget.IsHovered = false;
                _hoveredWidget.OnPointerExit?.Invoke(new PointerEvent(x, y, PointerButton.None, false));
                _hoveredWidget.Invalidate();
            }
            _hoveredWidget = hit;
            if (_hoveredWidget is not null)
            {
                _hoveredWidget.IsHovered = true;
                _hoveredWidget.OnPointerEnter?.Invoke(new PointerEvent(x, y, PointerButton.None, false));
                _hoveredWidget.Invalidate();
            }
        }
        hit?.OnPointerMove?.Invoke(new PointerEvent(x, y, PointerButton.None, false));
    }

    private void OnMouseDown(IMouse mouse, MouseButton btn)
    {
        float x = mouse.Position.X / DpiScale, y = mouse.Position.Y / DpiScale;
        var hit = _root?.HitTest(x, y);

        // Focus
        if (hit != _focusedWidget)
        {
            if (_focusedWidget is Widgets.TextInput ti) ti.IsFocused = false;
            _focusedWidget = hit;
            if (_focusedWidget is Widgets.TextInput ti2) ti2.IsFocused = true;
            _focusedWidget?.Invalidate();
        }

        _pressedWidget = hit;
        if (hit is not null)
        {
            hit.IsPressed = true;
            hit.OnPointerDown?.Invoke(new PointerEvent(x, y, MapButton(btn), true));
            hit.Invalidate();
        }
    }

    private void OnMouseUp(IMouse mouse, MouseButton btn)
    {
        float x = mouse.Position.X / DpiScale, y = mouse.Position.Y / DpiScale;
        var hit = _root?.HitTest(x, y);

        if (_pressedWidget is not null)
        {
            _pressedWidget.IsPressed = false;
            _pressedWidget.OnPointerUp?.Invoke(new PointerEvent(x, y, MapButton(btn), false));
            // Click = press + release on same widget
            if (_pressedWidget == hit)
                _pressedWidget.OnClick?.Invoke(new PointerEvent(x, y, MapButton(btn), false));
            _pressedWidget.Invalidate();
            _pressedWidget = null;
        }
    }

    private void OnScroll(IMouse mouse, ScrollWheel wheel) { /* TODO */ }

    private void OnKeyDown(IKeyboard kb, Key key, int _)
    {
        var mapped = MapKey(key);
        if (mapped.Length == 1) return; // printable char — OnKeyChar handles it with correct case/locale
        _focusedWidget?.OnKeyDown?.Invoke(new KeyEvent(
            mapped, true,
            kb.IsKeyPressed(Key.ControlLeft) || kb.IsKeyPressed(Key.ControlRight),
            kb.IsKeyPressed(Key.ShiftLeft)   || kb.IsKeyPressed(Key.ShiftRight),
            kb.IsKeyPressed(Key.AltLeft)     || kb.IsKeyPressed(Key.AltRight)));
    }

    private void OnKeyUp(IKeyboard kb, Key key, int _)
    {
        _focusedWidget?.OnKeyUp?.Invoke(new KeyEvent(MapKey(key), false, false, false, false));
    }

    private void OnKeyChar(IKeyboard kb, char c)
    {
        // Printable chars go through OnKeyDown as single-char key name
        if (!char.IsControl(c))
            _focusedWidget?.OnKeyDown?.Invoke(new KeyEvent(c.ToString(), true, false, false, false));
    }

    private static PointerButton MapButton(MouseButton b) => b switch
    {
        MouseButton.Left   => PointerButton.Left,
        MouseButton.Right  => PointerButton.Right,
        MouseButton.Middle => PointerButton.Middle,
        _ => PointerButton.None,
    };

    private static string MapKey(Key k) => k switch
    {
        Key.Backspace  => "Backspace",
        Key.Delete     => "Delete",
        Key.Enter      => "Enter",
        Key.Left       => "ArrowLeft",
        Key.Right      => "ArrowRight",
        Key.Up         => "ArrowUp",
        Key.Down       => "ArrowDown",
        Key.Home       => "Home",
        Key.End        => "End",
        Key.Tab        => "Tab",
        Key.Escape     => "Escape",
        _ => k.ToString(),
    };
}
