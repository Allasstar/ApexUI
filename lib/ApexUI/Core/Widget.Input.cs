namespace ApexUI.Core;

/// Input: every event reaches a widget through its protected virtual On… method (the widget's own
/// behavior, for subclasses) and then its public event (for users), so subscribing adds to a widget's
/// behavior instead of replacing it. See InputEventArgs for bubbling and Handled.
public abstract partial class Widget
{
    /// True while the pointer is over this widget or any of its descendants.
    public bool IsHovered { get; internal set; }

    /// True from a press on this widget until every pointer button is released.
    public bool IsPressed { get; internal set; }

    /// True while this widget receives key and text events.
    public bool IsFocused { get; internal set; }

    /// Can receive focus: by click, by Tab / Shift+Tab, or by Focus().
    public bool Focusable
    {
        get;
        set
        {
            if (field == value) return;
            field = value;
            if (!value && IsFocused) Host?.ClearFocus();
        }
    }

    /// Pointer shape while the pointer is over this widget (or while it has capture).
    /// Null uses the parent's cursor; the root default is an arrow.
    public CursorShape? Cursor
    {
        get;
        set
        {
            if (field == value) return;
            field = value;
            Host?.UpdateCursor();
        }
    }

    public bool HasPointerCapture => Host is { } host && host.PointerCapture == this;

    internal bool CanFocus => Focusable && IsEnabled && IsVisibleInTree;

    private bool IsVisibleInTree
    {
        get
        {
            for (var w = this; w is not null; w = w.Parent)
                if (!w.IsVisible) return false;
            return true;
        }
    }

    // Bubble from Source to the root until handled.
    public event Action<PointerEventArgs>?   PointerPressed;
    public event Action<PointerEventArgs>?   PointerReleased;
    public event Action<PointerEventArgs>?   PointerMoved;
    /// Left button pressed and released over the same widget (or its descendants).
    public event Action<PointerEventArgs>?   Click;
    public event Action<WheelEventArgs>?     Wheel;
    public event Action<KeyEventArgs>?       KeyDown;
    public event Action<KeyEventArgs>?       KeyUp;
    public event Action<TextInputEventArgs>? TextEntered;

    // Sent to each widget on its own, without bubbling.
    public event Action<PointerEventArgs>? PointerEntered;
    public event Action<PointerEventArgs>? PointerExited;
    public event Action? GotFocus;
    public event Action? LostFocus;
    public event Action? PointerCaptureLost;

    /// Moves keyboard focus here. Returns false when the widget is not shown, not Focusable,
    /// disabled or hidden.
    public bool Focus() => Host is { } host && host.SetFocus(this);

    /// Sends every pointer move and release to this widget (as Source) until all buttons are
    /// released or ReleasePointerCapture is called, even when the pointer leaves it.
    /// A press already captures the pressed widget; call this to take capture for an ancestor
    /// or to start a drag without a press.
    public bool CapturePointer() => Host is { } host && host.Capture(this);

    public void ReleasePointerCapture()
    {
        if (HasPointerCapture) Host!.ReleaseCapture();
    }

    protected virtual void OnPointerPressed(PointerEventArgs e)  { }
    protected virtual void OnPointerReleased(PointerEventArgs e) { }
    protected virtual void OnPointerMoved(PointerEventArgs e)    { }
    protected virtual void OnPointerEntered(PointerEventArgs e)  { }
    protected virtual void OnPointerExited(PointerEventArgs e)   { }
    protected virtual void OnClick(PointerEventArgs e)           { }
    protected virtual void OnWheel(WheelEventArgs e)             { }
    protected virtual void OnKeyDown(KeyEventArgs e)             { }
    protected virtual void OnKeyUp(KeyEventArgs e)               { }
    protected virtual void OnTextEntered(TextInputEventArgs e)   { }
    protected virtual void OnGotFocus()                          { }
    protected virtual void OnLostFocus()                         { }
    /// Capture ended: all buttons released, ReleasePointerCapture, another widget captured, or detach.
    /// Reset drag state here.
    protected virtual void OnPointerCaptureLost()                { }

    internal void RaiseInput(InputKind kind, InputEventArgs e)
    {
        switch (kind)
        {
            case InputKind.PointerPressed:  { var p = (PointerEventArgs)e; OnPointerPressed(p);  PointerPressed?.Invoke(p);  break; }
            case InputKind.PointerReleased: { var p = (PointerEventArgs)e; OnPointerReleased(p); PointerReleased?.Invoke(p); break; }
            case InputKind.PointerMoved:    { var p = (PointerEventArgs)e; OnPointerMoved(p);    PointerMoved?.Invoke(p);    break; }
            case InputKind.PointerEntered:  { var p = (PointerEventArgs)e; OnPointerEntered(p);  PointerEntered?.Invoke(p);  break; }
            case InputKind.PointerExited:   { var p = (PointerEventArgs)e; OnPointerExited(p);   PointerExited?.Invoke(p);   break; }
            case InputKind.Click:           { var p = (PointerEventArgs)e; OnClick(p);           Click?.Invoke(p);           break; }
            case InputKind.Wheel:           { var w = (WheelEventArgs)e;   OnWheel(w);           Wheel?.Invoke(w);           break; }
            case InputKind.KeyDown:         { var k = (KeyEventArgs)e;     OnKeyDown(k);         KeyDown?.Invoke(k);         break; }
            case InputKind.KeyUp:           { var k = (KeyEventArgs)e;     OnKeyUp(k);           KeyUp?.Invoke(k);           break; }
            case InputKind.TextEntered:     { var t = (TextInputEventArgs)e; OnTextEntered(t);   TextEntered?.Invoke(t);     break; }
        }
    }

    internal void RaiseGotFocus()
    {
        OnGotFocus();
        GotFocus?.Invoke();
    }

    internal void RaiseLostFocus()
    {
        OnLostFocus();
        LostFocus?.Invoke();
    }

    internal void RaisePointerCaptureLost()
    {
        OnPointerCaptureLost();
        PointerCaptureLost?.Invoke();
    }

    /// Drawn after the widget and its children while it has keyboard focus that came from the keyboard.
    /// Override to draw nothing when the widget shows focus its own way.
    protected virtual void DrawFocusRing(DrawContext ctx)
        => ctx.StrokeRoundRect(LayoutBounds, CornerRadius, ctx.Theme.FocusRing, ctx.Theme.FocusRingWidth);
}

internal enum InputKind
{
    PointerPressed, PointerReleased, PointerMoved, PointerEntered, PointerExited, Click,
    Wheel, KeyDown, KeyUp, TextEntered,
}
