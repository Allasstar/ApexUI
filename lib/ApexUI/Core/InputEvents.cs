namespace ApexUI.Core;

/// Base of every input event. Routed events start at Source and bubble to its ancestors
/// until a widget sets Handled. On one widget, its own handling (the virtual On… method)
/// and every subscriber run; Handled only stops the event from reaching the next ancestor.
///
/// The host reuses one instance per event kind, so read what you need during the call
/// and do not keep the object.
public abstract class InputEventArgs
{
    /// The widget the event started at: the hit widget, the captured widget, or the focused widget.
    public Widget Source { get; internal set; } = null!;

    public bool Handled { get; set; }

    public KeyModifiers Modifiers { get; internal set; }

    public bool Ctrl  => (Modifiers & KeyModifiers.Control) != 0;
    public bool Shift => (Modifiers & KeyModifiers.Shift)   != 0;
    public bool Alt   => (Modifiers & KeyModifiers.Alt)     != 0;
    public bool Super => (Modifiers & KeyModifiers.Super)   != 0;

    internal void Reset(Widget source)
    {
        Source  = source;
        Handled = false;
    }
}

/// An event at a pointer position.
public abstract class PositionedEventArgs : InputEventArgs
{
    /// Position in the receiving widget's layout space, the space of its LayoutBounds.
    /// Updated as the event bubbles past an ancestor with a ChildTransform (a scrolled Scroll).
    public float X { get; private set; }
    public float Y { get; private set; }

    /// Position in logical window coordinates, the same for every receiver. Use it to open overlays.
    public float WindowX { get; private set; }
    public float WindowY { get; private set; }

    internal void SetWindowPosition(float x, float y)
    {
        WindowX = X = x;
        WindowY = Y = y;
    }

    internal void MapTo(Widget widget) => (X, Y) = widget.PointFromWindow(WindowX, WindowY);

    internal void MapToParent(Widget parent) => (X, Y) = parent.ChildTransform.Apply(X, Y);
}

public sealed class PointerEventArgs : PositionedEventArgs
{
    /// The button that changed; None for moves, enter and exit.
    public PointerButton Button { get; internal set; }

    /// 1 for a single press, 2 for a double press, 3 for a triple press, and so on.
    /// Counts presses of the same button close together in time and place.
    public int ClickCount { get; internal set; }
}

/// Deltas are in wheel notches; positive DeltaY means the wheel turned away from the user (scroll up).
public sealed class WheelEventArgs : PositionedEventArgs
{
    public float DeltaX { get; internal set; }
    public float DeltaY { get; internal set; }
}

public sealed class KeyEventArgs : InputEventArgs
{
    public Key Key { get; internal set; }

    /// True for the auto-repeated KeyDown events sent while a key is held.
    public bool IsRepeat { get; internal set; }
}

/// Typed text, separate from KeyDown so a widget can tell typing from shortcuts.
public sealed class TextInputEventArgs : InputEventArgs
{
    public string Text { get; internal set; } = "";
}

public enum PointerButton { None, Left, Right, Middle }

[Flags]
public enum KeyModifiers
{
    None    = 0,
    Shift   = 1,
    Control = 2,
    Alt     = 4,
    /// Windows key, Command on macOS.
    Super   = 8,
}

/// Physical key, independent of keyboard layout. Use the TextEntered event for typed characters.
public enum Key
{
    Unknown,

    A, B, C, D, E, F, G, H, I, J, K, L, M, N, O, P, Q, R, S, T, U, V, W, X, Y, Z,
    D0, D1, D2, D3, D4, D5, D6, D7, D8, D9,
    F1, F2, F3, F4, F5, F6, F7, F8, F9, F10, F11, F12,

    Space, Enter, Tab, Backspace, Escape, Insert, Delete,
    Left, Right, Up, Down, Home, End, PageUp, PageDown,
    CapsLock, ScrollLock, NumLock, PrintScreen, Pause, Menu,

    Apostrophe, Comma, Minus, Period, Slash, Semicolon, Equal,
    LeftBracket, Backslash, RightBracket, GraveAccent,

    Keypad0, Keypad1, Keypad2, Keypad3, Keypad4, Keypad5, Keypad6, Keypad7, Keypad8, Keypad9,
    KeypadDecimal, KeypadDivide, KeypadMultiply, KeypadSubtract, KeypadAdd, KeypadEnter, KeypadEqual,

    ShiftLeft, ShiftRight, ControlLeft, ControlRight, AltLeft, AltRight, SuperLeft, SuperRight,
}
