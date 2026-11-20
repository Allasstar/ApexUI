namespace ApexUI.Widgets;

// Keys that press a focused button-like widget, like a click.
internal static class Activation
{
    public static bool IsActivationKey(KeyEventArgs e)
        => !e.IsRepeat
        && e.Modifiers == KeyModifiers.None
        && e.Key is Key.Space or Key.Enter or Key.KeypadEnter;

    // Down opens a closed menu or list, once per press.
    public static bool IsOpenKey(KeyEventArgs e)
        => !e.IsRepeat
        && e.Modifiers == KeyModifiers.None
        && e.Key == Key.Down;
}
