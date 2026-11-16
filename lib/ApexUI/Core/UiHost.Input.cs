namespace ApexUI.Core;

// Input routing: hit testing, the hover chain, pointer capture, click counting, focus and key routing.
// Event argument objects are reused for every event, so routing allocates nothing.
public sealed partial class UiHost
{
    private static readonly TimeSpan MultiClickTime = TimeSpan.FromMilliseconds(500);
    private const float MultiClickDistance = 4f;

    private readonly PointerEventArgs   _pointerArgs = new();
    private readonly PointerEventArgs   _hoverArgs   = new();
    private readonly WheelEventArgs     _wheelArgs   = new();
    private readonly KeyEventArgs       _keyArgs     = new();
    private readonly TextInputEventArgs _textArgs    = new();

    // Deepest widget first. Two lists, swapped on every move, so updating hover allocates nothing.
    private List<Widget> _hoverChain     = [];
    private List<Widget> _nextHoverChain = [];
    private readonly List<Widget> _focusOrder = [];
    private readonly bool[] _keysDown = new bool[(int)Key.SuperRight + 1];

    private Widget? _capture;
    private Widget? _pressed;
    private Widget? _focused;
    private int     _buttonsDown;
    private PointerButton _pressButton;

    private float _pointerX = float.NaN;
    private float _pointerY = float.NaN;

    private int           _clickCount;
    private PointerButton _lastPressButton;
    private TimeSpan      _lastPressTime;
    private float         _lastPressX;
    private float         _lastPressY;

    private KeyModifiers _modifiers;
    private bool        _lastInputWasKeyboard;
    private CursorShape _cursor = CursorShape.Arrow;

    public Widget? FocusedWidget  => _focused;
    public Widget? PointerCapture => _capture;

    /// True when the focused widget got focus from the keyboard; widgets draw a focus ring only then.
    public bool IsFocusVisible { get; private set; }

    /// Key presses no focused widget handled, for app-wide shortcuts. Set Handled to stop
    /// the default handling that follows (Tab / Shift+Tab move focus).
    public event Action<KeyEventArgs>? UnhandledKeyDown;

    // ── Focus ─────────────────────────────────────────────────────────────────

    internal bool SetFocus(Widget widget)
    {
        if (widget.Host != this || !widget.CanFocus) return false;
        SetFocus(widget, _lastInputWasKeyboard);
        return true;
    }

    public void ClearFocus() => SetFocus(null, false);

    /// Moves focus to the next (or previous) focusable widget in tree order, wrapping around.
    /// Inside an open modal overlay, focus stays in it. Returns false when nothing can take focus.
    public bool MoveFocus(bool forward)
    {
        var scope = _root;
        for (int i = _overlays.Count - 1; i >= 0; i--)
            if (_overlays[i].IsModal) { scope = _overlays[i]; break; }

        _focusOrder.Clear();
        if (scope is not null) CollectFocusable(scope);
        if (_focusOrder.Count == 0) return false;

        int index = _focused is null ? -1 : _focusOrder.IndexOf(_focused);
        int next  = index < 0
            ? (forward ? 0 : _focusOrder.Count - 1)
            : (index + (forward ? 1 : -1) + _focusOrder.Count) % _focusOrder.Count;
        SetFocus(_focusOrder[next], fromKeyboard: true);
        return true;
    }

    private void CollectFocusable(Widget widget)
    {
        if (!widget.IsVisible || !widget.IsEnabled) return;
        if (widget.Focusable) _focusOrder.Add(widget);
        foreach (var child in widget.Children)
            CollectFocusable(child);
    }

    private void SetFocus(Widget? widget, bool fromKeyboard)
    {
        if (IsFocusVisible != fromKeyboard)
        {
            IsFocusVisible = fromKeyboard;
            RequestFrame();
        }
        if (widget == _focused) return;

        var old  = _focused;
        _focused = widget;
        if (old is not null)
        {
            old.IsFocused = false;
            old.Invalidate();
            old.RaiseLostFocus();
        }
        if (widget is not null && widget == _focused)
        {
            widget.IsFocused = true;
            widget.Invalidate();
            widget.RaiseGotFocus();
        }
    }

    private void FocusOnPress(Widget? hit)
    {
        var w = hit;
        while (w is not null && !w.CanFocus) w = w.Parent;
        SetFocus(w, fromKeyboard: false);
    }

    // ── Capture and cursor ────────────────────────────────────────────────────

    internal bool Capture(Widget widget)
    {
        if (widget.Host != this) return false;
        if (_capture == widget) return true;
        var old  = _capture;
        _capture = widget;
        old?.RaisePointerCaptureLost();
        UpdateCursor();
        return true;
    }

    internal void ReleaseCapture()
    {
        var old  = _capture;
        _capture = null;
        old?.RaisePointerCaptureLost();
        UpdateCursor();
    }

    internal void UpdateCursor()
    {
        var w = _capture ?? (_hoverChain.Count > 0 ? _hoverChain[0] : null);
        while (w is not null && w.Cursor is null) w = w.Parent;
        var cursor = w?.Cursor ?? CursorShape.Arrow;
        if (cursor == _cursor) return;
        _cursor = cursor;
        Platform.SetCursor(cursor);
    }

    // A detached widget must not keep receiving pointer or key events.
    internal void Forget(Widget widget)
    {
        if (widget.IsHovered)
        {
            widget.IsHovered = false;
            _hoverChain.Remove(widget);
        }
        if (_pressed == widget)
        {
            widget.IsPressed = false;
            _pressed = null;
        }
        if (_capture == widget) ReleaseCapture();
        if (_focused == widget) SetFocus(null, IsFocusVisible);
    }

    // ── Routing ───────────────────────────────────────────────────────────────

    // Positions are re-expressed in each receiver's layout space on the way up.
    private static void Route(Widget source, InputKind kind, InputEventArgs e)
    {
        e.Reset(source);
        var positioned = e as PositionedEventArgs;
        positioned?.MapTo(source);
        for (var w = source; ; )
        {
            w.RaiseInput(kind, e);
            if (e.Handled) return;
            var parent = w.Parent;
            if (parent is null) return;
            positioned?.MapToParent(parent);
            w = parent;
        }
    }

    private void FillPointer(PointerEventArgs e, float x, float y, PointerButton button, int clickCount)
    {
        e.SetWindowPosition(x, y);
        e.Button     = button;
        e.ClickCount = clickCount;
        e.Modifiers  = _modifiers;
    }

    // Enter and exit go to each widget that the pointer entered or left, outermost entered last.
    private void UpdateHover(Widget? hit, float x, float y)
    {
        _nextHoverChain.Clear();
        for (var w = hit; w is not null; w = w.Parent)
            _nextHoverChain.Add(w);

        var left = _hoverChain;
        (_hoverChain, _nextHoverChain) = (_nextHoverChain, _hoverChain);

        FillPointer(_hoverArgs, x, y, PointerButton.None, 0);
        for (int i = 0; i < left.Count; i++)
        {
            var w = left[i];
            if (!w.IsHovered || _hoverChain.Contains(w)) continue;
            w.IsHovered = false;
            w.Invalidate();
            _hoverArgs.Reset(w);
            _hoverArgs.MapTo(w);
            w.RaiseInput(InputKind.PointerExited, _hoverArgs);
        }
        for (int i = _hoverChain.Count - 1; i >= 0; i--)
        {
            if (i >= _hoverChain.Count) continue; // a handler removed widgets from the chain
            var w = _hoverChain[i];
            if (w.IsHovered || w.Host != this) continue;
            w.IsHovered = true;
            w.Invalidate();
            _hoverArgs.Reset(w);
            _hoverArgs.MapTo(w);
            w.RaiseInput(InputKind.PointerEntered, _hoverArgs);
        }
        UpdateCursor();
    }

    private int CountClick(PointerButton button, float x, float y)
    {
        var now = Platform.Now;
        bool repeat = button == _lastPressButton
            && now - _lastPressTime <= MultiClickTime
            && MathF.Abs(x - _lastPressX) <= MultiClickDistance
            && MathF.Abs(y - _lastPressY) <= MultiClickDistance;
        _clickCount      = repeat ? _clickCount + 1 : 1;
        _lastPressButton = button;
        _lastPressTime   = now;
        _lastPressX      = x;
        _lastPressY      = y;
        return _clickCount;
    }

    private static int Bit(PointerButton button) => 1 << (int)button;

    private static bool IsWithin(Widget? widget, Widget ancestor)
    {
        for (var w = widget; w is not null; w = w.Parent)
            if (w == ancestor) return true;
        return false;
    }

    // ── Platform events ───────────────────────────────────────────────────────

    void IPlatformListener.PointerMove(float px, float py, KeyModifiers modifiers)
    {
        EnsureLayout();
        _modifiers = modifiers;
        float x = _pointerX = px / PixelScale;
        float y = _pointerY = py / PixelScale;

        var hit = HitTestAll(x, y);
        UpdateHover(hit, x, y);

        // Captured moves reach the capturing widget even outside its bounds.
        var target = _capture ?? hit;
        if (target is null) return;
        FillPointer(_pointerArgs, x, y, PointerButton.None, 0);
        Route(target, InputKind.PointerMoved, _pointerArgs);
    }

    void IPlatformListener.PointerDown(float px, float py, PointerButton button, KeyModifiers modifiers)
    {
        EnsureLayout();
        _modifiers = modifiers;
        _lastInputWasKeyboard = false;
        float x = _pointerX = px / PixelScale;
        float y = _pointerY = py / PixelScale;

        var hit = HitTestAll(x, y);
        UpdateHover(hit, x, y);
        int clicks = CountClick(button, x, y);

        if (_buttonsDown == 0)
        {
            _pressButton = button;
            FocusOnPress(hit);
            if (hit is not null && hit.Host == this)
            {
                _pressed      = hit;
                hit.IsPressed = true;
                hit.Invalidate();
                if (_capture is null) Capture(hit);
            }
        }
        _buttonsDown |= Bit(button);

        var target = _capture ?? hit;
        if (target is null) return;
        FillPointer(_pointerArgs, x, y, button, clicks);
        Route(target, InputKind.PointerPressed, _pointerArgs);
    }

    void IPlatformListener.PointerUp(float px, float py, PointerButton button, KeyModifiers modifiers)
    {
        EnsureLayout();
        _modifiers = modifiers;
        float x = _pointerX = px / PixelScale;
        float y = _pointerY = py / PixelScale;

        var hit = HitTestAll(x, y);
        UpdateHover(hit, x, y);

        // A release whose press happened outside the window has nothing to finish.
        if ((_buttonsDown & Bit(button)) == 0) return;
        _buttonsDown &= ~Bit(button);

        var target = _capture ?? hit;
        if (target is not null)
        {
            FillPointer(_pointerArgs, x, y, button, _clickCount);
            Route(target, InputKind.PointerReleased, _pointerArgs);
        }
        if (_buttonsDown != 0) return;

        var pressed = _pressed;
        _pressed = null;
        if (pressed is not null)
        {
            pressed.IsPressed = false;
            pressed.Invalidate();
        }
        if (_capture is not null) ReleaseCapture();

        if (button == PointerButton.Left && _pressButton == PointerButton.Left
            && pressed is not null && pressed.Host == this && IsWithin(hit, pressed))
        {
            FillPointer(_pointerArgs, x, y, button, _clickCount);
            Route(pressed, InputKind.Click, _pointerArgs);
        }
    }

    void IPlatformListener.Wheel(float deltaX, float deltaY, KeyModifiers modifiers)
    {
        if (float.IsNaN(_pointerX)) return;
        EnsureLayout();
        _modifiers = modifiers;

        var hit = HitTestAll(_pointerX, _pointerY);
        if (hit is null) return;
        _wheelArgs.SetWindowPosition(_pointerX, _pointerY);
        _wheelArgs.DeltaX    = deltaX;
        _wheelArgs.DeltaY    = deltaY;
        _wheelArgs.Modifiers = modifiers;
        Route(hit, InputKind.Wheel, _wheelArgs);

        // Scrolling moved content under a still pointer.
        EnsureLayout();
        UpdateHover(HitTestAll(_pointerX, _pointerY), _pointerX, _pointerY);
    }

    void IPlatformListener.KeyDown(Key key, KeyModifiers modifiers)
    {
        _modifiers = modifiers;
        _lastInputWasKeyboard = true;
        bool repeat = key != Key.Unknown && _keysDown[(int)key];
        if (key != Key.Unknown) _keysDown[(int)key] = true;

        FillKey(key, modifiers, repeat);
        if (_focused is not null)
            Route(_focused, InputKind.KeyDown, _keyArgs);
        if (_keyArgs.Handled) return;

        UnhandledKeyDown?.Invoke(_keyArgs);
        if (_keyArgs.Handled) return;

        if (key == Key.Tab && (modifiers & ~KeyModifiers.Shift) == 0)
            MoveFocus(forward: (modifiers & KeyModifiers.Shift) == 0);
    }

    void IPlatformListener.KeyUp(Key key, KeyModifiers modifiers)
    {
        _modifiers = modifiers;
        if (key != Key.Unknown) _keysDown[(int)key] = false;
        if (_focused is null) return;
        FillKey(key, modifiers, repeat: false);
        Route(_focused, InputKind.KeyUp, _keyArgs);
    }

    void IPlatformListener.TextEntered(string text)
    {
        if (_focused is null) return;
        _textArgs.Text      = text;
        _textArgs.Modifiers = _modifiers;
        Route(_focused, InputKind.TextEntered, _textArgs);
    }

    private void FillKey(Key key, KeyModifiers modifiers, bool repeat)
    {
        _keyArgs.Reset(_focused ?? _root!);
        _keyArgs.Key       = key;
        _keyArgs.Modifiers = modifiers;
        _keyArgs.IsRepeat  = repeat;
    }
}
