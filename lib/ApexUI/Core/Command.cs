namespace ApexUI.Core;

/// A named action with optional keyboard shortcuts and an enabled state, registered in a CommandManager.
/// Buttons bound with WithCommand run it and follow its enabled state.
public sealed class Command
{
    private readonly Action _execute;
    private readonly KeyGesture[] _gestures;

    internal Command(string name, string label, Action execute, KeyGesture[] gestures)
    {
        Name      = name;
        Label     = label;
        _execute  = execute;
        _gestures = gestures;
    }

    public string Name  { get; }
    public string Label { get; }

    public ReadOnlySpan<KeyGesture> Gestures => _gestures;

    /// A disabled command ignores Execute and its shortcuts; bound buttons show as disabled.
    public Bindable<bool> Enabled { get; } = new(true);

    public bool IsEnabled
    {
        get => Enabled.Value;
        set => Enabled.Value = value;
    }

    /// The label with the first shortcut, for tooltips: "Undo (Ctrl+Z)".
    public string Tooltip => _gestures.Length == 0 ? Label : $"{Label} ({_gestures[0]})";

    /// Runs the command if it is enabled. Returns whether it ran.
    public bool Execute()
    {
        if (!IsEnabled) return false;
        _execute();
        return true;
    }

    internal bool Matches(KeyEventArgs e)
    {
        foreach (var gesture in _gestures)
            if (gesture.Matches(e)) return true;
        return false;
    }
}
