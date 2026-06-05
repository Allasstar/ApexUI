// src/Core/Application.cs
//
// The entry point for every app built on ApexUI.
// Creates the OS window, runs the event loop, and drives Measure→Arrange→Draw
// every frame.  App developers never touch Silk.NET or SkiaSharp directly.

using Silk.NET.Core;
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
    private string?    _pendingIconPath;

    internal static Application? Current { get; private set; }
    private readonly List<Overlay> _overlays = [];

    private ThemePreset _preset = ThemePreset.Default;
    private bool        _isDark;

    public Theme Theme { get; set; } = Theme.Light;
    public float DpiScale { get; private set; } = 1f;

    public string FontFamily
    {
        get;
        set { field = value; _root?.InvalidateLayout(); }
    } = "Segoe UI";

    public float UiScale
    {
        get;
        set { field = Math.Clamp(value, 0.1f, 10f); _root?.InvalidateLayout(); }
    } = 1f;

    // Combined scale applied to layout coordinates and pointer positions.
    private float TotalScale => DpiScale * UiScale;

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
        Current = this;
    }

    // ── Public API ────────────────────────────────────────────────────────────

    /// Set the window icon from a raster or SVG file (auto-detected by extension).
    /// SVG is rendered at 16, 32, and 48 px so the OS can pick the best size.
    public Application SetIcon(string path)
    {
        _pendingIconPath = path;
        return this;
    }

    public Application BindUiScale(Bindable<float> source)
    {
        UiScale = source.Value;
        source.Changed += v => UiScale = v;
        return this;
    }

    public Application BindTheme(Bindable<ThemePreset> source)
    {
        _preset = source.Value;
        ApplyTheme();
        source.Changed += p => { _preset = p; ApplyTheme(); };
        return this;
    }

    public Application BindDarkMode(Bindable<bool> isDark)
    {
        _isDark = isDark.Value;
        ApplyTheme();
        isDark.Changed += v => { _isDark = v; ApplyTheme(); };
        return this;
    }

    public Application BindFontFamily(Bindable<string> source)
    {
        FontFamily = source.Value;
        source.Changed += v => FontFamily = v;
        return this;
    }

    private void ApplyTheme()
    {
        Theme = ThemeLibrary.Get(_preset, _isDark);
        _root?.Invalidate();
    }

    internal void RegisterOverlay(Overlay overlay)
    {
        if (!_overlays.Contains(overlay))
            _overlays.Add(overlay);
    }

    internal void UnregisterOverlay(Overlay overlay)
        => _overlays.Remove(overlay);

    private Widget? HitTestAll(float x, float y)
    {
        for (int i = _overlays.Count - 1; i >= 0; i--)
        {
            var hit = _overlays[i].HitTest(x, y);
            if (hit is not null) return hit;
        }
        return _root?.HitTest(x, y);
    }

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

        if (_pendingIconPath is not null)
            ApplyIcon(_pendingIconPath);

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

        if (_root.IsLayoutDirty)
            LayoutRoot();

        TickWidgets(_root, (float)delta);
        foreach (var o in _overlays) TickWidgets(o, (float)delta);

        var canvas = _surface.Canvas;
        canvas.Clear(Theme.Background);
        canvas.Save();
        canvas.Scale(UiScale, UiScale);

        var ctx = new DrawContext(canvas, Theme, DpiScale, FontFamily);
        _root.Draw(ctx);

        // Overlays are laid out and drawn after the root tree so they
        // appear above everything without inheriting any clip stack.
        var logicalSize = new Size(_window.Size.X / TotalScale, _window.Size.Y / TotalScale);
        foreach (var o in _overlays)
        {
            o.Measure(logicalSize);
            o.Arrange(new Rect(0, 0, logicalSize.Width, logicalSize.Height));
            o.Draw(ctx);
        }

        canvas.Restore();
        canvas.Flush();
    }

    private void LayoutRoot()
    {
        if (_root is null) return;
        var size = _window.Size;
        var available = new Size(size.X / TotalScale, size.Y / TotalScale);
        _root.Measure(available);
        _root.Arrange(new Rect(0, 0, available.Width, available.Height));
    }

    private void TickWidgets(Widget w, float delta)
    {
        if (w is ITickable t) t.Tick(delta);
        foreach (var child in w.Children) TickWidgets(child, delta);
    }

    // ── Input ─────────────────────────────────────────────────────────────────

    private void OnMouseMove(IMouse mouse, System.Numerics.Vector2 pos)
    {
        float x = pos.X / TotalScale, y = pos.Y / TotalScale;
        var hit = HitTestAll(x, y);

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
        // Route move to the pressed (captured) widget so drags work outside bounds
        (_pressedWidget ?? hit)?.OnPointerMove?.Invoke(new PointerEvent(x, y, PointerButton.None, false));
    }

    private void OnMouseDown(IMouse mouse, MouseButton btn)
    {
        float x = mouse.Position.X / TotalScale, y = mouse.Position.Y / TotalScale;
        var hit = HitTestAll(x, y);

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
        float x = mouse.Position.X / TotalScale, y = mouse.Position.Y / TotalScale;
        var hit = HitTestAll(x, y);

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

    private void OnScroll(IMouse mouse, ScrollWheel wheel)
    {
        // Walk up from the hovered widget to find the nearest scroll handler.
        var w = _hoveredWidget;
        while (w is not null)
        {
            if (w.OnScroll is not null) { w.OnScroll(wheel.X, wheel.Y); return; }
            w = w.Parent;
        }
    }

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

    // ── Icon ──────────────────────────────────────────────────────────────────

    private void ApplyIcon(string path)
    {
        if (!Path.IsPathRooted(path))
            path = Path.Combine(AppContext.BaseDirectory, path);

        if (!File.Exists(path))
        {
            Console.Error.WriteLine($"[ApexUI] Icon not found: {path}");
            return;
        }

        bool isSvg = Path.GetExtension(path).Equals(".svg", StringComparison.OrdinalIgnoreCase);

        if (isSvg)
        {
            var svg = new Svg.Skia.SKSvg();
            if (svg.Load(path) is null) return;
            var pic  = svg.Picture!;
            int[] sizes = [16, 32, 48];
            var rawImages = new RawImage[sizes.Length];
            for (int i = 0; i < sizes.Length; i++)
                rawImages[i] = RenderSvgToRaw(pic, sizes[i]);
            _window.SetWindowIcon(rawImages);
            svg.Dispose();
        }
        else
        {
            // ICO files contain multiple sizes — extract every frame so the OS picks the best one.
            // For single-image formats (PNG, JPG) SKCodec.FrameCount == 1, so this path works for all raster.
            using var codec = SKCodec.Create(path);
            if (codec is null) return;
            int count = Math.Max(1, codec.FrameCount);
            var rawImages = new RawImage[count];
            for (int i = 0; i < count; i++)
            {
                var info = codec.Info.WithColorType(SKColorType.Rgba8888).WithAlphaType(SKAlphaType.Unpremul);
                using var bmp = new SKBitmap(info);
                codec.GetPixels(info, bmp.GetPixels(), new SKCodecOptions(i));
                rawImages[i] = new RawImage(bmp.Width, bmp.Height, new Memory<byte>(bmp.Bytes));
            }
            _window.SetWindowIcon(rawImages);
        }
    }

    private static RawImage RenderSvgToRaw(SKPicture pic, int size)
    {
        float scale = size / Math.Max(pic.CullRect.Width, pic.CullRect.Height);
        var info = new SKImageInfo(size, size, SKColorType.Rgba8888, SKAlphaType.Unpremul);
        using var bmp    = new SKBitmap(info);
        using var canvas = new SKCanvas(bmp);
        canvas.Clear(SKColors.Transparent);
        canvas.Scale(scale, scale);
        canvas.DrawPicture(pic);
        return new RawImage(size, size, new Memory<byte>(bmp.Bytes));
    }

    private static RawImage BitmapToRaw(SKBitmap src)
    {
        var info = new SKImageInfo(src.Width, src.Height, SKColorType.Rgba8888, SKAlphaType.Unpremul);
        using var dst    = new SKBitmap(info);
        using var canvas = new SKCanvas(dst);
        canvas.Clear(SKColors.Transparent);
        canvas.DrawBitmap(src, 0, 0);
        return new RawImage(dst.Width, dst.Height, new Memory<byte>(dst.Bytes));
    }
}
