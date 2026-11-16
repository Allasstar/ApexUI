namespace ApexUI.Core;

public record struct PointerEvent(
    float  X,
    float  Y,
    PointerButton Button,
    bool   IsDown
);

public enum PointerButton { None, Left, Right, Middle }

public readonly record struct KeyEvent(Key Key, KeyModifiers Modifiers, bool IsDown)
{
    public bool Ctrl  => (Modifiers & KeyModifiers.Control) != 0;
    public bool Shift => (Modifiers & KeyModifiers.Shift)   != 0;
    public bool Alt   => (Modifiers & KeyModifiers.Alt)     != 0;
    public bool Super => (Modifiers & KeyModifiers.Super)   != 0;
}

/// Typed text, separate from KeyDown so a widget can tell typing from shortcuts.
public readonly record struct TextInputEvent(string Text);

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

/// Physical key, independent of keyboard layout. Use TextInputEvent for typed characters.
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
