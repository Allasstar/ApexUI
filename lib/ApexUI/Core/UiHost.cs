namespace ApexUI.Core;

/// The platform-neutral side of an app: owns the widget tree, lays it out, draws it,
/// routes input to widgets and keeps overlays, focus and theme. A backend (IPlatform)
/// supplies the window, the canvas and the raw input events.
///
///   new UiHost(new DesktopPlatform("My App", 900, 700)).Run(root);
public sealed class UiHost : IPlatformListener
{
    private readonly List<Overlay> _overlays = [];
    private DrawContext? _drawContext;

    private Widget? _root;
    private Widget? _hoveredWidget;
    private Widget? _focusedWidget;
    private Widget? _pressedWidget;

    private float _pixelWidth;
    private float _pixelHeight;

    private string _preset = ThemeLibrary.DefaultName;
    private bool   _isDark;

    public UiHost(IPlatform platform)
    {
        Platform = platform;
    }

    public IPlatform Platform { get; }

    public Theme Theme { get; set; } = Theme.Light;

    /// Surface pixels per logical unit reported by the platform (display scaling).
    public float DpiScale { get; private set; } = 1f;

    public string FontFamily
    {
        get;
        set { field = value; _root?.InvalidateLayout(); }
    } = FontCache.DefaultFamily;

    public float UiScale
    {
        get;
        set { field = Math.Clamp(value, 0.1f, 10f); _root?.InvalidateLayout(); }
    } = 1f;

    /// The widget tree shown in the window. Replacing it detaches the previous root.
    public Widget? Root
    {
        get => _root;
        set
        {
            if (_root == value) return;
            _root?.DetachFromHost();
            _root = value;
            _root?.AttachToHost(this);
            _root?.InvalidateLayout();
        }
    }

    // Shared by layout-time text measurement and every frame's DrawContext.
    internal FontCache Fonts { get; } = new();

    /// Surface pixels per logical unit: DpiScale × UiScale. Applied once, as the canvas transform;
    /// layout, drawing and input all work in logical units.
    public float PixelScale => DpiScale * UiScale;

    /// Shows root and runs the platform's event loop until the window closes.
    public void Run(Widget root)
    {
        Root = root;
        Platform.Run(this);
    }

    public UiHost BindUiScale(Bindable<float> source)
    {
        UiScale = source.Value;
        source.Changed += v => UiScale = v;
        return this;
    }

    public UiHost BindTheme(Bindable<string> source)
    {
        _preset = source.Value;
        ApplyTheme();
        source.Changed += p => { _preset = p; ApplyTheme(); };
        return this;
    }

    public UiHost BindDarkMode(Bindable<bool> isDark)
    {
        _isDark = isDark.Value;
        ApplyTheme();
        isDark.Changed += v => { _isDark = v; ApplyTheme(); };
        return this;
    }

    public UiHost BindFontFamily(Bindable<string> source)
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

    // ── Overlays ──────────────────────────────────────────────────────────────

    internal void ShowOverlay(Overlay overlay)
    {
        if (!_overlays.Contains(overlay)) _overlays.Add(overlay);
    }

    internal void HideOverlay(Overlay overlay) => _overlays.Remove(overlay);

    // A detached widget must not keep receiving pointer or key events.
    internal void Forget(Widget widget)
    {
        if (_hoveredWidget == widget) _hoveredWidget = null;
        if (_pressedWidget == widget) _pressedWidget = null;
        if (_focusedWidget == widget) _focusedWidget = null;
    }

    private Widget? HitTestAll(float x, float y)
    {
        for (int i = _overlays.Count - 1; i >= 0; i--)
        {
            var hit = _overlays[i].HitTest(x, y);
            if (hit is not null) return hit;
        }
        return _root?.HitTest(x, y);
    }

    // ── Layout and drawing ────────────────────────────────────────────────────

    void IPlatformListener.Resize(float pixelWidth, float pixelHeight, float dpiScale)
    {
        _pixelWidth  = pixelWidth;
        _pixelHeight = pixelHeight;
        DpiScale     = dpiScale;
        LayoutRoot();
    }

    void IPlatformListener.Render(SKCanvas canvas, float deltaSeconds)
    {
        if (_root is null) return;

        if (_root.IsLayoutDirty)
            LayoutRoot();

        TickWidgets(_root, deltaSeconds);
        foreach (var o in _overlays) TickWidgets(o, deltaSeconds);

        canvas.Clear(Theme.Background);
        canvas.Save();
        canvas.Scale(PixelScale, PixelScale);

        _drawContext ??= new DrawContext(canvas, Theme, PixelScale, FontFamily, Fonts);
        _drawContext.BeginFrame(canvas, Theme, PixelScale, FontFamily);
        var ctx = _drawContext;
        _root.Draw(ctx);

        // Overlays are laid out and drawn after the root tree so they
        // appear above everything without inheriting any clip stack.
        var logicalSize = LogicalWindowSize;
        foreach (var o in _overlays)
        {
            o.Measure(logicalSize);
            o.Arrange(new Rect(0, 0, logicalSize.Width, logicalSize.Height));
            o.Draw(ctx);
        }

        canvas.Restore();
    }

    void IPlatformListener.Close()
    {
        _drawContext?.Dispose();
        _drawContext = null;
        Fonts.Dispose();
    }

    private Size LogicalWindowSize => new(_pixelWidth / PixelScale, _pixelHeight / PixelScale);

    private void LayoutRoot()
    {
        if (_root is null) return;
        var available = LogicalWindowSize;
        _root.Measure(available);
        _root.Arrange(new Rect(0, 0, available.Width, available.Height));
    }

    private static void TickWidgets(Widget w, float delta)
    {
        if (w is ITickable t) t.Tick(delta);
        foreach (var child in w.Children) TickWidgets(child, delta);
    }

    // ── Input ─────────────────────────────────────────────────────────────────

    void IPlatformListener.PointerMove(float px, float py)
    {
        float x = px / PixelScale, y = py / PixelScale;
        var hit = HitTestAll(x, y);

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

    void IPlatformListener.PointerDown(float px, float py, PointerButton button)
    {
        float x = px / PixelScale, y = py / PixelScale;
        var hit = HitTestAll(x, y);

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
            hit.OnPointerDown?.Invoke(new PointerEvent(x, y, button, true));
            hit.Invalidate();
        }
    }

    void IPlatformListener.PointerUp(float px, float py, PointerButton button)
    {
        float x = px / PixelScale, y = py / PixelScale;
        var hit = HitTestAll(x, y);

        if (_pressedWidget is not null)
        {
            _pressedWidget.IsPressed = false;
            _pressedWidget.OnPointerUp?.Invoke(new PointerEvent(x, y, button, false));
            // Click = press + release on same widget
            if (_pressedWidget == hit)
                _pressedWidget.OnClick?.Invoke(new PointerEvent(x, y, button, false));
            _pressedWidget.Invalidate();
            _pressedWidget = null;
        }
    }

    void IPlatformListener.Wheel(float deltaX, float deltaY)
    {
        // Walk up from the hovered widget to find the nearest scroll handler.
        var w = _hoveredWidget;
        while (w is not null)
        {
            if (w.OnScroll is not null) { w.OnScroll(deltaX, deltaY); return; }
            w = w.Parent;
        }
    }

    void IPlatformListener.KeyDown(Key key, KeyModifiers modifiers)
        => _focusedWidget?.OnKeyDown?.Invoke(new KeyEvent(key, modifiers, true));

    void IPlatformListener.KeyUp(Key key, KeyModifiers modifiers)
        => _focusedWidget?.OnKeyUp?.Invoke(new KeyEvent(key, modifiers, false));

    void IPlatformListener.TextEntered(string text)
        => _focusedWidget?.OnTextInput?.Invoke(new TextInputEvent(text));
}
