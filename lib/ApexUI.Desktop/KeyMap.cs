using SilkKey = Silk.NET.Input.Key;

namespace ApexUI.Desktop;

internal static class KeyMap
{
    public static Key From(SilkKey k)
    {
        if (k is >= SilkKey.A and <= SilkKey.Z)               return Key.A + (k - SilkKey.A);
        if (k is >= SilkKey.Number0 and <= SilkKey.Number9)   return Key.D0 + (k - SilkKey.Number0);
        if (k is >= SilkKey.F1 and <= SilkKey.F12)            return Key.F1 + (k - SilkKey.F1);
        if (k is >= SilkKey.Keypad0 and <= SilkKey.Keypad9)   return Key.Keypad0 + (k - SilkKey.Keypad0);

        return k switch
        {
            SilkKey.Space          => Key.Space,
            SilkKey.Enter          => Key.Enter,
            SilkKey.Tab            => Key.Tab,
            SilkKey.Backspace      => Key.Backspace,
            SilkKey.Escape         => Key.Escape,
            SilkKey.Insert         => Key.Insert,
            SilkKey.Delete         => Key.Delete,
            SilkKey.Left           => Key.Left,
            SilkKey.Right          => Key.Right,
            SilkKey.Up             => Key.Up,
            SilkKey.Down           => Key.Down,
            SilkKey.Home           => Key.Home,
            SilkKey.End            => Key.End,
            SilkKey.PageUp         => Key.PageUp,
            SilkKey.PageDown       => Key.PageDown,
            SilkKey.CapsLock       => Key.CapsLock,
            SilkKey.ScrollLock     => Key.ScrollLock,
            SilkKey.NumLock        => Key.NumLock,
            SilkKey.PrintScreen    => Key.PrintScreen,
            SilkKey.Pause          => Key.Pause,
            SilkKey.Menu           => Key.Menu,
            SilkKey.Apostrophe     => Key.Apostrophe,
            SilkKey.Comma          => Key.Comma,
            SilkKey.Minus          => Key.Minus,
            SilkKey.Period         => Key.Period,
            SilkKey.Slash          => Key.Slash,
            SilkKey.Semicolon      => Key.Semicolon,
            SilkKey.Equal          => Key.Equal,
            SilkKey.LeftBracket    => Key.LeftBracket,
            SilkKey.BackSlash      => Key.Backslash,
            SilkKey.RightBracket   => Key.RightBracket,
            SilkKey.GraveAccent    => Key.GraveAccent,
            SilkKey.KeypadDecimal  => Key.KeypadDecimal,
            SilkKey.KeypadDivide   => Key.KeypadDivide,
            SilkKey.KeypadMultiply => Key.KeypadMultiply,
            SilkKey.KeypadSubtract => Key.KeypadSubtract,
            SilkKey.KeypadAdd      => Key.KeypadAdd,
            SilkKey.KeypadEnter    => Key.KeypadEnter,
            SilkKey.KeypadEqual    => Key.KeypadEqual,
            SilkKey.ShiftLeft      => Key.ShiftLeft,
            SilkKey.ShiftRight     => Key.ShiftRight,
            SilkKey.ControlLeft    => Key.ControlLeft,
            SilkKey.ControlRight   => Key.ControlRight,
            SilkKey.AltLeft        => Key.AltLeft,
            SilkKey.AltRight       => Key.AltRight,
            SilkKey.SuperLeft      => Key.SuperLeft,
            SilkKey.SuperRight     => Key.SuperRight,
            _                      => Key.Unknown,
        };
    }
}
