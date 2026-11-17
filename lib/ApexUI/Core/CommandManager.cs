namespace ApexUI.Core;

/// App-wide commands and their keyboard shortcuts. Attached to a host, it runs the command whose gesture
/// matches a key press that no focused widget handled. Shortcuts are ignored while a modal overlay is open,
/// and, with SuspendWhileEditingText, while a text field has focus (so typing "B" in a field stays typing).
///
///   var commands = new CommandManager();
///   var undo = commands.Add("edit.undo", "Undo", history.Undo, "Mod+Z");
///   commands.Attach(host);
///   toolbar.Add(new IconButton(undoIcon).WithCommand(undo));
public sealed class CommandManager
{
    private readonly List<Command> _commands = [];
    private readonly Action<KeyEventArgs> _onKeyDown;
    private UiHost? _host;

    public CommandManager() => _onKeyDown = e => HandleKey(e);

    public IReadOnlyList<Command> Commands => _commands;

    /// Ignore shortcuts while the focused widget edits text (EditsText). Default true.
    public bool SuspendWhileEditingText { get; set; } = true;

    /// Raised after a command runs from its shortcut or from Execute(name).
    public event Action<Command>? Executed;

    /// Registers a command. Gestures use KeyGesture.Parse syntax ("Mod+Shift+Z", "F", "Equal").
    public Command Add(string name, string label, Action execute, params string[] gestures)
    {
        var parsed = new KeyGesture[gestures.Length];
        for (int i = 0; i < gestures.Length; i++) parsed[i] = KeyGesture.Parse(gestures[i]);
        return Add(name, label, execute, parsed);
    }

    public Command Add(string name, string label, Action execute, KeyGesture[] gestures)
    {
        if (Find(name) is not null) throw new ArgumentException($"A command named '{name}' already exists.", nameof(name));
        var command = new Command(name, label, execute, gestures);
        _commands.Add(command);
        return command;
    }

    public Command? Find(string name)
    {
        foreach (var command in _commands)
            if (command.Name == name) return command;
        return null;
    }

    public Command this[string name] => Find(name) ?? throw new KeyNotFoundException($"No command named '{name}'.");

    public bool Execute(string name)
    {
        var command = this[name];
        if (!command.Execute()) return false;
        Executed?.Invoke(command);
        return true;
    }

    /// Listens to the host's unhandled key presses. A manager serves one host at a time.
    public void Attach(UiHost host)
    {
        if (_host == host) return;
        Detach();
        _host = host;
        host.UnhandledKeyDown += _onKeyDown;
    }

    public void Detach()
    {
        if (_host is null) return;
        _host.UnhandledKeyDown -= _onKeyDown;
        _host = null;
    }

    /// Runs the first enabled command matching the key and marks the event handled. Attach calls this for
    /// unhandled keys; call it directly to route keys from elsewhere.
    public bool HandleKey(KeyEventArgs e)
    {
        if (e.Handled || IsSuspended()) return false;

        foreach (var command in _commands)
        {
            if (!command.Matches(e) || !command.Execute()) continue;
            e.Handled = true;
            Executed?.Invoke(command);
            return true;
        }

        return false;
    }

    private bool IsSuspended()
        => _host is not null
        && (_host.HasModalOverlay || SuspendWhileEditingText && _host.FocusedWidget is { EditsText: true });
}
