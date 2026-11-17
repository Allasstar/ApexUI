namespace ApexUI.Core;

/// A key with the exact modifiers that must be held, for example Ctrl+Shift+Z.
/// Parse accepts "Mod" for the platform's primary shortcut modifier: Command on macOS, Ctrl elsewhere.
///
///   KeyGesture.Parse("Mod+S")     // Ctrl+S on Windows and Linux, Cmd+S on macOS
///   new KeyGesture(Key.F)          // F with no modifiers
public readonly record struct KeyGesture(Key Key, KeyModifiers Modifiers = KeyModifiers.None)
{
    /// Command on macOS, Control elsewhere.
    public static KeyModifiers PrimaryModifier { get; } = OperatingSystem.IsMacOS() ? KeyModifiers.Super : KeyModifiers.Control;

    public bool Matches(KeyEventArgs e) => e.Key == Key && e.Modifiers == Modifiers;

    /// Parses "Ctrl+Shift+Z", "Mod+O", "Alt+F4", "Equal" or "F". Modifier names: Ctrl, Shift, Alt, Super
    /// (also Cmd, Win, Meta) and Mod. The key is a Key name; single digits are accepted for D0..D9.
    public static KeyGesture Parse(string text)
    {
        if (TryParse(text, out var gesture)) return gesture;
        throw new FormatException($"'{text}' is not a key gesture.");
    }

    public static bool TryParse(string text, out KeyGesture gesture)
    {
        gesture = default;
        var parts = text.Split('+', StringSplitOptions.TrimEntries);
        if (parts.Length == 0 || parts[^1].Length == 0) return false;

        var modifiers = KeyModifiers.None;
        for (int i = 0; i < parts.Length - 1; i++)
        {
            if (!TryParseModifier(parts[i], out var modifier)) return false;
            modifiers |= modifier;
        }

        if (!TryParseKey(parts[^1], out var key)) return false;
        gesture = new KeyGesture(key, modifiers);
        return true;
    }

    /// Readable form for tooltips and menus, for example "Ctrl+Shift+Z" or "Cmd+S".
    public override string ToString()
    {
        string text = "";
        if ((Modifiers & KeyModifiers.Control) != 0) text += "Ctrl+";
        if ((Modifiers & KeyModifiers.Alt) != 0)     text += OperatingSystem.IsMacOS() ? "Option+" : "Alt+";
        if ((Modifiers & KeyModifiers.Shift) != 0)   text += "Shift+";
        if ((Modifiers & KeyModifiers.Super) != 0)   text += OperatingSystem.IsMacOS() ? "Cmd+" : "Win+";
        return text + KeyName(Key);
    }

    private static bool TryParseModifier(string name, out KeyModifiers modifier)
    {
        modifier = name.ToLowerInvariant() switch
        {
            "ctrl" or "control"           => KeyModifiers.Control,
            "shift"                       => KeyModifiers.Shift,
            "alt" or "option"             => KeyModifiers.Alt,
            "super" or "cmd" or "win" or "meta" => KeyModifiers.Super,
            "mod"                         => PrimaryModifier,
            _                             => KeyModifiers.None,
        };
        return modifier != KeyModifiers.None;
    }

    private static bool TryParseKey(string name, out Key key)
    {
        if (name.Length == 1 && char.IsAsciiDigit(name[0]))
        {
            key = Key.D0 + (name[0] - '0');
            return true;
        }

        return Enum.TryParse(name, ignoreCase: true, out key) && key != Key.Unknown && Enum.IsDefined(key);
    }

    private static string KeyName(Key key) => key switch
    {
        >= Key.D0 and <= Key.D9 => ((char)('0' + (key - Key.D0))).ToString(),
        Key.Equal               => "=",
        Key.Minus               => "-",
        Key.KeypadAdd           => "Num +",
        Key.KeypadSubtract      => "Num -",
        _                       => key.ToString(),
    };
}
