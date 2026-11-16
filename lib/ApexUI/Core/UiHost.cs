namespace ApexUI.Core;

/// The platform-neutral side of an app: owns the widget tree, lays it out, draws it,
/// routes input to widgets and keeps overlays, focus and theme. A backend (IPlatform)
/// supplies the window, the canvas and the raw input events.
///
///   new UiHost(new DesktopPlatform("My App", 900, 700)).Run(root);
public sealed partial class UiHost : IPlatformListener
{
    private readonly List<Overlay> _overlays = [];
    private DrawContext? _drawContext;

    // Swapped each frame so callbacks can request the next frame while the current list runs.
    private List<Action<TimeSpan>> _frameCallbacks        = [];
    private List<Action<TimeSpan>> _runningFrameCallbacks = [];

    private bool _frameRequested;
    private bool _rendering;
    private bool _overlaysNeedLayout;

    private Widget? _root;

    private float _pixelWidth;
    private float _pixelHeight;

    private string  _preset;
    private bool    _isDark;
    private string? _fontFamily;

    public UiHost(IPlatform platform)
    {
        Platform = platform;
        _preset  = Themes.DefaultName;
    }

    public IPlatform Platform { get; }

    /// Theme pairs BindTheme chooses from; starts with BuiltInThemes.
    public ThemeRegistry Themes { get; } = new();

    /// The theme every widget is laid out and drawn with. Setting it re-measures the whole tree.
    /// BindTheme, BindDarkMode and BindFontFamily set it from Themes.
    public Theme Theme
    {
        get;
        set
        {
            if (field == value) return;
            field = value;
            InvalidateAllMeasurements();
        }
    } = Theme.Light;

    /// Surface pixels per logical unit reported by the platform (display scaling).
    public float DpiScale { get; private set; } = 1f;

    public float UiScale
    {
        get;
        set
        {
            value = Math.Clamp(value, 0.1f, 10f);
            if (field == value) return;
            field = value;
            InvalidateAllLayout();
        }
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

    // Bumped when every widget's measurement may change (theme, font); widgets compare it with
    // the generation they were measured at instead of the host walking the tree.
    internal int LayoutGeneration { get; private set; } = 1;

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

    /// Overrides the font family of whichever theme BindTheme and BindDarkMode select.
    public UiHost BindFontFamily(Bindable<string> source)
    {
        _fontFamily = source.Value;
        ApplyTheme();
        source.Changed += v => { _fontFamily = v; ApplyTheme(); };
        return this;
    }

    private void ApplyTheme()
    {
        var theme = Themes.Get(_preset, _isDark);
        Theme = _fontFamily is null || _fontFamily == theme.FontFamily ? theme : theme with { FontFamily = _fontFamily };
    }

    // ── Frames ────────────────────────────────────────────────────────────────

    /// Calls callback once, just before the next frame is laid out and drawn, with that frame's time.
    /// Request again from inside the callback to animate; frames stop once nothing is requested.
    /// Pass a cached delegate (a field), not a new lambda per frame, to keep animations allocation-free.
    public void RequestAnimationFrame(Action<TimeSpan> callback)
    {
        _frameCallbacks.Add(callback);
        RequestFrame();
    }

    // Requests made while a frame is being rendered are collected and sent to the platform once it ends.
    internal void RequestFrame()
    {
        if (_frameRequested) return;
        _frameRequested = true;
        if (!_rendering) Platform.RequestFrame();
    }

    private void InvalidateAllMeasurements()
    {
        LayoutGeneration++;
        InvalidateAllLayout();
    }

    private void InvalidateAllLayout()
    {
        _root?.InvalidateLayout();
        _overlaysNeedLayout = true;
        RequestFrame();
    }

    private void RunFrameCallbacks(TimeSpan frameTime)
    {
        (_frameCallbacks, _runningFrameCallbacks) = (_runningFrameCallbacks, _frameCallbacks);
        foreach (var callback in _runningFrameCallbacks)
            callback(frameTime);
        _runningFrameCallbacks.Clear();
    }

    // ── Overlays ──────────────────────────────────────────────────────────────

    internal void ShowOverlay(Overlay overlay)
    {
        if (!_overlays.Contains(overlay)) _overlays.Add(overlay);
        RequestFrame();
    }

    internal void HideOverlay(Overlay overlay)
    {
        if (_overlays.Remove(overlay)) RequestFrame();
    }

    internal int OpenOverlayCount => _overlays.Count;

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
        InvalidateAllLayout();
    }

    void IPlatformListener.Render(SKCanvas canvas, TimeSpan frameTime)
    {
        _rendering      = true;
        _frameRequested = false;

        RunFrameCallbacks(frameTime);
        EnsureLayout();
        // Whatever the callbacks and layout changed is drawn below, so it needs no further frame.
        _frameRequested = false;

        canvas.Clear(Theme.Background);
        canvas.Save();
        canvas.Scale(PixelScale, PixelScale);

        _drawContext ??= new DrawContext(canvas, Theme, PixelScale, Fonts);
        _drawContext.BeginFrame(canvas, Theme, PixelScale);
        _root?.Draw(_drawContext);
        // Overlays are drawn after the root tree so they appear above everything
        // without inheriting any clip stack.
        foreach (var o in _overlays)
            o.Draw(_drawContext);

        canvas.Restore();

        _rendering = false;
        if (_frameCallbacks.Count > 0) _frameRequested = true;
        if (_frameRequested) Platform.RequestFrame();
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
        // Anchored overlays follow their anchors, which may have moved.
        _overlaysNeedLayout = true;
    }

    // Input can arrive between a change and the frame that lays it out; hit-test the layout the user will see.
    private void EnsureLayout()
    {
        if (_root is not null && _root.IsLayoutDirty)
            LayoutRoot();
        LayoutOverlays();
    }

    // Overlays fill the window (their backdrop is hit-testable) and are laid out only when
    // they, the window or the root tree changed. Anchored ones are re-arranged every time,
    // because scrolling moves an anchor without any layout; that costs a few cached calls.
    private void LayoutOverlays()
    {
        var size = LogicalWindowSize;
        foreach (var o in _overlays)
        {
            if (!_overlaysNeedLayout && !o.IsLayoutDirty && o.Anchor is null) continue;
            o.InvalidateArrange();
            o.Measure(size);
            o.Arrange(new Rect(0, 0, size.Width, size.Height));
        }
        _overlaysNeedLayout = false;
    }
}
